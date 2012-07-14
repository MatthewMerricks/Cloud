//
//  CLSyncService.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using win_client.Common;
using win_client.Services.Notification;
using win_client.Services.FileSystemMonitoring;
using win_client.Services.Indexing;
using win_client.Services.UiActivity;
using win_client.Services.FileSystemDispatcher;
using CloudApiPublic;
using CloudApiPrivate;
using CloudApiPublic.Support;
using CloudApiPublic.Model;
using CloudApiPrivate.Model.Settings;
using CloudApiPrivate.Static;
using CloudApiPrivate.Common;
using BadgeNET;
using FileMonitor;
using Newtonsoft.Json.Linq;
using CloudApiPublic.Static;
using System.Runtime.CompilerServices;

namespace win_client.Services.Sync
{
    public sealed class CLSyncService
    {
        #region "Static fields and properties"
        private static CLSyncService _instance = null;
        private static object _instanceLocker = new object();
        private static CLTrace _trace = null;
        private static CLPrivateRestClient _restClient = null;
        private static CLSptNSOperationQueue _uploadOperationQueue = null;       // this is the upload queue used by the REST client.
        private static CLSptNSOperationQueue _downloadOperationQueue = null;     // this is the download queue used by the REST client.
        private static List<string> _recentItems = null;
        private static int _syncItemsQueueCount = 0;
        private static List<CLEvent> _activeDownloadQueue = null;
        private static List<string> _currentSids = null;
        private static bool _waitingForCloudResponse = false;
        private static bool _needSyncFromCloud = false;
        private static List<CLEvent> _activeSyncQueue = null;
        private static List<CLEvent> _activeSyncFileQueue = null;
        private static List<CLEvent> _activeSyncFolderQueue = null;


        static DispatchQueueGeneric _com_cloud_sync_queue = null;
        static DispatchQueueGeneric get_cloud_sync_queue () {
            if (_com_cloud_sync_queue == null) {
                _com_cloud_sync_queue =  Dispatch.Queue_Create();
            }

            return _com_cloud_sync_queue;
        }

        private static bool _serviceStarted = false;
        public bool ServiceStarted
        {
            get
            {
                return _serviceStarted;
            }
            set
            {
                _serviceStarted = value;
            }
        }

        private static bool _wasOffline = false;
        public bool WasOffline
        {
            get
            {
                return _wasOffline;
            }
            set
            {
                _wasOffline = value;
            }
        }
        #endregion
        
