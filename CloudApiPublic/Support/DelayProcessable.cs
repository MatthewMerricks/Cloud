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
using System.Windows;

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
        public bool DelayCompleted { get; private set; }
        #endregion

        #region private fields
        // counter for number for number of times delay has been reset for this instance
        private int delayCounter = 0;
        // boolean to ensure delay has been started before a call to reset the delay fires
        private bool startedDelay = false;
        // resetable timer, default to lock on first wait (not set)
        private readonly AutoResetEvent delayEvent = new AutoResetEvent(false);
        // locker for delay event (cannot lock on event while it's being disposed)
        private readonly object delayEventLocker = new object();
        // indicates when delay event is disposed
        private bool delayEventDisposed = false;
        // boolean to store whether call to reset delay has begun waiting for a pulse-back from processing thread
        private bool waitingForReset = false;
        // field where the synchronization locker is stored (locked when DelayCompleted boolean is read or written to)
        private object DelayCompletedLocker;
        // stores whether to throw errors when running the process-related methods
        // (set if the constructor is passed a locker parameter)
        private bool IsProcessable = false;

        private Queue<KeyValuePair<Action<T>, Action<CLError, T>>> SynchronizedPreprocessingActions = null;
        private static bool IsProcessThreadRunning = false;
        private static readonly Queue<KeyValuePair<KeyValuePair<Action<T, object, int>, Action<CLError, T, object, int>>, KeyValuePair<T, object>>> ProcessingQueue = new Queue<KeyValuePair<KeyValuePair<Action<T, object, int>, Action<CLError, T, object, int>>, KeyValuePair<T, object>>>();
        private static readonly ReaderWriterLockSlim ProcessTerminationLocker = new ReaderWriterLockSlim();
        private static bool ProcessingTerminated = false;
        #endregion

        /// <summary>
        /// Abstract constructor to ensure required parameters are available for instance methods,
        /// DelayCompletedLocker to lock upon delay completion must be provided for syncing the DelayCompleted boolean
        /// </summary>
        /// <param name="DelayCompletedLocker">Object to lock on to synchronize setting DelayCompleted boolean</param>
        protected DelayProcessable(object DelayCompletedLocker = null)
        {
            // Ensure locker is passed in constructor
            if (DelayCompletedLocker != null)
            {
                IsProcessable = true;
            }
            // Store locker to field
            this.DelayCompletedLocker = DelayCompletedLocker;
        }

        #region public methods
        /// <summary>
        /// ¡¡ Must restart application after running this before processing can run again !!
        /// </summary>
        public static void TerminateAllProcessing()
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
        public void ProcessAfterDelay(Action<T, object, int> toProcess, object userstate, int millisecondWait, int maxDelays, Action<CLError, T, object, int> errorHandler = null)
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

            ProcessAfterDelayParams delayParams = new ProcessAfterDelayParams()
            {
                toProcess = toProcess,
                userstate = userstate,
                millisecondWait = millisecondWait,
                maxDelays = maxDelays,
                errorHandler = errorHandler
            };

            // Instantiate and start the thread responsible for delaying the processing the action
            (new Thread(new ParameterizedThreadStart((state) =>
            {
                Nullable<KeyValuePair<DelayProcessable<T>, ProcessAfterDelayParams>> castState = state as Nullable<KeyValuePair<DelayProcessable<T>, ProcessAfterDelayParams>>;

                if (castState == null)
                {
                    MessageBox.Show("DelayProcessable delaying thread requires state to be castable as the appropriate type");
                }
                else
                {
                    KeyValuePair<DelayProcessable<T>, ProcessAfterDelayParams> nonNullState = (KeyValuePair<DelayProcessable<T>, ProcessAfterDelayParams>)castState;

                    // Loop waiting until ResetEvent is allowed to timeout to millisecondWait parameter
                    // or maxDelays is reached
                    while (true)
                    {
                        bool timerSet = false;
                        lock (nonNullState.Key.delayEventLocker)
                        {
                            if (nonNullState.Key.delayEventDisposed)
                            {
                                timerSet = true;
                            }
                        }

                        if (!timerSet)
                        {
                            try
                            {
                                // Wait on timer reset or timeout

                                // ¡¡ A bug in underlying threading code can cause the next line to throw an ObjectDisposedException !!
                                timerSet = nonNullState.Key.delayEvent.WaitOne(nonNullState.Value.millisecondWait)
                                    && (nonNullState.Value.maxDelays < 0
                                        || nonNullState.Key.delayCounter < nonNullState.Value.maxDelays);
                                // ¡¡ A bug in underlying threading code can cause the next line to throw an ObjectDisposedException !!
                            }
                            catch (ObjectDisposedException)
                            {
                                timerSet = true;
                            }
                        }

                        if (timerSet)
                        {
                            // break out of loop if delay already completed (such as on dispose)
                            if (nonNullState.Key.DelayCompleted)
                            {
                                break;
                            }

                            // If timer was reset on another thread and the delay counter is less than the amount allowed,
                            // Prepare timer for another waiting round

                            // Ensure the timer reset thread is waiting for synchronization by continually waiting and checking
                            while (nonNullState.Key.waitingForReset)
                            {
                                // Arbitrary time to wait for reset thread to synchronize, it may never even hit this sleep
                                Thread.Sleep(50);
                            }
                            // Lock on AutoResetEvent to synchronize with reset thread
                            lock (nonNullState.Key.delayEventLocker)
                            {
                                if (!nonNullState.Key.delayEventDisposed)
                                {
                                    lock (nonNullState.Key.delayEvent)
                                    {
                                        // Record new timer reset
                                        nonNullState.Key.delayCounter++;
                                        // Reset timer for waiting again on next loop
                                        nonNullState.Key.delayEvent.Reset();
                                        // Pulse reset thread to continue
                                        Monitor.Pulse(nonNullState.Key.delayEvent);
                                    }
                                }
                            }
                        }
                        // Timer completed (timed out), stop reset loop to continue onto processing
                        else
                        {
                            // stop reset loop to continue to processing
                            break;
                        }
                    }
                    // Lock on external object (passed as parameter) to synchronize with containing collection operations
                    lock (nonNullState.Key.DelayCompletedLocker)
                    {
                        // Store if delay was already completed
                        bool storeDelayAlreadyCompleted = nonNullState.Key.DelayCompleted;
                        // Mark that delay completed for external synchronization
                        nonNullState.Key.DelayCompleted = true;

                        // Lock on AutoResetEvent to synchronize with reset thread
                        lock (nonNullState.Key.delayEventLocker)
                        {
                            if (!nonNullState.Key.delayEventDisposed)
                            {
                                lock (nonNullState.Key.delayEvent)
                                {
                                    // Pulse remaining reset threads so they all continue
                                    Monitor.PulseAll(nonNullState.Key.delayEvent);
                                }
                            }
                        }

                        // Run any preprocessing actions queued in the synchronized context
                        if (nonNullState.Key.SynchronizedPreprocessingActions != null)
                        {
                            while (nonNullState.Key.SynchronizedPreprocessingActions.Count > 0)
                            {
                                KeyValuePair<Action<T>, Action<CLError, T>> dequeuedPreprocess = nonNullState.Key.SynchronizedPreprocessingActions.Dequeue();
                                try
                                {
                                    dequeuedPreprocess.Key((T)nonNullState.Key);
                                }
                                catch (Exception ex)
                                {
                                    if (dequeuedPreprocess.Value != null)
                                    {
                                        dequeuedPreprocess.Value(ex, (T)nonNullState.Key);
                                    }
                                }
                            }
                        }

                        // Only process the action if the delay had not previously been completed
                        if (!storeDelayAlreadyCompleted)
                        {
                            lock (ProcessingQueue)
                            {
                                ProcessingQueue.Enqueue(new KeyValuePair<KeyValuePair<Action<T, object, int>, Action<CLError, T, object, int>>, KeyValuePair<T, object>>(
                                    new KeyValuePair<Action<T, object, int>, Action<CLError, T, object, int>>(nonNullState.Value.toProcess, nonNullState.Value.errorHandler),
                                    new KeyValuePair<T, object>((T)nonNullState.Key, nonNullState.Value.userstate)));

                                if (!IsProcessThreadRunning)
                                {
                                    IsProcessThreadRunning = true;

                                    ThreadPool.UnsafeQueueUserWorkItem(PostDelayProcessor, null);
                                }
                            }
                        }
                    }
                }
            }))).Start(new KeyValuePair<DelayProcessable<T>, ProcessAfterDelayParams>((T)this, delayParams));// starts the new thread after it was defined
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
        public void SetDelayBackToInitialValue()
        {
            // Throw error when object is not processable
            if (!IsProcessable)
            {
                throw new NullReferenceException("DelayCompletedLocker cannot be null");
            }

            // lock on this to prevent reset from firing and checking the startedDelay boolean simultaneously
            lock (this)
            {
                // if action has not already been timer-delayed, throw error
                if (!startedDelay)
                {
                    // inactive delay error
                    throw new Exception("Delay not already started");
                }
                // if action has not already begun processing, initiate timer reset
                if (!DelayCompleted)
                {
                    // start waiting for reset (will cause processing thread to loop waiting till this thread is waiting for pulse-back on delayEvent)
                    waitingForReset = true;
                    // reset delay timer
                    delayEvent.Set();
                    // Lock on AutoResetEvent to synchronize with processing thread
                    lock (delayEventLocker)
                    {
                        if (!delayEventDisposed)
                        {
                            lock (delayEvent)
                            {
                                // stop waiting for reset, allows processing thread to continue and pulse-back
                                waitingForReset = false;
                                // wait for pulse-back
                                Monitor.Wait(delayEvent);
                            }
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
        public bool EnqueuePreprocessingAction(Action<T> toEnqueue, Action<CLError, T> errorHandler = null)
        {
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

                if (toEnqueue == null)
                {
                    return true;
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
            lock (this)
            {
                if (!DelayCompleted)
                {
                    // lock on current object for changing DelayCompleted so it cannot be stopped/started simultaneously
                    lock (this)
                    {
                        // set delay completed so processing will not fire
                        DelayCompleted = true;
                    }

                    // Run dispose on inner managed objects based on disposing condition
                    if (disposing)
                    {
                        lock (delayEventLocker)
                        {
                            if (!delayEventDisposed)
                            {
                                lock (delayEvent)
                                {
                                    Monitor.PulseAll(delayEvent);

                                    // trigger processing thread to break out
                                    delayEvent.Set();

                                    delayEvent.Dispose();

                                    delayEventDisposed = true;
                                }
                            }
                        }
                    }

                    // Dispose local unmanaged resources last
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