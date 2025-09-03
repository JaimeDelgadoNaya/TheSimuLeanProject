using System;
using System.Collections.Generic;
using System.Linq;
using ChapasGA.Models;
using GeneticSharp.Domain;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Reinsertions;
using GeneticSharp.Domain.Terminations;

namespace ChapasGA.GA
{
    public class ChapaGARunner
    {
        public double BestFitness { get; set; }
        public int TotalInspections { get; set; }
        public int TotalDelays { get; set; }
        public IList<int> BestOrder { get; set; }
        public int[] BestBits { get; set; }
        public double[] CompletionTimes { get; set; }

        public void RunGA(IList<Chapa> chapas, int populationSize, int generations, float crossoverProb, float mutationProb, float elitismPercentage)
        {
            int n = chapas.Count;
            var mandatory = chapas.Select(c => c.inspeccionOn).ToArray();
            var prototype = new ChapaChromosome(n, mandatory);
            var fitness = new ChapaFitness(chapas);
            var population = new Population(populationSize, populationSize, prototype);
            population.CreateInitialGeneration();
            SeedPopulation(population, chapas, prototype);

            var selection = new TournamentSelection(4);
            var crossover = new ChapaCrossover();
            var mutation = new ChapaMutation();
            var ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation)
            {
                CrossoverProbability = crossoverProb,
                MutationProbability = mutationProb,
                Reinsertion = new ConfigurableElitistReinsertion(elitismPercentage),
                Termination = new GenerationNumberTermination(generations)
            };

            double bestSoFar = double.MinValue;
            int stagnant = 0;
            int mutationBoost = 0;
            ga.GenerationRan += (sender, args) =>
            {
                if (ga.BestChromosome.Fitness.HasValue && ga.BestChromosome.Fitness.Value > bestSoFar + 1e-6)
                {
                    bestSoFar = ga.BestChromosome.Fitness.Value;
                    stagnant = 0;
                }
                else
                {
                    stagnant++;
                }

                if (stagnant >= 30)
                {
                    var gen = ga.Population.CurrentGeneration;
                    int replace = gen.Chromosomes.Count / 2;
                    var worst = gen.Chromosomes.OrderBy(c => c.Fitness).Take(replace).ToList();
                    foreach (var w in worst)
                    {
                        var idx = gen.Chromosomes.IndexOf(w);
                        gen.Chromosomes[idx] = prototype.CreateNew();
                    }
                    ga.MutationProbability = 0.35f;
                    mutationBoost = 10;
                    stagnant = 0;
                }

                if (mutationBoost > 0)
                {
                    mutationBoost--;
                    if (mutationBoost == 0)
                    {
                        ga.MutationProbability = mutationProb;
                    }
                }

                if (ga.GenerationsNumber % 5 == 0)
                {
                    int eliteCount = (int)(ga.Population.CurrentGeneration.Chromosomes.Count * 0.05);
                    var elites = ga.Population.CurrentGeneration.Chromosomes
                        .OrderByDescending(c => c.Fitness)
                        .Take(eliteCount)
                        .Cast<ChapaChromosome>();
                    foreach (var elite in elites)
                    {
                        TwoOpt(elite, fitness);
                    }
                }
            };

            ga.Start();

            var best = ga.BestChromosome as ChapaChromosome;
            var details = fitness.EvaluateDetailed(best);
            BestFitness = details.fitness;
            TotalInspections = details.inspections;
            TotalDelays = details.delays;
            CompletionTimes = details.completionTimes;
            BestOrder = best.GetOrder();
            BestBits = best.GetInspectionBits();
        }

        private void SeedPopulation(Population population, IList<Chapa> chapas, ChapaChromosome prototype)
        {
            var chromos = population.CurrentGeneration.Chromosomes;
            if (chromos.Count == 0) return;
            var orderEdd = chapas.Select((c, i) => new { c, i }).OrderBy(x => x.c.DueDate).Select(x => x.i).ToArray();
            var edd = prototype.Clone() as ChapaChromosome;
            edd.SetOrder(orderEdd);
            chromos[0] = edd;
            if (chromos.Count > 1)
            {
                var moore = prototype.Clone() as ChapaChromosome;
                moore.SetOrder(MooreHodgson(chapas));
                chromos[1] = moore;
            }
            if (chromos.Count > 2)
            {
                var greedy = prototype.Clone() as ChapaChromosome;
                greedy.SetOrder(orderEdd);
                ApplyGreedyInspections(greedy, chapas);
                chromos[2] = greedy;
            }
        }

        private int[] MooreHodgson(IList<Chapa> chapas)
        {
            var jobs = chapas.Select((c, i) => new
            {
                Index = i,
                Due = c.DueDate,
                Proc = c.tSoldadura + (c.inspeccionOn == 1 ? c.tInspeccion : 0)
            }).OrderBy(j => j.Due).ToList();
            var schedule = new List<dynamic>();
            double time = 0;
            foreach (var job in jobs)
            {
                schedule.Add(job);
                time += job.Proc;
                if (time > job.Due)
                {
                    var worst = schedule.OrderByDescending(s => s.Proc).First();
                    schedule.Remove(worst);
                    time -= worst.Proc;
                }
            }
            var late = jobs.Where(j => !schedule.Contains(j)).OrderBy(j => j.Due);
            return schedule.Concat(late).Select(j => j.Index).ToArray();
        }

        private void ApplyGreedyInspections(ChapaChromosome chromo, IList<Chapa> chapas)
        {
            var order = chromo.GetOrder();
            var bits = chromo.GetInspectionBits();
            double C = 0;
            for (int i = 0; i < order.Length; i++)
            {
                int idx = order[i];
                var chapa = chapas[idx];
                double proc = chapa.tSoldadura;
                if (chapa.inspeccionOn == 1)
                {
                    bits[idx] = 1;
                    proc += chapa.tInspeccion;
                }
                else if (C + proc + chapa.tInspeccion <= chapa.DueDate)
                {
                    bits[idx] = 1;
                    proc += chapa.tInspeccion;
                }
                else
                {
                    bits[idx] = 0;
                }
                C += proc;
            }
            for (int i = 0; i < bits.Length; i++)
            {
                chromo.ReplaceGene(order.Length + i, new Gene(bits[i]));
            }
            chromo.Repair();
        }

        private void TwoOpt(ChapaChromosome chromo, ChapaFitness fitness)
        {
            var order = chromo.GetOrder();
            double best = fitness.Evaluate(chromo);
            int n = order.Length;
            for (int i = 0; i < n - 1; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    var newOrder = TwoOptSwap(order, i, j);
                    var clone = chromo.Clone() as ChapaChromosome;
                    clone.SetOrder(newOrder);
                    double f = fitness.Evaluate(clone);
                    if (f > best)
                    {
                        chromo.SetOrder(newOrder);
                        best = f;
                        order = newOrder;
                    }
                }
            }
        }

        private int[] TwoOptSwap(int[] order, int i, int j)
        {
            var result = (int[])order.Clone();
            Array.Reverse(result, i, j - i + 1);
            return result;
        }
    }
}
