using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LightGIS_1._0
{
    public partial class TreeViewEnhanced : TreeView
    {
        public TreeViewEnhanced()
        {
            InitializeComponent();
        }

        //消除双击的BUG
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x203) { m.Result = IntPtr.Zero; }
            else base.WndProc(ref m);
        }

    }
}
