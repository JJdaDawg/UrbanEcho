using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.FileManagement;

namespace UrbanEcho.Services
{
    public class FileDialogService : IFileDialogService
    {
        private readonly TopLevel _topLevel;
        private static readonly string[] options = new[] { "*.uep" };

        public FileDialogService(TopLevel topLevel)
        {
            _topLevel = topLevel;
        }

        public async Task<string?> OpenShapeFileAsync(string title, FilePickerFileType fileType)
        {
            var files = await _topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = new[] { fileType }
            });
            return files.Count > 0 ? files[0].Path.LocalPath : null;
        }

        public async Task<string?> OpenFileAsync()
        {
            var files = await _topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Project",
                AllowMultiple = false,
                FileTypeFilter = new[] { FileTypes.ProjectFile }
            });

            return files.Count > 0 ? files[0].Path.LocalPath : null;
        }

        public async Task<string?> SaveFileAsync()
        {
            var file = await _topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Project",
                DefaultExtension = "uep",
                FileTypeChoices = new[] { FileTypes.ProjectFile }
            });

            return file?.Path.LocalPath;
        }

        public async Task<string?> CreateProject()
        {
            var file = await _topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Create Project",
                DefaultExtension = "uep",
                FileTypeChoices = new[] { FileTypes.ProjectFile }
            });

            return file?.Path.LocalPath;
        }
    }
}
