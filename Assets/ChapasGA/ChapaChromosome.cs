using System;
using System.Collections.Generic;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Randomizations;

namespace ChapasGA
{
    /// <summary>
    /// Chromosome representing both the processing order and optional inspections.
    /// Genes 0..N-1 store the permutation of chapa indices and genes N..2N-1 store inspection bits (0/1).
    /// </summary>
    public class ChapaChromosome : ChromosomeBase
    {
        private readonly bool[] mandatoryInspections;
        private readonly int n;

        public ChapaChromosome(int n, bool[] mandatory) : base(2 * n)
        {
            this.n = n;
            mandatoryInspections = mandatory;

            // Initialize permutation randomly.
            var order = Enumerable.Range(0, n)
                .OrderBy(_ => RandomizationProvider.Current.GetDouble())
                .ToArray();
            for (int i = 0; i < n; i++)
            {
                ReplaceGene(i, new Gene(order[i]));
            }

            // Initialize inspection bits randomly, respecting mandatory ones.
            for (int i = 0; i < n; i++)
            {
                bool bit = mandatory[i] || RandomizationProvider.Current.GetInt(0, 2) == 1;
                ReplaceGene(n + i, new Gene(bit ? 1 : 0));
            }
        }

        public override Gene GenerateGene(int geneIndex)
        {
            // Not used because initialization handled in constructor.
            return new Gene(0);
        }

        public override IChromosome CreateNew()
        {
            return new ChapaChromosome(n, mandatoryInspections);
        }

        public override IChromosome Clone()
        {
            var clone = new ChapaChromosome(n, mandatoryInspections);
            var genes = GetGenes();
            for (int i = 0; i < genes.Length; i++)
            {
                clone.ReplaceGene(i, genes[i]);
            }
            return clone;
        }

        /// <summary>
        /// Gets the processing order as array of indices.
        /// </summary>
        public int[] GetOrder()
        {
            return GetGenes().Take(n).Select(g => (int)g.Value).ToArray();
        }

        /// <summary>
        /// Gets the inspection decisions as array of booleans.
        /// </summary>
        public bool[] GetInspections()
        {
            return GetGenes().Skip(n).Take(n).Select(g => (int)g.Value == 1).ToArray();
        }

        /// <summary>
        /// Ensures permutation validity and mandatory inspection bits after crossover/mutation.
        /// </summary>
        public void Repair()
        {
            // Repair permutation: ensure genes 0..n-1 form a permutation of 0..n-1
            var genes = GetGenes();
            var seen = new bool[n];
            var missing = new List<int>();
            for (int i = 0; i < n; i++)
            {
                int val = (int)genes[i].Value;
                if (val >= 0 && val < n && !seen[val])
                {
                    seen[val] = true;
                }
                else
                {
                    genes[i] = new Gene(-1); // mark to replace
                }
            }
            for (int v = 0; v < n; v++)
            {
                if (!seen[v]) missing.Add(v);
            }
            int mIndex = 0;
            for (int i = 0; i < n; i++)
            {
                if ((int)genes[i].Value == -1)
                {
                    genes[i] = new Gene(missing[mIndex++]);
                }
            }
            ReplaceGenes(0, genes.Take(n).ToArray());

            // Repair inspection bits: force mandatory to 1 and limit to 0/1
            for (int i = 0; i < n; i++)
            {
                int bit = (int)genes[n + i].Value;
                if (mandatoryInspections[i]) bit = 1;
                bit = bit != 0 ? 1 : 0;
                genes[n + i] = new Gene(bit);
            }
            ReplaceGenes(n, genes.Skip(n).Take(n).ToArray());
        }
    }
}
