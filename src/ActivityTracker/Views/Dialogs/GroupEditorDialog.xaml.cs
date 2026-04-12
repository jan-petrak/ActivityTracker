using System.Windows;
using System.Windows.Controls;
using ActivityTracker.Models;

namespace ActivityTracker.Views.Dialogs;

public partial class GroupEditorDialog : Window
{
    public ActivityGroup Result { get; private set; } = new();

    public GroupEditorDialog(ActivityGroup? existing)
    {
        InitializeComponent();

        if (existing != null)
        {
            NameBox.Text = existing.Name;
            Result = new ActivityGroup
            {
                Id = existing.Id,
                Name = existing.Name,
                Color = existing.Color,
                SortOrder = existing.SortOrder,
                Activities = existing.Activities
            };

            // Select matching color swatch
            foreach (RadioButton rb in ColorPanel.Children)
            {
                if (rb.Tag?.ToString() == existing.Color)
                {
                    rb.IsChecked = true;
                    break;
                }
            }
        }

        NameBox.Focus();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            MessageBox.Show("Please enter a group name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result.Name = NameBox.Text.Trim();

        foreach (RadioButton rb in ColorPanel.Children)
        {
            if (rb.IsChecked == true)
            {
                Result.Color = rb.Tag?.ToString() ?? "#4A90D9";
                break;
            }
        }

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
