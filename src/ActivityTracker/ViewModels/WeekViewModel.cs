using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ActivityTracker.Models;
using ActivityTracker.Services;

namespace ActivityTracker.ViewModels;

public class DayColumn
{
    public DateOnly Date { get; set; }
    public string Header { get; set; } = string.Empty;
    public bool IsToday { get; set; }
    public ObservableCollection<CalendarEntryItem> Entries { get; set; } = [];
    public ObservableCollection<DayEventOccurrence> DayEvents { get; set; } = [];
}

public partial class WeekViewModel : ObservableObject
{
    private readonly ICalendarService _calendarService;
    private readonly IDialogService _dialogService;
    private readonly IDataService _dataService;
    private readonly IAuditLogService _auditLog;

    [ObservableProperty]
    private DateOnly weekStart;

    [ObservableProperty]
    private ObservableCollection<DayColumn> days = [];

    [ObservableProperty]
    private string weekHeader = string.Empty;

    public static double HourHeight => 60.0;
    public static double TotalHeight => 24 * HourHeight;

    public WeekViewModel(ICalendarService calendarService, IDialogService dialogService, IDataService dataService, IAuditLogService auditLog)
    {
        _calendarService = calendarService;
        _dialogService = dialogService;
        _dataService = dataService;
        _auditLog = auditLog;
    }

    public void Load(DateOnly referenceDate)
    {
        var monday = referenceDate.AddDays(-(((int)referenceDate.DayOfWeek + 6) % 7));
        WeekStart = monday;
        var sunday = monday.AddDays(6);
        WeekHeader = $"{monday:MMM d} – {sunday:MMM d, yyyy}";

        var allEntries = _calendarService.GetEntriesForRange(monday, sunday);
        var allDayEvents = _calendarService.GetDayEventsForRange(monday, sunday);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var columns = new ObservableCollection<DayColumn>();
        for (var i = 0; i < 7; i++)
        {
            var day = monday.AddDays(i);
            columns.Add(new DayColumn
            {
                Date = day,
                Header = day.ToString("ddd d"),
                IsToday = day == today,
                Entries = new ObservableCollection<CalendarEntryItem>(
                    allEntries.Where(e => e.Date == day)),
                DayEvents = new ObservableCollection<DayEventOccurrence>(
                    allDayEvents.Where(e => e.Date == day))
            });
        }
        Days = columns;
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
            Load(WeekStart);
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
            Load(WeekStart);
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
            Load(WeekStart);
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
                $"Updated planned entry on {result.Date:yyyy-MM-dd} {result.StartTime:HH\\:mm}-{result.EndTime:HH\\:mm}",
                new { plannedEntryId = sourceId, before, after = result });
            Load(WeekStart);
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
                $"Deleted planned entry on {existing.Date:yyyy-MM-dd} {existing.StartTime:HH\\:mm}-{existing.EndTime:HH\\:mm}",
                new { plannedEntry = existing });
            Load(WeekStart);
        }
    }
}
