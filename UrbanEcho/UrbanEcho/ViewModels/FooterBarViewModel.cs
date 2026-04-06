using Avalonia.Media;
using Avalonia.Media.Immutable;
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
    /// <summary>
    /// Class for the footer bar shown at bottom of screen
    /// </summary>
    public partial class FooterBarViewModel : ObservableObject
    {
        [ObservableProperty] private string readyText = "Not Ready";
        [ObservableProperty] private string projectText = "No Project Loaded";
        [ObservableProperty] private string simTimeText = "--:--";
        [ObservableProperty] private string vehicleCountText = "Vehicles: 0";
        [ObservableProperty] private IBrush theBackgroundColor = Brushes.Black;

        public FooterBarViewModel()
        {
        }

        /// <summary>
        /// Updates the foot bar whenever called
        /// </summary>
        public void UpdateFooterView(string readyText, string projectText, string simTimeText, int vehicleCount)
        {
            ReadyText = readyText;
            ProjectText = projectText;
            SimTimeText = simTimeText;
            VehicleCountText = $"Vehicles: {vehicleCount}";
            if (SimManager.Instance.RunSimulation)
            {
                TheBackgroundColor = Brushes.IndianRed;
            }
            else if (SimManager.Instance.Paused)
            {
                TheBackgroundColor = Brushes.DarkOrange;
            }
            else
            {
                TheBackgroundColor = Brushes.Black;
            }
        }
    }
}