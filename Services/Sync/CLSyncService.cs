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
using win_client.DataModels.Settings;

namespace win_client.Services.Badging
{
    public sealed class CLSyncService
    {
        #region "Static fields ans properties"
        private static CLSyncService _instance = null;
        private static object _instanceLocker = new object();
        private static CLTrace _trace = null;
        private static CLPrivateRestClient _restClient = null;
        private static CLOperationQueue _uploadOperationQueue = null;       // this is the upload queue used by the REST client.
        private static CLOperationQueue _downloadOperationQueue = null;     // this is the download queue used by the REST client.
        private static List<object> _recentItems = null;
        private static int _syncItemsQueueCount = 0;
        private static List<object> _activeDownloadQueue = null;
        private static List<object> _currentSids = null;
        private static bool _waitingForCloudResponse = false;
        private static bool _needSyncFromCloud = false;
        private static bool _waitingForFSMResponse = false;
        private static List<object> _activeSyncQueue = null;

        static DispatchQueue _com_cloud_sync_queue = null;
        static DispatchQueue get_cloud_sync_queue () {
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
        }

        /// <summary>
        /// Start the syncing service.
        /// </summary>
        public void BeginSyncServices()
        {
            this.ServiceStarted = true;
            _recentItems = new List<object>();
            _restClient = new CLPrivateRestClient();
            _activeDownloadQueue = new List<object>();
            _activeSyncQueue = new List<object>();
            _currentSids = new List<object>();
            _trace.writeToLog(1, "BeginSyncServices: Cloud Sync has Started for Cloud Folder at Path: {0}.", Settings.Instance.CloudFolderPath);
            if (_wasOffline == true) 
            {
                Dispatch.Async(_com_cloud_sync_queue, () =>
                {
                    CLFSMonitoringService.Instance.CheckWithFSMForEvents();
                });
                _wasOffline = false;
            }
        }

        /// <summary>
        /// Stop the syncing service.
        /// </summary>
        public void StopSyncServices()
        {
            //this.ServiceStarted = false;
            //if (_syncItemsQueueCount > 0) {
            //    AnimateUIForSyncWithStatusMessageSyncActivityCount(false, (int) menuItemActivityLabelType.menuItemActivityLabelSynced, 0);
            //    _activeSyncQueue.Clear();
            //    _activeDownloadQueue.Clear();
            //    _currentSids.Clear();
            //    _downloadOperationQueue.CancelAllOperations();
            //    UploadOperationQueue.CancelAllOperations();
            //}

            _trace.writeToLog(1, "BeginSyncServices: Cloud Sync has Eneded for Cloud Folder at Path: {0}.", Settings.Instance.CloudFolderPath);
        }

        void SyncFromFileSystemMonitorWithGroupedUserEvents(CLFSMonitoringService fsm, Dictionary<string, object> /*NSDictionary*/ events)
        {
            //string sid;
            //if ((this.CurrentSIDs).LastObject() != null) {
            //    sid = (this.CurrentSIDs).LastObject();
            //}
            //else {
            //    sid = (CLSettings.SharedSettings()).Sid();
            //}

            //NSNumber eid = events.ObjectForKey("event_id");
            //NSMutableArray eventList = events.ObjectForKey(CLSyncEvents);
            //NSMutableArray fsmEvents = NSMutableArray.Array();
            //NSArray filteredEvents = this.FilterDuplicateEvents(eventList);
            //if (eventList.Count() > 0) {
            //    this.AnimateUIForSyncWithStatusMessageSyncActivityCount(true, menuItemActivityLabelPreSync, 0);
            //    filteredEvents.EnumerateObjectsUsingBlock(^ (object fileSystemEvent, NSUInteger idx, bool stop) {
            //        CLEvent Myevent = new CLEvent();
            //        CLMetadata fsEventMetadata = new CLMetadata(fileSystemEvent.ObjectForKey("metadata"));
            //        Myevent.Metadata = fsEventMetadata;
            //        Myevent.IsMDSEvent = false;
            //        Myevent.Action = fileSystemEvent.ObjectForKey("event");
            //        fsmEvents.AddObject(Myevent);
            //    }
            //    );
            //    fsmEvents = this.UpdateMetadataForFileEvents(fsmEvents);
            //    NSMutableDictionary eventIds = NSMutableDictionary.DictionaryWithObjectsAndKeys(eid, CLSyncEventID, sid, CLSyncID, null);
            //    if (fsmEvents.Count() > 0) {
            //        NSMutableDictionary fsmEventsDictionary = (CLEvent.FsmDictionaryForCLEvents(fsmEvents)).MutableCopy();
            //        NSDictionary syncFormCalls = NSDictionary.DictionaryWithObjectsAndKeys(eventIds.ObjectForKey(CLSyncID), CLSyncID, null);
            //        fsmEventsDictionary.AddEntriesFromDictionary(syncFormCalls);
            //        Console.WriteLine("Requesting Sync To Cloud: \n\n%@\n\n", fsmEventsDictionary);
            //        (this.RestClient).SyncToCloudCompletionHandlerOnQueue(fsmEventsDictionary, ^ (NSDictionary metadata, NSError error) {
            //            if (error == null) {
            //                Console.WriteLine("Response From Sync To Cloud: \n\n%@\n\n", metadata);
            //                if ((metadata.ObjectForKey(CLSyncEvents)).Count() > 0) {
            //                    string sid = metadata.ObjectForKey(CLSyncID);
            //                    if ((this.CurrentSIDs).ContainsObject(sid) == false) {
            //                        (this.CurrentSIDs).AddObject(sid);
            //                    }

            //                    eventIds.SetObjectForKey(sid, CLSyncID);
            //                    NSArray mdsEvents = metadata.ObjectForKey("events");
            //                    NSMutableArray events = NSMutableArray.Array();
            //                    mdsEvents.EnumerateObjectsUsingBlock(^ (object mdsEvent, NSUInteger idx, bool stop) {
            //                        if (!((mdsEvent.ObjectForKey("sync_header")).ObjectForKey("status")).IsEqualToString("not_found")) {
            //                            if ((mdsEvent.ObjectForKey("metadata")).IsKindOfClass([NSDictionary class])) {
            //                                events.AddObject(CLEvent.EventFromMDSEvent(mdsEvent));
            //                            }

            //                        }

            //                    }
            //                    );
            //                    this.PerformSyncOperationWithEventsWithEventIDsAndOrigin(events, eventIds, CLEventOriginMDS);
            //                }

            //            }

            //        }
            //        , get_cloud_sync_queue());
            //    }

            //}

        }

