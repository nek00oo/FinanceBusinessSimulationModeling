using System.Collections;
using System.Reflection;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;
using SimulationModeling;
using Exception = System.Exception;

public static class AggregatedHistogramBuilder
{
    private static readonly Random _random = new(42);

    public static void BuildAndSaveAggregatedHistograms(
        Parameters parameters, 
        string resultsDirectory, 
        ParameterCombinationsGenerator generator)
    {
        var aggregatedResultsDir = Path.Combine(resultsDirectory, "AggregatedHistograms");
        Directory.CreateDirectory(aggregatedResultsDir);

        // Список параметров для анализа с их описаниями
        var paramsToCheck = new Dictionary<string, (string Title, Func<ParameterCombination, object> Selector)>()
        {
            { "Beta", ("Beta Variations", c => c.Beta) },
            { "Alpha", ("Alpha Variations", c => c.Alpha) },
            { "OrderStdDev", ("Order StdDev Variations", c => c.OrderStdDev) },
            { "MeanCostOrder", ("Mean Cost Order Variations", c => c.MeanCostOrder) },
            { "AverageClientsMonth", ("Average Clients Month Variations", c => c.AverageClientsMonth) },
            { "Employees", ("Employees Variations", c => c.Employees) },
            { "Salary", ("Salary Variations", c => c.Salary) }
        };

        foreach (var paramInfo in paramsToCheck)
        {
            var paramName = paramInfo.Key;
            var paramValues = (IList)typeof(Parameters).GetProperty(paramName)?.GetValue(parameters)!;
        
            // Пропускаем параметры с менее чем 2 значениями, чтобы строить агрегированные графики
            if (paramValues == null || paramValues.Count < 2) 
            {
                Console.WriteLine($"Пропуск параметра {paramName}: недостаточно значений");
                continue;
            }

            BuildGroupedHistogram(
                parameters: parameters,
                resultsDirectory: resultsDirectory,
                aggregatedResultsDir: aggregatedResultsDir,
                generator: generator,
                varyingParamSelector: paramInfo.Value.Selector,
                paramName: paramName,
                groupTitle: paramInfo.Value.Title
            );
        }
    }

