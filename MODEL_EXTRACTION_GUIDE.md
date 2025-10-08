# Extracción y Uso de Modelos de Unity en Modo Headless

## ?? Objetivo

Permitir que el modelo de simulación configurado en el Inspector de Unity se extraiga y se ejecute en modo headless para optimización con GA, sin necesidad de reconstruir el modelo programáticamente.

---

## ?? Componentes Implementados

### **1. `SimulationConfig` - Configuración Serializable**
**Ubicación:** `Assets\SimuLean.Net\Serialization\SimulationConfig.cs`

Estructura que contiene la configuración completa del modelo:
- `List<ElementConfig>` - Lista de elementos (Source, Queue, Server, Sink, etc.)
- `List<ConnectionConfig>` - Lista de conexiones entre elementos
- `double MaxSimulationTime` - Tiempo máximo de simulación

```csharp
public class SimulationConfig
{
    public List<ElementConfig> Elements { get; set; }
    public List<ConnectionConfig> Connections { get; set; }
    public double MaxSimulationTime { get; set; }
}
```

### **2. `UnityModelExtractor` - Extractor de Configuración**
**Ubicación:** `Assets\SimuLean.Net\Serialization\UnityModelExtractor.cs`

Recorre la escena de Unity y extrae la configuración de todos los elementos `SElement`:

**Parámetros en Inspector:**
- `modelRoot` - GameObject raíz del modelo (opcional, busca en toda la escena si es null)
- `configFileName` - Nombre del archivo JSON para exportar

**Métodos:**
- `ExtractConfiguration()` - Extrae configuración desde Unity
- `ExportToJson()` - Exporta configuración a JSON
- `ImportFromJson(string json)` - Importa configuración desde JSON

### **3. `HeadlessModelFactory` - Constructor de Modelos Headless**
**Ubicación:** `Assets\SimuLean.Net\Serialization\HeadlessModelFactory.cs`

Construye un modelo SimuLean completo desde una configuración, sin dependencias de Unity.

**Métodos:**
- `BuildModel(SimulationConfig config)` - Construye modelo completo
- `GetElement(string id)` - Obtiene un elemento por ID
- `GetAllElements()` - Obtiene todos los elementos creados

---

## ?? Cómo Usar

### **Opción 1: Desde Inspector (Context Menus)**

1. **Seleccionar GameObject con `ChapasGAController`**
2. **Click derecho en el componente**
3. **Opciones disponibles:**
   - `Test: Extract Model from Unity` - Extrae configuración del modelo
   - `Test: Extract and Run with Unity Model` - Extrae y ejecuta simulación
   - `Test: Run Headless Simulation` - Ejecuta con modelo programático o extraído

### **Opción 2: Por Código**

```csharp
using SimuLean.Unity;
using SimuLean.Serialization;
using ChapasGA.GA;

// 1. Extraer configuración del modelo de Unity
var extractor = gameObject.AddComponent<UnityModelExtractor>();
extractor.modelRoot = modelRootGameObject; // Opcional
SimulationConfig config = extractor.ExtractConfiguration();

// 2. Configurar el runner con el modelo extraído
var runner = new ChapaGARunner();
runner.SetModelConfig(config);

// 3. Ejecutar simulación con el modelo de Unity
var result = runner.RunSimulationWithConfig(chapas, order, inspectionBits);
Debug.Log(result.ToString());
```

---

## ?? Workflow Completo

### **Paso 1: Diseñar Modelo en Unity**

1. Arrastra elementos `SElement` a la escena:
   - `UnityScheduleSource`
   - `UnityQueue`
   - `UnityMultiServer` o `UnityCombiner`
   - `UnitySink`

2. Configura parámetros en el Inspector:
   - Capacidades de colas
   - Tiempos de proceso
   - Conexiones (nextElement o myNextLink)

3. Organiza los elementos bajo un GameObject raíz (recomendado)

### **Paso 2: Extraer Configuración**

```csharp
// En ChapasGAController
public GameObject modelRoot; // Asignar en Inspector

public void ExtractModel()
{
    var extractor = new GameObject("Extractor").AddComponent<UnityModelExtractor>();
    extractor.modelRoot = modelRoot;
    
    SimulationConfig config = extractor.ExtractConfiguration();
    _runner.SetModelConfig(config);
    
    Destroy(extractor.gameObject);
}
```

### **Paso 3: Ejecutar Simulación Headless**

```csharp
// Opción A: Con modelo extraído
var result = _runner.RunSimulationWithConfig(chapas, order, inspectionBits);

// Opción B: Con modelo programático (original)
var result = _runner.RunHeadlessSimulation(chapas, order, inspectionBits);
```

---

## ?? Configuración en `ChapasGAController`

### **Nuevos Parámetros en Inspector:**

```csharp
[Header("Model Extraction")]
[SerializeField] private GameObject modelRoot;        // Raíz del modelo Unity
[SerializeField] private bool useExtractedModel;      // true = usar modelo Unity
```

### **Flags de Uso:**

- `useExtractedModel = false` ? Usa modelo programático (Source?Queue?Combiner?Sink)
- `useExtractedModel = true` ? Extrae y usa modelo configurado en Unity

---

## ?? Elementos Soportados

### **Actualmente Implementados:**

| Tipo Unity | Tipo SimuLean | Parámetros Extraídos |
|------------|---------------|----------------------|
| `UnityQueue` | `ItemsQueue` | capacity |
| `UnityGateQueue` | `GateQueue` | capacity |
| `UnityScheduleSource` | `ScheduleSource` | fileName, myName |
| `UnityMultiServer` | `MultiServer` | elementName, cTime, capacity |
| `UnityCombiner` | `Combiner` | elementName, requirements, capacity, batchMode |
| `UnitySink` | `Sink` | name |
| `UnityInfinitySource` | `InfiniteSource` | name |

