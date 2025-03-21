using MathNet.Numerics.Distributions;

namespace SimulationModeling;

public class CompanyModel
{
    private int _countEmployee;
    private double _averageSalary;
    
    public double AmountMoney { get; private set; }
    public ClientModel Client { get; set; }
    public OrderModel Order { get; set; }
    public LinearCongruentialGenerator Rng { get; set; }
    
    public CompanyModel(LinearCongruentialGenerator rng, int countEmployee, double averageSalary, OrderModel order)
    {
        Rng = rng;
        _countEmployee = countEmployee;
        _averageSalary = averageSalary;
        Order = order;
        Client = new ClientModel(Rng);
    }

    public double CalculateProfitMonth(int averageClientMonth, out int amountClientMonth, out int successOrders)
    {
        double profitMonth = 0;
        successOrders = 0;
        amountClientMonth = Client.GetAmountClient(averageClientMonth);
    
        // Параметры системы мотивации
        double riskPenalty = 0.15;
        double riskBonus = 0.20;
        double baseSalary = _averageSalary;

        // Рассчитываем среднюю сложность за месяц
        double totalComplexity = 0;
        for (int j = 0; j < amountClientMonth; j++)
        {
            double failChance = CalculateChanceFailedOrder();
            totalComplexity += failChance; // Используем шанс провала как меру сложности
            
            bool isSuccess = Rng.NextDouble() > failChance;
            double orderValue = Order.CalculateCostOrder() * (1 + Math.Pow(1 - failChance, 2));
            
            if (isSuccess)
            {
                profitMonth += orderValue;
                successOrders++;
                
                if (failChance > 0.7)
                {
                    profitMonth += orderValue * riskBonus;
                }
            }
            else
            {
                if (failChance < 0.3)
                {
                    baseSalary *= (1 + riskPenalty);
                }
            }
        }
        
        // Оптимальное количество сотрудников на основе средней сложности
        double avgComplexity = totalComplexity / amountClientMonth;
        double optimalEmployees = CalculateOptimalEmployees(avgComplexity);
        
        // Зарплатные расходы с адаптивной зависимостью
        double salaryCost = CalculateSalaryCost(optimalEmployees, baseSalary);
        
        profitMonth -= salaryCost;
        AmountMoney += profitMonth;
        
        return profitMonth;
    }

    private double CalculateChanceFailedOrder()
    {
        double alpha = CalculateDynamicAlpha();
        double beta = CalculateDynamicBeta();
        return Distributions.BetaDistribution(Rng, alpha, beta);
    }

    private double CalculateDynamicAlpha()
    {
        // Факторы риска
        double loadFactor = (double)Client.GetAmountClient(1) / _countEmployee;
        double complexityFactor = Order.OrderStdDev / Order.MeanCostOrder;
        
        // Нормализация
        double normalizedLoad = Normalize(loadFactor, 0.2, 5.0);
        double normalizedComplexity = Normalize(complexityFactor, 0.1, 1.0);
        
        return Math.Clamp(
            0.6 * normalizedLoad + 0.4 * normalizedComplexity,
            0.01, // Минимальное значение alpha
            0.99  // Максимальное значение alpha
        );
    }

    private double CalculateDynamicBeta()
    {
        // Нормализация количества сотрудников
        return Math.Clamp(
            Normalize(_countEmployee, 5, 20),
            0.01, // Минимальное значение beta
            0.99   // Максимальное значение beta
        );
    }

    private double CalculateOptimalEmployees(double complexity)
    {
        // Оптимальное количество сотрудников линейно зависит от сложности
        return 5 + 15 * complexity; // Диапазон от 5 до 20 сотрудников
    }

    private double CalculateSalaryCost(double optimalEmployees, double baseSalary)
    {
        // Квадратичная зависимость от отклонения от оптимала
        double deviation = _countEmployee - optimalEmployees;
        return _countEmployee * baseSalary * (1 + 0.03 * deviation * deviation);
    }

    private double Normalize(double value, double min, double max)
    {
        return Math.Max(0, Math.Min(1, (value - min) / (max - min)));
    }
}