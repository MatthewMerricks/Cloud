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
        public MonitorAgent MonitorAgent { get; private set; }
        public IndexingAgent IndexingAgent { get; private set; }

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
            // Initialize members, etc. here (at static initialization time).

            // Start the indexer at the first reference of the FSM.
            IndexingAgent indexerToSet;
            CLError indexCreationError = IndexingAgent.CreateNewAndInitialize(out indexerToSet);
            if (indexerToSet == null)
            {
                _trace.writeToLog(1, "CLFSMonitoringService: ERROR: Creating the indexer.");
            }
            IndexingAgent = indexerToSet;
        }

        /// <summary>
        /// Start the file system monitoring service.
        /// </summary>
        public void BeginFileSystemMonitoring()
        {

            // Todo: handle index creation error

            MonitorAgent monitorToSet;
            CLError fileMonitorCreationError = MonitorAgent.CreateNewAndInitialize(CLSettingsSync.Instance,
                IndexingAgent,
                out monitorToSet,
                global::CloudApiPublic.Sync.SyncEngine.Run);

            if (fileMonitorCreationError != null)
            {
                _trace.writeToLog(1, "CLFSMonitoringService: BeginFileSystemMonitoring: ERROR: Creating the MonitorAgent.  Msg: <{0}>. Code: {1}.", fileMonitorCreationError.errorDescription, fileMonitorCreationError.errorCode);
            }
            else
            {
                if (monitorToSet != null)
                {
                    try
                    {
                        this.MonitorAgent = monitorToSet;

                        CLAppMessages.Message_DidReceivePushNotificationFromServer.Register(monitorToSet,
                            (Action<CloudApiPublic.JsonContracts.NotificationResponse>)monitorToSet.PushNotification);

                        MonitorStatus returnStatus;
                        CLError fileMonitorStartError = MonitorAgent.Start(out returnStatus);
                        if (fileMonitorStartError != null)
                        {
                            _trace.writeToLog(1, "CLFSMonitoringService: BeginFileSystemMonitoring: ERROR: Starting the MonitorAgent.  Msg: <{0}>. Code: {1}.", fileMonitorStartError.errorDescription, fileMonitorStartError.errorCode);
                        }

                        CLError indexerStartError = IndexingAgent.StartInitialIndexing(MonitorAgent.BeginProcessing,
                            MonitorAgent.GetCurrentPath);
                        if (indexerStartError != null)
                        {
                            _trace.writeToLog(1, "CLFSMonitoringService: BeginFileSystemMonitoring: ERROR: Starting the indexer.  Msg: <{0}>. Code: {1}.", indexerStartError.errorDescription, indexerStartError.errorCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        _trace.writeToLog(1, "CLFSMonitoringService: BeginFileSystemMonitoring: ERROR: Exception.  Msg: <{0}>.", ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Stop the file system monitoring service.
        /// </summary>
        public void EndFileSystemMonitoring()
        {
            try
            {
                if (MonitorAgent != null)
                {
                    MonitorAgent.Dispose();
                    MonitorAgent = null;
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "CLFSMonitoringService: EndFileSystemMonitoring: ERROR: Exception.  Msg: <{0}>.", ex.Message);
            }
        }
    }
}
