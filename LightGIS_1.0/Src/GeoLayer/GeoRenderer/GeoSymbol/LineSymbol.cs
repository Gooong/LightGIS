using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace LightGIS_1._0.GeoLayer.GeoRenderer.GeoSymbol
{
    public class LineSymbol : Symbol
    {
        #region 字段
        private String _Label = "线符号";//符号标签
        //private bool _Visible;//是否可见
        private DashStyle _LineDashStyle;//线型
        private float _Width;//线宽
  //      private Color _color;//绘制颜色
        #endregion
        #region 构造函数
        public LineSymbol(DashStyle style, float width)
        {
            _LineDashStyle = style;
            _Width = width;
            _c = Color.Black;
        }
        public LineSymbol(DashStyle style, float width, Color color)
        {
            _LineDashStyle = style;
            _Width = width;
            _c = color;
        }


        #endregion
        #region 属性
        ///// <summary>
        ///// 获取和设置可见性
        ///// </summary>
        //public bool Visible
        //{
        //    get { return _Visible; }
        //    set { _Visible = value; }
        //}
        /// <summary>
        /// 获取和设置线型
        /// </summary>
        public DashStyle LineDashStyle
        {
            get { return _LineDashStyle; }
            set { _LineDashStyle = value; }
        }
        /// <summary>
        /// 获取和设置线宽
        /// </summary>
        public float Width
        {
            get { return _Width; }
            set { _Width = value; }
        }
        /// <summary>
        /// 获取和设置颜色
        /// </summary>
        #endregion

        #region 函数
        public override string ToString()
        {
            return _Label;
        }
        #endregion

    }
}
