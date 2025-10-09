# Refactored GA Optimization Architecture

## Overview

The optimization system has been refactored for clarity, maintainability, and reusability. The new architecture follows SOLID principles with clear separation of concerns.

## Architecture Diagram

```
???????????????????????????????????????????????????????????????
?                     UNITY UI LAYER                          ?
?  AsyncGAController.cs - Unity MonoBehaviour Controller      ?
???????????????????????????????????????????????????????????????
                   ?
                   ?
???????????????????????????????????????????????????????????????
?                  OPTIMIZATION LAYER                         ?
?  ChapaOptimizer.cs - Unified GA Optimizer                   ?
?  • OptimizeAsync() - Non-blocking execution                 ?
?  • Optimize() - Blocking execution                          ?
?  • Events: ProgressChanged, Completed                       ?
???????????????????????????????????????????????????????????????
               ?
               ???? ChapaSimulationEvaluator.cs
               ?    • RunSimulation()
               ?    • CalculateFitness()
               ?
               ???? SeqOptTools.cs
                    • TransformSequence()
                    • AddLabelsToDict()
```

## File Structure

```
Assets/GA/
??? Core/                                   # Generic interfaces
?   ??? ISimulationEvaluator.cs             ? NEW - Interface for simulation evaluators
?
??? Optimization/                           # Domain-specific optimization
?   ??? ChapaOptimizer.cs                   ? NEW - Unified optimizer (replaces AsyncGARunner + ChapaGARunner)
?   ??? ChapaSimulationEvaluator.cs         ? NEW - Simulation runner (replaces ChapaFitness)
?
??? Utils/                                  # Utilities
?   ??? SeqOptTools.cs                      ? KEEP - Sequence optimization tools
?   ??? SimulationMetrics.cs                ? NEW - Extracted from SimulationResult
?
??? Models/                                 # Data models
?   ??? Chapa.cs                            ? KEEP
?   ??? ChapaChromosome.cs                  ? KEEP
?   ??? ChapaCrossover.cs                   ? KEEP
?   ??? ChapaMutation.cs                    ? KEEP
?
??? [DEPRECATED - Can be removed]
    ??? ChapaGARunner.cs                    ? REPLACED by ChapaOptimizer
    ??? AsyncGARunner.cs                    ? REPLACED by ChapaOptimizer
    ??? ChapaFitness.cs                     ? REPLACED by ChapaSimulationEvaluator
```

## Key Improvements

### 1. **Unified Optimizer API**

**Before:**
```csharp
// Synchronous
var runner = new ChapaGARunner();
runner.SetModelConfig(config);
runner.RunGA(chapas, ...);

// Asynchronous
var asyncRunner = new AsyncGARunner();
asyncRunner.SetModelConfig(config);
await asyncRunner.RunGAAsync(chapas, ...);
```

**After:**
```csharp
var optimizer = new ChapaOptimizer(config);

// Synchronous
optimizer.Optimize(chapas, popSize, gens);

// Asynchronous
await optimizer.OptimizeAsync(chapas, popSize, gens);
```

### 2. **Interface-Based Design**

The `ISimulationEvaluator<TData, TResult>` interface allows:
- Easy testing with mock evaluators
- Swapping simulation engines without changing GA code
- Reusability for other optimization problems

```csharp
public interface ISimulationEvaluator<TData, TResult>
{
    TResult RunSimulation(IList<TData> data, int[] order, int[] decisionBits);
    double CalculateFitness(TResult result);
}
```

### 3. **Separation of Concerns**

| Class | Responsibility | Dependencies |
|-------|----------------|--------------|
| `ChapaOptimizer` | GA execution, events, cancellation | GeneticSharp, ISimulationEvaluator |
| `ChapaSimulationEvaluator` | Run simulations, calculate fitness | SimuLean, SeqOptTools |
| `SimulationMetrics` | Store results, calculate fitness | None (POCO) |
| `AsyncGAController` | Unity UI, threading | ChapaOptimizer, UnityMainThreadDispatcher |

### 4. **Cleaner Event System**

**Before:**
```csharp
public event Action<GAProgressEventArgs> ProgressChanged;
public event Action<GACompletedEventArgs> Completed;
```

**After:**
```csharp
public event Action<OptimizationProgressEventArgs> ProgressChanged;
public event Action<OptimizationCompletedEventArgs> Completed;
```

More descriptive names + consolidated in one place.

### 5. **Configurable Fitness Weights**

```csharp
var metrics = new SimulationMetrics
{
    DelayPenalty = 100.0,      // Configurable!
    InspectionReward = 10.0    // Configurable!
};
```

## Usage Examples

### Basic Synchronous Optimization

