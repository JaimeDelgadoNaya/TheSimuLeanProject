using System;
using System.Collections.Generic;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Randomizations;

namespace ChapasGA.GA
{
    [Obsolete("Use SequenceBinaryCrossover instead.")]
    public class ChapaCrossover : CrossoverBase
    {
        public ChapaCrossover() : base(2, 2)
        {
        }

        protected override IList<IChromosome> PerformCross(IList<IChromosome> parents)
        {
            var p1 = parents[0] as ChapaChromosome ?? throw new ArgumentException("Parent 1 is invalid");
            var p2 = parents[1] as ChapaChromosome ?? throw new ArgumentException("Parent 2 is invalid");

            var child1 = p1.Clone() as ChapaChromosome;
            var child2 = p2.Clone() as ChapaChromosome;

            var rnd = RandomizationProvider.Current;
            int size = p1.LengthPerPart;

            int cut1 = rnd.GetInt(0, size);
            int cut2 = rnd.GetInt(0, size);
            if (cut1 > cut2) (cut1, cut2) = (cut2, cut1);

            // Order crossover (OX)
            var o1 = p1.GetOrder();
            var o2 = p2.GetOrder();
            var c1 = new int[size];
            var c2 = new int[size];
            for (int i = 0; i < size; i++)
            {
                c1[i] = -1;
                c2[i] = -1;
            }
            for (int i = cut1; i <= cut2; i++)
            {
                c1[i] = o1[i];
                c2[i] = o2[i];
            }
            int current1 = (cut2 + 1) % size;
            int current2 = current1;
            for (int i = 0; i < size; i++)
            {
                int idx = (cut2 + 1 + i) % size;
                int val2 = o2[idx];
                if (Array.IndexOf(c1, val2) == -1)
                {
                    c1[current1] = val2;
                    current1 = (current1 + 1) % size;
                }
                int val1 = o1[idx];
                if (Array.IndexOf(c2, val1) == -1)
                {
                    c2[current2] = val1;
                    current2 = (current2 + 1) % size;
                }
            }
            for (int i = 0; i < size; i++)
            {
                child1.ReplaceGene(i, new Gene(c1[i]));
                child2.ReplaceGene(i, new Gene(c2[i]));
            }

            // Uniform crossover for bits
            var b1 = p1.GetInspectionBits();
            var b2 = p2.GetInspectionBits();
            for (int i = 0; i < size; i++)
            {
                if (rnd.GetDouble() < 0.5)
                {
                    child1.ReplaceGene(size + i, new Gene(b1[i]));
                    child2.ReplaceGene(size + i, new Gene(b2[i]));
                }
                else
                {
                    child1.ReplaceGene(size + i, new Gene(b2[i]));
                    child2.ReplaceGene(size + i, new Gene(b1[i]));
                }
            }

            child1.Repair();
            child2.Repair();
            return new List<IChromosome> { child1, child2 };
        }
    }
}
