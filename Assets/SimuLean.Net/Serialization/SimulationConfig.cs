using System;
using System.Collections.Generic;

namespace SimuLean.Serialization
{
    /// <summary>
    /// Configuración serializable de un modelo de simulación completo.
    /// Se puede guardar/cargar desde JSON, XML, o crear desde el Inspector de Unity.
    /// </summary>
    [Serializable]
    public class SimulationConfig
    {
        public List<ElementConfig> Elements { get; set; } = new List<ElementConfig>();
        public List<ConnectionConfig> Connections { get; set; } = new List<ConnectionConfig>();
        public double MaxSimulationTime { get; set; } = 10000.0;

        public SimulationConfig()
        {
        }
    }

    /// <summary>
    /// Configuración de un elemento individual del modelo.
    /// </summary>
    [Serializable]
    public class ElementConfig
    {
        public string Id { get; set; }                    // Identificador único (ej: "Source1", "Queue1")
        public string Type { get; set; }                  // Tipo de elemento (ej: "ScheduleSource", "ItemsQueue")
        public string Name { get; set; }                  // Nombre descriptivo
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        public ElementConfig()
        {
        }

        public ElementConfig(string id, string type, string name)
        {
            Id = id;
            Type = type;
            Name = name;
        }

        // Helpers para agregar parámetros comunes
        public void SetCapacity(int capacity)
        {
            Parameters["capacity"] = capacity;
        }

        public void SetFileName(string fileName)
        {
            Parameters["fileName"] = fileName;
        }

        public void SetProcessTime(double time)
        {
            Parameters["processTime"] = time;
        }

        public void SetDataDict(Dictionary<string, List<string>> dataDict)
        {
            Parameters["dataDict"] = dataDict;
        }

        public T GetParameter<T>(string key, T defaultValue = default(T))
        {
            if (Parameters.ContainsKey(key))
            {
                return (T)Parameters[key];
            }
            return defaultValue;
        }
    }

    /// <summary>
    /// Configuración de una conexión entre elementos.
    /// </summary>
    [Serializable]
    public class ConnectionConfig
    {
        public string SourceId { get; set; }              // ID del elemento origen
        public string TargetId { get; set; }              // ID del elemento destino
        public string ConnectionType { get; set; }        // Tipo de conexión ("Simple", "General", "CombinerMainInput", "CombinerAdditionalInput")
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();  // Parámetros adicionales (ej: inputPort)

        public ConnectionConfig()
        {
        }

        public ConnectionConfig(string sourceId, string targetId, string connectionType = "General")
        {
            SourceId = sourceId;
            TargetId = targetId;
            ConnectionType = connectionType;
        }
        
        public T GetParameter<T>(string key, T defaultValue = default(T))
        {
            if (Parameters != null && Parameters.ContainsKey(key))
            {
                return (T)Parameters[key];
            }
            return defaultValue;
        }
    }
}
