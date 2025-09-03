using System.Collections.Generic;
using System.IO;
using ChapasGA.Models;
using UnityEngine;

namespace ChapasGA.IO
{
    public class CsvResultWriter
    {
        public string WriteResult(string fileName, IList<Chapa> chapas, IList<int> order, int[] bits, double[] completionTimes, int inspections, int delays)
        {
            var path = Path.Combine(Application.persistentDataPath, fileName);
            using var sw = new StreamWriter(path);
            sw.WriteLine("OrderIndex;Name;DoInspect;CompletionTime;DueDate;IsLate");
            for (int i = 0; i < order.Count; i++)
            {
                int idx = order[i];
                var c = chapas[idx];
                bool doInspect = bits[idx] == 1;
                double completion = completionTimes[i];
                bool isLate = completion > c.DueDate;
                sw.WriteLine($"{i};{c.Name};{(doInspect ? 1 : 0)};{completion};{c.DueDate};{(isLate ? 1 : 0)}");
            }
            sw.WriteLine($"TotalInspections;{inspections}");
            sw.WriteLine($"TotalDelays;{delays}");
            return path;
        }
    }
}
