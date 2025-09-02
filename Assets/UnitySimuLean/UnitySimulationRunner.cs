using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
        /// Optional order provided by a genetic algorithm. Each entry represents
        /// the zero-based index of a row read from the Excel schedule. When
        /// specified, <see cref="ConfigureFromExcel"/> will rearrange the
        /// schedule to follow this sequence.
        /// </summary>
        public IList<int> GaSequence { get; set; }

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

            // Build an in-memory schedule representing the provided sequence.
            var dataDict = new Dictionary<string, List<string>>
            {
                {"Time", new List<string>()},
                {"Name", new List<string>()},
                {"Q", new List<string>()},
                {"type", new List<string>()}
            };

            foreach (var partId in sequence)
            {
                // For simplicity all arrivals occur at time 0 with quantity 1 and
                // a generic name. The important piece of information is the
                // 'type' which represents the part identifier.
                dataDict["Time"].Add("0");
                dataDict["Name"].Add("Part");
                dataDict["Q"].Add("1");
                dataDict["type"].Add(partId);
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

        /// <summary>
        /// Configures the simulation using a production schedule stored in an
        /// Excel file. The schedule is loaded through <see cref="ExcelScheduleLoader"/>
        /// and converted into the dictionary expected by <see cref="ScheduleSource"/>.
        /// When <see cref="GaSequence"/> is provided, the rows read from the
        /// spreadsheet are reordered accordingly before creating the source.
        /// </summary>
        /// <param name="path">Path to the Excel file containing the schedule.</param>
        public void ConfigureFromExcel(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            // Reset previous model state.
            _clock.Reset();
            Element.GetElements().Clear();

            // Load schedule from the Excel file.
            var (orderedRefs, attributes) = ExcelScheduleLoader.LoadSchedule(path);

            // Reorder using GA sequence if one has been supplied.
            if (GaSequence != null && GaSequence.Count == orderedRefs.Count)
            {
                var reordered = new List<string>();
                foreach (int idx in GaSequence)
                {
                    if (idx >= 0 && idx < orderedRefs.Count)
                    {
                        reordered.Add(orderedRefs[idx]);
                    }
                }
                orderedRefs = reordered;
            }

            // Build the data dictionary with all columns.
            var dataDict = new Dictionary<string, List<string>>
            {
                {"Time", new List<string>()},
                {"Name", new List<string>()},
                {"Q", new List<string>()},
                {"type", new List<string>()},
                {"Priority", new List<string>()}
            };

            foreach (var name in orderedRefs)
            {
                var entry = attributes[name];
                dataDict["Time"].Add(entry.Time.ToString(CultureInfo.InvariantCulture));
                dataDict["Name"].Add(name);
                dataDict["Q"].Add(entry.Quantity.ToString(CultureInfo.InvariantCulture));
                dataDict["type"].Add(entry.Type ?? string.Empty);
                dataDict["Priority"].Add(entry.Priority.ToString(CultureInfo.InvariantCulture));
            }

            // Rebuild source and sink using the loaded schedule. Use autoSort=false
            // to preserve the provided ordering (e.g., from a GA).
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

