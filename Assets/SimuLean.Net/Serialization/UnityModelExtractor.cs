using System.Collections.Generic;
using UnityEngine;
using SimuLean.Serialization;
using UnitySimuLean;

namespace SimuLean.Unity
{
    /// <summary>
    /// Extrae la configuración del modelo desde la escena de Unity.
    /// Recorre los componentes SElement y crea una SimulationConfig serializable.
    /// </summary>
    public class UnityModelExtractor : MonoBehaviour
    {
        [Header("Model Elements")]
        [Tooltip("Raíz del modelo - GameObject que contiene todos los elementos")]
        public GameObject modelRoot;

        [Header("Export Options")]
        [Tooltip("Nombre del archivo de configuración")]
        public string configFileName = "simulation_config.json";

        private Dictionary<SElement, string> elementIds = new Dictionary<SElement, string>();

        /// <summary>
        /// Extrae la configuración del modelo desde la escena de Unity.
        /// </summary>
        public SimulationConfig ExtractConfiguration()
        {
            var config = new SimulationConfig();
            elementIds.Clear();

            // 1. Encontrar todos los elementos SElement en la escena
            SElement[] elements = FindAllSimElements();

            if (elements.Length == 0)
            {
                Debug.LogWarning("No simulation elements found in scene!");
                return config;
            }

            Debug.Log($"Found {elements.Length} simulation elements");

            // 2. Crear configuración para cada elemento
            foreach (var element in elements)
            {
                var elementConfig = ExtractElementConfig(element);
                if (elementConfig != null)
                {
                    config.Elements.Add(elementConfig);
                }
            }

            // 3. Extraer conexiones entre elementos
            foreach (var element in elements)
            {
                ExtractConnections(element, config.Connections);
            }

            Debug.Log($"Extracted {config.Elements.Count} elements and {config.Connections.Count} connections");

            return config;
        }

        /// <summary>
        /// Encuentra todos los elementos SElement en el modelo.
        /// </summary>
        private SElement[] FindAllSimElements()
        {
            if (modelRoot != null)
            {
                return modelRoot.GetComponentsInChildren<SElement>();
            }
            else
            {
                // Buscar en toda la escena si no hay raíz especificada
                return FindObjectsOfType<SElement>();
            }
        }

        /// <summary>
        /// Extrae la configuración de un elemento individual.
        /// </summary>
        private ElementConfig ExtractElementConfig(SElement element)
        {
            if (element == null) return null;

            // Generar ID único para el elemento
            string id = GenerateElementId(element);
            elementIds[element] = id;

            var config = new ElementConfig(id, GetElementType(element), element.name);

            // Extraer parámetros específicos según el tipo
            ExtractSpecificParameters(element, config);

            return config;
        }

        /// <summary>
        /// Genera un ID único para un elemento.
        /// </summary>
        private string GenerateElementId(SElement element)
        {
            string typeName = GetElementType(element);
            int instanceId = element.GetInstanceID();
            return $"{typeName}_{element.name}_{instanceId}";
        }

        /// <summary>
        /// Obtiene el tipo de elemento (clase concreta).
        /// </summary>
        private string GetElementType(SElement element)
        {
            return element.GetType().Name;
        }

