using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;

namespace DataETL
{
    class CityYearBuilder
    {
        IMongoDatabase db;
        List<StationGroup> stationsByCity = new List<StationGroup>();
        List<Station> stations = new List<Station>();
        
        string city = "SANTA FE DE BOGOTÁ";
        StationGroup cityGroup;
        List<string> stationCollections = new List<string>();
        int averagedCollections;
        int cleanCollections;
        SyntheticYear synthYear;
        List<IMongoCollection<RecordMongo>> stationData = new List<IMongoCollection<RecordMongo>>();
        
        public CityYearBuilder()
        {
            db = MongoTools.connect("mongodb://localhost", "climaColombia");
        }
        public void readSythYearFromDB()
        {
            var coll = db.GetCollection<SyntheticYear>(city+ "TestYearMedian");
            List<SyntheticYear> synthYear = coll.Find(FilterDefinition<SyntheticYear>.Empty).ToList();
            synthYear[0].name = city;
            synthYear[0].info = AnnualSummary.getStationFromMongo(21206960, db);
            EPWWriter epww = new EPWWriter(synthYear[0], @"C:\Users\Admin\Documents\projects\IAPP\piloto\Climate");
        }

        public async Task averageYear()
        {
            getStationData();
            synthYear = new SyntheticYear();
            getTheStationData();
            //Task  f = Task.Run(() => { averageTheVariables(); });

            //f.Wait();
            await averageTheVariables();
            insertSytheticYear(city+"TestYearMedian");
        }
        private async Task averageTheVariables()
        {
            foreach (CollectionMongo c in synthYear.variables)
            {
                //await averageOneVariableList(c.name);
                await selectDayOfYear(c.name);
            }
        }
        public void insertSytheticYear(string collectionName)
        {
            var collection = db.GetCollection<SyntheticYear>(collectionName);
            
            var f = new Task(() => { collection.InsertOneAsync(synthYear); });
            f.Start();

        }
        private void getFirstLastYear(IMongoCollection<RecordMongo> collection,ref int startYr,ref int endYr)
        {
            
            var filter = FilterDefinition<RecordMongo>.Empty;
            using (IAsyncCursor<RecordMongo> cursor = collection.Find(filter).Limit(1).Sort("{time: 1}").ToCursor())
            {
                while (cursor.MoveNext())
                {
                    IEnumerable<RecordMongo> documents = cursor.Current;
                    //insert into the station collection
                    foreach (RecordMongo rm in documents)
                    {
                        startYr = rm.time.Year;
                    }
                }
            }
            using (IAsyncCursor<RecordMongo> cursor = collection.Find(filter).Limit(1).Sort("{time: -1}").ToCursor())
            {
                while (cursor.MoveNext())
                {
                    IEnumerable<RecordMongo> documents = cursor.Current;
                    //insert into the station collection
                    foreach (RecordMongo rm in documents)
                    {
                        endYr = rm.time.Year;
                    }
                }
            }
            
            
        }
        
