# Quick Reference Card - Generic GA Framework

## ?? Quick Setup (3 Steps)

### Step 1: Create Data Transformer
```csharp
public class MyDataTransformer : IDataTransformer<MyData>
{
    public Dictionary<string, List<string>> ConvertToDataDict(IList<MyData> data) { ... }
    public double CalculateMaxSimTime(IList<MyData> data) { ... }
    public string GetBinaryDecisionLabel() { ... }
}
```

### Step 2: Create Evaluator
```csharp
var transformer = new MyDataTransformer();
var evaluator = new GenericSimulationEvaluator<MyData>(config, transformer);
```

### Step 3: Run Optimization
```csharp
var optimizer = new GenericOptimizer<MyData>(
    evaluator,
    sequenceLength: N,  // 0 if no sequence optimization
    binaryLength: M     // 0 if no binary optimization
);

optimizer.Optimize(data, populationSize: 50, generations: 100);
```

---

## ?? Optimization Types

| Type | `sequenceLength` | `binaryLength` | Use Case |
|------|------------------|----------------|----------|
| **Sequence Only** | N | 0 | Job scheduling, routing |
| **Binary Only** | 0 | N | Feature selection, on/off decisions |
| **Combined** | N | N | Route + service level, schedule + inspections |

---

## ?? Common Patterns

### Pattern 1: Sync Optimization
```csharp
optimizer.Optimize(data, 50, 100);
var result = optimizer.BestSequence;
```

### Pattern 2: Async Optimization
```csharp
optimizer.ProgressChanged += OnProgress;
await optimizer.OptimizeAsync(data, 50, 100);
```

### Pattern 3: Parallel Evaluation
```csharp
optimizer.EnableParallelEvaluation = true;
optimizer.MaxDegreeOfParallelism = 4;
```

### Pattern 4: Cancellation
```csharp
optimizer.Cancel();
```

---

## ?? Results Access

```csharp
// Fitness
double fitness = optimizer.BestFitness;

// Sequence (if enabled)
IList<int> sequence = optimizer.BestSequence;

// Binary decisions (if enabled)
int[] binary = optimizer.BestBinaryDecisions;

// Metrics
int inspections = optimizer.TotalInspections;
int delays = optimizer.TotalDelays;
```

---

## ?? Obsolete Classes (Don't Use)

| ? Old | ? New |
|--------|--------|
| `ChapaOptimizer` | `GenericOptimizer<Chapa>` |
| `ChapaSimulationEvaluator` | `GenericSimulationEvaluator<Chapa>` |
| `ChapaChromosome` | `SequenceBinaryChromosome` |
| `ChapaCrossover` | `SequenceBinaryCrossover` |
| `ChapaMutation` | `SequenceBinaryMutation` |

---

## ?? Documentation

| File | Content |
|------|---------|
| `README_GENERIC_FRAMEWORK.md` | Quick start |
| `GENERIC_FRAMEWORK.md` | Complete guide |
| `MIGRATION_EXAMPLES.md` | Examples |
| `COMPLETE_GENERIFICATION_SUMMARY.md` | Full summary |

---

## ?? Chapa Example (Existing Use Case)

```csharp
// Load data
var chapas = LoadChapas();
var config = ExtractModel();

// Create optimizer
var transformer = new ChapaDataTransformer();
var evaluator = new GenericSimulationEvaluator<Chapa>(config, transformer);
var optimizer = new GenericOptimizer<Chapa>(evaluator, chapas.Count, chapas.Count);

// Run
optimizer.Optimize(chapas, 50, 100);

// Results
var order = optimizer.BestSequence;
var inspections = optimizer.BestBinaryDecisions;
```

---

## ?? IDataTransformer Interface

```csharp
public interface IDataTransformer<TData>
{
    // Convert to simulation format
    Dictionary<string, List<string>> ConvertToDataDict(IList<TData> data);
    
    // Calculate max simulation time
    double CalculateMaxSimTime(IList<TData> data);
    
    // Get binary decision label (null if none)
    string GetBinaryDecisionLabel();
}
```

---

## ? Performance Tips

1. **Enable parallel evaluation** for large populations
2. **Tune population size** based on problem size
3. **Monitor progress** with callbacks
4. **Use async** for UI responsiveness

---

## ?? Troubleshooting

| Problem | Solution |
|---------|----------|
| "At least one optimization type required" | Set `sequenceLength` or `binaryLength` > 0 |
| Results are null | Call `Optimize()` first |
| Slow optimization | Enable parallel evaluation |
| Poor results | Increase generations or population size |

---

**Need Help?** Check the documentation files! ??
