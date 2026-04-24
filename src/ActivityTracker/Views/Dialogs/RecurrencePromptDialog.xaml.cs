using System.Windows;

namespace ActivityTracker.Views.Dialogs;

public enum RecurrenceEditScope
{
    Cancel,
    ThisOccurrence,
    WholeSeries,
}

public partial class RecurrencePromptDialog : Window
{
    private RecurrenceEditScope _result = RecurrenceEditScope.Cancel;

    private RecurrencePromptDialog(string message)
    {
        InitializeComponent();
        MessageText.Text = message;
    }

    private void ThisOccurrence_Click(object sender, RoutedEventArgs e)
    {
        _result = RecurrenceEditScope.ThisOccurrence;
        DialogResult = true;
    }

    private void WholeSeries_Click(object sender, RoutedEventArgs e)
    {
        _result = RecurrenceEditScope.WholeSeries;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _result = RecurrenceEditScope.Cancel;
        DialogResult = false;
    }

    public static RecurrenceEditScope Show(string message)
    {
        var dialog = new RecurrencePromptDialog(message)
        {
            Owner = Application.Current.MainWindow
        };
        dialog.ShowDialog();
        return dialog._result;
    }
}
