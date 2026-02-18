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
    public partial class PropertiesViewModel : ObservableObject
    {
        private readonly IPanelService _panelService;

        public PropertiesViewModel(IPanelService panelService)
        {
            _panelService = panelService;
            CloseCommand = new RelayCommand(Close);
        }

        public RelayCommand CloseCommand { get; }

        private void Close() => _panelService.ToggleProperties(false);
    }
}
