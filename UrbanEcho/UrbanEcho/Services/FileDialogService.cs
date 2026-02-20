using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Services
{
    public class FileDialogService : IFileDialogService
    {
        private readonly TopLevel _topLevel;

        public FileDialogService(TopLevel topLevel)
        {
            _topLevel = topLevel;
        }

        public async Task<string?> OpenFileAsync()
        {
            var files = await _topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Project",
                AllowMultiple = false,
                // FileTypeChoices here to specify file acceptable file types
            });

            return files.Count > 0 ? files[0].Path.LocalPath : null;
        }

        public async Task<string?> SaveFileAsync()
        {
            var file = await _topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Project",
                DefaultExtension = "uep",
                // FileTypeChoices here to specify file acceptable file types
            });

            return file?.Path.LocalPath;
        }
    }
}
