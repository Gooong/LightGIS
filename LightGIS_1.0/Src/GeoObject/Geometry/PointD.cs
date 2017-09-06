using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightGIS_1._0.GeoObject
{
    public class PointD :Geometry
    {
        #region 字段
        private double _X, _Y;//坐标
        #endregion

        #region 构造函数
        public PointD()
        {

        }
        public PointD(double x, double y)
        {
            _X = x;
            _Y = y;
        }
        #endregion

        #region 属性
        /// <summary>
        /// 获取或设置X坐标
        /// </summary>
        public double X
        {
            //这里的get和set内容是必须写吗？
            get { return _X; }
            set { _X = value; }
        }
        /// <summary>
        /// 获取或设置Y坐标
        /// </summary>
        public double Y
        {
            get { return _Y; }
            set { _Y = value; }
        }

        public override RectangleD MBR
        {
            get { return new RectangleD(_X, _Y, 0, 0); }
        }

        #endregion

        #region 方法
        public PointD Clone()
        {
            return new PointD(_X, _Y);
        }

        #endregion
    }
}
