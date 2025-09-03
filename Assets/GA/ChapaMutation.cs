using System;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Randomizations;

namespace ChapasGA.GA
{
    public class ChapaMutation : MutationBase
    {
        protected override void PerformMutate(IChromosome chromosome, float probability)
        {
            var c = chromosome as ChapaChromosome ?? throw new ArgumentException("Invalid chromosome");
            var rnd = RandomizationProvider.Current;
            int size = c.LengthPerPart;
            if (rnd.GetDouble() <= probability)
            {
                var order = c.GetOrder();
                if (rnd.GetDouble() < 0.5)
                {
                    int i = rnd.GetInt(0, size);
                    int j = rnd.GetInt(0, size);
                    (order[i], order[j]) = (order[j], order[i]);
                }
                else
                {
                    int from = rnd.GetInt(0, size);
                    int to = rnd.GetInt(0, size);
                    var val = order[from];
                    var list = new System.Collections.Generic.List<int>(order);
                    list.RemoveAt(from);
                    list.Insert(to, val);
                    order = list.ToArray();
                }
                for (int k = 0; k < size; k++)
                {
                    c.ReplaceGene(k, new Gene(order[k]));
                }
            }
            if (rnd.GetDouble() <= probability)
            {
                int i = rnd.GetInt(0, size);
                var g = c.GetGene(size + i);
                int bit = (int)g.Value;
                bit = bit == 0 ? 1 : 0;
                c.ReplaceGene(size + i, new Gene(bit));
            }
            c.Repair();
        }
    }
}
