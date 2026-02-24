using Avalonia;
using Avalonia.Controls;

namespace UrbanEcho.Views.Controls;

public partial class PropertiesSection : UserControl
{
    public static readonly StyledProperty<string> HeaderProperty =
        AvaloniaProperty.Register<PropertiesSection, string>(nameof(Header));

    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<PropertiesSection, bool>(nameof(IsExpanded), defaultValue: true);

    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public PropertiesSection()
    {
        InitializeComponent();
    }

    private void ToggleButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        IsExpanded = !IsExpanded;
    }
}