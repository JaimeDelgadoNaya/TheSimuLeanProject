# Step-by-Step Migration Examples

This document provides practical examples of how to migrate from the Chapa-specific code to the generic framework, or how to create new optimization problems from scratch.

## Example 1: Keep Using Chapas (No Changes Needed)

Your existing code continues to work **without any changes**:

```csharp
// This code works exactly as before!
var loader = new ExcelChapaLoader();
var chapas = loader.LoadFromStreamingAssets("Chapas.xlsx");

var config = ExtractModelConfiguration();
var optimizer = new ChapaOptimizer(config);

optimizer.EnableParallelEvaluation = true;
optimizer.LogCallback = Debug.Log;

optimizer.Optimize(chapas, populationSize: 50, generations: 100);

Debug.Log($"Best Fitness: {optimizer.BestFitness}");
Debug.Log($"Best Order: {string.Join(", ", optimizer.BestOrder)}");
Debug.Log($"Inspections: {string.Join(", ", optimizer.BestInspectionBits)}");
```

**No migration needed!** The `ChapaOptimizer` and `ChapaSimulationEvaluator` now use the generic framework internally.

## Example 2: Migrate Chapas to Fully Generic (Optional)

If you want to use the generic framework directly:

### Before (Chapa-specific):
```csharp
var optimizer = new ChapaOptimizer(config);
optimizer.Optimize(chapas, 50, 100);

var order = optimizer.BestOrder;
var inspections = optimizer.BestInspectionBits;
```

### After (Fully generic):
```csharp
using ChapasGA.GA.Adapters;
using ChapasGA.GA.Optimization;

// Create data transformer and evaluator
var transformer = new ChapaDataTransformer();
var evaluator = new GenericSimulationEvaluator<Chapa>(config, transformer);

// Create generic optimizer
var optimizer = new GenericOptimizer<Chapa>(
    evaluator,
    sequenceLength: chapas.Count,  // Optimize order
    binaryLength: chapas.Count     // Optimize inspection decisions
);

// Run optimization (same API)
optimizer.Optimize(chapas, 50, 100);

// Get results (different property names)
var sequence = optimizer.BestSequence;        // was: BestOrder
var binary = optimizer.BestBinaryDecisions;   // was: BestInspectionBits
```

### Key Differences:
- Property name: `BestOrder` ? `BestSequence`
- Property name: `BestInspectionBits` ? `BestBinaryDecisions`
- Must specify sequence and binary lengths explicitly

## Example 3: New Problem - Job Scheduling (Sequence Only)

Let's create a job scheduling optimizer from scratch:

### Step 1: Define Your Data Type

```csharp
using System;

namespace MyApp.Models
{
    [Serializable]
    public class Job
    {
        public string JobId;
        public double ProcessingTime;
        public double SetupTime;
        public double DueDate;
        public int Priority;
    }
}
```

### Step 2: Create Data Transformer

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using ChapasGA.GA.Optimization;
using MyApp.Models;

namespace MyApp.Optimization
{
    public class JobDataTransformer : IDataTransformer<Job>
    {
        public Dictionary<string, List<string>> ConvertToDataDict(IList<Job> jobs)
        {
            return new Dictionary<string, List<string>>
            {
                ["Time"] = jobs.Select((j, i) => (i * 0.1).ToString()).ToList(),
                ["JobId"] = jobs.Select(j => j.JobId).ToList(),
                ["Q"] = jobs.Select(j => "1").ToList(),
                ["ProcessingTime"] = jobs.Select(j => j.ProcessingTime.ToString()).ToList(),
                ["SetupTime"] = jobs.Select(j => j.SetupTime.ToString()).ToList(),
                ["DueDate"] = jobs.Select(j => j.DueDate.ToString()).ToList(),
                ["Priority"] = jobs.Select(j => j.Priority.ToString()).ToList()
            };
        }

        public double CalculateMaxSimTime(IList<Job> jobs)
        {
            double totalTime = jobs.Sum(j => j.ProcessingTime + j.SetupTime);
            double arrivalTime = jobs.Count * 0.1;
            return arrivalTime + totalTime * 2.0; // 100% safety margin
        }

