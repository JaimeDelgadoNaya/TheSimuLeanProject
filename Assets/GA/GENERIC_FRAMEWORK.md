# Generic Optimization Framework

## Overview

The optimization system has been refactored into a **generic framework** that can handle:
- **Sequence optimization** (permutation problems)
- **Binary decision optimization** (0/1 choices)
- **Combined optimization** (both sequence + binary)

This allows the framework to be used for ANY optimization problem, not just "Chapas".

## Architecture

### Core Generic Components

```
Assets/GA/Core/
??? ISimulationEvaluator.cs          - Generic simulation interface
??? SequenceBinaryChromosome.cs      - Generic chromosome (replaces ChapaChromosome)
??? SequenceBinaryCrossover.cs       - Generic crossover operator
??? SequenceBinaryMutation.cs        - Generic mutation operator
```

### Optimization Layer

```
Assets/GA/Optimization/
??? GenericSimulationEvaluator.cs    - Generic simulation runner
??? GenericOptimizer.cs              - Generic GA optimizer
??? ChapaSimulationEvaluator.cs      - Chapa-specific wrapper (backward compatibility)
??? ChapaOptimizer.cs                - Chapa-specific wrapper (backward compatibility)
```

### Data Adapters

```
Assets/GA/Adapters/
??? ChapaDataTransformer.cs          - Transforms Chapa data for simulations
```

## Key Concepts

### 1. Generic Chromosome: `SequenceBinaryChromosome`

The generic chromosome supports three modes:

```csharp
// Sequence only (permutation problem)
var chromosome = new SequenceBinaryChromosome(sequenceLength: 10, binaryLength: 0);

// Binary only (0/1 decision problem)
var chromosome = new SequenceBinaryChromosome(sequenceLength: 0, binaryLength: 10);

// Combined (both sequence and binary)
var chromosome = new SequenceBinaryChromosome(sequenceLength: 10, binaryLength: 10);
```

### 2. Data Transformer: `IDataTransformer<TData>`

To use the generic system with your data type, implement `IDataTransformer<TData>`:

```csharp
public interface IDataTransformer<TData>
{
    // Convert your data to simulation format
    Dictionary<string, List<string>> ConvertToDataDict(IList<TData> data);
    
    // Calculate maximum simulation time
    double CalculateMaxSimTime(IList<TData> data);
    
    // Get the label name for binary decisions (e.g., "inspeccionOn")
    // Return null if no binary optimization
    string GetBinaryDecisionLabel();
}
```

### 3. Generic Evaluator: `GenericSimulationEvaluator<TData>`

Runs simulations with any data type:

```csharp
var transformer = new MyDataTransformer();
var evaluator = new GenericSimulationEvaluator<MyData>(modelConfig, transformer);

var metrics = evaluator.RunSimulation(myDataList, sequence, binaryDecisions);
```

### 4. Generic Optimizer: `GenericOptimizer<TData>`

Optimizes any problem:

```csharp
var evaluator = new GenericSimulationEvaluator<MyData>(config, transformer);
var optimizer = new GenericOptimizer<MyData>(
    evaluator, 
    sequenceLength: 20,  // Set to 0 if no sequence optimization
    binaryLength: 20     // Set to 0 if no binary optimization
);

// Run optimization
optimizer.Optimize(myDataList, populationSize: 50, generations: 100);

// Get results
var bestSequence = optimizer.BestSequence;
var bestDecisions = optimizer.BestBinaryDecisions;
```

## Usage Examples

### Example 1: Chapa Optimization (Existing Use Case)

The existing Chapa system now uses the generic framework under the hood:

```csharp
// Create optimizer (uses GenericOptimizer internally)
var optimizer = new ChapaOptimizer(modelConfig);

// Enable parallel evaluation
optimizer.EnableParallelEvaluation = true;
optimizer.MaxDegreeOfParallelism = 4;

// Run optimization
optimizer.Optimize(chapas, populationSize: 50, generations: 100);

// Results
Console.WriteLine($"Best Fitness: {optimizer.BestFitness}");
Console.WriteLine($"Best Order: {string.Join(", ", optimizer.BestOrder)}");
Console.WriteLine($"Inspections: {string.Join(", ", optimizer.BestInspectionBits)}");
```

### Example 2: Generic Job Scheduling Problem

Optimize job scheduling with setup times:

```csharp
// Define your data type
public class Job
{
    public string Name { get; set; }
    public double ProcessingTime { get; set; }
    public double SetupTime { get; set; }
    public double DueDate { get; set; }
}

// Create data transformer
public class JobDataTransformer : IDataTransformer<Job>
{
    public Dictionary<string, List<string>> ConvertToDataDict(IList<Job> jobs)
    {
        return new Dictionary<string, List<string>>
        {
            ["Name"] = jobs.Select(j => j.Name).ToList(),
            ["ProcessingTime"] = jobs.Select(j => j.ProcessingTime.ToString()).ToList(),
            ["SetupTime"] = jobs.Select(j => j.SetupTime.ToString()).ToList(),
            ["DueDate"] = jobs.Select(j => j.DueDate.ToString()).ToList()
        };
    }

    public double CalculateMaxSimTime(IList<Job> jobs)
    {
        return jobs.Sum(j => j.ProcessingTime + j.SetupTime) * 1.5;
    }

    public string GetBinaryDecisionLabel()
    {
        return null; // No binary decisions for this problem
    }
}

// Use the generic optimizer
var jobs = LoadJobs();
var transformer = new JobDataTransformer();
var evaluator = new GenericSimulationEvaluator<Job>(modelConfig, transformer);

var optimizer = new GenericOptimizer<Job>(
    evaluator,
    sequenceLength: jobs.Count,  // Optimize job order
    binaryLength: 0              // No binary decisions
);

optimizer.Optimize(jobs, 50, 100);
```

