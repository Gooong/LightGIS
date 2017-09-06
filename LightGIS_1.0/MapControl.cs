using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LightGIS_1._0.GeoObject;
using LightGIS_1._0.GeoLayer;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Remoting.Messaging;
using LightGIS_1._0.Src.GeoMap;
using LightGIS_1._0.GeoMap;
using System.Reflection;
using LightGIS_1._0.Src.GeoLayer.GeoRenderer;
using LightGIS_1._0.GeoLayer.GeoRenderer.GeoSymbol;
using static LightGIS_1._0.GeoLayer.GeoRenderer.GeoSymbol.PointSymbol;
using System.Drawing.Drawing2D;

namespace LightGIS_1._0
{
    public partial class MapControl : UserControl
    {
        #region 常量
        const double _RECT_TOLERANCE = 5;//用户的最小拉框
        #endregion

        #region 构造函数
        public MapControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// MapControl操作状态枚举类
        /// </summary>
        private enum MCState
        {
            NoOper = 0,//无操作
            Pan = 1,//漫游操作
            ZoomIn = 2,//放大操作
            ZoomOut = 3,//缩小操作
            SelectFeature = 4,//选择要素状态
            Identify = 5,//识别状态
            EditFeature = 6,//编辑已有要素状态
            TrackFeature = 7,//跟踪要素状态
        }
        #endregion

        #region 字段

        private Map _Map;
        private Bitmap _FixedBitMap = new Bitmap(1, 1);//一级地图

        private MCState _State = MCState.NoOper;//当前地图操作状态

        private double _MapScale = 2;//显示比例的倒数，地图投影数据是显示数据的_Scale倍



        //内部状态标识量
        private bool mResizing = false;//是否正在调整大小
        //private bool mDrawing = false;//是否正在绘制
        //private bool mPanning = false;//是否正在鼠标拖拽漫游

        private bool mDragging = false;

        //内部绘制相关变量
        private Point mStartPoint = new Point();//鼠标按下时的位置，是屏幕坐标
        private Point mMouseLocation = new Point();//鼠标当前的位置，用于漫游，也是屏幕坐标。
        private double mOffsetX, mOffsetY;//一级地图左上角相对于地图左上角的偏移量
        private float mBitMapOffsetX = 0, mBitMapOffsetY = 0;//MapControl窗口相对于_FixedBitMap的偏移

        //内部绘制要素
        private Rectangle mdRect;//选择或缩放时的矩形框
        private Stack<PointD> mdTrackingFeature = new Stack<PointD>();//存储正在描绘的要素
        private PointD mDraggingPoint = null;//正在拖拽的点
        private List<Geometry> _EditingGeometry;    //正在编辑的要素集合，存的只是一个阴影，修改它没用

        //用户可定义常量
        private bool muAutoSize = true;
        private float muZoomRatio = 1.2f;

        private PointSymbol _SelectedPointSymbol = new PointSymbol(PointStyleConstant.FillCircle, 10, Color.Cyan);
        private LineSymbol _SelectedLineSymbol = new LineSymbol(DashStyle.Solid, 5, Color.Cyan);
        private PolygonSymbol _SelectedPolygonSymbol = new PolygonSymbol(Color.White, new LineSymbol(DashStyle.Solid, 5, Color.Cyan));

        private Pen _TrackPen = new Pen(Color.DarkGreen, 2);    //跟踪要素时的画笔
        private Brush _VertexBrush = new SolidBrush(Color.DarkGreen);    //编辑多边形时，绘制角点的画笔
        private float _VertexWidth = 10;//角点的宽度

        private PointSymbol _PointShadowSymbol = new PointSymbol(PointStyleConstant.FillCircle, 10, Color.DarkGray);
        private LineSymbol _LineShadowSymbol = new LineSymbol(DashStyle.Dash, 2, Color.DarkGray);
        private PolygonSymbol _PolygonShadowSymbol = new PolygonSymbol(Color.WhiteSmoke,
            new LineSymbol(DashStyle.Dash, 5, Color.DarkGray));//阴影画笔

        private Pen _DragBoxPen = new Pen(Color.Black, 1);
        private float _SelectByPointTolerance = 10;//点选时的容限值，屏幕距离

