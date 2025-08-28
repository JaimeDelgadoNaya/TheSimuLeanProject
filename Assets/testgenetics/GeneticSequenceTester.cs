using UnityEngine;

namespace UnitySimuLean
{
    public class GeneticSequenceTester : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Optional settings asset that overrides local parameters.")]
        [SerializeField]
        private GeneticAlgorithmSettings settings;

        [Header("General Settings")]
        [Tooltip("Enable or disable genetic algorithm optimization.")]
        [SerializeField]
        private bool enableOptimization = true;

        [Header("Genetic Algorithm Parameters")]
        [Tooltip("Number of parts in the sequence.")]
        [Min(1)]
        [SerializeField]
        private int numberOfParts = 10;
        [Tooltip("Number of generations to evolve.")]
        [Min(1)]
        [SerializeField]
        private int generations = 100;
        [Tooltip("Population size per generation.")]
        [Min(1)]
        [SerializeField]
        private int populationSize = 50;

        [Header("Genetic Operators")]
        [Tooltip("Selection method to use.")]
        [SerializeField]
        private SelectionType selectionType = SelectionType.Elite;
        [Tooltip("Crossover operator to apply.")]
        [SerializeField]
        private CrossoverType crossoverType = CrossoverType.Ordered;
        [Tooltip("Mutation operator to apply.")]
        [SerializeField]
        private MutationType mutationType = MutationType.Twors;

        [Header("Dependencies")]
        [Tooltip("Simulation runner used to evaluate sequences.")]
        [SerializeField]
        private UnitySimulationRunnerBehaviour simulationRunner;

        private void Start()
        {
            if (enableOptimization)
            {
                RunOptimization();
            }
        }

        [ContextMenu("Run Optimization")]
        public void RunOptimization()
        {
            if (!enableOptimization)
            {
                return;
            }

            if (simulationRunner == null)
            {
                Debug.LogWarning("Simulation runner not assigned.");
                return;
            }

            var nParts = settings != null ? settings.numberOfParts : numberOfParts;
            var gens = settings != null ? settings.generations : generations;
            var popSize = settings != null ? settings.populationSize : populationSize;
            var selType = settings != null ? settings.selectionType : selectionType;
            var crossType = settings != null ? settings.crossoverType : crossoverType;
            var mutType = settings != null ? settings.mutationType : mutationType;

            var bestSequence = SequenceOptimizer.OptimizePartSequence(
                simulationRunner,
                nParts,
                gens,
                popSize,
                selType,
                crossType,
                mutType);

            if (bestSequence.Length > 0)
            {
                Debug.Log($"Optimized sequence: {string.Join(",", bestSequence)}");
                Debug.Log($"Total delay: {simulationRunner.TotalDelay}, inspection count: {simulationRunner.InspectionCount}");
            }
            else
            {
                Debug.Log("No sequence returned from optimizer.");
            }
        }
    }
}
