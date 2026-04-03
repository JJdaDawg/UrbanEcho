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
    /// Updates the footer with the current values
    /// </summary>
    internal class UpdateFooterEvent : IEventForUI
    {
        private string readyText;
        private string projectText;
        private string simTimeText;
        private int vehicleCount;

        public UpdateFooterEvent(string readyText, string projectText, string simTimeText, int vehicleCount)
        {
            this.readyText = readyText;
            this.projectText = projectText;
            this.simTimeText = simTimeText;
            this.vehicleCount = vehicleCount;
        }

        public void Run()
        {
            MainViewModel? mvm = MainWindow.Instance.GetMainViewModel();
            if (mvm is not null)
            {
                mvm.Footer.UpdateFooterView(readyText, projectText, simTimeText, vehicleCount);
            }
        }
    }
}