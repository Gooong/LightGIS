using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LightGIS_1._0
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
            searchFrm c = new searchFrm();
            c.Show();
        }
    }
}
