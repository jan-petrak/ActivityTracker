using ActivityTracker.Models;

namespace ActivityTracker.Services;

public class StatisticsService : IStatisticsService
{
    private readonly IDataService _dataService;
    private readonly ICalendarService _calendarService;

    public StatisticsService(IDataService dataService, ICalendarService calendarService)
    {
        _dataService = dataService;
        _calendarService = calendarService;
    }

    public StatsSummary CalculateForRange(DateOnly start, DateOnly end)
    {
        var data = _dataService.Data;
        var entries = _calendarService.GetEntriesForRange(start, end);
        var totalDays = (end.ToDateTime(TimeOnly.MinValue) - start.ToDateTime(TimeOnly.MinValue)).Days + 1;

        var groupSummaries = new List<GroupSummary>();
        foreach (var group in data.Groups)
        {
            var activityIds = group.Activities.Select(a => a.Id).ToHashSet();
            var groupEntries = entries.Where(e => activityIds.Contains(e.ActivityId)).ToList();
            if (groupEntries.Count == 0) continue;

            var totalHours = groupEntries.Sum(EntryHours);
            var activeDates = groupEntries.Select(e => e.Date).Distinct().OrderBy(d => d).ToList();

            groupSummaries.Add(new GroupSummary
            {
                GroupName = group.Name,
                Color = group.Color,
                TotalHours = totalHours,
                DailyAverage = totalDays > 0 ? totalHours / totalDays : 0,
                ActiveDays = activeDates.Count,
                CurrentStreak = CalculateStreak(activeDates, end)
            });
        }

        var hourDist = Enumerable.Range(0, 24).Select(h => new HourDistributionItem { Hour = h }).ToList();
        foreach (var entry in entries)
        {
            var startMinute = entry.StartTime.Hour * 60 + entry.StartTime.Minute;
            var endMinute = EntryEndMinute(entry);
            for (var m = startMinute; m < endMinute; m++)
            {
                hourDist[m / 60].TotalMinutes++;
            }
        }

        return new StatsSummary
        {
            TotalHours = entries.Sum(EntryHours),
            TotalSessions = entries.Count,
            GroupSummaries = groupSummaries,
            HourDistribution = hourDist
        };
    }

    public List<GoalProgressItem> CalculateGoalProgress(DateOnly referenceDate)
    {
        var data = _dataService.Data;
        var results = new List<GoalProgressItem>();

        foreach (var goal in data.Goals.Where(g => g.IsActive))
        {
            var (start, end) = GetGoalPeriodRange(goal.Period, referenceDate);
            var group = data.Groups.FirstOrDefault(g => g.Id == goal.GroupId);
            if (group == null) continue;

            HashSet<Guid> activityIds = goal.ActivityId.HasValue
                ? [goal.ActivityId.Value]
                : [.. group.Activities.Select(a => a.Id)];

            var hours = _calendarService.GetEntriesForRange(start, end)
                .Where(e => activityIds.Contains(e.ActivityId))
                .Sum(EntryHours);

            var activityName = goal.ActivityId.HasValue
                ? group.Activities.FirstOrDefault(a => a.Id == goal.ActivityId.Value)?.Name
                : null;

            var label = activityName != null
                ? $"{group.Name} / {activityName} ({goal.Period})"
                : $"{group.Name} ({goal.Period})";

            results.Add(new GoalProgressItem
            {
                Label = label,
                Color = group.Color,
                TargetHours = goal.TargetHours,
                ActualHours = hours
            });
        }

        return results;
    }

    private static int EntryEndMinute(CalendarEntryItem e) =>
        e.EndTime == TimeOnly.MinValue ? 24 * 60 : e.EndTime.Hour * 60 + e.EndTime.Minute;

    private static double EntryHours(CalendarEntryItem e) =>
        (EntryEndMinute(e) - (e.StartTime.Hour * 60 + e.StartTime.Minute)) / 60.0;

    private static (DateOnly Start, DateOnly End) GetGoalPeriodRange(GoalPeriod period, DateOnly reference)
    {
        return period switch
        {
            GoalPeriod.Weekly => (
                reference.AddDays(-(int)reference.DayOfWeek + (int)DayOfWeek.Monday),
                reference.AddDays(-(int)reference.DayOfWeek + (int)DayOfWeek.Monday + 6)),
            GoalPeriod.Monthly => (
                new DateOnly(reference.Year, reference.Month, 1),
                new DateOnly(reference.Year, reference.Month, DateTime.DaysInMonth(reference.Year, reference.Month))),
            _ => (reference, reference)
        };
    }

    private static int CalculateStreak(List<DateOnly> sortedDates, DateOnly upTo)
    {
        if (sortedDates.Count == 0) return 0;
        var streak = 0;
        var current = upTo;
        var dateSet = sortedDates.ToHashSet();
        while (dateSet.Contains(current))
        {
            streak++;
            current = current.AddDays(-1);
        }
        return streak;
    }
}
