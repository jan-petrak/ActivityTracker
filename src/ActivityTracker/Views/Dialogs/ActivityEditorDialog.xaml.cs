using System.Windows;
using ActivityTracker.Models;

namespace ActivityTracker.Views.Dialogs;

public partial class ActivityEditorDialog : Window
{
    public Activity Result { get; private set; } = new();

    public ActivityEditorDialog(Activity? existing)
    {
        InitializeComponent();

        if (existing != null)
        {
            NameBox.Text = existing.Name;
            Result = new Activity
            {
                Id = existing.Id,
                GroupId = existing.GroupId,
                Name = existing.Name,
                SortOrder = existing.SortOrder
            };
        }

        NameBox.Focus();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            MessageBox.Show("Please enter an activity name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result.Name = NameBox.Text.Trim();
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
