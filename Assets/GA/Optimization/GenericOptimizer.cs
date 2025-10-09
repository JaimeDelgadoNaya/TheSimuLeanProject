using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeneticSharp.Domain;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;
using GeneticSharp.Infrastructure.Framework.Threading;
using ChapasGA.GA.Core;
using ChapasGA.GA.Utils;

namespace ChapasGA.GA.Optimization
{
    /// <summary>
    /// Generic optimizer for sequence and/or binary decision optimization.
    /// Unified API for both synchronous and asynchronous GA execution.
    /// Supports parallel fitness evaluation for faster optimization.
    /// </summary>
    /// <typeparam name="TData">The type of data items being optimized</typeparam>
    public class GenericOptimizer<TData>
    {
        private readonly ISimulationEvaluator<TData, SimulationMetrics> evaluator;
        private CancellationTokenSource cancellationTokenSource;

        // Configuration
        public int SequenceLength { get; }
        public int BinaryLength { get; }
        public bool HasSequence => SequenceLength > 0;
        public bool HasBinary => BinaryLength > 0;

        // Results
        public double BestFitness { get; private set; }
        public int TotalInspections { get; private set; }
        public int TotalDelays { get; private set; }
        public IList<int> BestSequence { get; private set; }
        public int[] BestBinaryDecisions { get; private set; }

        // Events for async execution
        public event Action<OptimizationProgressEventArgs> ProgressChanged;
        public event Action<OptimizationCompletedEventArgs> Completed;
        
        // Logging callback
        public Action<string> LogCallback { get; set; }
        
        // Parallel execution settings
        public bool EnableParallelEvaluation { get; set; } = false;
        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// Creates a generic optimizer.
        /// </summary>
        /// <param name="evaluator">Simulation evaluator</param>
        /// <param name="sequenceLength">Length of sequence optimization (0 if not used)</param>
        /// <param name="binaryLength">Length of binary optimization (0 if not used)</param>
        public GenericOptimizer(
            ISimulationEvaluator<TData, SimulationMetrics> evaluator,
            int sequenceLength,
            int binaryLength)
        {
            if (sequenceLength < 0 || binaryLength < 0)
                throw new ArgumentException("Lengths cannot be negative");
            
            if (sequenceLength == 0 && binaryLength == 0)
                throw new ArgumentException("At least one optimization type must be specified");

            this.evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            this.SequenceLength = sequenceLength;
            this.BinaryLength = binaryLength;
        }

        #region Synchronous API

        /// <summary>
        /// Runs GA optimization synchronously (blocks calling thread).
        /// </summary>
        public void Optimize(
            IList<TData> data,
            int populationSize,
            int generations,
            float crossoverProb = 0.9f,
            float mutationProb = 0.15f)
        {
            RunGAInternal(data, populationSize, generations, crossoverProb, mutationProb, CancellationToken.None);
        }

        #endregion

        #region Asynchronous API

        /// <summary>
        /// Runs GA optimization asynchronously (non-blocking).
        /// </summary>
        public async Task OptimizeAsync(
            IList<TData> data,
            int populationSize,
            int generations,
            float crossoverProb = 0.9f,
            float mutationProb = 0.15f)
        {
            cancellationTokenSource = new CancellationTokenSource();
            
            await Task.Run(() => RunGAInternal(
                data,
                populationSize,
                generations,
                crossoverProb,
                mutationProb,
                cancellationTokenSource.Token));
        }

        /// <summary>
        /// Cancels the running async optimization.
        /// </summary>
        public void Cancel()
        {
            cancellationTokenSource?.Cancel();
        }

        #endregion

        #region Core GA Logic

        private void RunGAInternal(
            IList<TData> data,
            int populationSize,
            int generations,
            float crossoverProb,
            float mutationProb,
            CancellationToken cancellationToken)
        {
            try
            {
                Log($"[GenericOptimizer] Starting optimization for {data.Count} items");
                Log($"[GenericOptimizer] Configuration: Sequence={HasSequence} (len={SequenceLength}), Binary={HasBinary} (len={BinaryLength})");
                
                if (EnableParallelEvaluation)
                {
                    Log($"[GenericOptimizer] Parallel evaluation ENABLED (max {MaxDegreeOfParallelism} threads)");
                }
                else
                {
                    Log($"[GenericOptimizer] Parallel evaluation DISABLED (sequential mode)");
                }

                // Create GA components
                var chromosome = new SequenceBinaryChromosome(SequenceLength, BinaryLength);
                var fitness = new GenericFitnessAdapter(data, evaluator);
                var population = new Population(populationSize, populationSize, chromosome);
                var selection = new TournamentSelection();
                var crossover = new SequenceBinaryCrossover();
                var mutation = new SequenceBinaryMutation();
                
                var ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation)
                {
                    CrossoverProbability = crossoverProb,
                    MutationProbability = mutationProb,
                    Termination = new GenerationNumberTermination(generations)
                };
                
                // Configure parallel execution if enabled
                if (EnableParallelEvaluation)
                {
                    ga.TaskExecutor = new ParallelTaskExecutor
                    {
                        MinThreads = 1,
                        MaxThreads = MaxDegreeOfParallelism
                    };
                }

                // Subscribe to progress events
                ga.GenerationRan += (sender, e) =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        ga.Stop();
                        return;
                    }

