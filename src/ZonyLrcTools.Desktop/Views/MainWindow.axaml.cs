using Avalonia.Controls;

namespace ZonyLrcTools.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SizeChanged += (_, _) =>
        {
            ShellGrid.ColumnDefinitions[0].Width = new GridLength(Bounds.Width < 1100 ? 176 : 204);
            ContentArea.Padding = new Avalonia.Thickness(Bounds.Width < 1100 ? 16 : 24);
        };
    }
}
