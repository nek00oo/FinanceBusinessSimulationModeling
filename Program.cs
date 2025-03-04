using Newtonsoft.Json;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.SkiaSharp;
using OxyPlot.Series;

namespace SimulationModeling
{
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
            var employees = parameters.Employees;
            var salary = parameters.Salary;
            var averageClientsMonth = parameters.AverageClientsMonth;
            var meanCostOrder = parameters.MeanCostOrder;
            var orderStdDev = Math.Sqrt(parameters.OrderStdDev);
            var alpha = parameters.Alpha;
            var beta = parameters.Beta;

            var rng = new LinearCongruentialGenerator(123456789);

            var orderModel = new OrderModel(rng, meanCostOrder, orderStdDev);
            var companyModel = new CompanyModel(rng, employees, salary, orderModel)
            {
                Alpha = alpha,
                Betta = beta
            };

            var profits = new double[iterations];

            var csvFilePath = Path.Combine("C:\\Users\\valer\\RiderProjects\\SimulationModeling",
                "simulation_results.csv");

            using (var writer = new StreamWriter(csvFilePath))
            {
                writer.WriteLine("Iteration,Profit,Clients,SuccessfulOrders");
                for (int i = 0; i < iterations; i++)
                {
                    profits[i] = companyModel.CalculateProfitMonth(averageClientsMonth, out var amountClient,
                        out var successOrders);
                    var profit = (int)profits[i];
                    writer.WriteLine($"{i + 1},{profit},{amountClient},{successOrders}");
                }
            }

            var plotModel = new PlotModel
            {
                Title = "Распределение прибыли бизнеса",
                TitleColor = OxyColors.White,
                PlotAreaBorderColor = OxyColors.White,
            };
            var histogramSeries = new HistogramSeries
            {
                Title = "Прибыль",
                StrokeThickness = 1,
                FillColor = OxyColor.FromRgb(0, 0, 255),
                StrokeColor = OxyColor.FromRgb(255, 255, 255)
            };

            // Количество бинов по правилу Стёрджесса
            var binCount = (int)Math.Ceiling(1 + 3.322 * Math.Log10(profits.Length));

            double min = profits.Min();
            double max = profits.Max();

            var bins = HistogramHelpers.CreateUniformBins(min, max, binCount);

            var binningOptions = new BinningOptions(BinningOutlierMode.RejectOutliers,
                BinningIntervalType.InclusiveLowerBound,
                BinningExtremeValueMode.IncludeExtremeValues);

            var items = HistogramHelpers.Collect(profits, bins, binningOptions);

            histogramSeries.Items.AddRange(items);

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Прибыль",
                MinimumPadding = 0,
                MaximumPadding = 0,
                TextColor = OxyColors.White,
                TitleColor = OxyColors.White,
                TicklineColor = OxyColors.White,
                AxislineColor = OxyColors.White,
            });

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Количество результатов в данном диапазоне",
                TextColor = OxyColors.White,
                TitleColor = OxyColors.White,
                TicklineColor = OxyColors.White,
                AxislineColor = OxyColors.White,
            });

            plotModel.Series.Add(histogramSeries);

            var projectDirectory = "C:\\Users\\valer\\RiderProjects\\SimulationModeling";
            var filePath = Path.Combine(projectDirectory, "profit_distribution_2.png");

            using (var stream = File.Create(filePath))
            {
                var exporter = new PngExporter { Width = 1200, Height = 800 };
                exporter.Export(plotModel, stream);
            }

            Console.WriteLine($"График сохранён в '{filePath}'");

            stopwatch.Stop();
            TimeSpan ts = stopwatch.Elapsed;
            Console.WriteLine($"Время выполнения программы: {ts.TotalSeconds} секунд");
            Console.WriteLine($"Итоговая сумма за {iterations} итераций: {companyModel.AmountMoney}р");
        }
    }
    
}