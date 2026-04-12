using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ActivityTracker.Services;

namespace ActivityTracker.ViewModels;

public class DayColumn
{
    public DateOnly Date { get; set; }
    public string Header { get; set; } = string.Empty;
    public bool IsToday { get; set; }
    public ObservableCollection<CalendarEntryItem> Entries { get; set; } = [];
}

public partial class WeekViewModel : ObservableObject
{
    private readonly ICalendarService _calendarService;

    [ObservableProperty]
    private DateOnly weekStart;

    [ObservableProperty]
    private ObservableCollection<DayColumn> days = [];

    [ObservableProperty]
    private string weekHeader = string.Empty;

    public static double HourHeight => 60.0;
    public static double TotalHeight => 24 * HourHeight;

    public WeekViewModel(ICalendarService calendarService)
    {
        _calendarService = calendarService;
    }

    public void Load(DateOnly referenceDate)
    {
        var monday = referenceDate.AddDays(-(((int)referenceDate.DayOfWeek + 6) % 7));
        WeekStart = monday;
        var sunday = monday.AddDays(6);
        WeekHeader = $"{monday:MMM d} – {sunday:MMM d, yyyy}";

        var allEntries = _calendarService.GetEntriesForRange(monday, sunday);
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
                    allEntries.Where(e => e.Date == day))
            });
        }
        Days = columns;
    }
}
