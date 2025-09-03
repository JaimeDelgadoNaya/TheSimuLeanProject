using ChapaGA.Models;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace ChapaGA.IO;

public static class ExcelReader
{
    public static List<Chapa> ReadChapas(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Excel file not found: {path}");
        }

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        IWorkbook workbook = new XSSFWorkbook(fs);
        ISheet sheet = workbook.GetSheetAt(0);

        var headerRow = sheet.GetRow(0) ?? throw new Exception("Missing header row.");
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int c = 0; c < headerRow.LastCellNum; c++)
        {
            var cell = headerRow.GetCell(c);
            if (cell != null)
            {
                headers[cell.ToString().Trim()] = c;
            }
        }

        string[] required =
        {
            "Time","Name","Q","nRefuerzos","Referencia","tSoldadura","tInspeccion","inspeccionOn","DueDate","priorities"
        };
        foreach (var h in required)
        {
            if (!headers.ContainsKey(h))
            {
                throw new Exception($"Missing column '{h}' in Excel file.");
            }
        }

        var list = new List<Chapa>();
        for (int r = 1; r <= sheet.LastRowNum; r++)
        {
            var row = sheet.GetRow(r);
            if (row == null) continue;
            var name = row.GetCell(headers["Name"])?.ToString();
            if (string.IsNullOrWhiteSpace(name)) continue;
            var chapa = new Chapa
            {
                Name = name,
                TSoldadura = row.GetCell(headers["tSoldadura"]).NumericCellValue,
                TInspeccion = row.GetCell(headers["tInspeccion"]).NumericCellValue,
                InspeccionObligatoria = row.GetCell(headers["inspeccionOn"]).NumericCellValue == 1,
                DueDate = row.GetCell(headers["DueDate"]).NumericCellValue
            };
            list.Add(chapa);
        }

        return list;
    }
}
