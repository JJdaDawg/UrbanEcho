using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Mapsui;
using System.Threading.Tasks;
using UrbanEcho.Events.Sim;
using UrbanEcho.FileManagement;
using UrbanEcho.Messages;
using UrbanEcho.Models;
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
            Map? map = Sim.Sim.MyMap;
            if (map != null)
            {
                LoadFileEvent loadProjectEvent = new LoadFileEvent(UrbanEcho.FileManagement.FileTypes.FileType.ProjectFile, path, map);
                EventQueueForSim.Instance.Add(loadProjectEvent);
            }
        }

        public void OpenedProject(string path)
        {
            _currentProject = ProjectLayers.GetProject();

            if (_currentProject is not null)
            {
                HasProject = true;
                NotifyProjectCommands();
                WeakReferenceMessenger.Default.Send(new ProjectLoadedMessage());
                WeakReferenceMessenger.Default.Send(new LogMessage($"Project successfully opened '{path}'", LogSource.System));
            }
            else
            {
                HasProject = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveAsProject()
        {
            if (_currentProject is null) return;

            var path = await _fileDialogService.SaveFileAsync();
            if (path is null) return;

            ProjectFile.SaveAs(_currentProject, path);
            WeakReferenceMessenger.Default.Send(new LogMessage($"Project successfully saved '{path}'", LogSource.System));
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private void SaveProject()
        {
            if (_currentProject is null) return;
            ProjectFile.Save(_currentProject);
            WeakReferenceMessenger.Default.Send(new LogMessage("Project successfully saved", LogSource.System));
        }

        [RelayCommand]
        private async Task CreateProject()
        {
            var path = await _fileDialogService.CreateProject();
            if (path is null) return;

            _currentProject = new ProjectFile();
            ProjectFile.SaveAs(_currentProject, path);
            HasProject = true;
            NotifyProjectCommands();
            WeakReferenceMessenger.Default.Send(new ProjectLoadedMessage());
            WeakReferenceMessenger.Default.Send(new LogMessage($"Project successfully created '{path}'", LogSource.System));
        }

        [RelayCommand(CanExecute = nameof(CanClose))]
        private void CloseProject()
        {
            // TODO: Check to see if user wants to save changes before nulling the project
            _currentProject = null;
            HasProject = false;
            NotifyProjectCommands();
            WeakReferenceMessenger.Default.Send(new ProjectClosedMessage());
            WeakReferenceMessenger.Default.Send(new LogMessage("Project successfully closed", LogSource.System));
        }

        [RelayCommand(CanExecute = nameof(CanImportData))]
        private void ImportData()
        {
            // TODO: Implement importing of data
            WeakReferenceMessenger.Default.Send(new LogMessage("Data imported successfully", LogSource.System));
        }

        [RelayCommand(CanExecute = nameof(CanImportData))]
        private async Task ImportBackground()
        {
            var path = await _fileDialogService.OpenShapeFileAsync("Import Background", FileTypes.MbTiles);
            if (path is null) return;
            ProjectLayers.LoadBackgroundFile(path);
            WeakReferenceMessenger.Default.Send(new LogMessage($"Background loaded '{path}'", LogSource.System));
        }

        [RelayCommand(CanExecute = nameof(CanImportData))]
        private async Task ImportRoads()
        {
            var path = await _fileDialogService.OpenShapeFileAsync("Import Roads", FileTypes.ShapeFile);
            if (path is null) return;
            ProjectLayers.LoadRoadFile(path);
            WeakReferenceMessenger.Default.Send(new LogMessage($"Roads loaded '{path}'", LogSource.System));
        }

        [RelayCommand(CanExecute = nameof(CanImportData))]
        private async Task ImportIntersections()
        {
            var path = await _fileDialogService.OpenShapeFileAsync("Import Intersections", FileTypes.ShapeFile);
            if (path is null) return;
            ProjectLayers.LoadIntersectionsFile(path);
            WeakReferenceMessenger.Default.Send(new LogMessage($"Intersections loaded '{path}'", LogSource.System));
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
            ImportBackgroundCommand.NotifyCanExecuteChanged();
            ImportRoadsCommand.NotifyCanExecuteChanged();
            ImportIntersectionsCommand.NotifyCanExecuteChanged();
        }
    }
}