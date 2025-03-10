using SimulationModeling;

class Program
{
    static void Main()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var parameters = ParametersReader.ReadParametersFromJson(
            "C:\\Users\\valer\\RiderProjects\\SimulationModeling\\SimulationModeling\\parameters.json");
        if (parameters == null)
        {
            Console.WriteLine("Ошибка чтения параметров из файла.");
            return;
        }

        var iterations = parameters.Iterations;

        var resultsDirectory = Path.Combine("C:\\Users\\valer\\RiderProjects\\SimulationModeling", "Results");
        Directory.CreateDirectory(resultsDirectory);

        var generator = new ParameterCombinationsGenerator(parameters);

        foreach (var combination in generator.GenerateCombinations())
        {
            var rng = new LinearCongruentialGenerator(123456789);

            var orderModel = new OrderModel(rng, combination.MeanCostOrder, Math.Sqrt(combination.OrderStdDev));
            var companyModel = new CompanyModel(rng, combination.Employees, combination.Salary, orderModel)
            {
                Alpha = combination.Alpha,
                Betta = combination.Beta
            };

            var profits = new double[iterations];

            var csvFileName =
                $"results_E{combination.Employees}_S{combination.Salary}_C{combination.AverageClientsMonth}_M{combination.MeanCostOrder}_D{combination.OrderStdDev}_A{combination.Alpha}_B{combination.Beta}.csv";
            var csvFilePath = Path.Combine(resultsDirectory, csvFileName);

            using (var writer = new StreamWriter(csvFilePath))
            {
                writer.WriteLine("Iteration,Profit,Clients,SuccessfulOrders");
                for (int i = 0; i < iterations; i++)
                {
                    profits[i] = companyModel.CalculateProfitMonth(combination.AverageClientsMonth,
                        out var amountClient, out var successOrders);
                    var profit = (int)profits[i];
                    writer.WriteLine($"{i + 1},{profit},{amountClient},{successOrders}");
                }
            }

            Console.WriteLine($"Данные сохранены в '{csvFilePath}'");
        }
        
        AggregatedHistogramBuilder.BuildAndSaveAggregatedHistograms(parameters, resultsDirectory, generator);

        stopwatch.Stop();
        TimeSpan ts = stopwatch.Elapsed;
        Console.WriteLine($"Время выполнения программы: {ts.TotalSeconds} секунд");
    }
}