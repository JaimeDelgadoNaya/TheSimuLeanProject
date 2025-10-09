using UnityEngine;
using UnityEngine.UI;
using ChapasGA.GA;
using ChapasGA.IO;
using ChapasGA.Models;
using System.Collections.Generic;
using SimuLean.Unity;

namespace ChapasGA.Mono
{
    /// <summary>
    /// Controlador para ejecutar GA de forma asíncrona con UI de progreso
    /// </summary>
    public class AsyncGAController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private string excelFileName = "Llegada_Chapas.xlsx";
        [SerializeField] private GameObject modelRoot;

        [Header("GA Parameters")]
        [SerializeField] private int populationSize = 50;
        [SerializeField] private int generations = 100;
        [SerializeField] private float crossoverProb = 0.9f;
        [SerializeField] private float mutationProb = 0.15f;

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

        private AsyncGARunner runner;
        private ExcelChapaLoader loader = new ExcelChapaLoader();
        private List<Chapa> chapas;
        private List<float> fitnessHistory = new List<float>();
        private bool isRunning;
        private UnityMainThreadDispatcher dispatcher;

        private void Awake()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            // CRITICAL: Initialize dispatcher on main thread BEFORE any async operations
            dispatcher = UnityMainThreadDispatcher.Instance();

            if (startButton != null)
                startButton.onClick.AddListener(StartGAOptimization);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(CancelGA);

