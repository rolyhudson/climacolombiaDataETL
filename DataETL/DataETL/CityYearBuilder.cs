using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using Accord.Statistics.Distributions.Univariate;

namespace DataETL
{
    class CityYearBuilder
    {
        IMongoDatabase db;
        
        List<Station> stations = new List<Station>();

        public CityYearBuilder()
        {
            db = MongoTools.connect("mongodb://localhost", "climaColombia");
        }
        public void readSythYearFromDB()
        {
            stations = StationGrouping.getAllStationsFromDB(db);
            var coll = db.GetCollection<StationGroup>("cityGroups");
            var allCityGroups = coll.Find(FilterDefinition<StationGroup>.Empty).ToList();
            foreach (StationGroup sg in allCityGroups)
            {
                var city = sg.name;
                //city == "SANTA FE DE BOGOTÁ" || city == "MEDELLÍN" || averaged
                if (city == "CARTAGENA")
                {
                    var collection = db.GetCollection<SyntheticYear>(city + "cdfDaySelector");
                    List<SyntheticYear> synthYear = collection.Find(FilterDefinition<SyntheticYear>.Empty).ToList();
                    synthYear[0].name += "_cdfDay";
                    synthYear[0].info = AnnualSummary.getStationFromMongo(21206960, db);
                    EPWWriter epww = new EPWWriter(synthYear[0], @"C:\Users\Admin\Documents\projects\IAPP\piloto\Climate");
                }

            }
            
        }
        
        public async Task syntheticYearDataPrep()
        {
            stations = StationGrouping.getAllStationsFromDB(db);
            var coll = db.GetCollection<StationGroup>("cityGroups");
            var allCityGroups = coll.Find(FilterDefinition<StationGroup>.Empty).ToList();
            foreach (StationGroup sg in allCityGroups)
            {
                var city = sg.name;
                //city == "SANTA FE DE BOGOTÁ" || city == "MEDELLÍN" || averaged
                if (city == "SANTA FE DE BOGOTÁ" || city == "MEDELLÍN" || city == "CARTAGENA")
                {
                    var cityGroup = allCityGroups.Find(x => x.name == city);
                    var stationCollNames = getStationsColNames(cityGroup);
                    index60Minutes(stationCollNames);
                    averageTenMinute(ref stationCollNames);
                    index60Minutes(stationCollNames);
                }
                
            }
        }
        public async Task syntheticYearBatch()
        {
            stations = StationGrouping.getAllStationsFromDB(db);
            var coll = db.GetCollection<StationGroup>("cityGroups");
            var allCityGroups = coll.Find(FilterDefinition<StationGroup>.Empty).ToList();
            foreach (StationGroup sg in allCityGroups)
            {
                var city = sg.name;
                if (city == "CARTAGENA" )
                {
                    var cityGroup = allCityGroups.Find(x => x.name == city);
                    var stationCollNames = getStationsColNames(cityGroup);
                    var stationData = getTheStationData(stationCollNames);
                    SyntheticYear synthYear = new SyntheticYear();
                    synthYear.name = city;
                    await getDaysForVariables(synthYear, stationData);
                    
                    holeFiller(holeFinder(ref synthYear), ref synthYear);
                    insertSytheticYear(city + "cdfDaySelector", synthYear);
                }
            }
        }
        private void nightRadiation(ref CollectionMongo radvariables)
        {
            foreach (RecordMongo rm in radvariables.records)
            {
                if(rm.time.Hour<6||rm.time.Hour>6)
                {
                    rm.value = 0;
                }
            }
        }
        private List<Hole> holeFinder(ref SyntheticYear synthYear)
        {
            //fix night radiaiton
            var rs = synthYear.variables.Find(x => x.name == "RS");
            nightRadiation(ref rs);
            List<Hole> allHoles = new List<Hole>();
            foreach (CollectionMongo c in synthYear.variables)
            {
                
                bool newhole = true;
                Hole h = new Hole();
                DateTime prevDT = new DateTime();
                foreach (RecordMongo rm in c.records)
                {
                    
                    if (rm.value==-999.9)
                    {
                        if (newhole)
                        {
                            //found first of new hole
                            h = new Hole();
                            h.vcode = c.name;
                            h.setHoleStart(rm.time);
                            newhole = false;
                        }
                    }
                    if(rm.value!=-999.9&&!newhole)
                    {
                        //end of hole
                        h.setHoleEnd(prevDT);
                        newhole = true;
                        allHoles.Add(h);
                    }
                    prevDT = rm.time;
                }
                //hole at end of year
                if(!newhole)
                {
                    h.setHoleEnd(prevDT);
                    newhole = true;
                    allHoles.Add(h);
                }

            }
            return allHoles;
        }
        private void holeFiller(List<Hole> allHoles, ref SyntheticYear synthYear)
        {
            DateTime start = new DateTime();
            DateTime end = new DateTime();
            DateTime current = new DateTime();
            foreach(Hole h in allHoles)
            {
                TimeSpan holesize = h.getHoleEnd() - h.getHoleStart();
                if(holesize.Hours<5)
                {
                    start = h.getHoleStart().AddHours(-1);
                    if (h.getHoleEnd().Month == 12 && h.getHoleEnd().Day == 31 && h.getHoleEnd().Hour == 23)
                    {
                        //hole at final hour
                        end = new DateTime(start.Year, 1, 1, 0, 0, 0);
                    }
                    else { end = h.getHoleEnd().AddHours(1); }
                    
                    //interpolation
                    double v1 = synthYear.variables.Find(x => x.name == h.vcode).records.Find(r => r.time == start).value;
                    double v2 = synthYear.variables.Find(x => x.name == h.vcode).records.Find(r => r.time == end).value;
                    double range = v2 - v1;
                    double inc = range / (holesize.Hours + 2);
                    for (int i = 1;i<=holesize.Hours+1;i++)
                    {
                        current = start.AddHours(i);
                        try
                        {
                            var tofill = synthYear.variables.Find(x => x.name == h.vcode).records.Find(r => r.time == current);
                            tofill.value = v1 + (i * inc);
                        }
                        catch(Exception e)
                        {

                        }
                    }
                }
                else
                {
                    //select from neighour hood of days in week
                }
            }
        }
        private async Task getDaysForVariables(SyntheticYear synthYear, List<IMongoCollection<RecordMongo>> stationData)
        {
            Task.WhenAll(synthYear.variables.Select(c => selectDayOfYearCDF(c.name, synthYear, stationData)));

        }
        
