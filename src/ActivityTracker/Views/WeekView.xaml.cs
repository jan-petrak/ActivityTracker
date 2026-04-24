using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ActivityTracker.Services;
using ActivityTracker.ViewModels;
using ActivityTracker.Views.Dialogs;

namespace ActivityTracker.Views;

public partial class WeekView : UserControl
{
    private const double PixelsPerMinute = 1.0;
    private const int SnapMinutes = 15;

    private bool _isDragging;
    private double _dragStartY;
    private Canvas? _activeDragCanvas;
    private Border? _dragPreview;

    private enum EntryDragMode { None, Move, ResizeTop, ResizeBottom }

    private const double EntryEdgeZone = 6;
    private const double EntryDragThreshold = 4;

    private EntryDragMode _entryDragMode = EntryDragMode.None;
    private bool _entryDragActive;
    private Guid _entryDragId;
    private DateOnly _entryDragSourceDate;
    private Point _entryDragStartRoot;
    private double _entryOriginalStartMin;
    private double _entryOriginalEndMin;
    private int _entryCurrentStartMin;
    private int _entryCurrentEndMin;
    private DateOnly _entryCurrentDate;
    private ContentPresenter? _entryDragPresenter;
    private Border? _entryDragBorder;
    private KeyEventHandler? _entryDragKeyHandler;

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

        PositionWeekEntries(ic);
        ic.ItemContainerGenerator.StatusChanged += (_, _) =>
        {
            if (ic.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                PositionWeekEntries(ic);
        };
    }

