using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.ViewModels
{
    public partial class SimulationViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isRunning;

        [RelayCommand(CanExecute = nameof(CanStart))]
        private void Start()
        {
            IsRunning = true;
            StopCommand.NotifyCanExecuteChanged();
            PauseCommand.NotifyCanExecuteChanged();
            StartCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand(CanExecute = nameof(CanStop))]
        private void Stop()
        {
            IsRunning = false;
            StopCommand.NotifyCanExecuteChanged();
            PauseCommand.NotifyCanExecuteChanged();
            StartCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand(CanExecute = nameof(CanPause))]
        private void Pause()
        {
            // Pause logic here
        }

        private bool CanStart() => !IsRunning;
        private bool CanStop() => IsRunning;
        private bool CanPause() => IsRunning;
    }

}
