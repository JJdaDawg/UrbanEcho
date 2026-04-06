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
    /// <summary>
    /// ProjectViewModel Class handles UI for interacting with opening,closing,saving,creating projectfiles
    /// </summary>
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

        /// <summary>
        /// Opens a project
        /// </summary>
        /// <returns>Returns a <see cref="Task"/> </returns>
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

        /// <summary>
        /// Sets the project currently opened
        /// </summary>
        public void SetProject(ProjectFile? projectFile)
        {
            _currentProject = projectFile;

            if (_currentProject is not null)
            {
                HasProject = true;

                WeakReferenceMessenger.Default.Send(new ProjectLoadedMessage());

                ProjectName = _currentProject.FileName;
            }
            else
            {
                HasProject = false;
                WeakReferenceMessenger.Default.Send(new ProjectClosedMessage());
            }

            NotifyProjectCommands();
        }

        /// <summary>
        /// Save As Project
        /// </summary>
        /// <returns>Returns a <see cref="Task"/> </returns>
        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveAsProject()
        {
            if (_currentProject is null) return;

            var path = await _fileDialogService.SaveFileAsync();
            if (path is null) return;

            SaveAsProjectEvent saveAsProjectEvent = new SaveAsProjectEvent(_currentProject, path);
            EventQueueForSim.Instance.Add(saveAsProjectEvent);
        }

        /// <summary>
        /// Saves a project
        /// </summary>

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
        }

        /// <summary>
        /// Creates a project
        /// </summary>

        [RelayCommand]
        private void CreateProject()
        {
            if (MainWindow.Instance.GetMap() != null)
            {
                NewProjectEvent newProjectEvent = new NewProjectEvent(MainWindow.Instance.GetMap());
                EventQueueForSim.Instance.Add(newProjectEvent);
            }
        }

        /// <summary>
        /// Closes a project
        /// </summary>

        [RelayCommand(CanExecute = nameof(CanClose))]
        private void CloseProject()
        {
            if (MainWindow.Instance.GetMap() != null)
            {
                NewProjectEvent newProjectEvent = new NewProjectEvent(MainWindow.Instance.GetMap());
                EventQueueForSim.Instance.Add(newProjectEvent);
            }
        }

        /// <summary>
        /// Imports Data
        /// </summary>

        [RelayCommand(CanExecute = nameof(CanImportData))]
        private void ImportData()
        {
            //This function not used
        }

        /// <summary>
        /// Starts a event that loads the open street map background
        /// </summary>
        /// <returns>Returns a <see cref="Task"/> </returns>
        [RelayCommand(CanExecute = nameof(CanImportData))]
        private void ImportOSMBackground()
        {
            Map map = MainWindow.Instance.GetMap();

            LoadFileEvent loadBackgroundEvent = new LoadFileEvent(UrbanEcho.FileManagement.FileTypes.FileType.BackgroundFile, "osm", map);
            EventQueueForSim.Instance.Add(loadBackgroundEvent);
        }

        /// <summary>
        /// Starts a import viewport event, used for importing open street map data within a viewport
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanImportData))]
        private void ImportViewport()
        {
            ImportViewportEvent importViewportEvent = new ImportViewportEvent();
            EventQueueForSim.Instance.Add(importViewportEvent);
        }

        /// <summary>
        /// Imports a mbtiles background file
        /// </summary>
        /// <returns>Returns a <see cref="Task"/> </returns>
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
        }

        /// <summary>
        /// Imports road data
        /// </summary>
        /// <returns>Returns a <see cref="Task"/> </returns>
        [RelayCommand(CanExecute = nameof(CanImportData))]
        private async Task ImportRoads()
        {
            var path = await _fileDialogService.OpenShapeFileAsync("Import Roads", FileTypes.VectorFile);
            if (path is null) return;

            Map? map = MainWindow.Instance.GetMap();
            if (map != null)
            {
                LoadFileEvent loadRoadEvent = new LoadFileEvent(UrbanEcho.FileManagement.FileTypes.FileType.RoadLayerFile, path, map);
                EventQueueForSim.Instance.Add(loadRoadEvent);
            }
        }

        /// <summary>
        /// Imports intersection data
        /// </summary>
        /// <returns>Returns a <see cref="Task"/> </returns>
        [RelayCommand(CanExecute = nameof(CanImportData))]
        private async Task ImportIntersections()
        {
            var path = await _fileDialogService.OpenShapeFileAsync("Import Intersections", FileTypes.VectorFile);
            if (path is null) return;

            Map? map = MainWindow.Instance.GetMap();
            if (map != null)
            {
                LoadFileEvent loadIntersectionEvent = new LoadFileEvent(UrbanEcho.FileManagement.FileTypes.FileType.IntersectionLayerFile, path, map);
                EventQueueForSim.Instance.Add(loadIntersectionEvent);
            }
        }

        /// <summary>
        /// Imports census data
        /// </summary>
        /// <returns>Returns a <see cref="Task"/> </returns>
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

        /// <summary>
        ///Can save if project is not null
        /// </summary>
        private bool CanSave() => _currentProject is not null;

        /// <summary>
        ///Can close if project is not null
        /// </summary>
        private bool CanClose() => _currentProject is not null;

        /// <summary>
        ///Can import data if project is not null
        /// </summary>
        private bool CanImportData() => _currentProject is not null;

        /// <summary>
        ///Notifies UI elements ability to execute commands has changed
        /// </summary>
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