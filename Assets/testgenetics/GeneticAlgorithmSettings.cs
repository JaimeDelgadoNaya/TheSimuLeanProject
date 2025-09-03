using UnityEngine;

namespace UnitySimuLean
{
    [CreateAssetMenu(fileName = "GeneticAlgorithmSettings", menuName = "SimuLean/Genetic Algorithm Settings")]
    public class GeneticAlgorithmSettings : ScriptableObject
    {
        [Header("Genetic Algorithm Parameters")]
        [Tooltip("Number of parts in the sequence.")]
        [Min(1)]
        public int numberOfParts = 10;

        [Tooltip("Number of generations to evolve.")]
        [Min(1)]
        public int generations = 100;

        [Tooltip("Population size per generation.")]
        // GeneticSharp requires at least two chromosomes per generation.
        // Enforce a minimum of 2 to prevent runtime errors when configured
        // through a ScriptableObject.
        [Min(2)]
        public int populationSize = 50;

        [Header("Genetic Operators")]
        [Tooltip("Selection method to use.")]
        public SelectionType selectionType = SelectionType.Elite;

        [Tooltip("Crossover operator to apply.")]
        public CrossoverType crossoverType = CrossoverType.Ordered;

        [Tooltip("Mutation operator to apply.")]
        public MutationType mutationType = MutationType.Twors;
    }
}
