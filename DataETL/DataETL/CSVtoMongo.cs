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
    class CSVtoMongo
    {
        IMongoDatabase db;
        public void connect(string connectionString, string dbName)
        {
            db = MongoTools.connect(connectionString, dbName);
        }
        public void loopCSVData(string folder, char split)
        {
            string[] files = Directory.GetFiles(folder);
            foreach (string file in files)
            {
                if (Path.GetExtension(file).Contains("csv"))
                {
                    string collectionName = Path.GetFileNameWithoutExtension(file);
                    
                        db.CreateCollection(collectionName);
                        insertManyRecord(collectionName, file, split);
                }
            }
        }
        public void insertManyRecord(string collectionName, string file, char split)
        {
            var collection = db.GetCollection<RecordMongo>(collectionName);
            var listOfDocuments = new List<RecordMongo>();
            var limitAtOnce = 1000;
            var current = 0;
            StreamReader read = new StreamReader(file);
            string line = read.ReadLine();
            string[] prts;
            while (line != null)
            {

                prts = line.Split(split);
                
                var dataToInsert = new RecordMongo
                {
                    stationCode = Convert.ToInt32(prts[0]),
                    time = Convert.ToDateTime(prts[1]),
                    value = Convert.ToDouble(prts[2])
                };
                if (collectionName.Contains("NOAA"))
                {
                    //noaa uses UTC so this subtracts 5 hours to bogota
                    dataToInsert.time = dataToInsert.time.ToLocalTime();
                }
                else
                {
                    if (collectionName.Contains("variable"))
                    {
                        //ideams date stamp is local -5!!!!
                        dataToInsert.time = dataToInsert.time.AddHours(5);
                        //set as local
                        dataToInsert.time = DateTime.SpecifyKind(dataToInsert.time, DateTimeKind.Local);
                    }
                    else
                    {
                        dataToInsert.time = DateTime.SpecifyKind(dataToInsert.time, DateTimeKind.Local);
                    }
                }
                listOfDocuments.Add(dataToInsert);

                if (++current == limitAtOnce)
                {
                    current = 0;

                    var listToInsert = listOfDocuments;

                    var t = new Task(() => { collection.InsertManyAsync(listToInsert); });
                    t.Start();
                    listOfDocuments = new List<RecordMongo>();
                }
                line = read.ReadLine();
            }

            // insert remainder
            //await collection.InsertManyAsync(listOfDocuments);
            var f = new Task(() => { collection.InsertManyAsync(listOfDocuments); });
            f.Start();
            read.Close();
        }
    }
    public class RecordMongo
    {
        public ObjectId _id
        {
            get;
            set;
        }

        public int stationCode
        {
            get;
            set;
        }
        public double value
        {
            get;
            set;
        }
        public DateTime time
        {
            get;
            set;
        }
        public string processNote
        {
            get;
            set;
        }
    }
}
