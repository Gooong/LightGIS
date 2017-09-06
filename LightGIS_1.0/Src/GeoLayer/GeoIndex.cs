using LightGIS_1._0.GeoObject;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;

namespace LightGIS_1._0.GeoLayer
{
    class GeoIndex
    {
        #region 字段
        Layer mLayer;

        HashSet<DataRow>[][] sets1 = new HashSet<DataRow>[2][];
        HashSet<DataRow>[][] sets2 = new HashSet<DataRow>[4][];
        HashSet<DataRow>[][] sets3 = new HashSet<DataRow>[8][];

        RectangleD mMBR;

        #endregion

        #region 构造函数

        public GeoIndex(Layer layer)
        {
            mLayer = layer;
            mMBR = RectMain(mLayer);
            GeoIndexf(2);
            GeoIndexf(4);
            GeoIndexf(8);
        }

        #endregion

        #region 属性

        //TODO
        public RectangleD MBR
        {
            get;
        }
        #endregion

        #region 方法

        /// <summary>
        /// 生成地理索引
        /// </summary>
        /// <param name="layer"></param>
        public void CreateGeoIndex(Layer layer)
        {
            mLayer = layer;
            mMBR = RectMain(mLayer);
            GeoIndexf(2);
            GeoIndexf(4);
            GeoIndexf(8);
        }

        /// <summary>
        /// 添加索引
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="dataRow"></param>
        public void AddRecord(DataRow dataRow)
        {
            AddRecord123(dataRow, 2);
            AddRecord123(dataRow, 4);
            AddRecord123(dataRow, 8);
        }

