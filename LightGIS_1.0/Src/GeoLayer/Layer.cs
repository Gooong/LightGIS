using LightGIS_1._0.GeoMap;
using LightGIS_1._0.GeoObject;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using LightGIS_1._0.GeoLayer.GeoRenderer.GeoSymbol;
using LightGIS_1._0.Src.GeoLayer.GeoRenderer;
using System.Drawing;
using System.Drawing.Drawing2D;
using static LightGIS_1._0.Src.GeoLayer.GeoRenderer.Renderer;
using LightGIS_1._0.Src.GeoLayer.GeoRenderer.GoeSymbol;

namespace LightGIS_1._0.GeoLayer
{
    public class Layer
    {
        #region 常量
        private const int __MAX_STR_LENGTH = 255;//字符串属性最长长度
        private const int __MXA_COlUMN_NAME_LENGTH = 11;//字段名最长长度
        public const int FIELD_START_INDEX = 4;//属性起始列

        private const String __ID_COLUMN_NAME = "_ID_";
        private const String __GEO_COLUMN_NAME = "_GEO_DATA_";
        private const String __PRJ_COLUMN_NAME = "_PRJ_DATA_";
        private const String __TEXT_LABEL_COLUMN_NAME = "_TEXT_LABEL_DATA_";
        #endregion

        #region 字段
        string _FilePath;//图层shp文件存储位置
        DataTable _DT;//存储图层的要素和属性,用AcceptChange保存更改
        List<DataRow> _SelectedRecords = new List<DataRow>();//当前选中的要素，不包含正在编辑的要素
        List<DataRow> _EditingRecords = new List<DataRow>();//当前正在编辑的要素，必须要保存行

        Type _GeoType;
        TreeNode _Node; //图层节点

        //GeoIndex _Index;//图层空间索引
        RectangleD _MBR;//图层最小外包矩形，单位是地理坐标
        RectangleD _PRJMBR;//图层最小外包矩形，单位是投影坐标

        SimpleRenderer _SimpleRenderer;//简单渲染
        UniqueValueRenderer _UniqueValueRenderer;//唯一值渲染
        ClassBreakRenderer _ClassBreakRenderer;//分级渲染
        Renderer _Renderer;//当前的图层渲染
        TextSymbol _TextLabel;//文字标注

        RenderMethod _RenderMethod = RenderMethod.SimpleRender;//渲染方法枚举

        string _Description;//图层描述
        //bool _Editing = false;//图层是否正在被编辑
        bool _ReadOnly = false;//图层是否可编辑

        #endregion


        #region 构造函数

