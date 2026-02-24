using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Mapsui.Nts.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Messages;

namespace UrbanEcho.ViewModels
{
    public partial class EditModeViewModel : ObservableObject
    {
        private bool _hasProject;

        [ObservableProperty]
        private bool _isEditMode;

        public EditModeViewModel()
        {
            // Listens for when a project is laoded
            WeakReferenceMessenger.Default.Register<ProjectLoadedMessage>(this, (r, m) =>
            {
                _hasProject = true;
                NotifyEditCommands();
            });

            // Listens for when a project is closed
            WeakReferenceMessenger.Default.Register<ProjectClosedMessage>(this, (r, m) =>
            {
                _hasProject = false;
                _hasProject = false;
                NotifyEditCommands();
            });
        }

        partial void OnIsEditModeChanged(bool value)
        {
            WeakReferenceMessenger.Default.Send(new EditModeChangedMessage(value));
        }

        [RelayCommand(CanExecute = nameof(CanToggleEdit))]
        private void ToggleEditMode() => IsEditMode = !IsEditMode;

        [RelayCommand(CanExecute = nameof(CanEdit))]
        private void CreateRoad() { }

        [RelayCommand(CanExecute = nameof(CanEdit))]
        private void DeleteRoad() { }

        [RelayCommand(CanExecute = nameof(CanEdit))]
        private void CreateTrafficSignal() { }

        [RelayCommand(CanExecute = nameof(CanEdit))]
        private void DeleteTrafficSignal() { }

        private void NotifyEditCommands()
        {
            ToggleEditModeCommand.NotifyCanExecuteChanged();
            CreateRoadCommand.NotifyCanExecuteChanged();
            DeleteRoadCommand.NotifyCanExecuteChanged();
            CreateTrafficSignalCommand.NotifyCanExecuteChanged();
            DeleteTrafficSignalCommand.NotifyCanExecuteChanged();
        }

        private bool CanToggleEdit() => _hasProject;
        private bool CanEdit() => IsEditMode && _hasProject;
    }
}
