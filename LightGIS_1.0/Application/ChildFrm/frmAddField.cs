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
    public partial class frmAddField : Form
    {
        public frmAddField()
        {
            InitializeComponent();
        }

        public string FieldName
        {
            get { return textBox1.Text; }
        }

        public Type FieldType;

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 0)
            {
                FieldType = typeof(string);
            }
            else if (comboBox1.SelectedIndex == 1)
            {
                FieldType = typeof(Int32);
            }
            else if (comboBox1.SelectedIndex == 2)
            {
                FieldType = typeof(double);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (FieldType != null && FieldName != null)
            {
                DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}
