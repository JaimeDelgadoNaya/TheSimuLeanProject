using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Randomizations;

namespace ChapasGA.GA
{
    public class ChapaMutation : IMutation
    {
        public bool IsOrdered => true;

        public void Mutate(IChromosome chromosome, float probability)
        {
            Mutate(chromosome);
        }

        public void Mutate(IChromosome chromosome)
        {
            var c = (ChapaChromosome)chromosome;
            int n = c.Order.Length;
            int i1 = RandomizationProvider.Current.GetInt(0, n);
            int i2 = RandomizationProvider.Current.GetInt(0, n);
            var tmp = c.Order[i1];
            c.Order[i1] = c.Order[i2];
            c.Order[i2] = tmp;

            int bit = RandomizationProvider.Current.GetInt(0, n);
            if (!c.InspectionRequired[bit])
            {
                c.Inspect[bit] = !c.Inspect[bit];
            }
            c.Repair();
        }
    }
}
