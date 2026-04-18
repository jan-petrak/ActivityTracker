using System.Windows;

namespace ActivityTracker.Themes;

public partial class WindowsTheme : ResourceDictionary
{
    public WindowsTheme()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is DependencyObject d)
            Window.GetWindow(d)?.Close();
    }
}
