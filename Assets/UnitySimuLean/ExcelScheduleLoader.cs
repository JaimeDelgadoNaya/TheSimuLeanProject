using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using ExcelDataReader;

namespace UnitySimuLean
{
    /// <summary>
    /// Utility to read production schedules from an Excel file.
    /// Expected Excel format includes at least the following columns:
    /// Time | Name | Q | nRefuerzos | Referencia | tSoldadura | tInspeccion |
    /// inspeccionOn | DueDate | Priority
    ///
    /// Additional columns are ignored. Each row after the header represents
    /// a schedule entry.
    /// </summary>
    public static class ExcelScheduleLoader
    {
        /// <summary>
        /// Loads a schedule from the given Excel file. The method returns an
        /// ordered list of references and a dictionary with the attributes for
        /// each reference.
        /// </summary>
        /// <param name="filePath">Path to the Excel file.</param>
        /// <returns>
        /// orderedRefs: References ordered by time and priority.
        /// attributes: Map of reference to its attributes (time, quantity,
        /// nRefuerzos, tSoldadura, tInspeccion, inspeccionOn, DueDate,
        /// priority, etc.).
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
                        string headerKey = header.Trim();
                        columnIndex[headerKey] = col;

                        // Support plural header "priorities" by also mapping it to "Priority"
                        if (headerKey.Equals("priorities", StringComparison.OrdinalIgnoreCase))
                        {
                            columnIndex["Priority"] = col;
                        }
                    }
                }

                // Read each row after the headers
                for (int row = 1; row < table.Rows.Count; row++)
                {
                    if (table.Rows[row] == null) continue;

                    string referencia = null;
                    if (columnIndex.TryGetValue("Referencia", out int referenciaCol))
                    {
                        referencia = table.Rows[row][referenciaCol]?.ToString();
                    }

                    // If no explicit reference is provided fall back to Name.
                    string name = table.Rows[row][columnIndex["Name"]]?.ToString();
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    if (string.IsNullOrWhiteSpace(referencia)) referencia = name;

                    string timeStr = table.Rows[row][columnIndex["Time"]]?.ToString();
                    string quantityStr = table.Rows[row][columnIndex["Q"]]?.ToString();
                    string type = columnIndex.ContainsKey("type") ? table.Rows[row][columnIndex["type"]]?.ToString() : null;

                    string nRefuerzosStr = columnIndex.ContainsKey("nRefuerzos")
                        ? table.Rows[row][columnIndex["nRefuerzos"]]?.ToString()
                        : null;
                    string tSoldaduraStr = columnIndex.ContainsKey("tSoldadura")
                        ? table.Rows[row][columnIndex["tSoldadura"]]?.ToString()
                        : null;
                    string tInspeccionStr = columnIndex.ContainsKey("tInspeccion")
                        ? table.Rows[row][columnIndex["tInspeccion"]]?.ToString()
                        : null;
                    string inspeccionOnStr = columnIndex.ContainsKey("inspeccionOn")
                        ? table.Rows[row][columnIndex["inspeccionOn"]]?.ToString()
                        : null;
                    string dueDateStr = columnIndex.ContainsKey("DueDate")
                        ? table.Rows[row][columnIndex["DueDate"]]?.ToString()
                        : null;

                    string priorityStr = null;
                    if (columnIndex.TryGetValue("Priority", out int priorityCol))
                    {
                        priorityStr = table.Rows[row][priorityCol]?.ToString();
                    }

                    double.TryParse(timeStr, out double time);
                    int.TryParse(quantityStr, out int quantity);
                    int.TryParse(nRefuerzosStr, out int nRefuerzos);
                    double.TryParse(tSoldaduraStr, out double tSoldadura);
                    double.TryParse(tInspeccionStr, out double tInspeccion);
                    int.TryParse(inspeccionOnStr, out int inspeccionOn);
                    double.TryParse(dueDateStr, out double dueDate);
                    int.TryParse(priorityStr, out int priority);

                    // Use the row "Name" as the unique key so that duplicate
                    // references are preserved. The original "Referencia" is
                    // still stored inside the entry for downstream lookups.
                    var key = name;
                    attributes[key] = new ScheduleEntry
                    {
                        Time = time,
                        Name = name,
                        Quantity = quantity,
                        Type = type,
                        nRefuerzos = nRefuerzos,
                        Referencia = referencia,
                        tSoldadura = tSoldadura,
                        tInspeccion = tInspeccion,
                        inspeccionOn = inspeccionOn,
                        DueDate = dueDate,
                        Priority = priority
                    };
                }
            }

            // Order entries by time then by priority
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
        public string Name { get; set; }
        public int Quantity { get; set; }
        public string Type { get; set; }
        public int nRefuerzos { get; set; }
        public string Referencia { get; set; }
        public double tSoldadura { get; set; }
        public double tInspeccion { get; set; }
        public int inspeccionOn { get; set; }
        public double DueDate { get; set; }
        public int Priority { get; set; }
    }
}

