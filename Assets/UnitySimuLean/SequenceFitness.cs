using System;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;

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
            if (chromosome == null)
            {
                throw new ArgumentNullException(nameof(chromosome));
            }

            var seqChromosome = chromosome as SequenceChromosome;
            if (seqChromosome == null)
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

            // 4. Compute a fitness score. Lower delays/inspections yield a higher value.
            double fitness = -(totalDelay + _alpha * inspectionCount);

            // Return the computed fitness.
            return fitness;
        }
    }
}

