using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DataETL
{
    class Reader
    {
        List<StationRecord> stationMetaCollection = new List<StationRecord>();
        List<StationVariables> stationVariablesCollections = new List<StationVariables>();

        string[] folders = {
            @"D:\WORK\piloto\Climate\NOAA\data\variables"
        };
        public Reader()
        {
            CSVtoMongo importData = new CSVtoMongo();

            importData.connect("mongodb://localhost", "climaColombia");
            
            foreach (string folder in folders)
            {
                
                char split = ',';
                string[] files = Directory.GetFiles(folder);
                
               importData.loopCSVData(folder, split);
                
            }
        }
        private void readNOAA(string[] files)
        {
            string source = "NOAA";
            int linesprocessed = 0;
            foreach (string file in files)
            {
                string filename = Path.GetFileName(file);
                string variableName = Path.GetFileNameWithoutExtension(file);
                if (filename.Contains(".csv"))
                {
                    StreamReader sr = new StreamReader(file);
                    string line = sr.ReadLine();
                    while(line!=null)
                    {
                        string[] bits = line.Split(',');
                        //define the record
                        SingleRecord singleRecord = new SingleRecord();
                        singleRecord.source = source;
                        singleRecord.value = Convert.ToDouble(bits[2]);
                        singleRecord.variableName = variableName;
                        insertSingleRecord(singleRecord, Convert.ToDateTime(bits[1]), Convert.ToInt32(bits[0]), source);
                        linesprocessed++;
                        line = sr.ReadLine();
                    }
                    sr.Close();
                }
            }
        }
        private void insertSingleRecord(SingleRecord record, DateTime dt, int code, string source)
        {
            //find station source
            if (stationVariablesCollections.Exists(x => x.stationcode == code && x.source == source) )
            {
                //does datetime exsit in station_source
                var station_source = stationVariablesCollections.Find(x => x.stationcode == code && x.source == source);
                if (station_source.weatherRecords.Exists(x=>x.time==dt))
                {
                    var recordGroup = station_source.weatherRecords.Find(x => x.time == dt);
                    recordGroup.variables.Add(record);
                }
                else
                {
                    //new record
                    WeatherRecord wr = new WeatherRecord();
                    wr.time = dt;
                    wr.variables.Add(record);
                    //add to station_source
                    station_source.weatherRecords.Add(wr);
                }
            }
            else
            {
                //create station source
                StationVariables sv = new StationVariables();
                sv.source = source;
                sv.stationcode = code;
                //create first record
                WeatherRecord wr = new WeatherRecord();
                wr.time = dt;
                wr.variables.Add(record);
                sv.weatherRecords.Add(wr);
                //add to the collections set
                stationVariablesCollections.Add(sv);
            }
        }
        
    }
}
