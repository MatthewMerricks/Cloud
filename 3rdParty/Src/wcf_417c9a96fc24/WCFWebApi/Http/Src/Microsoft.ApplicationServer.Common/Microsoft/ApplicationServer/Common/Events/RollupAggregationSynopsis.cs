//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common.Events
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationServer.Common.Events;

    class RollupAggregationSynopsis : AggregationSynopsisBase<RollupAggregateTimeWindowQueue, RollupAggregateTimeWindow>
    {
        public RollupAggregationSynopsis(ProcessMetricEvent outputEventCallback, AggregationSettings settings)
            : base(outputEventCallback, settings)
        {
        }

        public void Update(MetricEvent metricEvt)
        {
            AggregateGroupKey key = new AggregateGroupKey(
                metricEvt.EventSource,
                metricEvt.InstanceId,
                metricEvt.TenantId,
                metricEvt.Dimensions,
                metricEvt.Name);

            Queue<MetricEvent> writeOutputEventQueue = null;

            lock (this.ThisLock)
            {
                // Lookup the correct time window queue
                RollupAggregateTimeWindowQueue timeWindowQueue = this.GetOrCreateAggregateTimeWindowQueue(key);

                // Update the time window queue
                timeWindowQueue.Update(key, metricEvt);

                // Check if we need to produce output events
                writeOutputEventQueue = this.ProduceOutputEvents();
            }

            this.EnqueueOutputEvents(writeOutputEventQueue);
        }

        protected override RollupAggregateTimeWindowQueue CreateAggregateTimeWindowQueue(Queue<MetricEvent> outputEventQueue)
        {
            return new RollupAggregateTimeWindowQueue(this.Settings, outputEventQueue);
        }
    }
}
