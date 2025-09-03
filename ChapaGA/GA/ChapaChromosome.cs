using GeneticSharp;

namespace ChapaGA.GA;

/// <summary>
/// Chromosome representing both the processing order of chapas and the inspection decisions.
/// Genes are stored as two parts:
/// - First N genes: permutation of indices (0..N-1).
/// - Last N genes: binary vector indicating optional inspections.
/// Mandatory inspections are always forced to 1 after genetic operations.
/// </summary>
public class ChapaChromosome : ChromosomeBase
{
    private readonly int _length;
    private readonly bool[] _mandatory;

    public ChapaChromosome(int length, bool[] mandatory) : base(length * 2)
    {
        _length = length;
        _mandatory = mandatory;
        InitializeGenes();
    }

    public int JobCount => _length;
    public bool[] Mandatory => _mandatory;

    public override Gene GenerateGene(int index)
    {
        // Not used because genes are created manually in constructor.
        return new Gene(0);
    }

    public override IChromosome CreateNew()
    {
        return new ChapaChromosome(_length, _mandatory);
    }

    private void InitializeGenes()
    {
        var rnd = RandomizationProvider.Current;
        var order = Enumerable.Range(0, _length).OrderBy(_ => rnd.GetInt(0, _length)).ToArray();
        var genes = new Gene[_length * 2];
        for (int i = 0; i < _length; i++)
        {
            genes[i] = new Gene(order[i]);
        }
        for (int i = 0; i < _length; i++)
        {
            var bit = _mandatory[i] ? 1 : rnd.GetInt(0, 2);
            genes[_length + i] = new Gene(bit);
        }
        ReplaceGenes(0, genes);
    }

    public int[] GetOrder()
    {
        return GetGenes().Take(_length).Select(g => (int)g.Value).ToArray();
    }

    public bool[] GetInspections()
    {
        return GetGenes().Skip(_length).Take(_length).Select(g => (int)g.Value == 1).ToArray();
    }

    public void SetOrder(int[] order)
    {
        for (int i = 0; i < _length; i++)
        {
            ReplaceGene(i, new Gene(order[i]));
        }
    }

    public void SetInspections(bool[] inspections)
    {
        for (int i = 0; i < _length; i++)
        {
            ReplaceGene(_length + i, new Gene(inspections[i] ? 1 : 0));
        }
    }

    /// <summary>
    /// Repairs chromosome ensuring permutation validity and mandatory inspections.
    /// </summary>
    public void Repair()
    {
        // Fix permutation
        var order = GetOrder();
        var seen = new HashSet<int>();
        var missing = new Queue<int>(Enumerable.Range(0, _length).Except(order));
        for (int i = 0; i < order.Length; i++)
        {
            if (!seen.Add(order[i]))
            {
                order[i] = missing.Dequeue();
            }
        }
        SetOrder(order);

        // Enforce mandatory inspections
        var ins = GetInspections();
        for (int i = 0; i < ins.Length; i++)
        {
            if (_mandatory[i])
            {
                ins[i] = true;
            }
        }
        SetInspections(ins);
    }
}