        /// <summary>
        /// 删除索引
        /// </summary>
        /// <param name="dataRow"></param>
        public void DeleteRecord(DataRow dataRow)
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    sets1[i][j].Remove(dataRow);
                }
            }
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    sets2[i][j].Remove(dataRow);
                }
            }
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    sets3[i][j].Remove(dataRow);
                }
            }
        }

        /// <summary>
        /// 点选索引
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public HashSet<DataRow> SelectByPoint(PointD point)
        {
            double sWidth = mMBR.Width / 8;
            double sHeight = mMBR.Height / 8;
            double a = point.X / sWidth;
            double b = point.Y / sHeight;
            int i = 0;
            int j = 0;
            i = (int)a;
            j = (int)b;
            Console.WriteLine(i + "," + j);
            if (i < 8 &&i>0&&j>0&& j < 8)
            {
                return sets3[i][j];
            }
            else
            {
                return null;
            }

        }

        /// <summary>
        /// 框选索引
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="Box"></param>
        /// <returns></returns>
        public HashSet<DataRow> SelectByBox(RectangleD Box)
        {
            double sWidth = mMBR.Width;
            double sHeight = mMBR.Height;
            HashSet<DataRow> sDataRow = new HashSet<DataRow>();
            if (Box.Height > (sHeight / 2) || Box.Width > (sWidth / 2))
            {
                return null;
            }
            else if (Box.Height > (sHeight / 4) || Box.Width > (sWidth / 4))
            {
                sWidth = sWidth / 4;
                sHeight = sHeight / 4;
                for (int i = 0; i < 4; i++)
                {
                    double sWidthCount = mMBR.MinX + i * sWidth;
                    for (int j = 0; j < 4; j++)
                    {
                        double sHeightCount = mMBR.MinY + j * sHeight;
                        if (Box.MinX > (sWidthCount + sWidth) || Box.MaxX < sWidthCount || Box.MinY > (sHeightCount + sHeight) || Box.MaxY < sHeightCount) { }
                        else
                        {
                            sDataRow.UnionWith(sets3[i][j]);
                        }
                    }
                }
            }
            else
            {
                sWidth = sWidth / 8;
                sHeight = sHeight / 8;
                for (int i = 0; i < 8; i++)
                {
                    double sWidthCount = mMBR.MinX + i * sWidth;
                    for (int j = 0; j < 8; j++)
                    {
                        double sHeightCount = mMBR.MinY + j * sHeight;
                        if (Box.MinX > (sWidthCount + sWidth) || Box.MaxX < sWidthCount || Box.MinY > (sHeightCount + sHeight) || Box.MaxY < sHeightCount) { }
                        else
                        {
                            sDataRow.UnionWith(sets3[i][j]);
                        }
                    }
                }
            }
            return sDataRow;
        }



        #endregion

        #region 私有函数

        /// <summary>
        /// 获取整个地图的最小外包矩形
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        private RectangleD RectMain(Layer layer)
        {
            RectangleD sRec = new RectangleD();
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
            return sRec;
        }

        /// <summary>
        /// 初始化索引函数（分123级分割方式）
        /// </summary>
        /// <param name="layer">图层编号</param>
        /// <param name="Div">分割数</param>
        private HashSet<DataRow>[][] GeoIndexf(int Div)
        {
            HashSet<DataRow>[][] sSet;
            if (Div == 2)
            {
                sSet = sets1;
            }
            else if (Div == 4)
            {
                sSet = sets2;
            }
            else
            {
                sSet = sets3;
            }

            for(int k = 0; k < Div; k++)
            {
                sSet[k] = new HashSet<DataRow>[Div];
                for(int s = 0; s < Div; s++)
                {
                    sSet[k][s] = new HashSet<DataRow>();
                }
            }


            double sWidth = mMBR.Width / Div;
            double sHeight = mMBR.Height / Div;
            for (int i=0; i<Div; i++)
            {
                double sWidthCount = mMBR.MinX + i * sWidth;
                for (int j=0; j<Div; j++)
                {
                    double sHeightCount = mMBR.MinY + j * sHeight;
                    if (mLayer.GeoType == typeof(MultiPolygon))
                    {
                        for (int k = 0; k < mLayer.GeoCount; ++k)
                        {
                            MultiPolygon mu = mLayer.Records.Rows[k].Field<MultiPolygon>(2);
                            if (mu != null)
                            {
                                if (mu.MBR.MinX > (sWidthCount + sWidth) || mu.MBR.MaxX < sWidthCount || mu.MBR.MinY > (sHeightCount + sHeight) || mu.MBR.MaxY < sHeightCount) { }
                                else
                                {
                                    sSet[i][j].Add(mLayer.Records.Rows[k]);
                                }
                            }

                        }
                    }
                    if (mLayer.GeoType == typeof(MultiPolyLine))
                    {
                        for (int k = 0; k < mLayer.GeoCount; ++k)
                        {
                            MultiPolyLine mu = mLayer.Records.Rows[k].Field<MultiPolyLine>(2);
                            if (mu.MBR.MinX > (sWidthCount + sWidth) || mu.MBR.MaxX < sWidthCount || mu.MBR.MinY > (sHeightCount + sHeight) || mu.MBR.MaxY < sHeightCount) { }
                            else
                            {
                                sSet[i][j].Add(mLayer.Records.Rows[k]);
                            }
                        }
                    }
                    if (mLayer.GeoType == typeof(PointD))
                    {
                        for (int k = 0; k < mLayer.GeoCount; ++k)
                        {
                            PointD mu = mLayer.Records.Rows[k].Field<PointD>(2);
                            if (mu.X > (sWidthCount + sWidth) || mu.X < sWidthCount || mu.Y > (sHeightCount + sHeight) || mu.Y < sHeightCount) { }
                            else
                            {
                                sSet[i][j].Add(mLayer.Records.Rows[k]);
                            }
                        }
                    }
                }
            }
            return sSet;
        }

        /// <summary>
        /// 辅助添加操作函数（分123级添加）
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="dataRow"></param>
        /// <param name="Div"></param>
        private void AddRecord123(DataRow dataRow, int Div)
        {

            double sWidth = mMBR.Width / Div;
            double sHeight = mMBR.Height / Div;
            for (int i = 0; i < Div; i++)
            {
                double sWidthCount = mMBR.MinX + i * sWidth;
                for (int j = 0; j < Div; j++)
                {
                    double sHeightCount = mMBR.MinY + j * sHeight;
                    if (mLayer.GeoType == typeof(MultiPolygon))
                    {
                        MultiPolygon mu = dataRow.Field<MultiPolygon>(2);
                        if (mu.MBR.MinX > (sWidthCount + sWidth) || mu.MBR.MaxX < sWidthCount || mu.MBR.MinY > (sHeightCount + sHeight) || mu.MBR.MaxY < sHeightCount) { }
                        else
                        {
                            if (Div == 2)
                                sets1[i][j].Add(dataRow);
                            if (Div == 4)
                                sets2[i][j].Add(dataRow);
                            if (Div == 8)
                                sets3[i][j].Add(dataRow);
                        }
                    }
                    if (mLayer.GeoType == typeof(MultiPolyLine))
                    {
                        {
                            MultiPolyLine mu = dataRow.Field<MultiPolyLine>(2);
                            if (mu.MBR.MinX > (sWidthCount + sWidth) || mu.MBR.MaxX < sWidthCount || mu.MBR.MinY > (sHeightCount + sHeight) || mu.MBR.MaxY < sHeightCount) { }
                            else
                            {
                                if (Div == 2)
                                    sets1[i][j].Add(dataRow);
                                if (Div == 4)
                                    sets2[i][j].Add(dataRow);
                                if (Div == 8)
                                    sets3[i][j].Add(dataRow);
                            }
                        }
                    }
                    if (mLayer.GeoType == typeof(PointD))
                    {
                        {
                            PointD mu = dataRow.Field<PointD>(2);
                            if (mu.X > (sWidthCount + sWidth) || mu.X < sWidthCount || mu.Y > (sHeightCount + sHeight) || mu.Y < sHeightCount) { }
                            else
                            {
                                if (Div == 2)
                                    sets1[i][j].Add(dataRow);
                                if (Div == 4)
                                    sets2[i][j].Add(dataRow);
                                if (Div == 8)
                                    sets3[i][j].Add(dataRow);
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }
}
