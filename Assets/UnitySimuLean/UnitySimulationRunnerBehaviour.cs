using UnityEngine;

namespace UnitySimuLean
{
    public class UnitySimulationRunnerBehaviour : MonoBehaviour, ISimulationRunner
    {
        private readonly UnitySimulationRunner runner = new UnitySimulationRunner();

        public void Configure(string[] sequence) => runner.Configure(sequence);

        public void Run() => runner.Run();

        public double TotalDelay => runner.TotalDelay;

        public int InspectionCount => runner.InspectionCount;
    }
}
