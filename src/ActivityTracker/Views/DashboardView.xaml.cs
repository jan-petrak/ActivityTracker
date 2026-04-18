using System.Windows;
using System.Windows.Controls;
using ActivityTracker.ViewModels;

namespace ActivityTracker.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    private void DayEventRow_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is Guid id
            && DataContext is DashboardViewModel vm)
        {
            vm.EditDayEvent(id);
        }
    }

    private void DayEventEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.Tag is Guid id
            && DataContext is DashboardViewModel vm)
        {
            vm.EditDayEvent(id);
        }
    }

    private void DayEventDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.Tag is Guid id
            && DataContext is DashboardViewModel vm)
        {
            var result = MessageBox.Show("Delete this whole-day event?", "Confirm delete",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
                vm.DeleteDayEvent(id);
        }
    }
}
