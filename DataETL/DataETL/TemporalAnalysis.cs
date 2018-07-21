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

namespace DataETL
{
    
    class TemporalAnalysis
    {
        IMongoDatabase db;
        List<Station> stations = new List<Station>();
        List<StationGroup> stationsByCity = new List<StationGroup>();
        
        public TemporalAnalysis()
        {
            db = MongoTools.connect("mongodb://localhost", "climaColombia");
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
                    var wcss = splitList(weatherCollections, 20);
                    int count = 0;
                    foreach(List<string> wcs in wcss)
                    {
                        cityGroupGraphic(cityGroup, wcs, cityGroup.name+"p_"+count);
                        count++;
                    }
                }
                else
                {
                    cityGroupGraphic(cityGroup, weatherCollections, cityGroup.name);
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
                    if (pointpair.Count > 0) AddChartToMaster(master, pointpair, wc, vname);
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
        private async Task<PointPairList> GenerateHourlyData(string collname, VariableMeta vm)
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
        private void AddChartToMaster(MasterPane master, PointPairList list,string title,string yaxistitle)
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
            // Add the curve
            LineItem myCurve = pane.AddCurve(yaxistitle, list, Color.Black, SymbolType.XCross);
            // Don't display the line (This makes a scatter plot)
            myCurve.Line.IsVisible = false;
            // Hide the symbol outline
            myCurve.Symbol.Border.IsVisible = true;
            // Fill the symbol interior with color
            myCurve.Symbol.Size = 2f;

            // Fill the background of the chart rect and pane
            //pane.Chart.Fill = new Fill(Color.White, Color.LightGoldenrodYellow, 45.0f);
            //pane.Fill = new Fill(Color.White, Color.SlateGray, 45.0f);

            pane.AxisChange();
            master.Add(pane);
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
