using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;
using UrbanEcho.Models;

namespace UrbanEcho.ViewModels
{
    public partial class ConsoleViewModel : ObservableObject
    {
        private readonly ObservableCollection<string> _mapLogs = new();
        private readonly ObservableCollection<string> _systemLogs = new();
        private readonly IPanelService _panelService;
        private bool _isOpen = true;

        [ObservableProperty] private LogSource _selectedSource = LogSource.System;
        [ObservableProperty] private bool _isVisible = true;

        public RelayCommand ToggleCommand { get; }

        public ConsoleViewModel(IPanelService panelService)
        {
            _panelService = panelService;
            ToggleCommand = new RelayCommand(Toggle);
        }

        public ObservableCollection<string> CurrentLogs => SelectedSource == LogSource.Map ? _mapLogs : _systemLogs;

        [RelayCommand] private void ClearConsole() => CurrentLogs.Clear();

        public void AddLog(string message, LogSource source)
        {
            if (source == LogSource.Map) _mapLogs.Add($"[Map] {message}");
            else _systemLogs.Add($"[Sys] {message}");
            OnPropertyChanged(nameof(CurrentLogs));
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            _panelService.ToggleConsole(_isOpen);
        }
    }
}
