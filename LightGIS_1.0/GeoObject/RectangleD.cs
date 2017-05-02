using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightGIS_1._0.GeoObject
{
    class RectangleD
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
            if ((minX > maxX) || (minY > maxY))
            {
                throw new Exception("Invalid rectangle!");
            }
            _MinX = minX;
            _MaxX = maxX;
            _MinY = minY;
            _MaxY = maxY;
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
        #endregion

        #region 方法
        public RectangleD Clone()
        {
            return new RectangleD(_MinX, _MaxX, _MinY, _MaxY);
        }
        #endregion
    }
}
