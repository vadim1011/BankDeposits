using CommunityToolkit.Mvvm.ComponentModel;

namespace BankDeposits.ViewModels;

public partial class HomePageViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "Банковские вклады";

    [ObservableProperty]
    private string _description = "Информационная система учета банковских вкладов с поддержкой импорта/экспорта данных, визуализацией и аналитическими отчетами.";
}