    private static void BuildGroupedHistogram(Parameters parameters, string resultsDirectory,
        string aggregatedResultsDir, ParameterCombinationsGenerator generator,
        Func<ParameterCombination, object> varyingParamSelector, string paramName, string groupTitle)
    {
        var existingCombinations = generator.GenerateCombinations().ToList();
        var fixedCombinations = existingCombinations
            .GroupBy(c => GetKey(c, paramName))
            .Select(g => g.First())
            .ToArray();

        foreach (var fixedCombination in fixedCombinations)
        {
            var plotModel = new PlotModel
            {
                Title = $"{groupTitle} ({GetFixedParamsText(fixedCombination, paramName)})",
                Subtitle = $"Вариация параметра: {paramName}",
                TitleColor = OxyColors.White,
                SubtitleColor = OxyColors.White,
                Background = OxyColors.Black,
                IsLegendVisible = true,
            };
            
            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Прибыль",
                TextColor = OxyColors.White,
                TitleColor = OxyColors.White,
                TicklineColor = OxyColors.White,
                AxislineColor = OxyColors.White
            });

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Количество результатов",
                TextColor = OxyColors.White,
                TitleColor = OxyColors.White,
                TicklineColor = OxyColors.White,
                AxislineColor = OxyColors.White
            });

            var variationValues = new HashSet<object>();
            bool hasData = false;

            foreach (var combination in existingCombinations)
            {
                if (!IsCombinationInGroup(fixedCombination, combination, paramName))
                    continue;

                var profits = LoadProfitsFromCsv(combination, resultsDirectory);
                if (profits.Length == 0)
                    continue;

                var variationValue = GetVaryingParamValue(combination, paramName);
                variationValues.Add(variationValue);

                var histogramSeries = CreateHistogramSeries(profits, combination, paramName);
                
                if (histogramSeries == null) continue;

                histogramSeries.Title = $"{paramName} = {variationValue}";
                plotModel.Series.Add(histogramSeries);
                hasData = true;
            }

            if (variationValues.Count > 0)
            {
                plotModel.Subtitle += $"\nИспользуемые значения: [{string.Join(", ", variationValues)}]";
            }

            if (!hasData)
            {
                Console.WriteLine($"Нет данных для группы: {plotModel.Title}");
                continue;
            }

            var plotFileName = GeneratePlotFileName(fixedCombination, paramName);
            var plotFilePath = Path.Combine(aggregatedResultsDir, plotFileName);

            try
            {
                using (var stream = File.Create(plotFilePath))
                {
                    new PngExporter { Width = 1200, Height = 800 }
                        .Export(plotModel, stream);
                }

                Console.WriteLine($"График сохранён: {plotFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения графика: {ex.Message}");
            }
        }
    }

    private static string GetFixedParamsText(ParameterCombination combination, string excludedParam)
    {
        var props = new List<string>();

        var properties = typeof(ParameterCombination).GetProperties()
            .Where(p => p.Name != excludedParam && !IsDefaultValue(combination, p));

        foreach (var prop in properties)
        {
            var value = prop.GetValue(combination);
            props.Add($"{prop.Name}={FormatValue(value)}");
        }

        return string.Join(", ", props);
    }

    private static bool IsDefaultValue(ParameterCombination combo, PropertyInfo prop)
    {
        var defaultValue = prop.PropertyType.IsValueType
            ? Activator.CreateInstance(prop.PropertyType)
            : null;

        return Equals(prop.GetValue(combo), defaultValue);
    }

    private static string FormatValue(object value)
    {
        return value switch
        {
            double d => d.ToString("N0"),
            int i => i.ToString(),
            _ => value?.ToString() ?? "null"
        };
    }

    private static string GeneratePlotFileName(ParameterCombination combo, string paramName)
    {
        return $"aggregated_{paramName}_" +
               $"E{combo.Employees}_" +
               $"S{combo.Salary}_" +
               $"C{combo.AverageClientsMonth}_" +
               $"M{combo.MeanCostOrder}_" +
               $"D{combo.OrderStdDev}_" +
               $"A{combo.Alpha}_" +
               $"B{combo.Beta}.png";
    }

    private static object GetKey(ParameterCombination combination, string excludedParamName)
    {
        return excludedParamName switch
        {
            "Beta" => new
            {
                combination.Employees, combination.Salary, combination.AverageClientsMonth, combination.MeanCostOrder,
                combination.OrderStdDev, combination.Alpha
            },
            "Alpha" => new
            {
                combination.Employees, combination.Salary, combination.AverageClientsMonth, combination.MeanCostOrder,
                combination.OrderStdDev, combination.Beta
            },
            "OrderStdDev" => new
            {
                combination.Employees, combination.Salary, combination.AverageClientsMonth, combination.MeanCostOrder,
                combination.Alpha, combination.Beta
            },
            "MeanCostOrder" => new
            {
                combination.Employees, combination.Salary, combination.AverageClientsMonth, combination.OrderStdDev,
                combination.Alpha, combination.Beta
            },
            "AverageClientsMonth" => new
            {
                combination.Employees, combination.Salary, combination.MeanCostOrder, combination.OrderStdDev,
                combination.Alpha, combination.Beta
            },
            "Employees" => new
            {
                combination.Salary, combination.AverageClientsMonth, combination.MeanCostOrder, combination.OrderStdDev,
                combination.Alpha, combination.Beta
            },
            "Salary" => new
            {
                combination.Employees, combination.AverageClientsMonth, combination.MeanCostOrder,
                combination.OrderStdDev, combination.Alpha, combination.Beta
            },
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

    private static bool CompareDoubles(double a, double b, double tolerance = 1e-9)
    {
        return Math.Abs(a - b) <= tolerance;
    }

    private static double[] LoadProfitsFromCsv(ParameterCombination combination, string resultsDirectory)
    {
        var csvFileName =
            $"results_E{combination.Employees}_S{combination.Salary}_C{combination.AverageClientsMonth}_M{combination.MeanCostOrder}_D{combination.OrderStdDev}_A{combination.Alpha}_B{combination.Beta}.csv";
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


    private static HistogramSeries? CreateHistogramSeries(double[] profits, ParameterCombination combination,
        string varyingParamName)
    {
        if (profits.Length == 0 || profits.Distinct().Count() < 2)
        {
            Console.WriteLine(
                $"Недостаточно данных для построения гистограммы: {varyingParamName}={GetVaryingParamValue(combination, varyingParamName)}");
            return null;
        }

        var binCount = (int)Math.Ceiling(1 + 3.322 * Math.Log10(profits.Length));
        var bins = HistogramHelpers.CreateUniformBins(profits.Min(), profits.Max(), binCount);
        var binningOptions = new BinningOptions(BinningOutlierMode.RejectOutliers,
            BinningIntervalType.InclusiveLowerBound, BinningExtremeValueMode.IncludeExtremeValues);
        var items = HistogramHelpers.Collect(profits, bins, binningOptions);

        if (items.Count == 0)
        {
            Console.WriteLine(
                $"Нет данных для гистограммы: {varyingParamName}={GetVaryingParamValue(combination, varyingParamName)}");
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