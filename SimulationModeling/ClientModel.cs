namespace SimulationModeling;

public class ClientModel(LinearCongruentialGenerator rng)
{
    /// <summary>
    /// Получает количество клиентов за месяц с использованием распределения Пуассона.
    /// </summary>
    /// <param name="averageClientMonth">Среднее количество клиентов в месяц</param>
    /// <returns>Количество клиентов за месяц</returns>
    public int GetAmountClient(int averageClientMonth) => Distributions.PoissonDistributions(rng, averageClientMonth);

}