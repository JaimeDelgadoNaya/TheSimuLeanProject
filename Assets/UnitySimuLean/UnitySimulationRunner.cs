using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using SimuLean;
using ExcelDataReader;

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
        private List<Dictionary<string, string>> _scheduleRows;

        /// <summary>
        /// Optional path to an Excel file containing the arrival schedule
        /// for the sheet metal pieces. When provided the optimizer will
        /// read <c>tSoldadura</c>, <c>tInspeccion</c> and any due date
        /// information directly from this file.
        /// </summary>
        public string ScheduleFilePath { get; set; }

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

        /// <summary>
        /// Loads the schedule from <see cref="ScheduleFilePath"/> if it has not
        /// been loaded yet.
        /// </summary>
        public void LoadSchedule()
        {
            if (_scheduleRows != null)
            {
                return;
            }

            if (string.IsNullOrEmpty(ScheduleFilePath))
            {
                throw new InvalidOperationException("ScheduleFilePath must be set before loading the schedule.");
            }

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            _scheduleRows = new List<Dictionary<string, string>>();
            using (var stream = File.Open(ScheduleFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var result = reader.AsDataSet();
                DataTable table = result.Tables[0];
                int colCount = table.Columns.Count;
                var headers = new string[colCount];
                for (int i = 0; i < colCount; i++)
                {
                    headers[i] = table.Rows[0][i]?.ToString() ?? string.Empty;
                }

                for (int row = 1; row < table.Rows.Count; row++)
                {
                    var dict = new Dictionary<string, string>();
                    for (int col = 0; col < colCount; col++)
                    {
                        dict[headers[col]] = table.Rows[row][col]?.ToString() ?? string.Empty;
                    }
                    _scheduleRows.Add(dict);
                }
            }
        }

        /// <summary>
        /// Total number of parts loaded from the schedule file.
        /// </summary>
        public int PartCount => _scheduleRows?.Count ?? 0;

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

            // Ensure the schedule rows are loaded from the Excel file.
            LoadSchedule();

            if (_scheduleRows == null || _scheduleRows.Count == 0)
            {
                throw new InvalidOperationException("No schedule data loaded.");
            }

            // Build an in-memory schedule respecting the provided sequence.
            // Each entry in the sequence corresponds to the index of a row
            // within the Excel schedule. All columns beyond Time/Name/Q are
            // preserved as labels so that processing and deadlines are taken
            // into account.
            var dataDict = new Dictionary<string, List<string>>();
            foreach (var key in _scheduleRows[0].Keys)
            {
                dataDict[key] = new List<string>();
            }

            foreach (var partId in sequence)
            {
                if (!int.TryParse(partId, out int rowIndex) ||
                    rowIndex < 0 || rowIndex >= _scheduleRows.Count)
                {
                    throw new ArgumentException($"Invalid part index: {partId}", nameof(sequence));
                }

                var row = _scheduleRows[rowIndex];
                foreach (var kv in row)
                {
                    dataDict[kv.Key].Add(kv.Value);
                }
            }

            // Rebuild source and sink with the new schedule. Attach provided
            // visual elements when available so that items can be observed in
            // the Unity scene. Fall back to headless execution otherwise.
            _source = new ScheduleSource("Source", _clock, null, dataDict, null, null, autoSort: false)
            {
                vElement = SourceView ?? new NullVElement()
            };
            _sink = new Sink("Sink", _clock)
            {
                vElement = SinkView ?? new NullVElement()
            };
            GeneralLink.CreateLink(_source, new List<Element> { _sink });

            // Reset metrics.
            _delayCount = 0;
            _inspectionCount = 0;
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

            // Collect metrics from the sink.
            _delayCount = _sink.GetRetrasados();
            _inspectionCount = _sink.GetInspecciones();
        }

        /// <inheritdoc />
        public int DelayCount => _delayCount;

        /// <inheritdoc />
        public int InspectionCount => _inspectionCount;
    }
}

