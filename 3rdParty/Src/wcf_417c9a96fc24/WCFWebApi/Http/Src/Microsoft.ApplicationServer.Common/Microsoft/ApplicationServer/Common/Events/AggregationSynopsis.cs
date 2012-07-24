//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common.Events
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationServer.Common.Events;

    class AggregationSynopsis : AggregationSynopsisBase<AggregateTimeWindowQueue, AggregateTimeWindow>
    {
        public AggregationSynopsis(ProcessMetricEvent outputEventCallback, AggregationSettings settings)
            : base(outputEventCallback, settings)
        {
        }

        // Update the synopsis state as a result of new input event
        public void Update(ResourceEvent rawEvent)
        {
            Queue<MetricEvent> writeOutputEventQueue = null;

            lock (this.ThisLock)
            {
                AggregateGroupKey key = new AggregateGroupKey(
                    rawEvent.EventSource,
                    rawEvent.InstanceId,
                    rawEvent.TenantId,
                    rawEvent.Dimensions,
                    rawEvent.Name);

                // Lookup the correct time window queue
                AggregateTimeWindowQueue timeWindowQueue = this.GetOrCreateAggregateTimeWindowQueue(key);

                // Update the time window queue
                timeWindowQueue.Update(key, rawEvent);

                // Check if we need to produce output events
                writeOutputEventQueue = this.ProduceOutputEvents();
            }
        
            this.EnqueueOutputEvents(writeOutputEventQueue);
        }

        protected override AggregateTimeWindowQueue CreateAggregateTimeWindowQueue(Queue<MetricEvent> outputEventQueue)
        {
            return new AggregateTimeWindowQueue(this.Settings, outputEventQueue);
        }
    }
}
