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
    private const double PixelsPerMinute = 1.0; // 60px per hour
    private const int SnapMinutes = 15;

    public static List<HourLabel> Hours { get; } = [.. Enumerable.Range(0, 24)
        .Select(h => new HourLabel
        {
            Label = new TimeOnly(h, 0).ToString("h tt"),
            Top = h * 60.0
        })];

    private bool _isDragging;
    private double _dragStartY;

    public DayView()
    {
        InitializeComponent();
        Loaded += DayView_Loaded;
    }

    private void DayView_Loaded(object sender, RoutedEventArgs e)
    {
        PositionEntryBlocks();
        EntriesControl.ItemContainerGenerator.StatusChanged += EntriesContainerGenerator_StatusChanged;
    }

    private void EntriesContainerGenerator_StatusChanged(object? sender, EventArgs e)
    {
        if (EntriesControl.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
        {
            PositionEntryBlocks();
        }
    }

    private void ScrollViewer_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ScrollViewer sv)
            sv.ScrollToVerticalOffset(7 * 60);
    }

    private void PositionEntryBlocks()
    {
        if (DataContext is not DayViewModel) return;

        var container = EntriesControl;
        for (var i = 0; i < container.Items.Count; i++)
        {
            if (container.ItemContainerGenerator.ContainerFromIndex(i) is ContentPresenter cp
                && container.Items[i] is CalendarEntryItem entry)
            {
                var top = entry.StartTime.Hour * 60 + entry.StartTime.Minute;
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

    private static int SnapToGrid(double y)
    {
        var totalMinutes = (int)(y / PixelsPerMinute);
        var snapped = (int)(Math.Round((double)totalMinutes / SnapMinutes) * SnapMinutes);
        return Math.Clamp(snapped, 0, 24 * 60);
    }

    private static TimeOnly MinutesToTime(int minutes)
    {
        minutes = Math.Clamp(minutes, 0, 23 * 60 + 59);
        return new TimeOnly(minutes / 60, minutes % 60);
    }

    // --- Drag to create ---

    private void DragCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        _dragStartY = e.GetPosition(DragCanvas).Y;
        DragCanvas.CaptureMouse();

        var snappedTop = SnapToGrid(_dragStartY) * PixelsPerMinute;
        Canvas.SetTop(DragPreview, snappedTop);
        DragPreview.Height = SnapMinutes * PixelsPerMinute;
        DragPreview.Width = Math.Max(DragCanvas.ActualWidth - 20, 100);
        UpdatePreviewText(snappedTop, snappedTop + SnapMinutes * PixelsPerMinute);
        DragPreview.Visibility = Visibility.Visible;

        e.Handled = true;
    }

    private void DragCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;

        var currentY = e.GetPosition(DragCanvas).Y;
        var startMin = SnapToGrid(_dragStartY);
        var endMin = SnapToGrid(currentY);

        if (startMin == endMin)
            endMin = startMin + SnapMinutes;

        var topMin = Math.Min(startMin, endMin);
        var bottomMin = Math.Max(startMin, endMin);

        Canvas.SetTop(DragPreview, topMin * PixelsPerMinute);
        DragPreview.Height = Math.Max((bottomMin - topMin) * PixelsPerMinute, SnapMinutes);
        DragPreview.Width = Math.Max(DragCanvas.ActualWidth - 20, 100);
        UpdatePreviewText(topMin * PixelsPerMinute, bottomMin * PixelsPerMinute);
    }

    private void DragCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        DragCanvas.ReleaseMouseCapture();
        DragPreview.Visibility = Visibility.Collapsed;

        if (DataContext is not DayViewModel vm) return;

        var currentY = e.GetPosition(DragCanvas).Y;
        var startMin = SnapToGrid(_dragStartY);
        var endMin = SnapToGrid(currentY);

        if (startMin == endMin)
            endMin = startMin + SnapMinutes;

        var topMin = Math.Min(startMin, endMin);
        var bottomMin = Math.Max(startMin, endMin);

        var startTime = MinutesToTime(topMin);
        var endTime = MinutesToTime(bottomMin);

        if (App.Services.GetService(typeof(IDataService)) is not IDataService dataService) return;
        if (App.Services.GetService(typeof(IDialogService)) is not IDialogService dialogService) return;

        // Create a PlannedEntry via drag
        var planned = new Models.PlannedEntry
        {
            Date = vm.Date,
            StartTime = startTime,
            EndTime = endTime
        };

        if (dialogService.ShowPlannedEntryEditor(dataService.Data.Groups, null, planned, out var result))
        {
            dataService.Data.PlannedEntries.Add(result);
            dataService.NotifyChanged();
            vm.Load(vm.Date);
        }
    }

    private void UpdatePreviewText(double topPx, double bottomPx)
    {
        var startMin = (int)(topPx / PixelsPerMinute);
        var endMin = (int)(bottomPx / PixelsPerMinute);
        var s = MinutesToTime(startMin);
        var en = MinutesToTime(endMin);
        DragPreviewText.Text = $"{s:HH:mm} – {en:HH:mm}  (drag to plan)";
    }

    private void EntryBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }
}
