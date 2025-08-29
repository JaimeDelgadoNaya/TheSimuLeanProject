using System;

namespace UnitySimuLean
{
    /// <summary>
    /// Abstraction of a simulation model capable of being configured with a
    /// sequence of parts and producing performance metrics after running.
    /// </summary>
    public interface ISimulationRunner
    {
        /// <summary>
        /// Configures the underlying simulation with the provided sequence of
        /// part identifiers. Implementations should reset any previous state
        /// so that consecutive calls start from the same conditions.
        /// </summary>
        /// <param name="sequence">Sequence of part identifiers.</param>
        void Configure(string[] sequence);

        /// <summary>
        /// Executes the simulation until completion.
        /// </summary>
        void Run();

        /// <summary>
        /// Gets the number of delayed items from the last executed simulation.
        /// </summary>
        int DelayCount { get; }

        /// <summary>
        /// Gets the number of inspections performed during the last run.
        /// </summary>
        int InspectionCount { get; }
    }
}

