using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.FileManagement
{
    public static class FileTypes
    {
        public enum FileType
        {
            ProjectFile = 1,
            BackgroundFile = 2,
            RoadLayerFile = 3,
            IntersectionLayerFile = 4
        };

        public static readonly FilePickerFileType ProjectFile = new("Urban Echo Project")
        {
            Patterns = new[] { "*.uep" }
        };

        public static readonly FilePickerFileType ShapeFile = new("Shapefile")
        {
            Patterns = new[] { "*.shp" }
        };

        public static readonly FilePickerFileType MbTiles = new("MBTiles")
        {
            Patterns = new[] { "*.mbtiles" }
        };
    }
}