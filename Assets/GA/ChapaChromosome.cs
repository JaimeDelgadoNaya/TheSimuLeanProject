using System;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Randomizations;

namespace ChapasGA.GA
{
    public class ChapaChromosome : ChromosomeBase
    {
        public int LengthPerPart { get; }

        public ChapaChromosome(int length)
            : base(length * 2)
        {
            LengthPerPart = length;
            CreateGenes();
            Repair();
        }

        public override IChromosome CreateNew()
        {
            return new ChapaChromosome(LengthPerPart);
        }

        public override Gene GenerateGene(int geneIndex)
        {
            var rnd = RandomizationProvider.Current;
            if (geneIndex < LengthPerPart)
            {
                return new Gene(rnd.GetInt(0, LengthPerPart));
            }
            return new Gene(rnd.GetInt(0, 2));
        }

        public int[] GetOrder()
        {
            return GetGenes().Take(LengthPerPart).Select(g => (int)g.Value).ToArray();
        }

        public int[] GetInspectionBits()
        {
            return GetGenes().Skip(LengthPerPart).Select(g => (int)g.Value).ToArray();
        }

        public void Repair()
        {
            // Repair permutation
            var order = GetOrder();
            var fixedOrder = order.Distinct().ToList();
            for (int i = 0; i < LengthPerPart; i++)
            {
                if (i >= fixedOrder.Count)
                {
                    // fill missing numbers
                    for (int k = 0; k < LengthPerPart; k++)
                    {
                        if (!fixedOrder.Contains(k))
                        {
                            fixedOrder.Add(k);
                            break;
                        }
                    }
                }
            }
            for (int i = 0; i < LengthPerPart; i++)
            {
                ReplaceGene(i, new Gene(fixedOrder[i]));
            }
        }
    }
}
