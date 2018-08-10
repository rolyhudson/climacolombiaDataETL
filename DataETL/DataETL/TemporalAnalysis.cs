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
using System.IO;
using ZedGraph;
using System.Drawing;
using Accord.Statistics;


namespace DataETL
{
    
    class TemporalAnalysis
    {
        IMongoDatabase db;
        List<Station> stations = new List<Station>();
        List<StationGroup> stationsByCity = new List<StationGroup>();
        List<string> weatherCollections = new List<string>();
        public TemporalAnalysis()
        {
            db = MongoTools.connect("mongodb://localhost", "climaColombia");
        }
        public void graphAllTS_RS()
        {
            var colls = MongoTools.collectionNames(db);
            foreach(string coll in colls)
            {
                if (coll[0] == 's')
                {
                    if (coll.Contains("RS") || coll.Contains("TS"))
                    {
                        graphSingleStation(coll, coll);
                    }
                }
            }
        }
        public void temporalCityGroupMonthly()
        {
            stations = StationGrouping.getAllStationsFromDB(db);
            var coll = db.GetCollection<StationGroup>("cityGroups");
            stationsByCity = coll.Find(FilterDefinition<StationGroup>.Empty).ToList();
            foreach (StationGroup cityGroup in stationsByCity)
            {
                weatherCollections = getStationsColNames(cityGroup);
                if (cityGroup.name == "SANTA FE DE BOGOTÁ")
                {
                    cityMonthlyGraphs(cityGroup);
                }
                else
                {
                    //cityMonthlyGraphs(cityGroup);
                }
            }
        }
        private List<string> getIncludedVariables()
        {
            List<string> vars = new List<string>();
            foreach(string coll in weatherCollections)
            {
                string[] parts = coll.Split('_');
                string vname = parts[4];
                if (!vars.Contains(vname)) vars.Add(vname);
            }
            return vars;
        }
        public void graphCityGroups()
        {
            stations = StationGrouping.getAllStationsFromDB(db);
            var coll = db.GetCollection<StationGroup>("cityGroups");
            stationsByCity = coll.Find(FilterDefinition<StationGroup>.Empty).ToList();
            graphPerCity();
        }
        private void graphPerCity()
        {
            foreach (StationGroup cityGroup in stationsByCity)
            {
                List<string> weatherCollections = getStationsColNames(cityGroup);
                if (cityGroup.name == "SANTA FE DE BOGOTÁ")
                {
                    var wcss = splitList(weatherCollections, 10);
                    int count = 0;
                    foreach(List<string> wcs in wcss)
                    {
                        
                            cityGroupGraphic(cityGroup, wcs, cityGroup.name + "p_" + count);
                            count++;
                        
                    }
                }
                else
                {
                    //cityGroupGraphic(cityGroup, weatherCollections, cityGroup.name);
                }
            }
        }
        
