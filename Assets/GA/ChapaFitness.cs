using System;
using System.Collections.Generic;
using ChapasGA.Models;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;

namespace ChapasGA.GA
{
    public class ChapaFitness : IFitness
    {
        private readonly IList<Chapa> _chapas;

        public ChapaFitness(IList<Chapa> chapas)
        {
            _chapas = chapas;
        }

        public double Evaluate(IChromosome chromosome)
        {
            var c = chromosome as ChapaChromosome ?? throw new ArgumentException("Invalid chromosome");
            return EvaluateDetailed(c).fitness;
        }

        public (double fitness, int inspections, int delays, double[] completionTimes) EvaluateDetailed(ChapaChromosome c)
        {
            var order = c.GetOrder();
            var bits = c.GetInspectionBits();
            double C = 0;
            int inspections = 0;
            int delays = 0;
            var completionTimes = new double[order.Length];
            for (int i = 0; i < order.Length; i++)
            {
                int idx = order[i];
                var chapa = _chapas[idx];
                bool doInspect = bits[idx] == 1 || chapa.inspeccionOn == 1;
                double proc = chapa.tSoldadura + (doInspect ? chapa.tInspeccion : 0);
                C += proc;
                completionTimes[i] = C;
                if (doInspect) inspections++;
                if (C > chapa.DueDate) delays++;
            }
            double fitness = (inspections * 1.0) - (delays * 100.0);
            return (fitness, inspections, delays, completionTimes);
        }
    }
}
