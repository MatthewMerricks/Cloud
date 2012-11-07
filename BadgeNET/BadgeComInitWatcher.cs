//
// BadgeComInitWatcher.cs
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
    public sealed class BadgeComInitWatcher : IDisposable
    {
        public event EventHandler<EventArgs> BadgeComInitialized;
        public event EventHandler<EventArgs> BadgeComWatcherFailed;

        private PubSubServerClass _pubSubServer = null;
        private static CLTrace _trace = CLTrace.Instance;
        private const int _kMillisecondsTimeoutSubscribingThread = 1000;
        private const int _kMillisecondsTimeoutWatchingThread = 5000;
        private Guid _guidSubscription;
        private Thread _threadSubscribing = null;
        private Thread _threadWatching = null;
        private bool _isSubscriberThreadAlive = false;
        private readonly object _locker = new object();

        /// <summary>
        /// Start a thread which will subscribe to BadgeCom_Initialization events.  
        /// </summary>
        public void StartWatchingBadgeCom()
        {
            // Hook up with the shared memory PubSubServer
            if (_pubSubServer == null)
            {
                _pubSubServer = new PubSubServerClass();
                _pubSubServer.Initialize();
            }

            // Start the threads.
            StartSubscribingThread();
            StartWatchingThread();
        }

        /// <summary>
        /// Subscribe and pull events from any of the BadgeCom instance threads.  
        /// </summary>
        private void SubscribingThreadProc()
        {
            try
            {
                // Generate a GUID to represent this subscription
                _guidSubscription = Guid.NewGuid();

                // Loop waiting for events.
                while (true)
                {
                    // Create or open this subcription.  Since the GUID is unique, this will create the subscription on the first call.
                    EnumPubSubServerSubscribeReturnCodes result = _pubSubServer.Subscribe(EnumEventType.BadgeCom_Initialization, _guidSubscription, _kMillisecondsTimeoutSubscribingThread);
                    if (result == EnumPubSubServerSubscribeReturnCodes.RC_SUBSCRIBE_GOT_EVENT)
                    {
                        EventHandler<EventArgs> handler = BadgeComInitialized;
                        if (handler != null)
                        {
                            handler(this, EventArgs.Empty);
                        }
                    }
                    else if (result == EnumPubSubServerSubscribeReturnCodes.RC_SUBSCRIBE_ERROR)
                    {
                        _trace.writeToLog(1, "BadgeComInitWatcher: SubscribingThreadProc: ERROR: From PubSubServer.Subscribe. Exception.");
                        break;
                    }
                    else if (result == EnumPubSubServerSubscribeReturnCodes.RC_SUBSCRIBE_ALREADY_CANCELLED)
                    {
                        _trace.writeToLog(1, "BadgeComInitWatcher: SubscribingThreadProc: ERROR: From PubSubServer.Subscribe.  Already cancelled.");
                        break;
                    }

                    // We're alive
                    lock (_locker)
                    {
                        _isSubscriberThreadAlive = true;
                    }


                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "BadgeComInitWatcher: SubscribingThreadProc: ERROR: Exception: Msg: <{0}>.", ex.Message);
            }
        }

        /// <summary>
        /// The subscribing thread may get stuck waiting on an event if the BadgeCom process is killed.  Monitor the subscribing thread for activity.
        /// If no activity is detected, kill the subscribing thread and attempt to restart it.
        /// </summary>
        private void WatchingThreadProc()
        {
            try
            {
                bool fRestartSubscribingThread;

                while (true)
                {
                    // Wait letting the subscribing thread work.
                    fRestartSubscribingThread = false;
                    Thread.Sleep(_kMillisecondsTimeoutWatchingThread);

                    // Did it do any work?
                    lock (_locker)
                    {
                        if (!_isSubscriberThreadAlive)
                        {
                            fRestartSubscribingThread = true;
                        }

                        _isSubscriberThreadAlive = false;               // reset it for the next look
                    }

                    // Restart the subscribing thread if we need to
                    if (fRestartSubscribingThread)
                    {
                        RestartSubcribingThread();
                    }
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "BadgeComInitWatcher: WatchingThreadProc: ERROR: Exception: Msg: <{0}>.", ex.Message);
                EventHandler<EventArgs> handler = BadgeComWatcherFailed;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }

                KillSubscribingThread();
            }
        }

        /// <summary>
        /// Kill the subscribing thread.
        /// </summary>
        private void KillSubscribingThread()
        {
            try
            {
                lock (_locker)
                {
                    if (_pubSubServer != null)
                    {
                        // Ask the subscribing thread to exit gracefully.
                        EnumPubSubServerCancelWaitingSubscriptionReturnCodes result = _pubSubServer.CancelWaitingSubscription(EnumEventType.BadgeCom_Initialization, _guidSubscription);
                        if (result != EnumPubSubServerCancelWaitingSubscriptionReturnCodes.RC_CANCEL_CANCELLED)
                        {
                            _trace.writeToLog(1, "BadgeComInitWatcher: KillSubscribingThread: ERROR: Cancelling. Result: {0}.", result.ToString());
                        }
                    }
                }

                // Wait for the thread to be gone.  If it takes too long, kill it.
                bool fThreadSubscribingInstantiated = false;
                lock (_locker)
                {
                    if (_threadSubscribing != null)
                    {
                        fThreadSubscribingInstantiated = true;
                    }
                }

                // Try to kill the thread if it is instantiated.
                if (fThreadSubscribingInstantiated)
                {
                    bool fThreadDead = false;
                    for (int i = 0; i < 3; i++)
                    {
                        if (_threadSubscribing.IsAlive)
                        {
                            Thread.Sleep(50);
                        }
                        else
                        {
                            fThreadDead = true;
                            break;
                        }
                    }

                    if (!fThreadDead)
                    {
                        _threadSubscribing.Abort();
                    }
                }

                _threadSubscribing = null;
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "BadgeComInitWatcher: KillSubscribingThread: ERROR: Exception: Msg: <{0}>.", ex.Message);
            }
        }

        /// <summary>
        /// Kill the watching thread.
        /// </summary>
        private void KillWatchingThread()
        {
            try
            {
                lock (_locker)
                {
                    if (_threadWatching != null)
                    {
                        _threadWatching.Abort();
                        _threadWatching = null;
                    }
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "BadgeComInitWatcher: KillWatchingThread: ERROR: Exception: Msg: <{0}>.", ex.Message);
            }
        }

        private void StartSubscribingThread()
        {
            try
            {
                lock (_locker)
                {
                    // Start a thread to subscribe and process BadgeCom initialization events.  Upon receiving one of these events,
                    // we will send the entire badging database for this process.
                    _threadSubscribing = new Thread(new ThreadStart(SubscribingThreadProc));
                    _threadSubscribing.Start();
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "BadgeComInitWatcher: StartSubscribingThread: ERROR: Exception: Msg: <{0}>.", ex.Message);
            }
        }

        private void StartWatchingThread()
        {
            try
            {
                // Start a thread to watch the thread that is watching BadgeCom.  This is necessary because BadgeCom may crash with
                // the threadWatcher thread waiting for an event to arrive.  That might result in a wait forever.  This thread
                // will kill the threadWatcher if it waits too long.  If it kills the thread, it will attempt to restart it.
                _threadWatching = new Thread(new ThreadStart(WatchingThreadProc));
                _threadWatching.Start();
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "BadgeComInitWatcher: StartWatchingThread: ERROR: Exception: Msg: <{0}>.", ex.Message);
            }
        }

        // Attempt to restart the subscribing thread.
        private void RestartSubcribingThread()
        {
            KillSubscribingThread();
            StartSubscribingThread();
        }

        #region IDisposable members

        // Standard IDisposable implementation based on MSDN System.IDisposable
        ~BadgeComInitWatcher()
        {
            Dispose(false);
        }

        // Standard IDisposable implementation based on MSDN System.IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        
        // Standard IDisposable implementation based on MSDN System.IDisposable
        private void Dispose(bool disposing)
        {
            lock (this)
            {
                // Run dispose on inner managed objects based on disposing condition
                if (disposing)
                {
                    // Kill both threads
                    try
                    {
                        KillSubscribingThread();
                        KillWatchingThread();
                    }
                    catch (Exception ex)
                    {
                        _trace.writeToLog(1, "BadgeComInitWatcher: Dispose: ERROR: Exception. Killing threads.: Msg: <{0}>.", ex.Message);
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
                    _trace.writeToLog(1, "BadgeComInitWatcher: Dispose: ERROR: Exception. Freeing PubSubServer resources. Msg: <{0}>.", ex.Message);
                }
            }
        }

        #endregion
    }
}
