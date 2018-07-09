using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StationGroups
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
        public static void writeCityGroups(List<CityGroup> citygroups)
        {
            StreamWriter sw = new StreamWriter(@"D:\WORK\piloto\webDev\tools\180622\citygroups.json");
            using (JsonTextWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartObject();
                writer.WritePropertyName("citygroups");
                writer.WriteStartArray();
                foreach (CityGroup c in citygroups)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("name");
                    writer.WriteValue(c.city.name);
                    writer.WritePropertyName("ele");
                    writer.WriteValue(Math.Round(c.city.elevation,2));
                    writer.WritePropertyName("lat");
                    writer.WriteValue(c.city.location[1]);
                    writer.WritePropertyName("lon");
                    writer.WriteValue(c.city.location[0]);
                    writer.WritePropertyName("localstations");
                    writer.WriteStartArray();
                    foreach(Result res in c.localStations.stations)
                    {
                        writer.WriteStartObject();
                        writer.WritePropertyName("name");
                        writer.WriteValue(res.name);
                        writer.WritePropertyName("code");
                        writer.WriteValue(res.code);
                        writer.WritePropertyName("ele");
                        writer.WriteValue(Math.Round(res.elevation,2));
                        writer.WritePropertyName("lat");
                        writer.WriteValue(res.location[1]);
                        writer.WritePropertyName("lon");
                        writer.WriteValue(res.location[0]);
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
        public static void writeRegional(List<StationGroup> regionalStations)
        {

            StreamWriter sw = new StreamWriter(@"D:\WORK\piloto\webDev\tools\180622\regionalstations.json");

            using (JsonTextWriter writer = new JsonTextWriter(sw))
            {

               writer.Formatting = Formatting.Indented;
                writer.WriteStartObject();
                writer.WritePropertyName("regions");
                writer.WriteStartArray();
                
                foreach (StationGroup r in regionalStations)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("name");
                    writer.WriteValue(r.name);
                    writer.WritePropertyName("stations");
                    writer.WriteStartArray();
                    foreach(Result res in r.stations)
                    {
                        writer.WriteStartObject();
                        writer.WritePropertyName("name");
                        writer.WriteValue(res.name);
                        writer.WritePropertyName("code");
                        writer.WriteValue(res.code);
                        writer.WritePropertyName("ele");
                        writer.WriteValue(Math.Round(res.elevation,2));
                        writer.WritePropertyName("lat");
                        writer.WriteValue(res.location[1]);
                        writer.WritePropertyName("lon");
                        writer.WriteValue(res.location[0]);
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