        /// <summary>
        /// 静态构造函数
        /// </summary>
        /// <param name="name">图层名称</param>
        /// <param name="geoType">图层类型</param>
        /// <returns>已初始化的图层，类型不对则返回null</returns>
        public static Layer CreateLayer(string name, Type geoType,string filePath)
        {
            if (geoType == typeof(PointD) || geoType == typeof(MultiPolyLine) || geoType == typeof(MultiPolygon))
            {
                Layer sLayer = new Layer();
                
                sLayer._FilePath = filePath;
                sLayer._DT = new DataTable(name);
                sLayer._GeoType = geoType;
                sLayer._Node = new TreeNode(name);
                sLayer._Node.Tag = sLayer;   //Tag设置成layer
                sLayer._Node.Checked = true;

                //加入ID列
                DataColumn mID = new DataColumn(__ID_COLUMN_NAME, typeof(int));
                mID.Unique = true;
                mID.AllowDBNull = false;
                mID.AutoIncrementSeed = 1;
                mID.AutoIncrement = true;
                sLayer._DT.Columns.Add(mID);

                //加入要素列，存地理坐标
                DataColumn mGeoData = new DataColumn(__GEO_COLUMN_NAME, geoType);
                mGeoData.AllowDBNull = true;
                sLayer._DT.Columns.Add(mGeoData);

                //加入要素列，存投影坐标
                DataColumn mPrjData = new DataColumn(__PRJ_COLUMN_NAME, geoType);
                mPrjData.AllowDBNull = true;
                sLayer._DT.Columns.Add(mPrjData);

                //加入要素列，存储注记位置
                DataColumn mTextLabeldData = new DataColumn(__TEXT_LABEL_COLUMN_NAME, typeof(PointD));
                mTextLabeldData.AllowDBNull = true;
                mTextLabeldData.DefaultValue = null;
                sLayer._DT.Columns.Add(mTextLabeldData);

                if (geoType == typeof(PointD))
                {
                    Symbol symbol = new PointSymbol(PointSymbol.PointStyleConstant.FillCircle, 4, Color.Black);
                    sLayer._SimpleRenderer = new SimpleRenderer(symbol);
                }
                else if (geoType == typeof(MultiPolyLine))
                {
                    Symbol symbol = new LineSymbol(DashStyle.Solid, 2, Color.Black);
                    sLayer._SimpleRenderer = new SimpleRenderer(symbol);
                }
                else
                {
                    LineSymbol outlineSymbol = new LineSymbol(DashStyle.Solid, 1, Color.Black);
                    Symbol symbol = new PolygonSymbol(Color.Red, outlineSymbol);
                    sLayer._SimpleRenderer = new SimpleRenderer(symbol);
                }
                sLayer._Renderer = sLayer._SimpleRenderer;  //设置默认Renderer
                sLayer._UniqueValueRenderer = new UniqueValueRenderer(sLayer);
                sLayer._ClassBreakRenderer = new ClassBreakRenderer(sLayer);
                sLayer._TextLabel = new TextSymbol(sLayer);

                return sLayer;
            }

            else
            {
                return null;
            }
        }

        private Layer() { }
        #endregion

        #region 事件
        //新增行会导致RowChanged,但不会导致ColumnChanged
        //新增Column不会导致RowChanged与ColumnChanged
        //值的更改会导致RowChanged和ColumnChanged



        ////投影坐标改变了，需要Map计算相应的GeoData
        //internal delegate void NeedToRefreshGeoDataHandle(object sender, DataRow dataRow);
        //internal event NeedToRefreshGeoDataHandle NeedToRefreshGeoData;

        //图层要素数目改了，需要重新绘制地图
        internal delegate void RecordsChangedHandle(object sender);
        internal event RecordsChangedHandle RecordsChanged;

        //图层可见性更改了，需要重新绘制地图并且TreeView也需要变
        internal delegate void VisiblityChangedHandle(object sender, bool visible);
        internal event VisiblityChangedHandle VisiblityChanged;

        //图层样式更改了，需要重新绘制一级地图，并刷新PropertyGrid
        internal delegate void SymbolChangedHandle(object sender);
        internal event SymbolChangedHandle SymbolChanged;

        #endregion

        #region 属性

        /// <summary>
        /// 图层的名称
        /// </summary>
        [Browsable(true), Category("基本"), DisplayName("图层名称"),Description("获取或设置图层名称")]
        public string Name
        {
            get { return _DT.TableName; }
            set
            {
                if(value!=null && value != _DT.TableName)
                {
                    try
                    {
                        _DT.TableName = value;
                        _Node.Text = value;
                        _Node.Name = value;
                    }
                    catch
                    {
                        ;
                    }
                }

            }
        }
        /// <summary>
        /// 图层包含的要素个数
        /// </summary>
        [Browsable(true), Category("基本"), DisplayName("要素数量"), Description("获取图层要素个数")]
        public int GeoCount
        {
            get { return _DT.Rows.Count; }
        }


        [Browsable(true), Category("基本"), DisplayName("存储路径"), Description("获取图层存储位置")]
        public string FilePath
        {
            get { return _FilePath; }
        }

        /// <summary>
        /// 图层在地图中的可见性
        /// </summary>
        [Browsable(true), Category("基本"), DisplayName("可见性"), Description("获取或更改图层在地图中的可见性")]
        public bool Visible
        {
            get { return _Node.Checked; }
            set
            {
                if (_Node.Checked != value)
                {
                    _Node.Checked = value;
                    VisiblityChanged?.Invoke(this, _Node.Checked);
                }
            }
        }


