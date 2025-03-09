using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;
using SimulationModeling;

public static class AggregatedHistogramBuilder
{
    private static readonly Random _random = new Random(42);

    public static void BuildAndSaveAggregatedHistograms(Parameters parameters, string resultsDirectory, ParameterCombinationsGenerator generator)
    {
        var iterations = parameters.Iterations;

        // Создаем поддиректорию AggregatedHistograms внутри Results
        var aggregatedResultsDir = Path.Combine(resultsDirectory, "AggregatedHistograms");
        Directory.CreateDirectory(aggregatedResultsDir);

        // Строим гистограммы для каждого параметра
        BuildGroupedHistogram(parameters, iterations, resultsDirectory, generator, param => param.Beta, "Beta", "Beta Variations");
        BuildGroupedHistogram(parameters, iterations, resultsDirectory, generator, param => param.Alpha, "Alpha", "Alpha Variations");
        BuildGroupedHistogram(parameters, iterations, resultsDirectory, generator, param => param.OrderStdDev, "OrderStdDev", "Order StdDev Variations");
        BuildGroupedHistogram(parameters, iterations, resultsDirectory, generator, param => param.MeanCostOrder, "MeanCostOrder", "Mean Cost Order Variations");
        BuildGroupedHistogram(parameters, iterations, resultsDirectory, generator, param => param.AverageClientsMonth, "AverageClientsMonth", "Average Clients Month Variations");
        BuildGroupedHistogram(parameters, iterations, resultsDirectory, generator, param => param.Employees, "Employees", "Employees Variations");
        BuildGroupedHistogram(parameters, iterations, resultsDirectory, generator, param => param.Salary, "Salary", "Salary Variations");
    }

