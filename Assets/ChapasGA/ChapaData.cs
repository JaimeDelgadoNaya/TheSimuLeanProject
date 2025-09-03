using System;

namespace ChapasGA
{
    /// <summary>
    /// Represents a single chapa entry loaded from the Excel input.
    /// </summary>
    public class ChapaData
    {
        public string Name { get; set; } = string.Empty;
        public double SoldaduraTime { get; set; }
        public double InspeccionTime { get; set; }
        /// <summary>
        /// 1 if inspection is mandatory, 0 otherwise.
        /// </summary>
        public int InspeccionOn { get; set; }
        public double DueDate { get; set; }
    }
}
