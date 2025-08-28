namespace UnitySimuLean
{
    /// <summary>
    /// Selection operators supported by the genetic algorithm.
    /// </summary>
    public enum SelectionType
    {
        Elite,
        RouletteWheel
    }

    /// <summary>
    /// Crossover operators supported by the genetic algorithm.
    /// </summary>
    public enum CrossoverType
    {
        Ordered,
        OnePoint
    }

    /// <summary>
    /// Mutation operators supported by the genetic algorithm.
    /// </summary>
    public enum MutationType
    {
        Twors,
        ReverseSequence
    }
}
