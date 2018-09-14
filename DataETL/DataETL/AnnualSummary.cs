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

namespace DataETL
{
    class AnnualSummary
    {
        IMongoDatabase db;
        List<StationSummary> stations = new List<StationSummary>();
        List<VariableMeta> variableMeta = new List<VariableMeta>();
        List<RecordMeta> recordInfo = new List<RecordMeta>();
        public AnnualSummary()
        {
            connect("mongodb://localhost", "climaColombia");
        }
        public void getRequiredVaribleMeta()
        {
            StreamReader sr = new StreamReader(@"C:\Users\Admin\Documents\projects\IAPP\piloto\Climate\VariablesMeta.csv");
            string line = sr.ReadLine();
            string[] parts;
            VariableMeta meta;
            while (line != null)
            {
                parts = line.Split(',');
                meta = new VariableMeta(parts[0], parts[1], parts[3], Convert.ToInt32(parts[6]), Convert.ToInt32(parts[5]),parts[2],Convert.ToInt32(parts[4]));
                variableMeta.Add(meta);
                line = sr.ReadLine();
            }
            sr.Close();
            insertManyRecord();
        }
        public void insertManyRecord()
        {
            var collection = db.GetCollection<VariableMeta>("metaVariables");
            var listOfDocuments = new List<VariableMeta>();
            var limitAtOnce = 1000;
            var current = 0;

            foreach (VariableMeta vm in variableMeta)
            {
                listOfDocuments.Add(vm);
                if (++current == limitAtOnce)
                {
                    current = 0;
                    var listToInsert = listOfDocuments;
                    var t = new Task(() => { collection.InsertManyAsync(listToInsert); });
                    t.Start();
                    listOfDocuments = new List<VariableMeta>();
                }
            }
            var f = new Task(() => { collection.InsertManyAsync(listOfDocuments); });
            f.Start();

        }
        private void connect(string connectionString, string dbName)
        {
            db = MongoTools.connect(connectionString, dbName);
        }
        
