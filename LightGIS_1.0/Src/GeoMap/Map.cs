using LightGIS_1._0.GeoLayer;
using LightGIS_1._0.GeoObject;
using LightGIS_1._0.Src.DataIO;
using LightGIS_1._0.Src.GeoMap;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LightGIS_1._0.GeoMap
{
    public class Map
    {
        #region 常量
        private const string _DEFAULT_NAME = "默认地图";
        #endregion

        #region 字段
        private string _Name;
        private string _Description;
        private string _FilePath;
        private ProjectedCoordinateSystem _PrjSystem;

        private List<Layer> _Layers;//图层列表
        private DataSet _DataSet;   //所有图层数据

        private bool _IsEditing;//地图是否有图层在被编辑
        private Layer _EditingLayer;//正在编辑的图层
        private TreeNode _TreeNode; //地图结构树


        #endregion

        #region 构造函数
        public Map(string name = _DEFAULT_NAME, string description = "无描述")
        {
            _Name = name;
            _PrjSystem = new ProjectedCoordinateSystem();
            _PrjSystem.ProjectedSystemChanged += _PrjSystem_ProjectedSystemChanged;
            _Description = description;

            _Layers = new List<Layer>();
            _DataSet = new DataSet();

            _TreeNode = new TreeNode(name);
            _TreeNode.Checked = true;
            _TreeNode.Tag = this;
            //_EditingRecord = new List<DataRow>();
            //Console.WriteLine("haha");
        }

        #endregion


        #region 属性
        [Browsable(true), Description("获取或设置地图名称"), Category("基本"), DisplayName("名称")]
        public string Name
        {
            get { return _Name; }
            set
            {
                if(value !=null && value != "")
                {
                    _Name = value;
                    _TreeNode.Text = value;
                    _TreeNode.Name = value;
                }
            }
        }

        [Browsable(true), Description("获取或设置地图描述"), Category("基本"), DisplayName("描述")]
        public string Description
        {
            get { return _Description; }
            set { _Description = value; }
        }

        [Browsable(true), Description("获取或设置地图存储路径"), Category("基本"), DisplayName("路径")]
        public string FilePath
        {
            get { return _FilePath; }
            set { _FilePath = value; }
        }

        [Browsable(true), Description("获取或设置地图可见性"), Category("基本"), DisplayName("可见性")]
        public bool Visible
        {
            get { return _TreeNode.Checked; }
            set { _TreeNode.Checked = value; }
        }

        [Browsable(true), Description("获取或设置地图的图层数量"), Category("基本"), DisplayName("图层数量")]
        public int Count
        {
            get { return _Layers.Count; }
        }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        [Browsable(true), Description("获取或设置地图投影"), Category("基本"), DisplayName("地图投影")]
        public ProjectedCoordinateSystem PrjSystem
        {
            get { return _PrjSystem; }
        }

        [Browsable(false)]
        public List<Layer> Layers
        {
            get { return _Layers; }
        }
        [Browsable(false)]
        public Layer EditingLayer
        {
            get { return _EditingLayer; }
        }
        [Browsable(false)]
        public bool Editing
        {
            get { return _IsEditing; }
        }
        [Browsable(false)]
        public TreeNode MapTree
        {
            get { return _TreeNode; }
        }
        [Browsable(false)]
        public DataSet Tables
        {
            get { return _DataSet; }
        }

        #endregion


        #region 方法
        /// <summary>
        /// 添加图层
        /// </summary>
        /// <param name="layer">图层</param>
        public void AddLayer(Layer layer)
        {
            //计算投影
            if (layer.GeoType == typeof(PointD))
            {
                PointD sPoint;
                foreach (DataRow mRow in layer.Records.Rows)
                {
                    sPoint = mRow.Field<PointD>(1);
                    if (sPoint != null)
                    {
                        mRow[2] = _PrjSystem.ToProjCo(sPoint);
                    }
                }
            }
            else if (layer.GeoType == typeof(MultiPolyLine))
            {
                MultiPolyLine sMultiPolyLine;
                foreach (DataRow mRow in layer.Records.Rows)
                {
                    sMultiPolyLine = mRow.Field<MultiPolyLine>(1);
                    if (sMultiPolyLine != null)
                    {
                        mRow[2] = _PrjSystem.ToProjCo(sMultiPolyLine);
                    }
                }
            }
            else if (layer.GeoType == typeof(MultiPolygon))
            {
                MultiPolygon sMultiPolygon;
                foreach (DataRow mRow in layer.Records.Rows)
                {
                    sMultiPolygon = mRow.Field<MultiPolygon>(1);
                    if (sMultiPolygon != null)
                    {
                        mRow[2] = _PrjSystem.ToProjCo(sMultiPolygon);
                    }
                }
            }

            Geometry sGeo;
            foreach (DataRow mRow in layer.Records.Rows)
            {
                if (!Convert.IsDBNull(mRow[2]))
                {
                    sGeo = (Geometry)mRow[2];
                    mRow[3] = LayerTools.GetCenterPoint(sGeo);
                }
            }
            if (layer.MBR == null)
            {
                layer.MBR = LayerTools.GetLayerGeoMBR(layer);
            }

            layer.PRJMBR = _PrjSystem.ToProjCo(layer.MBR);
            layer.Records.AcceptChanges();//保存更改
            layer.GenerateIndex();//计算地理索引

            //将地图数据锁住，不允许修改
            layer.ReadOnly = true;


            layer.VisiblityChanged += Layer_VisiblityChanged;
            layer.SymbolChanged += Layer_SymbolChanged;
            layer.RecordsChanged += Layer_RecordsChanged;

            _Layers.Insert(0, layer);
            if (_DataSet.Tables.Contains(layer.Name))
            {
                layer.Name += "_";
            }
            _DataSet.Tables.Add(layer.Records);
            _TreeNode.Nodes.Insert(0, layer.Node);

            LayerAdded?.Invoke(this, layer);//通知外部已经添加了图层
            MapPerformaceChanged?.Invoke(this);//重绘一级地图
        }


        /// <summary>
        /// 移除图层
        /// </summary>
        /// <param name="layer">图层</param>
        public void RemoveLayer(Layer layer)
        {
            int index = _Layers.IndexOf(layer);
            _Layers.Remove(layer);
            _DataSet.Tables.Remove(layer.Records);
            _TreeNode.Nodes.Remove(layer.Node);

            LayerRemoved?.Invoke(this, layer);
            MapPerformaceChanged?.Invoke(this);//重绘一级地图
        }

        /// <summary>
        /// 改变图层顺序
        /// </summary>
        /// <param name="layer">图层</param>
        /// <param name="index">新位置索引</param>
        public void ChangeLayerSequence(Layer layer, int index)
        {
            int mOldIndex = _Layers.IndexOf(layer);
            if (mOldIndex == index)
            {
                return;
            }
            else if (mOldIndex > index)
            {
                _Layers.Remove(layer);
                _Layers.Insert(index, layer);
            }
            else
            {
                _Layers.Insert(index, layer);
                _Layers.Remove(layer);
            }

            LayerSequenceChanged?.Invoke(this, layer);
            MapPerformaceChanged?.Invoke(this);//重绘一级地图
        }

        /// <summary>
        /// 从上往下点选图层，选到了之后就停止往下选
        /// </summary>
        /// <param name="point">选择点</param>
        /// <param name="tolerance">容限，单位为投影距离</param>
        public void SelectByPoint(PointD point, double tolerance)
        {
            ClearSelectedRecords();
            foreach (Layer mLayer in _Layers)
            {
                if (mLayer.Visible)
                {
                    mLayer.SelectedRecords = mLayer.SelectByPoint(point, tolerance);
                    if (mLayer.SelectedRecords.Count != 0)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 框选，选中的每个要素存在每个Layer的SelectedRecord中
        /// </summary>
        /// <param name="rect">选择框</param>
        public void SelectByBox(RectangleD rect)
        {
            ClearSelectedRecords();
            foreach (Layer mLayer in _Layers)
            {
                if (mLayer.Visible)
                {
                    mLayer.SelectedRecords = mLayer.SelectByBox(rect);
                }
            }
        }

        /// <summary>
        /// 清除选中的要素
        /// </summary>
        private void ClearSelectedRecords()
        {
            foreach (Layer mLayer in _Layers)
            {
                mLayer.SelectedRecords.Clear();
            }
        }


        #region 地图编辑方法


        /// <summary>
        /// 开始编辑地图
        /// </summary>
        /// <param name="layer">要编辑的图层</param>
        public void StartEditMap(Layer layer)
        {

            _EditingLayer = layer;
            layer.ReadOnly = false;
            _IsEditing = true;
            ClearSelectedRecords();//清除地图中已选中的要素
        }

        /// <summary>
        /// 设置要编辑的图层
        /// </summary>
        /// <param name="layer">要编辑的图层</param>
        public void SetEditLayer(Layer layer)
        {
            TmpSaveEdit();//原有图层要素保存
            _EditingLayer = layer;
            layer.ReadOnly = false;
        }

        /// <summary>
        /// 暂时保存编辑内容,将正在编辑的要素保存到原有要素，并计算地理坐标
        /// </summary>
        public void TmpSaveEdit()
        {
            if (_IsEditing)
            {
                if(_EditingLayer.GeoType == typeof(PointD))
                {
                    foreach (DataRow sRow in _EditingLayer.EditingRecord)
                    {
                        if (!Convert.IsDBNull(sRow[2]))
                        {
                            sRow[1] = _PrjSystem.ToLngLat((PointD)sRow[2]);
                            sRow[3] = ((PointD)sRow[2]).Clone();
                        }
                        else
                        {
                            sRow[1] = null;
                        }
                    }
                }
                else if (_EditingLayer.GeoType == typeof(MultiPolyLine))
                {
                    foreach (DataRow sRow in _EditingLayer.EditingRecord)
                    {
                        if (!Convert.IsDBNull(sRow[2]))
                        {
                            sRow[1] = _PrjSystem.ToLngLat((MultiPolyLine)sRow[2]);
                            sRow[3] = LayerTools.GetCenterPoint((MultiPolyLine)sRow[2]);
                        }
                        else
                        {
                            sRow[1] = null;
                        }
                    }
                }
                else if (_EditingLayer.GeoType == typeof(MultiPolygon))
                {
                    foreach (DataRow sRow in _EditingLayer.EditingRecord)
                    {
                        if (!Convert.IsDBNull(sRow[2]))
                        {
                            sRow[1] = _PrjSystem.ToLngLat((MultiPolygon)sRow[2]);
                            sRow[3] = LayerTools.GetCenterPoint((MultiPolygon)sRow[2]);
                        }
                        else
                        {
                            sRow[1] = null;
                        }
                    }
                }

                _EditingLayer.EditingRecord.Clear();
            }
        }

        /// <summary>
        /// 撤销并停止编辑
        /// </summary>
        public void CancleEditMap()
        {
            TmpSaveEdit();
            foreach (Layer mLayer in _Layers)
            {
                if (!mLayer.ReadOnly)
                {
                    mLayer.Records.RejectChanges();
                    mLayer.ReadOnly = true;
                }
            }
            _IsEditing = false;
            _EditingLayer = null;
        }

        /// <summary>
        /// 保存前面的编辑内容
        /// </summary>
        public void SavePreviousEdit()
        {
            TmpSaveEdit();
            foreach (Layer mLayer in _Layers)
            {
                if (!mLayer.ReadOnly)
                {
                    mLayer.Records.AcceptChanges();
                }
            }
        }

        /// <summary>
        /// 保存并停止编辑
        /// </summary>
        public void FinishEditMap()
        {
            TmpSaveEdit();
            foreach (Layer mLayer in _Layers)
            {
                if (!mLayer.ReadOnly)
                {
                    mLayer.Records.AcceptChanges();
                    mLayer.ReadOnly = true;
                    DataIO.SaveShp(mLayer, mLayer.FilePath);
                }
            }
            _IsEditing = false;
            _EditingLayer = null;
        }


        /// <summary>
        /// 编辑时点选要素，此时需要将上次选中的要素还回，并移除选中的要素
        /// </summary>
        /// <param name="point">点</param>
        /// <param name="tolerance">容限</param>
        /// <returns>要素列表</returns>
        public List<Geometry> EditingSelectByPoint(PointD point, double tolerance)
        {
            List<Geometry> selectGeometry = new List<Geometry>();
            if (!_IsEditing) { return selectGeometry; }

            List<DataRow> mSelectedRows = _EditingLayer.SelectByPoint(point, tolerance);
            if (_EditingLayer.EditingRecord.Count != 0 || mSelectedRows.Count != 0)
            {
                //原有集合里要素保存回去
                TmpSaveEdit();

                //复制一份，将自己变成赋值的，原有的交给外界
                if(_EditingLayer.GeoType == typeof(PointD))
                {
                    PointD sPoint,sClonePoint;
                    foreach(DataRow sRow in mSelectedRows)
                    {
                        if (!Convert.IsDBNull(sRow[2]))
                        {
                            sPoint = sRow.Field<PointD>(2);
                            sClonePoint = sPoint.Clone();

                            sRow[2] = sClonePoint;
                            selectGeometry.Add(sPoint);
                            _EditingLayer.EditingRecord.Add(sRow);
                        }
                    }    
                }
                else if (_EditingLayer.GeoType == typeof(MultiPolyLine))
                {
                    MultiPolyLine sMul, sCloneMul;
                    foreach (DataRow sRow in mSelectedRows)
                    {
                        if (!Convert.IsDBNull(sRow[2]))
                        {
                            sMul = sRow.Field<MultiPolyLine>(2);
                            sCloneMul = sMul.Clone();

                            sRow[2] = sCloneMul;
                            selectGeometry.Add(sMul);
                            _EditingLayer.EditingRecord.Add(sRow);
                        }
                    }
                }
                else
                {
                    MultiPolygon sMul, sCloneMul;
                    foreach (DataRow sRow in mSelectedRows)
                    {
                        if (!Convert.IsDBNull(sRow[2]))
                        {
                            sMul = sRow.Field<MultiPolygon>(2);
                            sCloneMul = sMul.Clone();

                            sRow[2] = sCloneMul;
                            selectGeometry.Add(sMul);
                            _EditingLayer.EditingRecord.Add(sRow);
                        }
                    }
                }
            }
            return selectGeometry;
        }

        public void AddGeometry(Layer layer, List<Geometry> gList,Type geoType)
        {
            if (_IsEditing && !layer.ReadOnly)
            {
                if(gList!=null && geoType == layer.GeoType)
                {
                    foreach(Geometry geo in gList)
                    {
                        layer.Records.Rows.Add(null,geo, _PrjSystem.ToProjCo(geo), LayerTools.GetCenterPoint(geo));
                    }
                }
            }
        }
        #endregion

        #endregion

        #region 事件

        /// <summary>
        /// 加入图层事件，此时图层栏需要加入图层
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="layer"></param>
        internal delegate void LayerAddedHandle(object sender, Layer layer);
        internal event LayerAddedHandle LayerAdded;

        /// <summary>
        /// 删除图层事件，可能属性栏需要改变，此时地图一级图层需要重新绘制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="layer"></param>
        internal delegate void LayerRemovedHandle(object sender, Layer layer);
        internal event LayerRemovedHandle LayerRemoved;

        /// <summary>
        /// 图层序列改变，此时地图一级图层需要重新绘制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="layer"></param>
        internal delegate void LayerSequenceChangedHandle(object sender, Layer layer);
        internal event LayerSequenceChangedHandle LayerSequenceChanged;

        /// <summary>
        /// 图层可见性改变，treeView，propretyGrid和地图需要对应，此时地图一级图层需要重新绘制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="layer"></param>
        internal delegate void LayerVisiblityChangedHandle(object sender, Layer layer);
        //internal event LayerVisiblityChangedHandle LayerVisiblityChanged;


        /// <summary>
        /// 图层样式改变，此时地图一级图层需要重新绘制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="layer"></param>
        internal delegate void LayerStyleChangedHandle(object sender, Layer layer);
        //internal event LayerStyleChangedHandle LayerStyleChanged;


        /// <summary>
        /// 地图一级表现发生改变，包括图层地理数据改变，可视性改变，图层样式改变，图层顺序改变和增加删除图层，此时需要重新绘制一级图层
        /// </summary>
        /// <param name="sender">地图</param>
        internal delegate void MapPerformaceChangedHanle(object sender);
        internal event MapPerformaceChangedHanle MapPerformaceChanged;


        ///// <summary>
        ///// 地图二级表现发生改变，包括选择的要素发生改变，此时需要重新叠加二级图层
        ///// </summary>
        ///// <param name="sender"></param>
        //internal delegate void MapSurfaceChangedHanle(object sender);
        //internal event MapSurfaceChangedHanle MapSurfaceChanged;

        /// <summary>
        /// 地图一级表现发生改变，包括图层地理数据改变，可视性改变，图层样式改变，图层顺序改变和增加删除图层，此时需要重新绘制一级图层
        /// </summary>
        /// <param name="sender">地图</param>
        internal delegate void LayerSymbolChangedHanle(object sender);
        internal event LayerSymbolChangedHanle LayerSymbolChanged;



        private void Layer_VisiblityChanged(object sender, bool visible)
        {
            //LayerVisiblityChanged(this, (Layer)sender);//图层可见性改变，通知其它控件
            MapPerformaceChanged?.Invoke(this);//重绘一级地图
        }

        private void Layer_SymbolChanged(object sender)
        {
            MapPerformaceChanged?.Invoke(this);//重绘一级地图
            LayerSymbolChanged?.Invoke(sender);
        }

        private void Layer_RecordsChanged(object sender)
        {
            //throw new NotImplementedException();
        }

        //投影信息改变了
        private void _PrjSystem_ProjectedSystemChanged(object sender)
        {
            foreach(Layer sLayer in _Layers)
            {
                //计算投影
                bool sRead = sLayer.ReadOnly;
                sLayer.ReadOnly = false;
                if (sLayer.GeoType == typeof(PointD))
                {
                    PointD sPoint;
                    foreach (DataRow mRow in sLayer.Records.Rows)
                    {
                        sPoint = mRow.Field<PointD>(1);
                        if (sPoint != null)
                        {
                            mRow[2] = _PrjSystem.ToProjCo(sPoint);
                        }
                    }
                }
                else if (sLayer.GeoType == typeof(MultiPolyLine))
                {
                    MultiPolyLine sMultiPolyLine;
                    foreach (DataRow mRow in sLayer.Records.Rows)
                    {
                        sMultiPolyLine = mRow.Field<MultiPolyLine>(1);
                        if (sMultiPolyLine != null)
                        {
                            mRow[2] = _PrjSystem.ToProjCo(sMultiPolyLine);
                        }
                    }
                }
                else if (sLayer.GeoType == typeof(MultiPolygon))
                {
                    MultiPolygon sMultiPolygon;
                    foreach (DataRow mRow in sLayer.Records.Rows)
                    {
                        sMultiPolygon = mRow.Field<MultiPolygon>(1);
                        if (sMultiPolygon != null)
                        {
                            mRow[2] = _PrjSystem.ToProjCo(sMultiPolygon);
                        }
                    }
                }

                Geometry sGeo;
                foreach (DataRow mRow in sLayer.Records.Rows)
                {
                    if (!Convert.IsDBNull(mRow[2]))
                    {
                        sGeo = (Geometry)mRow[2];
                        mRow[3] = LayerTools.GetCenterPoint(sGeo);
                    }
                }

                sLayer.MBR = LayerTools.GetLayerGeoMBR(sLayer);
                sLayer.PRJMBR = _PrjSystem.ToProjCo(sLayer.MBR);
                sLayer.Records.AcceptChanges();//保存更改
                sLayer.GenerateIndex();//计算地理索引
                MapPerformaceChanged?.Invoke(this);//重绘一级地图
                sLayer.ReadOnly = sRead;
            }
        }

        #endregion


    }
}
