namespace ChapaGA.Models;

public class Chapa
{
    public string Name { get; set; } = string.Empty;
    public double TSoldadura { get; set; }
    public double TInspeccion { get; set; }
    public bool InspeccionObligatoria { get; set; }
    public double DueDate { get; set; }
}