        public static List<List<string>> splitList(List<string> locations, int nSize)
        {
            var list = new List<List<string>>();

            for (int i = 0; i < locations.Count; i += nSize)
            {
                list.Add(locations.GetRange(i, Math.Min(nSize, locations.Count - i)));
            }
            return list;
        }
        private void cityMonthlyGraphs(StationGroup cityGroup)
        {
            List<string> allVars = getIncludedVariables();
            for (int m = 1; m < 13; m++)
            {
                foreach (string variable in allVars)
                {
                    int chartCount = 0;
                    foreach(string wc in weatherCollections)
                    {
                        if (wc.Contains(variable) &&wc.Contains("Clean"))
                            chartCount++;
                    }
                    monthsPerVariable(cityGroup.name, variable, m, chartCount);
                }
            }
            
        }
        private async Task monthsPerVariable(string city,string variable,int month,int nCharts)
        {
            string[] months = { "jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec" };
            string title = city + "_" + variable + "_" + months[month-1];
            ZedGraphControl zgc = new ZedGraphControl();
            MasterPane master = zgc.MasterPane;
            MasterPane mp = setMaster(ref master, 2000,666*nCharts, title);
            foreach (string wc in weatherCollections)
            {
                string[] parts = wc.Split('_');
                int stationcode = Convert.ToInt32(parts[1]);
                string vname = parts[4];
                string source = parts[2];
                int freq = Convert.ToInt32(parts[5]);
                if (vname==variable&&source.Contains("Clean"))
                {
                    if (source.Contains("IDEAM")) source = "IDEAM";
                    else source = "NOAA";
                    VariableMeta meta = AnnualSummary.getVariableMetaFromDB(vname, source, db);
                   
                    PointPairList pointpair = new PointPairList();
                    pointpair = await GenerateMonthlyData(wc, month, meta);
                    if (pointpair.Count > 0)
                    {
                        AddChartToMaster(master, pointpair, wc, vname, meta,true);
                    }
                }
            }
            saveGraphic(zgc, master, @"D:\WORK\piloto\Climate\groupMonthlyScatterCharts\" + title+".jpeg");
        }
       

        private MasterPane setMaster(ref MasterPane master,int width,int height,string title)
        {
            
            master.Rect = new RectangleF(0, 0, width, height);
            master.PaneList.Clear();
            master.Title.IsVisible = true;
            master.Title.Text = title;
            master.Title.FontSpec = new FontSpec("Arial", 7.0f, Color.Black, false, false, false);
            //master.Margin.All = 5;
            master.Legend.IsVisible = false;
            return master;
        }
        private void saveGraphic(ZedGraphControl zgc, MasterPane master,string filepath)
        {
            zgc.AxisChange();
            // Layout the GraphPanes using a default Pane Layout
            Bitmap b = new Bitmap((int)master.Rect.Width, 666*master.PaneList.Count);
            using (Graphics g = Graphics.FromImage(b))
            {
                master.SetLayout(g, PaneLayout.SingleColumn);
            }
            master.GetImage().Save(filepath, System.Drawing.Imaging.ImageFormat.Jpeg);
        }
        private async Task graphSingleStation(string collection,string filename)
        {
            //set the master pane
            ZedGraphControl zgc = new ZedGraphControl();
            MasterPane master = zgc.MasterPane;
            master.Rect = new RectangleF(0, 0, 2000, 666);
            master.PaneList.Clear();
            //master.Title.Text = "City: " + c.name +
            //   " lat: " + Math.Round(c.location[1], 3) + " lon: " + Math.Round(c.location[0], 3) +
            //   " alt: " + (int)(c.elevation);// + "\nDate range: " + startDate.Year + "_" + startDate.Month + " >> " + endDate.Year + "_" + endDate.Month;
            master.Title.FontSpec = new FontSpec("Arial", 7.0f, Color.Black, false, false, false);
            master.Margin.All = 5;
            master.Legend.IsVisible = false;

            int stationcode = 0;
            string vname = "";
            string source = "";
            int freq = 0;
            string[] parts = collection.Split('_');
            stationcode = Convert.ToInt32(parts[1]);
            vname = parts[4];
            source = parts[2];
            freq = Convert.ToInt32(parts[5]);

            VariableMeta meta = AnnualSummary.getVariableMetaFromDB(vname, source, db);
            PointPairList pointpair = new PointPairList();
            pointpair = await GenerateHourlyData(collection, meta);
            if (pointpair.Count > 0) AddChartToMaster(master, pointpair, collection, vname,meta,false);

            //save graphic
            // Refigure the axis ranges for the GraphPanes
            zgc.AxisChange();
            // Layout the GraphPanes using a default Pane Layout
            Bitmap b = new Bitmap(2000, 666);
            using (Graphics g = Graphics.FromImage(b))
            {
                master.SetLayout(g, PaneLayout.SingleColumn);
            }
            master.GetImage().Save(@"D:\WORK\piloto\Climate\IDEAM\DailyAnalysisTS_RS\" + filename + ".jpeg", System.Drawing.Imaging.ImageFormat.Jpeg);
        }
        private async Task cityGroupGraphic(StationGroup cityGroup, List<string> weatherCollections, string filename)
        {
            List<City> cities = MapTools.readCities();
            var c = cities.Find(x => x.name == cityGroup.name);
            //start end dates
           
            //set the master pane
            ZedGraphControl zgc = new ZedGraphControl();
            MasterPane master = zgc.MasterPane;
            master.Rect = new RectangleF(0, 0, 2000, 666 * weatherCollections.Count);
            master.PaneList.Clear();
                master.Title.Text = "City: " + c.name +
               " lat: " + Math.Round(c.location[1], 3) + " lon: " + Math.Round(c.location[0], 3) +
               " alt: " + (int)(c.elevation);// + "\nDate range: " + startDate.Year + "_" + startDate.Month + " >> " + endDate.Year + "_" + endDate.Month;
            master.Title.FontSpec = new FontSpec("Arial", 7.0f, Color.Black, false, false, false);
            master.Margin.All = 5;
            master.Legend.IsVisible = false;
            int stationcode = 0;
            string vname = "";
            string source = "";
            int freq = 0;
            foreach (string wc in weatherCollections)
            {
                ////create one scatter for each stationvariable
                if (!wc.Contains("Clean"))
                {
                    string[] parts = wc.Split('_');
                    stationcode = Convert.ToInt32(parts[1]);
                    vname = parts[4];
                    source = parts[2];
                    freq = Convert.ToInt32(parts[5]);

                    VariableMeta meta = AnnualSummary.getVariableMetaFromDB(vname, source, db);
                    PointPairList pointpair = new PointPairList();
                    pointpair = await GenerateHourlyData(wc, meta);
                    if (pointpair.Count > 0) AddChartToMaster(master, pointpair, wc, vname,meta, false);
                }
            }
            //save graphic
            // Refigure the axis ranges for the GraphPanes
            zgc.AxisChange();
            // Layout the GraphPanes using a default Pane Layout
            Bitmap b = new Bitmap(2000, 666 * weatherCollections.Count);
            using (Graphics g = Graphics.FromImage(b))
            {
                master.SetLayout(g, PaneLayout.SingleColumn);
            }
            master.GetImage().Save(@"D:\WORK\piloto\Climate\IDEAM\DailyAnalysis\" + filename + ".jpeg", System.Drawing.Imaging.ImageFormat.Jpeg);
        }
        public void testAggreate(string collname)
        {
            IMongoCollection<RecordMongo> collection = db.GetCollection<RecordMongo>(collname);

            var project =
                BsonDocument.Parse(
                    "{value: '$value',time:'$time',month: {$month: '$time'}}");

            var aggregationDocument =
                collection.Aggregate()
                    .Unwind("value")
                    .Project(project)
                    .Match(BsonDocument.Parse("{$and:[{'month' : {$eq : 1}},{'value':{$lte:100 }},{'value':{$gte: -100}}]}"))
                    .ToList();
            //"{'month' : {$eq : 1}}"
            //"{$and:[{'month' : {$eq : 1}},{'value':{$lte: }},{'value':{$gte: }}]}"
            foreach (var result in aggregationDocument)
            {
                Console.WriteLine(result.ToString());
            }

            Console.ReadLine();
        }
        private async Task<PointPairList> GenerateMonthlyData(string collname,int month, VariableMeta vm)
        {
            //testAggreate(collname);
            PointPairList list = new PointPairList();
           

            IMongoCollection<RecordMongo> collection = db.GetCollection<RecordMongo>(collname);
            var project =
                BsonDocument.Parse(
                    "{value: '$value',time:'$time',month: {$month: '$time'}}");
            try {
                var aggregationDocument =
                    collection.Aggregate()
                        .Unwind("value")
                        .Project(project)
                        .Match(BsonDocument.Parse("{$and:[{'month' : {$eq : " + month.ToString() + "}},{'value':{$lte:" + vm.max.ToString() + " }},{'value':{$gte:" + vm.min.ToString() + "}}]}"))
                        .ToList();
                if (aggregationDocument != null)
                {
                    foreach (var result in aggregationDocument)
                    {
                        //Console.WriteLine(result.ToString());
                        var hour = result.GetValue("time").ToLocalTime().Hour;
                        list.Add(hour, result.GetValue("value").ToDouble());
                    }
                }
            }
            catch(Exception e)
            {
                var error = "errorhere";
            }
            
            return list;
        }
        private async Task<PointPairList> GenerateHourlyData(string collname,  VariableMeta vm)
        {
            PointPairList list = new PointPairList();
            IMongoCollection<RecordMongo> collection = db.GetCollection<RecordMongo>(collname);
            var filter = FilterDefinition<RecordMongo>.Empty;
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
                        //only if the value is in range
                        if (rm.value > vm.min && rm.value < vm.max)
                        {
                            list.Add(rm.time.Hour, rm.value);
                        }
                        
                        
                    }
                }
            }
            return list;
        }
        private void AddChartToMaster(MasterPane master, PointPairList list,string title,string yaxistitle,VariableMeta meta,bool addStats)
        {
            GraphPane pane = new GraphPane();
            // Set the titles
            pane.Title.Text = title;
            pane.XAxis.Title.Text = "hour";
            pane.YAxis.Title.Text = yaxistitle;
            pane.Border.IsVisible = false;
            pane.XAxis.Scale.Max = 24;
            pane.XAxis.Scale.MajorStep = 4.0;
            pane.XAxis.Scale.MinorStep = 1.0;
            pane.YAxis.Scale.Min = meta.min;
            pane.YAxis.Scale.Max = meta.max;
            // Add the curve
            LineItem myCurve = pane.AddCurve(yaxistitle, list, Color.Black, SymbolType.XCross);
            // Don't display the line (This makes a scatter plot)
            myCurve.Line.IsVisible = false;
            // Hide the symbol outline
            myCurve.Symbol.Border.IsVisible = true;
            // Fill the symbol interior with color
            myCurve.Symbol.Size = 2f;
            if(addStats)
            {
                addStatsToPane(list,pane);
            }
            pane.AxisChange();
            master.Add(pane);
        }
        private void addStatsToPane(PointPairList list, GraphPane pane)
        {
            List<List<double>> bucketsHourly = new List<List<double>>();
            for (int h = 0; h < 24; h++) bucketsHourly.Add(new List<double>());
            foreach (PointPair pp in list)
            {
                bucketsHourly[(int)pp.X].Add(pp.Y);
            }
            PointPairList median = new PointPairList();
            PointPairList mode = new PointPairList();
            PointPairList mean = new PointPairList();
            int hour = 0;
            foreach (var hr in bucketsHourly)
            {
                if (hr.Count > 0)
                {
                    median.Add(new PointPair(hour, Measures.Median(hr.ToArray())));
                    mode.Add(new PointPair(hour, Measures.Mode(hr.ToArray())));
                    mean.Add(new PointPair(hour, Measures.Mean(hr.ToArray())));
                }
                hour++;
            }
            LineItem medianCurve = pane.AddCurve("Median", median, Color.Red, SymbolType.Square);
            medianCurve.Line.IsVisible = false;
            medianCurve.Symbol.Border.IsVisible = true;
            medianCurve.Symbol.Fill.Color = Color.Red;
            //medianCurve.Symbol.Fill.Type = FillType.Solid;
            medianCurve.Symbol.Size = 10f;

            LineItem modeCurve = pane.AddCurve("Mode", mode, Color.Blue, SymbolType.TriangleDown);
            modeCurve.Line.IsVisible = false;
            modeCurve.Symbol.Border.IsVisible = true;
            modeCurve.Symbol.Fill.Color = Color.Blue;
            modeCurve.Symbol.Fill.Type = FillType.Solid;
            modeCurve.Symbol.Size = 10f;

            LineItem meanCurve = pane.AddCurve("Mean", mean, Color.Green, SymbolType.Diamond);
            meanCurve.Line.IsVisible = false;
            meanCurve.Symbol.Border.IsVisible = true;
            meanCurve.Symbol.Fill.Color = Color.Green;
            meanCurve.Symbol.Fill.Type = FillType.Solid;
            meanCurve.Symbol.Size = 10f;
        }
        
        private List<string> getStationsColNames(StationGroup cityGroup)
        {
            List<string> collections = MongoTools.collectionNames(db);
            List<string> stationCollections = new List<string>();
            int scode = 0;
            
            foreach (string col in collections)
            {
                if (col[0] == 's')
                {
                    string[] parts = col.Split('_');
                    scode = Convert.ToInt32(parts[1]);
                    
                    foreach (int code in cityGroup.stationcodes)
                    {
                        if (scode == code) stationCollections.Add(col);
                    }
                }
            }
            return stationCollections;
        }
    }
}
