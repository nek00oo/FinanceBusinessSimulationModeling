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
                from employees in _parameters.Employees
                from salary in _parameters.Salary
                from averageClientMonth in _parameters.AverageClientsMonth
                from meanCostOrder in _parameters.MeanCostOrder
                from orderStdDev in _parameters.OrderStdDev
                select new ParameterCombination
            (
                employees,
                salary,
                averageClientMonth,
                meanCostOrder,
                orderStdDev
            );
        }
    }
}

public record ParameterCombination(
    int Employees,
    double Salary,
    int AverageClientsMonth,
    double MeanCostOrder,
    double OrderStdDev
);