using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace epwVisualiser
{
    class EpwImageMaker
    {
        List<string> files = new List<string>();
        String folder;
        public EpwImageMaker(String epwfolder)
        {
            getFiles(epwfolder);
            foreach(string file in this.files)
            {
                if(file.Contains(".epw"))
                {
                    List<EpwData> annualFields = getAnnualValues(file);
                    makeImage(annualFields, file);

                }
                

            }
        }
        private void getFiles(String path)
        {
            this.files = Directory.GetFiles(path).ToList();
        }
        private Color rainbowRGB(double value,double max, double min)
        {
            double range = max - min;
            double percent = (value - min) / range;
            double startCol = 0.2;
            double endCol = 1;
            int red;
            int grn;
            int blu;
            Color rgb = new Color();
            //percent is the position in the spectrum 0 = red 1 = violet
            //first flip 
            
                percent = 1 - percent;
                double threeSixty = Math.PI * 2;
                //but then we shift to squeeze into the desired range
                double scaledCol = percent * (endCol - startCol) + startCol;
                //startCol is a % into the roygbinv spectrum
                //endCol is a % before the end of the roygbinv spectrum
                red = Convert.ToInt16(Math.Sin(threeSixty * scaledCol + 2 * Math.PI / 3) * 128 + 127);
                grn = Convert.ToInt16(Math.Sin(threeSixty * scaledCol + 4 * Math.PI / 3) * 128 + 127);
                blu = Convert.ToInt16(Math.Sin(threeSixty * scaledCol + 0) * 128 + 127);
                if (red < 0) red = 0; if (red > 255) red = 255;
                if (grn < 0) grn = 0; if (grn > 255) grn = 255;
                if (blu < 0) blu = 0; if (blu > 255) blu = 255;
            
            ColorMine.ColorSpaces.Rgb colMRGB = new ColorMine.ColorSpaces.Rgb { R = red, B = blu, G = grn };
            colMRGB.R = red;
            colMRGB.G = grn;
            colMRGB.B = blu;
            var colMHSV = colMRGB.To<ColorMine.ColorSpaces.Hsv>();
            colMHSV.S = colMHSV.S*0.7;
            colMRGB = colMHSV.To<ColorMine.ColorSpaces.Rgb>();
            rgb = Color.FromArgb((int)colMRGB.R, (int)colMRGB.G, (int)colMRGB.B);
            return rgb;
        }
        private Color defineColorRadial(double value, double max, double min)
        {
            Color c = Color.Wheat;
            double range = max - min;
            double percent = (value - min) / range;
           
                ColorMine.ColorSpaces.Hsv hsv = new ColorMine.ColorSpaces.Hsv();
                hsv.H = 360*percent;
                hsv.S = 0.7;
                hsv.V = 1;
                var rgb = hsv.To < ColorMine.ColorSpaces.Rgb>();
                c = Color.FromArgb((int)rgb.R, (int)rgb.G, (int)rgb.B);
                return c;
        }
        private Color defineColor(double value, double max, double min)
        {
            Color c = Color.Wheat;

            return c;
        }
        private List<EpwData> getAnnualValues(String filename)
        {
            EpwData rs = new EpwData();rs.setName("RS");
            EpwData hr = new EpwData(); hr.setName("HR");
            EpwData nub = new EpwData();nub.setName("NUB");
            EpwData pr = new EpwData();pr.setName("PR");
            EpwData t = new EpwData();t.setName("TS");
            EpwData dv = new EpwData();dv.setName("DV");
            EpwData vv = new EpwData();vv.setName("VV");

            StreamReader sr = new StreamReader(filename);
            String line = sr.ReadLine();
            int lineCount = 0;
            while(line!=null)
            {
                if (lineCount >= 8)
                {
                    String[] fields = line.Split(',');
                    for (int i = 0; i < fields.Length; i++)
                    {
                        switch (i)
                        {
                            case 6:

                                t.values.Add(Convert.ToDouble(fields[i]));
                                break;
                            case 8:
                                hr.values.Add(Convert.ToDouble(fields[i]));
                                break;
                            case 14:
                                rs.values.Add(Convert.ToDouble(fields[i]));
                                break;
                            case 20:
                                dv.values.Add(Convert.ToDouble(fields[i]));
                                break;
                            case 21:
                                vv.values.Add(Convert.ToDouble(fields[i]));
                                break;
                            case 22:
                                nub.values.Add(Convert.ToDouble(fields[i]));
                                break;
                            case 33:
                                pr.values.Add(Convert.ToDouble(fields[i]));
                                break;
                        }
                    }
                }
                line = sr.ReadLine();
                lineCount++;
            }
            sr.Close();
            List<EpwData> fieldvalues = new List<EpwData>();
            fieldvalues.Add(t);
            fieldvalues.Add(rs);
            fieldvalues.Add(hr);
            fieldvalues.Add(dv);
            fieldvalues.Add(vv);
            fieldvalues.Add(pr);
            fieldvalues.Add(nub);
            return fieldvalues;
        }
        private void makeImage(List<EpwData> fieldvalues,string filename)
        {
            int width = 6510;
            int height = 340;
            Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(bitmap);
            // white back ground
            g.Clear(Color.White);
            for( int v=0;v< fieldvalues.Count; v++)
            {
                EpwData data = fieldvalues[v];
                int d = 0;
                int h = 0;
                int startX = v * 930 + 50;
                int startY = height - 50;
                Font tFont = new Font("Arial", 20);
                SolidBrush sBrush = new SolidBrush(System.Drawing.Color.Black);
                //image title
                if(v==0) g.DrawString(filename.Substring(filename.LastIndexOf("\\")), tFont, sBrush, startX, startY + 25);
                //graph title
                tFont = new Font("Arial", 16);
                g.DrawString(data.name, tFont, sBrush,  startX, 25);
                float x = 0;
                float y = 0;
                Color c = new Color();
                for (int i =0;i<data.values.Count;i++)
                {
                    y = startY - h * 10;
                    x = startX + d * 2;
                    if (data.values[i] >= data.nullValue) c = Color.Black;
                    else
                    {
                        if (data.name == "DV") c = this.defineColorRadial(data.values[i], data.min, data.max);
                        else c = this.rainbowRGB(data.values[i], data.min, data.max);
                    }
                        
                    SolidBrush p = new SolidBrush(c);
                    g.FillRectangle(p, x, y, 2, 10);
                    
                    h++;
                    if (h == 24)
                    {
                        d ++;
                        h=0;
                    }
                }
                //scale bar
                double range = data.max - data.min;
                double inc = range / 11;
                double val = 0;
                for (int s=0;s<11;s++)
                {
                    val = data.min+s * inc;
                    if (data.name == "DV") c = this.defineColorRadial(val, data.min, data.max);
                    else c = this.rainbowRGB(val, data.min, data.max);
                    SolidBrush p = new SolidBrush(c);
                    g.FillRectangle(p, x+30, height-60-(s*22), 20, 22);

                    g.DrawString(Math.Round(val,1).ToString(), tFont, sBrush, x + 60, height - 60 - (s * 22));
                }
            }
            String fname = filename.Substring(0, filename.LastIndexOf("."));
            bitmap.Save(fname+".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
        }
    }
    public class EpwData
    {
        public List<double> values = new List<double>();
        public String name;
        public double max;
        public double min;
        public double nullValue;
        public void setName(String n)
        {
            this.name = n;
            switch(this.name)
            {
                case "TS":
                    this.max = 40;
                    this.min = -5;
                    this.nullValue = 99.9;
                    break;
                case "RS":
                    this.max = 1000;
                    this.min = 0;
                    this.nullValue = 9999;
                    break;
                case "HR":
                    this.max = 110;
                    this.min = 0;
                    this.nullValue = 999;
                    break;
                case "NUB":
                    this.max = 10;
                    this.min = 0;
                    this.nullValue = 99;
                    break;
                case "PR":
                    this.max = 60;
                    this.min = 0;
                    this.nullValue = 999;
                    break;
                case "DV":
                    this.max = 360;
                    this.min = 0;
                    this.nullValue = 999;
                    break;
                case "VV":
                    this.max = 20;
                    this.min = 0;
                    this.nullValue = 999;
                    break;
            }

        }
        
    }
}
