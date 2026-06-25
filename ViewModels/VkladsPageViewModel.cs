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

public partial class VkladsPageViewModel : ViewModelBase
{
    private readonly IDbService _dbService;
    private readonly NavigationService _navigationService;
    private readonly ILoadInterface _csvService;
    private readonly ILoadInterface _excelService;
    private readonly ILoadInterface _jsonService;
    private readonly ILoadInterface _xmlService;

    private List<Vklad> _allVklads = new();

    [ObservableProperty]
    private ObservableCollection<Vklad> _vklads = new();

    [ObservableProperty]
    private Vklad? _selectedVklad;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public VkladsPageViewModel(IDbService dbService, NavigationService navigationService)
    {
        _dbService = dbService;
        _navigationService = navigationService;
        _csvService = new LoadCSVService();
        _excelService = new LoadExcelService();
        _jsonService = new LoadJSONService();
        _xmlService = new LoadXMLService();

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var list = await _dbService.GetVkladsAsync();
        _allVklads = list.ToList();
        Vklads = new ObservableCollection<Vklad>(list);
    }

    [RelayCommand]
    private void Search()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Vklads = new ObservableCollection<Vklad>(_allVklads);
            return;
        }

        var filtered = _allVklads.Where(v =>
            v.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();
        Vklads = new ObservableCollection<Vklad>(filtered);
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var box = MessageBoxManager.GetMessageBoxStandard("Добавление", "Функция добавления вклада", ButtonEnum.Ok);
        await box.ShowAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedVklad == null) return;

        var box = MessageBoxManager.GetMessageBoxStandard("Подтверждение",
            $"Удалить вклад '{SelectedVklad.Name}'?", ButtonEnum.YesNo);
        var result = await box.ShowAsync();

        if (result == ButtonResult.Yes)
        {
            var id = SelectedVklad.Id;
            await _dbService.DeleteVkladAsync(id);
            Vklads.Remove(SelectedVklad);
            _allVklads.RemoveAll(v => v.Id == id);
        }
    }

    [RelayCommand]
    private async Task ImportCSVAsync()
    {
        await ImportAsync(_csvService, "CSV");
    }

    [RelayCommand]
    private async Task ImportExcelAsync()
    {
        await ImportAsync(_excelService, "Excel");
    }

    [RelayCommand]
    private async Task ImportJSONAsync()
    {
        await ImportAsync(_jsonService, "JSON");
    }

    [RelayCommand]
    private async Task ImportXMLAsync()
    {
        await ImportAsync(_xmlService, "XML");
    }

    private async Task ImportAsync(ILoadInterface loader, string format)
    {
        try
        {
            var filePath = await PickFileAsync(loader.FileFilter, true);
            if (filePath == null) return;

            var (clients, vklads, accounts) = await loader.LoadAsync(filePath);

            foreach (var v in vklads)
            {
                var existing = _allVklads.FirstOrDefault(x => x.Id == v.Id);
                if (existing != null)
                    await _dbService.UpdateVkladAsync(v);
                else
                    await _dbService.AddVkladAsync(v);
            }

            await LoadAsync();
            var box = MessageBoxManager.GetMessageBoxStandard("Успех", $"Импорт из {format} выполнен успешно. Загружено {vklads.Count} вкладов.", ButtonEnum.Ok);
            await box.ShowAsync();
        }
        catch (Exception ex)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Ошибка", $"Ошибка импорта: {ex.Message}", ButtonEnum.Ok);
            await box.ShowAsync();
        }
    }

    [RelayCommand]
    private async Task ExportCSVAsync()
    {
        await ExportAsync(_csvService, "CSV");
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        await ExportAsync(_excelService, "Excel");
    }

    [RelayCommand]
    private async Task ExportJSONAsync()
    {
        await ExportAsync(_jsonService, "JSON");
    }

    [RelayCommand]
    private async Task ExportXMLAsync()
    {
        await ExportAsync(_xmlService, "XML");
    }

    private async Task ExportAsync(ILoadInterface saver, string format)
    {
        try
        {
            var filePath = await PickFileAsync(saver.FileFilter, false);
            if (filePath == null) return;

            await saver.SaveAsync(filePath, new List<Client>(), Vklads.ToList(), new List<Account>());

            var box = MessageBoxManager.GetMessageBoxStandard("Успех", $"Экспорт в {format} выполнен успешно.", ButtonEnum.Ok);
            await box.ShowAsync();
        }
        catch (Exception ex)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Ошибка", $"Ошибка экспорта: {ex.Message}", ButtonEnum.Ok);
            await box.ShowAsync();
        }
    }

    private static async Task<string?> PickFileAsync(string filter, bool open)
    {
        var topLevel = TopLevel.GetTopLevel(App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null);
        if (topLevel == null) return null;

        var options = new FilePickerOpenOptions
        {
            Title = "Выберите файл",
            AllowMultiple = false
        };

        if (open)
        {
            var result = await topLevel.StorageProvider.OpenFilePickerAsync(options);
            return result.Count > 0 ? result[0].Path.LocalPath : null;
        }
        else
        {
            var saveOptions = new FilePickerSaveOptions
            {
                Title = "Сохранить файл"
            };
            var result = await topLevel.StorageProvider.SaveFilePickerAsync(saveOptions);
            return result?.Path.LocalPath;
        }
    }
}