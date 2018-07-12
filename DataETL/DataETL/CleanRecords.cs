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
namespace DataETL
{
    
    class CleanRecords
    {
        IMongoDatabase db;
        List<CollectionMongo> newCleanData = new List<CollectionMongo>();
        public CleanRecords()
        {
            db = MongoTools.connect("mongodb://localhost/?maxPoolSize=1000", "climaColombia");
        }
        public void cleanSingle(string collection)
        {
            string[] parts = collection.Split('_');
            int stationcode = Convert.ToInt32(parts[1]);
            string vname = parts[4];
            if (vname == "PA") return;
            string source = parts[2];
            int freq = Convert.ToInt32(parts[5]);
            
            VariableMeta meta = AnnualSummary.getVariableMetaFromDB(vname, source, db);
            string newname = convertNameToClean(collection);
            //collection for the avergaed data
            CollectionMongo cm = new CollectionMongo();
            cm.name = newname;
            newCleanData.Add(cm);
            Task t1 = Task.Run(() => removeRecordsOutsideRange(stationcode, collection, meta, newname));
            t1.Wait();
            insertMany(cm.records, cm.name);
          
        }
        public void clean()
        {
            //("mongodb://localhost/?maxPoolSize=555");
            db = MongoTools.connect("mongodb://localhost/?maxPoolSize=1000", "climaColombia");
            Task t1 = Task.Run(() => cleanUp());

            //add the processed data to mongo
            t1.Wait();
            foreach (CollectionMongo cm in newCleanData)
            {
                insertMany(cm.records, cm.name);
            }
        }
        public void cleanUp()
        {
            List<string> collNames = MongoTools.collectionNames(db);
            string vname = "";
            int stationcode = 0;
            string source = "";
            int freq = 0;
            foreach (string collection in collNames)
            {
                //all station record collections start with an s_
                if (collection[0] == 's')
                {
                    string[] parts = collection.Split('_');
                    stationcode = Convert.ToInt32(parts[1]);
                    vname = parts[4];
                    if (vname == "PA") continue;
                    source = parts[2];
                    freq = Convert.ToInt32(parts[5]);
                    VariableMeta meta = AnnualSummary.getVariableMetaFromDB(vname, source, db);
                    if (freq == 60)
                    {
                        string newname = convertNameToClean(collection);
                        CollectionMongo cm = new CollectionMongo();
                        cm.name = newname;
                        removeRecordsOutsideRange(stationcode, collection, meta, newname);
                    }
                }
            }
        }
        public async Task removeRecordsOutsideRange(int scode, string collname, VariableMeta vm, string newcollectionname)
        {
            IMongoCollection<RecordMongo> collection = db.GetCollection<RecordMongo>(collname);
            var filter = FilterDefinition<RecordMongo>.Empty;
            FindOptions<RecordMongo> options = new FindOptions<RecordMongo>
            {
                BatchSize = 1000,
                NoCursorTimeout = true
            };
            using (IAsyncCursor<RecordMongo> cursor = await collection.FindAsync(filter, options))
            {
                while (await cursor.MoveNextAsync())
                {
                    IEnumerable<RecordMongo> batch = cursor.Current;
                    foreach (RecordMongo rm in batch)
                    {
                        //only if the value is in range
                        if (rm.value > vm.min && rm.value < vm.max)
                        {
                            addRecord(rm, newcollectionname);
                        }
                    }
                }
            }
        }
        private void addRecord(RecordMongo rm, string cName)
        {
            var cm = newCleanData.Find(x => x.name == cName);
            cm.records.Add(rm);
        }
        public string convertNameToClean(string name)
        {
            string[] parts = name.Split('_');
            string source = parts[2] + "Clean";
            string newname = parts[0] + "_" + parts[1] + "_" + source + "_" + parts[3] + "_" + parts[4] + "_60";
            return newname;
        }
        public void insertMany(List<RecordMongo> records, string collectionname)
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
    }
}
