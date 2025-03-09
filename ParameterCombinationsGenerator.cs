namespace SimulationModeling
{
    public class ParameterCombinationsGenerator
    {
        private Parameters _parameters;

        public ParameterCombinationsGenerator(Parameters parameters)
        {
            _parameters = parameters;
        }

        public IEnumerable<ParameterCombination> GenerateCombinations()
        {
            return 
                from t in _parameters.Employees
                from salary in _parameters.Salary
                from averageClientMonth in _parameters.AverageClientsMonth
                from meanCostOrder in _parameters.MeanCostOrder
                from orderStdDev in _parameters.OrderStdDev
                from alpha in _parameters.Alpha
                from beta in _parameters.Beta
                select new ParameterCombination
            (
                t,
                salary,
                averageClientMonth,
                meanCostOrder,
                orderStdDev,
                alpha,
                beta
            );
        }
    }
}

public record ParameterCombination(
    int Employees,
    double Salary,
    int AverageClientsMonth,
    double MeanCostOrder,
    double OrderStdDev,
    int Alpha,
    int Beta
);