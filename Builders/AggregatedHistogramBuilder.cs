using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;
using SimulationModeling;
using Exception = System.Exception;

public static class AggregatedHistogramBuilder
{
    private static readonly Random _random = new(42);
    
    public static void BuildAndSaveAggregatedHistograms(Parameters parameters, string resultsDirectory, ParameterCombinationsGenerator generator)
    {
        var iterations = parameters.Iterations;
        
        var aggregatedResultsDir = Path.Combine(resultsDirectory, "AggregatedHistograms");
        Directory.CreateDirectory(aggregatedResultsDir);
        
        BuildGroupedHistogram(parameters, iterations, resultsDirectory, aggregatedResultsDir, generator, param => param.Beta, "Beta", "Beta Variations");
        BuildGroupedHistogram(parameters, iterations, resultsDirectory, aggregatedResultsDir, generator, param => param.Alpha, "Alpha", "Alpha Variations");
        BuildGroupedHistogram(parameters, iterations, resultsDirectory, aggregatedResultsDir, generator, param => param.OrderStdDev, "OrderStdDev", "Order StdDev Variations");
        BuildGroupedHistogram(parameters, iterations, resultsDirectory, aggregatedResultsDir, generator, param => param.MeanCostOrder, "MeanCostOrder", "Mean Cost Order Variations");
        BuildGroupedHistogram(parameters, iterations, resultsDirectory, aggregatedResultsDir, generator, param => param.AverageClientsMonth, "AverageClientsMonth", "Average Clients Month Variations");
        BuildGroupedHistogram(parameters, iterations, resultsDirectory, aggregatedResultsDir, generator, param => param.Employees, "Employees", "Employees Variations");
        BuildGroupedHistogram(parameters, iterations, resultsDirectory, aggregatedResultsDir, generator, param => param.Salary, "Salary", "Salary Variations");
    }

    // private static void BuildGroupedHistogram(Parameters parameters, int iterations, string resultsDirectory,
    //     string aggregatedResultsDir, ParameterCombinationsGenerator generator,
    //     Func<ParameterCombination, object> varyingParamSelector, string paramName, string groupTitle)
    // {
    //     foreach (var fixedCombination in GetFixedCombinations(parameters, varyingParamSelector))
    //     {
    //         var plotModel = new PlotModel
    //         {
    //             Title =
    //                 $"{groupTitle} (E={fixedCombination.Employees}, S={fixedCombination.Salary}, C={fixedCombination.AverageClientsMonth}, M={fixedCombination.MeanCostOrder}, D={fixedCombination.OrderStdDev}, A={fixedCombination.Alpha})",
    //             TitleColor = OxyColors.White,
    //             Background = OxyColors.Black
    //         };
    //
    //         plotModel.Axes.Add(new LinearAxis
    //         {
    //             Position = AxisPosition.Bottom,
    //             Title = "Прибыль",
    //             TextColor = OxyColors.White,
    //             TitleColor = OxyColors.White,
    //             TicklineColor = OxyColors.White,
    //             AxislineColor = OxyColors.White,
    //         });
    //
    //         plotModel.Axes.Add(new LinearAxis
    //         {
    //             Position = AxisPosition.Left,
    //             Title = "Количество результатов",
    //             TextColor = OxyColors.White,
    //             TitleColor = OxyColors.White,
    //             TicklineColor = OxyColors.White,
    //             AxislineColor = OxyColors.White,
    //         });
    //
    //         bool hasData = false;
    //
    //         foreach (var combination in generator.GenerateCombinations())
    //         {
    //             if (!IsCombinationInGroup(fixedCombination, combination, varyingParamSelector))
    //                 continue;
    //
    //             var profits = LoadProfitsFromCsv(combination, resultsDirectory);
    //             if (profits.Length == 0 || profits.Distinct().Count() < 2)
    //                 continue;
    //
    //             var histogramSeries = CreateHistogramSeries(profits, combination, paramName);
    //
    //             plotModel.Series.Add(histogramSeries);
    //             hasData = true;
    //         }
    //
    //         if (!hasData)
    //         {
    //             Console.WriteLine($"Нет данных для группы: {groupTitle}");
    //             continue;
    //         }
    //
    //         var plotFileName =
    //             $"aggregated_{paramName}_E{fixedCombination.Employees}_S{fixedCombination.Salary}_C{fixedCombination.AverageClientsMonth}_M{fixedCombination.MeanCostOrder}_D{fixedCombination.OrderStdDev}_A{fixedCombination.Alpha}.png";
    //         var plotFilePath = Path.Combine(aggregatedResultsDir, plotFileName);
    //
    //         using (var stream = File.Create(plotFilePath))
    //         {
    //             var exporter = new PngExporter { Width = 1200, Height = 800 };
    //             exporter.Export(plotModel, stream);
    //         }
    //
    //         Console.WriteLine($"Агрегированный график для {paramName} сохранён в '{plotFilePath}'");
    //     }
    // }
    