        Array /*NSMutableArray*/ UpdateMetadataForFileEvents(Array /*NSMutableArray*/ events)
        {
            //events.EnumerateObjectsUsingBlock(^ (object obj, NSUInteger idx, bool stop) {
            //    CLEvent Myevent = obj;
            //    NSDictionary metadata;
            //    if ((Myevent.Action).IsEqualToString(CLEventTypeAddFile) || (Myevent.Action).IsEqualToString(CLEventTypeModifyFile)) {
            //        int do_try = 0;
            //        do {
            //            string fileSystemPath = ((CLSettings.SharedSettings()).CloudFolderPath()).StringByAppendingPathComponent(Myevent.Metadata.Path);
            //            metadata = NSDictionary.AttributesForItemAtPath(fileSystemPath);
            //            Myevent.Metadata.CreateDate = metadata.ObjectForKey("created_date");
            //            Myevent.Metadata.ModifiedDate = metadata.ObjectForKey("modified_date");
            //            Myevent.Metadata.Revision = metadata.ObjectForKey("revision");
            //            Myevent.Metadata.Hash = metadata.ObjectForKey("file_hash");
            //            Myevent.Metadata.Size = metadata.ObjectForKey("file_size");
            //            do_try++;
            //        }
            //        while ((Myevent.Metadata.CreateDate).RangeOfString("190").Location != NSNotFound && do_try < 1000);

            //    }

            //    if ((Myevent.Action).RangeOfString(CLEventTypeFileRange).Location != NSNotFound) {
            //        string cloudPath = Myevent.Metadata.Path;
            //        if ((Myevent.Action).IsEqualToString(CLEventTypeMoveFile) || (Myevent.Action).IsEqualToString(CLEventTypeRenameFile)) {
            //            cloudPath = Myevent.Metadata.FromPath;
            //        }

            //        CLMetadata indexedMetadata = CLIndexingServices.MetadataForItemAtCloudPathInContext(cloudPath, NSManagedObjectContext.
            //          ContextForCurrentThread());
            //        if (indexedMetadata != null) {
            //            if ((Myevent.Action).IsEqualToString(CLEventTypeAddFile)) {
            //                if ((Myevent.Metadata.Hash).IsEqualToString(indexedMetadata.Hash) == false && (Myevent.Metadata.Revision).IsEqualToString(
            //                  indexedMetadata.Revision) == false) {
            //                    Myevent.Metadata.Revision = indexedMetadata.Revision;
            //                    Myevent.Action = CLEventTypeModifyFile;
            //                }

            //            }
            //            else if ((Myevent.Action).IsEqualToString(CLEventTypeModifyFile)) {
            //                Myevent.Metadata.Revision = indexedMetadata.Revision;
            //            }
            //            else {
            //                Myevent.Metadata.Revision = indexedMetadata.Revision;
            //                Myevent.Metadata.Hash = indexedMetadata.Hash;
            //                Myevent.Metadata.CreateDate = indexedMetadata.CreateDate;
            //                Myevent.Metadata.ModifiedDate = indexedMetadata.ModifiedDate;
            //                Myevent.Metadata.Size = indexedMetadata.Size;
            //            }

            //        }

            //    }

            //}
            //);
            //return events;
            return new int[3];
        }

