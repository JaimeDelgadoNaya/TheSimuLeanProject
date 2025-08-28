using UnityEngine;

namespace UnitySimuLean
{
    [System.Serializable]
    public class GASettings
    {
        public int numberOfParts = 10;
        public int generations = 100;
        public int populationSize = 50;
        public SelectionOperator selection = SelectionOperator.Elite;
        public CrossoverOperator crossover = CrossoverOperator.Ordered;
        public MutationOperator mutation = MutationOperator.Twors;
    }

    public class GeneticSequenceTester : MonoBehaviour
    {
        [SerializeField] private bool enableOptimization = true;
        [SerializeField] private GASettings settings = new GASettings();
        [SerializeField] private UnitySimulationRunnerBehaviour simulationRunner;

        private void Start()
        {
            if (enableOptimization)
            {
                RunOptimization();
            }
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
                settings.numberOfParts,
                settings.generations,
                settings.populationSize,
                settings.selection,
                settings.crossover,
                settings.mutation);

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
