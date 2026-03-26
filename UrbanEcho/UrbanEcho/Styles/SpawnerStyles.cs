using Mapsui.Styles;
using Mapsui.Styles.Thematics;
using Mapsui.Nts;
using NetTopologySuite.Geometries;

namespace UrbanEcho.Styles
{
    public class SpawnerStyles
    {
        public ThemeStyle CreateThemeStyle()
        {
            return new ThemeStyle(f =>
            {
                if (f is GeometryFeature gf && gf.Geometry is Point)
                {
                    return new SymbolStyle
                    {
                        SymbolScale = 0.6,
                        Fill = new Mapsui.Styles.Brush(new Color(0, 200, 80, 220)),
                        Outline = new Pen(new Color(0, 60, 20, 255), 2),
                        SymbolType = SymbolType.Ellipse,
                    };
                }
                return null;
            });
        }
    }
}
