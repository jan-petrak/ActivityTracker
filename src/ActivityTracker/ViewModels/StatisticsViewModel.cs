using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ActivityTracker.Services;

namespace ActivityTracker.ViewModels;

public partial class StatisticsViewModel : ObservableObject
{
    private readonly IStatisticsService _statisticsService;
    private readonly IDataService _dataService;
    private readonly IDialogService _dialogService;
    private readonly IAuditLogService _auditLog;

    [ObservableProperty]
    private string periodSelection = "LastMonth";

    [ObservableProperty]
    private DateOnly rangeStart;

    [ObservableProperty]
    private DateOnly rangeEnd;

    [ObservableProperty]
    private double totalHours;

    [ObservableProperty]
    private int totalSessions;

    [ObservableProperty]
    private ObservableCollection<GroupSummary> groupSummaries = [];

    [ObservableProperty]
    private ObservableCollection<GoalProgressItem> goalProgress = [];

    [ObservableProperty]
    private ObservableCollection<HourDistributionItem> hourDistribution = [];

    public StatisticsViewModel(IStatisticsService statisticsService, IDataService dataService, IDialogService dialogService, IAuditLogService auditLog)
    {
        _statisticsService = statisticsService;
        _dataService = dataService;
        _dialogService = dialogService;
        _auditLog = auditLog;
        SelectPeriod("LastMonth");
    }

    [RelayCommand]
    private void SelectPeriod(string period)
    {
        PeriodSelection = period;
        var today = DateOnly.FromDateTime(DateTime.Today);

        (RangeStart, RangeEnd) = period switch
        {
            "LastWeek" => (today.AddDays(-7), today),
            "LastMonth" => (today.AddDays(-30), today),
            "Last3Months" => (today.AddDays(-90), today),
            _ => (RangeStart, RangeEnd)
        };

        Refresh();
    }

    [RelayCommand]
    private void Refresh()
    {
        var stats = _statisticsService.CalculateForRange(RangeStart, RangeEnd);
        TotalHours = stats.TotalHours;
        TotalSessions = stats.TotalSessions;
        GroupSummaries = new ObservableCollection<GroupSummary>(stats.GroupSummaries);
        HourDistribution = new ObservableCollection<HourDistributionItem>(stats.HourDistribution);

        var goals = _statisticsService.CalculateGoalProgress(DateOnly.FromDateTime(DateTime.Today));
        GoalProgress = new ObservableCollection<GoalProgressItem>(goals);
    }

    [RelayCommand]
    private void AddGoal()
    {
        if (_dialogService.ShowGoalEditor(_dataService.Data.Groups, null, out var goal))
        {
            _dataService.Data.Goals.Add(goal);
            _dataService.NotifyChanged();
            _auditLog.Log("GoalCreated",
                $"Created goal for group id {goal.GroupId}",
                new { goal });
            Refresh();
        }
    }
}