### Example 3: Machine Assignment Problem (Binary Only)

Optimize which machines to use without caring about order:

```csharp
public class Machine
{
    public string Name { get; set; }
    public double Cost { get; set; }
    public double Capacity { get; set; }
}

public class MachineDataTransformer : IDataTransformer<Machine>
{
    public Dictionary<string, List<string>> ConvertToDataDict(IList<Machine> machines)
    {
        return new Dictionary<string, List<string>>
        {
            ["Name"] = machines.Select(m => m.Name).ToList(),
            ["Cost"] = machines.Select(m => m.Cost.ToString()).ToList(),
            ["Capacity"] = machines.Select(m => m.Capacity.ToString()).ToList()
        };
    }

    public double CalculateMaxSimTime(IList<Machine> machines)
    {
        return 1000.0; // Fixed simulation time
    }

    public string GetBinaryDecisionLabel()
    {
        return "machineEnabled"; // Binary decision: enable machine or not
    }
}

// Optimize which machines to enable
var optimizer = new GenericOptimizer<Machine>(
    evaluator,
    sequenceLength: 0,              // No sequence optimization
    binaryLength: machines.Count    // Binary decision for each machine
);

optimizer.Optimize(machines, 50, 100);
var selectedMachines = optimizer.BestBinaryDecisions;
```

### Example 4: Vehicle Routing with Service Decisions

Optimize route AND decide which customers get premium service:

```csharp
public class Customer
{
    public string Name { get; set; }
    public double Demand { get; set; }
    public double ServiceTime { get; set; }
    public double PremiumServiceBonus { get; set; }
}

var optimizer = new GenericOptimizer<Customer>(
    evaluator,
    sequenceLength: customers.Count,  // Route optimization
    binaryLength: customers.Count     // Premium service decisions
);

optimizer.Optimize(customers, 100, 200);

var route = optimizer.BestSequence;                // Optimal visit order
var premiumService = optimizer.BestBinaryDecisions; // Which get premium service
```

## Migration Guide

### From Old Chapa-Specific Code

**Before:**
```csharp
// Old: Chapa-specific chromosome
var chromosome = new ChapaChromosome(length);
var order = chromosome.GetOrder();
var bits = chromosome.GetInspectionBits();
```

**After:**
```csharp
// New: Generic chromosome
var chromosome = new SequenceBinaryChromosome(sequenceLength, binaryLength);
var sequence = chromosome.GetSequence();
var binary = chromosome.GetBinaryDecisions();
```

**Before:**
```csharp
// Old: Chapa-specific operators
var crossover = new ChapaCrossover();
var mutation = new ChapaMutation();
```

**After:**
```csharp
// New: Generic operators
var crossover = new SequenceBinaryCrossover();
var mutation = new SequenceBinaryMutation();
```

### From Old Evaluator

**Before:**
```csharp
var evaluator = new ChapaSimulationEvaluator(config);
var metrics = evaluator.RunSimulation(chapas, order, inspectionBits);
```

**After (Generic):**
```csharp
var transformer = new ChapaDataTransformer();
var evaluator = new GenericSimulationEvaluator<Chapa>(config, transformer);
var metrics = evaluator.RunSimulation(chapas, sequence, binaryDecisions);
```

**Or (Backward Compatible):**
```csharp
// ChapaSimulationEvaluator now uses the generic version internally
var evaluator = new ChapaSimulationEvaluator(config);
var metrics = evaluator.RunSimulation(chapas, order, inspectionBits);
```

## Benefits of the Generic Framework

? **Reusability** - Use the same GA components for different optimization problems  
? **Type Safety** - Generic types ensure compile-time type checking  
? **Flexibility** - Support sequence-only, binary-only, or combined optimization  
? **Extensibility** - Easy to add new data types via `IDataTransformer<TData>`  
? **Backward Compatibility** - Old Chapa code still works via wrappers  
? **Testability** - Easy to mock with generic interfaces  
? **Maintainability** - Single implementation for all optimization types  

## Performance

The generic framework has **identical performance** to the old Chapa-specific code:
- Same algorithms (Order Crossover, Swap Mutation)
- Same simulation logic
- Same parallel evaluation support
- Zero runtime overhead from generics (C# generics are compiled to specialized types)

## Next Steps

1. **Try it with your own data type** - Implement `IDataTransformer<T>` for your domain
2. **Remove old code** - The old `ChapaChromosome`, `ChapaCrossover`, `ChapaMutation` can be removed
3. **Explore other problems** - Job scheduling, vehicle routing, resource allocation, etc.

## Questions?

For questions or issues with the generic framework:
- Check `Assets/GA/Core/SequenceBinaryChromosome.cs` - Generic chromosome documentation
- Check `Assets/GA/Optimization/GenericOptimizer.cs` - Generic optimizer API
- Check `Assets/GA/Adapters/ChapaDataTransformer.cs` - Example data transformer
- This file - Complete usage guide
