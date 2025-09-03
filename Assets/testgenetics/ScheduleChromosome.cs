using System;
using System.Collections.Generic;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Randomizations;

namespace UnitySimuLean
{
    /// <summary>
    /// Chromosome composed of two parts: a permutation of job indices that
    /// represents the processing order and a binary vector indicating whether
    /// each job should be inspected. The permutation occupies the first N genes
    /// and the inspection decisions the last N genes.
    /// </summary>
    public class ScheduleChromosome : ChromosomeBase
    {
        private readonly int _jobCount;

        public ScheduleChromosome(int jobCount) : base(jobCount * 2)
        {
            _jobCount = jobCount;

            var order = Enumerable.Range(0, jobCount)
                                   .OrderBy(_ => RandomizationProvider.Current.GetDouble())
                                   .ToArray();

            for (int i = 0; i < jobCount; i++)
            {
                ReplaceGene(i, new Gene(order[i]));
                ReplaceGene(jobCount + i, new Gene(RandomizationProvider.Current.GetInt(0, 2)));
            }
        }

        public override Gene GenerateGene(int geneIndex)
        {
            if (geneIndex < _jobCount)
            {
                var current = GetGenes().Take(_jobCount).Select(g => (int)g.Value).ToList();
                var available = Enumerable.Range(0, _jobCount).Except(current).ToList();
                int value = available.Count > 0
                    ? available[RandomizationProvider.Current.GetInt(0, available.Count)]
                    : RandomizationProvider.Current.GetInt(0, _jobCount);
                return new Gene(value);
            }
            else
            {
                return new Gene(RandomizationProvider.Current.GetInt(0, 2));
            }
        }

        public override IChromosome CreateNew()
        {
            return new ScheduleChromosome(_jobCount);
        }

        /// <summary>
        /// Gets the job order represented by the chromosome.
        /// </summary>
        public int[] GetOrder()
        {
            return GetGenes().Take(_jobCount).Select(g => (int)g.Value).ToArray();
        }

        /// <summary>
        /// Gets the inspection decision vector represented by the chromosome.
        /// </summary>
        public bool[] GetInspectionVector()
        {
            return GetGenes().Skip(_jobCount).Take(_jobCount)
                             .Select(g => (int)g.Value == 1)
                             .ToArray();
        }

        /// <summary>
        /// Replaces the genes of the chromosome using the specified order and
        /// inspection vectors.
        /// </summary>
        public void SetData(int[] order, bool[] inspection)
        {
            for (int i = 0; i < _jobCount; i++)
            {
                ReplaceGene(i, new Gene(order[i]));
                ReplaceGene(_jobCount + i, new Gene(inspection[i] ? 1 : 0));
            }
        }

        /// <summary>
        /// Repairs the chromosome ensuring the permutation part is valid and
        /// that inspection decisions are set to 1 when required by the job
        /// configuration.
        /// </summary>
        public void Repair(IReadOnlyList<Job> jobs)
        {
            var genes = GetGenes();
            var used = new HashSet<int>();
            for (int i = 0; i < _jobCount; i++)
            {
                int val = (int)genes[i].Value;
                if (!used.Add(val))
                {
                    var missing = Enumerable.Range(0, _jobCount).Except(used).ToList();
                    val = missing[RandomizationProvider.Current.GetInt(0, missing.Count)];
                    used.Add(val);
                }
                ReplaceGene(i, new Gene(val));
            }

            for (int i = 0; i < _jobCount; i++)
            {
                int jobIndex = (int)GetGene(i).Value;
                if (jobs[jobIndex].inspeccionOn == 1)
                {
                    ReplaceGene(_jobCount + i, new Gene(1));
                }
            }
        }
    }
}

