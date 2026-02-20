using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapsui;
using System.Threading.Tasks;
using UrbanEcho.Events.Sim;
using UrbanEcho.FileManagement;
using UrbanEcho.Services;

namespace UrbanEcho.ViewModels
{
    public partial class ProjectViewModel : ObservableObject
    {
        private readonly IFileDialogService _fileDialogService;
        private ProjectFile? _currentProject;

        [ObservableProperty] private bool _hasProject;

        public ProjectViewModel(IFileDialogService fileDialogService)
        {
            _fileDialogService = fileDialogService;
        }

        [RelayCommand]
        private async Task OpenProject()
        {
            var path = await _fileDialogService.OpenFileAsync();
            if (path is null) return;

            _currentProject = ProjectFile.Open(path);
            if (_currentProject is not null)
            {
                HasProject = true;
                NotifyProjectCommands();
            }
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveAsProject()
        {
            if (_currentProject is null) return;

            var path = await _fileDialogService.SaveFileAsync();
            if (path is null) return;

            ProjectFile.SaveAs(_currentProject, path);
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private void SaveProject()
        {
            if (_currentProject is null) return;
            ProjectFile.Save(_currentProject);
        }

        [RelayCommand]
        private async Task CreateProject()
        {
            // TODO: Implement create project
        }

        [RelayCommand(CanExecute = nameof(CanClose))] 
        private void CloseProject()
        {
            // TODO: Implement close project
        }

        [RelayCommand(CanExecute = nameof(CanImportData))]
        private void ImportData()
        {
            // TODO: Implement importing of data
        }

        private bool CanSave() => _currentProject is not null;
        private bool CanClose() => _currentProject is not null;
        private bool CanImportData() => _currentProject is not null;

        private void NotifyProjectCommands()
        {
            SaveAsProjectCommand.NotifyCanExecuteChanged();
            SaveProjectCommand.NotifyCanExecuteChanged();
            CloseProjectCommand.NotifyCanExecuteChanged();
            ImportDataCommand.NotifyCanExecuteChanged();
        }
    }
}