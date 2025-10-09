# Generic Optimization Framework - Quick Start

## ?? What Is This?

This is a **generic genetic algorithm framework** for optimization problems involving:
- **Sequence optimization** (e.g., job order, delivery routes)
- **Binary decisions** (e.g., yes/no choices, feature selection)
- **Combined optimization** (e.g., route + service level choices)

It replaces the Chapa-specific code with a reusable, type-safe generic implementation.

## ?? Documentation Index

| Document | Purpose | Read this if... |
|----------|---------|-----------------|
| **[GENERIC_FRAMEWORK.md](GENERIC_FRAMEWORK.md)** | Complete framework guide | You want to understand the architecture |
| **[MIGRATION_EXAMPLES.md](MIGRATION_EXAMPLES.md)** | Step-by-step examples | You want practical code examples |
| **[GENERIFICATION_SUMMARY.md](GENERIFICATION_SUMMARY.md)** | What changed summary | You want to know what was modified |
| **This file** | Quick start guide | You want to get started quickly |

## ?? Quick Start (3 Steps)

### Step 1: Define Your Data Type

```csharp
[Serializable]
public class Job
{
    public string JobId;
    public double ProcessingTime;
    public double DueDate;
}
```

### Step 2: Implement Data Transformer

```csharp
public class JobDataTransformer : IDataTransformer<Job>
{
    public Dictionary<string, List<string>> ConvertToDataDict(IList<Job> jobs)
    {
        return new Dictionary<string, List<string>>
        {
            ["JobId"] = jobs.Select(j => j.JobId).ToList(),
            ["ProcessingTime"] = jobs.Select(j => j.ProcessingTime.ToString()).ToList(),
            ["DueDate"] = jobs.Select(j => j.DueDate.ToString()).ToList()
        };
    }

    public double CalculateMaxSimTime(IList<Job> jobs)
    {
        return jobs.Sum(j => j.ProcessingTime) * 2.0;
    }

    public string GetBinaryDecisionLabel()
    {
        return null; // No binary decisions
    }
}
```

### Step 3: Run Optimization

```csharp
// Load your data
var jobs = LoadJobs();

// Create evaluator
var transformer = new JobDataTransformer();
var evaluator = new GenericSimulationEvaluator<Job>(modelConfig, transformer);

// Create optimizer
var optimizer = new GenericOptimizer<Job>(
    evaluator,
    sequenceLength: jobs.Count,  // Optimize job order
    binaryLength: 0              // No binary decisions
);

// Run optimization
optimizer.Optimize(jobs, populationSize: 50, generations: 100);

// Get results
var bestSequence = optimizer.BestSequence;
Console.WriteLine($"Optimal order: {string.Join(", ", bestSequence)}");
Console.WriteLine($"Fitness: {optimizer.BestFitness}");
```

That's it! ??

## ?? Common Use Cases

### Sequence Only (Order Optimization)
```csharp
var optimizer = new GenericOptimizer<Job>(
    evaluator,
    sequenceLength: jobs.Count,  // Optimize order
    binaryLength: 0              // No binary decisions
);
```

**Examples:** Job scheduling, vehicle routing, task ordering

### Binary Only (Selection/Decision)
```csharp
var optimizer = new GenericOptimizer<Machine>(
    evaluator,
    sequenceLength: 0,              // No sequence optimization
    binaryLength: machines.Count    // Yes/no for each machine
);
```

**Examples:** Feature selection, machine activation, quality sampling

### Combined (Both)
```csharp
var optimizer = new GenericOptimizer<Customer>(
    evaluator,
    sequenceLength: customers.Count,  // Route order
    binaryLength: customers.Count     // Service level per customer
);
```

**Examples:** Route + service level, schedule + inspections, **Chapa problem**

## ?? Backward Compatibility

**Your existing Chapa code works unchanged!**

```csharp
// This still works exactly as before
var optimizer = new ChapaOptimizer(modelConfig);
optimizer.Optimize(chapas, 50, 100);

var order = optimizer.BestOrder;
var inspections = optimizer.BestInspectionBits;
```

The old classes now use the generic framework internally.

## ?? What's Included

### Core Generic Components
- `SequenceBinaryChromosome` - Generic chromosome
- `SequenceBinaryCrossover` - Generic crossover operator
- `SequenceBinaryMutation` - Generic mutation operator
- `GenericSimulationEvaluator<T>` - Generic simulation runner
- `GenericOptimizer<T>` - Generic GA optimizer

### Backward Compatibility Wrappers
- `ChapaSimulationEvaluator` - Wraps generic evaluator
- `ChapaOptimizer` - Wraps generic optimizer

### Example Adapters
- `ChapaDataTransformer` - Example implementation

## ?? Learn More

### Want practical examples?
? Read **[MIGRATION_EXAMPLES.md](MIGRATION_EXAMPLES.md)**

### Want to understand the architecture?
? Read **[GENERIC_FRAMEWORK.md](GENERIC_FRAMEWORK.md)**

### Want to know what changed?
? Read **[GENERIFICATION_SUMMARY.md](GENERIFICATION_SUMMARY.md)**

## ? Key Benefits

? **Reusable** - Use for any optimization problem  
? **Type-safe** - Compile-time type checking  
? **Flexible** - Sequence, binary, or both  
? **Fast** - Supports parallel evaluation  
? **Tested** - Zero breaking changes  
? **Documented** - Complete usage guide  

## ?? Advanced Features

### Parallel Evaluation
```csharp
optimizer.EnableParallelEvaluation = true;
optimizer.MaxDegreeOfParallelism = 4;
```

### Async Execution
```csharp
optimizer.ProgressChanged += (e) => 
{
    Debug.Log($"Gen {e.CurrentGeneration}: {e.BestFitness}");
};

await optimizer.OptimizeAsync(data, 50, 100);
```

### Cancellation
```csharp
optimizer.Cancel(); // Stop async optimization
```

## ?? Performance

- **No runtime overhead** - C# generics compile to specialized types
- **Same algorithms** - Order Crossover, Swap Mutation
- **Thread-safe** - Parallel evaluation support
- **Tested** - Works with existing Chapa code

## ?? FAQ

### Q: Do I need to change my existing Chapa code?
**A:** No! It works unchanged. The old classes are now wrappers around the generic framework.

### Q: Can I optimize problems with only sequence OR only binary?
**A:** Yes! Set the unused length to 0.

### Q: How do I create a new optimization problem?
**A:** See Step 1-3 above, or check [MIGRATION_EXAMPLES.md](MIGRATION_EXAMPLES.md) for detailed examples.

### Q: Is this slower than the old code?
**A:** No. Performance is identical. C# generics have zero runtime overhead.

### Q: Can I use this outside Unity?
**A:** Yes! The core GA components don't depend on Unity. Only the simulation engine does.

## ?? Next Steps

1. ? **Try it** - Run your existing Chapa code (no changes needed)
2. ?? **Learn** - Read [MIGRATION_EXAMPLES.md](MIGRATION_EXAMPLES.md)
3. ?? **Build** - Create your own data transformer
4. ?? **Deploy** - Optimize your domain problems

## ?? Need Help?

- Check the documentation files above
- Look at `ChapaDataTransformer.cs` for a working example
- Review the inline code documentation
- Test with your existing Chapa code first

---

**Happy Optimizing!** ??
