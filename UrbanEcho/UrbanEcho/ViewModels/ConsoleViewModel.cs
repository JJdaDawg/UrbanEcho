using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.ViewModels
{
    public partial class ConsoleViewModel : ObservableObject
    {
        private readonly ObservableCollection<string> _mapLogs = new();
        private readonly ObservableCollection<string> _systemLogs = new();

        [ObservableProperty] private LogSource _selectedSource = LogSource.Map;
        [ObservableProperty] private bool _isVisible = true;

        public ObservableCollection<string> CurrentLogs => SelectedSource == LogSource.Map ? _mapLogs : _systemLogs;

        [RelayCommand] private void ClearConsole() => CurrentLogs.Clear();
        [RelayCommand] private void Close() => IsVisible = !IsVisible;

        public void AddLog(string message, LogSource source)
        {
            if (source == LogSource.Map) _mapLogs.Add($"[Map] {message}");
            else _systemLogs.Add($"[Sys] {message}");
            OnPropertyChanged(nameof(CurrentLogs));
        }
    }
}
