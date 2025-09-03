using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ChapasGA
{
    /// <summary>
    /// Unity behaviour wrapper to run the optimizer from the inspector.
    /// </summary>
    public class ChapaOptimizerBehaviour : MonoBehaviour
    {
        [SerializeField] private string excelPath = "Llegada_Chapas.xlsx";
        [SerializeField] private int generations = 500;
        [SerializeField] private int population = 100;
        [SerializeField] private bool dryRun = false;

        private void Start()
        {
            try
            {
                var chapas = ChapaOptimizer.LoadExcel(Path.Combine(Application.dataPath, "..", excelPath));
                if (dryRun)
                {
                    ChapaOptimizer.DryRun(chapas);
                }
                else
                {
                    var (best, fitness, inspections, delays, completion) = ChapaOptimizer.RunGA(chapas, generations, population);
                    var resultPath = Path.Combine(Application.dataPath, "..", "resultado_optimizacion.csv");
                    ChapaOptimizer.SaveCsv(resultPath, chapas, best, completion, inspections, delays);
                    Debug.Log($"BestFitness: {fitness}\nTotalInspections: {inspections}\nTotalDelays: {delays}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }
    }
}
