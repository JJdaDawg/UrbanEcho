using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MyApp;

public partial class ConsolePanel : UserControl
{
    public ConsolePanel()
    {
        InitializeComponent();
    }

    private void ComboBox_ActualThemeVariantChanged(object? sender, System.EventArgs e)
    {
    }
}