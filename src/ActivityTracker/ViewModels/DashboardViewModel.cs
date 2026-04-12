using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ActivityTracker.Services;

namespace ActivityTracker.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ICalendarService _calendarService;
    private readonly IStatisticsService _statisticsService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ObservableCollection<CalendarEntryItem> todaySchedule = [];

    [ObservableProperty]
    private double weeklyHoursLogged;

    [ObservableProperty]
    private ObservableCollection<GoalProgressItem> activeGoals = [];

    [ObservableProperty]
    private ObservableCollection<GroupSummary> weeklyGroupSummaries = [];

    public DashboardViewModel(
        ICalendarService calendarService,
        IStatisticsService statisticsService,
        INavigationService navigationService)
    {
        _calendarService = calendarService;
        _statisticsService = statisticsService;
        _navigationService = navigationService;
        Refresh();
    }

    [RelayCommand]
    private void Refresh()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var todayEntries = _calendarService.GetEntriesForDate(today);
        TodaySchedule = new ObservableCollection<CalendarEntryItem>(todayEntries);

        var weekStart = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
        if (today.DayOfWeek == DayOfWeek.Sunday) weekStart = weekStart.AddDays(-7);
        var weekEnd = weekStart.AddDays(6);

        var weekStats = _statisticsService.CalculateForRange(weekStart, weekEnd);
        WeeklyHoursLogged = weekStats.TotalHours;
        WeeklyGroupSummaries = new ObservableCollection<GroupSummary>(weekStats.GroupSummaries);

        var goals = _statisticsService.CalculateGoalProgress(today);
        ActiveGoals = new ObservableCollection<GoalProgressItem>(goals);
    }

    [RelayCommand]
    private void GoToCalendar()
    {
        _navigationService.NavigateTo<CalendarViewModel>();
    }
}
