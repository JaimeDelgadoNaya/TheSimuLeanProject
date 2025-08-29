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
        /// Runs a Genetic Algorithm and returns the best sequence found along
        /// with its associated performance metrics.
        /// </summary>
        /// <param name="runner">Simulation runner used to evaluate sequences.</param>
        /// <param name="numberOfParts">Number of parts in the sequence.</param>
        /// <param name="generations">Number of generations to evolve.</param>
        /// <param name="populationSize">Population size used by the GA.</param>
        /// <param name="selectionType">Selection operator used by the GA.</param>
        /// <param name="crossoverType">Crossover operator used by the GA.</param>
        /// <param name="mutationType">Mutation operator used by the GA.</param>
        /// <returns>
        /// A tuple containing the best sequence, its delay count and inspection
        /// count. If no sequence is found an empty sequence and default metrics
        /// are returned.
        /// </returns>
        public static (string[] bestSequence, int delayCount, int inspectionCount) OptimizePartSequence(
            ISimulationRunner runner,
            int numberOfParts,
            int generations = 100,
            int populationSize = 50,
            SelectionType selectionType = SelectionType.Elite,
            CrossoverType crossoverType = CrossoverType.Ordered,
            MutationType mutationType = MutationType.Twors)
        {
            var fitness = new SequenceFitness(runner);
            var chromosome = new SequenceChromosome(numberOfParts);
            var population = new Population(populationSize, populationSize * 2, chromosome);
            ISelection selection;
            switch (selectionType)
            {
                case SelectionType.RouletteWheel:
                    selection = new RouletteWheelSelection();
                    break;
                default:
                    selection = new EliteSelection();
                    break;
            }

            ICrossover crossover;
            switch (crossoverType)
            {
                case CrossoverType.OnePoint:
                    crossover = new OnePointCrossover();
                    break;
                default:
                    crossover = new OrderedCrossover();
                    break;
            }

            IMutation mutation;
            switch (mutationType)
            {
                case MutationType.ReverseSequence:
                    mutation = new ReverseSequenceMutation();
                    break;
                default:
                    mutation = new TworsMutation();
                    break;
            }

            var ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation)
            {
                Termination = new GenerationNumberTermination(generations)
            };

            ga.Start();

            var bestChromosome = ga.BestChromosome as SequenceChromosome;
            if (bestChromosome == null)
            {
                return (Array.Empty<string>(), 0, 0);
            }

            var bestSequence = bestChromosome.GetSequence();
            var bestFitness = bestChromosome.Fitness ?? 0;
            var (delayCount, inspectionCount) = fitness.GetMetrics(bestChromosome);

            Debug.Log(
                $"Best sequence found: {string.Join(",", bestSequence)} " +
                $"with fitness = {bestFitness}, delay count = {delayCount}, " +
                $"inspection count = {inspectionCount}");

            return (bestSequence, delayCount, inspectionCount);
        }
    }
}

