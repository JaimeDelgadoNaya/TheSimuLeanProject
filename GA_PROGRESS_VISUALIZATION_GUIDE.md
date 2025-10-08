# Visualización de Progreso del GA en Tiempo Real

## ?? Problema

**Unity se bloquea durante la optimización** porque el GA se ejecuta en el Main Thread de Unity, impidiendo ver progreso intermedio.

---

## ? Soluciones Implementadas

### **Solución 1: Logging Simple en Console** ? **MÁS SIMPLE**

Ya implementado en `ChapaGARunner.cs`:

```csharp
ga.GenerationRan += (sender, e) =>
{
    var currentFitness = ga.BestChromosome.Fitness.Value;
    
    UnityEngine.Debug.Log($"[GA] Generation {ga.GenerationsNumber}/{generations} | " +
                         $"Best Fitness: {currentFitness:F2} | " +
                         $"Time: {ga.TimeEvolving.TotalSeconds:F1}s");
};
```

**Output en Console:**
```
[GA] Generation 1/100 | Best Fitness: -523.45 | Time: 2.3s
[GA] Generation 2/100 | Best Fitness: -498.12 | Time: 4.7s
[GA] Generation 3/100 | Best Fitness: -467.89 | Time: 7.1s
...
```

**Uso:**
1. Click en Console tab en Unity
2. Ejecutar GA normalmente con `RunGA()`
3. Ver logs en tiempo real mientras ejecuta

**Ventajas:**
- ? Ya funciona, no requiere cambios
- ? No requiere UI adicional
- ? Simple y directo

**Desventajas:**
- ? Unity sigue bloqueado (no puedes interactuar)
- ? Solo texto, sin gráficos

---

### **Solución 2: GA Asíncrono con UI en Tiempo Real** ? **RECOMENDADO**

Usa `AsyncGARunner` que ejecuta el GA en **background thread** permitiendo que Unity siga respondiendo.

#### **Archivos Creados:**

1. **`AsyncGARunner.cs`**
   - Ejecuta GA en Task async
   - Eventos `ProgressChanged` y `Completed`
   - No bloquea Unity

2. **`AsyncGAController.cs`**
   - MonoBehaviour que controla la UI
   - Actualiza UI en tiempo real
   - Botones Start/Cancel

3. **`UnityMainThreadDispatcher.cs`**
   - Ejecuta acciones en Main Thread desde background
   - Necesario para actualizar UI desde Task

---

## ?? Setup de UI para GA Asíncrono

### **Paso 1: Crear UI Canvas**

```
Hierarchy:
?? Canvas (UI Canvas)
?  ?? ProgressPanel (Panel)
?     ?? GenerationText (Text): "Generation: 0/100"
?     ?? FitnessText (Text): "Best Fitness: -500.00"
?     ?? InspectionsText (Text): "Inspections: 5"
?     ?? DelaysText (Text): "Delays: 3"
?     ?? TimeText (Text): "Time: 12.5s"
?     ?? ProgressBar (Slider)
?     ?? StartButton (Button): "Start GA"
?     ?? CancelButton (Button): "Cancel"
```

### **Paso 2: Crear GameObject para Controller**

```
1. GameObject ? Create Empty ? "GAController"
2. Add Component ? AsyncGAController
3. En Inspector:
   - Excel File Name: "Llegada_Chapas.xlsx"
   - Model Root: <Arrastra tu modelo>
   - Population Size: 50
   - Generations: 100
   - Crossover Prob: 0.9
   - Mutation Prob: 0.15
4. Asignar referencias de UI:
   - Progress Panel ? ProgressPanel
   - Generation Text ? GenerationText
   - Fitness Text ? FitnessText
   - Inspections Text ? InspectionsText
   - Delays Text ? DelaysText
   - Time Text ? TimeText
   - Progress Bar ? ProgressBar
   - Start Button ? StartButton
   - Cancel Button ? CancelButton
```

### **Paso 3: Ejecutar**

1. Play en Unity
2. Click en "Start GA" button
3. Ver progreso en tiempo real
4. Unity NO se bloquea, puedes rotar cámara, etc.
5. Click "Cancel" para detener

---

## ?? Output en Tiempo Real

### **Console Logs:**
```
[AsyncGAController] Starting GA optimization...
[AsyncGAController] Loaded 20 chapas
[AsyncGAController] Extracted 4 elements

[AsyncGA] Gen 1/100 | Fitness: -523.45
[AsyncGA] Gen 2/100 | Fitness: -498.12
[AsyncGA] Gen 3/100 | Fitness: -467.89
...
[AsyncGA] Gen 100/100 | Fitness: -234.56

[AsyncGAController] GA Completed!
  Best Fitness: -234.56
  Inspections: 5
  Delays: 2
  Time: 45.3s
```

### **UI Updates:**
```
Generation: 45/100
Best Fitness: -345.67
Inspections: 7
Delays: 3
Time: 23.5s
[Progress Bar: 45%]
```

---