        /// <summary>
        /// 图层描述
        /// </summary>
        [Browsable(true), Category("基本"), DisplayName("描述"), Description("获取或更改图层描述")]
        public string Description
        {
            get { return _Description; }
            set { _Description = value; }
        }

        /// <summary>
        /// 图层的要素类型
        /// </summary>
        [Browsable(false)]
        public Type GeoType
        {
            get { return _GeoType; }
        }

        /// <summary>
        /// 图层要素集合
        /// </summary>
        [Browsable(false)]
        public DataTable Records
        {
            get { return _DT; }
        }



        /// <summary>
        /// 图层样式
        /// </summary>
        [Browsable(true), Description("获取或设置图层样式"), Category("样式"), DisplayName("图层样式")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public Renderer LayerRenderer
        {
            get { return _Renderer; }
        }

        [Browsable(true), Description("获取或设置图层样式"), Category("样式"), DisplayName("样式类别")]
        public RenderMethod LayerRenderMethod
        {
            get { return _RenderMethod; }
            set
            {
                if (_RenderMethod != value)
                {
                    _RenderMethod = value;
                    switch (_RenderMethod)
                    {
                        case RenderMethod.SimpleRender:
                            _Renderer = _SimpleRenderer;
                            break;
                        case RenderMethod.UniqueValueRender:
                            _Renderer = _UniqueValueRenderer;
                            break;
                        case RenderMethod.ClassBreakRender:
                            _Renderer = _ClassBreakRenderer;
                            break;
                    }
                    SymbolChanged?.Invoke(this);
                }
            }
        }


        [Browsable(true), Description("获取或设置图层注记"), Category("样式"), DisplayName("图层注记")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public TextSymbol TextLabel
        {
            get { return _TextLabel; }
        }

        [Browsable(false)]
        public SimpleRenderer LayerSimpleRenderer
        {
            get { return _SimpleRenderer; }
            //set { _SimpleRenderer = value; }
        }
        [Browsable(false)]
        public UniqueValueRenderer LayerUniqueValueRenderer
        {
            get { return _UniqueValueRenderer; }
            //set { _UniqueValueRenderer = value; }
        }
        [Browsable(false)]
        public ClassBreakRenderer LayerClassBreakRenderer
        {
            get { return _ClassBreakRenderer; }
            //set { _ClassBreakRenderer = value; }
        }



        /// <summary>
        /// 图层最小外包矩形
        /// </summary>
        [Browsable(false)]
        public RectangleD MBR
        {
            get { return _MBR; }
            set { _MBR = value; }
        }

        /// <summary>
        /// 图层最小外包矩形
        /// </summary>
        [Browsable(false)]
        public RectangleD PRJMBR
        {
            get { return _PRJMBR; }
            set { _PRJMBR = value; }
        }



        /// <summary>
        /// 当前选中的要素，不包含正在编辑的要素
        /// </summary>
        [Browsable(false)]
        public List<DataRow> SelectedRecords {
            get { return _SelectedRecords; }
            set { _SelectedRecords = value; }
        }

        /// <summary>
        /// 当前正在编辑的要素
        /// </summary>
        [Browsable(false)]
        public List<DataRow> EditingRecord
        {
            get { return _EditingRecords; }
            set { _EditingRecords = value; }
        }


        /// <summary>
        /// 获取或设置图层当前是否可以被编辑
        /// </summary>
        [Browsable(false)]
        public bool ReadOnly
        {
            get { return _ReadOnly; }
            set
            {
                _ReadOnly = value;
                foreach(DataColumn mColumn in _DT.Columns)
                {
                    mColumn.ReadOnly = value;
                }
            }
        }

        [Browsable(false)]
        public TreeNode Node
        {
            get { return _Node; }
        }

        #endregion


        #region 方法

        /// <summary>
        /// 增加字段
        /// </summary>
        /// <param name="name">字段名称（长度小于11）</param>
        /// <param name="type">字段类型</param>
        /// <returns>是否创建成功</returns>
        public bool AddField(string name,Type type)
        {
            if((type == typeof(Int32)) || (type == typeof(Double)) || (type == typeof(String)) || type == typeof(DateTime))
            {
                if (name.Length <= __MXA_COlUMN_NAME_LENGTH)
                {
                    DataColumn mData = new DataColumn(name, type);
                    mData.AllowDBNull = true;
                    _DT.Columns.Add(mData);
                    if(type == typeof(String))
                    {
                        mData.MaxLength = __MAX_STR_LENGTH;
                    }

                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 删除字段
        /// </summary>
        /// <param name="index">字段索引</param>
        /// <returns>是否删除成功</returns>
        public bool DeleteField(int index)
        {
            if (index >= FIELD_START_INDEX && index < _DT.Columns.Count)
            {
                DataColumn sColumn = _DT.Columns[index];
                if(sColumn.ColumnName == _TextLabel.Field)
                {
                    _TextLabel.Field = null;
                }
                if (sColumn.ColumnName == _ClassBreakRenderer.Field)
                {
                    _ClassBreakRenderer.Field = null;
                }
                if (sColumn.ColumnName == _UniqueValueRenderer.Field)
                {
                    _UniqueValueRenderer.Field = null;
                }

                _DT.Columns.RemoveAt(index);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 添加要素
        /// </summary>
        /// <param name="values">要素实体和属性</param>
        /// <returns>是否添加成功</returns>
        public bool AddRecord(Geometry geometry)
        {
            try
            {
                _DT.Rows.Add(null, null, geometry);
                return true;
            }
            catch
            {
                throw new Exception("添加要素失败");
            }
        }

        /// <summary>
        /// 删除要素
        /// </summary>
        /// <param name="row">要素</param>
        public void DeleteRecord(DataRow row)
        {
            _DT.Rows.Remove(row);
        }

        /// <summary>
        /// 修改字段名称
        /// </summary>
        /// <param name="name">新字段名称</param>
        /// <param name="index">字段索引</param>
        /// <returns></returns>
        public bool EditFieldName(string name, int index)
        {
            if(index>=FIELD_START_INDEX && index <_DT.Columns.Count && name.Length < __MXA_COlUMN_NAME_LENGTH)
            {
                _DT.Columns[index].ColumnName = name;
                return true;
            }
            return false;
        }


        /// <summary>
        /// 点选要素
        /// </summary>
        /// <param name="point">点</param>
        /// <param name="tolerance">容限</param>
        /// <returns>此次点击选中的要素</returns>
        public List<DataRow> SelectByPoint(PointD point, double tolerance)
        {
            //List<DataRow> mSelectedRows = _Index.SelectByPoint(point, tolerance);
            List<DataRow> mSelectedRows = new List<DataRow>();
            if(this._GeoType == typeof(MultiPolygon))
            {
                MultiPolygon sMul;
                foreach(DataRow mRow in this._DT.Rows)
                {
                    if (!Convert.IsDBNull(mRow[2]))
                    {
                        sMul = (MultiPolygon)mRow[2];
                        if (LayerTools.IsPointInMultiPolygon(point, sMul))
                        {
                            mSelectedRows.Add(mRow);
                        }
                    }
                }
            }
            else if (this._GeoType == typeof(MultiPolyLine))
            {
                MultiPolyLine sMul;
                foreach (DataRow mRow in this._DT.Rows)
                {
                    if (!Convert.IsDBNull(mRow[2]))
                    {
                        sMul = (MultiPolyLine)mRow[2];
                        if (LayerTools.IsPointOnMulPolyline(point, sMul,tolerance))
                        {
                            mSelectedRows.Add(mRow);
                        }
                    }
                }
            }
            else if (this._GeoType == typeof(PointD))
            {
                PointD sPoint;
                foreach (DataRow mRow in this._DT.Rows)
                {
                    if (!Convert.IsDBNull(mRow[2]))
                    {
                        sPoint = (PointD)mRow[2];
                        if (LayerTools.IsPointOnPoint(point,sPoint,tolerance))
                        {
                            mSelectedRows.Add(mRow);
                        }
                    }
                }
            }


            //HashSet<DataRow> mSet = _Index.SelectByPoint(point);
            //if (this._GeoType == typeof(MultiPolygon))
            //{
            //    MultiPolygon sMultiPolygon;
            //    if (mSet != null)
            //    {
            //        foreach (DataRow row in mSet)
            //        {
            //            sMultiPolygon = row.Field<MultiPolygon>(2);
            //            if (sMultiPolygon != null)
            //            {
            //                if (LayerTools.IsPointInMultiPolygon(point, sMultiPolygon))
            //                {
            //                    mSelectedRows.Add(row);
            //                }
            //                //Console.WriteLine("search");
            //            }
            //        }
            //    }

            //}
            //_SelectedRecords = mSelectedRows;
            return mSelectedRows;
        }

        /// <summary>
        /// 框选要素，需要考虑框的宽度为0的情况
        /// </summary>
        /// <param name="rect">选择框</param>
        /// <returns>选到的要素</returns>
        public List<DataRow> SelectByBox(RectangleD rect)
        {
            List<DataRow> mSelectedRows = new List<DataRow>();
            if (this._GeoType == typeof(MultiPolygon))
            {
                MultiPolygon sMul;
                foreach (DataRow mRow in this._DT.Rows)
                {
                    if (!Convert.IsDBNull(mRow[2]))
                    {
                        sMul = (MultiPolygon)mRow[2];
                        if (LayerTools.IsRectInterMultipolygon(sMul,rect))
                        {
                            mSelectedRows.Add(mRow);
                        }
                    }
                }
            }
            else if (this._GeoType == typeof(MultiPolyLine))
            {
                MultiPolyLine sMul;
                foreach (DataRow mRow in this._DT.Rows)
                {
                    if (!Convert.IsDBNull(mRow[2]))
                    {
                        sMul = (MultiPolyLine)mRow[2];
                        if (LayerTools.IsRectInterMultiPolyline(sMul,rect))
                        {
                            mSelectedRows.Add(mRow);
                        }
                    }
                }
            }
            else if (this._GeoType == typeof(PointD))
            {
                PointD sPoint;
                foreach (DataRow mRow in this._DT.Rows)
                {
                    if (!Convert.IsDBNull(mRow[2]))
                    {
                        sPoint = (PointD)mRow[2];
                        if (LayerTools.IsPointInRect(sPoint,rect))
                        {
                            mSelectedRows.Add(mRow);
                        }
                    }
                }
            }
            return mSelectedRows;
        }
  

        public static PointD GetClosePoint(PointD point, List<DataRow> rowList, double tolerance)
        {
            PointD sPoint = null;
            foreach(DataRow sRow in rowList)
            {
                if (!Convert.IsDBNull(sRow[2]))
                {
                    sPoint = LayerTools.GetClosePoint(point, (Geometry)sRow[2], tolerance);
                    if (sPoint != null) { return sPoint; }
                }
            }
            return null;
        }

        /// <summary>
        /// 添加要素，初始填充数据不用这个
        /// </summary>
        /// <param name="mDataRow"></param>
        public void AddRecord(DataRow mDataRow)
        {
            _DT.Rows.Add(mDataRow);
            //_Index.AddRecord(mDataRow);
            RecordsChanged?.Invoke(this);
        }

        /// <summary>
        /// 移除要素
        /// </summary>
        /// <param name="mDataRow"></param>
        public void RemoveRecord(DataRow dataRow)
        {
            _DT.Rows.Remove(dataRow);
            //_Index.DeleteRecord(dataRow);
            RecordsChanged?.Invoke(this);
        }

        /// <summary>
        /// 移除多个要素
        /// </summary>
        /// <param name="dataRows">需要移除的要素List</param>
        public void ReomveRecords(List<DataRow> dataRows)
        {
            foreach(DataRow mDataRow in dataRows)
            {
                _DT.Rows.Remove(mDataRow);
                //_Index.DeleteRecord(mDataRow);
            }
            RecordsChanged?.Invoke(this);
        }


        /// <summary>
        /// 计算地理索引
        /// </summary>
        public void GenerateIndex()
        {
            //计算索引
            //_Index = new GeoIndex(this);
        }
       
        
        #endregion
    }
}
