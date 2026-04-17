using ActivityTracker.Models;

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

        return entries.OrderBy(e => e.Date).ThenBy(e => e.StartTime).ToList();
    }
}
