using ChapaGA.Models;
using GeneticSharp;

namespace ChapaGA.GA;

/// <summary>
/// Fitness function for chapa scheduling.
/// </summary>
public class ChapaFitness : IFitness
{
    private readonly IList<Chapa> _chapas;

    public ChapaFitness(IList<Chapa> chapas)
    {
        _chapas = chapas;
    }

    public double Evaluate(IChromosome chromosome)
    {
        var result = EvaluateDetailed((ChapaChromosome)chromosome);
        return result.Fitness;
    }

    public FitnessResult EvaluateDetailed(ChapaChromosome chromosome)
    {
        var order = chromosome.GetOrder();
        var inspectBits = chromosome.GetInspections();

        int numInspections = 0;
        int numLate = 0;
        double completion = 0;
        var details = new List<ResultRow>();

        for (int pos = 0; pos < order.Length; pos++)
        {
            int idx = order[pos];
            var chapa = _chapas[idx];
            bool doInspect = chapa.InspeccionObligatoria || inspectBits[idx];
            double procTime = chapa.TSoldadura + (doInspect ? chapa.TInspeccion : 0);
            completion += procTime;
            if (doInspect) numInspections++;
            bool isLate = completion > chapa.DueDate;
            if (isLate) numLate++;
            details.Add(new ResultRow
            {
                OrderIndex = pos,
                Chapa = chapa,
                DoInspect = doInspect,
                CompletionTime = completion,
                IsLate = isLate
            });
        }

        double fitness = numInspections - 100 * numLate;
        return new FitnessResult
        {
            Fitness = fitness,
            Rows = details,
            TotalInspections = numInspections,
            TotalDelays = numLate
        };
    }
}

public class ResultRow
{
    public int OrderIndex { get; set; }
    public required Chapa Chapa { get; set; }
    public bool DoInspect { get; set; }
    public double CompletionTime { get; set; }
    public bool IsLate { get; set; }
}

public class FitnessResult
{
    public double Fitness { get; set; }
    public required List<ResultRow> Rows { get; set; }
    public int TotalInspections { get; set; }
    public int TotalDelays { get; set; }
}
