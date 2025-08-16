using UnityEngine;

namespace UnitySimuLean
{
    public class GeneticSequenceTester : MonoBehaviour
    {
        [SerializeField] private int numberOfParts = 10;
        [SerializeField] private int generations = 100;
        [SerializeField] private int populationSize = 50;
        [SerializeField] private ISimulationRunner simulationRunner;

        private void Start()
        {
            RunOptimization();
        }

        public void RunOptimization()
        {
            if (simulationRunner == null)
            {
                Debug.LogWarning("Simulation runner not assigned.");
                return;
            }

            var bestSequence = SequenceOptimizer.OptimizePartSequence(
                simulationRunner,
                numberOfParts,
                generations,
                populationSize);

            if (bestSequence.Length > 0)
            {
                Debug.Log($"Optimized sequence: {string.Join(",", bestSequence)}");
                Debug.Log($"Total delay: {simulationRunner.TotalDelay}, inspection count: {simulationRunner.InspectionCount}");
            }
            else
            {
                Debug.Log("No sequence returned from optimizer.");
            }
        }
    }
}
