using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Physics
{
    public class Ray
    {
        public Vector2 Start;
        public Vector2 Translation;

        public Ray(Vector2 start, Vector2 translation)
        {
            Start = start;
            Translation = translation;
        }
    }
}