### **Agregar Nuevos Elementos:**

En `UnityModelExtractor.cs`, método `ExtractSpecificParameters`:

```csharp
else if (element is UnityMyNewElement myElement)
{
    config.Parameters["param1"] = myElement.param1;
    config.Parameters["param2"] = myElement.param2;
    // etc...
}
```

En `HeadlessModelFactory.cs`, método `CreateElement`:

```csharp
case "UnityMyNewElement":
    return CreateMyNewElement(config, vElement);
```

Y agregar método de creación:

```csharp
private MyNewElement CreateMyNewElement(ElementConfig config, VElement vElement)
{
    var param1 = config.GetParameter<Type>("param1", defaultValue);
    var param2 = config.GetParameter<Type>("param2", defaultValue);
    
    return new MyNewElement(param1, param2, config.Name, clock, vElement);
}
```

---

## ?? Ejemplo Completo

```csharp
using UnityEngine;
using ChapasGA.GA;
using ChapasGA.Models;
using System.Collections.Generic;

public class ModelExtractionExample : MonoBehaviour
{
    [SerializeField] private GameObject simulationModelRoot;
    
    void Start()
    {
        // 1. Cargar datos
        var loader = new ExcelChapaLoader();
        List<Chapa> chapas = loader.LoadFromStreamingAssets("Llegada_Chapas.xlsx");
        
        // 2. Extraer modelo de Unity
        var extractor = gameObject.AddComponent<UnityModelExtractor>();
        extractor.modelRoot = simulationModelRoot;
        var config = extractor.ExtractConfiguration();
        
        Debug.Log($"Extracted {config.Elements.Count} elements:");
        foreach (var elem in config.Elements)
        {
            Debug.Log($"  - {elem.Name} ({elem.Type})");
        }
        
        // 3. Usar en GA Runner
        var runner = new ChapaGARunner();
        runner.SetModelConfig(config);
        
        // 4. Ejecutar simulaciones con diferentes órdenes
        int[] order1 = new int[] { 0, 1, 2, 3, 4 };
        var result1 = runner.RunSimulationWithConfig(chapas, order1, null);
        Debug.Log($"Result 1: {result1}");
        
        int[] order2 = new int[] { 4, 3, 2, 1, 0 };
        var result2 = runner.RunSimulationWithConfig(chapas, order2, null);
        Debug.Log($"Result 2: {result2}");
        
        // 5. Comparar
        Debug.Log($"Fitness difference: {result2.CalculateFitness() - result1.CalculateFitness():F2}");
        
        Destroy(extractor);
    }
}
```

---

## ?? Exportar/Importar Configuración (Opcional)

### **Exportar a JSON:**

```csharp
var extractor = gameObject.AddComponent<UnityModelExtractor>();
extractor.modelRoot = modelRoot;
string json = extractor.ExportToJson();

// Se guarda en: Application.persistentDataPath/simulation_config.json
Debug.Log("Exported to: " + Application.persistentDataPath);
```

### **Importar desde JSON:**

```csharp
string json = File.ReadAllText(path);
var config = JsonUtility.FromJson<SimulationConfig>(json);

var runner = new ChapaGARunner();
runner.SetModelConfig(config);
```

---

## ?? Debugging

### **Verificar Extracción:**

```csharp
[ContextMenu("Debug: Print Extracted Model")]
void DebugExtractedModel()
{
    var config = _extractor.ExtractConfiguration();
    
    Debug.Log($"=== EXTRACTED MODEL ===");
    Debug.Log($"Elements: {config.Elements.Count}");
    foreach (var elem in config.Elements)
    {
        Debug.Log($"  [{elem.Type}] {elem.Name} (ID: {elem.Id})");
        foreach (var param in elem.Parameters)
        {
            Debug.Log($"    {param.Key} = {param.Value}");
        }
    }
    
    Debug.Log($"\nConnections: {config.Connections.Count}");
    foreach (var conn in config.Connections)
    {
        Debug.Log($"  {conn.SourceId} ? {conn.TargetId} ({conn.ConnectionType})");
    }
}
```

### **Habilitar Logging en Simulación:**

```csharp
var factory = new HeadlessModelFactory(clock, enableLogging: true);
var elements = factory.BuildModel(config);
```

---

## ? Ventajas

1. **?? Diseño Visual**: Configura el modelo en Unity visualmente
2. **?? Sin Duplicación**: No necesitas reescribir el modelo en código
3. **?? Testeo Fácil**: Prueba en Unity, optimiza en headless
4. **?? Consistencia**: Mismo modelo en ambos modos
5. **? Performance**: Simulación headless ~1000x más rápida

---

## ?? Limitaciones Actuales

1. **ScheduleSource con DataDict**: No se extrae dataDict de Unity (se debe proporcionar en código)
2. **Parámetros Privados**: Solo se extraen parámetros públicos/serializados
3. **Elementos Complejos**: Algunos elementos pueden requerir configuración adicional

**Solución temporal**: Usar el método híbrido:
- Extraer estructura del modelo (elementos + conexiones)
- Configurar ScheduleSource programáticamente con datos de chapas

---

## ?? Próximos Pasos Sugeridos

1. **Integrar con Fitness Function**: Usar modelo extraído en `ChapaFitness.Evaluate()`
2. **Cache de Configuración**: Guardar configuración extraída para reutilizar
3. **Validación de Modelo**: Verificar que el modelo extraído es válido
4. **Soporte para más elementos**: Agregar soporte para Assembler, MultiAssembler, etc.

---

**Estado:** ? Implementado y Compilado  
**Fecha:** Enero 2025  
**Versión:** 1.0
