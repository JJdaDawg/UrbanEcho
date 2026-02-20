using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapsui.Nts.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.ViewModels
{
    public partial class EditModeViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isEditMode;

        [RelayCommand]
        private void ToggleEditMode() => IsEditMode = !IsEditMode;

        [RelayCommand(CanExecute = nameof(CanEdit))]
        private void CreateRoad() { }

        [RelayCommand(CanExecute = nameof(CanEdit))]
        private void DeleteRoad() { }

        [RelayCommand(CanExecute = nameof(CanEdit))]
        private void CreateTrafficSignal() { }

        [RelayCommand(CanExecute = nameof(CanEdit))]
        private void DeleteTrafficSignal() { }

        private bool CanEdit() => IsEditMode;
    }
}
