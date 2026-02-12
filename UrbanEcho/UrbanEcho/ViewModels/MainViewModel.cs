using CommunityToolkit.Mvvm.ComponentModel;
using Mapsui;
using Mapsui.UI.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string consoleText = "";

        [ObservableProperty]
        private Map myMap = new Map();

        public void UpdateConsoleText(string message)
        {
            ConsoleText += $"{message}";
            ConsoleText += $"{Environment.NewLine}";
        }
    }
}