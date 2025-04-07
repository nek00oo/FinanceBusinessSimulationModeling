namespace SimulationModeling;

public class OrderModel
{
    public double OrderStdDev { get; set; }
    public double MeanCostOrder { get; set; }
    private LinearCongruentialGenerator _rng;
    
    public OrderModel(LinearCongruentialGenerator rng, double meanCostOrder, double orderStdDev)
    {
        if (meanCostOrder < 0)
            throw new ArgumentException($"Mean cost order must be greater than or equal to 0, your value: {meanCostOrder}");
        
        if (orderStdDev < 0 && orderStdDev > meanCostOrder)
            throw new ArgumentException($"Dispersion must be greater than or equal to 0 and more than mean cost order: {meanCostOrder}, your value: {orderStdDev}");
        
        OrderStdDev = orderStdDev;
        MeanCostOrder = meanCostOrder;
        _rng = rng;
    }
    public double CalculateCostOrder() => Distributions.NormalDistribution(_rng, MeanCostOrder, OrderStdDev);
}