        //异步调用相关变量
        private CancellationTokenSource cts = null;
        DrawFixedBitMapHandler drawHandler = null;
        IAsyncResult iar = null;
        //图标
        Cursor mPanCursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("LightGIS_1._0.Resources.PanUp.ico"));
        Cursor mZoomInCursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("LightGIS_1._0.Resources.ZoomIn.ico"));
        Cursor mZoomOutCursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("LightGIS_1._0.Resources.ZoomOut.ico"));
        Cursor mCrossCursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("LightGIS_1._0.Resources.Cross.ico"));
        Cursor mSelectCursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("LightGIS_1._0.Resources.SelectionSelectTool.ico"));
        Cursor mIdentifyCursor = Cursors.Help;
        Cursor mEditCursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("LightGIS_1._0.Resources.EditingEditTool.ico"));
        Cursor mTrackCursor = Cursors.Cross;
        #endregion

        #region 属性


        [Browsable(true), Description("获取或设置地图缩放比例"), Category("基本"), DisplayName("缩放比例")]
        public double MapScale
        {
            get { return _MapScale; }
            set { _MapScale = value; }
        }

        [Browsable(true), Description("获取或设置地图点选容限"), Category("基本"), DisplayName("点选容限")]
        public float SelectByPointTolerance
        {
            get { return _SelectByPointTolerance; }
            set { _SelectByPointTolerance = value; }
        }





        [TypeConverter(typeof(ExpandableObjectConverter))]
        [Browsable(true), Description("获取或设置地图选择的点样式"), Category("样式"), DisplayName("选择的点样式")]
        public PointSymbol SelectedPointSymbol
        {
            get { return _SelectedPointSymbol; }
            set { _SelectedPointSymbol = value; }
        }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        [Browsable(true), Description("获取或设置地图选择的线样式"), Category("样式"), DisplayName("选择的线样式")]
        public LineSymbol LineShadowSymbol
        {
            get { return _SelectedLineSymbol; }
            set { _SelectedLineSymbol = value; }
        }
        [TypeConverter(typeof(ExpandableObjectConverter))]
        [Browsable(true), Description("获取或设置地图选择的面样式"), Category("样式"), DisplayName("选择的面样式")]
        public PolygonSymbol SelectedPolygonSymbol
        {
            get { return _SelectedPolygonSymbol; }
            set { _SelectedPolygonSymbol = value; }
        }
  

        [Browsable(false)]
        public Map GeoMap
        {
            get { return _Map; }
            set
            {
                if (value != null)
                {
                    _Map = value;
                    _Map.MapPerformaceChanged += _Map_MapPerformaceChanged;
                    _Map.LayerAdded += _Map_LayerAdded;
                    _Map.LayerSymbolChanged += _Map_LayerSymbolChanged;
                }
            }
        }

        [Browsable(false)]
        public List<Geometry> EditingGeometry
        {
            get { return _EditingGeometry; }
            set { _EditingGeometry = value; }
        }

        [Browsable(false)]
        public bool Resizing
        {
            set { mResizing = value; }
        }
        #endregion

        #region 状态方法
        public void NoOper()
        {
            this._State = MCState.NoOper;
            this.Cursor = Cursors.Default;
        }
        public void Pan()
        {
            this._State = MCState.Pan;
            this.Cursor = mPanCursor;
        }
        public void ZoomIn()
        {
            this._State = MCState.ZoomIn;
            this.Cursor = mZoomInCursor;
        }
        public void ZoomOut()
        {
            this._State = MCState.ZoomOut;
            this.Cursor = mZoomOutCursor;
        }
        public void SelectFeature()
        {
            this._State = MCState.SelectFeature;
            this.Cursor = mSelectCursor;
        }
        public void Identify()
        {
            this._State = MCState.Identify;
            this.Cursor = mIdentifyCursor;
        }
        public void EditFeature()
        {
            if (_Map.Editing)
            {
                this.EditingGeometry = new List<Geometry>();
                this._State = MCState.EditFeature;
                this.Cursor = mEditCursor;
            }
        }
        public void TrackFeature()
        {
            if (_Map.Editing)
            {
                this._State = MCState.TrackFeature;
                this.Cursor = mTrackCursor;
            }
        }

        //固定比例放大
        public void FixZoomIn()
        {
            this.ZoomByCenter(ToMapPoint(new PointF(this.Width / 2, this.Height / 2)), muZoomRatio);
            RefreshAllAsync();
        }

        //固定比例缩小
        public void FixZoomOut()
        {
            this.ZoomByCenter(ToMapPoint(new PointF(this.Width / 2, this.Height / 2)), 1 / muZoomRatio);
            RefreshAllAsync();
        }
        #endregion


        #region 方法
        /// <summary>
        /// 地图投影坐标计算到屏幕坐标
        /// </summary>
        /// <param name="">地图投影坐标</param>
        /// <returns>屏幕坐标</returns>
        public PointF FromMapPoint(PointD point)
        {
            float sX = (float)((point.X - mOffsetX) / _MapScale) -mBitMapOffsetX;
            float sY = (float)((mOffsetY - point.Y) / _MapScale) -mBitMapOffsetY;
            return new PointF(sX, sY);
        }

        public PointF[] FromMapPoints(PointD[] points)
        {
            PointF[] sPoints = new PointF[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                sPoints[i] = FromMapPoint(points[i]);
                //Console.WriteLine(sPoints[i]);
            }
            return sPoints;
        }

        /// <summary>
        /// 屏幕坐标转化为地图投影坐标
        /// </summary>
        /// <param name="point">屏幕坐标</param>
        /// <returns>地图投影坐标</returns>
        public PointD ToMapPoint(PointF point)
        {
            double sX = point.X * _MapScale + mOffsetX;
            double sY = mOffsetY - point.Y * _MapScale;
            return new PointD(sX, sY);
        }



        public PointD[] ToMapPoints(PointF[] points)
        {
            PointD[] mPoints = new PointD[points.Length];
            for (int i = 0; i < points.Length; ++i)
            {
                mPoints[i] = ToMapPoint(points[i]);
            }
            return mPoints;
        }

        /// <summary>
        /// 屏幕矩形转地图矩形
        /// </summary>
        /// <param name="rect">屏幕矩形</param>
        /// <returns></returns>
        public RectangleD ToMapRect(RectangleF rect)
        {
            double sWidth = rect.Width * _MapScale;
            double sHeight = rect.Height * _MapScale;
            return new RectangleD(ToMapPoint(new PointF(rect.X, rect.Bottom)), sWidth, sHeight);
        }

        /// <summary>
        /// 将地图中心移动至某点
        /// </summary>
        /// <param name="center"></param>
        public void MoveTo(PointD center)
        {
            mOffsetX = center.X - _MapScale * this.Width / 2;
            mOffsetY = center.Y + _MapScale * this.Height / 2;
        }

        /// <summary>
        /// 根据点放大和缩小地图
        /// </summary>
        /// <param name="center">中心点，投影坐标</param>
        /// <param name="ratio">放大比率</param>
        public void ZoomByCenter(PointD center, double ratio)
        {
            //中心点的坐标指的是地图坐标
            //修改三个变量
            _MapScale = _MapScale / ratio;//定义新的比例尺

            mOffsetX = mOffsetX + (1 - 1 / ratio) * (center.X - mOffsetX);
            mOffsetY = mOffsetY + (1 - 1 / ratio) * (center.Y - mOffsetY);

            DisplayScaleChanged?.Invoke(this, _MapScale);
        }

        /// <summary>
        /// 根据矩形盒缩放地图
        /// </summary>
        /// <param name="rect">矩形盒，</param>
        public void ZoomByBox(RectangleD rect)
        {
            //Console.WriteLine(rect.MinX);
            //Console.WriteLine(rect.MinY);
            //Console.WriteLine(rect.MaxX);
            //Console.WriteLine(rect.MaxY);

            if(rect.Width<=0 || rect.Height <= 0) { return; }
            
            double sScaleX;
            double sScaleY;
            sScaleX = rect.Width / this.Width;
            sScaleY = rect.Height / this.Height;

            //越大越好
            if (sScaleX >= sScaleY)
            {
                _MapScale = sScaleX;
                mOffsetX = rect.MinX;
                mOffsetY = rect.MinY + rect.Height / 2 + this.Height * _MapScale / 2;
            }
            else
            {
                _MapScale = sScaleY;
                mOffsetY = rect.MaxY;
                mOffsetX = rect.MaxX - rect.Width / 2 - this.Width * _MapScale / 2;
            }

            DisplayScaleChanged?.Invoke(this, _MapScale);
            //Console.WriteLine("moffsetX:" + mOffsetX);
            //Console.WriteLine("moffsetY:" + mOffsetY);
        }

        /// <summary>
        /// 回到初始状态
        /// </summary>
        public void ClearMapControl()
        {
            _Map = null;
            this.NoOper();
            //内部绘制相关变量
            mStartPoint = new Point();
            mMouseLocation = new Point();
            mBitMapOffsetX = 0;
            mBitMapOffsetY = 0;


            mdTrackingFeature = new Stack<PointD>();
            mDraggingPoint = null;
            _EditingGeometry = null;
            RefreshAllAsync();
        }
        #endregion


        #region 绘制方法

        #region 图层的绘制方法

        /// <summary>
        /// 绘制图层固定要素，包括图层原有的要素，以及这些要素的注记
        /// </summary>
        /// <param name="g"></param>
        /// <param name="layer"></param>
        public void DrawFixedLayer(Graphics g, Layer layer)
        {
            switch (layer.LayerRenderMethod)
            {
                case Renderer.RenderMethod.SimpleRender:
                    DrawSimpleRendererFeatures(g, layer.Records.Rows.Cast<DataRow>(), layer.LayerSimpleRenderer.symbol, layer.GeoType);
                    break;

                case Renderer.RenderMethod.UniqueValueRender:
                    if (layer.LayerUniqueValueRenderer.Field == null)
                    {
                        DrawSimpleRendererFeatures(g, layer.Records.Rows.Cast<DataRow>(), layer.LayerSimpleRenderer.symbol, layer.GeoType);
                    }
                    else
                    {
                        DrawUniqueValueRenderer(layer, g);
                    }
                    break;

                case Renderer.RenderMethod.ClassBreakRender:
                    if (layer.LayerClassBreakRenderer.Field == null)
                    {
                        DrawSimpleRendererFeatures(g, layer.Records.Rows.Cast<DataRow>(), layer.LayerSimpleRenderer.symbol, layer.GeoType);
                    }
                    else
                    {
                        DrawClassBreakRenderer(layer, g);
                    }
                    break;
            }
        }


        public void DrawLayerTextLabel(Graphics g, Layer layer)
        {
            if (layer.TextLabel.Visible && layer.TextLabel.Field != null)
            {
                String sField = layer.TextLabel.Field;
                if (layer.Records.Columns.Contains(sField))
                {
                    String sText;
                    PointF sPoint;
                    Font sFont = layer.TextLabel.TextFont;
                    float sX = layer.TextLabel.OffsetX;
                    float sY = layer.TextLabel.OffsetY;
                    Brush sBrush = new SolidBrush(layer.TextLabel.SymbolColor);
                    foreach (DataRow sRow in layer.Records.Rows)
                    {
                        if (!Convert.IsDBNull(sRow[3]) && !Convert.IsDBNull(sRow[sField]))
                        {

                            sText = sRow[sField].ToString();
                            sPoint = FromMapPoint((PointD)sRow[3]);
                            g.DrawString(sText, sFont, sBrush, sPoint.X + sX, sPoint.Y + sY);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 简单符号绘制法
        /// </summary>
        /// <param name="g"></param>
        /// <param name="rows"></param>
        /// <param name="symbol"></param>
        /// <param name="geoType"></param>
        public void DrawSimpleRendererFeatures(Graphics g, IEnumerable<DataRow> rows, Symbol symbol, Type geoType)
        {
            if (geoType == typeof(MultiPolygon))
            {
                PolygonSymbol sSymbol = (PolygonSymbol)symbol;
                Pen sPen = new Pen(sSymbol.OutlineColor, sSymbol.OutlineWidth);
                sPen.DashStyle = sSymbol.OutlineStyle;
                SolidBrush sBrush = new SolidBrush(sSymbol.SymbolColor);

                MultiPolygon sMultiPolygon;
                Polygon sPolygon;
                PointF[] sPoints;
                GraphicsPath sGP = new GraphicsPath();
                foreach (DataRow sRow in rows)
                {
                    if (!Convert.IsDBNull(sRow[2]))
                    {
                        sMultiPolygon = (MultiPolygon)sRow[2];
                        for (int j = 0; j < sMultiPolygon.Count; j++)
                        {
                            sPolygon = sMultiPolygon.Polygons[j];
                            sPoints = FromMapPoints(sPolygon.Points);
                            sGP.AddPolygon(sPoints);
                        }
                        g.FillPath(sBrush, sGP);
                        g.DrawPath(sPen, sGP);
                        sGP.Reset();
                    }

                }
            }
            else if (geoType == typeof(MultiPolyLine))
            {
                LineSymbol sSymbol = (LineSymbol)symbol;
                Pen sPen = new Pen(sSymbol.SymbolColor, sSymbol.Width);
                sPen.DashStyle = sSymbol.LineDashStyle;

                MultiPolyLine sMultipolyline;
                PolyLine sPolyline;
                PointF[] sPoints;
                foreach (DataRow sRow in rows)
                {
                    if (!Convert.IsDBNull(sRow[2]))
                    {
                        sMultipolyline = (MultiPolyLine)sRow[2];
                        for (int j = 0; j < sMultipolyline.Count; j++)
                        {
                            sPolyline = sMultipolyline.PolyLines[j];
                            sPoints = FromMapPoints(sPolyline.Points);
                            g.DrawLines(sPen, sPoints);
                        }
                    }
                }
            }
            else if (geoType == typeof(PointD))
            {
                PointSymbol sSymbol = (PointSymbol)symbol;
                Pen sPen = new Pen(sSymbol.SymbolColor);
                SolidBrush sBrush = new SolidBrush(sSymbol.SymbolColor);
                PointD sPointD;
                PointF sPointF;
                PointF[] sPArr = new PointF[3];
                switch (sSymbol.Style)
                {
                    case PointSymbol.PointStyleConstant.Circle:
                        foreach (DataRow sRow in rows)
                        {
                            if (!Convert.IsDBNull(sRow[2]))
                            {
                                sPointD = (PointD)sRow[2];
                                sPointF = FromMapPoint(sPointD);
                                g.DrawEllipse(sPen, sPointF.X - sSymbol.Size / 2, sPointF.Y - sSymbol.Size / 2, sSymbol.Size, sSymbol.Size);
                            }
                        }
                        break;
                    case PointSymbol.PointStyleConstant.FillCircle:
                        foreach (DataRow sRow in rows)
                        {
                            if (!Convert.IsDBNull(sRow[2]))
                            {
                                sPointD = (PointD)sRow[2];
                                sPointF = FromMapPoint(sPointD);
                                g.FillEllipse(sBrush, sPointF.X - sSymbol.Size / 2, sPointF.Y - sSymbol.Size / 2, sSymbol.Size, sSymbol.Size);
                            }
                        }
                        break;
                    case PointSymbol.PointStyleConstant.Square:
                        foreach (DataRow sRow in rows)
                        {
                            if (!Convert.IsDBNull(sRow[2]))
                            {
                                sPointD = (PointD)sRow[2];
                                sPointF = FromMapPoint(sPointD);
                                g.DrawRectangle(sPen, sPointF.X - sSymbol.Size / 2, sPointF.Y - sSymbol.Size / 2, sSymbol.Size, sSymbol.Size);
                            }
                        }
                        break;
                    case PointSymbol.PointStyleConstant.FillSquare:
                        foreach (DataRow sRow in rows)
                        {
                            if (!Convert.IsDBNull(sRow[2]))
                            {
                                sPointD = (PointD)sRow[2];
                                sPointF = FromMapPoint(sPointD);
                                g.FillRectangle(sBrush, sPointF.X - sSymbol.Size / 2, sPointF.Y - sSymbol.Size / 2, sSymbol.Size, sSymbol.Size);
                            }
                        }
                        break;
                    case PointSymbol.PointStyleConstant.Triangle:

                        foreach (DataRow sRow in rows)
                        {
                            if (!Convert.IsDBNull(sRow[2]))
                            {
                                sPointD = (PointD)sRow[2];
                                sPointF = FromMapPoint(sPointD);
                                sPArr[0] = new PointF(sPointF.X, sPointF.Y - 2 * sSymbol.Size / 3);
                                sPArr[1] = new PointF(sPointF.X - (float)Math.Sqrt(3) * sSymbol.Size / 3, sPointF.Y + sSymbol.Size / 3);
                                sPArr[2] = new PointF(sPointF.X + (float)Math.Sqrt(3) * sSymbol.Size / 3, sPointF.Y + sSymbol.Size / 3);
                                g.DrawPolygon(sPen, sPArr);
                            }
                        }
                        break;
                    case PointSymbol.PointStyleConstant.FillTriangle:
                        foreach (DataRow sRow in rows)
                        {
                            if (!Convert.IsDBNull(sRow[2]))
                            {
                                sPointD = (PointD)sRow[2];
                                sPointF = FromMapPoint(sPointD);
                                sPArr[0] = new PointF(sPointF.X, sPointF.Y + 2 * sSymbol.Size / 3);
                                sPArr[1] = new PointF(sPointF.X - (float)Math.Sqrt(3) * sSymbol.Size / 3, sPointF.Y - sSymbol.Size / 3);
                                sPArr[2] = new PointF(sPointF.X + (float)Math.Sqrt(3) * sSymbol.Size / 3, sPointF.Y - sSymbol.Size / 3);
                                g.FillPolygon(sBrush, sPArr);
                            }
                        }
                        break;
                    case PointSymbol.PointStyleConstant.Ring:
                        foreach (DataRow sRow in rows)
                        {
                            if (!Convert.IsDBNull(sRow[2]))
                            {
                                sPointD = (PointD)sRow[2];
                                sPointF = FromMapPoint(sPointD);
                                g.DrawEllipse(sPen, sPointF.X - sSymbol.Size / 2, sPointF.Y - sSymbol.Size / 2, sSymbol.Size, sSymbol.Size);
                                g.DrawEllipse(sPen, sPointF.X - sSymbol.Size / 4, sPointF.Y - sSymbol.Size / 4, sSymbol.Size / 2, sSymbol.Size / 2);
                            }
                        }
                        break;
                    case PointSymbol.PointStyleConstant.FillRing:
                        foreach (DataRow sRow in rows)
                        {
                            if (!Convert.IsDBNull(sRow[2]))
                            {
                                sPointD = (PointD)sRow[2];
                                sPointF = FromMapPoint(sPointD);
                                g.DrawEllipse(sPen, sPointF.X - sSymbol.Size / 2, sPointF.Y - sSymbol.Size / 2, sSymbol.Size, sSymbol.Size);
                                g.FillEllipse(sBrush, sPointF.X - sSymbol.Size / 4, sPointF.Y - sSymbol.Size / 4, sSymbol.Size / 2, sSymbol.Size / 2);
                            }
                        }
                        break;
                }
            }
        }


        public void DrawSimpleRendererFeatures(Graphics g, IEnumerable<Geometry> rows, Symbol symbol, Type geoType)
        {
            if (geoType == typeof(MultiPolygon))
            {
                PolygonSymbol sSymbol = (PolygonSymbol)symbol;
                Pen sPen = new Pen(sSymbol.OutlineColor, sSymbol.OutlineWidth);
                sPen.DashStyle = sSymbol.OutlineStyle;
                SolidBrush sBrush = new SolidBrush(sSymbol.SymbolColor);

                MultiPolygon sMultiPolygon;
                Polygon sPolygon;
                PointF[] sPoints;
                GraphicsPath sGP = new GraphicsPath();
                foreach (Geometry sRow in rows)
                {
                    sMultiPolygon = (MultiPolygon)sRow;
                    for (int j = 0; j < sMultiPolygon.Count; j++)
                    {
                        sPolygon = sMultiPolygon.Polygons[j];
                        sPoints = FromMapPoints(sPolygon.Points);
                        sGP.AddPolygon(sPoints);
                    }
                    g.FillPath(sBrush, sGP);
                    g.DrawPath(sPen, sGP);
                    sGP.Reset();
                }
            }
            else if (geoType == typeof(MultiPolyLine))
            {
                LineSymbol sSymbol = (LineSymbol)symbol;
                Pen sPen = new Pen(sSymbol.SymbolColor, sSymbol.Width);
                sPen.DashStyle = sSymbol.LineDashStyle;

                MultiPolyLine sMultipolyline;
                PolyLine sPolyline;
                PointF[] sPoints;
                foreach (Geometry sRow in rows)
                {
                    sMultipolyline = (MultiPolyLine)sRow;
                    for (int j = 0; j < sMultipolyline.Count; j++)
                    {
                        sPolyline = sMultipolyline.PolyLines[j];
                        sPoints = FromMapPoints(sPolyline.Points);
                        g.DrawLines(sPen, sPoints);
                    }
                }
            }
            else if (geoType == typeof(PointD))
            {
                PointSymbol sSymbol = (PointSymbol)symbol;
                Pen sPen = new Pen(sSymbol.SymbolColor);
                SolidBrush sBrush = new SolidBrush(sSymbol.SymbolColor);
                PointD sPointD;
                PointF sPointF;
                PointF[] sPArr = new PointF[3];
                switch (sSymbol.Style)
                {
                    case PointSymbol.PointStyleConstant.Circle:
                        foreach (Geometry sRow in rows)
                        {
                            sPointD = (PointD)sRow;
                            sPointF = FromMapPoint(sPointD);
                            g.DrawEllipse(sPen, sPointF.X - sSymbol.Size / 2, sPointF.Y - sSymbol.Size / 2, sSymbol.Size, sSymbol.Size);
                        }
                        break;
                    case PointSymbol.PointStyleConstant.FillCircle:
                        foreach (Geometry sRow in rows)
                        {
                            sPointD = (PointD)sRow;
                            sPointF = FromMapPoint(sPointD);
                            g.FillEllipse(sBrush, sPointF.X - sSymbol.Size / 2, sPointF.Y - sSymbol.Size / 2, sSymbol.Size, sSymbol.Size);
                        }
                        break;
                    case PointSymbol.PointStyleConstant.Square:
                        foreach (Geometry sRow in rows)
                        {
                            sPointD = (PointD)sRow;
                            sPointF = FromMapPoint(sPointD);
                            g.DrawRectangle(sPen, sPointF.X - sSymbol.Size / 2, sPointF.Y - sSymbol.Size / 2, sSymbol.Size, sSymbol.Size);

                        }
                        break;
                    case PointSymbol.PointStyleConstant.FillSquare:
                        foreach (Geometry sRow in rows)
                        {
                            sPointD = (PointD)sRow;
                            sPointF = FromMapPoint(sPointD);
                            g.FillRectangle(sBrush, sPointF.X - sSymbol.Size / 2, sPointF.Y - sSymbol.Size / 2, sSymbol.Size, sSymbol.Size);
                        }
                        break;
                    case PointSymbol.PointStyleConstant.Triangle:

                        foreach (Geometry sRow in rows)
                        {
                            sPointD = (PointD)sRow;
                            sPointF = FromMapPoint(sPointD);
                            sPArr[0] = new PointF(sPointF.X, sPointF.Y - 2 * sSymbol.Size / 3);
                            sPArr[1] = new PointF(sPointF.X - (float)Math.Sqrt(3) * sSymbol.Size / 3, sPointF.Y + sSymbol.Size / 3);
                            sPArr[2] = new PointF(sPointF.X + (float)Math.Sqrt(3) * sSymbol.Size / 3, sPointF.Y + sSymbol.Size / 3);
                            g.DrawPolygon(sPen, sPArr);
                        }
                        break;
                    case PointSymbol.PointStyleConstant.FillTriangle:
                        foreach (Geometry sRow in rows)
                        {
                            sPointD = (PointD)sRow;
                            sPointF = FromMapPoint(sPointD);
                            sPArr[1] = new PointF(sPointF.X, sPointF.Y + 2 * sSymbol.Size / 3);
                            sPArr[2] = new PointF(sPointF.X - (float)Math.Sqrt(3) * sSymbol.Size / 3, sPointF.Y - sSymbol.Size / 3);
                            sPArr[3] = new PointF(sPointF.X + (float)Math.Sqrt(3) * sSymbol.Size / 3, sPointF.Y - sSymbol.Size / 3);
                            g.FillPolygon(sBrush, sPArr);
                        }
                        break;
                    case PointSymbol.PointStyleConstant.Ring:
                        foreach (Geometry sRow in rows)
                        {
                            sPointD = (PointD)sRow;
                            sPointF = FromMapPoint(sPointD);
                            g.DrawEllipse(sPen, sPointF.X - sSymbol.Size / 2, sPointF.Y - sSymbol.Size / 2, sSymbol.Size, sSymbol.Size);
                            g.DrawEllipse(sPen, sPointF.X - sSymbol.Size / 4, sPointF.Y - sSymbol.Size / 4, sSymbol.Size / 2, sSymbol.Size / 2);
                        }
                        break;
                    case PointSymbol.PointStyleConstant.FillRing:
                        foreach (Geometry sRow in rows)
                        {
                            sPointD = (PointD)sRow;
                            sPointF = FromMapPoint(sPointD);
                            g.DrawEllipse(sPen, sPointF.X - sSymbol.Size / 2, sPointF.Y - sSymbol.Size / 2, sSymbol.Size, sSymbol.Size);
                            g.FillEllipse(sBrush, sPointF.X - sSymbol.Size / 4, sPointF.Y - sSymbol.Size / 4, sSymbol.Size / 2, sSymbol.Size / 2);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// 绘制唯一值渲染图层
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="g"></param>
        private void DrawUniqueValueRenderer(Layer layer, Graphics g)
        {
            UniqueValueRenderer renderer = (UniqueValueRenderer)layer.LayerRenderer;
            string sField = renderer.Field;
            string sValue;
            if(sField != null)
            {
                if (layer.GeoType == typeof(PointD))
                {
                    PointSymbol sSymbol = (PointSymbol)renderer.DefaultSymbol;
                    Pen sPen = new Pen(sSymbol.SymbolColor);
                    SolidBrush sBrush = new SolidBrush(sSymbol.SymbolColor);
                    PointD sPointD;
                    PointF sPointF;
                    PointF[] sPArr = new PointF[3];
                    switch (sSymbol.Style)
                    {
                        case PointSymbol.PointStyleConstant.Circle:
                            for (int i = 0; i < layer.Records.Rows.Count; i++)
                            {
                                if (!Convert.IsDBNull(layer.Records.Rows[i][2]))
                                {
                                    sPointD = (PointD)layer.Records.Rows[i][2];
                                    sPointF = FromMapPoint(sPointD);
                                    sValue = layer.Records.Rows[i][sField].ToString();
                                    sPen.Color = renderer.FindColor(sValue);
                                    g.DrawEllipse(sPen, sPointF.X - sSymbol.Size / 2, sPointF.Y - sSymbol.Size / 2, sSymbol.Size, sSymbol.Size);
                                }
                            }
                            break;
                        case PointSymbol.PointStyleConstant.FillCircle:
                            for (int i = 0; i < layer.Records.Rows.Count; i++)
                            {
                                if (!Convert.IsDBNull(layer.Records.Rows[i][2]))
                                {
                                    sPointD = (PointD)layer.Records.Rows[i][2];
                                    sPointF = FromMapPoint(sPointD);
                                    sValue = layer.Records.Rows[i][sField].ToString();
                                    sBrush.Color = renderer.FindColor(sValue);
                                    g.FillEllipse(sBrush, sPointF.X - sSymbol.Size / 2, sPointF.Y - sSymbol.Size / 2, sSymbol.Size, sSymbol.Size);
                                }
                            }
                            break;
                        case PointSymbol.PointStyleConstant.Square:
                            for (int i = 0; i < layer.Records.Rows.Count; i++)
                            {
                                if (!Convert.IsDBNull(layer.Records.Rows[i][2]))
                                {
                                    sPointD = (PointD)layer.Records.Rows[i][2];
                                    sPointF = FromMapPoint(sPointD);
                                    sValue = layer.Records.Rows[i][sField].ToString();
                                    sPen.Color = renderer.FindColor(sValue);
                                    g.DrawRectangle(sPen, sPointF.X - sSymbol.Size / 2, sPointF.Y - sSymbol.Size / 2, sSymbol.Size, sSymbol.Size);
                                }
                            }
                            break;
                        case PointSymbol.PointStyleConstant.FillSquare:
                            for (int i = 0; i < layer.Records.Rows.Count; i++)
                            {
                                if (!Convert.IsDBNull(layer.Records.Rows[i][2]))
                                {
                                    sPointD = (PointD)layer.Records.Rows[i][2];
                                    sPointF = FromMapPoint(sPointD);
                                    sValue = layer.Records.Rows[i][sField].ToString();
                                    sBrush.Color = renderer.FindColor(sValue);
                                    g.FillRectangle(sBrush, sPointF.X - sSymbol.Size / 2, sPointF.Y - sSymbol.Size / 2, sSymbol.Size, sSymbol.Size);
                                }
                            }
                            break;
                        case PointSymbol.PointStyleConstant.Triangle:

                            for (int i = 0; i < layer.Records.Rows.Count; i++)
                            {
                                if (!Convert.IsDBNull(layer.Records.Rows[i][2]))
                                {
                                    sPointD = (PointD)layer.Records.Rows[i][2];
                                    sPointF = FromMapPoint(sPointD);
                                    sPArr[0] = new PointF(sPointF.X, sPointF.Y - 2 * sSymbol.Size / 3);
                                    sPArr[1] = new PointF(sPointF.X - (float)Math.Sqrt(3) * sSymbol.Size / 3, sPointF.Y + sSymbol.Size / 3);
                                    sPArr[2] = new PointF(sPointF.X + (float)Math.Sqrt(3) * sSymbol.Size / 3, sPointF.Y + sSymbol.Size / 3);
                                    sValue = layer.Records.Rows[i][sField].ToString();
                                    sPen.Color = renderer.FindColor(sValue);
                                    g.DrawPolygon(sPen, sPArr);
                                }
                            }
                            break;
                        case PointSymbol.PointStyleConstant.FillTriangle:
                            for (int i = 0; i < layer.Records.Rows.Count; i++)
                            {
                                if (!Convert.IsDBNull(layer.Records.Rows[i][2]))
                                {
                                    sPointD = (PointD)layer.Records.Rows[i][2];
                                    sPointF = FromMapPoint(sPointD);
                                    sPArr[0] = new PointF(sPointF.X, sPointF.Y + 2 * sSymbol.Size / 3);
                                    sPArr[1] = new PointF(sPointF.X - (float)Math.Sqrt(3) * sSymbol.Size / 3, sPointF.Y - sSymbol.Size / 3);
                                    sPArr[2] = new PointF(sPointF.X + (float)Math.Sqrt(3) * sSymbol.Size / 3, sPointF.Y - sSymbol.Size / 3);
                                    sValue = layer.Records.Rows[i][sField].ToString();
                                    sBrush.Color = renderer.FindColor(sValue);
                                    g.FillPolygon(sBrush, sPArr);
                                }
                            }
                            break;
                        case PointSymbol.PointStyleConstant.Ring:
                            for (int i = 0; i < layer.Records.Rows.Count; i++)
                            {
                                if (!Convert.IsDBNull(layer.Records.Rows[i][2]))
                                {
                                    sPointD = (PointD)layer.Records.Rows[i][2];
                                    sPointF = FromMapPoint(sPointD);
                                    sValue = layer.Records.Rows[i][sField].ToString();
                                    sPen.Color = renderer.FindColor(sValue);
                                    g.DrawEllipse(sPen, sPointF.X - sSymbol.Size / 2, sPointF.Y - sSymbol.Size / 2, sSymbol.Size, sSymbol.Size);
                                    g.DrawEllipse(sPen, sPointF.X - sSymbol.Size / 4, sPointF.Y - sSymbol.Size / 4, sSymbol.Size / 2, sSymbol.Size / 2);
                                }
                            }
                            break;
                        case PointSymbol.PointStyleConstant.FillRing:
                            for (int i = 0; i < layer.Records.Rows.Count; i++)
                            {
                                if (!Convert.IsDBNull(layer.Records.Rows[i][2]))
                                {
                                    sPointD = (PointD)layer.Records.Rows[i][2];
                                    sPointF = FromMapPoint(sPointD);
                                    sValue = layer.Records.Rows[i][sField].ToString();
                                    sPen.Color = renderer.FindColor(sValue);
                                    sBrush.Color = renderer.FindColor(sValue);
                                    g.DrawEllipse(sPen, sPointF.X - sSymbol.Size / 2, sPointF.Y - sSymbol.Size / 2, sSymbol.Size, sSymbol.Size);
                                    g.FillEllipse(sBrush, sPointF.X - sSymbol.Size / 4, sPointF.Y - sSymbol.Size / 4, sSymbol.Size / 2, sSymbol.Size / 2);
                                }
                            }
                            break;
                    }
                }
                else if (layer.GeoType == typeof(MultiPolyLine))
                {
                    LineSymbol sSymbol = (LineSymbol)renderer.DefaultSymbol;
                    Pen sPen = new Pen(sSymbol.SymbolColor, sSymbol.Width);
                    sPen.DashStyle = sSymbol.LineDashStyle;
                    MultiPolyLine sMultipolyline;
                    PolyLine sPolyline;
                    PointF[] sPoints;
                    for (int i = 0; i < layer.Records.Rows.Count; i++)
                    {
                        if (!Convert.IsDBNull(layer.Records.Rows[i][2]))
                        {
                            sMultipolyline = (MultiPolyLine)layer.Records.Rows[i][2];
                            for (int j = 0; j < sMultipolyline.Count; j++)
                            {
                                sPolyline = sMultipolyline.PolyLines[j];
                                sPoints = FromMapPoints(sPolyline.Points);
                                sValue = layer.Records.Rows[i][sField].ToString();
                                sPen.Color = renderer.FindColor(sValue);
                                g.DrawLines(sPen, sPoints);
                            }
                        }
                    }
                }

                else if (layer.GeoType == typeof(MultiPolygon))
                {
                    PolygonSymbol sSymbol = (PolygonSymbol)renderer.DefaultSymbol;
                    Pen sPen = new Pen(sSymbol.OutlineColor, sSymbol.OutlineWidth);
                    sPen.DashStyle = sSymbol.OutlineStyle;
                    SolidBrush sBrush = new SolidBrush(sSymbol.SymbolColor);

                    MultiPolygon sMultiPolygon;
                    Polygon sPolygon;
                    PointF[] sPoints;
                    GraphicsPath sGP = new GraphicsPath();
                    for (int i = 0; i < layer.Records.Rows.Count; i++)
                    {
                        if (!Convert.IsDBNull(layer.Records.Rows[i][2]))
                        {
                            sMultiPolygon = (MultiPolygon)layer.Records.Rows[i][2];
                            for (int j = 0; j < sMultiPolygon.Count; j++)
                            {
                                sPolygon = sMultiPolygon.Polygons[j];
                                sPoints = FromMapPoints(sPolygon.Points);
                                sValue = layer.Records.Rows[i][sField].ToString();
                                sPen.Color = sSymbol.OutlineColor;
                                sBrush.Color = renderer.FindColor(sValue);

                                sGP.AddPolygon(sPoints);
                            }
                            g.FillPath(sBrush, sGP);
                            g.DrawPath(sPen, sGP);
                            sGP.Reset();
                        }

                    }
                }
            }
            
        }

        /// <summary>
        /// 绘制分级渲染图层
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="g"></param>
        private void DrawClassBreakRenderer(Layer layer, Graphics g)
        {
            ClassBreakRenderer renderer = (ClassBreakRenderer)layer.LayerRenderer;
            string sField = renderer.Field;
            double sValue;
            if (sField != null)
            {

                if (layer.GeoType == typeof(PointD))
                {
                    PointSymbol sSymbol = (PointSymbol)renderer.DefaultSymbol;
                    Pen sPen = new Pen(sSymbol.SymbolColor);
                    SolidBrush sBrush = new SolidBrush(sSymbol.SymbolColor);
                    PointD sPointD;
                    PointF sPointF;
                    PointF[] sPArr = new PointF[3];
                    switch (sSymbol.Style)
                    {
                        case PointSymbol.PointStyleConstant.Circle:
                            for (int i = 0; i < layer.Records.Rows.Count; i++)
                            {
                                if (!Convert.IsDBNull(layer.Records.Rows[i][2]))
                                {
                                    sPointD = (PointD)layer.Records.Rows[i][2];
                                    sPointF = FromMapPoint(sPointD);
                                    sValue = Double.Parse(layer.Records.Rows[i][sField].ToString());
                                    sPen.Color = renderer.FindColor(sValue);
                                    g.DrawEllipse(sPen, sPointF.X - sSymbol.Size / 2, sPointF.Y - sSymbol.Size / 2, sSymbol.Size, sSymbol.Size);
                                }
                            }
                            break;
                        case PointSymbol.PointStyleConstant.FillCircle:
                            for (int i = 0; i < layer.Records.Rows.Count; i++)
                            {
                                if (!Convert.IsDBNull(layer.Records.Rows[i][2]))
                                {
                                    sPointD = (PointD)layer.Records.Rows[i][2];
                                    sPointF = FromMapPoint(sPointD);
                                    sValue = Double.Parse(layer.Records.Rows[i][sField].ToString());
                                    sBrush.Color = renderer.FindColor(sValue);
                                    g.FillEllipse(sBrush, sPointF.X - sSymbol.Size / 2, sPointF.Y - sSymbol.Size / 2, sSymbol.Size, sSymbol.Size);
                                }
                            }
                            break;
                        case PointSymbol.PointStyleConstant.Square:
                            for (int i = 0; i < layer.Records.Rows.Count; i++)
                            {
                                if (!Convert.IsDBNull(layer.Records.Rows[i][2]))
                                {
                                    sPointD = (PointD)layer.Records.Rows[i][2];
                                    sPointF = FromMapPoint(sPointD);
                                    sValue = Double.Parse(layer.Records.Rows[i][sField].ToString());
                                    sPen.Color = renderer.FindColor(sValue);
                                    g.DrawRectangle(sPen, sPointF.X - sSymbol.Size / 2, sPointF.Y - sSymbol.Size / 2, sSymbol.Size, sSymbol.Size);
                                }
                            }
                            break;
                        case PointSymbol.PointStyleConstant.FillSquare:
                            for (int i = 0; i < layer.Records.Rows.Count; i++)
                            {
                                if (!Convert.IsDBNull(layer.Records.Rows[i][2]))
                                {
                                    sPointD = (PointD)layer.Records.Rows[i][2];
                                    sPointF = FromMapPoint(sPointD);
                                    sValue = Double.Parse(layer.Records.Rows[i][sField].ToString());
                                    sBrush.Color = renderer.FindColor(sValue);
                                    g.FillRectangle(sBrush, sPointF.X - sSymbol.Size / 2, sPointF.Y - sSymbol.Size / 2, sSymbol.Size, sSymbol.Size);
                                }
                            }
                            break;
                        case PointSymbol.PointStyleConstant.Triangle:

                            for (int i = 0; i < layer.Records.Rows.Count; i++)
                            {
                                if (!Convert.IsDBNull(layer.Records.Rows[i][2]))
                                {
                                    sPointD = (PointD)layer.Records.Rows[i][2];
                                    sPointF = FromMapPoint(sPointD);
                                    sPArr[0] = new PointF(sPointF.X, sPointF.Y - 2 * sSymbol.Size / 3);
                                    sPArr[1] = new PointF(sPointF.X - (float)Math.Sqrt(3) * sSymbol.Size / 3, sPointF.Y + sSymbol.Size / 3);
                                    sPArr[2] = new PointF(sPointF.X + (float)Math.Sqrt(3) * sSymbol.Size / 3, sPointF.Y + sSymbol.Size / 3);
                                    sValue = Double.Parse(layer.Records.Rows[i][sField].ToString());
                                    sPen.Color = renderer.FindColor(sValue);
                                    g.DrawLines(sPen, sPArr);
                                }
                            }
                            break;
                        case PointSymbol.PointStyleConstant.FillTriangle:
                            for (int i = 0; i < layer.Records.Rows.Count; i++)
                            {
                                if (!Convert.IsDBNull(layer.Records.Rows[i][2]))
                                {
                                    sPointD = (PointD)layer.Records.Rows[i][2];
                                    sPointF = FromMapPoint(sPointD);
                                    sPArr[0] = new PointF(sPointF.X, sPointF.Y + 2 * sSymbol.Size / 3);
                                    sPArr[1] = new PointF(sPointF.X - (float)Math.Sqrt(3) * sSymbol.Size / 3, sPointF.Y - sSymbol.Size / 3);
                                    sPArr[2] = new PointF(sPointF.X + (float)Math.Sqrt(3) * sSymbol.Size / 3, sPointF.Y - sSymbol.Size / 3);
                                    sValue = Double.Parse(layer.Records.Rows[i][sField].ToString());
                                    sBrush.Color = renderer.FindColor(sValue);
                                    g.FillPolygon(sBrush, sPArr);
                                }
                            }
                            break;
                        case PointSymbol.PointStyleConstant.Ring:
                            for (int i = 0; i < layer.Records.Rows.Count; i++)
                            {
                                if (!Convert.IsDBNull(layer.Records.Rows[i][2]))
                                {
                                    sPointD = (PointD)layer.Records.Rows[i][2];
                                    sPointF = FromMapPoint(sPointD);
                                    sValue = Double.Parse(layer.Records.Rows[i][sField].ToString());
                                    sPen.Color = renderer.FindColor(sValue);
                                    g.DrawEllipse(sPen, sPointF.X - sSymbol.Size / 2, sPointF.Y - sSymbol.Size / 2, sSymbol.Size, sSymbol.Size);
                                    g.DrawEllipse(sPen, sPointF.X - sSymbol.Size / 4, sPointF.Y - sSymbol.Size / 4, sSymbol.Size / 2, sSymbol.Size / 2);
                                }
                            }
                            break;
                        case PointSymbol.PointStyleConstant.FillRing:
                            for (int i = 0; i < layer.Records.Rows.Count; i++)
                            {
                                if (!Convert.IsDBNull(layer.Records.Rows[i][2]))
                                {
                                    sPointD = (PointD)layer.Records.Rows[i][2];
                                    sPointF = FromMapPoint(sPointD);
                                    sValue = Double.Parse(layer.Records.Rows[i][sField].ToString());
                                    sPen.Color = renderer.FindColor(sValue);
                                    sBrush.Color = sPen.Color;
                                    g.DrawEllipse(sPen, sPointF.X - sSymbol.Size / 2, sPointF.Y - sSymbol.Size / 2, sSymbol.Size, sSymbol.Size);
                                    g.FillEllipse(sBrush, sPointF.X - sSymbol.Size / 4, sPointF.Y - sSymbol.Size / 4, sSymbol.Size / 2, sSymbol.Size / 2);
                                }
                            }
                            break;
                    }
                }
                else if (layer.GeoType == typeof(MultiPolyLine))
                {
                    LineSymbol sSymbol = (LineSymbol)renderer.DefaultSymbol;
                    Pen sPen = new Pen(sSymbol.SymbolColor, sSymbol.Width);
                    sPen.DashStyle = sSymbol.LineDashStyle;
                    MultiPolyLine sMultipolyline;
                    PolyLine sPolyline;
                    PointF[] sPoints;
                    for (int i = 0; i < layer.Records.Rows.Count; i++)
                    {
                        if (!Convert.IsDBNull(layer.Records.Rows[i][2]))
                        {
                            sMultipolyline = (MultiPolyLine)layer.Records.Rows[i][2];
                            for (int j = 0; j < sMultipolyline.Count; j++)
                            {
                                sPolyline = sMultipolyline.PolyLines[j];
                                sPoints = FromMapPoints(sPolyline.Points);
                                sValue = Double.Parse(layer.Records.Rows[i][sField].ToString());
                                sPen.Color = renderer.FindColor(sValue);
                                g.DrawLines(sPen, sPoints);
                            }
                        }
                    }
                }

                else if (layer.GeoType == typeof(MultiPolygon))
                {
                    PolygonSymbol sSymbol = (PolygonSymbol)renderer.DefaultSymbol;
                    Pen sPen = new Pen(sSymbol.OutlineColor, sSymbol.OutlineWidth);
                    sPen.DashStyle = sSymbol.OutlineStyle;
                    SolidBrush sBrush = new SolidBrush(sSymbol.SymbolColor);

                    MultiPolygon sMultiPolygon;
                    Polygon sPolygon;
                    PointF[] sPoints;
                    GraphicsPath sGP = new GraphicsPath();
                    for (int i = 0; i < layer.Records.Rows.Count; i++)
                    {
                        if (!Convert.IsDBNull(layer.Records.Rows[i][2]))
                        {
                            sMultiPolygon = (MultiPolygon)layer.Records.Rows[i][2];
                            for (int j = 0; j < sMultiPolygon.Count; j++)
                            {
                                sPolygon = sMultiPolygon.Polygons[j];
                                sPoints = FromMapPoints(sPolygon.Points);
                                sValue = Double.Parse(layer.Records.Rows[i][sField].ToString());
                                sPen.Color = sSymbol.OutlineColor;
                                sBrush.Color = renderer.FindColor(sValue);
                                sGP.AddPolygon(sPoints);
                            }
                            g.FillPath(sBrush, sGP);
                            g.DrawPath(sPen, sGP);
                            sGP.Reset();
                        }

                    }
                }
            }

        }

        #endregion

        /// <summary>
        /// 绘制地图可变要素。包括：地图选中的要素，正在跟踪的要素，拉框
        /// </summary>
        /// <param name="g"></param>
        public void DrawLabileMap(Graphics g)
        {
            //(1)绘制地图选中的要素
            if (this._State != MCState.EditFeature)
            {
                foreach (Layer sLayer in _Map.Layers)
                {
                    if (sLayer.SelectedRecords.Count > 0)
                    {
                        if (sLayer.GeoType == typeof(PointD))
                        {
                            DrawSimpleRendererFeatures(g, sLayer.SelectedRecords, _SelectedPointSymbol, sLayer.GeoType);
                        }
                        if (sLayer.GeoType == typeof(MultiPolyLine))
                        {
                            DrawSimpleRendererFeatures(g, sLayer.SelectedRecords, _SelectedLineSymbol, sLayer.GeoType);
                        }
                        if (sLayer.GeoType == typeof(MultiPolygon))
                        {
                            DrawSimpleRendererFeatures(g, sLayer.SelectedRecords, _SelectedPolygonSymbol, sLayer.GeoType);
                        }
                    }
                }
            }

            //(2)绘制地图正在跟踪的要素
            if (this._State == MCState.TrackFeature)
            {
                mdTrackingFeature.Push(ToMapPoint(mMouseLocation));
                if (_Map.EditingLayer.GeoType == typeof(MultiPolygon))
                {
                    if (mdTrackingFeature.Count >= 2)
                    {
                        g.DrawPolygon(_TrackPen, FromMapPoints(mdTrackingFeature.ToArray()));
                    }
                }
                else if (_Map.EditingLayer.GeoType == typeof(MultiPolyLine))
                {
                    if (mdTrackingFeature.Count >= 2)
                    {
                        g.DrawLines(_TrackPen, FromMapPoints(mdTrackingFeature.ToArray()));
                    }

                }
                else if (_Map.EditingLayer.GeoType == typeof(PointD))
                {
                    //点不存在“绘制中”的状态，所以Do Nothing

                    //if (mdTrackingFeature.Count >= 2)
                    //{
                    //    g.FillEllipse()
                    //    g.DrawLine(_TrackPen, FromMapPoint(mdTrackingFeature.ToArray()[0]), FromMapPoint(mdTrackingFeature.ToArray()[0]));
                    //}
                }
                mdTrackingFeature.Pop();
            }

            //(3)绘制地图正在编辑的要素，包括阴影和真实要素
            if (this._State == MCState.EditFeature)
            {
                if (_Map.EditingLayer.EditingRecord != null && _EditingGeometry != null && _Map.EditingLayer.EditingRecord.Count > 0)
                {
                    if (_Map.EditingLayer.GeoType == typeof(MultiPolygon))
                    {
                        MultiPolygon sMultiPolygon;
                        PointF[] sPoints;

                        //绘制阴影
                        //DrawSimpleRendererFeatures(g, _EditingGeometry, _PolygonShadowSymbol, _Map.EditingLayer.GeoType);

                        //绘制真实图形
                        GraphicsPath sGP = new GraphicsPath();
                        List<PointF[]> sPointsList = new List<PointF[]>();
                        foreach (DataRow sRow in _Map.EditingLayer.EditingRecord)
                        {
                            sMultiPolygon = (MultiPolygon)sRow[2];
                            if (sMultiPolygon != null)
                            {
                                foreach (Polygon sPolygon in sMultiPolygon.Polygons)
                                {
                                    sPoints = FromMapPoints(sPolygon.Points);
                                    sGP.AddPolygon(sPoints);
                                    sPointsList.Add(sPoints);
                                }
                                g.DrawPath(_TrackPen, sGP);
                                sGP.Reset();

                                foreach (PointF[] sps in sPointsList)
                                {
                                    foreach (PointF sPoint in sps)
                                    {
                                        g.FillRectangle(_VertexBrush, sPoint.X - _VertexWidth / 2, sPoint.Y - _VertexWidth / 2, _VertexWidth, _VertexWidth);
                                    }
                                }

                            }
                        }
                    }
                    else if (_Map.EditingLayer.GeoType == typeof(MultiPolyLine))
                    {
                        MultiPolyLine sMultiPoltline;
                        PointF[] sPoints;


                        //绘制阴影
                       // DrawSimpleRendererFeatures(g, _EditingGeometry, _LineShadowSymbol, _Map.EditingLayer.GeoType);

                        //绘制真实图形
                        foreach (DataRow sRow in _Map.EditingLayer.EditingRecord)
                        {
                            sMultiPoltline = (MultiPolyLine)sRow[2];
                            if (sMultiPoltline != null)
                            {
                                foreach (PolyLine sPolyline in sMultiPoltline.PolyLines)
                                {
                                    sPoints = FromMapPoints(sPolyline.Points);
                                    g.DrawLines(_TrackPen, sPoints);  //画线
                                    //画点
                                    foreach (PointF sPoint in sPoints)
                                    {
                                        g.FillRectangle(_VertexBrush, sPoint.X - _VertexWidth / 2, sPoint.Y - _VertexWidth / 2, _VertexWidth, _VertexWidth);
                                    }
                                }
                            }
                        }
                    }

                    else if (_Map.EditingLayer.GeoType == typeof(PointD))
                    {
                        PointD sPointD;
                        PointF sPointF;


                        //绘制阴影
                        //DrawSimpleRendererFeatures(g, _EditingGeometry, _PointShadowSymbol, _Map.EditingLayer.GeoType);

                        //绘制真实图形
                        foreach (DataRow sRow in _Map.EditingLayer.EditingRecord)
                        {
                            sPointD = (PointD)sRow[2];
                            if (sPointD != null)
                            {
                                sPointF = FromMapPoint(sPointD);
                                g.FillRectangle(_VertexBrush, sPointF.X - _VertexWidth / 2, sPointF.Y - _VertexWidth / 2, _VertexWidth, _VertexWidth);
                            }
                        }
                    }
                }
            }

            //(4)绘制矩形框
            if (mDragging)
            {
                g.DrawRectangle(_DragBoxPen, mdRect);
            }

        }

        /// <summary>
        /// 绘制地图固定要素。包括：先各图层的样式，再各图层的注记
        /// </summary>
        private void DrawFixedMap()
        {
            cts = new CancellationTokenSource();

            Bitmap sBitMap = new Bitmap(this.Width, this.Height);
            Graphics g = Graphics.FromImage(sBitMap);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            mOffsetX = mOffsetX + mBitMapOffsetX * _MapScale;
            mOffsetY = mOffsetY - mBitMapOffsetY * _MapScale;
            mBitMapOffsetX = 0;
            mBitMapOffsetY = 0;

            if (_Map != null && _Map.Visible)
            {
                Layer sLayer;
                //(1)绘制各图层的样式
                for (int i = _Map.Layers.Count - 1; i >= 0; --i)
                {
                    sLayer = _Map.Layers[i];
                    if (sLayer.Visible)
                    {
                        DrawFixedLayer(g, sLayer);
                    }
                    if (cts.IsCancellationRequested) { break; }
                }

                //(2)绘制各图层的注记
                for (int i = _Map.Layers.Count - 1; i >= 0; --i)
                {
                    sLayer = _Map.Layers[i];
                    if (sLayer.Visible)
                    {
                        DrawLayerTextLabel(g, sLayer);
                    }
                    if (cts.IsCancellationRequested) { break; }
                }
            }

            //(3)绘制完毕,替换原有BitMap
            cts = null;
            lock (_FixedBitMap)
            {
                _FixedBitMap.Dispose();
                _FixedBitMap = sBitMap;
            }
        }


        #region 异步绘制方法
        private delegate void DrawFixedBitMapHandler();

        public void RefreshAllAsync()
        {
            //同一时间只能有一个线程在绘制，所以要取消上次绘制的线程
            if (cts != null)
            {
                cts.Cancel();
            }
            if (drawHandler != null)
            {
                drawHandler.EndInvoke(iar);
            }

            drawHandler = new DrawFixedBitMapHandler(DrawFixedMap);
            iar = drawHandler.BeginInvoke(RefreshAfterDraw, "DrawFinished");
        }


        private delegate void RefreshHandler();
        /// <summary>
        /// 固定图层绘制完毕后立即刷新
        /// </summary>
        /// <param name="iar"></param>
        private void RefreshAfterDraw(IAsyncResult iarr)
        {
            //DrawFixedBitMapHandler drawHander = (DrawFixedBitMapHandler)((AsyncResult)iar).AsyncDelegate;

            if (InvokeRequired)
            {
                this.Invoke(new RefreshHandler(this.Refresh));
            }
            else
            {
                this.Refresh();//刷新显示
            }

        }
        #endregion


        #endregion

        #region 事件
        public delegate void DisplayScaleChangedHandle(object sender, double scale);
        /// <summary>
        /// 显示的比例尺发生了变化
        /// </summary>
        public event DisplayScaleChangedHandle DisplayScaleChanged;


        public delegate void LayerSymbolChangedHandle(object sender);
        /// <summary>
        /// 显示的比例尺发生了变化
        /// </summary>
        public event LayerSymbolChangedHandle LayerSymbolChanged;

        private void _Map_MapPerformaceChanged(object sender)
        {
            this.RefreshAllAsync();    //重绘地图
        }

        private void _Map_LayerAdded(object sender, Layer layer)
        {
            this.ZoomByBox(layer.MBR);  //缩放至该图层大小
        }

        private void _Map_LayerSymbolChanged(object sender)
        {
            LayerSymbolChanged?.Invoke(sender);
        }


        #endregion

        #region 控件母版事件处理

        private void MapControl_Load(object sender, EventArgs e)
        {
            //DrawFixedBitMap();
            this.ResizeRedraw = false;
            this.Pan();
        }

        /// <summary>
        /// 地图重绘，此时重新贴图，然后绘制上层数据,如果没有在拖动，就重新绘制底图。地图Resize时会自动调用这个方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MapControl_Paint(object sender, PaintEventArgs e)
        {
            //Console.WriteLine("Paint");
            Graphics g = e.Graphics;
            //g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            //g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            //g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            //(1)贴上固定的图层
            lock (_FixedBitMap)
            {
                g.DrawImage(_FixedBitMap, -mBitMapOffsetX, -mBitMapOffsetY);//填充为一级地图
            }

            //(2)绘制可变图层
            if (_Map != null && _Map.Visible)
            {
                DrawLabileMap(g);
            }

        }

        //会自动调用Paint方法
        private void MapControl_Resize(object sender, EventArgs e)
        {
            //Console.WriteLine("resize:" + this.Size);
            if (!mResizing)
            {
                RefreshAllAsync();
            }

            //mBitMapOffsetX = (float)(_FixedBitMap.Width - this.Width) / 2;
            //mBitMapOffsetY = (float)(_FixedBitMap.Height - this.Height) / 2;
        }

        private void MapControl_MouseDown(object sender, MouseEventArgs e)
        {
            switch (_State)
            {
                case MCState.NoOper:    //无操作
                    break;

                case MCState.Pan:   //漫游操作
                    if (e.Button == MouseButtons.Left)
                    {
                        mMouseLocation = e.Location;
                    }
                    break;

                case MCState.ZoomIn:    //放大操作
                    if (e.Button == MouseButtons.Left)
                    {
                        mStartPoint = e.Location;
                        mdRect = new Rectangle(e.X, e.Y, e.X - mStartPoint.X, e.Y - mStartPoint.Y);
                        mDragging = true;
                    }
                    break;

                case MCState.ZoomOut:   //缩小操作
                    if (e.Button == MouseButtons.Left)
                    {
                        mStartPoint = e.Location;
                        mdRect = new Rectangle(e.X, e.Y, e.X - mStartPoint.X, e.Y - mStartPoint.Y);
                        mDragging = true;
                    }
                    break;

                case MCState.SelectFeature: //选择要素操作
                    if (e.Button == MouseButtons.Left)
                    {
                        mStartPoint = e.Location;
                        mdRect = new Rectangle(e.X, e.Y, e.X - mStartPoint.X, e.Y - mStartPoint.Y);
                        mDragging = true;
                        //点选要素
                        //_Map.SelectByPoint(ToMapPoint(e.Location), mSelectByPointTolerance * _MapScale);
                        //this.Refresh();
                    }
                    break;

                case MCState.Identify:  //识别要素操作
                    break;

                case MCState.EditFeature:   //编辑要素操作
                    if (_Map.Editing)
                    {
                        if (mDraggingPoint == null)
                        {
                            _EditingGeometry = _Map.EditingSelectByPoint(ToMapPoint(e.Location), _SelectByPointTolerance * _MapScale);
                            if (_EditingGeometry.Count > 0)
                            {
                                Refresh();
                            }
                        }

                        //拖拽点的时候
                        else
                        {
                            mStartPoint = e.Location;
                            mMouseLocation = e.Location;
                        }
                    }

                    break;

                case MCState.TrackFeature:  //描绘要素操作
                    //增加点
                    if (e.Button == MouseButtons.Left && e.Clicks == 1)
                    {
                        mdTrackingFeature.Push(ToMapPoint(e.Location));
                        if (_Map.EditingLayer.GeoType == typeof(PointD))
                        {
                            DataRow newRow = _Map.EditingLayer.Records.NewRow();
                            PointD mp = mdTrackingFeature.Pop();
                            newRow[2] = mp;
                            newRow[1] = _Map.PrjSystem.ToLngLat(mp);

                            _Map.EditingLayer.AddRecord(newRow);

                            mdTrackingFeature.Clear();
                            RefreshAllAsync();
                        }
                        else
                        {
                            this.Refresh();
                        }

                    }

                    //完成绘制，点击两次的时候也包含一次
                    if (e.Button == MouseButtons.Left && e.Clicks == 2)
                    {
                        if (_Map.EditingLayer.GeoType == typeof(PointD))
                        {

                        }
                        else
                        {
                            if (mdTrackingFeature.Count >= 3)
                            {
                                //Console.WriteLine("tracking finnished,points num:" + mdTrackingFeature.Count);
                                DataRow newRow = _Map.EditingLayer.Records.NewRow();

                                if (_Map.EditingLayer.GeoType == typeof(MultiPolygon))
                                {
                                    List<Polygon> p = new List<Polygon>();
                                    p.Add(new Polygon(mdTrackingFeature.ToArray()));
                                    MultiPolygon mp = new MultiPolygon(p);
                                    newRow[2] = mp;
                                    newRow[1] = _Map.PrjSystem.ToLngLat(mp);
                                }
                                else if (_Map.EditingLayer.GeoType == typeof(MultiPolyLine))
                                {
                                    List<PolyLine> p = new List<PolyLine>();
                                    p.Add(new PolyLine(mdTrackingFeature.ToArray()));
                                    MultiPolyLine mp = new MultiPolyLine(p);
                                    newRow[2] = mp;
                                    newRow[1] = _Map.PrjSystem.ToLngLat(mp);
                                }

                                _Map.EditingLayer.AddRecord(newRow);
                                mdTrackingFeature.Clear();
                                RefreshAllAsync();
                            }
                        }
                    }

                    //删除点
                    else if (e.Button == MouseButtons.Right)
                    {
                        if (mdTrackingFeature.Count != 0)
                        {
                            mdTrackingFeature.Pop();
                            this.Refresh();
                        }
                    }
                    break;
            }

        }

        private void MapControl_MouseMove(object sender, MouseEventArgs e)
        {
            switch (_State)
            {
                case MCState.NoOper:    //无操作
                    break;

                case MCState.Pan:   //漫游操作
                    if (e.Button == MouseButtons.Left)
                    {
                        PointD sPrePoint = ToMapPoint(mMouseLocation);//将上一次鼠标的位置转化为地图坐标
                        PointD sCurPoint = ToMapPoint(e.Location);
                        //修改offset变量
                        mBitMapOffsetX = mBitMapOffsetX + (float)((sPrePoint.X - sCurPoint.X) / _MapScale);
                        mBitMapOffsetY = mBitMapOffsetY - (float)((sPrePoint.Y - sCurPoint.Y) / _MapScale);
                        //刷新
                        Refresh();
                        //修改mMouseLocation
                        mMouseLocation = e.Location;
                    }
                    break;

                case MCState.ZoomIn:    //放大操作
                    if (e.Button == MouseButtons.Left)
                    {
                        mdRect.X = Math.Min(mStartPoint.X, e.X);
                        mdRect.Width = Math.Max(mStartPoint.X, e.X) - mdRect.X;
                        mdRect.Y = Math.Min(mStartPoint.Y, e.Y);
                        mdRect.Height = Math.Max(mStartPoint.Y, e.Y) - mdRect.Y;
                        Refresh();
                    }
                    break;

                case MCState.ZoomOut:   //缩小操作
                    if (e.Button == MouseButtons.Left)
                    {
                        mdRect.X = Math.Min(mStartPoint.X, e.X);
                        mdRect.Width = Math.Max(mStartPoint.X, e.X) - mdRect.X;
                        mdRect.Y = Math.Min(mStartPoint.Y, e.Y);
                        mdRect.Height = Math.Max(mStartPoint.Y, e.Y) - mdRect.Y;
                        Refresh();
                    }
                    break;

                case MCState.SelectFeature: //选择要素操作
                    if (e.Button == MouseButtons.Left)
                    {
                        mdRect.X = Math.Min(mStartPoint.X, e.X);
                        mdRect.Width = Math.Max(mStartPoint.X, e.X) - mdRect.X;
                        mdRect.Y = Math.Min(mStartPoint.Y, e.Y);
                        mdRect.Height = Math.Max(mStartPoint.Y, e.Y) - mdRect.Y;
                        this.Refresh();
                    }
                    break;

                case MCState.Identify:  //识别要素操作
                    break;

                case MCState.EditFeature:   //编辑要素操作
                    if (e.Button == MouseButtons.Left)
                    {
                        if (mDraggingPoint != null)
                        {
                            PointD sPrePoint = ToMapPoint(mMouseLocation);//将上一次鼠标的位置转化为地图坐标
                            PointD sCurPoint = ToMapPoint(e.Location);
                            //修改坐标
                            mDraggingPoint.X = mDraggingPoint.X - (sPrePoint.X - sCurPoint.X);
                            mDraggingPoint.Y = mDraggingPoint.Y - (sPrePoint.Y - sCurPoint.Y);
                            //刷新
                            //Console.WriteLine("Dragginf");
                            Refresh();
                            //修改mMouseLocation
                            mMouseLocation = e.Location;
                        }

                    }
                    else
                    {
                        if (_Map.Editing && _EditingGeometry != null)
                        {
                            mDraggingPoint = Layer.GetClosePoint(ToMapPoint(e.Location), _Map.EditingLayer.EditingRecord, _SelectByPointTolerance * _MapScale);
                            if (mDraggingPoint != null)
                            {
                                this.Cursor = Cursors.SizeAll;
                            }
                            else { this.Cursor = mEditCursor; }
                        }
                    }


                    break;

                case MCState.TrackFeature:  //描绘要素操作
                    mMouseLocation = e.Location;
                    if (mdTrackingFeature.Count != 0)
                    {
                        Refresh();
                    }
                    break;
            }
        }

        private void MapControl_MouseUp(object sender, MouseEventArgs e)
        {
            switch (_State)
            {
                case MCState.NoOper:    //无操作
                    break;

                case MCState.Pan:   //漫游操作
                    if (e.Button == MouseButtons.Left)
                    {
                        RefreshAllAsync();
                    }
                    break;

                case MCState.ZoomIn:    //放大操作
                    if (e.Button == MouseButtons.Left)
                    {
                        //点放大
                        mDragging = false;
                        if (mdRect.Height <= _RECT_TOLERANCE && mdRect.Width <= _RECT_TOLERANCE)
                        {
                            ZoomByCenter(ToMapPoint(e.Location), muZoomRatio);
                        }
                        //拉框放大
                        else
                        {
                            ZoomByBox(ToMapRect(mdRect));
                        }
                        //DrawFixedBitMap();
                        //Refresh();
                        RefreshAllAsync();
                    }
                    break;

                case MCState.ZoomOut:   //缩小操作
                    if (e.Button == MouseButtons.Left)
                    {
                        mDragging = false;
                        if (mdRect.Height <= _RECT_TOLERANCE && mdRect.Width <= _RECT_TOLERANCE)
                        {
                            ZoomByCenter(ToMapPoint(e.Location), 1 / muZoomRatio);
                        }
                        else
                        {
                            float sWidth = (float)(this.Width * this.Width) / mdRect.Width;
                            float sHeight = (float)(this.Height * this.Height) / mdRect.Height;
                            RectangleF sRect = new RectangleF((this.Width - sWidth) / 2, (this.Height - sHeight) / 2, sWidth, sHeight);
                            ZoomByBox(ToMapRect(sRect));
                        }
                        RefreshAllAsync();
                    }

                    break;

                case MCState.SelectFeature: //选择要素操作
                    mDragging = false;

                    if (mdRect.Height <= _RECT_TOLERANCE && mdRect.Width <= _RECT_TOLERANCE)
                    {
                        //点选要素
                        _Map.SelectByPoint(ToMapPoint(e.Location), _SelectByPointTolerance * _MapScale);
                    }
                    else
                    {
                        //框选要素
                        _Map.SelectByBox(ToMapRect(mdRect));
                    }
                    //Refresh();
                    RefreshAllAsync();
                    break;

                case MCState.Identify:  //识别要素操作
                    break;

                case MCState.EditFeature:   //编辑要素操作
                    RefreshAllAsync();
                    break;

                case MCState.TrackFeature:  //描绘要素操作
                    break;
            }

        }

        private void MapControl_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            switch (_State)
            {
                case MCState.NoOper:    //无操作
                    break;

                case MCState.Pan:   //漫游操作
                    break;

                case MCState.ZoomIn:    //放大操作
                    break;

                case MCState.ZoomOut:   //缩小操作
                    break;

                case MCState.SelectFeature: //选择要素操作
                    break;

                case MCState.Identify:  //识别要素操作
                    break;

                case MCState.EditFeature:   //编辑要素操作
                    break;

                case MCState.TrackFeature:  //描绘要素操作
                    break;
            }
        }

        private void MapControl_MouseWheel(object sender, MouseEventArgs e)
        {
            if (muAutoSize)
            {
                PointD sPoint = ToMapPoint(e.Location);

                if (e.Delta > 0)
                {
                    ZoomByCenter(sPoint, muZoomRatio);//放大
                    RefreshAllAsync();
                }
                else if (e.Delta < 0)
                {
                    ZoomByCenter(sPoint, 1 / muZoomRatio);//缩小
                    RefreshAllAsync();
                }
            }
        }

        #endregion
    }
}
