using System;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Randomizations;

namespace UnitySimuLean
{
    /// <summary>
    /// Mutation operator that swaps two positions in the order part and flips a
    /// bit in the inspection part of the chromosome.
    /// </summary>
    public class ScheduleMutation : MutationBase
    {
        private readonly OrderMutationType _orderType;

        public ScheduleMutation(OrderMutationType orderType) : base(false)
        {
            _orderType = orderType;
        }

        protected override void PerformMutate(IChromosome chromosome, double probability)
        {
            var sch = (ScheduleChromosome)chromosome;
            var order = sch.GetOrder();
            var bits = sch.GetInspectionVector();
            var rnd = RandomizationProvider.Current;

            if (rnd.GetDouble() <= probability)
            {
                int i = rnd.GetInt(0, order.Length);
                int j = rnd.GetInt(0, order.Length);
                if (_orderType == OrderMutationType.Swap)
                {
                    var temp = order[i];
                    order[i] = order[j];
                    order[j] = temp;
                }
                else
                {
                    var temp = order[i];
                    order[i] = order[j];
                    order[j] = temp;
                }
            }

            if (rnd.GetDouble() <= probability)
            {
                int idx = rnd.GetInt(0, bits.Length);
                bits[idx] = !bits[idx];
            }

            sch.SetData(order, bits);
        }
    }
}
