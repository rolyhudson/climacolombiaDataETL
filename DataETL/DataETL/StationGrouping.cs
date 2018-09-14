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
        List<int> activeStationCodes = new List<int>();
        List<Region> regions = new List<Region>();
        List<City> cities = new List<City>();
        List<StationGroup> stationsByRegion = new List<StationGroup>();
        List<StationGroup> stationsByCity = new List<StationGroup>();
        IMongoDatabase db;
        public StationGrouping(bool allideam)
        {
            db = MongoTools.connect("mongodb://localhost", "climaColombia");
            getData();
            if (allideam) makeGroupsALLIDEAM();
            else makeGroups();
            outputJSON();
            storeInMongo();
            writeStationCoords();

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
            getActiveStations();
            
            //getStationsFromDB(db);//this is the annula summary
            //gets the full list of meta data for all stations NOAA and IDEAM
            stations = getAllStationsFromDB(db);
        }
        private void getActiveStations()
        {
            List<string> collections = MongoTools.collectionNames(db);
            foreach(string collection in collections)
            {
                if(collection[0]=='s')
                {
                    string[] parts = collection.Split('_');
                    int code = Convert.ToInt32(parts[1]);
                    if(!activeStationCodes.Contains(code))
                    {
                        activeStationCodes.Add(code);
                    }
                }
            }
        }
        public void makeGroups()
        {
            setCityGroups();
            setRegionalGroups();
            getCityGroups();
            getRegionGroups();
        }
        public void makeGroupsALLIDEAM()
        {
            setCityGroups();
            setRegionalGroups();
            getAllIDEAMCityGroups();
            getAllIDEAMRegionGroups();
        }
        public void outputJSON()
        {
            JSONout.regionsToGEOJSON(regions);
            JSONout.writeGroup(stationsByRegion, @"C:\Users\Admin\Documents\projects\IAPP\piloto\webDev\tools\180707\regionalstations.json", stations,cities);
            JSONout.writeGroup(stationsByCity, @"C:\Users\Admin\Documents\projects\IAPP\piloto\webDev\tools\180707\citygroups.json", stations,cities);
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
        public static List<StationSummary> getStationsFromDB(IMongoDatabase db)
        {
            //record of the stations for which we have data
            var coll = db.GetCollection<StationSummary>("annualStationSummary_2018_9_12_11_25_37");
            var stationsummarys = coll.Find(FilterDefinition<StationSummary>.Empty).ToList();
            return stationsummarys;
        }
        private void writeStationCoords()
        {
            StreamWriter sw = new StreamWriter("writeStationCoords.txt");
            foreach(Station ss in stations)
            {
                //get the ref station details
                Station s = stations.Find(x => x.code == ss.code);
                if (s == null) s = new Station();
                sw.WriteLine(s.name + "," + s.source + "," + s.latitude + "," + s.longitude+ "," +s.elevation);
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
        private void getAllIDEAMCityGroups()
        {
            foreach (StationGroup cg in stationsByCity)
            {
                //point as geocoord lat lon
                var city = cities.Find(x => x.name == cg.name);
                var cityCoord = new GeoCoordinate(city.location[1], city.location[0]);
                double dist = 0;

                double eleDiff = 0;

                foreach (Station s in stations)
                {
                    //get the ref station details
                    
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
        private void getCityGroups()
        {
            foreach (StationGroup cg in stationsByCity)
            {
                //point as geocoord lat lon
                var city = cities.Find(x => x.name == cg.name);
                var cityCoord = new GeoCoordinate(city.location[1], city.location[0]);
                double dist = 0;

                double eleDiff = 0;

                foreach (int code in activeStationCodes)
                {
                    //get the ref station details
                    Station s = stations.Find(x => x.code == code);
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
            foreach (int code in activeStationCodes)
            {
                bool inside = false;
                //get the ref station details
                Station s = stations.Find(x => x.code == code);
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
        private void getAllIDEAMRegionGroups()
        {
            foreach (Station s in stations)
            {
                bool inside = false;
                
                foreach (Region r in regions)
                {
                    double[] lonlat = new double[] { s.longitude, s.latitude };
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
