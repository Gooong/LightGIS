using LightGIS_1._0.GeoLayer.GeoRenderer.GeoSymbol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using LightGIS_1._0.GeoLayer;
using System.ComponentModel;
using System.Data;

namespace LightGIS_1._0.Src.GeoLayer.GeoRenderer.GoeSymbol
{
    public class TextSymbol : Symbol
    {
        #region 字段
        private String _Label = "文字注记";
        private Font _TextFont;//字体
        private String _Field;//绑定的字段
        private float _OffsetX;
        private float _OffsetY;
        private bool _Visible;//是否可见
        private Layer _Layer;
        #endregion


        #region 构造函数

        public TextSymbol(Layer layer)
        {
            this._Layer = layer;
            _TextFont = new Font(FontFamily.GenericSansSerif, 10);
            _c = Color.Black;
            _Field = null;
            _OffsetX = 10;
            _OffsetY = -10;
            _Visible = false;
        }
        #endregion


        #region propertyGrid

        /// <summary>
        /// 下拉框
        /// </summary>
        private class FieldConverter : StringConverter
        {
            public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                if (context != null && context.Instance is TextSymbol)
                {
                    List<string> values = new List<string>();
                    DataTable sdt = (context.Instance as TextSymbol)._Layer.Records;
                    for (int i = 4; i < sdt.Columns.Count; i++)
                    {
                        values.Add(sdt.Columns[i].ColumnName);
                    }
                    return new StandardValuesCollection(values);
                }
                return base.GetStandardValues(context);
            }

            public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
            {
                return true;
            }
            public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
            {
                //true下拉框不可编辑
                return true;
            }
        }
        #endregion


        #region 属性
        [Browsable(true), DisplayName("可见性"), Description("获取或设置文字注记的可见性")]
        public bool Visible
        {
            get { return _Visible; }
            set
            {
                if(_Visible != value)
                {
                    _Visible = value;
                }
            }
        }

        [Browsable(true), DisplayName("字体"), Description("获取或设置文字注记的字体")]
        public Font TextFont
        {
            get { return _TextFont; }
            set { _TextFont = value; }
        }

        [Browsable(true), DisplayName("绑定字段"), Description("获取或设置文字注记的绑定字段")]
        [CategoryAttribute("坐标"), TypeConverter(typeof(FieldConverter))]
        public String Field
        {
            get { return _Field; }
            set { _Field = value; }
        }

        [Browsable(true), DisplayName("水平偏移"), Description("获取或设置文字注记的水平偏移")]
        public float OffsetX
        {
            get { return _OffsetX; }
            set { _OffsetX = value; }
        }

        [Browsable(true), DisplayName("垂直偏移"), Description("获取或设置文字注记的垂直偏移")]
        public float OffsetY
        {
            get { return _OffsetY; }
            set { _OffsetY = value; }
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
