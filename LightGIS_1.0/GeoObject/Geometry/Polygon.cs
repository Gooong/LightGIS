using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightGIS_1._0.GeoObject
{
    class Polygon : Geometry
    {
        #region 字段
        private PointD[] _Points;
        private RectangleD _MBR;//最小外包矩形
        private bool _IsIsland;//多边形是否是岛
        #endregion

        #region 构造函数
        //public Polygon()
        //{
        //    _Points = null;
        //    _MBR = new RectangleD();
        //}
        public Polygon(PointD[] points, RectangleD rect = null)
        {
            _Points = points;
            if (rect == null)
            {
                double mMinX = double.MaxValue, mMinY = double.MaxValue, mMaxX = double.MinValue, mMaxY = double.MinValue;
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

        //数目
        public int Count
        {
            get { return _Points.Length; }
        }
        
        //最小外包矩形
        public RectangleD MBR
        {
            get { return _MBR; }
        }

        //是否为岛
        public bool IsIsland
        {
            get { return _IsIsland; }
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
            _Points.CopyTo(mPoints, _Points.Length);
            return new PolyLine(mPoints, _MBR.Clone());
        }

        //判断并设置自己是否为岛
        public void RefreshIslandState()
        {
            double mMaxY = double.MinValue;
            int mMaxPosition = 0;//最高点的位置
            for(int i = 0; i < _Points.Length; i++)
            {
                if (_Points[i].Y > mMaxY)
                {
                    mMaxPosition = i;
                    mMaxY = _Points[i].Y;
                }
            }

            PointD mLeftPoint, mMiddlePoint, mRightPoint;
            mMiddlePoint = _Points[mMaxPosition];
            if (mMaxPosition == 0)
            {
                mLeftPoint = _Points[_Points.Length - 1];
                mRightPoint = _Points[mMaxPosition + 1];
            }
            else if (mMaxPosition == _Points.Length - 1)
            {
                mLeftPoint = _Points[mMaxPosition - 1];
                mRightPoint = _Points[0];
            }
            else
            {
                mLeftPoint = _Points[mMaxPosition - 1];
                mRightPoint = _Points[mMaxPosition + 1];
            }

            //求叉积
            double mCrossVal = (mMiddlePoint.X - mLeftPoint.X) * (mRightPoint.Y - mMiddlePoint.Y) - (mRightPoint.X - mMiddlePoint.X) * (mMiddlePoint.Y - mLeftPoint.Y);

            _IsIsland = (mCrossVal < 0);
        }

        #endregion
    }
}
