using System;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Randomizations;

namespace ChapasGA.GA
{
    [Obsolete("Use SequenceBinaryMutation instead.")]
    public class ChapaMutation : MutationBase
    {
        protected override void PerformMutate(IChromosome chromosome, float probability)
        {
            var c = chromosome as ChapaChromosome ?? throw new ArgumentException("Invalid chromosome");
            var rnd = RandomizationProvider.Current;
            int size = c.LengthPerPart;
            if (rnd.GetDouble() <= probability)
            {
                int i = rnd.GetInt(0, size);
                int j = rnd.GetInt(0, size);
                var gi = c.GetGene(i);
                var gj = c.GetGene(j);
                c.ReplaceGene(i, gj);
                c.ReplaceGene(j, gi);
            }
            if (rnd.GetDouble() <= probability)
            {
                int i = rnd.GetInt(0, size);
                var g = c.GetGene(size + i);
                int bit = (int)g.Value;
                bit = bit == 0 ? 1 : 0;
                c.ReplaceGene(size + i, new Gene(bit));
            }
            c.Repair();
        }
    }
}
