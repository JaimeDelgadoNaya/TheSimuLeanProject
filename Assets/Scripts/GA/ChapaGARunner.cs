using System.Collections.Generic;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;
using GeneticSharp.Domain;
using UnityEngine;
using ChapasGA.Models;

namespace ChapasGA.GA
{
    public class GARunResult
    {
        public double BestFitness;
        public int[] Order;
        public bool[] Inspect;
        public List<double> CompletionTimes;
        public int TotalInspections;
        public int TotalDelays;
    }

    public class ChapaGARunner
    {
        private readonly IList<Chapa> _chapas;

        public ChapaGARunner(IList<Chapa> chapas)
        {
            _chapas = chapas;
        }

        public GARunResult Run(int populationSize, int generations, float crossoverProb, float mutationProb, bool log)
        {
            var required = new bool[_chapas.Count];
            for (int i = 0; i < _chapas.Count; i++) required[i] = _chapas[i].InspeccionOn;
            var chromosome = new ChapaChromosome(_chapas.Count, required);
            var population = new Population(populationSize, populationSize, chromosome);
            var fitness = new ChapaFitness(_chapas);
            var selection = new TournamentSelection();
            var crossover = new ChapaCrossover();
            var mutation = new ChapaMutation();
            var ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation)
            {
                CrossoverProbability = crossoverProb,
                MutationProbability = mutationProb,
                Reinsertion = new GeneticSharp.Domain.Reinsertions.ElitistReinsertion()
            };
            ga.Termination = new GenerationNumberTermination(generations);
            ga.TaskExecutor = new TplTaskExecutor();
            if (log)
            {
                ga.GenerationRan += (s, e) =>
                {
                    Debug.Log($"Generation {ga.GenerationsNumber} - Fitness {ga.BestChromosome.Fitness}");
                };
            }
            ga.Start();
            var best = (ChapaChromosome)ga.BestChromosome;
            var eval = fitness.EvaluateWithStats(best);
            return new GARunResult
            {
                BestFitness = eval.Fitness,
                Order = best.Order,
                Inspect = best.Inspect,
                CompletionTimes = eval.CompletionTimes,
                TotalInspections = eval.TotalInspections,
                TotalDelays = eval.TotalDelays
            };
        }
    }
}
