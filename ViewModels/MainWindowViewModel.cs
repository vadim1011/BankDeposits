using BankDeposits.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BankDeposits.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly NavigationService _navigationService;

    [ObservableProperty]
    private ObservableObject? _currentPage;

    [ObservableProperty]
    private string _currentPageTitle = "Главная";

    public MainWindowViewModel(NavigationService navigationService)
    {
        _navigationService = navigationService;
        _navigationService.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(NavigationService.CurrentViewModel))
            {
                CurrentPage = _navigationService.CurrentViewModel;
                UpdatePageTitle();
            }
        };

        NavigateHome();
    }

    private void UpdatePageTitle()
    {
        CurrentPageTitle = CurrentPage switch
        {
            HomePageViewModel => "Главная",
            VkladsPageViewModel => "Вклады",
            ClientsPageViewModel => "Клиенты",
            AccountsPageViewModel => "Счета",
            GraphPageViewModel => "Графики",
            ReportPageViewModel => "Отчеты",
            _ => ""
        };
    }

    [RelayCommand]
    private void NavigateHome()
    {
        _navigationService.NavigateTo<HomePageViewModel>();
    }

    [RelayCommand]
    private void NavigateVklads()
    {
        _navigationService.NavigateTo<VkladsPageViewModel>();
    }

    [RelayCommand]
    private void NavigateClients()
    {
        _navigationService.NavigateTo<ClientsPageViewModel>();
    }

    [RelayCommand]
    private void NavigateAccounts()
    {
        _navigationService.NavigateTo<AccountsPageViewModel>();
    }

    [RelayCommand]
    private void NavigateGraphs()
    {
        _navigationService.NavigateTo<GraphPageViewModel>();
    }

    [RelayCommand]
    private void NavigateReports()
    {
        _navigationService.NavigateTo<ReportPageViewModel>();
    }
}
