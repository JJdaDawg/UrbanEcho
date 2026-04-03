using Mapsui.UI.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.ViewModels;
using UrbanEcho.Models;

namespace UrbanEcho.Events.UI
{
    /// <summary>
    /// Logs a message to the console
    /// </summary>
    internal class LogToConsole : IEventForUI
    {
        private MainViewModel? mainViewModel;
        private string message;

        private static string lastMessage = "";
        private static bool repeatedMessage;

        public LogToConsole(MainViewModel? mainViewModel, string message)
        {
            if (message == lastMessage)
            {
                repeatedMessage = true;
            }
            else
            {
                repeatedMessage = false;
            }
            lastMessage = message;
            this.mainViewModel = mainViewModel;
            this.message = $"{message} [{DateTime.Now}]";
        }

        public void Run()
        {
            if (!repeatedMessage)
            {
                mainViewModel?.Console.AddLog(message, LogSource.System);
            }
        }
    }
}