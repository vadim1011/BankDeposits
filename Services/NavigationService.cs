using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BankDeposits.Services;

public partial class NavigationService : ObservableObject
{
    [ObservableProperty]
    private ObservableObject? _currentViewModel;

    private readonly IServiceProvider _serviceProvider;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void NavigateTo<T>() where T : ObservableObject
    {
        CurrentViewModel = _serviceProvider.GetService(typeof(T)) as T;
    }

    public void NavigateTo(ObservableObject viewModel)
    {
        CurrentViewModel = viewModel;
    }
}