        private async Task averageOneVariableList(string vcode)
        {
            var v = synthYear.variables.Find(x => x.name == vcode);
            var builder = Builders<RecordMongo>.Filter;
            string[] pieces;
            
            int hourofsyntheticyear = 0;
            
            DateTime local = new DateTime();
            DateTime universal = new DateTime();
            foreach (RecordMongo r in v.records)
            {
                //this is the time we need to fill
                //need to filter for month day and hour
                int m = r.time.Month;
                int d = r.time.Day;
                int h = r.time.Hour;
                hourofsyntheticyear++;
                double value = 0;
                int foundValues = 0;
                List<double> valuesForHour = new List<double>();
                foreach (IMongoCollection<RecordMongo> sd in stationData)
                {
                    //only if the vcode matches
                    pieces = sd.CollectionNamespace.CollectionName.Split('_');
                    if (pieces[4] == vcode)
                    {
                        //loop all years 2000to2018
                        int startYr = 0;
                        int endYr = 0;
                        getFirstLastYear(sd, ref startYr, ref endYr);
                        for (int y = startYr; y < endYr; y++)
                        {

                            local = new DateTime(y, m, d, h, 0, 0);
                            universal = local.ToUniversalTime();
                            var filter = builder.Eq("time", universal);
                            //some collections have duplicate timestamps!
                            
                            using (IAsyncCursor<RecordMongo> cursor = await sd.FindAsync(filter))
                            {
                                while (await cursor.MoveNextAsync())
                                {
                                    IEnumerable<RecordMongo> documents = cursor.Current;
                                    //insert into the station collection
                                    foreach (RecordMongo sdrm in documents)
                                    {
                                        value = sdrm.value;
                                        if (vcode == "HR" && sdrm.value <= 1) value = value * 100;
                                        if (vcode == "NUB")
                                        {
                                            if (value == 9) value = 10;
                                            value = (int)(value / 8.0 * 10);
                                        }
                                        valuesForHour.Add(value);
                                        foundValues++;
                                    }
                                }
                            }
                        }
                    }
                }
                if (foundValues != 0)
                {
                    //r.value = Accord.Statistics.Measures.Mean(valuesForHour.ToArray());
                    r.value = Accord.Statistics.Measures.Median(valuesForHour.ToArray());
                    //randomly choose 1
                    //{
                    //    int total = valuesForHour.Count;
                    //    Random rand = new Random();
                    //    r.value = valuesForHour[rand.Next(0, total)];
                    //}
                   
                }
            }
        }
        private async Task selectDayOfYear(string vcode)
        {
            var v = synthYear.variables.Find(x => x.name == vcode);
            var builder = Builders<RecordMongo>.Filter;
            string[] pieces;

            int hourofsyntheticyear = 0;

            DateTime local = new DateTime();
            DateTime universal = new DateTime();
            foreach (RecordMongo r in v.records)
            {
                //this is the time we need to fill
                //need to filter for month day and hour
                int m = r.time.Month;
                int d = r.time.Day;
                int h = r.time.Hour;
                hourofsyntheticyear++;
                double value = 0;
                int foundValues = 0;
                List<double> valuesForHour = new List<double>();
                foreach (IMongoCollection<RecordMongo> sd in stationData)
                {
                    //only if the vcode matches
                    pieces = sd.CollectionNamespace.CollectionName.Split('_');
                    if (pieces[4] == vcode)
                    {
                        for (int doy = 1; doy < 366; doy++)
                        {
                            var project =
                                BsonDocument.Parse(
                                    "{value: '$value',time:'$time',dayOfYear: {$dayOfYear: '$time'},year: {$year: '$time'}}");
                            try
                            {
                                var aggregationDocument =
                                    sd.Aggregate()
                                        .Unwind("value")
                                        .Project(project)
                                        .Match(BsonDocument.Parse("{'dayOfYear' : {$eq : " + doy.ToString() + "}}"))
                                        .ToList();

                                IEnumerable<IGrouping<int, BsonDocument>> query = aggregationDocument.GroupBy(
                                    doc => doc.GetValue("year").ToInt32(),
                                    doc => doc);

                                foreach (IGrouping<int, BsonDocument> yearDayGroup in query)
                                {
                                    var year = yearDayGroup.Key;
                                    var hours = yearDayGroup.Count();
                                    //one group per day per year count should be 24
                                    List<BsonDocument> dayValues = new List<BsonDocument>();
                                    foreach (BsonDocument name in yearDayGroup)
                                    {
                                        dayValues.Add(name);
                                    }
                                    //    
                                }

                            }
                            catch (Exception e)
                            {
                                var error = "errorhere";
                            }
                        }
                    }
                }
                if (foundValues != 0)
                {
                    //r.value = Accord.Statistics.Measures.Mean(valuesForHour.ToArray());
                    r.value = Accord.Statistics.Measures.Median(valuesForHour.ToArray());
                    //randomly choose 1
                    //{
                    //    int total = valuesForHour.Count;
                    //    Random rand = new Random();
                    //    r.value = valuesForHour[rand.Next(0, total)];
                    //}

                }
            }
        }
        //private async Task<PointPairList> GenerateMonthlyData(string collname, int month, VariableMeta vm)
        //{
        //    //testAggreate(collname);
        //    PointPairList list = new PointPairList();


