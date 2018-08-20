using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataETL
{
    class EPWWriter
    {
        SyntheticYear synthYear;
        string directory;
        StreamWriter epw;
        string city = "";
        string country = "";
        //this is for wea to epw
        public EPWWriter(SyntheticYear locdata, string d)
        {
            this.synthYear = locdata;
            this.directory = d;
            this.city = this.synthYear.name;

            this.country = "Colombia";// StringTools.stripExt(new string(this.ld.where));
            Directory.CreateDirectory(this.directory + "\\epw");
            this.epw = new StreamWriter(this.directory + "\\epw\\" + city + ".epw");
            writeEPW();
            this.epw.Close();
        }
        private void writeEPW()
        {
            //see p60 in AuxiliaryProgramsEPWDocumentation
            epwLocation();
            epwDesignConditions();
            epwTypicalExtreme();
            epwGroundTemps();
            epwHolsDaylightSaving();
            epwComments(1, "conversion from wea");
            epwComments(2, "conversion from wea");
            epwHourlyData();

        }
        private void epwLocation()
        {
            StringBuilder line = new StringBuilder();
            line.Append("LOCATION,");
            line.Append(city + ",");//city 
            line.Append(",");//state province or region
            line.Append(country + ",");//country
            line.Append("upcData" + ",");//source
            line.Append(",");//World Meteorological Organization Station Number
            line.Append(this.synthYear.info.latitude.ToString() + ",");//latitude in degs range -90 to +90 default 0.0
            line.Append(this.synthYear.info.longitude.ToString() + ",");//longitude in degs range: -180.0 to +180.0 default 0.0
            line.Append("-5,");//time from GMT in hrs rnage: -12 to +12
            line.Append(this.synthYear.info.elevation.ToString());//elevation in m range -1000 to +9999.9
            this.epw.WriteLine(line);
            line.Clear();
        }
        private void epwDesignConditions()
        {
            StringBuilder line = new StringBuilder();
            line.Append("DESIGN CONDITIONS,");
            this.epw.WriteLine(line);
            line.Append(",");//number conditions
            line.Append(",");//source
            line.Append("Heating,");//
            //heating conditons?
            line.Append("Cooling");//source
            //cooling conditions
            this.epw.WriteLine(line);
            line.Clear();
        }
        private void epwTypicalExtreme()
        {
            StringBuilder line = new StringBuilder();
            line.Append("TYPICAL / EXTREME PERIODS,");
            line.Append(",");//field Number of Typical/ Extreme Periods
            line.Append(",");//field Typical/ Extreme Period 1 Name
            line.Append(",");//field Typical/ Extreme Period 1 Type
            line.Append(",");//field Period 1 Start Day
            line.Append(",");//field Period 1 End Day
            //note repeat last 3 until number of typical periods etc to # of periods entered
            this.epw.WriteLine(line);
            line.Clear();
        }
        private void epwGroundTemps()
        {
            StringBuilder line = new StringBuilder();
            line.Append("GROUND TEMPERATURE,");
            line.Append(",");//N1, Number of Ground Temperature Depths
            line.Append(",");//N2, \field Ground Temperature Depth 1\units m

            line.Append(",");//N3, \field Depth 1 Soil Conductivity\units W/ m - K,

            line.Append(",");//N4, \field Depth 1 Soil Density\units kg/ m3

            line.Append(",");//N5, \field Depth 1 Soil Specific Heat\units J / kg - K,

            line.Append(",");//N6, \field Depth 1 January Average Ground Temperature\units C

            line.Append(",");//N7, \field Depth 1 February Average Ground Temperature\units C

            line.Append(",");//N8, \field Depth 1 March Average Ground Temperature\units C

            line.Append(",");//N9, \field Depth 1 April Average Ground Temperature \units C

            line.Append(",");//N10, \field Depth 1 May Average Ground Temperature\units C

            line.Append(",");//N11, \field Depth 1 June Average Ground Temperature\units C

            line.Append(",");//N12, \field Depth 1 July Average Ground Temperature\units C

            line.Append(",");//N13, \field Depth 1 August Average Ground Temperature

            line.Append(",");//N14, \field Depth 1 September Average Ground Temperature\units C

            line.Append(",");//N15, \field Depth 1 October Average Ground Temperature\units C

            line.Append(",");//N16, \field Depth 1 November Average Ground Temperature\units C

            line.Append(",");//N17, \field Depth 1 December Average Ground Temperature\units C
            //\note repeat above(N2 - N17) to number of ground temp depths indicated
            this.epw.WriteLine(line);
            line.Clear();
        }
        private void epwHolsDaylightSaving()
        {
            StringBuilder line = new StringBuilder();
            line.Append("HOLIDAYS / DAYLIGHT SAVING,");
            line.Append(",");//A1, \field LeapYear Observed\type choice\key Yes\key No\note Yes if Leap Year will be observed for this file\note No if Leap Year days (29 Feb) should be ignored in this file
            line.Append(",");//A2, \field Daylight Saving Start Day
            line.Append(",");//A3, \field Daylight Saving End Day
            line.Append(",");//N1, \field Number of Holidays (essentially unlimited)
            line.Append(",");//A4, \field Holiday 1 Name
            line.Append(",");//A5, \field Holiday 1 Day
            //\note repeat above two fields until Number of Holidays is reached
            this.epw.WriteLine(line);
            line.Clear();
        }
        private void epwComments(int num, string comment)
        {
            StringBuilder line = new StringBuilder();
            line.Append("COMMENTS " + num.ToString() + ",");//A1 \field Comments_1
            this.epw.WriteLine(line);
            line.Clear();
        }
        private void epwDataPeriods()
        {
            StringBuilder line = new StringBuilder();
            line.Append("DATA PERIODS,");
            line.Append("1,");//N1, \field Number of Data Periods
            line.Append("1,");//N2, \field Number of Records per hour
            line.Append(",");//A1, \field Data Period 1 Name / Description
            line.Append("Sunday,");//A2, \field Data Period 1 Start Day of Week
            //\type choice
            //\key Sunday
            //\key Monday
            //\key Tuesday
            //\key Wednesday
            //\key Thursday
            //\key Friday
            //\key Saturday
            line.Append("1/1,");//A3, \field Data Period 1 Start Day
            line.Append("31-Dec,");//A4, \field Data Period 1 End Day
            //\note repeat above to number of data periods
            this.epw.WriteLine(line);
            line.Clear();
        }
        public double getValue(string vcode,DateTime datetime)
        {
            var v = synthYear.variables.Find(x => x.name == vcode);
            var r = v.records.Find(d => d.time == datetime);
            return r.value;
        }
        private void epwHourlyData()
        {
            int year = 0;
            int month = 1;
            double pressure = MapTools.getStandardPressure(synthYear.info.elevation);
            int hour = 1;
            int day = 1;
            //loop all the time stamps in the first variable
            for (int i = 0; i < synthYear.variables[0].records.Count; i++)//should be 8760 hourly data lists
            {
                //{ "dbTemp", "rHumid", "wDir", "wSpeed", "diffuseHorizRad", "normDirectRad", "skyCover", "liquidPrecipDepth" };
                DateTime currentHr = synthYear.variables[0].records[i].time;
                DateTime localcurrentHr = currentHr.ToLocalTime();
                year = localcurrentHr.Year;
                month = localcurrentHr.Month;
                day = localcurrentHr.Day;
                hour = localcurrentHr.Hour+1;
                StringBuilder line = new StringBuilder();
                line.Append("2016,");// N1, \field Year
                line.Append(month.ToString() + ",");//N2, \field Month
                line.Append(day.ToString() + ",");//N3, \field Day
                line.Append(hour.ToString() + ","); //N4, \field Hour
                line.Append("60,"); //N5, \field Minute
                line.Append(",");// A1, \field Data Source and Uncertainty Flags
                                 //\note Initial day of weather file is checked by EnergyPlus for validity(as shown below)
                                 //\note Each field is checked for "missing" as shown below.Reasonable values, calculated
                                 //\note values or the last "good" value is substituted.

                line.Append(getValue("TS", currentHr).ToString() + ","); //N6, \field Dry Bulb Temperature\units C\minimum > -70\maximum < 70\missing 99.9
                line.Append("99.9,"); //N7, \field Dew Point Temperature\units C\minimum > -70\maximum < 70\missing 99.9
                line.Append(getValue("HR", currentHr).ToString() + ","); // N8, \field Relative Humidity\missing 999.\minimum 0\maximum 110
                line.Append(pressure.ToString() + ",");// N9, \field Atmospheric Station Pressure\units Pa\missing 999999.\minimum > 31000\maximum < 120000
                line.Append("9999.,");//N10, \field Extraterrestrial Horizontal Radiation\units Wh / m2\missing 9999.\minimum 0
                line.Append("9999.,");//N11, \field Extraterrestrial Direct Normal Radiation\units Wh / m2\missing 9999.\minimum 0
                line.Append("9999.,");//N12, \field Horizontal Infrared Radiation Intensity\units Wh / m2\missing 9999.\minimum 0
                line.Append("9999.,");//N13, \field Global Horizontal Radiation \units Wh / m2\missing 9999.\minimum 0
                line.Append(getValue("RS", currentHr).ToString() + ",");//N14, \field Direct Normal Radiation \units Wh / m2\missing 9999.\minimum 0
                line.Append(getValue("RS", currentHr).ToString() + ",");//N15, \field Diffuse Horizontal Radiation\units Wh / m2\missing 9999.\minimum 0
                line.Append("999999.,");//N16, \field Global Horizontal Illuminance\units lux\missing 999999.\note will be missing if > = 999900\minimum 0
                line.Append("999999.,");//N17, \field Direct Normal Illuminance\units lux\missing 999999.\note will be missing if > = 999900\minimum 0
                line.Append("999999.,");//N18, \field Diffuse Horizontal Illuminance\units lux\missing 999999.\note will be missing if > = 999900\minimum 0
                line.Append("9999.,");//N19, \field Zenith Luminance\units Cd / m2\missing 9999.\note will be missing if > = 9999\minimum 0
                line.Append(getValue("DV", currentHr).ToString() + ",");//N20, \field Wind Direction\units degrees\missing 999.\minimum 0\maximum 360
                line.Append(getValue("VV", currentHr).ToString() + ",");//N21, \field Wind Speed\units m / s\missing 999.\minimum 0\maximum 40
                line.Append(getValue("NUB", currentHr).ToString()+",");//N22, \field Total Sky Cover\missing 99\minimum 0\maximum 10
                line.Append("99,");//N23, \field Opaque Sky Cover (used if Horizontal IR Intensity missing)\missing 99\minimum 0\maximum 10
                line.Append("9999,");//N24, \field Visibility\units km\missing 9999
                line.Append("99999,");//N25, \field Ceiling Height\units m\missing 99999
                line.Append(",");//N26, \field Present Weather Observation
                line.Append(",");// N27, \field Present Weather Codes
                line.Append("999,");//N28, \field Precipitable Water\units mm\missing 999
                line.Append(".999,");//N29, \field Aerosol Optical Depth\units thousandths\missing .999
                line.Append("999,");//N30, \field Snow Depth\units cm\missing 999
                line.Append("99,");//N31, \field Days Since Last Snowfall\missing 99
                line.Append("999,");//N32, \field Albedo\missing 999
                line.Append(getValue("PR", currentHr).ToString() + ",");//N33, \field Liquid Precipitation Depth\units mm\missing 999
                line.Append("99,");//N34; \field Liquid Precipitation Quantityunits hr\missing 99
                this.epw.WriteLine(line);
                line.Clear();
               
            }
        }
    }
}
