//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common.Events
{
    using System;
    using System.Globalization;

    static class AggregationUtility
    {
        // Round the timestamp down to the closest time window boundary
        public static DateTime SnapTimeWindowStartTime(DateTime dateTime, TimeSpan timeWindow)
        {
            if (timeWindow == AggregationDefaults.TimeWindowCurrent)
            {
                return dateTime;
            }

            int mod = 0;
            int totalSeconds = (int)timeWindow.TotalSeconds;
            switch (totalSeconds)
            {
                case 10: // 10 seconds:
                    mod = dateTime.Second % 10;
                    break;
                case 60: // 1 minute
                    mod = dateTime.Second;
                    break;
                case 300: // 5 minutes
                    mod = ((dateTime.Minute % 5) * 60) + dateTime.Second;
                    break;
                case 600: // 10 minutes
                    mod = ((dateTime.Minute % 10) * 60) + dateTime.Second;
                    break;
                case 3600: // 1 hour
                    mod = (dateTime.Minute * 60) + dateTime.Second;
                    break;
                default:
                    throw new NotSupportedException(totalSeconds.ToString(CultureInfo.InvariantCulture));
            }

            if (mod != 0)
            {
                dateTime = dateTime.Subtract(TimeSpan.FromSeconds(mod));
            }

            DateTime outputDateTime = new DateTime(
                dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second);
            return outputDateTime;
        }
    }
}
