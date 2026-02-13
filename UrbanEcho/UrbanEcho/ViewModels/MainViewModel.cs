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
    public enum LogSource { System, Map }

    public partial class MainViewModel : ObservableObject
    {
        private ObservableCollection<string> _mapLogs = new();
        private readonly ObservableCollection<string> _systemLogs = new();

        [ObservableProperty]
        private LogSource _selectedSource = LogSource.Map; // Default option

        public ObservableCollection<string> CurrentLogs => SelectedSource == LogSource.Map ? _mapLogs : _systemLogs;

        [ObservableProperty]
        private Map myMap = new Map();

        [ObservableProperty]
        private bool _isConsoleVisible = true;

        [RelayCommand]
        public void ToggleConsole()
        {
            IsConsoleVisible = !IsConsoleVisible;
        }

        partial void OnSelectedSourceChanged(LogSource value)
        {
            // When the console output ComboBox changes, tell the ListBox to refresh
            OnPropertyChanged(nameof(CurrentLogs));
        }

        [RelayCommand]
        public void ClearConsole() => CurrentLogs.Clear();

        public void UpdateConsoleText(string message, LogSource source = LogSource.Map)
        {
            if (source == LogSource.Map)
            {
                _mapLogs.Add($"[Map] {message}");
            }
            else
            {
                _systemLogs.Add($"[Sys] {message}");
            }

            OnPropertyChanged(nameof(CurrentLogs));
        }
    }
}