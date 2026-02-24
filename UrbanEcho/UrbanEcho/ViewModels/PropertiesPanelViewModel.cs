using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;

namespace UrbanEcho.ViewModels
{
    public partial class PropertiesPanelViewModel : ObservableObject
    {
        private readonly IPanelService _panelService;
        private bool _isOpen = true;

        public RelayCommand ToggleCommand { get; }

        public PropertiesPanelViewModel(IPanelService panelService)
        {
            _panelService = panelService;
            ToggleCommand = new RelayCommand(Toggle);
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            _panelService.ToggleProperties(_isOpen);
        }
    }
}
