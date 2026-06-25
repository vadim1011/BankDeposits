using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using BankDeposits.Models;
using BankDeposits.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace BankDeposits.ViewModels;

public partial class ReportPageViewModel : ViewModelBase
{
    private readonly IDbService _dbService;
    private readonly LoadPDFService _pdfService;

    [ObservableProperty]
    private List<ReportClient> _clientReport = new();

    [ObservableProperty]
    private List<ReportVklad> _vkladReport = new();

    [ObservableProperty]
    private List<ReportStatus> _statusReport = new();

    [ObservableProperty]
    private List<ReportExpiring> _expiringReport = new();

    [ObservableProperty]
    private List<ReportTopClient> _topClientsReport = new();

    [ObservableProperty]
    private int _selectedReportIndex;

    // Свойства видимости для отчетов
    public bool IsReport0Visible => SelectedReportIndex == 0;
    public bool IsReport1Visible => SelectedReportIndex == 1;
    public bool IsReport2Visible => SelectedReportIndex == 2;
    public bool IsReport3Visible => SelectedReportIndex == 3;
    public bool IsReport4Visible => SelectedReportIndex == 4;

    partial void OnSelectedReportIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsReport0Visible));
        OnPropertyChanged(nameof(IsReport1Visible));
        OnPropertyChanged(nameof(IsReport2Visible));
        OnPropertyChanged(nameof(IsReport3Visible));
        OnPropertyChanged(nameof(IsReport4Visible));
    }

    public ReportPageViewModel(IDbService dbService)
    {
        _dbService = dbService;
        _pdfService = new LoadPDFService();
        _ = LoadAllAsync();
    }

    private async Task LoadAllAsync()
    {
        await LoadClientReportAsync();
        await LoadVkladReportAsync();
        await LoadStatusReportAsync();
        await LoadExpiringReportAsync();
        await LoadTopClientsAsync();
    }

    private async Task LoadClientReportAsync()
    {
        var accounts = await _dbService.GetAccountsAsync();
        var clients = await _dbService.GetClientsAsync();

        ClientReport = clients.Select(c =>
        {
            var clientAccounts = accounts.Where(a => a.ClientId == c.Id).ToList();
            var totalAmount = clientAccounts.Sum(a => a.Amount);
            var totalIncome = clientAccounts.Sum(a => a.Income);
            var avgRate = clientAccounts.Any() ? clientAccounts.Average(a => a.Vklad?.Rate ?? 0) : 0;

            return new ReportClient
            {
                FullName = c.FullName,
                Passport = c.Passport,
                Phone = c.Phone ?? "",
                AccountCount = clientAccounts.Count,
                TotalAmount = totalAmount,
                TotalIncome = totalIncome,
                AvgRate = avgRate
            };
        }).ToList();
    }

    private async Task LoadVkladReportAsync()
    {
        var accounts = await _dbService.GetAccountsAsync();
        var vklads = await _dbService.GetVkladsAsync();

        VkladReport = vklads.Select(v =>
        {
            var vkladAccounts = accounts.Where(a => a.VkladId == v.Id).ToList();
            var totalAmount = vkladAccounts.Sum(a => a.Amount);
            var avgIncome = vkladAccounts.Any() ? vkladAccounts.Average(a => a.Income) : 0;

            return new ReportVklad
            {
                Name = v.Name,
                TermMonths = v.TermMonths,
                Rate = v.Rate,
                ClientCount = vkladAccounts.Select(a => a.ClientId).Distinct().Count(),
                TotalAmount = totalAmount,
                AvgIncome = avgIncome
            };
        }).ToList();
    }

    private async Task LoadStatusReportAsync()
    {
        var accounts = await _dbService.GetAccountsAsync();

        var activeAccounts = accounts.Where(a => a.Status == "Активен").ToList();
        var closedAccounts = accounts.Where(a => a.Status == "Закрыт").ToList();

        StatusReport = new List<ReportStatus>
        {
            new()
            {
                Status = "Активен",
                Count = activeAccounts.Count,
                TotalAmount = activeAccounts.Sum(a => a.Amount),
                TotalIncome = activeAccounts.Sum(a => a.Income)
            },
            new()
            {
                Status = "Закрыт",
                Count = closedAccounts.Count,
                TotalAmount = closedAccounts.Sum(a => a.Amount),
                TotalIncome = closedAccounts.Sum(a => a.Income)
            }
        };
    }

    private async Task LoadExpiringReportAsync()
    {
        var accounts = await _dbService.GetAccountsAsync();
        var now = DateTime.Now;
        var threshold = now.AddDays(30);

        var expiring = accounts
            .Where(a => a.Status == "Активен" && a.EndDate.Date >= now.Date && a.EndDate.Date <= threshold.Date)
            .Select(a => new ReportExpiring
            {
                ClientName = a.Client?.FullName ?? "",
                Phone = a.Client?.Phone ?? "",
                Amount = a.Amount,
                EndDate = a.EndDate,
                PayoutAmount = a.PayoutAmount
            })
            .ToList();

        ExpiringReport = expiring;
    }

    private async Task LoadTopClientsAsync()
    {
        var accounts = await _dbService.GetAccountsAsync();
        var clients = await _dbService.GetClientsAsync();

        TopClientsReport = clients
            .Select(c =>
            {
                var clientAccounts = accounts.Where(a => a.ClientId == c.Id).ToList();
                var totalIncome = clientAccounts.Sum(a => a.Income);
                var totalAmount = clientAccounts.Sum(a => a.Amount);
                var effectiveRate = totalAmount > 0 ? (double)(totalIncome / totalAmount * 100) : 0;

                return new ReportTopClient
                {
                    FullName = c.FullName,
                    TotalAmount = totalAmount,
                    TotalIncome = totalIncome,
                    EffectiveRate = effectiveRate
                };
            })
            .OrderByDescending(x => x.TotalIncome)
            .Take(10)
            .ToList();
    }

    [RelayCommand]
    private async Task ExportPDFAsync()
    {
        try
        {
            var filePath = await PickFileAsync();
            if (filePath == null) return;

            string[] headers;
            List<string[]> rows;
            string title;

            switch (SelectedReportIndex)
            {
                case 0:
                    title = "Сводка по клиентам";
                    headers = new[] { "ФИО", "Паспорт", "Телефон", "Кол-во счетов", "Общая сумма", "Доход", "Средняя ставка" };
                    rows = ClientReport.Select(r => new[]
                    {
                        r.FullName, r.Passport, r.Phone, r.AccountCount.ToString(),
                        r.TotalAmount.ToString("N2"), r.TotalIncome.ToString("N2"), $"{r.AvgRate:F2}%"
                    }).ToList();
                    break;
                case 1:
                    title = "Сводка по вкладам";
                    headers = new[] { "Вклад", "Срок", "Ставка", "Клиентов", "Общая сумма", "Средний доход" };
                    rows = VkladReport.Select(r => new[]
                    {
                        r.Name, $"{r.TermMonths} мес.", $"{r.Rate}%", r.ClientCount.ToString(),
                        r.TotalAmount.ToString("N2"), r.AvgIncome.ToString("N2")
                    }).ToList();
                    break;
                case 2:
                    title = "Активные и закрытые счета";
                    headers = new[] { "Статус", "Кол-во", "Сумма вложений", "Сумма дохода" };
                    rows = StatusReport.Select(r => new[]
                    {
                        r.Status, r.Count.ToString(), r.TotalAmount.ToString("N2"), r.TotalIncome.ToString("N2")
                    }).ToList();
                    break;
                case 3:
                    title = "Счета с истекающим сроком (30 дней)";
                    headers = new[] { "Клиент", "Телефон", "Сумма", "Дата окончания", "К выплате" };
                    rows = ExpiringReport.Select(r => new[]
                    {
                        r.ClientName, r.Phone, r.Amount.ToString("N2"), r.EndDate.ToString("dd.MM.yyyy"), r.PayoutAmount.ToString("N2")
                    }).ToList();
                    break;
                default:
                    title = "Рейтинг клиентов по доходности";
                    headers = new[] { "Клиент", "Сумма вложений", "Доход", "Эфф. ставка" };
                    rows = TopClientsReport.Select(r => new[]
                    {
                        r.FullName, r.TotalAmount.ToString("N2"), r.TotalIncome.ToString("N2"), $"{r.EffectiveRate:F2}%"
                    }).ToList();
                    break;
            }

            await _pdfService.SaveReportAsync(filePath, title, rows, headers);

            var box = MessageBoxManager.GetMessageBoxStandard("Успех", "PDF отчет сохранен.", ButtonEnum.Ok);
            await box.ShowAsync();
        }
        catch (Exception ex)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Ошибка", $"Ошибка экспорта: {ex.Message}", ButtonEnum.Ok);
            await box.ShowAsync();
        }
    }

    private static async Task<string?> PickFileAsync()
    {
        var topLevel = TopLevel.GetTopLevel(App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null);
        if (topLevel == null) return null;

        var saveOptions = new FilePickerSaveOptions
        {
            Title = "Сохранить PDF отчет"
        };
        var result = await topLevel.StorageProvider.SaveFilePickerAsync(saveOptions);
        return result?.Path.LocalPath;
    }
}

public class ReportClient
{
    public string FullName { get; set; } = string.Empty;
    public string Passport { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int AccountCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalIncome { get; set; }
    public double AvgRate { get; set; }
}

public class ReportVklad
{
    public string Name { get; set; } = string.Empty;
    public int TermMonths { get; set; }
    public double Rate { get; set; }
    public int ClientCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AvgIncome { get; set; }
}

public class ReportStatus
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalIncome { get; set; }
}

public class ReportExpiring
{
    public string ClientName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime EndDate { get; set; }
    public decimal PayoutAmount { get; set; }
}

public class ReportTopClient
{
    public string FullName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal TotalIncome { get; set; }
    public double EffectiveRate { get; set; }
}