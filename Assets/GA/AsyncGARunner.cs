using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChapasGA.Models;
using GeneticSharp.Domain;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;
using SimuLean.Serialization;

namespace ChapasGA.GA
{
    /// <summary>
    /// Runner asíncrono del GA con callbacks de progreso
    /// </summary>
    public class AsyncGARunner
    {
        public double BestFitness { get; private set; }
        public int TotalInspections { get; private set; }
        public int TotalDelays { get; private set; }
        public IList<int> BestOrder { get; private set; }
        public int[] BestBits { get; private set; }
        public double[] CompletionTimes { get; private set; }

        private SimulationConfig modelConfig;
        private CancellationTokenSource cancellationTokenSource;

        public event Action<GAProgressEventArgs> ProgressChanged;
        public event Action<GACompletedEventArgs> Completed;
        
        // Add logging callback for thread-safe Unity logging
        public Action<string> LogCallback { get; set; }

        public void SetModelConfig(SimulationConfig config)
        {
            modelConfig = config;
        }

        private void Log(string message)
        {
            // Try to use callback first, fallback to Console
            if (LogCallback != null)
            {
                LogCallback.Invoke(message);
            }
            else
            {
                System.Console.WriteLine(message);
            }
        }

        public async Task RunGAAsync(
            IList<Chapa> chapas, 
            int populationSize, 
            int generations, 
            float crossoverProb, 
            float mutationProb)
        {
            if (modelConfig == null)
            {
                throw new InvalidOperationException("Model configuration not set. Call SetModelConfig() before RunGAAsync().");
            }

            cancellationTokenSource = new CancellationTokenSource();

            await Task.Run(() => RunGAInternal(
                chapas, 
                populationSize, 
                generations, 
                crossoverProb, 
                mutationProb, 
                cancellationTokenSource.Token));
        }

        private void RunGAInternal(
            IList<Chapa> chapas, 
            int populationSize, 
            int generations, 
            float crossoverProb, 
            float mutationProb,
            CancellationToken cancellationToken)
        {
            try
            {
                Log("[AsyncGA] RunGAInternal started");
                
                int n = chapas.Count;
                Log($"[AsyncGA] Creating chromosome for {n} chapas");
                
                var chromosome = new ChapaChromosome(n);
                Log("[AsyncGA] Creating fitness evaluator");
                
                var fitness = new ChapaFitness(chapas, modelConfig);
                Log("[AsyncGA] Creating population");
                
                var population = new Population(populationSize, populationSize, chromosome);
                Log("[AsyncGA] Creating genetic operators");
                
                var selection = new TournamentSelection();
                var crossover = new ChapaCrossover();
                var mutation = new ChapaMutation();
                
                Log("[AsyncGA] Creating GA instance");
                var ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation)
                {
                    CrossoverProbability = crossoverProb,
                    MutationProbability = mutationProb,
                    Termination = new GenerationNumberTermination(generations)
                };

                Log("[AsyncGA] Subscribing to GenerationRan event");
                // Evento de progreso
                ga.GenerationRan += (sender, e) =>
                {
                    try
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            ga.Stop();
                            return;
                        }

                        var currentGen = ga.GenerationsNumber;
                        var bestFitness = ga.BestChromosome.Fitness.Value;
                        var elapsed = ga.TimeEvolving.TotalSeconds;

                        // Obtener detalles del mejor cromosoma actual
                        var bestChromo = ga.BestChromosome as ChapaChromosome;
                        var fitnessEval = fitness as ChapaFitness;
                        var details = fitnessEval?.EvaluateDetailed(bestChromo);

                        // Disparar evento de progreso (thread-safe para Unity)
                        // NO usar UnityEngine.Debug desde background thread
                        Log($"[AsyncGA] Gen {currentGen}/{generations} | Fitness: {bestFitness:F2} | " +
                            $"Inspections: {details?.inspections ?? 0} | Delays: {details?.delays ?? 0} | " +
                            $"Time: {elapsed:F1}s");
                        
                        // Use BeginInvoke to make this non-blocking
                        var progressEvent = ProgressChanged;
                        if (progressEvent != null)
                        {
                            var args = new GAProgressEventArgs
                            {
                                CurrentGeneration = currentGen,
                                TotalGenerations = generations,
                                BestFitness = bestFitness,
                                Inspections = details?.inspections ?? 0,
                                Delays = details?.delays ?? 0,
                                ElapsedSeconds = elapsed
                            };
                            
                            // Fire and forget - don't wait for handlers to complete
                            Task.Run(() => progressEvent.Invoke(args));
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"[AsyncGA] Error in GenerationRan handler: {ex.Message}");
                    }
                };

                // Ejecutar GA
                Log("[AsyncGA] Starting GA execution (ga.Start())...");
                ga.Start();
                Log("[AsyncGA] GA execution completed");

                // Obtener resultados finales
                var best = ga.BestChromosome as ChapaChromosome;
                var finalFitness = fitness as ChapaFitness;
                
                if (finalFitness != null)
                {
                    Log("[AsyncGA] Evaluating final results");
                    var details = finalFitness.EvaluateDetailed(best);
                    BestFitness = details.fitness;
                    TotalInspections = details.inspections;
                    TotalDelays = details.delays;
                    CompletionTimes = details.completionTimes;
                }
                else
                {
                    Log("[AsyncGA] Evaluating final fitness");
                    BestFitness = fitness.Evaluate(best);
                }
                
                BestOrder = best.GetOrder();
                BestBits = best.GetInspectionBits();

                Log("[AsyncGA] Invoking Completed event");
                // Disparar evento de completado (non-blocking)
                var completedEvent = Completed;
                if (completedEvent != null)
                {
                    var args = new GACompletedEventArgs
                    {
                        Success = true,
                        BestFitness = BestFitness,
                        TotalInspections = TotalInspections,
                        TotalDelays = TotalDelays,
                        TotalGenerations = generations,
                        TotalTime = ga.TimeEvolving.TotalSeconds
                    };
                    
                    // Fire and forget
                    Task.Run(() => completedEvent.Invoke(args));
                }
                
                Log("[AsyncGA] RunGAInternal completed successfully");
            }
            catch (Exception ex)
            {
                // NO usar UnityEngine.Debug desde background thread
                Log($"[AsyncGA] Error: {ex.Message}");
                Log($"[AsyncGA] Stack trace: {ex.StackTrace}");
                
                var completedEvent = Completed;
                if (completedEvent != null)
                {
                    var args = new GACompletedEventArgs
                    {
                        Success = false,
                        Error = ex.Message
                    };
                    
                    // Fire and forget
                    Task.Run(() => completedEvent.Invoke(args));
                }
            }
        }

        public void Cancel()
        {
            cancellationTokenSource?.Cancel();
        }
    }

    public class GAProgressEventArgs : EventArgs
    {
        public int CurrentGeneration { get; set; }
        public int TotalGenerations { get; set; }
        public double BestFitness { get; set; }
        public int Inspections { get; set; }
        public int Delays { get; set; }
        public double ElapsedSeconds { get; set; }
    }

    public class GACompletedEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public double BestFitness { get; set; }
        public int TotalInspections { get; set; }
        public int TotalDelays { get; set; }
        public int TotalGenerations { get; set; }
        public double TotalTime { get; set; }
        public string Error { get; set; }
    }
}
