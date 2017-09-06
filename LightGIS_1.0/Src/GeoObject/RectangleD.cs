using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightGIS_1._0.GeoObject
{
    public class RectangleD
    {
        #region 字段
        private double _MinX, _MaxX, _MinY, _MaxY;
        #endregion

        #region 构造函数
        public RectangleD()
        {
            _MinX = 0;
            _MaxX = 0;
            _MinY = 0;
            _MaxY = 0;
        }
        public RectangleD(double minX, double maxX, double minY, double maxY)
        {
            //if ((minX > maxX) || (minY > maxY))
            //{
            //    throw new Exception("Invalid rectangle!");
            //}
            _MinX = minX;
            _MaxX = maxX;
            _MinY = minY;
            _MaxY = maxY;
        }

        public RectangleD (PointD location, double width, double height)
        {
            _MinX = location.X;
            _MinY = location.Y;
            _MaxX = location.X + width;
            _MaxY = location.Y + height;
        }
        #endregion

        #region 属性
        /// <summary>
        /// 获取最小x坐标
        /// </summary>
        public double MinX
        {
            get { return _MinX; }
        }
        /// <summary>
        /// 获取最大x坐标
        /// </summary>
        public double MaxX
        {
            get { return _MaxX; }
        }
        /// <summary>
        /// 获取最小y坐标
        /// </summary>
        public double MinY
        {
            get { return _MinY; }
        }
        /// <summary>
        /// 获取最大y坐标
        /// </summary>
        public double MaxY
        {
            get { return _MaxY; }
        }
        /// <summary>
        /// 获取宽度
        /// </summary>
        public double Width
        {
            get { return _MaxX - _MinX; }
        }
        /// <summary>
        /// 获取高度
        /// </summary>
        public double Height
        {
            get { return _MaxY - _MinY; }
        }

        ///// <summary>
        ///// 获取矩形中心坐标
        ///// </summary>
        //public PointD Center
        //{
        //    get { return new PointD((_MinX + _MaxX) / 2, (_MinY + _MaxY) / 2); }
        //}
        #endregion

        #region 方法
        public RectangleD Clone()
        {
            return new RectangleD(_MinX, _MaxX, _MinY, _MaxY);
        }

        /// <summary>
        /// 矩形框合并
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static RectangleD operator +(RectangleD a, RectangleD b)
        {
            if (a == null && b == null) return null;
            if (a == null) return b.Clone();
            if (b == null) return a.Clone();
            return (new RectangleD(Math.Min(a.MinX, b.MinX), Math.Max(a.MaxX, b.MaxX)
                , Math.Min(a.MinY, b.MinY), Math.Max(a.MaxY, b.MaxY)));
        }

        public static RectangleD EmptyMBR
        {
            get { return new RectangleD(double.MaxValue, double.MinValue, double.MaxValue, double.MinValue); }
        }
        #endregion
    }
}
