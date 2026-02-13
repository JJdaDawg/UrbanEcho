using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

        [ObservableProperty]
        private bool _isConsoleVisible = true;

        [RelayCommand]
        public void ToggleConsole()
        {
            IsConsoleVisible = !IsConsoleVisible;
        }

        [RelayCommand]
        public void ClearConsole()
        {
            LogItems.Clear();
        }

        public void UpdateConsoleText(string message)
        {
            LogItems.Add(message);

            // Keep the logs from growing too large
            if (LogItems.Count > 500) LogItems.RemoveAt(0);
        }
    }
}