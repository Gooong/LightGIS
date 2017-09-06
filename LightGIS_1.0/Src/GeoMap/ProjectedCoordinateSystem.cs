using LightGIS_1._0.GeoObject;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LightGIS_1._0.Src.GeoMap
{
    public class ProjectedCoordinateSystem: ExpandableObjectConverter
    {
        public enum ProjectedSystem
        {
            None = 0,
            WebMercator = 1
        }


        private ProjectedSystem _ProjectedSystem = ProjectedSystem.None;    //默认是无投影
        private String _GeoInfo = "WGS1984";//地理坐标系
        #region 属性
        [Browsable(true), Description("获取或设置投影类型"), DisplayName("投影类型")]
        public ProjectedSystem projectedSystem
        {
            get { return _ProjectedSystem; }
            set
            {
                if (_ProjectedSystem != value)
                {
                    _ProjectedSystem = value;
                    ProjectedSystemChanged?.Invoke(this);
                }
            }
        }

        [Browsable(true), Description("获取地理坐标系"), DisplayName("地理坐标系")]
        public String GeoInfo {
            get { return _GeoInfo; }
        }
        #endregion


        #region 事件
        internal delegate void ProjectedSystemChangedHandle(object sender);
        internal event ProjectedSystemChangedHandle ProjectedSystemChanged;
        #endregion



        #region Web墨卡托投影

        //Web墨卡托转WGS84
        PointD lonLat2WebMercator(PointD lonLat)
        {

            double x = lonLat.X * 20037508.34 / 180;
            double y = Math.Log(Math.Tan((90 + lonLat.Y) * Math.PI / 360)) / (Math.PI / 180);
            y = y * 20037508.34 / 180;
            PointD mercator = new PointD(x, y);
            return mercator;
        }
        PointD[] lonLat2WebMercator(PointD[] lonLats)
        {
            PointD[] points = new PointD[lonLats.Length];
            for (int i = 0; i < lonLats.Length; i++)
            {
                points[i] = lonLat2WebMercator(lonLats[i]);
            }
            return points;
        }
        MultiPolyLine lonLat2WebMercator(MultiPolyLine multiPolyline)
        {
            List<PolyLine> lines = new List<PolyLine>();
            foreach (PolyLine polyline in multiPolyline.PolyLines)
            {
                lines.Add(new PolyLine(lonLat2WebMercator(polyline.Points)));
            }
            return new MultiPolyLine(lines);
        }

        MultiPolygon lonLat2WebMercator(MultiPolygon multiPolygon)
        {
            List<Polygon> polygons = new List<Polygon>();
            foreach (Polygon polygon in multiPolygon.Polygons)
            {
                polygons.Add(new Polygon(lonLat2WebMercator(polygon.Points)));
            }
            return new MultiPolygon(polygons);
        }


        //Web墨卡托转经纬度
        PointD WebMercator2lonLat(PointD mercator)
        {

            double x = mercator.X / 20037508.34 * 180;
            double y = mercator.Y / 20037508.34 * 180;
            y = 180 / Math.PI * (2 * Math.Atan(Math.Exp(y * Math.PI / 180)) - Math.PI / 2);
            PointD lonLat = new PointD(x, y);
            return lonLat;
        }

        PointD[] WebMercator2lonLat(PointD[] mercators)
        {
            PointD[] points = new PointD[mercators.Length];
            for (int i = 0; i < mercators.Length; i++)
            {
                points[i] = WebMercator2lonLat(mercators[i]);
            }
            return points;
        }

        MultiPolyLine WebMercator2lonLat(MultiPolyLine multiPolyline)
        {
            List<PolyLine> lines = new List<PolyLine>();
            foreach (PolyLine polyline in multiPolyline.PolyLines)
            {
                lines.Add(new PolyLine(WebMercator2lonLat(polyline.Points)));
            }
            return new MultiPolyLine(lines);
        }

        MultiPolygon WebMercator2lonLat(MultiPolygon multiPolygon)
        {
            List<Polygon> polygons = new List<Polygon>();
            foreach (Polygon polygon in multiPolygon.Polygons)
            {
                polygons.Add(new Polygon(WebMercator2lonLat(polygon.Points)));
            }
            return new MultiPolygon(polygons);
        }





        #endregion


        #region 投影函数
        internal Geometry ToProjCo(Geometry geo)
        {
            if(geo.GetType() == typeof(PointD))
            {
                return ToProjCo((PointD)geo);
            }
            else if (geo.GetType() == typeof(MultiPolyLine))
            {
                return ToProjCo((MultiPolyLine)geo);
            }
            else
            {
                return ToProjCo((MultiPolygon)geo);
            }
        }

        internal PointD ToProjCo(PointD geoPoint)
        {
            if (_ProjectedSystem == ProjectedSystem.WebMercator)
            {
                return lonLat2WebMercator(geoPoint);
            }
            else
            {
                return geoPoint.Clone();
            }
        }

        internal RectangleD ToProjCo(RectangleD geoMBR)
        {
            if (_ProjectedSystem == ProjectedSystem.WebMercator)
            {
                PointD leftTopP = lonLat2WebMercator(new PointD(geoMBR.MinX, geoMBR.MinY));
                PointD rigntBottomP = lonLat2WebMercator(new PointD(geoMBR.MaxX, geoMBR.MaxY));
                return new RectangleD(leftTopP.X, rigntBottomP.X, leftTopP.Y, rigntBottomP.Y);
            }
            else
            {
                return geoMBR.Clone();
            }
        }


        internal MultiPolyLine ToProjCo(MultiPolyLine geoMulline)
        {
            if (_ProjectedSystem == ProjectedSystem.WebMercator)
            {
                return lonLat2WebMercator(geoMulline);
            }
            else
            {
                return geoMulline.Clone();
            }
        }

        internal MultiPolygon ToProjCo(MultiPolygon gemMulgon)
        {
            if (_ProjectedSystem == ProjectedSystem.WebMercator)
            {
                return lonLat2WebMercator(gemMulgon);
            }
            else
            {
                return gemMulgon.Clone();
            }
        }


        internal PointD ToLngLat(PointD prjPoint)
        {
            if (_ProjectedSystem == ProjectedSystem.WebMercator)
            {
                return WebMercator2lonLat(prjPoint);
            }
            else
            {
                return prjPoint.Clone();
            }
        }


        internal MultiPolyLine ToLngLat(MultiPolyLine prjMulline)
        {
            if (_ProjectedSystem == ProjectedSystem.WebMercator)
            {
                return WebMercator2lonLat(prjMulline);
            }
            else
            {
                return prjMulline.Clone();
            }
        }


        internal MultiPolygon ToLngLat(MultiPolygon prjMulgon)
        {
            if (_ProjectedSystem == ProjectedSystem.WebMercator)
            {
                return WebMercator2lonLat(prjMulgon);
            }
            else
            {
                return prjMulgon.Clone();
            }
        }
        #endregion
    }

}