    private static void BuildGroupedHistogram(Parameters parameters, int iterations, string resultsDirectory,
        string aggregatedResultsDir, ParameterCombinationsGenerator generator,
        Func<ParameterCombination, object> varyingParamSelector, string paramName, string groupTitle)
    {
        // Получаем реальные комбинации и группируем их по ключу (без varyingParam)
        var existingCombinations = generator.GenerateCombinations().ToList();
        var fixedCombinations = existingCombinations
            .GroupBy(c => GetKey(c, paramName))
            .Select(g => g.First())
            .ToArray();

        foreach (var fixedCombination in fixedCombinations)
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

            foreach (var combination in existingCombinations)
            {
                if (!IsCombinationInGroup(fixedCombination, combination, paramName))
                    continue;

                var profits = LoadProfitsFromCsv(combination, resultsDirectory);
                if (profits.Length == 0)
                    continue;

                var histogramSeries = CreateHistogramSeries(profits, combination, paramName);
                if (histogramSeries != null)
                {
                    plotModel.Series.Add(histogramSeries);
                    hasData = true;
                }
            }

            if (hasData)
            {
                var plotFileName =
                    $"aggregated_{paramName}_E{fixedCombination.Employees}_S{fixedCombination.Salary}_C{fixedCombination.AverageClientsMonth}_M{fixedCombination.MeanCostOrder}_D{fixedCombination.OrderStdDev}_A{fixedCombination.Alpha}.png";
                var plotFilePath = Path.Combine(aggregatedResultsDir, plotFileName);

                using (var stream = File.Create(plotFilePath))
                {
                    var exporter = new PngExporter { Width = 1200, Height = 800 };
                    exporter.Export(plotModel, stream);
                }
            }
        }
    }

    private static object GetKey(ParameterCombination combination, string excludedParamName)
    {
        return excludedParamName switch
        {
            "Beta" => new { combination.Employees, combination.Salary, combination.AverageClientsMonth, combination.MeanCostOrder, combination.OrderStdDev, combination.Alpha },
            "Alpha" => new { combination.Employees, combination.Salary, combination.AverageClientsMonth, combination.MeanCostOrder, combination.OrderStdDev, combination.Beta },
            "OrderStdDev" => new { combination.Employees, combination.Salary, combination.AverageClientsMonth, combination.MeanCostOrder, combination.Alpha, combination.Beta },
            "MeanCostOrder" => new { combination.Employees, combination.Salary, combination.AverageClientsMonth, combination.OrderStdDev, combination.Alpha, combination.Beta },
            "AverageClientsMonth" => new { combination.Employees, combination.Salary, combination.MeanCostOrder, combination.OrderStdDev, combination.Alpha, combination.Beta },
            "Employees" => new { combination.Salary, combination.AverageClientsMonth, combination.MeanCostOrder, combination.OrderStdDev, combination.Alpha, combination.Beta },
            "Salary" => new { combination.Employees, combination.AverageClientsMonth, combination.MeanCostOrder, combination.OrderStdDev, combination.Alpha, combination.Beta },
            _ => throw new ArgumentException("Unknown parameter name")
        };
    }

    private static bool IsCombinationInGroup(ParameterCombination fixedCombination, 
        ParameterCombination currentCombination, string excludedParamName)
    {
        var properties = typeof(ParameterCombination).GetProperties();
        foreach (var prop in properties)
        {
            if (prop.Name == excludedParamName)
                continue;

            var fixedValue = prop.GetValue(fixedCombination);
            var currentValue = prop.GetValue(currentCombination);

            if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(double))
            {
                if (!CompareDoubles(Convert.ToDouble(fixedValue), Convert.ToDouble(currentValue)))
                    return false;
            }
            else
            {
                if (!fixedValue.Equals(currentValue))
                    return false;
            }
        }
        return true;
    }

    private static bool IsCombinationInGroup(ParameterCombination fixedCombination, 
        ParameterCombination currentCombination, 
        Func<ParameterCombination, object> varyingParamSelector)
    {
        
        if (fixedCombination.Employees != currentCombination.Employees ||
            fixedCombination.AverageClientsMonth != currentCombination.AverageClientsMonth ||
            fixedCombination.Alpha != currentCombination.Alpha)
        {
            return false;
        }
        
        if (!CompareDoubles(fixedCombination.Salary, currentCombination.Salary) ||
            !CompareDoubles(fixedCombination.MeanCostOrder, currentCombination.MeanCostOrder) ||
            !CompareDoubles(fixedCombination.OrderStdDev, currentCombination.OrderStdDev))
        {
            return false;
        }

        return !Equals(varyingParamSelector(fixedCombination), varyingParamSelector(currentCombination));
    }

    private static bool CompareDoubles(double a, double b, double tolerance = 1e-9)
    {
        return Math.Abs(a - b) <= tolerance;
    }

    private static ParameterCombination[] GetFixedCombinations(Parameters parameters,
        Func<ParameterCombination, object> varyingParamSelector)
    {
        return (
            from employees in parameters.Employees.DefaultIfEmpty(parameters.Employees.First())
            from salary in parameters.Salary.DefaultIfEmpty(parameters.Salary.First())
            from averageClientsMonth in parameters.AverageClientsMonth.DefaultIfEmpty(parameters.AverageClientsMonth
                .First())
            from meanCostOrder in parameters.MeanCostOrder.DefaultIfEmpty(parameters.MeanCostOrder.First())
            from orderStdDev in parameters.OrderStdDev.DefaultIfEmpty(parameters.OrderStdDev.First())
            from alpha in parameters.Alpha.DefaultIfEmpty(parameters.Alpha.First())
            let fixedCombination = new ParameterCombination(employees, salary, averageClientsMonth, meanCostOrder,
                orderStdDev, alpha, parameters.Beta.DefaultIfEmpty(parameters.Beta.First()).First())
            where !Equals(varyingParamSelector(fixedCombination),
                varyingParamSelector(new ParameterCombination(employees, salary, averageClientsMonth, meanCostOrder,
                    orderStdDev, alpha, 0)))
            select fixedCombination
        ).Distinct().ToArray();
    }

    private static double[] LoadProfitsFromCsv(ParameterCombination combination, string resultsDirectory)
    {
        var csvFileName = $"results_E{combination.Employees}_S{combination.Salary}_C{combination.AverageClientsMonth}_M{combination.MeanCostOrder}_D{combination.OrderStdDev}_A{combination.Alpha}_B{combination.Beta}.csv";
        var csvFilePath = Path.Combine(resultsDirectory, csvFileName);

        if (!File.Exists(csvFilePath))
        {
            Console.WriteLine($"Файл '{csvFilePath}' не найден.");
            return Array.Empty<double>();
        }

        try
        {
            return File.ReadLines(csvFilePath)
                .Skip(1)
                .Select(line => line.Split(','))
                .Where(parts => parts.Length > 1)
                .Select(parts => double.Parse(parts[1]))
                .ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка чтения данных из файла '{csvFilePath}': {ex.Message}");
            return [];
        }
    }
    

    private static HistogramSeries? CreateHistogramSeries(double[] profits, ParameterCombination combination, string varyingParamName)
    {
        if (profits.Length == 0 || profits.Distinct().Count() < 2)
        {
            Console.WriteLine($"Недостаточно данных для построения гистограммы: {varyingParamName}={GetVaryingParamValue(combination, varyingParamName)}");
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