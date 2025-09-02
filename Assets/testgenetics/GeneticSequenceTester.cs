using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

        [Tooltip("Run in headless mode and exit after optimization.")]
        [SerializeField]
        private bool headlessMode = false;

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
        // GeneticSharp requires at least two chromosomes per generation.
        // Enforce a minimum of 2 in the inspector to prevent runtime errors.
        [Min(2)]
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

        private const string CsvFileName = "optimization_results.csv";

        private void Start()
        {
            if (headlessMode)
            {
                Application.runInBackground = true;
            }

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

            var result = SequenceOptimizer.OptimizePartSequence(
                simulationRunner,
                nParts,
                gens,
                popSize,
                selType,
                crossType,
                mutType);

            var (bestSequence, delayCount, inspectionCount) = result;

            if (bestSequence.Length > 0)
            {
                Debug.Log($"Optimized sequence: {string.Join(",", bestSequence)}");
                Debug.Log($"Delay count: {delayCount}, inspection count: {inspectionCount}");
                WriteResultsToCsv(bestSequence, delayCount, inspectionCount);
            }
            else
            {
                Debug.Log("No sequence returned from optimizer.");
            }

            if (headlessMode)
            {
                Debug.Log("Headless mode active. Exiting application.");
#if UNITY_EDITOR
                EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        private void WriteResultsToCsv(string[] sequence, int delayCount, int inspectionCount)
        {
            var logsPath = Path.Combine(Application.dataPath, "..", "Logs");
            Directory.CreateDirectory(logsPath);
            var csvPath = Path.Combine(logsPath, CsvFileName);
            const string header = "Sequence,DelayCount,InspectionCount";
            if (!File.Exists(csvPath) || new FileInfo(csvPath).Length == 0)
            {
                File.WriteAllText(csvPath, header + "\n");
            }
            else
            {
                var firstLine = File.ReadLines(csvPath).FirstOrDefault();
                if (firstLine != header)
                {
                    var existingLines = File.ReadAllLines(csvPath);
                    existingLines[0] = header;
                    File.WriteAllLines(csvPath, existingLines);
                }
            }

            var sequenceValue = string.Join(",", sequence);
            var line = $"\"{sequenceValue}\",{delayCount},{inspectionCount}\n";
            File.AppendAllText(csvPath, line);
            Debug.Log($"Results written to {csvPath}");
        }
    }
}
