using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightGIS_1._0.GeoObject
{
    public class MultiPolygon : Geometry
    {
        #region 字段
        List<Polygon> _Polygons;
        RectangleD _MBR;
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造多多边形
        /// </summary>
        /// <param name="polygons">多边形组</param>
        /// <param name="MBR">最小外包矩形</param>
        /// <param name="needRefreash">每个多边形是否需要判断岛属性</param>
        public MultiPolygon(List<Polygon> polygons,RectangleD MBR = null,bool needRefreash = true)
        {
            _Polygons = polygons;

            if (needRefreash)
            {
                //判断并设置自己是否为岛
                foreach (Polygon mPolygon in polygons)
                {
                    mPolygon.RefreshIslandState();
                }
            }


            //生成MBR
            if (MBR != null)
            {
                _MBR = MBR;
            }
            else
            {
                double mMinX = double.MaxValue, mMinY = double.MaxValue, mMaxX = double.MinValue, mMaxY = double.MinValue;
                RectangleD mRect;
                foreach (Polygon mPolygon in polygons)
                {
                    mRect = mPolygon.MBR;
                    if (mRect.MinX < mMinX) { mMinX = mRect.MinX; }
                    if (mRect.MinY < mMinY) { mMinY = mRect.MinY; }
                    if (mRect.MaxX > mMaxX) { mMaxX = mRect.MaxX; }
                    if (mRect.MaxY > mMaxY) { mMaxY = mRect.MaxY; }
                }
                _MBR = new RectangleD(mMinX, mMaxX, mMinY, mMaxY);
            }

            //
        }
        #endregion

        #region 属性
        public List<Polygon> Polygons
        {
            get { return _Polygons; }
            set { _Polygons = value; }
        }

        public int Count
        {
            get { return _Polygons.Count; }
        }

        public override RectangleD MBR
        {
            get { return _MBR; }
        }
        #endregion


        #region 方法
        public Polygon GetPolygon(int index)
        {
            return _Polygons[index];
        }

        public void Clear()
        {
            _Polygons = new List<Polygon>();
            _MBR = new RectangleD();
        }

        public MultiPolygon Clone()
        {
            List<Polygon> mPolygons = new List<Polygon>();
            foreach(Polygon sPolygon in _Polygons)
            {
                mPolygons.Add(sPolygon.Clone());
            }
            return new MultiPolygon(mPolygons, _MBR.Clone(), false);
        }
        #endregion
    }
}
