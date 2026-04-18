namespace ActivityTracker.Services;

public class CalendarService : ICalendarService
{
    private readonly IDataService _dataService;
    private readonly IRecurrenceService _recurrenceService;

    public CalendarService(IDataService dataService, IRecurrenceService recurrenceService)
    {
        _dataService = dataService;
        _recurrenceService = recurrenceService;
    }

    public List<CalendarEntryItem> GetEntriesForDate(DateOnly date)
    {
        return GetEntriesForRange(date, date);
    }

    public List<CalendarEntryItem> GetEntriesForRange(DateOnly start, DateOnly end)
    {
        var entries = new List<CalendarEntryItem>();
        var data = _dataService.Data;
        var activityLookup = data.Groups
            .SelectMany(g => g.Activities.Select(a => (Group: g, Activity: a)))
            .ToDictionary(x => x.Activity.Id);

        foreach (var pe in data.PlannedEntries)
        {
            if (!activityLookup.TryGetValue(pe.ActivityId, out var info)) continue;

            IEnumerable<DateOnly> dates;
            if (pe.Recurrence != null)
            {
                dates = _recurrenceService.ExpandOccurrences(pe.Recurrence, start, end);
            }
            else
            {
                dates = pe.Date >= start && pe.Date <= end ? [pe.Date] : [];
            }

            foreach (var date in dates)
            {
                entries.Add(new CalendarEntryItem
                {
                    SourceId = pe.Id,
                    ActivityName = info.Activity.Name,
                    GroupName = info.Group.Name,
                    Color = info.Group.Color,
                    Date = date,
                    StartTime = pe.StartTime,
                    EndTime = pe.EndTime,
                    Notes = pe.Notes,
                    ActivityId = pe.ActivityId
                });
            }
        }

        return [.. entries.OrderBy(e => e.Date).ThenBy(e => e.StartTime)];
    }

    public List<DayEventOccurrence> GetDayEventsForRange(DateOnly start, DateOnly end)
    {
        var occurrences = new List<DayEventOccurrence>();

        foreach (var de in _dataService.Data.DayEvents)
        {
            IEnumerable<DateOnly> dates;
            if (de.Recurrence != null)
            {
                dates = _recurrenceService.ExpandOccurrences(de.Recurrence, start, end);
            }
            else
            {
                dates = de.Date >= start && de.Date <= end ? [de.Date] : [];
            }

            foreach (var date in dates)
            {
                occurrences.Add(new DayEventOccurrence
                {
                    SourceId = de.Id,
                    Title = de.Title,
                    Date = date,
                    ReminderDaysBefore = de.ReminderDaysBefore,
                    Notes = de.Notes
                });
            }
        }

        return [.. occurrences.OrderBy(o => o.Date).ThenBy(o => o.Title)];
    }

    public List<DayEventOccurrence> GetUpcomingDayEvents(DateOnly today)
    {
        var result = new List<DayEventOccurrence>();

        foreach (var de in _dataService.Data.DayEvents)
        {
            if (de.ReminderDaysBefore <= 0) continue;

            var windowEnd = today.AddDays(de.ReminderDaysBefore);
            DateOnly? next = null;

            if (de.Recurrence != null)
            {
                next = _recurrenceService
                    .ExpandOccurrences(de.Recurrence, today, windowEnd)
                    .Cast<DateOnly?>()
                    .FirstOrDefault();
            }
            else if (de.Date >= today && de.Date <= windowEnd)
            {
                next = de.Date;
            }

            if (next is not { } nextDate) continue;
            var daysUntil = nextDate.DayNumber - today.DayNumber;
            if (daysUntil <= 0) continue;

            result.Add(new DayEventOccurrence
            {
                SourceId = de.Id,
                Title = de.Title,
                Date = nextDate,
                ReminderDaysBefore = de.ReminderDaysBefore,
                Notes = de.Notes
            });
        }

        return [.. result.OrderBy(o => o.Date).ThenBy(o => o.Title)];
    }
}
