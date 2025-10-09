using System;
using System.Collections.Generic;
using System.Linq;
using ChapasGA.Models;
using ChapasGA.GA.Optimization;

namespace ChapasGA.GA.Adapters
{
    /// <summary>
    /// Data transformer for Chapa objects.
    /// Implements IDataTransformer to convert Chapas into simulation format.
    /// </summary>
    public class ChapaDataTransformer : IDataTransformer<Chapa>
    {
        public Dictionary<string, List<string>> ConvertToDataDict(IList<Chapa> chapas)
        {
            return new Dictionary<string, List<string>>
            {
                ["Time"] = chapas.Select((c, i) => (i * 0.1).ToString()).ToList(),
                ["Name"] = chapas.Select(c => c.Name ?? "Chapa").ToList(),
                ["Q"] = chapas.Select(c => "1").ToList(),
                ["tSoldadura"] = chapas.Select(c => c.tSoldadura.ToString()).ToList(),
                ["tInspeccion"] = chapas.Select(c => c.tInspeccion.ToString()).ToList(),
                ["DueDate"] = chapas.Select(c => c.DueDate.ToString()).ToList()
            };
        }

        public double CalculateMaxSimTime(IList<Chapa> chapas)
        {
            double totalProcessTime = chapas.Sum(c => c.tSoldadura + c.tInspeccion);
            double arrivalTime = chapas.Count * 0.1;
            return arrivalTime + totalProcessTime * 1.5; // 50% safety margin
        }

        public string GetBinaryDecisionLabel()
        {
            return "inspeccionOn";
        }
    }
}
