using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace test
{
    public partial class Map : UserControl
    {
        public Map()
        {
            InitializeComponent();
        }
        public string 名称 { set; get; }
        public string 描述 { set; get; }
        public Color 填充色 { set; get; }
        public Color 边界色 { set; get; }
        public int 边界宽度 { set; get; }
        public string 坐标系 { set; get; }
    }
}
