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

        [ObservableProperty] private string _projectName = string.Empty;
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
            Map? map = MainWindow.Instance.GetMap();
            if (map != null)
            {
                LoadFileEvent loadProjectEvent = new LoadFileEvent(UrbanEcho.FileManagement.FileTypes.FileType.ProjectFile, path, map);
                EventQueueForSim.Instance.Add(loadProjectEvent);
            }
        }

        public void SetProject(ProjectFile? projectFile)
        {
            _currentProject = projectFile;

            if (_currentProject is not null)
            {
                HasProject = true;
                //NotifyProjectCommands();
                WeakReferenceMessenger.Default.Send(new ProjectLoadedMessage());
                //WeakReferenceMessenger.Default.Send(new LogMessage($"Project successfully opened '{path}'", LogSource.System));
                ProjectName = _currentProject.FileName;
            }
            else
            {
                HasProject = false;
                WeakReferenceMessenger.Default.Send(new ProjectClosedMessage());
            }

            NotifyProjectCommands();
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveAsProject()
        {
            if (_currentProject is null) return;

            var path = await _fileDialogService.SaveFileAsync();
            if (path is null) return;

            SaveAsProjectEvent saveAsProjectEvent = new SaveAsProjectEvent(_currentProject, path);
            EventQueueForSim.Instance.Add(saveAsProjectEvent);

            //WeakReferenceMessenger.Default.Send(new LogMessage($"Project successfully saved '{path}'", LogSource.System));
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private void SaveProject()
        {
            if (_currentProject is null) return;
            if (_currentProject.PathForThisFile != string.Empty)
            {
                SaveProjectEvent saveProjectEvent = new SaveProjectEvent(_currentProject);
                EventQueueForSim.Instance.Add(saveProjectEvent);
            }
            else
            {
                Task saveAs = SaveAsProject();
            }

            //WeakReferenceMessenger.Default.Send(new LogMessage("Project successfully saved", LogSource.System));
        }

        [RelayCommand]
        private void CreateProject()
        {
            //var path = await _fileDialogService.CreateProject();
            //if (path is null) return;

            //_currentProject = new ProjectFile();
            //ProjectFile.SaveAs(_currentProject, path);
            if (MainWindow.Instance.GetMap() != null)
            {
                NewProjectEvent newProjectEvent = new NewProjectEvent(MainWindow.Instance.GetMap());
                EventQueueForSim.Instance.Add(newProjectEvent);
                //HasProject = true;
                //NotifyProjectCommands();
                //WeakReferenceMessenger.Default.Send(new ProjectLoadedMessage());
                //WeakReferenceMessenger.Default.Send(new LogMessage($"Project successfully created '{path}'", LogSource.System));
            }
            //_currentProject = ProjectLayers.GetProject();
        }

        [RelayCommand(CanExecute = nameof(CanClose))]
        private void CloseProject()
        {
            // TODO: Check to see if user wants to save changes before nulling the project
            //_currentProject = null;
            //HasProject = false;
            //NotifyProjectCommands();
            //WeakReferenceMessenger.Default.Send(new ProjectClosedMessage());
            //WeakReferenceMessenger.Default.Send(new LogMessage("Project successfully closed", LogSource.System));
            if (MainWindow.Instance.GetMap() != null)
            {
                NewProjectEvent newProjectEvent = new NewProjectEvent(MainWindow.Instance.GetMap());
                EventQueueForSim.Instance.Add(newProjectEvent);
            }
        }

        [RelayCommand(CanExecute = nameof(CanImportData))]
        private void ImportData()
        {
            // TODO: Implement importing of data
            //WeakReferenceMessenger.Default.Send(new LogMessage("Data imported successfully", LogSource.System));
        }

        [RelayCommand(CanExecute = nameof(CanImportData))]
        private void ImportOSMBackground()
        {
            Map map = MainWindow.Instance.GetMap();

            LoadFileEvent loadBackgroundEvent = new LoadFileEvent(UrbanEcho.FileManagement.FileTypes.FileType.BackgroundFile, "osm", map);
            EventQueueForSim.Instance.Add(loadBackgroundEvent);

            //ProjectLayers.LoadBackgroundFile(path);
            //WeakReferenceMessenger.Default.Send(new LogMessage($"Background loaded '{path}'", LogSource.System));
        }

        [RelayCommand(CanExecute = nameof(CanImportData))]
        private void ImportViewport()
        {
            ImportViewportEvent importViewportEvent = new ImportViewportEvent();
            EventQueueForSim.Instance.Add(importViewportEvent);
        }

        [RelayCommand(CanExecute = nameof(CanImportData))]
        private async Task ImportBackground()
        {
            var path = await _fileDialogService.OpenShapeFileAsync("Import Background", FileTypes.MbTiles);
            if (path is null) return;

            Map? map = MainWindow.Instance.GetMap();
            if (map != null)
            {
                LoadFileEvent loadBackgroundEvent = new LoadFileEvent(UrbanEcho.FileManagement.FileTypes.FileType.BackgroundFile, path, map);
                EventQueueForSim.Instance.Add(loadBackgroundEvent);
            }

            //ProjectLayers.LoadBackgroundFile(path);
            //WeakReferenceMessenger.Default.Send(new LogMessage($"Background loaded '{path}'", LogSource.System));
        }

        [RelayCommand(CanExecute = nameof(CanImportData))]
        private async Task ImportRoads()
        {
            var path = await _fileDialogService.OpenShapeFileAsync("Import Roads", FileTypes.VectorFile);
            if (path is null) return;
            //ProjectLayers.LoadRoadFile(path);
            //WeakReferenceMessenger.Default.Send(new LogMessage($"Roads loaded '{path}'", LogSource.System));
            Map? map = MainWindow.Instance.GetMap();
            if (map != null)
            {
                LoadFileEvent loadRoadEvent = new LoadFileEvent(UrbanEcho.FileManagement.FileTypes.FileType.RoadLayerFile, path, map);
                EventQueueForSim.Instance.Add(loadRoadEvent);
            }
        }

        [RelayCommand(CanExecute = nameof(CanImportData))]
        private async Task ImportIntersections()
        {
            var path = await _fileDialogService.OpenShapeFileAsync("Import Intersections", FileTypes.VectorFile);
            if (path is null) return;
            //ProjectLayers.LoadIntersectionsFile(path);
            //WeakReferenceMessenger.Default.Send(new LogMessage($"Intersections loaded '{path}'", LogSource.System));
            Map? map = MainWindow.Instance.GetMap();
            if (map != null)
            {
                LoadFileEvent loadIntersectionEvent = new LoadFileEvent(UrbanEcho.FileManagement.FileTypes.FileType.IntersectionLayerFile, path, map);
                EventQueueForSim.Instance.Add(loadIntersectionEvent);
            }
        }

        [RelayCommand(CanExecute = nameof(CanImportData))]
        private async Task ImportCensus()
        {
            var path = await _fileDialogService.OpenShapeFileAsync("Import Census Data", FileTypes.ShapeFile);
            if (path is null) return;
            Map? map = MainWindow.Instance.GetMap();
            if (map != null)
            {
                LoadFileEvent loadCensusEvent = new LoadFileEvent(UrbanEcho.FileManagement.FileTypes.FileType.CensusLayerFile, path, map);
                EventQueueForSim.Instance.Add(loadCensusEvent);
            }
        }

        private bool CanSave() => _currentProject is not null;

        private bool CanClose() => _currentProject is not null;

        private bool CanImportData() => _currentProject is not null;

        private void NotifyProjectCommands()
        {
            SaveAsProjectCommand.NotifyCanExecuteChanged();
            SaveProjectCommand.NotifyCanExecuteChanged();
            CloseProjectCommand.NotifyCanExecuteChanged();
            ImportViewportCommand.NotifyCanExecuteChanged();
            ImportDataCommand.NotifyCanExecuteChanged();
            ImportBackgroundCommand.NotifyCanExecuteChanged();
            ImportRoadsCommand.NotifyCanExecuteChanged();
            ImportIntersectionsCommand.NotifyCanExecuteChanged();
            ImportCensusCommand.NotifyCanExecuteChanged();
        }
    }
}