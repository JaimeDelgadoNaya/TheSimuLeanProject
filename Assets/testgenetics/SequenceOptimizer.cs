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
    public enum SelectionOperator { Elite, RouletteWheel, Tournament }

    public enum CrossoverOperator { Ordered, OnePoint, TwoPoint }

    public enum MutationOperator { Twors, ReverseSequence, Uniform }

    public static class SequenceOptimizer
    {
        /// <summary>
        /// Runs a Genetic Algorithm and returns the best sequence found.
        /// </summary>
        /// <param name="runner">Simulation runner used to evaluate sequences.</param>
        /// <param name="numberOfParts">Number of parts in the sequence.</param>
        /// <param name="generations">Number of generations to evolve.</param>
        /// <param name="populationSize">Population size used by the GA.</param>
        /// <param name="selectionOperator">Selection strategy.</param>
        /// <param name="crossoverOperator">Crossover strategy.</param>
        /// <param name="mutationOperator">Mutation strategy.</param>
        /// <returns>The best sequence of parts discovered.</returns>
        public static string[] OptimizePartSequence(
            ISimulationRunner runner,
            int numberOfParts,
            int generations,
            int populationSize,
            SelectionOperator selectionOperator,
            CrossoverOperator crossoverOperator,
            MutationOperator mutationOperator)
        {
            var fitness = new SequenceFitness(runner);
            var chromosome = new SequenceChromosome(numberOfParts);
            var population = new Population(populationSize, populationSize * 2, chromosome);

            ISelection selection = selectionOperator switch
            {
                SelectionOperator.RouletteWheel => new RouletteWheelSelection(),
                SelectionOperator.Tournament => new TournamentSelection(),
                _ => new EliteSelection()
            };

            ICrossover crossover = crossoverOperator switch
            {
                CrossoverOperator.OnePoint => new OnePointCrossover(),
                CrossoverOperator.TwoPoint => new TwoPointCrossover(),
                _ => new OrderedCrossover()
            };

            IMutation mutation = mutationOperator switch
            {
                MutationOperator.ReverseSequence => new ReverseSequenceMutation(),
                MutationOperator.Uniform => new UniformMutation(),
                _ => new TworsMutation()
            };

            var ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation)
            {
                Termination = new GenerationNumberTermination(generations)
            };

            ga.Start();

            var bestChromosome = ga.BestChromosome as SequenceChromosome;
            var bestSequence = bestChromosome?.GetSequence();
            var bestFitness = bestChromosome?.Fitness ?? 0;

            if (bestChromosome != null && bestSequence != null)
            {
                var (totalDelay, inspectionCount) = fitness.GetMetrics(bestChromosome);

                Debug.Log(
                    $"Best sequence found: {string.Join(",", bestSequence)} " +
                    $"with fitness = {bestFitness}, total delay = {totalDelay}, " +
                    $"inspection count = {inspectionCount}");

                return bestSequence;
            }

            return Array.Empty<string>();
        }
    }
}

