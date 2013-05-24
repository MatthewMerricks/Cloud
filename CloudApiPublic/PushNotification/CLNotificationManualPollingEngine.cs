﻿//  CLNotificationManualPollingEngine.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using Cloud.Interfaces;
using Cloud.Model;
using Cloud.Static;
using Cloud.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace Cloud.PushNotification
{
    internal sealed class CLNotificationManualPollingEngine : ICLNotificationEngine
    {
        #region Private fields

        private static CLTrace _trace = CLTrace.Instance;
        private CLSyncbox _syncbox = null;
        private ICLSyncSettingsAdvanced _copiedSettings = null;
        private readonly object _locker = new object();
        private bool _isInitialized = false;
        private bool _isStarted = false;
        private StartEngineTimeout _delegateStartEngineTimeout = null;
        private CancelEngineTimeout _delegateCancelEngineTimeout = null;
        private SendManualPoll _delegateSendManualPoll = null;
        private int _manualPollIntervalSeconds = 0;     // the actual number of seconds to use.  This is random between the max and min.

        #endregion

        #region Public properties

        public int MaxSuccesses
        {
            get
            {
                return _maxSuccesses;
            }
        }
        private int _maxSuccesses = 10;

        public int MaxFailures
        {
            get
            {
                return _maxFailures;
            }
        }
        private int _maxFailures = 1;

        #endregion

        #region Constructors

        public CLNotificationManualPollingEngine(
                        CLSyncbox syncbox, 
                        SendManualPoll delegateSendManualPoll)
        {
            if (syncbox == null)
            {
                throw new ArgumentNullException(Resources.SyncboxMustNotBeNull);
            }
            if (delegateSendManualPoll == null)
            {
                throw new ArgumentNullException(Resources.CLNotificationManualPollingEngineDelegateSendManualPollMustNotBeNull);
            }

            lock (_locker)
            {
                if (_isInitialized)
                {
                    throw new InvalidOperationException(Resources.CLNotificationManualPollingEngineAlreadyInitialized);
                }

                _syncbox = syncbox;
                _copiedSettings = syncbox.CopiedSettings;
                _delegateSendManualPoll = delegateSendManualPoll;
                _isInitialized = true;

                // Determine the manual polling interval to use for this instance.
                Random rnd = new Random();
                _manualPollIntervalSeconds = rnd.Next(CLDefinitions.MinManualPollingPeriodSeconds, CLDefinitions.MaxManualPollingPeriodSeconds);

            }
        }

        public CLNotificationManualPollingEngine()
        {
            throw new NotSupportedException(Resources.CLNotificationManualPollingEngineDefaultConstructorNotSupported);
        }

        #endregion

        #region Public methods

        public bool Start()
        {
            bool fToReturnSuccess = true;

            try
            {
                lock (_locker)
                {
                    if (!_isInitialized)
                    {
                        throw new InvalidOperationException(Resources.CLNotificationManualPollingEngineInitializeFirst);
                    }

                    if (_isStarted)
                    {
                        throw new InvalidOperationException(Resources.CLSyncEngineAlreadyStarted);
                    }
                    _isStarted = true;
                }

                Thread.Sleep(_manualPollIntervalSeconds * 1000);

                _delegateSendManualPoll();  // notify

                lock (_locker)
                {
                    _isStarted = false;
                    return true;
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, Resources.CLNotificationManualPollingEngineStartErrorExceptionMsg0, ex.Message);
                fToReturnSuccess = false;
            }

            return fToReturnSuccess;
        }

        public void Stop()
        {
            // Nothing to do here
        }

        public void TimerExpired(object userState)
        {
            // Nothing to do here.
        }

        #endregion
    }
}
