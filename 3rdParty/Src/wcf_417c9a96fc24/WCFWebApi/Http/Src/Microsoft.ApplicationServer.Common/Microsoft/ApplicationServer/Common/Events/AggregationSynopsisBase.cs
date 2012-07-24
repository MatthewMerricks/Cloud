//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common.Events
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.ApplicationServer.Common.Events;

    abstract class AggregationSynopsisBase<TQueue, TWindow> : IDisposable
        where TQueue : AggregateTimeWindowQueueBase<TWindow>
        where TWindow : AggregateTimeWindowBase
    {
        Dictionary<AggregateGroupKey, TQueue> timeWindowQueueMap;
        ClockEventGenerator clockGenerator;

        ProcessMetricEvent outputEventCallback;

        bool disposed;

        public AggregationSynopsisBase(ProcessMetricEvent outputEventCallback, AggregationSettings settings)
        {
            this.outputEventCallback = outputEventCallback;
            this.timeWindowQueueMap = new Dictionary<AggregateGroupKey, TQueue>();
            this.OutputEventQueue = new Queue<MetricEvent>();
            this.Settings = settings;
            this.ThisLock = new object();

            if (this.Settings.EnableExpirationClock)
            {
                this.clockGenerator = new ClockEventGenerator(settings.ExtraExpirationWaitTime);
                this.clockGenerator.ClockNotfication += new EventHandler(this.ClockNotfication);
            }
        }

        // Contains set of events issued as output
        public Queue<MetricEvent> OutputEventQueue { get; set; }

        public AggregationSettings Settings { get; private set; }

        protected object ThisLock { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract TQueue CreateAggregateTimeWindowQueue(Queue<MetricEvent> outputEventQueue);

        protected TQueue GetOrCreateAggregateTimeWindowQueue(AggregateGroupKey key)
        {
            TQueue timeWindowQueue;
            if (!this.timeWindowQueueMap.TryGetValue(key, out timeWindowQueue))
            {
                timeWindowQueue = this.CreateAggregateTimeWindowQueue(this.OutputEventQueue);
                this.timeWindowQueueMap.Add(key, timeWindowQueue);
            }

            return timeWindowQueue;
        }

        protected Queue<MetricEvent> ProduceOutputEvents()
        {
            if (this.OutputEventQueue.Count <= 0)
            {
                return null;
            }

            Queue<MetricEvent> writeOutputEventQueue = new Queue<MetricEvent>();
            while (this.OutputEventQueue.Count > 0)
            {
                writeOutputEventQueue.Enqueue(this.OutputEventQueue.Dequeue());
            }

            return writeOutputEventQueue;
        }

        protected void EnqueueOutputEvents(Queue<MetricEvent> writeOutputEventQueue)
        {
            if (writeOutputEventQueue != null)
            {
                while (writeOutputEventQueue.Count > 0)
                {
                    MetricEvent outputEvent = writeOutputEventQueue.Dequeue();
                    this.outputEventCallback(outputEvent);
                }
            }
        }

        void ClockNotfication(object sender, EventArgs e)
        {
            Queue<MetricEvent> writeOutputEventQueue = null;

            lock (this.ThisLock)
            {
                foreach (TQueue timeWindowQueue in this.timeWindowQueueMap.Values)
                {
                    timeWindowQueue.AdvanceTime();
                }

                // Check if we need to produce output events
                writeOutputEventQueue = this.ProduceOutputEvents();
            }

            this.EnqueueOutputEvents(writeOutputEventQueue);
        }

        void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (this.clockGenerator != null)
                    {
                        this.clockGenerator.Dispose();
                        this.clockGenerator = null;
                    }
                }

                this.disposed = true;
            }
        }

        class ClockEventGenerator : IDisposable
        {
            Timer clockTimer;
            int clockFrequencyInMilliseconds;
            bool disposed;
            object thisLock;

            public ClockEventGenerator(TimeSpan clockFrequency)
            {
                this.clockFrequencyInMilliseconds = (int)clockFrequency.TotalMilliseconds;
                this.clockTimer = new Timer(new TimerCallback(this.HandleTimerCallback));
                this.clockTimer.Change(this.clockFrequencyInMilliseconds, Timeout.Infinite);
                this.thisLock = new object();
            }

            public event EventHandler ClockNotfication;

            public void Dispose()
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }

            void HandleTimerCallback(object state)
            {
                try
                {
                    if (this.ClockNotfication != null)
                    {
                        this.ClockNotfication(this, EventArgs.Empty);
                    }
                }
                finally
                {
                    if (this.clockTimer != null)
                    {
                        lock (this.thisLock)
                        {
                            if (this.clockTimer != null)
                            {
                                this.clockTimer.Change(this.clockFrequencyInMilliseconds, Timeout.Infinite);
                            }
                        }
                    }
                }
            }

            void Dispose(bool disposing)
            {
                if (!this.disposed)
                {
                    if (disposing)
                    {
                        lock (this.thisLock)
                        {
                            if (this.clockTimer != null)
                            {
                                this.clockTimer.Change(Timeout.Infinite, 0);
                                this.clockTimer.Dispose();
                                this.clockTimer = null;
                            }
                        }
                    }

                    this.disposed = true;
                }
            }
        }
    }
}
