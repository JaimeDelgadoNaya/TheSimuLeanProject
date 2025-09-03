using System;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Randomizations;

namespace ChapasGA.GA
{
    public class ChapaChromosome : ChromosomeBase
    {
        private readonly int[] _mandatory;
        private readonly double _inspectProbability;
        public int LengthPerPart { get; }

        public ChapaChromosome(int length, int[] mandatory, double inspectProbability = 0.5)
            : base(length * 2)
        {
            LengthPerPart = length;
            _mandatory = mandatory;
            _inspectProbability = inspectProbability;
            CreateGenes();
            Repair();
        }

        public override IChromosome CreateNew()
        {
            return new ChapaChromosome(LengthPerPart, _mandatory, _inspectProbability);
        }

        public override Gene GenerateGene(int geneIndex)
        {
            var rnd = RandomizationProvider.Current;
            if (geneIndex < LengthPerPart)
            {
                return new Gene(rnd.GetInt(0, LengthPerPart));
            }
            var idx = geneIndex - LengthPerPart;
            if (_mandatory[idx] == 1)
            {
                return new Gene(1);
            }
            return new Gene(rnd.GetDouble() < _inspectProbability ? 1 : 0);
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
            // Repair bits
            var bits = GetInspectionBits();
            for (int i = 0; i < bits.Length; i++)
            {
                if (_mandatory[i] == 1)
                {
                    bits[i] = 1;
                }
            }
            for (int i = 0; i < bits.Length; i++)
            {
                ReplaceGene(LengthPerPart + i, new Gene(bits[i]));
            }
        }
    }
}
