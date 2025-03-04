namespace SimulationModeling;

public class OrderModel
{
    private double _orderStdDev;
    private double _meanCostOrder;
    private LinearCongruentialGenerator _rng;
    
    public OrderModel(LinearCongruentialGenerator rng, double meanCostOrder, double orderStdDev)
    {
        if (meanCostOrder < 0)
            throw new ArgumentException($"Mean cost order must be greater than or equal to 0, your value: {meanCostOrder}");
        
        if (orderStdDev < 0 && orderStdDev > meanCostOrder)
            throw new ArgumentException($"Dispersion must be greater than or equal to 0 and more than mean cost order: {meanCostOrder}, your value: {orderStdDev}");
        
        _orderStdDev = orderStdDev;
        _meanCostOrder = meanCostOrder;
        _rng = rng;
    }
    public double CalculateCostOrder() => Distributions.NormalDistribution(_rng, _meanCostOrder, _orderStdDev);
}
