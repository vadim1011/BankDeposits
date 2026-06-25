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

public partial class ClientsPageViewModel : ViewModelBase
{
    private readonly IDbService _dbService;
    private readonly ILoadInterface _csvService;
    private readonly ILoadInterface _excelService;
    private readonly ILoadInterface _jsonService;
    private readonly ILoadInterface _xmlService;

    private List<Client> _allClients = new();

    [ObservableProperty]
    private ObservableCollection<Client> _clients = new();

    [ObservableProperty]
    private Client? _selectedClient;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ClientsPageViewModel(IDbService dbService)
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
        var list = await _dbService.GetClientsAsync();
        _allClients = list.ToList();
        Clients = new ObservableCollection<Client>(list);
    }

    [RelayCommand]
    private void Search()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Clients = new ObservableCollection<Client>(_allClients);
            return;
        }

        var filtered = _allClients.Where(c =>
            c.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
            (c.Passport?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();

        Clients = new ObservableCollection<Client>(filtered);
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var box = MessageBoxManager.GetMessageBoxStandard("Информация",
            "Для добавления клиента используйте импорт данных.", ButtonEnum.Ok);
        await box.ShowAsync();
    }
    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedClient == null) return;

        var box = MessageBoxManager.GetMessageBoxStandard("Подтверждение",
            $"Удалить клиента '{SelectedClient.FullName}'?", ButtonEnum.YesNo);
        var result = await box.ShowAsync();

        if (result == ButtonResult.Yes)
        {
            var id = SelectedClient.Id; // Сохраняем ID до удаления
            await _dbService.DeleteClientAsync(id);
            Clients.Remove(SelectedClient);
            _allClients.RemoveAll(c => c.Id == id);
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

            foreach (var c in clients)
            {
                var existing = _allClients.FirstOrDefault(x => x.Id == c.Id);
                if (existing != null)
                    await _dbService.UpdateClientAsync(c);
                else
                    await _dbService.AddClientAsync(c);
            }

            await LoadAsync();
            var box = MessageBoxManager.GetMessageBoxStandard("Успех",
                $"Импорт из {format} выполнен. Загружено {clients.Count} клиентов.", ButtonEnum.Ok);
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

            await saver.SaveAsync(filePath, Clients.ToList(), new List<Vklad>(), new List<Account>());

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