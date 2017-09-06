using LightGIS_1._0.GeoLayer.GeoRenderer.GeoSymbol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Data;
using System.ComponentModel;
using LightGIS_1._0.GeoLayer;
using LightGIS_1._0.GeoObject;
using System.Drawing.Drawing2D;
using System.Collections;

namespace LightGIS_1._0.Src.GeoLayer.GeoRenderer
{
    public class UniqueValueRenderer : Renderer
    {
        #region 字段
        private String _Field;//绑定字段名称
        private Dictionary<string, Color> _ValueColorDic;//唯一值与符号字典
        private Symbol _DefaultSymbol;//默认符号
        private Layer _Layer;
        private Random rnd = new Random();
        #endregion
        #region 构造函数
        public UniqueValueRenderer(Layer layer)
        {
            this._Layer = layer;
            this._Field = null;
            _ValueColorDic = new Dictionary<string, Color>();
            this.ValueDic = new DictionaryPropertyGridAdapter(_ValueColorDic);
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

        #region propertyGrid

        /// <summary>
        /// 下拉框
        /// </summary>
        private class FieldConverter : StringConverter
        {
            public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                if (context != null && context.Instance is UniqueValueRenderer)
                {
                    List<string> values = new List<string>();
                    DataTable sdt = (context.Instance as UniqueValueRenderer)._Layer.Records;
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


        /// <summary>
        /// 显示字典
        /// </summary>
        public class DictionaryPropertyGridAdapter : ICustomTypeDescriptor
        {
            IDictionary _dictionary;

            public DictionaryPropertyGridAdapter(IDictionary d)
            {
                _dictionary = d;
            }

            public string GetComponentName()
            {
                return TypeDescriptor.GetComponentName(this, true);
            }

            public EventDescriptor GetDefaultEvent()
            {
                return TypeDescriptor.GetDefaultEvent(this, true);
            }

            public string GetClassName()
            {
                return TypeDescriptor.GetClassName(this, true);
            }

            public EventDescriptorCollection GetEvents(Attribute[] attributes)
            {
                return TypeDescriptor.GetEvents(this, attributes, true);
            }

            EventDescriptorCollection System.ComponentModel.ICustomTypeDescriptor.GetEvents()
            {
                return TypeDescriptor.GetEvents(this, true);
            }

            public TypeConverter GetConverter()
            {
                return TypeDescriptor.GetConverter(this, true);
            }

            public object GetPropertyOwner(PropertyDescriptor pd)
            {
                return _dictionary;
            }

            public AttributeCollection GetAttributes()
            {
                return TypeDescriptor.GetAttributes(this, true);
            }

            public object GetEditor(Type editorBaseType)
            {
                return TypeDescriptor.GetEditor(this, editorBaseType, true);
            }

            public PropertyDescriptor GetDefaultProperty()
            {
                return null;
            }

            PropertyDescriptorCollection
                System.ComponentModel.ICustomTypeDescriptor.GetProperties()
            {
                return ((ICustomTypeDescriptor)this).GetProperties(new Attribute[0]);
            }

            public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
            {
                ArrayList properties = new ArrayList();
                foreach (DictionaryEntry e in _dictionary)
                {
                    properties.Add(new DictionaryPropertyDescriptor(_dictionary, e.Key));
                }

                PropertyDescriptor[] props =
                    (PropertyDescriptor[])properties.ToArray(typeof(PropertyDescriptor));

                return new PropertyDescriptorCollection(props);
            }
        }

        public class DictionaryPropertyDescriptor : PropertyDescriptor
        {
            IDictionary _dictionary;
            object _key;

            internal DictionaryPropertyDescriptor(IDictionary d, object key)
                : base(key.ToString(), null)
            {
                _dictionary = d;
                _key = key;
            }

            public override Type PropertyType
            {
                get { return _dictionary[_key].GetType(); }
            }

            public override void SetValue(object component, object value)
            {
                _dictionary[_key] = value;
                //Console.WriteLine("SetValue");
            }

            public override object GetValue(object component)
            {
                return _dictionary[_key];
            }

            public override bool IsReadOnly
            {
                get { return false; }
            }

            public override Type ComponentType
            {
                get { return null; }
            }

            public override bool CanResetValue(object component)
            {
                return false;
            }

            public override void ResetValue(object component)
            {
            }

            public override bool ShouldSerializeValue(object component)
            {
                return false;
            }
        }

        #endregion


        #endregion


        #region 属性

        [Browsable(true), DisplayName("绑定字段"), Description("获取或设置绑定字段")]
        [TypeConverter(typeof(FieldConverter))]
        public String Field
        {
            get { return _Field; }
            set
            {
                if (_Field != value)
                {
                    _Field = value;
                    if (_Field != null)
                    {
                        object sObject;
                        string ss;
                        _ValueColorDic.Clear();
                        foreach (DataRow row in _Layer.Records.Rows)
                        {
                            sObject = row[_Field];
                            if (!Convert.IsDBNull(sObject))
                            {
                                ss = sObject.ToString();
                                if (!_ValueColorDic.ContainsKey(ss))
                                {
                                    _ValueColorDic[ss] = GenerateRandomColor();
                                }
                            }
                        }
                        GenerateRandomColor();
                    }
                    else
                    {
                        _ValueColorDic.Clear();
                    }
                }

            }
        }


        [Browsable(true), DisplayName("颜色方案"), Description("获取或设置颜色方案")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public DictionaryPropertyGridAdapter ValueDic
        {
            get;
        }
        [Browsable(false), DisplayName("颜色方案"), Description("获取或设置颜色方案")]
        public Dictionary<string, Color> ValueColorDic
        {
            get { return _ValueColorDic; }
            set { _ValueColorDic = value; }
        }

        [Browsable(true), DisplayName("默认符号"), Description("获取或设置颜默认符号")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public Symbol DefaultSymbol
        {
            get { return _DefaultSymbol; }
            set { _DefaultSymbol = value; }
        }

        #endregion
        #region 方法
        //增加一个值及相应的符号
        public void AddValue(string value, Color color)
        {
            ValueColorDic[value] = color;
        }
        //根据指定值获取相应颜色
        public Color FindColor(string value)
        {
            Color s;
            if (ValueColorDic.TryGetValue(value, out s))
                return s;
            else
                return DefaultSymbol.SymbolColor;
        }
        ////配置随机颜色
        //public void GenerateRandomColor(double maxH = 359, double minH =0, double maxS = 0.9, double minS = 0.5, double maxV = 0.8, double minV = 0.3)
        //{
        //    foreach (KeyValuePair<string, Color> kvp in _ValueSymbolDic)
        //    {
        //        Random rnd = new Random();
        //        double h = minH + rnd.NextDouble() * (maxH - minH);
        //        double s = minS + rnd.NextDouble() * (maxS - minS);
        //        double v = minV + rnd.NextDouble() * (maxV - minV);
        //        kvp.Value = ColorTranslator.HSB2RGB(h, s, v); ;
        //    }
        //}

        //生成随机颜色
        public Color GenerateRandomColor(double maxH = 359, double minH = 0, double maxS = 0.7, double minS = 0.3, double maxV = 0.8, double minV = 0.5)
        {
            double h = minH + rnd.NextDouble() * (maxH - minH);
            double s = minS + rnd.NextDouble() * (maxS - minS);
            double v = minV + rnd.NextDouble() * (maxV - minV);
            return  ColorTranslator.HSB2RGB(h, s, v); 
        }

        #endregion
    }
}
