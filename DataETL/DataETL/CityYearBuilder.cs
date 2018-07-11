using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
namespace DataETL
{
    class CityYearBuilder
    {
        IMongoDatabase db;
        List<StationGroup> stationsByCity = new List<StationGroup>();
        List<Station> stations = new List<Station>();
        public CityYearBuilder()
        {
            db = MongoTools.connect("mongodb://localhost", "climaColombia");
        }
        public void averageYear()
        {
            getStationData();
            synthesizeYears();
        }
        private void getStationData()
        {
            //list of station meta data
            stations = StationGrouping.getAllStationsFromDB(db);
            var coll = db.GetCollection<StationGroup>("cityGroups");
            stationsByCity = coll.Find(FilterDefinition<StationGroup>.Empty).ToList();
        }
        private void synthesizeYears()
        {
            foreach(StationGroup cg in stationsByCity)
            {
                if(cg.name== "SANTA FE DE BOGOTÁ")
                {
                    SyntheticYear syntheticyear = new SyntheticYear();
                    foreach(CollectionMongo cm in syntheticyear.variables)
                    {
                        string vname = cm.name;
                        foreach(RecordMongo rm in cm.records)
                        {
                            //loop the station group at this time
                            foreach(int scode in cg.stationcodes)
                            {
                                var r = getACollection(scode, vname);
                            }
                        }
                    }
                   
                }
            }
        }
        private List<RecordMongo> getACollection(int stationcode, string variable)
        {
            //get the collection with summary names
            List<RecordMongo> records = new List<RecordMongo>();
            var allCollections = MongoTools.collectionNames(db);
            string vname = "";
            int scode = 0;
            string source = "";
            int freq = 0;
            int count = 0;
            foreach (string collection in allCollections)
            {
                if (collection[0] == 's')
                {
                    string[] parts = collection.Split('_');
                    scode = Convert.ToInt32(parts[1]);
                    vname = parts[4];
                    source = parts[2];
                    freq = Convert.ToInt32(parts[5]);
                    if (scode == stationcode && vname == variable && freq == 60)
                    {
                        IMongoCollection<RecordMongo> stationData = db.GetCollection<RecordMongo>(collection);
                        var filter = FilterDefinition<RecordMongo>.Empty;
                        records = stationData.Find(filter).ToList();
                        return records;
                    }
                }
            }
            return records;
        }
    }
    class SyntheticYear
    {
        public string name { get; set; }
        public Station info { get; set; }
        public List<CollectionMongo> variables { get; set; }
        private string[] vNames = new string[] { "VV", "DV", "TS", "PR", "NUB", "HR", "RS" };
        public SyntheticYear()
        {
            variables = new List<CollectionMongo>();
            foreach (string vname in vNames) variables.Add(generateVariableYear(vname));
        }
        private CollectionMongo generateVariableYear(string name)
        {
            //one collection per variable
            CollectionMongo cm = new CollectionMongo();
            cm.name = name;
            //loop all hours in 8760 add a new RecordMongo
            int[] daysInMonths = new int[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            int month = 0;
            int year = 2000;
            foreach (int m in daysInMonths)
            {
                month++;
                for (int d = 1; d <= m; d++)
                {
                    for (int h = 0; h < 24; h++)
                    {
                        RecordMongo rm = new RecordMongo();
                        DateTime dt = new DateTime(year, month, d, h, 0, 0);
                        rm.value = -999.9;
                        rm.time = dt;
                        rm.processNote = "ccSyntheticYear";
                        cm.records.Add(rm);
                    }
                }
            }
            return cm;
        }
    }
}
