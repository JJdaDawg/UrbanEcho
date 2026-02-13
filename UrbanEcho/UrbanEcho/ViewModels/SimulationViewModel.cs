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
        }

        private bool CanStart() => !IsRunning;

        [RelayCommand]
        private void Stop()
        {
            IsRunning = false;
        }

        [RelayCommand]
        private void Pause()
        {
            // Pause logic here
        }
    }
}
