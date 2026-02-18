using Mapsui.UI.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Sim;
using UrbanEcho.ViewModels;
using UrbanEcho.Models;

namespace UrbanEcho.Events.UI
{
    internal class LogToConsole : IEventForUI
    {
        private MainViewModel? mainViewModel;
        private string message;

        public LogToConsole(MainViewModel? mainViewModel, string message)
        {
            this.mainViewModel = mainViewModel;
            this.message = message;
        }

        public void Run()
        {
            mainViewModel?.Console.AddLog(message, LogSource.Map);
        }
    }
}