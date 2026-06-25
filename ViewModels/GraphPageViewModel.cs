using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankDeposits.Models;
using BankDeposits.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace BankDeposits.ViewModels;

public partial class GraphPageViewModel : ViewModelBase
{
    private readonly IDbService _dbService;

    [ObservableProperty]
    private ISeries[] _barSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _pieSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _lineSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _histogramSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _barXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _lineXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _histogramXAxes = Array.Empty<Axis>();

    public GraphPageViewModel(IDbService dbService)
    {
        _dbService = dbService;
        BarXAxes = new Axis[] { new Axis() };
        LineXAxes = new Axis[] { new Axis() };
        HistogramXAxes = new Axis[] { new Axis() };

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var accounts = await _dbService.GetAccountsAsync();
        var vklads = await _dbService.GetVkladsAsync();
        var clients = await _dbService.GetClientsAsync();

        BuildBarChart(accounts, vklads);
        BuildPieChart(accounts);
        BuildLineChart(accounts);
        BuildHistogram(vklads);
    }

    private void BuildBarChart(List<Account> accounts, List<Vklad> vklads)
    {
        var grouped = accounts.GroupBy(a => a.Vklad?.Name ?? "Неизвестно")
            .Select(g => new { Name = g.Key, Sum = g.Sum(a => a.Amount) })
            .ToList();

        BarSeries = new ISeries[]
        {
            new ColumnSeries<decimal>
            {
                Name = "Сумма вложений",
                Values = grouped.Select(g => g.Sum).ToArray(),
                Fill = new SolidColorPaint(SKColors.SteelBlue),
                DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top,
                DataLabelsFormatter = p => $"{p.Coordinate.PrimaryValue:N0}"
            }

    };

        BarXAxes = new Axis[]
        {
            new Axis
            {
                Labels = grouped.Select(g => g.Name).ToArray(),
                LabelsRotation = 45
            }
        };
    }

    private void BuildPieChart(List<Account> accounts)
    {
        var ranges = new[]
        {
            new { Min = 0m, Max = 100000m, Label = "До 100 тыс." },
            new { Min = 100000m, Max = 500000m, Label = "100-500 тыс." },
            new { Min = 500000m, Max = 1000000m, Label = "500 тыс.-1 млн" },
            new { Min = 1000000m, Max = decimal.MaxValue, Label = "Более 1 млн" }
        };

        var counts = ranges.Select(r => accounts.Count(a => a.Amount >= r.Min && a.Amount < r.Max)).ToArray();
        var colors = new[] { SKColors.LightGreen, SKColors.LightBlue, SKColors.LightCoral, SKColors.LightGoldenrodYellow };

        PieSeries = ranges.Select((r, i) => new PieSeries<int>
        {
            Name = r.Label,
            Values = new[] { counts[i] },
            Fill = new SolidColorPaint(colors[i]),
            DataLabelsPaint = new SolidColorPaint(SKColors.Black),
            DataLabelsFormatter = p => $"{r.Label}: {p.Coordinate.PrimaryValue}"
        }).ToArray<ISeries>();
    }

    private void BuildLineChart(List<Account> accounts)
    {
        var monthly = accounts
            .GroupBy(a => new { a.OpenDate.Year, a.OpenDate.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new
            {
                Label = $"{g.Key.Month:D2}.{g.Key.Year}",
                Count = g.Count(),
                Sum = g.Sum(a => a.Amount)
            })
            .ToList();

        LineSeries = new ISeries[]
        {
            new LineSeries<int>
            {
                Name = "Количество счетов",
                Values = monthly.Select(m => m.Count).ToArray(),
                Fill = new SolidColorPaint(SKColors.CornflowerBlue.WithAlpha(50)),
                Stroke = new SolidColorPaint(SKColors.CornflowerBlue, 2)
            },
            new LineSeries<decimal>
            {
                Name = "Сумма (руб.)",
                Values = monthly.Select(m => m.Sum / 1000).ToArray(),
                Fill = new SolidColorPaint(SKColors.LightGreen.WithAlpha(50)),
                Stroke = new SolidColorPaint(SKColors.Green, 2)
            }
        };

        LineXAxes = new Axis[]
        {
            new Axis
            {
                Labels = monthly.Select(m => m.Label).ToArray()
            }
        };
    }

    private void BuildHistogram(List<Vklad> vklads)
    {
        var termGroups = new[] { 3, 6, 12, 18, 24 };
        var counts = termGroups.Select(t => vklads.Count(v => v.TermMonths == t)).ToArray();
        var labels = termGroups.Select(t => $"{t} мес.").ToArray();

        HistogramSeries = new ISeries[]
        {
            new ColumnSeries<int>
            {
                Name = "Количество вкладов",
                Values = counts,
                Fill = new SolidColorPaint(SKColors.MediumPurple),
                DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top,
                DataLabelsFormatter = p => $"{p.Coordinate.PrimaryValue}"
            }
        };

        HistogramXAxes = new Axis[]
        {
            new Axis
            {
                Labels = labels
            }
        };
    }
}
