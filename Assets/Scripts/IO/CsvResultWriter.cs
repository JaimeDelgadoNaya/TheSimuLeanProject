using System.Collections.Generic;
using System.IO;
using ChapasGA.Models;

namespace ChapasGA.IO
{
    public class CsvResultWriter
    {
        public void Write(string path, IList<Chapa> chapas, int[] order, bool[] inspect, IList<double> completion, int totalInspections, int totalDelays)
        {
            using (var sw = new StreamWriter(path, false))
            {
                sw.WriteLine("OrderIndex;Name;DoInspect;CompletionTime;DueDate;IsLate");
                for (int i = 0; i < order.Length; i++)
                {
                    var c = chapas[order[i]];
                    bool late = completion[i] > c.DueDate;
                    sw.WriteLine($"{i};{c.Name};{(inspect[i] ? 1 : 0)};{completion[i]};{c.DueDate};{(late ? 1 : 0)}");
                }
                sw.WriteLine($"TotalInspections;{totalInspections}");
                sw.WriteLine($"TotalDelays;{totalDelays}");
            }
        }
    }
}
