using System.Collections.Generic;
using System.Linq;
using ChapasGA.Models;
using GeneticSharp.Domain;
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

        public void RunGA(IList<Chapa> chapas, int populationSize, int generations, float crossoverProb, float mutationProb)
        {
            int n = chapas.Count;
            var mandatory = chapas.Select(c => c.inspeccionOn).ToArray();
            double baseTime = chapas.Sum(c => c.tSoldadura + (c.inspeccionOn == 1 ? c.tInspeccion : 0));
            double inspectProb = baseTime >= 0.9 * 21600 ? 0.1 : 0.5;
            var chromosome = new ChapaChromosome(n, mandatory, inspectProb);
            var fitness = new ChapaFitness(chapas);
            var population = new Population(populationSize, populationSize, chromosome);
            var selection = new TournamentSelection(3);
            var crossover = new ChapaCrossover();
            var mutation = new ChapaMutation();
            var termination = new OrTermination(new GenerationNumberTermination(generations), new FitnessStagnationTermination(100));
            var ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation)
            {
                CrossoverProbability = crossoverProb,
                MutationProbability = mutationProb,
                Reinsertion = new ElitistReinsertion(),
                Termination = termination
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
    }
}
