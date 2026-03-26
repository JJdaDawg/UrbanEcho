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
    internal class UpdateFooterEvent : IEventForUI
    {
        private string readyText;
        private string projectText;

        public UpdateFooterEvent(string readyText, string projectText)
        {
            this.readyText = readyText;
            this.projectText = projectText;
        }

        public void Run()
        {
            MainViewModel? mvm = MainWindow.Instance.GetMainViewModel();
            if (mvm is not null)
            {
                mvm.Footer.UpdateFooterView(readyText, projectText);
            }
        }
    }
}