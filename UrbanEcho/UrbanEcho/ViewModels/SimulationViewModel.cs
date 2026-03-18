using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.Sim;
using UrbanEcho.Messages;
using UrbanEcho.Models;

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
                IsRunning = false;
                IsPaused = false;
                NotifyAllCommands();
            });

            // Listens for when a project is closed
            WeakReferenceMessenger.Default.Register<ProjectClosedMessage>(this, (r, m) =>
            {
                _hasProject = false;
                IsRunning = false;
                IsPaused = false;
                NotifyAllCommands();
            });
        }

        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private bool _isPaused;

        [ObservableProperty]
        private bool _isPausedOrRunning;

        [RelayCommand(CanExecute = nameof(CanStart))]
        private void Start()
        {
            ControlSimEvent controlSimEvent = new ControlSimEvent(Sim.SimControlType.Start);
            EventQueueForSim.Instance.Add(controlSimEvent);
            IsRunning = true;
            IsPaused = false;
            IsPausedOrRunning = true;
            NotifyAllCommands();
            WeakReferenceMessenger.Default.Send(new LogMessage("Simulation started", LogSource.System));
        }

        [RelayCommand(CanExecute = nameof(CanStop))]
        private void Stop()
        {
            ControlSimEvent controlSimEvent = new ControlSimEvent(Sim.SimControlType.Stop);
            EventQueueForSim.Instance.Add(controlSimEvent);
            IsRunning = false;
            IsPaused = false;
            IsPausedOrRunning = false;
            NotifyAllCommands();
            WeakReferenceMessenger.Default.Send(new LogMessage("Simulation stopped", LogSource.System));
        }

        [RelayCommand(CanExecute = nameof(CanPause))]
        private void Pause()
        {
            IsRunning = !IsRunning;
            IsPaused = !IsPaused;
            IsPausedOrRunning = (IsRunning || IsPaused);
            ControlSimEvent controlSimEvent = new ControlSimEvent(Sim.SimControlType.Pause);
            EventQueueForSim.Instance.Add(controlSimEvent);
            WeakReferenceMessenger.Default.Send(new LogMessage(IsRunning ? "Simulation restarted" : "Simulation paused", LogSource.System));
        }

        [RelayCommand(CanExecute = nameof(CanSpeedUp))]
        private void SpeedUp()
        {
            ControlSimEvent controlSimEvent = new ControlSimEvent(Sim.SimControlType.SpeedUp);
            EventQueueForSim.Instance.Add(controlSimEvent);
        }

        [RelayCommand(CanExecute = nameof(CanSpeedDown))]
        private void SpeedDown()
        {
            ControlSimEvent controlSimEvent = new ControlSimEvent(Sim.SimControlType.SpeedDown);
            EventQueueForSim.Instance.Add(controlSimEvent);
        }

        public void ResetControls()
        {
            IsRunning = false;
            IsPaused = false;
            IsPausedOrRunning = false;
            NotifyAllCommands();
        }

        [RelayCommand]
        private void RealTime()
        { }

        private bool CanStart() => !IsRunning && _hasProject;

        private bool CanStop() => IsPausedOrRunning && _hasProject;

        private bool CanPause() => IsPausedOrRunning && _hasProject;

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