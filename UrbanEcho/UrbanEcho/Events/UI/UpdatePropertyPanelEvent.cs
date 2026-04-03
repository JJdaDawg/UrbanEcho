using Mapsui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.FileManagement;
using UrbanEcho.ViewModels;

namespace UrbanEcho.Events.UI
{
    /// <summary>
    /// Updates the property panel with the current values
    /// </summary>
    internal class UpdatePropertyPanelEvent : IEventForUI
    {
        public UpdatePropertyPanelEvent()
        {
        }

        public void Run()
        {
            MainViewModel? mvm = MainWindow.Instance.GetMainViewModel();
            if (mvm is not null)
            {
                if (mvm.Properties.HasSelection)
                {
                    if (mvm.Properties.SelectedProperties is not null)
                    {
                        mvm.Properties.SelectedProperties.UpdatePropertyView();
                    }
                }
            }
        }
    }
}