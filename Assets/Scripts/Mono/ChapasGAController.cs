using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ChapasGA.Models;
using ChapasGA.IO;
using ChapasGA.GA;

namespace ChapasGA.Mono
{
    public class ChapasGAController : MonoBehaviour
    {
        [SerializeField] private string excelFileName = "Llegada_Chapas.xlsx";
        [SerializeField] private int populationSize = 100;
        [SerializeField] private int generations = 500;
        [SerializeField] private float crossoverProb = 0.9f;
        [SerializeField] private float mutationProb = 0.15f;
        [SerializeField] private bool logToConsole = false;
        [SerializeField] private bool dryRun = false;

        private List<Chapa> _chapas;
        private GARunResult _result;

        public double BestFitness => _result?.BestFitness ?? 0;
        public int TotalInspections => _result?.TotalInspections ?? 0;
        public int TotalDelays => _result?.TotalDelays ?? 0;

        public void LoadExcel()
        {
            var loader = new ExcelChapaLoader();
            _chapas = loader.LoadFromStreamingAssets(excelFileName);
            if (logToConsole) Debug.Log($"Loaded {_chapas.Count} chapas.");
            if (dryRun)
            {
                var chrom = ChapaChromosome.CreateFromData(_chapas);
                var fitness = new ChapaFitness(_chapas);
                var eval = fitness.EvaluateWithStats(chrom);
                _result = new GARunResult
                {
                    BestFitness = eval.Fitness,
                    Order = chrom.Order,
                    Inspect = chrom.Inspect,
                    CompletionTimes = eval.CompletionTimes,
                    TotalInspections = eval.TotalInspections,
                    TotalDelays = eval.TotalDelays
                };
                if (logToConsole)
                {
                    Debug.Log($"DryRun -> Fitness: {_result.BestFitness}, Inspections: {_result.TotalInspections}, Delays: {_result.TotalDelays}");
                }
            }
        }

        public void RunGA()
        {
            if (_chapas == null || _chapas.Count == 0)
            {
                Debug.LogWarning("No chapas loaded.");
                return;
            }
            if (dryRun)
            {
                Debug.LogWarning("DryRun active; GA not executed.");
                return;
            }
            var runner = new ChapaGARunner(_chapas);
            _result = runner.Run(populationSize, generations, crossoverProb, mutationProb, logToConsole);
            if (logToConsole)
            {
                Debug.Log($"BestFitness: {_result.BestFitness}, Inspections: {_result.TotalInspections}, Delays: {_result.TotalDelays}");
            }
        }

        public void ExportCSV()
        {
            if (_result == null)
            {
                Debug.LogWarning("No result to export.");
                return;
            }
            string path = Path.Combine(Application.persistentDataPath, "resultado_optimizacion.csv");
            var writer = new CsvResultWriter();
            writer.Write(path, _chapas, _result.Order, _result.Inspect, _result.CompletionTimes, _result.TotalInspections, _result.TotalDelays);
            if (logToConsole)
            {
                Debug.Log($"CSV exported to {path}");
            }
        }
    }
}
