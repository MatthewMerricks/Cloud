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

namespace BadgeNET
{
    /// <summary>
    /// Class to watch for initialization events from BadgeCom (Explorer shell extension).
    /// </summary>
    public sealed class BadgeComInitWatcher
    {
        public event EventHandler<EventArgs> RaiseBadgeComInitialized;

        private PubSubServerClass _pubSubServer = null;


        /// <summary>
        /// Start a thread which will subscribe to BadgeCom_Initialization events.  
        /// </summary>
        public void StartWatchingBadgeCom()
        {
            // Start a thread to watch BadgeCom.
            Thread threadWatcher = new Thread(new ThreadStart(WatchBadgeComThreadProc));
            threadWatcher.Start();

            // Start a thread to watch the thread that is watching BadgeCom.  This is necessary because BadgeCom may crash with
            // the threadWatcher thread waiting for an event to arrive.  That might result in a wait forever.  This thread
            // will kill the threadWatcher if it waits too long.  If it kills the thread, it will attempt to restart it.
            Thread threadWatcherWatcher = new Thread(new ThreadStart(WatchWatcherThreadThreadProc));
            threadWatcherWatcher.Start();
        }

        private void WatchBadgeComThreadProc()
        {
            try
            {
                _pubSubServer = new PubSubServerClass();

                // Loop waiting for events.
                while (true)
                {
                    int result = _pubSubServer.Subscribe(EnumEventType.BadgeCom_Initialization, 100);
                    if (result == Enum

                }


            }
            catch (Exception ex)
            {
            }
        }

        private void WatchWatcherThreadThreadProc()
        {
            try
            {

            }
            catch (Exception ex)
            {
            }
        }
    }
}
