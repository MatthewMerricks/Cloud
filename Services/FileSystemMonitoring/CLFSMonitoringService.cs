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
using CloudApiPublic.Support;
using CloudApiPublic.Model;
using CloudApiPrivate.Model.Settings;
using CloudApiPublic.FileMonitor;
using CloudApiPublic.Sync;
using CloudApiPublic.SQLIndexer;
using CloudApiPublic.Static;
using win_client.Common;


namespace win_client.Services.FileSystemMonitoring
{
    public sealed class CLFSMonitoringService
    {
        static readonly CLFSMonitoringService _instance = new CLFSMonitoringService();
        private static Boolean _isLoaded = false;
        private static CLTrace _trace = CLTrace.Instance;
        internal CloudApiPublic.CLSync SyncBox { get; private set; }

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
                SyncBox = new CloudApiPublic.CLSync();
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(Settings.Instance.TraceLocation, Settings.Instance.LogErrors);
                _trace.writeToLog(1, "CLFSMonitoringService: ERROR: Creating the SyncBox. Msg: <{0}>", ex.Message);
            }
        }

        /// <summary>
        /// Start the file system monitoring service.
        /// </summary>
        public void BeginFileSystemMonitoring()
        {
            try
            {
                SyncBox.Start(CLSettingsSync.Instance);
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(Settings.Instance.TraceLocation, Settings.Instance.LogErrors);
                _trace.writeToLog(1, "CLFSMonitoringService: BeginFileSystemMonitoring: ERROR: Starting the SyncBox. Msg: <{0}>", ex.Message);
            }
        }

        /// <summary>
        /// Stop the file system monitoring service.
        /// </summary>
        public void EndFileSystemMonitoring()
        {
            try
            {
                SyncBox.Stop();
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(Settings.Instance.TraceLocation, Settings.Instance.LogErrors);
                _trace.writeToLog(1, "CLFSMonitoringService: EndFileSystemMonitoring. ERROR: Stop SyncBox. Msg: <{0}>", ex.Message);
            }
        }
    }
}
