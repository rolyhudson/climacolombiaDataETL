using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataETL
{
    class StationRecord
    {
        public string name { get; set; }
        public double[] location { get; set; }
        public string source { get; set; }
        public int code { get; set; }
        public string closestNOAA { get; set; }
    }
}
