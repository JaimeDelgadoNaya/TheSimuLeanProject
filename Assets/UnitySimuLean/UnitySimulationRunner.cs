using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using ExcelDataReader;
using SimuLean;

namespace UnitySimuLean
{
    /// <summary>
    /// Basic implementation of <see cref="ISimulationRunner"/> that builds a
    /// minimal SimuLean model consisting of a <see cref="ScheduleSource"/>
    /// feeding a <see cref="Sink"/>. The sequence of part identifiers is
    /// translated into an in-memory schedule which is applied on every
    /// configuration. Metrics are gathered from the sink once the simulation
    /// finishes.
    /// </summary>
    public class UnitySimulationRunner : ISimulationRunner
    {
        private readonly SimClock _clock = new SimClock();
        private ScheduleSource _source;
        private Sink _sink;

        // Cached data read from the arrival Excel file. Each entry represents a row
        // of the spreadsheet as a dictionary of column name to value.
        private List<Dictionary<string, string>> _arrivalData;

        /// <summary>
        /// Path to the Excel file containing arrival information for the sheets.
        /// Defaults to "Llegada_Chapas.xlsx" located at the application root.
        /// </summary>
        public string ArrivalExcelPath { get; set; } = "Llegada_Chapas.xlsx";

        /// <summary>
        /// Optional visual representation for the initial <see cref="ScheduleSource"/>.
        /// When set, generated items will use this <see cref="VElement"/> instead of a
        /// headless <see cref="NullVElement"/>.
        /// </summary>
        public VElement SourceView { get; set; }

        /// <summary>
        /// Optional visual representation for the final <see cref="Sink"/>.
        /// When set, items leaving the model will interact with this
        /// <see cref="VElement"/>.
        /// </summary>
        public VElement SinkView { get; set; }

        private int _delayCount;
        private int _inspectionCount;

        // Keep track of the last configured sequence so that metrics such as
        // delays can be recomputed even when the minimal model (Source -> Sink)
        // does not advance simulation time.
        private string[] _currentSequence;

        /// <inheritdoc />
        public void Configure(string[] sequence)
        {
            if (sequence == null)
            {
                throw new ArgumentNullException(nameof(sequence));
            }

            // Reset previous model state.
            _clock.Reset();
            Element.GetElements().Clear();

            // Ensure arrival data from Excel is loaded only once.
            if (_arrivalData == null)
            {
                _arrivalData = LoadArrivalData();
            }

            // Prepare the dictionary that will hold the schedule for the
            // configured sequence. Start by creating an entry for each column
            // found in the Excel file so all labels are preserved.
            var headers = new List<string>(_arrivalData[0].Keys);
            var dataDict = new Dictionary<string, List<string>>();
            foreach (var h in headers)
            {
                dataDict[h] = new List<string>();
            }

            // Populate the schedule using the provided sequence. Each partId is
            // interpreted as the zero-based index of the row in the Excel file.
            foreach (var partId in sequence)
            {
                if (!int.TryParse(partId, out int idx) || idx < 0 || idx >= _arrivalData.Count)
                {
                    throw new ArgumentException($"Invalid part identifier: {partId}");
                }

                var row = _arrivalData[idx];
                foreach (var h in headers)
                {
                    dataDict[h].Add(row.ContainsKey(h) ? row[h] : "");
                }
            }

            // Rebuild source and sink with the new schedule. Attach provided
            // visual elements when available so that items can be observed in
            // the Unity scene. Fall back to headless execution otherwise.
            _source = new ScheduleSource("Source", _clock, null, dataDict, null, null)
            {
                vElement = SourceView ?? new NullVElement()
            };
            _sink = new Sink("Sink", _clock)
            {
                vElement = SinkView ?? new NullVElement()
            };
            GeneralLink.CreateLink(_source, new List<Element> { _sink });

            // Inform the sink of the expected number of items so it can compute
            // overall timing when all have been processed.
            _sink.expectedItems = sequence.Length;

            // Reset metrics and keep the sequence for later evaluation.
            _delayCount = 0;
            _inspectionCount = 0;
            _currentSequence = sequence;
        }

        /// <summary>
        /// Loads the arrival data from the Excel file into memory.
        /// </summary>
        private List<Dictionary<string, string>> LoadArrivalData()
        {
            var rows = new List<Dictionary<string, string>>();

            var path = ArrivalExcelPath;
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Arrival Excel file not found", path);
            }

            // ExcelDataReader requires registering the code pages provider for
            // non UTF encodings.
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var result = reader.AsDataSet();
                var table = result.Tables[0];

                if (table.Rows.Count == 0)
                {
                    return rows;
                }

                int colCount = table.Columns.Count;
                var headers = new string[colCount];
                for (int c = 0; c < colCount; c++)
                {
                    headers[c] = table.Rows[0][c]?.ToString() ?? string.Empty;
                }

                for (int r = 1; r < table.Rows.Count; r++)
                {
                    var dict = new Dictionary<string, string>();
                    for (int c = 0; c < colCount; c++)
                    {
                        dict[headers[c]] = table.Rows[r][c]?.ToString() ?? string.Empty;
                    }
                    rows.Add(dict);
                }
            }

            return rows;
        }

        /// <summary>
        /// Computes the number of delayed items and inspections for the provided
        /// sequence using the cached arrival data. This is necessary because the
        /// simplified Source->Sink model does not account for processing times
        /// when advancing the simulation clock.
        /// </summary>
        private (int delays, int inspections) EvaluateSequence(string[] sequence)
        {
            double currentTime = 0.0;
            int delays = 0;
            int inspections = 0;

            foreach (var partId in sequence)
            {
                if (!int.TryParse(partId, out int idx) || idx < 0 || idx >= _arrivalData.Count)
                {
                    continue;
                }

                var row = _arrivalData[idx];

                double tSoldadura = ParseDouble(row, "tSoldadura");
                double tInspeccion = ParseDouble(row, "tInspeccion");
                bool inspeccionOn = ParseDouble(row, "inspeccionOn") >= 1.0;
                double dueDate = ParseDouble(row, "DueDate");

                currentTime += tSoldadura;
                if (inspeccionOn)
                {
                    currentTime += tInspeccion;
                    inspections++;
                }

                if (dueDate > 0 && currentTime > dueDate)
                {
                    delays++;
                }
            }

            return (delays, inspections);
        }

        private static double ParseDouble(Dictionary<string, string> row, string key)
        {
            if (row.TryGetValue(key, out string val))
            {
                if (double.TryParse(val?.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double d))
                {
                    return d;
                }
            }
            return 0.0;
        }

        /// <inheritdoc />
        public void Run()
        {
            if (_source == null || _sink == null)
            {
                throw new InvalidOperationException("Runner must be configured before running.");
            }

            // Start elements and process events until the agenda is empty.
            _source.Start();
            _sink.Start();
            while (_clock.AdvanceClock(double.MaxValue))
            {
                // Loop until no more events remain in the clock.
            }

            // Compute metrics using the cached sequence data.
            var (delays, inspections) = EvaluateSequence(_currentSequence);
            _delayCount = delays;
            _inspectionCount = inspections;
        }

        /// <inheritdoc />
        public int DelayCount => _delayCount;

        /// <inheritdoc />
        public int InspectionCount => _inspectionCount;
    }
}

