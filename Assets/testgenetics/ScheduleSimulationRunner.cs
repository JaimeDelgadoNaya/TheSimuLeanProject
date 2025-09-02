using System;
using System.Collections.Generic;
using SimuLean;

namespace UnitySimuLean
{
    /// <summary>
    /// Simulation runner that executes a model based on a schedule loaded
    /// from a data dictionary. The dictionary maps column headers to lists of
    /// values. During configuration the entries are reordered according to the
    /// provided sequence of reference identifiers, allowing genetic algorithms
    /// to evaluate different priority orders.
    /// </summary>
    public class ScheduleSimulationRunner : ISimulationRunner
    {
        private readonly Dictionary<string, List<string>> _baseData;
        private readonly List<string> _headers;
        private readonly SimClock _clock = new SimClock();
        private ScheduleSource _source;
        private Sink _sink;
        private int _delayCount;
        private int _inspectionCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduleSimulationRunner"/> class.
        /// </summary>
        /// <param name="baseData">Schedule data indexed by column header.</param>
        /// <param name="headers">Original header order for CSV reconstruction.</param>
        public ScheduleSimulationRunner(Dictionary<string, List<string>> baseData, List<string> headers)
        {
            _baseData = baseData ?? throw new ArgumentNullException(nameof(baseData));
            _headers = headers ?? throw new ArgumentNullException(nameof(headers));
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

            // Prepare a new data dictionary reordered according to the sequence
            // of references. Also assign sequential priorities reflecting the
            // new order.
            var dataDict = new Dictionary<string, List<string>>();
            foreach (var h in _headers)
            {
                dataDict[h] = new List<string>();
            }

            // Map reference to row index for quick lookup.
            var referencias = _baseData["Referencia"];
            var indexByRef = new Dictionary<string, int>();
            for (int i = 0; i < referencias.Count; i++)
            {
                indexByRef[referencias[i]] = i;
            }

            int priority = 1;
            foreach (var refId in sequence)
            {
                int idx = indexByRef[refId];
                foreach (var h in _headers)
                {
                    string value = _baseData[h][idx];
                    if (h.Equals("Priority", StringComparison.OrdinalIgnoreCase) ||
                        h.Equals("priorities", StringComparison.OrdinalIgnoreCase))
                    {
                        dataDict[h].Add(priority.ToString());
                    }
                    else
                    {
                        dataDict[h].Add(value);
                    }
                }
                priority++;
            }

            _source = new ScheduleSource("Source", _clock, null, dataDict, null, null, autoSort: false)
            {
                vElement = new NullVElement()
            };
            _sink = new Sink("Sink", _clock)
            {
                vElement = new NullVElement()
            };
            GeneralLink.CreateLink(_source, new List<Element> { _sink });
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

            _source.Start();
            _sink.Start();
            while (_clock.AdvanceClock(double.MaxValue))
            {
                // Process events until none remain.
            }
            _delayCount = _sink.GetRetrasados();
            _inspectionCount = _sink.GetInspecciones();
        }

        /// <inheritdoc />
        public int DelayCount => _delayCount;

        /// <inheritdoc />
        public int InspectionCount => _inspectionCount;
    }
}
