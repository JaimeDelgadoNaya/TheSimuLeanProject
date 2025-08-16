using System;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Randomizations;

namespace UnitySimuLean
{
    /// <summary>
    /// Chromosome that represents a sequence of part entries.
    /// Each gene contains the index of a part in the sequence.
    /// </summary>
    public class SequenceChromosome : ChromosomeBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SequenceChromosome"/> class
        /// with a random permutation of part indices.
        /// </summary>
        /// <param name="length">Number of parts in the sequence.</param>
        public SequenceChromosome(int length) : base(length)
        {
            var sequence = Enumerable.Range(0, length)
                                     .OrderBy(_ => RandomizationProvider.Current.GetDouble())
                                     .ToArray();

            for (int i = 0; i < length; i++)
            {
                ReplaceGene(i, new Gene(sequence[i]));
            }
        }

        /// <summary>
        /// Generates a gene ensuring it does not duplicate existing values,
        /// keeping the chromosome as a valid permutation.
        /// </summary>
        /// <param name="geneIndex">Index of the gene to be generated.</param>
        /// <returns>The generated gene.</returns>
        public override Gene GenerateGene(int geneIndex)
        {
            var currentValues = GetGenes().Select(g => (int)g.Value).ToList();
            var available = Enumerable.Range(0, Length)
                                      .Except(currentValues)
                                      .ToList();

            int value = available.Count > 0
                ? available[RandomizationProvider.Current.GetInt(0, available.Count)]
                : RandomizationProvider.Current.GetInt(0, Length);

            return new Gene(value);
        }

        /// <summary>
        /// Creates a new instance of the chromosome with the same length.
        /// </summary>
        /// <returns>A new SequenceChromosome.</returns>
        public override IChromosome CreateNew()
        {
            return new SequenceChromosome(Length);
        }

        /// <summary>
        /// Gets the sequence represented by this chromosome as an array of part indices.
        /// </summary>
        /// <returns>An array representing the part order.</returns>
        public int[] GetSequence()
        {
            return GetGenes().Select(g => (int)g.Value).ToArray();
        }
    }
}

