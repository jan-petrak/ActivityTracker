using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ActivityTracker.Services;
using ActivityTracker.ViewModels;
using ActivityTracker.Views.Dialogs;

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

    private enum EntryDragMode { None, Move, ResizeTop, ResizeBottom }

    private const double EntryEdgeZone = 6;
    private const double EntryDragThreshold = 4;

    private EntryDragMode _entryDragMode = EntryDragMode.None;
    private bool _entryDragActive;
    private Guid _entryDragId;
    private Point _entryDragStartPoint;
    private double _entryOriginalTop;
    private double _entryOriginalHeight;
    private ContentPresenter? _entryDragPresenter;
    private Border? _entryDragBorder;
    private KeyEventHandler? _entryDragKeyHandler;

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
                var startMin = (int)entry.Start.TimeOfDay.TotalMinutes;
                int endMin;
                if (entry.IsContinuation)
                    endMin = (int)entry.End.TimeOfDay.TotalMinutes;
                else
                    endMin = DateOnly.FromDateTime(entry.End) > DateOnly.FromDateTime(entry.Start)
                        ? 24 * 60
                        : (int)entry.End.TimeOfDay.TotalMinutes;
                Canvas.SetTop(cp, startMin);
                cp.Height = Math.Max(endMin - startMin, 20);
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

    private static DateTime MinutesToDateTime(DateOnly date, int minutes)
    {
        if (minutes >= 24 * 60)
            return date.AddDays(1).ToDateTime(new TimeOnly(0, 0));
        return date.ToDateTime(new TimeOnly(Math.Max(minutes, 0) / 60, Math.Max(minutes, 0) % 60));
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

        var start = MinutesToDateTime(vm.Date, topMin);
        var end = MinutesToDateTime(vm.Date, bottomMin);

        if (App.Services.GetService(typeof(IDataService)) is not IDataService dataService) return;
        if (App.Services.GetService(typeof(IDialogService)) is not IDialogService dialogService) return;

        var planned = new Models.PlannedEntry { Start = start, End = end };

        if (dialogService.ShowPlannedEntryEditor(dataService.Data.Groups, null, planned, out var result))
        {
            dataService.Data.PlannedEntries.Add(result);
            dataService.NotifyChanged();
            if (App.Services.GetService(typeof(IAuditLogService)) is IAuditLogService auditLog)
            {
                auditLog.Log("PlannedEntryCreated",
                    $"Created planned entry on {result.Date:yyyy-MM-dd} {result.Start:HH\\:mm}-{result.End:HH\\:mm} (via day-view drag)",
                    new { plannedEntry = result });
            }
            vm.Load(vm.Date);
        }
    }

    private void UpdatePreviewText(double topPx, double bottomPx)
    {
        var startMin = (int)(topPx / PixelsPerMinute);
        var endMin = (int)(bottomPx / PixelsPerMinute);
        var s = new TimeOnly(startMin / 60, startMin % 60);
        var en = endMin >= 24 * 60 ? new TimeOnly(0, 0) : new TimeOnly(endMin / 60, endMin % 60);
        var suffix = endMin >= 24 * 60 ? " +1d" : string.Empty;
        DragPreviewText.Text = $"{s:HH:mm} – {en:HH:mm}{suffix}  (drag to plan)";
    }

    private void EntryBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border || border.Tag is not Guid id)
        {
            e.Handled = true;
            return;
        }
        if (VisualTreeHelper.GetParent(border) is not ContentPresenter presenter)
        {
            e.Handled = true;
            return;
        }

        var point = e.GetPosition(border);
        var mode = point.Y <= EntryEdgeZone ? EntryDragMode.ResizeTop
            : point.Y >= border.ActualHeight - EntryEdgeZone ? EntryDragMode.ResizeBottom
            : EntryDragMode.Move;

        _entryDragMode = mode;
        _entryDragActive = false;
        _entryDragId = id;
        _entryDragStartPoint = e.GetPosition(DragCanvas);
        _entryDragPresenter = presenter;
        _entryDragBorder = border;
        _entryOriginalTop = Canvas.GetTop(presenter);
        _entryOriginalHeight = double.IsNaN(presenter.Height) ? presenter.ActualHeight : presenter.Height;

        border.CaptureMouse();
        e.Handled = true;
    }

    private void EntryBlock_MouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not Border border) return;

        if (!border.IsMouseCaptured || _entryDragMode == EntryDragMode.None)
        {
            var pt = e.GetPosition(border);
            border.Cursor = (pt.Y <= EntryEdgeZone || pt.Y >= border.ActualHeight - EntryEdgeZone)
                ? Cursors.SizeNS
                : Cursors.SizeAll;
            return;
        }

        var current = e.GetPosition(DragCanvas);
        var dy = current.Y - _entryDragStartPoint.Y;

        if (!_entryDragActive)
        {
            if (Math.Abs(dy) < EntryDragThreshold) return;
            _entryDragActive = true;
            HookEntryDragEscape();
        }

        var snappedDelta = SnapDelta(dy);
        var origStart = (int)Math.Round(_entryOriginalTop);
        var origEnd = (int)Math.Round(_entryOriginalTop + _entryOriginalHeight);
        int newStart = origStart, newEnd = origEnd;

        switch (_entryDragMode)
        {
            case EntryDragMode.Move:
                newStart = origStart + snappedDelta;
                newEnd = origEnd + snappedDelta;
                if (newStart < 0) { newEnd -= newStart; newStart = 0; }
                if (newEnd > 24 * 60) { newStart -= (newEnd - 24 * 60); newEnd = 24 * 60; }
                break;
            case EntryDragMode.ResizeTop:
                newStart = Math.Clamp(origStart + snappedDelta, 0, origEnd - SnapMinutes);
                newEnd = origEnd;
                break;
            case EntryDragMode.ResizeBottom:
                newStart = origStart;
                newEnd = Math.Clamp(origEnd + snappedDelta, origStart + SnapMinutes, 24 * 60);
                break;
        }

        if (_entryDragPresenter != null)
        {
            Canvas.SetTop(_entryDragPresenter, newStart);
            _entryDragPresenter.Height = Math.Max(newEnd - newStart, 20);
        }
    }

    private void EntryBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border || border.Tag is not Guid id)
        {
            ResetEntryDragState();
            return;
        }

        if (border.IsMouseCaptured) border.ReleaseMouseCapture();

        var wasActive = _entryDragActive;
        var presenter = _entryDragPresenter;
        var origTop = _entryOriginalTop;
        var origHeight = _entryOriginalHeight;

        ResetEntryDragState();

        if (!wasActive)
        {
            if (DataContext is DayViewModel vmClick) vmClick.EditEntry(id);
            e.Handled = true;
            return;
        }

        if (presenter == null || DataContext is not DayViewModel vm)
        {
            e.Handled = true;
            return;
        }

        var newTop = Canvas.GetTop(presenter);
        var newHeight = presenter.Height;
        var newStart = MinutesToDateTime(vm.Date, (int)Math.Round(newTop));
        var newEnd = MinutesToDateTime(vm.Date, (int)Math.Round(newTop + newHeight));

        if (!vm.Reschedule(id, newStart, newEnd))
        {
            Canvas.SetTop(presenter, origTop);
            presenter.Height = Math.Max(origHeight, 20);
        }
        e.Handled = true;
    }

    private static int SnapDelta(double dy)
    {
        var minutes = dy / PixelsPerMinute;
        return (int)Math.Round(minutes / SnapMinutes) * SnapMinutes;
    }

    private void ResetEntryDragState()
    {
        UnhookEntryDragEscape();
        _entryDragMode = EntryDragMode.None;
        _entryDragActive = false;
        _entryDragPresenter = null;
        _entryDragBorder = null;
    }

    private void HookEntryDragEscape()
    {
        var window = Window.GetWindow(this);
        if (window == null) return;
        _entryDragKeyHandler = (_, ev) =>
        {
            if (ev.Key == Key.Escape) CancelEntryDrag();
        };
        window.PreviewKeyDown += _entryDragKeyHandler;
    }

    private void UnhookEntryDragEscape()
    {
        if (_entryDragKeyHandler == null) return;
        var window = Window.GetWindow(this);
        if (window != null) window.PreviewKeyDown -= _entryDragKeyHandler;
        _entryDragKeyHandler = null;
    }

    private void CancelEntryDrag()
    {
        if (_entryDragBorder?.IsMouseCaptured == true)
            _entryDragBorder.ReleaseMouseCapture();
        if (_entryDragPresenter != null)
        {
            Canvas.SetTop(_entryDragPresenter, _entryOriginalTop);
            _entryDragPresenter.Height = Math.Max(_entryOriginalHeight, 20);
        }
        ResetEntryDragState();
    }

    private void EntryBlock_Edit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.Tag is Guid id
            && DataContext is DayViewModel vm)
        {
            vm.EditEntry(id);
        }
    }

    private void EntryBlock_Delete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.Tag is Guid id
            && DataContext is DayViewModel vm)
        {
            if (MessageDialog.ShowConfirm("Confirm delete", "Delete this planned entry?"))
                vm.DeleteEntry(id);
        }
    }

    private void DateHeader_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
    }

    private void AddDayEvent_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is DayViewModel vm)
            vm.AddDayEvent(vm.Date);
    }

    private void DayEventPill_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is Guid id
            && DataContext is DayViewModel vm)
        {
            vm.EditDayEvent(id);
            e.Handled = true;
        }
    }

    private void DayEventPill_Edit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.Tag is Guid id
            && DataContext is DayViewModel vm)
        {
            vm.EditDayEvent(id);
        }
    }

    private void DayEventPill_Delete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.Tag is Guid id
            && DataContext is DayViewModel vm)
        {
            if (MessageDialog.ShowConfirm("Confirm delete", "Delete this whole-day event?"))
                vm.DeleteDayEvent(id);
        }
    }
}
