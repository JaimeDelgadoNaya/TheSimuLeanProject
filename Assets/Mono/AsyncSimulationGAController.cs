using UnityEngine;
using UnityEngine.UI;
using ChapasGA.GA.Optimization;
using ChapasGA.GA.Adapters;
using ChapasGA.IO;
using ChapasGA.Models;
using System.Collections.Generic;
using SimuLean.Unity;

namespace ChapasGA.Mono
{
    /// <summary>
    /// Unity controller for running GA optimization asynchronously with UI feedback.
    /// Uses the generic optimization framework with Chapa data.
    /// </summary>
    public class AsyncSimulationGAController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private string excelFileName = "Llegada_Chapas.xlsx";
        [SerializeField] private GameObject modelRoot;

        [Header("GA Parameters")]
        [SerializeField] private int populationSize = 50;
        [SerializeField] private int generations = 100;
        [SerializeField] private float crossoverProb = 0.9f;
        [SerializeField] private float mutationProb = 0.15f;
        
        [Header("Performance")]
        [Tooltip("Enable parallel fitness evaluation (faster, uses multiple CPU cores)")]
        [SerializeField] private bool enableParallelEvaluation = false;
        [Tooltip("Maximum number of parallel threads (0 = use all CPU cores)")]
        [SerializeField] private int maxParallelThreads = 0;

        [Header("UI References")]
        [SerializeField] private GameObject progressPanel;
        [SerializeField] private Text generationText;
        [SerializeField] private Text fitnessText;
        [SerializeField] private Text inspectionsText;
        [SerializeField] private Text delaysText;
        [SerializeField] private Text timeText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Button startButton;
        [SerializeField] private Button cancelButton;

        [Header("Chart")]
        [SerializeField] private LineRenderer fitnessChart;
        [SerializeField] private float chartWidth = 5f;
        [SerializeField] private float chartHeight = 3f;

        private GenericOptimizer<Chapa> optimizer;
        private ExcelChapaLoader loader = new ExcelChapaLoader();
        private List<Chapa> chapas;
        private List<float> fitnessHistory = new List<float>();
        private bool isRunning;
        private UnityMainThreadDispatcher dispatcher;

        private void Awake()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            dispatcher = UnityMainThreadDispatcher.Instance();

            if (startButton != null)
                startButton.onClick.AddListener(StartOptimization);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(CancelOptimization);

