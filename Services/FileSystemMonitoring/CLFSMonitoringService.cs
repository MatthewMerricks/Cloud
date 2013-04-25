//
//  CLFSMonitoringService.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cloud.Support;
using Cloud.Model;
using CloudApiPrivate.Model.Settings;
using Cloud.FileMonitor;
using Cloud.Sync;
using Cloud.SQLIndexer;
using Cloud.Static;
using win_client.Common;


namespace win_client.Services.FileSystemMonitoring
{
    public sealed class CLFSMonitoringService
    {
        static readonly CLFSMonitoringService _instance = new CLFSMonitoringService();
        private static Boolean _isLoaded = false;
        private static CLTrace _trace = CLTrace.Instance;
        internal Cloud.CLSync Syncbox { get; private set; }

        /// <summary>
        /// Access Instance to get the singleton object.
        /// Then call methods on that instance.
        /// </summary>
        public static CLFSMonitoringService Instance
        {
            get
            {
                if (!_isLoaded)
                {
                    _isLoaded = true;

                    // Initialize at first Instance access here
                }
                return _instance;
            }
        }

        /// <summary>
        /// This is a private constructor, meaning no outsiders have access.
        /// </summary>
        private CLFSMonitoringService()
        {
            try
            {
                Syncbox = new Cloud.CLSync();
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.Log(Settings.Instance.TraceLocation, Settings.Instance.LogErrors);
                _trace.writeToLog(1, "CLFSMonitoringService: ERROR: Creating the Syncbox. Msg: <{0}>", ex.Message);
            }
        }

        /// <summary>
        /// Start the file system monitoring service.
        /// </summary>
        public void BeginFileSystemMonitoring()
        {
            try
            {
                Cloud.CLSyncStartStatus startStatus;
                Syncbox.Start(CLSettingsSync.Instance, out startStatus);
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.Log(Settings.Instance.TraceLocation, Settings.Instance.LogErrors);
                _trace.writeToLog(1, "CLFSMonitoringService: BeginFileSystemMonitoring: ERROR: Starting the Syncbox. Msg: <{0}>", ex.Message);
            }
        }

        /// <summary>
        /// Stop the file system monitoring service.
        /// </summary>
        public void EndFileSystemMonitoring()
        {
            try
            {
                Syncbox.Stop();
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.Log(Settings.Instance.TraceLocation, Settings.Instance.LogErrors);
                _trace.writeToLog(1, "CLFSMonitoringService: EndFileSystemMonitoring. ERROR: Stop Syncbox. Msg: <{0}>", ex.Message);
            }
        }
    }
}