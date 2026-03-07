using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Box2dNet.Interop;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;
using UrbanEcho.Services;
using UrbanEcho.Sim;
using UrbanEcho.UI;
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
        services.AddSingleton<IClipboardService>(new ClipboardService(mainWindow));
        services.AddSingleton<IFileDialogService>(new FileDialogService(mainWindow));
        services.AddSingleton<IVehicleService, VehicleService>();

        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Exit += OnExit;
            desktop.MainWindow = mainWindow;
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
            UIUpdate.Cts.Cancel();
            if (UIUpdate.UITask != null)
            {
                UIUpdate.UITask.Wait();
            }

            Sim.Sim.Free();
        }
        catch
        {
            //TODO: maybe log to file, can't show error since window is closing
        }
        finally
        {
            UIUpdate.Cts.Dispose();
        }
    }
}