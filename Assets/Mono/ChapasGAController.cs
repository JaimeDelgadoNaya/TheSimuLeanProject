using System.Collections.Generic;
using System.Linq;
using ChapasGA.GA;
using ChapasGA.IO;
using ChapasGA.Models;
using GeneticSharp.Domain.Chromosomes;
using UnityEngine;
using SimuLean.Unity;
using SimuLean.Serialization;

namespace ChapasGA.Mono
{
    public class ChapasGAController : MonoBehaviour
    {
        [Header("Data Source")]
        [SerializeField] private string excelFileName = "Llegada_Chapas.xlsx";

        [Header("GA Parameters")]
        [SerializeField] private int populationSize = 50;
        [SerializeField] private int generations = 10;
        [SerializeField] private float crossoverProb = 0.9f;
        [SerializeField] private float mutationProb = 0.15f;
        [SerializeField] private bool dryRun = false;
        [SerializeField] private bool logToConsole = false;

        [Header("Model Extraction")]
        [Tooltip("Raíz del modelo de simulación en Unity")]
        [SerializeField] private GameObject modelRoot;

        private List<Chapa> _chapas;
        private readonly ExcelChapaLoader _loader = new ExcelChapaLoader();
        private readonly ChapaGARunner _runner = new ChapaGARunner();
        private readonly CsvResultWriter _writer = new CsvResultWriter();
        private string _csvPath = string.Empty;

        private UnityModelExtractor _extractor;
        private SimulationConfig _modelConfig;

        public double BestFitness => _runner.BestFitness;
        public int TotalInspections => _runner.TotalInspections;
        public int TotalDelays => _runner.TotalDelays;
        public string CsvPath => _csvPath;

        private void Awake()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        public void LoadExcel()
        {
            _chapas = _loader.LoadFromStreamingAssets(excelFileName);
        }

        /// <summary>
        /// Extrae la configuración del modelo desde Unity.
        /// </summary>
        public void ExtractModel()
        {
            if (_extractor == null)
            {
                // Crear extractor temporal si no existe
                var extractorGO = new GameObject("TempModelExtractor");
                _extractor = extractorGO.AddComponent<UnityModelExtractor>();
                _extractor.modelRoot = modelRoot;
            }

            _modelConfig = _extractor.ExtractConfiguration();
            _runner.SetModelConfig(_modelConfig);

            Debug.Log($"[ChapasGAController] Model extracted: {_modelConfig.Elements.Count} elements, {_modelConfig.Connections.Count} connections");

            if (_extractor != null && _extractor.gameObject.name == "TempModelExtractor")
            {
                DestroyImmediate(_extractor.gameObject);
            }
        }

        public void RunGA()
        {
            if (_chapas == null || _chapas.Count == 0)
            {
                Debug.LogWarning("[ChapasGAController] No chapas loaded.");
                return;
            }

            if (_modelConfig == null)
            {
                Debug.LogError("[ChapasGAController] Model not extracted! Call ExtractModel() before RunGA().");
                return;
            }

            Debug.Log($"[ChapasGAController] Starting GA with {_chapas.Count} chapas, {populationSize} population, {generations} generations");

            if (dryRun)
            {
                Debug.Log("[ChapasGAController] DRY RUN MODE - Testing single chromosome");
                int n = _chapas.Count;
                var chromo = new ChapaChromosome(n);
                for (int i = 0; i < n; i++)
                {
                    chromo.ReplaceGene(i, new Gene(i));
                }
                chromo.Repair();
                var fitness = new ChapaFitness(_chapas, _modelConfig);
                var details = fitness.EvaluateDetailed(chromo);
                _runner.BestOrder = chromo.GetOrder();
                _runner.BestBits = chromo.GetInspectionBits();
                _runner.CompletionTimes = details.completionTimes;
                _runner.BestFitness = details.fitness;
                _runner.TotalInspections = details.inspections;
                _runner.TotalDelays = details.delays;

                Debug.Log($"[ChapasGAController] DRY RUN Results: Fitness={details.fitness:F2}, Inspections={details.inspections}, Delays={details.delays}");
            }
            else
            {
                _runner.RunGA(_chapas, populationSize, generations, crossoverProb, mutationProb);
                Debug.Log($"[ChapasGAController] GA Completed: BestFitness={_runner.BestFitness:F2}, Inspections={_runner.TotalInspections}, Delays={_runner.TotalDelays}");
            }

            if (logToConsole)
            {
                Debug.Log($"BestFitness {_runner.BestFitness} TotalInspections {_runner.TotalInspections} TotalDelays {_runner.TotalDelays}");
            }
        }

