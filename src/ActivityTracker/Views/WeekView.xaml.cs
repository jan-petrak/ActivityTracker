using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ActivityTracker.Services;
using ActivityTracker.ViewModels;

namespace ActivityTracker.Views;

public partial class WeekView : UserControl
{
    private const double PixelsPerMinute = 1.0;
    private const int SnapMinutes = 15;

    private bool _isDragging;
    private double _dragStartY;
    private Canvas? _activeDragCanvas;
    private Border? _dragPreview;

    public WeekView()
    {
        InitializeComponent();
    }

    private void ScrollViewer_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ScrollViewer sv)
            sv.ScrollToVerticalOffset(7 * 60);
    }

    private void WeekEntries_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ItemsControl ic) return;

        ic.Dispatcher.BeginInvoke(new Action(() =>
        {
            for (var i = 0; i < ic.Items.Count; i++)
            {
                if (ic.ItemContainerGenerator.ContainerFromIndex(i) is ContentPresenter cp
                    && ic.Items[i] is CalendarEntryItem entry)
                {
                    var top = entry.StartTime.Hour * 60 + entry.StartTime.Minute;
                    var height = (entry.EndTime.ToTimeSpan() - entry.StartTime.ToTimeSpan()).TotalMinutes;
                    Canvas.SetTop(cp, top);
                    cp.Height = Math.Max(height, 15);
                }
            }
        }), System.Windows.Threading.DispatcherPriority.Loaded);
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

    private void WeekDragCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Canvas canvas) return;

        _isDragging = true;
        _activeDragCanvas = canvas;
        _dragStartY = e.GetPosition(canvas).Y;
        canvas.CaptureMouse();

        // Create preview inline on this canvas
        _dragPreview = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(0x33, 0xF5, 0x9E, 0x0B)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(4),
            IsHitTestVisible = false,
            Child = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)),
                FontSize = 9,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(4, 2, 4, 2)
            }
        };

        var snappedTop = SnapToGrid(_dragStartY) * PixelsPerMinute;
        Canvas.SetTop(_dragPreview, snappedTop);
        _dragPreview.Height = SnapMinutes * PixelsPerMinute;
        canvas.Children.Add(_dragPreview);
        UpdateWeekPreviewText(snappedTop, snappedTop + SnapMinutes * PixelsPerMinute);

        e.Handled = true;
    }

    private void WeekDragCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || _activeDragCanvas == null || _dragPreview == null) return;

        var currentY = e.GetPosition(_activeDragCanvas).Y;
        var startMin = SnapToGrid(_dragStartY);
        var endMin = SnapToGrid(currentY);

        if (startMin == endMin)
            endMin = startMin + SnapMinutes;

        var topMin = Math.Min(startMin, endMin);
        var bottomMin = Math.Max(startMin, endMin);

        Canvas.SetTop(_dragPreview, topMin * PixelsPerMinute);
        _dragPreview.Height = Math.Max((bottomMin - topMin) * PixelsPerMinute, SnapMinutes);
        UpdateWeekPreviewText(topMin * PixelsPerMinute, bottomMin * PixelsPerMinute);
    }

    private void WeekDragCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging || _activeDragCanvas == null) return;

        var canvas = _activeDragCanvas;
        _isDragging = false;
        canvas.ReleaseMouseCapture();

        if (_dragPreview != null)
        {
            canvas.Children.Remove(_dragPreview);
            _dragPreview = null;
        }

        // Determine which date this column represents
        if (canvas.Tag is not DateOnly date) return;
        if (DataContext is not WeekViewModel vm) return;

        var currentY = e.GetPosition(canvas).Y;
        var startMin = SnapToGrid(_dragStartY);
        var endMin = SnapToGrid(currentY);

        if (startMin == endMin)
            endMin = startMin + SnapMinutes;

        var topMin = Math.Min(startMin, endMin);
        var bottomMin = Math.Max(startMin, endMin);

        var startTime = MinutesToTime(topMin);
        var endTime = MinutesToTime(bottomMin);

        var dataService = App.Services.GetService(typeof(IDataService)) as IDataService;
        var dialogService = App.Services.GetService(typeof(IDialogService)) as IDialogService;
        if (dataService == null || dialogService == null) return;

        var planned = new Models.PlannedEntry
        {
            Date = date,
            StartTime = startTime,
            EndTime = endTime
        };

        if (dialogService.ShowPlannedEntryEditor(dataService.Data.Groups, null, planned, out var result))
        {
            dataService.Data.PlannedEntries.Add(result);
            dataService.NotifyChanged();
            vm.Load(vm.WeekStart);
        }

        _activeDragCanvas = null;
    }

    private void UpdateWeekPreviewText(double topPx, double bottomPx)
    {
        if (_dragPreview?.Child is not TextBlock tb) return;
        var startMin = (int)(topPx / PixelsPerMinute);
        var endMin = (int)(bottomPx / PixelsPerMinute);
        var s = MinutesToTime(startMin);
        var en = MinutesToTime(endMin);
        tb.Text = $"{s:HH:mm}–{en:HH:mm}";
    }
}
