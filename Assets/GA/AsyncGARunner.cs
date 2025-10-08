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

        public void SetModelConfig(SimulationConfig config)
        {
            modelConfig = config;
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
                int n = chapas.Count;
                var chromosome = new ChapaChromosome(n);
                var fitness = new ChapaFitness(chapas, modelConfig);
                var population = new Population(populationSize, populationSize, chromosome);
                var selection = new TournamentSelection();
                var crossover = new ChapaCrossover();
                var mutation = new ChapaMutation();
                
                var ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation)
                {
                    CrossoverProbability = crossoverProb,
                    MutationProbability = mutationProb,
                    Termination = new GenerationNumberTermination(generations)
                };

                // Evento de progreso
                ga.GenerationRan += (sender, e) =>
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
                    System.Console.WriteLine($"[AsyncGA] Gen {currentGen}/{generations} | Fitness: {bestFitness:F2}");
                    
                    ProgressChanged?.Invoke(new GAProgressEventArgs
                    {
                        CurrentGeneration = currentGen,
                        TotalGenerations = generations,
                        BestFitness = bestFitness,
                        Inspections = details?.inspections ?? 0,
                        Delays = details?.delays ?? 0,
                        ElapsedSeconds = elapsed
                    });
                };

                // Ejecutar GA
                ga.Start();

                // Obtener resultados finales
                var best = ga.BestChromosome as ChapaChromosome;
                var finalFitness = fitness as ChapaFitness;
                
                if (finalFitness != null)
                {
                    var details = finalFitness.EvaluateDetailed(best);
                    BestFitness = details.fitness;
                    TotalInspections = details.inspections;
                    TotalDelays = details.delays;
                    CompletionTimes = details.completionTimes;
                }
                else
                {
                    BestFitness = fitness.Evaluate(best);
                }
                
                BestOrder = best.GetOrder();
                BestBits = best.GetInspectionBits();

                // Disparar evento de completado
                Completed?.Invoke(new GACompletedEventArgs
                {
                    Success = true,
                    BestFitness = BestFitness,
                    TotalInspections = TotalInspections,
                    TotalDelays = TotalDelays,
                    TotalGenerations = generations,
                    TotalTime = ga.TimeEvolving.TotalSeconds
                });
            }
            catch (Exception ex)
            {
                // NO usar UnityEngine.Debug desde background thread
                System.Console.WriteLine($"[AsyncGA] Error: {ex.Message}");
                
                Completed?.Invoke(new GACompletedEventArgs
                {
                    Success = false,
                    Error = ex.Message
                });
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
