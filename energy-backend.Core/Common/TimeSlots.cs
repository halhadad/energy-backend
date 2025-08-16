using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace energy_backend.Core.Common
{
    public static class TimeSlots
    {      
        // Floors a DateTime (UTC) to the start of the 5-second slot.
        
        public static DateTime FloorTo5sUtc(DateTime utcTime)
        {
            if (utcTime.Kind != DateTimeKind.Utc)
                utcTime = utcTime.ToUniversalTime();

            int seconds = utcTime.Second - (utcTime.Second % 5);
            return new DateTime(
                utcTime.Year,
                utcTime.Month,
                utcTime.Day,
                utcTime.Hour,
                utcTime.Minute,
                seconds,
                DateTimeKind.Utc
            );
        }
    }

}