        void NotificationServiceDidReceivePushNotificationFromServer(bool /*CLNotificationServices*/ ns, string notification)
        {
            //string sid;
            //if ((this.CurrentSIDs).LastObject() != null) {
            //    sid = (this.CurrentSIDs).LastObject();
            //}
            //else {
            //    sid = (CLSettings.SharedSettings()).Sid();
            //}

            //if (this.WaitingForCloudResponse == true) {
            //    this.NeedSyncFromCloud = true;
            //    return;
            //}

            //NSNumber eid = NSNumber.NumberWithInteger(Constants.CLDotNotSaveId);
            //NSDictionary events = NSDictionary.DictionaryWithObjectsAndKeys("/", CLMetadataCloudPath, sid, CLSyncID, null);
            //Console.WriteLine("Requesting Sync From Cloud: \n\n%@\n\n", events);
            //this.NeedSyncFromCloud = false;
            //this.WaitingForCloudResponse = true;
            //this.AnimateUIForSyncWithStatusMessageSyncActivityCount(true, menuItemActivityLabelPreSync, 0);
            //(this.RestClient).SyncFromCloudCompletionHandlerOnQueue(events, ^ (NSDictionary metadata, NSError error) {
            //    if (error == null) {
            //        string sid = metadata.ObjectForKey(CLSyncID);
            //        if ((metadata.ObjectForKey(CLSyncEvents)).Count() > 0) {
            //            if ((this.CurrentSIDs).ContainsObject(sid) == false) {
            //                (this.CurrentSIDs).AddObject(sid);
            //            }

            //            Console.WriteLine("Current number of active SIDs: %lu", (this.CurrentSIDs).Count());
            //            NSArray mdsEvents = metadata.ObjectForKey("events");
            //            NSMutableArray events = NSMutableArray.Array();
            //            mdsEvents.EnumerateObjectsUsingBlock(^ (object mdsEvent, NSUInteger idx, bool stop) {
            //                events.AddObject(CLEvent.EventFromMDSEvent(mdsEvent));
            //            }
            //            );
            //            Console.WriteLine("Response From Sync From Cloud: \n\n%@\n\n", metadata);
            //            NSDictionary eventIds = NSDictionary.DictionaryWithObjectsAndKeys(eid, CLSyncEventID, sid, CLSyncID, null);
            //            this.PerformSyncOperationWithEventsWithEventIDsAndOrigin(events, eventIds, CLEventOriginMDS);
            //        }
            //        else {
            //            (CLSettings.SharedSettings()).RecordSID(sid);
            //            this.AnimateUIForSyncWithStatusMessageSyncActivityCount(false, (int) menuItemActivityLabelType.MenuItemActivityLabelSynced, 0);
            //        }

            //    }
            //    else {
            //        this.AnimateUIForSyncWithStatusMessageSyncActivityCount(false, (int) menuItemActivityLabelType.MenuItemActivityLabelSynced, 0);
            //    }

            //    this.WaitingForCloudResponse = false;
            //    if (this.NeedSyncFromCloud == true) {
            //        this.NotificationServiceDidReceivePushNotificationFromServer(null, null);
            //    }

            //}
            //, get_cloud_sync_queue());
        }

        void PerformSyncOperationWithEventsWithEventIDsAndOrigin(Array /*NSArray*/ events, Dictionary<string, object> ids, CLEventOrigin origin)
        {
            //Console.WriteLine("%s", __FUNCTION__);
            //this.AnimateUIForSyncWithStatusMessageSyncActivityCount(true, menuItemActivityLabelIndexing, 0);
            //NSDictionary sortedEvents = this.SortSyncEventsByType(events);
            //sortedEvents = this.IndexSortedEvents(sortedEvents);
            //(this.ActiveSyncQueue).AddObjectsFromArray(sortedEvents.ObjectForKey(CLEventTypeAdd));
            //(this.ActiveSyncQueue).AddObjectsFromArray(sortedEvents.ObjectForKey(CLEventTypeModify));
            //(this.ActiveSyncQueue).AddObjectsFromArray(sortedEvents.ObjectForKey(CLEventTypeRenameMove));
            //(this.ActiveSyncQueue).AddObjectsFromArray(sortedEvents.ObjectForKey(CLEventTypeDelete));
            //this.SyncItemsQueueCount = this.SyncItemsQueueCount + (this.ActiveSyncQueue).Count();
            //this.AnimateUIForSyncWithStatusMessageSyncActivityCount(true, (int) menuItemActivityLabelType.MenuItemActivityLabelSyncing, this.
            //  SyncItemsQueueCount);
            //this.ProcessDeleteSyncEvents(sortedEvents.ObjectForKey(CLEventTypeDelete));
            //this.ProcessAddSyncEvents(sortedEvents.ObjectForKey(CLEventTypeAdd));
            //this.ProcessModifySyncEvents(sortedEvents.ObjectForKey(CLEventTypeModify));
            //this.ProcessRenameMoveSyncEvents(sortedEvents.ObjectForKey(CLEventTypeRenameMove));
            //this.UpdateIndexForActiveSyncEvents();
            //(CLUIActivityService.SharedService()).DisplayUserNotificationForSyncEvents(this.ActiveSyncQueue);
            //(this.ActiveSyncQueue).RemoveAllObjects();
            //this.SaveSyncStateWithSIDAndEID(ids.ObjectForKey(CLSyncID), ids.ObjectForKey(CLSyncEventID));
            //this.AnimateUIForSyncWithStatusMessageSyncActivityCount(false, (int) menuItemActivityLabelType.MenuItemActivityLabelSynced, 0);
        }

        Array /*NSArray*/ FilterDuplicateEvents(Array /*NSArray*/ events)
        {
            //NSMutableArray eventList = events.MutableCopy();
            //NSArray eventListCopy = events.Copy();
            //NSInteger index = eventListCopy.Count() - 1;
            //foreach (object Myobject in eventListCopy.ReverseObjectEnumerator()) {
            //    if (eventList.IndexOfObjectInRange(Myobject, NSMakeRange(0, index)) != NSNotFound) {
            //        eventList.RemoveObjectAtIndex(index);
            //    }

            //    index--;
            //}
            //return eventList;
            return new int[3];
        }

