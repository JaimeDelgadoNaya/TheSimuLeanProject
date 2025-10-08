# Guía de Testing del GA con Modelo Extraído de Unity

## ? Cambios Implementados

Se ha **simplificado completamente** el sistema para que **SOLO use el modelo extraído de Unity**:

- ? **Eliminado**: Modelo programático (`RunHeadlessSimulation()`)
- ? **Eliminado**: Constructor de `ChapaFitness` sin simulación
- ? **Requerido**: Modelo extraído de Unity (`SetModelConfig()`)
- ? **Agregado**: 4 Context Menus para testing completo

---

## ?? Flujo Obligatorio

```
1. Asignar modelRoot en Inspector
2. LoadExcel()
3. ExtractModel()  ? OBLIGATORIO
4. RunGA()
```

Si no se llama `ExtractModel()`, el GA lanzará error:
```
InvalidOperationException: Model configuration not set. Call SetModelConfig() before RunGA().
```

---

## ?? Tests Disponibles

### **Test 1: Load Excel**
```
Context Menu: "Test: Load Excel"
```
**Qué hace:**
- Carga chapas desde `StreamingAssets/Llegada_Chapas.xlsx`
- Muestra cuántas chapas se cargaron

**Output esperado:**
```
? Loaded 20 chapas from Excel
```

---

### **Test 2: Extract Model from Unity**
```
Context Menu: "Test: Extract Model from Unity"
```
**Qué hace:**
- Extrae la configuración del modelo desde la escena de Unity
- Lista todos los elementos y conexiones encontrados

**Output esperado:**
```
? Model extracted successfully:
   - Elements: 4
   - Connections: 3
     [UnityScheduleSource] ChapasSource
     [UnityQueue] Buffer
     [UnityCombiner] Soldadura
     [UnitySink] OutputSink
```

**?? Requisitos:**
- El campo `modelRoot` debe estar asignado en el Inspector
- El modelo debe tener elementos `SElement` en la escena

---

### **Test 3: Single Simulation Run**
```
Context Menu: "Test: Single Simulation Run"
```
**Qué hace:**
- Carga chapas (si no están cargadas)
- Extrae modelo (si no está extraído)
- Ejecuta **UNA** simulación con orden original
- Muestra métricas de desempeño

**Output esperado:**
```
========== SINGLE SIMULATION TEST ==========

[Test] Running simulation with original order...
? Simulation completed:
   - Items Processed: 20
   - Inspections: 0
   - Delays: 3
   - Simulation Time: 145.23s
   - Fitness: -445.23

========== TEST COMPLETED ==========
```

**Propósito:**
- Verificar que el modelo funciona correctamente
- Obtener baseline de fitness sin optimización
- Debuggear problemas antes de correr GA completo

---

### **Test 4: Full GA Pipeline** ? **RECOMENDADO**
```
Context Menu: "Test: Full GA Pipeline"
```
**Qué hace:**
1. Carga chapas desde Excel
2. Extrae modelo de Unity
3. Ejecuta GA completo (con parámetros reducidos para testing)
4. Exporta resultados a CSV

**Output esperado:**
```
========== FULL GA PIPELINE TEST ==========

[Step 1/4] Loading Excel...
? Loaded 20 chapas

[Step 2/4] Extracting Model from Unity...
? Extracted 4 elements, 3 connections

[Step 3/4] Running GA...
[ChapasGAController] Starting GA with 20 chapas, 10 population, 5 generations
[ChapasGAController] GA Completed: BestFitness=-234.56, Inspections=5, Delays=2
? GA Completed:
   - Best Fitness: -234.56
   - Total Inspections: 5
   - Total Delays: 2
   - Best Order: 3, 1, 4, 0, 2, ...

[Step 4/4] Exporting CSV...
? Exported to: C:\Users\...\resultado_optimizacion.csv

========== TEST COMPLETED SUCCESSFULLY ==========
```

**Propósito:**
- Test end-to-end completo
- Verificar toda la pipeline funciona
- Genera archivo CSV con resultados

---

## ?? Setup en Inspector

### **1. ChapasGAController GameObject:**

```
ChapasGAController (Component)
?? [Header] Data Source
?  ?? Excel File Name: "Llegada_Chapas.xlsx"
?
?? [Header] GA Parameters
?  ?? Population Size: 50
?  ?? Generations: 10
?  ?? Crossover Prob: 0.9
?  ?? Mutation Prob: 0.15
?  ?? Dry Run: ? (unchecked)
?  ?? Log To Console: ? (unchecked)
?
?? [Header] Model Extraction
   ?? Model Root: [Assign GameObject] ? ?? REQUERIDO
```

### **2. Modelo de Simulación:**

Asegúrate de que tu escena tenga:

```
SimulationModel (GameObject) ? Asignar a modelRoot
?? ChapasSource (UnityScheduleSource)
?  ?? fileName: "Llegada_Chapas.xlsx"
?  ?? nextElement ? Buffer
?
?? Buffer (UnityQueue)
?  ?? capacity: 100
?  ?? nextElement ? Soldadura
?
?? Soldadura (UnityCombiner)
?  ?? requirements: [1, 2]
?  ?? myInputs[0] ? RefuerzosQueue (si aplica)
?  ?? nextElement ? OutputSink
?
?? OutputSink (UnitySink)
```

