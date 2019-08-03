using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataETL
{
    class EPWsummary
    {
        EPWFile epwFormat = new EPWFile();
        List<EPWFile> toCompare = new List<EPWFile>();
        public EPWsummary()
        {
            epwFormat = defineFormat();
            printFormat();
        }
        public void getComparisons(string[] files)
        {
            foreach(string f in files)
            {
                toCompare.Add(readEPW(f));
            }
            printSummary();
        }
        private void printSummary()
        {
            StreamWriter sw = new StreamWriter(@"C: \Users\Admin\Documents\projects\IAPP\piloto\Climate\epwComparison\comparison.csv", false, Encoding.UTF8);
            List<string> filenames = toCompare.Select(p => p.filename).ToList();
            string line = "field code";
            filenames.ForEach(x => line += "," + x);
            sw.WriteLine(line);
            foreach (EPWGroup g in epwFormat.groups)
            {
                sw.WriteLine(g.name);
                int fieldNum = 0;
                foreach (EPWField f in g.fields)
                {
                    line = f.name;
                    foreach (EPWFile file in toCompare)
                    {
                        if(g.name=="ACTUAL DATA")
                        {
                            line += "," + file.missingdata[fieldNum].ToString();
                        }
                        else
                        {
                            var field = file.groups.Find(x => x.name == g.name).fields.Find(p => p.name == f.name);
                            line += "," + field.value;
                        }
                        
                    }
                    fieldNum++;
                    sw.WriteLine(line);
                }
                
            }
            line = "readErrors,";
            foreach (EPWFile file in toCompare)
            {
                file.unparsable.ForEach(x => line += "_" + x);
                line += ",";
            }
            sw.WriteLine(line);
            sw.Close();
        }
        private void printFormat()
        {
            StreamWriter sw = new StreamWriter(@"C: \Users\Admin\Documents\projects\IAPP\piloto\Climate\epwComparison\format.csv",false, Encoding.UTF8);
            foreach(EPWGroup g in epwFormat.groups)
            {
                sw.WriteLine(g.name);
                foreach(EPWField f in g.fields)
                {
                    string line = f.name;
                    
                    foreach(KeyValuePair<string,string> kvp in f.keyValues)
                    {
                        line += ","+kvp.Key ;
                        line += "," + kvp.Value;
                    }
                    sw.WriteLine(line);
                }
            }
            sw.Close();
        }
        private EPWFile readEPW(string file)
        {
            StreamReader sr = new StreamReader(file);
            var data = defineFormat();
            data.filename = Path.GetFileName(file);
            string line = sr.ReadLine();
            int lineCount = 0;
            while (line != null)
            {
                var parts = line.Split(',');
                var groupName = parts[0].Split(' ');
                var group = data.groups.Find(x => x.name==parts[0]);
                //if none found...its a data field
                //or the name is not recognised as a group
                try
                {
                    if (group != null)
                    {
                        switch (group.name)
                        {
                            case "COMMENTS 1":
                                for (int i = 1; i < parts.Length; i++)
                                {
                                    group.fields[0].value += parts[i] + "_";
                                }
                                break;
                            case "COMMENTS 2":
                                for (int i = 1; i < parts.Length; i++)
                                {
                                    group.fields[0].value += parts[i] + "_";
                                }
                                break;
                            case "DESIGN CONDITIONS":
                                CultureInfo culture = new CultureInfo("es-ES", false);

                                group.fields[0].value = parts[1];
                                group.fields[1].value = parts[2];

                                int index = 0;
                                for (int i = 3; i < parts.Length; i++)
                                {

                                    //is it a number
                                    double num = -1;
                                    if (Double.TryParse(parts[i], out num))
                                    {
                                        group.fields[index].value += parts[i] + "_";
                                    }
                                    else
                                    {
                                        //its text
                                        if (culture.CompareInfo.IndexOf(parts[i], "heating", CompareOptions.IgnoreCase) >= 0)
                                        {
                                            index = 2;
                                        }
                                        if (culture.CompareInfo.IndexOf(parts[i], "cooling", CompareOptions.IgnoreCase) >= 0)
                                        {
                                            index = 3;
                                        }
                                        if (culture.CompareInfo.IndexOf(parts[i], "extreme", CompareOptions.IgnoreCase) >= 0)
                                        {
                                            index = 4;
                                        }

                                    }
                                }
                                break;
                            default:
                                for (int i = 1; i < parts.Length; i++)
                                {
                                    try
                                    {
                                        group.fields[i - 1].value = parts[i];
                                        //the min format includes minimal set of fields
                                        //some epw double fields can be repeated so in case we find repeats catch
                                    }
                                    catch
                                    {

                                    }

                                }
                                break;
                        }

                    }
                    else
                    {
                        //probably we have a data row starting with a year
                        group = data.groups.Find(x => x.name.Contains("ACTUAL DATA"));
                        //should be 35 fields 6 to 34 have a missing value
                        for (int i = 0; i < group.fields.Count; i++)
                        {
                            //0 to 5 cannot be empty
                            if (i <= 5)
                            {
                                if (parts[i] == "") data.missingdata[i]++;
                            }
                            else
                            {
                                var missing = group.fields[i].keyValues.Find(p => p.Key == "missing");
                                if (parts[i] == "")
                                {
                                    data.missingdata[i]++;
                                }
                                else
                                {
                                    var max = Convert.ToDouble(missing.Value);
                                    var given = Convert.ToDouble(parts[i]);
                                    if (given >= max) data.missingdata[i]++;
                                }

                            }
                        }
                    }
                }
                catch
                {
                    data.unparsable.Add(lineCount);
                }

                line = sr.ReadLine();
                lineCount++;
            }
            sr.Close();
            return data;
        }
        private EPWFile defineFormat()
        {
            EPWFile epw = new EPWFile();
            StreamReader sr = new StreamReader(@"C:\Users\Admin\Documents\projects\IAPP\piloto\Climate\ClimateDataETL\epwFormat.txt");
            string line = sr.ReadLine();
            EPWGroup g = new EPWGroup();
            EPWField f = new EPWField();
            while (line != null)
            {
                var parts = line.Split(',');
                if (parts.Length==1)
                {
                    g = new EPWGroup();
                    g.name = parts[0];
                    epw.groups.Add(g);
                }
                else
                {
                    if(parts[0].Contains("A")|| parts[0].Contains("N"))
                    {
                        f = new EPWField();
                        f.name = parts[0];
                        g.fields.Add(f);
                        f.keyValues.Add(new KeyValuePair<string, string>(parts[1], parts[2]));
                    }
                    else
                    {
                       f.keyValues.Add(new KeyValuePair<string, string>(parts[0], parts[1]));
                    }
                }
                line = sr.ReadLine();
            }
            sr.Close();
            return epw;
        }
        
    }
    class EPWFile
    {
        public EPWFile()
        {
            groups = new List<EPWGroup>();
        }
        public EPWFile(EPWFile other)
        {
            groups = new List<EPWGroup>(other.groups);
        }
        public List<EPWGroup> groups;
        public List<int> unparsable = new List<int>();
        public string filename { get; set; }
        public int[] missingdata = new int[35];
    }
    class EPWGroup
    {
        public string name { get; set; }
        public List<EPWField> fields=new List<EPWField>();
    }
    class EPWField
    {
        public string name { get; set; }
        public string value { get; set; }
        public List<KeyValuePair<string, string>> keyValues = new List<KeyValuePair<string, string>>();
        public EPWField()
        {
            value = "";
        }
    }
}