        //    IMongoCollection<RecordMongo> collection = db.GetCollection<RecordMongo>(collname);
        //    var project =
        //        BsonDocument.Parse(
        //            "{value: '$value',time:'$time',month: {$month: '$time'}}");
        //    try
        //    {
        //        var aggregationDocument =
        //            collection.Aggregate()
        //                .Unwind("value")
        //                .Project(project)
        //                .Match(BsonDocument.Parse("{$and:[{'month' : {$eq : " + month.ToString() + "}},{'value':{$lte:" + vm.max.ToString() + " }},{'value':{$gte:" + vm.min.ToString() + "}}]}"))
        //                .ToList();
        //        if (aggregationDocument != null)
        //        {
        //            foreach (var result in aggregationDocument)
        //            {
        //                //Console.WriteLine(result.ToString());
        //                var hour = result.GetValue("time").ToLocalTime().Hour;
        //                list.Add(hour, result.GetValue("value").ToDouble());
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        var error = "errorhere";
        //    }

        //    return list;
        //}
        private async Task getTheStationData()
        {
            foreach (string c in stationCollections)
            {
                if(c.Contains("Clean"))
                {
                    stationData.Add(db.GetCollection<RecordMongo>(c));
                    //stationDataLists.Add(await db.GetCollection<RecordMongo>(c).Find(FilterDefinition<RecordMongo>.Empty).ToListAsync());
                }
            }
        }
        private void getStationData()
        {
            //list of station meta data
            stations = StationGrouping.getAllStationsFromDB(db);
            var coll = db.GetCollection<StationGroup>("cityGroups");
            stationsByCity = coll.Find(FilterDefinition<StationGroup>.Empty).ToList();
            cityGroup = stationsByCity.Find(x => x.name == city);
            getStationsColNames();
            //Task t1 = Task.Run(() => indexCityCollections());
            ////add the processed data to mongo
            //t1.Wait();
         
            //averageTenMinute();
            //clean60Minutes();
            //index60Minutes();
        }
        private void clean60Minutes()
        {
            CleanRecords cr = new CleanRecords();
            string newname = "";
            List<string> cleanStations = new List<string>();
            foreach (string c in stationCollections)
            {
                string[] parts = c.Split('_');
                int freq = Convert.ToInt32(parts[5]);
                string source = parts[2];
                if (freq == 60&&!source.Contains("Clean"))
                {

                    //store the collection to clean
                    cleanStations.Add(c);
                }
            }
            foreach (string c in cleanStations)
            {
              
                cr.cleanSingle(c);
                cleanCollections++;
                //store the clean collection name
                stationCollections.Add(cr.convertNameToClean(c));
            }
            
        }
        private void indexCityCollections()
        {
            IndexStationVariableCollections isvc = new IndexStationVariableCollections();
            foreach (string c in stationCollections)
            {

                isvc.createCollectionIndex(c);

            }
        }
        private void index60Minutes()
        {
            IndexStationVariableCollections isvc = new IndexStationVariableCollections();
            foreach (string c in stationCollections)
            {
                if (c.Contains("Clean")) isvc.createCollectionIndex(c);

            }
        }

        private void averageTenMinute()
        {
            TenMinuteConversion tmc = new TenMinuteConversion();
            
            List<string> tenminStations = new List<string>();
            int freq = 0;
            foreach(string c in stationCollections)
            {
                try {
                    string[] parts = c.Split('_');
                    freq = Convert.ToInt32(parts[5]);
                }
                catch(Exception e)
                {
                    var wtf = 0;
                }
                if(freq==10)
                {
                    //store the collection to average
                    tenminStations.Add(c);
                }
            }
            foreach(string c in tenminStations)
            {
               
                tmc.convertSingleCollection(c);
                averagedCollections++;
                //store the avergaed collection name
                stationCollections.Add(tmc.convertNameTo60min(c));
            }
           
        }
        private void getStationsColNames()
        {
            List<string> collections = MongoTools.collectionNames(db);
            string vname = "";
            int scode = 0;
            string source = "";
            int freq = 0;
            foreach (string col in collections)
            {
                if (col[0] == 's')
                {
                    string[] parts = col.Split('_');
                    try {
                        scode = Convert.ToInt32(parts[1]);
                        vname = parts[4];
                        source = parts[2];
                        freq = Convert.ToInt32(parts[5]);
                    }
                    catch(Exception e)
                    {
                        var wft = 0;
                    }
                    foreach (int code in cityGroup.stationcodes)
                    {
                        if (scode == code)
                        {
                            stationCollections.Add(col);
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
        public ObjectId Id { get; set; }
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
