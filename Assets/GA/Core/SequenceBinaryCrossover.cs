using System;
using System.Collections.Generic;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Randomizations;

namespace ChapasGA.GA.Core
{
    /// <summary>
    /// Generic crossover operator for SequenceBinaryChromosome.
    /// - Uses Order Crossover (OX) for sequence genes
    /// - Uses Uniform Crossover for binary genes
    /// </summary>
    public class SequenceBinaryCrossover : CrossoverBase
    {
        public SequenceBinaryCrossover() : base(2, 2)
        {
        }

        protected override IList<IChromosome> PerformCross(IList<IChromosome> parents)
        {
            var p1 = parents[0] as SequenceBinaryChromosome ?? 
                throw new ArgumentException("Parent 1 must be a SequenceBinaryChromosome");
            var p2 = parents[1] as SequenceBinaryChromosome ?? 
                throw new ArgumentException("Parent 2 must be a SequenceBinaryChromosome");

            if (p1.SequenceLength != p2.SequenceLength || p1.BinaryLength != p2.BinaryLength)
                throw new ArgumentException("Parents must have the same sequence and binary lengths");

            var child1 = p1.Clone() as SequenceBinaryChromosome;
            var child2 = p2.Clone() as SequenceBinaryChromosome;

            var rnd = RandomizationProvider.Current;

            // Crossover sequence part (if exists)
            if (p1.HasSequence)
            {
                CrossoverSequence(p1, p2, child1, child2, rnd);
            }

            // Crossover binary part (if exists)
            if (p1.HasBinary)
            {
                CrossoverBinary(p1, p2, child1, child2, rnd);
            }

            child1.RepairSequence();
            child2.RepairSequence();
            
            return new List<IChromosome> { child1, child2 };
        }

        /// <summary>
        /// Performs Order Crossover (OX) on the sequence genes.
        /// </summary>
        private void CrossoverSequence(
            SequenceBinaryChromosome p1, 
            SequenceBinaryChromosome p2,
            SequenceBinaryChromosome child1, 
            SequenceBinaryChromosome child2,
            IRandomization rnd)
        {
            int size = p1.SequenceLength;

            int cut1 = rnd.GetInt(0, size);
            int cut2 = rnd.GetInt(0, size);
            if (cut1 > cut2) (cut1, cut2) = (cut2, cut1);

            var o1 = p1.GetSequence();
            var o2 = p2.GetSequence();
            var c1 = new int[size];
            var c2 = new int[size];
            
            // Initialize with -1 (empty)
            for (int i = 0; i < size; i++)
            {
                c1[i] = -1;
                c2[i] = -1;
            }
            
            // Copy segment
            for (int i = cut1; i <= cut2; i++)
            {
                c1[i] = o1[i];
                c2[i] = o2[i];
            }
            
            // Fill remaining positions
            int current1 = (cut2 + 1) % size;
            int current2 = current1;
            
            for (int i = 0; i < size; i++)
            {
                int idx = (cut2 + 1 + i) % size;
                
                int val2 = o2[idx];
                if (Array.IndexOf(c1, val2) == -1)
                {
                    c1[current1] = val2;
                    current1 = (current1 + 1) % size;
                }
                
                int val1 = o1[idx];
                if (Array.IndexOf(c2, val1) == -1)
                {
                    c2[current2] = val1;
                    current2 = (current2 + 1) % size;
                }
            }
            
            // Update children
            for (int i = 0; i < size; i++)
            {
                child1.ReplaceGene(i, new Gene(c1[i]));
                child2.ReplaceGene(i, new Gene(c2[i]));
            }
        }

        /// <summary>
        /// Performs Uniform Crossover on the binary genes.
        /// </summary>
        private void CrossoverBinary(
            SequenceBinaryChromosome p1, 
            SequenceBinaryChromosome p2,
            SequenceBinaryChromosome child1, 
            SequenceBinaryChromosome child2,
            IRandomization rnd)
        {
            int size = p1.BinaryLength;
            int offset = p1.SequenceLength;

            var b1 = p1.GetBinaryDecisions();
            var b2 = p2.GetBinaryDecisions();
            
            for (int i = 0; i < size; i++)
            {
                if (rnd.GetDouble() < 0.5)
                {
                    child1.ReplaceGene(offset + i, new Gene(b1[i]));
                    child2.ReplaceGene(offset + i, new Gene(b2[i]));
                }
                else
                {
                    child1.ReplaceGene(offset + i, new Gene(b2[i]));
                    child2.ReplaceGene(offset + i, new Gene(b1[i]));
                }
            }
        }
    }
}
