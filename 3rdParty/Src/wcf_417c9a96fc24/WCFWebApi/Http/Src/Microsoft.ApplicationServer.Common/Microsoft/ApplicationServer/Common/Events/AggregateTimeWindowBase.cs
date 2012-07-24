//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common.Events
{
    using System;
    using Microsoft.ApplicationServer.Common.Events;

    abstract class AggregateTimeWindowBase
    {
        public AggregateTimeWindowBase(AggregateGroupKey key, DateTime startTime, DateTime endTime)
        {
            this.Key = key;
            this.StartTime = startTime;
            this.EndTime = endTime;
        }

        public AggregateGroupKey Key { get; private set; }

        public DateTime StartTime { get; private set; }

        public DateTime EndTime { get; private set; }

        public TimeSpan TimeWindow
        {
            get
            {
                return this.EndTime - this.StartTime;
            }
        }

        public abstract MetricEvent ProduceOutput();
    }
}
