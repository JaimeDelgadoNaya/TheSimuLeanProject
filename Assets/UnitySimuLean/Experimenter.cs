using System;
using System.Collections.Generic;
using UnityEngine;
using UnitySimuLean.Utilities;  // Asegúrate de tener FileWriterCSV en este namespace

namespace UnitySimuLean
{
    public class Experimenter : MonoBehaviour
    {
        // 1) Estado global del headless mode
        public static bool HeadlessActive { get; private set; }

        [SerializeField] private float maxTime = 100.0f;
        [SerializeField] private int timeScale = 100;

        [Header("Configuración")]
        [SerializeField] private bool headlessMode = false;

        [System.Serializable]
        public class ListWrapper
        {
            public List<float> myList;
        }

        [Header("Input Variables")]
        [SerializeField] private List<SElement> iElements;
        [SerializeField] private List<string> iParameters;
        [SerializeField] private List<ListWrapper> scenarios;

        private FileWriterCSV fileWriter;

        [Header("Output Variables")]
        [SerializeField] private List<SElement> oElements;
        [SerializeField] private List<string> Variables;

        private int currentScenario = 0;

        #region Events Set Up
        private void OnEnable()
        {
            UnitySimClock.Instance.SimEvents.OnExperimentStart += StartExperiment;
        }

        private void OnDisable()
        {
            UnitySimClock.Instance.SimEvents.OnExperimentStart -= StartExperiment;
        }
        #endregion

        private void Awake()
        {
            // 2) Inicializamos la propiedad estática
            HeadlessActive = headlessMode;
        }

        private void Start()
        {
            // 3) Nombre de fichero único por fecha
            var dateStamp = DateTime.Now.ToString("yyyyMMdd");
            fileWriter = new FileWriterCSV($"OutData-{dateStamp}");
        }

        private void StartExperiment()
        {
            // 4) Actualizamos de nuevo (por si cambió en Play)
            HeadlessActive = headlessMode;

            // 5) Desactivamos todos los componentes visuales ANTES de arrancar la sim
            if (headlessMode)
                DisableVisualComponents();

            // 6) Preparamos el escenario y arrancamos la sim
            currentScenario = 0;
            SetScenario(currentScenario);
            UnitySimClock.Instance.SimEvents.OnSimStart.Invoke(maxTime);
            UnitySimClock.Instance.SetTimeScale(timeScale);

            // 7) Cabecera del CSV
            fileWriter.AddText(new[] { "Instance", "Element", "Variable", "Value" });
            Debug.Log("Experiment Start. Timescale Adjusted");
        }

        /// <summary>
        /// Desactiva todos los MonoBehaviours de los elementos visuales
        /// para evitar Updates, animaciones, Instantiate, Destroy, etc.
        /// </summary>
        private void DisableVisualComponents()
        {
            foreach (SElement comp in UnitySimClock.Instance.Elements)
            {
                if (comp is MonoBehaviour mb)
                    mb.enabled = false;
            }
            Debug.Log("Headless mode: visual components disabled.");
        }

        private void FinishExperiment()
        {
            currentScenario = 0;
            UnitySimClock.Instance.SimEvents.OnExperimentFinish.Invoke();
            UnitySimClock.Instance.SetTimeScale(1);
            Debug.Log("Experiment Finish. Output file created.");
        }

        private bool SetScenario(int nextScenario)
        {
            if (nextScenario >= scenarios.Count)
                return false;

            for (int i = 0; i < iElements.Count; i++)
                SetPropertyValue(iElements[i], iParameters[i], scenarios[nextScenario].myList[i]);

            return true;
        }

        public static void SetPropertyValue(object obj, string propertyName, float value)
        {
            var property = obj.GetType().GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                if (property.PropertyType == typeof(int))
                    property.SetValue(obj, (int)value);
                else if (property.PropertyType == typeof(float))
                    property.SetValue(obj, (float)value);
                else
                    throw new ArgumentException($"Property '{propertyName}' no es int ni float.");
            }
            else
            {
                throw new ArgumentException($"Property '{propertyName}' no encontrada o de sólo lectura.");
            }
        }

        public static int? GetPropertyValue(object obj, string propertyName)
        {
            var property = obj.GetType().GetProperty(propertyName);
            if (property != null && property.CanRead)
            {
                var value = property.GetValue(obj);
                if (value != null)
                {
                    if (property.PropertyType == typeof(int))
                        return (int)value;
                    else if (property.PropertyType == typeof(float))
                        return Mathf.RoundToInt((float)value);
                    else
                        throw new ArgumentException($"Property '{propertyName}' no es int ni float.");
                }
            }
            else
            {
                throw new ArgumentException($"Property '{propertyName}' no encontrada o de sólo escritura.");
            }
            return null;
        }

        internal void NextScenario()
        {
            currentScenario++;
            for (int i = 0; i < oElements.Count; i++)
            {
                var val = GetPropertyValue(oElements[i], Variables[i])?.ToString() ?? "null";
                fileWriter.AddText(new[]
                {
                    (currentScenario - 1).ToString(),
                    oElements[i].name,
                    Variables[i],
                    val
                });
            }

            if (!SetScenario(currentScenario))
                FinishExperiment();
            else
                UnitySimClock.Instance.SimEvents.OnSimStart.Invoke(maxTime);
        }
    }
}
