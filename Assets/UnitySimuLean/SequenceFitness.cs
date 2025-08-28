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
        private readonly double _alpha;
        private readonly Dictionary<IChromosome, (double delay, int inspections)> _metrics =
            new Dictionary<IChromosome, (double delay, int inspections)>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SequenceFitness"/> class.
        /// </summary>
        /// <param name="runner">Simulation runner used to evaluate sequences.</param>
        /// <param name="alpha">
        /// Weight applied to the inspection count when computing the fitness
        /// value. Defaults to 1.
        /// </param>
        public SequenceFitness(ISimulationRunner runner, double alpha = 1.0)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _alpha = alpha;
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
        /// A tuple containing the fitness, total delay and inspection count
        /// obtained from the simulation run.
        /// </returns>
        public (double fitness, double totalDelay, int inspectionCount) EvaluateWithMetrics(IChromosome chromosome)
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
            double totalDelay = _runner.TotalDelay;
            int inspectionCount = _runner.InspectionCount;

            // Store metrics for later retrieval.
            _metrics[chromosome] = (totalDelay, inspectionCount);

            // 4. Compute a fitness score. Lower delays/inspections yield a higher value.
            double fitness = -(totalDelay + _alpha * inspectionCount);

            return (fitness, totalDelay, inspectionCount);
        }

        /// <summary>
        /// Gets the metrics collected for a previously evaluated chromosome.
        /// </summary>
        /// <param name="chromosome">Chromosome whose metrics are requested.</param>
        /// <returns>The delay and inspection count associated with the chromosome.</returns>
        public (double totalDelay, int inspectionCount) GetMetrics(IChromosome chromosome)
        {
            return _metrics.TryGetValue(chromosome, out var m) ? m : (double.NaN, 0);
        }
    }
}

