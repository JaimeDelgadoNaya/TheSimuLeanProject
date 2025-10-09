using System;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Randomizations;

namespace ChapasGA.GA.Core
{
    /// <summary>
    /// Generic mutation operator for SequenceBinaryChromosome.
    /// - Uses Swap Mutation for sequence genes
    /// - Uses Bit Flip Mutation for binary genes
    /// </summary>
    public class SequenceBinaryMutation : MutationBase
    {
        protected override void PerformMutate(IChromosome chromosome, float probability)
        {
            var c = chromosome as SequenceBinaryChromosome ?? 
                throw new ArgumentException("Chromosome must be a SequenceBinaryChromosome");
            
            var rnd = RandomizationProvider.Current;

            // Mutate sequence part (swap mutation)
            if (c.HasSequence && rnd.GetDouble() <= probability)
            {
                MutateSequence(c, rnd);
            }

            // Mutate binary part (bit flip mutation)
            if (c.HasBinary && rnd.GetDouble() <= probability)
            {
                MutateBinary(c, rnd);
            }

            c.RepairSequence();
        }

        /// <summary>
        /// Performs swap mutation on the sequence genes.
        /// </summary>
        private void MutateSequence(SequenceBinaryChromosome chromosome, IRandomization rnd)
        {
            int size = chromosome.SequenceLength;
            int i = rnd.GetInt(0, size);
            int j = rnd.GetInt(0, size);
            
            var gi = chromosome.GetGene(i);
            var gj = chromosome.GetGene(j);
            
            chromosome.ReplaceGene(i, gj);
            chromosome.ReplaceGene(j, gi);
        }

        /// <summary>
        /// Performs bit flip mutation on the binary genes.
        /// </summary>
        private void MutateBinary(SequenceBinaryChromosome chromosome, IRandomization rnd)
        {
            int size = chromosome.BinaryLength;
            int offset = chromosome.SequenceLength;
            int i = rnd.GetInt(0, size);
            
            var g = chromosome.GetGene(offset + i);
            int bit = (int)g.Value;
            bit = bit == 0 ? 1 : 0;
            
            chromosome.ReplaceGene(offset + i, new Gene(bit));
        }
    }
}
