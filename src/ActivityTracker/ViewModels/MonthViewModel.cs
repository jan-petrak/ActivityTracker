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

    [ObservableProperty]
    private int year;

    [ObservableProperty]
    private int month;

    [ObservableProperty]
    private string monthHeader = string.Empty;

    [ObservableProperty]
    private ObservableCollection<MonthDayCell> dayCells = [];

    private DateOnly _referenceDate;

    public MonthViewModel(ICalendarService calendarService, IDialogService dialogService, IDataService dataService)
    {
        _calendarService = calendarService;
        _dialogService = dialogService;
        _dataService = dataService;
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
            var dayEntries = allEntries.Where(e => e.Date == day).ToList();
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
            Load(_referenceDate);
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
            Load(_referenceDate);
        }
    }

    public void DeleteDayEvent(Guid sourceId)
    {
        if (_dataService.Data.DayEvents.RemoveAll(d => d.Id == sourceId) > 0)
        {
            _dataService.NotifyChanged();
            Load(_referenceDate);
        }
    }
}
