using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
namespace DataETL
{
    class IndexStationVariableCollections
    {
        IMongoDatabase db;
        public IndexStationVariableCollections()
        {
            db = MongoTools.connect("mongodb://localhost/?maxPoolSize=3000", "climaColombia");
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
                    createCollectionIndex(collection);
                    
                }
            }

        }
        private async Task createCollectionIndex(string collectionname)
        {
            IMongoCollection<RecordMongo> stationVariable = db.GetCollection<RecordMongo>(collectionname);           
            IndexKeysDefinition<RecordMongo> keysDef = "{ time: 1 }";
            var indexmodel = new CreateIndexModel<RecordMongo>(keysDef, new CreateIndexOptions() { Unique = false });
            try {
                var indexed = stationVariable.Indexes.CreateOne(indexmodel);
                var wtf = indexed;
            }
            catch(MongoCommandException e)
            {
                var caught = e;

            }
        }

    }
}