## ?? Configuración Avanzada

### **Habilitar Gráfico de Fitness (Opcional)**

Si quieres ver un **gráfico en tiempo real**:

1. Crear GameObject con `LineRenderer`:
   ```
   Hierarchy:
   ?? FitnessChart (LineRenderer)
      - Material: Default-Line
      - Width: 0.1
      - Color Gradient: Green ? Red
   ```

2. En `AsyncGAController` Inspector:
   - Asignar `Fitness Chart` ? FitnessChart
   - Chart Width: 5
   - Chart Height: 3

3. El gráfico se actualiza automáticamente mostrando evolución del fitness

---

## ?? Comparación de Soluciones

| Feature | Logging Simple | GA Asíncrono |
|---------|----------------|--------------|
| **Bloquea Unity** | ? Sí | ? No |
| **UI en Tiempo Real** | ? No | ? Sí |
| **Gráficos** | ? No | ? Sí (opcional) |
| **Cancelar GA** | ? No | ? Sí |
| **Setup** | ? Ninguno | ?? Requiere UI |
| **Performance** | ? Igual | ? Igual |
| **Complejidad** | ? Simple | ?? Moderada |

---

## ?? Uso Rápido

### **Para Logging Simple:**
```csharp
// Ya funciona automáticamente
controller.RunGA();

// Ver logs en Console
```

### **Para GA Asíncrono:**
```csharp
// Setup (una vez)
var asyncController = gameObject.AddComponent<AsyncGAController>();
asyncController.modelRoot = modelRootGameObject;
// Asignar UI references...

// Ejecutar
asyncController.StartGAOptimization();

// Unity sigue respondiendo!
// Ver progreso en UI en tiempo real
```

---

## ?? Eventos Disponibles en AsyncGARunner

```csharp
var runner = new AsyncGARunner();

// Evento de progreso (cada generación)
runner.ProgressChanged += (e) =>
{
    Debug.Log($"Gen {e.CurrentGeneration}: Fitness={e.BestFitness}");
    // Actualizar UI custom
};

// Evento de completado
runner.Completed += (e) =>
{
    if (e.Success)
    {
        Debug.Log($"Success! Fitness={e.BestFitness}");
    }
    else
    {
        Debug.LogError($"Failed: {e.Error}");
    }
};

await runner.RunGAAsync(chapas, 50, 100, 0.9f, 0.15f);
```

---

## ?? Recomendaciones

### **Para Desarrollo/Testing:**
- Usa **Logging Simple**
- No requiere setup adicional
- Suficiente para ver que el GA está progresando

### **Para Demos/Producción:**
- Usa **GA Asíncrono con UI**
- Experiencia de usuario profesional
- Permite cancelar optimización
- Muestra progreso visual

### **Para Debugging:**
- Usa **Logging Simple** con `logToConsole = true`
- Más información detallada en console
- No distrae con UI

---

## ?? Troubleshooting

### **"Unity se congela aún con AsyncGARunner"**

**Causa:** Las evaluaciones de fitness (simulaciones) son pesadas

**Solución:**
```csharp
// En ChapaFitness, reducir tiempo de simulación
private double CalculateMaxSimTime(List<Chapa> chapas)
{
    // Reducir margen de seguridad
    return arrivalTime + totalProcessTime * 1.2; // En lugar de 1.5
}
```

---

### **"No veo logs en Console"**

**Causa:** Console filtrada o parada

**Solución:**
1. Click en Console tab
2. Desactivar "Collapse"
3. Activar "Clear on Play" (OFF)
4. Scroll hasta abajo

---

### **"AsyncGAController no actualiza UI"**

**Causa:** Referencias de UI no asignadas

**Solución:**
1. Selecciona GameObject con AsyncGAController
2. Verifica que TODOS los campos de UI estén asignados
3. O asigna solo los que necesites (null-safe)

---

## ?? Performance Tips

### **Para GA más rápido:**
```csharp
// Reducir población y generaciones para testing
populationSize = 20;  // En lugar de 50
generations = 50;     // En lugar de 100

// Para producción, aumentar de nuevo
populationSize = 100;
generations = 500;
```

### **Para simulaciones más rápidas:**
```csharp
// En HeadlessModelFactory
var factory = new HeadlessModelFactory(clock, enableLogging: false);
                                                    // ? false = más rápido
```

---

## ? Checklist de Implementación

- [x] Logging simple implementado en `ChapaGARunner`
- [x] `AsyncGARunner` creado para ejecución async
- [x] `AsyncGAController` para control de UI
- [x] `UnityMainThreadDispatcher` para thread safety
- [ ] Crear UI Canvas con elementos necesarios
- [ ] Asignar referencias de UI en Inspector
- [ ] Probar con "Test: Run Async GA"
- [ ] (Opcional) Agregar LineRenderer para gráfico

---

**Estado:** ? Implementado  
**Fecha:** Enero 2025  
**Versión:** 1.0 - Visualización de Progreso en Tiempo Real
