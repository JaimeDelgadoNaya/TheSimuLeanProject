using GeneticSharp;

namespace ChapaGA.GA;

/// <summary>
/// Mutation that swaps two positions in the permutation and flips a bit in the inspection vector.
/// </summary>
public class ChapaMutation : IMutation
{
    public bool IsOrdered => true;

    public void Mutate(IChromosome chromosome, float probability)
    {
        var c = (ChapaChromosome)chromosome;
        var rnd = RandomizationProvider.Current;
        if (rnd.GetDouble() > probability)
        {
            return;
        }
        int len = c.JobCount;

        // Swap two positions in order
        int a = rnd.GetInt(0, len);
        int b = rnd.GetInt(0, len);
        var order = c.GetOrder();
        (order[a], order[b]) = (order[b], order[a]);
        c.SetOrder(order);

        // Flip a non-mandatory bit
        var ins = c.GetInspections();
        int idx;
        do
        {
            idx = rnd.GetInt(0, len);
        } while (c.Mandatory[idx]);
        ins[idx] = !ins[idx];
        c.SetInspections(ins);

        c.Repair();
    }
}
