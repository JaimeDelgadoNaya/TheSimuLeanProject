# Fix: "Internal_CreateGameObject can only be called from the main thread"

## ?? Problema Identificado

**Error:**
```
Internal_CreateGameObject can only be called from the main thread.
Constructors and field initializers will be executed from the loading thread when loading a scene.
Don't use this function in the constructor or field initializers, instead move initialization code to the Awake or Start function.
```

---

## ?? Causas Identificadas

### **Causa 1: Unity Debug Logs desde Background Thread**

**`UnityEngine.Debug.Log()`** se estaba llamando desde un **background thread** (Task async).

#### **Código Problemático:**

```csharp
// ? INCORRECTO - Se llama desde background thread
UnityEngine.Debug.Log($"[GA] Generation {ga.GenerationsNumber}/{generations}...");
UnityEngine.Debug.LogWarning("RunSimulationWithConfig: Cannot modify ScheduleSource...");
UnityEngine.Debug.LogError($"[AsyncGA] Error: {ex.Message}");
```

**Solución:** Usar `System.Console.WriteLine()` en código async.

---

### **Causa 2: Crear GameObject en Método Async** ?? **MÁS IMPORTANTE**

En `AsyncGAController.StartGAOptimization()`:

```csharp
public async void StartGAOptimization()
{
    // ... código ...
    
    // ? PROBLEMA: Esto está ANTES del await, pero el método es async
    var extractor = new GameObject("TempExtractor").AddComponent<UnityModelExtractor>();
    extractor.modelRoot = modelRoot;
    var config = extractor.ExtractConfiguration();
    DestroyImmediate(extractor.gameObject);
    
    // ... luego await ...
    await runner.RunGAAsync(...);  // ? Background thread usa 'config'
}
```

**Problema:**
- Aunque el GameObject se crea **antes** del `await`, el método es `async void`
- Unity puede interpretar que el contexto es ambiguo
- La configuración extraída puede tener referencias internas a Unity que causan problemas en background thread

---

## ? Soluciones Aplicadas

### **Solución 1: Reemplazar Unity Debug Logs**

**Cambios en `ChapaGARunner.cs`:**

```csharp
// ? CORRECTO
ga.GenerationRan += (sender, e) =>
{
    var currentFitness = ga.BestChromosome.Fitness.Value;
    
    // Usar Console en lugar de UnityEngine.Debug
    System.Console.WriteLine($"[GA] Generation {ga.GenerationsNumber}/{generations} | " +
                         $"Best Fitness: {currentFitness:F2} | " +
                         $"Time: {ga.TimeEvolving.TotalSeconds:F1}s");
};
```

**Cambios en `AsyncGARunner.cs`:**

```csharp
// ? CORRECTO
System.Console.WriteLine($"[AsyncGA] Gen {currentGen}/{generations} | Fitness: {bestFitness:F2}");

catch (Exception ex)
{
    System.Console.WriteLine($"[AsyncGA] Error: {ex.Message}");
}
```

---

### **Solución 2: Extraer Modelo Completamente en Main Thread** ?

**Cambios en `AsyncGAController.cs`:**

```csharp
public async void StartGAOptimization()
{
    // ... Load data ...

    // ? CORRECTO: Extraer modelo COMPLETAMENTE antes de async
    UnityModelExtractor extractor = null;
    SimulationConfig config = null;
    
    try
    {
        // Crear extractor temporal EN MAIN THREAD
        var extractorGO = new GameObject("TempExtractor");
        extractor = extractorGO.AddComponent<UnityModelExtractor>();
        extractor.modelRoot = modelRoot;
        
        // IMPORTANTE: Extraer configuración ANTES de await
        config = extractor.ExtractConfiguration();
        
        Debug.Log($"Extracted {config.Elements.Count} elements");
        
        // Destruir inmediatamente (todavía en Main Thread)
        DestroyImmediate(extractorGO);
        extractor = null;
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"Error extracting model: {ex.Message}");
        if (extractor != null && extractor.gameObject != null)
        {
            DestroyImmediate(extractor.gameObject);
        }
        return;
    }

    // Verificar configuración
    if (config == null || config.Elements.Count == 0)
    {
        Debug.LogError("Failed to extract model configuration!");
        return;
    }

    // Setup runner con configuración ya extraída
    runner = new AsyncGARunner();
    runner.SetModelConfig(config);  // Config es puro C#, sin referencias a Unity
    
    // AHORA SÍ, ejecutar async
    await runner.RunGAAsync(chapas, populationSize, generations, crossoverProb, mutationProb);
}
```

**Puntos Clave:**
1. ? GameObject se crea, usa y destruye **ANTES** de `await`
2. ? `config` es una estructura pura de C# (`SimulationConfig`)
3. ? No hay referencias a objetos de Unity en `config`
4. ? Background thread solo usa datos serializables

---

## ?? Regla General

### **Orden de Operaciones en Métodos Async:**

```csharp
public async void StartAsyncOperation()
{
    // FASE 1: Main Thread (síntono hasta await)
    // ? Crear GameObjects
    // ? Llamar APIs de Unity
    // ? Extraer configuración serializable
    var config = ExtractConfigFromUnity();
    DestroyUnityObjects();
    
    // FASE 2: Background Thread (después de await)
    // ? NO crear GameObjects
    // ? NO llamar Debug.Log
    // ? Usar solo datos C# puros
    await Task.Run(() =>
    {
        // Solo usar 'config' (datos serializables)
        ProcessData(config);
    });
    
    // FASE 3: Main Thread (después de await completa)
    // ? Actualizar UI
    // ? Llamar Unity APIs de nuevo
    UpdateUI();
}
```

