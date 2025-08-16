using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using ExcelDataReader;

namespace UnitySimuLean
{
    /// <summary>
    /// Utility to read production schedules from an Excel file.
    /// Expected Excel format:
    /// Time | Name | Q | type | Priority
    /// Each row after the header represents a schedule entry.
    /// </summary>
    public static class ExcelScheduleLoader
    {
        /// <summary>
        /// Loads a schedule from the given Excel file.
        /// The method returns an ordered list of references and
        /// a dictionary with the attributes for each reference.
        /// </summary>
        /// <param name="filePath">Path to the Excel file.</param>
        /// <returns>
        /// orderedRefs: References ordered by time and priority.
        /// attributes: Map of reference to its attributes (time, quantity, type, priority).
        /// </returns>
        public static (List<string> orderedRefs, Dictionary<string, ScheduleEntry> attributes) LoadSchedule(string filePath)
        {
            var orderedRefs = new List<string>();
            var attributes = new Dictionary<string, ScheduleEntry>();

            // Required to read Excel files in .NET Core
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                DataSet result = reader.AsDataSet();
                DataTable table = result.Tables[0];

                // Map headers to column indexes
                var columnIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int col = 0; col < table.Columns.Count; col++)
                {
                    string header = table.Rows[0][col]?.ToString();
                    if (!string.IsNullOrEmpty(header))
                    {
                        columnIndex[header.Trim()] = col;
                    }
                }

                // Read each row after the headers
                for (int row = 1; row < table.Rows.Count; row++)
                {
                    if (table.Rows[row] == null) continue;

                    string name = table.Rows[row][columnIndex["Name"]]?.ToString();
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    string timeStr = table.Rows[row][columnIndex["Time"]]?.ToString();
                    string quantityStr = table.Rows[row][columnIndex["Q"]]?.ToString();
                    string type = columnIndex.ContainsKey("type") ? table.Rows[row][columnIndex["type"]]?.ToString() : null;
                    string priorityStr = table.Rows[row][columnIndex["Priority"]]?.ToString();

                    double.TryParse(timeStr, out double time);
                    int.TryParse(quantityStr, out int quantity);
                    int.TryParse(priorityStr, out int priority);

                    attributes[name] = new ScheduleEntry
                    {
                        Time = time,
                        Quantity = quantity,
                        Type = type,
                        Priority = priority
                    };
                }
            }

            // Order references by time then by priority
            var ordered = new List<KeyValuePair<string, ScheduleEntry>>(attributes);
            ordered.Sort((a, b) =>
            {
                int cmp = a.Value.Time.CompareTo(b.Value.Time);
                if (cmp != 0) return cmp;
                return a.Value.Priority.CompareTo(b.Value.Priority);
            });

            foreach (var kv in ordered)
            {
                orderedRefs.Add(kv.Key);
            }

            return (orderedRefs, attributes);
        }
    }

    /// <summary>
    /// Container for the attributes of a schedule entry.
    /// </summary>
    public class ScheduleEntry
    {
        public double Time { get; set; }
        public int Quantity { get; set; }
        public string Type { get; set; }
        public int Priority { get; set; }
    }
}

