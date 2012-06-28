//
// DelayProcessable.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FileMonitor
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
        private AutoResetEvent delayEvent = new AutoResetEvent(false);
        // boolean to store whether call to reset delay has begun waiting for a pulse-back from processing thread
        private bool waitingForReset = false;
        // field where the synchronization locker is stored (locked when DelayCompleted boolean is read or written to)
        private object DelayCompletedLocker;
        // stores whether to throw errors when running the process-related methods
        // (set if the constructor is passed a locker parameter)
        private bool IsProcessable = false;
        private Queue<Action> SynchronizedPreprocessingActions = new Queue<Action>();
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
        /// Allows an action to be delayed taking this current instance and an optional userstate,
        /// delay is set in milliseconds and has a parameter for maximum number of allowed delays before it will process anyways,
        /// DelayCompletedLocker to lock upon delay completion must be provided for syncing the DelayCompleted boolean
        /// </summary>
        /// <param name="toProcess">Action to delay</param>
        /// <param name="userstate">Optional userstate passed upon processing the action</param>
        /// <param name="millisecondWait">Wait time in milliseconds before action is processed</param>
        /// <param name="maxDelays">Maximum times processing can be delayed before it processes anyways</param>
        public void ProcessAfterDelay(Action<T, object> toProcess, object userstate, int millisecondWait, int maxDelays)
        {
            // Throw error when object is not processable
            if (!IsProcessable)
            {
                throw new NullReferenceException("DelayCompletedLocker cannot be null");
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

            // Instantiate and start the thread responsible for delaying the processing the action
            (new Thread(() =>
            {
                // Loop waiting until ResetEvent is allowed to timeout to millisecondWait parameter
                // or maxDelays is reached
                while (true)
                {
                    // Wait on timer reset or timeout
                    if (delayEvent.WaitOne(millisecondWait)
                        && (maxDelays < 0
                            || delayCounter < maxDelays))
                    {
                        // break out of loop if delay already completed (such as on dispose)
                        if (DelayCompleted)
                        {
                            break;
                        }

                        // If timer was reset on another thread and the delay counter is less than the amount allowed,
                        // Prepare timer for another waiting round

                        // Ensure the timer reset thread is waiting for synchronization by continually waiting and checking
                        while (waitingForReset)
                        {
                            // Arbitrary time to wait for reset thread to synchronize, it may never even hit this sleep
                            Thread.Sleep(50);
                        }
                        // Lock on AutoResetEvent to synchronize with reset thread
                        lock (delayEvent)
                        {
                            // Record new timer reset
                            delayCounter++;
                            // Reset timer for waiting again on next loop
                            delayEvent.Reset();
                            // Pulse reset thread to continue
                            Monitor.Pulse(delayEvent);
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
                lock (DelayCompletedLocker)
                {
                    // Store if delay was already completed
                    bool storeDelayAlreadyCompleted = DelayCompleted;
                    // Mark that delay completed for external synchronization
                    DelayCompleted = true;
                    // Run any preprocessing actions queued in the synchronized context
                    if (SynchronizedPreprocessingActions != null)
                    {
                        while (SynchronizedPreprocessingActions.Count > 0)
                        {
                            SynchronizedPreprocessingActions.Dequeue()();
                        }
                    }

                    // Only process the action if the delay had not previously been completed
                    if (!storeDelayAlreadyCompleted)
                    {
                        // Process action
                        toProcess((T)this, userstate);
                    }
                }
                // Lock on AutoResetEvent to synchronize with reset thread
                lock (delayEvent)
                {
                    // Pulse remaining reset threads so they all continue
                    Monitor.PulseAll(delayEvent);
                }
            })).Start();// starts the new thread after it was defined
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
        /// <summary>
        /// Adds an action to run before the primary action which will synchronize on the DelayCompletedLocker
        /// </summary>
        /// <param name="toEnqueue">Action to run on preprocessing</param>
        /// <returns>Returns true if action is accepted for preprocessing, otherwise false means action will not be run</returns>
        public bool EnqueuePreprocessingAction(Action toEnqueue)
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
                // create the queue if necessary
                if (SynchronizedPreprocessingActions == null)
                {
                    SynchronizedPreprocessingActions = new Queue<Action>();
                }
                // enqueue the action to run before normal processing
                SynchronizedPreprocessingActions.Enqueue(toEnqueue);
            }
            return true;
        }
        #endregion

        #region IDisposable members
        public void Dispose()
        {
            // lock on this to prevent disposal during resets
            lock (this)
            {
                // set delay completed so processing will not fire
                DelayCompleted = true;
                // trigger processing thread to break out
                delayEvent.Set();
            }
        }
        #endregion
    }
}