        public async Task textSummaryStations(string dbname)
        {
            //summary based on total variables
            List<string> collNames = MongoTools.collectionNames(db);
            string firstletter = "";
            string vname = "";
            int stationcode = 0;
            string source = "";
            int freq = 0;
            
            foreach (string collection in collNames)
            {
                //all station record collections start with an s_
                string[] parts = collection.Split('_');
                firstletter = parts[0];
                if (firstletter == "s")
                {
                    stationcode = Convert.ToInt32(parts[1]);
                    vname = parts[4];
                    if (vname == "PA") continue;
                    source = parts[2];
                    freq = Convert.ToInt32(parts[5]);
                    
                    VariableMeta meta = getVariableMetaFromDB(vname, source,db);
                    RecordMeta rm = new RecordMeta(vname,freq);
                    addStation(stationcode);
                    addRecord(stationcode, rm);
                    await getTotalRecords(collection, stationcode, vname);
                    await getDateLimits(collection, stationcode, vname);
                    await insideRange(collection, stationcode, meta);
                }
            }
            annualStats();
            insertManyRecordStationSummary();
            printToSummary();
        }
        public void outputAnnual()
        {
            //this could select the most recent annual
            var coll = db.GetCollection<StationSummary>("annualStationSummary_2018_7_3_14_6_5");
           List<StationSummary> stations = coll.Find(FilterDefinition<StationSummary>.Empty).ToList();
            StreamWriter sw = new StreamWriter("annualSummary.csv");
            sw.WriteLine("code,name,source,TS,HR,PR,VV,DV,NUB");
            foreach(StationSummary ss in stations)
            {
                Station s = getStationFromMongo(ss.code,db);
                sw.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}",
                    ss.code,s.name,s.source,
                    ss.getTotalYrs("TS").ToString(),
                    ss.getTotalYrs("HR").ToString(),
                    ss.getTotalYrs("PR").ToString(),
                    ss.getTotalYrs("VV").ToString(),
                    ss.getTotalYrs("DV").ToString(),
                    ss.getTotalYrs("NUB").ToString()
                    ));
            }
            sw.Close();
        }
        public static VariableMeta getVariableMetaFromDB(string code,string source, IMongoDatabase db)
        {
            if (source.Contains("IDEAM")) source = "IDEAM";
            else source = "NOAA";
            IMongoCollection<VariableMeta> collection = db.GetCollection<VariableMeta>("metaVariables");
            var builder = Builders<VariableMeta>.Filter;
            var filter = builder.Eq("code", code) & builder.Eq("source", source);
            var vms = collection.FindSync(filter).ToList();
            if (vms.Count > 0)
            {
                return vms[0];
            }
            else
            {
                return null;
            }
        }
        public async Task<int> insideRange(string collname, int sCode, VariableMeta meta)
        {
            IMongoCollection<BsonDocument> collection = db.GetCollection<BsonDocument>(collname);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("stationCode", sCode) & builder.Gte("value", meta.min) & builder.Lte("value", meta.max);

            FindOptions<BsonDocument> options = new FindOptions<BsonDocument>
            {
                BatchSize = 1000,
                NoCursorTimeout = false
            };
           var  total = await collection.CountDocumentsAsync(filter);
            updateRecordInside(sCode, meta.code, (int)total);
            return 1;
        }
        public async Task<int> getTotalRecords(string collname, int sCode, string vName)
        {
            IMongoCollection<BsonDocument> collection = db.GetCollection<BsonDocument>(collname);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("stationCode", sCode);
            long total = 0;
            
            total =  await collection.CountDocumentsAsync(filter);
            
            updateRecordTotal(sCode, vName, total);
            return 1;
        }
        
        public async Task getDateLimits(string collname, int sCode, string vName)
        {
            IMongoCollection<BsonDocument> collection = db.GetCollection<BsonDocument>(collname);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("stationCode", sCode);

            DateTime oldest = new DateTime();
            await collection.Find(filter)
            .Limit(1)
            .Sort("{time: 1}")
            .ForEachAsync(doc =>
            {
                var date = doc["time"].ToUniversalTime();
                //ignore incorrect formats
                if (date.Year != 1) { oldest = date.ToLocalTime(); }
            });
            DateTime newest = new DateTime();
            await collection.Find(filter)
            .Limit(1)
            .Sort("{time: -1}")
            .ForEachAsync(doc =>
            {
                var date = doc["time"].ToUniversalTime();
                //ignore incorrect formats
                if (date.Year != 1) { newest = date.ToLocalTime(); }
            });

            updateRecordDates(sCode, vName, oldest, newest);
            
        }
        private void updateRecordInside(int scode, string rname, int v)
        {
            var s = stations.Find(x => x.code == scode);
            var rm = s.recordMeta.Find(x => x.name == rname);
            rm.insideRange = v;    
        }
        private void updateRecordDates(int scode, string rname, DateTime start, DateTime end)
        {
            var s = stations.Find(x => x.code == scode);
            var rm = s.recordMeta.Find(x => x.name == rname);
            rm.setDates(start, end);
            
        }
        private void updateRecordTotal(int scode, string rname, long total)
        {
            var s = stations.Find(x => x.code == scode);
            var rm = s.recordMeta.Find(x => x.name == rname);
            rm.count = total;
            
        }
        private void addRecord(int code, RecordMeta rm)
        {
            var s = stations.Find(x => x.code == code);
            s.addRecordMeta(rm);
        }
        private void addStation(int code)
        {
            var s = stations.Find(x => x.code == code);
            if(s==null)
            {
                StationSummary stat = new StationSummary(code);
                stations.Add(stat);
            }
        }
        public void insertManyRecordStationSummary()
        {
            DateTime dt = DateTime.Now;
            string collectionname = "annualStationSummary_" + dt.Year.ToString()
                + "_" + dt.Month.ToString() + "_" + dt.Day.ToString() + "_" + dt.Hour.ToString() + "_" + dt.Minute.ToString() + "_" + dt.Second.ToString();
            var collection = db.GetCollection<StationSummary>(collectionname);
            MongoTools.storeSummaryCollectionName(db, collectionname);
            var listOfDocuments = new List<StationSummary>();
            var limitAtOnce = 1000;
            var current = 0;

            foreach (StationSummary ss in stations)
            {
                listOfDocuments.Add(ss);
                if (++current == limitAtOnce)
                {
                    current = 0;
                    var listToInsert = listOfDocuments;
                    var t = new Task(() => { collection.InsertManyAsync(listToInsert); });
                    t.Start();
                    listOfDocuments = new List<StationSummary>();
                }
            }
            var f = new Task(() => { collection.InsertManyAsync(listOfDocuments); });
            f.Start();

        }
        private void annualStats()
        {
            foreach (StationSummary ss in stations)
            {
                foreach (RecordMeta rm in ss.recordMeta)
                {
                    if(rm.interval==60) rm.yearsReadings = Math.Round((rm.count / 8760.0), 2);
                    else rm.yearsReadings = Math.Round((rm.count / 52560.0), 2);
                }
            }
        }
        private void printToSummary()
        {
            StreamWriter sw = new StreamWriter("summary.csv");
            sw.WriteLine(",stationcode,nombre,lat,lon,alt");
            int count = 0;
            foreach (StationSummary ss in stations)
            {
                Station s = getStationFromMongo(ss.code,db);
                sw.WriteLine(count + "," + ss.code + "," + s.name + "," + s.latitude + "," + s.longitude + "," + s.elevation);
                sw.WriteLine(",variable_name,variable_count,variable_expected,percent_records,inside_range,percent_inside,startdate,enddate");
                foreach (RecordMeta rm in ss.recordMeta)
                {
                    double percentRecords = Math.Round((rm.count / (double)rm.expected), 2);
                    double percentInside = Math.Round((rm.insideRange / (double)rm.count), 2);
                    sw.WriteLine("," + rm.name + "," + rm.count + "," + rm.expected + "," + percentRecords + "," + rm.insideRange + "," + percentInside + "," + rm.oldest.ToString() + "," + rm.newest.ToString());
                }
                count++;
            }

            sw.Close();
        }
        public static Station getStationFromMongo(int code, IMongoDatabase db )
        {
            IMongoCollection<Station> collection = db.GetCollection<Station>("metaStations");
            var builder = Builders<Station>.Filter;
            var filter = builder.Eq("code", code);
            var s = collection.Find(filter).ToList();
            if (s.Count == 0) return new Station();
            else return s[0];
        }
    }
    
    public class VariableMeta
    {
        public ObjectId Id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public string units { get; set; }
        public int max { get; set; }
        public int min { get; set; }
        public string source { get; set; }
        public int freq { get; set; }
        public VariableMeta(string c, string n, string u, int ma, int mi,string sou,int f)
        {
            code = c;
            name = n;
            units = u;
            max = ma;
            min = mi;
            source = sou;
            freq = f;
        }
    }
    public class RecordMeta
    {
        public DateTime oldest { get; set; }
        public DateTime newest { get; set; }
        public long count{get;set;}
        public string name{get;set;}
        public int expected{get;set;}
        public int insideRange{get;set;}
        public int hourlyReadingsExpected{get;set;}
        public int tenMinReadingsExpected{get;set;}
        public double yearsReadings{get;set;}
        public List<double> day {get;set;}
        public List<double> readings{get;set;}
        public List<double> readingsOutofRange{get;set;}
        public int interval{get;set;}
        public RecordMeta(string n, int f)
        {
            name = n;
            interval =f;
            count = 0;
            insideRange = 0;
            day = new List<double>();
            readings = new List<double>();
            readingsOutofRange = new List<double>();
        }
        
        public void incrementInside(int amount)
        {
            insideRange += amount;
        }
        public void setDates(DateTime start, DateTime end)
        {
            oldest = start;
            newest = end;
            var diff = newest - oldest;
            hourlyReadingsExpected = (int)diff.TotalHours;
            tenMinReadingsExpected = (int)(diff.TotalMinutes / 10.0);
            if (interval == 60)
            {
                expected = (int)hourlyReadingsExpected;
            }
            else
            {
                expected = (int)tenMinReadingsExpected;
            }
        }
    }
    public class StationSummary
    {
        public ObjectId Id { get; set; }
        public List<RecordMeta> recordMeta { get; set; }
        public int code { get; set; }
        public double percentExpected { get; set; }
        
        public StationSummary(int icode)
        {
            code = icode;
            recordMeta = new List<RecordMeta>();
        }
        public void addRecordMeta(RecordMeta record)
        {
            recordMeta.Add(record);
        }
        public double getTotalYrs(string recordname)
        {
            var rm = recordMeta.Find(x => x.name == recordname);
            if (rm == null) return 0.0;
            else return rm.yearsReadings;
        }
    }
    
}
