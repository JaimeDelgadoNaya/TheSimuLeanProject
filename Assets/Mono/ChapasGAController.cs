using System.Collections.Generic;
using ChapasGA.GA;
using ChapasGA.IO;
using ChapasGA.Models;
using GeneticSharp.Domain.Chromosomes;
using UnityEngine;

namespace ChapasGA.Mono
{
    public class ChapasGAController : MonoBehaviour
    {
        [SerializeField] private string excelFileName = "Llegada_Chapas.xlsx";
        [SerializeField] private int populationSize = 100;
        [SerializeField] private int generations = 500;
        [SerializeField] private float crossoverProb = 0.9f;
        [SerializeField] private float mutationProb = 0.15f;
        [SerializeField] private bool dryRun = false;
        [SerializeField] private bool logToConsole = false;

        private List<Chapa> _chapas;
        private readonly ExcelChapaLoader _loader = new ExcelChapaLoader();
        private readonly ChapaGARunner _runner = new ChapaGARunner();
        private readonly CsvResultWriter _writer = new CsvResultWriter();
        private string _csvPath = string.Empty;

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

        public void RunGA()
        {
            if (_chapas == null || _chapas.Count == 0)
            {
                Debug.LogWarning("No chapas loaded.");
                return;
            }

            if (dryRun)
            {
                int n = _chapas.Count;
                var chromo = new ChapaChromosome(n);
                for (int i = 0; i < n; i++)
                {
                    chromo.ReplaceGene(i, new Gene(i));
                }
                chromo.Repair();
                var fitness = new ChapaFitness(_chapas);
                var details = fitness.EvaluateDetailed(chromo);
                _runner.BestOrder = chromo.GetOrder();
                _runner.BestBits = chromo.GetInspectionBits();
                _runner.CompletionTimes = details.completionTimes;
                _runner.BestFitness = details.fitness;
                _runner.TotalInspections = details.inspections;
                _runner.TotalDelays = details.delays;
            }
            else
            {
                _runner.RunGA(_chapas, populationSize, generations, crossoverProb, mutationProb);
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
    }
}
