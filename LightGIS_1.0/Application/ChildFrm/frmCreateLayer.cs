using LightGIS_1._0.GeoObject;
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
    public partial class frmCreateLayer : Form
    {
        public frmCreateLayer()
        {
            InitializeComponent();
        }

        #region
        public string layerName;
        public string filePath
        {
            get { return textBox1.Text; }
        }
        public Type geoType;
        #endregion

        private void button3_Click(object sender, EventArgs e)
        {
            if(mSaveLayerDialog.ShowDialog(this) == DialogResult.OK)
            {
                textBox2.Text = filePath;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(comboBox1.SelectedIndex == 0)
            {
                geoType = typeof(PointD);
            }
            else if (comboBox1.SelectedIndex == 1)
            {
                geoType = typeof(MultiPolyLine);
            }
            else if (comboBox1.SelectedIndex == 2)
            {
                geoType = typeof(MultiPolygon);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            bool sClose = true;
            if(layerName ==null || layerName == "")
            {
                layerName = "新图层";
            }
            if (filePath == null)
            {
                MessageBox.Show(this,"请选择文件目录");
                sClose = false;
            }
            if(geoType == null)
            {
                MessageBox.Show(this,"请选择图层要素类型");
                sClose = false;
            }
            if (sClose)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
