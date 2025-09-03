using System;

namespace ChapasGA.Models
{
    [Serializable]
    public class Chapa
    {
        public string Name;
        public double TSoldadura;
        public double TInspeccion;
        public bool InspeccionOn;
        public double DueDate;
    }
}
