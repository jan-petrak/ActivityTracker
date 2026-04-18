namespace ActivityTracker.Services;

public class CalendarEntryItem
{
    public Guid SourceId { get; set; }
    public string ActivityName { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string Color { get; set; } = "#4A90D9";
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Notes { get; set; }
    public Guid ActivityId { get; set; }

    public double DurationHours => (EndTime.ToTimeSpan() - StartTime.ToTimeSpan()).TotalHours;

    public string Summary
    {
        get
        {
            var hours = DurationHours;
            var abbrev = GroupName.Length > 3 ? GroupName[..3].ToUpperInvariant() : GroupName.ToUpperInvariant();
            return $"{hours:0.#}h [{abbrev}] {ActivityName}";
        }
    }
}

public class DayEventOccurrence
{
    public Guid SourceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public int ReminderDaysBefore { get; set; }
    public string? Notes { get; set; }

    public int DaysUntil(DateOnly today) => Date.DayNumber - today.DayNumber;
}

public interface ICalendarService
{
    List<CalendarEntryItem> GetEntriesForDate(DateOnly date);
    List<CalendarEntryItem> GetEntriesForRange(DateOnly start, DateOnly end);
    List<DayEventOccurrence> GetDayEventsForRange(DateOnly start, DateOnly end);
    List<DayEventOccurrence> GetUpcomingDayEvents(DateOnly today);
}
