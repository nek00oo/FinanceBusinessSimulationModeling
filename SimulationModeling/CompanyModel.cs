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
        Client = new ClientModel(rng);
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
            totalComplexity += failChance;
            
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
        
        // Оптимальное количество сотрудников
        double avgComplexity = totalComplexity / amountClientMonth;
        double optimalEmployees = CalculateOptimalEmployees(avgComplexity, amountClientMonth);
        
        // Зарплатные расходы
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
        // Учитываем нагрузку и сложность заказов
        double loadFactor = (double)Client.GetAmountClient(1) / _countEmployee;
        double complexityFactor = Order.OrderStdDev / Order.MeanCostOrder;
        
        // Логарифмическая нормализация нагрузки
        double normalizedLoad = Math.Log(1 + loadFactor);
        return Math.Clamp(
            0.6 * normalizedLoad + 0.4 * complexityFactor,
            0.01, 
            0.99
        );
    }

    private double CalculateDynamicBeta()
    {
        // Beta пропорциональна количеству сотрудников
        return Math.Clamp(_countEmployee * 0.05, 0.01, 0.99);
    }

    private double CalculateOptimalEmployees(double complexity, int clients)
    {
        // Нелинейная зависимость: учитываем сложность и количество клиентов
        return 0.5 * clients + 10 * complexity * complexity;
    }

    private double CalculateSalaryCost(double optimalEmployees, double baseSalary)
    {
        double deviation = _countEmployee - optimalEmployees;
        // Кубическая зависимость штрафа
        return _countEmployee * baseSalary * (1 + 0.001 * Math.Pow(deviation, 3));
    }
}