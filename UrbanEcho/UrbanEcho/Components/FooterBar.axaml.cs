using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using UrbanEcho.ViewModels;

namespace UrbanEcho;

public partial class FooterBar : UserControl
{
    public FooterBar()
    {
        InitializeComponent();

        this.DataContextChanged += (s, e) =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.Simulation.PropertyChanged += (_, __) =>
                {
                    // Update background when running state changes
                    FooterBorder.Background = vm.Simulation.IsRunning ? Brushes.IndianRed : Brushes.Black;
                };
            }
        };
    }
}