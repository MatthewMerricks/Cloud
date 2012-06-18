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
using BadgeNET;

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
        private static List<object> _recentItems = null;
        private static int _syncItemsQueueCount = 0;
        private static List<object> _activeDownloadQueue = null;
        private static List<string> _currentSids = null;
        private static bool _waitingForCloudResponse = false;
        private static bool _needSyncFromCloud = false;
        private static bool _waitingForFSMResponse = false;
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
        }

        /// <summary>
        /// Start the syncing service.
        /// </summary>
        public void BeginSyncServices()
        {
            _serviceStarted = true;
            _recentItems = new List<object>();
            _restClient = new CLPrivateRestClient();
            _activeDownloadQueue = new List<object>(); 
            _activeSyncQueue = new List<CLEvent>();
            _activeSyncFileQueue = new List<CLEvent>();
            _activeSyncFolderQueue = new List<CLEvent>();
            _currentSids = new List<string>();
            _trace.writeToLog(1, "BeginSyncServices: Cloud Sync has Started for Cloud Folder at Path: {0}.", Settings.Instance.CloudFolderPath);

            if (_wasOffline == true) 
            {
                Dispatch.Async(_com_cloud_sync_queue, new Action<object>((x) =>
                {
                    CLFSMonitoringService.Instance.CheckWithFSMForEvents();
                }), null);
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


        Array /*NSMutableArray*/ UpdateMetadataForFileEvents(Array /*NSMutableArray*/ events)
        {
#if TRASH
    __block NSMutableArray *eventsToBeRemoved = [NSMutableArray array];
    [events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {

        CLEvent *event = obj;
        NSDictionary *metadata;
        NSString *cloudPath = event.metadata.path;
        
        if ([event.action isEqualToString:CLEventTypeMoveFile] || [event.action isEqualToString:CLEventTypeRenameFile]) {
            cloudPath = event.metadata.fromPath;
        }

        // for new files we get the current file metadata from the file system.
        if ([event.action isEqualToString:CLEventTypeAddFile] || [event.action isEqualToString:CLEventTypeModifyFile]) {

            // Check if this file item is a symblink, if this object is a in fact a link, the utility method will return a valid target path.
            NSString *fileSystemPath =[[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:cloudPath];
            CLAppDelegate *appDelegate = [NSApp delegate];
            if ([appDelegate isFileAliasAtPath:fileSystemPath] == YES) {
                // get the target path
                NSString *targetPath = [fileSystemPath stringByIterativelyResolvingSymlinkOrAlias];
                if (targetPath != nil) {
                    // symblink events are always recognized as files, therefore simply replace the occurence of the word file with link for the event action.
                    event.action = [event.action stringByReplacingCharactersInRange:[event.action rangeOfString:CLEventTypeFileRange] withString:@"link"];

                    if ([targetPath rangeOfString:[[CLSettings sharedSettings] cloudFolderPath]].location == NSNotFound) {
                        targetPath = [NSString stringWithFormat:@"/%@", targetPath];
                    }

                    event.metadata.targetPath = targetPath;
                    
                    // this link is useless, if path target matches the link path, then we don't have a valid link resolutions. (see NSString+CloudUtilities.m)
                    if ([targetPath isEqualToString:fileSystemPath]) {
                        [eventsToBeRemoved addObject:events];
                    }
                }
            }
            else { // all regular file events
                int do_try = 0;
                
                do {
                    @autoreleasepool {
                        NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:event.metadata.path];
                        metadata = [NSDictionary attributesForItemAtPath:fileSystemPath];
                        
                        event.metadata.createDate = [metadata objectForKey:CLMetadataFileCreateDate];
                        event.metadata.modifiedDate = [metadata objectForKey:CLMetadataFileModifiedDate];
                        event.metadata.revision = [metadata objectForKey:CLMetadataFileRevision];
                        event.metadata.hash = [metadata objectForKey:CLMetadataFileHash];
                        event.metadata.size = [metadata objectForKey:CLMetadataFileSize];
                        event.metadata.customAttributes = nil;
                        if ([event.metadata.createDate rangeOfString:@"190"].location == NSNotFound) {
                            event.metadata.customAttributes = [CLExtendedAttributes archiveAndEncodeExtendedAttributesAtPath:fileSystemPath];
                        }
                    }
                    
                    do_try ++;
                }
                while ( [event.metadata.createDate rangeOfString:@"190"].location != NSNotFound && do_try < 3000); // 1 second. TODO: This hack sucks!
            }
        }
        
        // all other file events we get stored index data for this event item.
        if ([event.action rangeOfString:CLEventTypeFileRange].location != NSNotFound) {

            CLMetadata *indexedMetadata = [CLIndexingServices metadataForItemAtCloudPath:cloudPath];
            
            if (indexedMetadata != nil) { // we have an object indexed for this event.

                // for add events, if the darn thing already exists in the index, it means that FSM failed to pick up the event as a modify
                // let's make sure of that and if it turns out to be true, then we need to change the event to become a modify type.
                if ([event.action isEqualToString:CLEventTypeAddFile]) {
                    if ([event.metadata.hash isEqualToString:indexedMetadata.hash] == NO &&
                        [event.metadata.revision isEqualToString:indexedMetadata.revision] == NO) {
                        event.metadata.revision = indexedMetadata.revision;
                        event.action = CLEventTypeModifyFile;
                    }
                }
                else if ([event.action isEqualToString:CLEventTypeModifyFile]) { // for modify we only want to revision
                    event.metadata.revision = indexedMetadata.revision;
                }
                else { // we want it all for all other cases.
                    
                    event.metadata.revision = indexedMetadata.revision;
                    event.metadata.hash = indexedMetadata.hash;
                    event.metadata.createDate = indexedMetadata.createDate;
                    event.metadata.modifiedDate = indexedMetadata.modifiedDate;
                    event.metadata.size = indexedMetadata.size;
                }

                if (indexedMetadata.targetPath != nil) { // we have a link object, convert
                    // symblink events are always recognized as files, therefore simply replace the occurence of the word file with link for the event action.
                    event.action = [event.action stringByReplacingCharactersInRange:[event.action rangeOfString:CLEventTypeFileRange] withString:@"link"];
                    event.metadata.targetPath = indexedMetadata.targetPath;
                }
            }
        }
    }];

    // discard events that we don't care for (like invalid symblinks).
    if ([eventsToBeRemoved count] > 0) {
        [events removeObjectsInArray:eventsToBeRemoved];
    }
    
    return events;
#endif // TRASH
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

        // - (NSDictionary *)sortSyncEventsByType:(NSArray *)events
        Dictionary<string, object> SortSyncEventsByType(List<CLEvent> events)
        {
#if TRASH
    NSLog(@"%s", __FUNCTION__);
    NSMutableArray * addEvents = [NSMutableArray array];
    NSMutableArray * modifyEvents = [NSMutableArray array];
    NSMutableArray * moveRenameEvents = [NSMutableArray array];
    NSMutableArray * deleteEvents = [NSMutableArray array];
    
    [events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
        
        CLEvent *event = obj;        
        NSString *eventAction = event.action;
        if (event.isMDSEvent) {
            eventAction = event.syncHeader.action;
        }
        
        if ([eventAction rangeOfString:CLEventTypeAddRange].location != NSNotFound) {
            [addEvents addObject:event];
        }
        if ([eventAction rangeOfString:CLEventTypeModifyFile].location != NSNotFound) {
            [modifyEvents addObject:event];
        }
        if ([eventAction rangeOfString:CLEventTypeRenameRange].location != NSNotFound) {
            [moveRenameEvents addObject:event];
        }
        if ([eventAction rangeOfString:CLEventTypeMoveRange].location != NSNotFound ) {
            [moveRenameEvents addObject:event];
        }
        if ([eventAction rangeOfString:CLEventTypeDeleteRange].location != NSNotFound) {
            [deleteEvents addObject:event];
        }
    }];
    
    return [NSDictionary dictionaryWithObjectsAndKeys:addEvents, CLEventTypeAdd, modifyEvents ,CLEventTypeModify, moveRenameEvents,  CLEventTypeRenameMove, deleteEvents, CLEventTypeDelete, nil];
#endif // TRASH

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

        // - (NSDictionary *)indexSortedEvents:(NSDictionary *)sortedEvent
        Dictionary<string, object> IndexSortedEvents(Dictionary<string, object> sortedEvent)
        {
            _trace.writeToLog(9, "CLSyncService: IndexSortedEvents: Entry.");

            // This is the order that events need to be processed. Do not change.
            //__block NSMutableArray *deleteEvents     = [sortedEvent objectForKey:CLEventTypeDelete];
            //__block NSMutableArray *addEvents        = [sortedEvent objectForKey:CLEventTypeAdd];
            //__block NSMutableArray *modifyEvents     = [sortedEvent objectForKey:CLEventTypeModify];
            //__block NSMutableArray *moveRenameEvents = [sortedEvent objectForKey:CLEventTypeRenameMove];
            List<CLEvent> deleteEvents = (List<CLEvent>)sortedEvent[CLDefinitions.CLEventTypeDelete];
            List<CLEvent> addEvents = (List<CLEvent>)sortedEvent[CLDefinitions.CLEventTypeAdd];
            List<CLEvent> modifyEvents = (List<CLEvent>)sortedEvent[CLDefinitions.CLEventTypeModify];
            List<CLEvent> moveRenameEvents = (List<CLEvent>)sortedEvent[CLDefinitions.CLEventTypeRenameMove];

            // Check if we can reconcile these requested events with our Index.
            // [[addEvents copy] enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop)
            List<CLEvent> addEventsCopy = new List<CLEvent>(addEvents);
            addEventsCopy.ForEach(obj =>
            {
                //CLEvent* addEvent = obj;

                //NSString* eventAction = addEvent.action;
                //if (addEvent.isMDSEvent)
                //{
                //    eventAction = addEvent.syncHeader.action;
                //}
                CLEvent addEvent = obj;
                string eventAction = addEvent.Action;
                if (addEvent.IsMDSEvent)
                {
                    eventAction = addEvent.SyncHeader.Action;
                }

                // Determine if file or folder action.
                // BOOL isfFileEvent = [eventAction rangeOfString:CLEventTypeFileRange].location != NSNotFound ? YES : NO;
                bool isFileEvent = eventAction.Contains(CLDefinitions.CLEventTypeFileRange);

                // Determine if this file system item already exists in the index.
                // CLMetadata *indexedMetadata = [CLIndexingServices metadataForItemAtCloudPath:addEvent.metadata.path];
                CLMetadata indexedMetadata = CLIndexingService.Instance.MetadataForItemAtCloudPath(addEvent.Metadata.Path);

                // TODO: Even though we don't have this in the index we still download it if the file exists and the hashes match.. ? We should add it to the index
                // and then punt the event?
                // if (indexedMetadata != nil) { 
                if (indexedMetadata != null)
                {                                               // this object shouldn't exist in our index, since it's the first time we're adding it.
                    if (isFileEvent)                            // file event
                    {
                        // if both the hash and revisions of this new event match the one existing in our index, we can punt, because it means we already have this file.
                        // FSM: If the event came from FSM (for the first time) it's certain that we don't have this file indexed, so this code is never going to run, however
                        // if the FSM throws a second event our way with the same file (it may happen because of a bug in apple's FSEventStream API), then we punt the duplicate event.
                        // MDS: Same applies to events received from the MDS, if we already have this file indexed (hash and revision match) and the file exisits, we don't have to re-download
                        // the file, we just punt.
                        //if ([addEvent.metadata.hash isEqualToString:indexedMetadata.hash] && [addEvent.metadata.revision isEqualToString:indexedMetadata.revision]) {

                        //    NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:addEvent.metadata.path];
                        //    if ([[NSFileManager defaultManager] fileExistsAtPath:fileSystemPath]) { // checking if file exsits in the file system.

                        //        [addEvents removeObject:addEvent]; // Punt this event.

                        //        // in this case we badged the file
                        //        [self badgeFileAtCloudPath:addEvent.metadata.path withBadge:cloudAppBadgeSynced];
                        //    }

                        //}

                        if (addEvent.Metadata.Hash.Equals(indexedMetadata.Hash, StringComparison.InvariantCulture) && addEvent.Metadata.Revision.Equals(indexedMetadata.Revision, StringComparison.InvariantCulture))
                        {
                            string fileSystemPath =  Settings.Instance.CloudFolderPath + addEvent.Metadata.Path;
                            if (File.Exists(fileSystemPath))
                            {
                                addEvents.Remove(addEvent);

                                // In this case we badged the file
                                //TODO: Reference the BadgeNet assembly to get the badge type enumeration.
                                this.BadgeFileAtCloudPathWithBadge(addEvent.Metadata.Path, (int)CLConstants.CloudAppIconBadgeType.cloudAppBadgeSynced);
                            }

                        }

                    }
                    else                                     // folder event
                    {
                        // it's super simple: if the folder already existis in our index and exists in the file system, then we punt.
                        //NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:addEvent.metadata.path];
                        //if ([[NSFileManager defaultManager] fileExistsAtPath:fileSystemPath]) { // checking if file exsits in the file system.
                    
                        //    [addEvents removeObject:addEvent]; // Punt this event.

                        //    // in this case we badged the file
                        //    [self badgeFileAtCloudPath:addEvent.metadata.path withBadge:cloudAppBadgeSynced];

                        //}
                        string fileSystemPath = Settings.Instance.CloudFolderPath + addEvent.Metadata.Path;
                        if (File.Exists(fileSystemPath))
                        {
                            addEvents.Remove(addEvent);

                            // In this case we badged the file
                            //TODO: Reference the BadgeNet assembly to get the badge type enumeration.
                            this.BadgeFileAtCloudPathWithBadge(addEvent.Metadata.Path, (int)CLConstants.CloudAppIconBadgeType.cloudAppBadgeSynced);
                        }
                    }
                }

            });

            // [[deleteEvents copy] enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
            List<CLEvent> deleteEventsCopy = new List<CLEvent>(deleteEvents);
            deleteEventsCopy.ForEach(obj =>
            {
                //    CLEvent deleteEvent = obj;
                //    string eventAction = deleteEvent.Action;
                //    if (deleteEvent.IsMDSEvent) {
                //        eventAction = deleteEvent.SyncHeader.Action;
                //    }
                //    CLMetadata *indexedMetadata = [CLIndexingServices metadataForItemAtCloudPath:deleteEvent.metadata.path];
                //    if (indexedMetadata == null) {
                //        string fileSystemPath = ((CLSettings.SharedSettings()).CloudFolderPath()).StringByAppendingPathComponent(deleteEvent.Metadata.Path);
                //        if ((NSFileManager.DefaultManager()).FileExistsAtPath(fileSystemPath) == false) {
                //            deleteEvents.RemoveObject(deleteEvent);
                //        }
                //    }
                CLEvent deleteEvent = obj;
                string eventAction = deleteEvent.Action;
                if (deleteEvent.IsMDSEvent)
                {
                    eventAction = deleteEvent.SyncHeader.Action;
                }

                CLMetadata indexedMetadata = CLIndexingService.Instance.MetadataForItemAtCloudPath(deleteEvent.Metadata.Path);
                if (indexedMetadata == null) 
                {
                    string fileSystemPath = Settings.Instance.CloudFolderPath + deleteEvent.Metadata.Path;
                    if (!File.Exists(fileSystemPath))
                    {
                        deleteEvents.Remove(deleteEvent);
                    }
                }
            });

            //return NSDictionary.DictionaryWithObjectsAndKeys(addEvents, CLEventTypeAdd, modifyEvents, CLEventTypeModify, moveRenameEvents, CLEventTypeRenameMove
            //  , deleteEvents, CLEventTypeDelete, null);
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
        public void SyncFromFileSystemMonitorWithGroupedUserEventsCallback(Dictionary<string, object> eventsDictionary)
        {

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

            // NSNumber *eid = [events objectForKey:@"event_id"];
            // NSMutableArray *eventList = [events objectForKey:CLSyncEvents];
            // NSMutableArray *fsmEvents = [NSMutableArray array];
            int eid = (int)eventsDictionary[CLDefinitions.CLEventKey];

            IEnumerable<Dictionary<string, object>> filteredEvents = null;
            object eventsValue = eventsDictionary["events"];
            Array castEvents = eventsValue as Array;

            List<CLEvent> fsmEvents = new List<CLEvent>();

            // Filtering duplicate events
            // NSArray *filteredEvents = [self filterDuplicateEvents:eventList];
            if (castEvents != null)
            {
                filteredEvents = castEvents.OfType<Dictionary<string, object>>()
                    .Where(currentEvent => currentEvent.ContainsKey("event")
                        && currentEvent.ContainsKey("metadata"))
                    .Select((currentEvent, index) => new EventHolder() { Index = index, Event = currentEvent })
                    .Distinct(EventComparer.Instance)
                    .OrderBy(currentEvent => currentEvent.Index)
                    .Select(currentEvent => currentEvent.Event);
            }
            else
            {
                _trace.writeToLog(1, "CLSyncService: SyncFromFileSystemMonitorWithGroupedUserEvents: ERROR: No events from file system monitor.");
                return;
            }

            // if ([eventList count] > 0) {
            if (filteredEvents.Count<Dictionary<string, object>>() > 0)
            {
                // Update UI with activity.
                //TODO: Update the UI with zero synced files.
                // [self animateUIForSync:YES withStatusMessage:menuItemActivityLabelPreSync syncActivityCount:0];

                // Generate FSM CLEvents 
                //[filteredEvents enumerateObjectsUsingBlock:^(id fileSystemEvent, NSUInteger idx, BOOL *stop) {
                //    @autoreleasepool {
                //        CLEvent *event = [[CLEvent alloc] init];
                //        CLMetadata *fsEventMetadata = [[CLMetadata alloc] initWithDictionary:[fileSystemEvent objectForKey:@"metadata"]];
                //        event.metadata = fsEventMetadata;
                //        event.isMDSEvent = NO;
                //        event.action = [fileSystemEvent objectForKey:@"event"];
                //        [fsmEvents addObject:event];
                //    }
                //}];
                // At this point, filteredEvents is an IEnumerable of Dictionary<string, object>, where each
                // Dictionary looks like:
                //             Dictionary
                //                 "event", string (event name: One of the CLEventType* strings listed above)
                //                 "metadata", Dictionary
                //                     if eventName is RenameFile/Folder or MoveFile/Folder
                //                         "from_path", oldPath of the item (without the Cloud folder root)
                //                         "to_path", path of the item (without the Cloud folder root)
                //                     else eventName is NOT one of RenameFile/Folder or MoveFile/Folder
                //                         "path", path of the item (without the Cloud folder root)
                //                     endelse eventName is NOT one of RenameFile/Folder or MoveFile/Folder
                // We will iterate over this collection and build a CLEvent from each Dictionary.
                foreach(Dictionary<string, object> fileSystemEvent in filteredEvents)
                {
                    CLEvent evt = new CLEvent();
                    CLMetadata fsEventMetadata = new CLMetadata((Dictionary<string, object>)fileSystemEvent[CLDefinitions.CLSyncEventMetadata]);
                    evt.Metadata = fsEventMetadata;
                    evt.IsMDSEvent = false;
                    evt.Action = (string)fileSystemEvent[CLDefinitions.CLSyncEvent];
                    fsmEvents.Add(evt);
                }

                // Update events with metadata.
                // fsmEvents = [self updateMetadataForFileEvents:fsmEvents];
                fsmEvents = UpdateMetadataForFileEvents(fsmEvents);

                // Sorting.
                // This results in the following:
                //      Dictionary<string, object>
                //          "type_add", List<CLEvent>  // list of add events
                //          "type_modify", List<CLEvent>  // list of modify events
                //          "type_rename_move", List<CLEvent>  // list of modify events
                //          "type_delete", List<CLEvent>  // list of modify events
                // NSDictionary *sortedEvents = [self sortSyncEventsByType:fsmEvents];
                Dictionary<string, object> sortedEvents = SortSyncEventsByType(fsmEvents);
        
                // Preprocess Index (may filter out duplicate events based on existing index items)
                // sortedEvents = [self indexSortedEvents:sortedEvents];
                sortedEvents = IndexSortedEvents(sortedEvents);

                // if ([sortedEvents count] > 0) {
                if (sortedEvents.Count > 0)
                {                                       // we may have eliminated events while filtering them thru our index.

                    // Adding objects to our active sync queue
                    // This builds a new List<CLEvent> with all of the adds at the front, followed 
                    // by the modifies, rename/moves and finally the deleted.
                    //NSMutableArray *sortedFSMEvents = [NSMutableArray array];
                    //[sortedFSMEvents addObjectsFromArray:[sortedEvents objectForKey:CLEventTypeAdd]];
                    //[sortedFSMEvents addObjectsFromArray:[sortedEvents objectForKey:CLEventTypeModify]];
                    //[sortedFSMEvents addObjectsFromArray:[sortedEvents objectForKey:CLEventTypeRenameMove]];
                    //[sortedFSMEvents addObjectsFromArray:[sortedEvents objectForKey:CLEventTypeDelete]];
                    List<CLEvent> sortedFsmEvents = new List<CLEvent>();
                    sortedFsmEvents.AddRange((List<CLEvent>)sortedEvents[CLDefinitions.CLEventTypeAdd]);
                    sortedFsmEvents.AddRange((List<CLEvent>)sortedEvents[CLDefinitions.CLEventTypeModify]);
                    sortedFsmEvents.AddRange((List<CLEvent>)sortedEvents[CLDefinitions.CLEventTypeRenameMove]);
                    sortedFsmEvents.AddRange((List<CLEvent>)sortedEvents[CLDefinitions.CLEventTypeDelete]);

                    // Dictionary< NSMutableDictionary *fsmEventsDictionary = [[CLEvent fsmDictionaryForCLEvents:fsmEvents] mutableCopy];
                    Dictionary<string, object> fsmEventsDictionary =  CLEvent.FsmDictionaryForCLEvents(fsmEvents);
             
                    // NSDictionary *syncFormCalls = [NSDictionary dictionaryWithObjectsAndKeys:sid, CLSyncID, nil];
                    // [fsmEventsDictionary addEntriesFromDictionary:syncFormCalls];
                    fsmEventsDictionary.Add(CLDefinitions.CLSyncID, sid);

                    _trace.writeToLog(1, "CLSyncService: SyncFromFileSystemMonitorWithGroupedUserEvents: Requesting sync to Cloud. sid: {0}.", sid);

                    //[self.restClient syncToCloud:fsmEventsDictionary completionHandler:^(NSDictionary *metadata, NSError *error) {
                
                    //    if (error == nil) {
                    
                    //        NSLog(@"Response From Sync To Cloud: \n\n%@\n\n", metadata);
                    
                    //        if ([[metadata objectForKey:CLSyncEvents] count] > 0) {
                        
                    //            // override with sid sent by phil
                    //            NSString *newSid = [metadata objectForKey:CLSyncID];
                    //            if ([self.currentSIDs containsObject:newSid] == NO) {
                    //                [self.currentSIDs addObject:newSid];
                    //            }
                        
                    //            // add received events.
                    //            NSArray *mdsEvents = [metadata objectForKey:@"events"];
                    //            NSMutableArray *events = [NSMutableArray array];
                        
                    //            [mdsEvents enumerateObjectsUsingBlock:^(id mdsEvent, NSUInteger idx, BOOL *stop) {
                            
                    //                // if status is not found, metadata is null.
                    //                if (![[[mdsEvent objectForKey:@"sync_header"] objectForKey:@"status"] isEqualToString:@"not_found"]) { 
                    //                    // check for valid dictionary
                    //                    if ([[mdsEvent objectForKey:@"metadata"] isKindOfClass:[NSDictionary class]]){
                    //                        [events addObject:[CLEvent eventFromMDSEvent:mdsEvent]];
                    //                    }
                    //                }
                    //            }];
                        
                    //            // Dispatch for processing.
                    //            NSMutableDictionary *eventIds = [NSMutableDictionary dictionaryWithObjectsAndKeys:eid, CLSyncEventID, newSid, CLSyncID, nil];
                    //            [self performSyncOperationWithEvents:events withEventIDs:eventIds andOrigin:CLEventOriginMDS];
                    //        }
                    //    }else {
                    //        NSLog(@"%s - %@", __FUNCTION__, error);
                    //    }
                    //} onQueue:get_cloud_sync_queue()];

                    _restClient.SyncToCloud_WithCompletionHandler_OnQueue_Async(fsmEventsDictionary, (result) =>
                    {
                        if (result.Error == null)
                        {
                            Dictionary<string, object> metadata = (Dictionary<string, object>)result.JsonResult[CLDefinitions.CLSyncEventMetadata];
                    
                            _trace.writeToLog(1, "CLSyncService: SyncFromFileSystemMonitorWithGroupedUserEvents: Response From Sync To Cloud: {0}.", 
                                    result.JsonResult[CLDefinitions.CLSyncEventMetadata]);
                    
                            if (((List<CLEvent>)metadata[CLDefinitions.CLSyncEvents]).Count > 0) 
                            {
                                // Override with sid sent by server
                                string newSid = (string)metadata[CLDefinitions.CLSyncID];
                                if (!_currentSids.Contains(newSid))
                                {
                                    _currentSids.Add(newSid);
                                }
                        
                                // Add received events.
                                List<object> mdsEvents = (List<object>)metadata[CLDefinitions.CLSyncEvents];
                                List<CLEvent> eventsReceived = new List<CLEvent>();
                        
                                mdsEvents.ForEach(obj =>
                                {
                                    // If status is not found, metadata is null.
                                    Dictionary<string, object> mdsEventDictionary = (Dictionary<string, object>)obj;
                                    Dictionary<string, object> syncHeaderDictionary = (Dictionary<string, object>)mdsEventDictionary[CLDefinitions.CLSyncEventHeader];
                                    if (syncHeaderDictionary.ContainsKey(CLDefinitions.CLSyncEventStatus))
                                    {
                                        eventsReceived.Add(CLEvent.EventFromMDSEvent(mdsEventDictionary));
                                    }
                                });

                                // Dispatch for processing.
                                Dictionary<string, object> eventIds = new Dictionary<string, object>()
                                {
                                    {CLDefinitions.CLSyncEventID, eid.ToString()},
                                    {CLDefinitions.CLSyncID, newSid}
                                };

                                PerformSyncOperationWithEventsWithEventIDsAndOrigin(eventsReceived, eventIds, CLEventOrigin.CLEventOriginMDS);
                            }
                        }
                        else
                        {
                            _trace.writeToLog(1, "CLSyncService: SyncFromFileSystemMonitorWithGroupedUserEvents: ERROR {0}.", result.Error.errorDescription);
                        }
                    }, get_cloud_sync_queue());
                }
           }
        }

        // - (NSMutableArray *)updateMetadataForFileEvents:(NSMutableArray *)events
        List<CLEvent> UpdateMetadataForFileEvents(List<CLEvent> events)
        {
            events.ForEach(evt =>
            {
                //TODO: Implement this method.
#if TRASH
                NSDictionary metadata;
                Dictionary<string, object> metadata;

                if ((Myevent.Action).IsEqualToString(CLEventTypeAddFile) || (Myevent.Action).IsEqualToString(CLEventTypeModifyFile)) {
                    int do_try = 0;
                    do {
                        string fileSystemPath = ((CLSettings.SharedSettings()).CloudFolderPath()).StringByAppendingPathComponent(Myevent.Metadata.Path);
                        metadata = NSDictionary.AttributesForItemAtPath(fileSystemPath);
                        Myevent.Metadata.CreateDate = metadata.ObjectForKey(CLMetadataFileCreateDate);
                        Myevent.Metadata.ModifiedDate = metadata.ObjectForKey(CLMetadataFileModifiedDate);
                        Myevent.Metadata.Revision = metadata.ObjectForKey(CLMetadataFileRevision);
                        Myevent.Metadata.Hash = metadata.ObjectForKey(CLMetadataFileHash);
                        Myevent.Metadata.Size = metadata.ObjectForKey(CLMetadataFileSize);
                        do_try++;
                    }
                    while ((Myevent.Metadata.CreateDate).RangeOfString("190").Location != NSNotFound && do_try < 3000);  // 3 seconds.  TODO: This hack sucks!
                }

                 All other file events we get stored index data for this event item.
                if ((Myevent.Action).RangeOfString(CLEventTypeFileRange).Location != NSNotFound) {
                    string cloudPath = Myevent.Metadata.Path;
                    if ((Myevent.Action).IsEqualToString(CLEventTypeMoveFile) || (Myevent.Action).IsEqualToString(CLEventTypeRenameFile)) {
                        cloudPath = Myevent.Metadata.FromPath;
                    }

                    CLMetadata indexedMetadata = CLIndexingServices.MetadataForItemAtCloudPath(cloudPath);
                    if (indexedMetadata != null) {
                        // For add events, if the darn thing already exists in the index, it means that FSM failed to pick up the event as a modify.
                        // Let's make sure of that and if it turns out to be true, then we need to change the event to become a modify type.
                        if ((Myevent.Action).IsEqualToString(CLEventTypeAddFile)) {
                            if ((Myevent.Metadata.Hash).IsEqualToString(indexedMetadata.Hash) == false && (Myevent.Metadata.Revision).IsEqualToString(
                                indexedMetadata.Revision) == false) {
                                Myevent.Metadata.Revision = indexedMetadata.Revision;
                                Myevent.Action = CLEventTypeModifyFile;
                            }

                        }
                        else if ((Myevent.Action).IsEqualToString(CLEventTypeModifyFile)) {
                            Myevent.Metadata.Revision = indexedMetadata.Revision;
                        }
                        else {   // we want it all for all other cases.
                            Myevent.Metadata.Revision = indexedMetadata.Revision;
                            Myevent.Metadata.Hash = indexedMetadata.Hash;
                            Myevent.Metadata.CreateDate = indexedMetadata.CreateDate;
                            Myevent.Metadata.ModifiedDate = indexedMetadata.ModifiedDate;
                            Myevent.Metadata.Size = indexedMetadata.Size;
                        }

                    }

                }

                 check if this item is a symblink, if this object is a in fact a link, the utility method will return YES.
                /*NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:cloudPath];
                CLAppDelegate *appDelegate = [NSApp delegate];
                if ([appDelegate isFileAliasAtPath:fileSystemPath]) {
                     symblink events are always recognized as files, therefore simply replace the occurence of the word file with link in the event type.
                    event.action = [event.action stringByReplacingCharactersInRange:[event.action rangeOfString:CLEventTypeFileRange] withString:@"link"];
                } */
#endif // TRASH
            });
            //return events;
            return new List<CLEvent>();  //TODO: Replace this.
        }

        void NotificationServiceDidReceivePushNotificationFromServer(bool /*CLNotificationServices*/ ns, string notification)
        {
            //TODO: Implement this method
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
    
            //[self.restClient syncFromCloud:events completionHandler:^(NSDictionary *metadata, NSError *error) {
        
            //    if (error == nil) {
            
            //        NSLog(@"%s - Synced from cloud successfull with no objects returned.", __FUNCTION__);
            //        // get sync id.
            //        NSString *sid = [metadata objectForKey:CLSyncID]; // override with sid sent by phil
            
            //        if ([[metadata objectForKey:CLSyncEvents] count] > 0) {
                
            //            if ([self.currentSIDs containsObject:sid] == NO) {
            //                [self.currentSIDs addObject:sid];
            //            }
                
            //            NSLog(@"Current number of active SIDs: %lu" , [self.currentSIDs count]);
                
            //            NSArray *mdsEvents = [metadata objectForKey:@"events"];
            //            NSMutableArray *events = [NSMutableArray array];
                
            //            [mdsEvents enumerateObjectsUsingBlock:^(id mdsEvent, NSUInteger idx, BOOL *stop) {
            //                [events addObject:[CLEvent eventFromMDSEvent:mdsEvent]];
            //            }];
                
            //            NSLog(@"Response From Sync From Cloud: \n\n%@\n\n", metadata);
                
            //            NSDictionary *eventIds = [NSDictionary dictionaryWithObjectsAndKeys:eid, CLSyncEventID, sid, CLSyncID, nil];
                
            //            [self performSyncOperationWithEvents:events withEventIDs:eventIds andOrigin:CLEventOriginMDS];
                
            //        }else {
            //            // Update UI with activity.
            //            [self animateUIForSync:NO withStatusMessage:menuItemActivityLabelSynced syncActivityCount:0];
            //        }
            
            //    }else {
            
            //        NSLog(@"%s - %@", __FUNCTION__, error);
            //        // Update UI with activity.
            //        [self animateUIForSync:NO withStatusMessage:menuItemActivityLabelSynced syncActivityCount:0];
            //    }
        
            //    self.waitingForCloudResponse = NO;
        
            //    if (self.needSyncFromCloud == YES) {
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

            //            if ([self.currentSIDs containsObject:sid] == NO) {
            //                [self.currentSIDs addObject:sid];
            //            }

            //            NSLog(@"Current number of active SIDs: %lu" , [self.currentSIDs count]);

            //            NSArray *mdsEvents = [metadata objectForKey:@"events"];
            //            NSMutableArray *events = [NSMutableArray array];

            //            [mdsEvents enumerateObjectsUsingBlock:^(id mdsEvent, NSUInteger idx, BOOL *stop) {
            //                [events addObject:[CLEvent eventFromMDSEvent:mdsEvent]];
            //            }];

            //            NSLog(@"Response From Sync From Cloud: \n\n%@\n\n", metadata);

            //            NSDictionary *eventIds = [NSDictionary dictionaryWithObjectsAndKeys:eid, CLSyncEventID, sid, CLSyncID, nil];

            //            [self performSyncOperationWithEvents:events withEventIDs:eventIds andOrigin:CLEventOriginMDS];

            //        }else {
            //            // Update UI with activity.
            //            [self animateUIForSync:NO withStatusMessage:menuItemActivityLabelSynced syncActivityCount:0];
            //        }

            //    }else {

            //        NSLog(@"%s - %@", __FUNCTION__, error);
            //        // Update UI with activity.
            //        [self animateUIForSync:NO withStatusMessage:menuItemActivityLabelSynced syncActivityCount:0];
            //    }

            //    self.waitingForCloudResponse = NO;

            //    if (self.needSyncFromCloud == YES) {
            //        [self notificationService:nil didReceivePushNotificationFromServer:nil];
            //    }
            //} onQueue:get_cloud_sync_queue()];
            _restClient.SyncFromCloud_WithCompletionHandler_OnQueue_Async(events, (result) =>
            {
                if (result.Error == null)
                {
                    Dictionary<string, object> metadata = (Dictionary<string, object>)result.JsonResult[CLDefinitions.CLSyncEventMetadata];
                    
                    _trace.writeToLog(1, "CLSyncService: SyncFromFileSystemMonitorWithGroupedUserEvents: Response From Sync From Cloud: {0}.", 
                            result.JsonResult[CLDefinitions.CLSyncEventMetadata]);
                    
                    // get sync id.
                    // NSString *sid = [metadata objectForKey:CLSyncID]; // override with sid sent by server
                    string sidInner = (string)metadata[CLDefinitions.CLSyncID];

                    // if ([[metadata objectForKey:CLSyncEvents] count] > 0) {
                    if (((Dictionary<string, object>)metadata[CLDefinitions.CLSyncEvents]).Count() > 0)
                    {
                        // if ([self.currentSIDs containsObject:sid] == NO) {
                        //      [self.currentSIDs addObject:sid];
                        // }
                        if (!_currentSids.Contains(sidInner))
                        {
                            _currentSids.Add(sidInner);
                        }

                        _trace.writeToLog(9, "Current number of active SIDs: {0}.", _currentSids.Count());

                        // NSArray *mdsEvents = [metadata objectForKey:@"events"];
                        // NSMutableArray *events = [NSMutableArray array];
                        List<Dictionary<string, object>> mdsEvents = (List<Dictionary<string, object>>)metadata[CLDefinitions.CLSyncEvents];
                        List<CLEvent> eventsInner = new List<CLEvent>();

                        // [mdsEvents enumerateObjectsUsingBlock:^(id mdsEvent, NSUInteger idx, BOOL *stop) {
                        //     [events addObject:[CLEvent eventFromMDSEvent:mdsEvent]];
                        // }];
                        mdsEvents.ForEach(mdsEvent =>
                        {
                            eventsInner.Add(CLEvent.EventFromMDSEvent(mdsEvent));
                        });

                        _trace.writeToLog(9, "Response From Sync From Cloud: {0}.", metadata);

                        // NSDictionary *eventIds = [NSDictionary dictionaryWithObjectsAndKeys:eid, CLSyncEventID, sid, CLSyncID, nil];
                        Dictionary<string, object> eventIds = new Dictionary<string,object>()
                        {
                            {CLDefinitions.CLSyncEventID, eid},
                            {CLDefinitions.CLSyncID, sidInner}
                        };

                        // [self performSyncOperationWithEvents:events withEventIDs:eventIds andOrigin:CLEventOriginMDS];
                        PerformSyncOperationWithEventsWithEventIDsAndOrigin(eventsInner, eventIds, CLEventOrigin.CLEventOriginMDS);
                    }
                    else
                    {
                        // Update UI with activity.
                        // [self animateUIForSync:NO withStatusMessage:menuItemActivityLabelSynced syncActivityCount:0];
                        //TODO: Implement this.
                    }
                }
                else
                {
                    _trace.writeToLog(1, "CLSyncService: SyncFromFileSystemMonitorWithGroupedUserEvents: ERROR {0}.", result.Error.errorDescription);
                }
            }, get_cloud_sync_queue());
        }

        // - (void)performSyncOperationWithEvents:(NSArray *)events withEventIDs:(NSDictionary *)ids andOrigin:(CLEventOrigin)origin
        void PerformSyncOperationWithEventsWithEventIDsAndOrigin(List<CLEvent> events, Dictionary<string, object> ids, CLEventOrigin origin)
        {
            //TODO: Implement this function.
            //NSLog(@"%s", __FUNCTION__);
        
            //// Update UI with activity.
            //[self animateUIForSync:YES withStatusMessage:menuItemActivityLabelIndexing syncActivityCount:0];
    
            //// Sorting, bitches.
            //NSDictionary *sortedEvents = [self sortSyncEventsByType:events];
    
            //// Preprocess Index 
            //sortedEvents = [self indexSortedEvents:sortedEvents];

            ///* Process Sync Events */
    
            //// Adding objects to our active sync queue
            //[self.activeSyncQueue addObjectsFromArray:[sortedEvents objectForKey:CLEventTypeAdd]];
            //[self.activeSyncQueue addObjectsFromArray:[sortedEvents objectForKey:CLEventTypeModify]];
            //[self.activeSyncQueue addObjectsFromArray:[sortedEvents objectForKey:CLEventTypeRenameMove]];
            //[self.activeSyncQueue addObjectsFromArray:[sortedEvents objectForKey:CLEventTypeDelete]];

            //// Get separated file and folder events
            //self.activeSyncFolderQueue = [self separateFolderFromFileForActiveEvents:self.activeSyncQueue wantsFolderEvents:YES];
            //self.activeSyncFileQueue = [self separateFolderFromFileForActiveEvents:self.activeSyncQueue wantsFolderEvents:NO];
    
            //// Get total object count in sync queue
            //self.syncItemsQueueCount =  self.syncItemsQueueCount + [self.activeSyncQueue count];
    
            //// Update UI with sync activity
            //[self animateUIForSync:YES withStatusMessage:menuItemActivityLabelSyncing syncActivityCount:self.syncItemsQueueCount];
    
            //// Delete Files and Folders
            //[self processDeleteSyncEvents:[sortedEvents objectForKey:CLEventTypeDelete]];
    
            //// Add Folders
            //[self processAddFolderSyncEvents:[sortedEvents objectForKey:CLEventTypeAdd]];
        
            //// Rename/Move Folders
            //[self processRenameMoveFolderSyncEvents:[sortedEvents objectForKey:CLEventTypeRenameMove]];
    
            //// Add Files
            //[self processAddFileSyncEvents:[sortedEvents objectForKey:CLEventTypeAdd]];
    
            //// Modify Files
            //[self processModifyFileSyncEvents:[sortedEvents objectForKey:CLEventTypeModify]];
    
            //// Rename/Move Files
            //[self processRenameMoveFileSyncEvents:[sortedEvents objectForKey:CLEventTypeRenameMove]];

            //// Add Links
            //[self processAddLinkSyncEvents:[sortedEvents objectForKey:CLEventTypeAdd]];
    
            //// Modify Links
            //[self processModifyLinkSyncEvents:[sortedEvents objectForKey:CLEventTypeModify]];
    
            //// Rename/Move Links
            //[self processRenameMoveLinkSyncEvents:[sortedEvents objectForKey:CLEventTypeRenameMove]];

            //// Updating index.
            //[self updateIndexForActiveSyncEvents];

            //// Display user notification
            //[[CLUIActivityService sharedService] displayUserNotificationForSyncEvents:self.activeSyncQueue];
    
            //// Remove active items from queue (if any left)
            //[self.activeSyncQueue removeAllObjects];
    
            //// Sync finished.
            //[self saveSyncStateWithSID:[ids objectForKey:CLSyncID] andEID:[ids objectForKey:CLSyncEventID]];
    
            //// Update UI with activity.
            //[self animateUIForSync:NO withStatusMessage:menuItemActivityLabelSynced syncActivityCount:0];

            //&&&&
            _trace.writeToLog(9, "PerformSyncOperationWithEventsWithEventIDsAndOrigin: Entry.");

            // Update UI with activity.
            // [self animateUIForSync:YES withStatusMessage:menuItemActivityLabelIndexing syncActivityCount:0];
            //TODO: Implement this.

            // Sorting, bitches.
            // NSDictionary *sortedEvents = [self sortSyncEventsByType:events];
            Dictionary<string, object> sortedEvents = SortSyncEventsByType(events);

            // Preprocess Index 
            // sortedEvents = [self indexSortedEvents:sortedEvents];
            sortedEvents = IndexSortedEvents(sortedEvents);

            ///* Process Sync Events */

            //// Adding objects to our active sync queue
            //[self.activeSyncQueue addObjectsFromArray:[sortedEvents objectForKey:CLEventTypeAdd]];
            //[self.activeSyncQueue addObjectsFromArray:[sortedEvents objectForKey:CLEventTypeModify]];
            //[self.activeSyncQueue addObjectsFromArray:[sortedEvents objectForKey:CLEventTypeRenameMove]];
            //[self.activeSyncQueue addObjectsFromArray:[sortedEvents objectForKey:CLEventTypeDelete]];
            _activeSyncQueue.AddRange((List<CLEvent>)sortedEvents[CLDefinitions.CLEventTypeAdd]);
            _activeSyncQueue.AddRange((List<CLEvent>)sortedEvents[CLDefinitions.CLEventTypeModify]);
            _activeSyncQueue.AddRange((List<CLEvent>)sortedEvents[CLDefinitions.CLEventTypeRenameMove]);
            _activeSyncQueue.AddRange((List<CLEvent>)sortedEvents[CLDefinitions.CLEventTypeDelete]);

            // Get separated file and folder events
            // self.activeSyncFolderQueue = [self separateFolderFromFileForActiveEvents:self.activeSyncQueue wantsFolderEvents:YES];
            // self.activeSyncFileQueue = [self separateFolderFromFileForActiveEvents:self.activeSyncQueue wantsFolderEvents:NO];
            _activeSyncFolderQueue = SeparateFolderFromFileForActiveEvents_WantsFolderEvents(_activeSyncFolderQueue, wantsFolderEvents: true);
            _activeSyncFileQueue = SeparateFolderFromFileForActiveEvents_WantsFolderEvents(_activeSyncFolderQueue, wantsFolderEvents: false);

            // Get total object count in sync queue
            // self.syncItemsQueueCount =  self.syncItemsQueueCount + [self.activeSyncQueue count];
            _syncItemsQueueCount = _syncItemsQueueCount + _activeSyncQueue.Count();

            // Update UI with sync activity
            // [self animateUIForSync:YES withStatusMessage:menuItemActivityLabelSyncing syncActivityCount:self.syncItemsQueueCount];
            //TODO: Implement this. 

            // Delete Files and Folders
            // [self processDeleteSyncEvents:[sortedEvents objectForKey:CLEventTypeDelete]];
            ProcessDeleteSyncEvents((List<CLEvent>)sortedEvents[CLDefinitions.CLEventTypeDelete]);

            // Add Folders
            // [self processAddFolderSyncEvents:[sortedEvents objectForKey:CLEventTypeAdd]];
            ProcessAddFolderSyncEvents((List<CLEvent>)sortedEvents[CLDefinitions.CLEventTypeAdd]);

            // Rename/Move Folders
            // [self processRenameMoveFolderSyncEvents:[sortedEvents objectForKey:CLEventTypeRenameMove]];
            ProcessRenameMoveFolderSyncEvents((List<CLEvent>)sortedEvents[CLDefinitions.CLEventTypeRenameMove]);

            // Add Files
            // [self processAddFileSyncEvents:[sortedEvents objectForKey:CLEventTypeAdd]];
            ProcessAddFileSyncEvents(sortedEvents[CLDefinitions.CLEventTypeAdd]);

            //// Modify Files
            //[self processModifyFileSyncEvents:[sortedEvents objectForKey:CLEventTypeModify]];

            //// Rename/Move Files
            //[self processRenameMoveFileSyncEvents:[sortedEvents objectForKey:CLEventTypeRenameMove]];

            //// Add Links
            //[self processAddLinkSyncEvents:[sortedEvents objectForKey:CLEventTypeAdd]];

            //// Modify Links
            //[self processModifyLinkSyncEvents:[sortedEvents objectForKey:CLEventTypeModify]];

            //// Rename/Move Links
            //[self processRenameMoveLinkSyncEvents:[sortedEvents objectForKey:CLEventTypeRenameMove]];

            //// Updating index.
            //[self updateIndexForActiveSyncEvents];

            //// Display user notification
            //[[CLUIActivityService sharedService] displayUserNotificationForSyncEvents:self.activeSyncQueue];

            //// Remove active items from queue (if any left)
            //[self.activeSyncQueue removeAllObjects];

            //// Sync finished.
            //[self saveSyncStateWithSID:[ids objectForKey:CLSyncID] andEID:[ids objectForKey:CLSyncEventID]];

            //// Update UI with activity.
            //[self animateUIForSync:NO withStatusMessage:menuItemActivityLabelSynced syncActivityCount:0];

        }

        //- (void)processAddFileSyncEvents:(NSArray *)events
        void ProcessAddFileSyncEvents(List<CLEvent> events)
        {
            //    NSLog(@"%s", __FUNCTION__);
            
            //    // Break down upload and downloads
            //    NSMutableArray *uploadEvents = [NSMutableArray array];
            //    NSMutableArray *downloadEvents = [NSMutableArray array];
    
            //    // Add File events.
            //    [events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
        
            //        CLEvent *event = obj;
        
            //        if (event.isMDSEvent) {
            //            NSString *actionType = event.syncHeader.action;
            //            NSString *status = event.syncHeader.status;
            
            //            // file events only.
            //            if ([actionType isEqualToString:CLEventTypeAddFile]) {
                
            //                if (status == nil) { // MDS origin, Philis told us we need to do this.
                    
            //                    // we need to download this file.
            //                    [downloadEvents addObject:event];
                    
            //                } else { //FSM origin, we created this file, need to check for upload.
                    
            //                    if ([status isEqualToString:CLEventTypeUpload] || [status isEqualToString:CLEventTypeUploading]) { // we need to upload this file.
                        
            //                        [uploadEvents addObject:event];
            //                    }
                    
            //                    if ([status isEqualToString:CLEventTypeExists] || [status isEqualToString:CLEventTypeDuplicate]) { // we do not need to upload this file.
                        
            //                        // update ui.
            //                        [self performUpdateForSyncEvent:event success:YES];
            //                    }
                    
            //                    if ([status isEqualToString:CLEventTypeConflict]) {
                        
            //                        // TODO: handle conflict here.
                        
            //                        // update ui.
            //                        [self performUpdateForSyncEvent:event success:YES];
            //                    }
            //                }
            //            }
            //        }
            //    }];
    
            //    // execute upload and download events.
            //    if ([uploadEvents count] > 0) {
            //        [self dispatchUploadEvents:uploadEvents];
            //    }
    
            //    if ([downloadEvents count] > 0) {
            //        [self dispatchDownloadEvents:downloadEvents];
            //    }

            //&&&&

            _trace.writeToLog(9, "CLSyncService: ProcessAddFileSyncEvents: Entry.");

            // Break down upload and downloads
            // NSMutableArray *uploadEvents = [NSMutableArray array];
            // NSMutableArray *downloadEvents = [NSMutableArray array];
            List<CLEvent> uploadEvents = new List<CLEvent>();
            List<CLEvent> downloadEvents = new List<CLEvent>();

            // Add File events.
            // [events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
            events.ForEach(obj =>
            {
                // CLEvent *event = obj;
                CLEvent evt = obj;

                if (evt.IsMDSEvent)
                {
                    // NSString *actionType = event.syncHeader.action;
                    // NSString *status = event.syncHeader.status;
                    string actionType = evt.SyncHeader.Action;
                    string status = evt.SyncHeader.Status;

                    // File events only.
                    // if ([actionType isEqualToString:CLEventTypeAddFile]) {
                    if (actionType.Equals(CLDefinitions.CLEventTypeAddFile, StringComparison.InvariantCulture))
                    {
                        if (status == null)                    // MDS origin, Philis told us we need to do this.
                        {

                            // We need to download this file.
                            // [downloadEvents addObject:event];

                        }
                        else
                        {                                       //FSM origin, we created this file, need to check for upload.
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
                                PerformUpdateForSyncEventSuccess(evt, success: true);
                            }

                            // if ([status isEqualToString:CLEventTypeConflict]) {
                            if (status.Equals(CLDefinitions.CLEventTypeConflict))
                            {
                                // TODO: handle conflict here.
                                // Update ui.
                                // [self performUpdateForSyncEvent:event success:YES];
                                PerformUpdateForSyncEventSuccess(evt, success: true);
                            }

                        }
                    }
                }
            });

            // Execute upload and download events.
            // if ([uploadEvents count] > 0) {
            //     [self dispatchUploadEvents:uploadEvents];
            // }
            if (uploadEvents.Count() > 0)
            {
                DispatchUploadEvents(uploadEvents);
            }

            // if ([downloadEvents count] > 0) {
            //    [self dispatchDownloadEvents:downloadEvents];
            // }
            if (downloadEvents.Count() > 0)
            {
                DispatchDownloadEvents(downloadEvents);
            }

        }

        //- (void)processRenameMoveFolderSyncEvents:(NSArray *)events
        void ProcessRenameMoveFolderSyncEvents(List<CLEvent> events)
        {
            //    NSLog(@"%s", __FUNCTION__);
    
            //    // Rename/Move Folders events.
            //    [events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
        
            //        CLEvent *event = obj;
            //        NSString *actionType = event.syncHeader.action;
            //        NSString *status = event.syncHeader.status;
            //        NSString *toPath = event.metadata.toPath;
            //        NSString *fromPath = event.metadata.fromPath;
        
            //        // folder events first.
            //        if ([actionType rangeOfString:CLEventTypeFolderRange].location != NSNotFound) {
            
            //            if ([actionType rangeOfString:CLEventTypeRenameRange].location != NSNotFound ||
            //                [actionType rangeOfString:CLEventTypeMoveRange].location != NSNotFound ) {
                
            //                BOOL success = YES; // assume true for events we originated (since they already happened), override value for MDS execution.
                
            //                if (status == nil) { // MDS origin, Philis told us we need to do this.
            //                    success = [[CLFSDispatcher defaultDispatcher] moveItemAtPath:fromPath to:toPath error:nil];
            //                }
                
            //                // update ui.
            //                [self performUpdateForSyncEvent:event success:success];
            //            }
            //        }
            //    }];

            //&&&&
            _trace.writeToLog(9, "CLSyncService: ProcessRenameMoveFolderSyncEvents: Entry.");

            // Rename/Move Folders events.
            //    [events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
            events.ForEach(obj =>
            {
                // CLEvent *event = obj;
                CLEvent evt = obj;

                // NSString *actionType = event.syncHeader.action;
                // NSString *status = event.syncHeader.status;
                // NSString *toPath = event.metadata.toPath;
                // NSString *fromPath = event.metadata.fromPath;
                string actionType = evt.SyncHeader.Action;
                string status = evt.SyncHeader.Status;
                string toPath = evt.Metadata.ToPath;
                string fromPath = evt.Metadata.FromPath;

                // Folder events first.
                // if ([actionType rangeOfString:CLEventTypeFolderRange].location != NSNotFound) {
                if (actionType.Contains(CLDefinitions.CLEventTypeFolderRange))
                {

                    // if ([actionType rangeOfString:CLEventTypeRenameRange].location != NSNotFound ||
                    //          [actionType rangeOfString:CLEventTypeMoveRange].location != NSNotFound ) {
                    if (actionType.Contains(CLDefinitions.CLEventTypeRenameRange) ||
                        actionType.Contains(CLDefinitions.CLEventTypeMoveRange))
                    {

                        // BOOL success = YES; // assume true for events we originated (since they already happened), override value for MDS execution.
                        bool success = true;

                        // if (status == nil) { // MDS origin, Philis told us we need to do this.
                        //     success = [[CLFSDispatcher defaultDispatcher] moveItemAtPath:fromPath to:toPath error:nil];
                        // }
                        if (status == null)
                        {
                            CLError error = null;
                            success = CLFSDispatcher.Instance.MoveItemAtPath(fromPath, toPath, out error);
                        }

                        // update ui.
                        // [self performUpdateForSyncEvent:event success:success];
                        PerformUpdateForSyncEventSuccess(evt, success);
                    }
                }
            });

        }

        //- (void)processAddFolderSyncEvents:(NSArray *)events
        void ProcessAddFolderSyncEvents(List<CLEvent> events)
        {
            //NSLog(@"%s", __FUNCTION__);

            //// Add Folder events.
            //[events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
        
            //    CLEvent *event = obj;
            //    NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:event.metadata.path];
            //    if (event.isMDSEvent) {
            //        NSString *actionType = event.syncHeader.action;
            //        NSString *status = event.syncHeader.status;
            
            //        // folder events only.
            //        if ([actionType isEqualToString:CLEventTypeAddFolder]) {
                
            //            BOOL success = YES; // assume true for events we originated (since they already happened), override value for MDS execution.
            //            BOOL createdAttributes = NO;

            //            if (status == nil) { // MDS origin, Philis told us we need to do this.

            //                success = [[CLFSDispatcher defaultDispatcher] createDirectoryAtPath:event.metadata.path error:nil];

            //                if (success == NO) {

            //                    // TODO: check error here and try to remediate.
            //                }
            //                else {
                        
            //                    [self badgeFileAtCloudPath:event.metadata.path withBadge:cloudAppBadgeSyncing]; // mark folder as syncing

            //                    NSError *attributesError;
            //                    createdAttributes = [[CLFSDispatcher defaultDispatcher] updateAttributesUsingMetadata:event.metadata forItemAtPath:fileSystemPath error:&attributesError];
            //                    if (attributesError) {
            //                        NSLog(@"%s - %@", __FUNCTION__, [attributesError description]);
            //                    }
            //                }
            //            }
                
            //            // update ui.
            //            [self performUpdateForSyncEvent:event success:success];
            //        }
            //    } 
            //}];

            //&&&&&&

            _trace.writeToLog(9, "ProcessAddFolderSyncEvents: Entry.");

            // Add Folder events.
            // [events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
            events.ForEach(obj =>
            {
                // CLEvent *event = obj;
                CLEvent evt = obj;

                // NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:event.metadata.path];
                string fileSystemPath = Settings.Instance.CloudFolderPath + evt.Metadata.Path;

                if (evt.IsMDSEvent)
                {
                    // NSString *actionType = event.syncHeader.action;
                    // NSString *status = event.syncHeader.status;
                    string actiontype = evt.SyncHeader.Action;
                    string status = evt.SyncHeader.Status;

                    // Folder events only.
                    // if ([actionType isEqualToString:CLEventTypeAddFolder]) {
                    if (actiontype.Equals(CLDefinitions.CLEventTypeAddFolder, StringComparison.InvariantCulture))
                    {
                        // BOOL success = YES; // assume true for events we originated (since they already happened), override value for MDS execution.
                        // BOOL createdAttributes = NO;
                        bool success = true;    // assume true for events we originated (since they already happened), override value for MDS execution.
                        bool createdAttributes = false;

                        // if (status == nil) { // MDS origin, Philis told us we need to do this.
                        if (status == null)
                        {
                            // success = [[CLFSDispatcher defaultDispatcher] createDirectoryAtPath:event.metadata.path error:nil];
                            Directory.CreateDirectory(evt.Metadata.Path);
                            //TODO: The above CreateDirectory should set the success variable, and we should handle failures.
                            //TODO: We should also handle exceptions.
                            success = true;
                            if (!success)
                            {
                                // TODO: check error here and try to remediate.
                            }
                            else {

                                // [self badgeFileAtCloudPath:event.metadata.path withBadge:cloudAppBadgeSyncing]; // mark folder as syncing
                                //TODO: Implement this.

                                // NSError *attributesError;
                                // createdAttributes = [[CLFSDispatcher defaultDispatcher] updateAttributesUsingMetadata:event.metadata forItemAtPath:fileSystemPath error:&attributesError];
                                // if (attributesError) {
                                //     NSLog(@"%s - %@", __FUNCTION__, [attributesError description]);
                                // }
                                //TODO: Implement this.
                            }
                        }

                        // update ui.
                        // [self performUpdateForSyncEvent:event success:success];
                        //TODO: Implement this.
                    }
                } 
            });
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

        void UpdateIndexForActiveSyncEvents()
        {
            //TODO: Implement this method.
            //[self animateUIForSync:YES withStatusMessage:menuItemActivityLabelIndexing syncActivityCount:0];

            //[[self.activeSyncQueue copy] enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
            //    CLEvent *event = obj;
        
            //    if ([event.syncHeader.action rangeOfString:CLEventTypeAddRange].location != NSNotFound) { // Add events, new items to index.
            //        [CLIndexingServices addMetedataItem:event.metadata pending:NO];
            //    }
        
            //    if ([event.syncHeader.action rangeOfString:CLEventTypeDeleteRange].location != NSNotFound) { // Delete events, remove item from index.
            //        [CLIndexingServices removeMetadataItemWithCloudPath:event.metadata.path];
            //    }
        
            //    if ([event.syncHeader.action rangeOfString:CLEventTypeMoveRange].location != NSNotFound ||
            //        [event.syncHeader.action rangeOfString:CLEventTypeRenameRange].location != NSNotFound ||
            //        [event.syncHeader.action isEqualToString:CLEventTypeModifyFile]) { // Move/Rename/Modify events, update existing items to index.
            //        [CLIndexingServices updateLocalIndexItemWithEvent:event pending:NO];
            //    }
            //}];
    
            //if ([[NSManagedObjectContext defaultContext] hasChanges]){
            //    [CLIndexingServices saveDataInContext:[NSManagedObjectContext defaultContext]];
            //}


        }

        void processAddFileSyncEvents(Array /*NSArray*/ events)
        {
            //TODO: Implement this function.
            //NSLog(@"%s", __FUNCTION__);
            
            //// Break down upload and downloads
            //NSMutableArray *uploadEvents = [NSMutableArray array];
            //NSMutableArray *downloadEvents = [NSMutableArray array];
    
            //// Add File events.
            //[events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
        
            //    CLEvent *event = obj;
        
            //    if (event.isMDSEvent) {
            //        NSString *actionType = event.syncHeader.action;
            //        NSString *status = event.syncHeader.status;
            
            //        // file events only.
            //        if ([actionType isEqualToString:CLEventTypeAddFile]) {
                
            //            if (status == nil) { // MDS origin, Philis told us we need to do this.
                    
            //                // we need to download this file.
            //                [downloadEvents addObject:event];
                    
            //            } else { //FSM origin, we created this file, need to check for upload.
                    
            //                if ([status isEqualToString:CLEventTypeUpload] || [status isEqualToString:CLEventTypeUploading]) { // we need to upload this file.
                        
            //                    [uploadEvents addObject:event];
            //                }
                    
            //                if ([status isEqualToString:CLEventTypeExists] || [status isEqualToString:CLEventTypeDuplicate]) { // we do not need to upload this file.
                        
            //                    // update ui.
            //                    [self performUpdateForSyncEvent:event success:YES];
            //                }
                    
            //                if ([status isEqualToString:CLEventTypeConflict]) {
                        
            //                    // TODO: handle conflict here.
                        
            //                    // update ui.
            //                    [self performUpdateForSyncEvent:event success:YES];
            //                }
            //            }
            //        }
            //    }
            //}];
    
            //// execute upload and download events.
            //if ([uploadEvents count] > 0) {
            //    [self dispatchUploadEvents:uploadEvents];
            //}
    
            //if ([downloadEvents count] > 0) {
            //    [self dispatchDownloadEvents:downloadEvents];
            //}

        }

        void processModifyFileSyncEvents(Array events)
        {
            //TODO: Implement this function.
            //NSLog(@"%s", __FUNCTION__);
    
            //// Break down upload and downloads
            //NSMutableArray *uploadEvents = [NSMutableArray array];
            //NSMutableArray *downloadEvents = [NSMutableArray array];
    
            //// Modify File events.
            //[events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
        
            //    CLEvent *event = obj;
            //    NSString *actionType = event.syncHeader.action;
            //    NSString *status = event.syncHeader.status;

            //    // folder events first.
            //    if ([actionType isEqualToString:CLEventTypeModifyFile]) {
            
            //        if (status == nil) { // MDS origin, Philis told us we need to do this.
                
            //            // we need to download this file.
            //            [downloadEvents addObject:event];
            //            [self badgeFileAtCloudPath:event.metadata.path withBadge:cloudAppBadgeSyncing]; // mark the file to be uploaded as syncing.

            //        } else { //FSM origin, we modified this file, need to check for upload.
                
            //            if ([status isEqualToString:CLEventTypeUpload] || [status isEqualToString:CLEventTypeUploading]) { // we need to upload this file.
                    
            //                [uploadEvents addObject:event];
            //            }
                
            //            if ([status isEqualToString:CLEventTypeExists] || [status isEqualToString:CLEventTypeDuplicate] || [status isEqualToString:CLEventTypeUploading]) { // we do not need to upload this file.
                    
            //                // update ui.
            //                [self performUpdateForSyncEvent:event success:YES];
            //            }
                
            //            if ([status isEqualToString:CLEventTypeConflict]) {
                    
            //                // TODO: handle conflict here.

            //                // update ui.
            //                [self performUpdateForSyncEvent:event success:YES];
            //            }
            //        }
            //    }
            //}];
    
            //// execute upload and download events.
            //if ([uploadEvents count] > 0) {
            //    [self dispatchUploadEvents:uploadEvents];
            //}
    
            //if ([downloadEvents count] > 0) {
        
            //    // sorting downloads by size (ascending)
            //    NSArray *sortedDownloadEvents = [downloadEvents sortedArrayUsingComparator: ^(CLEvent * event1, CLEvent *event2) {
            
            //        if ([event1.metadata.size intValue] > [event2.metadata.size intValue]) {
            //            return (NSComparisonResult)NSOrderedDescending;
            //        }
            
            //        if ([event1.metadata.size intValue] < [event2.metadata.size intValue]) {
            //            return (NSComparisonResult)NSOrderedAscending;
            //        }
            //        return (NSComparisonResult)NSOrderedSame;
            //    }];
        
            //    [self dispatchDownloadEvents:sortedDownloadEvents];
            //}
        }

        void processRenameMoveFileSyncEvents(Array events)
        {
            //TODO: Implement this function.
            //NSLog(@"%s", __FUNCTION__);
        
            //// Rename/Move File events.
            //[events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
        
            //    CLEvent *event = obj;
            //    NSString *actionType = event.syncHeader.action;
            //    NSString *status = event.syncHeader.status;
            //    NSString *toPath = event.metadata.toPath;
            //    NSString *fromPath = event.metadata.fromPath;

            //    // File events only.
            //    if ([actionType rangeOfString:CLEventTypeFileRange].location != NSNotFound) {
            
            //        if ([actionType rangeOfString:CLEventTypeRenameRange].location != NSNotFound ||
            //            [actionType rangeOfString:CLEventTypeMoveRange].location != NSNotFound ) {
                
            //            BOOL success = YES; // assume true for events we originated (since they already happened), override value for MDS execution.
                
            //            if (status == nil) { // MDS origin, Philis told us we need to do this.
            //                success = [[CLFSDispatcher defaultDispatcher] moveItemAtPath:fromPath to:toPath error:nil];
            //            }
                
            //            // update ui.
            //            [self performUpdateForSyncEvent:event success:success];
            //        }
            //    }
            //}];
        }

        void ProcessDeleteSyncEvents(List<CLEvent> events)
        {
            //TODO: Implement this.
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

        void DispatchUploadEvents(List<CLEvent> events)
        {
            //NSLog(@"%s", __FUNCTION__);
            //NSMutableArray *operations = [NSMutableArray array];
    
            //if (self.uploadOperationQueue == nil) {
            //    self.uploadOperationQueue = [[CLOperationQueue alloc] init];
            //    self.uploadOperationQueue.maxConcurrentOperationCount = 6;
            //}
    
            //NSLog(@"Number of uploads to start: %lu", [events count]);
    
            //__block NSInteger totalExpectedUploadBytes = 0;
            //__block NSInteger totalUploadedBytes = 0;
            //__block NSTimeInterval start;

            //[events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
        
            //    CLEvent *event = obj;
            //    NSString *path = event.metadata.path;
            //    NSString *storageKey = event.metadata.storage_key;
            //    NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:path];
        
            //    totalExpectedUploadBytes = totalExpectedUploadBytes +[event.metadata.size integerValue];

            //    NSLog(@"File to be uploaded: %@, Storage Key: %@", path, storageKey);
        
            //    __block CLHTTPConnectionOperation *uploadOperation = [self.restClient streamingUploadOperationForStorageKey:storageKey withFileSystemPath:fileSystemPath fileSize:event.metadata.size andMD5Hash:event.metadata.hash];
        
            //    [uploadOperation setUploadProgressBlock:^(NSInteger bytesWritten, NSInteger totalBytesWritten, NSInteger totalBytesExpectedToWrite) {
            
            //        totalUploadedBytes = totalUploadedBytes + bytesWritten;
            
            
            //        NSTimeInterval now = [NSDate timeIntervalSinceReferenceDate];
            //        double elapsedSeconds = now - start;
            //        double secondsLeft = (((double)totalExpectedUploadBytes - (double)totalUploadedBytes) / ((double)totalUploadedBytes / elapsedSeconds));
            //        double progress = (double)totalUploadedBytes / (double)totalExpectedUploadBytes;
            
            //        //NSLog(@"Sent %ld of %ld bytes - Progress: %f", totalUploadedBytes, totalExpectedUploadBytes, progress);
            
            //        [[CLUIActivityService sharedService] updateActivityViewWithProgress:progress
            //                                                                    timeLeft:secondsLeft
            //                                                                      bytes:(double)totalUploadedBytes
            //                                                               ofTotalBytes:(double)totalExpectedUploadBytes
            //                                                                  fileCount:[self.uploadOperationQueue operationCount]
            //                                                            andActivityType:activityViewLabelUpload];
            //    }];
        
            //    [uploadOperation setOperationCompletionBlock:^(CLHTTPConnectionOperation *operation, id responseObject, NSError *error) {
            
            //        NSLog(@"Upload Status: %li", [operation.response statusCode]);
            
            //        if ([operation.response statusCode] == 201) {
                
            //            NSLog(@"Upload Completed for File: %@", path);
            //            NSLog(@"Opperations remaining: %lu", [[self.uploadOperationQueue operations] count]);
            //            // update index and ui.
            //            [self performUpdateForSyncEvent:event success:YES];
                
            //        } else if ([operation.response statusCode] == 304){
                
            //            NSLog(@"The file already exists on the server");
            //            // update index and ui.
            //            [self performUpdateForSyncEvent:event success:YES];
                
            //        }else {
            //            NSLog(@"Upload Failed with status:%li for File: %@",[operation.response statusCode], path);
            //        }
            
            //        if (error) {
                
            //            // Error handler (back processor). Likely to happen due to network interruptions.
            //            // TODO: Handle the upload failure -- for now update the index to not pending.. we need to handle the error!!
            //            NSLog(@"Failed to Upload File: %@. Error: %@, Code: %ld", path, [error localizedDescription], [error code]);
            //            NSLog(@"Opperations remaining: %lu", [[self.uploadOperationQueue operations] count]);
                
            //            // update index and ui.
            //            [self performUpdateForSyncEvent:event success:NO];
            //        }
            
            //        if ([self.uploadOperationQueue operationCount] <= 0) {
                
            //            [[CLUIActivityService sharedService] updateActivityViewWithProgress:1 timeLeft:0 bytes:0 ofTotalBytes:0 fileCount:0 andActivityType:activityViewLabelSynced];
            //        }
            
            //    }];
        
            //    [operations addObject:uploadOperation];
            //}];
    
            //NSLog(@"Starting Upload Operarions");
            //start = [NSDate timeIntervalSinceReferenceDate];
            //[self.uploadOperationQueue addOperations:operations waitUntilFinished:YES];
            //NSLog(@"Finished Upload Operarions");

            //&&&&&&&&&&&&&
            _trace.writeToLog(9," CLSyncService: DispatchUploadEvents: EntryPointNotFoundException.");
            //NSMutableArray *operations = [NSMutableArray array];

            //if (self.uploadOperationQueue == nil) {
            //    self.uploadOperationQueue = [[CLOperationQueue alloc] init];
            //    self.uploadOperationQueue.maxConcurrentOperationCount = 6;
            //}

            //NSLog(@"Number of uploads to start: %lu", [events count]);

            //__block NSInteger totalExpectedUploadBytes = 0;
            //__block NSInteger totalUploadedBytes = 0;
            //__block NSTimeInterval start;

            //[events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {

            //    CLEvent *event = obj;
            //    NSString *path = event.metadata.path;
            //    NSString *storageKey = event.metadata.storage_key;
            //    NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:path];

            //    totalExpectedUploadBytes = totalExpectedUploadBytes +[event.metadata.size integerValue];

            //    NSLog(@"File to be uploaded: %@, Storage Key: %@", path, storageKey);

            //    __block CLHTTPConnectionOperation *uploadOperation = [self.restClient streamingUploadOperationForStorageKey:storageKey withFileSystemPath:fileSystemPath fileSize:event.metadata.size andMD5Hash:event.metadata.hash];

            //    [uploadOperation setUploadProgressBlock:^(NSInteger bytesWritten, NSInteger totalBytesWritten, NSInteger totalBytesExpectedToWrite) {

            //        totalUploadedBytes = totalUploadedBytes + bytesWritten;


            //        NSTimeInterval now = [NSDate timeIntervalSinceReferenceDate];
            //        double elapsedSeconds = now - start;
            //        double secondsLeft = (((double)totalExpectedUploadBytes - (double)totalUploadedBytes) / ((double)totalUploadedBytes / elapsedSeconds));
            //        double progress = (double)totalUploadedBytes / (double)totalExpectedUploadBytes;

            //        //NSLog(@"Sent %ld of %ld bytes - Progress: %f", totalUploadedBytes, totalExpectedUploadBytes, progress);

            //        [[CLUIActivityService sharedService] updateActivityViewWithProgress:progress
            //                                                                    timeLeft:secondsLeft
            //                                                                      bytes:(double)totalUploadedBytes
            //                                                               ofTotalBytes:(double)totalExpectedUploadBytes
            //                                                                  fileCount:[self.uploadOperationQueue operationCount]
            //                                                            andActivityType:activityViewLabelUpload];
            //    }];

            //    [uploadOperation setOperationCompletionBlock:^(CLHTTPConnectionOperation *operation, id responseObject, NSError *error) {

            //        NSLog(@"Upload Status: %li", [operation.response statusCode]);

            //        if ([operation.response statusCode] == 201) {

            //            NSLog(@"Upload Completed for File: %@", path);
            //            NSLog(@"Opperations remaining: %lu", [[self.uploadOperationQueue operations] count]);
            //            // update index and ui.
            //            [self performUpdateForSyncEvent:event success:YES];

            //        } else if ([operation.response statusCode] == 304){

            //            NSLog(@"The file already exists on the server");
            //            // update index and ui.
            //            [self performUpdateForSyncEvent:event success:YES];

            //        }else {
            //            NSLog(@"Upload Failed with status:%li for File: %@",[operation.response statusCode], path);
            //        }

            //        if (error) {

            //            // Error handler (back processor). Likely to happen due to network interruptions.
            //            // TODO: Handle the upload failure -- for now update the index to not pending.. we need to handle the error!!
            //            NSLog(@"Failed to Upload File: %@. Error: %@, Code: %ld", path, [error localizedDescription], [error code]);
            //            NSLog(@"Opperations remaining: %lu", [[self.uploadOperationQueue operations] count]);

            //            // update index and ui.
            //            [self performUpdateForSyncEvent:event success:NO];
            //        }

            //        if ([self.uploadOperationQueue operationCount] <= 0) {

            //            [[CLUIActivityService sharedService] updateActivityViewWithProgress:1 timeLeft:0 bytes:0 ofTotalBytes:0 fileCount:0 andActivityType:activityViewLabelSynced];
            //        }

            //    }];

            //    [operations addObject:uploadOperation];
            //}];

            //NSLog(@"Starting Upload Operarions");
            //start = [NSDate timeIntervalSinceReferenceDate];
            //[self.uploadOperationQueue addOperations:operations waitUntilFinished:YES];
            //NSLog(@"Finished Upload Operarions");
        }

        void DispatchDownloadEvents(List<CLEvent> events)
        {
            //Console.WriteLine("%s", __FUNCTION__);
            //NSMutableArray operations = NSMutableArray.Array();
            //if (this.DownloadOperationQueue == null) {
            //    this.DownloadOperationQueue = new CLSptNSOperationQueue();
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