    private static void PositionWeekEntries(ItemsControl ic)
    {
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
        Canvas.SetLeft(_dragPreview, 0);
        _dragPreview.Height = SnapMinutes * PixelsPerMinute;
        _dragPreview.Width = canvas.ActualWidth;
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
        _dragPreview.Width = _activeDragCanvas.ActualWidth;
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

        if (App.Services.GetService(typeof(IDataService)) is not IDataService dataService) return;
        if (App.Services.GetService(typeof(IDialogService)) is not IDialogService dialogService) return;

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
            if (App.Services.GetService(typeof(IAuditLogService)) is IAuditLogService auditLog)
            {
                auditLog.Log("PlannedEntryCreated",
                    $"Created planned entry on {result.Date:yyyy-MM-dd} {result.StartTime:HH\\:mm}-{result.EndTime:HH\\:mm} (via week-view drag)",
                    new { plannedEntry = result });
            }
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

    private void DayHeader_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
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
        var columnBorder = FindDateAncestor(border);
        if (columnBorder?.Tag is not DateOnly sourceDate)
        {
            e.Handled = true;
            return;
        }

        var pointInBorder = e.GetPosition(border);
        var mode = pointInBorder.Y <= EntryEdgeZone ? EntryDragMode.ResizeTop
            : pointInBorder.Y >= border.ActualHeight - EntryEdgeZone ? EntryDragMode.ResizeBottom
            : EntryDragMode.Move;

        _entryDragMode = mode;
        _entryDragActive = false;
        _entryDragId = id;
        _entryDragSourceDate = sourceDate;
        _entryDragPresenter = presenter;
        _entryDragBorder = border;
        _entryOriginalStartMin = Canvas.GetTop(presenter);
        var height = double.IsNaN(presenter.Height) ? presenter.ActualHeight : presenter.Height;
        _entryOriginalEndMin = _entryOriginalStartMin + height;
        _entryCurrentStartMin = (int)Math.Round(_entryOriginalStartMin);
        _entryCurrentEndMin = (int)Math.Round(_entryOriginalEndMin);
        _entryCurrentDate = sourceDate;
        _entryDragStartRoot = e.GetPosition(WeekRoot);

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

        var current = e.GetPosition(WeekRoot);
        var dx = current.X - _entryDragStartRoot.X;
        var dy = current.Y - _entryDragStartRoot.Y;

        if (!_entryDragActive)
        {
            if (Math.Abs(dx) < EntryDragThreshold && Math.Abs(dy) < EntryDragThreshold) return;
            _entryDragActive = true;
            if (_entryDragMode == EntryDragMode.Move) DragGhost.Visibility = Visibility.Visible;
            HookEntryDragEscape();
        }

        var snappedDelta = SnapDelta(dy);
        var origStart = (int)Math.Round(_entryOriginalStartMin);
        var origEnd = (int)Math.Round(_entryOriginalEndMin);
        int newStart, newEnd;

        switch (_entryDragMode)
        {
            case EntryDragMode.ResizeTop:
                newStart = Math.Clamp(origStart + snappedDelta, 0, origEnd - SnapMinutes);
                newEnd = origEnd;
                _entryCurrentStartMin = newStart;
                _entryCurrentEndMin = newEnd;
                if (_entryDragPresenter != null)
                {
                    Canvas.SetTop(_entryDragPresenter, newStart);
                    _entryDragPresenter.Height = Math.Max(newEnd - newStart, 15);
                }
                break;

            case EntryDragMode.ResizeBottom:
                newStart = origStart;
                newEnd = Math.Clamp(origEnd + snappedDelta, origStart + SnapMinutes, 24 * 60);
                _entryCurrentStartMin = newStart;
                _entryCurrentEndMin = newEnd;
                if (_entryDragPresenter != null)
                    _entryDragPresenter.Height = Math.Max(newEnd - newStart, 15);
                break;

            case EntryDragMode.Move:
                newStart = origStart + snappedDelta;
                newEnd = origEnd + snappedDelta;
                if (newStart < 0) { newEnd -= newStart; newStart = 0; }
                if (newEnd > 24 * 60) { newStart -= (newEnd - 24 * 60); newEnd = 24 * 60; }

                var targetCol = HitTestColumn(current);
                _entryCurrentDate = targetCol?.Tag is DateOnly td ? td : _entryDragSourceDate;
                _entryCurrentStartMin = newStart;
                _entryCurrentEndMin = newEnd;

                if (_entryDragPresenter != null)
                {
                    Canvas.SetTop(_entryDragPresenter, newStart);
                    _entryDragPresenter.Height = Math.Max(newEnd - newStart, 15);
                }

                Canvas.SetLeft(DragGhost, current.X + 12);
                Canvas.SetTop(DragGhost, current.Y + 12);
                DragGhostText.Text = _entryCurrentDate != _entryDragSourceDate
                    ? $"{_entryCurrentDate:ddd MMM d}  ·  {MinutesToTime(newStart):HH\\:mm}–{MinutesToTime(newEnd):HH\\:mm}"
                    : $"{MinutesToTime(newStart):HH\\:mm}–{MinutesToTime(newEnd):HH\\:mm}";
                break;
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
        var mode = _entryDragMode;
        var presenter = _entryDragPresenter;
        var origStart = _entryOriginalStartMin;
        var origEnd = _entryOriginalEndMin;
        var sourceDate = _entryDragSourceDate;
        var currentStartMin = _entryCurrentStartMin;
        var currentEndMin = _entryCurrentEndMin;
        var currentDate = _entryCurrentDate;

        DragGhost.Visibility = Visibility.Collapsed;
        ResetEntryDragState();

        if (!wasActive)
        {
            if (DataContext is WeekViewModel vmClick) vmClick.EditEntry(id);
            e.Handled = true;
            return;
        }

        if (presenter == null || DataContext is not WeekViewModel vm)
        {
            e.Handled = true;
            return;
        }

        var newStart = MinutesToTime(currentStartMin);
        var newEnd = MinutesToTime(currentEndMin);
        var targetDate = mode == EntryDragMode.Move ? currentDate : sourceDate;

        var applied = vm.Reschedule(id, sourceDate, targetDate, newStart, newEnd);
        if (!applied && (mode == EntryDragMode.ResizeTop || mode == EntryDragMode.ResizeBottom))
        {
            Canvas.SetTop(presenter, origStart);
            presenter.Height = Math.Max(origEnd - origStart, 15);
        }
        e.Handled = true;
    }

    private static int SnapDelta(double dy)
    {
        var minutes = dy / PixelsPerMinute;
        return (int)Math.Round(minutes / SnapMinutes) * SnapMinutes;
    }

    private Border? HitTestColumn(Point rootPoint)
    {
        DependencyObject? hit = null;
        VisualTreeHelper.HitTest(
            WeekRoot, null,
            r => { hit = r.VisualHit; return HitTestResultBehavior.Stop; },
            new PointHitTestParameters(rootPoint));
        while (hit != null)
        {
            if (hit is Border b && b.Tag is DateOnly) return b;
            hit = VisualTreeHelper.GetParent(hit);
        }
        return null;
    }

    private static Border? FindDateAncestor(DependencyObject start)
    {
        var node = VisualTreeHelper.GetParent(start);
        while (node != null)
        {
            if (node is Border b && b.Tag is DateOnly) return b;
            node = VisualTreeHelper.GetParent(node);
        }
        return null;
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
            Canvas.SetTop(_entryDragPresenter, _entryOriginalStartMin);
            _entryDragPresenter.Height = Math.Max(_entryOriginalEndMin - _entryOriginalStartMin, 15);
        }
        DragGhost.Visibility = Visibility.Collapsed;
        ResetEntryDragState();
    }

    private void EntryBlock_Edit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.Tag is Guid id
            && DataContext is WeekViewModel vm)
        {
            vm.EditEntry(id);
        }
    }

    private void EntryBlock_Delete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.Tag is Guid id
            && DataContext is WeekViewModel vm)
        {
            if (MessageDialog.ShowConfirm("Confirm delete", "Delete this planned entry?"))
                vm.DeleteEntry(id);
        }
    }

    private void AddDayEvent_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.Tag is DateOnly date
            && DataContext is WeekViewModel vm)
        {
            vm.AddDayEvent(date);
        }
    }

    private void DayEventPill_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is Guid id
            && DataContext is WeekViewModel vm)
        {
            vm.EditDayEvent(id);
            e.Handled = true;
        }
    }

    private void DayEventPill_Edit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.Tag is Guid id
            && DataContext is WeekViewModel vm)
        {
            vm.EditDayEvent(id);
        }
    }

    private void DayEventPill_Delete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.Tag is Guid id
            && DataContext is WeekViewModel vm)
        {
            if (MessageDialog.ShowConfirm("Confirm delete", "Delete this whole-day event?"))
                vm.DeleteDayEvent(id);
        }
    }
}
