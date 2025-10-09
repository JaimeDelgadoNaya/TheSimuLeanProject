using System;
using System.Collections.Generic;
using System.Linq;
using SimuLean;
using SimuLean.Headless;
using SimuLean.Serialization;
using ChapasGA.GA.Core;
using ChapasGA.GA.Utils;

namespace ChapasGA.GA.Optimization
{
    /// <summary>
    /// Generic simulation evaluator that runs headless simulations.
    /// Can work with any data type that needs sequence and/or binary optimization.
    /// Thread-safe for parallel evaluation.
    /// </summary>
    /// <typeparam name="TData">The type of data items being optimized</typeparam>
    public class GenericSimulationEvaluator<TData> : ISimulationEvaluator<TData, SimulationMetrics>
    {
        private readonly SimulationConfig modelConfigTemplate;
        private readonly IDataTransformer<TData> dataTransformer;
        
        /// <summary>
        /// Creates a generic simulation evaluator.
        /// </summary>
        /// <param name="modelConfig">Template simulation configuration</param>
        /// <param name="dataTransformer">Transforms data items into simulation format</param>
        public GenericSimulationEvaluator(
            SimulationConfig modelConfig, 
            IDataTransformer<TData> dataTransformer)
        {
            this.modelConfigTemplate = modelConfig ?? throw new ArgumentNullException(nameof(modelConfig));
            this.dataTransformer = dataTransformer ?? throw new ArgumentNullException(nameof(dataTransformer));
        }

        /// <summary>
        /// Runs a headless simulation with the specified order and binary decisions.
        /// Thread-safe: Creates a deep clone of the config for each evaluation.
        /// </summary>
        public SimulationMetrics RunSimulation(IList<TData> data, int[] order, int[] binaryDecisions)
        {
            // Apply default values if not provided
            order = order ?? Enumerable.Range(0, data.Count).ToArray();
            binaryDecisions = binaryDecisions ?? new int[data.Count];

            // CRITICAL: Clone the config for thread safety
            var localConfig = CloneSimulationConfig(modelConfigTemplate);

            // Transform data using the provided transformer
            var dataDict = dataTransformer.ConvertToDataDict(data);
            var priorities = ConvertOrderToPriorities(order);
            
            // Add binary decisions if provided
            if (binaryDecisions != null && binaryDecisions.Length > 0)
            {
                var binaryLabel = dataTransformer.GetBinaryDecisionLabel();
                if (!string.IsNullOrEmpty(binaryLabel))
                {
                    SeqOptTools.AddLabelsToDict(dataDict, binaryLabel, binaryDecisions);
                }
            }
            
            var reorderedDataDict = SeqOptTools.TransformSequence(dataDict, priorities);
            
            // Update local config
            InjectReorderedDataIntoConfig(localConfig, reorderedDataDict);

            // Create and run headless simulation
            var clock = new SimClock();
            var factory = new HeadlessModelFactory(clock, enableLogging: false);
            var elements = factory.BuildModel(localConfig);

            // Find sink to collect results
            Sink sink = FindSink(elements);
            if (sink != null)
            {
                sink.expectedItems = data.Count;
            }

            // Start simulation
            foreach (var element in elements.Values)
            {
                element.Start();
            }

            // Run simulation
            double maxSimTime = dataTransformer.CalculateMaxSimTime(data);
            clock.AdvanceClock(maxSimTime);

            // Collect results
            return new SimulationMetrics
            {
                TotalItems = sink?.GetNumberIterms() ?? 0,
                TotalInspections = sink?.GetInspecciones() ?? 0,
                TotalDelays = sink?.GetRetrasados() ?? 0,
                SimulationTime = clock.GetSimulationTime()
            };
        }

        /// <summary>
        /// Calculates fitness from simulation metrics.
        /// </summary>
        public double CalculateFitness(SimulationMetrics result)
        {
            return result.CalculateFitness();
        }

        #region Helper Methods

