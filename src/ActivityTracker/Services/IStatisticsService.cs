namespace ActivityTracker.Services;

public class GroupSummary
{
    public string GroupName { get; set; } = string.Empty;
    public string Color { get; set; } = "#4A90D9";
    public double TotalHours { get; set; }
    public double DailyAverage { get; set; }
    public int ActiveDays { get; set; }
    public int CurrentStreak { get; set; }
}

public class GoalProgressItem
{
    public string Label { get; set; } = string.Empty;
    public string Color { get; set; } = "#4A90D9";
    public double TargetHours { get; set; }
    public double ActualHours { get; set; }
    public double ProgressPercentage => TargetHours > 0 ? Math.Min(ActualHours / TargetHours * 100, 100) : 0;
}

public class HourDistributionItem
{
    public int Hour { get; set; }
    public double TotalMinutes { get; set; }
}

public class StatsSummary
{
    public double TotalHours { get; set; }
    public int TotalSessions { get; set; }
    public List<GroupSummary> GroupSummaries { get; set; } = [];
    public List<HourDistributionItem> HourDistribution { get; set; } = [];
}

public interface IStatisticsService
{
    StatsSummary CalculateForRange(DateOnly start, DateOnly end);
    List<GoalProgressItem> CalculateGoalProgress(DateOnly referenceDate);
}
