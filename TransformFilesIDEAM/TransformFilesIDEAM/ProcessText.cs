using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using System.Globalization;

namespace TransformFilesIDEAM
{
    class ProcessText
    {
        public List<VariableFile> sortedFiles { get; set; }
        private string folder;
        private string source;
        public ProcessText(string sourceFolder, string sourceOrg, string type)
        {

            sortedFiles = new List<VariableFile>();
            folder = sourceFolder;
            source = sourceOrg;
            switch (type)
            {
                case "radIDEAM":
                    processRad();
                    break;
                case "s_vIDEAM":
                    processStationVariable();
                    break;
                case "bog_bucIDEAM":
                    processBogBuc();
                    break;
                case "variableIDEAM":
                    processVariable();
                    break;
            }
        }
        
        private static void convertToCSV(string filename,string destinationFolder)
        {
            Application app = new Application();
            try
            {

            Workbook wbWorkbook = app.Workbooks.Open(filename);
            Worksheet ws = wbWorkbook.Worksheets[1];
            ws.SaveAs(destinationFolder+"\\"+Path.GetFileNameWithoutExtension(filename)+".csv", XlFileFormat.xlCSV);
            Marshal.ReleaseComObject(ws);
            }
            finally
            {
                app.Quit();
            }
        
        }
        public static void convertFolderOfXLSX(string folder)
        {
            string[] files = Directory.GetFiles(folder);
            foreach (string file in files)
            {
                if (Path.GetExtension(file) == ".xlsx")
                {
                    ProcessText.convertToCSV(file, folder);
                }
            }
        }
        private void processVariable()
        {
            string[] files = Directory.GetFiles(folder);
            
            foreach (string file in files)
            {
                if (Path.GetExtension(file) == ".csv")
                {
                    string filename = Path.GetFileNameWithoutExtension(file);
                    string vCode = filename.Substring(0, 2);
                    string freq = filename.Substring(3);
                    VariableFile vf = sortedFiles.Find(x => x.variableName == vCode);
                    if (vf == null)
                    {
                        vf = new VariableFile();
                        vf.source = source;
                        vf.variableName = vCode;
                        vf.freq = freq;
                        vf.subset = "variable";
                        sortedFiles.Add(vf);
                    }
                    StreamReader sr = new StreamReader(file);
                    string line = sr.ReadLine();
                    while (line != null)
                    {

                        string[] parts = line.Split('|');
                        Record r = new Record();
                        
                        DateTime dt = new DateTime();

                        if (DateTime.TryParse(parts[2], out dt))
                        {
                            r.datetime = dt;
                        }
                        else
                        {
                            r.datetime = dt;
                        }
                        r.stationCode = Convert.ToInt32(parts[0]);
                        double val;
                        if (parts.Length > 2)
                        {
                            parts[2].Replace(',', ',');
                            if (Double.TryParse(parts[3], out val))
                            { r.value = val; }
                        }
                        vf.records.Add(r);
                        line = sr.ReadLine();
                    }
                    sr.Close();
                }
            }
            printSorted();
        }
        private void processBogBuc()
        {
            string[] files = Directory.GetFiles(folder);
            
            foreach (string file in files)
            {
                
                if (Path.GetExtension(file) == ".txt")
                {
                    string filename = Path.GetFileNameWithoutExtension(file);
                    string vCode = filename.Substring(0, 2);
                    
                    int firstBar = filename.IndexOf("_");
                    int lastBar = filename.LastIndexOf("_");
                    string stationShortName = filename.Substring(firstBar+1, lastBar - firstBar-1);
                    string freq = filename.Substring(lastBar + 1);
                    
                    if (freq == "1d") continue;
                    if (freq == "10min") freq = "10";
                    if (freq == "1h") freq = "60";
                    VariableFile vf = sortedFiles.Find(x => x.variableName == vCode);
                    if (vf == null)
                    {
                        vf = new VariableFile();
                        vf.source = source;
                        vf.variableName = vCode;
                        vf.freq = freq;
                        vf.subset = "BogBuc";
                        sortedFiles.Add(vf);
                    }
                    int sCode = 0;
                    switch(stationShortName)
                    {
                        case "UNal":
                            sCode = 21205012;
                            break;
                        case "ApEDor":
                            sCode = 21205791;
                            break;
                        case "CBol":
                            sCode = 21206940;
                            break;
                        case "IdeamBog":
                            sCode = 21206960;
                            break;
                        case "NeoM":
                            sCode = 23195230;
                            break;
                        case "NvaGen":
                            sCode = 21206600;
                            break;
                        case "VTer":
                            sCode = 21206920;
                            break;
                    }
                    StreamReader sr = new StreamReader(file);
                    string line = sr.ReadLine();
                    while (line != null)
                    {

                        string[] parts = line.Split(';');
                        Record r = new Record();
                        string[] timecode = parts[1].Split(' ');
                        DateTime dt = new DateTime();

                        if (DateTime.TryParse(parts[0] + " " + timecode[0], out dt))
                        {
                            r.datetime = dt;
                        }
                        else
                        {
                            r.datetime = dt;
                        }
                        r.stationCode = sCode;
                        double val;
                        if (parts.Length > 2)
                        {
                            parts[2].Replace(',', ',');
                            if (Double.TryParse(parts[2], out val))
                            {
                                r.value = val;
                            }
                        }
                        vf.records.Add(r);
                        line = sr.ReadLine();
                    }
                    sr.Close();

                }
            }
            printSorted();
        }
        private void processStationVariable()
        {
            string[] files = Directory.GetFiles(folder);
            
            foreach (string file in files)
            {
                if (Path.GetExtension(file) == ".txt")
                {
                    int firstBar = file.IndexOf("_");
                    int lastSlash = file.LastIndexOf("\\");
                    var firstnum = file[lastSlash];
                    int sCode = Convert.ToInt32(file.Substring(lastSlash+1, firstBar- lastSlash-1));
                    int lastBar = file.LastIndexOf("_");
                    string vCode = file.Substring(lastBar+1, 2);
                    //get variable file to add to
                    string freq = file.Substring(lastBar + 1);
                    if (freq.Contains("2")) freq = "60";
                    else freq = "10";
                    VariableFile vf = sortedFiles.Find(x => x.variableName == vCode);
                    if(vf==null)
                    {
                        vf = new VariableFile();
                        vf.source = source;
                        vf.variableName = vCode;
                        vf.subset = "statVar";
                        vf.freq = freq;
                        sortedFiles.Add(vf);
                    }
                    StreamReader sr = new StreamReader(file);
                    string line = sr.ReadLine();
                    while(line!= null)
                    {
                        
                        string[] parts = line.Split(';');
                        Record r = new Record();
                        string[] timecode = parts[1].Split(' ');
                        DateTime dt = new DateTime();
                        
                        if (DateTime.TryParse(parts[0] + " " + timecode[0], out dt))
                        {
                            r.datetime = dt;
                        }
                        else
                        {
                            r.datetime = dt;
                        }
                        r.stationCode = sCode;
                        double val;
                        if (parts.Length > 2)
                        {
                            parts[2].Replace(',', ',');
                            if (Double.TryParse(parts[2], out val))
                            { r.value = val; }
                        }
                        vf.records.Add(r);
                        line = sr.ReadLine();
                    }
                    sr.Close();
                }
            }
            printSorted();
        }
        private int findVariableSet(string vCode)
        {
            int index = 0;
            for(int i = 0;i<sortedFiles.Count;i++)
            {

            }
            return index;
        }

