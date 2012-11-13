//
// BadgeComPubSubEvents.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using BadgeCOMLib;
using CloudApiPublic.Support;

namespace BadgeNET
{
    /// <summary>
    /// Class to watch for initialization events from BadgeCom (Explorer shell extension).
    /// </summary>
    public sealed class BadgeComPubSubEvents : IDisposable
    {
        #region Public events

        public event EventHandler<EventArgs> BadgeComInitialized;
        public event EventHandler<EventArgs> BadgeComInitializedSubscriptionFailed;

        #endregion

        #region Private fields

        private PubSubServerClass _pubSubServer = null;
        private static CLTrace _trace = CLTrace.Instance;
        private const int _kMillisecondsTimeoutSubscribingThread = 1000;
        private const int _kMillisecondsTimeoutWatchingThread = 20000;
        private Guid _guidSubscription;
        private Thread _threadSubscribing = null;
        private Thread _threadWatching = null;
        private bool _isSubscriberThreadAlive = false;
        private readonly object _locker = new object();
        private static bool _fDebugging = false;
        private Semaphore _semSync = null;
        private Semaphore _semWatcher = null;
        private bool _fRequestSubscribingThreadExit = false;
        private bool _fRequestWatchingThreadExit = false;
        
        #endregion

        #region Public methods


        /// <summary>
        /// Initialize and load the BadgeCom PubSubServer.
        /// </summary>
        public void Initialize()
        {
            try
            {
                // Hook up with the shared memory PubSubServer
                _trace.writeToLog(9, "BadgeComPubSubEvents: Initialize: Entry.");
                if (_pubSubServer == null)
                {
                    _pubSubServer = new PubSubServerClass();
                    _pubSubServer.Initialize();
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "BadgeComPubSubEvents: Initialize: ERROR: Exception: Msg: <{0}>.", ex.Message);
            }
        }

        /// <summary>
        /// Publish an event to BadgeCom
        /// </summary>
        public void PublishEventToBadgeCom(EnumEventSubType eventSubType, EnumCloudAppIconBadgeType badgeType, string fullPath)
        {
            try
            {
                _trace.writeToLog(9, "BadgeComPubSubEvents: PublishEventToBadgeCom: Entry. eventSubType: {0}. badgeType: {1}. fullPath: {2}.", eventSubType, badgeType, fullPath);
                if (_pubSubServer == null)
                {
                    throw new Exception("Call Initialize() first");
                }

                // Publish the event
                EnumPubSubServerPublishReturnCodes result = _pubSubServer.Publish(EnumEventType.BadgeNet_To_BadgeCom, eventSubType, badgeType, fullPath);
                if (result != EnumPubSubServerPublishReturnCodes.RC_PUBLISH_OK)
                {
                    _trace.writeToLog(1, "BadgeComPubSubEvents: PublishEventToBadgeCom: ERROR: From Publish. Result: {0}.", result.ToString());
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "BadgeComPubSubEvents: PublishEventToBadgeCom: ERROR: Exception: Msg: <{0}>.", ex.Message);
            }
        }

        /// <summary>
        /// Start a thread which will subscribe to BadgeCom_Initialization events.  
        /// </summary>
        public void SubscribeToBadgeComInitializationEvents()
        {
            try
            {
                // Start the subscribing thread.
                _trace.writeToLog(9, "BadgeComPubSubEvents: SubscribeToBadgeComInitializationEvents: Entry.");
                bool startedOk = StartSubscribingThread();

                if (startedOk)
                {
                    StartWatchingThread();
                }
                else
                {
                    _trace.writeToLog(1, "BadgeComPubSubEvents: SubscribeToBadgeComInitializationEvents: ERROR: Subscribing thread did not start.");
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "BadgeComPubSubEvents: SubscribeToBadgeComInitializationEvents: ERROR: Exception: Msg: <{0}>.", ex.Message);
            }
        }

        #endregion

        #region Support functions
        
