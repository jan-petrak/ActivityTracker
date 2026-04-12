using System.Windows.Controls;
using ActivityTracker.Models;
using ActivityTracker.ViewModels;

namespace ActivityTracker.Views;

public partial class ActivitiesView : UserControl
{
    public ActivitiesView()
    {
        InitializeComponent();
    }

    private void TreeView_SelectedItemChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is not ActivitiesViewModel vm) return;

        switch (e.NewValue)
        {
            case ActivityGroup group:
                vm.SelectedGroup = group;
                vm.SelectedActivity = null;
                break;
            case Activity activity:
                vm.SelectedActivity = activity;
                var parentGroup = vm.Groups.FirstOrDefault(g => g.Activities.Contains(activity));
                if (parentGroup != null) vm.SelectedGroup = parentGroup;
                break;
        }
    }
}
