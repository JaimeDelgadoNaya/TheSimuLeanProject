using System;
using System.Collections.Generic;
using System.IO;

namespace UnitySimuLean
{
    /// <summary>
    /// Helper class that reads a schedule file, applies a genetic algorithm to
    /// minimise delays by reordering priorities and writes the optimised
    /// schedule and metrics to CSV files.
    /// </summary>
    public static class SchedulePriorityOptimizer
    {
        /// <summary>
        /// Optimises the schedule found at <paramref name="inputCsv"/> and
        /// writes the updated schedule (with new priorities) to
        /// <paramref name="outputSchedule"/>. A separate CSV containing the
        /// delay and inspection counts is written to
        /// <paramref name="outputStats"/>.
        /// </summary>
        public static void Optimise(string inputCsv, string outputSchedule, string outputStats,
            int generations = 100, int populationSize = 50)
        {
            if (string.IsNullOrEmpty(inputCsv))
            {
                throw new ArgumentException("Input path required", nameof(inputCsv));
            }

            var (headers, dataDict) = LoadSchedule(inputCsv);
            if (!dataDict.ContainsKey("Referencia"))
            {
                throw new InvalidOperationException("Schedule must contain a 'Referencia' column.");
            }

            var referencias = dataDict["Referencia"];            
            var runner = new ScheduleSimulationRunner(dataDict, headers);
            var (bestSequence, delay, inspections) =
                SequenceOptimizer.OptimizePartSequence(runner, referencias, generations, populationSize);

            WriteOptimisedSchedule(outputSchedule, headers, dataDict, bestSequence);
            WriteStats(outputStats, delay, inspections);
        }

        // Loads a CSV schedule into a header list and data dictionary.
        private static (List<string> headers, Dictionary<string, List<string>> dataDict) LoadSchedule(string path)
        {
            var lines = File.ReadAllLines(path);
            if (lines.Length == 0)
            {
                throw new InvalidOperationException("Schedule file is empty.");
            }
            var headers = new List<string>(lines[0].Split(','));
            var dict = new Dictionary<string, List<string>>();
            foreach (var h in headers)
            {
                dict[h] = new List<string>();
            }
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var values = lines[i].Split(',');
                for (int c = 0; c < headers.Count && c < values.Length; c++)
                {
                    dict[headers[c]].Add(values[c]);
                }
            }
            return (headers, dict);
        }

        // Writes the optimised schedule with updated priorities and order.
        private static void WriteOptimisedSchedule(string path, List<string> headers,
            Dictionary<string, List<string>> baseData, string[] sequence)
        {
            var indexByRef = new Dictionary<string, int>();
            var referencias = baseData["Referencia"];
            for (int i = 0; i < referencias.Count; i++)
            {
                indexByRef[referencias[i]] = i;
            }

            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine(string.Join(",", headers));
                int priority = 1;
                foreach (var refId in sequence)
                {
                    int idx = indexByRef[refId];
                    var row = new List<string>();
                    foreach (var h in headers)
                    {
                        string value = baseData[h][idx];
                        if (h.Equals("Priority", StringComparison.OrdinalIgnoreCase) ||
                            h.Equals("priorities", StringComparison.OrdinalIgnoreCase))
                        {
                            value = priority.ToString();
                        }
                        row.Add(value);
                    }
                    writer.WriteLine(string.Join(",", row));
                    priority++;
                }
            }
        }

        // Writes delay and inspection counts to a simple CSV.
        private static void WriteStats(string path, int delay, int inspections)
        {
            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine("Metric,Value");
                writer.WriteLine($"Delays,{delay}");
                writer.WriteLine($"Inspections,{inspections}");
            }
        }
    }
}