        Dictionary<string, object> /*NSDictionary*/ SortSyncEventsByType(Array /*NSArray*/ events)
        {
            //Console.WriteLine("%s", __FUNCTION__);
            //NSMutableArray addEvents = NSMutableArray.Array();
            //NSMutableArray modifyEvents = NSMutableArray.Array();
            //NSMutableArray moveRenameEvents = NSMutableArray.Array();
            //NSMutableArray deleteEvents = NSMutableArray.Array();
            //events.EnumerateObjectsUsingBlock(^ (object obj, NSUInteger idx, bool stop) {
            //    CLEvent Myevent = obj;
            //    string eventAction = Myevent.Action;
            //    if (Myevent.IsMDSEvent) {
            //        eventAction = Myevent.SyncHeader.Action;
            //    }

            //    if (eventAction.RangeOfString(CLEventTypeAddRange).Location != NSNotFound) {
            //        addEvents.AddObject(Myevent);
            //    }

            //    if (eventAction.RangeOfString(CLEventTypeModifyFile).Location != NSNotFound) {
            //        modifyEvents.AddObject(Myevent);
            //    }

            //    if (eventAction.RangeOfString(CLEventTypeRenameRange).Location != NSNotFound) {
            //        moveRenameEvents.AddObject(Myevent);
            //    }

            //    if (eventAction.RangeOfString(CLEventTypeMoveRange).Location != NSNotFound) {
            //        moveRenameEvents.AddObject(Myevent);
            //    }

            //    if (eventAction.RangeOfString(CLEventTypeDeleteRange).Location != NSNotFound) {
            //        deleteEvents.AddObject(Myevent);
            //    }

            //}
            //);
            //return NSDictionary.DictionaryWithObjectsAndKeys(addEvents, CLEventTypeAdd, modifyEvents, CLEventTypeModify, moveRenameEvents, CLEventTypeRenameMove
            //  , deleteEvents, CLEventTypeDelete, null);
            return new Dictionary<string, object>();
        }

        Dictionary<string, object> /*NSDictionary*/ IndexSortedEvents(Dictionary<string, object> /*NSDictionary*/ sortedEvent)
        {
            //Console.WriteLine("%s", __FUNCTION__);
            //NSMutableArray deleteEvents = sortedEvent.ObjectForKey(CLEventTypeDelete);
            //NSMutableArray addEvents = sortedEvent.ObjectForKey(CLEventTypeAdd);
            //NSMutableArray modifyEvents = sortedEvent.ObjectForKey(CLEventTypeModify);
            //NSMutableArray moveRenameEvents = sortedEvent.ObjectForKey(CLEventTypeRenameMove);
            //(addEvents.Copy()).EnumerateObjectsUsingBlock(^ (object obj, NSUInteger idx, bool stop) {
            //    CLEvent addEvent = obj;
            //    string eventAction = addEvent.Action;
            //    if (addEvent.IsMDSEvent) {
            //        eventAction = addEvent.SyncHeader.Action;
            //    }

            //    bool isfFileEvent = eventAction.RangeOfString(CLEventTypeFileRange).Location != NSNotFound ? true : false;
            //    CLMetadata indexedMetadata = CLIndexingServices.MetadataForItemAtCloudPathInContext(addEvent.Metadata.Path, NSManagedObjectContext.
            //      ContextForCurrentThread());
            //    if (indexedMetadata != null) {
            //        if (isfFileEvent) {
            //            if ((addEvent.Metadata.Hash).IsEqualToString(indexedMetadata.Hash) && (addEvent.Metadata.Revision).IsEqualToString(indexedMetadata.
            //              Revision)) {
            //                string fileSystemPath = ((CLSettings.SharedSettings()).CloudFolderPath()).StringByAppendingPathComponent(addEvent.Metadata.Path);
            //                if ((NSFileManager.DefaultManager()).FileExistsAtPath(fileSystemPath)) {
            //                    addEvents.RemoveObject(addEvent);
            //                    this.BadgeFileAtCloudPathWithBadge(addEvent.Metadata.Path, (int) cloudAppIconBadgeType.CloudAppBadgeSynced);
            //                }

            //            }

            //        }
            //        else {
            //            string fileSystemPath = ((CLSettings.SharedSettings()).CloudFolderPath()).StringByAppendingPathComponent(addEvent.Metadata.Path);
            //            if ((NSFileManager.DefaultManager()).FileExistsAtPath(fileSystemPath)) {
            //                addEvents.RemoveObject(addEvent);
            //                this.BadgeFileAtCloudPathWithBadge(addEvent.Metadata.Path, (int) cloudAppIconBadgeType.CloudAppBadgeSynced);
            //            }

            //        }

            //    }

            //}
            //);
            //(deleteEvents.Copy()).EnumerateObjectsUsingBlock(^ (object obj, NSUInteger idx, bool stop) {
            //    CLEvent deleteEvent = obj;
            //    string eventAction = deleteEvent.Action;
            //    if (deleteEvent.IsMDSEvent) {
            //        eventAction = deleteEvent.SyncHeader.Action;
            //    }

            //    CLMetadata indexedMetadata = CLIndexingServices.MetadataForItemAtCloudPathInContext(deleteEvent.Metadata.Path, NSManagedObjectContext.
            //      ContextForCurrentThread());
            //    if (indexedMetadata == null) {
            //        string fileSystemPath = ((CLSettings.SharedSettings()).CloudFolderPath()).StringByAppendingPathComponent(deleteEvent.Metadata.Path);
            //        if ((NSFileManager.DefaultManager()).FileExistsAtPath(fileSystemPath) == false) {
            //            deleteEvents.RemoveObject(deleteEvent);
            //        }

            //    }

            //}
            //);
            //return NSDictionary.DictionaryWithObjectsAndKeys(addEvents, CLEventTypeAdd, modifyEvents, CLEventTypeModify, moveRenameEvents, CLEventTypeRenameMove
            //  , deleteEvents, CLEventTypeDelete, null);
            return new Dictionary<string, object>();
        }

