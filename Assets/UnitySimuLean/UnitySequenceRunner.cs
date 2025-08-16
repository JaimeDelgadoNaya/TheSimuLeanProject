using UnityEngine;

namespace UnitySimuLean
{
    /// <summary>
    /// Simple simulation runner that configures a <see cref="UnityInfinitySource"/>
    /// with a predefined sequence of part identifiers and starts the
    /// simulation. Metrics such as total delay or inspection count can be
    /// collected by other components and exposed through this runner.
    /// </summary>
    public class UnitySequenceRunner : MonoBehaviour, ISimulationRunner
    {
        [SerializeField] private UnityInfinitySource source;

        [SerializeField] private float maxTime = 1000f;

        public double TotalDelay { get; private set; }

        public int InspectionCount { get; private set; }

        /// <summary>
        /// Configures the simulation by assigning the part sequence to the
        /// source. Resets accumulated metrics.
        /// </summary>
        /// <param name="sequence">Sequence of part identifiers.</param>
        public void Configure(int[] sequence)
        {
            if (source != null)
            {
                source.SetSequence(sequence);
            }

            TotalDelay = 0;
            InspectionCount = 0;
        }

        /// <summary>
        /// Starts the simulation. It assumes that <see cref="UnitySimClock"/>
        /// is present in the scene and will drive the model forward.
        /// </summary>
        public void Run()
        {
            UnitySimClock.Instance.SimEvents.OnSimStart.Invoke(maxTime);
        }
    }
}

