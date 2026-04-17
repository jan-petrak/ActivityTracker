using System.Windows;
using System.Windows.Controls;
using ActivityTracker.Models;

namespace ActivityTracker.Views.Dialogs;

public partial class PlannedEntryEditorDialog : Window
{
    public PlannedEntry Result { get; private set; } = new();

    private readonly CheckBox[] _dayCheckboxes;

    public PlannedEntryEditorDialog(List<ActivityGroup> groups, Guid? defaultActivityId, PlannedEntry? existing)
    {
        InitializeComponent();
        _dayCheckboxes = [ChkMon, ChkTue, ChkWed, ChkThu, ChkFri, ChkSat, ChkSun];

        var items = groups.SelectMany(g =>
            g.Activities.Select(a => new ActivityComboItem
            {
                Id = a.Id,
                Display = $"{g.Name} / {a.Name}"
            })).ToList();

        ActivityCombo.ItemsSource = items;

        if (existing != null)
        {
            Result = new PlannedEntry
            {
                Id = existing.Id,
                ActivityId = existing.ActivityId,
                Date = existing.Date,
                StartTime = existing.StartTime,
                EndTime = existing.EndTime,
                Recurrence = existing.Recurrence,
                Notes = existing.Notes
            };

            DateBox.Text = existing.Date.ToString("yyyy-MM-dd");
            StartTimeBox.Text = existing.StartTime.ToString("HH:mm");
            EndTimeBox.Text = existing.EndTime.ToString("HH:mm");
            NotesBox.Text = existing.Notes ?? string.Empty;
            ActivityCombo.SelectedValue = existing.ActivityId;

            if (existing.Recurrence != null)
            {
                RecurringCheck.IsChecked = true;
                RecurrenceTypeCombo.SelectedIndex = (int)existing.Recurrence.Type;
                IntervalBox.Text = existing.Recurrence.Interval.ToString();
                foreach (var day in existing.Recurrence.DaysOfWeek)
                {
                    var idx = ((int)day + 6) % 7; // Mon=0..Sun=6
                    _dayCheckboxes[idx].IsChecked = true;
                }
                if (existing.Recurrence.EndDate.HasValue)
                    EndDateBox.Text = existing.Recurrence.EndDate.Value.ToString("yyyy-MM-dd");
            }
        }
        else
        {
            DateBox.Text = DateTime.Today.ToString("yyyy-MM-dd");
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

        if (!DateTime.TryParseExact(DateBox.Text, "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var date))
        {
            MessageBox.Show("Please enter a valid date (YYYY-MM-DD).", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        if (RecurringCheck.IsChecked == true)
        {
            var daysOfWeek = new List<DayOfWeek>();
            DayOfWeek[] mapping = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];

            for (var i = 0; i < _dayCheckboxes.Length; i++)
            {
                if (_dayCheckboxes[i].IsChecked == true)
                    daysOfWeek.Add(mapping[i]);
            }

            _ = int.TryParse(IntervalBox.Text, out var interval);
            if (interval < 1) interval = 1;

            Result.Recurrence = new RecurrencePattern
            {
                Type = (RecurrenceType)RecurrenceTypeCombo.SelectedIndex,
                Interval = interval,
                DaysOfWeek = daysOfWeek,
                DayOfMonth = date.Day,
                StartDate = DateOnly.FromDateTime(date),
                EndDate = DateTime.TryParseExact(EndDateBox.Text, "yyyy-MM-dd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var parsedEnd)
                    ? DateOnly.FromDateTime(parsedEnd)
                    : null
            };
        }
        else
        {
            Result.Recurrence = null;
        }

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
