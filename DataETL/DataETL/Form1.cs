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
        private void dailyScatter(object sender, EventArgs e)
        {
            TemporalAnalysis ta = new TemporalAnalysis();
            ta.graphCityGroups();
        }
        private void dailyScatterMonthly(object sender, EventArgs e)
        {
            TemporalAnalysis ta = new TemporalAnalysis();
            ta.temporalCityGroupMonthly();
        }
        private void dailyScatterTSRS(object sender, EventArgs e)
        {
            TemporalAnalysis ta = new TemporalAnalysis();
            ta.graphAllTS_RS();
        }
        private void checkTenMinAverages(object sender, EventArgs e)
        {
            MongoTools.checkAveraging();
        }
        private void cloudClean(object sender, EventArgs e)
        {
            MongoTools.cloudCleaner();
        }
        private void checkIndexes(object sender, EventArgs e)
        {
            MongoTools.checkIndexes();
        }
        private void addIndexes(object sender, EventArgs e)
        {
            IndexStationVariableCollections isvc = new IndexStationVariableCollections();
         
        }
        private void visualiseEpws(object sender, EventArgs e)
        {
            epwVisualiser.EpwImageMaker images = new epwVisualiser.EpwImageMaker(@"C:\Users\Admin\Documents\projects\IAPP\piloto\Climate\epw");
        }
        private void syntheticYear(object sender, EventArgs e)
        {
            CityYearBuilder cyb = new CityYearBuilder();
            //cyb.syntheticYearBatch("medianHour");
            //cyb.syntheticYearDataPrep("regionGroups");
            //cyb.syntheticYearBatch("meanHour");
            //cyb.syntheticYearBatch("randomHour");
            //cyb.syntheticYearBatch("cdfDay");
        }
        private void fixSyntheticYear(object sender, EventArgs e)
        {
            CityYearFixer cyf = new CityYearFixer();
        }
        private void readSyntheticYear(object sender, EventArgs e)
        {
            CityYearBuilder cyb = new CityYearBuilder();
            cyb.readSythYearFromDB();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            //Reader reader = new Reader();
        }
        private void uploadTest(object sender, EventArgs e)
        {
            string[] folders = {
                //reload
            //@"C:\Users\Admin\Documents\projects\IAPP\piloto\Climate\IDEAM\data\StationVariable\processed",
            //@"C:\Users\Admin\Documents\projects\IAPP\piloto\Climate\IDEAM\data\StationVariableBogBuc\processed",
            @"C:\Users\Admin\Documents\projects\IAPP\piloto\Climate\NOAA\data\variables",
                //tohere to check split completion
            ///@"C:\Users\Admin\Documents\projects\IAPP\piloto\Climate\IDEAM\data\Variable\processed",
            //@"C:\Users\Admin\Documents\projects\IAPP\piloto\Climate\IDEAM\data\Station_Radiation\processed",
            
            };
            String key = "noaa";
            //MongoTools.cleanUpByKeyword(key);
            //Reader reader = new Reader(folders);
            Splitter split = new Splitter();
            split.splitByKeyWord("climaColombia", key);

            //TemporalAnalysis ta = new TemporalAnalysis();
            //ta.dateTimeTest(key);

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
        private void printcitymeta(object sender, EventArgs e)
        {
            MonthlySummary ds = new MonthlySummary();
            ds.printCityMeta();
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
        private void addVariables(object sender,EventArgs e)
        {
            AnnualSummary s = new AnnualSummary();
            s.getRequiredVaribleMeta();
        }
        private void removePACollections(object sender, EventArgs e)
        {
            MongoTools.cleanUp();
        }
        private void checkRadLoad(object sender, EventArgs e)
        {
            MongoTools.checkRadiationLoaded();
        }
        private void convertTenMinuteCollections(object sender, EventArgs e)
        {
            
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
