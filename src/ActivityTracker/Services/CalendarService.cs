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

        // Look one day before to catch cross-midnight continuations into range start
        var queryStart = start.AddDays(-1);

        foreach (var pe in data.PlannedEntries)
        {
            if (!activityLookup.TryGetValue(pe.ActivityId, out var info)) continue;

            var duration = pe.End - pe.Start;
            var startTod = pe.Start.TimeOfDay;

            IEnumerable<DateOnly> occurrenceDates;
            if (pe.Recurrence != null)
            {
                occurrenceDates = _recurrenceService.ExpandOccurrences(pe.Recurrence, queryStart, end);
            }
            else
            {
                occurrenceDates = pe.Date >= queryStart && pe.Date <= end ? [pe.Date] : [];
            }

            foreach (var occDate in occurrenceDates)
            {
                var iStart = occDate.ToDateTime(TimeOnly.FromTimeSpan(startTod));
                var iEnd = iStart + duration;
                var iEndDate = DateOnly.FromDateTime(iEnd);

                // Primary item: lives on occDate if in [start, end]
                if (occDate >= start && occDate <= end)
                {
                    entries.Add(new CalendarEntryItem
                    {
                        SourceId = pe.Id,
                        ActivityName = info.Activity.Name,
                        GroupName = info.Group.Name,
                        Color = info.Group.Color,
                        Start = iStart,
                        End = iEnd,
                        IsContinuation = false,
                        Notes = pe.Notes,
                        ActivityId = pe.ActivityId
                    });
                }

                // Continuation items: one per subsequent day in [start, end]
                // Skip if end is exactly midnight — nothing to show on that day
                for (var contDate = (occDate >= start ? occDate : start.AddDays(-1)).AddDays(1);
                     contDate <= iEndDate && contDate <= end && (contDate < iEndDate || iEnd.TimeOfDay > TimeSpan.Zero);
                     contDate = contDate.AddDays(1))
                {
                    entries.Add(new CalendarEntryItem
                    {
                        SourceId = pe.Id,
                        ActivityName = info.Activity.Name,
                        GroupName = info.Group.Name,
                        Color = info.Group.Color,
                        Start = contDate.ToDateTime(TimeOnly.MinValue),
                        End = iEnd,
                        IsContinuation = true,
                        Notes = pe.Notes,
                        ActivityId = pe.ActivityId
                    });
                }
            }
        }

        return [.. entries.OrderBy(e => e.Date).ThenBy(e => e.Start)];
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
