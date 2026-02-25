using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Models
{
    public struct FRect
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;

        public FRect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}