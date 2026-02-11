using Mapsui.UI.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Sim;
using UrbanEcho.ViewModels;

namespace UrbanEcho.Events.UI
{
    internal class ShowMessageConsoleWindowEvent : IEventForUI
    {
        private MainViewModel? mainViewModel;
        private string message;

        public ShowMessageConsoleWindowEvent(MainViewModel? mainViewModel, string message)
        {
            this.mainViewModel = mainViewModel;
            this.message = message;
        }

        public void Run()
        {
            mainViewModel?.UpdateConsoleText(message);
        }
    }
}