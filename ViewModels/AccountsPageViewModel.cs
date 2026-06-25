using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

public partial class AccountsPageViewModel : ViewModelBase
{
    private readonly IDbService _dbService;
    private readonly ILoadInterface _csvService;
    private readonly ILoadInterface _excelService;
    private readonly ILoadInterface _jsonService;
    private readonly ILoadInterface _xmlService;

    private List<Account> _allAccounts = new();

    [ObservableProperty]
    private ObservableCollection<Account> _accounts = new();

    [ObservableProperty]
    private Account? _selectedAccount;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public AccountsPageViewModel(IDbService dbService)
    {
        _dbService = dbService;
        _csvService = new LoadCSVService();
        _excelService = new LoadExcelService();
        _jsonService = new LoadJSONService();
        _xmlService = new LoadXMLService();

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var list = await _dbService.GetAccountsAsync();
        _allAccounts = list.ToList();
        Accounts = new ObservableCollection<Account>(list);
    }

    [RelayCommand]
    private void Search()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Accounts = new ObservableCollection<Account>(_allAccounts);
            return;
        }

        var filtered = _allAccounts.Where(a =>
            a.Client?.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false)
            .ToList();

        Accounts = new ObservableCollection<Account>(filtered);
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var box = MessageBoxManager.GetMessageBoxStandard("Информация",
            "Для добавления счета используйте импорт данных.", ButtonEnum.Ok);
        await box.ShowAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedAccount == null) return;

        var box = MessageBoxManager.GetMessageBoxStandard("Подтверждение",
            $"Удалить счет №{SelectedAccount.Id}?", ButtonEnum.YesNo);
        var result = await box.ShowAsync();

        if (result == ButtonResult.Yes)
        {
            var id = SelectedAccount.Id; // Сохраняем ID до удаления
            await _dbService.DeleteAccountAsync(id);
            Accounts.Remove(SelectedAccount);
            _allAccounts.RemoveAll(a => a.Id == id);
        }
    }
    [RelayCommand]
    private async Task ImportCSVAsync() => await ImportAsync(_csvService, "CSV");
    [RelayCommand]
    private async Task ImportExcelAsync() => await ImportAsync(_excelService, "Excel");
    [RelayCommand]
    private async Task ImportJSONAsync() => await ImportAsync(_jsonService, "JSON");
    [RelayCommand]
    private async Task ImportXMLAsync() => await ImportAsync(_xmlService, "XML");

    private async Task ImportAsync(ILoadInterface loader, string format)
    {
        try
        {
            var filePath = await PickFileAsync(loader.FileFilter, true);
            if (filePath == null) return;

            var (clients, vklads, accounts) = await loader.LoadAsync(filePath);

            foreach (var a in accounts)
            {
                var existing = _allAccounts.FirstOrDefault(x => x.Id == a.Id);
                if (existing != null)
                    await _dbService.UpdateAccountAsync(a);
                else
                    await _dbService.AddAccountAsync(a);
            }

            await LoadAsync();
            var box = MessageBoxManager.GetMessageBoxStandard("Успех",
                $"Импорт из {format} выполнен. Загружено {accounts.Count} счетов.", ButtonEnum.Ok);
            await box.ShowAsync();
        }
        catch (Exception ex)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Ошибка",
                $"Ошибка импорта: {ex.Message}", ButtonEnum.Ok);
            await box.ShowAsync();
        }
    }

    [RelayCommand]
    private async Task ExportCSVAsync() => await ExportAsync(_csvService, "CSV");
    [RelayCommand]
    private async Task ExportExcelAsync() => await ExportAsync(_excelService, "Excel");
    [RelayCommand]
    private async Task ExportJSONAsync() => await ExportAsync(_jsonService, "JSON");
    [RelayCommand]
    private async Task ExportXMLAsync() => await ExportAsync(_xmlService, "XML");

    private async Task ExportAsync(ILoadInterface saver, string format)
    {
        try
        {
            var filePath = await PickFileAsync(saver.FileFilter, false);
            if (filePath == null) return;

            await saver.SaveAsync(filePath, new List<Client>(), new List<Vklad>(), Accounts.ToList());

            var box = MessageBoxManager.GetMessageBoxStandard("Успех",
                $"Экспорт в {format} выполнен успешно.", ButtonEnum.Ok);
            await box.ShowAsync();
        }
        catch (Exception ex)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Ошибка",
                $"Ошибка экспорта: {ex.Message}", ButtonEnum.Ok);
            await box.ShowAsync();
        }
    }

    private static async Task<string?> PickFileAsync(string filter, bool open)
    {
        var topLevel = TopLevel.GetTopLevel(App.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow : null);
        if (topLevel == null) return null;

        if (open)
        {
            var options = new FilePickerOpenOptions
            {
                Title = "Выберите файл",
                AllowMultiple = false
            };
            var result = await topLevel.StorageProvider.OpenFilePickerAsync(options);
            return result.Count > 0 ? result[0].Path.LocalPath : null;
        }
        else
        {
            var saveOptions = new FilePickerSaveOptions { Title = "Сохранить файл" };
            var result = await topLevel.StorageProvider.SaveFilePickerAsync(saveOptions);
            return result?.Path.LocalPath;
        }
    }
}