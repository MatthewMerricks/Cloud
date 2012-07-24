//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common.Events
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    // using Microsoft.AppFabric.Tracing;
    using Microsoft.ApplicationServer.Common.Events;

    abstract class AggregateTimeWindowQueueBase<T>
        where T : AggregateTimeWindowBase
    {
        public AggregateTimeWindowQueueBase(AggregationSettings settings, Queue<MetricEvent> outputEventQueue)
        {
            /*
            this.TimeWindowList = new List<T>();
            this.EndTimeLastProducedEvent = DateTime.MinValue;
            this.NewestEventTimeCreated = DateTime.MinValue;
            this.Settings = settings;
            this.OutputEventQueue = outputEventQueue;
             */
        }

        // TimeWindow objects are ordered from oldest to newest 
        protected List<T> TimeWindowList { get; set; }

        protected DateTime EndTimeLastProducedEvent { get; set; }

        protected DateTime NewestEventTimeCreated { get; set; }

        protected AggregationSettings Settings { get; set; }

        protected Queue<MetricEvent> OutputEventQueue { get; set; }

        public bool AdvanceTime(DateTime eventTime)
        {
            if (eventTime > this.NewestEventTimeCreated)
            {
                this.NewestEventTimeCreated = eventTime;
                return this.CleanExpiredTimeWindows();
            }

            return false;
        }

        public bool AdvanceTime()
        {
            DateTime currentTime = DateTime.UtcNow;

            // When advancing the internal clock using the timer, we employ a
            // special optimization to not advance the clock unnecessarily
            if (currentTime > (this.NewestEventTimeCreated + this.Settings.ClockExpirationWaitTime) &&
                this.NewestEventTimeCreated > DateTime.MinValue)
            {
                this.NewestEventTimeCreated = currentTime;
                return this.CleanExpiredTimeWindows();
            }

            return false;
        }

        protected static TimeSpan MinimumTimeSpan(TimeSpan timeSpan1, TimeSpan timeSpan2)
        {
            TimeSpan result = timeSpan1;
            if (timeSpan2 < result)
            {
                result = timeSpan2;
            }

            return result;
        }
        
        protected abstract T CreateAggregateTimeWindow(AggregateGroupKey key, DateTime startTime, TimeSpan timeWindow);

        // Check if we need to produce output events. Return true if we issued output
        protected bool CleanExpiredTimeWindows()
        {
            bool issuedOutput = false;

            // We only consider application time when computing the expiration time, not system time
            DateTime cutOffTime = this.NewestEventTimeCreated - this.Settings.ExtraExpirationWaitTime;

            // Find expired time windows
            while (this.TimeWindowList.Count > 0)
            {
                AggregateTimeWindowBase timeWindow = this.TimeWindowList[0];

                if (cutOffTime < timeWindow.EndTime)
                {
                    break;
                }

                MetricEvent metricEvent = timeWindow.ProduceOutput();
                issuedOutput = true;

                // If the last output was a zero metric, don't continue to produce zero metrics
                if (metricEvent.Count != 0)
                {
                    this.OutputEventQueue.Enqueue(metricEvent);
                }

                this.EndTimeLastProducedEvent = timeWindow.EndTime;
                this.TimeWindowList.RemoveAt(0);
            }

            return issuedOutput;
        }

        // Return true if we should discard the event
        protected bool ShouldDiscard(ResourceEvent evt)
        {
            // If the input event belongs to a time window which we already issued output for, we must discard the input event.
            // Otherwise we will be in a situation where the issued output events could overlap in time.
            if (evt.TimeCreated < this.EndTimeLastProducedEvent)
            {
                /*
                ManagementEtwProvider.Provider.EventWriteMonitoringDropEvent(
                    evt.Name, evt.EventSource, evt.TimeCreated.ToString(CultureInfo.InvariantCulture), this.EndTimeLastProducedEvent.ToString(CultureInfo.InvariantCulture));
                */
                return true;
            }

            return false;
        }

        protected bool ShouldDiscard(MetricEvent evt)
        {
            if (evt.TimeCreated < this.EndTimeLastProducedEvent)
            {
                /*
                ManagementEtwProvider.Provider.EventWriteMonitoringDropEvent(
                    evt.Name, evt.EventSource, evt.TimeCreated.ToString(CultureInfo.InvariantCulture), this.EndTimeLastProducedEvent.ToString(CultureInfo.InvariantCulture));
                */
                return true;
            }

            return false;
        }

        protected void FillMissingTimeWindowAtEnd(AggregateGroupKey key, DateTime metricStartTime, TimeSpan metricTimeWindowSpan)
        {
            DateTime currentStartTime = this.TimeWindowList[this.TimeWindowList.Count - 1].EndTime;
            if (currentStartTime < metricStartTime)
            {   // We need to construct TimeWindows to fill in missing gaps.
                // Use the smaller of the time intervals so that the missing gaps can be constructed to cover the timeline.
                TimeSpan currentTimeWindow = MinimumTimeSpan(this.TimeWindowList[this.TimeWindowList.Count - 1].TimeWindow, metricTimeWindowSpan);
                while (currentStartTime < metricStartTime)
                {
                    this.TimeWindowList.Add(this.CreateAggregateTimeWindow(key, currentStartTime, currentTimeWindow));
                    currentStartTime = currentStartTime.Add(currentTimeWindow);
                }
            }
        }

        protected void FillMissingTimeWindowAtBeginning(AggregateGroupKey key, DateTime metricEndTime, TimeSpan metricTimeWindowSpan)
        {
            DateTime currentStartTime = metricEndTime;
            if (currentStartTime < this.TimeWindowList[0].StartTime)
            {
                List<T> tempTimeWindowList = new List<T>();

                // We need to construct TimeWindows to fill in missing gaps.
                // Use the smaller of the time intervals so that the missing gaps can be constructed to cover the timeline.
                TimeSpan currentTimeWindow = MinimumTimeSpan(this.TimeWindowList[0].TimeWindow, metricTimeWindowSpan);
                while (currentStartTime < this.TimeWindowList[0].StartTime)
                {
                    tempTimeWindowList.Add(this.CreateAggregateTimeWindow(key, currentStartTime, currentTimeWindow));
                    currentStartTime = currentStartTime.Add(currentTimeWindow);
                }

                this.TimeWindowList.InsertRange(0, tempTimeWindowList);
            }
        }

        protected void AppendNewFoundTimeWindow(T aggregateTimeWindow, T nextAggregateTimeWindow)
        {
            this.TimeWindowList.Add(aggregateTimeWindow);

            // In the case when we append the new time window to the end, we also want to create the next time window as well
            // This is to cover the case where we still issue output with Count=0 for idle applications.
            this.TimeWindowList.Add(nextAggregateTimeWindow);
        }
    }
}

