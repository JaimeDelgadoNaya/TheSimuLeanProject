using System.Collections.Generic;
using System.Linq;
using ChapasGA.GA.Optimization;
using ChapasGA.GA.Adapters;
using ChapasGA.IO;
using ChapasGA.Models;
using UnityEngine;
using SimuLean.Unity;
using SimuLean.Serialization;

namespace ChapasGA.Mono
{
    /// <summary>
    /// Unity controller for running GA optimization synchronously (blocking).
    /// Uses the generic optimization framework with Chapa data.
    /// Useful for testing and batch processing.
    /// </summary>
    public class SimulationGAController : MonoBehaviour
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
        
        [Header("Performance")]
        [Tooltip("Enable parallel fitness evaluation (faster, uses multiple CPU cores)")]
        [SerializeField] private bool enableParallelEvaluation = false;
        [Tooltip("Maximum number of parallel threads (0 = use all CPU cores)")]
        [SerializeField] private int maxParallelThreads = 0;

        [Header("Model Extraction")]
        [Tooltip("Raíz del modelo de simulación en Unity")]
        [SerializeField] private GameObject modelRoot;

        private List<Chapa> _chapas;
        private readonly ExcelChapaLoader _loader = new ExcelChapaLoader();
        private readonly CsvResultWriter _writer = new CsvResultWriter();
        private string _csvPath = string.Empty;

        private GenericOptimizer<Chapa> _optimizer;
        private SimulationConfig _modelConfig;

        // Public properties for accessing results
        public double BestFitness => _optimizer?.BestFitness ?? 0;
        public int TotalInspections => _optimizer?.TotalInspections ?? 0;
        public int TotalDelays => _optimizer?.TotalDelays ?? 0;
        public IList<int> BestOrder => _optimizer?.BestSequence;
        public int[] BestInspectionBits => _optimizer?.BestBinaryDecisions;
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
            UnityModelExtractor extractor = null;
            
