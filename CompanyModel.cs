using MathNet.Numerics.Distributions;

namespace SimulationModeling;

public class CompanyModel
{
    // Параметры распределения Бета
    // Если _alpha > _beta, распределение скошено вправо
    // _alpha < _beta, распределение скошено влево
    private int _alpha;
    private int _beta;
    
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

    public int Alpha
    {
        get => _alpha;
        set
        {
            if (value <= 0)
                throw new ArgumentException($"Alpha must be greater than 0, your value: {value}");
            _alpha = value;
        }
    }

    public int Betta
    {
        get => _beta;
        set
        {
            if (value <= 0)
                throw new ArgumentException($"Beta must be greater than 0, your value: {value}");
            _beta = value;
        }
    }

    public double CalculateSalaryEmployee(int countEmployee, double salary) => salary * countEmployee;

    /// <summary>
    /// Вычисляет прибыль компании за месяц.
    /// </summary>
    /// <param name="averageClientMonth">Среднее количество клиентов в месяц</param>
    /// <param name="amountClientMonth">Итоговое количество клиентов</param>
    /// <param name="successOrders">Количество выполненных заказов</param>
    /// <returns>Прибыль за месяц</returns>
    public double CalculateProfitMonth(int averageClientMonth, out int amountClientMonth, out int successOrders)
    {
        double profitMonth = 0;
        successOrders = 0;
        amountClientMonth = Client.GetAmountClient(averageClientMonth);
        for (int j = 0; j < amountClientMonth; j++)
        {
            if (Rng.NextDouble() > CalculateChanceFailedOrder())
            {
                profitMonth += Order.CalculateCostOrder();
                successOrders++;
            }
        }
        
        profitMonth -= CalculateSalaryEmployee(_countEmployee, _averageSalary);
        
        AmountMoney += profitMonth;

        return profitMonth;
    }
    
    /// <summary>
    /// Вычисляет вероятность невыполнения заказа с использованием распределения Бета.
    /// </summary>
    /// <returns>Вероятность невыполнения заказа</returns>
    private double CalculateChanceFailedOrder() => Distributions.BetaDistribution(Rng, Alpha, Betta);
}