        public string GetBinaryDecisionLabel()
        {
            // No binary decisions for pure scheduling
            return null;
        }
    }
}
```

### Step 3: Create Optimizer

```csharp
using System.Collections.Generic;
using ChapasGA.GA.Optimization;
using MyApp.Models;
using UnityEngine;
using SimuLean.Serialization;

namespace MyApp.Optimization
{
    public class JobScheduleOptimizer : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private string jobsFile = "Jobs.xlsx";
        
        [Header("GA Parameters")]
        [SerializeField] private int populationSize = 50;
        [SerializeField] private int generations = 100;
        
        [Header("Model")]
        [SerializeField] private GameObject modelRoot;

        private List<Job> jobs;
        private GenericOptimizer<Job> optimizer;

        public void LoadJobs()
        {
            // Load your jobs from Excel/CSV/JSON
            jobs = LoadJobsFromFile(jobsFile);
            Debug.Log($"Loaded {jobs.Count} jobs");
        }

        public void RunOptimization()
        {
            if (jobs == null || jobs.Count == 0)
            {
                Debug.LogError("No jobs loaded!");
                return;
            }

            // Extract model configuration
            var config = ExtractModelConfig();

            // Create transformer and evaluator
            var transformer = new JobDataTransformer();
            var evaluator = new GenericSimulationEvaluator<Job>(config, transformer);

            // Create optimizer (sequence only, no binary decisions)
            optimizer = new GenericOptimizer<Job>(
                evaluator,
                sequenceLength: jobs.Count,  // Optimize job order
                binaryLength: 0              // No binary decisions
            );

            // Enable parallel evaluation
            optimizer.EnableParallelEvaluation = true;
            optimizer.LogCallback = Debug.Log;

            // Run optimization
            optimizer.Optimize(jobs, populationSize, generations);

            // Display results
            DisplayResults();
        }

        private void DisplayResults()
        {
            Debug.Log("=== Optimization Results ===");
            Debug.Log($"Best Fitness: {optimizer.BestFitness:F2}");
            Debug.Log($"Total Delays: {optimizer.TotalDelays}");
            
            var optimalSequence = optimizer.BestSequence;
            Debug.Log($"Optimal Job Sequence: {string.Join(" ? ", optimalSequence)}");
            
            // Show job details in optimal order
            for (int i = 0; i < optimalSequence.Count; i++)
            {
                var jobIndex = optimalSequence[i];
                var job = jobs[jobIndex];
                Debug.Log($"  Position {i + 1}: {job.JobId} (Process: {job.ProcessingTime}s, Due: {job.DueDate}s)");
            }
        }

        private SimulationConfig ExtractModelConfig()
        {
            // Use UnityModelExtractor or load from file
            var extractor = new UnityEngine.GameObject("TempExtractor")
                .AddComponent<SimuLean.Unity.UnityModelExtractor>();
            extractor.modelRoot = modelRoot;
            var config = extractor.ExtractConfiguration();
            Destroy(extractor.gameObject);
            return config;
        }

        private List<Job> LoadJobsFromFile(string filename)
        {
            // Implement your file loading logic here
            // For example, use ExcelDataReader or JSON
            return new List<Job>();
        }
    }
}
```

## Example 4: Machine Selection (Binary Only)

Optimize which machines to activate:

### Step 1: Define Data Type

```csharp
[Serializable]
public class Machine
{
    public string MachineId;
    public double OperatingCost;      // Cost per hour
    public double ProductionRate;     // Items per hour
    public double MaintenanceCost;    // Daily maintenance
    public double SetupCost;          // One-time setup cost
}
```

### Step 2: Create Transformer

```csharp
public class MachineDataTransformer : IDataTransformer<Machine>
{
    public Dictionary<string, List<string>> ConvertToDataDict(IList<Machine> machines)
    {
        return new Dictionary<string, List<string>>
        {
            ["MachineId"] = machines.Select(m => m.MachineId).ToList(),
            ["OperatingCost"] = machines.Select(m => m.OperatingCost.ToString()).ToList(),
            ["ProductionRate"] = machines.Select(m => m.ProductionRate.ToString()).ToList(),
            ["MaintenanceCost"] = machines.Select(m => m.MaintenanceCost.ToString()).ToList(),
            ["SetupCost"] = machines.Select(m => m.SetupCost.ToString()).ToList()
        };
    }

    public double CalculateMaxSimTime(IList<Machine> machines)
    {
        return 480.0; // 8-hour shift
    }