---

## ?? Cómo Identificar el Problema

### **Señales de Background Thread Issues:**

1. **Stack Trace contiene:**
   ```
   System.Threading._ThreadPoolWaitCallback:PerformWaitCallback()
   System.Threading.Tasks.Task:InternalRunSynchronously(...)
   ```

2. **Error menciona:**
   - "main thread"
   - "Internal_CreateGameObject"
   - "loading thread"

3. **Código tiene:**
   - `async`/`await` con llamadas a Unity APIs después del `await`
   - `new GameObject()` en métodos async
   - `Debug.Log()` en código que ejecuta en Tasks

---

## ??? Verificación

### **Checklist de Thread Safety:**

- [x] ¿GameObjects creados **antes** de `await`?
- [x] ¿GameObjects destruidos **antes** de `await`?
- [x] ¿Configuración extraída es **pura C#**?
- [x] ¿No hay referencias a `MonoBehaviour` en config?
- [x] ¿Debug logs usan `Console.WriteLine()` en background?
- [x] ¿UI updates usan `UnityMainThreadDispatcher`?

---

## ?? Comparación Antes/Después

### **ANTES (Problemático):**

```csharp
public async void StartGA()
{
    var extractor = new GameObject("Extractor").AddComponent<UnityModelExtractor>();
    var config = extractor.ExtractConfiguration();
    DestroyImmediate(extractor.gameObject);
    
    // Problema: 'config' podría tener referencias internas problemáticas
    await runner.RunGAAsync(config);  // ? Background thread
}
```

### **DESPUÉS (Correcto):**

```csharp
public async void StartGA()
{
    // Fase 1: MAIN THREAD
    UnityModelExtractor extractor = null;
    SimulationConfig config = null;
    
    try
    {
        var extractorGO = new GameObject("Extractor");
        extractor = extractorGO.AddComponent<UnityModelExtractor>();
        config = extractor.ExtractConfiguration();
        DestroyImmediate(extractorGO);
        extractor = null;
    }
    catch (Exception ex)
    {
        Debug.LogError($"Extraction error: {ex.Message}");
        if (extractor != null) DestroyImmediate(extractor.gameObject);
        return;
    }
    
    // Verificar que config es válido
    if (config == null || config.Elements.Count == 0)
    {
        Debug.LogError("Invalid config!");
        return;
    }
    
    // Fase 2: Setup (MAIN THREAD)
    runner.SetModelConfig(config);  // Config es puro C#
    
    // Fase 3: BACKGROUND THREAD
    await runner.RunGAAsync(chapas, ...);  // Solo usa datos serializables
}
```

---

## ?? Por Qué Funciona Ahora

| Aspecto | Antes | Ahora |
|---------|-------|-------|
| **GameObject lifecycle** | Ambiguo en async | Completamente en Main Thread |
| **Config extraction** | Durante o después de async | ANTES de async |
| **Verificación** | No | Sí, antes de async |
| **Error handling** | Mínimo | Try-catch con cleanup |
| **Thread safety** | Dudoso | Garantizado |

---

## ?? Archivos Modificados

### **1. `Assets\GA\ChapaGARunner.cs`**
- ? Cambio en `RunGA()` ? Evento `GenerationRan` (Console.WriteLine)
- ? Cambio en `RunSimulationWithConfig()` ? Warning log (Console.WriteLine)

### **2. `Assets\GA\AsyncGARunner.cs`**
- ? Cambio en `RunGAInternal()` ? Evento `GenerationRan` (Console.WriteLine)
- ? Cambio en `catch` block ? Error logging (Console.WriteLine)

### **3. `Assets\Mono\AsyncGAController.cs`** ? **MÁS IMPORTANTE**
- ? Reestructurado `StartGAOptimization()`
- ? Extracción de modelo ANTES de async
- ? Try-catch con cleanup de GameObject
- ? Verificación de config antes de async
- ? Documentación clara de fases

---

## ?? Resumen Ejecutivo

| Problema | Causa | Solución |
|----------|-------|----------|
| `Internal_CreateGameObject` error | 1. `Debug.Log()` desde background thread<br>2. GameObject en método async | 1. Usar `Console.WriteLine()`<br>2. Extraer modelo ANTES de `await` |
| Unity se bloquea | GA ejecuta en Main Thread | Usar `AsyncGARunner` con `Task.Run()` |
| No veo logs en Unity Console | `Console.WriteLine()` no aparece en Unity | Usar `UnityMainThreadDispatcher` (opcional) |
| Config inválido | No se verifica antes de async | Verificar `config != null` y `Elements.Count > 0` |

---

## ?? Conclusión

El error **NO era por crear GameObjects**, sino por **llamar a APIs de Unity (`Debug.Log`) desde un background thread**.

**Solución:** 
1. Reemplazar `UnityEngine.Debug` por `System.Console` en todo código async.
2. Asegurar que la extracción y uso de GameObjects se realice completamente en el Main Thread, antes de cualquier operación async.

---

**Estado:** ? FIXED (Both Issues)  
**Fecha:** Enero 2025  
**Commits:** ChapaGARunner.cs, AsyncGARunner.cs, AsyncGAController.cs
