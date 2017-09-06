using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightGIS_1._0.GeoObject
{
    public abstract class Geometry
    {
        public virtual RectangleD MBR
        {
            get;
        }
    }
}
