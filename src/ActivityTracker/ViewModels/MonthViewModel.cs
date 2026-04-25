using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ActivityTracker.Models;
using ActivityTracker.Services;

namespace ActivityTracker.ViewModels;

public class MonthDayCell
{
    public DateOnly Date { get; set; }
    public bool IsCurrentMonth { get; set; }
    public bool IsToday { get; set; }
    public double TotalHours { get; set; }
    public ObservableCollection<CalendarEntryItem> Entries { get; set; } = [];
    public List<DayEventOccurrence> DayEvents { get; set; } = [];

    public string DayNumber => Date.Day.ToString();
    public bool HasDayEvents => DayEvents.Count > 0;
    public string DayEventsTooltip => string.Join("\n", DayEvents.Select(e => e.Title));

    public string SummaryText
    {
        get
        {
            if (Entries.Count == 0) return string.Empty;
            var lines = Entries.Take(3).Select(e => e.Summary);
            var text = string.Join("\n", lines);
            if (Entries.Count > 3)
                text += $"\n+{Entries.Count - 3} more";
            return text;
        }
    }
}

public partial class MonthViewModel : ObservableObject
{
    private readonly ICalendarService _calendarService;
    private readonly IDialogService _dialogService;
    private readonly IDataService _dataService;
    private readonly IAuditLogService _auditLog;

    [ObservableProperty]
    private int year;

    [ObservableProperty]
    private int month;

    [ObservableProperty]
    private string monthHeader = string.Empty;

    [ObservableProperty]
    private ObservableCollection<MonthDayCell> dayCells = [];

    private DateOnly _referenceDate;

    public MonthViewModel(ICalendarService calendarService, IDialogService dialogService, IDataService dataService, IAuditLogService auditLog)
    {
        _calendarService = calendarService;
        _dialogService = dialogService;
        _dataService = dataService;
        _auditLog = auditLog;
    }

    public void Load(DateOnly referenceDate)
    {
        _referenceDate = referenceDate;
        Year = referenceDate.Year;
        Month = referenceDate.Month;
        MonthHeader = referenceDate.ToString("MMMM yyyy");

        var firstOfMonth = new DateOnly(Year, Month, 1);
        var mondayOffset = ((int)firstOfMonth.DayOfWeek + 6) % 7;
        var gridStart = firstOfMonth.AddDays(-mondayOffset);

        var gridEnd = gridStart.AddDays(41);

        var allEntries = _calendarService.GetEntriesForRange(gridStart, gridEnd);
        var allDayEvents = _calendarService.GetDayEventsForRange(gridStart, gridEnd);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var cells = new ObservableCollection<MonthDayCell>();
        for (var i = 0; i < 42; i++)
        {
            var day = gridStart.AddDays(i);
            // Exclude continuation items from month view — they belong to the start date
            var dayEntries = allEntries.Where(e => e.Date == day && !e.IsContinuation).ToList();
            var dayDayEvents = allDayEvents.Where(e => e.Date == day).ToList();
            cells.Add(new MonthDayCell
            {
                Date = day,
                IsCurrentMonth = day.Month == Month,
                IsToday = day == today,
                TotalHours = dayEntries.Sum(e => e.DurationHours),
                Entries = new ObservableCollection<CalendarEntryItem>(dayEntries),
                DayEvents = dayDayEvents
            });
        }
        DayCells = cells;
    }

    public void AddDayEvent(DateOnly targetDate)
    {
        var template = new DayEvent { Date = targetDate };
        if (_dialogService.ShowDayEventEditor(template, out var result))
        {
            _dataService.Data.DayEvents.Add(result);
            _dataService.NotifyChanged();
            _auditLog.Log("DayEventCreated",
                $"Created whole-day event '{result.Title}' on {result.Date:yyyy-MM-dd}",
                new { dayEvent = result });
            Load(_referenceDate);
        }
    }

    public void EditDayEvent(Guid sourceId)
    {
        var existing = _dataService.Data.DayEvents.FirstOrDefault(d => d.Id == sourceId);
        if (existing == null) return;
        var before = AuditSnapshots.Clone(existing);
        if (_dialogService.ShowDayEventEditor(existing, out var result))
        {
            var idx = _dataService.Data.DayEvents.FindIndex(d => d.Id == sourceId);
            if (idx >= 0) _dataService.Data.DayEvents[idx] = result;
            _dataService.NotifyChanged();
            _auditLog.Log("DayEventUpdated",
                $"Updated whole-day event '{result.Title}' on {result.Date:yyyy-MM-dd}",
                new { dayEventId = sourceId, before, after = result });
            Load(_referenceDate);
        }
    }

    public void DeleteDayEvent(Guid sourceId)
    {
        var existing = _dataService.Data.DayEvents.FirstOrDefault(d => d.Id == sourceId);
        if (existing == null) return;
        if (_dataService.Data.DayEvents.RemoveAll(d => d.Id == sourceId) > 0)
        {
            _dataService.NotifyChanged();
            _auditLog.Log("DayEventDeleted",
                $"Deleted whole-day event '{existing.Title}' on {existing.Date:yyyy-MM-dd}",
                new { dayEvent = existing });
            Load(_referenceDate);
        }
    }


    public void EditEntry(Guid sourceId)
    {
        var existing = _dataService.Data.PlannedEntries.FirstOrDefault(p => p.Id == sourceId);
        if (existing == null) return;
        var before = AuditSnapshots.Clone(existing);
        if (_dialogService.ShowPlannedEntryEditor(_dataService.Data.Groups, null, existing, out var result))
        {
            var idx = _dataService.Data.PlannedEntries.FindIndex(p => p.Id == sourceId);
            if (idx >= 0) _dataService.Data.PlannedEntries[idx] = result;
            _dataService.NotifyChanged();
            _auditLog.Log("PlannedEntryUpdated",
                $"Updated planned entry on {result.Date:yyyy-MM-dd} {result.Start:HH\\:mm}-{result.End:HH\\:mm}",
                new { plannedEntryId = sourceId, before, after = result });
            Load(_referenceDate);
        }
    }

    public void DeleteEntry(Guid sourceId)
    {
        var existing = _dataService.Data.PlannedEntries.FirstOrDefault(p => p.Id == sourceId);
        if (existing == null) return;
        if (_dataService.Data.PlannedEntries.RemoveAll(p => p.Id == sourceId) > 0)
        {
            _dataService.NotifyChanged();
            _auditLog.Log("PlannedEntryDeleted",
                $"Deleted planned entry on {existing.Date:yyyy-MM-dd} {existing.Start:HH\\:mm}-{existing.End:HH\\:mm}",
                new { plannedEntry = existing });
            Load(_referenceDate);
        }
    }

    
}