        void UpdateIndexForActiveSyncEvents()
        {
            //this.AnimateUIForSyncWithStatusMessageSyncActivityCount(true, menuItemActivityLabelIndexing, 0);
            //((this.ActiveSyncQueue).Copy()).EnumerateObjectsUsingBlock(^ (object obj, NSUInteger idx, bool stop) {
            //    CLEvent Myevent = obj;
            //    if ((Myevent.SyncHeader.Action).RangeOfString(CLEventTypeAddRange).Location != NSNotFound) {
            //        CLIndexingServices.AddMetedataItemPendingInContext(Myevent.Metadata, false, NSManagedObjectContext.ContextForCurrentThread());
            //    }

            //    if ((Myevent.SyncHeader.Action).RangeOfString(CLEventTypeDeleteRange).Location != NSNotFound) {
            //        CLIndexingServices.RemoveMetadataItemWithCloudPath(Myevent.Metadata.Path);
            //    }

            //    if ((Myevent.SyncHeader.Action).RangeOfString(CLEventTypeMoveRange).Location != NSNotFound || (Myevent.SyncHeader.Action).RangeOfString(
            //      CLEventTypeRenameRange).Location != NSNotFound || (Myevent.SyncHeader.Action).IsEqualToString(CLEventTypeModifyFile)) {
            //        CLIndexingServices.UpdateLocalIndexItemWithEventPendingInContext(Myevent, false, NSManagedObjectContext.ContextForCurrentThread());
            //    }

            //}
            //);
            //if ((NSManagedObjectContext.ContextForCurrentThread()).HasChanges()) {
            //    CLIndexingServices.SaveDataInContext(NSManagedObjectContext.ContextForCurrentThread());
            //}

        }

        void ProcessAddSyncEvents(Array /*NSArray*/ events)
        {
            //Console.WriteLine("%s", __FUNCTION__);
            //events.EnumerateObjectsUsingBlock(^ (object obj, NSUInteger idx, bool stop) {
            //    CLEvent Myevent = obj;
            //    if (Myevent.IsMDSEvent) {
            //        string actionType = Myevent.SyncHeader.Action;
            //        string status = Myevent.SyncHeader.Status;
            //        if (actionType.IsEqualToString(CLEventTypeAddFolder)) {
            //            bool success = true;
            //            if (status == null) {
            //                success = (CLFSDispatcher.DefaultDispatcher()).CreateDirectoryAtPathError(Myevent.Metadata.Path, null);
            //                if (success == false) {
            //                }

            //            }

            //            this.PerformUpdateForSyncEventSuccess(Myevent, success);
            //        }

            //    }

            //}
            //);
            //NSMutableArray uploadEvents = NSMutableArray.Array();
            //NSMutableArray downloadEvents = NSMutableArray.Array();
            //events.EnumerateObjectsUsingBlock(^ (object obj, NSUInteger idx, bool stop) {
            //    CLEvent Myevent = obj;
            //    if (Myevent.IsMDSEvent) {
            //        string actionType = Myevent.SyncHeader.Action;
            //        string status = Myevent.SyncHeader.Status;
            //        if (actionType.IsEqualToString(CLEventTypeAddFile)) {
            //            if (status == null) {
            //                downloadEvents.AddObject(Myevent);
            //            }
            //            else {
            //                if (status.IsEqualToString(CLEventTypeUpload) || status.IsEqualToString(CLEventTypeUploading)) {
            //                    uploadEvents.AddObject(Myevent);
            //                }

            //                if (status.IsEqualToString(CLEventTypeExists) || status.IsEqualToString(CLEventTypeDuplicate)) {
            //                    this.PerformUpdateForSyncEventSuccess(Myevent, true);
            //                }

            //                if (status.IsEqualToString(CLEventTypeConflict)) {
            //                    this.PerformUpdateForSyncEventSuccess(Myevent, true);
            //                }

            //            }

            //        }

            //    }

            //}
            //);
            //if (uploadEvents.Count() > 0) {
            //    this.DispatchUploadEvents(uploadEvents);
            //}

            //if (downloadEvents.Count() > 0) {
            //    this.DispatchDownloadEvents(downloadEvents);
            //}

        }

        void ProcessModifySyncEvents(Array events)
        {
            //Console.WriteLine("%s", __FUNCTION__);
            //NSMutableArray uploadEvents = NSMutableArray.Array();
            //NSMutableArray downloadEvents = NSMutableArray.Array();
            //events.EnumerateObjectsUsingBlock(^ (object obj, NSUInteger idx, bool stop) {
            //    CLEvent Myevent = obj;
            //    string actionType = Myevent.SyncHeader.Action;
            //    string status = Myevent.SyncHeader.Status;
            //    if (actionType.IsEqualToString(CLEventTypeModifyFile)) {
            //        if (status == null) {
            //            downloadEvents.AddObject(Myevent);
            //        }
            //        else {
            //            if (status.IsEqualToString(CLEventTypeUpload) || status.IsEqualToString(CLEventTypeUploading)) {
            //                uploadEvents.AddObject(Myevent);
            //            }

            //            if (status.IsEqualToString(CLEventTypeExists) || status.IsEqualToString(CLEventTypeDuplicate) || status.IsEqualToString(
            //              CLEventTypeUploading)) {
            //                this.PerformUpdateForSyncEventSuccess(Myevent, true);
            //            }

            //            if (status.IsEqualToString(CLEventTypeConflict)) {
            //                this.PerformUpdateForSyncEventSuccess(Myevent, true);
            //            }

            //        }

            //    }

            //}
            //);
            //if (uploadEvents.Count() > 0) {
            //    this.DispatchUploadEvents(uploadEvents);
            //}

            //if (downloadEvents.Count() > 0) {
            //    NSArray sortedDownloadEvents = downloadEvents.SortedArrayUsingComparator(^ (CLEvent event1, CLEvent event2) {
            //        if ((event1.Metadata.Size).IntValue() > (event2.Metadata.Size).IntValue()) {
            //            return (NSComparisonResult) NSOrderedDescending;
            //        }

            //        if ((event1.Metadata.Size).IntValue() < (event2.Metadata.Size).IntValue()) {
            //            return (NSComparisonResult) NSOrderedAscending;
            //        }

            //        return (NSComparisonResult) NSOrderedSame;
            //    }
            //    );
            //    this.DispatchDownloadEvents(sortedDownloadEvents);
            //}

        }

