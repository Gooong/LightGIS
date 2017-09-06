using LightGIS_1._0.GeoLayer;
using LightGIS_1._0.GeoMap;
using LightGIS_1._0.GeoObject;
using LightGIS_1._0.Src.DataIO;
using LightGIS_1._0.Src.GeoMap;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LightGIS_1._0
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }

        #region 字段
        Size mMapControlSize;//记录MapControl的Size
        Map mMap;   //程序中的地图

        List<Geometry> mCopyGeometry;//复制的集合
        Type mCopyGeoType;//复制的元素类型

        int mColumnIndex = -1;//要删除的列index
        #endregion

        #region 特定功能函数

        #region 编辑相关

        void DeleteColumn(int index)
        {
            if(mMap != null && mMap.Editing)
            {
                try
                {
                    Layer sLayer = (Layer)dataGridView1.Tag;
                    sLayer.DeleteField(index);
                }
                catch (Exception ed)
                {
                    MessageBox.Show(ed.Message, "出错");
                }
            }

        }
        void AddColumn(string name , Type type)
        {
            if(mMap!=null && mMap.Editing)
            {
                try
                {
                    Layer sLayer = (Layer)dataGridView1.Tag;
                    sLayer.AddField(name,type);
                }
                catch (Exception ea)
                {
                    MessageBox.Show(ea.Message, "出错");
                }
            }
        }


        void Delete()
        {
            if (mMap.Editing)
            {
                if (mMap.EditingLayer.EditingRecord.Count != 0)
                {
                    mMap.EditingLayer.ReomveRecords(mMap.EditingLayer.EditingRecord);
                    mMap.EditingLayer.EditingRecord.Clear();
                    mapControl1.EditingGeometry.Clear();
                    mapControl1.RefreshAllAsync();
                }
            }
        }

        void Copy()
        {
            if (mMap.Editing)
            {
                mCopyGeoType = mMap.EditingLayer.GeoType;
                if(mapControl1.EditingGeometry.Count != 0)
                {
                    mCopyGeometry = new List<Geometry>(mapControl1.EditingGeometry);
                }
            }
        }

        void Paste()
        {
            if (mMap.Editing)
            {
                if(mCopyGeometry !=null && mCopyGeometry.Count != 0)
                {
                    if(mCopyGeoType != mMap.EditingLayer.GeoType)
                    {
                        MessageBox.Show("粘贴的元素类型和目标图层类型不一致！");
                    }
                    else
                    {
                        mMap.AddGeometry(mMap.EditingLayer, mCopyGeometry, mCopyGeoType);
                        mapControl1.RefreshAllAsync();
                    }
                }
            }
        }


        void ClearSelect()
        {
            if (mMap != null)
            {
                foreach (Layer sLayer in mMap.Layers)
                {
                    sLayer.SelectedRecords.Clear();
                }
                mapControl1.Refresh();
            }

        }

        void PanToSelect()
        {
            if(mMap != null)
            {
                RectangleD sMBR = RectangleD.EmptyMBR;
                foreach (Layer sLayer in mMap.Layers)
                {
                    foreach (DataRow sRow in sLayer.SelectedRecords)
                    {
                        if (!Convert.IsDBNull(sRow[2]))
                        {
                            sMBR += ((Geometry)sRow[2]).MBR;
                        }
                    }
                }
                if (sMBR.Width > 0 && sMBR.Height > 0)
                {
                    PointD sCenter = new PointD(sMBR.MinX + sMBR.Width / 2, sMBR.MinY + sMBR.Height / 2);
                    mapControl1.MoveTo(sCenter);
                    mapControl1.RefreshAllAsync();
                }
            }

        }

        void ZoomToSelect()
        {
            if (mMap != null)
            {
                RectangleD sMBR = RectangleD.EmptyMBR;
                foreach (Layer sLayer in mMap.Layers)
                {
                    foreach (DataRow sRow in sLayer.SelectedRecords)
                    {
                        if (!Convert.IsDBNull(sRow[2]))
                        {
                            sMBR += ((Geometry)sRow[2]).MBR;
                        }
                    }
                }
                if (sMBR.Width > 0 && sMBR.Height > 0)
                {
                    mapControl1.ZoomByBox(sMBR);
                    mapControl1.RefreshAllAsync();
                }
            }

        }
        #endregion

        #region 图层相关
        //增加图层
        void AddLayer()
        {
            if (mReadLayerDialog.ShowDialog(this) == DialogResult.OK)
            {
                //try
                //{
                Layer sLayer = DataIO.ReadShp(mReadLayerDialog.FileName);
                if (mMap == null)
                {
                    Map sMap = new Map();
                    BandNewMap(sMap);
                }
                mMap.AddLayer(sLayer);
                SelectLayer(sLayer);

                //}
                //catch(Exception e)
                //{
                //    MessageBox.Show(e.Message, "读取shp文件失败");
                //}

            }
        }

        void SelectLayer(Layer layer)
        {
            treeViewEnhanced1.SelectedNode = layer.Node;
            dataGridView1.BringToFront();
            dataGridView1.DataMember = layer.Name;
            dataGridView1.Tag = layer;
            dataGridView1.Columns[0].Visible = false;
            dataGridView1.Columns[1].Visible = false;
            dataGridView1.Columns[2].Visible = false;
            dataGridView1.Columns[3].Visible = false;
            propertyGrid1.SelectedObject = layer;
            if (mMap.Editing)
            {
                当前编辑图层.Text = "当前图层：" + layer.Name;
                mMap.SetEditLayer(layer);
            }
            //if(mMap.e)
        }

        void SaveLayer(Layer layer)
        {
            if(mSaveLayerDialog.ShowDialog(this) == DialogResult.OK)
            {

                try
                {
                    DataIO.SaveShp(layer, mSaveLayerDialog.FileName);
                }
                catch (Exception esmap)
                {
                    MessageBox.Show(this,esmap.Message,"保存图层错误");
                }
            }
        }

        /// <summary>
        /// 创建图层按钮函数
        /// </summary>
        /// <returns></returns>
        Layer FrmCreateLayer()
        {
            frmCreateLayer sFCL = new frmCreateLayer();
            if(sFCL.ShowDialog(this) == DialogResult.OK)
            {
                Layer slayer = Layer.CreateLayer(sFCL.layerName,sFCL.geoType,sFCL.filePath);
                return slayer;
            }
            return null;
        }

        #endregion

        #region 地图相关

        void SelectMap()
        {
            if (mMap != null)
            {
                propertyGrid1.SelectedObject = mMap;
                propertyGrid2.SelectedObject = mapControl1;
                propertyGrid2.BringToFront();
                dataGridView1.DataMember = "";
            }
        }


        //移除当前绑定的地图
        void RemoveMap()
        {
            dataGridView1.DataMember = null;
            dataGridView1.DataSource = null;
            propertyGrid1.SelectedObject = null;
            propertyGrid2.SelectedObject = null;
            treeViewEnhanced1.Nodes.Clear();

            mapControl1.ClearMapControl();
            mMap = null;
            this.地图操作strip.Enabled = false;
        }

        /// <summary>
        /// 创建地图按钮函数,包括确认是否新建
        /// </summary>
        void CreateMap()
        {
            bool sDecided = true;
            if (mMap != null)
            {
                MessageBoxButtons messBtn = MessageBoxButtons.OKCancel;
                DialogResult dr = MessageBox.Show("确认关闭原有地图吗？","确认关闭", messBtn);
                if (dr == DialogResult.Cancel)
                {
                    sDecided = false;
                }
                else if (dr == DialogResult.OK)
                {
                    sDecided = true;
                    RemoveMap();
                }
            }

            if (sDecided)
            {
                this.mMap = new Map();
                BandNewMap(mMap);
            }
        }

        /// <summary>
        /// 绑定新地图，含有移除操作
        /// </summary>
        void BandNewMap(Map sMap)
        {
            if (mMap != null)
            {
                RemoveMap();
            }
            mMap = sMap;
            mapControl1.GeoMap = sMap;
            dataGridView1.DataSource = sMap.Tables;
            treeViewEnhanced1.Nodes.Clear();
            treeViewEnhanced1.Nodes.Add(sMap.MapTree);
            treeViewEnhanced1.SelectedNode = sMap.MapTree;
            SelectMap();
            this.地图操作strip.Enabled = true;
        }

        void ReadNewMap()
        {
            if (mReadMapDialog.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    Map sMap = DataIO.ReadMap(mReadMapDialog.FileName);
                    BandNewMap(sMap);
                }
                catch (Exception emap)
                {
                    MessageBox.Show(this, "读取地图文件错误", emap.Message);
                }
            }
        }

        /// <summary>
        /// 地图另存为
        /// </summary>
        void SaveMapAs()
        {
            if (mMap != null)
            {
                if (mSaveMapDialog.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        DataIO.SaveMap(mMap, mSaveMapDialog.FileName);
                        mMap.FilePath = mSaveMapDialog.FileName;
                    }
                    catch (Exception esmap)
                    {
                        MessageBox.Show(this, "保存地图错误", esmap.Message);
                    }
                }
            }
        }
        
        /// <summary>
        /// 保存地图，包括选择存储路径
        /// </summary>
        void SaveMap()
        {
            if (mMap != null)
            {
                String sFilePath = mMap.FilePath;
                if (sFilePath == null || sFilePath == "")
                {
                    if (mSaveMapDialog.ShowDialog(this) == DialogResult.OK)
                    {
                        sFilePath = mSaveMapDialog.FileName;
                        mMap.FilePath = sFilePath;
                    }
                }

                if (sFilePath != null && sFilePath != "")
                {
                    DataIO.SaveMap(mMap, sFilePath);
                }

            }
        }

        void PackageMapTo()
        {
            if (mMap != null)
            {
                if (mSaveMapDialog.ShowDialog() == DialogResult.OK)
                {
                    string sFilePath = mSaveMapDialog.FileName;
                    string sDirPath = Path.GetDirectoryName(sFilePath) + "/layers/";
                    if (!Directory.Exists(sDirPath))
                    {
                        Directory.CreateDirectory(sDirPath);
                    }

                    try
                    {
                        DataIO.SaveMap(mMap, sFilePath);
                        foreach (Layer sLayer in mMap.Layers)
                        {
                            DataIO.SaveShp(sLayer, sDirPath + sLayer.Name + ".shp");
                        }
                    }
                    catch (Exception epa)
                    {
                        MessageBox.Show(this, epa.Message, "打包地图错误");
                    }

                }
            }
        }

        /// <summary>
        /// 导出为图片
        /// </summary>
        void ExportToImage()
        {
            if(mMap != null)
            {
                if (mSaveImageDialog.ShowDialog(this) == DialogResult.OK)
                {
                    Bitmap sBitmap = new Bitmap(mapControl1.Width, mapControl1.Height);
                    mapControl1.DrawToBitmap(sBitmap, new Rectangle(0, 0, mapControl1.Width, mapControl1.Height));
                    try
                    {
                        sBitmap.Save(mSaveImageDialog.FileName);
                    }
                    catch (Exception ee)
                    {
                        MessageBox.Show(this, "保存文件错误", ee.Message);
                    }
                }
            }

        }
        /// <summary>
        /// 插入背景图
        /// </summary>
        void ImportImage()
        {
            if (mOpenImageDialog.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    Image sImage = Bitmap.FromFile(mOpenImageDialog.FileName);
                    mapControl1.BackgroundImage = sImage;
                }
                catch (Exception es)
                {
                    MessageBox.Show(this, "读入文件错误", es.Message);
                }

            }
        }

        #endregion

        #region 整体相关
        /// <summary>
        /// 是否退出程序
        /// </summary>
        /// <returns></returns>
        bool ExitLightGIS()
        {
            if(mMap == null)
            {
                return true;
            }
            else
            {
                if (mMap.Editing)
                {
                    MessageBox.Show("请退出编辑状态再关闭");
                    return false;
                }
                else
                {
                    MessageBoxButtons messBtn = MessageBoxButtons.OKCancel;
                    if (MessageBox.Show("确认退出程序吗？", "确认退出", messBtn) == DialogResult.OK)
                    {
                        SaveMap();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
        #endregion

        #endregion


        #region 工具条事件处理


        #region 地图操作工具条
        private void 放大_Click(object sender, EventArgs e)
        {
            mapControl1.ZoomIn();
        }
        private void 缩小_Click(object sender, EventArgs e)
        {
            mapControl1.ZoomOut();
        }

        private void 漫游_Click(object sender, EventArgs e)
        {
            mapControl1.Pan();
        }

        private void 固定比例放大_Click(object sender, EventArgs e)
        {
            mapControl1.FixZoomIn();
        }

        private void 固定比例缩小_Click(object sender, EventArgs e)
        {
            mapControl1.FixZoomOut();            
        }

        private void 选择要素_Click(object sender, EventArgs e)
        {
            mapControl1.SelectFeature();
        }

        private void 清除所选要素_Click(object sender, EventArgs e)
        {
            ClearSelect();
        }

        private void 平移至所选要素_Click(object sender, EventArgs e)
        {
            PanToSelect();

        }

        private void 缩放至所选要素_Click(object sender, EventArgs e)
        {
            ZoomToSelect();
        }

        private void 识别_Click(object sender, EventArgs e)
        {
            mapControl1.Identify();
        }


        #endregion

        #region 文件操作工具条
        private void 新建地图_Click(object sender, EventArgs e)
        {
            CreateMap();
        }

        private void 打开地图_Click(object sender, EventArgs e)
        {
            ReadNewMap();
        }

        private void 保存地图_Click(object sender, EventArgs e)
        {
            SaveMap();
        }

        private void 打包地图_Click(object sender, EventArgs e)
        {
            PackageMapTo();
        }

        private void 导出地图_Click(object sender, EventArgs e)
        {
            ExportToImage();
        }

        private void 创建图层_Click(object sender, EventArgs e)
        {
            Layer sLayer = FrmCreateLayer();
            if (sLayer != null)
            {
                if (mMap == null)
                {
                    Map sMap = new Map();
                    BandNewMap(sMap);
                }
                mMap.AddLayer(sLayer);
                SelectLayer(sLayer);
            }
        }

        private void 插入图层_Click(object sender, EventArgs e)
        {
            AddLayer();
        }


        private void 插入图片_Click(object sender, EventArgs e)
        {
            ImportImage();
        }


        #endregion

        #region 编辑操作工具条

        private void 剪贴_Click(object sender, EventArgs e)
        {

        }

        private void 复制_Click(object sender, EventArgs e)
        {
            Copy();
        }

        private void 粘贴_Click(object sender, EventArgs e)
        {
            Paste();
        }

        private void 删除_Click(object sender, EventArgs e)
        {
            Delete();
        }

        private void 选择全部_Click(object sender, EventArgs e)
        {

        }

        private void 取消选择_Click(object sender, EventArgs e)
        {

        }
        #endregion

        #region 编辑工具条

        private void 开始编辑_Click(object sender, EventArgs e)
        {
            if (mMap != null)
            {
                if (mMap.Layers.Count != 0)
                {
                    if (mMap.MapTree.IsSelected)
                    {
                        treeViewEnhanced1.SelectedNode = mMap.Layers[0].Node;
                    }
                    dataGridView1.ReadOnly = false;
                    mMap.StartEditMap((Layer)treeViewEnhanced1.SelectedNode.Tag);
                    当前编辑图层.Enabled = true;
                    保存编辑内容ToolStripMenuItem.Enabled = true;
                    停止编辑ToolStripMenuItem.Enabled = true;
                    编辑折点.Enabled = true;
                    新建要素.Enabled = true;
                    开始编辑.Enabled = false;
                    mapControl1.EditFeature();
                    dataGridView1.Refresh();
                }
            }
        }

        private void 停止编辑ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBoxButtons messBtn = MessageBoxButtons.YesNoCancel;
            DialogResult dr = MessageBox.Show("保存已编辑的内容吗？", "保存编辑", messBtn);
            if (dr == DialogResult.Cancel)
            {
                return;
            }
            else if(dr == DialogResult.No)
            {
                mMap.CancleEditMap();
            }
            else if(dr == DialogResult.Yes)
            {
                mMap.FinishEditMap();
            }

            当前编辑图层.Enabled = false;
            保存编辑内容ToolStripMenuItem.Enabled = false;
            停止编辑ToolStripMenuItem.Enabled = false;
            编辑折点.Enabled = false;
            新建要素.Enabled = false;
            开始编辑.Enabled = true;
            mapControl1.Pan();

            dataGridView1.ReadOnly = true;
        }

        //保存编辑内容，创建还原点
        private void 保存编辑内容ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mapControl1.EditingGeometry.Clear();
            mMap.SavePreviousEdit();
        }

        private void 编辑折点_Click(object sender, EventArgs e)
        {
            mapControl1.EditFeature();
        }

        private void 新建要素_Click(object sender, EventArgs e)
        {
            mapControl1.TrackFeature();
        }
        #endregion

        //折叠按钮
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (splitContainer2.Panel2Collapsed)
            {
                splitContainer2.Panel2Collapsed = false;
                dataGridView1.Enabled = true;
                propertyGrid1.Enabled = true;
                this.toolStripButton1.Image = Properties.Resources.right;
            }
            else
            {
                splitContainer2.Panel2Collapsed = true;
                dataGridView1.Enabled = false;
                propertyGrid1.Enabled = false;
                this.toolStripButton1.Image = Properties.Resources.left;
            }
        }

        #endregion

        #region 菜单事件处理

        #region 文件菜单
        private void 新建ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateMap();
        }

        private void 打开ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReadNewMap();
        }

        private void 保存ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveMap();
        }


        private void 保存副本ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveMapAs();
        }

        private void 打包地图ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PackageMapTo();
        }

        private void 导出地图ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportToImage();
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ExitLightGIS())
            {
                this.Close();
            }
        }
        #endregion

        #region 图层菜单
        private void 创建图层ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Layer sLayer = FrmCreateLayer();
            if (sLayer != null)
            {
                if (mMap == null)
                {
                    Map sMap = new Map();
                    BandNewMap(sMap);
                }
                mMap.AddLayer(sLayer);
                SelectLayer(sLayer);
            }
        }

        private void 插入图层ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddLayer();
        }

        private void 保存图层副本ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeViewEnhanced1.SelectedNode != null && treeViewEnhanced1.SelectedNode != treeViewEnhanced1.Nodes[0])
            {
                SaveLayer((Layer)treeViewEnhanced1.SelectedNode.Tag);
            }
            else
            {
                MessageBox.Show("请选在图层栏中选择图层");
            }
        }

        private void 移除图层DToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (mMap != null)
            {
                TreeNode sNode = treeViewEnhanced1.SelectedNode;
                if (sNode != null)
                {
                    //地图节点
                    if (treeViewEnhanced1.Nodes.Contains(sNode))
                    {

                    }

                    //图层节点
                    else
                    {
                        mMap.RemoveLayer((Layer)sNode.Tag);
                    }
                }
            }
        }
        #endregion

        #region 编辑菜单


        private void 复制ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Copy();
        }

        private void 粘贴ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Paste();
        }

        private void 删除ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Delete();
        }
        #endregion

        #region 选择菜单
        private void 按属性选择ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void 按位置选择ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mapControl1.SelectFeature();
        }

        private void 缩放至所选要素ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ZoomToSelect();
        }

        private void 平移至所选要素ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PanToSelect();
        }

        private void 清除所选要素ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearSelect();
        }

        #endregion

        #endregion

        #region 窗体事件处理

        #region treeView事件处理
        private void treeViewEnhanced1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            mapControl1.RefreshAllAsync();
            propertyGrid1.Refresh();
        }

        private void TreeViewEnhanced1_ItemDrag(object sender, ItemDragEventArgs e)
        {
            DoDragDrop(e.Item, DragDropEffects.Move);
        }

        private void TreeViewEnhanced1_DragEnter(object sender, DragEventArgs e)
        {
            //只接受节点的拖拽
            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }

        }

        private void TreeViewEnhanced1_DragDrop(object sender, DragEventArgs e)
        {
            //只接受节点拖拽
            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                TreeNode sRootNode = treeViewEnhanced1.Nodes[0];
                TreeNode sFromNode = (TreeNode)(e.Data.GetData(typeof(TreeNode)));  //被拖拽的节点
                TreeNode sToNode = treeViewEnhanced1.GetNodeAt(treeViewEnhanced1.PointToClient(new Point(e.X, e.Y)));   //获取目标节点

                if (sToNode!= sRootNode && sFromNode!= sRootNode && sToNode != sFromNode)
                {
                    Layer sLayer = (Layer)sFromNode.Tag;
                    this.mMap.Layers.Remove(sLayer);
                    int index;
                    if(sToNode == null)
                    {
                        sFromNode.Remove();
                        index = sRootNode.Nodes.Count;
                    }
                    else if (sFromNode.NextNode == sToNode)
                    {
                        sFromNode.Remove();
                        index = sToNode.Index + 1;
                    }
                    else
                    {
                        sFromNode.Remove();
                        index = sToNode.Index;
                    }
                    sRootNode.Nodes.Insert(index, sFromNode);
                    mMap.Layers.Insert(index, sLayer);
                    mapControl1.RefreshAllAsync();
                }
                treeViewEnhanced1.SelectedNode = sFromNode;
            }
        }

        //选定的内容发生变化
        private void treeViewEnhanced1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            //Console.WriteLine("treeViewEnhanced1_AfterSelect");
            //选中的是地图
            if (treeViewEnhanced1.Nodes.Contains(e.Node))
            {
                //Map sMap = (Map)e.Node.Tag;
                SelectMap();
            }
            //选中的是图层
            else
            {
                Layer sLayer = (Layer)e.Node.Tag;
                SelectLayer(sLayer);
            }
        }
        #endregion

        #region dataGridView事件处理

        private void dataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (mMap != null && e.Button == MouseButtons.Right)
            {
                删除列DToolStripMenuItem.Enabled = mMap.Editing;
                添加列AToolStripMenuItem.Enabled = mMap.Editing;
                dVContextMenuStrip1.Show(MousePosition.X, MousePosition.Y);
                mColumnIndex = e.ColumnIndex;
                //Console.WriteLine(mColumnIndex);
            }
        }

        private void 属性查询ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (mMap != null)
            {
                try
                {
                    searchFrm sF = new searchFrm();
                    if(sF.ShowDialog(this) == DialogResult.OK)
                    {
                        Layer sLayer = (Layer)dataGridView1.Tag;
                        DataRow[] Rows = sLayer.Records.Select(sF.SQLText);

                        if (Rows != null)
                        {
                            sLayer.SelectedRecords = new List<DataRow>(Rows);
                            mapControl1.RefreshAllAsync();
                        }
                    }
                }
                catch(Exception es)
                {
                    MessageBox.Show(es.Message);
                }
            }

            mColumnIndex = -1;
        }

        private void 添加列AToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmAddField sFAF = new frmAddField();
            if(sFAF.ShowDialog(this) == DialogResult.OK)
            {
                AddColumn(sFAF.FieldName, sFAF.FieldType);
            }
            mColumnIndex = -1;
        }

        private void 删除列DToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteColumn(mColumnIndex);
            mColumnIndex = -1;
        }

        #endregion

        private void frmMain_ResizeBegin(object sender, EventArgs e)
        {
            //Console.WriteLine("frmMain_ResizeBegin");
            mapControl1.Resizing = true;
            mMapControlSize = mapControl1.Size;
        }

        private void frmMain_ResizeEnd(object sender, EventArgs e)
        {
            //Console.WriteLine("frmMain_ResizeEnd");
            mapControl1.Resizing = false;
            if (mapControl1.Size != mMapControlSize)
            {
                mapControl1.RefreshAllAsync();
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = !ExitLightGIS();
        }

        private void mapControl1_LayerSymbolChanged(object sender)
        {
            propertyGrid1.Refresh();
            //Console.WriteLine("mapControl1_LayerSymbolChanged");
        }

        //private void frmMain_MouseMove(object sender, MouseEventArgs e)
        //{
        //    Console.WriteLine("ss");
        //    PointD sPointOnMap = mapControl1.ToMapPoint(e.Location);
        //    位置.Text = "X:" + sPointOnMap.X.ToString("0.00") + "    Y:" + sPointOnMap.Y.ToString("0.00");
        //}
        #endregion


    }
}
