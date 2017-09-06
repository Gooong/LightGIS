using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace LightGIS_1._0.GeoLayer.GeoRenderer.GeoSymbol
{
    public abstract class Symbol
    {
        protected Color _c;
        /// <summary>
        /// 获取或设置符号颜色
        /// </summary>
        public Color SymbolColor
        {
            get { return _c; }
            set { _c = value; }
        }

    }
}
