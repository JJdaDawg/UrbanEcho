using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Box2dNet.Interop;
using System;
using System.Threading;
using System.Threading.Tasks;
using UrbanEcho.Sim;

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
            Sim.Sim.Cts.Cancel();

            if (Sim.Sim.SimTask != null)
            {
                Sim.Sim.SimTask.Wait();
                B2Api.b2DestroyWorld(World.WorldId);//Destroy world
            }
        }
        catch
        {
            //TODO: maybe log to file, can't show error since window is closing
        }
        finally
        {
            Sim.Sim.Cts.Dispose();
        }
    }
}