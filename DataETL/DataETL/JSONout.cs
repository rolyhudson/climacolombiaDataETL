using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataETL
{
    class JSONout
    {
        public static void regionsToGEOJSON(List<Region> regions)
        {
            StreamWriter sw = new StreamWriter(@"D:\WORK\piloto\webDev\tools\180622\regionsGEOJSON.json");
            using (JsonTextWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartObject();
                writer.WritePropertyName("type");
                writer.WriteValue("FeatureCollection");
                writer.WritePropertyName("features");
                writer.WriteStartArray();
                foreach(Region r in regions)
                {
                    writer.WriteStartObject();
                    //feature info here
                    writer.WritePropertyName("type");
                    writer.WriteValue("Feature");
                    writer.WritePropertyName("geometry");
                    writer.WriteStartObject();
                    
                    writer.WritePropertyName("type");
                    writer.WriteValue("Polygon");
                    writer.WritePropertyName("coordinates");
                    writer.WriteStartArray();
                    writer.WriteStartArray();
                    
                    for(int i=0;i<r.vertices.Count;i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(r.vertices[i][0]);
                        writer.WriteValue(r.vertices[i][1]);
                        
                        writer.WriteEndArray();

                    }
                    writer.WriteEndArray();
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            sw.Close();
        }
        
        public static void writeGroup(List<StationGroup> groups, string file,List<Station> stations, List<City> cities)
        {
            string propname = "cities";
            if(file.Contains("region")) propname = "regions";
           
            StreamWriter sw = new StreamWriter(file);
            using (JsonTextWriter writer = new JsonTextWriter(sw))
            {

                writer.Formatting = Formatting.Indented;
                writer.WriteStartObject();
                writer.WritePropertyName(propname);
                writer.WriteStartArray();
                foreach (StationGroup sg in groups)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("name");
                    writer.WriteValue(sg.name);
                    if(propname=="cities")
                    {
                        var city = cities.Find(x => x.name == sg.name);
                        if (city == null) city = new City();
                        writer.WritePropertyName("ele");
                        writer.WriteValue(Math.Round(city.elevation, 2));
                        writer.WritePropertyName("lat");
                        writer.WriteValue(city.location[1]);
                        writer.WritePropertyName("lon");
                        writer.WriteValue(city.location[0]);
                    }
                    writer.WritePropertyName("stations");
                    writer.WriteStartArray();
                    foreach(int scode in sg.stationcodes)
                    {
                        Station s = new Station();
                        s = stations.Find(x => x.code == scode);
                        if (s == null) s = new Station();
                        writer.WriteStartObject();
                        
                        writer.WritePropertyName("code");
                        writer.WriteValue(scode);

                        writer.WritePropertyName("name");
                        writer.WriteValue(s.name);
                        
                        writer.WritePropertyName("ele");
                        writer.WriteValue(Math.Round(s.elevation, 2));
                        writer.WritePropertyName("lat");
                        writer.WriteValue(s.latitude);
                        writer.WritePropertyName("lon");
                        writer.WriteValue(s.longitude);
                        writer.WriteEndObject();

                    }
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }
                
                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            sw.Close();

        }
    }
}
