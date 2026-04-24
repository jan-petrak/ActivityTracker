using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ActivityTracker.Models;
using ActivityTracker.Services;
using ActivityTracker.Views.Dialogs;

namespace ActivityTracker.ViewModels;

public partial class ActivitiesViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly IDialogService _dialogService;
    private readonly IAuditLogService _auditLog;

    [ObservableProperty]
    private ObservableCollection<ActivityGroup> groups = [];

    [ObservableProperty]
    private ActivityGroup? selectedGroup;

    [ObservableProperty]
    private Activity? selectedActivity;

    public ActivitiesViewModel(IDataService dataService, IDialogService dialogService, IAuditLogService auditLog)
    {
        _dataService = dataService;
        _dialogService = dialogService;
        _auditLog = auditLog;
        Refresh();
    }

    private void Refresh()
    {
        Groups = new ObservableCollection<ActivityGroup>(_dataService.Data.Groups);
    }

    [RelayCommand]
    private void AddGroup()
    {
        if (_dialogService.ShowGroupEditor(null, out var group))
        {
            _dataService.Data.Groups.Add(group);
            Groups.Add(group);
            _dataService.NotifyChanged();
            _auditLog.Log("GroupCreated",
                $"Created group '{group.Name}'",
                new { group });
        }
    }

    [RelayCommand]
    private void EditGroup()
    {
        if (SelectedGroup == null) return;
        var before = new { name = SelectedGroup.Name, color = SelectedGroup.Color };
        if (_dialogService.ShowGroupEditor(SelectedGroup, out var updated))
        {
            SelectedGroup.Name = updated.Name;
            SelectedGroup.Color = updated.Color;
            _dataService.NotifyChanged();
            _auditLog.Log("GroupUpdated",
                $"Updated group '{SelectedGroup.Name}'",
                new { groupId = SelectedGroup.Id, before, after = new { name = updated.Name, color = updated.Color } });
            Refresh();
        }
    }

    [RelayCommand]
    private void DeleteGroup()
    {
        if (SelectedGroup == null) return;

        var group = SelectedGroup;
        var activityIds = group.Activities.Select(a => a.Id).ToHashSet();
        var cascadedEntries = _dataService.Data.PlannedEntries
            .Where(e => activityIds.Contains(e.ActivityId)).ToList();
        var cascadedGoals = _dataService.Data.Goals
            .Where(g => g.GroupId == group.Id).ToList();

        var message =
            $"Delete group '{group.Name}'?\n\n" +
            $"This will also remove {group.Activities.Count} activities, " +
            $"{cascadedEntries.Count} planned entries, and {cascadedGoals.Count} goals.";

        if (!MessageDialog.ShowConfirm("Confirm delete", message, "Delete", "Cancel")) return;

        _dataService.Data.PlannedEntries.RemoveAll(e => activityIds.Contains(e.ActivityId));
        _dataService.Data.Goals.RemoveAll(g => g.GroupId == group.Id);
        _dataService.Data.Groups.Remove(group);
        Groups.Remove(group);
        _dataService.NotifyChanged();
        SelectedGroup = null;

        _auditLog.Log("GroupDeleted",
            $"Deleted group '{group.Name}' " +
            $"(cascaded {group.Activities.Count} activities, {cascadedEntries.Count} planned entries, {cascadedGoals.Count} goals)",
            new { group, cascadedPlannedEntries = cascadedEntries, cascadedGoals });
    }

    [RelayCommand]
    private void AddActivity()
    {
        if (SelectedGroup == null) return;
        if (_dialogService.ShowActivityEditor(null, out var activity))
        {
            activity.GroupId = SelectedGroup.Id;
            SelectedGroup.Activities.Add(activity);
            _dataService.NotifyChanged();
            _auditLog.Log("ActivityCreated",
                $"Created activity '{activity.Name}' in group '{SelectedGroup.Name}'",
                new { activity, groupName = SelectedGroup.Name });
        }
    }

    [RelayCommand]
    private void EditActivity()
    {
        if (SelectedGroup == null || SelectedActivity == null) return;
        var before = new { name = SelectedActivity.Name };
        if (_dialogService.ShowActivityEditor(SelectedActivity, out var updated))
        {
            SelectedActivity.Name = updated.Name;
            _dataService.NotifyChanged();
            _auditLog.Log("ActivityUpdated",
                $"Updated activity '{SelectedActivity.Name}' in group '{SelectedGroup.Name}'",
                new { activityId = SelectedActivity.Id, before, after = new { name = updated.Name } });
        }
    }

    [RelayCommand]
    private void DeleteActivity()
    {
        if (SelectedGroup == null || SelectedActivity == null) return;

        var activity = SelectedActivity;
        var group = SelectedGroup;
        var cascadedEntries = _dataService.Data.PlannedEntries
            .Where(e => e.ActivityId == activity.Id).ToList();

        var message =
            $"Delete activity '{activity.Name}' from group '{group.Name}'?\n\n" +
            $"This will also remove {cascadedEntries.Count} planned entries.";

        if (!MessageDialog.ShowConfirm("Confirm delete", message, "Delete", "Cancel")) return;

        _dataService.Data.PlannedEntries.RemoveAll(e => e.ActivityId == activity.Id);
        group.Activities.Remove(activity);
        _dataService.NotifyChanged();
        SelectedActivity = null;

        _auditLog.Log("ActivityDeleted",
            $"Deleted activity '{activity.Name}' from group '{group.Name}' " +
            $"(cascaded {cascadedEntries.Count} planned entries)",
            new { activity, groupName = group.Name, cascadedPlannedEntries = cascadedEntries });
    }

    [RelayCommand]
    private void AddPlannedEntry()
    {
        if (SelectedActivity == null) return;
        if (_dialogService.ShowPlannedEntryEditor(_dataService.Data.Groups, SelectedActivity.Id, null, out var entry))
        {
            entry.ActivityId = SelectedActivity.Id;
            _dataService.Data.PlannedEntries.Add(entry);
            _dataService.NotifyChanged();
            _auditLog.Log("PlannedEntryCreated",
                $"Created planned entry for activity '{SelectedActivity.Name}' on {entry.Date:yyyy-MM-dd} {entry.StartTime:HH\\:mm}-{entry.EndTime:HH\\:mm}",
                new { plannedEntry = entry });
        }
    }
}
