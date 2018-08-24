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
    
    class TenMinuteConversion
    {
        List<CollectionMongo> newAveragedData = new List<CollectionMongo>();
        IMongoDatabase db;
        public TenMinuteConversion()
        {
            db = MongoTools.connect("mongodb://localhost/?maxPoolSize=1000", "climaColombia");
        }
        public void convert()
        {
            //("mongodb://localhost/?maxPoolSize=555");
            db = MongoTools.connect("mongodb://localhost/?maxPoolSize=1000", "climaColombia");
            Task t1 = Task.Run(() => convert10min());

            //add the processed data to mongo
            t1.Wait();
            foreach(CollectionMongo cm in newAveragedData)
            {
                insertMany(cm.records, cm.name);
            }
        }
        public void convertSingleCollection(string collection)
        {
            string[] parts = collection.Split('_');
            int stationcode = Convert.ToInt32(parts[1]);
            string vname = parts[4];
            
            string source = parts[2];
            int freq = Convert.ToInt32(parts[5]);
            if (freq == 60) return;
            VariableMeta meta = AnnualSummary.getVariableMetaFromDB(vname, source, db);
            string newname = convertNameTo60min(collection);
            //collection for the avergaed data
            CollectionMongo cm = new CollectionMongo();
            cm.name = newname;
            newAveragedData.Add(cm);
            Task t1 = Task.Run(() => sortByDateAndAverage(stationcode, collection, meta, newname));
            t1.Wait();
            insertMany(cm.records, cm.name);
        }
        public void convert10min()
        {
            List<string> collNames = MongoTools.collectionNames(db);
            string vname = "";
            int stationcode = 0;
            string source = "";
            int freq = 0;
            int tenmincollections = 0;
            foreach (string collection in collNames)
            {
                //all station record collections start with an s_
                if (collection[0] == 's'&&!collection.Contains("averaged"))
                {
                    convertSingleCollection(collection);
                    
                }
            }
        }
        public async Task sortByDateAndAverage(int scode, string collname, VariableMeta vm,string newcollectionname)
        {
            IMongoCollection<RecordMongo> collection = db.GetCollection<RecordMongo>(collname);
            var filter = FilterDefinition<RecordMongo>.Empty;
            var sorter = Builders<RecordMongo>.Sort.Ascending("time");
            FindOptions<RecordMongo> options = new FindOptions<RecordMongo>
            {
                BatchSize = 500,
                NoCursorTimeout = false,
                Sort = sorter
            };
            DateTime currentHour = new DateTime();
            bool firstrecord = true;
            
            using (IAsyncCursor<RecordMongo> cursor = await collection.FindAsync(filter, options))
            {
                double hourtotal = 0;
                int recordsPerHr = 0;
                while (await cursor.MoveNextAsync())
                {
                    IEnumerable<RecordMongo> batch = cursor.Current;
                    foreach (RecordMongo rm in batch)
                    {
                        //only if the value is in range
                        if (rm.value > vm.min && rm.value < vm.max)
                        {
                            if (firstrecord)
                            {
                                currentHour = rm.time;
                                firstrecord = false;
                            }
                            if(rm.time.DayOfYear==currentHour.DayOfYear&& rm.time.Hour == currentHour.Hour)
                            {
                                recordsPerHr++;
                                hourtotal += rm.value;
                            }
                            else
                            {
                                //make a new record and add to the list
                                
                                RecordMongo avrm = new RecordMongo();
                                avrm.processNote = "averaged from 10min readings";
                                avrm.stationCode = scode;
                                avrm.time = new DateTime(currentHour.Year, currentHour.Month, currentHour.Day, currentHour.Hour, 0, 0);
                                avrm.value = hourtotal / recordsPerHr;
                                if (!Double.IsNaN(avrm.value))
                                {
                                    addRecord(avrm, newcollectionname);
                                }
                                
                                //reset the counter and total
                                
                                recordsPerHr =0;
                                hourtotal =0;
                                //set the new hour
                                currentHour = rm.time;
                            }
                        }
                    }
                }
            }
        }
        public string convertNameTo60min(string collection)
        {
            string[] parts = collection.Split('_');
            string source = parts[2]+"averaged";
            string newname = parts[0] + "_"+ parts[1] + "_" + source + "_" + parts[3] + "_"+ parts[4] + "_60";
            return newname;
        }
        public void insertMany(List<RecordMongo> records,string collectionname)
        {
            var collection = db.GetCollection<RecordMongo>(collectionname);
            var listOfDocuments = new List<RecordMongo>();
            var limitAtOnce = 1000;
            var current = 0;
            foreach (RecordMongo rm in records)
            {
                listOfDocuments.Add(rm);
                if (++current == limitAtOnce)
                {
                    current = 0;
                    var listToInsert = listOfDocuments;
                    var t = new Task(() => { collection.InsertManyAsync(listToInsert); });
                    t.Start();
                    listOfDocuments = new List<RecordMongo>();
                }
            }
            var f = new Task(() => { collection.InsertManyAsync(listOfDocuments); });
            f.Start();

        }
        private void addRecord(RecordMongo rm,string cName)
        {
            var cm = newAveragedData.Find(x => x.name == cName);
            cm.records.Add(rm);
        }
    }
    class CollectionMongo
    {
        public List<RecordMongo> records { get; set; }
        public string name { get; set; }
        public CollectionMongo()
        {
            records = new List<RecordMongo>();
        }
    }
}