    public string GetBinaryDecisionLabel()
    {
        return "machineEnabled"; // 1 = enabled, 0 = disabled
    }
}
```

### Step 3: Run Optimization

```csharp
var machines = LoadMachines();

var transformer = new MachineDataTransformer();
var evaluator = new GenericSimulationEvaluator<Machine>(config, transformer);

// Binary only - no sequence optimization
var optimizer = new GenericOptimizer<Machine>(
    evaluator,
    sequenceLength: 0,              // No sequence optimization
    binaryLength: machines.Count    // Decide which machines to enable
);

optimizer.Optimize(machines, populationSize: 100, generations: 200);

// Get results
var machineDecisions = optimizer.BestBinaryDecisions;

Debug.Log("=== Optimal Machine Configuration ===");
for (int i = 0; i < machineDecisions.Length; i++)
{
    if (machineDecisions[i] == 1)
    {
        Debug.Log($"? Enable: {machines[i].MachineId}");
    }
    else
    {
        Debug.Log($"? Disable: {machines[i].MachineId}");
    }
}

Debug.Log($"Total Cost Savings: {optimizer.BestFitness:F2}");
```

## Example 5: Delivery Routes with Service Levels (Combined)

Optimize delivery routes AND choose service level for each customer:

### Step 1: Define Data Type

```csharp
[Serializable]
public class Delivery
{
    public string CustomerId;
    public double Latitude;
    public double Longitude;
    public double PackageWeight;
    public double StandardServiceCost;
    public double ExpressServiceCost;
    public double CustomerPriority;
}
```

### Step 2: Create Transformer

```csharp
public class DeliveryDataTransformer : IDataTransformer<Delivery>
{
    public Dictionary<string, List<string>> ConvertToDataDict(IList<Delivery> deliveries)
    {
        return new Dictionary<string, List<string>>
        {
            ["CustomerId"] = deliveries.Select(d => d.CustomerId).ToList(),
            ["Latitude"] = deliveries.Select(d => d.Latitude.ToString()).ToList(),
            ["Longitude"] = deliveries.Select(d => d.Longitude.ToString()).ToList(),
            ["Weight"] = deliveries.Select(d => d.PackageWeight.ToString()).ToList(),
            ["StandardCost"] = deliveries.Select(d => d.StandardServiceCost.ToString()).ToList(),
            ["ExpressCost"] = deliveries.Select(d => d.ExpressServiceCost.ToString()).ToList(),
            ["Priority"] = deliveries.Select(d => d.CustomerPriority.ToString()).ToList()
        };
    }

    public double CalculateMaxSimTime(IList<Delivery> deliveries)
    {
        return deliveries.Count * 15.0; // 15 minutes per delivery
    }

    public string GetBinaryDecisionLabel()
    {
        return "expressService"; // 1 = express, 0 = standard
    }
}
```

### Step 3: Run Combined Optimization

```csharp
var deliveries = LoadDeliveries();

var transformer = new DeliveryDataTransformer();
var evaluator = new GenericSimulationEvaluator<Delivery>(config, transformer);

// Combined optimization: route AND service level
var optimizer = new GenericOptimizer<Delivery>(
    evaluator,
    sequenceLength: deliveries.Count,  // Optimize delivery order
    binaryLength: deliveries.Count     // Choose service level for each
);

// Use more generations for complex problems
optimizer.EnableParallelEvaluation = true;
optimizer.Optimize(deliveries, populationSize: 100, generations: 500);

// Get results
var optimalRoute = optimizer.BestSequence;
var serviceDecisions = optimizer.BestBinaryDecisions;

Debug.Log("=== Optimal Delivery Plan ===");
Debug.Log($"Total Profit: {optimizer.BestFitness:F2}");
Debug.Log($"Total Delays: {optimizer.TotalDelays}");

