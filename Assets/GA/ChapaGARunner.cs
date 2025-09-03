using System.Collections.Generic;
using ChapasGA.Models;
using GeneticSharp.Domain;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Selections;
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
            var chromosome = new ChapaChromosome(n);
            var fitness = new ChapaFitness(chapas);
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
