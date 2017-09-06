using LightGIS_1._0.GeoLayer;
using LightGIS_1._0.GeoMap;
using LightGIS_1._0.GeoObject;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
namespace LightGIS_1._0.Src.DataIO
{
    public static class DataIO
    {
        #region 方法
        //读shapefile
        public static Layer ReadShp(string fileName)
        {
            Layer layer;
            int sIndex = fileName.LastIndexOf("\\");
            string sName = fileName.Substring(sIndex + 1, fileName.Length - sIndex - 1 - 4);//文件名作为图层默认名称
            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);


            //读文件头
            Int32 FileCode = OnChangeByteOrder(br.ReadInt32());//读取文件编码
            br.ReadBytes(20);//跳过5个无用编码
            Int32 FileLength = OnChangeByteOrder(br.ReadInt32());//读取文件长度
            Int32 Version = br.ReadInt32();//读取版本号
            Int32 GeoType = br.ReadInt32();//读取文件所记录的空间数据几何类型
            Double Xmin = br.ReadDouble();//读取空间数据所占空间范围最小X值
            Double Ymin = br.ReadDouble();//读取空间数据所占空间范围最小Y值
            Double Xmax = br.ReadDouble();//读取空间数据所占空间范围最大X值
            Double Ymax = br.ReadDouble();//读取空间数据所占空间范围最大Y值
            br.ReadBytes(32);//跳过最大/最小Z值和M值，因为本程序只支持点、线、面数据类型

            //根据空间数据的类型进行空间数据的读取，记录在layer中
            switch (GeoType)
            {
                case 1://空间数据几何类型为点
                    layer = Layer.CreateLayer(sName, typeof(PointD), fileName);
                    ReadShpPoint(br, ref layer);
                    break;
                case 3://空间数据几何类型为线
                    layer = Layer.CreateLayer(sName, typeof(MultiPolyLine), fileName);
                    ReadShpPolyline(br, ref layer);
                    break;
                case 5://空间数据几何类型为多边形
                    layer = Layer.CreateLayer(sName, typeof(MultiPolygon), fileName);
                    ReadShpPolygon(br, ref layer);
                    break;
                default:
                    throw new FileLoadException("不支持的数据类型");
            }
            layer.MBR = new RectangleD(Xmin, Xmax, Ymin, Ymax);
            br.Dispose();
            fs.Dispose();
            ReadDbf(ref layer, fileName);
            return layer;
        }
        //将图层信息保存到shp文件中
        public static void SaveShp(Layer layer, string fileName)
        {
            FileStream fs = new FileStream(fileName, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);
            //写文件头
            bw.Write(OnChangeByteOrder(9994));
            for (int i = 0; i < 5; i++)
            {
                bw.Write(0);
            }
            Type GeoType = layer.GeoType;//获取图层记录的要素类型
            Int32 TypeCode;//要素类型对应的编码
            Int32 FileLength = 50;//文件长度，初值为头文件长度50
            Int32 RecondNum = layer.Records.Rows.Count;//要素个数
            //计算要素类型对应的编码和文件长度
            if (GeoType == typeof(PointD))
            {
                TypeCode = 1;
                for (int i = 0; i < RecondNum; i++)
                {
                    if (Convert.IsDBNull(layer.Records.Rows[i][1]))
                        FileLength += 6;
                    else
                        FileLength += 14;
                }
            }
            else if (GeoType == typeof(MultiPolyLine))
            {
                TypeCode = 3;
                for (int i = 0; i < RecondNum; i++)
                {
                    if (Convert.IsDBNull(layer.Records.Rows[i][1]))
                        FileLength += 6;
                    else
                    {
                        MultiPolyLine mMultiPolyLine = (MultiPolyLine)layer.Records.Rows[i][1];
                        FileLength += (4 + GetCodeLength(mMultiPolyLine));
                    }
                }
            }
            else if (GeoType == typeof(MultiPolygon))
            {
                TypeCode = 5;
                for (int i = 0; i < RecondNum; i++)
                {
                    if (Convert.IsDBNull(layer.Records.Rows[i][1]))
                        FileLength += 6;
                    else
                    {
                        MultiPolygon mMultiPolygon = (MultiPolygon)layer.Records.Rows[i][1];
                        FileLength += (4 + GetCodeLength(mMultiPolygon));
                    }

                }
            }
            else
            {
                throw new Exception("不支持的数据类型");
            }
            bw.Write(OnChangeByteOrder(FileLength));//写入文件长度
            bw.Write(1000);//写入版本号
            bw.Write(TypeCode);//写入几何类型
            //写入MBR数据
            bw.Write(layer.MBR.MinX);
            bw.Write(layer.MBR.MinY);
            bw.Write(layer.MBR.MaxX);
            bw.Write(layer.MBR.MaxY);
            //最大、最小Z值、M值为0
            for (int i = 0; i < 4; i++)
            {
                bw.Write((double)0);
            }

            //写空间数据
            switch (TypeCode)
            {
                case 1://空间数据几何类型为点                   
                    SavePoint(bw, ref layer);
                    break;
                case 3://空间数据几何类型为线
                    SavePolyline(bw, ref layer);
                    break;
                case 5://空间数据几何类型为多边形
                    SavePolygon(bw, ref layer);
                    break;
                default:
                    throw new FileLoadException("不支持的数据类型");
            }
            bw.Dispose();
            fs.Dispose();
            SaveDbf(ref layer, fileName);
            SaveShx(ref layer, fileName);
        }

