using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ActivityTracker.ViewModels;
using ActivityTracker.Views.Dialogs;

namespace ActivityTracker.Views;

public partial class MonthView : UserControl
{
    public MonthView()
    {
        InitializeComponent();
    }

    private void DayCell_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
    }

    private void AddDayEvent_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.Tag is DateOnly date
            && DataContext is MonthViewModel vm)
        {
            vm.AddDayEvent(date);
        }
    }

    private void DayEventIcon_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe) return;
        if (fe.DataContext is not MonthDayCell cell) return;
        if (DataContext is not MonthViewModel vm) return;
        if (cell.DayEvents.Count == 0) return;

        if (cell.DayEvents.Count == 1)
        {
            vm.EditDayEvent(cell.DayEvents[0].SourceId);
        }
        else
        {
            var menu = new ContextMenu { PlacementTarget = fe };
            foreach (var occ in cell.DayEvents)
            {
                var item = new MenuItem { Header = occ.Title };
                var sid = occ.SourceId;
                item.Click += (_, _) => vm.EditDayEvent(sid);
                menu.Items.Add(item);
            }
            menu.IsOpen = true;
        }

        e.Handled = true;
    }

    private void DayEventIcon_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe) return;
        if (fe.DataContext is not MonthDayCell cell) return;
        if (DataContext is not MonthViewModel vm) return;
        if (cell.DayEvents.Count == 0) return;

        var menu = new ContextMenu { PlacementTarget = fe };

        if (cell.DayEvents.Count == 1)
        {
            var occ = cell.DayEvents[0];
            var edit = new MenuItem { Header = "Edit" };
            edit.Click += (_, _) => vm.EditDayEvent(occ.SourceId);
            menu.Items.Add(edit);

            var del = new MenuItem { Header = "Delete" };
            del.Click += (_, _) => ConfirmDelete(vm, occ.SourceId);
            menu.Items.Add(del);
        }
        else
        {
            foreach (var occ in cell.DayEvents)
            {
                var header = new MenuItem { Header = occ.Title };
                var edit = new MenuItem { Header = "Edit" };
                var sid = occ.SourceId;
                edit.Click += (_, _) => vm.EditDayEvent(sid);
                header.Items.Add(edit);

                var del = new MenuItem { Header = "Delete" };
                del.Click += (_, _) => ConfirmDelete(vm, sid);
                header.Items.Add(del);

                menu.Items.Add(header);
            }
        }

        menu.IsOpen = true;
        e.Handled = true;
    }

    private static void ConfirmDelete(MonthViewModel vm, System.Guid id)
    {
        if (MessageDialog.ShowConfirm("Confirm delete", "Delete this whole-day event?"))
            vm.DeleteDayEvent(id);
    }

    private void EntryRow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is Guid id
            && DataContext is MonthViewModel vm)
        {
            vm.EditEntry(id);
            e.Handled = true;
        }
    }

    private void EntryRow_Edit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.Tag is Guid id
            && DataContext is MonthViewModel vm)
        {
            vm.EditEntry(id);
        }
    }

    private void EntryRow_Delete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.Tag is Guid id
            && DataContext is MonthViewModel vm)
        {
            if (MessageDialog.ShowConfirm("Confirm delete", "Delete this planned entry?"))
                vm.DeleteEntry(id);
        }
    }
}
