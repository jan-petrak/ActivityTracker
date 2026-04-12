using System.Windows;
using ActivityTracker.Models;

namespace ActivityTracker.Views.Dialogs;

public partial class TimeEntryEditorDialog : Window
{
    public TimeEntry Result { get; private set; } = new();

    public TimeEntryEditorDialog(List<ActivityGroup> groups, Guid? defaultActivityId, TimeEntry? existing)
    {
        InitializeComponent();

        // Populate activity combo
        var items = groups.SelectMany(g =>
            g.Activities.Select(a => new ActivityComboItem
            {
                Id = a.Id,
                Display = $"{g.Name} / {a.Name}"
            })).ToList();

        ActivityCombo.ItemsSource = items;

        if (existing != null)
        {
            Result = new TimeEntry
            {
                Id = existing.Id,
                ActivityId = existing.ActivityId,
                Date = existing.Date,
                StartTime = existing.StartTime,
                EndTime = existing.EndTime,
                Notes = existing.Notes
            };

            DatePick.SelectedDate = existing.Date.ToDateTime(TimeOnly.MinValue);
            StartTimeBox.Text = existing.StartTime.ToString("HH:mm");
            EndTimeBox.Text = existing.EndTime.ToString("HH:mm");
            NotesBox.Text = existing.Notes ?? string.Empty;
            ActivityCombo.SelectedValue = existing.ActivityId;
        }
        else
        {
            DatePick.SelectedDate = DateTime.Today;
            if (defaultActivityId.HasValue)
                ActivityCombo.SelectedValue = defaultActivityId.Value;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (ActivityCombo.SelectedValue is not Guid activityId)
        {
            MessageBox.Show("Please select an activity.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (DatePick.SelectedDate is not DateTime date)
        {
            MessageBox.Show("Please select a date.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TimeOnly.TryParse(StartTimeBox.Text, out var startTime) ||
            !TimeOnly.TryParse(EndTimeBox.Text, out var endTime))
        {
            MessageBox.Show("Please enter valid times (HH:mm).", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (endTime <= startTime)
        {
            MessageBox.Show("End time must be after start time.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result.ActivityId = activityId;
        Result.Date = DateOnly.FromDateTime(date);
        Result.StartTime = startTime;
        Result.EndTime = endTime;
        Result.Notes = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim();

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
