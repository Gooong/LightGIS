using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightGIS_1._0.Src.GeoLayer.GeoRenderer
{
    public abstract class Renderer
    {
        public enum RenderMethod
        {
            SimpleRender = 0,//简单渲染
            UniqueValueRender = 1,//唯一值渲染
            ClassBreakRender = 2//分级渲染
        }
    }
}
