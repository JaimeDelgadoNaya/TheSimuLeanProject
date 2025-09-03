using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using GeneticSharp.Domain;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace ChapasGA
{
    /// <summary>
    /// Provides loading from Excel, running the GA and exporting results.
    /// </summary>
    public static class ChapaOptimizer
    {
        public static List<ChapaData> LoadExcel(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Excel file not found: {path}");
            }

            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            IWorkbook workbook = new XSSFWorkbook(fs);
            var sheet = workbook.GetSheetAt(0);
            var headerRow = sheet.GetRow(0) ?? throw new Exception("Sheet is empty");

            var headers = new Dictionary<string, int>();
            for (int c = 0; c < headerRow.LastCellNum; c++)
            {
                headers[headerRow.GetCell(c)?.ToString() ?? string.Empty] = c;
            }
            string[] required = new[] { "Time", "Name", "Q", "nRefuerzos", "Referencia", "tSoldadura", "tInspeccion", "inspeccionOn", "DueDate", "priorities" };
            foreach (var r in required)
            {
                if (!headers.ContainsKey(r))
                {
                    throw new Exception($"Missing required column: {r}");
                }
            }

            var list = new List<ChapaData>();
            for (int r = 1; r <= sheet.LastRowNum; r++)
            {
                var row = sheet.GetRow(r);
                if (row == null) continue;
                var chapa = new ChapaData
                {
                    Name = row.GetCell(headers["Name"]).ToString(),
                    SoldaduraTime = row.GetCell(headers["tSoldadura"]).NumericCellValue,
                    InspeccionTime = row.GetCell(headers["tInspeccion"]).NumericCellValue,
                    InspeccionOn = (int)row.GetCell(headers["inspeccionOn"]).NumericCellValue,
                    DueDate = row.GetCell(headers["DueDate"]).NumericCellValue
                };
                list.Add(chapa);
            }
            return list;
        }

        public static void DryRun(List<ChapaData> chapas)
        {
            Console.WriteLine($"Dry run: {chapas.Count} chapas loaded.");
            double C = 0;
            for (int i = 0; i < chapas.Count; i++)
            {
                var ch = chapas[i];
                bool inspect = ch.InspeccionOn == 1;
                double proc = ch.SoldaduraTime + (inspect ? ch.InspeccionTime : 0);
                C += proc;
                Console.WriteLine($"{i}:{ch.Name} -> C={C}");
            }
        }

        public static (ChapaChromosome best, double fitness, int inspections, int delays, double[] completionTimes) RunGA(List<ChapaData> chapas, int generations, int popSize)
        {
            int n = chapas.Count;
            var mandatory = chapas.Select(c => c.InspeccionOn == 1).ToArray();
            var chromosome = new ChapaChromosome(n, mandatory);
            var population = new Population(Math.Min(60, popSize), popSize, chromosome);
            var fitness = new ChapaFitness(chapas);
            var selection = new TournamentSelection();
            var crossover = new ChapaCrossover();
            var mutation = new ChapaMutation(mandatory);
            var ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation)
            {
                Termination = new GenerationNumberTermination(generations),
                MutationProbability = 0.15f,
                CrossoverProbability = 0.9f
            };
            ga.Start();
            var best = (ChapaChromosome)ga.BestChromosome;
            // Evaluate best to get counts and completion times
            var order = best.GetOrder();
            var inspect = best.GetInspections();
            var completion = new double[n];
            double C = 0;
            int numInspect = 0;
            int numLate = 0;
            for (int pos = 0; pos < n; pos++)
            {
                int idx = order[pos];
                var ch = chapas[idx];
                bool doInspect = ch.InspeccionOn == 1 || inspect[idx];
                double proc = ch.SoldaduraTime + (doInspect ? ch.InspeccionTime : 0);
                C += proc;
                completion[idx] = C;
                if (doInspect) numInspect++;
                if (C > ch.DueDate) numLate++;
            }
            return (best, ga.BestChromosome.Fitness ?? double.NaN, numInspect, numLate, completion);
        }

        public static void SaveCsv(string path, List<ChapaData> chapas, ChapaChromosome chrom, double[] completion, int totalInspections, int totalDelays)
        {
            using var writer = new StreamWriter(path);
            writer.WriteLine("OrderIndex;Name;DoInspect;CompletionTime;DueDate;IsLate");
            var order = chrom.GetOrder();
            var inspect = chrom.GetInspections();
            for (int pos = 0; pos < order.Length; pos++)
            {
                int idx = order[pos];
                var ch = chapas[idx];
                bool doInspect = ch.InspeccionOn == 1 || inspect[idx];
                double C = completion[idx];
                int isLate = C > ch.DueDate ? 1 : 0;
                writer.WriteLine($"{pos};{ch.Name};{(doInspect ? 1 : 0)};{C.ToString(CultureInfo.InvariantCulture)};{ch.DueDate.ToString(CultureInfo.InvariantCulture)};{isLate}");
            }
            writer.WriteLine($"TotalInspections;{totalInspections}");
            writer.WriteLine($"TotalDelays;{totalDelays}");
        }
    }
}
