using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Device.Location;

namespace DataETL
{
    class FlexiQuery
    {
        IMongoDatabase db;
        
        List<Station> stations = new List<Station>();
        List<int> activeStationCodes = new List<int>();
        public FlexiQuery()
        {
            db = MongoTools.connect("mongodb://localhost", "climaColombia");
            getActiveStations();
        }
        public void writeToEPW()
        {
            CityYearBuilder cyb = new CityYearBuilder();
            cyb.writeEPW("sumapaz", 3.858849, -74.301720, 3500);
        }
        public void ByDistanceFromLatLong(double[] lonlat,double ele)
        {
            stations = StationGrouping.getAllStationsFromDB(db);
            var siteCoord = new GeoCoordinate(lonlat[1], lonlat[0]);

            double dist = 0;

            double eleDiff = 0;
            
            StationGroup sg = new StationGroup();
            sg.name = "sumapaz";
            foreach (int actScode in activeStationCodes)
            {
                //get the ref station details

                //find within radius altitude
                
                if(stations.Exists(x => x.code == actScode))
                {
                    Station s = stations.Find(x => x.code == actScode);
                    var sCoord = new GeoCoordinate(s.latitude, s.longitude);
                    dist = siteCoord.GetDistanceTo(sCoord);
                    eleDiff = ele - s.elevation;
                    if (dist < 20000)//&& Math.Abs(eleDiff) < 100
                    {
                        sg.stationcodes.Add(s.code);
                    }
                }
                
            }
            CityYearBuilder cyb = new CityYearBuilder();
            cyb.prepOneGroup(sg);
            cyb.makeSynthYear(sg, "medianHour");
        }
        private void getActiveStations()
        {
            List<string> collections = MongoTools.collectionNames(db);
            
            foreach (string collection in collections)
            {
                if (collection[0] == 's')
                {
                    string[] parts = collection.Split('_');
                    int code = Convert.ToInt32(parts[1]);
                    if (!activeStationCodes.Contains(code))
                    {
                        activeStationCodes.Add(code);
                    }
                }
            }
        }
    }
}
