using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace LightGIS_1._0.GeoLayer.GeoRenderer.GeoSymbol
{
    public class PolygonSymbol : Symbol
    {
        #region 字段
        private String _Label = "面符号";//符号标签
        //private bool _Visible;//是否可见
                              //     private Color _FillColor;//填充颜色
        private LineSymbol _Outline;//边界样式
        #endregion
        #region 构造函数

        public PolygonSymbol(Color fillColor, LineSymbol outline)
        {
            _c = fillColor;
            _Outline = outline;
        }

        #endregion
        #region 属性
        ///// <summary>
        ///// 获取和设置符号标签
        ///// </summary>
        //public String Label
        //{
        //    get { return _Label; }
        //    set { _Label = value; }
        //}
        ///// <summary>
        ///// 获取和设置可见性
        ///// </summary>
        //public bool Visible
        //{
        //    get { return _Visible; }
        //    set { _Visible = value; }
        //}
        /// <summary>
        /// 获取和设置边界类型
        /// </summary>
        public DashStyle OutlineStyle
        {
            get { return _Outline.LineDashStyle; }
            set { _Outline.LineDashStyle = value; }
        }
        /// <summary>
        /// 获取和设置边界宽度
        /// </summary>
        public float OutlineWidth
        {
            get { return _Outline.Width; }
            set { _Outline.Width = value; }
        }
        /// <summary>
        /// 获取和设置边界颜色
        /// </summary>
        public Color OutlineColor
        {
            get { return _Outline.SymbolColor; }
            set { _Outline.SymbolColor = value; }
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
