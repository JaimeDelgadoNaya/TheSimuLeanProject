using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using ExcelDataReader;
using UnityEngine;

namespace UnitySimuLean
{
    /// <summary>
    /// Loads job data for the genetic algorithm from an Excel file located in
    /// the StreamingAssets folder. The file must contain a worksheet with the
    /// headers: Time, Name, Q, nRefuerzos, Referencia, tSoldadura,
    /// tInspeccion, inspeccionOn, DueDate and priorities.
    /// Only a subset of these fields is used to build the job list.
    /// </summary>
    public static class ExcelJobLoader
    {
        private static readonly string[] RequiredHeaders = new[]
        {
            "Time", "Name", "Q", "nRefuerzos", "Referencia",
            "tSoldadura", "tInspeccion", "inspeccionOn",
            "DueDate", "priorities"
        };

        /// <summary>
        /// Reads the specified Excel file and returns the list of jobs for the
        /// optimization model. The file is searched under
        /// Application.streamingAssetsPath.
        /// </summary>
        /// <param name="excelFileName">Excel file name.</param>
        /// <returns>List of jobs parsed from the first worksheet.</returns>
        /// <exception cref="FileNotFoundException">Thrown when the file cannot be found.</exception>
        /// <exception cref="InvalidDataException">Thrown when any required column is missing.</exception>
        public static List<Job> LoadJobs(string excelFileName)
        {
            var path = Path.Combine(Application.streamingAssetsPath, excelFileName);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Excel file not found at {path}");
            }

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using var fs = File.OpenRead(path);
            using var reader = ExcelReaderFactory.CreateReader(fs);
            var ds = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
            });

            if (ds.Tables.Count == 0)
            {
                throw new InvalidDataException("Excel file does not contain any worksheet.");
            }

            var table = ds.Tables[0];

            foreach (var header in RequiredHeaders)
            {
                if (!table.Columns.Contains(header))
                {
                    throw new InvalidDataException($"Missing required column '{header}'.");
                }
            }

            var jobs = new List<Job>();
            foreach (DataRow row in table.Rows)
            {
                var name = row["Name"]?.ToString();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var job = new Job
                {
                    Name = name,
                    tSoldadura = TryParseDouble(row["tSoldadura"]),
                    tInspeccion = TryParseDouble(row["tInspeccion"]),
                    inspeccionOn = (int)TryParseDouble(row["inspeccionOn"]),
                    DueDate = TryParseDouble(row["DueDate"])
                };

                jobs.Add(job);
            }

            return jobs;
        }

        private static double TryParseDouble(object value)
        {
            if (value == null)
            {
                return 0d;
            }

            if (double.TryParse(value.ToString(), out var result))
            {
                return result;
            }

            return 0d;
        }
    }

    /// <summary>
    /// Represents a single job in the scheduling model.
    /// </summary>
    public class Job
    {
        public string Name;
        public double tSoldadura;
        public double tInspeccion;
        public int inspeccionOn;
        public double DueDate;
    }
}