---

## ?? Checklist de Testing

### **Paso 1: Configuración Inicial**
- [ ] Crear GameObject vacío llamado "GAController"
- [ ] Agregar componente `ChapasGAController`
- [ ] Asignar `modelRoot` en Inspector
- [ ] Verificar que `Llegada_Chapas.xlsx` existe en `StreamingAssets`

### **Paso 2: Tests Básicos**
- [ ] Context Menu ? "Test: Load Excel" ? Verificar chapas cargadas
- [ ] Context Menu ? "Test: Extract Model from Unity" ? Verificar elementos extraídos
- [ ] Context Menu ? "Test: Single Simulation Run" ? Verificar simulación funciona

### **Paso 3: Test Completo**
- [ ] Context Menu ? "Test: Full GA Pipeline"
- [ ] Verificar que no hay errores en Console
- [ ] Verificar que se generó archivo CSV
- [ ] Revisar fitness mejoró vs baseline

### **Paso 4: GA Real (Parámetros Completos)**
- [ ] En Inspector: Population Size = 50, Generations = 100
- [ ] Llamar `RunGA()` desde UI o código
- [ ] Esperar optimización completa
- [ ] Exportar resultados con `ExportCSV()`

---

## ?? Troubleshooting

### **Error: "Model configuration not set"**
```
? InvalidOperationException: Model configuration not set.
```
**Solución:**
- Asegúrate de llamar `ExtractModel()` antes de `RunGA()`
- O usa Context Menu "Test: Full GA Pipeline" que lo hace automáticamente

---

### **Error: "modelRoot is not assigned"**
```
? modelRoot is not assigned in Inspector!
```
**Solución:**
1. Selecciona el GameObject con `ChapasGAController`
2. En Inspector, arrastra el GameObject raíz del modelo a `Model Root`
3. Si no tienes un modelo, créalo con los elementos `SElement`

---

### **Warning: "Cannot modify ScheduleSource data"**
```
?? RunSimulationWithConfig: Cannot modify ScheduleSource data after creation.
```
**Explicación:**
- El `ScheduleSource` se crea con datos del Excel original
- No se puede modificar el orden después de creado
- **Esto es esperado** - es una limitación conocida

**Workaround temporal:**
- El GA evalúa fitness con el orden del Excel original
- Para orden real del GA, necesitarías recrear el source cada vez
- Esto se mejorará en futuras versiones

---

### **Simulación muy lenta**
```
?? GA tarda varios minutos...
```
**Soluciones:**
1. Reducir parámetros temporalmente:
   - Population Size: 10-20
   - Generations: 5-10

2. Usar Dry Run mode:
   - Activar `Dry Run` en Inspector
   - Evalúa solo 1 cromosoma

3. Habilitar logging para ver progreso:
   - Activar `Log To Console` en Inspector

---

## ?? Interpretación de Resultados

### **Fitness Score:**
```
Fitness = -(Delays * 100) - (Inspections * 10) - (Time * 1)
```

**Ejemplo:**
```
Delays: 3
Inspections: 5
Time: 145.23s

Fitness = -(3 * 100) - (5 * 10) - (145.23 * 1)
        = -300 - 50 - 145.23
        = -495.23
```

**Objetivo:** Maximizar fitness (acercarlo a 0)
- Fitness más alto = Mejor solución
- Fitness = 0 ? Sin delays, sin inspections, tiempo mínimo

---

## ?? Ejemplo de Uso Completo

```csharp
using UnityEngine;
using ChapasGA.Mono;

public class GATestRunner : MonoBehaviour
{
    [SerializeField] private ChapasGAController gaController;
    
    void Start()
    {
        RunCompleteTest();
    }
    
    void RunCompleteTest()
    {
        Debug.Log("=== Starting Complete GA Test ===");
        
        // 1. Load data
        gaController.LoadExcel();
        
        // 2. Extract model
        gaController.ExtractModel();
        
        // 3. Test single simulation
        gaController.TestSingleSimulation();
        
        // 4. Run GA
        gaController.RunGA();
        
        // 5. Export results
        gaController.ExportCSV();
        
        Debug.Log($"=== Test Completed ===");
        Debug.Log($"Best Fitness: {gaController.BestFitness}");
        Debug.Log($"CSV Path: {gaController.CsvPath}");
    }
}
```

---

## ? Confirmación de Funcionamiento

Si todo funciona correctamente, deberías ver:

1. **Console sin errores**
2. **Mensaje de GA completado** con fitness, inspections y delays
3. **Archivo CSV generado** en carpeta del proyecto
4. **Fitness mejorado** comparado con simulación sin optimización

---

## ?? Próximos Pasos

Después de confirmar que funciona:

1. **Ajustar parámetros del GA**:
   - Population Size: 100-200
   - Generations: 200-500

2. **Experimentar con función de fitness**:
   - Ajustar pesos de penalties en `SimulationResult.CalculateFitness()`

3. **Validar en Unity**:
   - Reproducir mejor solución en simulación visual de Unity
   - Comparar resultados headless vs Unity

4. **Optimizar performance**:
   - Paralelizar evaluaciones de fitness
   - Cachear resultados si es posible

---

**Estado:** ? Listo para Testing  
**Fecha:** Enero 2025  
**Versión:** 3.0 - Solo Modelo Extraído de Unity
