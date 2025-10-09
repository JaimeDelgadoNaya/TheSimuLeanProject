using System.Collections.Generic;
using ChapasGA.Models;
using GeneticSharp.Domain;
using GeneticSharp.Domain.Fitnesses;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;
using SimuLean;
using SimuLean.Headless;
using SimuLean.Serialization;
using System.Linq;

namespace ChapasGA.GA
{
    public class ChapaGARunner
    {
        public double BestFitness { get; set; }
        public int TotalInspections { get; set; }
        public int TotalDelays { get; set; }
        public IList<int> BestOrder { get; set; }
        public int[] BestBits { get; set; }
        public double[] CompletionTimes { get; set; }

        // Almacenar configuraci�n del modelo desde Unity
        private SimulationConfig modelConfig;

        public void RunGA(IList<Chapa> chapas, int populationSize, int generations, float crossoverProb, float mutationProb)
        {
            if (modelConfig == null)
            {
                throw new System.InvalidOperationException("Model configuration not set. Call SetModelConfig() before RunGA().");
            }

            int n = chapas.Count;
            var chromosome = new ChapaChromosome(n);
            var fitness = new ChapaFitness(chapas, modelConfig);
            var population = new Population(populationSize, populationSize, chromosome);
            var selection = new TournamentSelection();
            var crossover = new ChapaCrossover();
            var mutation = new ChapaMutation();
            var ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation)
            {
                CrossoverProbability = crossoverProb,
                MutationProbability = mutationProb,
                Termination = new GenerationNumberTermination(generations)
            };

            // Suscribirse a eventos del GA para logging
            ga.GenerationRan += (sender, e) =>
            {
                var bestChromosome = ga.BestChromosome as ChapaChromosome;
                var currentFitness = ga.BestChromosome.Fitness.Value;
                
                // NO usar UnityEngine.Debug desde background thread en AsyncGARunner
                // Use Console instead when called from background thread
                System.Console.WriteLine($"[GA] Generation {ga.GenerationsNumber}/{generations} | " +
                                     $"Best Fitness: {currentFitness:F2} | " +
                                     $"Time: {ga.TimeEvolving.TotalSeconds:F1}s");
            };

            ga.Start();

            var best = ga.BestChromosome as ChapaChromosome;
            
            // Evaluar el mejor cromosoma para obtener detalles
            var fitnessEvaluator = fitness as ChapaFitness;
            if (fitnessEvaluator != null)
            {
                var details = fitnessEvaluator.EvaluateDetailed(best);
                BestFitness = details.fitness;
                TotalInspections = details.inspections;
                TotalDelays = details.delays;
                CompletionTimes = details.completionTimes;
            }
            else
            {
                BestFitness = fitness.Evaluate(best);
            }
            
            BestOrder = best.GetOrder();
            BestBits = best.GetInspectionBits();
        }

        /// <summary>
        /// Establece la configuraci�n del modelo extra�da desde Unity.
        /// </summary>
        public void SetModelConfig(SimulationConfig config)
        {
            modelConfig = config;
        }

        /// <summary>
        /// Obtiene la configuraci�n del modelo actual.
        /// </summary>
        public SimulationConfig GetModelConfig()
        {
            return modelConfig;
        }

        /// <summary>
        /// Convierte la lista de Chapas a un diccionario column-oriented para usar con SeqOptTools.
        /// </summary>
        private Dictionary<string, List<string>> ChapasToDataDict(List<Chapa> chapas)
        {
            var dataDict = new Dictionary<string, List<string>>();

            // Map Chapa properties to dictionary columns
            // Assuming arrival times are sequential with small intervals
            dataDict["Time"] = chapas.Select((c, index) => (index * 0.1).ToString()).ToList();
            dataDict["Name"] = chapas.Select(c => c.Name ?? "Chapa").ToList();
            dataDict["Q"] = chapas.Select(c => "1").ToList(); // 1 item per chapa
            dataDict["tSoldadura"] = chapas.Select(c => c.tSoldadura.ToString()).ToList();
            dataDict["tInspeccion"] = chapas.Select(c => c.tInspeccion.ToString()).ToList();
            dataDict["DueDate"] = chapas.Select(c => c.DueDate.ToString()).ToList();
            
            // inspeccionOn will be added separately via AddLabelsToDict in RunSimulationWithConfig

            return dataDict;
        }

