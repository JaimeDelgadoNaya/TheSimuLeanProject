# Parallel Evaluation in GA Optimization

## Overview

The optimization system now supports **parallel fitness evaluation**, allowing multiple simulations to run simultaneously on different CPU cores. This can significantly speed up GA execution.

## Quick Answer to Your Question

> **Question:** Are the controllers parallelizing the optimization? Like using several threads to run several instances for evaluation in parallel?

**Answer:**

- **Before:** ? NO - All evaluations ran sequentially (one at a time)
- **After:** ? YES (OPTIONAL) - You can now enable parallel evaluation in both controllers

## How Parallelization Works

### Sequential Mode (Default)
```
Generation 1:
?? Eval Chromosome 1  (2s)
?? Eval Chromosome 2  (2s)
?? Eval Chromosome 3  (2s)
?? Eval Chromosome 4  (2s)
Total: 8 seconds
```

### Parallel Mode (New!)
```
Generation 1 (4 cores):
?? Eval Chromosome 1  (2s)  ? Thread 1
?? Eval Chromosome 2  (2s)  ? Thread 2
?? Eval Chromosome 3  (2s)  ? Thread 3
?? Eval Chromosome 4  (2s)  ? Thread 4
Total: 2 seconds (4x speedup!)
```

## Architecture

```
??????????????????????????????????????????????????
?  AsyncGAController / ChapasGAController        ?
?  [enableParallelEvaluation = true/false]       ?
??????????????????????????????????????????????????
               ?
               ?
??????????????????????????????????????????????????
?  ChapaOptimizer                                ?
?  EnableParallelEvaluation = true               ?
?  MaxDegreeOfParallelism = 4                    ?
??????????????????????????????????????????????????
               ?
               ?
??????????????????????????????????????????????????
?  GeneticSharp + ParallelTaskExecutor           ?
?  (manages thread pool)                         ?
??????????????????????????????????????????????????
               ?
               ???? Thread 1 ? ChapaSimulationEvaluator
               ???? Thread 2 ? ChapaSimulationEvaluator
               ???? Thread 3 ? ChapaSimulationEvaluator
               ???? Thread 4 ? ChapaSimulationEvaluator
```

## Usage

### In Unity Inspector

#### AsyncGAController / ChapasGAController

**Performance Section:**
- **Enable Parallel Evaluation** ?? Check to enable
- **Max Parallel Threads** 
  - `0` = Use all CPU cores (default)
  - `2` = Use 2 threads
  - `4` = Use 4 threads
  - etc.

### In Code

```csharp
// Create optimizer
var optimizer = new ChapaOptimizer(modelConfig);

// Enable parallel evaluation
optimizer.EnableParallelEvaluation = true;
optimizer.MaxDegreeOfParallelism = 4; // or Environment.ProcessorCount

// Run optimization (will use parallel evaluation)
await optimizer.OptimizeAsync(chapas, popSize, gens);
```

## Performance Comparison

### Example: Population Size 50, Generations 100

| Mode | Evaluations | Time per Eval | Total Time | Speedup |
|------|-------------|---------------|------------|---------|
| **Sequential** | 5,000 | 2s | ~2.7 hours | 1x |
| **Parallel (4 cores)** | 5,000 | 2s | ~40 minutes | **4x** |
| **Parallel (8 cores)** | 5,000 | 2s | ~20 minutes | **8x** |

*Actual speedup depends on:*
- Number of CPU cores
- Simulation complexity
- Memory bandwidth
- Thread overhead

## Important Considerations

### ? When to Use Parallel Evaluation

- **Large population sizes** (50+)
- **Expensive simulations** (>1 second per evaluation)
- **Long optimization runs** (100+ generations)
- **Multi-core CPU available** (4+ cores)
- **Enough RAM** (each thread needs memory for simulation)

### ? When NOT to Use Parallel Evaluation

- **Small populations** (<20) - overhead dominates
- **Fast simulations** (<0.5 seconds) - threading overhead not worth it
- **Limited CPU** (2 cores or less)
- **Limited RAM** (parallel simulations consume more memory)
- **Debugging** - sequential is easier to debug

### Memory Usage

Each parallel thread runs a full simulation, which consumes memory:

```
Memory Usage = Base Memory + (Thread Count × Simulation Memory)
```

**Example:**
- Base memory: 500 MB
- Simulation memory per thread: 100 MB
- 4 threads: 500 + (4 × 100) = 900 MB
- 8 threads: 500 + (8 × 100) = 1300 MB

### Thread Safety

