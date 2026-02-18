using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BruTile.Wms;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;
using UrbanEcho.Sim;
using UrbanEcho.ViewModels;

namespace UrbanEcho;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public static IServiceProvider Services { get; private set; } = null!;

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        var mainWindow = new MainWindow();
        services.AddSingleton<IPanelService>(mainWindow);
        services.AddSingleton<MainViewModel>();

        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Exit += OnExit;
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    //https://stackoverflow.com/questions/75247536/call-method-on-application-exit-in-avalonia
    private void OnStartup(object s, ControlledApplicationLifetimeStartupEventArgs e)

    {
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        try
        {
            Simulation.Cts.Cancel();

            if (Simulation.SimTask != null)
            {
                Simulation.SimTask.Wait();
            }
        }
        catch (System.Exception ex)
        {
            //TODO: Add errors for task had a exception
        }
        finally
        {
            Simulation.Cts.Dispose();
        }
    }
}