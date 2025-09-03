using System;
using System.Collections.Generic;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;

namespace UnitySimuLean
{
    /// <summary>
    /// Fitness function for the scheduling problem. It evaluates chromosomes by
    /// simulating the sequential processing of jobs on a single machine and
    /// computing delays and inspections performed.
    /// </summary>
    public class ScheduleFitness : IFitness
    {
        private readonly IReadOnlyList<Job> _jobs;
        private readonly Dictionary<IChromosome, (int delay, int inspections)> _metrics =
            new Dictionary<IChromosome, (int delay, int inspections)>();

        public ScheduleFitness(IReadOnlyList<Job> jobs)
        {
            _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        }

        public double Evaluate(IChromosome chromosome)
        {
            var (fitness, _, _) = EvaluateWithMetrics(chromosome);
            return fitness;
        }

        public (double fitness, int delayCount, int inspectionCount) EvaluateWithMetrics(IChromosome chromosome)
        {
            if (!(chromosome is ScheduleChromosome sch))
            {
                throw new ArgumentException("Chromosome must be ScheduleChromosome", nameof(chromosome));
            }

            sch.Repair(_jobs);
            var order = sch.GetOrder();
            var inspect = sch.GetInspectionVector();

            double completion = 0;
            int delays = 0;
            int inspections = 0;

            for (int pos = 0; pos < order.Length; pos++)
            {
                var job = _jobs[order[pos]];
                bool doInspection = inspect[pos] || job.inspeccionOn == 1;
                double procTime = job.tSoldadura + (doInspection ? job.tInspeccion : 0);
                completion += procTime;
                if (completion > job.DueDate)
                {
                    delays++;
                }
                if (doInspection)
                {
                    inspections++;
                }
            }

            double fitness = inspections - (delays * 100);
            _metrics[chromosome] = (delays, inspections);
            return (fitness, delays, inspections);
        }

        public (int delayCount, int inspectionCount) GetMetrics(IChromosome chromosome)
        {
            if (_metrics.TryGetValue(chromosome, out var m))
            {
                _metrics.Clear();
                _metrics[chromosome] = m;
                return m;
            }
            return (0, 0);
        }
    }
}

