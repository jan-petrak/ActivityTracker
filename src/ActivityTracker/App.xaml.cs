using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ActivityTracker.Services;
using ActivityTracker.ViewModels;

namespace ActivityTracker;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        // Services
        services.AddSingleton<IDataService, JsonDataService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IRecurrenceService, RecurrenceService>();
        services.AddSingleton<ICalendarService, CalendarService>();
        services.AddSingleton<IStatisticsService, StatisticsService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IAuditLogService, AuditLogService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<CalendarViewModel>();
        services.AddTransient<DayViewModel>();
        services.AddTransient<WeekViewModel>();
        services.AddTransient<MonthViewModel>();
        services.AddTransient<ActivitiesViewModel>();
        services.AddTransient<StatisticsViewModel>();

        Services = services.BuildServiceProvider();

        var dataService = Services.GetRequiredService<IDataService>();
        await dataService.LoadAsync();

        var mainWindow = new MainWindow
        {
            DataContext = Services.GetRequiredService<MainViewModel>()
        };
        mainWindow.Show();
    }
}
