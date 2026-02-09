using BruTile.MbTiles;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts.Providers.Shapefile;
using Mapsui.Providers;
using Mapsui.Rendering.Skia;
using Mapsui.Styles;
using Mapsui.Tiling.Layers;
using Mapsui.UI.Avalonia;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp
{
    public class SetupMap
    {
        public static void Init(MapControl MyMapControl)
        {
            MyMapControl.Map.CRS = "EPSG:3857"; // The Map CRS needs to be set

            //Load the styles to use
            MapRenderer.RegisterStyleRenderer(typeof(RoadStyle), new RoadStyleRenderer());
        }
    }
}