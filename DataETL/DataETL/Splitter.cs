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
using System.Windows.Forms;

namespace DataETL
{
    class Splitter
    {
        IMongoDatabase db;
        private void connect(string connectionString, string dbName)
        {
            db = MongoTools.connect(connectionString, dbName);
        }
        public async Task splitVariables(string dbname)
        {
            connect("mongodb://localhost", dbname);

            List<string> collNames = MongoTools.collectionNames(db);
            await splitLoop(collNames);
            MessageBox.Show("Finsihed");
        }
        private async Task splitLoop(List<string> collNames)
        {
            foreach (string collection in collNames)
            {
                splitter(collection);
            }
        }
        private async Task splitter(string collectionname)
        {
            List<int> codes = MongoTools.distinct(collectionname, db, "stationCode");
            IMongoCollection<BsonDocument> variableCollection = db.GetCollection<BsonDocument>(collectionname);
            FindOptions<BsonDocument> options = new FindOptions<BsonDocument>
            {
                BatchSize = 1000,
                NoCursorTimeout = false
            };
            foreach (int stationcode in codes)
            {
                //get or make a station collection
                IMongoCollection<BsonDocument> stationVariableCollection = db.GetCollection<BsonDocument>("s_"+stationcode + "_" + collectionname);
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("stationCode", stationcode);
                //find in the variable collection
                using (IAsyncCursor<BsonDocument> cursor = await variableCollection.FindAsync(filter, options))
                {

                    while (await cursor.MoveNextAsync())
                    {

                        IEnumerable<BsonDocument> documents = cursor.Current;
                        //insert into the station collection
                        await stationVariableCollection.InsertManyAsync(documents);
                    }
                }
            }
        }
    }
}