        /// <summary>
        /// Subscribe and pull events from any of the BadgeCom instance threads.  
        /// </summary>
        private void SubscribingThreadProc()
        {
            //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@ DEBUG REMOVE @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
            int testCount = 30;
            //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@ DEBUG REMOVE @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
            try
            {
                _trace.writeToLog(9, "BadgeComPubSubEvents: SubscribingThreadProc: Entry.");
                bool fSemaphoreReleased = false;

                // Generate a GUID to represent this subscription
                _guidSubscription = Guid.NewGuid();

                // Loop waiting for events.
                while (true)
                {
                    // Exit if we should
                    if (_fRequestSubscribingThreadExit)
                    {
                        _trace.writeToLog(9, "BadgeComPubSubEvents: SubscribingThreadProc: Requested to exit.  Break out of loop.");
                        break;
                    }
                    // Create or open this subcription.  Since the GUID is unique, this will create the subscription on the first call.
                    EnumEventSubType outEventSubType;
                    EnumCloudAppIconBadgeType outBadgeType;
                    string outFullPath;
                    EnumPubSubServerSubscribeReturnCodes result = _pubSubServer.Subscribe(EnumEventType.BadgeCom_To_BadgeNet, _guidSubscription, _kMillisecondsTimeoutSubscribingThread,
                                 out outEventSubType, out outBadgeType, out outFullPath);
                    //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@ DEBUG REMOVE @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
                    testCount--;
                    //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@ DEBUG REMOVE @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
                    if (result == EnumPubSubServerSubscribeReturnCodes.RC_SUBSCRIBE_GOT_EVENT)
                    {
                        _trace.writeToLog(9, "BadgeComPubSubEvents: SubscribingThreadProc: Got an event.");
                        EventHandler<EventArgs> handler = BadgeComInitialized;
                        if (handler != null)
                        {
                            handler(this, EventArgs.Empty);
                        }
                    }
                    else if (result == EnumPubSubServerSubscribeReturnCodes.RC_SUBSCRIBE_ERROR)
                    {
                        _trace.writeToLog(1, "BadgeComPubSubEvents: SubscribingThreadProc: ERROR: From PubSubServer.Subscribe. Exception.");
                        break;
                    }
                    else if (result == EnumPubSubServerSubscribeReturnCodes.RC_SUBSCRIBE_ALREADY_CANCELLED)
                    {
                        _trace.writeToLog(1, "BadgeComPubSubEvents: SubscribingThreadProc: ERROR: From PubSubServer.Subscribe.  Already cancelled.");
                        break;
                    }

                    // Keep alive
                    lock (_locker)
                    {
                        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@ DEBUG REMOVE @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
                        if (testCount >= 0)
                        {
                            _isSubscriberThreadAlive = true;
                        }
                        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@ DEBUG REMOVE @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
                        //_isSubscriberThreadAlive = true;          //@@@@@@@@@@@@ DEBUG add this back.

                        // Let the starting thread know we have subscribed, but just once.
                        if (!fSemaphoreReleased)
                        {
                            _trace.writeToLog(1, "BadgeComPubSubEvents: SubscribingThreadProc: Subscribed. Post starting thread.");
                            fSemaphoreReleased = true;
                            _semSync.Release();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "BadgeComPubSubEvents: SubscribingThreadProc: ERROR: Exception: Msg: <{0}>.", ex.Message);
            }

            _trace.writeToLog(1, "BadgeComPubSubEvents: SubscribingThreadProc: Subscriber thread exit.");
        }

        /// <summary>
        /// The subscribing thread may get stuck waiting on an event if the BadgeCom process is killed.  Monitor the subscribing thread for activity.
        /// If no activity is detected, kill the subscribing thread and attempt to restart it.
        /// </summary>
        private void WatchingThreadProc()
        {
            try
            {
                _trace.writeToLog(9, "BadgeComPubSubEvents: WatchingThreadProc: Entry.");
                bool fRestartSubscribingThread;

                while (true)
                {
                    // Wait letting the subscribing thread work.
                    fRestartSubscribingThread = false;
                    _trace.writeToLog(9, "BadgeComPubSubEvents: WatchingThreadProc: Wait for next look.");
                    _semWatcher.WaitOne(_kMillisecondsTimeoutWatchingThread);
                    _trace.writeToLog(9, "BadgeComPubSubEvents: WatchingThreadProc: Out of wait.  Check the subscribing thread.");

                    // Exit if we should
                    if (_fRequestWatchingThreadExit)
                    {
                        _trace.writeToLog(9, "BadgeComPubSubEvents: WatchingThreadProc: Requested to exit.  Break out of loop.");
                        break;
                    }

                    // Did the subscribing thread do any work?
                    lock (_locker)
                    {
                        if (!_isSubscriberThreadAlive)
                        {
                            if (!_fDebugging)
                            {
                                fRestartSubscribingThread = true;
                            }
                        }

                        _isSubscriberThreadAlive = false;               // reset it for the next look
                    }

                    // Restart the subscribing thread if we need to
                    if (fRestartSubscribingThread)
                    {
                        _trace.writeToLog(9, "BadgeComPubSubEvents: WatchingThreadProc: Restart subscribing thread.");
                        RestartSubcribingThread();
                    }
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "BadgeComPubSubEvents: WatchingThreadProc: ERROR: Exception: Msg: <{0}>.", ex.Message);
                if (!_fRequestWatchingThreadExit && !_fRequestWatchingThreadExit)
                {
                    EventHandler<EventArgs> handler = BadgeComInitializedSubscriptionFailed;
                    if (handler != null)
                    {
                        _trace.writeToLog(9, "BadgeComPubSubEvents: WatchingThreadProc: Fire event BadgeComInitializedSubscriptionFailed.");
                        handler(this, EventArgs.Empty);
                    }

                    _trace.writeToLog(9, "BadgeComPubSubEvents: WatchingThreadProc: Kill the subscribing thread.");
                    KillSubscribingThread();
                }
            }

            _trace.writeToLog(1, "BadgeComPubSubEvents: WatchingThreadProc: Watching thread exit.");
        }

        /// <summary>
        /// Kill the subscribing thread.
        /// </summary>
        private void KillSubscribingThread()
        {
            try
            {
                _trace.writeToLog(9, "BadgeComPubSubEvents: KillSubscribingThread: Entry.");
                bool fThreadSubscribingInstantiated = false;
                lock (_locker)
                {
                    if (_pubSubServer != null)
                    {
                        // Cancel the subscription the thread may be waiting on.
                        _trace.writeToLog(9, "BadgeComPubSubEvents: KillSubscribingThread: Cancel our subscription.");
                        EnumPubSubServerCancelWaitingSubscriptionReturnCodes result = _pubSubServer.CancelWaitingSubscription(EnumEventType.BadgeCom_To_BadgeNet, _guidSubscription);
                        if (result != EnumPubSubServerCancelWaitingSubscriptionReturnCodes.RC_CANCEL_OK)
                        {
                            _trace.writeToLog(1, "BadgeComPubSubEvents: KillSubscribingThread: ERROR: Cancelling. Result: {0}.", result.ToString());
                        }
                    }

                    if (_threadSubscribing != null)
                    {
                        fThreadSubscribingInstantiated = true;
                    }
                }

                // Try to kill the thread if it is instantiated.
                if (fThreadSubscribingInstantiated)
                {
                    bool fThreadDead = false;

                    // Wait for the thread to be gone.  If it takes too long, kill it.
                    _trace.writeToLog(9, "BadgeComPubSubEvents: KillSubscribingThread: Request subscribing thread exit.");
                    _fRequestSubscribingThreadExit = true;              // request the thread to exit
                    for (int i = 0; i < 3; i++)
                    {
                        if (_threadSubscribing.IsAlive)
                        {
                            _trace.writeToLog(9, "BadgeComPubSubEvents: KillSubscribingThread: Wait for the subscribing thread to exit.");
                            Thread.Sleep(50);
                        }
                        else
                        {
                            _trace.writeToLog(9, "BadgeComPubSubEvents: KillSubscribingThread: Subscribing thread is dead.");
                            fThreadDead = true;
                            break;
                        }
                    }

                    if (!fThreadDead)
                    {
                        _trace.writeToLog(9, "BadgeComPubSubEvents: KillSubscribingThread: Abort subscribing thread.");
                        _threadSubscribing.Abort();
                    }
                }

                _threadSubscribing = null;
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "BadgeComPubSubEvents: KillSubscribingThread: ERROR: Exception: Msg: <{0}>.", ex.Message);
            }
        }

        /// <summary>
        /// Kill the watching thread.
        /// </summary>
        private void KillWatchingThread()
        {
            try
            {
                _trace.writeToLog(9, "BadgeComPubSubEvents: KillWatchingThread: Entry.");
                // Request the thread to exit and wait for the thread to be gone.  If it takes too long, kill it.
                bool fThreadWatchingInstantiated = false;
                lock (_locker)
                {
                    if (_threadWatching != null)
                    {
                        _trace.writeToLog(9, "BadgeComPubSubEvents: KillWatchingThread: Request the thread to exit and post its wait.");
                        _fRequestWatchingThreadExit = true;             // request the thread to exit
                        _semWatcher.Release();                          // knock it out of its wait

                        fThreadWatchingInstantiated = true;
                    }
                }

                // Wait for the watching thread to exit, and kill it if it takes too long.
                if (fThreadWatchingInstantiated)
                {
                    _trace.writeToLog(9, "BadgeComPubSubEvents: KillWatchingThread: Wait for watching thread to exit.");
                    bool fThreadDead = false;
                    for (int i = 0; i < 3; i++)
                    {
                        if (_threadWatching.IsAlive)
                        {
                            _trace.writeToLog(9, "BadgeComPubSubEvents: KillWatchingThread: Let the watching thread work.");
                            Thread.Sleep(50);
                        }
                        else
                        {
                            _trace.writeToLog(9, "BadgeComPubSubEvents: KillWatchingThread: Watching thread is dead.");
                            fThreadDead = true;
                            break;
                        }
                    }

                    if (!fThreadDead)
                    {
                        _trace.writeToLog(9, "BadgeComPubSubEvents: KillWatchingThread: Abort watching thread.");
                        _threadWatching.Abort();
                    }
                }

                _threadWatching = null;

            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "BadgeComPubSubEvents: KillWatchingThread: ERROR: Exception: Msg: <{0}>.", ex.Message);
            }
        }

