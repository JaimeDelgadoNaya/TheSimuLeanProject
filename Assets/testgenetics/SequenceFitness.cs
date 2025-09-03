using System;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;
using System.Collections.Generic;

namespace UnitySimuLean
{
    /// <summary>
    /// Fitness function that evaluates a chromosome representing an entry
    /// sequence of parts by executing a SimuLean model and retrieving its
    /// performance metrics.
    /// </summary>
    public class SequenceFitness : IFitness
    {
        private readonly ISimulationRunner _runner;
        private readonly Dictionary<IChromosome, (int delay, int inspections)> _metrics =
            new Dictionary<IChromosome, (int delay, int inspections)>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SequenceFitness"/> class.
        /// </summary>
        /// <param name="runner">Simulation runner used to evaluate sequences.</param>
        public SequenceFitness(ISimulationRunner runner)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        }

        /// <summary>
        /// Evaluates the specified chromosome by configuring and running the
        /// simulation with its part sequence.
        /// </summary>
        /// <param name="chromosome">Chromosome to evaluate.</param>
        /// <returns>The fitness value associated with the chromosome.</returns>
        public double Evaluate(IChromosome chromosome)
        {
            var (fitness, _, _) = EvaluateWithMetrics(chromosome);
            return fitness;
        }

        /// <summary>
        /// Evaluates the chromosome and returns its fitness along with the
        /// collected performance metrics.
        /// </summary>
        /// <param name="chromosome">Chromosome to evaluate.</param>
        /// <returns>
        /// A tuple containing the fitness, delay count and inspection count
        /// obtained from the simulation run.
        /// </returns>
        public (double fitness, int delayCount, int inspectionCount) EvaluateWithMetrics(IChromosome chromosome)
        {
            if (chromosome == null)
            {
                throw new ArgumentNullException(nameof(chromosome));
            }

            if (!(chromosome is SequenceChromosome seqChromosome))
            {
                throw new ArgumentException(
                    "Chromosome must be a SequenceChromosome instance.",
                    nameof(chromosome));
            }

            // 1. Configure simulation with the sequence represented by the chromosome.
            string[] sequence = seqChromosome.GetSequence();
            _runner.Configure(sequence);

            // 2. Run the simulation to completion.
            _runner.Run();

            // 3. Collect performance metrics from the simulation.
            int delayCount = _runner.DelayCount;
            int inspectionCount = _runner.InspectionCount;

            // Store metrics for later retrieval.
            _metrics[chromosome] = (delayCount, inspectionCount);

            // 4. Compute a fitness score that rewards inspections and penalizes
            // delays. Ensure the fitness value remains positive so the GA can
            // properly maximize it.
            double fitness = inspectionCount - delayCount;
            if (fitness <= 0)
            {
                fitness = 1e-6;
            }

            return (fitness, delayCount, inspectionCount);
        }

        /// <summary>
        /// Gets the metrics collected for a previously evaluated chromosome.
        /// </summary>
        /// <param name="chromosome">Chromosome whose metrics are requested.</param>
        /// <returns>The delay count and inspection count associated with the chromosome.</returns>
        public (int delayCount, int inspectionCount) GetMetrics(IChromosome chromosome)
        {
            if (_metrics.TryGetValue(chromosome, out var metrics))
            {
                // Remove metrics from previous chromosomes to avoid uncontrolled
                // growth of the dictionary. Retain only the metrics for the
                // requested chromosome so they remain available if needed again.
                _metrics.Clear();
                _metrics[chromosome] = metrics;
                return metrics;
            }

            return (0, 0);
        }
    }
}

