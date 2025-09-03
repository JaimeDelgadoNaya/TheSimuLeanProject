using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using ChapasGA.Models;
using ExcelDataReader;
using UnityEngine;

namespace ChapasGA.IO
{
    public class ExcelChapaLoader
    {
        private static readonly string[] RequiredHeaders = { "Name", "tSoldadura", "inspeccionOn", "DueDate" };
        private const string InspectionHeader = "tInspeccion";

        public List<Chapa> LoadFromStreamingAssets(string excelFileName)
        {
            var path = Path.Combine(Application.streamingAssetsPath, excelFileName);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Excel file not found at {path}");
            }

            var result = new List<Chapa>();
            using var fs = File.OpenRead(path);
            using var reader = ExcelReaderFactory.CreateReader(fs);
            var ds = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
            });

            if (ds.Tables.Count == 0)
            {
                throw new Exception("Excel file contains no tables.");
            }

            var table = ds.Tables[0];
            foreach (var header in RequiredHeaders)
            {
                if (!table.Columns.Contains(header))
                {
                    throw new Exception($"Missing required column: {header}");
                }
            }

            foreach (DataRow row in table.Rows)
            {
                double tSold = Math.Max(0, Convert.ToDouble(row["tSoldadura"]));
                double tIns = table.Columns.Contains(InspectionHeader) && row[InspectionHeader] != DBNull.Value
                    ? Math.Max(0, Convert.ToDouble(row[InspectionHeader]))
                    : 0;
                int insOn = Convert.ToInt32(row["inspeccionOn"]);
                object dueObj = table.Columns.Contains("DueDate") ? row["DueDate"] : null;
                double due = 21600;
                if (dueObj != null && dueObj != DBNull.Value)
                {
                    double parsedDue = Convert.ToDouble(dueObj);
                    if (parsedDue > 0)
                    {
                        due = parsedDue;
                    }
                }

                var chapa = new Chapa
                {
                    Name = row["Name"].ToString(),
                    tSoldadura = tSold,
                    tInspeccion = tIns,
                    inspeccionOn = insOn,
                    DueDate = due
                };
                result.Add(chapa);
            }

            return result;
        }
    }
}
