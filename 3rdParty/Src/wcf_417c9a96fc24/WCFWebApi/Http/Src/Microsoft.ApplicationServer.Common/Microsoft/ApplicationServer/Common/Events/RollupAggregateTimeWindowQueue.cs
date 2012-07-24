//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common.Events
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    //using Microsoft.AppFabric.Tracing;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Common.Events;

    class RollupAggregateTimeWindowQueue : AggregateTimeWindowQueueBase<RollupAggregateTimeWindow>
    {
        public RollupAggregateTimeWindowQueue(AggregationSettings settings, Queue<MetricEvent> outputEventQueue)
            : base(settings, outputEventQueue)
        {
        }

        public void Update(AggregateGroupKey key, MetricEvent evt)
        {
            Fx.Assert(evt.TimeWindow != AggregationDefaults.TimeWindowCurrent, "We don't support processing speculative metric events");

            if (this.ShouldDiscard(evt))
            {
                return;
            }

            RollupAggregateTimeWindow[] aggregateTimeWindows = this.GetOrCreateAggregateTimeWindow(key, evt);
            if (aggregateTimeWindows.Length == 1)
            {
                aggregateTimeWindows[0].Update(evt);
            }
            else
            {
                UpdateMultipleTimeWindows(evt, aggregateTimeWindows);
            }

            this.AdvanceTime(evt.TimeCreated);
            this.ProduceCurrentOutput();
        }

        protected override RollupAggregateTimeWindow CreateAggregateTimeWindow(AggregateGroupKey key, DateTime startTime, TimeSpan timeWindow)
        {
            return RollupAggregateTimeWindow.Create(key, startTime, timeWindow);
        }

        static void UpdateMultipleTimeWindows(MetricEvent metricEvent, RollupAggregateTimeWindow[] aggregateTimeWindows)
        {
            Fx.Assert(aggregateTimeWindows.Length > 1, "AggregateTimeWindows array length greater than 1");

            int countIncrement = metricEvent.Count / aggregateTimeWindows.Length;
            int countRemainder = metricEvent.Count % aggregateTimeWindows.Length;
            double total = metricEvent.Average * metricEvent.Count;
            double totalIncrement = total / aggregateTimeWindows.Length;

            // Spread the contributions of metricEvent.Count and metricEvent.Value evenly across the different time windows.
            MetricEvent tempMetricEvent = metricEvent.Clone();
            tempMetricEvent.Count = countIncrement;
            tempMetricEvent.Total = totalIncrement;
            tempMetricEvent.Average = totalIncrement / tempMetricEvent.Count;
            
            for (int i = 0; i < aggregateTimeWindows.Length - 1; i++)
            {
                aggregateTimeWindows[i].Update(tempMetricEvent);
            }

            // Any remaining contribution is added to the last time window.
            tempMetricEvent.Count = countIncrement + countRemainder;
            tempMetricEvent.Total = total - ((aggregateTimeWindows.Length - 1) * totalIncrement);
            tempMetricEvent.Average = tempMetricEvent.Total / tempMetricEvent.Count;
            aggregateTimeWindows[aggregateTimeWindows.Length - 1].Update(tempMetricEvent);
        }

        void ProduceCurrentOutput()
        {
            if (this.TimeWindowList.Count > 0)
            {
                // Current output should be calculated over all existing time windows that are being tracking.  The timestamp should be set to the earliest metric event.
                MetricEvent currentMetricEvent = this.TimeWindowList[0].ProduceOutput();
                currentMetricEvent.TimeWindow = AggregationDefaults.TimeWindowCurrent;

                for (int i = 1; i < this.TimeWindowList.Count; i++)
                {
                    MetricEvent metricEvent = this.TimeWindowList[i].ProduceOutput();
                    if (metricEvent.Count > 0)
                    {
                        currentMetricEvent.Total += metricEvent.Total;
                        currentMetricEvent.Count += metricEvent.Count;

                        if (metricEvent.Maximum > currentMetricEvent.Maximum)
                        {
                            currentMetricEvent.Maximum = metricEvent.Maximum;
                        }

                        if (metricEvent.Minimum > currentMetricEvent.Minimum)
                        {
                            currentMetricEvent.Minimum = metricEvent.Minimum;
                        }

                        if (!string.IsNullOrEmpty(metricEvent.AdditionalData))
                        {
                            currentMetricEvent.AdditionalData = metricEvent.AdditionalData;
                        }
                    }
                }

                if (currentMetricEvent.Count > 0)
                {
                    currentMetricEvent.Average = currentMetricEvent.Total / currentMetricEvent.Count;
                }

                this.OutputEventQueue.Enqueue(currentMetricEvent);
            }
        }

        // Handles interval based events
        RollupAggregateTimeWindow[] GetOrCreateAggregateTimeWindow(AggregateGroupKey key, MetricEvent metricEvent)
        {
            TimeSpan timeWindow = metricEvent.TimeWindow;
            DateTimeOffset metricEndTime = metricEvent.TimeCreated.Add(timeWindow);

            List<RollupAggregateTimeWindow> foundAggregateTimeWindows = new List<RollupAggregateTimeWindow>();

            // Find the correct TimeWindow that the input event belongs to            
            foreach (RollupAggregateTimeWindow aggregateTimeWindow in this.TimeWindowList)
            {
                if ((metricEvent.TimeCreated >= aggregateTimeWindow.StartTime &&
                    metricEndTime <= aggregateTimeWindow.EndTime) ||
                    (metricEvent.TimeCreated <= aggregateTimeWindow.StartTime &&
                    metricEndTime >= aggregateTimeWindow.EndTime))
                {
                    foundAggregateTimeWindows.Add(aggregateTimeWindow);
                }
                
                if (metricEndTime < aggregateTimeWindow.StartTime)
                {
                    break;
                }
            }

            // If the TimeWindow does not exist, create a new TimeWindow
            if (foundAggregateTimeWindows.Count == 0)
            {
                RollupAggregateTimeWindow foundAggregateTimeWindow = RollupAggregateTimeWindow.Create(key, metricEvent.TimeCreated, timeWindow);
                foundAggregateTimeWindows.Add(foundAggregateTimeWindow);

                if (this.TimeWindowList.Count == 0)
                {
                    this.AppendNewFoundTimeWindow(foundAggregateTimeWindow, RollupAggregateTimeWindow.Create(key, metricEvent.TimeCreated.Add(timeWindow), timeWindow));
                }
                else if (foundAggregateTimeWindow.StartTime >= this.TimeWindowList[this.TimeWindowList.Count - 1].EndTime)
                {  // New TimeWindow belongs at the end.  
                    this.FillMissingTimeWindowAtEnd(key, metricEvent.TimeCreated, timeWindow);
                    this.AppendNewFoundTimeWindow(foundAggregateTimeWindow, RollupAggregateTimeWindow.Create(key, metricEvent.TimeCreated.Add(timeWindow), timeWindow));
                }
                else
                {  // New TimeWindow belongs at the beginning of the list.
                    this.FillMissingTimeWindowAtBeginning(key, foundAggregateTimeWindow.EndTime, timeWindow);
                    this.TimeWindowList.Insert(0, foundAggregateTimeWindow);
                }
            }
            else
            { // In this case, we found matching time windows.
                if (foundAggregateTimeWindows[foundAggregateTimeWindows.Count - 1].EndTime < metricEndTime)
                {
                    // In this case, the input event partially overlaps existing time window objects.  We need to create the remaining windows on the right side.
                    DateTime currentStartTime = foundAggregateTimeWindows[foundAggregateTimeWindows.Count - 1].EndTime;
                    TimeSpan currentTimeWindowSpan = foundAggregateTimeWindows[foundAggregateTimeWindows.Count - 1].TimeWindow;

                    while (currentStartTime < metricEndTime)
                    {
                        RollupAggregateTimeWindow currentTimeWindow = RollupAggregateTimeWindow.Create(key, currentStartTime, currentTimeWindowSpan);
                        foundAggregateTimeWindows.Add(currentTimeWindow);
                        this.TimeWindowList.Add(currentTimeWindow);
                        currentStartTime = currentStartTime.Add(currentTimeWindowSpan);
                    }
                }

                if (metricEvent.TimeCreated < foundAggregateTimeWindows[0].StartTime)
                {
                    // In this case, the input event partially overlaps existing time window objects.  We need to create the remaining windows on the left side.
                    DateTime currentStartTime = metricEvent.TimeCreated;
                    TimeSpan currentTimeWindowSpan = foundAggregateTimeWindows[0].TimeWindow;
                    List<RollupAggregateTimeWindow> tempTimeWindowList = new List<RollupAggregateTimeWindow>();

                    while (currentStartTime < foundAggregateTimeWindows[0].StartTime)
                    {
                        RollupAggregateTimeWindow currentTimeWindow = RollupAggregateTimeWindow.Create(key, currentStartTime, currentTimeWindowSpan);
                        tempTimeWindowList.Add(currentTimeWindow);
                        currentStartTime = currentStartTime.Add(currentTimeWindowSpan);
                    }

                    this.TimeWindowList.InsertRange(0, tempTimeWindowList);
                    foundAggregateTimeWindows.InsertRange(0, tempTimeWindowList);
                }
            }

            return foundAggregateTimeWindows.ToArray();
        }
    }
}
