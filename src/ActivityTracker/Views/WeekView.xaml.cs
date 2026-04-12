using System.Windows;
using System.Windows.Controls;
using ActivityTracker.Services;

namespace ActivityTracker.Views;

public partial class WeekView : UserControl
{
    public WeekView()
    {
        InitializeComponent();
    }

    private void ScrollViewer_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ScrollViewer sv)
        {
            sv.ScrollToVerticalOffset(7 * 60);
        }
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
}
