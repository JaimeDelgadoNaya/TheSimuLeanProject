using ChapaGA.GA;
using System.Globalization;
using System.Text;

namespace ChapaGA.IO;

public static class CsvWriter
{
    public static void Write(string path, FitnessResult result)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("OrderIndex;Name;DoInspect;CompletionTime;DueDate;IsLate");
        foreach (var r in result.Rows)
        {
            writer.WriteLine($"{r.OrderIndex};{r.Chapa.Name};{(r.DoInspect ? 1 : 0)};{r.CompletionTime.ToString(CultureInfo.InvariantCulture)};{r.Chapa.DueDate.ToString(CultureInfo.InvariantCulture)};{(r.IsLate ? 1 : 0)}");
        }
        writer.WriteLine($"TotalInspections;{result.TotalInspections}");
        writer.WriteLine($"TotalDelays;{result.TotalDelays}");
    }
}
