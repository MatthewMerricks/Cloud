using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSdkSyncSample.Support
{
    public sealed class Utilities
    {
        /// <summary>
        /// Convert a string to a ulong (UInt64).
        /// </summary>
        /// <param name="inString">The string to convert.</param>
        /// <param name="value">out: The converted value.</param>
        /// <returns>bool: true: Successful conversion.</returns>
        public static bool ConvertStringToUlong(string inString, out ulong value)
        {
            bool toReturn = true;
            try
            {
                value = Convert.ToUInt64(inString);
            }
            catch
            {
                value = 0;
                toReturn = false;
            }

            return toReturn;
        }

    }
}
