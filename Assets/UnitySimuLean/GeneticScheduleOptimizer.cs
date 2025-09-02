using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UnitySimuLean
{
    /// <summary>
    /// Performs a simple genetic algorithm search over the order of a schedule
    /// in order to minimise the number of delayed items with respect to their
    /// due dates. The optimiser reads a CSV schedule file and outputs a new
    /// schedule with updated priorities and a summary of delays.
    /// </summary>
    public static class GeneticScheduleOptimizer
    {
        /// <summary>
        /// Optimises the schedule contained in <paramref name="inputCsv"/> and
        /// writes the reordered schedule to <paramref name="scheduleOutput"/> as
        /// well as a delay summary to <paramref name="summaryOutput"/>.
        /// </summary>
        public static void Optimize(string inputCsv, string scheduleOutput, string summaryOutput,
                                    int populationSize = 50, int generations = 100)
        {
            var entries = LoadSchedule(inputCsv);
            int taskCount = entries.Count;

            // Create initial population of random permutations
            var rand = new Random();
            var baseOrder = Enumerable.Range(0, taskCount).ToList();
            var population = new List<List<int>>();
            for (int i = 0; i < populationSize; i++)
            {
                var order = new List<int>(baseOrder);
                Shuffle(order, rand);
                population.Add(order);
            }

            List<int> bestOrder = null;
            int bestFitness = int.MaxValue;

            for (int gen = 0; gen < generations; gen++)
            {
                var scored = new List<(List<int> order, int delays)>();
                foreach (var order in population)
                {
                    int delays = Evaluate(entries, order);
                    scored.Add((order, delays));
                    if (delays < bestFitness)
                    {
                        bestFitness = delays;
                        bestOrder = new List<int>(order);
                    }
                }

                // Select the best individuals
                scored.Sort((a, b) => a.delays.CompareTo(b.delays));
                var nextPopulation = new List<List<int>>();
                int elite = Math.Max(1, populationSize / 5);
                for (int i = 0; i < elite; i++)
                    nextPopulation.Add(new List<int>(scored[i].order));

                // Create the rest of the population via crossover and mutation
                while (nextPopulation.Count < populationSize)
                {
                    var parent1 = scored[rand.Next(elite)].order;
                    var parent2 = scored[rand.Next(elite)].order;
                    var child = Crossover(parent1, parent2, rand);
                    Mutate(child, rand);
                    nextPopulation.Add(child);
                }

                population = nextPopulation;
            }

            // Assign new priorities according to best order
            for (int i = 0; i < bestOrder.Count; i++)
                entries[bestOrder[i]].Priority = i + 1;

            // Write reordered schedule and compute final delays
            var scheduleLines = new List<string>();
            scheduleLines.Add("Name,Priority,InspectionOn,Delay");
            int currentTime = 0;
            int totalDelays = 0;
            foreach (var idx in bestOrder)
            {
                var e = entries[idx];
                int start = (int)Math.Max(e.Time, currentTime);
                int duration = (int)e.tSoldadura + (e.inspeccionOn ? (int)e.tInspeccion : 0);
                int completion = start + duration;
                bool delayed = completion > e.DueDate;
                if (delayed) totalDelays++;
                scheduleLines.Add($"{e.Name},{e.Priority},{e.inspeccionOn},{(delayed ? 1 : 0)}");
                currentTime = completion;
            }
            File.WriteAllLines(scheduleOutput, scheduleLines);

            // Write summary file with total delay count
            File.WriteAllLines(summaryOutput, new[] { "Delays", bestFitness.ToString() });
        }

        /// <summary>Represents a single row in the schedule.</summary>
        class ScheduleEntry
        {
            public double Time;
            public string Name;
            public int Q;
            public int nRefuerzos;
            public string Referencia;
            public double tSoldadura;
            public double tInspeccion;
            public bool inspeccionOn;
            public double DueDate;
            public int Priority;
        }

        static List<ScheduleEntry> LoadSchedule(string csvPath)
        {
            var entries = new List<ScheduleEntry>();
            var lines = File.ReadAllLines(csvPath);
            if (lines.Length == 0) return entries;

            var headers = lines[0].Split(',');
            var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
                index[headers[i].Trim()] = i;

            for (int i = 1; i < lines.Length; i++)
            {
                var cols = lines[i].Split(',');
                if (cols.Length != headers.Length) continue;
                var entry = new ScheduleEntry
                {
                    Time = double.Parse(cols[index["Time"]]),
                    Name = cols[index["Name"]],
                    Q = int.Parse(cols[index["Q"]]),
                    nRefuerzos = int.Parse(cols[index["nRefuerzos"]]),
                    Referencia = cols[index["Referencia"]],
                    tSoldadura = double.Parse(cols[index["tSoldadura"]]),
                    tInspeccion = double.Parse(cols[index["tInspeccion"]]),
                    inspeccionOn = cols[index["inspeccionOn"]].Trim().Equals("1", StringComparison.OrdinalIgnoreCase) ||
                                   cols[index["inspeccionOn"]].Trim().Equals("true", StringComparison.OrdinalIgnoreCase),
                    DueDate = double.Parse(cols[index["DueDate"]]),
                    Priority = int.Parse(cols[index["priorities"]])
                };
                entries.Add(entry);
            }
            return entries;
        }

        static int Evaluate(List<ScheduleEntry> entries, List<int> order)
        {
            double currentTime = 0;
            int delays = 0;
            foreach (var idx in order)
            {
                var e = entries[idx];
                currentTime = Math.Max(e.Time, currentTime);
                currentTime += e.tSoldadura;
                if (e.inspeccionOn)
                    currentTime += e.tInspeccion;
                if (currentTime > e.DueDate)
                    delays++;
            }
            return delays;
        }

        static void Shuffle(List<int> list, Random rand)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        static List<int> Crossover(List<int> p1, List<int> p2, Random rand)
        {
            int size = p1.Count;
            var child = Enumerable.Repeat(-1, size).ToList();
            int start = rand.Next(size);
            int end = rand.Next(start, size);
            var used = new HashSet<int>();

            for (int i = start; i <= end; i++)
            {
                child[i] = p1[i];
                used.Add(child[i]);
            }

            int idx = 0;
            for (int i = 0; i < size; i++)
            {
                if (child[i] != -1) continue;
                while (used.Contains(p2[idx])) idx++;
                child[i] = p2[idx];
                used.Add(child[i]);
            }
            return child;
        }

        static void Mutate(List<int> order, Random rand)
        {
            if (rand.NextDouble() < 0.1)
            {
                int a = rand.Next(order.Count);
                int b = rand.Next(order.Count);
                (order[a], order[b]) = (order[b], order[a]);
            }
        }
    }
}

