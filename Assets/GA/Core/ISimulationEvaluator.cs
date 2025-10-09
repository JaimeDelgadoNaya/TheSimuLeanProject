using System.Collections.Generic;

namespace ChapasGA.GA.Core
{
    /// <summary>
    /// Interface for evaluating simulation-based optimization solutions.
    /// Provides a contract for running simulations and calculating fitness metrics.
    /// </summary>
    /// <typeparam name="TData">Type of input data (e.g., Chapa, Order, etc.)</typeparam>
    /// <typeparam name="TResult">Type of simulation result</typeparam>
    public interface ISimulationEvaluator<TData, TResult>
    {
        /// <summary>
        /// Runs a simulation with the given data and configuration.
        /// </summary>
        /// <param name="data">Input data for the simulation</param>
        /// <param name="order">Sequence order (0-based indices)</param>
        /// <param name="decisionBits">Binary decisions for each item (e.g., inspection flags)</param>
        /// <returns>Simulation result containing metrics</returns>
        TResult RunSimulation(IList<TData> data, int[] order, int[] decisionBits);
        
        /// <summary>
        /// Calculates fitness score from simulation result.
        /// Higher values indicate better solutions.
        /// </summary>
        /// <param name="result">Simulation result</param>
        /// <returns>Fitness score</returns>
        double CalculateFitness(TResult result);
    }
}
