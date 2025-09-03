using System;
using System.Collections.Generic;
using GeneticSharp.Domain;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;
using GeneticSharp.Domain.Reinsertions;

namespace UnitySimuLean
{
    /// <summary>
    /// Configures and executes the genetic algorithm for the scheduling model.
    /// </summary>
    public static class ScheduleOptimizer
    {
        public static (int[] order, bool[] inspections, int delayCount, int inspectionCount) Optimize(
            IReadOnlyList<Job> jobs,
            int populationSize = 100,
            int generations = 500,
            double crossoverProb = 0.9,
            double mutationProb = 0.15,
            OrderCrossoverType orderCrossover = OrderCrossoverType.PMX,
            BitCrossoverType bitCrossover = BitCrossoverType.Uniform,
            OrderMutationType orderMutation = OrderMutationType.Twors)
        {
            populationSize = Math.Max(2, populationSize);

            var chromosome = new ScheduleChromosome(jobs.Count);
            var population = new Population(populationSize, populationSize, chromosome);
            var fitness = new ScheduleFitness(jobs);

            var selection = new TournamentSelection();
            var crossover = new ScheduleCrossover(orderCrossover, bitCrossover) { Probability = crossoverProb };
            var mutation = new ScheduleMutation(orderMutation) { Probability = mutationProb };

            var ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation)
            {
                Reinsertion = new ElitistReinsertion(),
                Termination = new GenerationNumberTermination(generations)
            };

            ga.Start();

            var best = ga.BestChromosome as ScheduleChromosome;
            if (best == null)
            {
                return (Array.Empty<int>(), Array.Empty<bool>(), 0, 0);
            }

            var order = best.GetOrder();
            var inspections = best.GetInspectionVector();
            var (delayCount, inspectionCount) = fitness.GetMetrics(best);
            return (order, inspections, delayCount, inspectionCount);
        }
    }
}