? **Safe:** Each simulation runs independently with its own:
- `SimClock`
- `HeadlessModelFactory`
- `Element` instances
- `SimulationMetrics`

? **NOT Safe (but avoided):**
- Shared static data
- Unity API calls (already handled by dispatcher)
- File I/O during evaluation

## Implementation Details

### ParallelTaskExecutor

Uses `GeneticSharp.Infrastructure.Framework.Threading.ParallelTaskExecutor`:

```csharp
ga.TaskExecutor = new ParallelTaskExecutor
{
    MinThreads = 1,
    MaxThreads = MaxDegreeOfParallelism
};
```

This manages a thread pool that evaluates multiple chromosomes concurrently.

### Evaluation Flow

```csharp
// Population of 50 chromosomes, 4 threads
Generation 1:
  Batch 1: Chromosomes 1-4   (parallel)
  Batch 2: Chromosomes 5-8   (parallel)
  Batch 3: Chromosomes 9-12  (parallel)
  ...
  Batch 13: Chromosomes 49-50 (parallel)
```

### Logging

Parallel mode logs indicate which mode is active:

```
[ChapaOptimizer] Parallel evaluation ENABLED (max 4 threads)
```

or

```
[ChapaOptimizer] Parallel evaluation DISABLED (sequential mode)
```

## Async vs Parallel - What's the Difference?

| Concept | What It Does | Use Case |
|---------|--------------|----------|
| **Async** (`AsyncGAController`) | Prevents Unity UI from freezing | Always beneficial |
| **Parallel** (this feature) | Speeds up fitness evaluation | Optional, for performance |

You can use **both** together:

```
AsyncGAController
  ? (runs GA on background thread)
ChapaOptimizer
  ? (spawns multiple threads for evaluation)
ParallelTaskExecutor
  ?
Multiple simulations running simultaneously
```

## Testing

### Test Sequential vs Parallel

1. **Sequential Test:**
   - Set `enableParallelEvaluation = false`
   - Set `generations = 10`, `populationSize = 20`
   - Run and note time

2. **Parallel Test:**
   - Set `enableParallelEvaluation = true`
   - Set `maxParallelThreads = 4`
   - Same parameters as above
   - Run and compare time

### Expected Results

With 4 cores and 20 chromosomes per generation:

```
Sequential:  20 evals × 2s = 40s per generation
Parallel:    20 evals / 4 = 5 batches × 2s = 10s per generation
Speedup:     40s / 10s = 4x faster
```

## Troubleshooting

### "No speedup observed"

**Possible causes:**
1. CPU already at 100% (other processes running)
2. Simulation too fast (threading overhead dominates)
3. Memory bottleneck (RAM or cache thrashing)
4. Population size too small

**Solutions:**
- Close other applications
- Increase population size
- Monitor Task Manager during optimization
- Try different `maxParallelThreads` values

### "Out of memory exception"

**Cause:** Too many parallel threads consuming RAM

**Solutions:**
- Reduce `maxParallelThreads`
- Reduce population size
- Close other applications
- Add more RAM

### "Slower with parallel mode"

**Cause:** Threading overhead > speedup gain

**Solutions:**
- Use sequential mode instead
- Increase population size (amortize overhead)
- Optimize simulation code

## Best Practices

1. **Start Sequential** - Get baseline performance
2. **Enable Parallel** - Test with 2, 4, 8 threads
3. **Monitor Resources** - Watch CPU and RAM usage
4. **Find Sweet Spot** - Best thread count for your system
5. **Document Results** - Note speedup for future reference

## Code Changes Summary

### New Properties in ChapaOptimizer

```csharp
public bool EnableParallelEvaluation { get; set; } = false;
public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
```

### New Inspector Fields

```csharp
[SerializeField] private bool enableParallelEvaluation = false;
[SerializeField] private int maxParallelThreads = 0;
```

### Configuration in Controllers

```csharp
optimizer.EnableParallelEvaluation = enableParallelEvaluation;
if (maxParallelThreads > 0)
{
    optimizer.MaxDegreeOfParallelism = maxParallelThreads;
}
```

## Conclusion

? **You NOW have parallel evaluation!**  
? **Works in both AsyncGAController and ChapasGAController**  
? **Configurable via Inspector or code**  
? **Can achieve 4-8x speedup on multi-core systems**  

The feature is **optional** and **backwards compatible** - existing code works without changes (defaults to sequential mode).

For best results, enable parallel evaluation when:
- Running long optimizations (100+ generations)
- Using large populations (50+ individuals)
- On multi-core CPUs (4+ cores)
- With sufficient RAM (2GB+ available)

Happy optimizing! ??
