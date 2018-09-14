using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace epwVisualiser
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private void genImageSummaries(object sender, EventArgs e)
        {
            EpwImageMaker images = new EpwImageMaker(@"C:\Users\Admin\Documents\projects\IAPP\piloto\Climate\epw");
        }
    }
}