        /// <summary>
        /// Creates a deep clone of the simulation config to ensure thread safety.
        /// </summary>
        private SimulationConfig CloneSimulationConfig(SimulationConfig template)
        {
            var clone = new SimulationConfig
            {
                MaxSimulationTime = template.MaxSimulationTime,
                Elements = new List<ElementConfig>(),
                Connections = new List<ConnectionConfig>()
            };

            // Clone elements
            foreach (var elem in template.Elements)
            {
                var elemClone = new ElementConfig(elem.Id, elem.Type, elem.Name);
                
                if (elem.Parameters != null)
                {
                    elemClone.Parameters = new Dictionary<string, object>();
                    foreach (var kvp in elem.Parameters)
                    {
                        // Deep clone collections
                        if (kvp.Value is Dictionary<string, List<string>> dict)
                        {
                            var dictClone = new Dictionary<string, List<string>>();
                            foreach (var entry in dict)
                            {
                                dictClone[entry.Key] = new List<string>(entry.Value);
                            }
                            elemClone.Parameters[kvp.Key] = dictClone;
                        }
                        else if (kvp.Value is List<string> list)
                        {
                            elemClone.Parameters[kvp.Key] = new List<string>(list);
                        }
                        else if (kvp.Value is int[] intArray)
                        {
                            elemClone.Parameters[kvp.Key] = (int[])intArray.Clone();
                        }
                        else
                        {
                            elemClone.Parameters[kvp.Key] = kvp.Value;
                        }
                    }
                }
                
                clone.Elements.Add(elemClone);
            }

            // Clone connections
            foreach (var conn in template.Connections)
            {
                clone.Connections.Add(new ConnectionConfig(conn.SourceId, conn.TargetId, conn.ConnectionType));
            }

            return clone;
        }

        private int[] ConvertOrderToPriorities(int[] order)
        {
            if (order == null || order.Length == 0)
            {
                throw new ArgumentException("Order array cannot be null or empty");
            }

            var priorities = new int[order.Length];
            
            for (int i = 0; i < order.Length; i++)
            {
                int orderValue = order[i];
                
                if (orderValue < 0 || orderValue >= order.Length)
                {
                    throw new ArgumentException(
                        $"Invalid order value at index {i}: {orderValue}. " +
                        $"Order values must be in range [0, {order.Length - 1}]");
                }
                
                priorities[orderValue] = i + 1; // Convert to 1-based
            }
            
            return priorities;
        }

        private void InjectReorderedDataIntoConfig(SimulationConfig config, Dictionary<string, List<string>> reorderedDataDict)
        {
            foreach (var elemConfig in config.Elements)
            {
                if (elemConfig.Type == "UnityScheduleSource")
                {
                    elemConfig.Parameters = elemConfig.Parameters ?? new Dictionary<string, object>();
                    elemConfig.Parameters["dataDict"] = reorderedDataDict;
                    elemConfig.Parameters["autoSort"] = false;
                }
            }
        }

        private Sink FindSink(Dictionary<string, Element> elements)
        {
            foreach (var element in elements.Values)
            {
                if (element is Sink sink)
                    return sink;
            }
            return null;
        }

        #endregion
    }

    /// <summary>
    /// Interface for transforming domain-specific data into simulation format.
    /// Implement this interface for your specific data type.
    /// </summary>
    /// <typeparam name="TData">The type of data items</typeparam>
    public interface IDataTransformer<TData>
    {
        /// <summary>
        /// Converts a list of data items into a column-oriented dictionary for simulation.
        /// </summary>
        Dictionary<string, List<string>> ConvertToDataDict(IList<TData> data);
        
        /// <summary>
        /// Calculates the maximum simulation time based on the data.
        /// </summary>
        double CalculateMaxSimTime(IList<TData> data);
        
        /// <summary>
        /// Gets the label name for binary decisions (e.g., "inspeccionOn").
        /// Return null or empty if no binary decisions are used.
        /// </summary>
        string GetBinaryDecisionLabel();
    }
}
