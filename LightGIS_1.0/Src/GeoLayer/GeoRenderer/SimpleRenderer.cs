using LightGIS_1._0.GeoLayer.GeoRenderer.GeoSymbol;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LightGIS_1._0.Src.GeoLayer.GeoRenderer
{
    public class SimpleRenderer : Renderer
    {
        #region 字段
        private String _Label = "简单渲染";
        private Symbol _Symbol;
        #endregion
        #region 构造函数


        public SimpleRenderer(Symbol symbol)
        {
            _Symbol = symbol;
        }
        #endregion
        #region 属性

        /// <summary>
        /// 获取或设置单一符号
        /// </summary>
        [Browsable(true), DisplayName("符号样式"), Description("获取或设置样式的符号")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public Symbol symbol
        {
            get { return _Symbol; }
            set { _Symbol = value; }
        }

        #endregion


        #region 函数
        public override string ToString()
        {
            return _Label;
        }
        #endregion
    }
}