        void ProcessRenameMoveSyncEvents(Array events)
        {
            //Console.WriteLine("%s", __FUNCTION__);
            //events.EnumerateObjectsUsingBlock(^ (object obj, NSUInteger idx, bool stop) {
            //    CLEvent Myevent = obj;
            //    string actionType = Myevent.SyncHeader.Action;
            //    string status = Myevent.SyncHeader.Status;
            //    string toPath = Myevent.Metadata.ToPath;
            //    string fromPath = Myevent.Metadata.FromPath;
            //    if (actionType.RangeOfString(CLEventTypeRenameRange).Location != NSNotFound || actionType.RangeOfString(CLEventTypeMoveRange).Location !=
            //      NSNotFound) {
            //        bool success = true;
            //        if (status == null) {
            //            success = (CLFSDispatcher.DefaultDispatcher()).MoveItemAtPathToError(fromPath, toPath, null);
            //        }

            //        this.PerformUpdateForSyncEventSuccess(Myevent, success);
            //    }

            //}
            //);
        }

        void ProcessDeleteSyncEvents(Array events)
        {
            //Console.WriteLine("%s", __FUNCTION__);
            //events.EnumerateObjectsUsingBlock(^ (object obj, NSUInteger idx, bool stop) {
            //    CLEvent Myevent = obj;
            //    string actionType = Myevent.SyncHeader.Action;
            //    string status = Myevent.SyncHeader.Status;
            //    string path = Myevent.Metadata.Path;
            //    if (actionType.RangeOfString(CLEventTypeDeleteRange).Location != NSNotFound) {
            //        bool success = true;
            //        NSError error;
            //        if (status == null) {
            //            success = (CLFSDispatcher.DefaultDispatcher()).DeleteItemAtPathError(path, error);
            //        }

            //        if ((status.IsEqualToString("ok") || status.IsEqualToString("already_deleted")) || success == true) {
            //            CLIndexingServices.RemoveMetadataItemWithCloudPath(Myevent.Metadata.Path);
            //        }
            //        else {
            //            Console.WriteLine("%s - There was an error deleting a file system item. Error: %@", __FUNCTION__, error);
            //        }

            //        this.PerformUpdateForSyncEventSuccess(Myevent, success);
            //    }

            //}
            //);
        }

        void DispatchUploadEvents(Array events)
        {
            //Console.WriteLine("%s", __FUNCTION__);
            //NSMutableArray operations = NSMutableArray.Array();
            //if (this.UploadOperationQueue == null) {
            //    this.UploadOperationQueue = new CLOperationQueue();
            //    this.UploadOperationQueue.MaxConcurrentOperationCount = 6;
            //}

            //Console.WriteLine("Number of uploads to start: %lu", events.Count());
            //NSInteger totalExpectedUploadBytes = 0;
            //NSInteger totalUploadedBytes = 0;
            //events.EnumerateObjectsUsingBlock(^ (object obj, NSUInteger idx, bool stop) {
            //    CLEvent Myevent = obj;
            //    string path = Myevent.Metadata.Path;
            //    string storageKey = Myevent.Metadata.Storage_key;
            //    string fileSystemPath = ((CLSettings.SharedSettings()).CloudFolderPath()).StringByAppendingPathComponent(path);
            //    totalExpectedUploadBytes = totalExpectedUploadBytes + (Myevent.Metadata.Size).IntegerValue();
            //    Console.WriteLine("File to be uploaded: %@, Storage Key: %@", path, storageKey);
            //    CLHTTPConnectionOperation uploadOperation = (this.RestClient).StreamingUploadOperationForStorageKeyWithFileSystemPathFileSizeAndMD5Hash(
            //      storageKey, fileSystemPath, Myevent.Metadata.Size, Myevent.Metadata.Hash);
            //    uploadOperation.SetUploadProgressBlock(^ (NSInteger bytesWritten, NSInteger totalBytesWritten, NSInteger totalBytesExpectedToWrite) {
            //        totalUploadedBytes = totalUploadedBytes + bytesWritten;
            //        double progress = (double) totalUploadedBytes / (double) totalExpectedUploadBytes;
            //        (CLUIActivityService.SharedService()).UpdateActivityViewWithProgressBytesOfTotalBytesFileCountAndActivityType(progress, (double)
            //          totalUploadedBytes, (double) totalExpectedUploadBytes, (this.UploadOperationQueue).OperationCount(), activityViewLabelUpload);
            //    }
            //    );
            //    uploadOperation.SetOperationCompletionBlock(^ (CLHTTPConnectionOperation operation, object responseObject, NSError error) {
            //        Console.WriteLine("Upload Status: %li", (operation.Response).StatusCode());
            //        if ((operation.Response).StatusCode() == 201) {
            //            Console.WriteLine("Upload Completed for File: %@", path);
            //            Console.WriteLine("Opperations remaining: %lu", ((this.UploadOperationQueue).Operations()).Count());
            //            this.PerformUpdateForSyncEventSuccess(Myevent, true);
            //        }
            //        else if ((operation.Response).StatusCode() == 304) {
            //            Console.WriteLine("The file already exists on the server");
            //            this.PerformUpdateForSyncEventSuccess(Myevent, true);
            //        }

            //        if (error) {
            //            Console.WriteLine("Failed to Upload File: %@. Error: %@, Code: %ld", path, error.LocalizedDescription(), error.Code());
            //            Console.WriteLine("Opperations remaining: %lu", ((this.UploadOperationQueue).Operations()).Count());
            //            this.PerformUpdateForSyncEventSuccess(Myevent, false);
            //        }

            //        if ((this.UploadOperationQueue).OperationCount() <= 0) {
            //            (CLUIActivityService.SharedService()).UpdateActivityViewWithProgressBytesOfTotalBytesFileCountAndActivityType(1, 0, 0, 0,
            //              activityViewLabelSynced);
            //        }

            //    }
            //    );
            //    operations.AddObject(uploadOperation);
            //}
            //);
            //Console.WriteLine("Starting Upload Operarions");
            //(this.UploadOperationQueue).AddOperationsWaitUntilFinished(operations, true);
            //Console.WriteLine("Finished Upload Operarions");
        }

