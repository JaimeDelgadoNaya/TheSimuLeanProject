using System;
using System.Collections;
using System.Collections.Generic;
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
        private Dictionary<string, ScheduleEntry> _baseSchedule;

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

        /// <inheritdoc />
        public void LoadSchedule(Dictionary<string, ScheduleEntry> schedule)
        {
            _baseSchedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
        }

        /// <inheritdoc />
        public void Configure(string[] sequence)
        {
            if (sequence == null)
            {
                throw new ArgumentNullException(nameof(sequence));
            }
            if (_baseSchedule == null)
            {
                throw new InvalidOperationException("Schedule must be loaded before configuring the runner.");
            }

            // Reset previous model state.
            _clock.Reset();
            Element.GetElements().Clear();

            // Build an in-memory schedule using the baseline schedule and the
            // provided priority order.
            var dataDict = new Dictionary<string, List<string>>
            {
                {"Time", new List<string>()},
                {"Name", new List<string>()},
                {"Q", new List<string>()},
                {"nRefuerzos", new List<string>()},
                {"Referencia", new List<string>()},
                {"tSoldadura", new List<string>()},
                {"tInspeccion", new List<string>()},
                {"inspeccionOn", new List<string>()},
                {"DueDate", new List<string>()},
                {"Priority", new List<string>()}
            };

            // Include type column only when present in the schedule.
            bool includeType = false;
            foreach (var entry in _baseSchedule.Values)
            {
                if (!string.IsNullOrEmpty(entry.Type))
                {
                    includeType = true;
                    break;
                }
            }
            if (includeType)
            {
                dataDict["type"] = new List<string>();
            }

            for (int i = 0; i < sequence.Length; i++)
            {
                var refId = sequence[i];
                if (!_baseSchedule.TryGetValue(refId, out var entry))
                {
                    throw new ArgumentException($"Unknown reference: {refId}", nameof(sequence));
                }

                dataDict["Time"].Add(entry.Time.ToString());
                dataDict["Name"].Add(entry.Name);
                dataDict["Q"].Add(entry.Quantity.ToString());
                dataDict["nRefuerzos"].Add(entry.nRefuerzos.ToString());
                dataDict["Referencia"].Add(entry.Referencia);
                dataDict["tSoldadura"].Add(entry.tSoldadura.ToString());
                dataDict["tInspeccion"].Add(entry.tInspeccion.ToString());
                dataDict["inspeccionOn"].Add(entry.inspeccionOn.ToString());
                dataDict["DueDate"].Add(entry.DueDate.ToString());
                dataDict["Priority"].Add((i + 1).ToString());
                if (includeType)
                {
                    dataDict["type"].Add(entry.Type);
                }
            }

            // Rebuild source and sink with the new schedule. Attach provided
            // visual elements when available so that items can be observed in
            // the Unity scene. Fall back to headless execution otherwise.
            _source = new ScheduleSource("Source", _clock, null, dataDict, null, null, false)
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

