using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UrbanEcho.Models;
using CommunityToolkit.Mvvm.Messaging;
using UrbanEcho.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.ViewModels
{
    public partial class SimulationViewModel : ObservableObject
    {
        private bool _hasProject;

        public SimulationViewModel() 
        {
            // Listens for when a project is loaded
            WeakReferenceMessenger.Default.Register<ProjectLoadedMessage>(this, (r, m) =>
            {
                _hasProject = true;
                NotifyAllCommands();
            });

            // Listens for when a project is closed
            WeakReferenceMessenger.Default.Register<ProjectClosedMessage>(this, (r, m) =>
            {
                _hasProject = false;
                IsRunning = false;
                NotifyAllCommands();
            });
        }

        [ObservableProperty]
        private bool _isRunning;

        [RelayCommand(CanExecute = nameof(CanStart))]
        private void Start()
        {
            IsRunning = true;
            NotifyAllCommands();
            WeakReferenceMessenger.Default.Send(new LogMessage("Simulation started", LogSource.System));
        }

        [RelayCommand(CanExecute = nameof(CanStop))]
        private void Stop()
        {
            IsRunning = false;
            NotifyAllCommands();
            WeakReferenceMessenger.Default.Send(new LogMessage("Simulation stopped", LogSource.System));
        }

        [RelayCommand(CanExecute = nameof(CanPause))]
        private void Pause()
        {
            WeakReferenceMessenger.Default.Send(new LogMessage("Simulation paused", LogSource.System));
        }

        [RelayCommand(CanExecute = nameof(CanSpeedUp))]
        private void SpeedUp() { }

        [RelayCommand(CanExecute = nameof(CanSpeedDown))]
        private void SpeedDown() { }

        [RelayCommand]
        private void RealTime() { }

        private bool CanStart() => !IsRunning && _hasProject;
        private bool CanStop() => IsRunning && _hasProject;
        private bool CanPause() => IsRunning && _hasProject;
        private bool CanSpeedUp() => IsRunning && _hasProject;
        private bool CanSpeedDown() => IsRunning && _hasProject;

        private void NotifyAllCommands()
        {
            StartCommand.NotifyCanExecuteChanged();
            StopCommand.NotifyCanExecuteChanged();
            PauseCommand.NotifyCanExecuteChanged();
            SpeedUpCommand.NotifyCanExecuteChanged();
            SpeedDownCommand.NotifyCanExecuteChanged();
        }
    }
}
