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
    internal class UpdatePropertyPanelEvent : IEventForUI
    {
        public UpdatePropertyPanelEvent()
        {
        }

        public void Run()
        {
            MainViewModel? mvm = UrbanEcho.Sim.Sim.GetMainViewModel();
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