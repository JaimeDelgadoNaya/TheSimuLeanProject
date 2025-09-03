using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnitySimuLean
{
    public class GeneticScheduleTester : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField]
        private GeneticAlgorithmSettings settings;

        [Header("General Settings")]
        [SerializeField]
        private bool enableOptimization = true;

        [SerializeField]
        private bool headlessMode = false;

        [Tooltip("Excel file containing job data located under StreamingAssets.")]
        [SerializeField]
        private string excelFileName = "DatosPrevia.xlsx";

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

            var jobs = ExcelJobLoader.LoadJobs(excelFileName);

            var popSize = settings != null ? settings.populationSize : 100;
            var gens = settings != null ? settings.generations : 500;
            var crossProb = settings != null ? settings.crossoverProb : 0.9;
            var mutProb = settings != null ? settings.mutationProb : 0.15;
            var orderCross = settings != null ? settings.orderCrossover : OrderCrossoverType.PMX;
            var bitCross = settings != null ? settings.bitCrossover : BitCrossoverType.Uniform;
            var orderMut = settings != null ? settings.orderMutation : OrderMutationType.Twors;

            var result = ScheduleOptimizer.Optimize(jobs, popSize, gens, crossProb, mutProb, orderCross, bitCross, orderMut);
            var (order, inspections, delayCount, inspectionCount) = result;
            var sequence = order.Select(i => jobs[i].Name).ToArray();

            Debug.Log($"Optimized order: {string.Join(",", sequence)}");
            Debug.Log($"Delay count: {delayCount}, inspection count: {inspectionCount}");
            WriteResultsToCsv(sequence, delayCount, inspectionCount);

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
            if (!File.Exists(csvPath))
            {
                File.WriteAllText(csvPath, "Sequence,DelayCount,InspectionCount\n");
            }

            var sequenceValue = string.Join(",", sequence);
            var line = $"\"{sequenceValue}\",{delayCount},{inspectionCount}\n";
            File.AppendAllText(csvPath, line);
            Debug.Log($"Results written to {csvPath}");
        }
    }
}