        /// <summary>
        /// Start the subscribing thread.
        /// </summary>
        /// <returns>bool: true: Subscribed OK.</returns>
        private bool StartSubscribingThread()
        {
            _trace.writeToLog(9, "BadgeComPubSubEvents: StartSubscribingThread: Entry.");
            bool result = false;

            try
            {
                // The subscription should complete before the watching thread starts
                _semSync = new Semaphore(0, 1);
                _semWatcher = new Semaphore(0, 1);

                lock (_locker)
                {
                    // Start a thread to subscribe and process BadgeCom initialization events.  Upon receiving one of these events,
                    // we will send the entire badging database for this process.
                    _threadSubscribing = new Thread(new ThreadStart(SubscribingThreadProc));
                    _threadSubscribing.Start();
                }

                // Wait for the subscribing thread to start properly
                result = _semSync.WaitOne(5000);

                if (!result)
                {
                    _trace.writeToLog(1, "BadgeComPubSubEvents: SubscribeToBadgeComInitializationEvents: ERROR: Subscribing thread did not start.");
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "BadgeComPubSubEvents: StartSubscribingThread: ERROR: Exception: Msg: <{0}>.", ex.Message);
            }

            _trace.writeToLog(1, "BadgeComPubSubEvents: SubscribeToBadgeComInitializationEvents: Exit with code: {0}.", result);
            return result;
        }

