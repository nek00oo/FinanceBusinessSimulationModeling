namespace SimulationModeling;

public class LinearCongruentialGenerator(long seed, long a = 1664525, long c = 1013904223, long m = 4294967296)
{
    private long _seed = seed;

    /// <summary>
    /// Генерирует следующее псевдослучайное число.
    /// </summary>
    /// <returns>Псевдослучайное число в диапазоне [0, 1)</returns>
    public double NextDouble()
    {
        _seed = (a * _seed + c) % m;
        return (double)_seed / m;
    }
}

