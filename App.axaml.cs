using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BankDeposits.Services;
using BankDeposits.ViewModels;
using BankDeposits.Views;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BankDeposits;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        // Seed database
        var dbService = serviceProvider.GetRequiredService<IDbService>();
        dbService.SeedDataAsync().Wait();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = serviceProvider.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IDbService, DbService>();
        services.AddSingleton<NavigationService>();

        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<HomePageViewModel>();
        services.AddTransient<VkladsPageViewModel>();
        services.AddTransient<ClientsPageViewModel>();
        services.AddTransient<AccountsPageViewModel>();
        services.AddTransient<GraphPageViewModel>();
        services.AddTransient<ReportPageViewModel>();
    }
}
