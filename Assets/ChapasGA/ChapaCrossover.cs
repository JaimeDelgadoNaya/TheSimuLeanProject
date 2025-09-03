using System;
using System.Collections.Generic;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Randomizations;

namespace ChapasGA
{
    /// <summary>
    /// Custom crossover handling permutation and binary parts separately.
    /// </summary>
    public class ChapaCrossover : CrossoverBase
    {
        public ChapaCrossover() : base(2)
        {
        }

        protected override IList<IChromosome> PerformCross(IList<IChromosome> parents)
        {
            var p1 = (ChapaChromosome)parents[0];
            var p2 = (ChapaChromosome)parents[1];
            int n = p1.Length / 2;

            var c1 = (ChapaChromosome)p1.Clone();
            var c2 = (ChapaChromosome)p2.Clone();

            // Order crossover using OX (order crossover)
            int cut1 = RandomizationProvider.Current.GetInt(0, n);
            int cut2 = RandomizationProvider.Current.GetInt(cut1, n);
            var order1 = p1.GetOrder();
            var order2 = p2.GetOrder();
            var child1Order = OrderedCrossover(order1, order2, cut1, cut2);
            var child2Order = OrderedCrossover(order2, order1, cut1, cut2);

            // Inspection bits - one point crossover
            var bits1 = p1.GetInspections();
            var bits2 = p2.GetInspections();
            int pivot = RandomizationProvider.Current.GetInt(0, n);
            var child1Bits = bits1.Take(pivot).Concat(bits2.Skip(pivot)).ToArray();
            var child2Bits = bits2.Take(pivot).Concat(bits1.Skip(pivot)).ToArray();

            for (int i = 0; i < n; i++)
            {
                c1.ReplaceGene(i, new Gene(child1Order[i]));
                c2.ReplaceGene(i, new Gene(child2Order[i]));
                c1.ReplaceGene(n + i, new Gene(child1Bits[i] ? 1 : 0));
                c2.ReplaceGene(n + i, new Gene(child2Bits[i] ? 1 : 0));
            }

            c1.Repair();
            c2.Repair();
            return new List<IChromosome> { c1, c2 };
        }

        private static int[] OrderedCrossover(int[] parent1, int[] parent2, int cut1, int cut2)
        {
            int n = parent1.Length;
            var child = Enumerable.Repeat(-1, n).ToArray();
            Array.Copy(parent1, cut1, child, cut1, cut2 - cut1);
            int current = cut2 % n;
            foreach (var gene in parent2)
            {
                if (!child.Contains(gene))
                {
                    child[current] = gene;
                    current = (current + 1) % n;
                }
            }
            return child;
        }
    }
}