        public static Map ReadMap(string fileName)
        {
            Map sMap;
            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            StreamReader br = new StreamReader(fs);

            string sName = br.ReadLine();   //第一行，地图名称
            string sDescription = br.ReadLine();    //第二行，地图描述
            int sLayerCount = Int32.Parse(br.ReadLine());   //第三行，地图图层数
            sMap = new Map(sName, sDescription);
            sMap.FilePath = fileName;
            string sLayerPath;
            Layer sLayer;
            for(int i = 0; i < sLayerCount; i++)
            {
                try
                {
                    sLayerPath = br.ReadLine();
                    sLayer = ReadShp(sLayerPath);
                    sMap.AddLayer(sLayer);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }

            }

            br.Dispose();
            fs.Dispose();

            return sMap;
        }

        public static void SaveMap(Map map,string fileName)
        {
            FileStream fs = new FileStream(fileName, FileMode.Create);
            StreamWriter bw = new StreamWriter(fs);

            string sName = Regex.Replace(map.Name, @"[/n/r]", "");
            string sDescription = Regex.Replace(map.Description, @"[/n/r]", "");
            bw.WriteLine(sName);
            bw.WriteLine(sDescription);
            bw.WriteLine(map.Count);

            for(int i = 0;i< map.Count; i++)
            {
                bw.WriteLine(map.Layers[i].FilePath);
            }

            bw.Dispose();
            fs.Dispose();
        }

