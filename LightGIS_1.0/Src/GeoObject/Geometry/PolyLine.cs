using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightGIS_1._0.GeoObject
{
    public class PolyLine : Geometry
    {
        #region 字段
        //private List<PointD> _Points = new List<PointD>();//顶点序列，最后一项不包含首项
        private PointD[] _Points;
        private RectangleD _MBR;//最小外包矩形
        #endregion

        #region 构造函数
        //public PolyLine()
        //{
        //    _Points = new PointD[0];
        //    _MBR = new RectangleD();
        //}
        //输入顶点序列和最小外包矩形构造
        public PolyLine(PointD[] points,RectangleD rect = null)
        {
            _Points = points;
            if (rect == null)
            {
                double mMinX=double.MaxValue, mMinY=double.MaxValue, mMaxX=double.MinValue, mMaxY=double.MinValue;
                foreach (PointD mP in _Points)
                {
                    if (mP.X < mMinX)
                    {
                        mMinX = mP.X;
                    }
                    if (mP.Y < mMinY)
                    {
                        mMinY = mP.Y;
                    }
                    if (mP.X > mMaxX)
                    {
                        mMaxX = mP.X;
                    }
                    if (mP.Y > mMaxY)
                    {
                        mMaxY = mP.Y;
                    }
                }
                _MBR = new RectangleD(mMinX, mMaxX, mMinY, mMaxY);
            }
            else
            {
                _MBR = rect;
            }
        }
        #endregion

        #region 属性
        public PointD[] Points
        {
            get { return _Points; }
            set { _Points = value; }
        }
        public int Count
        {
            get { return _Points.Length; }
        }
        public override RectangleD MBR
        {
            get { return _MBR; }
        }
        #endregion

        #region 方法
        public PointD GetPoint(int index)
        {
            return _Points[index];
        }

        ////加入点，效率不高，用到的较少
        //public void AddPoint(PointD point)
        //{
        //    PointD[] mPoints = new PointD[_Points.Length + 1];
        //    for(int i = 0; i < _Points.Length; i++)
        //    {
        //        mPoints[i] = _Points[i];
        //    }
        //    mPoints[_Points.Length + 1] = point;
        //    _Points = mPoints;
        //}

        public void Clear()
        {
            _Points = null;
            _MBR = new RectangleD();
        }

        public PolyLine Clone()
        {
            PointD[] mPoints = new PointD[_Points.Length];
            for (int i = 0; i < _Points.Length; i++)
            {
                mPoints[i] = _Points[i].Clone();
            }
            return new PolyLine(mPoints, _MBR.Clone());
        }

        #endregion
    }
}
