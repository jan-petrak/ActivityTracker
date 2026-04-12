using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ActivityTracker.Models;
using ActivityTracker.Services;

namespace ActivityTracker.ViewModels;

public partial class ActivitiesViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<ActivityGroup> groups = [];

    [ObservableProperty]
    private ActivityGroup? selectedGroup;

    [ObservableProperty]
    private Activity? selectedActivity;

    [ObservableProperty]
    private ObservableCollection<TimeEntry> selectedActivityEntries = [];

    public ActivitiesViewModel(IDataService dataService, IDialogService dialogService)
    {
        _dataService = dataService;
        _dialogService = dialogService;
        Refresh();
    }

    private void Refresh()
    {
        Groups = new ObservableCollection<ActivityGroup>(_dataService.Data.Groups);
    }

    partial void OnSelectedActivityChanged(Activity? value)
    {
        if (value == null)
        {
            SelectedActivityEntries = [];
            return;
        }
        var entries = _dataService.Data.TimeEntries
            .Where(e => e.ActivityId == value.Id)
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.StartTime);
        SelectedActivityEntries = new ObservableCollection<TimeEntry>(entries);
    }

    [RelayCommand]
    private void AddGroup()
    {
        if (_dialogService.ShowGroupEditor(null, out var group))
        {
            _dataService.Data.Groups.Add(group);
            _dataService.NotifyChanged();
            Refresh();
        }
    }

    [RelayCommand]
    private void EditGroup()
    {
        if (SelectedGroup == null) return;
        if (_dialogService.ShowGroupEditor(SelectedGroup, out var updated))
        {
            SelectedGroup.Name = updated.Name;
            SelectedGroup.Color = updated.Color;
            _dataService.NotifyChanged();
            Refresh();
        }
    }

    [RelayCommand]
    private void DeleteGroup()
    {
        if (SelectedGroup == null) return;
        var activityIds = SelectedGroup.Activities.Select(a => a.Id).ToHashSet();
        _dataService.Data.TimeEntries.RemoveAll(e => activityIds.Contains(e.ActivityId));
        _dataService.Data.PlannedEntries.RemoveAll(e => activityIds.Contains(e.ActivityId));
        _dataService.Data.Goals.RemoveAll(g => g.GroupId == SelectedGroup.Id);
        _dataService.Data.Groups.Remove(SelectedGroup);
        _dataService.NotifyChanged();
        SelectedGroup = null;
        Refresh();
    }

    [RelayCommand]
    private void AddActivity()
    {
        if (SelectedGroup == null) return;
        if (_dialogService.ShowActivityEditor(_dataService.Data.Groups, SelectedGroup.Id, null, out var activity))
        {
            activity.GroupId = SelectedGroup.Id;
            SelectedGroup.Activities.Add(activity);
            _dataService.NotifyChanged();
            Refresh();
        }
    }

    [RelayCommand]
    private void EditActivity()
    {
        if (SelectedGroup == null || SelectedActivity == null) return;
        if (_dialogService.ShowActivityEditor(_dataService.Data.Groups, SelectedGroup.Id, SelectedActivity, out var updated))
        {
            SelectedActivity.Name = updated.Name;
            _dataService.NotifyChanged();
            Refresh();
        }
    }

    [RelayCommand]
    private void DeleteActivity()
    {
        if (SelectedGroup == null || SelectedActivity == null) return;
        _dataService.Data.TimeEntries.RemoveAll(e => e.ActivityId == SelectedActivity.Id);
        _dataService.Data.PlannedEntries.RemoveAll(e => e.ActivityId == SelectedActivity.Id);
        SelectedGroup.Activities.Remove(SelectedActivity);
        _dataService.NotifyChanged();
        SelectedActivity = null;
        Refresh();
    }

    [RelayCommand]
    private void AddTimeEntry()
    {
        if (SelectedActivity == null) return;
        if (_dialogService.ShowTimeEntryEditor(_dataService.Data.Groups, SelectedActivity.Id, null, out var entry))
        {
            entry.ActivityId = SelectedActivity.Id;
            _dataService.Data.TimeEntries.Add(entry);
            _dataService.NotifyChanged();
            OnSelectedActivityChanged(SelectedActivity);
        }
    }

    [RelayCommand]
    private void EditTimeEntry(TimeEntry entry)
    {
        if (_dialogService.ShowTimeEntryEditor(_dataService.Data.Groups, entry.ActivityId, entry, out var updated))
        {
            entry.Date = updated.Date;
            entry.StartTime = updated.StartTime;
            entry.EndTime = updated.EndTime;
            entry.Notes = updated.Notes;
            _dataService.NotifyChanged();
            OnSelectedActivityChanged(SelectedActivity);
        }
    }

    [RelayCommand]
    private void DeleteTimeEntry(TimeEntry entry)
    {
        _dataService.Data.TimeEntries.Remove(entry);
        _dataService.NotifyChanged();
        OnSelectedActivityChanged(SelectedActivity);
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
        }
    }
}
