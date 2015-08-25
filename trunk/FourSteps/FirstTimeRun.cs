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
    public partial class FirstTimeRun : Form
    {
        public FirstTimeRun()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void panel1_MouseClick(object sender, MouseEventArgs e)
        {
            //this.Focus();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void FirstTimeRun_Load(object sender, EventArgs e)
        {
            //label1.Padding = new Padding(panel1.Width / 2, panel1.Height / 2, panel1.Width / 2, panel1.Height / 2);
        }
    }
}
