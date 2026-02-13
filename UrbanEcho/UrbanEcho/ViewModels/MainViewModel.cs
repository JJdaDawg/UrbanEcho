using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapsui;
using Mapsui.UI.Avalonia;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<string> _logItems = new();

        [ObservableProperty]
        private Map myMap = new Map();

        public void UpdateConsoleText(string message)
        {
            LogItems.Add(message);

            // Keep the logs from growing too large
            if (LogItems.Count > 500) LogItems.RemoveAt(0);
        }
    }
}