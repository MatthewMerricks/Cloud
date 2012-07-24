//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common.Events
{
    using System;
    using Microsoft.ApplicationServer.Common.Events;

    class RollupAggregationProcessor : IDisposable
    {
        RollupAggregationSynopsis synopsis;
        Action<object> processEventTestHook; // Test Hook
        ProcessMetricEvent processEvent;
        bool disposed;

        public RollupAggregationProcessor(ProcessMetricEvent processEvent)
            : this()
        {
            this.processEvent = processEvent;
        }

        public RollupAggregationProcessor(Action<object> processEventTestHook)
            : this()
        {
            this.processEventTestHook = processEventTestHook;
        }

        private RollupAggregationProcessor()
        {
            this.ExpireAggregationClockFrequency = AggregationDefaults.FarmExpireAggregationClockFrequency;
            this.ExtraExpirationWaitTime = AggregationDefaults.FarmExtraExpirationWaitTime;
            this.ClockExpirationWaitTime = AggregationDefaults.FarmClockExpirationWaitTime;
        }

        public TimeSpan ExpireAggregationClockFrequency { get; set; }

        public TimeSpan ExtraExpirationWaitTime { get; set; }

        public TimeSpan ClockExpirationWaitTime { get; set; }

        public void Start()
        {
            AggregationSettings settings = new AggregationSettings()
            {
                EnableExpirationClock = AggregationDefaults.FarmEnableExpirationClock,
                ExpireAggregationClockFrequency = this.ExpireAggregationClockFrequency,
                ExtraExpirationWaitTime = this.ExtraExpirationWaitTime,
                ClockExpirationWaitTime = this.ClockExpirationWaitTime
            };
            this.synopsis = new RollupAggregationSynopsis(
                this.processEvent != null ? this.processEvent : this.ConsumeMetricEvent, 
                settings);
        }

        public void Stop()
        {
            // On stop, any state is discarded.  No output is issued for pending aggregations.
            this.synopsis.Dispose();
            this.synopsis = null;
        }

        public void Publish(MetricEvent evt)
        {
            this.synopsis.Update(evt);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        void ConsumeMetricEvent(MetricEvent metricEvent)
        {
            if (this.processEventTestHook != null)
            {
                this.processEventTestHook(metricEvent);
            }
        }

        void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (this.synopsis != null)
                    {
                        this.synopsis.Dispose();
                        this.synopsis = null;
                    }
                }

                this.disposed = true;
            }
        }
    }
}
