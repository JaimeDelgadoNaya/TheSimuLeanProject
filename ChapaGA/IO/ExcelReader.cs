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
        using var workbook = new XSSFWorkbook(fs);
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
            var name = row.GetCell(headers["Name"], MissingCellPolicy.RETURN_BLANK_AS_NULL)?.ToString();
            if (string.IsNullOrWhiteSpace(name)) continue;
            double tSoldadura = GetNumericCell(row, headers["tSoldadura"], "tSoldadura", r + 1);
            double tInspeccion = GetNumericCell(row, headers["tInspeccion"], "tInspeccion", r + 1);
            bool inspeccionOn = GetNumericCell(row, headers["inspeccionOn"], "inspeccionOn", r + 1) == 1;
            double dueDate = GetNumericCell(row, headers["DueDate"], "DueDate", r + 1);
            var chapa = new Chapa
            {
                Name = name!,
                TSoldadura = tSoldadura,
                TInspeccion = tInspeccion,
                InspeccionObligatoria = inspeccionOn,
                DueDate = dueDate
            };
            list.Add(chapa);
        }

        return list;
    }

    private static double GetNumericCell(IRow row, int index, string column, int rowNumber)
    {
        var cell = row.GetCell(index, MissingCellPolicy.RETURN_BLANK_AS_NULL);
        if (cell == null || cell.CellType != CellType.Numeric)
        {
            throw new Exception($"Cell '{column}' at row {rowNumber} is not numeric or is missing.");
        }
        return cell.NumericCellValue;
    }
}
