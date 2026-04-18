using System.Windows;
using System.Windows.Controls;
using ActivityTracker.Models;

namespace ActivityTracker.Views.Dialogs;

public partial class DayEventEditorDialog : Window
{
    public DayEvent Result { get; private set; } = new();

    private readonly CheckBox[] _dayCheckboxes;

    public DayEventEditorDialog(DayEvent? existing)
    {
        InitializeComponent();
        _dayCheckboxes = [ChkMon, ChkTue, ChkWed, ChkThu, ChkFri, ChkSat, ChkSun];

        if (existing != null)
        {
            Result = new DayEvent
            {
                Id = existing.Id,
                Title = existing.Title,
                Date = existing.Date,
                ReminderDaysBefore = existing.ReminderDaysBefore,
                Recurrence = existing.Recurrence,
                Notes = existing.Notes
            };

            TitleBox.Text = existing.Title;
            DateBox.Text = existing.Date.ToString("yyyy-MM-dd");
            ReminderDaysBox.Text = existing.ReminderDaysBefore.ToString();
            NotesBox.Text = existing.Notes ?? string.Empty;

            if (existing.Recurrence != null)
            {
                RecurringCheck.IsChecked = true;
                RecurrenceTypeCombo.SelectedIndex = (int)existing.Recurrence.Type;
                IntervalBox.Text = existing.Recurrence.Interval.ToString();
                foreach (var day in existing.Recurrence.DaysOfWeek)
                {
                    var idx = ((int)day + 6) % 7;
                    _dayCheckboxes[idx].IsChecked = true;
                }
                if (existing.Recurrence.EndDate.HasValue)
                    EndDateBox.Text = existing.Recurrence.EndDate.Value.ToString("yyyy-MM-dd");
            }
        }
        else
        {
            DateBox.Text = DateTime.Today.ToString("yyyy-MM-dd");
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var title = TitleBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title))
        {
            MessageBox.Show("Please enter a title.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!DateTime.TryParseExact(DateBox.Text, "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var date))
        {
            MessageBox.Show("Please enter a valid date (YYYY-MM-DD).", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(ReminderDaysBox.Text, out var reminderDays) || reminderDays < 0)
        {
            MessageBox.Show("Please enter a non-negative number of reminder days.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result.Title = title;
        Result.Date = DateOnly.FromDateTime(date);
        Result.ReminderDaysBefore = reminderDays;
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
