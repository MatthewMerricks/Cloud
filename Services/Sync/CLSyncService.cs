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
        private static List<object> _activeSyncQueue = null;

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
            _activeSyncQueue = new List<object>();
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
            addEventsCopy.ForEach(obj =>
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

        //- (void)syncFromFileSystemMonitor:(CLFSMonitoringService *)fsm withGroupedUserEvents:(NSDictionary *)events
        public void SyncFromFileSystemMonitorWithGroupedUserEvents(Dictionary<string, object> eventsDictionary)
        {

            // NSString *sid;
            // if ([self.currentSIDs lastObject] != nil) {
            //     sid = [self.currentSIDs lastObject];
            // }else {
            //    sid = [[CLSettings sharedSettings] sid];
            // }
            string sid;
            if (_currentSids.Last<object>() != null)
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
            long eid = (long)eventsDictionary[CLDefinitions.CLEventKey];
            List<object> eventList = (List<object>)eventsDictionary[CLDefinitions.CLSyncEvents];
            List<CLEvent> fsmEvents = new List<CLEvent>();

            // Filtering duplicate events
            // NSArray *filteredEvents = [self filterDuplicateEvents:eventList];
            List<object> filteredEvents = eventList.Distinct().ToList();
    
            // if ([eventList count] > 0) {
            if (eventList.Count > 0)
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
                filteredEvents.ForEach((fileSystemEvent) =>
                {
                    CLEvent evt = new CLEvent();
                    CLMetadata fsEventMetadata = new CLMetadata((Dictionary<string, object>)((Dictionary<string, object>)fileSystemEvent)[CLDefinitions.CLSyncEventMetadata]);
                    evt.Metadata = fsEventMetadata;
                    evt.IsMDSEvent = false;
                    evt.Action = (string)((Dictionary<string, object>)fileSystemEvent)[CLDefinitions.CLSyncEvent];
                });

                // Update events with metadata.
                // fsmEvents = [self updateMetadataForFileEvents:fsmEvents];
                //TODO: The Windows file system monitor has already retrieved the file metadata.  The following processing should not be necessary.
                //fsmEvents = UpdateMetadataForFileEvents(fsmEvents);

                // Sorting.
                // NSDictionary *sortedEvents = [self sortSyncEventsByType:fsmEvents];
                Dictionary<string, object> sortedEvents = SortSyncEventsByType(fsmEvents);
        
                // Preprocess Index (may filter out duplicate events based on existing index items)
                // sortedEvents = [self indexSortedEvents:sortedEvents];
                sortedEvents = IndexSortedEvents(sortedEvents);

                // if ([sortedEvents count] > 0) {
                if (sortedEvents.Count > 0)
                {                                       // we may have eliminated events while filtering them thru our index.

                    //NSMutableArray *sortedFSMEvents = [NSMutableArray array];
                    //// Adding objects to our active sync queue
                    //[sortedFSMEvents addObjectsFromArray:[sortedEvents objectForKey:CLEventTypeAdd]];
                    //[sortedFSMEvents addObjectsFromArray:[sortedEvents objectForKey:CLEventTypeModify]];
                    //[sortedFSMEvents addObjectsFromArray:[sortedEvents objectForKey:CLEventTypeRenameMove]];
                    //[sortedFSMEvents addObjectsFromArray:[sortedEvents objectForKey:CLEventTypeDelete]];
                    List<CLEvent> sortedFsmEvents = new List<CLEvent>();

                    // Adding objects to our active sync queue
                    sortedFsmEvents.Add((CLEvent)sortedEvents[CLDefinitions.CLEventTypeAdd]);
                    sortedFsmEvents.Add((CLEvent)sortedEvents[CLDefinitions.CLEventTypeModify]);
                    sortedFsmEvents.Add((CLEvent)sortedEvents[CLDefinitions.CLEventTypeRenameMove]);
                    sortedFsmEvents.Add((CLEvent)sortedEvents[CLDefinitions.CLEventTypeDelete]);

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

                    _restClient.SyncToCloud_WithCompletionHandler_OnQueue(fsmEventsDictionary, (Dictionary<string, object> metadata, CLError error) =>
                    {
                        if (error == null)
                        {
                    
                            _trace.writeToLog(1, "CLSyncService: SyncFromFileSystemMonitorWithGroupedUserEvents: Response From Sync To Cloud: {0}.", metadata);
                    
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
                                Dictionary<string, string> eventIds = new Dictionary<string, string>()
                                {
                                    {CLDefinitions.CLSyncEventID, eid.ToString()},
                                    {CLDefinitions.CLSyncID, newSid}
                                };

                                PerformSyncOperationWithEventsWithEventIDsAndOrigin(eventsDictionary, eventIds, CLEventOrigin.CLEventOriginMDS);
                            }
                        }
                        else
                        {
                            _trace.writeToLog(1, "CLSyncService: SyncFromFileSystemMonitorWithGroupedUserEvents: ERROR {0}.", error.errorDescription);
                        }
                    }, get_cloud_sync_queue());
                }
           }
        }

        //TODO: The Windows file system monitor has already retrieved the file metadata.  The following processing should not be necessary.
        // - (NSMutableArray *)updateMetadataForFileEvents:(NSMutableArray *)events
        //List<CLEvent> UpdateMetadataForFileEvents(List<CLEvent> events)
        //{
            //events.ForEach(evt =>
            //{
                //  NSDictionary metadata;
                //Dictionary<string, object> metadata;

                //if ((Myevent.Action).IsEqualToString(CLEventTypeAddFile) || (Myevent.Action).IsEqualToString(CLEventTypeModifyFile)) {
                //    int do_try = 0;
                //    do {
                //        string fileSystemPath = ((CLSettings.SharedSettings()).CloudFolderPath()).StringByAppendingPathComponent(Myevent.Metadata.Path);
                //        metadata = NSDictionary.AttributesForItemAtPath(fileSystemPath);
                //        Myevent.Metadata.CreateDate = metadata.ObjectForKey(CLMetadataFileCreateDate);
                //        Myevent.Metadata.ModifiedDate = metadata.ObjectForKey(CLMetadataFileModifiedDate);
                //        Myevent.Metadata.Revision = metadata.ObjectForKey(CLMetadataFileRevision);
                //        Myevent.Metadata.Hash = metadata.ObjectForKey(CLMetadataFileHash);
                //        Myevent.Metadata.Size = metadata.ObjectForKey(CLMetadataFileSize);
                //        do_try++;
                //    }
                //    while ((Myevent.Metadata.CreateDate).RangeOfString("190").Location != NSNotFound && do_try < 3000);  // 3 seconds.  TODO: This hack sucks!
                //}

                // All other file events we get stored index data for this event item.
                //if ((Myevent.Action).RangeOfString(CLEventTypeFileRange).Location != NSNotFound) {
                //    string cloudPath = Myevent.Metadata.Path;
                //    if ((Myevent.Action).IsEqualToString(CLEventTypeMoveFile) || (Myevent.Action).IsEqualToString(CLEventTypeRenameFile)) {
                //        cloudPath = Myevent.Metadata.FromPath;
                //    }

                //    CLMetadata indexedMetadata = CLIndexingServices.MetadataForItemAtCloudPath(cloudPath);
                //    if (indexedMetadata != null) {
                //        // For add events, if the darn thing already exists in the index, it means that FSM failed to pick up the event as a modify.
                //        // Let's make sure of that and if it turns out to be true, then we need to change the event to become a modify type.
                //        if ((Myevent.Action).IsEqualToString(CLEventTypeAddFile)) {
                //            if ((Myevent.Metadata.Hash).IsEqualToString(indexedMetadata.Hash) == false && (Myevent.Metadata.Revision).IsEqualToString(
                //                indexedMetadata.Revision) == false) {
                //                Myevent.Metadata.Revision = indexedMetadata.Revision;
                //                Myevent.Action = CLEventTypeModifyFile;
                //            }

                //        }
                //        else if ((Myevent.Action).IsEqualToString(CLEventTypeModifyFile)) {
                //            Myevent.Metadata.Revision = indexedMetadata.Revision;
                //        }
                //        else {   // we want it all for all other cases.
                //            Myevent.Metadata.Revision = indexedMetadata.Revision;
                //            Myevent.Metadata.Hash = indexedMetadata.Hash;
                //            Myevent.Metadata.CreateDate = indexedMetadata.CreateDate;
                //            Myevent.Metadata.ModifiedDate = indexedMetadata.ModifiedDate;
                //            Myevent.Metadata.Size = indexedMetadata.Size;
                //        }

                //    }

                //}

                // check if this item is a symblink, if this object is a in fact a link, the utility method will return YES.
                /*NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:cloudPath];
                CLAppDelegate *appDelegate = [NSApp delegate];
                if ([appDelegate isFileAliasAtPath:fileSystemPath]) {
                    // symblink events are always recognized as files, therefore simply replace the occurence of the word file with link in the event type.
                    event.action = [event.action stringByReplacingCharactersInRange:[event.action rangeOfString:CLEventTypeFileRange] withString:@"link"];
                } */
            //});
            // return events;
            //return new List<CLEvent>();
        //}

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

        // - (void)performSyncOperationWithEvents:(NSArray *)events withEventIDs:(NSDictionary *)ids andOrigin:(CLEventOrigin)origin
        void PerformSyncOperationWithEventsWithEventIDsAndOrigin(Dictionary<string, object> eventsDictionary, Dictionary<string, string> ids, CLEventOrigin origin)
        {
            //Console.WriteLine("%s", __FUNCTION__);
            //this.AnimateUIForSyncWithStatusMessageSyncActivityCount(true, menuItemActivityLabelIndexing, 0);
            //NSDictionary sortedEvents = this.SortSyncEventsByType(eventsDictionary);
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
            //    this.UploadOperationQueue = new CLSptNsOperationQueue();
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
