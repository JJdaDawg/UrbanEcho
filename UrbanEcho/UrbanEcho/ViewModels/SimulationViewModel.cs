using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UrbanEcho.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.ViewModels
{
    public partial class SimulationViewModel : ObservableObject
    {
        private readonly ConsoleViewModel _console;
        private readonly ProjectViewModel _project;

        public SimulationViewModel(ConsoleViewModel console, ProjectViewModel project) 
        {
            _console = console;
            _project = project;
            _project.PropertyChanged += (s, args) => 
            {
                if (args.PropertyName == nameof(ProjectViewModel.HasProject))
                {
                    NotifyAllCommands();
                }
            };
        }

        [ObservableProperty]
        private bool _isRunning;

        [RelayCommand(CanExecute = nameof(CanStart))]
        private void Start()
        {
            IsRunning = true;
            NotifyAllCommands();
            _console.AddLog("Simulation started", LogSource.System);
        }

        [RelayCommand(CanExecute = nameof(CanStop))]
        private void Stop()
        {
            IsRunning = false;
            NotifyAllCommands();
            _console.AddLog("Simulation stopped", LogSource.System);
        }

        [RelayCommand(CanExecute = nameof(CanPause))]
        private void Pause()
        {
            _console.AddLog("Simulation paused", LogSource.System);
        }

        [RelayCommand(CanExecute = nameof(CanSpeedUp))]
        private void SpeedUp() { }

        [RelayCommand(CanExecute = nameof(CanSpeedDown))]
        private void SpeedDown() { }

        [RelayCommand]
        private void RealTime() { }

        private bool CanStart() => !IsRunning && _project.HasProject;
        private bool CanStop() => IsRunning && _project.HasProject;
        private bool CanPause() => IsRunning && _project.HasProject;
        private bool CanSpeedUp() => IsRunning && _project.HasProject;
        private bool CanSpeedDown() => IsRunning && _project.HasProject;

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
