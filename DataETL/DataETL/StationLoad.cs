using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Device.Location;
using MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace DataETL
{
    class StationLoad
    {
        List<Station> stations = new List<Station>();
        IMongoDatabase db;
        public StationLoad()
        {
            getIDEAMstatioInfo();
            getNOAAstationInfo();
            findClosestOther();
            dumpText();
            loadToMongo();
        }
        private void dumpText()
        {
            StreamWriter sw = new StreamWriter("C:\\Users\\Admin\\Documents\\projects\\IAPP\\piloto\\Climate\\stationsCombined.csv");
            foreach(Station s in stations)
            {
                sw.WriteLine(s.source + "," + s.code.ToString());
            }
            sw.Close();
        }
        private void loadToMongo()
        {
            db = MongoTools.connect("mongodb://localhost", "climaColombia");
            insertManyRecord();
        }
        public void insertManyRecord()
        {
            var collection = db.GetCollection<Station>("metaStations");
            var listOfDocuments = new List<Station>();
            var limitAtOnce = 1000;
            var current = 0;
            
            foreach(Station s in stations)
            {
                listOfDocuments.Add(s);
                if (++current == limitAtOnce)
                {
                    current = 0;
                    var listToInsert = listOfDocuments;
                    var t = new Task(() => { collection.InsertManyAsync(listToInsert); });
                    t.Start();
                    listOfDocuments = new List<Station>();
                }
            }
            var f = new Task(() => { collection.InsertManyAsync(listOfDocuments); });
            f.Start();
            
        }
        private void findClosestOther()
        {
            string othersource = "";
            foreach(Station s in stations)
            {
                if(s.source=="IDEAM") othersource = "NOAA";
                else othersource = "IDEAM";
                var thisCoord = new GeoCoordinate(s.latitude, s.longitude);
                double dist = 0;
                double mindist = 100000000;
                int otherCode = 0;
                foreach (Station os in stations)
                {
                    if (os.source == othersource)
                    {
                        var otherCoord = new GeoCoordinate(os.latitude, os.longitude);
                        dist = thisCoord.GetDistanceTo(otherCoord);
                        if (dist < mindist)
                        {
                            mindist = dist;
                            otherCode = os.code;
                        }
                    }
                }
                if (mindist < 20000)
                {
                    s.closestIDEAM_NOAA = otherCode;
                }
                else
                {
                    s.closestIDEAM_NOAA = -1;
                }
            }
        }
        public void getIDEAMstatioInfo()
        {
            StreamReader sr = new StreamReader("C:\\Users\\Admin\\Documents\\projects\\IAPP\\piloto\\Climate\\IDEAM\\Estaciones_del_IDEAM.csv");
            string line = sr.ReadLine();
            line = sr.ReadLine();
            while (line != null)
            {
                string[] chunks = line.Split(',');
                Station sm = new Station();
                sm.source = "IDEAM";
                sm.country = "Colombia";
                sm.code = Convert.ToInt32(chunks[1]);
                sm.name = chunks[2];
                
                
                sm.latitude = Convert.ToDouble(chunks[9]);
                sm.longitude = Convert.ToDouble(chunks[10]);
                if (chunks[11].Contains("."))
                {
                    String[] p = chunks[11].Split('.');
                    while(p[1].Length!=3)
                    {
                        p[1] += "0";
                    }
                    sm.elevation = Convert.ToDouble(p[0] + p[1]);
                }
                else sm.elevation = Convert.ToDouble(chunks[11].Replace(".", string.Empty));
                stations.Add(sm);
                line = sr.ReadLine();

            }
            sr.Close();
        }
        public void getNOAAstationInfo()
        {
            string[] files = Directory.GetFiles(@"C:\Users\Admin\Documents\projects\IAPP\piloto\Climate\NOAA\data\");
            foreach (string file in files)
            {
                if (file.Contains("stn"))
                {
                    StreamReader sr = new StreamReader(file);
                    string line = sr.ReadLine();
                    //second line is ----- ------ marking the fields
                    line = sr.ReadLine();
                    string[] bits = line.Split(' ');

                    //get the next line first station
                    line = sr.ReadLine();
                    int charsum = 0;
                    string info = "";
                    Station ns = new Station();
                    while (line != null)
                    {
                        charsum = 0;
                        ns = new Station();
                        ns.source = "NOAA";
                        for (int i = 0; i < bits.Length; i++)
                        {

                            info = line.Substring(charsum, bits[i].Length);
                            info = info.Trim();
                            switch (i)
                            {
                                case 0:
                                    //code
                                    string[] parts = info.Split(' ');
                                    ns.code = Convert.ToInt32(parts[0]);
                                    break;
                                case 1:
                                    //name
                                    ns.name = info;
                                    break;
                                case 2:
                                    //country
                                    ns.country = info;
                                    break;
                                case 3:
                                    //state
                                    break;
                                case 4:
                                    //lat
                                    ns.latitude = Convert.ToDouble(info);
                                    break;
                                case 5:
                                    //lon
                                    ns.longitude = Convert.ToDouble(info);
                                    break;
                                case 6:
                                    //ele
                                    ns.elevation = Convert.ToDouble(info);
                                    break;
                            }
                            charsum += bits[i].Length + 1;
                        }
                        stations.Add(ns);
                        line = sr.ReadLine();
                    }

                    sr.Close();

                }
            }
           
        }
    }
    public class Station
    {
        public ObjectId Id { get; set; }
        public string name { get; set; }
        public int code { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public double elevation { get; set; }
        public int closestIDEAM_NOAA { get; set; }
        public string source { get; set; }
        public string country { get; set; }

    }
}
