using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ActivityTracker.Services;
using ActivityTracker.ViewModels;

namespace ActivityTracker.Views;

public class HourLabel
{
    public string Label { get; set; } = string.Empty;
    public double Top { get; set; }
}

public partial class DayView : UserControl
{
    public static List<HourLabel> Hours { get; } = Enumerable.Range(0, 24)
        .Select(h => new HourLabel
        {
            Label = new TimeOnly(h, 0).ToString("h tt"),
            Top = h * 60.0
        }).ToList();

    public DayView()
    {
        InitializeComponent();
        Loaded += DayView_Loaded;
    }

    private void DayView_Loaded(object sender, RoutedEventArgs e)
    {
        PositionEntryBlocks();
    }

    private void ScrollViewer_Loaded(object sender, RoutedEventArgs e)
    {
        // Scroll to 7 AM by default
        if (sender is ScrollViewer sv)
        {
            sv.ScrollToVerticalOffset(7 * 60);
        }
    }

    private void PositionEntryBlocks()
    {
        if (DataContext is not DayViewModel vm) return;

        var container = EntriesControl;
        for (var i = 0; i < container.Items.Count; i++)
        {
            if (container.ItemContainerGenerator.ContainerFromIndex(i) is ContentPresenter cp
                && container.Items[i] is CalendarEntryItem entry)
            {
                var top = (entry.StartTime.Hour * 60 + entry.StartTime.Minute);
                var height = (entry.EndTime.ToTimeSpan() - entry.StartTime.ToTimeSpan()).TotalMinutes;
                Canvas.SetTop(cp, top);
                cp.Height = Math.Max(height, 20);
            }
        }
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo info)
    {
        base.OnRenderSizeChanged(info);
        PositionEntryBlocks();
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not DayViewModel vm) return;
        var pos = e.GetPosition((IInputElement)sender);
        var hour = (int)(pos.Y / 60.0);
        var snappedHour = Math.Clamp(hour, 0, 23);

        var dataService = App.Services.GetService(typeof(IDataService)) as IDataService;
        var dialogService = App.Services.GetService(typeof(IDialogService)) as IDialogService;
        if (dataService == null || dialogService == null) return;

        var newEntry = new Models.TimeEntry
        {
            Date = vm.Date,
            StartTime = new TimeOnly(snappedHour, 0),
            EndTime = new TimeOnly(Math.Min(snappedHour + 1, 23), snappedHour + 1 > 23 ? 59 : 0)
        };

        if (dialogService.ShowTimeEntryEditor(dataService.Data.Groups, null, newEntry, out var result))
        {
            result.ActivityId = result.ActivityId;
            dataService.Data.TimeEntries.Add(result);
            dataService.NotifyChanged();
            vm.Load(vm.Date);
        }
    }

    private void EntryBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true; // Prevent Canvas click
    }
}
