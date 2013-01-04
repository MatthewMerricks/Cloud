//
//  CLNetworkMonitorService.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudApiPublic.Support;
using CloudApiPublic.Sync;
using CloudApiPublic.Model;
using CloudApiPrivate.Model.Settings;

namespace win_client.Services.Badging
{
    public sealed class CLNetworkMonitorService
    {
        private static CLNetworkMonitorService _instance = null;
        private static object _instanceLocker = new object();
        private static CLTrace _trace = CLTrace.Instance;

        // True if we have a network connection to MDS
        private bool _cloudReach;
        public bool CloudReach
        {
            get { return _cloudReach; }
            private set { _cloudReach = value; }
        }

        /// <summary>
        /// Access Instance to get the singleton object.
        /// Then call methods on that instance.
        /// </summary>
        public static CLNetworkMonitorService Instance
        {
            get
            {
                lock (_instanceLocker)
                {
                    if (_instance == null)
                    {
                        _instance = new CLNetworkMonitorService();

                        // Initialize at first Instance access here
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// This is a private constructor, meaning no outsiders have access.
        /// </summary>
        private CLNetworkMonitorService()
        {
            // Initialize members, etc. here (at static initialization time).

            //TODO: Make CloudReach dynamic with network connection to MDS.
            _cloudReach = true;
        }

        /// <summary>
        /// Start the network monitoring service.
        /// </summary>
        public void BeginNetworkMonitoring()
        {

        }

        /// <summary>
        /// Stop the network monitoring service.
        /// </summary>
        public void EndNetworkMonitoring()
        {
            try
            {
                //// HttpScheduler was made internal, this call is now available statically in CLSync
                //HttpScheduler.DisposeBothSchedulers();

                CloudApiPublic.CLSync.ShutdownSchedulers();
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(Settings.Instance.TraceLocation, Settings.Instance.LogErrors);
                _trace.writeToLog(1, "CLNetworkMonitorService: EndNetworkMonitoring: ERROR: Exception. Msg: <{0}>.", ex.Message);
            }
        }
    }
}
