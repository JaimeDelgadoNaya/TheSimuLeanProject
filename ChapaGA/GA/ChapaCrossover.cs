using GeneticSharp;

namespace ChapaGA.GA;

/// <summary>
/// Crossover combining order crossover (OX) for the permutation part and
/// uniform crossover for inspection bits.
/// </summary>
public class ChapaCrossover : ICrossover
{
    public int ParentsNumber => 2;
    public int ChildrenNumber => 2;
    public bool IsOrdered => true;
    public int MinChromosomeLength => 2;

    public IList<IChromosome> Cross(IList<IChromosome> parents)
    {
        var p1 = (ChapaChromosome)parents[0];
        var p2 = (ChapaChromosome)parents[1];
        int len = p1.JobCount;
        var rnd = RandomizationProvider.Current;

        // Order crossover (OX)
        int cut1 = rnd.GetInt(0, len);
        int cut2 = rnd.GetInt(0, len);
        if (cut1 > cut2) (cut1, cut2) = (cut2, cut1);

        int[] o1 = Enumerable.Repeat(-1, len).ToArray();
        int[] o2 = Enumerable.Repeat(-1, len).ToArray();
        var parent1Order = p1.GetOrder();
        var parent2Order = p2.GetOrder();

        for (int i = cut1; i < cut2; i++)
        {
            o1[i] = parent1Order[i];
            o2[i] = parent2Order[i];
        }

        FillOrder(o1, parent2Order, cut2);
        FillOrder(o2, parent1Order, cut2);

        // Uniform crossover for inspection bits
        var ins1 = new bool[len];
        var ins2 = new bool[len];
        var p1Ins = p1.GetInspections();
        var p2Ins = p2.GetInspections();
        for (int i = 0; i < len; i++)
        {
            if (rnd.GetDouble() < 0.5)
            {
                ins1[i] = p1Ins[i];
                ins2[i] = p2Ins[i];
            }
            else
            {
                ins1[i] = p2Ins[i];
                ins2[i] = p1Ins[i];
            }
        }

        var c1 = new ChapaChromosome(len, p1.Mandatory);
        c1.SetOrder(o1);
        c1.SetInspections(ins1);
        c1.Repair();

        var c2 = new ChapaChromosome(len, p1.Mandatory);
        c2.SetOrder(o2);
        c2.SetInspections(ins2);
        c2.Repair();

        return new List<IChromosome> { c1, c2 };
    }

    private static void FillOrder(int[] child, int[] donor, int start)
    {
        int len = child.Length;
        int pos = start % len;
        foreach (var gene in donor)
        {
            if (child.Contains(gene)) continue;
            child[pos] = gene;
            pos = (pos + 1) % len;
        }
    }
}
