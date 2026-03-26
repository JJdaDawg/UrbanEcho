using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;
using UrbanEcho.Messages;
using UrbanEcho.Models;
using UrbanEcho.Sim;

namespace UrbanEcho.ViewModels
{
    public partial class FooterBarViewModel : ObservableObject
    {
        [ObservableProperty] private string readyText = "Not Ready";
        [ObservableProperty] private string projectText = "No Project Loaded";

        public FooterBarViewModel()
        {
        }

        public void UpdateFooterView(string readyText, string projectText)
        {
            ReadyText = readyText;
            ProjectText = projectText;
            OnPropertyChanged(nameof(ReadyText));
            OnPropertyChanged(nameof(ProjectText));
        }
    }
}