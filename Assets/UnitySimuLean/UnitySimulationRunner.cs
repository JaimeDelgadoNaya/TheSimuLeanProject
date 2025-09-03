using System;
using System.Collections;
using System.Collections.Generic;
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

        private class PlateInfo
        {
            public double ArrivalTime;
            public double WeldTime;
            public double InspectionTime;
            public int InspectionOn;
            public double DueDate;
        }

        private Dictionary<string, PlateInfo> _plateData;
        private const string ArrivalFile = "Llegada_Chapas.xlsx";

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

        private void LoadPlateData()
        {
            if (_plateData != null)
            {
                return;
            }

            _plateData = new Dictionary<string, PlateInfo>();
            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                using var stream = File.Open(ArrivalFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = ExcelReaderFactory.CreateReader(stream);
                var result = reader.AsDataSet();
                var table = result.Tables[0];
                int cols = table.Columns.Count;
                string[] headers = new string[cols];
                for (int c = 0; c < cols; c++)
                {
                    headers[c] = table.Rows[0][c]?.ToString() ?? string.Empty;
                }

                for (int r = 1; r < table.Rows.Count; r++)
                {
                    var rowDict = new Dictionary<string, string>();
                    for (int c = 0; c < cols; c++)
                    {
                        rowDict[headers[c]] = table.Rows[r][c]?.ToString() ?? string.Empty;
                    }

                    if (!rowDict.TryGetValue("Referencia", out var refId) || string.IsNullOrWhiteSpace(refId))
                    {
                        continue;
                    }

                    var info = new PlateInfo();
                    if (rowDict.TryGetValue("Time", out var tStr))
                    {
                        double.TryParse(tStr, NumberStyles.Any, CultureInfo.InvariantCulture, out info.ArrivalTime);
                    }
                    if (rowDict.TryGetValue("tSoldadura", out var wStr))
                    {
                        double.TryParse(wStr, NumberStyles.Any, CultureInfo.InvariantCulture, out info.WeldTime);
                    }
                    if (rowDict.TryGetValue("tInspeccion", out var iStr))
                    {
                        double.TryParse(iStr, NumberStyles.Any, CultureInfo.InvariantCulture, out info.InspectionTime);
                    }
                    if (rowDict.TryGetValue("inspeccionOn", out var onStr))
                    {
                        int.TryParse(onStr, NumberStyles.Any, CultureInfo.InvariantCulture, out info.InspectionOn);
                    }
                    if (rowDict.TryGetValue("DueDate", out var dStr))
                    {
                        double.TryParse(dStr, NumberStyles.Any, CultureInfo.InvariantCulture, out info.DueDate);
                    }

                    _plateData[refId] = info;
                }
            }
            catch (IOException)
            {
                // If the file cannot be read, leave dictionary empty and rely on defaults.
            }
        }

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

            LoadPlateData();

            // Build an in-memory schedule representing the provided sequence.
            var dataDict = new Dictionary<string, List<string>>
            {
                {"Time", new List<string>()},
                {"Name", new List<string>()},
                {"Q", new List<string>()},
                {"type", new List<string>()},
                {"tSoldadura", new List<string>()},
                {"tInspeccion", new List<string>()},
                {"inspeccionOn", new List<string>()},
                {"DueDate", new List<string>()}
            };

            foreach (var partId in sequence)
            {
                if (!_plateData.TryGetValue(partId, out var info))
                {
                    info = new PlateInfo();
                }

                dataDict["Time"].Add(info.ArrivalTime.ToString(CultureInfo.InvariantCulture));
                dataDict["Name"].Add("Part");
                dataDict["Q"].Add("1");
                dataDict["type"].Add(partId);
                dataDict["tSoldadura"].Add(info.WeldTime.ToString(CultureInfo.InvariantCulture));
                dataDict["tInspeccion"].Add(info.InspectionTime.ToString(CultureInfo.InvariantCulture));
                dataDict["inspeccionOn"].Add(info.InspectionOn.ToString(CultureInfo.InvariantCulture));
                dataDict["DueDate"].Add(info.DueDate.ToString(CultureInfo.InvariantCulture));
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

