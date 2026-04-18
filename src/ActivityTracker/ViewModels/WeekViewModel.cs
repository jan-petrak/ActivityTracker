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

    [ObservableProperty]
    private DateOnly weekStart;

    [ObservableProperty]
    private ObservableCollection<DayColumn> days = [];

    [ObservableProperty]
    private string weekHeader = string.Empty;

    public static double HourHeight => 60.0;
    public static double TotalHeight => 24 * HourHeight;

    public WeekViewModel(ICalendarService calendarService, IDialogService dialogService, IDataService dataService)
    {
        _calendarService = calendarService;
        _dialogService = dialogService;
        _dataService = dataService;
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
            Load(WeekStart);
        }
    }

    public void EditDayEvent(Guid sourceId)
    {
        var existing = _dataService.Data.DayEvents.FirstOrDefault(d => d.Id == sourceId);
        if (existing == null) return;
        if (_dialogService.ShowDayEventEditor(existing, out var result))
        {
            var idx = _dataService.Data.DayEvents.FindIndex(d => d.Id == sourceId);
            if (idx >= 0) _dataService.Data.DayEvents[idx] = result;
            _dataService.NotifyChanged();
            Load(WeekStart);
        }
    }

    public void DeleteDayEvent(Guid sourceId)
    {
        if (_dataService.Data.DayEvents.RemoveAll(d => d.Id == sourceId) > 0)
        {
            _dataService.NotifyChanged();
            Load(WeekStart);
        }
    }
}
