using ActivityTracker.Models;

namespace ActivityTracker.Services;

public class RecurrenceService : IRecurrenceService
{
    public IEnumerable<DateOnly> ExpandOccurrences(RecurrencePattern pattern, DateOnly rangeStart, DateOnly rangeEnd)
    {
        var endDate = pattern.EndDate.HasValue && pattern.EndDate.Value < rangeEnd
            ? pattern.EndDate.Value
            : rangeEnd;

        switch (pattern.Type)
        {
            case RecurrenceType.Daily:
                for (var date = pattern.StartDate; date <= endDate; date = date.AddDays(pattern.Interval))
                {
                    if (date >= rangeStart && !pattern.Exceptions.Contains(date))
                        yield return date;
                }
                break;

            case RecurrenceType.Weekly:
                var weekStart = pattern.StartDate;
                while (weekStart <= endDate)
                {
                    foreach (var day in pattern.DaysOfWeek)
                    {
                        var daysUntil = ((int)day - (int)weekStart.DayOfWeek + 7) % 7;
                        var date = weekStart.AddDays(daysUntil);
                        if (date >= rangeStart && date <= endDate && date >= pattern.StartDate
                            && !pattern.Exceptions.Contains(date))
                        {
                            yield return date;
                        }
                    }
                    weekStart = weekStart.AddDays(7 * pattern.Interval);
                }
                break;

            case RecurrenceType.Monthly:
                if (!pattern.DayOfMonth.HasValue) yield break;
                var current = new DateOnly(pattern.StartDate.Year, pattern.StartDate.Month, 1);
                while (current <= endDate)
                {
                    var daysInMonth = DateTime.DaysInMonth(current.Year, current.Month);
                    var dayNum = Math.Min(pattern.DayOfMonth.Value, daysInMonth);
                    var date = new DateOnly(current.Year, current.Month, dayNum);
                    if (date >= rangeStart && date <= endDate && date >= pattern.StartDate
                        && !pattern.Exceptions.Contains(date))
                    {
                        yield return date;
                    }
                    current = current.AddMonths(pattern.Interval);
                }
                break;
        }
    }
}
