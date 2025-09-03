using System.Collections.Generic;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Randomizations;

namespace ChapasGA.GA
{
    public class ChapaCrossover : ICrossover
    {
        public int ParentsNumber => 2;
        public int ChildrenNumber => 2;
        public bool IsOrdered => true;

        public IList<IChromosome> Cross(IList<IChromosome> parents)
        {
            var p1 = (ChapaChromosome)parents[0].Clone();
            var p2 = (ChapaChromosome)parents[1].Clone();

            int[] child1Order = Ordered(p1.Order, p2.Order);
            int[] child2Order = Ordered(p2.Order, p1.Order);
            bool[] child1Inspect = Uniform(p1.Inspect, p2.Inspect);
            bool[] child2Inspect = Uniform(p2.Inspect, p1.Inspect);

            var c1 = new ChapaChromosome(p1.Order.Length, p1.InspectionRequired)
            {
                Order = child1Order,
                Inspect = child1Inspect
            };
            var c2 = new ChapaChromosome(p1.Order.Length, p1.InspectionRequired)
            {
                Order = child2Order,
                Inspect = child2Inspect
            };
            c1.Repair();
            c2.Repair();
            return new List<IChromosome> { c1, c2 };
        }

        private int[] Ordered(int[] a, int[] b)
        {
            int n = a.Length;
            int cut1 = RandomizationProvider.Current.GetInt(0, n);
            int cut2 = RandomizationProvider.Current.GetInt(0, n);
            if (cut1 > cut2)
            {
                var t = cut1; cut1 = cut2; cut2 = t;
            }
            var child = new int[n];
            for (int i = 0; i < n; i++) child[i] = -1;
            for (int i = cut1; i <= cut2; i++) child[i] = a[i];
            int current = (cut2 + 1) % n;
            for (int i = 0; i < n; i++)
            {
                int gene = b[(cut2 + 1 + i) % n];
                if (System.Array.IndexOf(child, gene) == -1)
                {
                    child[current] = gene;
                    current = (current + 1) % n;
                }
            }
            return child;
        }

        private bool[] Uniform(bool[] a, bool[] b)
        {
            int n = a.Length;
            var child = new bool[n];
            for (int i = 0; i < n; i++)
            {
                child[i] = RandomizationProvider.Current.GetDouble() < 0.5 ? a[i] : b[i];
            }
            return child;
        }
    }
}