                    ReportProgress(ga, generations, fitness);
                };

                // Run GA
                Log("[GenericOptimizer] Starting GA execution...");
                ga.Start();
                Log("[GenericOptimizer] GA execution completed");

                // Extract results
                ExtractResults(ga.BestChromosome as SequenceBinaryChromosome, fitness);

                // Notify completion
                NotifyCompleted(true, null, ga.TimeEvolving.TotalSeconds);
            }
            catch (Exception ex)
            {
                Log($"[GenericOptimizer] Error: {ex.Message}");
                NotifyCompleted(false, ex.Message, 0);
            }
        }

        private void ReportProgress(GeneticAlgorithm ga, int totalGenerations, GenericFitnessAdapter fitness)
        {
            try
            {
                var currentGen = ga.GenerationsNumber;
                var bestFitness = ga.BestChromosome.Fitness.Value;
                var elapsed = ga.TimeEvolving.TotalSeconds;

                var bestChromo = ga.BestChromosome as SequenceBinaryChromosome;
                var metrics = fitness.EvaluateDetailed(bestChromo);

                Log($"[GenericOptimizer] Gen {currentGen}/{totalGenerations} | " +
                    $"Fitness: {bestFitness:F2} | Inspections: {metrics.TotalInspections} | " +
                    $"Delays: {metrics.TotalDelays} | Time: {elapsed:F1}s");

                var progressEvent = ProgressChanged;
                if (progressEvent != null)
                {
                    var args = new OptimizationProgressEventArgs
                    {
                        CurrentGeneration = currentGen,
                        TotalGenerations = totalGenerations,
                        BestFitness = bestFitness,
                        Inspections = metrics.TotalInspections,
                        Delays = metrics.TotalDelays,
                        ElapsedSeconds = elapsed
                    };
                    
                    Task.Run(() => progressEvent.Invoke(args));
                }
            }
            catch (Exception ex)
            {
                Log($"[GenericOptimizer] Error in progress reporting: {ex.Message}");
            }
        }

        private void ExtractResults(SequenceBinaryChromosome bestChromosome, GenericFitnessAdapter fitness)
        {
            var metrics = fitness.EvaluateDetailed(bestChromosome);
            
            BestFitness = metrics.CalculateFitness();
            TotalInspections = metrics.TotalInspections;
            TotalDelays = metrics.TotalDelays;
            BestSequence = bestChromosome.GetSequence();
            BestBinaryDecisions = bestChromosome.GetBinaryDecisions();

            Log($"[GenericOptimizer] Final results - Fitness: {BestFitness:F2}, " +
                $"Inspections: {TotalInspections}, Delays: {TotalDelays}");
        }

        private void NotifyCompleted(bool success, string error, double totalTime)
        {
            var completedEvent = Completed;
            if (completedEvent != null)
            {
                var args = new OptimizationCompletedEventArgs
                {
                    Success = success,
                    BestFitness = BestFitness,
                    TotalInspections = TotalInspections,
                    TotalDelays = TotalDelays,
                    TotalTime = totalTime,
                    Error = error
                };
                
                Task.Run(() => completedEvent.Invoke(args));
            }
        }

        private void Log(string message)
        {
            LogCallback?.Invoke(message);
        }

        #endregion

        #region Nested Fitness Adapter

        /// <summary>
        /// Adapts ISimulationEvaluator to GeneticSharp's IFitness interface.
        /// </summary>
        private class GenericFitnessAdapter : IFitness
        {
            private readonly IList<TData> data;
            private readonly ISimulationEvaluator<TData, SimulationMetrics> evaluator;

            public GenericFitnessAdapter(IList<TData> data, ISimulationEvaluator<TData, SimulationMetrics> evaluator)
            {
                this.data = data;
                this.evaluator = evaluator;
            }

            public double Evaluate(IChromosome chromosome)
            {
                var metrics = EvaluateDetailed(chromosome as SequenceBinaryChromosome);
                return evaluator.CalculateFitness(metrics);
            }

            public SimulationMetrics EvaluateDetailed(SequenceBinaryChromosome chromosome)
            {
                if (chromosome == null)
                    throw new ArgumentException("Invalid chromosome");

                var sequence = chromosome.GetSequence();
                var binary = chromosome.GetBinaryDecisions();
                
                return evaluator.RunSimulation(data, sequence, binary);
            }
        }

        #endregion
    }

    #region Event Args

    public class OptimizationProgressEventArgs : EventArgs
    {
        public int CurrentGeneration { get; set; }
        public int TotalGenerations { get; set; }
        public double BestFitness { get; set; }
        public int Inspections { get; set; }
        public int Delays { get; set; }
        public double ElapsedSeconds { get; set; }
    }

    public class OptimizationCompletedEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public double BestFitness { get; set; }
        public int TotalInspections { get; set; }
        public int TotalDelays { get; set; }
        public double TotalTime { get; set; }
        public string Error { get; set; }
    }

    #endregion
}