        /// <summary>
        /// Start the watching thread.
        /// </summary>
        private void StartWatchingThread()
        {
            try
            {
                _trace.writeToLog(9, "BadgeComPubSubEvents: StartWatchingThread: Entry.");
                lock (_locker)
                {
                    // Start a thread to watch the thread that is watching BadgeCom.  This is necessary because BadgeCom may crash with
                    // the threadWatcher thread waiting for an event to arrive.  That might result in a wait forever.  This thread
                    // will kill the threadWatcher if it waits too long.  If it kills the thread, it will attempt to restart it.
                    _threadWatching = new Thread(new ThreadStart(WatchingThreadProc));
                    _threadWatching.Start();
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "BadgeComPubSubEvents: StartWatchingThread: ERROR: Exception: Msg: <{0}>.", ex.Message);
            }
        }

        /// <summary>
        /// Attempt to restart the subscribing thread.
        /// </summary>
        private void RestartSubcribingThread()
        {
            _trace.writeToLog(9, "BadgeComPubSubEvents: RestartSubcribingThread: Entry.");
            KillSubscribingThread();
            bool startedOk = StartSubscribingThread();
            if (!startedOk)
            {
                _trace.writeToLog(1, "BadgeComPubSubEvents: RestartSubcribingThread: ERROR: Subscribing thread did not start.");
            }
        }

        #endregion

        #region IDisposable members

        /// <summary>
        /// Standard IDisposable implementation based on MSDN System.IDisposable.
        /// </summary>
        ~BadgeComPubSubEvents()
        {
            Dispose(false);
        }

        /// <summary>
        /// Standard IDisposable implementation based on MSDN System.IDisposable.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Standard IDisposable implementation based on MSDN System.IDisposable.
        /// </summary>
        private void Dispose(bool disposing)
        {
            _trace.writeToLog(9, "BadgeComPubSubEvents: Dispose: Entry.");
            lock (this)
            {
                // Run dispose on inner managed objects based on disposing condition
                if (disposing)
                {
                    // Kill both threads
                    try
                    {
                        _fRequestSubscribingThreadExit = true;              // preemptive strike
                        _fRequestWatchingThreadExit = true;
                        KillSubscribingThread();
                        KillWatchingThread();
                    }
                    catch (Exception ex)
                    {
                        _trace.writeToLog(1, "BadgeComPubSubEvents: Dispose: ERROR: Exception. Killing threads.: Msg: <{0}>.", ex.Message);
                    }
                }

                // Dispose any local unmanaged resources.
                // Free the PubSub resources
                try
                {
                    if (_pubSubServer != null)
                    {
                        _pubSubServer.Terminate();
                        _pubSubServer = null;
                    }

                }
                catch (Exception ex)
                {
                    _trace.writeToLog(1, "BadgeComPubSubEvents: Dispose: ERROR: Exception. Freeing PubSubServer resources. Msg: <{0}>.", ex.Message);
                }
            }
        }

        #endregion
    }
}
