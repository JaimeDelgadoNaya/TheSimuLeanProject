using System;
using System.Collections.Generic;
using System.Linq;
using ChapasGA.Models;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;
using SimuLean.Serialization;

namespace ChapasGA.GA
{
    public class ChapaFitness : IFitness
    {
        private readonly IList<Chapa> _chapas;
        private readonly ChapaGARunner _runner;

        /// <summary>
        /// Constructor con simulación headless usando modelo extraído de Unity
        /// </summary>
        /// <param name="chapas">Lista de chapas</param>
        /// <param name="modelConfig">Configuración del modelo extraído desde Unity</param>
        public ChapaFitness(IList<Chapa> chapas, SimulationConfig modelConfig)
        {
            _chapas = chapas;
            _runner = new ChapaGARunner();
            _runner.SetModelConfig(modelConfig);
        }

        public double Evaluate(IChromosome chromosome)
        {
            var c = chromosome as ChapaChromosome ?? throw new ArgumentException("Invalid chromosome");
            return EvaluateDetailed(c).fitness;
        }

        public (double fitness, int inspections, int delays, double[] completionTimes) EvaluateDetailed(ChapaChromosome c)
        {
            return EvaluateWithSimulation(c);
        }

        /// <summary>
        /// Evaluación usando simulación headless (modelo extraído de Unity)
        /// </summary>
        private (double fitness, int inspections, int delays, double[] completionTimes) EvaluateWithSimulation(ChapaChromosome c)
        {
            var order = c.GetOrder();
            var bits = c.GetInspectionBits();

            // Convertir IList<int> a int[]
            var orderArray = order.ToArray();

            // Ejecutar simulación headless
            var result = _runner.RunSimulationWithConfig(
                _chapas as List<Chapa> ?? new List<Chapa>(_chapas),
                orderArray,
                bits
            );

            // Calcular fitness usando métricas de la simulación
            double fitness = result.CalculateFitness();

            // Crear array of completion times (por ahora vacío, se puede mejorar)
            double[] completionTimes = new double[bits.Length];
            
            return (fitness, result.TotalInspections, result.TotalDelays, completionTimes);
        }
    }
}
