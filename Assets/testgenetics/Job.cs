using UnityEngine;

namespace UnitySimuLean
{
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