        public void insertSytheticYear(string collectionName,SyntheticYear synthYear)
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
        
        private async Task averageOneVariableList(string vcode, SyntheticYear synthYear, List<IMongoCollection<RecordMongo>> stationData)
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
        private async Task selectDayOfYearCDF(string vcode,SyntheticYear synthYear, List<IMongoCollection<RecordMongo>> stationData)
        {
            var v = synthYear.variables.Find(x => x.name == vcode);
            var builder = Builders<RecordMongo>.Filter;
            string[] pieces;
            VariableMeta vm;
            List<int> missingDays = new List<int>();
            List<List<RecordMongo>> possDayValues;
            for (int doy = 1; doy < 366; doy++)
            {
                //no leap year in epw
                if(doy==67)
                {

                    var br = 0;
                }
                //each day will have several canadidate days sourced from each collection of same variable
                possDayValues = new List<List<RecordMongo>>();
                foreach (IMongoCollection<RecordMongo> sd in stationData)
                {
                    
                    //only if the vcode matches
                    pieces = sd.CollectionNamespace.CollectionName.Split('_');
                    if (pieces[4] == vcode)
                    {
                        string source = pieces[2];
                        if (source.Contains("NOAA")) source = "NOAA";
                        else source = "IDEAM";
                        vm = AnnualSummary.getVariableMetaFromDB(vcode, source, db);
                        var project =
                            BsonDocument.Parse(
                                "{value: '$value',time:'$time',dayOfYear: {$dayOfYear: '$time'},year: {$year: '$time'},minute: {$minute: '$time'}}");
                        try
                        {
                            //.Match(BsonDocument.Parse("{'dayOfYear' : {$eq : " + doy.ToString() + "}}"))
                            var aggregationDocument =
                                sd.Aggregate()
                                    .Unwind("value")
                                    .Project(project)
                                    .Match(BsonDocument.Parse("{$and:[" +
                                    "{'dayOfYear' : {$eq : " + doy.ToString() + "}}" +
                                    "{'minute':{$eq:0 }}" +
                                    ",{'value':{$lte:" + vm.max.ToString() + " }}" +
                                    ",{'value':{$gte:" + vm.min.ToString() + "}}]}"))
                                    .ToList();

                            IEnumerable<IGrouping<int, BsonDocument>> query = aggregationDocument.GroupBy(
                                doc => doc.GetValue("year").ToInt32(),
                                doc => doc);
                            if(vcode=="NUB")
                            {
                                var b = 0;
                            }

                            foreach (IGrouping<int, BsonDocument> yearDayGroup in query)
                            {
                                var year = yearDayGroup.Key;
                                var hours = yearDayGroup.Count();
                                //one group per day per year count should be 24
                                //but many noaa data are sometimes day time only 6-6 12 readings
                                if (hours >=12)
                                {
                                    List<RecordMongo> dayValues = new List<RecordMongo>();
                                    foreach (BsonDocument name in yearDayGroup)
                                    {
                                        RecordMongo rm = new RecordMongo();
                                        
                                        double value = name.GetValue("value").ToDouble();
                                        //check nub and HRs are in the right range
                                        if (vcode == "HR" && value <= 1)
                                        {
                                            value = value * 100;
                                        }
                                        if (vcode == "NUB")
                                        {
                                            //noaa's cloud is oktas
                                            if (value == 9) value = 10;
                                            else { value = (int)(value / 8.0 * 10); }
                                        }
                                        rm.value = value;
                                        rm.time = name.GetValue("time").ToLocalTime();
                                        dayValues.Add(rm);
                                    }
                                    possDayValues.Add(dayValues);
                                }
                            }

                        }
                        catch (Exception e)
                        {
                            var error = "errorhere";
                        }

                    }
                }
                if (possDayValues.Count > 0)
                {
                    List<RecordMongo> dayToInsert = typicalDay(possDayValues,vcode);
                    addValuesToSynthYear(dayToInsert,ref v, doy, vcode);
                }
                else
                {
                    //no possible days found empty day
                    missingDays.Add(doy);
                }
            }
            if(missingDays.Count>0)
            {
                fillMissingDays(missingDays);
            }
            //fill missing days

        }
        private void fillMissingDays(List<int> missingDays)
        {
            //get 3 days before and 3 after
            foreach (int day in missingDays)
            {
                for (int i = day - 3; i <= day + 3; i++)
                {
                    if (i == day) continue;

                }
            }
        }
        
