namespace SimulationModeling;

public class Parameters
{
    public List<int> Employees { get; set; } = new();
    public List<double> Salary { get; set; } = new();
    public List<int> AverageClientsMonth { get; set; } = new();
    public List<double> MeanCostOrder { get; set; } = new();
    public List<double> OrderStdDev { get; set; } = new();
    public List<int> Alpha { get; set; } = new();
    public List<int> Beta { get; set; } = new();
    public int Iterations { get; set; }
}