using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DataETL
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private void addIndexes(object sender, EventArgs e)
        {
            IndexStationVariableCollections isvc = new IndexStationVariableCollections();
         
        }
        private void syntheticYear(object sender, EventArgs e)
        {
            CityYearBuilder cyb = new CityYearBuilder();
            cyb.averageYear();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            Reader reader = new Reader();
        }
        private void splitData(object sender, EventArgs e)
        {
            Splitter s = new Splitter();
            s.splitVariables("climaColombia");
        }
        private void summary(object sender, EventArgs e)
        {
            AnnualSummary ds = new AnnualSummary();
            //ds.getRequiredVaribleMeta();
            try
            {
               ds.textSummaryStations("climaColombia");
            }
            catch(Exception ex)
            {
                var r = ex;
            }
        }
        private void monthlysummary(object sender, EventArgs e)
        {
            MonthlySummary ds = new MonthlySummary();
            //ds.getRequiredVaribleMeta();
            try
            {
                ds.monthlySummary();
            }
            catch (Exception ex)
            {
                var r = ex;
            }
        }
        private void monthlygraphs(object sender, EventArgs e)
        {
            MonthlySummary ds = new MonthlySummary();
            ds.plot();
        }
        private void groupedmonthlygraphs(object sender, EventArgs e)
        {
            MonthlySummary ds = new MonthlySummary();
            ds.groupMonthly();
        }
        private void outputAnnualSummary(object sender, EventArgs e)
        {
            AnnualSummary ds = new AnnualSummary();
            ds.outputAnnual();
        }
        private void setGroups(object sender, EventArgs e)
        {
            StationGrouping sg = new StationGrouping(false);
            
        }
        private void removePACollections(object sender, EventArgs e)
        {
            MongoTools.removeCollectionsAverage();
        }
        private void convertTenMinuteCollections(object sender, EventArgs e)
        {
            TenMinuteConversion tmc = new TenMinuteConversion();
            tmc.convert();
        }
        private void dropIndexes(object sender, EventArgs e)
        {
            MongoTools.dropIndexes();
        }
        private void loadStations(object sender, EventArgs e)
        {
            StationLoad sl = new StationLoad();
        }
        private void button2_Click(object sender, EventArgs e)
        {
            SQLconnector sqlConn = new SQLconnector();
            sqlConn.connect();

        }
    }
}