            try
            {
                var extractorGO = new GameObject("TempModelExtractor");
                extractor = extractorGO.AddComponent<UnityModelExtractor>();
                extractor.modelRoot = modelRoot;
                
                _modelConfig = extractor.ExtractConfiguration();
                
                Debug.Log($"[SimulationGAController] Model extracted: {_modelConfig.Elements.Count} elements, {_modelConfig.Connections.Count} connections");
                
                DestroyImmediate(extractorGO);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SimulationGAController] Error extracting model: {ex.Message}");
                if (extractor != null && extractor.gameObject != null)
                    DestroyImmediate(extractor.gameObject);
                throw;
            }
        }

        public void RunGA()
        {
            if (_chapas == null || _chapas.Count == 0)
            {
                Debug.LogWarning("[SimulationGAController] No chapas loaded.");
                return;
            }

            if (_modelConfig == null)
            {
                Debug.LogError("[SimulationGAController] Model not extracted! Call ExtractModel() before RunGA().");
                return;
            }

            Debug.Log($"[SimulationGAController] Starting GA with {_chapas.Count} chapas, {populationSize} population, {generations} generations");

            if (dryRun)
            {
                Debug.Log("[SimulationGAController] DRY RUN MODE - Testing single evaluation");
                RunDryRunTest();
            }
            else
            {
                // Create transformer and evaluator
                var transformer = new ChapaDataTransformer();
                var evaluator = new GenericSimulationEvaluator<Chapa>(_modelConfig, transformer);
                
                // Create generic optimizer
                _optimizer = new GenericOptimizer<Chapa>(
                    evaluator, 
                    sequenceLength: _chapas.Count,  // Optimize sequence (order)
                    binaryLength: _chapas.Count     // Optimize binary decisions (inspections)
                );
                
                // Configure parallel evaluation
                _optimizer.EnableParallelEvaluation = enableParallelEvaluation;
                if (maxParallelThreads > 0)
                {
                    _optimizer.MaxDegreeOfParallelism = maxParallelThreads;
                }
                
                // Setup logging
                _optimizer.LogCallback = (message) => 
                {
                    if (logToConsole || message.Contains("Gen "))
                        Debug.Log(message);
                };

                // Run optimization (blocks until complete)
                _optimizer.Optimize(_chapas, populationSize, generations, crossoverProb, mutationProb);
                
                Debug.Log($"[SimulationGAController] GA Completed: BestFitness={BestFitness:F2}, Inspections={TotalInspections}, Delays={TotalDelays}");
            }

            if (logToConsole)
            {
                Debug.Log($"BestFitness {BestFitness} TotalInspections {TotalInspections} TotalDelays {TotalDelays}");
            }
        }

        private void RunDryRunTest()
        {
            // Test a single evaluation with original order
            var transformer = new ChapaDataTransformer();
            var evaluator = new GenericSimulationEvaluator<Chapa>(_modelConfig, transformer);
            
            int[] originalOrder = Enumerable.Range(0, _chapas.Count).ToArray();
            int[] noInspections = new int[_chapas.Count];
            
            var metrics = evaluator.RunSimulation(_chapas, originalOrder, noInspections);
            double fitness = evaluator.CalculateFitness(metrics);
            
            Debug.Log($"[SimulationGAController] DRY RUN Results:");
            Debug.Log($"  Fitness: {fitness:F2}");
            Debug.Log($"  Inspections: {metrics.TotalInspections}");
            Debug.Log($"  Delays: {metrics.TotalDelays}");
            Debug.Log($"  Simulation Time: {metrics.SimulationTime:F2}s");
        }

        public void ExportCSV()
        {
            if (_chapas == null || BestOrder == null)
            {
                Debug.LogWarning("[SimulationGAController] No results to export.");
                return;
            }
            
            // Create dummy completion times (not currently tracked)
            double[] completionTimes = new double[_chapas.Count];
            
            _csvPath = _writer.WriteResult(
                "resultado_optimizacion.csv", 
                _chapas, 
                BestOrder, 
                BestInspectionBits, 
                completionTimes, 
                TotalInspections, 
                TotalDelays);
            
            if (logToConsole)
            {
                Debug.Log($"BestFitness {BestFitness} TotalInspections {TotalInspections} TotalDelays {TotalDelays} CSV: {_csvPath}");
            }
            
            Debug.Log($"[SimulationGAController] Results exported to: {_csvPath}");
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
                return;
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
                Debug.LogWarning("modelRoot is not assigned in Inspector!");
                
            }
            ExtractModel();
            Debug.Log($"✅ Extracted {_modelConfig.Elements.Count} elements, {_modelConfig.Connections.Count} connections");

            // 3. Run GA (with small parameters for testing)
            Debug.Log("\n[Step 3/4] Running GA...");
            int originalPopSize = populationSize;
            int originalGens = generations;
            
            RunGA();
            
            populationSize = originalPopSize;
            generations = originalGens;
            
            Debug.Log($"✅ GA Completed:");
            Debug.Log($"   - Best Fitness: {BestFitness:F2}");
            Debug.Log($"   - Total Inspections: {TotalInspections}");
            Debug.Log($"   - Total Delays: {TotalDelays}");
            if (BestOrder != null)
                Debug.Log($"   - Best Order: {string.Join(", ", BestOrder)}");

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
                    return;
                }
                ExtractModel();
            }

            Debug.Log($"\n[Test] Running simulation with original order...");
            
            // Create evaluator and run single simulation
            var transformer = new ChapaDataTransformer();
            var evaluator = new GenericSimulationEvaluator<Chapa>(_modelConfig, transformer);
            
            int[] originalOrder = Enumerable.Range(0, _chapas.Count).ToArray();
            int[] noInspections = new int[_chapas.Count];
            
            var metrics = evaluator.RunSimulation(_chapas, originalOrder, noInspections);
            double fitness = evaluator.CalculateFitness(metrics);
            
            Debug.Log($"✅ Simulation completed:");
            Debug.Log($"   - Items Processed: {metrics.TotalItems}");
            Debug.Log($"   - Inspections: {metrics.TotalInspections}");
            Debug.Log($"   - Delays: {metrics.TotalDelays}");
            Debug.Log($"   - Simulation Time: {metrics.SimulationTime:F2}s");
            Debug.Log($"   - Fitness: {fitness:F2}");

            Debug.Log("\n========== TEST COMPLETED ==========");
        }

        [ContextMenu("Test: Dry Run Evaluation")]
        public void TestDryRun()
        {
            Debug.Log("========== DRY RUN TEST ==========");
            
            if (_chapas == null || _chapas.Count == 0)
            {
                LoadExcel();
            }

            if (_modelConfig == null)
            {
                if (modelRoot == null)
                {
                    Debug.LogWarning("modelRoot is not assigned!");
                    return;
                }
                ExtractModel();
            }

            bool originalDryRun = dryRun;
            dryRun = true;
            
            RunGA();
            
            dryRun = originalDryRun;
            
            Debug.Log("\n========== DRY RUN COMPLETED ==========");
        }
    }
}
