//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common.Events
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Common.Events;

    // Represents the state for a single time window per (Metric, Grouping Key)
    // Encapsulates the specific rules for computing the aggregation
    class RollupAggregateTimeWindow : AggregateTimeWindowBase
    {
        private RollupAggregateTimeWindow(AggregateGroupKey key, DateTime startTime, DateTime endTime)
            : base(key, startTime, endTime)
        {
            this.State = new AverageAggregateState(this);
        }

        AggregateState State { get; set; }

        public static RollupAggregateTimeWindow Create(AggregateGroupKey key, DateTime startTime, TimeSpan timeWindow)
        {
            DateTime endTime = startTime.Add(timeWindow);
            return Create(key, startTime, endTime);
        }

        public static RollupAggregateTimeWindow Create(AggregateGroupKey key, DateTime startTime, DateTime endTime)
        {
            return new RollupAggregateTimeWindow(key, startTime, endTime);
        }

        public void Update(MetricEvent evt)
        {
            this.State.Update(evt);
        }

        public override MetricEvent ProduceOutput()
        {
            return this.State.ProduceOutput();
        }

        abstract class AggregateState
        {
            public AggregateState(RollupAggregateTimeWindow aggregateTimeWindow)
            {
                this.AggregateTimeWindow = aggregateTimeWindow;
            }

            public RollupAggregateTimeWindow AggregateTimeWindow { get; set; }

            public abstract void Update(MetricEvent evt);

            public abstract MetricEvent ProduceOutput();
        }

        class AverageAggregateState : AggregateState
        {
            public AverageAggregateState(RollupAggregateTimeWindow aggregateTimeWindow)
                : base(aggregateTimeWindow)
            {
            }

            public double Value
            {
                get
                {
                    if (this.Count > 0)
                    {
                        return this.Total / this.Count;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }

            double Total { get; set; }

            int Count { get; set; }

            double? Minimum { get; set; }

            double? Maximum { get; set; }

            string AdditionalData { get; set; }

            public override void Update(MetricEvent evt)
            {
                this.Count += evt.Count;
                this.Total += evt.Total;

                if (!string.IsNullOrEmpty(evt.AdditionalData))
                {
                    this.AdditionalData = evt.AdditionalData;
                }

                if (!this.Maximum.HasValue || evt.Maximum > this.Maximum)
                {
                    this.Maximum = evt.Maximum;
                }

                if (!this.Minimum.HasValue || evt.Minimum < this.Minimum)
                {
                    this.Minimum = evt.Minimum;
                }
            }

            public override MetricEvent ProduceOutput()
            {
                MetricEvent metricEvent = new MetricEvent()
                {
                    Name = this.AggregateTimeWindow.Key.Metric,
                    TenantId = this.AggregateTimeWindow.Key.TenantId,
                    EventSource = this.AggregateTimeWindow.Key.EventSource,
                    InstanceId = this.AggregateTimeWindow.Key.InstanceId,
                    TimeCreated = this.AggregateTimeWindow.StartTime,
                    TimeWindow = this.AggregateTimeWindow.EndTime - this.AggregateTimeWindow.StartTime,
                    AdditionalData = this.AdditionalData,
                    Average = this.Value,
                    Total = this.Total,
                    Count = this.Count,
                    Minimum = this.Minimum.HasValue ? this.Minimum.Value : 0,
                    Maximum = this.Maximum.HasValue ? this.Maximum.Value : 0
                };

                if (this.AggregateTimeWindow.Key.Dimensions != null && this.AggregateTimeWindow.Key.Dimensions.Count > 0)
                {
                    foreach (KeyValuePair<string, string> pair in this.AggregateTimeWindow.Key.Dimensions)
                    {
                        metricEvent.Dimensions.Add(pair.Key, pair.Value);
                    }
                }

                return metricEvent;
            }
        }
    }
}
