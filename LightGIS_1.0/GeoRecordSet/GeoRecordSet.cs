using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace LightGIS_1._0.GeoRecordSet
{
    class GeoRecordSet
    {
        DataSet ds = new DataSet("test");
        DataTable dt = new DataTable("test");
        DataColumn dc = new DataColumn("string",typeof(string));
        DataColumn dc1 = new DataColumn("int", typeof(int));

        public GeoRecordSet()
        {
            dt.Columns.Add(dc);
            dt.Columns.Add(dc1);

            dt.Rows.Add("12", 12);
        }
        
    }
}
