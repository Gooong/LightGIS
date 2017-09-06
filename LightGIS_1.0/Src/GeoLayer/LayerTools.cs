using LightGIS_1._0.GeoLayer;
using LightGIS_1._0.GeoObject;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace LightGIS_1._0.GeoLayer
{
    internal static class LayerTools
    {

        #region 计算函数

        public static RectangleD GetLayerGeoMBR(Layer layer)
        {
            RectangleD sRec = RectangleD.EmptyMBR;
            if (layer.GeoType == typeof(MultiPolygon))
            {
                for (int i = 0; i < layer.GeoCount; ++i)
                {
                    MultiPolygon mu = layer.Records.Rows[i].Field<MultiPolygon>(2);
                    if (mu != null)
                    {
                        sRec += mu.MBR;
                    }

                }
            }
            if (layer.GeoType == typeof(MultiPolyLine))
            {
                for (int i = 0; i < layer.GeoCount; ++i)
                {
                    MultiPolyLine mu = layer.Records.Rows[i].Field<MultiPolyLine>(2);
                    if (mu != null)
                    {
                        sRec += mu.MBR;
                    }
                }
            }
            if (layer.GeoType == typeof(PointD))
            {
                for (int i = 0; i < layer.GeoCount; ++i)
                {
                    PointD mu = layer.Records.Rows[i].Field<PointD>(2);
                    sRec = new RectangleD(Math.Min(sRec.MinX, mu.X), Math.Max(sRec.MaxX, mu.X)
                , Math.Min(sRec.MinY, mu.Y), Math.Max(sRec.MaxY, mu.Y));
                }
            }
            if(sRec.Width<0 || sRec.Height < 0)
            {
                sRec = new RectangleD();
            }
            return sRec;
        }

        //计算标注位置
        public static PointD GetCenterPoint(Geometry geo)
        {
            if (geo.GetType() == typeof(PointD))
            {
                return ((PointD)geo).Clone();
            }
            else if (geo.GetType() == typeof(MultiPolyLine))
            {
                MultiPolyLine mMultiPolyLine = (MultiPolyLine)geo;
                if (mMultiPolyLine.Count == 0)
                {
                    return null;
                }
                int MaxPolyLineIndex = 0;
                if (mMultiPolyLine.Count != 1)
                {
                    for (int i = 1; i < mMultiPolyLine.Count; i++)
                    {
                        if (mMultiPolyLine.PolyLines[i].Count > mMultiPolyLine.PolyLines[MaxPolyLineIndex].Count)
                            MaxPolyLineIndex = i;
                    }
                }
                int n = mMultiPolyLine.PolyLines[MaxPolyLineIndex].Count;
                if (n == 0)
                    return null;
                return mMultiPolyLine.PolyLines[MaxPolyLineIndex].Points[n / 2].Clone();
            }
            else
            {
                MultiPolygon mMultiPolygon = (MultiPolygon)geo;
                if (mMultiPolygon.Count == 0)
                {
                    return null;
                }
                int MaxPolygonIndex = 0;
                if (mMultiPolygon.Count != 1)
                {
                    for (int i = 1; i < mMultiPolygon.Count; i++)
                    {
                        if (mMultiPolygon.Polygons[i].Count > mMultiPolygon.Polygons[MaxPolygonIndex].Count)
                            MaxPolygonIndex = i;
                    }
                }
                Polygon mPolygon = mMultiPolygon.Polygons[MaxPolygonIndex];
                if (mPolygon.Count == 0)
                    return null;
                RectangleD mMBR = mPolygon.MBR;
                PointD Center = new PointD();
                PointD p1 = new PointD(mMBR.MinX, (mMBR.MaxY + mMBR.MinY) / 2);
                PointD p2 = new PointD(mMBR.MaxX, (mMBR.MaxY + mMBR.MinY) / 2);
                List<double> InsectX = new List<double>();
                for (int i = 0; i < mPolygon.Count - 1; i++)
                {
                    PointD sP1 = mPolygon.GetPoint(i);
                    PointD sP2 = mPolygon.GetPoint(i + 1);
                    if (IsRayInterLine(p1, sP1, sP2))
                    {
                        InsectX.Add(mPolygon.Points[i].X);
                    }
                }
                int MaxLengthIndex = 0;
                double MaxLength = 0;
                InsectX.Sort();
                if (InsectX.Count < 2)
                {
                    return mPolygon.Points[0];
                }
                else
                {
                    MaxLength = InsectX[1] - InsectX[0];
                    for (int i = 1; i < InsectX.Count / 2; i++)
                    {
                        if (InsectX[2 * i + 1] - InsectX[2 * i] > MaxLength)
                        {
                            MaxLength = InsectX[2 * i + 1] - InsectX[2 * i];
                            MaxLengthIndex = i;
                        }
                    }
                }

                Center.X = (InsectX[2 * MaxLengthIndex] + InsectX[2 * MaxLengthIndex + 1]) / 2;
                Center.Y = (mMBR.MaxY + mMBR.MinY) / 2;
                return Center;
            }
        }

        #endregion


        #region 点选使用函数
        /// <summary>
        /// 两点是否重合(点选是否选中点）
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="tolerance">阈值，地理距离</param>
        /// <returns></returns>
        static public bool IsPointOnPoint(PointD a, PointD b, double tolerance)
        {
            //这里的点坐标是屏幕点坐标
            double sDistance = DistanceBetweenPoints(a, b);
            if (sDistance <= tolerance)
                return true;
            return false;
        }

        /// <summary>
        /// 判断点是否在Polyline上（点选是否选中线）
        /// </summary>
        /// <param name="point"></param>
        /// <param name="polyline"></param>
        /// <param name="tolerance">阈值，地理距离</param>
        /// <returns></returns>
        static public bool IsPointOnPolyline(PointD point, PolyLine polyline, double tolerance)
        {
            //先将点与外包矩形对比
            if (IsPointInRect(point, polyline.MBR) == false)
                return false;
            //对每条线段求最近距离，并进行比较
            int sCount = polyline.Count;
            for (int i = 0; i < sCount - 1; i++)
            {
                double sDis = DistanceOfPointToSegment(polyline.GetPoint(i), polyline.GetPoint(i + 1), point);
                if (sDis <= tolerance)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 判断点是否在MultiPolyline上（点选是否选中线）
        /// </summary>
        /// <param name="point"></param>
        /// <param name="multiPolyline"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        static public bool IsPointOnMulPolyline(PointD point, MultiPolyLine multiPolyline, double tolerance)
        {
            //先将点与外包矩形对比
            if (IsPointInRect(point, multiPolyline.MBR) == false)
                return false;

            foreach (PolyLine sLine in multiPolyline.PolyLines)
            {
                if (IsPointOnPolyline(point, sLine, tolerance))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 一个点是否在简单多边形内部(点选是否选中简单多边形）
        /// </summary>
        /// <param name="point"></param>
        /// <param name="polygon"></param>
        /// <returns></returns>
        static public bool IsPointInPolygon(PointD point, Polygon polygon)
        {
            if (!IsPointInRect(point, polygon.MBR)) { return false; }
            int sCount = polygon.Count;
            int sInterLineNum = 0;
            PointD sP1, sP2;
            //逐个判断每条边是否与点引出的射线相交
            for (int i = 0; i < sCount - 1; i++)
            {
                sP1 = polygon.GetPoint(i);
                sP2 = polygon.GetPoint(i + 1);
                if (IsRayInterLine(point, sP1, sP2))
                {
                    sInterLineNum++;
                }
            }
            //判断最后一个顶点与起始点组成的边是否与射线相交
            if (IsRayInterLine(point, polygon.GetPoint(0), polygon.GetPoint(sCount - 1)))
                sInterLineNum++;

            return (sInterLineNum % 2 == 1);
        }

        /// <summary>
        /// 一个点是否在复杂多边形内部（点选是否选中了复杂多边形）
        /// </summary>
        /// <param name="point"></param>
        /// <param name="multipolygon"></param>
        /// <returns></returns>
        static public bool IsPointInMultiPolygon(PointD point, MultiPolygon multipolygon)
        {
            if (!IsPointInRect(point, multipolygon.MBR)) { return false; }
            int spolygonCount = multipolygon.Count;
            int sInterLineNum = 0;
            //循环对每个polygon求交点并计算交点总数
            for (int i = 0; i < spolygonCount; i++)
            {
                Polygon mpolygon = multipolygon.GetPolygon(i);
                int sCount = mpolygon.Count;
                PointD sP1 = new PointD();
                PointD sP2 = new PointD();
                for (int j = 0; j < sCount - 1; j++)
                {
                    sP1 = mpolygon.GetPoint(j);
                    sP2 = mpolygon.GetPoint(j + 1);
                    if (IsRayInterLine(point, sP1, sP2))
                    {
                        sInterLineNum++;
                    }
                }
                if (IsRayInterLine(point, mpolygon.GetPoint(0), mpolygon.GetPoint(sCount - 1)))
                    sInterLineNum++;
            }

            return (sInterLineNum % 2 == 1);
        }


        public static PointD GetClosePoint(PointD point, Geometry geometry, double tolerance)
        {
            if (geometry.GetType() == typeof(PointD))
            {
                if (IsPointOnPoint(point, (PointD)geometry, tolerance))
                {
                    return (PointD)geometry;
                }
                else { return null; }
            }
            else if (geometry.GetType() == typeof(MultiPolygon))
            {
                return GetClosePoint(point, (MultiPolygon)geometry, tolerance);
            }
            else if (geometry.GetType() == typeof(MultiPolyLine))
            {
                return GetClosePoint(point, (MultiPolyLine)geometry, tolerance);
            }
            else
            {
                return null;
            }
        }
        static private PointD GetClosePoint(PointD point, MultiPolyLine multiPolyline, double tolerance)
        {
            if (multiPolyline == null) { return null; }
            RectangleD smbr = multiPolyline.MBR;
            RectangleD sRect = new RectangleD(smbr.MinX - tolerance, smbr.MaxX + tolerance, smbr.MinY - tolerance, smbr.MaxY + tolerance);
            if (!IsPointInRect(point, sRect)) { return null; }


            foreach (PolyLine sPolyline in multiPolyline.PolyLines)
            {
                foreach (PointD sPoint in sPolyline.Points)
                {
                    if (IsPointOnPoint(point, sPoint, tolerance)) { return sPoint; }
                }
            }
            return null;
        }
        static private PointD GetClosePoint(PointD point, MultiPolygon multiPolygon, double tolerance)
        {
            if (multiPolygon == null) { return null; }
            RectangleD smbr = multiPolygon.MBR;
            RectangleD sRect = new RectangleD(smbr.MinX - tolerance, smbr.MaxX + tolerance, smbr.MinY - tolerance, smbr.MaxY + tolerance);
            if (!IsPointInRect(point, sRect)) { return null; }


            foreach (Polygon sPolygon in multiPolygon.Polygons)
            {
                for (int i = 0; i < sPolygon.Points.Length; i++)
                {
                    if (IsPointOnPoint(point, sPolygon.Points[i], tolerance)) { return sPolygon.Points[i]; }
                }
            }
            return null;
        }



        #endregion

        #region 框选使用函数

        /// <summary>
        /// 判断点是否在矩形框内(框选是否选中点）
        /// </summary>
        /// <param name="point"></param>
        /// <param name="rect"></param>
        /// <returns></returns>
        static public bool IsPointInRect(PointD point, RectangleD rect)
        {
            if (point.X > rect.MaxX || point.X < rect.MinX)
                return false;
            if (point.Y > rect.MaxY || point.Y < rect.MinY)
                return false;
            return true;
        }

        /// <summary>
        /// 判断线是否位于矩形盒内（框选是否选中线）
        /// </summary>
        /// <param name="line"></param>
        /// <param name="rect"></param>
        /// <returns></returns>
        static public bool IsRectInterPolyline(PolyLine line, RectangleD rect)
        {
            //先对外包矩形判断
            if (IsMBRInterRect(line.MBR, rect) == false)
                return false;
            //其他
            int sCount = line.Count;
            for (int i = 1; i < sCount; i++)
            {
                if (IsLineInterRect(line.GetPoint(i - 1), line.GetPoint(i), rect))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 判断线是否位于矩形盒内（框选是否选中线）
        /// </summary>
        /// <param name="multiPolyline"></param>
        /// <param name="rect"></param>
        /// <returns></returns>
        static public bool IsRectInterMultiPolyline(MultiPolyLine multiPolyline, RectangleD rect)
        {
            //先对外包矩形判断
            if (IsMBRInterRect(multiPolyline.MBR, rect) == false)
                return false;

            foreach (PolyLine line in multiPolyline.PolyLines)
            {
                if (IsRectInterPolyline(line, rect))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断简单多边形是否位于矩形盒内(框选是否选中简单多边形）
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="rect"></param>
        /// <returns></returns>
        static public bool IsRectInterPolygon(Polygon polygon, RectangleD rect)
        {
            //判断外包矩形与矩形框是否相交
            if (IsMBRInterRect(polygon.MBR, rect) == false)
                return false;
            //先判断是否有点在矩形框内
            int sCount = polygon.Count;
            for (int i = 0; i < sCount; i++)
            {
                if (IsPointInRect(polygon.GetPoint(i), rect))
                    return true;
            }
            //判断矩形框点是否在多边形内
            if (IsPointInPolygon(new PointD(rect.MaxX, rect.MaxY), polygon))
                return true;
            if (IsPointInPolygon(new PointD(rect.MaxX, rect.MinY), polygon))
                return true;
            if (IsPointInPolygon(new PointD(rect.MinX, rect.MaxY), polygon))
                return true;
            if (IsPointInPolygon(new PointD(rect.MinX, rect.MinY), polygon))
                return true;
            //逐一判断每条边是否与矩形盒相交
            for (int i = 0; i < sCount - 1; i++)
            {
                if (IsLineInterLine(polygon.GetPoint(i), polygon.GetPoint(i + 1), new PointD(rect.MaxX, rect.MaxY), new PointD(rect.MaxX, rect.MinY)))
                    return true;
                if (IsLineInterLine(polygon.GetPoint(i), polygon.GetPoint(i + 1), new PointD(rect.MaxX, rect.MaxY), new PointD(rect.MinX, rect.MaxY)))
                    return true;
                if (IsLineInterLine(polygon.GetPoint(i), polygon.GetPoint(i + 1), new PointD(rect.MinX, rect.MaxY), new PointD(rect.MinX, rect.MinY)))
                    return true;
                if (IsLineInterLine(polygon.GetPoint(i), polygon.GetPoint(i + 1), new PointD(rect.MinX, rect.MinY), new PointD(rect.MaxX, rect.MinY)))
                    return true;
            }
            //判断最后一个顶点与起始点组成的边是否与矩形盒相交
            if (IsLineInterLine(polygon.GetPoint(0), polygon.GetPoint(sCount - 1), new PointD(rect.MaxX, rect.MaxY), new PointD(rect.MaxX, rect.MinY)))
                return true;
            if (IsLineInterLine(polygon.GetPoint(0), polygon.GetPoint(sCount - 1), new PointD(rect.MaxX, rect.MaxY), new PointD(rect.MinX, rect.MaxY)))
                return true;
            if (IsLineInterLine(polygon.GetPoint(0), polygon.GetPoint(sCount - 1), new PointD(rect.MinX, rect.MaxY), new PointD(rect.MinX, rect.MinY)))
                return true;
            if (IsLineInterLine(polygon.GetPoint(0), polygon.GetPoint(sCount - 1), new PointD(rect.MinX, rect.MinY), new PointD(rect.MaxX, rect.MinY)))
                return true;
            //其他
            return false;
        }

        /// <summary>
        /// 判断复杂多边形是否位于矩形盒内(框选是否选中复杂多边形）
        /// </summary>
        /// <param name="multipolygon"></param>
        /// <param name="rect"></param>
        /// <returns></returns>
        static public bool IsRectInterMultipolygon(MultiPolygon multipolygon, RectangleD rect)
        {
            //判断外包矩形与矩形框是否相交
            if (IsMBRInterRect(multipolygon.MBR, rect) == false)
                return false;
            //先判断是否有点在矩形框内
            int spolygonCount = multipolygon.Count;
            for (int i = 0; i < spolygonCount; i++)
            {
                Polygon spolygon = multipolygon.GetPolygon(i);
                int sCount = spolygon.Count;
                for (int j = 0; j < sCount; j++)
                {
                    if (IsPointInRect(spolygon.GetPoint(j), rect))
                        return true;
                }
            }
            //判断矩形框点是否在复杂多边形内
            if (IsPointInMultiPolygon(new PointD(rect.MaxX, rect.MaxY), multipolygon))
                return true;
            if (IsPointInMultiPolygon(new PointD(rect.MaxX, rect.MinY), multipolygon))
                return true;
            if (IsPointInMultiPolygon(new PointD(rect.MinX, rect.MaxY), multipolygon))
                return true;
            if (IsPointInMultiPolygon(new PointD(rect.MinX, rect.MinY), multipolygon))
                return true;
            //接着判断复杂多边形的边是否与矩形框相交
            //循环对每个polygon的每条边做求交判断
            for (int i = 0; i < spolygonCount; i++)
            {
                Polygon mpolygon = multipolygon.GetPolygon(i);
                int sCount = mpolygon.Count;
                for (int j = 0; j < sCount - 1; j++)
                {
                    if (IsLineInterLine(mpolygon.GetPoint(j), mpolygon.GetPoint(j + 1), new PointD(rect.MaxX, rect.MaxY), new PointD(rect.MaxX, rect.MinY)))
                        return true;
                    if (IsLineInterLine(mpolygon.GetPoint(j), mpolygon.GetPoint(j + 1), new PointD(rect.MaxX, rect.MaxY), new PointD(rect.MinX, rect.MaxY)))
                        return true;
                    if (IsLineInterLine(mpolygon.GetPoint(j), mpolygon.GetPoint(j + 1), new PointD(rect.MinX, rect.MaxY), new PointD(rect.MinX, rect.MinY)))
                        return true;
                    if (IsLineInterLine(mpolygon.GetPoint(j), mpolygon.GetPoint(j + 1), new PointD(rect.MinX, rect.MinY), new PointD(rect.MaxX, rect.MinY)))
                        return true;
                }
                //判断最后一个顶点与起始点组成的边是否与矩形盒相交
                if (IsLineInterLine(mpolygon.GetPoint(0), mpolygon.GetPoint(sCount - 1), new PointD(rect.MaxX, rect.MaxY), new PointD(rect.MaxX, rect.MinY)))
                    return true;
                if (IsLineInterLine(mpolygon.GetPoint(0), mpolygon.GetPoint(sCount - 1), new PointD(rect.MaxX, rect.MaxY), new PointD(rect.MinX, rect.MaxY)))
                    return true;
                if (IsLineInterLine(mpolygon.GetPoint(0), mpolygon.GetPoint(sCount - 1), new PointD(rect.MinX, rect.MaxY), new PointD(rect.MinX, rect.MinY)))
                    return true;
                if (IsLineInterLine(mpolygon.GetPoint(0), mpolygon.GetPoint(sCount - 1), new PointD(rect.MinX, rect.MinY), new PointD(rect.MaxX, rect.MinY)))
                    return true;
            }
            //其他
            return false;
        }

        #endregion

        #region 私有函数

        /// <summary>
        /// 判断点是否在线段上
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="point"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        static private bool IsPointOnSegment(PointD a, PointD b, PointD point, double tolerance)
        {
            //定义Tolerance平方
            double sPowTolerance = tolerance * tolerance;
            //定义point到a点距离平方
            double sPowDisFroma = (point.X - a.X) * (point.X - a.X) + (point.Y - a.Y) * (point.Y - a.Y);
            if (sPowDisFroma <= sPowTolerance)
                return true;
            double sPowDisFromb = (point.X - b.X) * (point.X - b.X) + (point.Y - b.Y) * (point.Y - b.Y);
            //定义point到b点距离平方
            if (sPowDisFromb <= sPowTolerance)
                return true;
            if (PedelInSegment(a, b, point))
            {
                //定义点积
                double sDotProduct = (point.X - a.X) * (b.X - a.X) + (point.Y - a.Y) * (b.Y - a.Y);
                if ((sPowDisFroma - sDotProduct) <= sPowTolerance)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 判断点到线段的垂足是否在线段上
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        static private bool PedelInSegment(PointD a, PointD b, PointD point)
        {
            //定义点积
            double saDotProduct = (point.X - a.X) * (b.X - a.X) + (point.Y - a.Y) * (b.Y - a.Y);
            double sbDotProduct = (point.X - b.X) * (a.X - b.X) + (point.Y - b.Y) * (a.Y - b.Y);
            //定义ab长度平方
            double sPowDistanceOfab = (b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y);
            if (saDotProduct <= sPowDistanceOfab && sbDotProduct <= sPowDistanceOfab)
                return true;
            return false;
        }

        /// <summary>
        /// 从某个点到一条线段的距离
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        static private double DistanceOfPointToSegment(PointD a, PointD b, PointD point)
        {
            double ax = a.X, ay = a.Y, bx = b.X, by = b.Y;
            double px = point.X, py = point.Y;
            if (ax == bx && ay == by)
            {
                return Math.Sqrt((px - ax) * (px - ax) + (py - ay) * (py - ay));
            }
            //不重合
            double spx = bx - ax, spy = by - ay;
            double som = spx * spx + spy * spy;
            double u = ((px - ax) * px + (py - ay) * py) / som;
            if (u < 0)
                u = 0;
            if (u > 1)
                u = 1;
            double X = ax + u * spx, Y = ay + u * spy;
            return DistanceBetweenPoints(point, new PointD(X, Y));
        }

        static private double DistanceBetweenPoints(PointD p1, PointD p2)
        {
            return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
        }


        /// <summary>
        /// 判断多边形的外包矩形与矩形框是否相交
        /// </summary>
        /// <param name="mbr"></param>
        /// <param name="rect"></param>
        /// <returns></returns>
        static private bool IsMBRInterRect(RectangleD mbr, RectangleD rect)
        {
            if (mbr.MinX > rect.MaxX || mbr.MaxX < rect.MinX)
                return false;
            if (mbr.MinY > rect.MaxY || mbr.MaxY < rect.MinY)
                return false;
            return true;
        }

        /// <summary>
        /// 判段线段ab与线段cd是否相交
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        static private bool IsLineInterLine(PointD a, PointD b, PointD c, PointD d)
        {
            //定义点积
            double saDotProduct = (a.X - c.X) * (d.X - c.X) + (a.Y - c.Y) * (d.Y - c.Y);
            if (saDotProduct == 0)//点积为零表示在线段上
                return true;
            double sbDotProduct = (b.X - c.X) * (d.X - c.X) + (b.Y - c.Y) * (d.Y - c.Y);
            if (sbDotProduct == 0)//点积为零表示在线段上
                return true;
            double scDotProduct = (c.X - a.X) * (b.X - a.X) + (c.Y - a.Y) * (b.Y - a.Y);
            if (scDotProduct == 0)//点积为零表示在线段上
                return true;
            double sdDotProduct = (d.X - a.X) * (b.X - a.X) + (d.Y - a.Y) * (b.Y - a.Y);
            if (sdDotProduct == 0)
                return true;
            //点积为负表示在线段两侧,分半在两侧那么肯定相交
            if (scDotProduct * sdDotProduct < 0 && saDotProduct * sbDotProduct < 0)
                return true;
            return false;
        }

        /// <summary>
        /// 从一点出发的射线和一条两点确认的线段是否相交
        /// </summary>
        /// <param name="Ray"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        static private bool IsRayInterLine(PointD Ray, PointD a, PointD b)
        {
            //注意需要判断上下交点，本程序规定，过下交点为交，而上不交
            double rayX = Ray.X, rayY = Ray.Y;
            double aX = a.X, aY = a.Y, bX = b.X, bY = b.Y;
            //射线与线段平行or线段长度为0
            if (aY == bY)
                return false;
            //射线完全在线段上方或下方
            if (rayY > aY && rayY > bY)
                return false;
            if (rayY < aY && rayY < bY)
                return false;
            //当位于射线y值在线段中间时
            //简单情况，射线明显在a，b右侧时
            if (rayX > aX && rayX > bX)
                return false;
            //复杂情况，如果与端点相交
            if (rayY == aY)
            {
                if (aY > bY)
                    return false;
                return true;
            }
            if (rayY == bY)
            {
                if (bY > aY)
                    return false;
                return true;
            }
            //不与端点相交，用面积法判断在左右哪一侧
            double sArea = (aX - rayX) * (bY - rayY) - (aY - rayY) * (bX - rayX);
            if (sArea <= 0 && aY > bY)
                return true;
            if (sArea >= 0 && aY < bY)
                return true;
            return false;
        }

        /// <summary>
        /// 判断线段与矩形盒是否相交
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="rect"></param>
        /// <returns></returns>
        static private bool IsLineInterRect(PointD a, PointD b, RectangleD rect)
        {
            //先判断是否都在矩形外侧
            if (a.X < rect.MinX && b.X < rect.MinX)
                return false;
            if (a.X > rect.MaxX && b.X > rect.MaxX)
                return false;
            if (a.Y > rect.MaxY && b.Y > rect.MaxY)
                return false;
            if (a.Y < rect.MinY && b.Y < rect.MinY)
                return false;
            //先判断两个端点是否在在矩形框内
            if (IsPointInRect(a, rect))
                return true;
            if (IsPointInRect(b, rect))
                return true;
            //然后判断是否有线段上部分点在矩形框内的情况
            if (IsLineInterLine(a, b, new PointD(rect.MaxX, rect.MaxY), new PointD(rect.MaxX, rect.MinY)))
                return true;
            if (IsLineInterLine(a, b, new PointD(rect.MaxX, rect.MaxY), new PointD(rect.MinX, rect.MaxY)))
                return true;
            if (IsLineInterLine(a, b, new PointD(rect.MinX, rect.MaxY), new PointD(rect.MinX, rect.MinY)))
                return true;
            if (IsLineInterLine(a, b, new PointD(rect.MinX, rect.MinY), new PointD(rect.MaxX, rect.MinY)))
                return true;
            return false;
        }

        #endregion
    }
}