        /// <summary>
        /// Ejecuta simulaci�n usando modelo configurado desde Unity con el orden y bits de inspecci�n del GA.
        /// </summary>
        public SimulationResult RunSimulationWithConfig(List<Chapa> chapas, int[] order = null, int[] inspectionBits = null)
        {
            if (modelConfig == null)
            {
                throw new System.InvalidOperationException("Model configuration not set. Call SetModelConfig() first.");
            }

            // Si no se proporciona orden, usar secuencia original
            if (order == null)
            {
                order = Enumerable.Range(0, chapas.Count).ToArray();
            }

            // Si no se proporcionan bits de inspecci�n, no inspeccionar ninguna
            if (inspectionBits == null)
            {
                inspectionBits = new int[chapas.Count];
            }

            // ========================================
            // NUEVO: Usar SeqOptTools para reordenar
            // ========================================
            
            // 1. Convertir chapas a dataDict
            var chapaDataDict = ChapasToDataDict(chapas);
            
            // 2. Convertir order (0-based) a priorities (1-based)
            // order[i] = j significa que el item i debe ir a la posici�n j
            // pero SeqOptTools espera priorities donde priorities[i] = j significa
            // que el item en la posición i original debe ir a la posición j nueva
            
            // Invertir el order array para obtener priorities
            var priorities = new int[order.Length];
            for (int i = 0; i < order.Length; i++)
            {
                // order[i] es el índice original del item que debe estar en posición i
                // priorities[order[i]] = i + 1 (1-based)
                priorities[order[i]] = i + 1;
            }
            
            // 3. Agregar inspection bits al dataDict
            SeqOptTools.AddLabelsToDict(chapaDataDict, "inspeccionOn", inspectionBits);
            
            // 4. Reordenar usando TransformSequence
            var reorderedDataDict = SeqOptTools.TransformSequence(chapaDataDict, priorities);
            
            // 5. Actualizar la configuración del modelo para usar el dataDict reordenado
            UpdateModelConfigWithReorderedData(reorderedDataDict);

            // Crear SimClock
            var clock = new SimClock();

            // Crear factory para construir modelo headless
            var factory = new HeadlessModelFactory(clock, enableLogging: false);

            // Construir modelo desde configuraci�n actualizada
            var elements = factory.BuildModel(modelConfig);

            // Buscar el source - ya debe estar configurado con los datos reordenados
            ScheduleSource source = FindScheduleSource(elements);
            if (source != null)
            {
                // El source ahora debe estar usando el dataDict reordenado
                System.Console.WriteLine($"[ChapaGARunner] ScheduleSource found and configured with reordered data");
            }

            // Buscar el sink para obtener resultados
            Sink sink = FindSink(elements);
            if (sink != null)
            {
                sink.expectedItems = chapas.Count;
            }

            // Inicializar todos los elementos
            foreach (var element in elements.Values)
            {
                element.Start();
            }

            // Ejecutar simulaci�n
            double maxSimTime = CalculateMaxSimTime(chapas);
            clock.AdvanceClock(maxSimTime);

            // Recopilar resultados
            var result = new SimulationResult();
            if (sink != null)
            {
                result.TotalItems = sink.GetNumberIterms();
                result.TotalInspections = sink.GetInspecciones();
                result.TotalDelays = sink.GetRetrasados();
            }
            result.SimulationTime = clock.GetSimulationTime();

            return result;
        }

        /// <summary>
        /// Actualiza la configuraci�n del modelo para que el ScheduleSource use el dataDict reordenado.
        /// </summary>
        private void UpdateModelConfigWithReorderedData(Dictionary<string, List<string>> reorderedDataDict)
        {
            // Buscar el elemento ScheduleSource en la configuraci�n
            foreach (var elemConfig in modelConfig.Elements)
            {
                if (elemConfig.Type == "UnityScheduleSource")
                {
                    // Actualizar el dataDict en los parámetros
                    if (elemConfig.Parameters == null)
                    {
                        elemConfig.Parameters = new Dictionary<string, object>();
                    }
                    
                    elemConfig.Parameters["dataDict"] = reorderedDataDict;
                    elemConfig.Parameters["autoSort"] = false; // IMPORTANTE: No reordenar automáticamente
                    
                    System.Console.WriteLine($"[ChapaGARunner] Updated ScheduleSource '{elemConfig.Name}' with reordered data");
                }
            }
        }

        /// <summary>
        /// Busca el primer ScheduleSource en los elementos creados.
        /// </summary>
        private ScheduleSource FindScheduleSource(Dictionary<string, Element> elements)
        {
            foreach (var element in elements.Values)
            {
                if (element is ScheduleSource source)
                {
                    return source;
                }
            }
            return null;
        }

        /// <summary>
        /// Busca el primer Sink en los elementos creados.
        /// </summary>
        private Sink FindSink(Dictionary<string, Element> elements)
        {
            foreach (var element in elements.Values)
            {
                if (element is Sink sink)
                {
                    return sink;
                }
            }
            return null;
        }

        /// <summary>
        /// Calcula el tiempo m�ximo de simulaci�n necesario
        /// </summary>
        private double CalculateMaxSimTime(List<Chapa> chapas)
        {
            // Tiempo base + suma de todos los tiempos de proceso + margen de seguridad
            double totalProcessTime = 0;
            foreach (var chapa in chapas)
            {
                totalProcessTime += chapa.tSoldadura + chapa.tInspeccion;
            }

            // Agregar tiempo de llegada + margen de 50%
            double arrivalTime = chapas.Count * 0.1;
            double maxTime = arrivalTime + totalProcessTime * 1.5;

            return maxTime;
        }
    }

    /// <summary>
    /// Resultado de una simulaci�n headless
    /// </summary>
    public class SimulationResult
    {
        public int TotalItems { get; set; }
        public int TotalInspections { get; set; }
        public int TotalDelays { get; set; }
        public double SimulationTime { get; set; }
        public int QueueLength { get; set; }

        /// <summary>
        /// Calcula una m�trica de fitness combinada
        /// </summary>
        public double CalculateFitness()
        {
            // Penalizaciones:
            // - Cada retraso: -100 puntos
            // - Cada inspecci�n: -10 puntos
            // - Tiempo de simulaci�n: -1 punto por unidad de tiempo
            
            double fitness = 0;
            fitness -= TotalDelays * 100.0;
            fitness -= TotalInspections * 10.0;
            fitness -= SimulationTime * 1.0;

            return fitness;
        }

        public override string ToString()
        {
            return $"Items: {TotalItems}, Inspecciones: {TotalInspections}, Retrasos: {TotalDelays}, " +
                   $"Tiempo: {SimulationTime:F2}s, Cola: {QueueLength}, Fitness: {CalculateFitness():F2}";
        }
    }
}
