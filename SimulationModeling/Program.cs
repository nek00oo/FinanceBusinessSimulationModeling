using GeneticSharp;
using SimulationModeling;
using System;
using System.Diagnostics;
using System.Linq;

class Program
{
    static void Main()
    {
        var stopwatch = Stopwatch.StartNew();

        var parameters = ParametersReader.ReadParametersFromJson(
            "C:\\Users\\valer\\RiderProjects\\SimulationModeling\\SimulationModeling\\parameters.json");
        if (parameters == null) return;

        var chromosome = new Chromosome(
            employees: parameters.Employees.ToArray(),
            salary: parameters.Salary.ToArray(),
            clients: parameters.AverageClientsMonth.ToArray(),
            meanCost: parameters.MeanCostOrder.ToArray(),
            stdDev: parameters.OrderStdDev.ToArray()
        );
        
        var population = new Population(15, 30, chromosome);
        var fitnessFunction = new FitnessFunction(parameters.Iterations);
        var selection = new TournamentSelection();
        var crossover = new UniformCrossover();
        var mutation = new UniformMutation();
        
        var ga = new GeneticAlgorithm(
            population: population,
            fitness: fitnessFunction,
            selection: selection,
            crossover: crossover,
            mutation: mutation)
        {
            Termination = new GenerationNumberTermination(3),
            MutationProbability = 0.25f,
            CrossoverProbability = 0.85f
        };

        ga.Start();
        var bestChromosome = ga.BestChromosome as Chromosome;
        
        if (bestChromosome != null)
        {
            var bestParams = bestChromosome.GetParameters();
            Console.WriteLine("\nОптимальные параметры:");
            Console.WriteLine($"Employees: {bestParams.Employees}");
            Console.WriteLine($"Salary: {bestParams.Salary:N0}");
            Console.WriteLine($"Average Clients: {bestParams.AverageClientsMonth}");
            Console.WriteLine($"Mean Cost: {bestParams.MeanCostOrder:N0}");
            Console.WriteLine($"StdDev: {bestParams.OrderStdDev:N0}");
            Console.WriteLine($"Profit: {bestChromosome.Fitness:N0}");
        }

        Console.WriteLine($"\nВремя выполнения: {stopwatch.Elapsed.TotalSeconds:N1} сек");
    }
}

class Chromosome : ChromosomeBase
{
    private readonly int[] _employees;
    private readonly double[] _salary;
    private readonly int[] _clients;
    private readonly double[] _meanCost;
    private readonly double[] _stdDev;

    public Chromosome(
        int[] employees, 
        double[] salary, 
        int[] clients, 
        double[] meanCost, 
        double[] stdDev) : base(5)
    {
        _employees = employees;
        _salary = salary;
        _clients = clients;
        _meanCost = meanCost;
        _stdDev = stdDev;
        
        CreateGenes();
    }

    public override Gene GenerateGene(int geneIndex)
    {
        return geneIndex switch
        {
            0 => new Gene(_employees.OrderBy(x => Guid.NewGuid()).First()),
            1 => new Gene(_salary.OrderBy(x => Guid.NewGuid()).First()),
            2 => new Gene(_clients.OrderBy(x => Guid.NewGuid()).First()),
            3 => new Gene(_meanCost.OrderBy(x => Guid.NewGuid()).First()),
            4 => new Gene(_stdDev.OrderBy(x => Guid.NewGuid()).First()),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public override IChromosome CreateNew() => new Chromosome(_employees, _salary, _clients, _meanCost, _stdDev);

    public ParameterCombination GetParameters() => new(
        (int)GetGene(0).Value,
        (double)GetGene(1).Value,
        (int)GetGene(2).Value,
        (double)GetGene(3).Value,
        (double)GetGene(4).Value
    );
}

class FitnessFunction : IFitness
{
    private readonly int _iterations;

    public FitnessFunction(int iterations) => _iterations = iterations;

    public double Evaluate(IChromosome chromosome)
    {
        var combo = (chromosome as Chromosome)!.GetParameters();
        var rng = new LinearCongruentialGenerator(123456789);
        var model = new CompanyModel(
            rng,
            combo.Employees,
            combo.Salary,
            new OrderModel(rng, combo.MeanCostOrder, Math.Sqrt(combo.OrderStdDev))
        );

        double total = 0;
        for (int i = 0; i < _iterations; i++)
            total += model.CalculateProfitMonth(combo.AverageClientsMonth, out _, out _);

        return total / _iterations;
    }
}