        void DispatchDownloadEvents(Array events)
        {
            //Console.WriteLine("%s", __FUNCTION__);
            //NSMutableArray operations = NSMutableArray.Array();
            //if (this.DownloadOperationQueue == null) {
            //    this.DownloadOperationQueue = new CLOperationQueue();
            //    this.DownloadOperationQueue.MaxConcurrentOperationCount = 6;
            //}

            //NSInteger totalExpectedDownloadBytes = 0;
            //NSInteger totalDownloadedBytes = 0;
            //events.EnumerateObjectsUsingBlock(^ (object obj, NSUInteger idx, bool stop) {
            //    CLEvent Myevent = obj;
            //    string path = Myevent.Metadata.Path;
            //    string fileSystemPath = ((CLSettings.SharedSettings()).CloudFolderPath()).StringByAppendingPathComponent(path);
            //    totalExpectedDownloadBytes = totalExpectedDownloadBytes + (Myevent.Metadata.Size).IntegerValue();
            //    Console.WriteLine("File to be downloaded: %@, Storage Key: %@", path, Myevent.Metadata.Storage_key);
            //    CLHTTPConnectionOperation downloadOperation = (this.RestClient).StreamingDownloadOperationForStorageKeyWithFileSystemPathFileSizeAndMD5Hash(
            //      Myevent.Metadata.Storage_key, fileSystemPath, Myevent.Metadata.Size, Myevent.Metadata.Hash);
            //    downloadOperation.SetDownloadProgressBlock(^ (NSInteger bytesRead, NSInteger totalBytesRead, NSInteger totalBytesExpectedToRead) {
            //        totalDownloadedBytes = totalDownloadedBytes + bytesRead;
            //        double progress = (double) totalDownloadedBytes / (double) totalExpectedDownloadBytes;
            //        (CLUIActivityService.SharedService()).UpdateActivityViewWithProgressBytesOfTotalBytesFileCountAndActivityType(progress, (double)
            //          totalDownloadedBytes, (double) totalExpectedDownloadBytes, (this.DownloadOperationQueue).OperationCount(), activityViewLabelDownload);
            //    }
            //    );
            //    downloadOperation.SetOperationCompletionBlock(^ (CLHTTPConnectionOperation operation, object responseObject, NSError error) {
            //        if ((operation.Response).StatusCode() == 200) {
            //            Console.WriteLine("Download Completed for file: %@", path);
            //            Console.WriteLine("Opperations remaining: %lu", ((this.DownloadOperationQueue).Operations()).Count());
            //            bool success = true;
            //            if (responseObject) {
            //                NSError fsError;
            //                success = (CLFSDispatcher.DefaultDispatcher()).CreateFileWithDataAtPathError(responseObject, path, fsError);
            //                if (success == false) {
            //                    Console.WriteLine("%s - There was a problem writing a file to path:%@", __FUNCTION__, path);
            //                    if (fsError) {
            //                        Console.WriteLine("%s - There was an error writing a file to path: %@ error: %@", __FUNCTION__, path, fsError);
            //                    }

            //                }

            //            }
            //            else {
            //                NSError attributesError;
            //                bool attributesSet = (CLFSDispatcher.DefaultDispatcher()).SetAttributesUsingMetadataOfItemAtPathError(Myevent.Metadata,
            //                  fileSystemPath, attributesError);
            //                if (attributesSet) {
            //                    if (attributesError) {
            //                        Console.WriteLine("%s - %@", __FUNCTION__, attributesError.Description());
            //                    }

            //                }
            //                else {
            //                    Console.WriteLine("Failed to update attributes in: %s", __FUNCTION__);
            //                }

            //            }

            //            this.PerformUpdateForSyncEventSuccess(Myevent, true);
            //        }
            //        else {
            //            Console.WriteLine("%s - Download returned code: %ld", __FUNCTION__, (operation.Response).StatusCode());
            //        }

            //        if (error) {
            //            this.PerformUpdateForSyncEventSuccess(Myevent, false);
            //            Console.WriteLine("Failed to Download File: %@. Error: %@, Code: %ld", path, error.LocalizedDescription(), error.Code());
            //        }

            //        (this.ActiveDownloadQueue).RemoveObject(Myevent);
            //        if ((this.DownloadOperationQueue).OperationCount() <= 0) {
            //            (CLUIActivityService.SharedService()).UpdateActivityViewWithProgressBytesOfTotalBytesFileCountAndActivityType(1, 0, 0, 0,
            //              activityViewLabelSynced);
            //        }

            //        responseObject = null;
            //    }
            //    );
            //    operations.AddObject(downloadOperation);
            //    (this.ActiveDownloadQueue).AddObject(Myevent);
            //}
            //);
            //Console.WriteLine("Starting Download Operarions");
            //(this.DownloadOperationQueue).AddOperationsWaitUntilFinished(operations, true);
            //Console.WriteLine("Finished Download Operarions");
        }

