using System;
using System.Collections.Generic;
using System.Linq;
using ChapasGA.Models;
using SimuLean.Serialization;
using ChapasGA.GA.Core;
using ChapasGA.GA.Utils;
using ChapasGA.GA.Adapters;

namespace ChapasGA.GA.Optimization
{
    /// <summary>
    /// Evaluates Chapa sequences by running headless simulations.
    /// Implements ISimulationEvaluator to decouple simulation logic from GA execution.
    /// Thread-safe for parallel evaluation.
    /// 
    /// NOTE: This is now a wrapper around GenericSimulationEvaluator for backward compatibility.
    /// Consider using GenericSimulationEvaluator directly for new code.
    /// </summary>
    public class ChapaSimulationEvaluator : ISimulationEvaluator<Chapa, SimulationMetrics>
    {
        private readonly GenericSimulationEvaluator<Chapa> genericEvaluator;
        
        public ChapaSimulationEvaluator(SimulationConfig modelConfig)
        {
            if (modelConfig == null)
                throw new ArgumentNullException(nameof(modelConfig));
            
            var transformer = new ChapaDataTransformer();
            this.genericEvaluator = new GenericSimulationEvaluator<Chapa>(modelConfig, transformer);
        }

        /// <summary>
        /// Runs a headless simulation with the specified order and inspection decisions.
        /// Thread-safe: Creates a deep clone of the config for each evaluation.
        /// </summary>
        public SimulationMetrics RunSimulation(IList<Chapa> chapas, int[] order, int[] inspectionBits)
        {
            return genericEvaluator.RunSimulation(chapas, order, inspectionBits);
        }

        /// <summary>
        /// Calculates fitness from simulation metrics.
        /// </summary>
        public double CalculateFitness(SimulationMetrics result)
        {
            return genericEvaluator.CalculateFitness(result);
        }
    }
}
