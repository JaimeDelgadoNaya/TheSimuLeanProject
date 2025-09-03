using System;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Infrastructure.Framework.Threading;

namespace ChapasGA.GA
{
    public class ChapaChromosome : ChromosomeBase
    {
        private readonly int _jobs;
        private readonly bool[] _inspectionRequired;
        private int[] _order;
        private bool[] _inspect;

        public ChapaChromosome(int jobs, bool[] inspectionRequired) : base(2 * jobs)
        {
            _jobs = jobs;
            _inspectionRequired = inspectionRequired;
            InitializeRandom();
        }

        private ChapaChromosome(ChapaChromosome source) : base(source.Length)
        {
            _jobs = source._jobs;
            _inspectionRequired = source._inspectionRequired;
            _order = source._order.ToArray();
            _inspect = source._inspect.ToArray();
            SyncGenes();
        }

        public int[] Order
        {
            get => _order;
            set { _order = value; SyncGenes(); }
        }

        public bool[] Inspect
        {
            get => _inspect;
            set { _inspect = value; SyncGenes(); }
        }

        public bool[] InspectionRequired => _inspectionRequired;

        private void InitializeRandom()
        {
            _order = GeneticSharp.Domain.Randomizations.RandomizationProvider.Current.GetPermutation(_jobs);
            _inspect = new bool[_jobs];
            for (int i = 0; i < _jobs; i++)
            {
                _inspect[i] = _inspectionRequired[i] || GeneticSharp.Domain.Randomizations.RandomizationProvider.Current.GetDouble() > 0.5;
            }
            SyncGenes();
        }

        private void SyncGenes()
        {
            for (int i = 0; i < _jobs; i++)
            {
                ReplaceGene(i, new Gene(_order[i]));
                ReplaceGene(_jobs + i, new Gene(_inspect[i] ? 1 : 0));
            }
        }

        public void Repair()
        {
            for (int i = 0; i < _jobs; i++)
            {
                if (_inspectionRequired[i])
                    _inspect[i] = true;
            }
            SyncGenes();
        }

        public override Gene GenerateGene(int geneIndex)
        {
            if (geneIndex < _jobs)
                return new Gene(_order[geneIndex]);
            return new Gene(_inspect[geneIndex - _jobs] ? 1 : 0);
        }

        public override IChromosome CreateNew()
        {
            return new ChapaChromosome(_jobs, _inspectionRequired);
        }

        public override IChromosome Clone()
        {
            return new ChapaChromosome(this);
        }

        public static ChapaChromosome CreateFromData(System.Collections.Generic.IList<Models.Chapa> chapas)
        {
            var required = chapas.Select(c => c.InspeccionOn).ToArray();
            var c = new ChapaChromosome(chapas.Count, required);
            c._order = Enumerable.Range(0, chapas.Count).ToArray();
            c._inspect = chapas.Select(ch => ch.InspeccionOn).ToArray();
            c.SyncGenes();
            return c;
        }
    }
}
