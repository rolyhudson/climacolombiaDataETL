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
using System.Device.Location;
using System.IO;

namespace DataETL
{
    class StationGrouping
    {
        List<Station> stations = new List<Station>();
        List<StationSummary> stationsummarys = new List<StationSummary>();
        List<Region> regions = new List<Region>();
        List<City> cities = new List<City>();
        List<StationGroup> stationsByRegion = new List<StationGroup>();
        List<StationGroup> stationsByCity = new List<StationGroup>();
        IMongoDatabase db;
        public StationGrouping()
        {
            db = MongoTools.connect("mongodb://localhost", "climaColombia");
            getData();
            makeGroups();
            outputJSON();
            storeInMongo();
            //writeStationCoords();
        }
        private void storeInMongo()
        {
            insertManyRecord("regionGroups", stationsByRegion);
            insertManyRecord("cityGroups", stationsByCity);
        }
        public void getData()
        {
            cities = MapTools.readCities();
            regions = MapTools.readRegions();
            //get the city's region
            getCityRegionName();
            //gets all the stations for which we have data
            getStationsFromDB();
            //gets the full list of meta data for all stations NOAA and IDEAM
            stations = getAllStationsFromDB(db);
        }
        public void makeGroups()
        {
            setCityGroups();
            setRegionalGroups();
            getCityGroups();
            getRegionGroups();
        }
        public void outputJSON()
        {
            JSONout.regionsToGEOJSON(regions);
            JSONout.writeGroup(stationsByRegion, @"D:\WORK\piloto\webDev\tools\180707\regionalstations.json",stations,cities);
            JSONout.writeGroup(stationsByCity, @"D:\WORK\piloto\webDev\tools\180707\citygroups.json",stations,cities);
        }
        public void insertManyRecord(string collectionName,List<StationGroup> groups)
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
        private void getStationsFromDB()
        {
            //record of the stations for which we have data
            var coll = db.GetCollection<StationSummary>("annualStationSummary_2018_7_3_14_6_5");
            stationsummarys = coll.Find(FilterDefinition<StationSummary>.Empty).ToList();
            
        }
        private void writeStationCoords()
        {
            StreamWriter sw = new StreamWriter("writeStationCoords.txt");
            foreach(StationSummary ss in stationsummarys)
            {
                //get the ref station details
                Station s = stations.Find(x => x.code == ss.code);
                if (s == null) s = new Station();
                sw.WriteLine(s.latitude + "," + s.longitude);
            }
            sw.Close();
        }
        public static List<Station> getAllStationsFromDB(IMongoDatabase db)
        {
            //get the collection with summary names
            IMongoCollection<Station> stationMeta = db.GetCollection<Station>("metaStations");
            var filter = FilterDefinition<Station>.Empty;
            
            var stations =  stationMeta.Find(filter).ToList();
            return stations;
        }
        private void setRegionalGroups()
        {
            //set up regional groups
            StationGroup sg = new StationGroup();
            foreach (Region r in regions)
            {
                sg = new StationGroup();
                sg.name = r.name;
                stationsByRegion.Add(sg);
            }
            //extra group for those outside
            sg = new StationGroup();
            sg.name = "outside";
            stationsByRegion.Add(sg);
        }
        private void setCityGroups()
        {
            StationGroup sg = new StationGroup();
            foreach (City c in cities)
            {
                sg = new StationGroup();
                sg.name = c.name;
                stationsByCity.Add(sg);
            }
        }
        private void getCityRegionName()
        {
            foreach (City c in cities)
            {
                foreach (Region r in regions)
                {
                    if (MapTools.isPointInPolygon(c.location, r.vertices))
                    {
                        //add the region group name to city
                        c.regionName = r.name;
                        break;
                    }
                }
            }
        }
        private void getCityGroups()
        {
            foreach (StationGroup cg in stationsByCity)
            {
                //point as geocoord lat lon
                var city = cities.Find(x => x.name == cg.name);
                var cityCoord = new GeoCoordinate(city.location[1], city.location[0]);
                double dist = 0;

                double eleDiff = 0;

                foreach (StationSummary ss in stationsummarys)
                {
                    //get the ref station details
                    Station s = stations.Find(x => x.code == ss.code);
                    if (s == null) s = new Station();
                    //find within radius altitude

                    var sCoord = new GeoCoordinate(s.latitude, s.longitude);
                    dist = cityCoord.GetDistanceTo(sCoord);
                    eleDiff = city.elevation - s.elevation;
                    if (dist < 50000 && Math.Abs(eleDiff) < 100)
                    {
                        cg.stationcodes.Add(s.code);
                    }
                }
            }
        }
        private void getRegionGroups()
        {
            foreach (StationSummary ss in stationsummarys)
            {
                bool inside = false;
                //get the ref station details
                Station s = stations.Find(x => x.code == ss.code);
                if (s == null) s = new Station();
                foreach (Region r in regions)
                {
                    double[] lonlat = new double[] {s.longitude, s.latitude };
                    if (MapTools.isPointInPolygon(lonlat, r.vertices))
                    {
                        inside = true;
                        var group = stationsByRegion.Find(x => x.name == r.name);
                        group.stationcodes.Add(s.code);
                        //break;
                    }
                }
                //for the ones that fall outside
                if (!inside)
                {
                    var groupOut = stationsByRegion.Find(x => x.name == "outside");
                    groupOut.stationcodes.Add(s.code);
                    //break;
                }
            }
        }
    }
    public class StationGroup
    {
        public ObjectId Id { get; set; }
        public string name { get; set; }
        public List<int> stationcodes { get; set; }
        public StationGroup()
        {
            stationcodes = new List<int>();
        }


    }
    
}
