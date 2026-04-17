using System.Windows;
using System.Windows.Media;

namespace ActivityTracker;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        StateChanged += OnStateChanged;
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (RootBorder != null)
        {
            // Maximized WPF+WindowChrome windows extend ~7px past the monitor work area.
            RootBorder.Padding = WindowState == WindowState.Maximized
                ? new Thickness(7)
                : new Thickness(0);
            RootBorder.BorderThickness = WindowState == WindowState.Maximized
                ? new Thickness(0)
                : new Thickness(1);
        }

        if (FindName("MaximizeGlyph") is System.Windows.Shapes.Path glyph)
        {
            glyph.Data = WindowState == WindowState.Maximized
                ? Geometry.Parse("M2,0.5 L9.5,0.5 L9.5,8 M0.5,2.5 L7.5,2.5 L7.5,9.5 L0.5,9.5 Z")
                : Geometry.Parse("M0.5,0.5 L9.5,0.5 L9.5,9.5 L0.5,9.5 Z");
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