for (int i = 0; i < optimalRoute.Count; i++)
{
    var deliveryIndex = optimalRoute[i];
    var delivery = deliveries[deliveryIndex];
    var serviceType = serviceDecisions[deliveryIndex] == 1 ? "Express" : "Standard";
    
    Debug.Log($"Stop {i + 1}: {delivery.CustomerId} ? {serviceType} Service");
}
```

## Common Patterns

### Pattern 1: Load ? Extract ? Optimize ? Export

```csharp
public class OptimizationPipeline
{
    public void RunCompletePipeline()
    {
        // 1. Load data
        var data = LoadDataFromFile("data.xlsx");
        
        // 2. Extract simulation model
        var config = ExtractModelConfiguration();
        
        // 3. Create transformer and evaluator
        var transformer = new MyDataTransformer();
        var evaluator = new GenericSimulationEvaluator<MyData>(config, transformer);
        
        // 4. Create and configure optimizer
        var optimizer = new GenericOptimizer<MyData>(evaluator, data.Count, data.Count);
        optimizer.EnableParallelEvaluation = true;
        optimizer.ProgressChanged += OnProgressChanged;
        
        // 5. Run optimization
        optimizer.Optimize(data, 50, 100);
        
        // 6. Export results
        ExportResults(optimizer.BestSequence, optimizer.BestBinaryDecisions);
    }

    private void OnProgressChanged(OptimizationProgressEventArgs e)
    {
        Debug.Log($"Gen {e.CurrentGeneration}: Fitness = {e.BestFitness:F2}");
    }
}
```

### Pattern 2: Async Optimization with UI Updates

```csharp
public class AsyncOptimizationController : MonoBehaviour
{
    private GenericOptimizer<MyData> optimizer;
    
    public async void StartOptimization()
    {
        var data = LoadData();
        var evaluator = CreateEvaluator();
        
        optimizer = new GenericOptimizer<MyData>(evaluator, data.Count, 0);
        
        // Subscribe to progress
        optimizer.ProgressChanged += UpdateUI;
        optimizer.Completed += OnCompleted;
        
        // Run async
        await optimizer.OptimizeAsync(data, 50, 100);
    }
    
    public void CancelOptimization()
    {
        optimizer?.Cancel();
    }
    
    private void UpdateUI(OptimizationProgressEventArgs e)
    {
        UnityMainThreadDispatcher.Instance.Enqueue(() =>
        {
            progressBar.value = e.CurrentGeneration / (float)e.TotalGenerations;
            fitnessText.text = $"Fitness: {e.BestFitness:F2}";
        });
    }
    
    private void OnCompleted(OptimizationCompletedEventArgs e)
    {
        UnityMainThreadDispatcher.Instance.Enqueue(() =>
        {
            if (e.Success)
            {
                Debug.Log($"Optimization completed in {e.TotalTime:F1}s");
                DisplayResults();
            }
            else
            {
                Debug.LogError($"Optimization failed: {e.Error}");
            }
        });
    }
}
```

## Tips and Best Practices

### 1. Choose the Right Optimization Type

- **Sequence only** (`binaryLength: 0`): Job scheduling, routing, ordering
- **Binary only** (`sequenceLength: 0`): Feature selection, on/off decisions
- **Combined**: Route + service level, schedule + quality checks

### 2. Tune GA Parameters

```csharp
// For small problems (< 20 items)
optimizer.Optimize(data, populationSize: 30, generations: 50);

// For medium problems (20-100 items)
optimizer.Optimize(data, populationSize: 50, generations: 100);

// For large problems (> 100 items)
optimizer.Optimize(data, populationSize: 100, generations: 200);
optimizer.EnableParallelEvaluation = true;
```

### 3. Monitor Performance

```csharp
optimizer.LogCallback = (msg) => 
{
    if (msg.Contains("Gen "))
        Debug.Log(msg); // Only log generation updates
};
```

### 4. Handle Errors Gracefully

```csharp
optimizer.Completed += (e) =>
{
    if (!e.Success)
    {
        Debug.LogError($"Optimization failed: {e.Error}");
        // Rollback to default solution
        UseDefaultSolution();
    }
};
```

## Troubleshooting

### Problem: "At least one optimization type must be specified"
**Solution:** You must have either `sequenceLength > 0` or `binaryLength > 0`

### Problem: Results are null
**Solution:** Call `Optimize()` before accessing results

### Problem: Simulation times out
**Solution:** Increase `CalculateMaxSimTime()` return value in your transformer

### Problem: Poor optimization results
**Solution:** Increase population size and/or generations

## Next Steps

1. Try Example 1 with your existing Chapa code
2. Create a simple test case with Example 3 (job scheduling)
3. Experiment with different GA parameters
4. Create your own data transformer for your domain
5. Share your results and get feedback!
