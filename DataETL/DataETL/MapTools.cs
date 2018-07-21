using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataETL
{
    class MapTools
    {
        public static bool isPointInPolygon(double[] point, List<double[]> vs)
        {
            // ray-casting algorithm based on
            // http://www.ecse.rpi.edu/Homepages/wrf/Research/Short_Notes/pnpoly.html

            double x = point[0], y = point[1];

            bool inside = false;
            for (int i = 0, j = vs.Count - 1; i < vs.Count; j = i++)
            {
                double xi = vs[i][0], yi = vs[i][1];
                double xj = vs[j][0], yj = vs[j][1];

                bool intersect = ((yi > y) != (yj > y))
                    && (x < (xj - xi) * (y - yi) / (yj - yi) + xi);
                if (intersect) inside = !inside;
            }

            return inside;
        }
        private static void rewriteCities(List<City> cities)
        {
            StreamWriter sw = new StreamWriter(@"D:\WORK\piloto\Climate\ClimateDataETL\stationGroups\cities.csv");
            foreach(City c in cities)
            {
                sw.WriteLine(c.location[0].ToString() + "," + c.location[1].ToString() + "," + c.name + "," + c.elevation.ToString());
            }
            sw.Close();
        }
        public static List<City> readCities()
        {
            List<City> cities = new List<City>();
            StreamReader sr = new StreamReader(@"D:\WORK\piloto\Climate\ClimateDataETL\stationGroups\cities.csv");
            string line = sr.ReadLine();
            while (line != null)
            {
                City city = new City();
                string[] bits = line.Split(',');
                city.name = bits[2];
                city.location[0] = Convert.ToDouble(bits[0]);
                city.location[1] = Convert.ToDouble(bits[1]);
                //city.elevation = getElevationFromGoogle(city.location);
                city.elevation = Convert.ToDouble(bits[3]);
                cities.Add(city);
                line = sr.ReadLine();
            }
            sr.Close();
            //writeCitiesToJSON(cities);
            //rewriteCities(cities);
            return cities;
        }
        private static void writeCitiesToJSON(List<City> cities)
        {
            StreamWriter sw = new StreamWriter(@"D:\WORK\piloto\webDev\data\json\colombiaCapitalCities.json");
            sw.Write("[");
            for(int i=0;i<cities.Count;i++)
            {
                //{"code":14019030,"name":"ESC NAVAL CIOH AUTOM [14019030]","latitude":"10.390563888889","longitude":" -75.533830555556","altitude":"1"},
                if(i==cities.Count-1) sw.Write("{\"code\":" + "\"0000\"," +
                    "\"name\":" + "\""+cities[i].name+"\""+", " +
                    "\"latitue\":" + "\"" + cities[i].location[1].ToString() + "\"" + ", " +
                    "\"longitude\":" + "\"" + cities[i].location[0].ToString() + "\"" + ", " +
                    "\"altitude\":" + "\"" + cities[i].elevation.ToString() + "\"" + "}");
                else
                    sw.Write("{\"code\":" + "\"0000\"," +
                    "\"name\":" + "\"" + cities[i].name + "\"" + ", " +
                    "\"latitude\":" + "\"" + cities[i].location[1].ToString() + "\"" + ", " +
                    "\"longitude\":" + "\"" + cities[i].location[0].ToString() + "\"" + ", " +
                    "\"altitude\":" + "\"" + cities[i].elevation.ToString() + "\"" + "},");

            }
            sw.Write("]");
            sw.Close();
        }
        private static double getElevationFromGoogle(double[] location)
        {
            double ele = 0;
            string key = "AIzaSyBD3lJf0hrnvRZJn6g4bm6FEUljZ9FcEeE";
            string lat = location[1].ToString();
            string lon = location[0].ToString();
            //https://maps.googleapis.com/maps/api/elevation/json?locations=39.7391536,-104.9847034&key=YOUR_API_KEY
            string url = "https://maps.googleapis.com/maps/api/elevation/json?locations=" + lat + "," + lon + "&key=" + key;
            WebClient client = new WebClient();
            string value = client.DownloadString(url);
            JObject data = new JObject();
            using (JsonTextReader reader = new JsonTextReader(new StringReader(value)))
            {
                data = (JObject)JToken.ReadFrom(reader);
                ele = (double)data.SelectToken("results[0].elevation");
            }
            return ele;
        }

        public static List<Region> readRegions()
        {
            StreamReader sr = new StreamReader(@"D:\WORK\piloto\Climate\ClimateDataETL\stationGroups\regions.csv");
            List<Region> regions = new List<Region>();
            string line = sr.ReadLine();
            while (line != null)
            {
                Region region = new Region();
                List<double[]> coords = new List<double[]>();
                string[] bits = line.Split(',');
                double[] p = new double[2];
                for (int i=0;i<bits.Length;i++)
                {
                    if(i==0)
                    {
                        region.name = bits[i];
                    }
                    else
                    {
                        if(i%2!=0)
                        {
                            //lon                      
                            p[0] = Convert.ToDouble(bits[i]);
                            
                        }
                        else
                        {
                            //lat
                            p[1] = Convert.ToDouble(bits[i]);
                            coords.Add(p);
                            p = new double[2];
                        }
                    }
                }
                region.vertices = coords;
                regions.Add(region);
                line = sr.ReadLine();
            }

            sr.Close();
            return regions;
        }
        static public double getStandardPressure(double altitude/* meters */)   // Returns result in Pascals
        {
            // Below 51 km: Practical Meteorology by Roland Stull, pg 12
            // Above 51 km: http://www.braeunig.us/space/atmmodel.htm
            // Validation data: https://www.avs.org/AVS/files/c7/c7edaedb-95b2-438f-adfb-36de54f87b9e.pdf

            altitude = altitude / 1000.0;  // Convert m to km
            double geopot_height = getGeopotential(altitude);

            double t = getStandardTemperature(geopot_height);

            if (geopot_height <= 11)
                return 101325 * Math.Pow(288.15 / t, -5.255877);
            else if (geopot_height <= 20)
                return 22632.06 * Math.Exp(-0.1577 * (geopot_height - 11));
            else if (geopot_height <= 32)
                return 5474.889 * Math.Pow(216.65 / t, 34.16319);
            else if (geopot_height <= 47)
                return 868.0187 * Math.Pow(228.65 / t, 12.2011);
            else if (geopot_height <= 51)
                return 110.9063 * Math.Exp(-0.1262 * (geopot_height - 47));
            else if (geopot_height <= 71)
                return 66.93887 * Math.Pow(270.65 / t, -12.2011);
            else if (geopot_height <= 84.85)
                return 3.956420 * Math.Pow(214.65 / t, -17.0816);

            //throw std::out_of_range("altitude must be less than 86 km.");
            return -1;
        }

        // geopot_height = earth_radius * altitude / (earth_radius + altitude) /// All in km
        // Temperature is in Kelvin = 273.15 + Celsius
        static double getStandardTemperature(double geopot_height)
        {
            // Standard atmospheric pressure
            // Below 51 km: Practical Meteorology by Roland Stull, pg 12
            // Above 51 km: http://www.braeunig.us/space/atmmodel.htm

            if (geopot_height <= 11)          // Troposphere
                return 288.15 - (6.5 * geopot_height);
            else if (geopot_height <= 20)     // Stratosphere starts
                return 216.65;
            else if (geopot_height <= 32)
                return 196.65 + geopot_height;
            else if (geopot_height <= 47)
                return 228.65 + 2.8 * (geopot_height - 32);
            else if (geopot_height <= 51)     // Mesosphere starts
                return 270.65;
            else if (geopot_height <= 71)
                return 270.65 - 2.8 * (geopot_height - 51);
            else if (geopot_height <= 84.85)
                return 214.65 - 2 * (geopot_height - 71);
            // Thermosphere has high kinetic temperature (500 C to 2000 C) but temperature
            // as measured by a thermometer would be very low because of almost vacuum.

            //throw std::out_of_range("geopot_height must be less than 84.85 km.")
            return -1;
        }

        static double getGeopotential(double altitude_km)
        {
            double EARTH_RADIUS = 6356.766; // km

            return EARTH_RADIUS * altitude_km / (EARTH_RADIUS + altitude_km);
        }
    }
    public class City
    {
        public string name { get; set; }
        public string regionName { get; set; }
        public double[] location { get; set; }
        public double elevation { get; set; }
        public City()
        {
            location = new double[2];
        }
    }
    class Region
    {
        public string name { get; set; }
        public List<double[]> vertices { get; set; }
        public Region()
        {
            vertices = new List<double[]>();
        }
    }
}
