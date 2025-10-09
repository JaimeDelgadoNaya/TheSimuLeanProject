using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChapasGA.Models;
using SimuLean.Serialization;
using ChapasGA.GA.Core;
using ChapasGA.GA.Utils;

namespace ChapasGA.GA.Optimization
{
    /// <summary>
    /// High-level optimizer for Chapa sequence optimization.
    /// Unified API for both synchronous and asynchronous GA execution.
    /// Supports parallel fitness evaluation for faster optimization.
    /// 
    /// NOTE: This is now a wrapper around GenericOptimizer for backward compatibility.
    /// Consider using GenericOptimizer directly for new code.
    /// </summary>
    public class ChapaOptimizer
    {
        private GenericOptimizer<Chapa> genericOptimizer;
        private readonly SimulationConfig modelConfig;

        // Settings that may be set before Optimize is called
        private bool enableParallelEvaluation = false;
        private int maxDegreeOfParallelism = Environment.ProcessorCount;
        private Action<string> logCallback;

        // Results
        public double BestFitness => genericOptimizer?.BestFitness ?? 0;
        public int TotalInspections => genericOptimizer?.TotalInspections ?? 0;
        public int TotalDelays => genericOptimizer?.TotalDelays ?? 0;
        public IList<int> BestOrder => genericOptimizer?.BestSequence;
        public int[] BestInspectionBits => genericOptimizer?.BestBinaryDecisions;

        // Events for async execution
        public event Action<OptimizationProgressEventArgs> ProgressChanged;
        public event Action<OptimizationCompletedEventArgs> Completed;
        
        // Logging callback
        public Action<string> LogCallback 
        { 
            get => logCallback;
            set 
            { 
                logCallback = value;
                if (genericOptimizer != null)
                    genericOptimizer.LogCallback = value;
            }
        }
        
        // Parallel execution settings
        public bool EnableParallelEvaluation 
        { 
            get => enableParallelEvaluation;
            set 
            { 
                enableParallelEvaluation = value;
                if (genericOptimizer != null)
                    genericOptimizer.EnableParallelEvaluation = value;
            }
        }
        
        public int MaxDegreeOfParallelism 
        { 
            get => maxDegreeOfParallelism;
            set 
            { 
                maxDegreeOfParallelism = value;
                if (genericOptimizer != null)
                    genericOptimizer.MaxDegreeOfParallelism = value;
            }
        }

        public ChapaOptimizer(SimulationConfig modelConfig)
        {
            this.modelConfig = modelConfig ?? throw new ArgumentNullException(nameof(modelConfig));
        }

        #region Synchronous API

        /// <summary>
        /// Runs GA optimization synchronously (blocks calling thread).
        /// </summary>
        public void Optimize(
            IList<Chapa> chapas,
            int populationSize,
            int generations,
            float crossoverProb = 0.9f,
            float mutationProb = 0.15f)
        {
            InitializeOptimizer(chapas.Count);
            genericOptimizer.Optimize(chapas, populationSize, generations, crossoverProb, mutationProb);
        }

        #endregion

        #region Asynchronous API

        /// <summary>
        /// Runs GA optimization asynchronously (non-blocking).
        /// </summary>
        public async Task OptimizeAsync(
            IList<Chapa> chapas,
            int populationSize,
            int generations,
            float crossoverProb = 0.9f,
            float mutationProb = 0.15f)
        {
            InitializeOptimizer(chapas.Count);
            await genericOptimizer.OptimizeAsync(chapas, populationSize, generations, crossoverProb, mutationProb);
        }

        /// <summary>
        /// Cancels the running async optimization.
        /// </summary>
        public void Cancel()
        {
            genericOptimizer?.Cancel();
        }

        #endregion

        private void InitializeOptimizer(int dataLength)
        {
            var evaluator = new ChapaSimulationEvaluator(modelConfig);
            
            // For Chapas, we optimize both sequence and inspection bits
            genericOptimizer = new GenericOptimizer<Chapa>(evaluator, dataLength, dataLength);
            
            // Transfer settings
            genericOptimizer.EnableParallelEvaluation = enableParallelEvaluation;
            genericOptimizer.MaxDegreeOfParallelism = maxDegreeOfParallelism;
            genericOptimizer.LogCallback = logCallback;
            
            // Forward events
            genericOptimizer.ProgressChanged += (args) => ProgressChanged?.Invoke(args);
            genericOptimizer.Completed += (args) => Completed?.Invoke(args);
        }
    }
}
