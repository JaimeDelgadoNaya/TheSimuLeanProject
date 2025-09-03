using System;
using System.Collections.Generic;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;

namespace ChapasGA
{
    /// <summary>
    /// Fitness function for chapa scheduling problem.
    /// </summary>
    public class ChapaFitness : IFitness
    {
        private readonly IList<ChapaData> chapas;

        public ChapaFitness(IList<ChapaData> chapas)
        {
            this.chapas = chapas;
        }

        public double Evaluate(IChromosome chromosome)
        {
            var c = (ChapaChromosome)chromosome;
            var order = c.GetOrder();
            var inspect = c.GetInspections();

            int numInspections = 0;
            int numLate = 0;
            double completion = 0;

            for (int pos = 0; pos < order.Length; pos++)
            {
                int idx = order[pos];
                var chapa = chapas[idx];
                bool doInspect = chapa.InspeccionOn == 1 || inspect[idx];
                double procTime = chapa.SoldaduraTime + (doInspect ? chapa.InspeccionTime : 0);
                completion += procTime;
                if (doInspect) numInspections++;
                if (completion > chapa.DueDate) numLate++;
            }
            return numInspections - 100 * numLate;
        }
    }
}