        /// <summary>
        /// Extrae parámetros específicos según el tipo de elemento.
        /// </summary>
        private void ExtractSpecificParameters(SElement element, ElementConfig config)
        {
            // UnityQueue / ItemsQueue
            if (element is UnityQueue queue)
            {
                config.SetCapacity(queue.capacity);
            }
            // UnityGateQueue
            else if (element is UnityGateQueue gateQueue)
            {
                config.SetCapacity(gateQueue.capacity);
            }
            // UnityScheduleSource
            else if (element is UnityScheduleSource scheduleSource)
            {
                config.SetFileName(scheduleSource.fileName);
                config.Parameters["myName"] = scheduleSource.myName;
                // UnityScheduleSource no tiene sheetName ni autoSort públicos
                // Usar valores por defecto
                config.Parameters["sheetName"] = "";
                config.Parameters["autoSort"] = true;
            }
            // UnityMultiServer
            else if (element is UnityMultiServer multiServer)
            {
                config.Parameters["elementName"] = multiServer.elementName;
                config.Parameters["cTime"] = multiServer.cTime;
                config.SetCapacity(multiServer.capacity);
            }
            // UnityCombiner
            else if (element is UnityCombiner combiner)
            {
                config.Parameters["elementName"] = combiner.elementName;
                config.Parameters["requirements"] = combiner.requirements;
                config.Parameters["meanDelay"] = combiner.meanDelay;
                config.SetCapacity(combiner.capacity);
                config.Parameters["batchMode"] = combiner.batchMode;
                config.Parameters["updateRequirements"] = combiner.updateRequirements;
                if (combiner.updateLabels != null)
                {
                    config.Parameters["updateLabels"] = new List<string>(combiner.updateLabels);
                }
                
                // Extraer inputs adicionales del Combiner (más allá del puerto principal)
                // Los inputs[0] se conectan vía nextElement, inputs[1+] son las entradas adicionales
                if (combiner.myInputs != null && combiner.myInputs.Length > 0)
                {
                    List<string> inputIds = new List<string>();
                    foreach (var input in combiner.myInputs)
                    {
                        if (input != null && elementIds.ContainsKey(input))
                        {
                            inputIds.Add(elementIds[input]);
                        }
                    }
                    config.Parameters["additionalInputs"] = inputIds;
                }
            }
            // UnitySink
            else if (element is UnitySink sink)
            {
                config.Parameters["name"] = sink.name;
            }
            // Agregar más tipos según sea necesario...
        }

        /// <summary>
        /// Extrae las conexiones de un elemento.
        /// </summary>
        private void ExtractConnections(SElement element, List<ConnectionConfig> connections)
        {
            if (element == null) return;

            string sourceId = elementIds.ContainsKey(element) ? elementIds[element] : null;
            if (sourceId == null) return;

            // Conexión simple (nextElement) - Para Combiner, esto será el puerto principal (input[0])
            if (element.nextElement != null && elementIds.ContainsKey(element.nextElement))
            {
                string targetId = elementIds[element.nextElement];
                
                // Si el target es un Combiner, marcar como conexión al puerto principal
                if (element.nextElement is UnityCombiner)
                {
                    var conn = new ConnectionConfig(sourceId, targetId, "CombinerMainInput");
                    conn.Parameters = new Dictionary<string, object> { { "inputPort", 0 } };
                    connections.Add(conn);
                }
                else
                {
                    connections.Add(new ConnectionConfig(sourceId, targetId, "Simple"));
                }
            }

            // Conexión MultiLink (myNextLink)
            if (element.myNextLink != null)
            {
                foreach (var output in element.myNextLink.outputs)
                {
                    if (output != null && elementIds.ContainsKey(output))
                    {
                        string targetId = elementIds[output];
                        connections.Add(new ConnectionConfig(sourceId, targetId, "MultiLink"));
                    }
                }
            }
            
            // IMPORTANTE: No extraer myInputs aquí para Combiner porque ya se guardó en Parameters
            // Las conexiones adicionales del Combiner se manejarán en el Factory
        }

        /// <summary>
        /// Exporta la configuración a JSON.
        /// </summary>
        public string ExportToJson()
        {
            var config = ExtractConfiguration();
            string json = JsonUtility.ToJson(config, prettyPrint: true);
            
            // Guardar en archivo si es necesario
            string path = System.IO.Path.Combine(Application.persistentDataPath, configFileName);
            System.IO.File.WriteAllText(path, json);
            
            Debug.Log($"Configuration exported to: {path}");
            return json;
        }

        /// <summary>
        /// Importa configuración desde JSON.
        /// </summary>
        public SimulationConfig ImportFromJson(string json)
        {
            return JsonUtility.FromJson<SimulationConfig>(json);
        }

        // Context Menu para testing
        [ContextMenu("Extract Model Configuration")]
        public void TestExtractConfiguration()
        {
            var config = ExtractConfiguration();
            Debug.Log($"Extracted {config.Elements.Count} elements and {config.Connections.Count} connections");
            
            foreach (var elem in config.Elements)
            {
                Debug.Log($"Element: {elem.Name} (Type: {elem.Type}, ID: {elem.Id})");
            }
        }

        [ContextMenu("Export to JSON")]
        public void TestExportToJson()
        {
            string json = ExportToJson();
            Debug.Log("Configuration exported:\n" + json);
        }
    }
}
