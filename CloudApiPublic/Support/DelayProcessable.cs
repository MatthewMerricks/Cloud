//
// DelayProcessable.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace CloudApiPublic.Support
{
    /// <summary>
    /// Implement this abstract class on a data class that requires delayed processing,
    /// meant to be used in a context with a synchronized container (DelayCompletedLocker parameter)
    /// with completion notification via property DelayCompleted
    /// </summary>
    /// <typeparam name="T">Use the same type of class as the generic-typed parameter</typeparam>
    public abstract class DelayProcessable<T> : IDisposable where T : DelayProcessable<T>
    {
        #region public property
        /// <summary>
        /// When locked on the container instance (that was passed in as DelayCompletedLocker parameter on construction),
        /// this boolean can be used to tell if the delay has completed (and processing has begun)
        /// </summary>
        internal bool DelayCompleted { get; private set; }
        #endregion

        #region private fields
        // counter for number for number of times delay has been reset for this instance
        private int delayCounter = 0;
        // boolean to ensure delay has been started before a call to reset the delay fires
        private bool startedDelay = false;
        // field where the synchronization locker is stored (locked when DelayCompleted boolean is read or written to)
        private readonly object DelayCompletedLocker;
        // stores whether to throw errors when running the process-related methods
        // (set if the constructor is passed a locker parameter)
        private bool IsProcessable = false;

        private Queue<KeyValuePair<Action<T>, Action<CLError, T>>> SynchronizedPreprocessingActions = null;
        private static bool IsProcessThreadRunning = false;
        private static readonly Queue<KeyValuePair<KeyValuePair<Action<T, object, int>, Action<CLError, T, object, int>>, KeyValuePair<T, object>>> ProcessingQueue = new Queue<KeyValuePair<KeyValuePair<Action<T, object, int>, Action<CLError, T, object, int>>, KeyValuePair<T, object>>>();
        private static readonly ReaderWriterLockSlim ProcessTerminationLocker = new ReaderWriterLockSlim();
        private static bool ProcessingTerminated = false;

        private static bool TimeReaderRunning = false;
        private static readonly LinkedList<Tuple<DelayProcessable<T>, Action<T, object, int>, object, int, Action<CLError, T, object, int>>> DelayQueue = new LinkedList<Tuple<DelayProcessable<T>, Action<T, object, int>, object, int, Action<CLError, T, object, int>>>();
        private static int highestMillisecondWaitInBatch = 0;
        private static DateTime MaxDateTime = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
        private static DateTime MinDateTime = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        private DateTime StartedProcessingTime = MaxDateTime;
        private int ResetCounter = 0;
        #endregion

        protected DelayProcessable() : this(null) { }

        /// <summary>
        /// Abstract constructor to ensure required parameters are available for instance methods,
        /// DelayCompletedLocker to lock upon delay completion must be provided for syncing the DelayCompleted boolean
        /// </summary>
        /// <param name="DelayCompletedLocker">Object to lock on to synchronize setting DelayCompleted boolean</param>
        internal protected DelayProcessable(object DelayCompletedLocker = null)
        {
            // Ensure locker is passed in constructor
            if (DelayCompletedLocker != null)
            {
                IsProcessable = true;
            }
            // Store locker to field
            this.DelayCompletedLocker = DelayCompletedLocker;
        }

        #region internal methods
        /// <summary>
        /// ¡¡ Must restart application after running this before processing can run again !!
        /// </summary>
        internal static void TerminateAllProcessing()
        {
            ProcessTerminationLocker.EnterWriteLock();
            try
            {
                ProcessingTerminated = true;
            }
            finally
            {
                ProcessTerminationLocker.ExitWriteLock();
            }
        }

        /// <summary>
        /// Allows an action to be delayed taking this current instance and an optional userstate,
        /// delay is set in milliseconds and has a parameter for maximum number of allowed delays before it will process anyways,
        /// DelayCompletedLocker to lock upon delay completion must be provided for syncing the DelayCompleted boolean
        /// </summary>
        /// <param name="toProcess">Action to delay</param>
        /// <param name="userstate">Optional userstate passed upon processing the action</param>
        /// <param name="millisecondWait">Wait time in milliseconds before action is processed</param>
        /// <param name="maxDelays">Maximum times processing can be delayed before it processes anyways</param>
        internal void ProcessAfterDelay(Action<T, object, int> toProcess, object userstate, int millisecondWait, int maxDelays, Action<CLError, T, object, int> errorHandler = null)
        {
            // Throw error when object is not processable
            if (!IsProcessable)
            {
                throw new NullReferenceException("DelayCompletedLocker cannot be null");
            }

            ProcessTerminationLocker.EnterReadLock();
            try
            {
                if (ProcessingTerminated)
                {
                    throw new ObjectDisposedException("ProcessingTerminated");
                }
            }
            finally
            {
                ProcessTerminationLocker.ExitReadLock();
            }

            // lock on this to prevent reset from firing and checking the boolean simultaneously
            lock (this)
            {
                // If action has already been delay-processed, throw error
                if (startedDelay)
                {
                    // Already processed error
                    throw new Exception("This DelayProcessable instance has already been delay-processed");
                }
                // mark this instance as started for the delay so it can be reset
                startedDelay = true;
            }

            lock (DelayQueue)
            {
                if (millisecondWait > highestMillisecondWaitInBatch)
                {
                    highestMillisecondWaitInBatch = millisecondWait;
                }

                StartedProcessingTime = DateTime.UtcNow;

                DelayQueue.AddLast(new Tuple<DelayProcessable<T>, Action<T, object, int>, object, int, Action<CLError, T, object, int>>(this, toProcess, userstate, maxDelays, errorHandler));

                if (!TimeReaderRunning)
                {
                    ThreadPool.UnsafeQueueUserWorkItem(TimeReaderProcess, null);
                    TimeReaderRunning = true;
                }
            }
        }

        private class ProcessAfterDelayParams
        {
            public Action<T, object, int> toProcess { get; set; }
            public object userstate { get; set; }
            public int millisecondWait { get; set; }
            public int maxDelays { get; set; }
            public Action<CLError, T, object, int> errorHandler { get; set; }
        }

        /// <summary>
        /// Resets the delay on an action that was previous timer-delayed to fire,
        /// must be called after the action has been set for processing
        /// </summary>
        internal void SetDelayBackToInitialValue()
        {
            // Throw error when object is not processable
            if (!IsProcessable)
            {
                throw new NullReferenceException("DelayCompletedLocker cannot be null");
            }

            lock (DelayQueue)
            {
                lock (this)
                {
                    if (!startedDelay)
                    {
                        // delay is already at initial value
                        return;
                    }
                }

                if (!DelayCompleted)
                {
                    LinkedListNode<Tuple<DelayProcessable<T>, Action<T, object, int>, object, int, Action<CLError, T, object, int>>> findThisNode = DelayQueue.First;
                    while (findThisNode != null)
                    {
                        if (findThisNode.Value.Item1 == this)
                        {
                            break;
                        }

                        findThisNode = findThisNode.Next;
                    }

                    if (findThisNode != null
                        && findThisNode.Value.Item1.StartedProcessingTime.CompareTo(MinDateTime) != 0)
                    {
                        Tuple<DelayProcessable<T>, Action<T, object, int>, object, int, Action<CLError, T, object, int>> thisNodeValue = findThisNode.Value;

                        DelayQueue.Remove(findThisNode);

                        ResetCounter++;

                        if (thisNodeValue.Item4 > ResetCounter)
                        {
                            StartedProcessingTime = MinDateTime;

                            DelayQueue.AddFirst(thisNodeValue);
                        }
                        else
                        {
                            StartedProcessingTime = DateTime.UtcNow;

                            DelayQueue.AddLast(thisNodeValue);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds an action to run before the primary action which will synchronize on the DelayCompletedLocker
        /// </summary>
        /// <param name="toEnqueue">Action to run on preprocessing</param>
        /// <returns>Returns true if action is accepted for preprocessing, otherwise false means action will not be run</returns>
        internal bool EnqueuePreprocessingAction(Action<T> toEnqueue, Action<CLError, T> errorHandler = null)
        {
            if (toEnqueue == null)
            {
                return true;
            }

            // Throw error when object is not processable
            if (!IsProcessable)
            {
                throw new NullReferenceException("DelayCompletedLocker cannot be null");
            }

            // lock on the same synchronization object as the DelayCompleted boolean to check
            lock (DelayCompletedLocker)
            {
                // if DelayCompleted is true then processing already started and the enqueued action would not process
                if (DelayCompleted)
                {
                    return false;
                }

                // create the queue if necessary
                if (SynchronizedPreprocessingActions == null)
                {
                    SynchronizedPreprocessingActions = new Queue<KeyValuePair<Action<T>, Action<CLError, T>>>();
                }
                // enqueue the action to run before normal processing
                SynchronizedPreprocessingActions.Enqueue(new KeyValuePair<Action<T>, Action<CLError, T>>(toEnqueue,
                    errorHandler));
            }
            return true;
        }
        #endregion

        #region IDisposable members
        // Standard IDisposable implementation based on MSDN System.IDisposable
        ~DelayProcessable()
        {
            Dispose(false);
        }
        // Standard IDisposable implementation based on MSDN System.IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region private methods
        // Standard IDisposable implementation based on MSDN System.IDisposable
        private void Dispose(bool disposing)
        {
            if (DelayCompletedLocker == null)
            {
                ProcessDisposeCompletion();
            }
            else
            {
                lock (DelayCompletedLocker)
                {
                    ProcessDisposeCompletion();
                }
            }
        }
        private void ProcessDisposeCompletion()
        {
            if (!DelayCompleted)
            {
                // set delay completed so processing will not fire
                DelayCompleted = true;

                // Dispose local unmanaged resources last
            }
        }

        private static void TimeReaderProcess(object state)
        {
            Func<bool> checkDisposed = () =>
            {
                ProcessTerminationLocker.EnterReadLock();
                try
                {
                    return ProcessingTerminated;
                }
                finally
                {
                    ProcessTerminationLocker.ExitReadLock();
                }
            };

            while (!checkDisposed())
            {
                List<Tuple<DelayProcessable<T>, Action<T, object, int>, object, int, Action<CLError, T, object, int>>> expiredTimers = new List<Tuple<DelayProcessable<T>, Action<T, object, int>, object, int, Action<CLError, T, object, int>>>();

                lock (DelayQueue)
                {
                    DateTime latestTimeAllowed = DateTime.UtcNow.Subtract(TimeSpan.FromMilliseconds(highestMillisecondWaitInBatch));

                    LinkedListNode<Tuple<DelayProcessable<T>, Action<T, object, int>, object, int, Action<CLError, T, object, int>>> currentToCheck = DelayQueue.First;

                    if (currentToCheck == null)
                    {
                        TimeReaderRunning = false;
                        return;
                    }

                    while (currentToCheck != null)
                    {
                        if (latestTimeAllowed.CompareTo(currentToCheck.Value.Item1.StartedProcessingTime) < 0)
                        {
                            break;
                        }

                        expiredTimers.Add(currentToCheck.Value);
                        currentToCheck = currentToCheck.Next;
                        DelayQueue.RemoveFirst();
                    }

                    if (DelayQueue.Count == 0)
                    {
                        highestMillisecondWaitInBatch = 0;
                    }
                }

                bool atLeastOneExpiredTimerFired = false;

                if (expiredTimers.Count > 0)
                {
                    lock (ProcessingQueue)
                    {
                        foreach (Tuple<DelayProcessable<T>, Action<T, object, int>, object, int, Action<CLError, T, object, int>> currentExpiredTimer in expiredTimers)
                        {
                            lock (currentExpiredTimer.Item1.DelayCompletedLocker)
                            {
                                if (currentExpiredTimer.Item1.DelayCompleted)
                                {
                                    continue;
                                }

                                atLeastOneExpiredTimerFired = true;

                                // Run any preprocessing actions queued in the synchronized context
                                if (currentExpiredTimer.Item1.SynchronizedPreprocessingActions != null
                                    && currentExpiredTimer.Item1.SynchronizedPreprocessingActions.Count > 0)
                                {
                                    lock (currentExpiredTimer.Item1.DelayCompletedLocker)
                                    {
                                        while (currentExpiredTimer.Item1.SynchronizedPreprocessingActions.Count > 0)
                                        {
                                            KeyValuePair<Action<T>, Action<CLError, T>> dequeuedPreprocess = currentExpiredTimer.Item1.SynchronizedPreprocessingActions.Dequeue();
                                            try
                                            {
                                                dequeuedPreprocess.Key((T)currentExpiredTimer.Item1);
                                            }
                                            catch (Exception ex)
                                            {
                                                if (dequeuedPreprocess.Value != null)
                                                {
                                                    dequeuedPreprocess.Value(ex, (T)currentExpiredTimer.Item1);
                                                }
                                            }
                                        }
                                    }
                                }

                                currentExpiredTimer.Item1.DelayCompleted = true;
                            }

                            ProcessingQueue.Enqueue(new KeyValuePair<KeyValuePair<Action<T, object, int>, Action<CLError, T, object, int>>, KeyValuePair<T, object>>(
                                new KeyValuePair<Action<T, object, int>, Action<CLError, T, object, int>>(currentExpiredTimer.Item2, currentExpiredTimer.Item5),
                                new KeyValuePair<T, object>((T)currentExpiredTimer.Item1, currentExpiredTimer.Item3)));
                        }

                        if (atLeastOneExpiredTimerFired
                            && !IsProcessThreadRunning)
                        {
                            IsProcessThreadRunning = true;

                            ThreadPool.UnsafeQueueUserWorkItem(PostDelayProcessor, null);
                        }
                    }
                }

                if (!atLeastOneExpiredTimerFired)
                {
                    Thread.Sleep(250);
                }
            }
        }

        private static void PostDelayProcessor(object state)
        {
            Func<bool> checkDisposed = () =>
            {
                ProcessTerminationLocker.EnterReadLock();
                try
                {
                    return ProcessingTerminated;
                }
                finally
                {
                    ProcessTerminationLocker.ExitReadLock();
                }
            };

            while (!checkDisposed())
            {
                KeyValuePair<KeyValuePair<Action<T, object, int>, Action<CLError, T, object, int>>, KeyValuePair<T, object>> toRun;
                int remainingCount;
                lock (ProcessingQueue)
                {
                    if (ProcessingQueue.Count == 0)
                    {
                        IsProcessThreadRunning = false;
                        break;
                    }

                    toRun = ProcessingQueue.Dequeue();
                    remainingCount = ProcessingQueue.Count;
                }

                try
                {
                    toRun.Key.Key(toRun.Value.Key, toRun.Value.Value, remainingCount);
                }
                catch (Exception ex)
                {
                    if (toRun.Key.Value != null)
                    {
                        toRun.Key.Value(ex, toRun.Value.Key, toRun.Value.Value, remainingCount);
                    }
                }
            }
        }
        #endregion
    }
}