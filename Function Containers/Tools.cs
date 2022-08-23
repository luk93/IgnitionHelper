using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgnitionHelper
{
    static class Tools
    {
        public static bool IsFileLocked(string filePath)
        {
            try
            {
                var stream = File.OpenRead(filePath);
                return false;
            }
            catch (IOException)
            {
                return true;
            }
        }
        public static string GetDateTimeString()
        {
            string dateTime = DateTime.Now.Year.ToString();
            if (DateTime.Now.Month.ToString().Length == 1)
            {
                dateTime += "0" + DateTime.Now.Month.ToString();
            }
            else dateTime += DateTime.Now.Month.ToString();
            if (DateTime.Now.Day.ToString().Length == 1)
            {
                dateTime += "0" + DateTime.Now.Day.ToString();
            }
            else dateTime += DateTime.Now.Day.ToString();
            dateTime += "_";
            if (DateTime.Now.TimeOfDay.Hours.ToString().Length == 1)
            {
                dateTime += "0" + DateTime.Now.TimeOfDay.Hours.ToString();
            }
            else dateTime += DateTime.Now.TimeOfDay.Hours;
            if (DateTime.Now.TimeOfDay.Minutes.ToString().Length == 1)
            {
                dateTime += "0" + DateTime.Now.TimeOfDay.Minutes.ToString();
            }
            else dateTime += DateTime.Now.TimeOfDay.Minutes;
            return dateTime;
        }
    }
}
