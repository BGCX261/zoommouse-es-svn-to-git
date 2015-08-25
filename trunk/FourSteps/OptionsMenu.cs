using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ZoomMouse
{
    public partial class OptionsMenu : Form
    {
        Main parent;        

        public OptionsMenu(Main prt)
        {
            InitializeComponent();
            this.parent = prt;
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (numericUpDown1.Value < 2)
            {
                numericUpDown1.Value = 2;
            } else if (numericUpDown1.Value > 7)
            {
                numericUpDown1.Value = 7;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        //private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        //{
        //    if (numericUpDown2.Value < 0.5M)
        //    {
        //        numericUpDown2.Value = 0.5M;
        //    }
        //    else if (numericUpDown2.Value > 10.0M)
        //    {
        //        numericUpDown2.Value = 10.0M;
        //    }
        //}

        //private void checkBox1_CheckedChanged(object sender, EventArgs e)
        //{

        //}
    }
}
