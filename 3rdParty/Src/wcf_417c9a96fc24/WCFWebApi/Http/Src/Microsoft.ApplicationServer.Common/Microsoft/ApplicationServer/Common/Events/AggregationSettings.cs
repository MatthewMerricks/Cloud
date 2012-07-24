//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common.Events
{
    using System;

    class AggregationSettings
    {
        public bool EnableExpirationClock { get; set; }

        public TimeSpan ExpireAggregationClockFrequency { get; set; }

        public TimeSpan ExtraExpirationWaitTime { get; set; }

        public TimeSpan ClockExpirationWaitTime { get; set; }

        public TimeSpan TimeWindow { get; set; }
    }
}

