using UnityEngine;

namespace UnitySimuLean
{
    [CreateAssetMenu(fileName = "GeneticAlgorithmSettings", menuName = "SimuLean/Genetic Algorithm Settings")]
    public class GeneticAlgorithmSettings : ScriptableObject
    {
        [Header("Genetic Algorithm Parameters")]
        [Tooltip("Population size per generation.")]
        [Min(2)]
        public int populationSize = 100;

        [Tooltip("Number of generations to evolve.")]
        [Min(1)]
        public int generations = 500;

        [Tooltip("Probability of applying crossover.")]
        [Range(0f, 1f)]
        public double crossoverProb = 0.9;

        [Tooltip("Probability of applying mutation.")]
        [Range(0f, 1f)]
        public double mutationProb = 0.15;

        [Header("Genetic Operators")]
        [Tooltip("Selection method to use.")]
        public SelectionType selectionType = SelectionType.Tournament;

        [Tooltip("Crossover operator for the order part.")]
        public OrderCrossoverType orderCrossover = OrderCrossoverType.PMX;

        [Tooltip("Crossover operator for the inspection part.")]
        public BitCrossoverType bitCrossover = BitCrossoverType.Uniform;

        [Tooltip("Mutation operator for the order part.")]
        public OrderMutationType orderMutation = OrderMutationType.Twors;
    }
}
