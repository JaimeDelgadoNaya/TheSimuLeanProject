using System;
using System.Collections.Generic;
using System.IO;

namespace ChapasGA
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string excelPath = "Llegada_Chapas.xlsx";
            int generations = 500;
            int population = 100;
            bool dryRun = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--excel":
                        excelPath = args[++i];
                        break;
                    case "--generations":
                        generations = int.Parse(args[++i]);
                        break;
                    case "--pop":
                        population = int.Parse(args[++i]);
                        break;
                    case "--dry-run":
                        dryRun = true;
                        break;
                }
            }

            try
            {
                var chapas = ChapaOptimizer.LoadExcel(excelPath);
                if (dryRun)
                {
                    ChapaOptimizer.DryRun(chapas);
                    return;
                }
                var (best, fitness, inspections, delays, completion) = ChapaOptimizer.RunGA(chapas, generations, population);
                ChapaOptimizer.SaveCsv("resultado_optimizacion.csv", chapas, best, completion, inspections, delays);
                Console.WriteLine($"BestFitness: {fitness}");
                Console.WriteLine($"TotalInspections: {inspections}");
                Console.WriteLine($"TotalDelays: {delays}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }
    }
}
