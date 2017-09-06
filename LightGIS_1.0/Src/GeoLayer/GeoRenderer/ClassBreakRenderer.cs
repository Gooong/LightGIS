using LightGIS_1._0.GeoLayer.GeoRenderer.GeoSymbol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Data;
using System.ComponentModel;
using System.Collections;
using LightGIS_1._0.GeoLayer;
using LightGIS_1._0.GeoObject;
using System.Drawing.Drawing2D;

namespace LightGIS_1._0.Src.GeoLayer.GeoRenderer
{
    public class ClassBreakRenderer : Renderer
    {
        #region 字段
        private string _Field;//绑定字段名称

        private Symbol _DefaultSymbol;//整体的符号
        private Color _StartColor;//渐变的起始颜色
        private Color _EndColor;//渐变的结束颜色
        private double[] _Breaks;//分割值数组,比符号数组少一
        private Color[] _Colors;//符号数组
        private Layer _Layer;

        #endregion
        #region 构造函数
        public ClassBreakRenderer(Layer layer)
        {
            this._Layer = layer;
            this._Field = null;
            this._StartColor = Color.FromArgb(237, 248, 233);
            this._EndColor = Color.FromArgb(35, 139, 69);

            //生成默认符号
            if (_Layer.GeoType == typeof(PointD))
            {
                _DefaultSymbol = new PointSymbol(PointSymbol.PointStyleConstant.FillCircle, 4, Color.Black);
            }
            else if (_Layer.GeoType == typeof(MultiPolyLine))
            {
                _DefaultSymbol = new LineSymbol(DashStyle.Solid, 2, Color.Black);
            }
            else
            {
                LineSymbol outlineSymbol = new LineSymbol(DashStyle.Solid, 1, Color.Black);
                _DefaultSymbol = new PolygonSymbol(Color.Red, outlineSymbol);
            }

        }
        #endregion
        #region 属性
        /// <summary>
        /// 获取和设置绑定字段
        /// </summary>
        [Browsable(true), DisplayName("绑定字段"), Description("获取或设置样式的绑定字段")]
        [CategoryAttribute("坐标"), TypeConverter(typeof(FieldConverter))]
        public string Field
        {
            get { return _Field; }
            set
            {
                _Field = value;
                if (_Field != null)
                {
                    _Field = value;
                    object sObject;
                    DataColumn sColumn = _Layer.Records.Columns[_Field];

                    //生成Breaks数组
                    double sMin = Double.MaxValue,sMax = Double.MinValue;
                    double sDouble;
                    foreach(DataRow sRow in _Layer.Records.Rows)
                    {
                        sObject = sRow[_Field];
                        if (!Convert.IsDBNull(sObject))
                        {
                            if (sColumn.DataType == typeof(Int32)) { sDouble = (Int32)sObject; }
                            else { sDouble = (double)sObject; }
                            if (sDouble < sMin) { sMin = sDouble; }
                            if(sDouble > sMax) { sMax = sDouble; }
                        }
                    }
                    _Breaks = GetBreaks(sMin, sMax, _Layer.GeoCount);   //生成Breaks
                    GenerateRampColors();//根据起止颜色生成颜色
                }
                else
                {
                    _Breaks = null;
                }
            }
        }

        [Browsable(true), DisplayName("默认符号"), Description("获取或设置样式的默认符号")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public Symbol DefaultSymbol
        {
            get { return _DefaultSymbol; }
        }

        /// <summary>
        /// 获取分割值数组
        /// </summary>
        [Browsable(true), DisplayName("分割值"), Description("获取或设置样式的分割值")]
        public double[] Breaks
        {
            get { return _Breaks; }
        }

        /// <summary>
        /// 获取符号数组
        /// </summary>
        [Browsable(true), DisplayName("颜色组"), Description("获取或设置样式的颜色组")]
        public Color[] Colors
        {
            get { return _Colors; }
        }

        [Browsable(true), DisplayName("起始颜色"), Description("获取或设置样式的起始颜色")]
        public Color StartColor
        {
            get { return _StartColor; }
            set
            {
                _StartColor = value;
                GenerateRampColors();
            }
        }

        [Browsable(true), DisplayName("终止颜色"), Description("获取或设置样式的终止颜色")]
        public Color EndColor
        {
            get { return _EndColor; }
            set
            {
                _EndColor = value;
                GenerateRampColors();
            }
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
                if (context != null && context.Instance is ClassBreakRenderer)
                {
                    List<string> values = new List<string>();
                    DataTable sdt = (context.Instance as ClassBreakRenderer)._Layer.Records;
                    DataColumn sColumn;
                    for (int i = 4; i < sdt.Columns.Count; i++)
                    {
                        sColumn = sdt.Columns[i];
                        if(sColumn.DataType == typeof(Int32) || sColumn.DataType == typeof(double))
                        {
                            values.Add(sdt.Columns[i].ColumnName);
                        }
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


        #region 方法
        //对所有符号生成渐变颜色
        public void GenerateRampColors()
        {
            if(_Breaks !=null && _Breaks.Length != 0)
            {

                int sBreakCount = _Breaks.Length;
                double startH = _StartColor.GetHue();
                double startS = _StartColor.GetSaturation();
                double startB = _StartColor.GetBrightness();
                double endH = _EndColor.GetHue();
                double endS = _EndColor.GetSaturation();
                double endB = _EndColor.GetBrightness();
                double intervalH = (endH - startH) / sBreakCount;
                double intervalS = (endS - startS) / sBreakCount;
                double intervalB = (endB - startB) / sBreakCount;
                _Colors = new Color[sBreakCount + 1];
                _Colors[0] = _StartColor;
                for (int i = 1; i < sBreakCount; i++)
                {
                    _Colors[i] = ColorTranslator.HSB2RGB(startH + i * intervalH, startS + i * intervalS, startB + i * intervalB);
                }
                _Colors[sBreakCount] = _EndColor;
            }
        }
        //根据指定的值，返回相应地符号
        public Color FindColor(double value)
        {
            if (value < Breaks[0])
                return _Colors[0];
            for (int i = 1; i < Breaks.Length; i++)
            {
                if (value < Breaks[i])
                    return _Colors[i];
            }
            return _Colors[Breaks.Length];
        }
        //用户改变某一等级颜色时，修改符号数组对应的颜色
        public void ChangeSymbol()
        {

        }

        //根据最大值和最小值和数据数量划分数组,平均分级
        private double[] GetBreaks(double min, double max,int dataCount)
        {
            if (max < min)
            {
                return new double[] { 0 };
            }
            else if (min  ==  max)
            {
                return new double[] { min-1,max+1 };
            }
            else
            {
                int sCount = 2 + (int)(Math.Log10(dataCount) * 1.4);//breaks的个数
                double[] sBreaks = new double[sCount];
                double h = (max - min) / (sCount + 1);
                for(int i = 0; i < sCount; i++)
                {
                    sBreaks[i] = min + (i+1) * h;
                }
                return sBreaks;
            }

        }

        #endregion
    }
}
