using System.Windows;

namespace ActivityTracker.Views.Dialogs;

public partial class MessageDialog : Window
{
    private MessageDialog(string title, string message, string primaryText, string? secondaryText)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
        PrimaryButton.Content = primaryText;
        if (secondaryText != null)
        {
            SecondaryButton.Content = secondaryText;
            SecondaryButton.Visibility = Visibility.Visible;
        }
    }

    private void Primary_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    private void Secondary_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    public static void ShowInfo(string title, string message)
    {
        var dialog = new MessageDialog(title, message, "OK", null)
        {
            Owner = Application.Current.MainWindow
        };
        dialog.ShowDialog();
    }

    public static bool ShowConfirm(string title, string message, string yesText = "Yes", string noText = "No")
    {
        var dialog = new MessageDialog(title, message, yesText, noText)
        {
            Owner = Application.Current.MainWindow
        };
        return dialog.ShowDialog() == true;
    }
}
