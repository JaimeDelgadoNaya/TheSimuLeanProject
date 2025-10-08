using System;
using System.Collections.Generic;
using SimuLean.Serialization;

namespace SimuLean.Headless
{
    /// <summary>
    /// Factory que construye un modelo de simulación headless desde una configuración.
    /// Crea elementos SimuLean sin dependencias de Unity.
    /// </summary>
    public class HeadlessModelFactory
    {
        private Dictionary<string, Element> createdElements = new Dictionary<string, Element>();
        private SimClock clock;
        private bool enableLogging;

        public HeadlessModelFactory(SimClock clock, bool enableLogging = false)
        {
            this.clock = clock;
            this.enableLogging = enableLogging;
        }

        /// <summary>
        /// Construye el modelo completo desde una configuración.
        /// </summary>
        public Dictionary<string, Element> BuildModel(SimulationConfig config)
        {
            createdElements.Clear();

            // 1. Crear todos los elementos
            foreach (var elementConfig in config.Elements)
            {
                var element = CreateElement(elementConfig);
                if (element != null)
                {
                    createdElements[elementConfig.Id] = element;
                }
            }

            // 2. Crear conexiones entre elementos
            foreach (var connection in config.Connections)
            {
                CreateConnection(connection);
            }
            
            // 3. Crear conexiones adicionales de Combiners (inputs array)
            foreach (var elementConfig in config.Elements)
            {
                if (elementConfig.Type == "UnityCombiner")
                {
                    ConnectCombinerAdditionalInputs(elementConfig);
                }
            }

            return createdElements;
        }
        
        /// <summary>
        /// Conecta las entradas adicionales del Combiner (más allá del puerto principal).
        /// </summary>
        private void ConnectCombinerAdditionalInputs(ElementConfig combinerConfig)
        {
            if (!createdElements.ContainsKey(combinerConfig.Id))
            {
                return;
            }

            var combiner = createdElements[combinerConfig.Id] as Combiner;
            if (combiner == null)
            {
                return;
            }

            // Obtener la lista de IDs de inputs adicionales
            var additionalInputIds = combinerConfig.GetParameter<List<string>>("additionalInputs");
            if (additionalInputIds == null || additionalInputIds.Count == 0)
            {
                return;
            }

            // Conectar cada input adicional al CombinerInput correspondiente
            for (int i = 0; i < additionalInputIds.Count; i++)
            {
                string inputId = additionalInputIds[i];
                
                if (!createdElements.ContainsKey(inputId))
                {
                    Console.WriteLine($"Additional input element not found: {inputId}");
                    continue;
                }

                Element sourceElement = createdElements[inputId];
                
                // El índice del CombinerInput es i (porque input[0] es el puerto principal vía GeneralLink)
                // Los inputs adicionales son inputs[1], inputs[2], etc.
                int combinerInputIndex = i;
                
                CombinerInput combinerInput = combiner.GetComponentInput(combinerInputIndex);
                
                if (combinerInput == null)
                {
                    Console.WriteLine($"CombinerInput {combinerInputIndex} not found in Combiner {combinerConfig.Name}");
                    continue;
                }

                // Crear conexión GeneralLink desde el source al CombinerInput
                GeneralLink.CreateLink(sourceElement, new List<Element> { combinerInput });
                
                if (enableLogging)
                {
                    Console.WriteLine($"Connected {sourceElement.GetName()} ? Combiner[{combinerConfig.Name}].Input[{combinerInputIndex}]");
                }
            }
        }

        /// <summary>
        /// Crea un elemento individual según su configuración.
        /// </summary>
        private Element CreateElement(ElementConfig config)
        {
            VElement vElement = enableLogging ? new HeadlessVElement(true) : null;

            switch (config.Type)
            {
                case "UnityScheduleSource":
                    return CreateScheduleSource(config, vElement);

                case "UnityQueue":
                    return CreateItemsQueue(config, vElement);

                case "UnityGateQueue":
                    return CreateGateQueue(config, vElement);

                case "UnityMultiServer":
                    return CreateMultiServer(config, vElement);

                case "UnityCombiner":
                    return CreateCombiner(config, vElement);

                case "UnitySink":
                    return CreateSink(config, vElement);

                case "UnityInfinitySource":
                    return CreateInfiniteSource(config, vElement);

                default:
                    Console.WriteLine($"Unknown element type: {config.Type}");
                    return null;
            }
        }

