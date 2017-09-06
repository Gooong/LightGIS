using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using LightGIS_1._0.GeoLayer.GeoRenderer;
using LightGIS_1._0.GeoLayer.GeoRenderer.GeoSymbol;

namespace LightGIS_1._0.GeoLayer.GeoRenderer.GeoSymbol
{
    public class PointSymbol : Symbol
    {
        #region 字段
        private String _Label = "点符号";//符号标签
        //private bool _Visible;//是否可见
        private PointStyleConstant _Style;//符号类型
        private float _Size;//符号大小
        #endregion

        #region 构造函数
        public PointSymbol(PointStyleConstant style, float size)
        {
            _Style = style;
            _Size = size;
            _c = Color.Black;
        }
        public PointSymbol(PointStyleConstant style, float size, Color color)
        {
            _Style = style;
            _Size = size;
            _c = color;
        }
        /// <summary>
        /// 点符号类型枚举类
        /// </summary>
        public enum PointStyleConstant
        {
            Circle = 0,
            FillCircle = 1,
            Square = 2,
            FillSquare = 3,
            Triangle = 4,
            FillTriangle = 5,
            Ring = 6,
            FillRing = 7,
        }
        #endregion
        #region 属性
        //public String Label
        //{
        //    get { return _Label; }
        //    set { _Label = value; }
        //}
        //public bool Visible
        //{
        //    get { return _Visible; }
        //    set { _Visible = value; }
        //}
        public PointStyleConstant Style
        {
            get { return _Style; }
            set { _Style = value; }
        }
        public float Size
        {
            get { return _Size; }
            set { _Size = value; }
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
