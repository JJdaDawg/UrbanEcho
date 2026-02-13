using CommunityToolkit.Mvvm.ComponentModel;
using Mapsui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.ViewModels
{
    public partial class MapViewModel : ObservableObject
    {
        [ObservableProperty]
        private Map myMap = new Map();
    }
}