```csharp
// Load data
var chapas = loader.LoadFromStreamingAssets("Chapas.xlsx");

// Extract model
var config = ExtractModelConfiguration();

// Create optimizer
var optimizer = new ChapaOptimizer(config);

// Run optimization (blocks thread)
optimizer.Optimize(chapas, populationSize: 50, generations: 100);

// Get results
Debug.Log($"Best Fitness: {optimizer.BestFitness}");
Debug.Log($"Best Order: {string.Join(", ", optimizer.BestOrder)}");
```

### Asynchronous with Progress Updates

```csharp
var optimizer = new ChapaOptimizer(config);

// Subscribe to events
optimizer.ProgressChanged += (e) =>
{
    Debug.Log($"Gen {e.CurrentGeneration}: Fitness={e.BestFitness}");
};

optimizer.Completed += (e) =>
{
    if (e.Success)
        Debug.Log($"Optimization completed in {e.TotalTime}s");
    else
        Debug.LogError($"Optimization failed: {e.Error}");
};

// Run async
await optimizer.OptimizeAsync(chapas, 50, 100);
```

### Custom Fitness Weights

```csharp
// In ChapaSimulationEvaluator constructor or via property
var evaluator = new ChapaSimulationEvaluator(config);
// Configure metrics before returning them
var metrics = evaluator.RunSimulation(chapas, order, bits);
metrics.DelayPenalty = 150.0;     // Penalize delays more
metrics.InspectionReward = 5.0;   // Reward inspections less
```

## Migration Guide

### For Existing Code Using `ChapaGARunner`

**Old Code:**
```csharp
var runner = new ChapaGARunner();
runner.SetModelConfig(config);
runner.RunGA(chapas, popSize, gens, crossProb, mutProb);
var fitness = runner.BestFitness;
```

**New Code:**
```csharp
var optimizer = new ChapaOptimizer(config);
optimizer.Optimize(chapas, popSize, gens, crossProb, mutProb);
var fitness = optimizer.BestFitness;
```

### For Existing Code Using `AsyncGARunner`

**Old Code:**
```csharp
var asyncRunner = new AsyncGARunner();
asyncRunner.SetModelConfig(config);
asyncRunner.ProgressChanged += OnProgress;
await asyncRunner.RunGAAsync(chapas, popSize, gens, crossProb, mutProb);
```

**New Code:**
```csharp
var optimizer = new ChapaOptimizer(config);
optimizer.ProgressChanged += OnProgress;
await optimizer.OptimizeAsync(chapas, popSize, gens, crossProb, mutProb);
```

## Testing

### Unit Testing the Evaluator

```csharp
[Test]
public void TestSimulationEvaluator()
{
    var config = CreateMockConfig();
    var evaluator = new ChapaSimulationEvaluator(config);
    
    var chapas = CreateTestChapas();
    var order = new int[] { 0, 1, 2 };
    var bits = new int[] { 1, 0, 1 };
    
    var metrics = evaluator.RunSimulation(chapas, order, bits);
    
    Assert.Greater(metrics.TotalInspections, 0);
    Assert.AreEqual(2, metrics.TotalInspections); // bits sum
}
```

### Mocking the Evaluator

```csharp
public class MockEvaluator : ISimulationEvaluator<Chapa, SimulationMetrics>
{
    public SimulationMetrics RunSimulation(IList<Chapa> data, int[] order, int[] bits)
    {
        return new SimulationMetrics
        {
            TotalItems = data.Count,
            TotalInspections = bits.Sum(),
            TotalDelays = 0
        };
    }
    
    public double CalculateFitness(SimulationMetrics result)
    {
        return result.CalculateFitness();
    }
}

// Use in tests
var optimizer = new ChapaOptimizer(mockEvaluator);
```

## Benefits of Refactoring

? **Clarity** - Each class has a single, clear responsibility  
? **Maintainability** - Easy to understand and modify  
? **Testability** - Interface-based design enables mocking  
? **Reusability** - `ISimulationEvaluator` can be used for other problems  
? **Extensibility** - Easy to add new fitness metrics or constraints  
? **Reduced Duplication** - Unified API for sync/async execution  
? **Better Naming** - More descriptive class and method names  

## Performance Considerations

The refactoring maintains the same performance characteristics:
- Simulation execution is identical
- Event handling uses non-blocking Task.Run()
- No additional memory allocations in hot paths

## Next Steps

1. ? Remove deprecated files (`ChapaGARunner.cs`, `AsyncGARunner.cs`, `ChapaFitness.cs`)
2. ? Update `ChapasGAController.cs` to use `ChapaOptimizer`
3. Add unit tests for `ChapaSimulationEvaluator`
4. Consider extracting `SimulationMetrics.DelayPenalty/InspectionReward` to constructor parameters
5. Document fitness function in user-facing documentation

## Questions?

For questions or issues with the refactored architecture, check:
- `Assets/GA/Core/ISimulationEvaluator.cs` - Interface documentation
- `Assets/GA/Optimization/ChapaOptimizer.cs` - Main API
- This file - Architecture overview
