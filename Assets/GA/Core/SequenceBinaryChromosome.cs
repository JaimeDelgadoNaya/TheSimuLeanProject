using System;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Randomizations;

namespace ChapasGA.GA.Core
{
    /// <summary>
    /// Generic chromosome for optimization problems involving:
    /// - Sequence optimization (permutation of items)
    /// - Binary decisions (0/1 choices for each item)
    /// - Combined optimization (both sequence and binary)
    /// </summary>
    public class SequenceBinaryChromosome : ChromosomeBase
    {
        public int SequenceLength { get; }
        public int BinaryLength { get; }
        public bool HasSequence => SequenceLength > 0;
        public bool HasBinary => BinaryLength > 0;

        /// <summary>
        /// Creates a chromosome for sequence and/or binary optimization.
        /// </summary>
        /// <param name="sequenceLength">Length of the sequence (permutation). Set to 0 if no sequence optimization needed.</param>
        /// <param name="binaryLength">Length of the binary array. Set to 0 if no binary optimization needed.</param>
        public SequenceBinaryChromosome(int sequenceLength, int binaryLength)
            : base(sequenceLength + binaryLength)
        {
            if (sequenceLength < 0 || binaryLength < 0)
                throw new ArgumentException("Lengths cannot be negative");
            
            if (sequenceLength == 0 && binaryLength == 0)
                throw new ArgumentException("At least one optimization type (sequence or binary) must be specified");

            SequenceLength = sequenceLength;
            BinaryLength = binaryLength;
            CreateGenes();
            
            if (HasSequence)
                RepairSequence();
        }

        public override IChromosome CreateNew()
        {
            return new SequenceBinaryChromosome(SequenceLength, BinaryLength);
        }

        public override Gene GenerateGene(int geneIndex)
        {
            var rnd = RandomizationProvider.Current;
            
            // Sequence genes (permutation)
            if (geneIndex < SequenceLength)
            {
                return new Gene(rnd.GetInt(0, SequenceLength));
            }
            
            // Binary genes (0 or 1)
            return new Gene(rnd.GetInt(0, 2));
        }

        /// <summary>
        /// Gets the sequence (permutation) part of the chromosome.
        /// Returns null if no sequence optimization is configured.
        /// </summary>
        public int[] GetSequence()
        {
            if (!HasSequence)
                return null;
                
            return GetGenes().Take(SequenceLength).Select(g => (int)g.Value).ToArray();
        }

        /// <summary>
        /// Gets the binary decision part of the chromosome.
        /// Returns null if no binary optimization is configured.
        /// </summary>
        public int[] GetBinaryDecisions()
        {
            if (!HasBinary)
                return null;
                
            return GetGenes().Skip(SequenceLength).Select(g => (int)g.Value).ToArray();
        }

        /// <summary>
        /// Repairs the sequence part to ensure it's a valid permutation.
        /// Only affects sequence genes if they exist.
        /// </summary>
        public void RepairSequence()
        {
            if (!HasSequence)
                return;

            var sequence = GetSequence();
            var fixedSequence = sequence.Distinct().ToList();
            
            // Fill in missing numbers
            for (int i = 0; i < SequenceLength; i++)
            {
                if (i >= fixedSequence.Count)
                {
                    for (int k = 0; k < SequenceLength; k++)
                    {
                        if (!fixedSequence.Contains(k))
                        {
                            fixedSequence.Add(k);
                            break;
                        }
                    }
                }
            }
            
            // Update genes
            for (int i = 0; i < SequenceLength; i++)
            {
                ReplaceGene(i, new Gene(fixedSequence[i]));
            }
        }

        /// <summary>
        /// Sets the sequence part of the chromosome.
        /// </summary>
        public void SetSequence(int[] sequence)
        {
            if (!HasSequence)
                throw new InvalidOperationException("This chromosome does not have sequence optimization enabled");
                
            if (sequence.Length != SequenceLength)
                throw new ArgumentException($"Sequence length must be {SequenceLength}");

            for (int i = 0; i < SequenceLength; i++)
            {
                ReplaceGene(i, new Gene(sequence[i]));
            }
            
            RepairSequence();
        }

        /// <summary>
        /// Sets the binary decisions part of the chromosome.
        /// </summary>
        public void SetBinaryDecisions(int[] decisions)
        {
            if (!HasBinary)
                throw new InvalidOperationException("This chromosome does not have binary optimization enabled");
                
            if (decisions.Length != BinaryLength)
                throw new ArgumentException($"Binary length must be {BinaryLength}");

            for (int i = 0; i < BinaryLength; i++)
            {
                ReplaceGene(SequenceLength + i, new Gene(decisions[i]));
            }
        }
    }
}
