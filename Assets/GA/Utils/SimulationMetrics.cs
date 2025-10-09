namespace ChapasGA.GA.Utils
{
    /// <summary>
    /// Encapsulates simulation result metrics with fitness calculation.
    /// Separates data from calculation logic for better testability and reusability.
    /// </summary>
    public class SimulationMetrics
    {
        public int TotalItems { get; set; }
        public int TotalInspections { get; set; }
        public int TotalDelays { get; set; }
        public double SimulationTime { get; set; }
        public int QueueLength { get; set; }

        // Fitness weights (can be configured)
        public double DelayPenalty { get; set; } = 100.0;
        public double InspectionReward { get; set; } = 10.0;
        
        /// <summary>
        /// Calculates fitness based on configured weights.
        /// Higher fitness = better solution (fewer delays, more inspections)
        /// </summary>
        public double CalculateFitness()
        {
            double fitness = 0;
            fitness -= TotalDelays * DelayPenalty;      // Penalize delays
            fitness += TotalInspections * InspectionReward;  // Reward inspections
            return fitness;
        }

        public override string ToString()
        {
            return $"Items: {TotalItems}, Inspections: {TotalInspections}, Delays: {TotalDelays}, " +
                   $"Time: {SimulationTime:F2}s, Queue: {QueueLength}, Fitness: {CalculateFitness():F2}";
        }
    }
}
