using System.Linq;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Randomizations;

namespace SimuLean
{
    /// <summary>
    /// Chromosome representing a sequence of part indices.
    /// Each gene stores the index of a part, and the chromosome
    /// holds a permutation of all parts.
    /// </summary>
    public class SequenceChromosome : ChromosomeBase
    {
        private readonly int _length;

        /// <summary>
        /// Initializes a new instance of the <see cref="SequenceChromosome"/> class
        /// with a random permutation of <paramref name="length"/> part indices.
        /// </summary>
        /// <param name="length">Number of parts in the sequence.</param>
        public SequenceChromosome(int length) : base(length)
        {
            _length = length;

            var permutation = RandomizationProvider.Current.GetPermutation(length);
            for (int i = 0; i < length; i++)
            {
                ReplaceGene(i, new Gene(permutation[i]));
            }
        }

        /// <summary>
        /// Generates a gene for the specified index. Genes are initialized
        /// with their own index; the constructor later replaces them with a
        /// random permutation to ensure uniqueness.
        /// </summary>
        /// <param name="geneIndex">Index of the gene.</param>
        /// <returns>A new <see cref="Gene"/> representing the given index.</returns>
        public override Gene GenerateGene(int geneIndex)
        {
            return new Gene(geneIndex);
        }

        /// <summary>
        /// Creates a new chromosome with the same length.
        /// </summary>
        /// <returns>A new <see cref="SequenceChromosome"/> instance.</returns>
        public override IChromosome CreateNew()
        {
            return new SequenceChromosome(_length);
        }

        /// <summary>
        /// Retrieves the sequence of part indices represented by this chromosome.
        /// </summary>
        /// <returns>Array with part indices in their current order.</returns>
        public int[] GetSequence()
        {
            return GetGenes().Select(g => (int)g.Value).ToArray();
        }
    }
}
