using MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataETL
{
    class MongoTools
    {
        
        public static IMongoDatabase connect(string connectionString, string dbName)
        {
            
            var client = new MongoClient(connectionString);
            
            return client.GetDatabase(dbName);
        }
        public static List<string> collectionNames(IMongoDatabase db)
        {
            List<string> cnames = new List<string>();
            try
            {
                foreach (var item in db.ListCollectionsAsync().Result.ToListAsync<BsonDocument>().Result)
                {
                    cnames.Add(item["name"].ToString());
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
            return cnames;
        }
        public static void removeCollections()
        {
            IMongoDatabase db = MongoTools.connect("mongodb://localhost", "climaColombia");
            //clean up bog buc with PA variable
            List<string> collNames = MongoTools.collectionNames(db);
            foreach (string collection in collNames)
            {
                //all station record collections start with an s_
                string[] parts = collection.Split('_');
                string firstletter = parts[0];
                if(firstletter=="s")
                {
                    string vname = parts[4];
                    if (vname == "PA")
                    {
                        var coll = db.GetCollection<BsonDocument>(collection);
                        db.DropCollection(collection);
                    }

                }
            }
        }
        public static void removeCollectionsAnnual()
        {
            IMongoDatabase db = MongoTools.connect("mongodb://localhost", "climaColombia");
            //clean up bog buc with PA variable
            List<string> collNames = MongoTools.collectionNames(db);
            foreach (string collection in collNames)
            {
                if (collection.Contains("annual"))
                {
                        var coll = db.GetCollection<BsonDocument>(collection);
                        var t = coll.Find(new BsonDocument()).ToList();
                        //db.DropCollection(collection);
                }
            }
        }
        public static List<StationSummary> getCollectionAsList(string collectionname)
        {
            IMongoDatabase db = MongoTools.connect("mongodb://localhost", "climaColombia");
            
            var coll = db.GetCollection<StationSummary>(collectionname);
            var t = coll.Find(FilterDefinition<StationSummary>.Empty).ToList();
            //db.DropCollection(collection);
            return t;
        }
        public static void storeSummaryCollectionName(IMongoDatabase db,string nametostore)
        {
            var collection = db.GetCollection<BsonDocument>("summaryCollectionNames");
            var document = new BsonDocument
            {
                {"name",nametostore},
                {"time", DateTime.Now }
            };
            collection.InsertOne(document);
        }
        public static List<int> distinct(string collectionName, IMongoDatabase db,string keystring)
        {
            //return distinct values in collection
            var collection = db.GetCollection<BsonDocument>(collectionName);
            var f1 = new BsonDocument();
            IList<Int32> distinct = collection.Distinct<Int32>(keystring, f1).ToList<Int32>();
            return distinct.ToList();
        }
    }
}
