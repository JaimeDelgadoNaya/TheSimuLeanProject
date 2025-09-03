using System;
using System.Collections.Generic;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Randomizations;

namespace UnitySimuLean
{
    /// <summary>
    /// Custom crossover that applies permutation crossovers to the order part
    /// of the chromosome and binary crossovers to the inspection part.
    /// </summary>
    public class ScheduleCrossover : CrossoverBase
    {
        private readonly OrderCrossoverType _orderType;
        private readonly BitCrossoverType _bitType;

        public ScheduleCrossover(OrderCrossoverType orderType, BitCrossoverType bitType)
            : base(2, 2)
        {
            _orderType = orderType;
            _bitType = bitType;
        }

        protected override IList<IChromosome> PerformCross(IList<IChromosome> parents)
        {
            var p1 = (ScheduleChromosome)parents[0];
            var p2 = (ScheduleChromosome)parents[1];

            var order1 = p1.GetOrder();
            var order2 = p2.GetOrder();
            int[] child1Order;
            int[] child2Order;
            if (_orderType == OrderCrossoverType.PMX)
            {
                (child1Order, child2Order) = PMX(order1, order2);
            }
            else
            {
                (child1Order, child2Order) = OX(order1, order2);
            }

            var bits1 = p1.GetInspectionVector();
            var bits2 = p2.GetInspectionVector();
            bool[] child1Bits;
            bool[] child2Bits;
            if (_bitType == BitCrossoverType.Uniform)
            {
                (child1Bits, child2Bits) = Uniform(bits1, bits2);
            }
            else
            {
                (child1Bits, child2Bits) = OnePoint(bits1, bits2);
            }

            var c1 = (ScheduleChromosome)p1.CreateNew();
            var c2 = (ScheduleChromosome)p2.CreateNew();
            c1.SetData(child1Order, child1Bits);
            c2.SetData(child2Order, child2Bits);
            return new List<IChromosome> { c1, c2 };
        }

        private (int[] child1, int[] child2) PMX(int[] a, int[] b)
        {
            int n = a.Length;
            int cut1 = RandomizationProvider.Current.GetInt(0, n);
            int cut2 = RandomizationProvider.Current.GetInt(0, n);
            if (cut1 > cut2)
            {
                var tmp = cut1; cut1 = cut2; cut2 = tmp;
            }
            var child1 = Enumerable.Repeat(-1, n).ToArray();
            var child2 = Enumerable.Repeat(-1, n).ToArray();
            for (int i = cut1; i <= cut2; i++)
            {
                child1[i] = b[i];
                child2[i] = a[i];
            }
            for (int i = cut1; i <= cut2; i++)
            {
                MapGene(a[i], b[i], child1, a, b, cut1, cut2);
                MapGene(b[i], a[i], child2, b, a, cut1, cut2);
            }
            FillRemaining(child1, a);
            FillRemaining(child2, b);
            return (child1, child2);
        }

        private void MapGene(int fromParent, int toParent, int[] child, int[] pA, int[] pB, int c1, int c2)
        {
            int n = child.Length;
            int pos = Array.IndexOf(pB, fromParent);
            while (pos >= c1 && pos <= c2)
            {
                fromParent = pA[pos];
                pos = Array.IndexOf(pB, fromParent);
            }
            if (child[pos] == -1)
            {
                child[pos] = fromParent;
            }
        }

        private void FillRemaining(int[] child, int[] parent)
        {
            for (int i = 0; i < child.Length; i++)
            {
                if (child[i] == -1)
                {
                    child[i] = parent[i];
                }
            }
        }

        private (int[] child1, int[] child2) OX(int[] a, int[] b)
        {
            int n = a.Length;
            int cut1 = RandomizationProvider.Current.GetInt(0, n);
            int cut2 = RandomizationProvider.Current.GetInt(0, n);
            if (cut1 > cut2)
            {
                var tmp = cut1; cut1 = cut2; cut2 = tmp;
            }
            var child1 = Enumerable.Repeat(-1, n).ToArray();
            var child2 = Enumerable.Repeat(-1, n).ToArray();
            for (int i = cut1; i <= cut2; i++)
            {
                child1[i] = a[i];
                child2[i] = b[i];
            }
            FillOX(child1, b, cut2);
            FillOX(child2, a, cut2);
            return (child1, child2);
        }

        private void FillOX(int[] child, int[] donor, int start)
        {
            int n = child.Length;
            int idx = (start + 1) % n;
            foreach (var gene in donor)
            {
                if (!child.Contains(gene))
                {
                    child[idx] = gene;
                    idx = (idx + 1) % n;
                }
            }
        }

        private (bool[] child1, bool[] child2) Uniform(bool[] a, bool[] b)
        {
            int n = a.Length;
            var c1 = new bool[n];
            var c2 = new bool[n];
            for (int i = 0; i < n; i++)
            {
                if (RandomizationProvider.Current.GetInt(0, 2) == 0)
                {
                    c1[i] = a[i];
                    c2[i] = b[i];
                }
                else
                {
                    c1[i] = b[i];
                    c2[i] = a[i];
                }
            }
            return (c1, c2);
        }

        private (bool[] child1, bool[] child2) OnePoint(bool[] a, bool[] b)
        {
            int n = a.Length;
            int cut = RandomizationProvider.Current.GetInt(1, n);
            var c1 = new bool[n];
            var c2 = new bool[n];
            for (int i = 0; i < n; i++)
            {
                if (i < cut)
                {
                    c1[i] = a[i];
                    c2[i] = b[i];
                }
                else
                {
                    c1[i] = b[i];
                    c2[i] = a[i];
                }
            }
            return (c1, c2);
        }
    }
}