            if (progressPanel != null)
                progressPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (dispatcher == null)
                dispatcher = UnityMainThreadDispatcher.Instance();
        }

        public async void StartOptimization()
        {
            if (dispatcher == null)
                dispatcher = UnityMainThreadDispatcher.Instance();

            if (isRunning)
            {
                Debug.LogWarning("[AsyncSimulationGAController] Optimization is already running!");
                return;
            }

            Debug.Log("[AsyncSimulationGAController] Starting optimization...");

            // Load data
            if (chapas == null || chapas.Count == 0)
            {
                chapas = loader.LoadFromStreamingAssets(excelFileName);
                Debug.Log($"[AsyncSimulationGAController] Loaded {chapas.Count} chapas");
            }

            // Extract model configuration
            var config = ExtractModelConfiguration();
            if (config == null)
                return;

            // Create transformer and evaluator
            var transformer = new ChapaDataTransformer();
            var evaluator = new GenericSimulationEvaluator<Chapa>(config, transformer);
            
            // Create generic optimizer
            optimizer = new GenericOptimizer<Chapa>(
                evaluator,
                sequenceLength: chapas.Count,  // Optimize sequence (order)
                binaryLength: chapas.Count     // Optimize binary decisions (inspections)
            );
            
            // Configure parallel evaluation
            optimizer.EnableParallelEvaluation = enableParallelEvaluation;
            if (maxParallelThreads > 0)
            {
                optimizer.MaxDegreeOfParallelism = maxParallelThreads;
            }
            
            // Setup logging
            optimizer.LogCallback = (message) =>
            {
                dispatcher.Enqueue(() =>
                {
                    if (message.Contains("Error")) Debug.LogError(message);
                    else if (message.Contains("Warning")) Debug.LogWarning(message);
                    else if (message.Contains("Gen ")) Debug.Log($"<color=cyan>{message}</color>");
                    else Debug.Log(message);
                });
            };
            
            // Subscribe to events
            optimizer.ProgressChanged += OnProgressChanged;
            optimizer.Completed += OnCompleted;

            // Show UI
            isRunning = true;
            fitnessHistory.Clear();
            ShowProgressUI();

            Debug.Log("[AsyncSimulationGAController] Starting async optimization...");

            // Run optimization
            try
            {
                await optimizer.OptimizeAsync(chapas, populationSize, generations, crossoverProb, mutationProb);
                Debug.Log("[AsyncSimulationGAController] Optimization completed successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AsyncSimulationGAController] Error: {ex.Message}\n{ex.StackTrace}");
                HideProgressUI();
            }
            finally
            {
                if (optimizer != null)
                {
                    optimizer.ProgressChanged -= OnProgressChanged;
                    optimizer.Completed -= OnCompleted;
                }
            }
        }

        private SimuLean.Serialization.SimulationConfig ExtractModelConfiguration()
        {
            UnityModelExtractor extractor = null;
            
            try
            {
                var extractorGO = new GameObject("TempExtractor");
                extractor = extractorGO.AddComponent<UnityModelExtractor>();
                extractor.modelRoot = modelRoot;
                
                var config = extractor.ExtractConfiguration();
                Debug.Log($"[AsyncSimulationGAController] Extracted {config.Elements.Count} elements, {config.Connections.Count} connections");
                
                DestroyImmediate(extractorGO);
                return config;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AsyncSimulationGAController] Error extracting model: {ex.Message}");
                if (extractor != null && extractor.gameObject != null)
                    DestroyImmediate(extractor.gameObject);
                return null;
            }
        }

        private void OnProgressChanged(OptimizationProgressEventArgs e)
        {
            dispatcher.Enqueue(() =>
            {
                UpdateUI(e);
                UpdateChart((float)e.BestFitness);
            });
        }

        private void OnCompleted(OptimizationCompletedEventArgs e)
        {
            dispatcher.Enqueue(() =>
            {
                if (e.Success)
                {
                    Debug.Log($"[AsyncSimulationGAController] Optimization Completed!");
                    Debug.Log($"  Best Fitness: {e.BestFitness:F2}");
                    Debug.Log($"  Inspections: {e.TotalInspections}");
                    Debug.Log($"  Delays: {e.TotalDelays}");
                    Debug.Log($"  Time: {e.TotalTime:F1}s");
                }
                else
                {
                    Debug.LogError($"[AsyncSimulationGAController] Optimization Failed: {e.Error}");
                }

                HideProgressUI();
            });
        }

        private void UpdateUI(OptimizationProgressEventArgs e)
        {
            if (generationText != null)
                generationText.text = $"Generation: {e.CurrentGeneration}/{e.TotalGenerations}";

            if (fitnessText != null)
                fitnessText.text = $"Best Fitness: {e.BestFitness:F2}";

            if (inspectionsText != null)
                inspectionsText.text = $"Inspections: {e.Inspections}";

            if (delaysText != null)
                delaysText.text = $"Delays: {e.Delays}";

            if (timeText != null)
                timeText.text = $"Time: {e.ElapsedSeconds:F1}s";

            if (progressBar != null)
                progressBar.value = (float)e.CurrentGeneration / e.TotalGenerations;
        }

        private void UpdateChart(float fitness)
        {
            fitnessHistory.Add(fitness);

            if (fitnessChart == null || fitnessHistory.Count < 2)
                return;

            fitnessChart.positionCount = fitnessHistory.Count;

            float minFitness = float.MaxValue;
            float maxFitness = float.MinValue;
            
            foreach (var f in fitnessHistory)
            {
                if (f < minFitness) minFitness = f;
                if (f > maxFitness) maxFitness = f;
            }

            float range = Mathf.Max(maxFitness - minFitness, 1f);

            for (int i = 0; i < fitnessHistory.Count; i++)
            {
                float x = (float)i / (fitnessHistory.Count - 1) * chartWidth;
                float normalizedFitness = (fitnessHistory[i] - minFitness) / range;
                float y = normalizedFitness * chartHeight;
                
                fitnessChart.SetPosition(i, new Vector3(x, y, 0));
            }
        }

        private void ShowProgressUI()
        {
            if (progressPanel != null)
                progressPanel.SetActive(true);

            if (startButton != null)
                startButton.interactable = false;

            if (cancelButton != null)
                cancelButton.interactable = true;
        }

        private void HideProgressUI()
        {
            isRunning = false;

            if (progressPanel != null)
                progressPanel.SetActive(false);

            if (startButton != null)
                startButton.interactable = true;

            if (cancelButton != null)
                cancelButton.interactable = false;
        }

        public void CancelOptimization()
        {
            if (optimizer != null && isRunning)
            {
                Debug.Log("[AsyncSimulationGAController] Cancelling optimization...");
                optimizer.Cancel();
            }
        }

        private void OnDestroy()
        {
            CancelOptimization();
            
            if (optimizer != null)
            {
                optimizer.ProgressChanged -= OnProgressChanged;
                optimizer.Completed -= OnCompleted;
                optimizer.LogCallback = null;
            }
        }

        private void OnDisable()
        {
            if (isRunning)
            {
                Debug.Log("[AsyncSimulationGAController] Component disabled while optimization is running. Cancelling...");
                CancelOptimization();
            }
        }

        [ContextMenu("Test: Run Optimization")]
        public void TestOptimization()
        {
            StartOptimization();
        }

#if UNITY_EDITOR
        [ContextMenu("Debug: Check Dispatcher Queue")]
        private void DebugCheckQueue()
        {
            if (dispatcher != null)
                Debug.Log($"[AsyncSimulationGAController] Dispatcher queue size: {dispatcher.GetQueueSize()}");
            else
                Debug.LogWarning("[AsyncSimulationGAController] Dispatcher not initialized");
        }
#endif
    }
}
