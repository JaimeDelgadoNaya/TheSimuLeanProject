using System.Collections.Generic;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;
using ChapasGA.Models;

namespace ChapasGA.GA
{
    public class FitnessResult
    {
        public double Fitness;
        public List<double> CompletionTimes;
        public int TotalInspections;
        public int TotalDelays;
    }

    public class ChapaFitness : IFitness
    {
        private readonly IList<Chapa> _chapas;

        public ChapaFitness(IList<Chapa> chapas)
        {
            _chapas = chapas;
        }

        public double Evaluate(IChromosome chromosome)
        {
            var res = EvaluateWithStats((ChapaChromosome)chromosome);
            return res.Fitness;
        }

        public FitnessResult EvaluateWithStats(ChapaChromosome chromosome)
        {
            var order = chromosome.Order;
            var inspect = chromosome.Inspect;
            double time = 0;
            int inspections = 0;
            int delays = 0;
            var completion = new List<double>(order.Length);

            for (int i = 0; i < order.Length; i++)
            {
                var chapa = _chapas[order[i]];
                double proc = chapa.TSoldadura + (inspect[i] ? chapa.TInspeccion : 0);
                time += proc;
                completion.Add(time);
                if (inspect[i]) inspections++;
                if (time > chapa.DueDate) delays++;
            }

            return new FitnessResult
            {
                Fitness = inspections - delays * 100,
                CompletionTimes = completion,
                TotalInspections = inspections,
                TotalDelays = delays
            };
        }
    }
}