        /// <summary>
        /// Access Instance to get the singleton object.
        /// Then call methods on that instance.
        /// </summary>
        public static CLSyncService Instance
        {
            get
            {
                lock (_instanceLocker)
                {
                    if (_instance == null)
                    {
                        _instance = new CLSyncService();

                        // Initialize at first Instance access here
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// This is a private constructor, meaning no outsiders have access.
        /// </summary>
        private CLSyncService()
        {
            // Initialize members, etc. here (at static initialization time).
            _trace = CLTrace.Instance;

            // [_sharedService prepareForSyncServiceToStart];
            PrepareForSyncServiceToStart();
        }

        //- (void)prepareForSyncServiceToStart
        private void PrepareForSyncServiceToStart()
        {
            // self.recentItems = [NSMutableArray array];
            // self.restClient = [[CLPrivateRestClient alloc] init];
            // self.activeDownloadQueue = [NSMutableArray array];
            // self.activeSyncQueue = [NSMutableArray array];
            // self.currentSIDs = [NSMutableArray array];
    
            // // Download and upload opperation queue's
            // if (self.downloadOperationQueue == nil) {
            //     self.downloadOperationQueue = [[CLOperationQueue alloc] init];
            //     self.downloadOperationQueue.maxConcurrentOperationCount = 6;
            // }
    
            // if (self.uploadOperationQueue == nil) {
            //     self.uploadOperationQueue = [[CLOperationQueue alloc] init];
            //     self.uploadOperationQueue.maxConcurrentOperationCount = 6;
            // }
            //&&&&

            // self.recentItems = [NSMutableArray array];
            // self.restClient = [[CLPrivateRestClient alloc] init];
            // self.activeDownloadQueue = [NSMutableArray array];
            // self.activeSyncQueue = [NSMutableArray array];
            // self.currentSIDs = [NSMutableArray array];
            _recentItems = new List<string>();
            _restClient = new CLPrivateRestClient();
            _activeDownloadQueue = new List<CLEvent>();
            _activeSyncQueue = new List<CLEvent>();
            _activeSyncFileQueue = null;
            _activeSyncFolderQueue = null;
            _currentSids = new List<string>();

            // // Download and upload opperation queue's
            // if (self.downloadOperationQueue == nil) {
            //     self.downloadOperationQueue = [[CLOperationQueue alloc] init];
            //     self.downloadOperationQueue.maxConcurrentOperationCount = 6;
            // }
            if (_downloadOperationQueue == null)
            {
                _downloadOperationQueue = new CLSptNSOperationQueue(6);
            }

            // if (self.uploadOperationQueue == nil) {
            //     self.uploadOperationQueue = [[CLOperationQueue alloc] init];
            //     self.uploadOperationQueue.maxConcurrentOperationCount = 6;
            // }
            if (_uploadOperationQueue == null)
            {
                _uploadOperationQueue = new CLSptNSOperationQueue(6);
            }

        }

        /// <summary>
        /// Start the syncing service.
        /// </summary>
        public void BeginSyncServices()
        {
            // Merged 7/13/12
            //self.waitingForCloudResponse = NO;
            //self.serviceStarted = YES;
    
            //// Start receiving callbacks
            //[[CLNotificationServices sharedService] setDelegate:self];
            //[[CLFSMonitoringService sharedService] setDelegate:self];
    
            //if ([self.downloadOperationQueue isSuspended]) {
            //    [self.downloadOperationQueue setSuspended:NO];
            //}
    
            //if([self.uploadOperationQueue isSuspended]) {
            //    [self.uploadOperationQueue setSuspended:NO];
            //}
    
            //NSLog(@"%s Cloud Sync has Started for Cloud Folder at Path: %@", __FUNCTION__, [[CLSettings sharedSettings] cloudFolderPath]);
    
            //if (self.wasOffline == YES) {
            //    // slight delay here to get the network back up. 
            //    [[CLFSMonitoringService sharedService] performSelector:@selector(checkWithFSMForEvents) withObject:nil afterDelay:5];
            //    self.wasOffline = NO;
            //}
            //&&&&

            //self.waitingForCloudResponse = NO;
            //self.serviceStarted = YES;
            _waitingForCloudResponse = false;
            _serviceStarted = true;

            //// Start receiving callbacks
            //TODO: Necessary?
            //[[CLNotificationServices sharedService] setDelegate:self];
            //[[CLFSMonitoringService sharedService] setDelegate:self];

            //if ([self.downloadOperationQueue isSuspended]) {
            //    [self.downloadOperationQueue setSuspended:NO];
            //}
            if (_downloadOperationQueue.IsSuspended)
            {
                _downloadOperationQueue.IsSuspended = false;
            }

            //if([self.uploadOperationQueue isSuspended]) {
            //    [self.uploadOperationQueue setSuspended:NO];
            //}
            if (_uploadOperationQueue.IsSuspended)
            {
                _uploadOperationQueue.IsSuspended = false;
            }

            //NSLog(@"%s Cloud Sync has Started for Cloud Folder at Path: %@", __FUNCTION__, [[CLSettings sharedSettings] cloudFolderPath]);
            _trace.writeToLog(1, "BeginSyncServices: Cloud Sync has Started for Cloud Folder at Path: {0}.", Settings.Instance.CloudFolderPath);

            //if (self.wasOffline == YES) {
            //    // slight delay here to get the network back up. 
            //    [[CLFSMonitoringService sharedService] performSelector:@selector(checkWithFSMForEvents) withObject:nil afterDelay:5];
            //    self.wasOffline = NO;
            //}
            if (_wasOffline)
            {
                CLFSMonitoringService.Instance.Agent.FireSimulatedPushNotification();
                _wasOffline = false;
            }
        }

        /// <summary>
        /// Stop the syncing service.
        /// </summary>
        public void StopSyncServices()
        {
            _serviceStarted = false;

            // Stop receiving callbacks
            // Note: Not necessary since this is a singleton.  We'll just drive the methods directly.
            //[[CLNotificationServices sharedService] setDelegate:nil];
            //[[CLFSMonitoringService sharedService] setDelegate:nil];

           if (_syncItemsQueueCount > 0) {
                //TODO: Clear the number of synced files to zero in the UI system tray status.
                // AnimateUIForSyncWithStatusMessageSyncActivityCount(false, (int) menuItemActivityLabelType.menuItemActivityLabelSynced, 0);

                _activeSyncQueue.Clear();
                _activeSyncFileQueue.Clear();
                _activeSyncFolderQueue.Clear();
                _activeDownloadQueue.Clear();
                _currentSids.Clear();
                _downloadOperationQueue.CancelAllOperations();
                _uploadOperationQueue.CancelAllOperations();
            }

            _trace.writeToLog(1, "BeginSyncServices: Cloud Sync has Eneded for Cloud Folder at Path: {0}.", Settings.Instance.CloudFolderPath);
        }

        // - (NSDictionary *)sortSyncEventsByType:(NSArray *)events
        Dictionary<string, object> SortSyncEventsByType(List<CLEvent> events)
        {
            // Merged 7/4/12
            //NSLog(@"%s", __FUNCTION__);
            //NSMutableArray * addEvents = [NSMutableArray array];
            //NSMutableArray * modifyEvents = [NSMutableArray array];
            //NSMutableArray * moveRenameEvents = [NSMutableArray array];
            //NSMutableArray * deleteEvents = [NSMutableArray array];
    
            //[events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
        
            //    CLEvent *event = obj;
            //    NSString *eventAction = event.action;
            //    if (event.isMDSEvent) {
            //        eventAction = event.syncHeader.action;
            //    }
        
            //    if ([eventAction rangeOfString:CLEventTypeAddRange].location != NSNotFound) {
            //        [addEvents addObject:event];
            //    }
            //    if ([eventAction rangeOfString:CLEventTypeModifyFile].location != NSNotFound) {
            //        [modifyEvents addObject:event];
            //    }
            //    if ([eventAction rangeOfString:CLEventTypeRenameRange].location != NSNotFound) {
            //        [moveRenameEvents addObject:event];
            //    }
            //    if ([eventAction rangeOfString:CLEventTypeMoveRange].location != NSNotFound ) {
            //        [moveRenameEvents addObject:event];
            //    }
            //    if ([eventAction rangeOfString:CLEventTypeDeleteRange].location != NSNotFound) {
            //        [deleteEvents addObject:event];
            //    }
            //}];
    
            //return [NSDictionary dictionaryWithObjectsAndKeys:addEvents, CLEventTypeAdd, modifyEvents ,CLEventTypeModify, moveRenameEvents,  CLEventTypeRenameMove, deleteEvents, CLEventTypeDelete, nil];

            _trace.writeToLog(9, "CLSyncService: SortSyncEventsByType: Entry.");

            List<CLEvent> addEvents = new List<CLEvent>();
            List<CLEvent> modifyEvents = new List<CLEvent>();
            List<CLEvent> moveRenameEvents = new List<CLEvent>();
            List<CLEvent> deleteEvents = new List<CLEvent>();

            events.ForEach(obj =>
            {
                CLEvent thisEvent = obj;
                string eventAction = thisEvent.Action;
                if (thisEvent.IsMDSEvent)
                {
                    eventAction = thisEvent.SyncHeader.Action;
                }

                if (eventAction.Contains(CLDefinitions.CLEventTypeAddRange))
                {
                    addEvents.Add(thisEvent);
                }

                if (eventAction.Contains(CLDefinitions.CLEventTypeModifyFile))
                {
                    modifyEvents.Add(thisEvent);
                }

                if (eventAction.Contains(CLDefinitions.CLEventTypeRenameRange))
                {
                    moveRenameEvents.Add(thisEvent);
                }

                if (eventAction.Contains(CLDefinitions.CLEventTypeMoveRange))
                {
                    moveRenameEvents.Add(thisEvent);
                }

                if (eventAction.Contains(CLDefinitions.CLEventTypeDeleteRange))
                {
                    deleteEvents.Add(thisEvent);
                }
            });

            Dictionary<string, object> rc = new Dictionary<string, object>()
            {
                {CLDefinitions.CLEventTypeAdd, addEvents},
                {CLDefinitions.CLEventTypeModify, modifyEvents},
                {CLDefinitions.CLEventTypeRenameMove, moveRenameEvents},
                {CLDefinitions.CLEventTypeDelete, deleteEvents}
            };
            return rc;
        }





        /// <summary>
        /// Used for EventComparer to do a deep comparison of a file system event dictionary
        /// containing "event" and "metadata" keys.  Note:  This is not yet a CLEvent.
        /// Index is the index of this item used to maintain sort order.
        /// </summary>
        private class EventHolder
        {
            public int Index { get; set; }
            public Dictionary<string, object> Event { get; set; }
        }

        /// <summary>
        /// Perform a deep comparison of two system event dictionary "events"
        /// containing "event" and "metadata" keys.  Note:  This is not yet a CLEvent.
        /// </summary>
        private class EventComparer : IEqualityComparer<EventHolder>
        {
            public static EventComparer Instance
            {
                get
                {
                    lock (InstanceLocker)
                    {
                        if (_instance == null)
                        {
                            _instance = new EventComparer();
                        }
                        return _instance;
                    }
                }
            }
            private static EventComparer _instance = null;
            private static object InstanceLocker = new object();

            private EventComparer() { }

            #region IEqualityComparer<EventHolder> members
            public bool Equals(EventHolder x, EventHolder y)
            {
                Dictionary<string, object> xDict = x.Event as Dictionary<string, object>;
                Dictionary<string, object> yDict = y.Event as Dictionary<string, object>;
                if (xDict == null && yDict == null)
                    return true;
                if (xDict == null || yDict == null)
                    return false;
                if (!xDict.ContainsKey("event")
                    || !xDict.ContainsKey("metadata")
                    || !yDict.ContainsKey("event")
                    || !yDict.ContainsKey("metadata"))
                    return false;
                Dictionary<string, object> xMetadata;
                Dictionary<string, object> yMetadata;
                if (!(xDict["event"] is string)
                    || (xMetadata = xDict["metadata"] as Dictionary<string, object>) != null
                    || !(yDict["event"] is string)
                    || (yMetadata = yDict["metadata"] as Dictionary<string, object>) != null)
                    return false;
                if (!((string)xDict["event"]).Equals((string)yDict["event"], StringComparison.InvariantCulture))
                    return false;
                if (xMetadata.ContainsKey("path"))
                {
                    if (!yMetadata.ContainsKey("path"))
                        return false;
                    if (!(xMetadata["path"] is string)
                        || !(yMetadata["path"] is string))
                        return false;
                    return ((string)xMetadata["path"]).Equals((string)yMetadata["path"], StringComparison.InvariantCulture);
                }
                else if (xMetadata.ContainsKey("from_path")
                    && xMetadata.ContainsKey("to_path"))
                {
                    if (!yMetadata.ContainsKey("from_path")
                        || !yMetadata.ContainsKey("to_path"))
                        return false;
                    if (!(xMetadata["from_path"] is string)
                        || !(xMetadata["to_path"] is string)
                        || !(yMetadata["from_path"] is string)
                        || !(yMetadata["to_path"] is string))
                        return false;
                    return ((string)xMetadata["from_path"]).Equals((string)yMetadata["from_path"], StringComparison.InvariantCulture)
                        && ((string)xMetadata["to_path"]).Equals((string)yMetadata["to_path"], StringComparison.InvariantCulture);
                }
                return false;
            }

            public int GetHashCode(EventHolder obj)
            {
                return obj.Event.GetHashCode();
            }
            #endregion
        }


        /// <summary>
        /// This is a callback from the file system monitor.  FSM is presenting us with a list of events.
        /// These are not CLEvents.  The eventsDictionary parameter is in a special format like:
        ///     Dictionary
	    ///         "event_count", 99999  // number of events in the group
	    ///         "event_id", 99999     // last eid in group
        ///         "events", Array
        ///             Dictionary
        ///                 "event", string (event name: One of the CLEventType* strings listed above)
        ///                 "metadata", Dictionary
        ///                     if eventName is RenameFile/Folder or MoveFile/Folder
        ///                         "from_path", oldPath of the item (without the Cloud folder root)
        ///                         "to_path", path of the item (without the Cloud folder root)
        ///                     else eventName is NOT one of RenameFile/Folder or MoveFile/Folder
        ///                         "path", path of the item (without the Cloud folder root)
        ///                     endelse eventName is NOT one of RenameFile/Folder or MoveFile/Folder
        /// <param name="eventsDictionary">Events in the format described above.</param>
        /// </summary>
        //- (void)syncFromFileSystemMonitor: (CLFSMonitoringService *)fsm withGroupedUserEvents:(NSDictionary *)events
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SyncFromFileSystemMonitorWithGroupedUserEventsCallback(Dictionary<string, object> eventsDictionary)
        {
            // Update UI with activity.
            //TODO: Implement this UI.
            // [self animateUIForSync:YES withStatusMessage:menuItemActivityLabelFSM syncActivityCount:0];

            // NSString *sid;
            // if ([self.currentSIDs lastObject] != nil) {
            //     sid = [self.currentSIDs lastObject];
            // }else {
            //    sid = [[CLSettings sharedSettings] sid];
            // }
            string sid;
            if (_currentSids.Count > 0 && _currentSids.Last<string>() != null)
            {
                sid = _currentSids.Last<string>();
            }
            else
            {
                sid = Settings.Instance.Sid;
            }

            int eid = (int)eventsDictionary[CLDefinitions.CLEventKey];

            List<Dictionary<string, object>> castEvents = (List<Dictionary<string, object>>) eventsDictionary["events"];

            if (castEvents.Count > 0)        // we may have eliminated events while filtering them thru our index.
            {
                Dictionary<string, object> fsmEventsDictionary = new Dictionary<string,object>();
                fsmEventsDictionary.Add(CLDefinitions.CLSyncEvents, castEvents);
                fsmEventsDictionary.Add(CLDefinitions.CLSyncID, sid);

                _trace.writeToLog(1, "CLSyncService: SyncFromFileSystemMonitorWithGroupedUserEventsCallback: Requesting sync to cloud.");
                SyncToCloudWithEvents_andEID(fsmEventsDictionary, eid);
            }
            else
            {
                NotificationServiceDidReceivePushNotificationFromServer(false, "test");
                //TODO: Remove this test code&&&&&&&&&&&&&&&&&&&

                // Update UI with activity.
                //TODO: Implement this UI.
                // [self animateUIForSync:NO withStatusMessage:menuItemActivityLabelSynced syncActivityCount:0];
            }
        }

        //- (void)syncToCloudWithEvents:(NSDictionary *)events andEID:(NSNumber *)eid
        void SyncToCloudWithEvents_andEID(Dictionary<string, object> events, int eid)
        {
            //    NSString *sid;
            //    if ([self.currentSIDs lastObject] != nil) {
            //        sid = [self.currentSIDs lastObject];
            //    }else {
            //        sid = [[CLSettings sharedSettings] sid];
            //    }

            //    //NSLog(@"Events From FSM: %@", events);

            //    [self.restClient syncToCloud:events completionHandler:^(NSDictionary *metadata, NSError *error) {

            //        if (error == nil) {

            //            NSLog(@"Response From Sync To Cloud: \n\n%@\n\n", metadata);

            //            if ([[metadata objectForKey:CLSyncEvents] count] > 0) {

            //                // override with sid sent by phil
            //                NSString *newSid = [metadata objectForKey:CLSyncID];

            //                if ([self.currentSIDs containsObject:newSid] == NO) {
            //                    [self.currentSIDs addObject:newSid];
            //                }

            //                // add received events.
            //                NSArray *mdsEvents = [metadata objectForKey:CLSyncEvents];
            //                NSMutableArray *events = [NSMutableArray array];

            //                [mdsEvents enumerateObjectsUsingBlock:^(id mdsEvent, NSUInteger idx, BOOL *stop) {

            //                    // if status is not found, metadata is null.
            //                    if (![[[mdsEvent objectForKey:CLSyncEventHeader] objectForKey:CLSyncEventStatus] isEqualToString:CLEventTypeNotFound]) {
            //                        // check for valid dictionary
            //                        if ([[mdsEvent objectForKey:CLSyncEventMetadata] isKindOfClass:[NSDictionary class]]){
            //                            [events addObject:[CLEvent eventFromMDSEvent:mdsEvent]];
            //                        }
            //                    }
            //                }];

            //                // Dispatch for processing.
            //                NSMutableDictionary *eventIds = [NSMutableDictionary dictionaryWithObjectsAndKeys:eid, CLSyncEventID, newSid, CLSyncID, nil];
            //                [self performSyncOperationWithEvents:events withEventIDs:eventIds andOrigin:CLEventOriginMDS];
            //            }
            //        } else {

            //            NSLog(@"%s - %@", __FUNCTION__, error);
            //            // Update UI with activity.
            //            [self animateUIForSync:NO withStatusMessage:menuItemActivityLabelSynced syncActivityCount:0];

            //            // If we fail to talk to MDS retry every 10 seconds till MDS is backup
            //            dispatch_after(dispatch_time(DISPATCH_TIME_NOW, 10 * NSEC_PER_SEC),dispatch_get_main_queue(),^{
            //                [self syncToCloudWithEvents:events andEID:eid];
            //            });
            //        }
            //    } onQueue:get_cloud_sync_queue()];
            //&&&&

            //    NSString *sid;
            //    if ([self.currentSIDs lastObject] != nil) {
            //        sid = [self.currentSIDs lastObject];
            //    }else {
            //        sid = [[CLSettings sharedSettings] sid];
            //    }
            string sid;
            if (_currentSids.Count > 0 && _currentSids.Last<string>() != null)
            {
                sid = _currentSids.Last<string>();
            }
            else
            {
                sid = Settings.Instance.Sid;
            }

            _restClient.SyncToCloud_WithCompletionHandler_OnQueue_Async(events, (result) =>
            {
                if (result.Error == null)
                {
                    //            NSLog(@"Response From Sync To Cloud: \n\n%@\n\n", metadata);

                    //            if ([[metadata objectForKey:CLSyncEvents] count] > 0) {

                    //                // override with sid sent by phil
                    //                NSString *newSid = [metadata objectForKey:CLSyncID];

                    //                if ([self.currentSIDs containsObject:newSid] == NO) {
                    //                    [self.currentSIDs addObject:newSid];
                    //                }

                    //                // add received events.
                    //                NSArray *mdsEvents = [metadata objectForKey:CLSyncEvents];
                    //                NSMutableArray *events = [NSMutableArray array];

                    //                [mdsEvents enumerateObjectsUsingBlock:^(id mdsEvent, NSUInteger idx, BOOL *stop) {

                    //                    // if status is not found, metadata is null.
                    //                    if (![[[mdsEvent objectForKey:CLSyncEventHeader] objectForKey:CLSyncEventStatus] isEqualToString:CLEventTypeNotFound]) {
                    //                        // check for valid dictionary
                    //                        if ([[mdsEvent objectForKey:CLSyncEventMetadata] isKindOfClass:[NSDictionary class]]){
                    //                            [events addObject:[CLEvent eventFromMDSEvent:mdsEvent]];
                    //                        }
                    //                    }
                    //                }];

                    //                // Dispatch for processing.
                    //                NSMutableDictionary *eventIds = [NSMutableDictionary dictionaryWithObjectsAndKeys:eid, CLSyncEventID, newSid, CLSyncID, nil];
                    //                [self performSyncOperationWithEvents:events withEventIDs:eventIds andOrigin:CLEventOriginMDS];
                    //            }
                    Dictionary<string, object> metadata = result.JsonResult;
                    if (metadata != null
                        && metadata.Count > 0
                        && metadata.ContainsKey(CLDefinitions.CLSyncEvents)
                        && metadata.ContainsKey(CLDefinitions.CLSyncID)
                        && metadata.GetType() == typeof(Dictionary<string, object>))
                    {
                        // Override with sid sent by server
                        // NSString *newSid = [metadata objectForKey:CLSyncID];
                        string newSid = (string)metadata[CLDefinitions.CLSyncID];

                        // if ([self.currentSIDs containsObject:newSid] == NO) {
                        if (!_currentSids.Contains(newSid))
                        {
                            // [self.currentSIDs addObject:newSid];
                            _currentSids.Add(newSid);
                        }

                        // Add received events.
                        // NSArray *mdsEvents = [metadata objectForKey:CLSyncEvents];
                        // NSMutableArray *events = [NSMutableArray array];
                        JArray mdsEvents = (JArray)metadata[CLDefinitions.CLSyncEvents];
                        List<CLEvent> eventsReceived = new List<CLEvent>();

                        // [mdsEvents enumerateObjectsUsingBlock:^(id mdsEvent, NSUInteger idx, BOOL *stop) {
                        foreach (JToken mdsEvent in mdsEvents)
                        {
                            // If status is not found, metadata is null.
                            // if (![[[mdsEvent objectForKey:CLSyncEventHeader] objectForKey:CLSyncEventStatus] isEqualToString:CLEventTypeNotFound]) {
                            //   // check for valid dictionary
                            //   if ([[mdsEvent objectForKey:CLSyncEventMetadata] isKindOfClass:[NSDictionary class]]){
                            //      [events addObject:[CLEvent eventFromMDSEvent:mdsEvent]];
                            //   }
                            // }
                            Dictionary<string, object> mdsEventDictionary = mdsEvent.ToObject<Dictionary<string, object>>();
                            Dictionary<string, object> syncHeaderDictionary = ((JToken)mdsEventDictionary[CLDefinitions.CLSyncEventHeader]).ToObject<Dictionary<string, object>>();
                            if (syncHeaderDictionary.ContainsKey(CLDefinitions.CLSyncEventStatus))
                            {
                                eventsReceived.Add(CLEvent.EventFromMDSEvent(() =>
                                {
                                    lock (CLFSMonitoringService.Instance.IndexingAgent)
                                    {
                                        return CLFSMonitoringService.Instance.IndexingAgent.LastSyncId;
                                    }
                                },
                                CLFSMonitoringService.Instance.MonitorAgent.GetCurrentPath,
                                mdsEventDictionary,
                                SyncDirection.To));
                            }
                        }

                        // Dispatch for processing.
                        // NSMutableDictionary *eventIds = [NSMutableDictionary dictionaryWithObjectsAndKeys:eid, CLSyncEventID, newSid, CLSyncID, nil];
                        Dictionary<string, object> eventIds = new Dictionary<string, object>()
                                {
                                    {CLDefinitions.CLSyncEventID, eid.ToString()},
                                    {CLDefinitions.CLSyncID, newSid}
                                };

                        // [self performSyncOperationWithEvents:events withEventIDs:eventIds andOrigin:CLEventOriginMDS];
                        PerformSyncOperationWithEvents_withEventIDs_andOrigin(eventsReceived, eventIds, CLEventOrigin.CLEventOriginMDS);
                    }
                }
                else
                {
                    _trace.writeToLog(1, "CLSyncService: SyncFromFileSystemMonitorWithGroupedUserEvents: ERROR {0}.", result.Error.errorDescription);
                }
            }, get_cloud_sync_queue());
        }



        //- (CLEvent *)updateEventMetadata:(CLEvent *)event
        CLEvent UpdateEventMetadata(CLEvent evt)
        {
            // Merged 7/2/12
            // NSDictionary *metadata = nil;
            // NSString *cloudPath = event.metadata.path;
    
            // if ([event.action isEqualToString:CLEventTypeMoveFile] || [event.action isEqualToString:CLEventTypeRenameFile]) {
            //     cloudPath = event.metadata.fromPath;
            // }
    
            // if ([event.action rangeOfString:CLEventTypeFolderRange].location != NSNotFound) {
            //     event.metadata.isDirectory = YES;
            // }
    
            // // for new files we get the current file metadata from the file system.
            // if ([event.action isEqualToString:CLEventTypeAddFile] ||
            //     [event.action isEqualToString:CLEventTypeModifyFile] ||
            //     [event.action isEqualToString:CLEventTypeAddFolder]) {
        
            //     // Check if this file item is a symblink, if this object is a in fact a link, the utility method will return a valid target path.
            //     NSString *fileSystemPath =[[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:cloudPath];
            //     if ([fileSystemPath isAliasFinderInfoFlag] == YES) {
            //         // get the target path
            //         NSString *targetPath = [fileSystemPath stringByIterativelyResolvingSymlinkOrAlias];
            //         if (targetPath != nil) {
            //             // symblink events are always recognized as files, therefore simply replace the occurence of the word file with link for the event action.
            //             event.action = [event.action stringByReplacingCharactersInRange:[event.action rangeOfString:CLEventTypeFileRange] withString:@"link"];
                
            //             if ([targetPath rangeOfString:[[CLSettings sharedSettings] cloudFolderPath]].location == NSNotFound) {
            //                 targetPath = [NSString stringWithFormat:@"/%@", targetPath];
            //             }
                
            //             event.metadata.targetPath = targetPath;
                
            //             // this link is useless, if path target matches the link path, then we don't have a valid link resolutions. (see NSString+CloudUtilities.m)
            //             if ([targetPath isEqualToString:fileSystemPath]) {
            //                 event = nil; //[eventsToBeRemoved addObject:events];
            //             }
            //         }
            //     }
            //     else { // all regular file events
            //         int do_try = 0;
            
            //         do {
            //             @autoreleasepool {
            //                 NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:event.metadata.path];
            //                 if ([[NSFileManager defaultManager] fileExistsAtPath:fileSystemPath] == YES) { // only attempt to get attributes for files that do exist
            //                     metadata = [NSDictionary attributesForItemAtPath:fileSystemPath];
            //                 }
            //                 event.metadata.createDate = [metadata objectForKey:CLMetadataFileCreateDate];
            //                 event.metadata.modifiedDate = [metadata objectForKey:CLMetadataFileModifiedDate];
            //                 if ([event.action rangeOfString:CLEventTypeFileRange].location != NSNotFound) { // these are only relevant for files
            //                     event.metadata.revision = [metadata objectForKey:CLMetadataFileRevision];
            //                     event.metadata.hash = event.metadata.hash; //[metadata objectForKey:CLMetadataFileHash];
            //                     event.metadata.size = [metadata objectForKey:CLMetadataFileSize];
            //                     event.metadata.isDirectory = NO;
            //                     event.metadata.customAttributes = nil;
            //                     // TODO: Fix extended attributes
            ////                     if ([event.metadata.createDate rangeOfString:@"190"].location == NSNotFound) {
            ////                         event.metadata.customAttributes = [CLExtendedAttributes archiveAndEncodeExtendedAttributesAtPath:fileSystemPath];
            ////                     }
            //                 }
            //             }
                
            //             do_try ++;
            //         }
            //         while ( [event.metadata.createDate rangeOfString:@"190"].location != NSNotFound && do_try < 3000); // 1 second. TODO: This hack sucks!
            //     }
            // }
    
            // // all other file events we get stored index data for this event item.
            // if ([event.action rangeOfString:CLEventTypeFileRange].location != NSNotFound) {
        
            //     CLMetadata *indexedMetadata = [CLIndexingServices indexedMetadataForEvent:event];
        
            //     if (indexedMetadata != nil) { // we have an object indexed for this event.
            
            //         // for add events, if the darn thing already exists in the index, it means that FSM failed to pick up the event as a modify
            //         // let's make sure of that and if it turns out to be true, then we need to change the event to become a modify type.
            //         if ([event.action isEqualToString:CLEventTypeAddFile]) {
            //             if ([event.metadata.hash isEqualToString:indexedMetadata.hash] == NO &&
            //                 [event.metadata.revision isEqualToString:indexedMetadata.revision] == NO) {
            //                 event.metadata.revision = indexedMetadata.revision;
            //                 event.action = CLEventTypeModifyFile;
            //             }
            //         }
            //         else if ([event.action isEqualToString:CLEventTypeModifyFile]) { // for modify we only want to revision
            //             event.metadata.revision = indexedMetadata.revision;
            //         }
            //         else { // we want it all for all other cases.
                
            //             event.metadata.revision = indexedMetadata.revision;
            //             event.metadata.hash = indexedMetadata.hash;
            //             event.metadata.createDate = indexedMetadata.createDate;
            //             event.metadata.modifiedDate = indexedMetadata.modifiedDate;
            //             event.metadata.size = indexedMetadata.size;
            //         }
            
            //         if (indexedMetadata.targetPath != nil) { // we have a link object, convert
            //             // symblink events are always recognized as files, therefore simply replace the occurence of the word file with link for the event action.
            //             event.action = [event.action stringByReplacingCharactersInRange:[event.action rangeOfString:CLEventTypeFileRange] withString:@"link"];
            //             event.metadata.targetPath = indexedMetadata.targetPath;
            //         }
            //     }
            // }
    
            // return event;
            //&&&&

            // NSDictionary *metadata = nil;
            // NSString *cloudPath = event.metadata.path;
            Dictionary<string, object> metadata = null;
            string cloudPath = evt.Metadata.Path;

            // if ([event.action isEqualToString:CLEventTypeMoveFile] || [event.action isEqualToString:CLEventTypeRenameFile]) {
            if (evt.Action.Equals(CLDefinitions.CLEventTypeMoveFile) || evt.Action.Equals(CLDefinitions.CLEventTypeRenameFile))
            {
                // cloudPath = event.metadata.fromPath;
                cloudPath = evt.Metadata.FromPath;
            }

            // if ([event.action rangeOfString:CLEventTypeFolderRange].location != NSNotFound) {
            if (evt.Action.Contains(CLDefinitions.CLEventTypeFolderRange))
            {
                // event.metadata.isDirectory = YES;
                /* Replaced with the lines below since TargetPath is readonly: */ //evt.Metadata.IsDirectory = true;
                if (evt.ChangeReference != null)
                {
                    if (evt.ChangeReference.Metadata == null)
                    {
                        evt.ChangeReference.Metadata = new FileMetadata();
                    }
                    evt.ChangeReference.Metadata.HashableProperties = new FileMetadataHashableProperties(true,// this first input parameter is the only one changed
                        evt.ChangeReference.Metadata.HashableProperties.LastTime,// copied
                        evt.ChangeReference.Metadata.HashableProperties.CreationTime,// copied
                        evt.ChangeReference.Metadata.HashableProperties.Size);// copied
                }
            }

            // for new files we get the current file metadata from the file system.
            // if ([event.action isEqualToString:CLEventTypeAddFile] ||
            //     [event.action isEqualToString:CLEventTypeModifyFile] ||
            //     [event.action isEqualToString:CLEventTypeAddFolder]) {
            if (evt.Action.Equals(CLDefinitions.CLEventTypeAddFile, StringComparison.InvariantCulture) || 
                evt.Action.Equals(CLDefinitions.CLEventTypeModifyFile, StringComparison.InvariantCulture) || 
                evt.Action.Equals(CLDefinitions.CLEventTypeAddFolder, StringComparison.InvariantCulture))
            {
                // Check if this file item is a symblink, if this object is a in fact a link, the utility method will return a valid target path.
                // NSString *fileSystemPath =[[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:cloudPath];
                string fileSystemPath = Settings.Instance.CloudFolderPath + cloudPath;

                // if ([fileSystemPath isAliasFinderInfoFlag] == YES) {
                if (CLFileShortcuts.FileIsShortcut(fileSystemPath))
                {
                    // Get the target path
                    // NSString *targetPath = [fileSystemPath stringByIterativelyResolvingSymlinkOrAlias];
                    string targetPath = CLFileShortcuts.GetShortcutTargetFile(fileSystemPath);

                    // if (targetPath != nil) {
                    if (!string.IsNullOrWhiteSpace(targetPath))
                    {
                        // Symblink events are always recognized as files, therefore simply replace the occurence of the word file with link for the event action.
                        // event.action = [event.action stringByReplacingCharactersInRange:[event.action rangeOfString:CLEventTypeFileRange] withString:@"link"];
                        evt.Action = evt.Action.Replace(CLDefinitions.CLEventTypeFileRange, CLDefinitions.CLEventTypeLinkRange);

                        // if ([targetPath rangeOfString:[[CLSettings sharedSettings] cloudFolderPath]].location == NSNotFound) {
                        if (targetPath.Contains(Settings.Instance.CloudFolderPath))
                        {
                            // targetPath = [NSString stringWithFormat:@"/%@", targetPath];
                            targetPath = String.Format("/{0}", targetPath);
                        }

                        // event.metadata.targetPath = targetPath;
                        /* Replaced with the lines below since TargetPath is readonly: */ //evt.Metadata.TargetPath = targetPath;
                        if (evt.ChangeReference != null)
                        {
                            evt.ChangeReference.LinkTargetPath = targetPath;
                        }

                        // This link is useless, if path target matches the link path, then we don't have a valid link resolutions. (see NSString+CloudUtilities.m)
                        // if ([targetPath isEqualToString:fileSystemPath]) {
                        if (targetPath.Equals(Settings.Instance.CloudFolderPath, StringComparison.InvariantCulture))
                        {
                            // event = nil; //[eventsToBeRemoved addObject:events];
                            evt = null;
                        }
                    }
                }
                else
                {
                    // int do_try = 0;
                    // do {
                    //     @autoreleasepool {
                    //         NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:event.metadata.path];
                    //         if ([[NSFileManager defaultManager] fileExistsAtPath:fileSystemPath] == YES) { // only attempt to get attributes for files that do exist
                    //             metadata = [NSDictionary attributesForItemAtPath:fileSystemPath];
                    //         }
                    //         event.metadata.createDate = [metadata objectForKey:CLMetadataFileCreateDate];
                    //         event.metadata.modifiedDate = [metadata objectForKey:CLMetadataFileModifiedDate];
                    //         if ([event.action rangeOfString:CLEventTypeFileRange].location != NSNotFound) { // these are only relevant for files
                    //             event.metadata.revision = [metadata objectForKey:CLMetadataFileRevision];
                    //             event.metadata.hash = event.metadata.hash; //[metadata objectForKey:CLMetadataFileHash];
                    //             event.metadata.size = [metadata objectForKey:CLMetadataFileSize];
                    //             event.metadata.isDirectory = NO;
                    //             event.metadata.customAttributes = nil;
                    //             // TODO: Fix extended attributes
                    ////             if ([event.metadata.createDate rangeOfString:@"190"].location == NSNotFound) {
                    ////                 event.metadata.customAttributes = [CLExtendedAttributes archiveAndEncodeExtendedAttributesAtPath:fileSystemPath];
                    ////             }
                    //         }
                    //     }
                    //     do_try ++;
                    // }
                    // while ( [event.metadata.createDate rangeOfString:@"190"].location != NSNotFound && do_try < 3000); // 1 second. TODO: This hack sucks!

                    string fileSystemPath2 = Settings.Instance.CloudFolderPath + evt.Metadata.Path;
                    CLMetadata fileMetadata =
                        new CLMetadata(() =>
                            {
                                lock (CLFSMonitoringService.Instance.IndexingAgent)
                                {
                                    return CLFSMonitoringService.Instance.IndexingAgent.LastSyncId;
                                }
                            },
                            CLFSMonitoringService.Instance.MonitorAgent.GetCurrentPath);
                    if (evt.ChangeReference != null)
                    {
                        fileMetadata.ChangeReference = evt.ChangeReference;

                        metadata = CLMetadata.DictionaryFromMetadataItem(fileMetadata);
                        //CLMetadata.CLMetadataProcessedInternals processedInternals = new CLMetadata.CLMetadataProcessedInternals(CLFSMonitoringService.Instance.MonitorAgent.GetCurrentPath,
                        //    (string) metadata[CLDefinitions.CLMetadataFileCreateDate],
                        //    (string) metadata[CLDefinitions.CLMetadataFileModifiedDate],
                        string metadataCreationDate = (string)metadata[CLDefinitions.CLMetadataFileCreateDate];
                        string metadataModifiedDate = (string)metadata[CLDefinitions.CLMetadataFileModifiedDate];
                        string metadataRevision;
                        string metadataHash;
                        string metadataSize;
                        bool metadataIsDirectory;
                        if (evt.Action.Contains(CLDefinitions.CLEventTypeFileRange))         // these are only relevant for files
                        {
                            metadataRevision = (string)metadata[CLDefinitions.CLMetadataFileRevision];
                            metadataHash = (string)metadata[CLDefinitions.CLMetadataFileHash];         // The original code did not do this
                            metadataSize = (string)metadata[CLDefinitions.CLMetadataFileSize];
                            metadataIsDirectory = false;
                            //TODO: No custom file attributes in Windows?
                        }
                        else
                        {
                            metadataRevision = evt.ChangeReference.Revision;
                            evt.ChangeReference.GetMD5LowercaseString(out metadataHash);
                            metadataSize = (evt.ChangeReference.Metadata == null
                                || evt.ChangeReference.Metadata.HashableProperties.Size == null
                                    ? null
                                    : evt.ChangeReference.Metadata.HashableProperties.Size.ToString());
                            if (evt.ChangeReference.Metadata == null)
                            {
                                metadataIsDirectory = false;
                            }
                            else
                            {
                                metadataIsDirectory = evt.ChangeReference.Metadata.HashableProperties.IsFolder;
                            }
                        }

                        CLMetadata.CLMetadataProcessedInternals processedInternals = new CLMetadata.CLMetadataProcessedInternals(null,// paths are not being updated
                            metadataCreationDate,
                            metadataModifiedDate,
                            metadataSize,
                            null,// event id is not being updated
                            null,// paths are not being updated
                            null,// paths are not being updated
                            null);// paths are not being updated

                        fileMetadata.ChangeReference.Revision = metadataRevision;
                        if (fileMetadata.ChangeReference.Metadata == null)
                        {
                            fileMetadata.ChangeReference.Metadata = new FileMetadata();
                        }
                        fileMetadata.ChangeReference.Metadata.HashableProperties = new FileMetadataHashableProperties(metadataIsDirectory,
                            processedInternals.ModifiedDate,
                            processedInternals.CreationDate,
                            processedInternals.Size);

                    }
                }
            }

            // All other file events we get stored index data for this event item.
            // if ([event.action rangeOfString:CLEventTypeFileRange].location != NSNotFound) {
            if (evt.Action.Contains(CLDefinitions.CLEventTypeFileRange))
            {
                // CLMetadata *indexedMetadata = [CLIndexingServices indexedMetadataForEvent:event];
                CLMetadata indexedMetadata = CLIndexingService.Instance.IndexedMetadataForEvent(evt);

                // if (indexedMetadata != nil) { // we have an object indexed for this event.
                if (indexedMetadata != null)
                {
                    // // for add events, if the darn thing already exists in the index, it means that FSM failed to pick up the event as a modify
                    // // let's make sure of that and if it turns out to be true, then we need to change the event to become a modify type.
                    // if ([event.action isEqualToString:CLEventTypeAddFile]) {
                    if (evt.Action.Contains(CLDefinitions.CLEventTypeAddFile))
                    {
                        // if ([event.metadata.hash isEqualToString:indexedMetadata.hash] == NO &&
                        //     [event.metadata.revision isEqualToString:indexedMetadata.revision] == NO) {
                        if (!evt.Metadata.Hash.Equals(indexedMetadata.Hash, StringComparison.InvariantCulture))
                        {
                            // event.metadata.revision = indexedMetadata.revision;
                            // event.action = CLEventTypeModifyFile;
                            /* Replaced with the following lines since Revision is read-only: */ //evt.Metadata.Revision = indexedMetadata.Revision;
                            if (evt.ChangeReference != null)
                            {
                                evt.ChangeReference.Revision = indexedMetadata.Revision;
                            }
                            evt.Action = CLDefinitions.CLEventTypeModifyFile;
                        }
                    }
                    // else if ([event.action isEqualToString:CLEventTypeModifyFile]) { // for modify we only want to revision
                    else if (evt.Action.Equals(CLDefinitions.CLEventTypeModifyFile, StringComparison.InvariantCulture))
                    {
                        // event.metadata.revision = indexedMetadata.revision;
                        /* Replaced with the following lines since Revision is read-only: */ //evt.Metadata.Revision = indexedMetadata.Revision;
                        if (evt.ChangeReference != null)
                        {
                            evt.ChangeReference.Revision = indexedMetadata.Revision;
                        }
                    }
                    else  // we want it all for all other cases.
                    {
                        // event.metadata.revision = indexedMetadata.revision;
                        // event.metadata.hash = indexedMetadata.hash;
                        // event.metadata.createDate = indexedMetadata.createDate;
                        // event.metadata.modifiedDate = indexedMetadata.modifiedDate;
                        // event.metadata.size = indexedMetadata.size;
                        /* Replaced with the following lines since the next five properties are read-only */
                        /*
                         * evt.Metadata.Revision = indexedMetadata.Revision;
                         * evt.Metadata.Hash = indexedMetadata.Hash;
                         * evt.Metadata.CreateDate = indexedMetadata.CreateDate;
                         * evt.Metadata.ModifiedDate = indexedMetadata.ModifiedDate;
                         * evt.Metadata.Size = indexedMetadata.Size;
                         */
                        if (evt.ChangeReference != null)
                        {
                            evt.ChangeReference.Revision = indexedMetadata.Revision;

                            CLMetadata.CLMetadataProcessedInternals processedInternals = new CLMetadata.CLMetadataProcessedInternals(null,// paths are not being updated
                                indexedMetadata.CreateDate,
                                indexedMetadata.ModifiedDate,
                                indexedMetadata.Size,
                                null,// event id is not being updated
                                null,// paths are not being updated
                                null,// paths are not being updated
                                null);// paths are not being updated

                            if (evt.ChangeReference.Metadata == null)
                            {
                                evt.ChangeReference.Metadata = new FileMetadata();
                            }
                            evt.ChangeReference.Metadata.HashableProperties = new FileMetadataHashableProperties(evt.ChangeReference.Metadata.HashableProperties.IsFolder,// not changed
                                processedInternals.ModifiedDate,
                                processedInternals.CreationDate,
                                processedInternals.Size);
                            string indexedMetadataHashed = indexedMetadata.Hash;
                            if (string.IsNullOrWhiteSpace(indexedMetadataHashed))
                            {
                                evt.ChangeReference.SetMD5(null);
                            }
                            else
                            {
                                evt.ChangeReference.SetMD5(Enumerable.Range(0, indexedMetadataHashed.Length)
                                    .Where(currentHex => currentHex % 2 == 0)
                                    .Select(currentHex => Convert.ToByte(indexedMetadataHashed.Substring(currentHex, 2), 16))
                                    .ToArray());
                            }
                        }
                    }

                    // if (indexedMetadata.targetPath != nil) { // we have a link object, convert
                    if (indexedMetadata.TargetPath != null)     // we have a link object, convert
                    {
                        // // symblink events are always recognized as files, therefore simply replace the occurence of the word file with link for the event action.
                        // event.action = [event.action stringByReplacingCharactersInRange:[event.action rangeOfString:CLEventTypeFileRange] withString:@"link"];
                        // event.metadata.targetPath = indexedMetadata.targetPath;
                        evt.Action = evt.Action.StringByReplacingCharactersInRange(evt.Action.RangeOfString(CLDefinitions.CLEventTypeFileRange), withString: CLDefinitions.CLEventTypeLinkRange);
                    }
                }
            }

            // I didn't see where this method was being called,
            // so I update the database metadata in case the calling method does not do it:
            // -David
            if (evt.ChangeReference != null)
            {
                CLFSMonitoringService.Instance.IndexingAgent.MergeEventIntoDatabase(evt.ChangeReference, null);
            }

            // return event;
            return evt;
        }

        //- (void)notificationService:(CLNotificationServices *)ns didReceivePushNotificationFromServer:(NSString *)notification
        void NotificationServiceDidReceivePushNotificationFromServer(bool /*CLNotificationServices*/ ns, string notification)
        {
            // Merged 7/13/12
            //NSString *sid;
    
            //if ([self.currentSIDs lastObject] != nil) {
            //    sid = [self.currentSIDs lastObject];
            //}else {
            //    sid = [[CLSettings sharedSettings] sid];
            //}
    
            //if (self.waitingForCloudResponse == YES) {
            //    self.needSyncFromCloud = YES;
            //    return;
            //}
    
            //NSNumber *eid = [NSNumber numberWithInteger:CLDotNotSaveId];
    
            //NSDictionary *events = [NSDictionary dictionaryWithObjectsAndKeys:@"/", CLMetadataCloudPath, sid, CLSyncID, nil];
    
            //NSLog(@"Requesting Sync From Cloud: \n\n%@\n\n", events);
    
            //self.needSyncFromCloud = NO;
            //self.waitingForCloudResponse = YES;
    
            //// Update UI with activity.
            //[self animateUIForSync:YES withStatusMessage:menuItemActivityLabelPreSync syncActivityCount:0];

            //__weak CLSyncService *weakSelf = self;
    
            //[self.restClient syncFromCloud:events completionHandler:^(NSDictionary *metadata, NSError *error) {
            //    __strong CLSyncService *strongSelf = weakSelf;       
            //    if (error == nil) {
            
            //        // get sync id.
            //        NSString *sid = [metadata objectForKey:CLSyncID]; // override with sid sent by phil
            
            //        if ([[metadata objectForKey:CLSyncEvents] count] > 0) {
                
            //            if ([strongSelf.currentSIDs containsObject:sid] == NO) {
            //                [strongSelf.currentSIDs addObject:sid];
            //            }
                
            //            NSLog(@"Current number of active SIDs: %lu" , [strongSelf.currentSIDs count]);
                
            //            NSArray *mdsEvents = [metadata objectForKey:@"events"];
            //            NSMutableArray *events = [NSMutableArray array];
                
            //            [mdsEvents enumerateObjectsUsingBlock:^(id mdsEvent, NSUInteger idx, BOOL *stop) {
            //                [events addObject:[CLEvent eventFromMDSEvent:mdsEvent]];
            //            }];
                
            //            NSLog(@"Response From Sync From Cloud: \n\n%@\n\n", metadata);
                
            //            NSDictionary *eventIds = [NSDictionary dictionaryWithObjectsAndKeys:eid, CLSyncEventID, sid, CLSyncID, nil];
                
            //            [strongSelf performSyncOperationWithEvents:events withEventIDs:eventIds andOrigin:CLEventOriginMDS];
                
            //        }else {

            //            NSLog(@"Response From Sync From Cloud: \n\n%@\n\n", metadata);
            //            NSLog(@"%s - Synced from cloud successfull with no objects returned.", __FUNCTION__);

            //            if ([self.activeSyncQueue count] == 0) {   //bug? Should be strongSelf
            //                if (sid != nil) { // only save if SID is not nil.
            //                    [[CLSettings sharedSettings] recordSID:sid];
            //                }
            //            }
            //
            //            // Update UI with activity.
            //            [strongSelf animateUIForSync:NO withStatusMessage:menuItemActivityLabelSynced syncActivityCount:0];
            //        }
            
            //    }else {

            //         NSLog(@"%s - %@", __FUNCTION__, error);
            //        // Update UI with activity.
            //        [strongSelf animateUIForSync:NO withStatusMessage:menuItemActivityLabelSynced syncActivityCount:0];
            //    }
        
            //    self.waitingForCloudResponse = NO;
        
            //    if (strongSelf.needSyncFromCloud == YES) {
            //        [self notificationService:nil didReceivePushNotificationFromServer:nil];
            //    }
            //} onQueue:get_cloud_sync_queue()];

            //&&&&
            string sid;

            //if ([self.currentSIDs lastObject] != nil) {
            //    sid = [self.currentSIDs lastObject];
            //}else {
            //    sid = [[CLSettings sharedSettings] sid];
            //}
            if (_currentSids.Count > 0 && _currentSids.Last<string>() != null)
            {
                sid = _currentSids.Last<string>();
            }
            else
            {
                sid = Settings.Instance.Sid;
            }

            //if (self.waitingForCloudResponse == YES) {
            //    self.needSyncFromCloud = YES;
            //    return;
            //}
            if (_waitingForCloudResponse)
            {
                _needSyncFromCloud = true;
                return;
            }

            //NSNumber *eid = [NSNumber numberWithInteger:CLDotNotSaveId];
            ulong eid = CLConstants.CLDoNotSaveId;

            //NSDictionary *events = [NSDictionary dictionaryWithObjectsAndKeys:@"/", CLMetadataCloudPath, sid, CLSyncID, nil];
            Dictionary<string, object> events = new Dictionary<string,object>()
            {
                {CLDefinitions.CLMetadataCloudPath, "/"},
                {CLDefinitions.CLSyncID, sid}
            };

            _trace.writeToLog(1, "Requesting Sync From Cloud: {0}.", events);

            //self.needSyncFromCloud = NO;
            //self.waitingForCloudResponse = YES;
            _needSyncFromCloud = false;
            _waitingForCloudResponse = true;

            // Update UI with activity.
            //TODO: Implement this.
            //[self animateUIForSync:YES withStatusMessage:menuItemActivityLabelPreSync syncActivityCount:0];

            //[self.restClient syncFromCloud:events completionHandler:^(NSDictionary *metadata, NSError *error) {

            //    if (error == nil) {

            //        NSLog(@"%s - Synced from cloud successfull with no objects returned.", __FUNCTION__);
            //        // get sync id.
            //        NSString *sid = [metadata objectForKey:CLSyncID]; // override with sid sent by phil

            //        if ([[metadata objectForKey:CLSyncEvents] count] > 0) {

            //            if ([strongSelf.currentSIDs containsObject:sid] == NO) {
            //                [strongSelf.currentSIDs addObject:sid];
            //            }

            //            NSLog(@"Current number of active SIDs: %lu" , [strongSelf.currentSIDs count]);

            //            NSArray *mdsEvents = [metadata objectForKey:@"events"];
            //            NSMutableArray *events = [NSMutableArray array];

            //            [mdsEvents enumerateObjectsUsingBlock:^(id mdsEvent, NSUInteger idx, BOOL *stop) {
            //                [events addObject:[CLEvent eventFromMDSEvent:mdsEvent]];
            //            }];

            //            NSLog(@"Response From Sync From Cloud: \n\n%@\n\n", metadata);

            //            NSDictionary *eventIds = [NSDictionary dictionaryWithObjectsAndKeys:eid, CLSyncEventID, sid, CLSyncID, nil];

            //            [strongSelf performSyncOperationWithEvents:events withEventIDs:eventIds andOrigin:CLEventOriginMDS];

            //        }else {

            //            NSLog(@"Response From Sync From Cloud: \n\n%@\n\n", metadata);
            //            NSLog(@"%s - Synced from cloud successfull with no objects returned.", __FUNCTION__);

            //            if ([strongSelf.activeSyncQueue count] == 0) {   //bug? Should be strongSelf
            //                if (sid != nil) { // only save if SID is not nil.
            //                    [[CLSettings sharedSettings] recordSID:sid];
            //                }
            //            }
            //
            //            // Update UI with activity.
            //            [strongstrongSelf animateUIForSync:NO withStatusMessage:menuItemActivityLabelSynced syncActivityCount:0];
            //        }

            //    }else {

            //        NSLog(@"%s - %@", __FUNCTION__, error);
            //        // Update UI with activity.
            //        [strongSelf animateUIForSync:NO withStatusMessage:menuItemActivityLabelSynced syncActivityCount:0];
            //    }

            //    strongSelf.waitingForCloudResponse = NO;

            //    if (strongSelf.needSyncFromCloud == YES) {
            //        [strongSelf notificationService:nil didReceivePushNotificationFromServer:nil];
            //    }
            //} onQueue:get_cloud_sync_queue()];
            _restClient.SyncFromCloud_WithCompletionHandler_OnQueue_Async(events, (result) =>
            {
                if (result.Error == null)
                {                    
                    _trace.writeToLog(1, "CLSyncService: SyncFromFileSystemMonitorWithGroupedUserEvents: Response From Sync From Cloud received: {0}.", result); 

                    string newSid = null;
                    // if ([[metadata objectForKey:CLSyncEvents] count] > 0) {
                    Dictionary<string, object> metadata = result.JsonResult;
                    if (metadata != null
                        && metadata.Count > 0
                        && metadata.ContainsKey(CLDefinitions.CLSyncEvents)
                        && metadata.ContainsKey(CLDefinitions.CLSyncID)
                        && metadata.GetType() == typeof(Dictionary<string, object>))
                    {

                        // get sync id.
                        // NSString *sid = [metadata objectForKey:CLSyncID]; // override with sid sent by server
                        newSid = (string)metadata[CLDefinitions.CLSyncID];

                        // if ([strongSelf.currentSIDs containsObject:sid] == NO) {
                        //      [strongSelf.currentSIDs addObject:sid];
                        // }
                        if (!_currentSids.Contains(newSid))
                        {
                            _currentSids.Add(newSid);
                        }

                        _trace.writeToLog(9, "CLSyncService: NotificationServiceDidReceivePushNotificationFromServer: Current number of active SIDs: {0}.", _currentSids.Count());

                        // Add received events.
                        // NSArray *mdsEvents = [metadata objectForKey:@"events"];
                        // NSMutableArray *events = [NSMutableArray array];
                        JArray mdsEvents = (JArray)metadata[CLDefinitions.CLSyncEvents];
                        List<CLEvent> eventsReceived = new List<CLEvent>();

                        // [mdsEvents enumerateObjectsUsingBlock:^(id mdsEvent, NSUInteger idx, BOOL *stop) {
                        //     [events addObject:[CLEvent eventFromMDSEvent:mdsEvent]];
                        // }];
                        foreach (JToken mdsEvent in mdsEvents)
                        {
                            Dictionary<string, object> mdsEventDictionary = mdsEvent.ToObject<Dictionary<string, object>>();
                            eventsReceived.Add(CLEvent.EventFromMDSEvent(() =>
                                {
                                    lock (CLFSMonitoringService.Instance.IndexingAgent)
                                    {
                                        return CLFSMonitoringService.Instance.IndexingAgent.LastSyncId;
                                    }
                                },
                                CLFSMonitoringService.Instance.MonitorAgent.GetCurrentPath,
                                mdsEventDictionary,
                                SyncDirection.From));
                        }

                        _trace.writeToLog(9, "CLSyncService: NotificationServiceDidReceivePushNotificationFromServer: Response From Sync From Cloud: {0}.", metadata);

                        // NSDictionary *eventIds = [NSDictionary dictionaryWithObjectsAndKeys:eid, CLSyncEventID, sid, CLSyncID, nil];
                        Dictionary<string, object> eventIds = new Dictionary<string,object>()
                        {
                            {CLDefinitions.CLSyncEventID, eid.ToString()},
                            {CLDefinitions.CLSyncID, newSid}
                        };

                        // [strongSelf performSyncOperationWithEvents:events withEventIDs:eventIds andOrigin:CLEventOriginMDS];
                        PerformSyncOperationWithEvents_withEventIDs_andOrigin(eventsReceived, eventIds, CLEventOrigin.CLEventOriginMDS);
                    }
                    else
                    {
                        // NSLog(@"Response From Sync From Cloud: \n\n%@\n\n", metadata);
                        // NSLog(@"%s - Synced from cloud successfull with no objects returned.", __FUNCTION__);
                        _trace.writeToLog(9, "CLSyncService: NotificationServiceDidReceivePushNotificationFromServer: Response from sync_from_Cloud: {0}.", metadata);

                        // if ([strongSelf.activeSyncQueue count] == 0) {   //bug? Should be strongSelf
                        if (_activeSyncQueue.Count == 0)
                        {
                            // if (sid != nil) { // only save if SID is not nil.
                            if (newSid != null)
                            {
                                // [[CLSettings sharedSettings] recordSID:sid];
                                Settings.Instance.recordSID(newSid);
                            }
                        }

                        // Update UI with activity.
                        // [strongSelf animateUIForSync:NO withStatusMessage:menuItemActivityLabelSynced syncActivityCount:0];
                        //TODO: Implement this.
                    }

                    // strongSelf.waitingForCloudResponse = NO;
                    _waitingForCloudResponse = false;

                    // if (strongSelf.needSyncFromCloud == YES) {
                    if (_needSyncFromCloud)
                    {
                        //TODO: Implement notification to recurse on this function.
                        //  [strongSelf notificationService:nil didReceivePushNotificationFromServer:nil];
                    }
                }
                else
                {
                    _trace.writeToLog(1, "CLSyncService: SyncFromFileSystemMonitorWithGroupedUserEvents: ERROR {0}.", result.Error.errorDescription);
                }
            }, get_cloud_sync_queue());
        }

        //- (void)performSyncOperationWithEvents:(NSArray *)events withEventIDs:(NSDictionary *)ids andOrigin:(CLEventOrigin)origin
        void PerformSyncOperationWithEvents_withEventIDs_andOrigin(List<CLEvent> events, Dictionary<string, object> ids, CLEventOrigin origin)
        {
            // Merged 7/10/12
            //NSLog(@"%s", __FUNCTION__);
    
            //// Update UI with activity.
            //[self animateUIForSync:YES withStatusMessage:menuItemActivityLabelIndexing syncActivityCount:0];
    
            //// Indexing, bitches.
            //NSArray *indexedEvents = [self indexSyncEventsByType:events];
    
            ///* Process Sync Events */
    
            //// Adding objects to our active sync queue
            //[self.activeSyncQueue addObjectsFromArray:indexedEvents];
    
            //// Get separated file and folder events
            //self.activeSyncFolderQueue = [self separateFolderFromFileForActiveEvents:self.activeSyncQueue wantsFolderEvents:YES];
            //self.activeSyncFileQueue = [self separateFolderFromFileForActiveEvents:self.activeSyncQueue wantsFolderEvents:NO];
        
            //// Get total object count in sync queue
            //self.syncItemsQueueCount = [self.activeSyncQueue count]; // incremental.
    
            //// Update UI with sync activity
            //[self animateUIForSync:YES withStatusMessage:menuItemActivityLabelSyncing syncActivityCount:self.syncItemsQueueCount];

    
            //// Process events in order they were received.
            //[self.activeSyncQueue enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
        
            //    CLEvent *event = obj;
            //    NSString *eventAction = event.syncHeader.action;
        
            //    if ([eventAction rangeOfString:CLEventTypeDeleteRange].location != NSNotFound) {
            //        // delete (any type) events
            //        [self processDeleteSyncEvent:event];
            //    }
        
            //    if ([eventAction isEqualToString:CLEventTypeAddFolder]) {
            //        // add folder events
            //        [self processAddFolderSyncEvent:event];
            //    }
        
            //    if ([eventAction isEqualToString:CLEventTypeRenameFolder]) {
            //        // rename folder events
            //        [self processRenameMoveFolderSyncEvent:event];
            //    }
        
            //    if ([eventAction isEqualToString:CLEventTypeMoveFolder]) {
            //        // move folder events
            //        [self processRenameMoveFolderSyncEvent:event];
            //    }
        
            //    if ([eventAction isEqualToString:CLEventTypeAddFile]) {
            //        // add file events
            //        [self processAddFileSyncEvent:event];
            //    }
        
            //    if ([eventAction isEqualToString:CLEventTypeModifyFile]) {
            //        // modify file events
            //        [self processModifyFileSyncEvent:event];
            //    }
        
            //    if ([eventAction isEqualToString:CLEventTypeRenameFile]) {
            //        // rename file events
            //        [self processRenameMoveFileSyncEvent:event];
            //    }
        
            //    if ([eventAction isEqualToString:CLEventTypeMoveFile]) {
            //        // move file events
            //        [self processRenameMoveFileSyncEvent:event];
            //    }
        
            //    if ([eventAction isEqualToString:CLEventTypeAddLink]) {
            //        // add link events
            //        [self processAddLinkSyncEvent:event];
            //    }
        
            //    if ([eventAction isEqualToString:CLEventTypeModifyLink]) {
            //        // modify link events
            //        [self processModifyLinkSyncEvent:event];
            //    }
        
            //    if ([eventAction isEqualToString:CLEventTypeRenameLink]) {
            //        // rename link events
            //        [self processRenameMoveLinkSyncEvent:event];
            //    }
        
            //    if ([eventAction isEqualToString:CLEventTypeMoveLink]) {
            //        // move link events
            //        [self processRenameMoveLinkSyncEvent:event];
            //    }
            //}];
    
            //// Display user notification
            //[[CLUIActivityService sharedService] displayUserNotificationForSyncEvents:self.activeSyncQueue];
    
            //// Remove active items from queue
            //[self.activeSyncQueue removeAllObjects];
    
            //// Sync finished.
            //[self saveSyncStateWithSID:[ids objectForKey:CLSyncID] andEID:[ids objectForKey:CLSyncEventID]];
    
            //// Update UI with activity.
            //[self animateUIForSync:NO withStatusMessage:menuItemActivityLabelSynced syncActivityCount:0];
    
            //if ([[NSManagedObjectContext defaultContext] hasChanges]){
            //    [CLIndexingServices saveDataInContext:[NSManagedObjectContext defaultContext]];
            //}
    
            //if (self.needSyncFromCloud == YES) {
            //    [self notificationService:nil didReceivePushNotificationFromServer:nil];
            //}
            //&&&&

            //NSLog(@"%s", __FUNCTION__);
            _trace.writeToLog(9, "CLSyncService: PerformSyncOperationWithEvents_withEventIds_andOrigin: Entry.");

            // Update UI with activity.
            //TODO: Implement this UI.
            //[self animateUIForSync:YES withStatusMessage:menuItemActivityLabelIndexing syncActivityCount:0];

            //// Indexing, bitches.
            //NSArray *indexedEvents = [self indexSyncEventsByType:events];
            List<CLEvent> indexedEvents = IndexSyncEventsByType(events);

            ///* Process Sync Events */

            //// Adding objects to our active sync queue
            //[self.activeSyncQueue addObjectsFromArray:indexedEvents];
            _activeSyncQueue.AddRange(indexedEvents);

            //// Get separated file and folder events
            //self.activeSyncFolderQueue = [self separateFolderFromFileForActiveEvents:self.activeSyncQueue wantsFolderEvents:YES];
            //self.activeSyncFileQueue = [self separateFolderFromFileForActiveEvents:self.activeSyncQueue wantsFolderEvents:NO];
            _activeSyncFolderQueue = SeparateFolderFromFileForActiveEvents_WantsFolderEvents(_activeSyncQueue, wantsFolderEvents: true);
            _activeSyncFileQueue = SeparateFolderFromFileForActiveEvents_WantsFolderEvents(_activeSyncQueue, wantsFolderEvents: false);

            //// Get total object count in sync queue
            //self.syncItemsQueueCount = [self.activeSyncQueue count]; // incremental.
            _syncItemsQueueCount = _activeSyncQueue.Count;

            //// Update UI with sync activity
            //TODO: Implement this UI.
            //[self animateUIForSync:YES withStatusMessage:menuItemActivityLabelSyncing syncActivityCount:self.syncItemsQueueCount];

            //// Process events in order they were received.
            //[self.activeSyncQueue enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
            foreach (CLEvent evt in _activeSyncQueue)
            {
                // CLEvent *event = obj;
                // NSString *eventAction = event.syncHeader.action;
                string eventAction = evt.SyncHeader.Action;

                // if ([eventAction rangeOfString:CLEventTypeDeleteRange].location != NSNotFound) {
                if (eventAction.Contains(CLDefinitions.CLEventTypeDeleteRange))
                {
                    // Delete (any type) events
                    // [self processDeleteSyncEvent:event];
                    ProcessDeleteSyncEvent(evt);
                }

                // if ([eventAction isEqualToString:CLEventTypeAddFolder]) {
                if (eventAction.Equals(CLDefinitions.CLEventTypeAddFolder, StringComparison.InvariantCulture))
                {
                    // Add folder events
                    // [self processAddFolderSyncEvent:event];
                    ProcessAddFolderSyncEvent(evt);
                }

                // if ([eventAction isEqualToString:CLEventTypeRenameFolder]) {
                if (eventAction.Equals(CLDefinitions.CLEventTypeRenameFolder, StringComparison.InvariantCulture))
                {
                    // Rename folder events
                    // [self processRenameMoveFolderSyncEvent:event];
                    ProcessRenameMoveFolderSyncEvent(evt);
                }

                // if ([eventAction isEqualToString:CLEventTypeMoveFolder]) {
                if (eventAction.Equals(CLDefinitions.CLEventTypeMoveFolder, StringComparison.InvariantCulture))
                {
                    // Move folder events
                    // [self processRenameMoveFolderSyncEvent:event];
                    ProcessRenameMoveFolderSyncEvent(evt);
                }

                // if ([eventAction isEqualToString:CLEventTypeAddFile]) {
                if (eventAction.Equals(CLDefinitions.CLEventTypeAddFile, StringComparison.InvariantCulture))
                {
                    // Aadd file events
                    // [self processAddFileSyncEvent:event];
                    ProcessAddFileSyncEvent(evt);
                }

                // if ([eventAction isEqualToString:CLEventTypeModifyFile]) {
                if (eventAction.Equals(CLDefinitions.CLEventTypeModifyFile, StringComparison.InvariantCulture))
                {
                    // Modify file events
                    // [self processModifyFileSyncEvent:event];
                    ProcessModifyFileSyncEvent(evt);
                }

                // if ([eventAction isEqualToString:CLEventTypeRenameFile]) {
                if (eventAction.Equals(CLDefinitions.CLEventTypeRenameFile, StringComparison.InvariantCulture))
                {
                    // Rename file events
                    // [self processRenameMoveFileSyncEvent:event];
                    ProcessRenameMoveFileSyncEvent(evt);
                }

                // if ([eventAction isEqualToString:CLEventTypeMoveFile]) {
                if (eventAction.Equals(CLDefinitions.CLEventTypeMoveFile, StringComparison.InvariantCulture))
                {
                    // Move file events
                    // [self processRenameMoveFileSyncEvent:event];
                    ProcessRenameMoveFileSyncEvent(evt);
                }

                // if ([eventAction isEqualToString:CLEventTypeAddLink]) {
                if (eventAction.Equals(CLDefinitions.CLEventTypeAddLink, StringComparison.InvariantCulture))
                {
                    // Add link events
                    // [self processAddLinkSyncEvent:event];
                    ProcessAddLinkSyncEvent(evt);
                }

                // if ([eventAction isEqualToString:CLEventTypeModifyLink]) {
                if (eventAction.Equals(CLDefinitions.CLEventTypeModifyLink, StringComparison.InvariantCulture))
                {
                    // Modify link events
                    // [self processModifyLinkSyncEvent:event];
                    ProcessModifyLinkSyncEvent(evt);
                }

                // if ([eventAction isEqualToString:CLEventTypeRenameLink]) {
                if (eventAction.Equals(CLDefinitions.CLEventTypeRenameLink, StringComparison.InvariantCulture))
                {
                    // Rename link events
                    // [self processRenameMoveLinkSyncEvent:event];
                    ProcessRenameMoveLinkSyncEvent(evt);
                }

                // if ([eventAction isEqualToString:CLEventTypeMoveLink]) {
                if (eventAction.Equals(CLDefinitions.CLEventTypeMoveLink, StringComparison.InvariantCulture))
                {
                    // Move link events
                    // [self processRenameMoveLinkSyncEvent:event];
                    ProcessRenameMoveLinkSyncEvent(evt);
                }
            }

            //// Display user notification
            //TODO: Implement this UI.
            //[[CLUIActivityService sharedService] displayUserNotificationForSyncEvents:self.activeSyncQueue];

            // Remove active items from queue
            //[self.activeSyncQueue removeAllObjects];
            _activeSyncQueue.RemoveAll((CLEvent evt) => { return true; });

            //// Sync finished.
            //[self saveSyncStateWithSID:[ids objectForKey:CLSyncID] andEID:[ids objectForKey:CLSyncEventID]];
            SaveSyncStateWithSIDAndEID((string)ids[CLDefinitions.CLSyncID], Convert.ToUInt64((string)ids[CLDefinitions.CLSyncEventID]));

            //// Update UI with activity.
            //TODO: Implement this UI.
            //[self animateUIForSync:NO withStatusMessage:menuItemActivityLabelSynced syncActivityCount:0];

            //if ([[NSManagedObjectContext defaultContext] hasChanges]){
            //    [CLIndexingServices saveDataInContext:[NSManagedObjectContext defaultContext]];
            //}
            CLIndexingService.Instance.SaveDataInContext(null);

            //if (self.needSyncFromCloud == YES) {
            //    [self notificationService:nil didReceivePushNotificationFromServer:nil];
            //}
            if (_needSyncFromCloud)
            {
                //TODO: Implement this notification
                // [self notificationService:nil didReceivePushNotificationFromServer:nil];
            }

        }

        //- (NSMutableArray *)sortEventsFoldersFirstThenFiles:(NSArray *)events
        List<CLEvent> SortEventsFoldersFirstThenFiles(List<CLEvent> events)
        {
            // Merged 7/10/12
            // NSArray *folderEventItems = [events filteredArrayUsingPredicate:[NSPredicate predicateWithBlock:^BOOL(id evaluatedObject, NSDictionary *bindings) {
            //     CLEvent *event = evaluatedObject;
            //     return (event.metadata.isDirectory  == YES);
            // }]];
    
            // NSArray *fileEventItems = [events filteredArrayUsingPredicate:[NSPredicate predicateWithBlock:^BOOL(id evaluatedObject, NSDictionary *bindings) {
            //     CLEvent *event = evaluatedObject;
            //     return (event.metadata.isDirectory == NO);
            // }]];
    
            // NSSortDescriptor *sortbyNumberOfPathComponents = [[NSSortDescriptor alloc] initWithKey:@"metadata" ascending:YES comparator:^NSComparisonResult(id obj1, id obj2) {
            //     CLMetadata *metadata1 = obj1;
            //     CLMetadata *metadata2 = obj2;
            //     NSUInteger metadata1PathCount = [[metadata1.path componentsSeparatedByString:@"/"] count];
            //     NSUInteger metadata2PathCount = [[metadata2.path componentsSeparatedByString:@"/"] count];
            //     NSComparisonResult results;
            //     if (metadata1PathCount > metadata2PathCount) {
            //         results = NSOrderedDescending;
            //     }
            //     if (metadata1PathCount < metadata2PathCount) {
            //         results = NSOrderedAscending;
            //     }
            //     if (metadata1PathCount == metadata2PathCount) {
            //         results = NSOrderedSame;
            //     }
            //     return results;
            // }];
    
            //    // Folders must be added to the index in a parent child order. We do not want to add children to the index before thier parents.
            //    // Files must be added after all the folders have been processed.
            //    NSArray *sortedFoldersItems = [folderEventItems sortedArrayUsingDescriptors:@[sortbyNumberOfPathComponents]];
            //    NSMutableArray *sortedEvents = [NSMutableArray arrayWithArray:sortedFoldersItems];
            //    [sortedEvents addObjectsFromArray:fileEventItems];
            //    return sortedEvents;
            //&&&&

            // NSArray *folderEventItems = [events filteredArrayUsingPredicate:[NSPredicate predicateWithBlock:^BOOL(id evaluatedObject, NSDictionary *bindings) {
            //     CLEvent *event = evaluatedObject;
            //     return (event.metadata.isDirectory  == YES);
            // }]];
            List<CLEvent> folderEventItems = new List<CLEvent>();
            folderEventItems = events.FindAll((CLEvent evtIndex) => { return (evtIndex.Metadata.IsDirectory == true); });

            // NSArray *fileEventItems = [events filteredArrayUsingPredicate:[NSPredicate predicateWithBlock:^BOOL(id evaluatedObject, NSDictionary *bindings) {
            //     CLEvent *event = evaluatedObject;
            //     return (event.metadata.isDirectory == NO);
            // }]];
            List<CLEvent> fileEventItems = new List<CLEvent>();
            fileEventItems = events.FindAll((CLEvent evtIndex) => { return (evtIndex.Metadata.IsDirectory == false); });

            // NSSortDescriptor *sortbyNumberOfPathComponents = [[NSSortDescriptor alloc] initWithKey:@"metadata" ascending:YES comparator:^NSComparisonResult(id obj1, id obj2) {
            //     CLMetadata *metadata1 = obj1;
            //     CLMetadata *metadata2 = obj2;
            //     NSUInteger metadata1PathCount = [[metadata1.path componentsSeparatedByString:@"/"] count];
            //     NSUInteger metadata2PathCount = [[metadata2.path componentsSeparatedByString:@"/"] count];
            //     NSComparisonResult results;
            //     if (metadata1PathCount > metadata2PathCount) {
            //         results = NSOrderedDescending;
            //     }
            //     if (metadata1PathCount < metadata2PathCount) {
            //         results = NSOrderedAscending;
            //     }
            //     if (metadata1PathCount == metadata2PathCount) {
            //         results = NSOrderedSame;
            //     }
            //     return results;
            // }];

            // // Folders must be added to the index in a parent child order. We do not want to add children to the index before thier parents.
            // // Files must be added after all the folders have been processed.
            // NSArray *sortedFoldersItems = [folderEventItems sortedArrayUsingDescriptors:@[sortbyNumberOfPathComponents]];
            // NSMutableArray *sortedEvents = [NSMutableArray arrayWithArray:sortedFoldersItems];
            folderEventItems.Sort((CLEvent event1, CLEvent event2) =>
            {
                var separators = new char[] {
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar
                };

                CLMetadata metadata1 = event1.Metadata;
                CLMetadata metadata2 = event2.Metadata;
                long metadata1PathCount = metadata1.Path.Split(separators, StringSplitOptions.RemoveEmptyEntries).Length;
                long metadata2PathCount = metadata2.Path.Split(separators, StringSplitOptions.RemoveEmptyEntries).Length;

                if (metadata1PathCount > metadata2PathCount)
                {
                    return 1;
                }

                if (metadata1PathCount < metadata2PathCount)
                {
                    return -1;
                }

                return 0;
            });

            // [sortedEvents addObjectsFromArray:fileEventItems];
            folderEventItems.AddRange(fileEventItems);

            //    return sortedEvents;
            return folderEventItems;
        }



        //- (NSArray *)indexSyncEventsByType:(NSArray *)events
        List<CLEvent> IndexSyncEventsByType(List<CLEvent> events)
        {
            // Merged 7/10/12
            // NSLog(@"%s", __FUNCTION__);
    
            // __block NSMutableArray *indexedEvents = [self sortEventsFoldersFirstThenFiles:events];
    
            // [events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
        
            //     CLEvent *event = obj;
            //     NSString *eventAction = event.syncHeader.action;
        
            //     if ([eventAction rangeOfString:CLEventTypeAddRange].location != NSNotFound) {
            //         if ([self indexAddEvent:event] == NO) {
            //             [indexedEvents removeObject:event];
            //         }
            //     }
            //     if ([eventAction rangeOfString:CLEventTypeModifyFile].location != NSNotFound) {
            //         if ([self indexModifyEvent:event] == NO) {
            //             [indexedEvents removeObject:event];
            //         }
            //     }
            //     if ([eventAction rangeOfString:CLEventTypeRenameRange].location != NSNotFound) {
            //         if ([self indexMoveRenameEvent:event] == NO) {
            //             [indexedEvents removeObject:event];
            //         }
            //     }
            //     if ([eventAction rangeOfString:CLEventTypeMoveRange].location != NSNotFound ) {
            //         if ([self indexMoveRenameEvent:event] == NO) {
            //             [indexedEvents removeObject:event];
            //         }
            //     }
            //     if ([eventAction rangeOfString:CLEventTypeDeleteRange].location != NSNotFound) {
            //         if ([self indexDeleteEvent:event] == NO) {
            //             [indexedEvents removeObject:event];
            //         }
            //     }
        
            //     if (idx % 20 == 0) {
            //         [CLIndexingServices saveDataInContext:[NSManagedObjectContext defaultContext]];
            //     }
            // }];
    
            // return indexedEvents;
            //&&&&

            // NSLog(@"%s", __FUNCTION__);
            _trace.writeToLog(9, "CLSyncService: IndexSyncEventsByType: Entry.");

            // __block NSMutableArray *indexedEvents = [self sortEventsFoldersFirstThenFiles:events];
            List<CLEvent> indexedEvents = SortEventsFoldersFirstThenFiles(events);

            // [events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
            int idx = 0;
            foreach (CLEvent evt in events)
            {
                // CLEvent *event = obj;
                // NSString *eventAction = event.syncHeader.action;
                string eventAction = evt.SyncHeader.Action;

                // if ([eventAction rangeOfString:CLEventTypeAddRange].location != NSNotFound) {
                if (eventAction.Contains(CLDefinitions.CLEventTypeAddRange))
                {
                    // if ([self indexAddEvent:event] == NO) {
                    if (!IndexAddEvent(evt))
                    {
                        // [indexedEvents removeObject:event];
                        indexedEvents.Remove(evt);
                    }
                }

                // if ([eventAction rangeOfString:CLEventTypeModifyFile].location != NSNotFound) {
                if (eventAction.Contains(CLDefinitions.CLEventTypeModifyFile))
                {
                    // if ([self indexModifyEvent:event] == NO) {
                    if (!IndexModifyEvent(evt))
                    {
                        // [indexedEvents removeObject:event];
                        indexedEvents.Remove(evt);
                    }
                }

                // if ([eventAction rangeOfString:CLEventTypeRenameRange].location != NSNotFound) {
                if (eventAction.Contains(CLDefinitions.CLEventTypeRenameRange))
                {
                    // if ([self indexMoveRenameEvent:event] == NO) {
                    if (!IndexMoveRenameEvent(evt))
                    {
                        // [indexedEvents removeObject:event];
                        indexedEvents.Remove(evt);
                    }
                }

                // if ([eventAction rangeOfString:CLEventTypeMoveRange].location != NSNotFound ) {
                if (eventAction.Contains(CLDefinitions.CLEventTypeMoveRange))
                {
                    // if ([self indexMoveRenameEvent:event] == NO) {
                    if (!IndexMoveRenameEvent(evt))
                    {
                        // [indexedEvents removeObject:event];
                        indexedEvents.Remove(evt);
                    }
                }

                // if ([eventAction rangeOfString:CLEventTypeDeleteRange].location != NSNotFound) {
                if (eventAction.Contains(CLDefinitions.CLEventTypeDeleteRange))
                {
                    // if ([self indexDeleteEvent:event] == NO) {
                    if (!IndexDeleteEvent(evt))
                    {
                        // [indexedEvents removeObject:event];
                        indexedEvents.Remove(evt);
                    }
                }

                // if (idx % 20 == 0) {
                if (idx++ % 20 == 0)
                {
                    // [CLIndexingServices saveDataInContext:[NSManagedObjectContext defaultContext]];
                    CLIndexingService.Instance.SaveDataInContext(null);
                }
            }

            // return indexedEvents;
            return indexedEvents;
        }

        //- (BOOL)indexMoveRenameEvent:(CLEvent *)moveRenameEvent
        bool IndexMoveRenameEvent(CLEvent moveRenameEvent)
        {
            // Merged 7/10/12
            // BOOL shouldProcessEvent = YES;
    
            // CLMetadata *indexedMetadata = [CLIndexingServices indexedMetadataForEvent:moveRenameEvent];
            // NSString *fromFileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:moveRenameEvent.metadata.fromPath];
            // NSString *toFileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:moveRenameEvent.metadata.toPath];
    
            // BOOL isMDSEvent = (moveRenameEvent.syncHeader.status != nil) ? YES : NO;
    
            // if (indexedMetadata) {
            //     if ([[NSFileManager defaultManager] fileExistsAtPath:fromFileSystemPath]) {
            //         [CLIndexingServices markItemForEvent:moveRenameEvent asPending:YES];
            //     }else if ([[NSFileManager defaultManager] fileExistsAtPath:toFileSystemPath]){
            //         if (isMDSEvent == YES) {
            //             [CLIndexingServices markItemForEvent:moveRenameEvent asPending:YES];
            //         }
            //         else{
            //             [self badgeFileAtCloudPath:moveRenameEvent.metadata.toPath withBadge:cloudAppBadgeSynced];
            //             shouldProcessEvent = NO;
            //         }
            //     }
            // }else {
            //     NSLog(@"%s - THIS SHOULD NEVER HAPPEN BUT IT DID!!  ERROR", __FUNCTION__);
            //     if ([[NSFileManager defaultManager] fileExistsAtPath:fromFileSystemPath]) {
            //         // TODO: Create index object from event (using Metadata from FS)
            //         // let it ride...
            //     }else {
            //         if ([[NSFileManager defaultManager] fileExistsAtPath:toFileSystemPath]){
            //             // TODO: Create index object from event (using Metadata from FS)
            //             // Punt (the file is already at destination
            //             // Badge
            //         }
            //     }
            // }

            // return shouldProcessEvent;
            //&&&&

            // BOOL shouldProcessEvent = YES;
            bool shouldProcessEvent = true;

            // CLMetadata *indexedMetadata = [CLIndexingServices indexedMetadataForEvent:moveRenameEvent];
            // NSString *fromFileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:moveRenameEvent.metadata.fromPath];
            // NSString *toFileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:moveRenameEvent.metadata.toPath];
            CLMetadata indexedMetadata = CLIndexingService.Instance.IndexedMetadataForEvent(moveRenameEvent);
            string fromFileSystemPath = Settings.Instance.CloudFolderPath + moveRenameEvent.Metadata.FromPath;
            string toFileSystemPath = Settings.Instance.CloudFolderPath + moveRenameEvent.Metadata.ToPath;

            // BOOL isMDSEvent = (moveRenameEvent.syncHeader.status != nil) ? YES : NO;
            bool isMDSEvent = (moveRenameEvent.SyncHeader.Status != null) ? true : false;

            // if (indexedMetadata) {
            if (indexedMetadata != null)
            {
                // if ([[NSFileManager defaultManager] fileExistsAtPath:fromFileSystemPath]) {
                if (File.Exists(fromFileSystemPath))
                {
                    // [CLIndexingServices markItemForEvent:moveRenameEvent asPending:YES];
                    CLIndexingService.Instance.MarkItemForEvent_asPending(moveRenameEvent, pending: true);
                }
                // }else if ([[NSFileManager defaultManager] fileExistsAtPath:toFileSystemPath]){
                else if (File.Exists(toFileSystemPath))
                {
                    // if (isMDSEvent == YES) {
                    if (isMDSEvent)
                    {
                        // [CLIndexingServices markItemForEvent:moveRenameEvent asPending:YES];
                        CLIndexingService.Instance.MarkItemForEvent_asPending(moveRenameEvent, pending: true);
                    }
                    else
                    {
                        // [self badgeFileAtCloudPath:moveRenameEvent.metadata.toPath withBadge:cloudAppBadgeSynced];
                        // shouldProcessEvent = NO;
                        BadgeFileAtCloudPath_withBadge(moveRenameEvent.Metadata.ToPath, cloudAppIconBadgeType.cloudAppBadgeSynced);
                        shouldProcessEvent = false;
                    }
                }
            }
            else
            {
                // NSLog(@"%s - THIS SHOULD NEVER HAPPEN BUT IT DID!!  ERROR", __FUNCTION__);
                _trace.writeToLog(1, "CLSyncService: IndexMoveRenameEvent: ERROR: This should never happen but it did!");

                // if ([[NSFileManager defaultManager] fileExistsAtPath:fromFileSystemPath]) {
                if (File.Exists(fromFileSystemPath))
                {
                    // TODO: Create index object from event (using Metadata from FS)
                    // let it ride...
                }
                else
                {
                    // if ([[NSFileManager defaultManager] fileExistsAtPath:toFileSystemPath]){
                    if (File.Exists(toFileSystemPath))
                    {
                        // TODO: Create index object from event (using Metadata from FS)
                        // Punt (the file is already at destination
                        // Badge
                    }
                }
            }

            // return shouldProcessEvent;
            return shouldProcessEvent;
        }

        //- (BOOL)indexModifyEvent:(CLEvent *)modifyEvent
        bool IndexModifyEvent(CLEvent modifyEvent)
        {
            // Merged 7/10/12
            // BOOL shouldProcessEvent = YES;
    
            // CLMetadata *indexedMetadata = [CLIndexingServices indexedMetadataForEvent:modifyEvent];
            // if (indexedMetadata) {
            //     NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:modifyEvent.metadata.path];
            //     if ([[NSFileManager defaultManager] fileExistsAtPath:fileSystemPath]){
            //         [CLIndexingServices markItemForEvent:modifyEvent asPending:YES];
            //     }
            // }

            // return shouldProcessEvent;
            //&&&&

            // BOOL shouldProcessEvent = YES;
            bool shouldProcessEvent = true;

            // CLMetadata *indexedMetadata = [CLIndexingServices indexedMetadataForEvent:modifyEvent];
            CLMetadata indexedMetadata = CLIndexingService.Instance.IndexedMetadataForEvent(modifyEvent);

            // if (indexedMetadata) {
            if (indexedMetadata != null)
            {
                // NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:modifyEvent.metadata.path];
                string fileSystemPath = Settings.Instance.CloudFolderPath + modifyEvent.Metadata.Path;

                // if ([[NSFileManager defaultManager] fileExistsAtPath:fileSystemPath]){
                if (File.Exists(fileSystemPath))
                {
                    // [CLIndexingServices markItemForEvent:modifyEvent asPending:YES];
                    CLIndexingService.Instance.MarkItemForEvent_asPending(modifyEvent, pending: true);
                }
            }

            // return shouldProcessEvent;
            return shouldProcessEvent;
        }

        //- (BOOL)indexAddEvent:(CLEvent *)addEvent
        bool IndexAddEvent(CLEvent addEvent)
        {
            // Merged 7/10/12
            // BOOL shouldProcessEvent = YES;
    
            // CLMetadata *indexedMetadata = [CLIndexingServices indexedMetadataForEvent:addEvent];
            // NSString *eventAction = addEvent.syncHeader.action;
            // BOOL isFileEvent = [eventAction rangeOfString:CLEventTypeFileRange].location != NSNotFound ? YES : NO;
            // if (indexedMetadata) {
        
            //     NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:addEvent.metadata.path];
            //     if (isFileEvent == YES) {
            //         if (indexedMetadata.isPending == NO) {
            //             if ([[NSFileManager defaultManager] fileExistsAtPath:fileSystemPath]) {
            //                 if ([addEvent.metadata.hash isEqualToString:indexedMetadata.hash]){
            //                     [self badgeFileAtCloudPath:addEvent.metadata.path withBadge:cloudAppBadgeSynced];
            //                     shouldProcessEvent = NO;
            //                 }else {
            //                     // TODO: Check to see if the item is in the active sync queue
            //                     [CLIndexingServices markItemForEvent:addEvent asPending:YES];
            //                 }
            //             }else {
            //                 // Check to see if
            //                 [CLIndexingServices markItemForEvent:addEvent asPending:YES];
            //             }
            //         }else {
            //             // TODO: Pending == YES
            //             // TODO: Check to see if the event is in the active sync queue if yes punt if no check if it exists in file system and hash are the same.
            //         }
            //     }else {
            //         if (indexedMetadata.isPending == NO){
            //             if ([[NSFileManager defaultManager] fileExistsAtPath:fileSystemPath]){
            //                 [self badgeFileAtCloudPath:addEvent.metadata.path withBadge:cloudAppBadgeSynced];
            //                 shouldProcessEvent = NO;
            //             } else {
            //                 [CLIndexingServices markItemForEvent:addEvent asPending:YES];
            //             }
            //         }else {
            //             // TODO: Pending == YES
            //             // TODO: Check to see if the event is in the active sync queue if yes punt
            //         }
            //     }
            // }else {
        
            //     [CLIndexingServices addMetedataItem:addEvent.metadata pending:YES];
            //     if (isFileEvent == NO) NSLog(@"Folder Events Added to Index: %@", addEvent.metadata.path);
            // }
    
            // return shouldProcessEvent;
            //&&&&

            // BOOL shouldProcessEvent = YES;
            bool shoulProcessEvent = true;

            // CLMetadata *indexedMetadata = [CLIndexingServices indexedMetadataForEvent:addEvent];
            CLMetadata indexedMetadata = CLIndexingService.Instance.IndexedMetadataForEvent(addEvent);

            // NSString *eventAction = addEvent.syncHeader.action;
            string eventAction = addEvent.SyncHeader.Action;

            // BOOL isFileEvent = [eventAction rangeOfString:CLEventTypeFileRange].location != NSNotFound ? YES : NO;
            bool isFileEvent = eventAction.Contains(CLDefinitions.CLEventTypeFileRange);

            // if (indexedMetadata) {
            if (indexedMetadata != null)
            {
                // NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:addEvent.metadata.path];
                string fileSystemPath = Settings.Instance.CloudFolderPath + addEvent.Metadata.Path;

                // if (isFileEvent == YES) {
                if (isFileEvent)
                {
                    // if (indexedMetadata.isPending == NO) {
                    if (!indexedMetadata.IsPending)
                    {
                        // if ([[NSFileManager defaultManager] fileExistsAtPath:fileSystemPath]) {
                        if (File.Exists(fileSystemPath))
                        {
                            // if ([addEvent.metadata.hash isEqualToString:indexedMetadata.hash]){
                            if (addEvent.Metadata.Hash.Equals(indexedMetadata.Hash, StringComparison.InvariantCulture))
                            {
                                // [self badgeFileAtCloudPath:addEvent.metadata.path withBadge:cloudAppBadgeSynced];
                                BadgeFileAtCloudPath_withBadge(addEvent.Metadata.Path, cloudAppIconBadgeType.cloudAppBadgeSynced);
                                // shouldProcessEvent = NO;
                                shoulProcessEvent = false;
                            }
                            else
                            {
                                // // TODO: Check to see if the item is in the active sync queue
                                // [CLIndexingServices markItemForEvent:addEvent asPending:YES];
                                CLIndexingService.Instance.MarkItemForEvent_asPending(addEvent, pending: true);
                            }
                        }
                        else
                        {
                            // // Check to see if
                            // [CLIndexingServices markItemForEvent:addEvent asPending:YES];
                            CLIndexingService.Instance.MarkItemForEvent_asPending(addEvent, pending: true);
                        }
                    }
                    else
                    {
                        // // TODO: Pending == YES
                        // // TODO: Check to see if the event is in the active sync queue if yes punt if no check if it exists in file system and hash are the same.
                    }
                }
                else
                {
                    // if (indexedMetadata.isPending == NO){
                    if (!indexedMetadata.IsPending)
                    {
                        // if ([[NSFileManager defaultManager] fileExistsAtPath:fileSystemPath]){
                        if (File.Exists(fileSystemPath))
                        {
                            // [self badgeFileAtCloudPath:addEvent.metadata.path withBadge:cloudAppBadgeSynced];
                            // shouldProcessEvent = NO;
                            BadgeFileAtCloudPath_withBadge(addEvent.Metadata.Path, cloudAppIconBadgeType.cloudAppBadgeSynced);
                            shoulProcessEvent = false;
                        }
                        else
                        {
                            // [CLIndexingServices markItemForEvent:addEvent asPending:YES];
                            CLIndexingService.Instance.MarkItemForEvent_asPending(addEvent, pending: true);
                        }
                    }
                    else
                    {
                        // // TODO: Pending == YES
                        // // TODO: Check to see if the event is in the active sync queue if yes punt
                    }
                }
            }
            else
            {
                // [CLIndexingServices addMetedataItem:addEvent.metadata pending:YES];
                CLIndexingService.Instance.AddMetadataItem_pending(addEvent.Metadata, pending: true);

                // if (isFileEvent == NO) NSLog(@"Folder Events Added to Index: %@", addEvent.metadata.path);
                if (!isFileEvent)
                {
                    _trace.writeToLog(9, "CLSyncService: IndexAddEvent: FOlder events added to index, path: {0}.", addEvent.Metadata.Path);
                }
            }

            // return shouldProcessEvent;
            return shoulProcessEvent;
        }

        //- (BOOL)indexDeleteEvent:(CLEvent *)deleteEvent
        bool IndexDeleteEvent(CLEvent deleteEvent)
        {
            // Merged 7/10/12
            // BOOL shouldProcessEvent = YES;
            // CLMetadata *indexedMetadata = [CLIndexingServices indexedMetadataForEvent:deleteEvent];
            // if (indexedMetadata) {
            //     [CLIndexingServices markItemForEvent:deleteEvent asPending:YES];
            // }else {
            //     NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:deleteEvent.metadata.path];
            //     if ([[NSFileManager defaultManager] fileExistsAtPath:fileSystemPath] == NO) {
            //         shouldProcessEvent = NO;
            //     }else {
            //         // process event
            //     }
            // }
    
            // return shouldProcessEvent;
            //&&&&

            // BOOL shouldProcessEvent = YES;
            bool shouldProcessEvent = true;

            // CLMetadata *indexedMetadata = [CLIndexingServices indexedMetadataForEvent:deleteEvent];
            CLMetadata indexedMetadata = CLIndexingService.Instance.IndexedMetadataForEvent(deleteEvent);

            // if (indexedMetadata) {
            if (indexedMetadata != null)
            {
                // [CLIndexingServices markItemForEvent:deleteEvent asPending:YES];
                CLIndexingService.Instance.MarkItemForEvent_asPending(deleteEvent, pending: true);
            }
            else
            {
                // NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:deleteEvent.metadata.path];
                string fileSystemPath = Settings.Instance.CloudFolderPath + deleteEvent.Metadata.Path;

                // if ([[NSFileManager defaultManager] fileExistsAtPath:fileSystemPath] == NO) {
                if (!File.Exists(fileSystemPath))
                {
                    // shouldProcessEvent = NO;
                    shouldProcessEvent = false;
                }
                else
                {
                    // Process event
                }
            }

            // return shouldProcessEvent;
            return shouldProcessEvent;
        }

        //- (void)updateIndexForSyncEvent:(CLEvent *)event
        void UpdateIndexForSyncEvent(CLEvent evt)
        {
            // Merged 7/10/12
            // NSString *eventType = event.syncHeader.action;
            // // Update index for event completion success..
    
            // if ([eventType rangeOfString:CLEventTypeDeleteRange].location != NSNotFound){
            //     [CLIndexingServices removeItemForEvent:event];
            // }else if ([eventType rangeOfString:CLEventTypeAddRange].location != NSNotFound){
            //     [CLIndexingServices markItemForEvent:event asPending:NO];
            // }else {
            //     [CLIndexingServices updateLocalIndexItemWithEvent:event pending:NO];
            // }
            // [CLIndexingServices saveDataInContext:[NSManagedObjectContext defaultContext]];
            //&&&&

            // NSString *eventType = event.syncHeader.action;
            string eventType = evt.SyncHeader.Action;

            // // Update index for event completion success..

            // if ([eventType rangeOfString:CLEventTypeDeleteRange].location != NSNotFound){
            if (eventType.Contains(CLDefinitions.CLEventTypeDeleteRange))
            {
                // [CLIndexingServices removeItemForEvent:event];
                CLIndexingService.Instance.RemoveItemForEvent(evt);
            }
            // }else if ([eventType rangeOfString:CLEventTypeAddRange].location != NSNotFound){
            else if (eventType.Contains(CLDefinitions.CLEventTypeAddRange))
            {
                // [CLIndexingServices markItemForEvent:event asPending:NO];
                CLIndexingService.Instance.MarkItemForEvent_asPending(evt, pending: false);
            }
            else
            {
                // [CLIndexingServices updateLocalIndexItemWithEvent:event pending:NO];
                CLIndexingService.Instance.UpdateLocalIndexItemWithEvent_pending(evt, pending: false);
            }

            // [CLIndexingServices saveDataInContext:[NSManagedObjectContext defaultContext]];
            CLIndexingService.Instance.SaveDataInContext(null);
        }

        //- (void)processDeleteSyncEvent:(CLEvent *)event
        void ProcessDeleteSyncEvent(CLEvent evt)
        {
            // Merged 7/10/12
            // NSString *actionType = event.syncHeader.action;
            // NSString *status = event.syncHeader.status;
            // NSString *path = event.metadata.path;
    
            // // folder events first.
            // if ([actionType rangeOfString:CLEventTypeDeleteRange].location != NSNotFound) {
        
            //     BOOL success = YES; // assume true for events we originated (since they already happened), override value for MDS execution.
        
            //     NSError *error;
            //     if (status == nil) { // MDS origin, Philis told us we need to do this.
            //         success = [[CLFSDispatcher defaultDispatcher] deleteItemAtPath:path error:&error];
            //     }
        
            //     if (success == NO) {
            //         NSLog(@"%s - There was an error deleting a file system item. Error: %@", __FUNCTION__ ,error );
            //     }
        
            //     // update index and ui.
            //     [self performUpdateForSyncEvent:event success:success];
            // }
            //&&&&

            // NSString *actionType = event.syncHeader.action;
            // NSString *status = event.syncHeader.status;
            // NSString *path = event.metadata.path;
            string actionType = evt.SyncHeader.Action;
            string status = evt.SyncHeader.Status;
            string path = evt.Metadata.Path;

            // Folder events first.
            // if ([actionType rangeOfString:CLEventTypeDeleteRange].location != NSNotFound) {
            if (actionType.Contains(CLDefinitions.CLEventTypeDeleteRange))
            {
                // BOOL success = YES; // assume true for events we originated (since they already happened), override value for MDS execution.
                bool success = true;     // assume true for events we originated (since they already happened), override value for MDS execution.

                // NSError *error;
                CLError error = null;

                // if (status == nil) { // MDS origin, Philis told us we need to do this.
                if (status == null)    // MDS origin, Philis told us we need to do this.
                {
                    // success = [[CLFSDispatcher defaultDispatcher] deleteItemAtPath:path error:&error];
                    success = CLFSDispatcher.Instance.DeleteItemAtPath_error(path, out error);
                }

                // if (success == NO) {
                if (!success)
                {
                    // NSLog(@"%s - There was an error deleting a file system item. Error: %@", __FUNCTION__ ,error );
                    _trace.writeToLog(1, "CLSyncService: ProcessDeleteSyncEvent: There was an error deleting a file system item.  Error: {0}. Code: {1}.", error.errorDescription, error.errorCode);
                }

                // Update index and ui.
                // [self performUpdateForSyncEvent:event success:success];
                PerformUpdateForSyncEvent_Success(evt, success: success);
            }
        }

        //- (void)processAddFolderSyncEvent:(CLEvent *)event
        void ProcessAddFolderSyncEvent(CLEvent evt)
        {
            // Merged 7/10/12
            //NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:event.metadata.path];
    
            //if (event.isMDSEvent) {
        
            //    NSString *status = event.syncHeader.status;
        
            //    BOOL success = YES; // assume true for events we originated (since they already happened), override value for MDS execution.
            //    BOOL createdAttributes = NO;
        
            //    if (status == nil) { // MDS origin, Philis told us we need to do this.
            
            //        success = [[CLFSDispatcher defaultDispatcher] createDirectoryAtPath:event.metadata.path error:nil];
            
            //        if (success == NO) {
                
            //            // TODO: check error here and try to remediate.
            //        }
            //        else {
                
            //            [self badgeFileAtCloudPath:event.metadata.path withBadge:cloudAppBadgeSyncing]; // mark folder as syncing
                
            //            NSError *attributesError;
            //            createdAttributes = [[CLFSDispatcher defaultDispatcher] updateAttributesUsingMetadata:event.metadata forItemAtPath:fileSystemPath error:&attributesError];
            //            if (attributesError) {
            //                NSLog(@"%s - %@", __FUNCTION__, [attributesError description]);
            //            }
            //        }
            //    }
        
            //    // update index and ui.
            //    [self performUpdateForSyncEvent:event success:success];
            //}
            //&&&&

            // NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:event.metadata.path];
            string fileSystemPath = Settings.Instance.CloudFolderPath + evt.Metadata.Path;
    
            // if (event.isMDSEvent) {
            if (evt.IsMDSEvent)
            {        
                // NSString *status = event.syncHeader.status;
                string status = evt.SyncHeader.Status;
        
                // BOOL success = YES; // assume true for events we originated (since they already happened), override value for MDS execution.
                // BOOL createdAttributes = NO;
                bool success = true;    // assume true for events we originated (since they already happened), override value for MDS execution.
                bool createdAttributes = false;
        
                // if (status == nil) { // MDS origin, Philis told us we need to do this.
                if (status == null)
                {
                    // success = [[CLFSDispatcher defaultDispatcher] createDirectoryAtPath:event.metadata.path error:nil];
                    CLError error = null;
                    success = CLFSDispatcher.Instance.CreateDirectoryAtPath_error(evt.Metadata.Path, out error);
                    // if (success == NO) {
                    if (!success)
                    {
                        //TODO: check error here and try to remediate.
                    }
                    else
                    {               
                        // [self badgeFileAtCloudPath:event.metadata.path withBadge:cloudAppBadgeSyncing]; // mark folder as syncing
                        BadgeFileAtCloudPath_withBadge(evt.Metadata.Path, cloudAppIconBadgeType.cloudAppBadgeSyncing);
                
                        // NSError *attributesError;
                        CLError attributesError = null;

                        // createdAttributes = [[CLFSDispatcher defaultDispatcher] updateAttributesUsingMetadata:event.metadata forItemAtPath:fileSystemPath error:&attributesError];
                        createdAttributes = CLFSDispatcher.Instance.UpdateAttributesUsingMetadata_forItemAtPath_error(evt.Metadata, fileSystemPath, out attributesError);
                        // if (attributesError) {
                        if (attributesError != null)
                        {
                            // NSLog(@"%s - %@", __FUNCTION__, [attributesError description]);
                            _trace.writeToLog(1, "CLSyncService: ProcessAddFolderSyncEvent: ERROR: Updating attributes for file <{0}>.  Error: {1}, Code: {1}.", fileSystemPath, error.errorDescription, error.errorCode);
                        }
                    }
                }
        
                // Update index and ui.
                // [self performUpdateForSyncEvent:event success:success];
                PerformUpdateForSyncEvent_Success(evt, success: success);
            }
        }

        //- (void)processRenameMoveFolderSyncEvent:(CLEvent *)event
        void ProcessRenameMoveFolderSyncEvent(CLEvent evt)
        {
            // Merged 7/10/12
            // NSString *status = event.syncHeader.status;
            // NSString *toPath = event.metadata.toPath;
            // NSString *fromPath = event.metadata.fromPath;
                    
            // BOOL success = YES; // assume true for events we originated (since they already happened), override value for MDS execution.
    
            // if (status == nil) { // MDS origin, Philis told us we need to do this.
            //     success = [[CLFSDispatcher defaultDispatcher] moveItemAtPath:fromPath to:toPath error:nil];
            // }
    
            // Update index and ui.
            // [self performUpdateForSyncEvent:event success:success];
            //&&&&

            // NSString *status = event.syncHeader.status;
            // NSString *toPath = event.metadata.toPath;
            // NSString *fromPath = event.metadata.fromPath;
            string status = evt.SyncHeader.Status;
            string toPath = evt.Metadata.ToPath;
            string fromPath = evt.Metadata.FromPath;

            // BOOL success = YES; // assume true for events we originated (since they already happened), override value for MDS execution.
            bool success = true;    // assume true for events we originated (since they already happened), override value for MDS execution.

            // if (status == nil) { // MDS origin, Philis told us we need to do this.
            if (status == null)
            {
                // success = [[CLFSDispatcher defaultDispatcher] moveItemAtPath:fromPath to:toPath error:nil];
                CLError error;
                success = CLFSDispatcher.Instance.MoveItemAtPath_to_error(fromPath, toPath, out error);
                if (!success)
                {
                    //TODO: Check error an try to remediate.
                }
            }

            // Update index and ui.
            // [self performUpdateForSyncEvent:event success:success];
            PerformUpdateForSyncEvent_Success(evt, success: success);
        }



        //- (void)processAddFileSyncEvent:(CLEvent *)event
        void ProcessAddFileSyncEvent(CLEvent evt)
        {
            // Merged 7/10/12
            // // Break down upload and downloads
            // NSMutableArray *uploadEvents = [NSMutableArray array];
            // NSMutableArray *downloadEvents = [NSMutableArray array];

            // if (event.isMDSEvent) {
        
            //     NSString *status = event.syncHeader.status;
        
            //     if (status == nil) { // MDS origin, Philis told us we need to do this.
            
            //         // we need to download this file.
            //         [downloadEvents addObject:event];
            
            //     } else { //FSM origin, we created this file, need to check for upload.
            
            //         if ([status isEqualToString:CLEventTypeUpload] || [status isEqualToString:CLEventTypeUploading]) { // we need to upload this file.
                
            //             [uploadEvents addObject:event];
            //         }
            
            //         if ([status isEqualToString:CLEventTypeExists] || [status isEqualToString:CLEventTypeDuplicate]) { // we do not need to upload this file.
                
            //             // update index and ui.
            //             [self performUpdateForSyncEvent:event success:YES];
            //         }
            
            //         if ([status isEqualToString:CLEventTypeConflict]) {
                
            //             // TODO: handle conflict here.
                
            //             // update index and ui.
            //             [self performUpdateForSyncEvent:event success:YES];
            //         }
            //     }
            // }
            
            // // execute upload and download events.
            // if ([uploadEvents count] > 0) {
            //     [self dispatchUploadEvents:uploadEvents];
            // }
    
            // if ([downloadEvents count] > 0) {
            //     [self dispatchDownloadEvents:downloadEvents];
            // }
            //&&&&

            // Break down upload and downloads
            // NSMutableArray *uploadEvents = [NSMutableArray array];
            // NSMutableArray *downloadEvents = [NSMutableArray array];
            List<CLEvent> uploadEvents = new List<CLEvent>();
            List<CLEvent> downloadEvents = new List<CLEvent>();

            // if (event.isMDSEvent) {
            if (evt.IsMDSEvent)
            {
                // NSString *status = event.syncHeader.status;
                string status = evt.SyncHeader.Status;

                // if (status == nil) { // MDS origin, Philis told us we need to do this.
                if (string.IsNullOrWhiteSpace(status))
                {
                    // We need to download this file.
                    // [downloadEvents addObject:event];
                    downloadEvents.Add(evt);
                }
                else   //FSM origin, we created this file, need to check for upload.
                {
                    // if ([status isEqualToString:CLEventTypeUpload] || [status isEqualToString:CLEventTypeUploading]) { // we need to upload this file.
                    if (status.Equals(CLDefinitions.CLEventTypeUpload, StringComparison.InvariantCulture) ||
                        status.Equals(CLDefinitions.CLEventTypeUploading, StringComparison.InvariantCulture))
                    {
                        // [uploadEvents addObject:event];
                        uploadEvents.Add(evt);
                    }

                    // if ([status isEqualToString:CLEventTypeExists] || [status isEqualToString:CLEventTypeDuplicate]) { // we do not need to upload this file.
                    if (status.Equals(CLDefinitions.CLEventTypeExists, StringComparison.InvariantCulture) ||
                        status.Equals(CLDefinitions.CLEventTypeDuplicate, StringComparison.InvariantCulture))
                    {
                        // Update index and ui.
                        // [self performUpdateForSyncEvent:event success:YES];
                        PerformUpdateForSyncEvent_Success(evt, success: true);
                    }

                    // if ([status isEqualToString:CLEventTypeConflict]) {
                    if (status.Equals(CLDefinitions.CLEventTypeConflict, StringComparison.InvariantCulture))
                    {
                        //     // TODO: handle conflict here.

                        // Update index and ui.
                        // [self performUpdateForSyncEvent:event success:YES];
                        PerformUpdateForSyncEvent_Success(evt, success: true);
                    }
                }
            }

            // Execute upload and download events.
            // if ([uploadEvents count] > 0) {
            if (uploadEvents.Count > 0)
            {
                // [self dispatchUploadEvents:uploadEvents];
                DispatchUploadEvents(uploadEvents);
            }

            // if ([downloadEvents count] > 0) {
            if (downloadEvents.Count > 0)
            {
                // [self dispatchDownloadEvents:downloadEvents];
                DispatchDownloadEvents(downloadEvents);
            }
        }

        //- (void)processModifyFileSyncEvent:(CLEvent *)event
        void ProcessModifyFileSyncEvent(CLEvent evt)
        {
            // Merged 7/10/12
            // // Break down upload and downloads
            // NSMutableArray *uploadEvents = [NSMutableArray array];
            // NSMutableArray *downloadEvents = [NSMutableArray array];

            // NSString *status = event.syncHeader.status;
        
            // if (status == nil) { // MDS origin, Philis told us we need to do this.
        
            //     // we need to download this file.
            //     [downloadEvents addObject:event];
            //     [self badgeFileAtCloudPath:event.metadata.path withBadge:cloudAppBadgeSyncing]; // mark the file to be uploaded as syncing.
        
            // } else { //FSM origin, we modified this file, need to check for upload.
        
            //     if ([status isEqualToString:CLEventTypeUpload] || [status isEqualToString:CLEventTypeUploading]) { // we need to upload this file.
            
            //         [uploadEvents addObject:event];
            //     }
        
            //     if ([status isEqualToString:CLEventTypeExists] || [status isEqualToString:CLEventTypeDuplicate]) { // we do not need to upload this file.
            
            //         // update ui.
            //         [self performUpdateForSyncEvent:event success:YES];
            //     }
        
            //     if ([status isEqualToString:CLEventTypeConflict]) {
            
            //         // TODO: handle conflict here.
            
            //         // update ui.
            //         [self performUpdateForSyncEvent:event success:YES];
            //     }
            // }

            // // execute upload and download events.
            // if ([uploadEvents count] > 0) {
            //     [self dispatchUploadEvents:uploadEvents];
            // }
    
            // if ([downloadEvents count] > 0) {
        
            //     // sorting downloads by size (ascending)
            //     NSArray *sortedDownloadEvents = [downloadEvents sortedArrayUsingComparator: ^(CLEvent * event1, CLEvent *event2) {
            
            //         if ([event1.metadata.size intValue] > [event2.metadata.size intValue]) {
            //             return (NSComparisonResult)NSOrderedDescending;
            //         }
            
            //         if ([event1.metadata.size intValue] < [event2.metadata.size intValue]) {
            //             return (NSComparisonResult)NSOrderedAscending;
            //         }
            //         return (NSComparisonResult)NSOrderedSame;
            //     }];
        
            //     [self dispatchDownloadEvents:sortedDownloadEvents];
            // }
            //&&&&

            // // Break down upload and downloads
            // NSMutableArray *uploadEvents = [NSMutableArray array];
            // NSMutableArray *downloadEvents = [NSMutableArray array];
            List<CLEvent> uploadEvents = new List<CLEvent>();
            List<CLEvent> downloadEvents = new List<CLEvent>();

            // NSString *status = event.syncHeader.status;
            string status = evt.SyncHeader.Status;

            // if (status == nil) { // MDS origin, Philis told us we need to do this.
            if (status == null)
            {

                // We need to download this file.
                // [downloadEvents addObject:event];
                // [self badgeFileAtCloudPath:event.metadata.path withBadge:cloudAppBadgeSyncing]; // mark the file to be uploaded as syncing.
                downloadEvents.Add(evt);
                BadgeFileAtCloudPath_withBadge(evt.Metadata.Path, cloudAppIconBadgeType.cloudAppBadgeSyncing);
            }
            else   //FSM origin, we modified this file, need to check for upload.
            {
                // if ([status isEqualToString:CLEventTypeUpload] || [status isEqualToString:CLEventTypeUploading]) { // we need to upload this file.
                if (status.Equals(CLDefinitions.CLEventTypeUpload, StringComparison.InvariantCulture) ||
                    status.Equals(CLDefinitions.CLEventTypeUploading, StringComparison.InvariantCulture))
                {
                    // [uploadEvents addObject:event];
                    uploadEvents.Add(evt);
                }

                // if ([status isEqualToString:CLEventTypeExists] || [status isEqualToString:CLEventTypeDuplicate]) { // we do not need to upload this file.
                if (status.Equals(CLDefinitions.CLEventTypeExists, StringComparison.InvariantCulture) ||
                    status.Equals(CLDefinitions.CLEventTypeDuplicate, StringComparison.InvariantCulture))
                {
                    // Update ui.
                    // [self performUpdateForSyncEvent:event success:YES];
                    PerformUpdateForSyncEvent_Success(evt, success: true);
                }

                // if ([status isEqualToString:CLEventTypeConflict]) {
                if (status.Equals(CLDefinitions.CLEventTypeConflict, StringComparison.InvariantCulture))
                {
                    // TODO: handle conflict here.

                    // Update ui.
                    // [self performUpdateForSyncEvent:event success:YES];
                    PerformUpdateForSyncEvent_Success(evt, success: true);
                }
            }

            // Execute upload and download events.
            // if ([uploadEvents count] > 0) {
            //     [self dispatchUploadEvents:uploadEvents];
            // }
            if (uploadEvents.Count > 0)
            {
                DispatchUploadEvents(uploadEvents);
            }

            // if ([downloadEvents count] > 0) {
            if (downloadEvents.Count > 0)
            {
                // Sorting downloads by size (ascending)
                // NSArray *sortedDownloadEvents = [downloadEvents sortedArrayUsingComparator: ^(CLEvent * event1, CLEvent *event2) {

                //     if ([event1.metadata.size intValue] > [event2.metadata.size intValue]) {
                //         return (NSComparisonResult)NSOrderedDescending;
                //     }

                //     if ([event1.metadata.size intValue] < [event2.metadata.size intValue]) {
                //         return (NSComparisonResult)NSOrderedAscending;
                //     }
                //     return (NSComparisonResult)NSOrderedSame;
                // }];

                // [self dispatchDownloadEvents:sortedDownloadEvents];
                downloadEvents.Sort((CLEvent event1, CLEvent event2) =>
                {
                    if (Convert.ToInt64(event1.Metadata.Size) > Convert.ToInt64(event2.Metadata.Size))
                    {
                        return 1;
                    }

                    if (Convert.ToInt64(event1.Metadata.Size) < Convert.ToInt64(event2.Metadata.Size))
                    {
                        return -1;
                    }

                    return 0;
                });
                DispatchDownloadEvents(downloadEvents);
            }
        }

        //- (void)processRenameMoveFileSyncEvent:(CLEvent *)event
        void ProcessRenameMoveFileSyncEvent(CLEvent evt)
        {
            // Merged 7/10/12
            // NSString *actionType = event.syncHeader.action;
            // NSString *status = event.syncHeader.status;
            // NSString *toPath = event.metadata.toPath;
            // NSString *fromPath = event.metadata.fromPath;
                
            // if ([actionType rangeOfString:CLEventTypeRenameRange].location != NSNotFound ||
            //     [actionType rangeOfString:CLEventTypeMoveRange].location != NSNotFound ) {
        
            //     BOOL success = YES; // assume true for events we originated (since they already happened), override value for MDS execution.
        
            //     if (status == nil) { // MDS origin, Philis told us we need to do this.
            //         success = [[CLFSDispatcher defaultDispatcher] moveItemAtPath:fromPath to:toPath error:nil];
            //     }
        
            //     // update index and ui.
            //     [self performUpdateForSyncEvent:event success:success];
            // }
            //&&&&

            // NSString *actionType = event.syncHeader.action;
            // NSString *status = event.syncHeader.status;
            // NSString *toPath = event.metadata.toPath;
            // NSString *fromPath = event.metadata.fromPath;
            string actionType = evt.SyncHeader.Action;
            string status = evt.SyncHeader.Status;
            string toPath = evt.Metadata.ToPath;
            string fromPath = evt.Metadata.FromPath;

            // if ([actionType rangeOfString:CLEventTypeRenameRange].location != NSNotFound ||
            //      [actionType rangeOfString:CLEventTypeMoveRange].location != NSNotFound ) {
            if (actionType.Contains(CLDefinitions.CLEventTypeRenameRange) ||
                actionType.Contains(CLDefinitions.CLEventTypeMoveRange))
            {
                // BOOL success = YES;  // assume true for events we originated (since they already happened), override value for MDS execution. 
                bool success = true;  // assume true for events we originated (since they already happened), override value for MDS execution.

                // if (status == nil) { // MDS origin, Philis told us we need to do this.
                if (status == null)    // MDS origin, Philis told us we need to do this.
                {
                    // success = [[CLFSDispatcher defaultDispatcher] moveItemAtPath:fromPath to:toPath error:nil];
                    CLError error = null;
                    success = CLFSDispatcher.Instance.MoveItemAtPath_to_error(fromPath, toPath, out error);
                }

                // Update index and ui.
                // [self performUpdateForSyncEvent:event success:success];
                PerformUpdateForSyncEvent_Success(evt, success: success);
            }
        }

        //- (void)processAddLinkSyncEvent:(CLEvent *)event
        void ProcessAddLinkSyncEvent(CLEvent evt)
        {
            // Merged 7/10/12
            // if (event.isMDSEvent) {
        
            //     NSString *status = event.syncHeader.status;
        
            //     BOOL success = YES; // assume true for events we originated (since they already happened), override value for MDS execution.
            //     if (status == nil) { // MDS origin, Philis told us we need to do this.
            
            //         success = [[CLFSDispatcher defaultDispatcher] createSymbLinkAtPath:event.metadata.path withTarget:event.metadata.targetPath];
            //     }
        
            //     // update index and ui
            //     [self performUpdateForSyncEvent:event success:success];
            // }
            //&&&&

            // if (event.isMDSEvent) {
            if (evt.IsMDSEvent)
            {
                // NSString *status = event.syncHeader.status;
                string status = evt.SyncHeader.Status;

                // BOOL success = YES; // assume true for events we originated (since they already happened), override value for MDS execution.
                bool success = true;

                // if (status == nil) { // MDS origin, Philis told us we need to do this.
                if (status == null)
                {
                    // success = [[CLFSDispatcher defaultDispatcher] createSymbLinkAtPath:event.metadata.path withTarget:event.metadata.targetPath];
                    success = CLFSDispatcher.Instance.CreateSymbLinkAtPath_withTarget(evt.Metadata.Path, evt.Metadata.TargetPath);
                }

                // Update index and ui
                // [self performUpdateForSyncEvent:event success:success];
                PerformUpdateForSyncEvent_Success(evt, success: success);
            }
        }

        //- (void)processModifyLinkSyncEvent:(CLEvent *)event
        void ProcessModifyLinkSyncEvent(CLEvent evt)
        {
            // Merged 7/10/12
            // NSString *status = event.syncHeader.status;
        
            // BOOL success = YES; // assume true for events we originated (since they already happened), override value for MDS execution.
            // if (status == nil) { // MDS origin, Philis told us we need to do this.
        
            //     success = [[CLFSDispatcher defaultDispatcher] createSymbLinkAtPath:event.metadata.path withTarget:event.metadata.targetPath];
            // }
    
            // // update index and ui.
            // [self performUpdateForSyncEvent:event success:success];
            //&&&&

            // NSString *status = event.syncHeader.status;
            string status = evt.SyncHeader.Status;

            // BOOL success = YES; // assume true for events we originated (since they already happened), override value for MDS execution.
            bool success = true;   // assume true for events we originated (since they already happened), override value for MDS execution.

            // if (status == nil) { // MDS origin, Philis told us we need to do this.
            if (status == null)
            {
                // success = [[CLFSDispatcher defaultDispatcher] createSymbLinkAtPath:event.metadata.path withTarget:event.metadata.targetPath];
                success = CLFSDispatcher.Instance.CreateSymbLinkAtPath_withTarget(evt.Metadata.Path, evt.Metadata.TargetPath);
            }

            // Update index and ui.
            // [self performUpdateForSyncEvent:event success:success];
            PerformUpdateForSyncEvent_Success(evt, success: success);
        }

        //- (void)processRenameMoveLinkSyncEvent:(CLEvent *)event
        void ProcessRenameMoveLinkSyncEvent(CLEvent evt)
        {
            // Merged 7/10/12
            // NSString *actionType = event.syncHeader.action;
            // NSString *status = event.syncHeader.status;
            // NSString *toPath = event.metadata.toPath;
            // NSString *fromPath = event.metadata.fromPath;
            string actionType = evt.SyncHeader.Action;
            string status = evt.SyncHeader.Status;
            string toPath = evt.Metadata.ToPath;
            string fromPath = evt.Metadata.FromPath;

            // if ([actionType rangeOfString:CLEventTypeRenameRange].location != NSNotFound ||
            //     [actionType rangeOfString:CLEventTypeMoveRange].location != NSNotFound ) {
            if (actionType.Contains(CLDefinitions.CLEventTypeRenameRange) ||
                actionType.Contains(CLDefinitions.CLEventTypeMoveRange))
            {
                // BOOL success = YES; // assume true for events we originated (since they already happened), override value for MDS execution.
                bool success = true;   // assume true for events we originated (since they already happened), override value for MDS execution.
        
                // if (status == nil) { // MDS origin, Philis told us we need to do this.
                if (status == null)
                {
                    // success = [[CLFSDispatcher defaultDispatcher] moveItemAtPath:fromPath to:toPath error:nil];
                    CLError error = null;
                    success = CLFSDispatcher.Instance.MoveItemAtPath_to_error(fromPath, toPath, out error);
                }
        
                // Update index and ui.
                // [self performUpdateForSyncEvent:event success:success];
                PerformUpdateForSyncEvent_Success(evt, success: success);
            }
        }



        List<CLEvent> SeparateFolderFromFileForActiveEvents_WantsFolderEvents(List<CLEvent> activeEvents, bool wantsFolderEvents)
        {
            //NSMutableArray *events = [NSMutableArray array];
    
            //if (wantsFolderEvents == YES) {

            //    [activeEvents enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {

            //        CLEvent *event = obj;
            //        if ([event.syncHeader.action rangeOfString:CLEventTypeFolderRange].location != NSNotFound) {
            //            [events addObject:event];
            //        } 
            //    }];
            //}
            //else {
            //    [activeEvents enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
            
            //        CLEvent *event = obj;
            //        if ([event.syncHeader.action rangeOfString:CLEventTypeFileRange].location != NSNotFound) {
            //            [events addObject:event];
            //        }
            //    }];
            //}

            //return events;

            //&&&&
            // NSMutableArray *events = [NSMutableArray array];
            List<CLEvent> events = new List<CLEvent>();
    
            if (wantsFolderEvents)
            {

                // [activeEvents enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
                activeEvents.ForEach(obj =>
                {
                    // CLEvent *event = obj;
                    CLEvent evt = obj;

                    // if ([event.syncHeader.action rangeOfString:CLEventTypeFolderRange].location != NSNotFound) {
                    if (evt.SyncHeader.Action.Contains(CLDefinitions.CLEventTypeFolderRange))
                    {
                        // [events addObject:event];
                        events.Add(evt);
                    }
                });
            }
            else
            {
                // [activeEvents enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
                activeEvents.ForEach(obj =>
                {
                    // CLEvent *event = obj;
                    CLEvent evt = obj;

                    // if ([event.syncHeader.action rangeOfString:CLEventTypeFileRange].location != NSNotFound) {
                    if (evt.SyncHeader.Action.Contains(CLDefinitions.CLEventTypeFileRange))
                    {
                        // [events addObject:event];
                        events.Add(evt);
                    }
                });
            
            }

            // return events;
            return events;
        }

        //- (void)dispatchUploadEvents:(NSArray *)events
        void DispatchUploadEvents(List<CLEvent> events)
        {
            // Merged 7/10/12
            // NSLog(@"%s", __FUNCTION__);
            // NSMutableArray *operations = [NSMutableArray array];

            // NSLog(@"Number of uploads to start: %lu", [events count]);
    
            // [events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
        
            //     CLEvent *event = obj;
            //     CLHTTPConnectionOperation *uploadOperation = [self uploadOperationForEvent:event];
            //     [operations addObject:uploadOperation];
            // }];
    
            // NSLog(@"Starting Upload Operarions");
            // [self.uploadOperationQueue addOperations:operations waitUntilFinished:YES];
            // [self.uploadOperationQueue waitUntilAllOperationsAreFinished];

            // NSLog(@"Finished Upload Operarions");
            //&&&&

            // NSLog(@"%s", __FUNCTION__);
            _trace.writeToLog(9," CLSyncService: DispatchUploadEvents: Entry.");

            // NSMutableArray *operations = [NSMutableArray array];
            List<CLSptNSOperation> operations = new List<CLSptNSOperation>();

            // NSLog(@"Number of uploads to start: %lu", [events count]);
            _trace.writeToLog(9, " CLSyncService: DispatchUploadEvents: Number of uploads to start: {0}.", events.Count);

            // [events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
            foreach (CLEvent evt in events)
            {
                //     CLEvent *event = obj;
                //     CLHTTPConnectionOperation *uploadOperation = [self uploadOperationForEvent:event];
                CLHTTPConnectionOperation uploadOperation = UploadOperationForEvent(evt);

                //     [operations addObject:uploadOperation];
                operations.Add(uploadOperation);
            }

            // NSLog(@"Starting Upload Operarions");
            _trace.writeToLog(9, " CLSyncService: DispatchUploadEvents: Starting upload operations.");

            // [self.uploadOperationQueue addOperations:operations waitUntilFinished:YES];
            _uploadOperationQueue.AddOperations(operations);

            // [self.uploadOperationQueue waitUntilAllOperationsAreFinished];
            _uploadOperationQueue.WaitUntilFinished();

            // NSLog(@"Finished Upload Operarions");
            _trace.writeToLog(9, " CLSyncService: DispatchUploadEvents: Finished upload operations.");
        }

        //- (CLHTTPConnectionOperation *)uploadOperationForEvent:(CLEvent *)event
        CLHTTPConnectionOperation UploadOperationForEvent(CLEvent evt)
        {   
            // Merged 7/10/12
            // NSString *path = event.metadata.path;
            // NSString *storageKey = event.metadata.storage_key;
            // NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:path];
    
            // __block NSInteger totalExpectedUploadBytes = 0;
            // __block NSInteger totalUploadedBytes = 0;
            // __block NSTimeInterval start = [NSDate timeIntervalSinceReferenceDate];;
    
            // totalExpectedUploadBytes = totalExpectedUploadBytes +[event.metadata.size integerValue];
    
            // NSLog(@"File to be uploaded: %@, Storage Key: %@", path, storageKey);
    
            // __block CLHTTPConnectionOperation *uploadOperation = [self.restClient streamingUploadOperationForStorageKey:storageKey withFileSystemPath:fileSystemPath fileSize:event.metadata.size andMD5Hash:event.metadata.hash];
    
            // [uploadOperation setUploadProgressBlock:^(NSInteger bytesWritten, NSInteger totalBytesWritten, NSInteger totalBytesExpectedToWrite) {
        
            //     totalUploadedBytes = totalUploadedBytes + bytesWritten;
                
            //     NSTimeInterval now = [NSDate timeIntervalSinceReferenceDate];
            //     double elapsedSeconds = now - start;
            //     double secondsLeft = (((double)totalExpectedUploadBytes - (double)totalUploadedBytes) / ((double)totalUploadedBytes / elapsedSeconds));
            //     double progress = (double)totalUploadedBytes / (double)totalExpectedUploadBytes;
        
            //     //NSLog(@"Sent %ld of %ld bytes - Progress: %f", totalUploadedBytes, totalExpectedUploadBytes, progress);
        
            //     [[CLUIActivityService sharedService] updateActivityViewWithProgress:progress
            //                                                                timeLeft:secondsLeft
            //                                                                   bytes:(double)totalUploadedBytes
            //                                                            ofTotalBytes:(double)totalExpectedUploadBytes
            //                                                               fileCount:[self.uploadOperationQueue operationCount]
            //                                                         andActivityType:activityViewLabelUpload];
            // }];
    
            // __weak CLSyncService *weakSelf = self;
    
            // [uploadOperation setOperationCompletionBlock:^(CLHTTPConnectionOperation *operation, id responseObject, NSError *error) {
            //     __strong CLSyncService *strongSelf = weakSelf;
            //     if (strongSelf) {
            //         NSLog(@"Upload Status: %li", [operation.response statusCode]);
            //         //TODO : The MDS should be returning 201 for a successfull upload, but there is a bug returning 200. We need to change this back to 201 when this bug has been fixed.
            
            //         if (([operation.response statusCode] == 201 ) || ([operation.response statusCode] == 200 )) {
               
            //             NSLog(@"Upload Completed for File: %@", path);
            //             NSLog(@"Opperations remaining: %lu", [[strongSelf.uploadOperationQueue operations] count]);
            //            // update index and ui.
            //            [strongSelf performUpdateForSyncEvent:event success:YES];
                
            //         } else if ([operation.response statusCode] == 304){
                
            //             NSLog(@"The file already exists on the server");
            //             // update index and ui.
            //             [strongSelf performUpdateForSyncEvent:event success:YES];
                
            //         }else {
            //             NSLog(@"Upload Failed with status:%li for File: %@",[operation.response statusCode], path);
            //             [self retryEvent:event isDownloadEvent:NO];
            //         }
            
            //         if (error) {
                
            //             // Error handler (back processor). Likely to happen due to network interruptions.
            //             // TODO: Handle the upload failure -- for now update the index to not pending.. we need to handle the error!!
            //             NSLog(@"Failed to Upload File: %@. Error: %@, Code: %ld", path, [error localizedDescription], [error code]);
            //             NSLog(@"Opperations remaining: %lu", [[self.uploadOperationQueue operations] count]);
                
            //             [self retryEvent:event isDownloadEvent:NO];
            //         }
            
            //         if ([strongSelf.uploadOperationQueue operationCount] <= 0) {
                
            //             [[CLUIActivityService sharedService] updateActivityViewWithProgress:1 timeLeft:0 bytes:0 ofTotalBytes:0 fileCount:0 andActivityType:activityViewLabelSynced];
            //         }
            //     }
            // }];
    
            // return uploadOperation;
            //&&&&

            // NSString *path = event.metadata.path;
            // NSString *storageKey = event.metadata.storage_key;
            // NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:path];
            string path = evt.Metadata.Path;
            string storageKey = evt.Metadata.Storage_key;
            string fileSystemPath = Settings.Instance.CloudFolderPath + path;

            // __block NSInteger totalExpectedUploadBytes = 0;
            // __block NSInteger totalUploadedBytes = 0;
            // __block NSTimeInterval start = [NSDate timeIntervalSinceReferenceDate];;
            long totalExpectedUploadBytes = 0;
            long totalUploadedBytes = 0;
            DateTime start = DateTime.Now;

            // totalExpectedUploadBytes = totalExpectedUploadBytes +[event.metadata.size integerValue];
            totalExpectedUploadBytes += Convert.ToInt64(evt.Metadata.Size);

            // NSLog(@"File to be uploaded: %@, Storage Key: %@", path, storageKey);
            _trace.writeToLog(9, "CLSyncService: UploadOperationForEvent: FIle to be uploaded: {0}, Storage key: {1}.", path, storageKey);

            // __block CLHTTPConnectionOperation *uploadOperation = [self.restClient streamingUploadOperationForStorageKey:storageKey withFileSystemPath:fileSystemPath fileSize:event.metadata.size andMD5Hash:event.metadata.hash];
            CLHTTPConnectionOperation uploadOperation = _restClient.StreamingUploadOperationForStorageKey_WithFileSystemPath_FileSize_AndMd5Hash(storageKey, fileSystemPath, evt.Metadata.Size, evt.Metadata.Hash);

            //TODO: Implement this UI functionality.
            // [uploadOperation setUploadProgressBlock:^(NSInteger bytesWritten, NSInteger totalBytesWritten, NSInteger totalBytesExpectedToWrite) {

            //     totalUploadedBytes = totalUploadedBytes + bytesWritten;

            //     NSTimeInterval now = [NSDate timeIntervalSinceReferenceDate];
            //     double elapsedSeconds = now - start;
            //     double secondsLeft = (((double)totalExpectedUploadBytes - (double)totalUploadedBytes) / ((double)totalUploadedBytes / elapsedSeconds));
            //     double progress = (double)totalUploadedBytes / (double)totalExpectedUploadBytes;

            //     //NSLog(@"Sent %ld of %ld bytes - Progress: %f", totalUploadedBytes, totalExpectedUploadBytes, progress);

            //     [[CLUIActivityService sharedService] updateActivityViewWithProgress:progress
            //                                                                timeLeft:secondsLeft
            //                                                                   bytes:(double)totalUploadedBytes
            //                                                            ofTotalBytes:(double)totalExpectedUploadBytes
            //                                                               fileCount:[self.uploadOperationQueue operationCount]
            //                                                         andActivityType:activityViewLabelUpload];
            // }];

            // __weak CLSyncService *weakSelf = self;

            // [uploadOperation setOperationCompletionBlock:^(CLHTTPConnectionOperation *operation, id responseObject, NSError *error) {
            uploadOperation.SetOperationCompletionBlock((CLHTTPConnectionOperation operation, CLError error) =>
            {
                // __strong CLSyncService *strongSelf = weakSelf;
                // if (strongSelf) {

                    // NSLog(@"Upload Status: %li", [operation.response statusCode]);
                    _trace.writeToLog(9, " CLSyncService: DispatchUploadEvents: Upload Status: {0}.", operation.Response.StatusCode);

                    // if (([operation.response statusCode] == 201 ) || ([operation.response statusCode] == 200 )) {
                    // //TODO : The MDS should be returning 201 for a successfull upload, but there is a bug returning 200. We need to change this back to 201 when this bug has been fixed.
                    if (operation.Response.StatusCode == HttpStatusCode.Created || operation.Response.StatusCode == HttpStatusCode.OK)  // 201 or 200
                    {
                        // NSLog(@"Upload Completed for File: %@", path);
                        // NSLog(@"Opperations remaining: %lu", [[self.uploadOperationQueue operations] count]);
                        _trace.writeToLog(9, " CLSyncService: DispatchUploadEvents: Upload completed for file: {0}.", path);
                        _trace.writeToLog(9, " CLSyncService: DispatchUploadEvents: Operations remaining: {0}.", _uploadOperationQueue.OperationCount);

                        // update index and ui.
                        // [self performUpdateForSyncEvent:event success:YES];
                        PerformUpdateForSyncEvent_Success(evt, success: true);
                    }
                    // } else if ([operation.response statusCode] == 304){
                    else if (operation.Response.StatusCode == HttpStatusCode.NotModified)  // 304
                    {
                        // NSLog(@"The file already exists on the server");
                        _trace.writeToLog(9, " CLSyncService: DispatchUploadEvents: The file already exists on the server: {0}.", path);

                        // update index and ui.
                        // [self performUpdateForSyncEvent:event success:YES];
                        PerformUpdateForSyncEvent_Success(evt, success: true);

                        // [self retryEvent:event isDownloadEvent:NO];
                        RetryEvent_isDownload(evt, isDownload: false);
                    }
                    // }else {
                    else
                    {
                        // NSLog(@"Upload Failed with status:%li for File: %@",[operation.response statusCode], path);
                        _trace.writeToLog(9, " CLSyncService: DispatchUploadEvents: Upload Failed with status: {0} for File: {1}.", operation.Response.StatusCode, path);
                    }

                    // Handle a potential error
                    if (error != null)
                    {

                        // Error handler (back processor). Likely to happen due to network interruptions.
                        // TODO: Handle the upload failure -- for now update the index to not pending.. we need to handle the error!!
                        // NSLog(@"Failed to Upload File: %@. Error: %@, Code: %ld", path, [error localizedDescription], [error code]);
                        // NSLog(@"Opperations remaining: %lu", [[self.uploadOperationQueue operations] count]);
                        _trace.writeToLog(9, " CLSyncService: DispatchUploadEvents: Failed to Upload File: {0}. Error: {1}, Code: {2}.", path, error.errorDescription, error.errorCode);
                        _trace.writeToLog(9, " CLSyncService: DispatchUploadEvents: Operations remaining: {0}.", _uploadOperationQueue.OperationCount);

                        // Update index and ui.
                        // [self performUpdateForSyncEvent:event success:NO];
                        PerformUpdateForSyncEvent_Success(evt, success: false);
                    }

                    // Update the UI
                    // if ([self.uploadOperationQueue operationCount] <= 0) {
                    if (_uploadOperationQueue.OperationCount <= 0)
                    {
                        //TODO: Implement this UI status.
                        // [[CLUIActivityService sharedService] updateActivityViewWithProgress:1 timeLeft:0 bytes:0 ofTotalBytes:0 fileCount:0 andActivityType:activityViewLabelSynced];
                    }
                // }  // strongSelf
            });

            // return uploadOperation;
            return uploadOperation;
        }

        //- (void)dispatchDownloadEvents:(NSArray *)events
        void DispatchDownloadEvents(List<CLEvent> events)
        {
            // Merged 7/10/12
            // NSLog(@"%s", __FUNCTION__);
    
            // NSMutableArray *operations = [NSMutableArray array];

            // [events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
        
            //     __block CLEvent *event = obj;
            //     CLHTTPConnectionOperation *downloadOperation = [self  downloadOpperationForEvent:event];
       
            //     [operations addObject:downloadOperation];
        
            //     [self.activeDownloadQueue addObject:event];
            // }];
    
            // NSLog(@"Starting Download Operations");
            // [self.downloadOperationQueue addOperations:operations waitUntilFinished:YES];
            // [self.downloadOperationQueue waitUntilAllOperationsAreFinished];

            // NSLog(@"Finished Download Operations");
            //&&&&

            // NSLog(@"%s", __FUNCTION__);
            _trace.writeToLog(9, "CLSyncService: DispatchDownloadEvents: Entry.");

            // NSMutableArray *operations = [NSMutableArray array];
            List<CLSptNSOperation> operations = new List<CLSptNSOperation>();

            // [events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
            foreach (CLEvent evt in events)
            {
                // __block CLEvent *event = obj;
                // CLHTTPConnectionOperation *downloadOperation = [self  downloadOpperationForEvent:event];
                CLHTTPConnectionOperation downloadOperation = DownloadOperationForEvent(evt);

                // [operations addObject:downloadOperation];
                operations.Add(downloadOperation);

                // [self.activeDownloadQueue addObject:event];
                _activeDownloadQueue.Add(evt);
            }

            // NSLog(@"Starting Download Operations");
            _trace.writeToLog(9, " CLSyncService: DispatchUploadEvents: Starting download operations.");

            // [self.downloadOperationQueue addOperations:operations waitUntilFinished:YES];
            _downloadOperationQueue.AddOperations(operations);

            // [self.downloadOperationQueue waitUntilAllOperationsAreFinished];
            _downloadOperationQueue.WaitUntilFinished();

            // NSLog(@"Finished Download Operations");
            _trace.writeToLog(9, " CLSyncService: DispatchUploadEvents: Finished download operations.");
        }


        //- (CLHTTPConnectionOperation *)downloadOpperationForEvent:(CLEvent *)event
        CLHTTPConnectionOperation DownloadOperationForEvent(CLEvent evt)
        {
            // Merged 7/10/12
            // NSString *path = event.metadata.path;
            // NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:path];
    
            // __block NSInteger totalExpectedDownloadBytes = 0;
            // __block NSInteger totalDownloadedBytes = 0;
            // __block NSTimeInterval start;
    
            // __block CLHTTPConnectionOperation *downloadOperation = [self.restClient streamingDownloadOperationForStorageKey:event.metadata.storage_key
            //                                                                                                 withFileSystemPath:fileSystemPath
            //                                                                                                           fileSize:event.metadata.size
            //                                                                                                         andMD5Hash:event.metadata.hash];
    
            // [downloadOperation setDownloadProgressBlock:^(NSInteger bytesRead, NSInteger totalBytesRead, NSInteger totalBytesExpectedToRead) {
        
            //     totalDownloadedBytes = totalDownloadedBytes + bytesRead;
        
            //     NSTimeInterval now = [NSDate timeIntervalSinceReferenceDate];
            //     double progress = (double)totalDownloadedBytes / (double)totalExpectedDownloadBytes;
            //     double elapsedSeconds = now - start;
            //     double secondsLeft = (((double)totalExpectedDownloadBytes - (double)totalDownloadedBytes) / ((double)totalDownloadedBytes / elapsedSeconds));
            //     [[CLUIActivityService sharedService] updateActivityViewWithProgress:progress
            //                                                                timeLeft:secondsLeft
            //                                                                   bytes:(double)totalDownloadedBytes
            //                                                            ofTotalBytes:(double)totalExpectedDownloadBytes
            //                                                               fileCount:[self.downloadOperationQueue operationCount]
            //                                                         andActivityType:activityViewLabelDownload];
            // }];
    
            // __weak CLSyncService *weakSelf = self;
    
            // [downloadOperation setOperationCompletionBlock:^(CLHTTPConnectionOperation *operation, id responseObject, NSError *error) {
        
            //     __strong CLSyncService *strongSelf = weakSelf;
        
            //     if (!error) {
            //         if ([operation.response statusCode] == 200) {
                
            //             NSLog(@"Download Completed for file: %@", path);
            //             NSLog(@"Opperations remaining: %lu", [[self.downloadOperationQueue operations] count]);
                
            //             NSError *attributesError;
            //             BOOL attributesSet = [[CLFSDispatcher defaultDispatcher] updateAttributesUsingMetadata:event.metadata forItemAtPath:fileSystemPath error:&attributesError];
            //             if (attributesSet) {
            //                 if (attributesError) {
            //                     NSLog(@"%s - %@", __FUNCTION__, [attributesError description]);
            //                 }
            //             }else {
            //                 NSLog(@"Failed to update attributes in: %s", __FUNCTION__);
            //             }
                
            //             [strongSelf performUpdateForSyncEvent:event success:YES];
               
            //         } else {
            //             NSLog(@"%s - Download returned code: %ld", __FUNCTION__, [operation.response statusCode]);
            //             [strongSelf retryEvent:event isDownloadEvent:YES];
            //         }
            
            //     }else {
            
            //         [strongSelf retryEvent:event isDownloadEvent:YES];
            //         NSLog(@"Failed to Download File: %@. Error: %@, Code: %ld", path, [error localizedDescription], [error code]);
            //     }
        
            //     // TODO: ? check to see if object is there?
            //     [strongSelf.activeDownloadQueue removeObject:event];
        
            //     if ([strongSelf.downloadOperationQueue operationCount] <= 0) {
            //         [[CLUIActivityService sharedService] updateActivityViewWithProgress:1 timeLeft:0 bytes:0 ofTotalBytes:0 fileCount:0 andActivityType:activityViewLabelSynced];
            //     }
        
            //     responseObject = nil;
        
            // }];
    
            // return downloadOperation;
            //&&&&

            // NSString *path = event.metadata.path;
            // NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:path];
            string path = evt.Metadata.Path;
            string fileSystemPath = Settings.Instance.CloudFolderPath + path;

            // __block NSInteger totalExpectedDownloadBytes = 0;
            // __block NSInteger totalDownloadedBytes = 0;
            // __block NSTimeInterval start;
            long totalExpectedDownloadBytes = 0;
            long totalDownloadedBytes = 0;
            DateTime start = DateTime.Now;

            // __block CLHTTPConnectionOperation *downloadOperation = [self.restClient streamingDownloadOperationForStorageKey:event.metadata.storage_key
            //                                                                                                 withFileSystemPath:fileSystemPath
            //                                                                                                           fileSize:event.metadata.size
            //                                                                                                         andMD5Hash:event.metadata.hash];
            CLHTTPConnectionOperation downloadOperation = _restClient.StreamingDownloadOperationForStorageKey_WithFileSystemPath_FileSize_AndMd5Hash(evt.Metadata.Storage_key, fileSystemPath, evt.Metadata.Size, evt.Metadata.Hash);

            //TODO: Implement this UI progress code.
            // [downloadOperation setDownloadProgressBlock:^(NSInteger bytesRead, NSInteger totalBytesRead, NSInteger totalBytesExpectedToRead) {

            //     totalDownloadedBytes = totalDownloadedBytes + bytesRead;

            //     NSTimeInterval now = [NSDate timeIntervalSinceReferenceDate];
            //     double progress = (double)totalDownloadedBytes / (double)totalExpectedDownloadBytes;
            //     double elapsedSeconds = now - start;
            //     double secondsLeft = (((double)totalExpectedDownloadBytes - (double)totalDownloadedBytes) / ((double)totalDownloadedBytes / elapsedSeconds));
            //     [[CLUIActivityService sharedService] updateActivityViewWithProgress:progress
            //                                                                timeLeft:secondsLeft
            //                                                                   bytes:(double)totalDownloadedBytes
            //                                                            ofTotalBytes:(double)totalExpectedDownloadBytes
            //                                                               fileCount:[self.downloadOperationQueue operationCount]
            //                                                         andActivityType:activityViewLabelDownload];
            // }];

            // __weak CLSyncService *weakSelf = self;

            // [downloadOperation setOperationCompletionBlock:^(CLHTTPConnectionOperation *operation, id responseObject, NSError *error) {
            downloadOperation.SetOperationCompletionBlock((CLHTTPConnectionOperation operation, CLError error) =>
            {

                // __strong CLSyncService *strongSelf = weakSelf;

                // if (!error) {
                if (error != null)
                {
                    // if ([operation.response statusCode] == 200) {
                    if (operation.Response.StatusCode == HttpStatusCode.OK)
                    {

                        // NSLog(@"Download Completed for file: %@", path);
                        // NSLog(@"Opperations remaining: %lu", [[self.downloadOperationQueue operations] count]);
                        _trace.writeToLog(9, " CLSyncService: DispatchDownloadEvents: Download completed for file: {0}.", path);
                        _trace.writeToLog(9, " CLSyncService: DispatchDownloadEvents: Operations remaining: {0}.", _downloadOperationQueue.OperationCount);

                        // NSError *attributesError;
                        CLError attributesError = null;

                        // BOOL attributesSet = [[CLFSDispatcher defaultDispatcher] updateAttributesUsingMetadata:event.metadata forItemAtPath:fileSystemPath error:&attributesError];
                        bool attributesSet = CLFSDispatcher.Instance.UpdateAttributesUsingMetadata_forItemAtPath_error(evt.Metadata, fileSystemPath, out attributesError);

                        // if (attributesSet) {
                        if (attributesSet)
                        {
                            // if (attributesError) {
                            //     NSLog(@"%s - %@", __FUNCTION__, [attributesError description]);
                            // }
                            if (attributesError != null)
                            {
                                _trace.writeToLog(1, " CLSyncService: DispatchDownloadEvents: ERROR: {0}.", attributesError.errorDescription);
                            }
                        }
                        else
                        {
                            // NSLog(@"Failed to update attributes in: %s", __FUNCTION__);
                            _trace.writeToLog(1, " CLSyncService: DispatchDownloadEvents: ERROR: Failed to update attributes for file {0}.", path);
                        }

                        // [self performUpdateForSyncEvent:event success:YES];
                        PerformUpdateForSyncEvent_Success(evt, success: true);

                    }
                    else
                    {
                        // NSLog(@"%s - Download returned code: %ld", __FUNCTION__, [operation.response statusCode]);
                        _trace.writeToLog(1, " CLSyncService: DispatchDownloadEvents: ERROR: Download returned code {0}.", operation.Response.StatusCode.ToString());

                        // [strongSelf retryEvent:event isDownloadEvent:YES];
                        RetryEvent_isDownload(evt, isDownload: true);
                    }

                }
                else
                {
                    // [strongSelf retryEvent:event isDownloadEvent:YES];
                    RetryEvent_isDownload(evt, isDownload: true);

                    // NSLog(@"Failed to Download File: %@. Error: %@, Code: %ld", path, [error localizedDescription], [error code]);
                    _trace.writeToLog(1, " CLSyncService: DispatchDownloadEvents: ERROR: Failed to download file {0}.  Error: {1}, Code: {2}.", path, error.errorDescription, error.errorCode);
                }

                // // TODO: ? check to see if object is there?
                // [strongSelf.activeDownloadQueue removeObject:event];
                _activeDownloadQueue.Remove(evt);

                // if ([strongSelf.downloadOperationQueue operationCount] <= 0) {
                if (_downloadOperationQueue.OperationCount <= 0)
                {
                    //TODO: Implement this UI.
                    // [[CLUIActivityService sharedService] updateActivityViewWithProgress:1 timeLeft:0 bytes:0 ofTotalBytes:0 fileCount:0 andActivityType:activityViewLabelSynced];
                }

                // responseObject = nil;

            });

            // return downloadOperation;
            return downloadOperation;
        }

        // - (void)saveSyncStateWithSID:(NSString *)sid andEID:(NSNumber *)eid
        void SaveSyncStateWithSIDAndEID(string sid, ulong eid)
        {
            // Merged 7/4/12
            // NSLog(@"%s", __FUNCTION__);
            // if ([self.currentSIDs containsObject:sid]) {
            //     NSLog(@"Current SID Stack contains the sid we are saving: %@", sid);
        
            //     NSLog(@" Save Global Sid: %@ Current number of active SIDs: %lu" , sid ,[self.currentSIDs count]);
            //     [self.currentSIDs removeObject:sid];
            //     NSLog(@" Save Global Sid: %@ Current number of active SIDs: %lu" , sid ,[self.currentSIDs count]);
            // }
    
            // if ([sid isEqualToString:[[NSNumber numberWithInteger:CLDotNotSaveId] stringValue]] == NO) { // only save for SyncFrom Events
            //     if (sid != nil) {
            //         [[CLSettings sharedSettings] recordSID:sid];
            //     }
            // }
    
            // if ([eid integerValue] != [[NSNumber numberWithInteger:CLDotNotSaveId] integerValue]) { // only save for SyncTo Events
            //     if (eid != nil) {
            //         [[CLSettings sharedSettings] recordEventId:eid];
            //     }
            // }
            //&&&&

            // NSLog(@"%s", __FUNCTION__);
            _trace.writeToLog(9, "CLSyncService: SaveSyncStateWithSIDAndEID: sid: {0}, eid: {1}.", sid, eid);

            // if ([self.currentSIDs containsObject:sid]) {
            if (_currentSids.Contains(sid))
            {
                // NSLog(@"Current SID Stack contains the sid we are saving: %@", sid);
                _trace.writeToLog(9, "CLSyncService: SaveSyncStateWithSIDAndEID: Current SID stack contains the sid we are saving: {0}.", sid);

                // NSLog(@" Save Global Sid: %@ Current number of active SIDs: %lu" , sid ,[self.currentSIDs count]);
                _trace.writeToLog(9, "CLSyncService: SaveSyncStateWithSIDAndEID: Save global SID: {0}. Current number of active SIDs: {1}.", sid, _currentSids.Count);

                // [self.currentSIDs removeObject:sid];
                _currentSids.Remove(sid);

                // NSLog(@" Save Global Sid: %@ Current number of active SIDs: %lu" , sid ,[self.currentSIDs count]);
                _trace.writeToLog(9, "CLSyncService: SaveSyncStateWithSIDAndEID: Current number of active SIDs: {1}.", sid, _currentSids.Count);
            }

            // if ([sid isEqualToString:[[NSNumber numberWithInteger:CLDotNotSaveId] stringValue]] == NO) { // only save for SyncFrom Events
            if (sid.Equals(CLConstants.CLDoNotSaveId.ToString(), StringComparison.InvariantCulture))
            {
                // if (sid != nil) {
                if (!string.IsNullOrWhiteSpace(sid))
                {
                    // [[CLSettings sharedSettings] recordSID:sid];
                    Settings.Instance.recordSID(sid);
                }
            }

            // if ([eid integerValue] != [[NSNumber numberWithInteger:CLDotNotSaveId] integerValue]) { // only save for SyncTo Events
            if (eid != CLConstants.CLDoNotSaveId)
            {
                // if (eid != nil) {
                if (eid != 0)
                {
                    // [[CLSettings sharedSettings] recordEventId:eid];
                    Settings.Instance.RecordEventId(eid);
                }
            }

            // Added call to indexer to mark completed event in database
            // -David
            CLFSMonitoringService.Instance.IndexingAgent.MarkEventAsCompletedOnPreviousSync((int)eid);
        }

        //- (void)performUpdateForSyncEvent:(CLEvent *)event success:(BOOL)success
        void PerformUpdateForSyncEvent_Success(CLEvent evt, bool success)
        {
            //cloudAppIconBadgeType badgeType = cloudAppBadgeSynced;
            //NSString *eventType = event.syncHeader.action;

            //if (success) {

            //    [self updateIndexForSyncEvent:event];

            //    // Add file event to our recent files array
            //    if ([eventType rangeOfString:CLEventTypeFileRange].location != NSNotFound) { // only file events
            //        if (self.syncItemsQueueCount <= 8 && self.syncItemsQueueCount > 0) {
            //            if ([eventType rangeOfString:CLEventTypeMoveRange].location != NSNotFound ||
            //                [eventType rangeOfString:CLEventTypeRenameRange].location != NSNotFound) {
            //                [self.recentItems addObject:event.metadata.toPath];
            //            } else {
            //                [self.recentItems addObject:event.metadata.path];
            //            }
            //        }
            //    }
            //}
            //else {
            //    // Update badging for file.
            //    badgeType = cloudAppBadgeFailed;
            //    // todo: Error handler for failed event (back processor).
            //}

            //// decrease queued objects count
            //self.syncItemsQueueCount = self.syncItemsQueueCount - 1;

            //// Update activity indicator UI
            //menuItemActivityLabelType messageType = menuItemActivityLabelSyncing;
            //BOOL animate = YES;
            //if (self.syncItemsQueueCount <= 0) {
            //    messageType = menuItemActivityLabelSynced;
            //    animate = NO;
            //}
            //[self animateUIForSync:animate withStatusMessage:messageType syncActivityCount:self.syncItemsQueueCount];


            //// Determine cloudPath for item to be badged.
            //NSString *cloudPath = event.metadata.path;
            //if (event.metadata.toPath != nil) {
            //    cloudPath = event.metadata.toPath;
            //}

            //if ([eventType rangeOfString:CLEventTypeFileRange].location != NSNotFound) { // only automatically badge file events

            //    // go ahead and badge the file
            //    [self badgeFileAtCloudPath:cloudPath withBadge:badgeType];

            //    // now remove it from the active file queue
            //    if ([self.activeSyncFileQueue containsObject:event]){
            //        [self.activeSyncFileQueue removeObject:event];
            //    }

            //    // am I the last file to be synced in this folder?
            //    NSString *folderPath = [cloudPath stringByDeletingLastPathComponent];

            //    // is folder path root?
            //    if ([folderPath isEqualToString:@""] == NO) {

            //        __block BOOL shouldBadgeFolder = YES;
            //        [[self.activeSyncFileQueue copy] enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {

            //            CLEvent *fileEvent = obj;
            //            NSString *folderPathForFileEvent = [fileEvent.metadata.path stringByDeletingLastPathComponent];
            //            if (fileEvent.metadata.toPath != nil) {
            //                folderPathForFileEvent = [fileEvent.metadata.toPath stringByDeletingLastPathComponent];
            //            }

            //            // if we find the folder path inside the file events, it means this folder still has items to be synced
            //            // so we shouldn't mark it as synced.
            //            if ([folderPath isEqualToString:folderPathForFileEvent]) {
            //                shouldBadgeFolder = NO;
            //                *stop = YES; // we found something, no need to go continue.
            //            }
            //        }];

            //        if (shouldBadgeFolder == YES) {

            //            // badge folder then remove it from the folder list.
            //            [self badgeFileAtCloudPath:folderPath withBadge:badgeType];

            //            [[self.activeSyncFolderQueue copy] enumerateObjectsWithOptions:NSEnumerationReverse usingBlock:^(id obj, NSUInteger idx, BOOL *stop) {

            //                CLEvent *folderEvent = obj;
            //                NSString *folderPathForFolderEvent = folderEvent.metadata.path;
            //                if (folderEvent.metadata.toPath != nil) {
            //                    folderPathForFolderEvent = folderEvent.metadata.toPath;
            //                }

            //                if ([folderPath isEqualToString:folderPathForFolderEvent]) {
            //                    [self.activeSyncFolderQueue removeObject:folderEvent];
            //                }

            //                // subfolders of this folder should be marked as sync as well
            //                if ([folderPathForFolderEvent rangeOfString:folderPath].location != NSNotFound) {
            //                    [self badgeFileAtCloudPath:folderPathForFolderEvent withBadge:badgeType];
            //                }
            //            }];
            //        }
            //    }
            //}

            //if ([eventType rangeOfString:CLEventTypeFolderRange].location != NSNotFound) { // empty folders should automatically be badged.

            //    __block BOOL shouldBadgeFolder = YES;
            //    [self.activeSyncQueue enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {

            //        CLEvent *activeEvent = obj;
            //        NSString *eventPath = activeEvent.metadata.path;
            //        if (activeEvent.metadata.toPath != nil) {
            //            eventPath = activeEvent.metadata.toPath;
            //        }

            //        // if the path is not exactly our current path, but it has range of the current path, it means the folder is not going to be empty.
            //        if ([cloudPath isEqualToString:eventPath] == NO && [eventPath rangeOfString:cloudPath].location != NSNotFound) {
            //            shouldBadgeFolder = NO;
            //            *stop = YES;
            //        }
            //    }];

            //    if (shouldBadgeFolder == YES) { //badge empty folder and remove it from active folder queue
            //        [self badgeFileAtCloudPath:cloudPath withBadge:badgeType];
            //        if ([self.activeSyncFolderQueue containsObject:event]) {
            //            [self.activeSyncFolderQueue removeObject:event];
            //        }
            //    }
            //}

            //// Lastly record recently changed files and figure out if we have any folders left to badge
            //if (self.syncItemsQueueCount <= 0) {

            //    [[CLSettings sharedSettings] recordRecentItems:[self.recentItems copy]];
            //    [self.recentItems removeAllObjects];

            //    // badge the remaining parent folders when the last file is done syncing.
            //    if ([[cloudPath pathComponents] count] >= 2) {
            //        [[cloudPath pathComponents] enumerateObjectsUsingBlock:^(NSString *pathItem, NSUInteger idx, BOOL *stop) {

            //            if (idx > 1) {
            //                NSString *path = [cloudPath substringToIndex:[cloudPath rangeOfString:pathItem].location];                    
            //                [self badgeFileAtCloudPath:path withBadge:badgeType];
            //            }
            //        }];
            //    }
            //}
            //&&&&

            //cloudAppIconBadgeType badgeType = cloudAppBadgeSynced;
            cloudAppIconBadgeType badgeType = cloudAppIconBadgeType.cloudAppBadgeSynced;

            //NSString *eventType = event.syncHeader.action;
            string eventType = evt.SyncHeader.Action;

            //if (success) {
            if (success)
            {
                // [self updateIndexForSyncEvent:event];
                UpdateIndexForSyncEvent(evt);

                // Add file event to our recent files array
                // if ([eventType rangeOfString:CLEventTypeFileRange].location != NSNotFound) { // only file events
                if (eventType.Contains(CLDefinitions.CLEventTypeFileRange))  // only file events
                {
                    // if (self.syncItemsQueueCount <= 8 && self.syncItemsQueueCount > 0) {
                    if (_syncItemsQueueCount <= 8 && _syncItemsQueueCount > 0)
                    {
                        // if ([eventType rangeOfString:CLEventTypeMoveRange].location != NSNotFound ||
                        //      [eventType rangeOfString:CLEventTypeRenameRange].location != NSNotFound) {
                        if (eventType.Contains(CLDefinitions.CLEventTypeMoveRange) ||
                            eventType.Contains(CLDefinitions.CLEventTypeRenameRange))
                        {
                            // [self.recentItems addObject:event.metadata.toPath];
                            _recentItems.Add(evt.Metadata.ToPath);
                        }
                        else
                        {
                            // [self.recentItems addObject:event.metadata.path];
                            _recentItems.Add(evt.Metadata.Path);
                        }
                    }
                }
            }
            else
            {
                // Update badging for file.
                // badgeType = cloudAppBadgeFailed;
                badgeType = cloudAppIconBadgeType.cloudAppBadgeFailed;

                // // todo: Error handler for failed event (back processor).
            }

            // Decrease queued objects count
            //self.syncItemsQueueCount = self.syncItemsQueueCount - 1;
            _syncItemsQueueCount -= 1;

            // Update activity indicator UI
            //menuItemActivityLabelType messageType = menuItemActivityLabelSyncing;
            menuItemActivityLabelType messageType = menuItemActivityLabelType.menuItemActivityLabelSyncing;

            //BOOL animate = YES;
            bool animate = true;
            //if (self.syncItemsQueueCount <= 0) {
            if (_syncItemsQueueCount <= 0)
            {
                // messageType = menuItemActivityLabelSynced;
                // animate = NO;
                messageType = menuItemActivityLabelType.menuItemActivityLabelSynced;
                animate = false;
            }

            //TODO: Implement UI.
            //[self animateUIForSync:animate withStatusMessage:messageType syncActivityCount:self.syncItemsQueueCount];

            // Determine cloudPath for item to be badged.
            //NSString *cloudPath = event.metadata.path;
            string cloudPath = evt.Metadata.Path;

            //if (event.metadata.toPath != nil) {
            if (!string.IsNullOrWhiteSpace(evt.Metadata.ToPath))
            {
                // cloudPath = event.metadata.toPath;
                cloudPath = evt.Metadata.ToPath;
            }

            //if ([eventType rangeOfString:CLEventTypeFileRange].location != NSNotFound) { // only automatically badge file events)
            if (eventType.Contains(CLDefinitions.CLEventTypeFileRange))
            {

                // Go ahead and badge the file
                //[self badgeFileAtCloudPath:cloudPath withBadge:badgeType];
                BadgeFileAtCloudPath_withBadge(cloudPath, badgeType);

                // Now remove it from the active file queue
                //if ([self.activeSyncFileQueue containsObject:event]){
                if (_activeSyncFileQueue.Contains(evt))
                {
                    // [self.activeSyncFileQueue removeObject:event];
                    _activeSyncFileQueue.Remove(evt);
                }

                // Am I the last file to be synced in this folder?
                //NSString *folderPath = [cloudPath stringByDeletingLastPathComponent];
                string folderPath = cloudPath.StringByDeletingLastPathComponent();

                // Is folder path root?
                //if ([folderPath isEqualToString:@""] == NO) {
                // NOTE: If folderPath is "", that is the condition for NO path, not the "path root"???
                // TODO: Check this
                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    // __block BOOL shouldBadgeFolder = YES;
                    bool shouldBadgeFolder = true;

                    // [[self.activeSyncFileQueue copy] enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
                    foreach (CLEvent fileEvent in _activeSyncFileQueue)
                    {
                        // CLEvent *fileEvent = obj;
                        // NSString *folderPathForFileEvent = [fileEvent.metadata.path stringByDeletingLastPathComponent];
                        string folderPathForFileEvent = fileEvent.Metadata.Path.StringByDeletingLastPathComponent();

                        // if (fileEvent.metadata.toPath != nil) {
                        if (!string.IsNullOrWhiteSpace(fileEvent.Metadata.ToPath))
                        {
                            // folderPathForFileEvent = [fileEvent.metadata.toPath stringByDeletingLastPathComponent];
                            folderPathForFileEvent = fileEvent.Metadata.ToPath.StringByDeletingLastPathComponent();
                        }

                        // If we find the folder path inside the file events, it means this folder still has items to be synced
                        // so we shouldn't mark it as synced.
                        // if ([folderPath isEqualToString:folderPathForFileEvent]) {
                        if (folderPath.Equals(folderPathForFileEvent, StringComparison.InvariantCulture))
                        {
                            // shouldBadgeFolder = NO;
                            // *stop = YES; // we found something, no need to go continue.
                            shouldBadgeFolder = false;
                            break;
                        }
                    }

                    // if (shouldBadgeFolder == YES) {
                    if (shouldBadgeFolder)
                    {

                        // Badge folder then remove it from the folder list.
                        // [self badgeFileAtCloudPath:folderPath withBadge:badgeType];
                        BadgeFileAtCloudPath_withBadge(folderPath, badgeType);

                        // [[self.activeSyncFolderQueue copy] enumerateObjectsWithOptions:NSEnumerationReverse usingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
                        List<CLEvent> activeSyncFolderQueueReverseCopy = new List<CLEvent>(_activeSyncFolderQueue);
                        activeSyncFolderQueueReverseCopy.Reverse();
                        foreach (CLEvent folderEvent in activeSyncFolderQueueReverseCopy)
                        {
                            // CLEvent *folderEvent = obj;
                            // NSString *folderPathForFolderEvent = folderEvent.metadata.path;
                            string folderPathForFolderEvent = folderEvent.Metadata.Path;

                            // if (folderEvent.metadata.toPath != nil) {
                            if (!string.IsNullOrWhiteSpace(folderEvent.Metadata.ToPath))
                            {
                                // folderPathForFolderEvent = folderEvent.metadata.toPath;
                                folderPathForFolderEvent = folderEvent.Metadata.ToPath;
                            }

                            // if ([folderPath isEqualToString:folderPathForFolderEvent]) {
                            if (folderPath.Equals(folderPathForFolderEvent, StringComparison.InvariantCulture))
                            {
                                // [self.activeSyncFolderQueue removeObject:folderEvent];
                                _activeSyncFolderQueue.Remove(folderEvent);
                            }

                            // // subfolders of this folder should be marked as sync as well
                            // if ([folderPathForFolderEvent rangeOfString:folderPath].location != NSNotFound) {
                            if (folderPathForFolderEvent.Contains(folderPath))
                            {
                                // [self badgeFileAtCloudPath:folderPathForFolderEvent withBadge:badgeType];
                                BadgeFileAtCloudPath_withBadge(folderPathForFolderEvent, badgeType);
                            }
                        }
                    }
                }
            }

            //if ([eventType rangeOfString:CLEventTypeFolderRange].location != NSNotFound) { // empty folders should automatically be badged.
            if (eventType.Contains(CLDefinitions.CLEventTypeFolderRange))
            {
           

                // __block BOOL shouldBadgeFolder = YES;
                bool shouldBadgeFolder = true;
                // [self.activeSyncQueue enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
                foreach (CLEvent activeEvent in _activeSyncQueue)
                {
                    // CLEvent *activeEvent = obj;
                    // NSString *eventPath = activeEvent.metadata.path;
                    string eventPath = activeEvent.Metadata.Path;

                    // if (activeEvent.metadata.toPath != nil) {
                    if (!string.IsNullOrWhiteSpace(activeEvent.Metadata.ToPath))
                    {
                        // eventPath = activeEvent.metadata.toPath;
                        eventPath = activeEvent.Metadata.ToPath;
                    }

                    // // if the path is not exactly our current path, but it has range of the current path, it means the folder is not going to be empty.
                    // if ([cloudPath isEqualToString:eventPath] == NO && [eventPath rangeOfString:cloudPath].location != NSNotFound) {
                    if (cloudPath.Equals(eventPath, StringComparison.InvariantCulture) &&
                        eventPath.Contains(cloudPath))
                    {
                        // shouldBadgeFolder = NO;
                        // *stop = YES;
                        shouldBadgeFolder = false;
                        break;
                    }
                }
                
                // if (shouldBadgeFolder == YES) { //badge empty folder and remove it from active folder queue
                if (shouldBadgeFolder)
                {
                    // [self badgeFileAtCloudPath:cloudPath withBadge:badgeType];
                    BadgeFileAtCloudPath_withBadge(cloudPath, badgeType);

                    // if ([self.activeSyncFolderQueue containsObject:event]) {
                    if (_activeSyncFolderQueue.Contains(evt))
                    {
                        // [self.activeSyncFolderQueue removeObject:event];
                        _activeSyncFolderQueue.Remove(evt);
                    }
                }
            }

            //// Lastly record recently changed files and figure out if we have any folders left to badge
            //if (self.syncItemsQueueCount <= 0) {
            if (_syncItemsQueueCount <= 0)
            {

                // [[CLSettings sharedSettings] recordRecentItems:[self.recentItems copy]];
                // [self.recentItems removeAllObjects];
                Settings.Instance.recordRecentItems(new List<string>(_recentItems));
                _recentItems.RemoveAll((string s) => { return true; });

                // Badge the remaining parent folders when the last file is done syncing.
                // if ([[cloudPath pathComponents] count] >= 2) {
                if (cloudPath.PathComponents().Length >= 2)
                {
                    // [[cloudPath pathComponents] enumerateObjectsUsingBlock:^(NSString *pathItem, NSUInteger idx, BOOL *stop) {
                    int idx = 0;
                    foreach (string pathItem in cloudPath.PathComponents())
                    {
                        // if (idx > 1) {
                        if (idx > 1)
                        {
                            // NSString *path = [cloudPath substringToIndex:[cloudPath rangeOfString:pathItem].location];                    
                            string path = cloudPath.SubstringToIndex(cloudPath.RangeOfString(pathItem).Location);

                            // [self badgeFileAtCloudPath:path withBadge:badgeType];
                            BadgeFileAtCloudPath_withBadge(path, badgeType);
                        }
                        ++idx;
                    }
                }
            }

        }

        //- (void)animateUIForSync:(BOOL)animate withStatusMessage:(menuItemActivityLabelType)message syncActivityCount:(NSInteger)count
        void AnimateUIForSyncWithStatusMessageSyncActivityCount(bool animate, menuItemActivityLabelType message, int count)
        {
            // Merged 7/7/12
            // [[CLUIActivityService sharedService] shouldAnimateStatusItemIconForActiveSync:animate];
            // [[CLUIActivityService sharedService] updateSyncMenuItemWithStatusMessages:message andFileCount:count];
    
            // if (animate == YES) {
            //     [self badgeFileAtCloudPath:@"" withBadge:cloudAppBadgeSyncing];
            // } else {
            //     [self badgeFileAtCloudPath:@"" withBadge:cloudAppBadgeSynced];
            // }
            //&&&&

            //TODO: Implement UI to animate toast with status message.
            // [[CLUIActivityService sharedService] shouldAnimateStatusItemIconForActiveSync:animate];
            // [[CLUIActivityService sharedService] updateSyncMenuItemWithStatusMessages:message andFileCount:count];

            // if (animate == YES) {
            if (animate)
            {

                // [self badgeFileAtCloudPath:@"" withBadge:cloudAppBadgeSyncing];
                BadgeFileAtCloudPath_withBadge("", cloudAppIconBadgeType.cloudAppBadgeSyncing);
            }
            else
            {
                // [self badgeFileAtCloudPath:@"" withBadge:cloudAppBadgeSynced];
                BadgeFileAtCloudPath_withBadge("", cloudAppIconBadgeType.cloudAppBadgeSynced);
            }
        }

        void BadgeFileAtCloudPath_withBadge(string cloudPath, cloudAppIconBadgeType badge)
        {
            //NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:cloudPath];
            //[[CLUIActivityService sharedService] badgeIconAtPath:fileSystemPath withStatus:badge];
            string fileSystemPath = Settings.Instance.CloudFolderPath + cloudPath;
            BadgeNET.IconOverlay.setBadgeType(badge, fileSystemPath);
        }

        //- (void)retryEvent:(CLEvent *)event isDownload:(BOOL)isDownload
        void RetryEvent_isDownload(CLEvent evt, bool isDownload)
        {
            // Merged 7/7/12
            //if (event.retryAttemps <= 3) {
            //    if (isDownload == YES) {
            //        CLHTTPConnectionOperation *downloadOperation = [self downloadOpperationForEvent:event];
            //        if (self.downloadOperationQueue){
            //            [self.downloadOperationQueue addOperation:downloadOperation];
            //            [self.activeDownloadQueue addObject:downloadOperation];
            //             NSLog(@"Retrying: %@, Storage Key: %@", event.metadata.path, event.metadata.storage_key);
            //        }
            //    }else {
            //        CLHTTPConnectionOperation *uploadOperation = [self uploadOperationForEvent:event];
            //        if (self.uploadOperationQueue){
            //            [self.uploadOperationQueue addOperation:uploadOperation];
            //             NSLog(@"Retrying: %@, Storage Key: %@", event.metadata.path, event.metadata.storage_key);
            //        }
            //    }
            //}else {
            //    if (isDownload == YES) {
            //        // TODO: Set up recovery 
            //        //[self.failedDownloadEventQueue addObject:event];
            //        [self performUpdateForSyncEvent:event success:NO];
            //        [[NSNotificationCenter defaultCenter] postNotificationName:@"CLSyncServicesDownloadFailedNotification" object:event];
            //    }else {
            //        // TODO: Set up recovery 
            //        //[self.failedUploadEventQueue addObject:event];
            //        [self performUpdateForSyncEvent:event success:NO];
            //        [[NSNotificationCenter defaultCenter] postNotificationName:@"CLSyncServicesUploadFailedNotification" object:event];
            //    }
            //}
    
            //event.retryAttemps++;
            //&&&&

            //if (event.retryAttemps <= 3) {
            if (evt.RetryAttempts <= 3)
            {
                // if (isDownload == YES) {
                if (isDownload)
                {
                    // CLHTTPConnectionOperation *downloadOperation = [self downloadOpperationForEvent:event];
                    CLHTTPConnectionOperation downloadOperation = DownloadOperationForEvent(evt);
                    if (downloadOperation != null)
                    {
                        // if (self.downloadOperationQueue){
                        if (_downloadOperationQueue != null)
                        {
                            // [self.downloadOperationQueue addOperation:downloadOperation];
                            // [self.activeDownloadQueue addObject:downloadOperation];
                            // NSLog(@"Retrying: %@, Storage Key: %@", event.metadata.path, event.metadata.storage_key);
                            _downloadOperationQueue.EnqueueOperation(downloadOperation);
                            //_activeDownloadQueue.Add(downloadOperation);   //bug???
                            _trace.writeToLog(1, "CLSyncService: RetryEvent: Retrying: {0}, Storage key: {1}.", evt.Metadata.Path, evt.Metadata.Storage_key);
                        }
                    }
                }
                 else
                {
                    // CLHTTPConnectionOperation *uploadOperation = [self uploadOperationForEvent:event]; 
                    CLHTTPConnectionOperation uploadOperation = UploadOperationForEvent(evt);

                    // if (self.uploadOperationQueue){
                    if (uploadOperation != null)
                    {
                        // [self.uploadOperationQueue addOperation:uploadOperation];
                        //  NSLog(@"Retrying: %@, Storage Key: %@", event.metadata.path, event.metadata.storage_key);
                        _uploadOperationQueue.EnqueueOperation(uploadOperation);
                        _trace.writeToLog(1, "CLSyncService: RetryEvent: Retrying(2): {0}, Storage key: {1}.", evt.Metadata.Path, evt.Metadata.Storage_key);

                    }
                }
            }
            else
            {
                if (isDownload)
                {
                    // TODO: Set up recovery 
                    // //[self.failedDownloadEventQueue addObject:event];

                    // [self performUpdateForSyncEvent:event success:NO];
                    PerformUpdateForSyncEvent_Success(evt, success: false);

                    //TODO: Implement this notification and the watcher.
                    // [[NSNotificationCenter defaultCenter] postNotificationName:@"CLSyncServicesDownloadFailedNotification" object:event];
                }
                else
                {
                    //TODO: Set up recovery 
                    // //[self.failedUploadEventQueue addObject:event];

                    // [self performUpdateForSyncEvent:event success:NO];
                    PerformUpdateForSyncEvent_Success(evt, success: false);

                    //TODO: Implement this notification and the watcher.
                    // [[NSNotificationCenter defaultCenter] postNotificationName:@"CLSyncServicesUploadFailedNotification" object:event];
                }
            }

            //event.retryAttemps++;
            evt.RetryAttempts++;
        }
    }
}


