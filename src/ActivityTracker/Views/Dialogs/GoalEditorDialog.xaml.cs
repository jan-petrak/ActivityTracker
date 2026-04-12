using System.Windows;
using System.Windows.Controls;
using ActivityTracker.Models;

namespace ActivityTracker.Views.Dialogs;

public partial class GoalEditorDialog : Window
{
    private readonly List<ActivityGroup> _groups;
    public Goal Result { get; private set; } = new();

    public GoalEditorDialog(List<ActivityGroup> groups, Goal? existing)
    {
        InitializeComponent();
        _groups = groups;
        GroupCombo.ItemsSource = groups;

        if (existing != null)
        {
            Result = new Goal
            {
                Id = existing.Id,
                GroupId = existing.GroupId,
                ActivityId = existing.ActivityId,
                Period = existing.Period,
                TargetHours = existing.TargetHours,
                IsActive = existing.IsActive
            };

            GroupCombo.SelectedValue = existing.GroupId;
            PeriodCombo.SelectedIndex = (int)existing.Period;
            TargetBox.Text = existing.TargetHours.ToString("0.#");

            if (existing.ActivityId.HasValue)
                ActivityCombo.SelectedValue = existing.ActivityId.Value;
        }
        else if (groups.Count > 0)
        {
            GroupCombo.SelectedIndex = 0;
        }
    }

    private void GroupCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GroupCombo.SelectedItem is ActivityGroup group)
        {
            ActivityCombo.ItemsSource = group.Activities;
            ActivityCombo.SelectedIndex = -1;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (GroupCombo.SelectedValue is not Guid groupId)
        {
            MessageBox.Show("Please select a group.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!double.TryParse(TargetBox.Text, out var target) || target <= 0)
        {
            MessageBox.Show("Please enter a valid target (positive number).", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result.GroupId = groupId;
        Result.ActivityId = ActivityCombo.SelectedValue as Guid?;
        Result.Period = (GoalPeriod)PeriodCombo.SelectedIndex;
        Result.TargetHours = target;
        Result.IsActive = true;

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
