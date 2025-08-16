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

        private double _totalDelay;
        private int _inspectionCount;

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

            // Rebuild source and sink with the new schedule.
            _source = new ScheduleSource("Source", _clock, null, dataDict, null, null);
            _sink = new Sink("Sink", _clock);
            GeneralLink.CreateLink(_source, new List<Element> { _sink });

            // Reset metrics.
            _totalDelay = 0.0;
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
            _totalDelay = _sink.GetRetrasados();
            _inspectionCount = _sink.GetInspecciones();
        }

        /// <inheritdoc />
        public double TotalDelay => _totalDelay;

        /// <inheritdoc />
        public int InspectionCount => _inspectionCount;
    }
}