            if (progressPanel != null)
                progressPanel.SetActive(false);
        }

        private void OnEnable()
        {
            // Ensure dispatcher is available when component is enabled (especially in Edit mode)
            if (dispatcher == null)
            {
                dispatcher = UnityMainThreadDispatcher.Instance();
            }
        }

        public async void StartGAOptimization()
        {
            // Ensure dispatcher is initialized (especially important in Edit mode)
            if (dispatcher == null)
            {
                dispatcher = UnityMainThreadDispatcher.Instance();
            }

            if (isRunning)
            {
                Debug.LogWarning("[AsyncGAController] GA is already running!");
                return;
            }

            Debug.Log("[AsyncGAController] Starting GA optimization...");

            // IMPORTANTE: Todo esto se ejecuta en MAIN THREAD antes del await
            
            // 1. Load data
            if (chapas == null || chapas.Count == 0)
            {
                chapas = loader.LoadFromStreamingAssets(excelFileName);
                Debug.Log($"[AsyncGAController] Loaded {chapas.Count} chapas");
            }


            // Extraer modelo completamente ANTES de iniciar el Task
            UnityModelExtractor extractor = null;
            SimuLean.Serialization.SimulationConfig config = null;
            
            try
            {
                // Crear extractor temporal
                var extractorGO = new GameObject("TempExtractor");
                extractor = extractorGO.AddComponent<UnityModelExtractor>();
                extractor.modelRoot = modelRoot;
                
                // Extraer configuración (esto DEBE completarse en Main Thread)
                config = extractor.ExtractConfiguration();
                
                Debug.Log($"[AsyncGAController] Extracted {config.Elements.Count} elements, {config.Connections.Count} connections");
                
                // Destruir extractor inmediatamente
                DestroyImmediate(extractorGO);
                extractor = null;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AsyncGAController] Error extracting model: {ex.Message}");
                if (extractor != null && extractor.gameObject != null)
                {
                    DestroyImmediate(extractor.gameObject);
                }
                return;
            }

            // Verificar que la configuración se extrajo correctamente
            if (config == null || config.Elements.Count == 0)
            {
                Debug.LogError("[AsyncGAController] Failed to extract model configuration!");
                return;
            }

            // 3. Setup runner (en Main Thread)
            runner = new AsyncGARunner();
            runner.SetModelConfig(config);  // Pasar la configuración ya extraída
            
            // Setup thread-safe logging callback
            runner.LogCallback = (message) =>
            {
                // This will be called from background thread, enqueue to main thread
                // Use different log levels based on message content
                dispatcher.Enqueue(() =>
                {
                    if (message.Contains("Error") || message.Contains("Failed"))
                    {
                        Debug.LogError(message);
                    }
                    else if (message.Contains("Warning"))
                    {
                        Debug.LogWarning(message);
                    }
                    else if (message.Contains("Gen ") && message.Contains("Fitness"))
                    {
                        // Highlight generation progress with color in editor
                        Debug.Log($"<color=cyan>{message}</color>");
                    }
                    else
                    {
                        Debug.Log(message);
                    }
                });
            };
            
            // Subscribe to events with dispatcher already captured on main thread
            runner.ProgressChanged += OnProgressChanged;
            runner.Completed += OnCompleted;

            // 4. Show UI
            isRunning = true;
            fitnessHistory.Clear();
            
            if (progressPanel != null)
                progressPanel.SetActive(true);

            if (startButton != null)
                startButton.interactable = false;

            if (cancelButton != null)
                cancelButton.interactable = true;

            Debug.Log("[AsyncGAController] Starting async GA execution...");

            // 5. Run GA asynchronously (AHORA SÍ, background thread)
            // El config ya está completamente extraído, no hay más llamadas a Unity
            try
            {
                await runner.RunGAAsync(chapas, populationSize, generations, crossoverProb, mutationProb);
                Debug.Log("[AsyncGAController] GA async execution completed successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AsyncGAController] Error during GA execution: {ex.Message}");
                Debug.LogError($"Stack trace: {ex.StackTrace}");
                ResetUI();
            }
            finally
            {
                // Unsubscribe from events
                if (runner != null)
                {
                    runner.ProgressChanged -= OnProgressChanged;
                    runner.Completed -= OnCompleted;
                }
            }
        }

        private void OnProgressChanged(GAProgressEventArgs e)
        {
            // Este método se llama desde background thread, usar dispatcher capturado
            dispatcher.Enqueue(() =>
            {
                UpdateUI(e);
                UpdateChart((float)e.BestFitness);
            });
        }

        private void OnCompleted(GACompletedEventArgs e)
        {
            // Este método se llama desde background thread, usar dispatcher capturado
            dispatcher.Enqueue(() =>
            {
                if (e.Success)
                {
                    Debug.Log($"[AsyncGAController] GA Completed!");
                    Debug.Log($"  Best Fitness: {e.BestFitness:F2}");
                    Debug.Log($"  Inspections: {e.TotalInspections}");
                    Debug.Log($"  Delays: {e.TotalDelays}");
                    Debug.Log($"  Time: {e.TotalTime:F1}s");
                }
                else
                {
                    Debug.LogError($"[AsyncGAController] GA Failed: {e.Error}");
                }

                ResetUI();
            });
        }

        private void UpdateUI(GAProgressEventArgs e)
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

        private void ResetUI()
        {
            isRunning = false;

            if (progressPanel != null)
                progressPanel.SetActive(false);

            if (startButton != null)
                startButton.interactable = true;

            if (cancelButton != null)
                cancelButton.interactable = false;
        }

        public void CancelGA()
        {
            if (runner != null && isRunning)
            {
                Debug.Log("[AsyncGAController] Cancelling GA...");
                runner.Cancel();
            }
        }

        private void OnDestroy()
        {
            CancelGA();
            
            // Clean up event subscriptions
            if (runner != null)
            {
                runner.ProgressChanged -= OnProgressChanged;
                runner.Completed -= OnCompleted;
                runner.LogCallback = null;
            }

#if UNITY_EDITOR
            // In Edit mode, if this was the last component using the dispatcher, clean it up
            if (!Application.isPlaying)
            {
                // Note: Only dispose if you're sure no other components are using it
                // For safety, we'll just log here
                Debug.Log("[AsyncGAController] Component destroyed in Edit mode. Dispatcher remains active for other potential users.");
            }
#endif
        }

        private void OnDisable()
        {
            // Cancel any running GA when component is disabled
            if (isRunning)
            {
                Debug.Log("[AsyncGAController] Component disabled while GA is running. Cancelling...");
                CancelGA();
            }
        }

        [ContextMenu("Test: Run Async GA")]
        public void TestAsyncGA()
        {
            StartGAOptimization();
        }

#if UNITY_EDITOR
        [ContextMenu("Debug: Check Dispatcher Queue")]
        private void DebugCheckQueue()
        {
            if (dispatcher != null)
            {
                int queueSize = dispatcher.GetQueueSize();
                Debug.Log($"[AsyncGAController] Dispatcher queue size: {queueSize}");
            }
            else
            {
                Debug.LogWarning("[AsyncGAController] Dispatcher not initialized");
            }
        }

        [ContextMenu("Debug: Clear Dispatcher Queue")]
        private void DebugClearQueue()
        {
            if (dispatcher != null)
            {
                dispatcher.ClearQueue();
                Debug.Log("[AsyncGAController] Dispatcher queue cleared");
            }
        }
#endif
    }
}
