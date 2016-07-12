using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Support
{
    public enum TimeOfDay
    {
        Morning,
        Day,
        Evening,
        Night
    }

    public enum TimeOfWeek
    {
        Normal,
        Weekend,
    }

    public class TimeHelper
    {
        public static TimeOfDay TimeOfDay
        {
            get
            {
                DateTime time = DateTime.Now;

                if (time.Hour < 7)
                    return TimeOfDay.Night;
                else if (time.Hour < 13)
                    return TimeOfDay.Morning;
                else if (time.Hour < 18)
                    return TimeOfDay.Day;
                else
                    return TimeOfDay.Evening;
            }
        }

        public static bool IsDay
        {
            get
            {
                return TimeOfDay == TimeOfDay.Day;
            }
        }

        public static bool IsNight
        {
            get
            {
                return TimeOfDay == TimeOfDay.Night;
            }
        }       

        public static TimeOfWeek TimeOfWeek
        {
            get
            {
                DateTime time = DateTime.Now;

                if ((int)time.DayOfWeek < 5)
                    return TimeOfWeek.Normal;
                return TimeOfWeek.Weekend;
            }
        }
    }
}
