using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using ZedGraph;
using System.Drawing;

namespace DataETL
{
    class MonthlySummary
    {
        IMongoDatabase db;
        List<StationMonthly> stations = new List<StationMonthly>();
        public MonthlySummary()
        {
            db = MongoTools.connect("mongodb://localhost", "climaColombia");
        }
        public void plot()
        {
            //get station info
            stations = getMonthlySummaryFromDB();
            foreach (StationMonthly sm in stations)
            {
                //multiMonthLineGraph(sm);
                multiMonthBarChart(sm);
            }
        }
        public List<StationMonthly> getMonthlySummaryFromDB()
        {
            //get the collection with summary names
            IMongoCollection<BsonDocument> names = db.GetCollection<BsonDocument>("summaryCollectionNames");
            var name = names.Find(new BsonDocument()).ToList();
            IMongoCollection<StationMonthly> collection = db.GetCollection<StationMonthly>("monthlyStationSummary_2018_7_8_14_15_27");
            
            var filter = FilterDefinition<StationMonthly>.Empty;
            var vms = collection.FindSync(filter).ToList();
            return vms;
        }
        private void multiMonthLineGraph(StationMonthly sm)
        {
            ZedGraphControl zgc = new ZedGraphControl();
            MasterPane master = zgc.MasterPane;
            master.Rect = new RectangleF(0, 0, 2400, 600);
            master.PaneList.Clear();
            master.Title.IsVisible = true;
            master.Title.Text = sm.code.ToString();
            master.Margin.All = 10;
            master.Legend.IsVisible = false;
            GraphPane pane1 = new GraphPane();
            pane1.Legend.IsVisible = true;
            pane1.YAxis.Title.Text = "n records";
            pane1.XAxis.Title.Text = "cumulative months";
            //find the first month and year of all monthlytotals
            //generate yaxis titles 
            int startYear = 0;
            int startMonth = 0;
            sm.firstMonth(ref startYear, ref startMonth);
            int endYear = 0;
            int endMonth = 0;
            sm.lastMonth(ref endYear, ref endMonth);
            DateTime startDate = new DateTime();
            if (startYear != 10000) startDate = new DateTime(startYear, startMonth, 1);
            DateTime endDate = new DateTime();
            if (endYear != 0) endDate = new DateTime(endYear, endMonth, 1);
            int monthsSpan = (int)Math.Round((endDate - startDate).Days / 30.0, 1);
            foreach (VariableMonthly vm in sm.variablesMonthly)
            {
                Color col;
                switch (vm.variableName)
                {
                    case "VV":
                        col = Color.Red;
                        break;
                    case "DV":
                        col = Color.DodgerBlue;
                        break;
                    case "NUB":
                        col = Color.ForestGreen;
                        break;
                    case "RS":
                        col = Color.BlueViolet;
                        break;
                    case "TS":
                        col = Color.Red;
                        break;
                    case "HR":
                        col = Color.LawnGreen;
                        break;
                    case "PR":
                        col = Color.Orchid;
                        break;
                    default:
                        col = Color.Black;
                        break;
                }
                List<double> xvalues = new List<double>();
                List<double> yvalues = new List<double>();
               
                for (int m = 0; m <= monthsSpan+1; m++)
                {
                    xvalues.Add(m);
                    yvalues.Add(0);
                    
                }
                List<MonthValue> monthlyvalues = new List<MonthValue>();
                foreach (MonthTotal mt in vm.monthlytotals)
                {
                    if (mt.month == 0 || mt.year == 0) continue;
                    DateTime date = new DateTime(mt.year, mt.month, 1);
                    int datediff = (int)Math.Round((date - startDate).TotalDays / 30.0, 1);
                    if(vm.interval==10) yvalues[datediff] = mt.total/6;
                    else yvalues[datediff] = mt.total;
                }
                
                double[] x = xvalues.ToArray();
                double[] y = yvalues.ToArray();
                LineItem myCurve = new LineItem(vm.variableName, x, y, col,SymbolType.None, 3.0f);
                pane1.CurveList.Add(myCurve);

            }
            master.Add(pane1);
            // Refigure the axis ranges for the GraphPanes
            zgc.AxisChange();

            // Layout the GraphPanes using a default Pane Layout
            Bitmap b = new Bitmap(600, 1200);
            using (Graphics g = Graphics.FromImage(b))
            {
                master.SetLayout(g, PaneLayout.SingleColumn);
            }
            master.GetImage().Save(@"D:\WORK\piloto\Climate\monthlyGraphs\" + sm.code.ToString() + ".jpeg", System.Drawing.Imaging.ImageFormat.Jpeg);

        }
        private void multiMonthBarChart(StationMonthly sm)
        {
            ZedGraphControl zgc = new ZedGraphControl();
            MasterPane master = zgc.MasterPane;
            master.Rect = new RectangleF(0, 0, 2400,600);
            master.PaneList.Clear();
            master.Title.IsVisible = true;
            master.Title.Text = sm.code.ToString();
            master.Margin.All = 10;
            master.Legend.IsVisible = false;
            GraphPane pane1 = new GraphPane();
            pane1.BarSettings.Type = BarType.Stack;
            pane1.Legend.IsVisible = true;
            pane1.YAxis.Title.Text = "n records";
            pane1.XAxis.Title.Text = "cumulative months";
            //find the first month and year of all monthlytotals
            //generate yaxis titles 
            int startYear = 0;
            int startMonth = 0;
            sm.firstMonth(ref startYear, ref startMonth);
            int endYear = 0;
            int endMonth = 0;
            sm.lastMonth(ref endYear, ref endMonth);
            DateTime startDate = new DateTime();
            if (startYear != 10000) startDate = new DateTime(startYear, startMonth, 1);
            DateTime endDate = new DateTime();
            if (endYear != 0) endDate = new DateTime(endYear, endMonth, 1);
            int monthsSpan = (int)Math.Round((endDate - startDate).Days / 30.0,1);
            foreach (VariableMonthly vm in sm.variablesMonthly)
            {
                List<double> xvalues = new List<double>();
                for (int m = 0; m <= monthsSpan+1; m++)
                {
                    xvalues.Add(0);
                }
                Color col;
                switch(vm.variableName)
                {
                    case "VV":
                        col = Color.Red;
                        break;
                    case "DV":
                        col = Color.DodgerBlue;
                        break;
                    case "NUB":
                        col = Color.ForestGreen;
                        break;
                    case "RS":
                        col = Color.BlueViolet;
                        break;
                    case "TS":
                        col = Color.YellowGreen;
                        break;
                    case "HR":
                        col = Color.LawnGreen;
                        break;
                    case "PR":
                        col = Color.Orchid;
                        break;
                    default:
                        col = Color.Black;
                        break;
                }
                foreach (MonthTotal mt in vm.monthlytotals)
                {
                    if (mt.month == 0 || mt.year == 0) continue;
                    DateTime date = new DateTime(mt.year, mt.month, 1);
                    int datediff = (int)Math.Round((date - startDate).TotalDays/30.0,1);
                    if (vm.interval == 10) xvalues[datediff] = mt.total / 6;
                    else xvalues[datediff] = mt.total;
                }
                

                double[] x = xvalues.ToArray();

                BarItem myBar = pane1.AddBar(vm.variableName, null, x, col);
                myBar.Bar.Fill = new Fill(col,col,col);
                myBar.Bar.Border.IsVisible = false;

            }
            master.Add(pane1);
            // Refigure the axis ranges for the GraphPanes
            zgc.AxisChange();

            // Layout the GraphPanes using a default Pane Layout
            Bitmap b = new Bitmap(600, 1200);
            using (Graphics g = Graphics.FromImage(b))
            {
                master.SetLayout(g, PaneLayout.SingleColumn);
            }
            master.GetImage().Save(@"D:\WORK\piloto\Climate\monthlyBarCharts\" + sm.code.ToString() + ".jpeg", System.Drawing.Imaging.ImageFormat.Jpeg);
            
        }
        public static void getGroupFirstMonth(List<StationMonthly> group,ref int minyear, ref int minmonth)
        {
            minyear = 10000;
            minmonth = 13;
            int yr = 0;
            int mth = 0;
            foreach (StationMonthly sm in group)
            {
                sm.firstMonth(ref yr, ref mth);
                if (yr <= minyear)
                {
                    minyear = yr;
                    if (mth < minmonth) minmonth = mth;
                }
            }
        }
        public static void getGroupLastMonth(List<StationMonthly> group, ref int maxyear, ref int maxmonth)
        {
            maxyear = 0;
            maxmonth = 0;
            int yr = 0;
            int mth = 0;
            foreach (StationMonthly sm in group)
            {
                sm.lastMonth(ref yr, ref mth);
                if (yr >= maxyear)
                {
                    maxyear = yr;
                    if (mth > maxmonth) maxmonth = mth;
                }
            }
        }
        public void groupMonthly()
        {
            //get groups from DB
            var cityGroups = db.GetCollection<StationGroup>("cityGroups");
            List<StationGroup> stationgroups = cityGroups.Find(FilterDefinition<StationGroup>.Empty).ToList();
            List<StationMonthly> stationsMonthly = getMonthlySummaryFromDB();
            
            foreach (StationGroup sg in stationgroups)
            {
                //if (sg.name == "SANTA FE DE BOGOTÁ")
                //{
                    List<StationMonthly> group = new List<StationMonthly>();
                    foreach (int scode in sg.stationcodes)
                    {
                        var s = stationsMonthly.Find(x => x.code == scode);
                        group.Add(s);
                    }
                    if(group.Count>0)groupMonthBarChart(group, sg.name);
                //}
            }
        }
        private void groupMonthBarChart(List<StationMonthly> group,string groupname)
        {
            List<Station> stations = StationGrouping.getAllStationsFromDB(db);
            List<City> cities = MapTools.readCities();
            var c = cities.Find(x => x.name == groupname);
            //find the first month and year of all monthlytotals
            //generate yaxis titles 
            int startYear = 0;
            int startMonth = 0;
            getGroupFirstMonth(group, ref startYear, ref startMonth);
            int endYear = 0;
            int endMonth = 0;
            getGroupLastMonth(group, ref endYear, ref endMonth);
            DateTime startDate = new DateTime();
            if (startYear != 10000) startDate = new DateTime(startYear, startMonth, 1);
            DateTime endDate = new DateTime();
            if (endYear != 0) endDate = new DateTime(endYear, endMonth, 1);
            int monthsSpan = (int)Math.Round((endDate - startDate).Days / 30.0, 1);
            //set the master pane
            ZedGraphControl zgc = new ZedGraphControl();
            MasterPane master = zgc.MasterPane;
            master.Rect = new RectangleF(0, 0, 2000, 666*group.Count);
            master.PaneList.Clear();
            master.Title.IsVisible = true;
            master.Title.Text = "City: " +c.name +
                " lat: " + Math.Round(c.location[1], 3) + " lon: " + Math.Round(c.location[0], 3) +
                " alt: " + (int)(c.elevation) + "\nDate range: "+startDate.Year +"_"+ startDate.Month+ " >> " + endDate.Year +"_" +endDate.Month;
            master.Title.FontSpec = new FontSpec("Arial", 7.0f, Color.Black, false, false, false);
            master.Margin.All = 5;
            master.Legend.IsVisible = false;
            foreach (StationMonthly sm in group)
            {
                var s = stations.Find(x => x.code == sm.code);
                GraphPane pane1 = new GraphPane();
                pane1.BarSettings.Type = BarType.Stack;
                pane1.Border.IsVisible = false;
                pane1.Title.Text = sm.code.ToString()+" "+ s.name+
                    " lat: "+Math.Round(s.latitude,3)+" lon: "+ Math.Round(s.longitude,3)+
                    " alt: "+(int)(s.elevation);
                //only show the first
                pane1.Legend.IsVisible = true;

                //pane1.YAxis.Title.Text = "n records";
                pane1.XAxis.Scale.Max = monthsSpan;
                pane1.XAxis.Scale.MajorStep = 12.0;
                pane1.XAxis.Scale.MinorStep = 1.0;
                pane1.XAxis.MajorTic.Size = 10;
                pane1.XAxis.MajorGrid.IsVisible = true;
                foreach (VariableMonthly vm in sm.variablesMonthly)
                {
                    List<double> xvalues = new List<double>();
                    for (int m = 0; m <= monthsSpan + 1; m++)
                    {
                        xvalues.Add(0);
                    }
                    Color col;
                    switch (vm.variableName)
                    {
                        case "VV":
                            col = Color.Red;
                            break;
                        case "DV":
                            col = Color.DodgerBlue;
                            break;
                        case "NUB":
                            col = Color.ForestGreen;
                            break;
                        case "RS":
                            col = Color.BlueViolet;
                            break;
                        case "TS":
                            col = Color.YellowGreen;
                            break;
                        case "HR":
                            col = Color.LawnGreen;
                            break;
                        case "PR":
                            col = Color.Orchid;
                            break;
                        default:
                            col = Color.Black;
                            break;
                    }
                    foreach (MonthTotal mt in vm.monthlytotals)
                    {
                        if (mt.month == 0 || mt.year == 0) continue;
                        DateTime date = new DateTime(mt.year, mt.month, 1);
                        
                        int datediff = (int)Math.Round((date - startDate).TotalDays / 30.0, 1);
                        if (vm.interval == 10) xvalues[datediff] = mt.total / 6;
                        else xvalues[datediff] = mt.total;
                    }
                    double[] x = xvalues.ToArray();

                    BarItem myBar = pane1.AddBar(vm.variableName, null, x, col);
                    myBar.Bar.Fill = new Fill(col, col, col);
                    myBar.Bar.Border.IsVisible = false;
                 }
                pane1.AxisChange();
                master.Add(pane1);
            }
            // Refigure the axis ranges for the GraphPanes
            zgc.AxisChange();
            // Layout the GraphPanes using a default Pane Layout
            Bitmap b = new Bitmap(2000, 666 * group.Count);
            using (Graphics g = Graphics.FromImage(b))
            {
                master.SetLayout(g, PaneLayout.SingleColumn);
            }
            master.GetImage().Save(@"D:\WORK\piloto\Climate\groupMonthlyBarCharts\" + groupname + ".jpeg", System.Drawing.Imaging.ImageFormat.Jpeg);


        }
        public async Task monthlySummary()
        {
            List<string> collNames = MongoTools.collectionNames(db);
            string firstletter = "";
            string vname = "";
            int stationcode = 0;
            string source = "";
            int freq = 0;
            int count = 0;
            foreach (string collection in collNames)
            {
                //all station record collections start with an s_
                string[] parts = collection.Split('_');
                firstletter = parts[0];
                if (firstletter == "s")
                {
                    stationcode = Convert.ToInt32(parts[1]);
                    vname = parts[4];
                    if (vname == "PA") continue;
                    source = parts[2];
                    freq = Convert.ToInt32(parts[5]);
                    VariableMeta meta = AnnualSummary.getVariableMetaFromDB(vname, source,db);
                    VariableMonthly vm = new VariableMonthly(vname, freq);
                    addStation(stationcode);
                    addMonthlyRecord(stationcode, vm);
                    
                    await sortByDate(stationcode, collection, meta);
                    count++;
                }
            }
            insertManyMonthlySummary();
        }
        public void addMonthlyRecord(int scode, VariableMonthly vm)
        {
            var s = stations.Find(x => x.code == scode);
            if (s.variablesMonthly.Exists(x => x.variableName == vm.variableName))
            {
                var wtf = 0;
            }
            else
            {
                s.variablesMonthly.Add(vm);
            }
        }
        public void addStation(int code)
        {
            var s = stations.Find(x => x.code == code);
            if(s==null)
            {
                StationMonthly sm = new StationMonthly(code);
                stations.Add(sm);
            }
        }
        public async Task sortByDate(int sCode, string collname, VariableMeta vm)
        {
            IMongoCollection<RecordMongo> collection = db.GetCollection<RecordMongo>(collname);
            var filter = FilterDefinition<RecordMongo>.Empty;
            var sorter = Builders<RecordMongo>.Sort.Ascending("time");
            FindOptions<RecordMongo> options = new FindOptions<RecordMongo>
            {
                BatchSize = 1000,
                NoCursorTimeout = true
            };
            using (IAsyncCursor<RecordMongo> cursor = await collection.FindAsync(filter, options))
            {
                while (await cursor.MoveNextAsync())
                {
                    IEnumerable<RecordMongo> batch = cursor.Current;
                    foreach (RecordMongo rm in batch)
                    {
                        if (rm.value > vm.min && rm.value < vm.max)
                        {
                            //inside the range add to month total
                            incrementMonthTotal(sCode, vm.code, rm.time.Year, rm.time.Month);
                        }
                        
                    }
                }
            }
           
        }
        private void incrementMonthTotal(int scode,string vname,int year,int month)
        {
            var s = stations.Find(x => x.code == scode);
             var vm = s.variablesMonthly.Find(x => x.variableName == vname);
            var mt = vm.monthlytotals.Find(x => x.month == month && x.year == year);
            if (mt == null)
            {
                MonthTotal m = new MonthTotal();
                m.year = year;
                m.month = month;
                m.total = 1;
                vm.monthlytotals.Add(m);
            }
            else mt.total++;
        }
        private void addMonthlyTotals(int scode,string variable, List<MonthTotal> mtotals)
        {
            var s = stations.Find(x => x.code == scode);
            var mt = s.variablesMonthly.Find(x => x.variableName == variable);
            mt.monthlytotals = mtotals;
        }
        public void insertManyMonthlySummary()
        {
            DateTime dt = DateTime.Now;
            string collectionname = "monthlyStationSummary_" + dt.Year.ToString()
                + "_" + dt.Month.ToString() + "_" + dt.Day.ToString() + "_" + dt.Hour.ToString() + "_" + dt.Minute.ToString() + "_" + dt.Second.ToString();
            var collection = db.GetCollection<StationMonthly>(collectionname);
            MongoTools.storeSummaryCollectionName(db, collectionname);
            var listOfDocuments = new List<StationMonthly>();
            var limitAtOnce = 1000;
            var current = 0;

            foreach (StationMonthly ss in stations)
            {
                listOfDocuments.Add(ss);
                if (++current == limitAtOnce)
                {
                    current = 0;
                    var listToInsert = listOfDocuments;
                    var t = new Task(() => { collection.InsertManyAsync(listToInsert); });
                    t.Start();
                    listOfDocuments = new List<StationMonthly>();
                }
            }
            var f = new Task(() => { collection.InsertManyAsync(listOfDocuments); });
            f.Start();

        }
    }
    public class StationMonthly
    {
        public ObjectId Id { get; set; }
        public int code { get; set; }
        public List<VariableMonthly> variablesMonthly { get; set; }
        public StationMonthly(int scode)
        {
            code = scode;
            variablesMonthly = new List<VariableMonthly>();
        }
        public void addVariableMonthly(VariableMonthly vm)
        {
            variablesMonthly.Add(vm);
        }
        public void firstMonth(ref int minyear, ref int minmonth)
        {
            minyear = 10000;
            minmonth = 13;
            int yr = 0;
            int mth = 0;
            foreach (VariableMonthly vm in variablesMonthly)
            {
                vm.firstMonth(ref yr,ref mth);
                if (yr <= minyear)
                {
                    minyear = yr;
                    if (mth < minmonth) minmonth = mth;
                }
            }
        }
        public void lastMonth(ref int maxyear, ref int maxmonth)
        {
            maxyear = 0;
            maxmonth = 0;
            int yr = 0;
            int mth = 0;
            foreach (VariableMonthly vm in variablesMonthly)
            {
                vm.lastMonth(ref yr, ref mth);
                if (yr >= maxyear)
                {
                    maxyear = yr;
                    if (mth > maxmonth) maxmonth = mth;
                }
            }
        }
    }
    public class VariableMonthly
    {
        public List<MonthTotal> monthlytotals { get; set; }
        public string variableName { get; set; }
        public int interval { get; set; }
        public VariableMonthly(string name,int freq)
        {
            variableName = name;
            interval = freq;
            monthlytotals = new List<MonthTotal>();
        }
        public void firstMonth(ref int minyear,ref int minmonth)
        {
            minyear = 10000;
            minmonth = 13;
            foreach(MonthTotal mt in monthlytotals)
            {
                if (mt.year <= minyear)
                {
                    minyear = mt.year;
                    if (mt.month < minmonth) minmonth = mt.month;
                }
            }

        }
        public void lastMonth(ref int maxyear, ref int maxmonth)
        {
            maxyear = 0;
            maxmonth = 0;
            foreach (MonthTotal mt in monthlytotals)
            {
                if (mt.year >= maxyear)
                {
                    maxyear = mt.year;
                    if (mt.month > maxmonth) maxmonth = mt.month;
                }
            }

        }
    }
    public class MonthTotal
    {
        public int month { get; set; }
        public double total { get; set; }
        public int year { get; set; }
    }
    public class MonthValue
    {
        public double month;
        public double value;
    }
}
