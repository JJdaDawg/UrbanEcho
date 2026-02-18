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
    public partial class ProjectExplorerViewModel : ObservableObject
    {
        private readonly IPanelService _panelService;

        public ProjectExplorerViewModel(IPanelService panelService)
        {
            _panelService = panelService;
            CloseCommand = new RelayCommand(Close);
        }

        public RelayCommand CloseCommand { get; }

        private void Close() => _panelService.ToggleProjectExplorer(false);
    }
}
