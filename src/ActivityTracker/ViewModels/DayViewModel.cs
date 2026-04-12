using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ActivityTracker.Services;

namespace ActivityTracker.ViewModels;

public partial class DayViewModel : ObservableObject
{
    private readonly ICalendarService _calendarService;
    private readonly IDialogService _dialogService;
    private readonly IDataService _dataService;

    [ObservableProperty]
    private DateOnly date;

    [ObservableProperty]
    private ObservableCollection<CalendarEntryItem> entries = [];

    [ObservableProperty]
    private string dateHeader = string.Empty;

    public static double HourHeight => 60.0;
    public static double TotalHeight => 24 * HourHeight;

    public DayViewModel(ICalendarService calendarService, IDialogService dialogService, IDataService dataService)
    {
        _calendarService = calendarService;
        _dialogService = dialogService;
        _dataService = dataService;
    }

    public void Load(DateOnly date)
    {
        Date = date;
        DateHeader = date.ToString("dddd, MMMM d, yyyy");
        var items = _calendarService.GetEntriesForDate(date);
        Entries = new ObservableCollection<CalendarEntryItem>(items);
    }
}
