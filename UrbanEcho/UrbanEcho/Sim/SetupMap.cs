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
using UrbanEcho.Styles;

namespace UrbanEcho.Sim
{
    public class SetupMap
    {
        public static void Init(MapControl MyMapControl)
        {
            MyMapControl.Map.CRS = "EPSG:3857"; // The Map CRS needs to be set

            //Load the styles to use that are not default
            //other styles that are default will already be registered
            MapRenderer.RegisterStyleRenderer(typeof(RoadStyle), new RoadStyleRenderer());

            //Add default Zoom limit right away so no crashes if mouse wheel scrolling without layer loaded
            ProjectLayers.SetDefaultZoomLimit(MyMapControl);
        }
    }
}