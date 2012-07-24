//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common.Events
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.ApplicationServer.Common.Events;

    delegate void ProcessMetricEvent(MetricEvent metricEvent);

    class AggregationProcessor : IDisposable
    {
        AggregationSynopsis synopsis;
        Action<object> processEventTestHook; // Test Hook
        ProcessMetricEvent processEvent;
        bool disposed;

        public AggregationProcessor(ProcessMetricEvent processEvent)
            : this()
        {
            this.processEvent = processEvent;
        }

        public AggregationProcessor(Action<object> processEventTestHook)
            : this()
        {
            this.processEventTestHook = processEventTestHook;
        }

        private AggregationProcessor()
        {
            this.EnableExpirationClock = AggregationDefaults.LocalEnableExpirationClock;
            this.ExpireAggregationClockFrequency = AggregationDefaults.LocalExpireAggregationClockFrequency;
            this.ExtraExpirationWaitTime = AggregationDefaults.LocalExtraExpirationWaitTime;
            this.ClockExpirationWaitTime = AggregationDefaults.LocalClockExpirationWaitTime;
            this.TimeWindow = AggregationDefaults.TimeWindow;
        }

        public bool EnableExpirationClock { get; set; }

        public TimeSpan ExpireAggregationClockFrequency { get; set; }

        public TimeSpan ExtraExpirationWaitTime { get; set; }

        public TimeSpan ClockExpirationWaitTime { get; set; }

        public TimeSpan TimeWindow { get; set; }

        public void Start()
        {
            AggregationSettings settings = new AggregationSettings()
            {
                EnableExpirationClock = this.EnableExpirationClock,
                ExpireAggregationClockFrequency = this.ExpireAggregationClockFrequency,
                ExtraExpirationWaitTime = this.ExtraExpirationWaitTime,
                ClockExpirationWaitTime = this.ClockExpirationWaitTime,
                TimeWindow = this.TimeWindow
            };

            this.synopsis = new AggregationSynopsis(
                this.processEvent != null ? this.processEvent : this.ConsumeMetricEvent, 
                settings);
        }

        public void Stop()
        {
            // On stop, any state is discarded.  No output is issued for pending aggregations.
            this.synopsis.Dispose();
            this.synopsis = null;
        }

        // Returns true if the event matches at least one policy
        public void Publish(ResourceEvent evt)
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
