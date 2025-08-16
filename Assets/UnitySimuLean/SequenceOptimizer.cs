using System;
using GeneticSharp.Domain;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Terminations;
using UnityEngine;

namespace UnitySimuLean
{
    /// <summary>
    /// Utility class that configures and runs a Genetic Algorithm to optimize
    /// the sequence of parts entering a SimuLean model.
    /// </summary>
    public static class SequenceOptimizer
    {
        /// <summary>
        /// Runs a Genetic Algorithm and returns the best sequence found.
        /// </summary>
        /// <param name="runner">Simulation runner used to evaluate sequences.</param>
        /// <param name="numberOfParts">Number of parts in the sequence.</param>
        /// <param name="generations">Number of generations to evolve.</param>
        /// <param name="populationSize">Population size used by the GA.</param>
        /// <returns>The best sequence of parts discovered.</returns>
        public static int[] OptimizePartSequence(
            ISimulationRunner runner,
            int numberOfParts,
            int generations = 100,
            int populationSize = 50)
        {
            var fitness = new SequenceFitness(runner);
            var chromosome = new SequenceChromosome(numberOfParts);
            var population = new Population(populationSize, populationSize * 2, chromosome);
            var selection = new EliteSelection();
            var crossover = new OrderedCrossover();
            var mutation = new TworsMutation();

            var ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation)
            {
                Termination = new GenerationNumberTermination(generations)
            };

            ga.Start();

            var bestChromosome = ga.BestChromosome as SequenceChromosome;
            var bestSequence = bestChromosome?.GetSequence();
            var bestFitness = bestChromosome?.Fitness ?? 0;

            if (bestSequence != null)
            {
                runner.Configure(bestSequence);
                runner.Run();
                var totalDelay = runner.TotalDelay;
                var inspectionCount = runner.InspectionCount;

                Debug.Log(
                    $"Best sequence found: {string.Join(",", bestSequence)} " +
                    $"with fitness = {bestFitness}, total delay = {totalDelay}, " +
                    $"inspection count = {inspectionCount}");
            }

            return bestSequence ?? Array.Empty<int>();
        }
    }
}

