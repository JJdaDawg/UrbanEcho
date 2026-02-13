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

        public SimulationViewModel(ConsoleViewModel console)
        {
            _console = console;
        }

        [ObservableProperty]
        private bool _isRunning;

        [RelayCommand(CanExecute = nameof(CanStart))]
        private void Start()
        {
            IsRunning = true;
            StopCommand.NotifyCanExecuteChanged();
            PauseCommand.NotifyCanExecuteChanged();
            StartCommand.NotifyCanExecuteChanged();
            _console.AddLog("Simulation started", LogSource.System);
        }

        [RelayCommand(CanExecute = nameof(CanStop))]
        private void Stop()
        {
            IsRunning = false;
            StopCommand.NotifyCanExecuteChanged();
            PauseCommand.NotifyCanExecuteChanged();
            StartCommand.NotifyCanExecuteChanged();
            _console.AddLog("Simulation stopped", LogSource.System);
        }

        [RelayCommand(CanExecute = nameof(CanPause))]
        private void Pause()
        {
            // Pause logic here
            _console.AddLog("Simulation paused", LogSource.System);
        }

        [RelayCommand(CanExecute = nameof(CanSpeedUp))]
        private void SpeedUp()
        {

        }

        [RelayCommand(CanExecute = nameof(CanSpeedDown))]
        private void SpeedDown()
        {

        }

        [RelayCommand]
        private void RealTime()
        {

        }

        private bool CanStart() => !IsRunning;
        private bool CanStop() => IsRunning;
        private bool CanPause() => IsRunning;
        private bool CanSpeedUp() => IsRunning;
        private bool CanSpeedDown() => IsRunning;
    }

}
