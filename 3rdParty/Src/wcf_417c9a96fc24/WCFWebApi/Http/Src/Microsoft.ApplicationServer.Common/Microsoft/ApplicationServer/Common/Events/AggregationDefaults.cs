//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common.Events
{
    using System;

    static class AggregationDefaults
    {
        public static readonly TimeSpan TimeWindow = TimeSpan.FromSeconds(60);
        public static readonly TimeSpan TimeWindowCurrent = TimeSpan.Zero;
        public static readonly bool LocalEnableExpirationClock = true;
        public static readonly bool FarmEnableExpirationClock = true;
        public static readonly TimeSpan LocalExpireAggregationClockFrequency = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan FarmExpireAggregationClockFrequency = TimeSpan.FromSeconds(60);
        public static readonly TimeSpan LocalExtraExpirationWaitTime = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan FarmExtraExpirationWaitTime = TimeSpan.FromSeconds(90);
        public static readonly TimeSpan LocalClockExpirationWaitTime = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan FarmClockExpirationWaitTime = TimeSpan.FromMinutes(10);
    }
}
