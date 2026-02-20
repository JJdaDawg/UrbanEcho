using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Services
{
    public interface IClipboardService
    {
        Task SetTextAsync(string text);
        Task<string?> GetTextAsync();
    }
}
