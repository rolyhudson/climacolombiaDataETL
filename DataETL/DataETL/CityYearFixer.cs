using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataETL
{
    class CityYearFixer
    {
        IMongoDatabase db;
        String logFile;
        List<Station> stations = new List<Station>();
        List<NeededData> neededData = new List<NeededData>();
        List<City> cities = new List<City>();
        List<Region> regions = new List<Region>();
        List<StationGroup> cityRegionGroup = new List<StationGroup>();
        List<StationGroup> allRegionGroups = new List<StationGroup>();
        
        public CityYearFixer()
        {
            

        }
        public void setup()
        {
            neededData = readRequiredData();
            db = MongoTools.connect("mongodb://localhost", "climaColombia");
            defineCityRegionGroups();
        }
        private void defineCityRegionGroups()
        {
            var coll = db.GetCollection<StationGroup>("regionGroups");
            allRegionGroups = coll.Find(FilterDefinition<StationGroup>.Empty).ToList();
            cities = MapTools.readCities();
            regions = MapTools.readRegions();
            
            stations = StationGrouping.getAllStationsFromDB(db);
            setCityRegionGroups();
            //insertManyRecord("cityRegionGroups", cityRegionGroup);

        }
        public void printCityRegionGroups()
        {
            db = MongoTools.connect("mongodb://localhost", "climaColombia");
            var coll = db.GetCollection<StationGroup>("cityRegionGroups");
            allRegionGroups = coll.Find(FilterDefinition<StationGroup>.Empty).ToList();
            StreamWriter sw = new StreamWriter("regiongroups.csv");
            foreach(StationGroup sg in allRegionGroups)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(sg.name + ",");
                foreach(int code in sg.stationcodes)
                {
                    sb.Append(code + ",");
                }
                sw.WriteLine(sb.ToString());
            }
            sw.Close();
            cities = MapTools.readCities();
            

            stations = StationGrouping.getAllStationsFromDB(db);
            JSONout.writeGroup(allRegionGroups, @"C:\Users\Admin\Documents\projects\IAPP\climaColombiaOrg\tools\cityGroups\cityregiongroups.json", stations, cities);
        }
        public void insertManyRecord(string collectionName, List<StationGroup> groups)
        {
            var collection = db.GetCollection<StationGroup>(collectionName);
            var listOfDocuments = new List<StationGroup>();
            var limitAtOnce = 2;
            var current = 0;

            foreach (StationGroup s in groups)
            {
                listOfDocuments.Add(s);
                if (++current == limitAtOnce)
                {
                    current = 0;
                    var listToInsert = listOfDocuments;
                    var t = new Task(() => { collection.InsertManyAsync(listToInsert); });
                    t.Start();
                    listOfDocuments = new List<StationGroup>();
                }
            }
            var f = new Task(() => { collection.InsertManyAsync(listOfDocuments); });
            f.Start();

        }
        private void setCityRegionGroups()
        {
            StationGroup sg = new StationGroup();
            foreach (City c in cities)
            {
                if (neededData.Exists(n => n.name == c.name)){
                    c.regionName = findRegion(c);
                    sg = new StationGroup();
                    sg.name = c.name;
                    cityRegionGroup.Add(sg);
                    StationGroup regionStations = allRegionGroups.Find(rg => rg.name == c.regionName);
                    foreach(int scode in regionStations.stationcodes)
                    {
                        Station s = stations.Find(p => p.code == scode);
                        if(Math.Abs(s.elevation-c.elevation)<100){
                            sg.stationcodes.Add(scode);
                        }
                    }
                }
                
            }
        }
        private string findRegion(City c)
        {
            string region = "undefined";
            foreach (Region r in regions)
            {
                if (MapTools.isPointInPolygon(c.location, r.vertices))
                {
                    //add the region group name to city
                    region = r.name;
                    break;
                }

            }
            return region;
        }
        public List<NeededData> readRequiredData()
        {
            List<NeededData> neededData = new List<NeededData>();
            StreamReader sr = new StreamReader(@"C:\Users\Admin\Documents\projects\IAPP\piloto\Climate\ClimateDataETL\needed.csv");
            string line = sr.ReadLine();
            while (line != null)
            {
                string[] parts = line.Split(',');
                var nd = new NeededData();
                nd.name = parts[0];
                for(int i = 1; i < parts.Length; i++)
                {
                    if(parts[i]!="")nd.reqVariables.Add(parts[i]);
                }
                neededData.Add(nd);
                line = sr.ReadLine();
            }
            sr.Close();
            return neededData;
        }
    }
    class NeededData
    {
       public  string name { get; set; }
        public List<string> reqVariables = new List<string>();
    }
}
