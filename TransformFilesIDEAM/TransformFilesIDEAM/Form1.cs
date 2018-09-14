using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TransformFilesIDEAM
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

         
        }
        private void convertRadFiles(object sender, EventArgs e)
        {
            ProcessText.convertFolderOfXLSX(@"C:\Users\Admin\Documents\projects\IAPP\piloto\Climate\IDEAM\data\Station_Radiation");
        }
        private void processRadiationFiles(object sender, EventArgs e)
        {
            ProcessText process = new ProcessText(@"C:\Users\Admin\Documents\projects\IAPP\piloto\Climate\IDEAM\data\Station_Radiation", "IDEAM", "radIDEAM");
        }
        private void processStation_VariableFiles(object sender, EventArgs e)
        {
            ProcessText process = new ProcessText(@"C:\Users\Admin\Documents\projects\IAPP\piloto\Climate\IDEAM\data\StationVariable", "IDEAM", "s_vIDEAM");
        }
        private void processBog_Buc(object sender, EventArgs e)
        {
            ProcessText process = new ProcessText(@"C:\Users\Admin\Documents\projects\IAPP\piloto\Climate\IDEAM\data\StationVariableBogBuc", "IDEAM", "bog_bucIDEAM");
        }
        private void processVariable(object sender,EventArgs e)
        {
            ProcessText process = new ProcessText(@"C:\Users\Admin\Documents\projects\IAPP\piloto\Climate\IDEAM\data\Variable\needed", "IDEAM", "variableIDEAM");
        }
    }
}
