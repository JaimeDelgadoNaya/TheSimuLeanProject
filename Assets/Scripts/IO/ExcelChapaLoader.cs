using System.Collections.Generic;
using System.IO;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEngine;
using ChapasGA.Models;

namespace ChapasGA.IO
{
    public class ExcelChapaLoader
    {
        public List<Chapa> LoadFromStreamingAssets(string fileName)
        {
            var path = Path.Combine(Application.streamingAssetsPath, fileName);
            var chapas = new List<Chapa>();
            if (!File.Exists(path))
            {
                Debug.LogError($"Excel file not found at {path}");
                return chapas;
            }

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = new XSSFWorkbook(fs);
                ISheet sheet = workbook.GetSheetAt(0);
                for (int i = 1; i <= sheet.LastRowNum; i++)
                {
                    var row = sheet.GetRow(i);
                    if (row == null) continue;
                    string name = row.GetCell(1)?.ToString();
                    double tSold = row.GetCell(5)?.NumericCellValue ?? 0;
                    double tInsp = row.GetCell(6)?.NumericCellValue ?? 0;
                    bool inspOn = (row.GetCell(7)?.NumericCellValue ?? 0) > 0;
                    double dueDate = row.GetCell(8)?.NumericCellValue ?? 0;
                    if (string.IsNullOrEmpty(name)) continue;
                    chapas.Add(new Chapa
                    {
                        Name = name,
                        TSoldadura = tSold,
                        TInspeccion = tInsp,
                        InspeccionOn = inspOn,
                        DueDate = dueDate
                    });
                }
            }
            return chapas;
        }
    }
}
