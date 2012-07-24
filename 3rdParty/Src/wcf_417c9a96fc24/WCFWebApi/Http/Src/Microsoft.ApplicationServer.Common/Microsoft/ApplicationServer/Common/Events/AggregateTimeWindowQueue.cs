//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common.Events
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Microsoft.ApplicationServer.Common.Events;

    class AggregateTimeWindowQueue : AggregateTimeWindowQueueBase<AggregateTimeWindow>
    {
        public AggregateTimeWindowQueue(AggregationSettings settings, Queue<MetricEvent> outputEventQueue)
            : base(settings, outputEventQueue)
        {
        }

        public void Update(AggregateGroupKey key, ResourceEvent evt)
        {
            if (this.ShouldDiscard(evt))
            {
                return;
            }

            AggregateTimeWindow aggregateTimeWindow = this.GetOrCreateAggregateTimeWindow(key, evt, this.Settings.TimeWindow);
            aggregateTimeWindow.Update(evt);

            this.AdvanceTime(evt.TimeCreated);
        }

        protected override AggregateTimeWindow CreateAggregateTimeWindow(AggregateGroupKey key, DateTime startTime, TimeSpan timeWindow)
        {
            return AggregateTimeWindow.Create(key, startTime, timeWindow);
        }

        AggregateTimeWindow GetOrCreateAggregateTimeWindow(AggregateGroupKey key, ResourceEvent evt, TimeSpan timeWindow)
        {
            AggregateTimeWindow foundAggregateTimeWindow = null;

            // Find the correct TimeWindow that the input event belongs to
            foreach (AggregateTimeWindow aggregateTimeWindow in this.TimeWindowList)
            {
                // When checking if an event belongs to a time window, the window is represented as [a, b)
                if (evt.TimeCreated >= aggregateTimeWindow.StartTime &&
                    evt.TimeCreated < aggregateTimeWindow.EndTime)
                {
                    foundAggregateTimeWindow = aggregateTimeWindow;
                    break;
                }
            }

            // If the TimeWindow does not exist, create a new TimeWindow
            if (foundAggregateTimeWindow == null)
            {
                // We want time windows to start on well defined boundaries
                DateTime startTime = AggregationUtility.SnapTimeWindowStartTime(evt.TimeCreated, timeWindow);
                TimeSpan timeWindowSpan = timeWindow;

                foundAggregateTimeWindow = AggregateTimeWindow.Create(key, startTime, timeWindowSpan);
                if (this.TimeWindowList.Count == 0)
                {
                    this.AppendNewFoundTimeWindow(foundAggregateTimeWindow, AggregateTimeWindow.Create(key, startTime.Add(timeWindowSpan), timeWindowSpan));
                }
                else if (foundAggregateTimeWindow.StartTime >= this.TimeWindowList[this.TimeWindowList.Count - 1].EndTime)
                {   // New TimeWindow belongs at the end.  
                    this.FillMissingTimeWindowAtEnd(key, startTime, timeWindowSpan);
                    this.AppendNewFoundTimeWindow(foundAggregateTimeWindow, AggregateTimeWindow.Create(key, startTime.Add(timeWindowSpan), timeWindowSpan));
                }
                else
                {   // New TimeWindow belongs at the beginning of the list.
                    this.FillMissingTimeWindowAtBeginning(key, foundAggregateTimeWindow.EndTime, timeWindowSpan);
                    this.TimeWindowList.Insert(0, foundAggregateTimeWindow);
                }
            }

            return foundAggregateTimeWindow;
        }
    }
}

