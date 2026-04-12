using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace ActivityTracker.ViewModels;

public partial class CalendarViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private ObservableObject? activeCalendarView;

    [ObservableProperty]
    private DateTime selectedDate = DateTime.Today;

    [ObservableProperty]
    private string viewMode = "Week";

    public CalendarViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        SwitchView("Week");
    }

    [RelayCommand]
    private void SwitchView(string mode)
    {
        ViewMode = mode;
        ActiveCalendarView = mode switch
        {
            "Day" => CreateDayViewModel(DateOnly.FromDateTime(SelectedDate)),
            "Week" => CreateWeekViewModel(DateOnly.FromDateTime(SelectedDate)),
            "Month" => CreateMonthViewModel(DateOnly.FromDateTime(SelectedDate)),
            _ => ActiveCalendarView
        };
    }

    [RelayCommand]
    private void NavigateForward()
    {
        SelectedDate = ViewMode switch
        {
            "Day" => SelectedDate.AddDays(1),
            "Week" => SelectedDate.AddDays(7),
            "Month" => SelectedDate.AddMonths(1),
            _ => SelectedDate
        };
        SwitchView(ViewMode);
    }

    [RelayCommand]
    private void NavigateBack()
    {
        SelectedDate = ViewMode switch
        {
            "Day" => SelectedDate.AddDays(-1),
            "Week" => SelectedDate.AddDays(-7),
            "Month" => SelectedDate.AddMonths(-1),
            _ => SelectedDate
        };
        SwitchView(ViewMode);
    }

    [RelayCommand]
    private void GoToToday()
    {
        SelectedDate = DateTime.Today;
        SwitchView(ViewMode);
    }

    private DayViewModel CreateDayViewModel(DateOnly date)
    {
        var vm = _serviceProvider.GetRequiredService<DayViewModel>();
        vm.Load(date);
        return vm;
    }

    private WeekViewModel CreateWeekViewModel(DateOnly date)
    {
        var vm = _serviceProvider.GetRequiredService<WeekViewModel>();
        vm.Load(date);
        return vm;
    }

    private MonthViewModel CreateMonthViewModel(DateOnly date)
    {
        var vm = _serviceProvider.GetRequiredService<MonthViewModel>();
        vm.Load(date);
        return vm;
    }
}
