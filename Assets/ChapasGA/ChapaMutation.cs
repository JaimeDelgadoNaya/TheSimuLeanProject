using System;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Randomizations;

namespace ChapasGA
{
    /// <summary>
    /// Custom mutation: swap two positions in the permutation and flip one optional inspection bit.
    /// </summary>
    public class ChapaMutation : MutationBase
    {
        private readonly bool[] mandatory;

        public ChapaMutation(bool[] mandatory)
        {
            this.mandatory = mandatory;
        }

        protected override void PerformMutate(IChromosome chromosome, float probability)
        {
            var c = (ChapaChromosome)chromosome;
            int n = c.Length / 2;

            // Permutation swap mutation
            if (RandomizationProvider.Current.GetDouble() <= probability)
            {
                int i = RandomizationProvider.Current.GetInt(0, n);
                int j = RandomizationProvider.Current.GetInt(0, n);
                var g1 = c.GetGene(i);
                var g2 = c.GetGene(j);
                c.ReplaceGene(i, g2);
                c.ReplaceGene(j, g1);
            }

            // Flip bit mutation (on optional positions only)
            if (RandomizationProvider.Current.GetDouble() <= probability)
            {
                int idx;
                do
                {
                    idx = RandomizationProvider.Current.GetInt(0, n);
                } while (mandatory[idx]);
                var bit = c.GetGene(n + idx);
                int val = ((int)bit.Value) == 1 ? 0 : 1;
                c.ReplaceGene(n + idx, new Gene(val));
            }

            c.Repair();
        }
    }
}
