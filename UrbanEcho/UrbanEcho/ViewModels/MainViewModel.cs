using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string consoleText = "";

        public void UpdateConsoleText(string message)
        {
            ConsoleText += $"{message}";
            ConsoleText += $"{Environment.NewLine}";
        }
    }
}