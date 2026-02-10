using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace UrbanEcho;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
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
        catch (Exception ex)
        {
            //TODO: Add errors for task had a exception
        }
        finally
        {
            Simulation.Cts.Dispose();
        }
    }
}