using System;
using System.Collections.Generic;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Reinsertions;

namespace ChapasGA.GA
{
    public class ConfigurableElitistReinsertion : ReinsertionBase
    {
        private readonly float _elitismPercentage;

        public ConfigurableElitistReinsertion(float elitismPercentage) : base(false, true)
        {
            _elitismPercentage = elitismPercentage;
        }

        protected override IList<IChromosome> PerformSelectChromosomes(IPopulation population, IList<IChromosome> offspring, IList<IChromosome> parents)
        {
            int elitismCount = (int)Math.Round(population.MinSize * _elitismPercentage);
            elitismCount = Math.Min(elitismCount, Math.Min(parents.Count, population.MinSize));

            var selected = parents.OrderByDescending(p => p.Fitness).Take(elitismCount).ToList();
            int remaining = population.MinSize - selected.Count;
            if (remaining > 0)
            {
                selected.AddRange(offspring.OrderByDescending(o => o.Fitness).Take(remaining));
            }

            return selected;
        }
    }
}
