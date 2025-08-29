using UnityEngine;

namespace UnitySimuLean
{
    public class UnitySimulationRunnerBehaviour : MonoBehaviour, ISimulationRunner
    {
        [SerializeField] private UnityScheduleSource source;
        [SerializeField] private UnitySink sink;

        private readonly UnitySimulationRunner runner = new UnitySimulationRunner();

        public void Configure(string[] sequence)
        {
            runner.SourceView = source;
            runner.SinkView = sink;
            runner.Configure(sequence);
        }

        public void Run() => runner.Run();

        public int DelayCount => runner.DelayCount;

        public int InspectionCount => runner.InspectionCount;
    }
}