        public void ExportCSV()
        {
            if (_chapas == null || _runner.BestOrder == null)
            {
                Debug.LogWarning("No results to export.");
                return;
            }
            _csvPath = _writer.WriteResult("resultado_optimizacion.csv", _chapas, _runner.BestOrder, _runner.BestBits, _runner.CompletionTimes, _runner.TotalInspections, _runner.TotalDelays);
            if (logToConsole)
            {
                Debug.Log($"BestFitness {_runner.BestFitness} TotalInspections {_runner.TotalInspections} TotalDelays {_runner.TotalDelays} CSV: {_csvPath}");
            }
        }

        // ========== CONTEXT MENU TESTS ==========

        [ContextMenu("Test: Load Excel")]
        public void TestLoadExcel()
        {
            LoadExcel();
            Debug.Log($"✅ Loaded {_chapas.Count} chapas from Excel");
        }

        [ContextMenu("Test: Extract Model from Unity")]
        public void TestExtractModel()
        {
            if (modelRoot == null)
            {
                Debug.LogWarning("modelRoot is not assigned in Inspector!");
            }

            ExtractModel();
            
            Debug.Log($"✅ Model extracted successfully:");
            Debug.Log($"   - Elements: {_modelConfig.Elements.Count}");
            Debug.Log($"   - Connections: {_modelConfig.Connections.Count}");
            
            foreach (var elem in _modelConfig.Elements)
            {
                Debug.Log($"     [{elem.Type}] {elem.Name}");
            }
        }

        [ContextMenu("Test: Full GA Pipeline")]
        public void TestFullGAPipeline()
        {
            Debug.Log("========== FULL GA PIPELINE TEST ==========");
            
            // 1. Load Excel
            Debug.Log("\n[Step 1/4] Loading Excel...");
            LoadExcel();
            Debug.Log($"✅ Loaded {_chapas.Count} chapas");

            // 2. Extract Model
            Debug.Log("\n[Step 2/4] Extracting Model from Unity...");
            if (modelRoot == null)
            {
                Debug.LogWarning("modelRoot is not assigned in Inspector");
            }
            ExtractModel();
            Debug.Log($"✅ Extracted {_modelConfig.Elements.Count} elements, {_modelConfig.Connections.Count} connections");

            // 3. Run GA (with small parameters for testing)
            Debug.Log("\n[Step 3/4] Running GA...");
            int originalPopSize = populationSize;
            int originalGens = generations;
            
            populationSize = 10;  // Small for testing
            generations = 5;       // Small for testing
            
            RunGA();
            
            populationSize = originalPopSize;
            generations = originalGens;
            
            Debug.Log($"✅ GA Completed:");
            Debug.Log($"   - Best Fitness: {BestFitness:F2}");
            Debug.Log($"   - Total Inspections: {TotalInspections}");
            Debug.Log($"   - Total Delays: {TotalDelays}");
            Debug.Log($"   - Best Order: {string.Join(", ", _runner.BestOrder)}");

            // 4. Export CSV
            Debug.Log("\n[Step 4/4] Exporting CSV...");
            ExportCSV();
            Debug.Log($"✅ Exported to: {CsvPath}");

            Debug.Log("\n========== TEST COMPLETED SUCCESSFULLY ==========");
        }

        [ContextMenu("Test: Single Simulation Run")]
        public void TestSingleSimulation()
        {
            Debug.Log("========== SINGLE SIMULATION TEST ==========");
            
            // Load data
            if (_chapas == null || _chapas.Count == 0)
            {
                LoadExcel();
            }

            // Extract model
            if (_modelConfig == null)
            {
                if (modelRoot == null)
                {
                    Debug.LogWarning("modelRoot is not assigned!");
                }
                ExtractModel();
            }

            Debug.Log($"\n[Test] Running simulation with original order...");
            
            // Run simulation with original order
            int[] originalOrder = Enumerable.Range(0, _chapas.Count).ToArray();
            int[] noInspections = new int[_chapas.Count];
            
            var result = _runner.RunSimulationWithConfig(_chapas, originalOrder, noInspections);
            
            Debug.Log($"✅ Simulation completed:");
            Debug.Log($"   - Items Processed: {result.TotalItems}");
            Debug.Log($"   - Inspections: {result.TotalInspections}");
            Debug.Log($"   - Delays: {result.TotalDelays}");
            Debug.Log($"   - Simulation Time: {result.SimulationTime:F2}s");
            Debug.Log($"   - Fitness: {result.CalculateFitness():F2}");

            Debug.Log("\n========== TEST COMPLETED ==========");
        }
    }
}
