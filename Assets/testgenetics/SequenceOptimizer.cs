using System;
using System.Collections.Generic;
using System.Linq;
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
            // GeneticSharp requires at least two chromosomes per generation.
            // Clamp the population size to the minimum allowed value to avoid
            // runtime exceptions when a smaller value is provided.
            populationSize = Math.Max(2, populationSize);

            // Run a baseline simulation to determine the required number of inspections.
            var baselineSequence = new string[numberOfParts];
            for (int i = 0; i < numberOfParts; i++)
            {
                baselineSequence[i] = i.ToString();
            }
            runner.Configure(baselineSequence);
            runner.Run();
            var requiredInspectionCount = runner.InspectionCount;

            var fitness = new SequenceFitness(runner, requiredInspectionCount);
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

        /// <summary>
        /// Runs a Genetic Algorithm and returns the best sequence found for a
        /// set of explicit part identifiers. This overload is useful when the
        /// parts are not simply indexed from 0..N but have meaningful
        /// identifiers such as references from a production schedule.
        /// </summary>
        /// <param name="runner">Simulation runner used to evaluate sequences.</param>
        /// <param name="partIds">Identifiers of the parts to arrange.</param>
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
            IList<string> partIds,
            int generations = 100,
            int populationSize = 50,
            SelectionType selectionType = SelectionType.Elite,
            CrossoverType crossoverType = CrossoverType.Ordered,
            MutationType mutationType = MutationType.Twors)
        {
            if (partIds == null)
            {
                throw new ArgumentNullException(nameof(partIds));
            }

            // Ensure a valid population size for GeneticSharp.
            populationSize = Math.Max(2, populationSize);

            // Determine the required number of inspections from a baseline run
            // using the provided sequence in its original order.
            var baselineSequence = partIds.ToArray();
            runner.Configure(baselineSequence);
            runner.Run();
            var requiredInspectionCount = runner.InspectionCount;

            var fitness = new SequenceFitness(runner, requiredInspectionCount);
            var chromosome = new SequenceChromosome(partIds);
            var population = new Population(populationSize, populationSize * 2, chromosome);

            ISelection selection = selectionType switch
            {
                SelectionType.RouletteWheel => new RouletteWheelSelection(),
                _ => new EliteSelection(),
            };

            ICrossover crossover = crossoverType switch
            {
                CrossoverType.OnePoint => new OnePointCrossover(),
                _ => new OrderedCrossover(),
            };

            IMutation mutation = mutationType switch
            {
                MutationType.ReverseSequence => new ReverseSequenceMutation(),
                _ => new TworsMutation(),
            };

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
            var (delayCount, inspectionCount) = fitness.GetMetrics(bestChromosome);

            Debug.Log(
                $"Best sequence found: {string.Join(",", bestSequence)} " +
                $"with delay count = {delayCount}, inspection count = {inspectionCount}");

            return (bestSequence, delayCount, inspectionCount);
        }
    }
}

