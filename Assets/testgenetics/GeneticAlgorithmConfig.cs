namespace UnitySimuLean
{
    /// <summary>
    /// Selection operators supported by the genetic algorithm.
    /// </summary>
    public enum SelectionType
    {
        Tournament
    }

    /// <summary>
    /// Crossover operators for the permutation part of the chromosome.
    /// </summary>
    public enum OrderCrossoverType
    {
        PMX,
        OX
    }

    /// <summary>
    /// Crossover operators for the binary inspection part of the chromosome.
    /// </summary>
    public enum BitCrossoverType
    {
        Uniform,
        OnePoint
    }

    /// <summary>
    /// Mutation operators for the permutation part of the chromosome.
    /// </summary>
    public enum OrderMutationType
    {
        Swap,
        Twors
    }
}
