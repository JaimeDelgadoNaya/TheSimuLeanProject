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

        // Almacenar configuración del modelo desde Unity
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
        /// Establece la configuración del modelo extraída desde Unity.
        /// </summary>
        public void SetModelConfig(SimulationConfig config)
        {
            modelConfig = config;
        }

        /// <summary>
        /// Obtiene la configuración del modelo actual.
        /// </summary>
        public SimulationConfig GetModelConfig()
        {
            return modelConfig;
        }

        /// <summary>
        /// Ejecuta simulación usando modelo configurado desde Unity.
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

            // Si no se proporcionan bits de inspección, no inspeccionar ninguna
            if (inspectionBits == null)
            {
                inspectionBits = new int[chapas.Count];
            }

            // Crear SimClock
            var clock = new SimClock();

            // Crear factory para construir modelo headless
            var factory = new HeadlessModelFactory(clock, enableLogging: false);

            // Construir modelo desde configuración
            var elements = factory.BuildModel(modelConfig);

            // Buscar el source y configurarlo con los datos de las chapas
            ScheduleSource source = FindScheduleSource(elements);
            if (source != null)
            {
                // NOTA: ScheduleSource ya fue creado con los datos del Excel
                // Aquí necesitamos reemplazarlo con los datos ordenados del GA
                // Por ahora, esto es una limitación - el source debe ser recreado
                // NO usar UnityEngine.Debug desde background thread
                System.Console.WriteLine("RunSimulationWithConfig: Cannot modify ScheduleSource data after creation. Using original Excel order.");
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

            // Ejecutar simulación
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
        /// Calcula el tiempo máximo de simulación necesario
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
    /// Resultado de una simulación headless
    /// </summary>
    public class SimulationResult
    {
        public int TotalItems { get; set; }
        public int TotalInspections { get; set; }
        public int TotalDelays { get; set; }
        public double SimulationTime { get; set; }
        public int QueueLength { get; set; }

        /// <summary>
        /// Calcula una métrica de fitness combinada
        /// </summary>
        public double CalculateFitness()
        {
            // Penalizaciones:
            // - Cada retraso: -100 puntos
            // - Cada inspección: -10 puntos
            // - Tiempo de simulación: -1 punto por unidad de tiempo
            
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