        /// <summary>
        /// Crea un ScheduleSource desde configuración.
        /// </summary>
        private ScheduleSource CreateScheduleSource(ElementConfig config, VElement vElement)
        {
            string fileName = config.GetParameter<string>("fileName");
            string sheetName = config.GetParameter<string>("sheetName");
            bool autoSort = config.GetParameter<bool>("autoSort", true);
            var dataDict = config.GetParameter<Dictionary<string, List<string>>>("dataDict");

            return new ScheduleSource(
                name: config.Name,
                state: clock,
                fileName: fileName,
                dataDict: dataDict,
                modelItem: null,
                sheetName: sheetName,
                autoSort: autoSort,
                vElement: vElement
            );
        }

        /// <summary>
        /// Crea un ItemsQueue desde configuración.
        /// </summary>
        private ItemsQueue CreateItemsQueue(ElementConfig config, VElement vElement)
        {
            int capacity = config.GetParameter<int>("capacity", 100);

            return new ItemsQueue(
                capacity: capacity,
                myName: config.Name,
                sClock: clock,
                vElement: vElement
            );
        }

        /// <summary>
        /// Crea un GateQueue desde configuración.
        /// </summary>
        private GateQueue CreateGateQueue(ElementConfig config, VElement vElement)
        {
            int capacity = config.GetParameter<int>("capacity", 100);

            return new GateQueue(
                capacity: capacity,
                myName: config.Name,
                sClock: clock,
                vElement: vElement
            );
        }

        /// <summary>
        /// Crea un MultiServer desde configuración.
        /// </summary>
        private MultiServer CreateMultiServer(ElementConfig config, VElement vElement)
        {
            double cTime = config.GetParameter<double>("cTime", 2.0);
            int capacity = config.GetParameter<int>("capacity", 1);

            // Crear array de procesos de tiempo
            DoubleRandomProcess[] cycleTime = new DoubleRandomProcess[capacity];
            for (int i = 0; i < capacity; i++)
            {
                cycleTime[i] = new PoissonProcess(cTime);
            }

            return new MultiServer(
                randomTimes: cycleTime,
                name: config.Name,
                sClock: clock,
                vElement: vElement
            );
        }

        /// <summary>
        /// Crea un Combiner desde configuración.
        /// </summary>
        private Combiner CreateCombiner(ElementConfig config, VElement vElement)
        {
            int[] requirements = config.GetParameter<int[]>("requirements", new int[] { 1 });
            double meanDelay = config.GetParameter<double>("meanDelay", 2.0);
            int capacity = config.GetParameter<int>("capacity", 1);
            bool batchMode = config.GetParameter<bool>("batchMode", false);
            bool updateRequirements = config.GetParameter<bool>("updateRequirements", false);
            var updateLabels = config.GetParameter<List<string>>("updateLabels");

            var delayStrategy = new ConstantDouble(meanDelay);

            return new Combiner(
                requirements: requirements,
                delayStrategy: delayStrategy,
                name: config.Name,
                simClock: clock,
                batchMode: batchMode,
                pullMode: null,
                updateRequirements: updateRequirements,
                updateLabels: updateLabels,
                capacity: capacity,
                vElement: vElement
            );
        }

        /// <summary>
        /// Crea un Sink desde configuración.
        /// </summary>
        private Sink CreateSink(ElementConfig config, VElement vElement)
        {
            return new Sink(
                name: config.Name,
                state: clock,
                vElement: vElement
            );
        }

        /// <summary>
        /// Crea un InfiniteSource desde configuración.
        /// </summary>
        private InfiniteSource CreateInfiniteSource(ElementConfig config, VElement vElement)
        {
            return new InfiniteSource(
                name: config.Name,
                state: clock,
                vElement: vElement
            );
        }

        /// <summary>
        /// Crea una conexión entre dos elementos.
        /// </summary>
        private void CreateConnection(ConnectionConfig connection)
        {
            if (!createdElements.ContainsKey(connection.SourceId))
            {
                Console.WriteLine($"Source element not found: {connection.SourceId}");
                return;
            }

            if (!createdElements.ContainsKey(connection.TargetId))
            {
                Console.WriteLine($"Target element not found: {connection.TargetId}");
                return;
            }

            Element source = createdElements[connection.SourceId];
            Element target = createdElements[connection.TargetId];

            // Crear conexión según el tipo
            if (connection.ConnectionType == "Simple")
            {
                SimpleLink.CreateLink(source, target);
            }
            else // "General" o "MultiLink"
            {
                GeneralLink.CreateLink(source, new List<Element> { target });
            }
        }

        /// <summary>
        /// Obtiene un elemento creado por su ID.
        /// </summary>
        public Element GetElement(string id)
        {
            return createdElements.ContainsKey(id) ? createdElements[id] : null;
        }

        /// <summary>
        /// Obtiene todos los elementos creados.
        /// </summary>
        public Dictionary<string, Element> GetAllElements()
        {
            return createdElements;
        }
    }
}
