using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KWTroubleshooter
{
    internal static class Utils
    {
        public static bool EqualsDate(this DateTime dt, DateTime dt1)
        {
            if ((dt1 - dt).Duration() <= TimeSpan.FromSeconds(2.5))
                return true;
            return false;
        }
    }
}
