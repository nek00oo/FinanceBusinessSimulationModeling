namespace SimulationModeling.Builders;

using System;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;

public static class PlotBuilder
{
    public static void BuildAndSaveHistogram(double[] profits, string filePath, ParameterCombination combination)
    {
        var plotModel = new PlotModel
        {
            Title =
                $"Распределение прибыли бизнеса (E={combination.Employees}, S={combination.Salary}, C={combination.AverageClientsMonth}, M={combination.MeanCostOrder}, D={combination.OrderStdDev}, A={combination.Alpha}, B={combination.Beta})",
            TitleColor = OxyColors.White,
            PlotAreaBorderColor = OxyColors.White,
            Background = OxyColors.Black
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

        using (var stream = File.Create(filePath))
        {
            var exporter = new PngExporter { Width = 1200, Height = 800 };
            exporter.Export(plotModel, stream);
        }

        Console.WriteLine($"График сохранён в '{filePath}'");
    }
}