    private static void BuildGroupedHistogram(Parameters parameters, int iterations, string resultsDirectory,
        ParameterCombinationsGenerator generator, Func<ParameterCombination, object> varyingParamSelector,
        string paramName, string groupTitle)
    {
        foreach (var fixedCombination in GetFixedCombinations(parameters, varyingParamSelector))
        {
            var plotModel = new PlotModel
            {
                Title =
                    $"{groupTitle} (E={fixedCombination.Employees}, S={fixedCombination.Salary}, C={fixedCombination.AverageClientsMonth}, M={fixedCombination.MeanCostOrder}, D={fixedCombination.OrderStdDev}, A={fixedCombination.Alpha})",
                TitleColor = OxyColors.White,
                Background = OxyColors.Black
            };

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Прибыль",
                TextColor = OxyColors.White,
                TitleColor = OxyColors.White,
                TicklineColor = OxyColors.White,
                AxislineColor = OxyColors.White,
            });

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Количество результатов",
                TextColor = OxyColors.White,
                TitleColor = OxyColors.White,
                TicklineColor = OxyColors.White,
                AxislineColor = OxyColors.White,
            });

            bool hasData = false;

            foreach (var combination in generator.GenerateCombinations())
            {
                if (!IsCombinationInGroup(fixedCombination, combination, varyingParamSelector))
                    continue;

                var profits = LoadProfitsFromCsv(combination, resultsDirectory, iterations); // Используем Results
                if (profits == null || profits.Length == 0 || profits.Distinct().Count() < 2)
                    continue;

                var histogramSeries = CreateHistogramSeries(profits, combination, paramName);
                if (histogramSeries == null)
                    continue;

                plotModel.Series.Add(histogramSeries);
                hasData = true;
            }

            if (!hasData)
            {
                Console.WriteLine($"Нет данных для группы: {groupTitle}");
                continue;
            }

            // Сохраняем график в AggregatedHistograms
            var plotFileName =
                $"aggregated_{paramName}_E{fixedCombination.Employees}_S{fixedCombination.Salary}_C{fixedCombination.AverageClientsMonth}_M{fixedCombination.MeanCostOrder}_D{fixedCombination.OrderStdDev}_A{fixedCombination.Alpha}.png";
            var plotFilePath = Path.Combine(Path.Combine(resultsDirectory, "AggregatedHistograms"), plotFileName);

            using (var stream = File.Create(plotFilePath))
            {
                var exporter = new PngExporter { Width = 1200, Height = 800 };
                exporter.Export(plotModel, stream);
            }

            Console.WriteLine($"Агрегированный график для {paramName} сохранён в '{plotFilePath}'");
        }
    }

    private static bool IsCombinationInGroup(ParameterCombination fixedCombination,
        ParameterCombination currentCombination, Func<ParameterCombination, object> varyingParamSelector)
    {
        return fixedCombination.Employees == currentCombination.Employees &&
               fixedCombination.Salary == currentCombination.Salary &&
               fixedCombination.AverageClientsMonth == currentCombination.AverageClientsMonth &&
               fixedCombination.MeanCostOrder == currentCombination.MeanCostOrder &&
               fixedCombination.OrderStdDev == currentCombination.OrderStdDev &&
               fixedCombination.Alpha == currentCombination.Alpha &&
               !Equals(varyingParamSelector(fixedCombination), varyingParamSelector(currentCombination));
    }

    private static ParameterCombination[] GetFixedCombinations(Parameters parameters,
        Func<ParameterCombination, object> varyingParamSelector)
    {
        return (
            from employees in parameters.Employees
            from salary in parameters.Salary
            from averageClientsMonth in parameters.AverageClientsMonth
            from meanCostOrder in parameters.MeanCostOrder
            from orderStdDev in parameters.OrderStdDev
            from alpha in parameters.Alpha
            select new ParameterCombination(
                employees,
                salary,
                averageClientsMonth,
                meanCostOrder,
                orderStdDev,
                alpha,
                Convert.ToInt32(varyingParamSelector(new ParameterCombination(employees, salary, averageClientsMonth,
                    meanCostOrder, orderStdDev, alpha, 0)))
            )
        ).Distinct().ToArray();
    }

    private static double[] LoadProfitsFromCsv(ParameterCombination combination, string resultsDirectory,
        int iterations)
    {
        var csvFileName =
            $"results_E{combination.Employees}_S{combination.Salary}_C{combination.AverageClientsMonth}_M{combination.MeanCostOrder}_D{combination.OrderStdDev}_A{combination.Alpha}_B{combination.Beta}.csv";
        var csvFilePath = Path.Combine(resultsDirectory, csvFileName); // Ищем в Results

        if (!File.Exists(csvFilePath))
        {
            Console.WriteLine($"Файл '{csvFilePath}' не найден.");
            return Array.Empty<double>();
        }

        try
        {
            return File.ReadLines(csvFilePath)
                .Skip(1) // Пропускаем заголовок
                .Select(line => line.Split(',')) // Разделяем строки по запятой
                .Where(parts => parts.Length > 1) // Проверяем, что есть второй столбец
                .Select(parts => double.Parse(parts[1])) // Парсим второй столбец как прибыль
                .ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка чтения данных из файла '{csvFilePath}': {ex.Message}");
            return Array.Empty<double>();
        }
    }
    

    private static HistogramSeries CreateHistogramSeries(double[] profits, ParameterCombination combination, string varyingParamName)
    {
        if (profits.Length == 0 || profits.Distinct().Count() < 2)
        {
            Console.WriteLine($"Недостаточно уникальных данных для гистограммы: {varyingParamName}={GetVaryingParamValue(combination, varyingParamName)}");
            return null;
        }

        var binCount = (int)Math.Ceiling(1 + 3.322 * Math.Log10(profits.Length));
        var bins = HistogramHelpers.CreateUniformBins(profits.Min(), profits.Max(), binCount);
        var binningOptions = new BinningOptions(BinningOutlierMode.RejectOutliers, BinningIntervalType.InclusiveLowerBound, BinningExtremeValueMode.IncludeExtremeValues);
        var items = HistogramHelpers.Collect(profits, bins, binningOptions);

        if (items.Count == 0)
        {
            Console.WriteLine($"Нет данных для гистограммы: {varyingParamName}={GetVaryingParamValue(combination, varyingParamName)}");
            return null;
        }

        var histogramSeries = new HistogramSeries
        {
            Title = $"{varyingParamName}={GetVaryingParamValue(combination, varyingParamName)}",
            StrokeThickness = 1,
            FillColor = OxyColor.FromArgb(180, (byte)(255 * _random.NextDouble()), (byte)(255 * _random.NextDouble()),
                (byte)(255 * _random.NextDouble())),
            StrokeColor = OxyColors.White
        };

        foreach (var item in items)
        {
            histogramSeries.Items.Add(item);
        }

        return histogramSeries;
    }

    private static object GetVaryingParamValue(ParameterCombination combination, string varyingParamName)
    {
        return varyingParamName switch
        {
            "Beta" => combination.Beta,
            "Alpha" => combination.Alpha,
            "OrderStdDev" => combination.OrderStdDev,
            "MeanCostOrder" => combination.MeanCostOrder,
            "AverageClientsMonth" => combination.AverageClientsMonth,
            "Employees" => combination.Employees,
            "Salary" => combination.Salary,
            _ => throw new ArgumentException("Unknown parameter name")
        };
    }
}