using System;
using System.Collections.Generic;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Randomizations;

namespace UnitySimuLean
{
    /// <summary>
    /// Chromosome that represents a sequence of part entries.
    /// Each gene contains the index of a part in the sequence and
    /// provides lookups between part identifiers and their positions.
    /// </summary>
    public class SequenceChromosome : ChromosomeBase
    {
        private readonly string[] idByIndex; // Maps index to part identifier.
        private readonly Dictionary<string, int> indexById; // Reverse map from identifier to index.

        /// <summary>
        /// Initializes a new instance of the <see cref="SequenceChromosome"/> class
        /// with a random permutation of part indices.
        /// </summary>
        /// <param name="length">Number of parts in the sequence.</param>
        public SequenceChromosome(int length)
            : this(Enumerable.Range(0, length).Select(i => i.ToString()).ToList())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SequenceChromosome"/> class
        /// using the provided part identifiers.
        /// </summary>
        /// <param name="partIds">List of part identifiers.</param>
        public SequenceChromosome(IList<string> partIds) : base(partIds.Count)
        {
            idByIndex = partIds.ToArray();
            indexById = partIds.Select((id, idx) => new { id, idx })
                                .ToDictionary(x => x.id, x => x.idx);

            var sequence = Enumerable.Range(0, partIds.Count)
                                     .OrderBy(_ => RandomizationProvider.Current.GetDouble())
                                     .ToArray();

            for (int i = 0; i < partIds.Count; i++)
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
        /// Creates a new instance of the chromosome preserving the mapping
        /// between sequence indexes and part identifiers.
        /// </summary>
        /// <returns>A new <see cref="SequenceChromosome"/> with a fresh random
        /// sequence but referencing the same part identifiers.</returns>
        public override IChromosome CreateNew()
        {
            // Use the original list of part identifiers instead of numeric
            // placeholders to ensure evolved chromosomes can be translated
            // back into valid schedule references.
            return new SequenceChromosome(idByIndex);
        }

        /// <summary>
        /// Gets the sequence represented by this chromosome as an array of part identifiers.
        /// </summary>
        /// <returns>An array representing the part order.</returns>
        public string[] GetSequence()
        {
            return GetGenes().Select(g => idByIndex[(int)g.Value]).ToArray();
        }

        /// <summary>
        /// Gets the index associated with the specified part identifier in the original collection.
        /// </summary>
        /// <param name="partId">The part identifier.</param>
        /// <returns>The zero-based index of the part.</returns>
        /// <exception cref="ArgumentException">Thrown when the part identifier is unknown.</exception>
        public int GetPartIndex(string partId)
        {
            if (!indexById.TryGetValue(partId, out var idx))
            {
                throw new ArgumentException($"Unknown part identifier: {partId}", nameof(partId));
            }

            return idx;
        }

        /// <summary>
        /// Gets the position of the specified part identifier within the current sequence.
        /// </summary>
        /// <param name="partId">The part identifier.</param>
        /// <returns>The zero-based position of the part inside the chromosome.</returns>
        /// <exception cref="ArgumentException">Thrown when the part identifier is unknown.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the part is not part of the sequence.</exception>
        public int GetPosition(string partId)
        {
            var partIndex = GetPartIndex(partId);
            var genes = GetGenes();

            for (int i = 0; i < genes.Length; i++)
            {
                if ((int)genes[i].Value == partIndex)
                {
                    return i;
                }
            }

            throw new InvalidOperationException($"Part identifier '{partId}' does not exist in the chromosome sequence.");
        }
    }
}

