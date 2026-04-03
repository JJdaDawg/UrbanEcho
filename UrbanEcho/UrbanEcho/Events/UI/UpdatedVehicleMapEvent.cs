using Mapsui;
using Mapsui.UI;
using Mapsui.UI.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.Sim;
using UrbanEcho.FileManagement;

namespace UrbanEcho.Events.UI
{
    /// <summary>
    /// Sets a flag indicating that data for the vehicle layer has changed so Mapsui redraws the layer
    /// </summary>
    internal class UpdatedVehicleMapEvent : IEventForUI
    {
        private List<IFeature> VehicleList = new List<IFeature>();

        public UpdatedVehicleMapEvent(List<IFeature> vehicleList)
        {
            VehicleList = vehicleList;
        }

        public void Run()
        {
            ProjectLayers.SetVehicleLayerDataChanged(VehicleList);
        }
    }
}