using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Services
{
    public class ClipboardService : IClipboardService
    {
        private readonly TopLevel _topLevel;

        public ClipboardService(TopLevel topLevel)
        {
            _topLevel = topLevel;
        }

        public Task SetTextAsync(string text) => _topLevel.Clipboard!.SetTextAsync(text);

        public Task<string?> GetTextAsync() => _topLevel.Clipboard!.TryGetTextAsync();
    }
}
