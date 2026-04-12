using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ActivityTracker.Services;

namespace ActivityTracker.ViewModels;

public class MonthDayCell
{
    public DateOnly Date { get; set; }
    public bool IsCurrentMonth { get; set; }
    public bool IsToday { get; set; }
    public double TotalHours { get; set; }
    public ObservableCollection<CalendarEntryItem> Entries { get; set; } = [];

    public string DayNumber => Date.Day.ToString();

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

    [ObservableProperty]
    private int year;

    [ObservableProperty]
    private int month;

    [ObservableProperty]
    private string monthHeader = string.Empty;

    [ObservableProperty]
    private ObservableCollection<MonthDayCell> dayCells = [];

    public MonthViewModel(ICalendarService calendarService)
    {
        _calendarService = calendarService;
    }

    public void Load(DateOnly referenceDate)
    {
        Year = referenceDate.Year;
        Month = referenceDate.Month;
        MonthHeader = referenceDate.ToString("MMMM yyyy");

        var firstOfMonth = new DateOnly(Year, Month, 1);
        var mondayOffset = ((int)firstOfMonth.DayOfWeek + 6) % 7;
        var gridStart = firstOfMonth.AddDays(-mondayOffset);

        var lastOfMonth = new DateOnly(Year, Month, DateTime.DaysInMonth(Year, Month));
        var gridEnd = gridStart.AddDays(41); // 6 weeks

        var allEntries = _calendarService.GetEntriesForRange(gridStart, gridEnd);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var cells = new ObservableCollection<MonthDayCell>();
        for (var i = 0; i < 42; i++)
        {
            var day = gridStart.AddDays(i);
            var dayEntries = allEntries.Where(e => e.Date == day).ToList();
            cells.Add(new MonthDayCell
            {
                Date = day,
                IsCurrentMonth = day.Month == Month,
                IsToday = day == today,
                TotalHours = dayEntries.Sum(e => e.DurationHours),
                Entries = new ObservableCollection<CalendarEntryItem>(dayEntries)
            });
        }
        DayCells = cells;
    }
}
