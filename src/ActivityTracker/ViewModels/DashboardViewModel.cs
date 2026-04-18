using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ActivityTracker.Services;

namespace ActivityTracker.ViewModels;

public class UpcomingDayEventItem
{
    public DayEventOccurrence Occurrence { get; init; } = null!;
    public int DaysUntil { get; init; }

    public Guid SourceId => Occurrence.SourceId;
    public string Title => Occurrence.Title;
    public DateOnly Date => Occurrence.Date;

    public string RelativeLabel => DaysUntil switch
    {
        0 => "Today",
        1 => "Tomorrow",
        _ => $"in {DaysUntil} days"
    };
}

public partial class DashboardViewModel : ObservableObject
{
    private readonly ICalendarService _calendarService;
    private readonly IStatisticsService _statisticsService;
    private readonly INavigationService _navigationService;
    private readonly IDataService _dataService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<CalendarEntryItem> todaySchedule = [];

    [ObservableProperty]
    private ObservableCollection<DayEventOccurrence> todayDayEvents = [];

    [ObservableProperty]
    private ObservableCollection<UpcomingDayEventItem> upcomingDayEvents = [];

    [ObservableProperty]
    private double weeklyHoursLogged;

    [ObservableProperty]
    private ObservableCollection<GoalProgressItem> activeGoals = [];

    [ObservableProperty]
    private ObservableCollection<GroupSummary> weeklyGroupSummaries = [];

    public DashboardViewModel(
        ICalendarService calendarService,
        IStatisticsService statisticsService,
        INavigationService navigationService,
        IDataService dataService,
        IDialogService dialogService)
    {
        _calendarService = calendarService;
        _statisticsService = statisticsService;
        _navigationService = navigationService;
        _dataService = dataService;
        _dialogService = dialogService;
        Refresh();
    }

    [RelayCommand]
    private void Refresh()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var todayEntries = _calendarService.GetEntriesForDate(today);
        TodaySchedule = new ObservableCollection<CalendarEntryItem>(todayEntries);

        var todayDayEvts = _calendarService.GetDayEventsForRange(today, today);
        TodayDayEvents = new ObservableCollection<DayEventOccurrence>(todayDayEvts);

        var upcoming = _calendarService.GetUpcomingDayEvents(today)
            .Select(o => new UpcomingDayEventItem { Occurrence = o, DaysUntil = o.DaysUntil(today) });
        UpcomingDayEvents = new ObservableCollection<UpcomingDayEventItem>(upcoming);

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

    public void EditDayEvent(Guid sourceId)
    {
        var existing = _dataService.Data.DayEvents.FirstOrDefault(d => d.Id == sourceId);
        if (existing == null) return;

        if (_dialogService.ShowDayEventEditor(existing, out var result))
        {
            var idx = _dataService.Data.DayEvents.FindIndex(d => d.Id == sourceId);
            if (idx >= 0) _dataService.Data.DayEvents[idx] = result;
            _dataService.NotifyChanged();
            Refresh();
        }
    }

    public void DeleteDayEvent(Guid sourceId)
    {
        var removed = _dataService.Data.DayEvents.RemoveAll(d => d.Id == sourceId);
        if (removed > 0)
        {
            _dataService.NotifyChanged();
            Refresh();
        }
    }
}
