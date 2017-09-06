using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightGIS_1._0.GeoObject
{
    public class MultiPolyLine : Geometry
    {
        #region 字段
        List<PolyLine> _polyLines;
        RectangleD _MBR;
        #endregion

        #region 构造函数
        //public MultiPolyLine()
        //{
        //    _polyLines = new List<PolyLine>();
        //    _MBR = new RectangleD();
        //}

        public MultiPolyLine(List<PolyLine> polyLines,RectangleD MBR = null)
        {
            _polyLines = polyLines;
            if (MBR != null)
            {
                _MBR = MBR;
            }
            else
            {
                double mMinX = double.MaxValue, mMinY = double.MaxValue, mMaxX = double.MinValue, mMaxY = double.MinValue;
                RectangleD mRect;
                foreach (PolyLine mPolyLine in polyLines)
                {
                    mRect = mPolyLine.MBR;
                    if (mRect.MinX < mMinX) { mMinX = mRect.MinX; }
                    if (mRect.MinY < mMinY) { mMinY = mRect.MinY; }
                    if (mRect.MaxX > mMaxX) { mMaxX = mRect.MaxX; }
                    if (mRect.MaxY > mMaxY) { mMaxY = mRect.MaxY; }
                }
                _MBR = new RectangleD(mMinX, mMaxX, mMinY, mMaxY);
            }
        }

        #endregion

        #region 属性
        public List<PolyLine> PolyLines
        {
            get { return _polyLines; }
            set { _polyLines = value; }
        }
        public override RectangleD MBR
        {
            get { return _MBR; }
        }
        public int Count
        {
            get { return _polyLines.Count; }
        }
        #endregion

        #region 方法
        public PolyLine GetPolygon(int index)
        {
            return _polyLines[index];
        }

        public void Clear()
        {
            _polyLines = new List<PolyLine>();
            _MBR = new RectangleD();
        }

        public MultiPolyLine Clone()
        {
            List<PolyLine> sPolylines = new List<PolyLine>();
            foreach (PolyLine sPolyline in _polyLines)
            {
                sPolylines.Add(sPolyline.Clone());
            }
            return new MultiPolyLine(sPolylines, _MBR.Clone());
        }
        #endregion
    }
}
