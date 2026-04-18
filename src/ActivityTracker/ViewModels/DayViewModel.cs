using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ActivityTracker.Models;
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
    private ObservableCollection<DayEventOccurrence> dayEvents = [];

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
        var dayEvts = _calendarService.GetDayEventsForRange(date, date);
        DayEvents = new ObservableCollection<DayEventOccurrence>(dayEvts);
    }

    public void AddDayEvent(DateOnly targetDate)
    {
        var template = new DayEvent { Date = targetDate };
        if (_dialogService.ShowDayEventEditor(template, out var result))
        {
            _dataService.Data.DayEvents.Add(result);
            _dataService.NotifyChanged();
            Load(Date);
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
            Load(Date);
        }
    }

    public void DeleteDayEvent(Guid sourceId)
    {
        if (_dataService.Data.DayEvents.RemoveAll(d => d.Id == sourceId) > 0)
        {
            _dataService.NotifyChanged();
            Load(Date);
        }
    }
}
