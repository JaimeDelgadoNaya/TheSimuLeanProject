using ChapaGA.GA;
using ChapaGA.IO;
using GeneticSharp;

namespace ChapaGA;

class Program
{
    static void Main(string[] args)
    {
        string excelPath = Path.Combine(AppContext.BaseDirectory, "Llegada_Chapas.xlsx");
        int generations = 500;
        int population = 100;
        bool dryRun = false;

        // Argument parsing
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--excel":
                    if (i + 1 < args.Length) excelPath = args[++i];
                    break;
                case "--generations":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var g)) generations = g;
                    break;
                case "--pop":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var p)) population = p;
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
            }
        }

        try
        {
            if (!Path.IsPathRooted(excelPath))
            {
                var candidate = Path.Combine(AppContext.BaseDirectory, excelPath);
                if (File.Exists(candidate)) excelPath = candidate;
            }
            var chapas = ExcelReader.ReadChapas(excelPath);

            if (dryRun)
            {
                Console.WriteLine($"Chapas detectadas: {chapas.Count}");
                double C = 0;
                for (int i = 0; i < chapas.Count; i++)
                {
                    var c = chapas[i];
                    bool doInspect = c.InspeccionObligatoria;
                    double proc = c.TSoldadura + (doInspect ? c.TInspeccion : 0);
                    C += proc;
                    Console.WriteLine($"{i}: {c.Name} -> C={C}");
                }
                return;
            }

            bool[] mandatory = chapas.Select(c => c.InspeccionObligatoria).ToArray();
            var chromosome = new ChapaChromosome(chapas.Count, mandatory);
            int minPop = population == 100 ? 60 : population;
            int maxPop = population;
            var populationObj = new Population(minPop, maxPop, chromosome);
            var fitness = new ChapaFitness(chapas);
            var selection = new TournamentSelection();
            var crossover = new ChapaCrossover();
            var mutation = new ChapaMutation();
            var ga = new GeneticAlgorithm(populationObj, fitness, selection, crossover, mutation)
            {
                CrossoverProbability = 0.9f,
                MutationProbability = 0.15f,
                Termination = new GenerationNumberTermination(generations)
            };

            ga.Start();

            var best = (ChapaChromosome)ga.BestChromosome;
            var result = fitness.EvaluateDetailed(best);
            CsvWriter.Write("resultado_optimizacion.csv", result);

            Console.WriteLine($"BestFitness: {result.Fitness}");
            Console.WriteLine($"TotalInspections: {result.TotalInspections}");
            Console.WriteLine($"TotalDelays: {result.TotalDelays}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
