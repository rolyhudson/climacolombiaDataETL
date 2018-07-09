using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Device.Location;

namespace StationGroups
{
    class StationReader
    {
        List<StationMeta> stationMeta = new List<StationMeta>();
        List<Result> stationResults = new List<Result>();
        List<Region> regions = new List<Region>();
        List<City> cities = new List<City>();
        List<CityGroup> citygroups = new List<CityGroup>();
        List<StationGroup> stationsByRegion = new List<StationGroup>();
        public StationReader()
        {
            getStationList();
            readResults();
            regions = MapTools.readRegions();
            JSONout.regionsToGEOJSON(regions);
            //set up regional groups
            StationGroup sg = new StationGroup();
            foreach (Region r in regions)
            {
                sg = new StationGroup();
                sg.name = r.name;
                stationsByRegion.Add(sg);
            }
            sg = new StationGroup();
            sg.name = "outside";
            stationsByRegion.Add(sg);

            groupStationsByRegion();

            cities = MapTools.readCities();
                //set up city groups
            foreach(City city in cities)
            {
                CityGroup cg = new CityGroup();
                cg.city = city;
                citygroups.Add(cg);
            }

            addRegionGroup();
            getLocalStations();
            outputNumeric();
            outputRegionalGroups();
            JSONout.writeRegional(stationsByRegion);
            JSONout.writeCityGroups(citygroups);
        }
        private void outputRegionalGroups()
        {
            StreamWriter sw = new StreamWriter(@"D:\WORK\piloto\Climate\ClimateDataETL\stationGroups\regionalStationGroups.csv", false, Encoding.UTF8);
            foreach (StationGroup rg in stationsByRegion)
            {
                sw.WriteLine("regional stations: " + rg.name + ",ele,lon,lat,DV,HR,NUB,PRE,RDIR,T,VV,AVG");
                foreach (Result r in rg.stations)
                {
                    sw.Write(r.name + "," + r.elevation + "," + r.location[0].ToString() + "," + r.location[1].ToString() + ",");
                    for (int i = 0; i < r.variables.Count; i++)
                    {
                        if (i == r.variables.Count - 1) sw.WriteLine(r.variables[i].value.ToString());
                        else sw.Write(r.variables[i].value.ToString() + ",");
                    }
                }
            }
            sw.Close();
        }
        private void outputNumeric()
        {
            StreamWriter sw = new StreamWriter(@"D:\WORK\piloto\Climate\ClimateDataETL\stationGroups\stationGroups.csv",false,Encoding.UTF8);
            foreach (CityGroup cg in citygroups)
            {
                sw.WriteLine(cg.city.name + "," + cg.city.regionName + "," + cg.city.elevation + "," + cg.city.location[0].ToString() + "," + cg.city.location[1].ToString());
                sw.WriteLine(",local stations:,ele,lon,lat,DV,HR,NUB,PRE,RDIR,T,VV,AVG");
                foreach (Result r in cg.localStations.stations)
                {
                    sw.Write("," + r.name + "," + r.elevation + "," + r.location[0].ToString() + "," + r.location[1].ToString() + ",");
                    for(int i=0;i< r.variables.Count;i++)
                    {
                        if(i==r.variables.Count-1) sw.WriteLine(r.variables[i].value.ToString() );
                        else sw.Write(r.variables[i].value.ToString() + ",");
                    }
                }
                
            }

            sw.Close();
        }
        private void addRegionGroup()
        {
            foreach(CityGroup cg in citygroups)
            {
                foreach(Region r in regions)
                {
                    if(MapTools.isPointInPolygon(cg.city.location, r.vertices))
                    {
                        //add the region group name to city
                        cg.city.regionName = r.name;
                        break;
                    }
                }
            }
        }
        private void groupStationsByRegion()
        {
            
            foreach (Result res in stationResults)
            {
                bool inside = false;
                foreach (Region r in regions)
                {
                    if (MapTools.isPointInPolygon(res.location,r.vertices))
                    {
                        inside = true;
                        var group = stationsByRegion.Find(x => x.name == r.name);
                        group.stations.Add(res);
                        break;
                    }
                }
                //for the ones that fall outside
                if (!inside)
                {
                    var groupOut = stationsByRegion.Find(x => x.name == "outside");
                    groupOut.stations.Add(res);
                   
                }
            }
        }
        private void getLocalStations()
        {
            foreach (CityGroup cg in citygroups)
            {
                StationGroup sg = new StationGroup();
                sg.name = cg.city.name;
                //point as geocoord lat lon
                var cityCoord = new GeoCoordinate(cg.city.location[1], cg.city.location[0]);
                double dist = 0;
                
                double eleDiff = 0;
                
                foreach (Result res in stationResults)
                {
                    //find within radius altitude
                    var resCoord = new GeoCoordinate(res.location[1], res.location[0]);
                    dist = cityCoord.GetDistanceTo(resCoord);
                    eleDiff = cg.city.elevation - res.elevation;
                    if(dist<50000&&Math.Abs(eleDiff)<100)
                    {
                        sg.stations.Add(res);
                    }
                }
                cg.localStations = sg;
            }
        }
        private void getStationList()
        {
            StreamReader sr = new StreamReader("D:\\WORK\\piloto\\Climate\\IDEAM\\Estaciones_del_IDEAM.csv");
            string line = sr.ReadLine();
            line = sr.ReadLine();
            while (line != null)
            {
                string[] chunks = line.Split(',');
                StationMeta sm = new StationMeta();
                sm.areaoperativa = chunks[0];
                sm.codigo = Convert.ToInt32(chunks[1]);
                sm.nombre = chunks[2];
                sm.clase = chunks[3];
                sm.categoria = chunks[4];
                sm.estado = chunks[5];
                sm.departamento = chunks[6];
                sm.municipio = chunks[7];
                sm.corriente = chunks[8];
                sm.lat = chunks[9];
                sm.lon = chunks[10];
                sm.altitud = chunks[11].Replace(".", string.Empty);
                sm.fecha_instalacion = chunks[12];
                sm.fecha_suspension = chunks[13];
                stationMeta.Add(sm);
                line = sr.ReadLine();

            }
            sr.Close();
        }
        private void readResults()
        {
            //reading johns results
            StreamReader sr = new StreamReader(@"D:\WORK\piloto\Climate\ClimateDataETL\stationGroups\Resultados.csv");
            string line = sr.ReadLine();
            string[] bits = line.Split(',');
            List<string> vNames = bits.ToList();
            line = sr.ReadLine();
            int firstBrak = 0;
            int lastBrak = 0;
            
            while (line!=null)
            {
                
                bits = line.Split(',');
                Result res = new Result();
                for (int i=0;i<bits.Length;i++)
                {
                    if(i==0)
                    {
                        firstBrak = bits[i].IndexOf('[');
                        lastBrak = bits[i].LastIndexOf(']');
                        res.name = bits[i].Substring(0, firstBrak);
                        
                        res.code = Convert.ToInt32(bits[i].Substring(firstBrak+1, lastBrak-firstBrak-1));
                    }
                    else
                    {
                        NameValue nv = new NameValue();
                        nv.name = vNames[i - 1];
                        nv.value = Convert.ToDouble(bits[i]);
                        res.variables.Add(nv);
                    }
                    
                }
                getStationLocation(res);
                stationResults.Add(res);
                line = sr.ReadLine();
            }
            sr.Close();
        }
        private void getStationLocation(Result station)
        {
            
                for (int i = 0; i < stationMeta.Count; i++)
                {
                    if (station.code == stationMeta[i].codigo)
                    {
                        station.location[0] = Convert.ToDouble(stationMeta[i].lon);
                        station.location[1] = Convert.ToDouble(stationMeta[i].lat);
                        station.elevation = Convert.ToDouble(stationMeta[i].altitud);
                        break;
                    }
                }
        }
    }
    public class Result
    {
        public string name { get; set; }
        public int code { get; set; }
        
        public List<NameValue> variables { get; set; }
        public double[] location { get; set; }
        public double elevation { get; set; }
        public Result()
        {
            variables = new List<NameValue>();
            location = new double[2];
        }
    }
    public class StationGroup
    {
        public string name { get; set; }
        public List<Result> stations { get; set; }
        public StationGroup()
        {
            stations = new List<Result>();
        }
    }
    public class NameValue
    {
        public string name { get; set; }
        public double value { get; set; }
    }
    
    public class StationMeta
    {
        //for the data of all stations
        public string areaoperativa;
        public int codigo;
        public string nombre;
        public string clase;
        public string categoria;
        public string estado;
        public string departamento;
        public string municipio;
        public string corriente;

        public string lat;
        public string lon;
        public string altitud;
        public string fecha_instalacion;
        public string fecha_suspension;
    }
    public class CityGroup
    {
        public City city { get; set; }
        
        public StationGroup localStations { get; set; }
        public CityGroup()
        {
            
            localStations = new StationGroup();
        }
    }
}
