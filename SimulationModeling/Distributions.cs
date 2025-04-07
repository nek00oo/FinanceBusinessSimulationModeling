namespace SimulationModeling;

public static class Distributions
{
    /// <summary>
    /// Пуассоновского распределениe.
    /// </summary>
    /// <param name="rng">Генератор</param>
    /// <param name="lambda">Среднее количество за период</param>
    /// <returns>Количество</returns>
    public static int PoissonDistributions(LinearCongruentialGenerator rng, int lambda)
    {
        double L = Math.Exp(-lambda);
        int k = 0;
        double p = 1.0;

        do
        {
            k++;
            p *= rng.NextDouble();
        } while (p > L);

        return k - 1;
    }

    /// <summary>
    /// Нормальное распределение.
    /// </summary>
    /// <param name="rng">Генератор</param>
    /// <param name="mean">Среднее значение</param>
    /// <param name="stdDev">Стандартное отклонение</param>
    /// <returns>Случайное число из нормального распределения</returns>
    public static double NormalDistribution(LinearCongruentialGenerator rng, double mean, double stdDev)
    {
        double u1 = rng.NextDouble();
        double u2 = rng.NextDouble();
        double z0 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        return mean + stdDev * z0;
    }

    public static double BetaDistribution(LinearCongruentialGenerator rng, double alpha, double beta)
    {
        double x = GammaDistribution(rng, alpha, 1);
        double y = GammaDistribution(rng, beta, 1);
        return x / (x + y);
    }

    /// <summary>
    /// Гамма-распределение.
    /// </summary>
    /// <param name="rng">Генератор</param>
    /// <param name="shape">Параметр формы (альфа)</param>
    /// <param name="scale">Параметр масштаба (1/бета)</param>
    /// <returns>Случайное число из гамма-распределения</returns>
    public static double GammaDistribution(LinearCongruentialGenerator rng, double shape, double scale)
    {
        if (shape < 1)
        {
            var u = rng.NextDouble();
            var v = GammaDistribution(rng, 1 + shape, scale);
            return Math.Pow(u, 1.0 / shape) * v;
        }
        
        var d = shape - 1.0 / 3.0;
        var c = 1.0 / Math.Sqrt(9.0 * d);
        double x;

        while (true)
        {
            var u1 = rng.NextDouble();
            var u2 = rng.NextDouble();
            var v = Math.Log(u1 / (1.0 - u1));
            x = 1.0 + c * v;
            var x_cubed = x * x * x;

            if (x > 0 && u2 < 1.0 + 0.3678794411714423215956 * v * v * x_cubed)
            {
                return d * x_cubed * scale;
            }
        }
    }
}