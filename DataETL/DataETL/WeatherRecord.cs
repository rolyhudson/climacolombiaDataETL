using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataETL
{
    class WeatherRecord
    {
        public DateTime time { get; set; }
        //list of variables for the time stamp
        //order should be predefined
        public List<SingleRecord> variables { get; set; }
        public WeatherRecord()
        {
            variables = new List<SingleRecord>();
        }
    }
    class SingleRecord
    {
        public double value { get; set; }
        public string source { get; set; }
        public string variableName { get; set; }
    }
    class StationVariables
    {
        public int stationcode { get; set; }
        public string source { get; set; }
        public List<WeatherRecord> weatherRecords { get; set; }
        public StationVariables()
        {
            weatherRecords = new List<WeatherRecord>();
        }

    }
    
}