        #endregion
        #region 私有函数
        //将位序为big的数据转为little
        private static Int32 OnChangeByteOrder(Int32 indata)
        {
            byte[] a = new byte[4];
            a[3] = (byte)(indata & 0xFF);
            a[2] = (byte)((indata & 0xFF00) >> 8);
            a[1] = (byte)((indata & 0xFF0000) >> 16);
            a[0] = (byte)((indata >> 24) & 0xFF);
            return BitConverter.ToInt32(a, 0);
        }
        //读取点状目标数据
        private static void ReadShpPoint(BinaryReader br, ref Layer layer)
        {
            Int32 RecordNum;//记录号
            Int32 ContentLength;//坐标记录长度
            while (true)//逐个记录进行读取，直到文件末尾
            {
                try
                {
                    RecordNum = OnChangeByteOrder(br.ReadInt32());
                }
                catch (EndOfStreamException)
                {
                    break;
                }
                ContentLength = OnChangeByteOrder(br.ReadInt32());
                Int32 ShapeType = br.ReadInt32();//记录的几何类型，点为1
                if (ShapeType == 0)//null shape
                {
                    layer.Records.Rows.Add(RecordNum, null, null);//将该记录添加到layer中
                    continue;
                }
                Double x = br.ReadDouble();//x坐标
                Double y = br.ReadDouble();//y坐标
                PointD mPoint = new PointD(x, y);
                layer.Records.Rows.Add(RecordNum, mPoint, null);//将该记录添加到layer中
            }
        }
        //读取线状目标数据
        private static void ReadShpPolyline(BinaryReader br, ref Layer layer)
        {
            Int32 RecordNum;//记录号
            Int32 ContentLength;//坐标记录长度
            while (true)//逐个记录进行读取，直到文件末尾
            {
                try
                {
                    RecordNum = OnChangeByteOrder(br.ReadInt32());
                }
                catch (EndOfStreamException)
                {
                    break;
                }
                ContentLength = OnChangeByteOrder(br.ReadInt32());
                Int32 ShapeType = br.ReadInt32();//记录的几何类型，线状目标为3
                if (ShapeType == 0)//null shape
                {
                    layer.Records.Rows.Add(RecordNum, null, null);//将该记录添加到layer中
                    continue;
                }
                //读取外包矩形数据
                Double MinX = br.ReadDouble();
                Double MinY = br.ReadDouble();
                Double MaxX = br.ReadDouble();
                Double MaxY = br.ReadDouble();
                RectangleD MBR = new RectangleD(MinX, MaxX, MinY, MaxY);

                Int32 NumParts = br.ReadInt32();//构成当前线目标的子线段个数
                Int32 NumPoints = br.ReadInt32();//构成当前线目标的坐标点个数
                Int32[] Parts = new Int32[NumParts + 1];//记录每个子线段的点坐标信息在所有点坐标信息中的起始位置,为计算方便在末尾加入坐标点总个数
                for (int i = 0; i < NumParts; i++)
                {
                    Parts[i] = br.ReadInt32();
                }
                Parts[NumParts] = NumPoints;

                //读取所有点的坐标信息，记录在各个子线段中
                List<PolyLine> mPolyLine = new List<PolyLine>(NumParts);
                for (int i = 0; i < NumParts; i++)//对各个子线段的点坐标进行读取
                {
                    int PointNum = Parts[i + 1] - Parts[i];//当前子线段的点个数

                    PointD[] Points = new PointD[PointNum];
                    for (int j = 0; j < PointNum; j++)
                    {
                        Double x = br.ReadDouble();//x坐标
                        Double y = br.ReadDouble();//y坐标
                        Points[j] = new PointD(x, y);
                    }
                    mPolyLine.Add(new PolyLine(Points));
                }
                MultiPolyLine mMultiPolyLine = new MultiPolyLine(mPolyLine, MBR);//生成读取的MultiPolyLine对象
                layer.Records.Rows.Add(RecordNum, mMultiPolyLine, null);//将该记录添加到layer中
            }
        }
        //读取面状目标数据
        private static void ReadShpPolygon(BinaryReader br, ref Layer layer)
        {
            Int32 RecordNum;//记录号
            Int32 ContentLength;//坐标记录长度
            while (true)//逐个记录进行读取，直到文件末尾
            {
                try
                {
                    RecordNum = OnChangeByteOrder(br.ReadInt32());
                }
                catch (EndOfStreamException)
                {
                    break;
                }

                ContentLength = OnChangeByteOrder(br.ReadInt32());
                Int32 ShapeType = br.ReadInt32();//记录的几何类型，面状目标为5
                if (ShapeType == 0)//null shape
                {
                    layer.Records.Rows.Add(RecordNum, null, null);//将该记录添加到layer中
                    continue;
                }
                //读取外包矩形数据
                Double MinX = br.ReadDouble();
                Double MinY = br.ReadDouble();
                Double MaxX = br.ReadDouble();
                Double MaxY = br.ReadDouble();
                RectangleD MBR = new RectangleD(MinX, MaxX, MinY, MaxY);

                Int32 NumParts = br.ReadInt32();//构成当前多边形目标的子环个数
                Int32 NumPoints = br.ReadInt32();//构成当前多边形目标的坐标点个数
                Int32[] Parts = new Int32[NumParts + 1];//记录每个子环的点坐标信息在所有点坐标信息中的起始位置,为计算方便在末尾加入坐标点总个数
                for (int i = 0; i < NumParts; i++)
                {
                    Parts[i] = br.ReadInt32();
                }
                Parts[NumParts] = NumPoints;

                //读取所有点的坐标信息，记录在各个子环中
                List<Polygon> mPolygon = new List<Polygon>(NumParts);
                for (int i = 0; i < NumParts; i++)//对各个子环的点坐标进行读取
                {
                    int PointNum = Parts[i + 1] - Parts[i];//当前子环的点个数

                    PointD[] Points = new PointD[PointNum - 1];
                    for (int j = 0; j < PointNum - 1; j++)
                    {
                        Double x = br.ReadDouble();//x坐标
                        Double y = br.ReadDouble();//y坐标
                        Points[j] = new PointD(x, y);
                    }
                    br.ReadBytes(16);//最后一个点坐标即为第一个点坐标，跳过
                    mPolygon.Add(new Polygon(Points));
                }
                MultiPolygon mMultiPolygon = new MultiPolygon(mPolygon, MBR);//生成读取的MultiPolygon对象
                layer.Records.Rows.Add(RecordNum, mMultiPolygon, null);//将该记录添加到layer中
            }
        }
        //读dbf文件
        private static void ReadDbf(ref Layer layer, string fileName)
        {
            int sIndex = fileName.LastIndexOf("\\");
            string sName = fileName.Substring(0, fileName.Length - 4) + ".dbf";
            //FileStream fs = null;
            //BinaryReader br = null;

            using (FileStream fs = new FileStream(sName, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {

                    //（1）读文件头
                    byte Vertion = br.ReadByte();//版本信息
                                                 //更新日期
                    byte Year = br.ReadByte();
                    byte Mouth = br.ReadByte();
                    byte Day = br.ReadByte();

                    Int32 RecordNum = br.ReadInt32();//记录条数
                    Int16 HeadLength = br.ReadInt16();//文件头字节数
                    Int16 RecordLength = br.ReadInt16();//一条记录的字节长度
                    br.ReadBytes(20);//跳过保留字节、dBASE IV编密码标记、MXD标识等信息

                    int FieldsCount = (HeadLength - 32) / 32;//表中记录项的个数
                    Byte[] FieldLength = new Byte[FieldsCount];//各记录项的长度
                    for (int i = 0; i < FieldsCount; i++)//读取各个记录项描述信息
                    {
                        Byte[] sFieldName = br.ReadBytes(11);//记录项名称
                        string FieldName = Encoding.GetEncoding("GBK").GetString(sFieldName).Trim();//将记录项名称转为字符串
                        Byte FieldType = br.ReadByte();//记录项数据类型
                        br.ReadBytes(4);//跳过保留字节
                        FieldLength[i] = br.ReadByte();//记录项长度
                        Byte DemicalCount = br.ReadByte();//记录项精度
                        br.ReadBytes(14);//跳过保留字节、工作区ID、MDX标识

                        if (FieldType == 67)//记录项是字符型
                        {
                            layer.AddField(FieldName, typeof(string));
                            layer.Records.Columns[Layer.FIELD_START_INDEX + i].MaxLength = FieldLength[i];
                        }
                        else if (FieldType == 78)//记录项是数值型
                        {
                            if (DemicalCount == 0)
                            {
                                layer.AddField(FieldName, typeof(Int32));
                            }
                            else
                            {
                                layer.AddField(FieldName, typeof(double));
                            }
                        }
                        else
                        {
                            layer.AddField(FieldName, typeof(string));
                            layer.Records.Columns[Layer.FIELD_START_INDEX + i].MaxLength = FieldLength[i];
                        }
                    }
                    byte Terminator = br.ReadByte();//记录项终止标识
                    //（2）读文件记录

                    Byte[] svalue;
                    Int32 sint;
                    Double sdouble;
                    string str;

                    for (int i = 0; i < RecordNum; i++)//逐条记录进行读取
                    {
                        br.ReadByte();//每条记录开头是一个空字符
                        for (int j = 0; j < FieldsCount; j++)//读取记录中的每个记录项
                        {
                            svalue = br.ReadBytes(FieldLength[j]);
                  
                            if (layer.Records.Columns[j + Layer.FIELD_START_INDEX].DataType == typeof(Int32))
                            {
                                try
                                {
                                    str = Encoding.ASCII.GetString(svalue);
                                    if (Int32.TryParse(str, out sint))
                                    {
                                        layer.Records.Rows[i][j + Layer.FIELD_START_INDEX] = sint;
                                    }
                                }
                                catch (Exception)//文件中可能有非法字符或浮点数，遇到则不显示或进行转换
                                {
                                    try
                                    {
                                        str = Encoding.ASCII.GetString(svalue);
                                        if (Double.TryParse(str, out sdouble))
                                        {
                                            layer.Records.Rows[i][j + Layer.FIELD_START_INDEX] = (double)sdouble;
                                        }
                                    }
                                    catch (Exception) { }
                                }
                            }
                            else if (layer.Records.Columns[j + Layer.FIELD_START_INDEX].DataType == typeof(Double))
                            {
                                try
                                {
                                    str = Encoding.ASCII.GetString(svalue);
                                    if (Double.TryParse(str, out sdouble))
                                    {
                                        layer.Records.Rows[i][j + Layer.FIELD_START_INDEX] = sdouble;
                                    }
                                }
                                catch (Exception) { }
                            }
                            else if (layer.Records.Columns[j + Layer.FIELD_START_INDEX].DataType == typeof(string))
                            {
                                layer.Records.Rows[i][j + Layer.FIELD_START_INDEX] = Encoding.GetEncoding("GBK").GetString(svalue).Trim();
                            }
                        }
                    }
                }
            }
        }
        //保存点状目标数据
        private static void SavePoint(BinaryWriter bw, ref Layer layer)
        {
            int RecordNum = layer.Records.Rows.Count;
            for (int i = 0; i < RecordNum; i++)
            {
                bw.Write(OnChangeByteOrder(i + 1));//ID
                if (Convert.IsDBNull(layer.Records.Rows[i][1]))//null shape
                {
                    bw.Write(OnChangeByteOrder(2));
                    bw.Write(0);//几何类型
                    continue;
                }
                bw.Write(OnChangeByteOrder(10));//坐标记录长度
                bw.Write(1);//几何类型
                //坐标信息
                PointD mpoint = (PointD)layer.Records.Rows[i][1];
                bw.Write(mpoint.X);
                bw.Write(mpoint.Y);
            }
        }
        //保存线状目标数据
        private static void SavePolyline(BinaryWriter bw, ref Layer layer)
        {
            int RecordNum = layer.Records.Rows.Count;
            for (int i = 0; i < RecordNum; i++)
            {
                bw.Write(OnChangeByteOrder(i + 1));//ID
                if (Convert.IsDBNull(layer.Records.Rows[i][1]))//null shape
                {
                    bw.Write(OnChangeByteOrder(2));
                    bw.Write(0);//几何类型
                    continue;
                }
                MultiPolyLine mMultiPolyLine = (MultiPolyLine)layer.Records.Rows[i][1];
                bw.Write(OnChangeByteOrder(GetCodeLength(mMultiPolyLine)));//坐标记录长度
                bw.Write(3);//写入几何类型
                //MBR数据
                bw.Write(mMultiPolyLine.MBR.MinX);
                bw.Write(mMultiPolyLine.MBR.MinY);
                bw.Write(mMultiPolyLine.MBR.MaxX);
                bw.Write(mMultiPolyLine.MBR.MaxY);
                Int32 NumParts = mMultiPolyLine.Count;
                bw.Write(NumParts);//子线段个数
                Int32 NumPoints = (GetCodeLength(mMultiPolyLine) - 22 - 2 * NumParts) / 8;
                bw.Write(NumPoints);//点个数
                Int32 parts = 0;
                bw.Write(parts);//每个子线段的点坐标信息在所有点坐标信息中的起始位置，第一个为0
                for (int j = 0; j < NumParts - 1; j++)
                {
                    parts += mMultiPolyLine.PolyLines[j].Count;
                    bw.Write(parts);
                }
                //点坐标信息
                for (int j = 0; j < NumParts; j++)
                {
                    for (int k = 0; k < mMultiPolyLine.PolyLines[j].Count; k++)
                    {
                        bw.Write(mMultiPolyLine.PolyLines[j].Points[k].X);
                        bw.Write(mMultiPolyLine.PolyLines[j].Points[k].Y);
                    }
                }
            }
        }
        //保存面状目标数据
        private static void SavePolygon(BinaryWriter bw, ref Layer layer)
        {
            int RecordNum = layer.Records.Rows.Count;
            for (int i = 0; i < RecordNum; i++)
            {
                bw.Write(OnChangeByteOrder(i + 1));//ID
                if (Convert.IsDBNull(layer.Records.Rows[i][1]))//null shape
                {
                    bw.Write(OnChangeByteOrder(2));//坐标记录长度
                    bw.Write(0);//几何类型
                    continue;
                }
                MultiPolygon mMultiPolygon = (MultiPolygon)layer.Records.Rows[i][1];
                bw.Write(OnChangeByteOrder(GetCodeLength(mMultiPolygon)));//坐标记录长度
                bw.Write(5);//写入几何类型
                //MBR数据
                bw.Write(mMultiPolygon.MBR.MinX);
                bw.Write(mMultiPolygon.MBR.MinY);
                bw.Write(mMultiPolygon.MBR.MaxX);
                bw.Write(mMultiPolygon.MBR.MaxY);
                Int32 NumParts = mMultiPolygon.Count;
                bw.Write(NumParts);//子环个数
                Int32 NumPoints = (GetCodeLength(mMultiPolygon) - 22 - 2 * NumParts) / 8;
                bw.Write(NumPoints);//点个数
                Int32 parts = 0;
                bw.Write(parts);//每个子环的点坐标信息在所有点坐标信息中的起始位置，第一个为0
                for (int j = 0; j < NumParts - 1; j++)
                {
                    parts += (mMultiPolygon.Polygons[j].Count + 1);
                    bw.Write(parts);
                }
                //点坐标信息
                for (int j = 0; j < NumParts; j++)
                {
                    for (int k = 0; k < mMultiPolygon.Polygons[j].Count; k++)
                    {
                        bw.Write(mMultiPolygon.Polygons[j].Points[k].X);
                        bw.Write(mMultiPolygon.Polygons[j].Points[k].Y);
                    }
                    bw.Write(mMultiPolygon.Polygons[j].Points[0].X);
                    bw.Write(mMultiPolygon.Polygons[j].Points[0].Y);
                }
            }
        }
        //保存Dbf文件
        private static void SaveDbf(ref Layer layer, string fileName)
        {
            int sIndex = fileName.LastIndexOf("\\");
            string sName = fileName.Substring(0, fileName.Length - 4) + ".dbf";
            FileStream fs = new FileStream(sName, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);
            //（1）写文件头
            bw.Write((byte)3);//版本信息
            //更新日期
            byte Year = (byte)(DateTime.Now.Year - 1900);
            byte Mouth = (byte)DateTime.Now.Month;
            byte Day = (byte)DateTime.Now.Day;
            bw.Write(Year);
            bw.Write(Mouth);
            bw.Write(Day);
            Int32 RecordNum = layer.Records.Rows.Count;
            bw.Write(RecordNum);//记录数

            //读取各字段数据长度、精度、类型，计算每条记录的长度
            Int16 FieldCount = (Int16)(layer.Records.Columns.Count - Layer.FIELD_START_INDEX);//字段数
            bw.Write((Int16)(33 + 32 * FieldCount));//头文件字节数
            Byte[] FieldLength = new byte[FieldCount];//字段长度数组
            Byte[] FieldDemical = new byte[FieldCount];//字段精度数组
            Int16 RecordLength = 1;//每条记录的长度
            byte[] FieldType = new byte[FieldCount];//字段类型数组
            for (int i = 0; i < FieldCount; i++)
            {
                if (layer.Records.Columns[Layer.FIELD_START_INDEX + i].DataType == typeof(int))
                {
                    FieldLength[i] = 11;
                    FieldType[i] = 78;
                    FieldDemical[i] = 0;
                }
                else if (layer.Records.Columns[Layer.FIELD_START_INDEX + i].DataType == typeof(double))
                {
                    FieldLength[i] = 31;
                    FieldType[i] = 78;
                    FieldDemical[i] = 15;
                }
                else
                {
                    FieldLength[i] = (byte)layer.Records.Columns[Layer.FIELD_START_INDEX + i].MaxLength;
                    FieldType[i] = 67;
                    FieldDemical[i] = 0;
                }
                RecordLength += (Int16)FieldLength[i];
            }
            bw.Write(RecordLength);
            for (int i = 0; i < 4; i++)//保留字节
                bw.Write(0);
            bw.Write((byte)0);//MDX标识
            bw.Write((byte)0x4D);//Language driver ID
            bw.Write((Int16)0);//保留字节
            //字段信息描述数组
            for (int i = 0; i < FieldCount; i++)
            {
                //字段名称，用空字符补足11位后转为ascii码值写入文件
                string FieldName = layer.Records.Columns[Layer.FIELD_START_INDEX + i].ColumnName;
                FieldName = FieldName.PadRight(11, '\0');
                Byte[] byteName = System.Text.Encoding.ASCII.GetBytes(FieldName);
                bw.Write(byteName);

                bw.Write(FieldType[i]);//字段数据类型
                bw.Write(0);//保留字节
                bw.Write(FieldLength[i]);//字段数据长度
                bw.Write(FieldDemical[i]);//字段数据精度
                //保留字节
                bw.Write((Int16)0);
                bw.Write(0);
                bw.Write(0);
                bw.Write(0);
            }
            bw.Write((byte)13);//记录项终止标识
            //（2）写文件记录
            for (int i = 0; i < RecordNum; i++)
            {
                bw.Write((byte)32);//每条记录开头是一个空字符
                for (int j = 0; j < FieldCount; j++)//读取记录中的每个记录项
                {
                    //将每个值转成字符串，再转为GBK编码，写入文件，并用空格补齐
                    string svalue = layer.Records.Rows[i][j + Layer.FIELD_START_INDEX].ToString();
                    Byte[] byteValue = System.Text.Encoding.GetEncoding("GBK").GetBytes(svalue);
                    bw.Write(byteValue);
                    for (int k = 0; k < FieldLength[j] - byteValue.Count(); k++)
                    {
                        bw.Write((byte)32);
                    }
                }
            }
            bw.Write((byte)0x1A);//DBF文件结尾
            bw.Dispose();
            fs.Dispose();
        }
        //保存shx文件
        private static void SaveShx(ref Layer layer, string fileName)
        {
            int sIndex = fileName.LastIndexOf("\\");
            string sName = fileName.Substring(0, fileName.Length - 4) + ".shx";
            FileStream fs = new FileStream(sName, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);
            //（1）写文件头
            bw.Write(OnChangeByteOrder(9994));
            for (int i = 0; i < 5; i++)
            {
                bw.Write(0);
            }
            Type GeoType = layer.GeoType;//获取图层记录的要素类型
            Int32 RecondNum = layer.Records.Rows.Count;//要素个数
            Int32 FileLength = 50 + RecondNum * 4;//文件长度
            bw.Write(OnChangeByteOrder(FileLength));//写入文件长度
            bw.Write(1000);//写入版本号
            //写入几何类型
            if (GeoType == typeof(PointD))
            {
                bw.Write(1);
            }
            else if (GeoType == typeof(MultiPolyLine))
            {
                bw.Write(3);
            }
            else if (GeoType == typeof(MultiPolygon))
            {
                bw.Write(5);
            }
            else
            {
                throw new Exception("不支持的数据类型");
            }
            //写入MBR数据
            bw.Write(layer.MBR.MinX);
            bw.Write(layer.MBR.MinY);
            bw.Write(layer.MBR.MaxX);
            bw.Write(layer.MBR.MaxY);
            //最大、最小Z值、M值为0
            for (int i = 0; i < 4; i++)
            {
                bw.Write((double)0);
            }

            //（2）写实体信息部分
            Int32 Offset = 50;//坐标文件中对应记录的起始位置相对文件起始位置的偏移量，第一条记录为50
            Int32 ContentLength = 0;//坐标文件中对应记录的长度
            for (int i = 0; i < RecondNum; i++)
            {
                bw.Write(OnChangeByteOrder(Offset));//写入偏移量
                //获取对应记录的长度
                if (Convert.IsDBNull(layer.Records.Rows[i][1]))//null shape
                    ContentLength = 2;
                else
                {
                    if (GeoType == typeof(PointD))
                        ContentLength = 10;
                    else if (GeoType == typeof(MultiPolyLine))
                    {
                        MultiPolyLine mMultiPolyLine = (MultiPolyLine)layer.Records.Rows[i][1];
                        ContentLength = GetCodeLength(mMultiPolyLine);
                    }
                    else
                    {
                        MultiPolygon mMultiPolygon = (MultiPolygon)layer.Records.Rows[i][1];
                        ContentLength = GetCodeLength(mMultiPolygon);
                    }
                }
                bw.Write(OnChangeByteOrder(ContentLength));//写入记录长度
                Offset += (ContentLength + 4);//更新偏移量
            }
            bw.Dispose();
            fs.Dispose();
        }

        private static int GetCodeLength(MultiPolyLine multiPolyline)
        {
            int n = multiPolyline.Count;
            int length = 0;
            for (int i = 0; i < n; i++)
            {
                length += multiPolyline.PolyLines[i].Count * 8;//每个点坐标信息占8个长度
            }
            return 22 + 2 * n + length;
        }

        private static int GetCodeLength(MultiPolygon multiPolygon)
        {
            int n = multiPolygon.Count;
            int length = 0;
            for (int i = 0; i < n; i++)
            {
                length += (multiPolygon.Polygons[i].Count + 1) * 8;//每个点坐标信息占8个长度
            }
            return 22 + 2 * n + length;
        }

        #endregion
    }
}

