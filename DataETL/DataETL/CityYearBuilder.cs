﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using Accord.Statistics.Distributions.Univariate;
using System.IO;

namespace DataETL
{
    class CityYearBuilder
    {
        IMongoDatabase db;
        String logFile;
        List<Station> stations = new List<Station>();

        public CityYearBuilder()
        {
            db = MongoTools.connect("mongodb://localhost", "climaColombia");
            this.logFile = "syntheticYearBuilder_" + DateTime.Now.Millisecond+".txt";
            this.addLineToLogFile("INFO: process launched");
        }
        public void readSythYearFromDB()
        {
            stations = StationGrouping.getAllStationsFromDB(db);
            //for the regional fix use:
            //var coll = db.GetCollection<StationGroup>("cityRegionGroups");
            //for the city groups use:
            var coll = db.GetCollection<StationGroup>("cityGroups");
            var allCityGroups = coll.Find(FilterDefinition<StationGroup>.Empty).ToList();
            this.addLineToLogFile("INFO: preparing to read synthYearsFromDB");
            foreach (StationGroup sg in allCityGroups)
            {
                var city = sg.name;
                //the good from the city groups
                //city == "SANTA FE DE BOGOTÁ" || city == "BARRANQUILLA" || city == "CARTAGENA"||city == "LETICIA"
                if (city == "LETICIA")
                {

                    List<City> cities = MapTools.readCities();
                    City current = cities.Find(c => c.name == city);
                    writeEPW(current.name, current.location[1], current.location[0], current.elevation);
                }

            }
            
        }
        public void writeEPW(string name,double lat,double lon,double ele)
        {
            var collection = db.GetCollection<SyntheticYear>(name + "_medianHour");
            List<SyntheticYear> synthYear = collection.Find(FilterDefinition<SyntheticYear>.Empty).ToList();
            try
            {
                synthYear[0].name += "_synthYear_rc2";
                synthYear[0].info = new Station();
                synthYear[0].info.latitude = lat;
                synthYear[0].info.longitude = lon;
                synthYear[0].info.elevation = ele;
                synthYear[0].info.country = "COLOMBIA";
                synthYear[0].info.source = "clima-colombia synthetic year 2018";
                EPWWriter epww = new EPWWriter(synthYear[0], @"C:\Users\Admin\Documents\projects\IAPP\piloto\Climate");
                this.addLineToLogFile("INFO: " + name + " written as EPW");
            }
            catch
            {
                this.addLineToLogFile("WARN: " + name + " synth year could not be read from DB");
            }
        }
        public async Task prepOneGroup(StationGroup sg)
        {
            try
            {
                var stationCollNames = getStationsColNames(sg);
                averageTenMinute(ref stationCollNames);
                await index60Minutes(stationCollNames);
                this.addLineToLogFile("INFO: " + sg.name + " data prep completed");
            }
            catch
            {
                this.addLineToLogFile("WARN: " + sg.name + " data prep failed");
            }
        }
        public async Task syntheticYearDataPrep(string groups)
        {
            stations = StationGrouping.getAllStationsFromDB(db);
            var coll = db.GetCollection<StationGroup>(groups);
            var allCityGroups = coll.Find(FilterDefinition<StationGroup>.Empty).ToList();
            this.addLineToLogFile("INFO: preparing data");
            foreach (StationGroup sg in allCityGroups)
            {
                var city = sg.name;
                
                    try
                    {
                        var cityGroup = allCityGroups.Find(x => x.name == city);
                        var stationCollNames = getStationsColNames(cityGroup);
                        
                        averageTenMinute(ref stationCollNames);
                        await index60Minutes(stationCollNames);
                        this.addLineToLogFile("INFO: "+city+" data prep completed");
                    }
                    catch
                    {
                        this.addLineToLogFile("WARN: " + city + " data prep failed");
                    }
                          
            }
        }
        public async Task syntheticYearBatchFixer(string method)
        {
            CityYearFixer cyf = new CityYearFixer();
            List<NeededData> neededData = cyf.readRequiredData();
            stations = StationGrouping.getAllStationsFromDB(db);
            var coll = db.GetCollection<StationGroup>("cityRegionGroups");
            var allCityGroups = coll.Find(FilterDefinition<StationGroup>.Empty).ToList();
            this.addLineToLogFile("INFO: starting batch of synth years");
            List<Task> tasks = new List<Task>();
            foreach (StationGroup sg in allCityGroups)
            {
                var city = sg.name;
                NeededData cityNeeds = neededData.Find(nd => nd.name == city);
                if (cityNeeds != null)
                {
                    if (city != "MITU")
                    {
                        var cityGroup = allCityGroups.Find(x => x.name == city);
                        var stationCollNames = getStationsColNames(cityGroup);
                        tasks.Add(fixCity(city, method, cityNeeds, stationCollNames));
                    }
                    

                }
                
            }
            await Task.WhenAll(tasks);
        }
        public async Task fixCity(string city,string method, NeededData cityNeeds, List<string> stationCollNames)
        {
            List<IMongoCollection<RecordMongo>> stationData = new List<IMongoCollection<RecordMongo>>();
            try
            {
                //ignore 10min collections
                stationData = getTheStationData(stationCollNames.FindAll(s => s.Contains("_60")));
                this.addLineToLogFile("INFO: found ref data for " + city + " synth year");
            }
            catch
            {
                this.addLineToLogFile("WARN: no ref data found for " + city + " synth year");
            }
            //read the synthyear for this city
            var collection = db.GetCollection<SyntheticYear>(city + "_medianHour");
            List<SyntheticYear> synthYear = collection.Find(FilterDefinition<SyntheticYear>.Empty).ToList();
            SyntheticYear sy = synthYear[0];
            SyntheticYear.convertSyntheticYear(ref sy);
            try
            {
                await getDaysForSelectedVariables(sy, stationData, method, cityNeeds.reqVariables);
                this.addLineToLogFile("INFO: calculated data for " + city + " synth year");
            }
            catch
            {
                this.addLineToLogFile("WARN: error in calculating values for " + city + " synth year");
            }

            try
            {
                holeFiller(holeFinder(ref sy), ref sy);
                this.addLineToLogFile("INFO: hole filling succeeded for " + city + " synth year");
            }
            catch
            {
                this.addLineToLogFile("WARN: hole filling failed for " + city + " synth year");
            }
            try
            {
                insertSytheticYear(city + "_" + method + "regionFix", sy);
                this.addLineToLogFile("INFO: " + city + " synth year was stored in DB");
            }
            catch
            {
                this.addLineToLogFile("WARN: " + city + " synth year was not stored in DB");
            }
        }
        public async Task makeSynthYear(StationGroup sg, string method)
        {
            
            List<IMongoCollection<RecordMongo>> stationData = new List<IMongoCollection<RecordMongo>>();
            try
            {
                
                var stationCollNames = getStationsColNames(sg);
                await index60Minutes(stationCollNames);
                //ignore 10min collections
                stationData = getTheStationData(stationCollNames.FindAll(s => s.Contains("60")));
                this.addLineToLogFile("INFO: found ref data for " + sg.name + " synth year");
            }
            catch
            {
                this.addLineToLogFile("WARN: no ref data found for " + sg.name + " synth year");
            }
            SyntheticYear synthYear = new SyntheticYear();
            synthYear.name = sg.name;
            try
            {
                await getDaysForVariables(synthYear, stationData, method);
                this.addLineToLogFile("INFO: calculated data for " + sg.name + " synth year");
            }
            catch
            {
                this.addLineToLogFile("WARN: error in calculating values for " + sg.name + " synth year");
            }

            try
            {
                holeFiller(holeFinder(ref synthYear), ref synthYear);
                this.addLineToLogFile("INFO: hole filling succeeded for " + sg.name + " synth year");
            }
            catch
            {
                this.addLineToLogFile("WARN: hole filling failed for " + sg.name + " synth year");
            }
            try
            {
                insertSytheticYear(sg.name + "_" + method, synthYear);
                this.addLineToLogFile("INFO: " + sg.name + " synth year was stored in DB");
            }
            catch
            {
                this.addLineToLogFile("WARN: " + sg.name + " synth year was not stored in DB");
            }
        }
        public async Task syntheticYearBatch(string method)
        {
            stations = StationGrouping.getAllStationsFromDB(db);
            var coll = db.GetCollection<StationGroup>("cityGroups");
            var allCityGroups = coll.Find(FilterDefinition<StationGroup>.Empty).ToList();
            this.addLineToLogFile("INFO: starting batch of synth years");
           
            
            foreach (StationGroup sg in allCityGroups)
            {
                await makeSynthYear(sg, method);
            }
        }
        private void nightRadiation(ref CollectionMongo radvariables)
        {
            foreach (RecordMongo rm in radvariables.records)
            {
                if(rm.time.Hour<6||rm.time.Hour>18)
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
                    //check for start end year 
                    try
                    {
                        if (h.getHoleStart().Month == 1 && h.getHoleStart().Day == 1 && h.getHoleStart().Hour == 0)
                        {
                            //hole at first hour of year
                            start = new DateTime(h.getHoleStart().Year, 12, 31, 23, 0, 0);
                        }
                        else { start = h.getHoleStart().AddHours(-1); }
                        if (h.getHoleEnd().Month == 12 && h.getHoleEnd().Day == 31 && h.getHoleEnd().Hour == 23)
                        {
                            //hole at final hour of year
                            end = new DateTime(h.getHoleStart().Year, 1, 1, 0, 0, 0);
                        }
                        else { end = h.getHoleEnd().AddHours(1); }

                        //interpolation
                        double v1 = synthYear.variables.Find(x => x.name == h.vcode).records.Find(r => r.time == start).value;
                        double v2 = synthYear.variables.Find(x => x.name == h.vcode).records.Find(r => r.time == end).value;
                        double range = v2 - v1;
                        double inc = range / (holesize.Hours + 2);
                        for (int i = 1; i <= holesize.Hours + 1; i++)
                        {
                            current = start.AddHours(i);

                            var tofill = synthYear.variables.Find(x => x.name == h.vcode).records.Find(r => r.time == current);
                            tofill.value = v1 + (i * inc);


                        }
                    }
                    catch (Exception e)
                    {
                        this.addLineToLogFile("WARN: " + h.vcode + " holefilling error between "+h.getHoleStart().ToString()+" and "+h.getHoleEnd().ToString());
                    }
                }
                else
                {
                    //select from neighour hood of days in week
                }
            }
        }
        private async Task getDaysForVariables(SyntheticYear synthYear, List<IMongoCollection<RecordMongo>> stationData,string method)
        {
            if (method == "cdfDay")
            {
                await Task.WhenAll(synthYear.variables.Select(c => selectDayOfYearCDF(c.name, synthYear, stationData)));
                this.addLineToLogFile("INFO: got CDF daily values for " + synthYear.name + " synth year");
            }
            else
            {
                await Task.WhenAll(synthYear.variables.Select(c => generateHour(c.name, synthYear, stationData, method)));
                this.addLineToLogFile("INFO: calculated hourly values for " + synthYear.name + " synth year");
            }
            
        }
        private async Task getDaysForSelectedVariables(SyntheticYear synthYear, List<IMongoCollection<RecordMongo>> stationData, string method,List<string> fields)
        {
            if (method == "cdfDay")
            {
                List<Task> tasks = new List<Task>();
                foreach (string field in fields)
                {
                    tasks.Add(selectDayOfYearCDF(field, synthYear, stationData));

                }
                await Task.WhenAll(tasks);
                
                this.addLineToLogFile("INFO: got CDF daily values for " + synthYear.name + " synth year");
            }
            else
            {
                //List<Task> tasks = new List<Task>();
                foreach(string field in fields)
                {
                    await generateHour(field, synthYear, stationData, method);
                    
                }
                //await Task.WhenAll(tasks);
                this.addLineToLogFile("INFO: calculating hourly values for " + synthYear.name + " synth year");
            }

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
        
        private async Task generateHour(string vcode, SyntheticYear synthYear, List<IMongoCollection<RecordMongo>> stationData,string meanMedian)
        {
            var v = synthYear.variables.Find(x => x.name == vcode);
            var builder = Builders<RecordMongo>.Filter;
            string[] pieces;
            VariableMeta vm;
            int hourofsyntheticyear = 0;

            DateTime local = new DateTime();
            DateTime universal = new DateTime();
            //find collections with current variable
            List<IMongoCollection<RecordMongo>> sourceStationData = new List<IMongoCollection<RecordMongo>>();
            foreach (IMongoCollection<RecordMongo> sd in stationData)
            {
                pieces = sd.CollectionNamespace.CollectionName.Split('_');
                if (pieces[4] == vcode)
                {
                    sourceStationData.Add(sd);
                }
            }
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
                foreach (IMongoCollection<RecordMongo> sd in sourceStationData)
                {
                    //only if the vcode matches
                    pieces = sd.CollectionNamespace.CollectionName.Split('_');
                   
                    string source = pieces[2];
                    
                    vm = AnnualSummary.getVariableMetaFromDB(vcode, source, db);
                    int startYr = 0;
                    int endYr = 0;
                    getFirstLastYear(sd, ref startYr, ref endYr);
                    if (startYr == 1) startYr = 1980;
                    if(endYr ==1) startYr = 2018;
                    for (int y = startYr; y < endYr; y++)
                    {

                        local = new DateTime(y, m, d, h, 0, 0);
                        universal = local.ToUniversalTime();
                        var filter = builder.Eq("time", universal) & builder.Gte("value", vm.min) & builder.Lte("value", vm.max);
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
                if (foundValues != 0)
                {
                    if(meanMedian=="meanHour")r.value = Accord.Statistics.Measures.Mean(valuesForHour.ToArray());
                    if(meanMedian == "medianHour") r.value = Accord.Statistics.Measures.Median(valuesForHour.ToArray());
                    if(meanMedian=="randomHour")
                    {
                        int total = valuesForHour.Count;
                        Random rand = new Random();
                        r.value = valuesForHour[rand.Next(0, total)];
                    }

                }
            }
        }
        private async Task generateHour2(string vcode, SyntheticYear synthYear, List<IMongoCollection<RecordMongo>> stationData, string meanMedian)
        {
            var v = synthYear.variables.Find(x => x.name == vcode);
            var builder = Builders<RecordMongo>.Filter;
            string[] pieces;
            VariableMeta vm;
            int hourofsyntheticyear = 0;
            
            DateTime universal = new DateTime();
            //find collections with current variable
            List<IMongoCollection<RecordMongo>> sourceStationData = new List<IMongoCollection<RecordMongo>>();
            foreach (IMongoCollection<RecordMongo> sd in stationData)
            {
                pieces = sd.CollectionNamespace.CollectionName.Split('_');
                if (pieces[4] == vcode)
                {
                    sourceStationData.Add(sd);
                }
            }
            foreach (RecordMongo r in v.records)
            {
                //synth year is local time
                universal = r.time.ToUniversalTime();
                int h = universal.Hour;
                int doy = universal.DayOfYear;
                hourofsyntheticyear++;
                int foundValues = 0;
               
                List<double> valuesForHour = new List<double>();
                foreach (IMongoCollection<RecordMongo> sd in sourceStationData)
                {
                    pieces = sd.CollectionNamespace.CollectionName.Split('_');
                    string source = pieces[2];
                    if (source.Contains("NOAA")) source = "NOAA";
                    else source = "IDEAM";
                    vm = AnnualSummary.getVariableMetaFromDB(vcode, source, db);
                    var project =
                        BsonDocument.Parse(
                            "{value: '$value',time:'$time',dayOfYear: {$dayOfYear: '$time'},hour: {$hour: '$time'}}");
                    try
                    {
                            
                        var aggregationDocument =
                            sd.Aggregate()
                                .Unwind("value")
                                .Project(project)
                                .Match(BsonDocument.Parse("{$and:[" +
                                "{'dayOfYear' : {$eq : " + doy.ToString() + "}}" +
                                ",{'hour' : {$eq : " + h.ToString() + "}}" +
                                ",{'value':{$lte:" + vm.max.ToString() + " }}" +
                                ",{'value':{$gte:" + vm.min.ToString() + "}}]}"))
                                .ToList();

                        IEnumerable<IGrouping<int, BsonDocument>> query = aggregationDocument.GroupBy(
                            doc => doc.GetValue("dayOfYear").ToInt32(),
                            doc => doc);

                        foreach (IGrouping<int, BsonDocument> hourValsGroup in query)
                        {
  
                            foreach (BsonDocument name in hourValsGroup)
                            {
                                double value = name.GetValue("value").ToDouble();
                                foundValues++;
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
                                    
                            valuesForHour.Add(value);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        this.addLineToLogFile("WARN: " + synthYear.name + vcode + " error finding hourly values at day of year: "+doy +" ,hour : "+ h);
                    }
                    
                }
                if (foundValues != 0)
                {
                    if (meanMedian == "meanHour") r.value = Accord.Statistics.Measures.Mean(valuesForHour.ToArray());
                    if (meanMedian == "medianHour") r.value = Accord.Statistics.Measures.Median(valuesForHour.ToArray());
                    if (meanMedian == "randomHour")
                    {
                        int total = valuesForHour.Count;
                        Random rand = new Random();
                        r.value = valuesForHour[rand.Next(0, total)];
                    }

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
            //find collections with current variable
            List<IMongoCollection<RecordMongo>> sourceStationData = new List<IMongoCollection<RecordMongo>>();
            foreach (IMongoCollection<RecordMongo> sd in stationData)
            {
                pieces = sd.CollectionNamespace.CollectionName.Split('_');
                if (pieces[4] == vcode)
                {
                    sourceStationData.Add(sd);
                }
            }
            for (int doy = 1; doy < 366; doy++)
            {
                //each day will have several canadidate days sourced from each collection of same variable
                possDayValues = new List<List<RecordMongo>>();
                foreach (IMongoCollection<RecordMongo> sd in sourceStationData)
                {
                    pieces = sd.CollectionNamespace.CollectionName.Split('_');
                    string source = pieces[2];
                    if (source.Contains("NOAA")) source = "NOAA";
                    else source = "IDEAM";
                    vm = AnnualSummary.getVariableMetaFromDB(vcode, source, db);
                    var project =
                        BsonDocument.Parse(
                            "{value: '$value',time:'$time',dayOfYear: {$dayOfYear: '$time'},year: {$year: '$time'}}");
                    try
                    {
                        //.Match(BsonDocument.Parse("{'dayOfYear' : {$eq : " + doy.ToString() + "}}"))
                        var aggregationDocument =
                            sd.Aggregate()
                                .Unwind("value")
                                .Project(project)
                                .Match(BsonDocument.Parse("{$and:[" +
                                "{'dayOfYear' : {$eq : " + doy.ToString() + "}}" +
                                ",{'value':{$lte:" + vm.max.ToString() + " }}" +
                                ",{'value':{$gte:" + vm.min.ToString() + "}}]}"))
                                .ToList();

                        IEnumerable<IGrouping<int, BsonDocument>> query = aggregationDocument.GroupBy(
                            doc => doc.GetValue("year").ToInt32(),
                            doc => doc);

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
                        this.addLineToLogFile("WARN: " + synthYear.name + vcode + " error finding cdf day at day of year: " + doy);
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
                this.addLineToLogFile("WARN: " + vcode + " error adding cdf day on day of year: " + doy);
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
        private async Task indexCityCollections(List<string> stationCollections)
        {
            IndexStationVariableCollections isvc = new IndexStationVariableCollections();
            foreach (string c in stationCollections)
            {
                isvc.createCollectionIndex(c);
            }
        }
        private async Task index60Minutes(List<string> stationCollections)
        {
            IndexStationVariableCollections isvc = new IndexStationVariableCollections();
            foreach (string c in stationCollections)
            {
                isvc.createCollectionIndex(c);
            }
        }
        private void addLineToLogFile(String line)
        {
            StreamWriter sw = new StreamWriter(this.logFile,true);
            sw.WriteLine(line);
            sw.Close();
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
                    this.addLineToLogFile("WARN: ten minute averaging error in collection: " + c);
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
                        this.addLineToLogFile("WARN: error finding collection: "+ col + "for city group:" + cityGroup.name);
                    
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
        private string[] vNames = new string[] { "NUB", "TS", "DV", "VV", "PR", "HR", "RS", };
        public SyntheticYear()
        {
            variables = new List<CollectionMongo>();
            foreach (string vname in vNames) variables.Add(generateVariableYear(vname));
        }
        public static void convertSyntheticYear(ref SyntheticYear yearToCopy)
        {
            //input synth year is in UTC time
            foreach(CollectionMongo vcoll in yearToCopy.variables)
            {
                
                foreach(RecordMongo r in vcoll.records)
                {
                    r.time = r.time.ToLocalTime();
                }
            }
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
