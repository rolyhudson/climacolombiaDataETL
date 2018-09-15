using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataETL
{
    class SolarRadiation
    {

        public SolarRadiation()
        {

        }


        private static double declination(int doy)
        {
            double trop = 23.45 * 0.0174533;
            double dayRatio = (284.0 + doy) / 365.0 * 2 * Math.PI;
            double dec = trop * Math.Sin(dayRatio);
            return dec;
        }
        private static double hourAngle(double localsolartime)
        {
            double hra = 15 * (localsolartime - 12);
            return hra * 0.0174533;
        }
        private static double localSolarTime(double localtime, double timecorrection)
        {
            return localtime + timecorrection / 60;
        }
        private static double timeCorrection(double lon, double localStanTimeMeridian, double eqnOfTime)
        {
            return 4 * (lon - localStanTimeMeridian) + eqnOfTime;
        }
        private static double eqnOfTime(int doy)
        {


            double eot = -7.655 * Math.Sin(doy) + 9.873 * Math.Sin(2 * doy + 3.588);
            return eot;
        }
        private static double localStanTimeMeridian(int deltaLocalUTC)
        {
            return 15 * deltaLocalUTC;
        }
        public static double etRadHourly(int doy, double lat, double lon, int localTime)
        {
            //general principles:
            //https://www.researchgate.net/file.PostFileLoader.html?id=553e4871d685ccd10e8b4618&assetKey=AS%3A273765705945088%401442282238044
            //more detail on equations
            ////https://www.pveducation.org/pvcdrom/properties-of-sunlight/solar-time
            //eqn of time
            ////https://www.intmath.com/blog/mathematics/the-equation-of-time-5039
            lat = lat * 0.0174533;
            double etr = 0.0;
            double dec = declination(doy);
            double lstm = localStanTimeMeridian(-5);
            double eot = eqnOfTime(doy);
            double timeCorr = timeCorrection(lon, lstm, eot);
            double hra1 = hourAngle(localSolarTime(localTime, timeCorr));
            double hra2 = hourAngle(localSolarTime(localTime + 1, timeCorr));
            double solarCons = 1367;
            double gon = solarCons * (1 + 0.033 * Math.Cos(360 * doy / 365));
            etr = (12 * 3600 / Math.PI) * gon * (Math.Cos(lat) * Math.Cos(dec) * (Math.Sin(hra2) - Math.Sin(hra1)) +
                Math.PI * (hra2 - hra1) / Math.PI / 2 * Math.Sin(lat) * Math.Sin(dec));
            //convert from joules to watts
            etr = etr / 3600;
            if (etr < 0) etr = 0;
            return etr;
        }
        public static double getDiffuse(double global, int doy, double lat, double lon, int localTime)
        {
            double kd = 0.0;
            double kt = global / etRadHourly(doy, lat, lon, localTime);
            if (kt <= 0.15) kd = 0.977;
            if (kt > 0.15 && kt <= 0.7) kd = 1.237 - 1.361 * kt;
            if (kt > 0.7) kd = 0.273;
            return kd * global;
        }
        public static double getDirect(double global,double diffuse)
        {
            double direct = global - diffuse;
            return direct;
        }
    }
}