        void SaveSyncStateWithSIDAndEID(string sid, ulong /*NSNumber*/ eid)
        {
            //Console.WriteLine("%s", __FUNCTION__);
            //if ((this.CurrentSIDs).ContainsObject(sid)) {
            //    Console.WriteLine("Current SID Stack contains the sid we are saving: %@", sid);
            //    Console.WriteLine(" Save Global Sid: %@ Current number of active SIDs: %lu", sid, (this.CurrentSIDs).Count());
            //    (this.CurrentSIDs).RemoveObject(sid);
            //    Console.WriteLine(" Save Global Sid: %@ Current number of active SIDs: %lu", sid, (this.CurrentSIDs).Count());
            //}

            //if (sid.IsEqualToString((NSNumber.NumberWithInteger(Constants.CLDotNotSaveId)).StringValue()) == false) {
            //    (CLSettings.SharedSettings()).RecordSID(sid);
            //}

            //if (eid.IntegerValue() != (NSNumber.NumberWithInteger(Constants.CLDotNotSaveId)).IntegerValue()) {
            //    (CLSettings.SharedSettings()).RecordEventId(eid);
            //}

        }

        void PerformUpdateForSyncEventSuccess(CLEvent Myevent, bool success)
        {
            //cloudAppIconBadgeType badgeType = (int) cloudAppIconBadgeType.CloudAppBadgeSynced;
            //if (success) {
            //    string eventName = Myevent.SyncHeader.Action;
            //    if (eventName.RangeOfString(CLEventTypeFileRange).Location != NSNotFound) {
            //        if (this.SyncItemsQueueCount <= 8 && this.SyncItemsQueueCount > 0) {
            //            if (eventName.RangeOfString(CLEventTypeMoveRange).Location != NSNotFound || eventName.RangeOfString(CLEventTypeRenameRange).Location !=
            //              NSNotFound) {
            //                (this.RecentItems).AddObject(Myevent.Metadata.ToPath);
            //            }
            //            else {
            //                (this.RecentItems).AddObject(Myevent.Metadata.Path);
            //            }

            //        }

            //    }

            //}
            //else {
            //    badgeType = cloudAppIconBadgeType.CloudAppBadgeFailed;
            //}

            //string cloudPath = Myevent.Metadata.Path;
            //if (Myevent.Metadata.ToPath != null) {
            //    cloudPath = Myevent.Metadata.ToPath;
            //}

            //this.BadgeFileAtCloudPathWithBadge(cloudPath, badgeType);
            //this.SyncItemsQueueCount = this.SyncItemsQueueCount - 1;
            //menuItemActivityLabelType messageType = (int) menuItemActivityLabelType.MenuItemActivityLabelSyncing;
            //bool animate = true;
            //if (this.SyncItemsQueueCount <= 0) {
            //    messageType = menuItemActivityLabelType.MenuItemActivityLabelSynced;
            //    animate = false;
            //}

            //this.AnimateUIForSyncWithStatusMessageSyncActivityCount(animate, messageType, this.SyncItemsQueueCount);
            //if (this.SyncItemsQueueCount <= 0) {
            //    (CLSettings.SharedSettings()).RecordRecentItems((this.RecentItems).Copy());
            //    (this.RecentItems).RemoveAllObjects();
            //}

        }

        void AnimateUIForSyncWithStatusMessageSyncActivityCount(bool animate, menuItemActivityLabelType message, ulong /*NSInteger*/ count)
        {
            //(CLUIActivityService.SharedService()).ShouldAnimateStatusItemIconForActiveSync(animate);
            //(CLUIActivityService.SharedService()).UpdateSyncMenuItemWithStatusMessagesAndFileCount(message, count);
            //if (animate == true) {
            //    this.BadgeFileAtCloudPathWithBadge("", (int) cloudAppIconBadgeType.CloudAppBadgeSyncing);
            //}
            //else {
            //    this.BadgeFileAtCloudPathWithBadge("", (int) cloudAppIconBadgeType.CloudAppBadgeSynced);
            //}

        }

        void BadgeFileAtCloudPathWithBadge(string cloudPath, int /*cloudAppIconBadgeType*/ badge)
        {
            //string fileSystemPath = ((CLSettings.SharedSettings()).CloudFolderPath()).StringByAppendingPathComponent(cloudPath);
            //(CLUIActivityService.SharedService()).BadgeIconAtPathWithStatus(fileSystemPath, badge);
        }
    }
}
