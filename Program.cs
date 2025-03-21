using SimulationModeling;
using SimulationModeling.Builders;
using System.Diagnostics;

class Program
{
    static void Main()
    {
        var stopwatch = Stopwatch.StartNew();

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

        // Проверяем, все ли параметры имеют только одно значение
        bool isSingleCombination = parameters.Employees.Count == 1 &&
                                   parameters.Salary.Count == 1 &&
                                   parameters.AverageClientsMonth.Count == 1 &&
                                   parameters.MeanCostOrder.Count == 1 &&
                                   parameters.OrderStdDev.Count == 1;

        foreach (var combination in generator.GenerateCombinations())
        {
            var rng = new LinearCongruentialGenerator(123456789);

            var orderModel = new OrderModel(rng, combination.MeanCostOrder, Math.Sqrt(combination.OrderStdDev));
            var companyModel = new CompanyModel(rng, combination.Employees, combination.Salary, orderModel);

            var profits = new double[iterations];

            var csvFileName =
                $"results_E{combination.Employees}_S{combination.Salary}_C{combination.AverageClientsMonth}_M{combination.MeanCostOrder}_D{combination.OrderStdDev}.csv";
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

            // Если все параметры имеют одно значение, строим обычный график
            if (isSingleCombination)
            {
                var plotFileName = "SINGLE_HISTOGRAM.png";
                var plotFilePath = Path.Combine(resultsDirectory, plotFileName);

                PlotBuilder.BuildAndSaveHistogram(profits, plotFilePath, combination);
            }
        }

        // Если есть вариации параметров, строим агрегированные графики
        if (!isSingleCombination)
        {
            AggregatedHistogramBuilder.BuildAndSaveAggregatedHistograms(parameters, resultsDirectory, generator);
        }

        stopwatch.Stop();
        TimeSpan ts = stopwatch.Elapsed;
        Console.WriteLine($"Время выполнения программы: {ts.TotalSeconds} секунд");
    }
}