        private void processRad()
        {
            string[] files = Directory.GetFiles(folder);
            VariableFile rad = new VariableFile();
            rad.variableName = "RS";
            rad.source = source;
            rad.subset = "rad";
            rad.freq = "60";
            foreach(string file in files)
            {
                if(Path.GetExtension(file)==".csv")
                {
                    int firstNum = file.LastIndexOf("-");
                    int lastNum = file.LastIndexOf("0");
                    int sCode = Convert.ToInt32(file.Substring(firstNum+1, lastNum - firstNum));
                    StreamReader sr = new StreamReader(file);
                    string line = sr.ReadLine();
                    line = sr.ReadLine();
                    DateTime dt = new DateTime();
                    while (line!=null)
                    {
                        Record r = new Record();
                        string[] parts = line.Split(',');
                        //date format given is MM/DD/YYYY HH:MM
                        if (parts[0] != "")
                        {
                            string[] datetime = parts[1].Split(' ');
                            int hour = 0;
                            int minutes = 0;
                            string[] monthdayyear = datetime[0].Split('/');// day month year
                            if (datetime.Length != 1)
                            {
                                string[] hoursminssecs = datetime[1].Split(':');//hours mins secs
                                hour = Convert.ToInt32(hoursminssecs[0]);
                                minutes = Convert.ToInt32(hoursminssecs[1]);
                            }


                            try
                            {
                                dt = new DateTime(Convert.ToInt32(monthdayyear[2]), Convert.ToInt32(monthdayyear[0]), Convert.ToInt32(monthdayyear[1])
                                    , hour, minutes, 0);
                            }
                            catch
                            {
                                var b = 0;
                            }
                            r.datetime = dt;
                            if (parts[2] != "") r.value = Convert.ToDouble(parts[2].Replace(',', '.'));
                            r.stationCode = sCode;
                            rad.records.Add(r);
                        }
                        line = sr.ReadLine();
                    }
                    sr.Close();
                }
            }
            sortedFiles.Add(rad);
            printSorted();
        }
        private void printSorted()
        {
            foreach(VariableFile vf in sortedFiles)
            {
                StreamWriter sw = new StreamWriter(folder + "\\" + vf.source + "_" + vf.subset + "_" + vf.variableName + "_" + vf.freq + ".csv");
                foreach(Record r in vf.records)
                {
                    sw.WriteLine(r.stationCode.ToString() + "," + r.datetime.ToString() + "," + r.value.ToString());
                }
                sw.Close();
            }
            
        }
    }
    
    class Record
    {
        public DateTime datetime { get; set; }
        public int stationCode { get; set; }
        public double value { get; set; }
        public Record()
        {
            value = -999;
        }
    }
    class VariableFile
    {
        public string variableName { get; set; }
        public string source { get; set; }
        public string subset { get; set; }
        public string freq { get; set; }
        public List<Record> records { get; set; }
        public VariableFile()
        {
            records = new List<Record>();
        }
    }
}