        private List<RecordMongo> typicalDay(List<List<RecordMongo>> possDayValues,string vcode)
        {
            List<double> longTermValues = new List<double>();
            //list of all candidate days cdfs
            List<EmpiricalDistribution> dayCDFS = new List<EmpiricalDistribution>();
            foreach (List<RecordMongo> day in possDayValues)
            {
                List<double> dayValues = new List<double>();
                foreach (RecordMongo rm in day)
                {
                    if (rm.value != -999.9)
                    {
                        //only actual values in the cdfs
                        longTermValues.Add(rm.value);
                        dayValues.Add(rm.value);
                    }
                }
                dayCDFS.Add(new EmpiricalDistribution(dayValues.ToArray()));
            }
            //longterm cdf all days found
            EmpiricalDistribution longterm = new EmpiricalDistribution(longTermValues.ToArray());
            List<double> finkelSch = new List<double>();
            var range = longterm.GetRange(0.9);
            double inc = (range.Max - range.Min) / 20;

            foreach (EmpiricalDistribution candDay in dayCDFS)
            {
                double sample = range.Min;
                double fs = 0;
                while (sample <= range.Max)
                {
                    fs += Math.Abs(candDay.DistributionFunction(sample) - longterm.DistributionFunction(sample));
                    sample += inc;
                }
                //24 is the n values per day
                finkelSch.Add(fs / 24);
            }
            int minindex = finkelSch.IndexOf(finkelSch.Min());
            List<RecordMongo> selectedday = possDayValues[minindex];
            return selectedday;
        }
        private void addValuesToSynthYear(List<RecordMongo> candidateDay,ref CollectionMongo variableRecords,int doy,string vcode)
        {
            try
            {
                //assign to synthetic year
                List<RecordMongo> synthDay = variableRecords.records.FindAll(x => x.time.DayOfYear == doy);
                
                if (synthDay.Count > 0)
                {
                    foreach (RecordMongo cm in candidateDay)
                    {
                        double value = cm.value;
                        RecordMongo synthRecordForUpdate = synthDay.Find(x => x.time.Hour == cm.time.Hour);
                        synthRecordForUpdate.value = value;
                    }
                }
            }
            catch (Exception e)
            {
                var error = "errorhere";
            }
        }
        private void simpleHoleFill(ref List<RecordMongo> candidateDay, CollectionMongo variableRecords)
        {
            foreach (RecordMongo cm in candidateDay)
            {
               
            }
        }
        private async Task selectDayOfYear(string vcode,SyntheticYear synthYear, List<IMongoCollection<RecordMongo>> stationData)
        {
            var v = synthYear.variables.Find(x => x.name == vcode);
            var builder = Builders<RecordMongo>.Filter;
            string[] pieces;

                List<List<RecordMongo>> possDayValues;
                for (int doy = 1; doy < 366; doy++)
                {
                if (doy == 60) continue;
                    //each day will have several canadidate days sourced from each collection of same variable
                    possDayValues = new List<List<RecordMongo>>();
                    foreach (IMongoCollection<RecordMongo> sd in stationData)
                    {
                        //only if the vcode matches
                        pieces = sd.CollectionNamespace.CollectionName.Split('_');
                        if (pieces[4] == vcode)
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
                                    if (hours == 24)
                                    {
                                        List<RecordMongo> dayValues = new List<RecordMongo>();
                                        foreach (BsonDocument name in yearDayGroup)
                                        {
                                            RecordMongo rm = new RecordMongo();
                                            rm.value = name.GetValue("value").ToDouble();
                                            rm.time = name.GetValue("time").ToLocalTime();
                                            dayValues.Add(rm);
                                        }
                                        possDayValues.Add(dayValues);
                                    }
                                }

                            }
                            catch (Exception e)
                            {
                                var error = "errorhere";
                            }

                        }
                    }
                    if (possDayValues.Count>0)
                    {
                    //randomly choose 1
                    int total = 0;
                    Random rand = new Random();
                    List<RecordMongo> candidateDay;
                    List<RecordMongo> synthDay;
                    List<int> hourcheck = new List<int>();
                    RecordMongo synthRecordForUpdate;
                    double value = 0;
                        try
                            {
                                total = possDayValues.Count;
                            
                                candidateDay = possDayValues[rand.Next(0, total)];
                                //assign to synthetic year
                                synthDay = v.records.FindAll(x => x.time.DayOfYear == doy);
                            //doy 60 does not exisit
                            if (synthDay.Count>0)
                            {
                                foreach (RecordMongo cm in candidateDay)
                                {
                                    value = cm.value;
                                    if (vcode == "HR" && value <= 1) value = value * 100;
                                    if (vcode == "NUB")
                                    {
                                        if (value == 9) value = 10;
                                        value = (int)(value / 8.0 * 10);
                                    }
                                    hourcheck.Add(cm.time.Hour);
                                    synthRecordForUpdate = synthDay.Find(x => x.time.Hour == cm.time.Hour);

                                    synthRecordForUpdate.value = value;
                                }
                            }
                            }
                             catch(Exception e)
                            {
                            var error = "errorhere";
                        }
                    }
                }
            
        }
        
        private List<IMongoCollection<RecordMongo>> getTheStationData(List<string> stationCollections)
        {
            List<IMongoCollection<RecordMongo>> stationData = new List<IMongoCollection<RecordMongo>>();
            foreach (string c in stationCollections)
            {
                stationData.Add(db.GetCollection<RecordMongo>(c));
            }
            return stationData;
        }
        
        private void getStationData()
        {
            //list of station meta data
            stations = StationGrouping.getAllStationsFromDB(db);
            var coll = db.GetCollection<StationGroup>("cityGroups");
            //stationsByCity = coll.Find(FilterDefinition<StationGroup>.Empty).ToList();
            //cityGroup = stationsByCity.Find(x => x.name == city);
            //getStationsColNames();
            //Task t1 = Task.Run(() => indexCityCollections());
            ////add the processed data to mongo
            //t1.Wait();
         
        }
        private void clean60Minutes(ref List<string> stationCollections)
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
                //store the clean collection name
                stationCollections.Add(cr.convertNameToClean(c));
            }
            
        }
        private void indexCityCollections(List<string> stationCollections)
        {
            IndexStationVariableCollections isvc = new IndexStationVariableCollections();
            foreach (string c in stationCollections)
            {
                isvc.createCollectionIndex(c);
            }
        }
        private void index60Minutes(List<string> stationCollections)
        {
            IndexStationVariableCollections isvc = new IndexStationVariableCollections();
            foreach (string c in stationCollections)
            {
                isvc.createCollectionIndex(c);
            }
        }

        private void averageTenMinute(ref List<string> stationCollections)
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
                stationCollections.Add(tmc.convertNameTo60min(c));
            }
           
        }
        private List<string> getStationsColNames(StationGroup cityGroup)
        {
            List<string> collections = MongoTools.collectionNames(db);
            string vname = "";
            int scode = 0;
            string source = "";
            int freq = 0;
            List<string> stationCollections = new List<string>();
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
                        if (scode == code&&freq==60)
                        {
                            stationCollections.Add(col);
                        }

                    }
                }
            }
            return stationCollections;
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
    class Hole
    {
        private DateTime holeStart { get; set; }
        private DateTime holeEnd { get; set; }
        public string vcode { get; set; }

        public void setHoleEnd(DateTime end)
        {
            if(end<this.holeStart)
            {
                this.holeStart = end;
            }
            else
            {
                this.holeEnd = end;
            }
        }
        public void setHoleStart(DateTime start)
        {
            this.holeStart = start;
        }
        public DateTime getHoleEnd()
        {
            return this.holeEnd;
        }
        public DateTime getHoleStart()
        {
            return this.holeStart;
        }
    }
    class SyntheticYear
    {
        public ObjectId Id { get; set; }
        public string name { get; set; }
        public Station info { get; set; }
        public List<CollectionMongo> variables { get; set; }
        private string[] vNames = new string[] { "NUB", "TS", "DV", "VV", "PR", "HR", "RS" };
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
            int year = 2001;
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
