using System.Windows;
using ActivityTracker.Models;
using ActivityTracker.Views.Dialogs;

namespace ActivityTracker.Services;

public class DialogService : IDialogService
{
    public bool ShowGroupEditor(ActivityGroup? existing, out ActivityGroup result)
    {
        var dialog = new GroupEditorDialog(existing)
        {
            Owner = Application.Current.MainWindow
        };
        if (dialog.ShowDialog() == true)
        {
            result = dialog.Result;
            return true;
        }
        result = existing ?? new ActivityGroup();
        return false;
    }

    public bool ShowActivityEditor(Activity? existing, out Activity result)
    {
        var dialog = new ActivityEditorDialog(existing)
        {
            Owner = Application.Current.MainWindow
        };
        if (dialog.ShowDialog() == true)
        {
            result = dialog.Result;
            return true;
        }
        result = existing ?? new Activity();
        return false;
    }

    public bool ShowPlannedEntryEditor(List<ActivityGroup> groups, Guid? defaultActivityId, PlannedEntry? existing, out PlannedEntry result)
    {
        var dialog = new PlannedEntryEditorDialog(groups, defaultActivityId, existing)
        {
            Owner = Application.Current.MainWindow
        };
        if (dialog.ShowDialog() == true)
        {
            result = dialog.Result;
            return true;
        }
        result = existing ?? new PlannedEntry();
        return false;
    }

    public bool ShowGoalEditor(List<ActivityGroup> groups, Goal? existing, out Goal result)
    {
        var dialog = new GoalEditorDialog(groups, existing)
        {
            Owner = Application.Current.MainWindow
        };
        if (dialog.ShowDialog() == true)
        {
            result = dialog.Result;
            return true;
        }
        result = existing ?? new Goal();
        return false;
    }
}
