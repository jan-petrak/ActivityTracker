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

    [RelayCommand]
    private void AddGroup()
    {
        if (_dialogService.ShowGroupEditor(null, out var group))
        {
            _dataService.Data.Groups.Add(group);
            Groups.Add(group);
            _dataService.NotifyChanged();
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
        _dataService.Data.PlannedEntries.RemoveAll(e => activityIds.Contains(e.ActivityId));
        _dataService.Data.Goals.RemoveAll(g => g.GroupId == SelectedGroup.Id);
        _dataService.Data.Groups.Remove(SelectedGroup);
        Groups.Remove(SelectedGroup);
        _dataService.NotifyChanged();
        SelectedGroup = null;
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
        }
    }

    [RelayCommand]
    private void EditActivity()
    {
        if (SelectedGroup == null || SelectedActivity == null) return;
        if (_dialogService.ShowActivityEditor(SelectedActivity, out var updated))
        {
            SelectedActivity.Name = updated.Name;
            _dataService.NotifyChanged();
        }
    }

    [RelayCommand]
    private void DeleteActivity()
    {
        if (SelectedGroup == null || SelectedActivity == null) return;
        _dataService.Data.PlannedEntries.RemoveAll(e => e.ActivityId == SelectedActivity.Id);
        SelectedGroup.Activities.Remove(SelectedActivity);
        _dataService.NotifyChanged();
        SelectedActivity = null;
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
