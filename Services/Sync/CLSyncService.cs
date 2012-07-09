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
        private static List<object> _activeDownloadQueue = null;
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
        }

        /// <summary>
        /// Start the syncing service.
        /// </summary>
        public void BeginSyncServices()
        {
            _serviceStarted = true;
            _recentItems = new List<string>();
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

        //- (NSDictionary *)indexSortedEvents:(NSDictionary *)sortedEvent
        Dictionary<string, object> IndexSortedEvents(Dictionary<string, object> sortedEvent)
        {
            // Merged 7/4/12
            //NSLog(@"%s", __FUNCTION__);
    
            ////// Move / Rename
    
            //// This is the order that events need to be processed. Do not change.
            //NSMutableArray *deleteEvents     = [sortedEvent objectForKey:CLEventTypeDelete];
            //NSMutableArray *addEvents        = [sortedEvent objectForKey:CLEventTypeAdd];
            //NSMutableArray *modifyEvents     = [sortedEvent objectForKey:CLEventTypeModify];
            //NSMutableArray *moveRenameEvents = [sortedEvent objectForKey:CLEventTypeRenameMove];
    
            //deleteEvents = [self indexDeleteEvents:deleteEvents];
            //addEvents    = [self indexAddEvents:addEvents];
            //modifyEvents = [self indexModifyEvents:modifyEvents];
            //moveRenameEvents = [self indexMoveRenameEvents:moveRenameEvents];
    
            //return [NSDictionary dictionaryWithObjectsAndKeys:addEvents,        CLEventTypeAdd,
            //        modifyEvents ,    CLEventTypeModify,
            //        moveRenameEvents, CLEventTypeRenameMove,
            //        deleteEvents,     CLEventTypeDelete , nil];
            //&&&&

            // NSLog(@"%s", __FUNCTION__);
            _trace.writeToLog(9, "CLSyncService: IndexSortedEvents: Entry.");

            // Move / Rename
  
            // This is the order that events need to be processed. Do not change.
            // NSMutableArray *deleteEvents     = [sortedEvent objectForKey:CLEventTypeDelete];
            // NSMutableArray *addEvents        = [sortedEvent objectForKey:CLEventTypeAdd];
            // NSMutableArray *modifyEvents     = [sortedEvent objectForKey:CLEventTypeModify];
            // NSMutableArray *moveRenameEvents = [sortedEvent objectForKey:CLEventTypeRenameMove];
            List<CLEvent> deleteEvents = (List<CLEvent>)sortedEvent[CLDefinitions.CLEventTypeDelete];
            List<CLEvent> addEvents = (List<CLEvent>)sortedEvent[CLDefinitions.CLEventTypeAdd];
            List<CLEvent> modifyEvents = (List<CLEvent>)sortedEvent[CLDefinitions.CLEventTypeModify];
            List<CLEvent> moveRenameEvents = (List<CLEvent>)sortedEvent[CLDefinitions.CLEventTypeRenameMove];
    
            // deleteEvents = [self indexDeleteEvents:deleteEvents];
            // addEvents    = [self indexAddEvents:addEvents];
            // modifyEvents = [self indexModifyEvents:modifyEvents];
            // moveRenameEvents = [self indexMoveRenameEvents:moveRenameEvents];
            deleteEvents = IndexDeleteEvents(deleteEvents);
            addEvents = IndexAddEvents(addEvents);
            modifyEvents = IndexModifyEvents(modifyEvents);
            moveRenameEvents = IndexMoveRenameEvents(moveRenameEvents);
    
            // return [NSDictionary dictionaryWithObjectsAndKeys:addEvents,        CLEventTypeAdd,
            //            modifyEvents ,    CLEventTypeModify,
            //            moveRenameEvents, CLEventTypeRenameMove,
            //            deleteEvents,     CLEventTypeDelete , nil];
            Dictionary<string, object> rc = new Dictionary<string, object>()
            {
                {CLDefinitions.CLEventTypeAdd, addEvents},
                {CLDefinitions.CLEventTypeModify, modifyEvents},
                {CLDefinitions.CLEventTypeRenameMove, moveRenameEvents},
                {CLDefinitions.CLEventTypeDelete, deleteEvents}
            };
            return rc;
        }

        //- (NSMutableArray *)indexMoveRenameEvents:(NSMutableArray *)events
        List<CLEvent> IndexMoveRenameEvents(List<CLEvent> events)
        {
            // Merged 7/4/12
            // __block NSMutableArray *moveRenameEvents = events;
            // [[events copy] enumerateObjectsUsingBlock:^(CLEvent *moveRenameEvent, NSUInteger idx, BOOL *stop) {
            //     CLMetadata *indexedMetadata = [CLIndexingServices indexedMetadataForEvent:moveRenameEvent];
            //     NSString *fromFileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:moveRenameEvent.metadata.fromPath];
            //     NSString *toFileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:moveRenameEvent.metadata.toPath];
        
            //     BOOL isMDSEvent = (moveRenameEvent.syncHeader.status != nil) ? YES : NO;
        
            //     if (indexedMetadata) {
            //         if ([[NSFileManager defaultManager] fileExistsAtPath:fromFileSystemPath]) {
            //             [CLIndexingServices markItemForEvent:moveRenameEvent asPending:YES];
            //         }else if ([[NSFileManager defaultManager] fileExistsAtPath:toFileSystemPath]){
            //             if (isMDSEvent == YES) {
            //                 [CLIndexingServices markItemForEvent:moveRenameEvent asPending:YES];
            //             }
            //             else{
            //                 [self badgeFileAtCloudPath:moveRenameEvent.metadata.toPath withBadge:cloudAppBadgeSynced];
            //                 [moveRenameEvents removeObject:moveRenameEvent];
            //             }
            //         }
            //     }else {
            //         NSLog(@"%s - THIS SHOULD NEVER HAPPEN BUT IT DID!!  ERROR", __FUNCTION__);
            //         if ([[NSFileManager defaultManager] fileExistsAtPath:fromFileSystemPath]) {
            //             // TODO: Create index object from event (using Metadata from FS)
            //             // let it ride...
            //         }else {
            //             if ([[NSFileManager defaultManager] fileExistsAtPath:toFileSystemPath]){
            //                 // TODO: Create index object from event (using Metadata from FS)
            //                 // Punt (the file is already at destination
            //                 // Badge
            //             }
            //         }
            //     }
            //     if (idx % 20 == 0) {
            //         [CLIndexingServices saveDataInContext:[NSManagedObjectContext defaultContext]];
            //     }
            // }];    
            // return moveRenameEvents;
            //&&&&

            // __block NSMutableArray *moveRenameEvents = events;
            // [[events copy] enumerateObjectsUsingBlock:^(CLEvent *moveRenameEvent, NSUInteger idx, BOOL *stop) {
            List<CLEvent> moveRenameEvents = new List<CLEvent>(events);
            int idx = 0;
            moveRenameEvents.ForEach(moveRenameEvent =>
            {
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
                        CLIndexingService.Instance.MarkItemForEvent_asPending(moveRenameEvent, asPending: true);
                    }
                    // }else if ([[NSFileManager defaultManager] fileExistsAtPath:toFileSystemPath]){
                    else if (File.Exists(toFileSystemPath))
                    {
                        // if (isMDSEvent == YES) {
                        if (isMDSEvent)
                        {
                            // [CLIndexingServices markItemForEvent:moveRenameEvent asPending:YES];
                            CLIndexingService.MarItemForEvent_asPending(moveRenameEvent, asPending: true);
                        }
                        else
                        {
                            //TODO: Implement UI badging
                            //     [self badgeFileAtCloudPath:moveRenameEvent.metadata.toPath withBadge:cloudAppBadgeSynced];

                            // [moveRenameEvents removeObject:moveRenameEvent];
                            moveRenameEvents.Remove(moveRenameEvent);
                        }
                    }
                }
                else
                {
                    // NSLog(@"%s - indexedMetadata is null.  THIS SHOULD NEVER HAPPEN BUT IT DID!!  ERROR", __FUNCTION__);
                    _trace.writeToLog(1, "CLSyncService: IndexMoveRenameEvents: ERROR: indexedMetadata is null.");

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

                // if (idx % 20 == 0) {
                if (idx % 20 == 0)
                {
                    // [CLIndexingServices saveDataInContext:[NSManagedObjectContext defaultContext]];
                    CLIndexingService.Instance.SaveDataInContext();
                }
                ++idx;
            });

            // return moveRenameEvents;
            return moveRenameEvents;
        }

        //- (NSMutableArray *)indexModifyEvents:(NSMutableArray *)events
        List<CLEvent> IndexModifyEvents(List<CLEvent> events)
        {
            // Merged 7/4/12
            // __block NSMutableArray *modifyEvents = events;
            // [[events copy] enumerateObjectsUsingBlock:^(CLEvent *modifyEvent, NSUInteger idx, BOOL *stop) {
            //     CLMetadata *indexedMetadata = [CLIndexingServices indexedMetadataForEvent:modifyEvent];
            //     if (indexedMetadata) {
            //         NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:modifyEvent.metadata.path];
            //         if ([[NSFileManager defaultManager] fileExistsAtPath:fileSystemPath]){
            //             [CLIndexingServices markItemForEvent:modifyEvent asPending:YES];
            //         }
            //     }
            //     if (idx % 20 == 0) {
            //         [CLIndexingServices saveDataInContext:[NSManagedObjectContext defaultContext]];
            //     }
            // }];
    
            // return modifyEvents;
            //&&&&

            // __block NSMutableArray *modifyEvents = events;
            // [[events copy] enumerateObjectsUsingBlock:^(CLEvent *modifyEvent, NSUInteger idx, BOOL *stop) {
            List<CLEvent> modifyEvents = new List<CLEvent>(events);
            int idx = 0;
            modifyEvents.ForEach(modifyEvent =>
            {
                // CLMetadata *indexedMetadata = [CLIndexingServices indexedMetadataForEvent:modifyEvent];
                CLMetadata indexedMetadata = CLIndexingService.Instance.IndexMetadataForEvent(modifyEvent);

                // if (indexedMetadata) {
                if (indexedMetadata != null)
                {
                    // NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:modifyEvent.metadata.path];
                    string fileSystemPath = Settings.Instance.CloudFolderPath + modifyEvent.Metadata.Path;

                    // if ([[NSFileManager defaultManager] fileExistsAtPath:fileSystemPath]){
                    if (File.Exists(fileSystemPath))
                    {
                        // [CLIndexingServices markItemForEvent:modifyEvent asPending:YES];
                        CLIndexingService.Instance.MarkItemForEvent_asPending(modifyEvent, asPending: true);
                    }
                }

                // if (idx % 20 == 0) {
                if (idx % 20 == 0)
                {
                    // [CLIndexingServices saveDataInContext:[NSManagedObjectContext defaultContext]];
                    CLIndexingService.Instance.SaveDataInContext();
                }
                ++idx;
            });
    
            // return modifyEvents;
            return modifyEvents;
        }

        //- (NSMutableArray *)indexAddEvents:(NSMutableArray *)events
        List<CLEvent> IndexAddEvents(List<CLEvent> events)
        {
            // Merged 7/4/12
            // __block NSMutableArray *addEvents = [self sortAddEventsFoldersFirstThenFiles:events];
    
            // [[addEvents copy] enumerateObjectsUsingBlock:^(CLEvent *addEvent, NSUInteger idx, BOOL *stop) {
            //     CLMetadata *indexedMetadata = [CLIndexingServices indexedMetadataForEvent:addEvent];
            //     NSString *eventAction = addEvent.syncHeader.action;
            //     BOOL isFileEvent = [eventAction rangeOfString:CLEventTypeFileRange].location != NSNotFound ? YES : NO;
            //     if (indexedMetadata) {
            
            //         NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:addEvent.metadata.path];
            //         if (isFileEvent == YES) {
            //             if (indexedMetadata.isPending == NO) {
            //                 if ([[NSFileManager defaultManager] fileExistsAtPath:fileSystemPath]) {
            //                     if ([addEvent.metadata.hash isEqualToString:indexedMetadata.hash]){
            //                         [self badgeFileAtCloudPath:addEvent.metadata.path withBadge:cloudAppBadgeSynced];
            //                         [addEvents removeObject:addEvent];
            //                     }else {
            //                         // TODO: Check to see if the item is in the active sync queue
            //                         [CLIndexingServices markItemForEvent:addEvent asPending:YES];
            //                     }
            //                 }else {
            //                     // Check to see if
            //                     [CLIndexingServices markItemForEvent:addEvent asPending:YES];
            //                 }
            //             }else {
            //                 // TODO: Pending == YES
            //                 // TODO: Check to see if the event is in the active sync queue if yes punt if no check if it exists in file system and hash are the same.
            //             }
            //         }else {
            //             if (indexedMetadata.isPending == NO){
            //                 if ([[NSFileManager defaultManager] fileExistsAtPath:fileSystemPath]){
            //                     [self badgeFileAtCloudPath:addEvent.metadata.path withBadge:cloudAppBadgeSynced];
            //                     [addEvents removeObject:addEvent];
            //                 }else {
            //                     [CLIndexingServices markItemForEvent:addEvent asPending:YES];
            //                 }
            //             }else {
            //                 // TODO: Pending == YES
            //                 // TODO: Check to see if the event is in the active sync queue if yes punt
            //             }
            //         }
            //     }else {
            
            //         [CLIndexingServices addMetedataItem:addEvent.metadata pending:YES];
            //     }
            //     if (idx % 20 == 0) {
            //         [CLIndexingServices saveDataInContext:[NSManagedObjectContext defaultContext]];
            //     }
            // }];
            // return addEvents;
            //&&&&

            // __block NSMutableArray *addEvents = [self sortAddEventsFoldersFirstThenFiles:events];
            List<CLEvent> addEventsSorted = SortAddEventsFoldersFirstThenFiles(events);
    
            // [[addEvents copy] enumerateObjectsUsingBlock:^(CLEvent *addEvent, NSUInteger idx, BOOL *stop) {
            List<CLEvent> addEvents = new List<CLEvent>(addEventsSorted);
            int idx = 0;
            addEvents.ForEach(addEvent =>
            {
                // CLMetadata *indexedMetadata = [CLIndexingServices indexedMetadataForEvent:addEvent];
                CLMetadata indexedMetadata = CLIndexingService.Instance.IndexedMetadataForEvent(addEventsSorted);

                // NSString *eventAction = addEvent.syncHeader.action;
                // BOOL isFileEvent = [eventAction rangeOfString:CLEventTypeFileRange].location != NSNotFound ? YES : NO;
                string eventAction = addEvent.SyncHeader.Action;
                bool isFileEvent = eventAction.Contains(CLDefinitions.CLEventTypeFileRange) ? true : false;

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
                                    //TODO: Implement UI badging
                                    // [self badgeFileAtCloudPath:addEvent.metadata.path withBadge:cloudAppBadgeSynced];
                                    
                                    // [addEvents removeObject:addEvent];
                                    addEvents.Remove(addEvent);
                                }
                                else
                                {
                                    // TODO: Check to see if the item is in the active sync queue
                                    // [CLIndexingServices markItemForEvent:addEvent asPending:YES];
                                    CLIndexingService.Instance.MarkItemForEvent_asPending(addEvent, asPending: true);
                                }
                            }
                            else
                            {
                                // TODO: Check to see if the event is in the active sync queue if yes punt
                                // [CLIndexingServices markItemForEvent:addEvent asPending:YES];
                                CLIndexingService.Instance.MarkItemForEvent_asPending(addEvent, asPending: true);
                            }
                        }
                        else
                        {
                            // TODO: Pending == YES
                            // TODO: Check to see if the event is in the active sync queue if yes punt if no check if it exists in file system and hash are the same.
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
                                //TODO: Implement UI badging.
                                // [self badgeFileAtCloudPath:addEvent.metadata.path withBadge:cloudAppBadgeSynced];

                                // [addEvents removeObject:addEvent];
                                addEvents.Remove(addEvent);
                            }
                            else
                            {
                                // [CLIndexingServices markItemForEvent:addEvent asPending:YES];
                                CLIndexingService.Instance.MarkItemForEvent_asPending(addEvent, asPending: true);
                            }
                        }
                        else
                        {
                            // TODO: Pending == YES
                            // TODO: Check to see if the event is in the active sync queue if yes punt
                        }
                    }
                }
                else
                {
                    // [CLIndexingServices addMetedataItem:addEvent.metadata pending:YES];
                    CLIndexingService.Instance.AddMetadataItem_pending(addEvent.Metadata, pending: true);
                }
    
                // if (idx % 20 == 0) {
                if (idx % 20 == 0)
                {
                    // [CLIndexingServices saveDataInContext:[NSManagedObjectContext defaultContext]];
                    CLIndexingService.Instance.SaveDataInContext();
                }
                ++idx;
            });

            // return addEvents;
            return addEvents;
        }

        //- (NSMutableArray *)indexDeleteEvents:(NSMutableArray *)events
        List<CLEvent> IndexDeleteEvents(List<CLEvent> events)
        {
            // Merged 7/4/12
            // __block NSMutableArray *deleteEvents = events;
    
            // [[events copy] enumerateObjectsUsingBlock:^(CLEvent *deleteEvent, NSUInteger idx, BOOL *stop) {
        
            //     CLMetadata *indexedMetadata = [CLIndexingServices indexedMetadataForEvent:deleteEvent];
            //     if (indexedMetadata) {
            //         [CLIndexingServices markItemForEvent:deleteEvent asPending:YES];
            //     }else {
            //         NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:deleteEvent.metadata.path];
            //         if ([[NSFileManager defaultManager] fileExistsAtPath:fileSystemPath] == NO) {
            //             [deleteEvents removeObject:deleteEvent];
            //         }else {
            //             // process event
            //         }
            //     }
            //     if (idx % 20 == 0) {
            //         [CLIndexingServices saveDataInContext:[NSManagedObjectContext defaultContext]];
            //     }
            // }];
    
            // return deleteEvents;
            //&&&&

            // __block NSMutableArray *deleteEvents = events;
            // [[events copy] enumerateObjectsUsingBlock:^(CLEvent *deleteEvent, NSUInteger idx, BOOL *stop) {
            List<CLEvent> deleteEvents = new List<CLEvent>(events);
            int idx = 0;
            deleteEvents.ForEach(deleteEvent =>
            {
                // CLMetadata *indexedMetadata = [CLIndexingServices indexedMetadataForEvent:deleteEvent];
                CLMetadata indexedMetadata = CLIndexingService.Instance.IndexedMetadataForEvent(deleteEvent);

                // if (indexedMetadata) {
                if (indexedMetadata != null)
                {
                    // [CLIndexingServices markItemForEvent:deleteEvent asPending:YES];
                    CLIndexingService.Instance.MarkItemForEvent_asPending(deleteEvent, asPending: true);
                }
                else
                {
                    // NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:deleteEvent.metadata.path];
                    string fileSystemPath = Settings.Instance.CloudFolderPath + deleteEvent.Metadata.Path;

                    // if ([[NSFileManager defaultManager] fileExistsAtPath:fileSystemPath] == NO) {
                    if (!File.Exists(fileSystemPath))
                    {
                        // [deleteEvents removeObject:deleteEvent];
                        deleteEvents.Remove(deleteEvent);
                    }
                    else
                    {
                        //TODO: process event
                    }
                }

                // if (idx % 20 == 0) {
                if (idx % 20 == 0)
                {
                    // [CLIndexingServices saveDataInContext:[NSManagedObjectContext defaultContext]];
                    CLIndexingService.Instance.SaveDataInContext();
                }
                ++idx;
            });

            // return deleteEvents;
            return deleteEvents;
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
                    Dictionary<string, object> metadata = (Dictionary<string, object>)result.JsonResult[CLDefinitions.CLSyncEventMetadata];

                    _trace.writeToLog(1, "CLSyncService: SyncFromFileSystemMonitorWithGroupedUserEvents: Response From Sync To Cloud: {0}.",
                            result.JsonResult[CLDefinitions.CLSyncEventMetadata]);

                    // if ([[metadata objectForKey:CLSyncEvents] count] > 0) {
                    if (((List<CLEvent>)metadata[CLDefinitions.CLSyncEvents]).Count > 0)
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
                        List<object> mdsEvents = (List<object>)metadata[CLDefinitions.CLSyncEvents];
                        List<CLEvent> eventsReceived = new List<CLEvent>();

                        // [mdsEvents enumerateObjectsUsingBlock:^(id mdsEvent, NSUInteger idx, BOOL *stop) {
                        mdsEvents.ForEach(obj =>
                        {
                            // If status is not found, metadata is null.
                            // if (![[[mdsEvent objectForKey:CLSyncEventHeader] objectForKey:CLSyncEventStatus] isEqualToString:CLEventTypeNotFound]) {
                            //   // check for valid dictionary
                            //   if ([[mdsEvent objectForKey:CLSyncEventMetadata] isKindOfClass:[NSDictionary class]]){
                            //      [events addObject:[CLEvent eventFromMDSEvent:mdsEvent]];
                            //   }
                            // }
                            Dictionary<string, object> mdsEventDictionary = (Dictionary<string, object>)obj;
                            Dictionary<string, object> syncHeaderDictionary = (Dictionary<string, object>)mdsEventDictionary[CLDefinitions.CLSyncEventHeader];
                            if (syncHeaderDictionary.ContainsKey(CLDefinitions.CLSyncEventStatus))
                            {
                                eventsReceived.Add(CLEvent.EventFromMDSEvent(mdsEventDictionary));
                            }
                        });

                        // Dispatch for processing.
                        // NSMutableDictionary *eventIds = [NSMutableDictionary dictionaryWithObjectsAndKeys:eid, CLSyncEventID, newSid, CLSyncID, nil];
                        Dictionary<string, object> eventIds = new Dictionary<string, object>()
                                {
                                    {CLDefinitions.CLSyncEventID, eid.ToString()},
                                    {CLDefinitions.CLSyncID, newSid}
                                };

                        // [self performSyncOperationWithEvents:events withEventIDs:eventIds andOrigin:CLEventOriginMDS];
                        PerformSyncOperationWithEventsWithEventIDsAndOrigin(eventsReceived, eventIds, CLEventOrigin.CLEventOriginMDS);
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
                evt.Metadata.IsDirectory = true;
            }

            // for new files we get the current file metadata from the file system.
            // if ([event.action isEqualToString:CLEventTypeAddFile] ||
            //     [event.action isEqualToString:CLEventTypeModifyFile] ||
            //     [event.action isEqualToString:CLEventTypeAddFolder]) {
            if (evt.Action.Equals(CLDefinitions.CLEventTypeAddFile) || evt.Action.Equals(CLDefinitions.CLEventTypeModifyFile) || evt.Action.Equals(CLDefinitions.CLEventTypeAddFolder))
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
                    if (targetPath != String.Empty)
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
                        evt.Metadata.TargetPath = targetPath;

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
                    CLMetadata fileMetadata = new CLMetadata(fileSystemPath2);
                    metadata = CLMetadata.DictionaryFromMetadataItem(fileMetadata);
                    evt.Metadata.CreateDate = (string) metadata[CLDefinitions.CLMetadataFileCreateDate];
                    evt.Metadata.ModifiedDate = (string) metadata[CLDefinitions.CLMetadataFileModifiedDate];
                    if (evt.Action.Contains(CLDefinitions.CLEventTypeFileRange))         // these are only relevant for files
                    {
                        evt.Metadata.Revision = (string)metadata[CLDefinitions.CLMetadataFileRevision];
                        evt.Metadata.Hash = (string)metadata[CLDefinitions.CLMetadataFileHash];         // The original code did not do this
                        evt.Metadata.Size = (string)metadata[CLDefinitions.CLMetadataFileSize];
                        evt.Metadata.IsDirectory = false;
                        //TODO: No custom file attributes in Windows?
                    }
                }
            }

            // All other file events we get stored index data for this event item.
            // if ([event.action rangeOfString:CLEventTypeFileRange].location != NSNotFound) {
            if (evt.Action.Contains(CLDefinitions.CLEventTypeFileRange))
            {
                // CLMetadata *indexedMetadata = [CLIndexingServices indexedMetadataForEvent:event];
                // if (indexedMetadata != nil) { // we have an object indexed for this event.
                //     // for add events, if the darn thing already exists in the index, it means that FSM failed to pick up the event as a modify
                //     // let's make sure of that and if it turns out to be true, then we need to change the event to become a modify type.
                //     if ([event.action isEqualToString:CLEventTypeAddFile]) {
                //         if ([event.metadata.hash isEqualToString:indexedMetadata.hash] == NO &&
                //             [event.metadata.revision isEqualToString:indexedMetadata.revision] == NO) {
                //             event.metadata.revision = indexedMetadata.revision;
                //             event.action = CLEventTypeModifyFile;
                //         }
                //     }
                //     else if ([event.action isEqualToString:CLEventTypeModifyFile]) { // for modify we only want to revision
                //         event.metadata.revision = indexedMetadata.revision;
                //     }
                //     else { // we want it all for all other cases.
                //         event.metadata.revision = indexedMetadata.revision;
                //         event.metadata.hash = indexedMetadata.hash;
                //         event.metadata.createDate = indexedMetadata.createDate;
                //         event.metadata.modifiedDate = indexedMetadata.modifiedDate;
                //         event.metadata.size = indexedMetadata.size;
                //     }
                //     if (indexedMetadata.targetPath != nil) { // we have a link object, convert
                //         // symblink events are always recognized as files, therefore simply replace the occurence of the word file with link for the event action.
                //         event.action = [event.action stringByReplacingCharactersInRange:[event.action rangeOfString:CLEventTypeFileRange] withString:@"link"];
                //         event.metadata.targetPath = indexedMetadata.targetPath;
                //     }
                // }
            }

            // return event;
            return evt;
        }

        void NotificationServiceDidReceivePushNotificationFromServer(bool /*CLNotificationServices*/ ns, string notification)
        {
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

                    // self.waitingForCloudResponse = NO;
                    _waitingForCloudResponse = false;

                    // if (self.needSyncFromCloud == YES) {
                    if (_needSyncFromCloud)
                    {
                        //TODO: Implement notification.
                        //  [self notificationService:nil didReceivePushNotificationFromServer:nil];
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
            // Merged 7/4/12
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
            //self.syncItemsQueueCount = [self.activeSyncQueue count];
    
            //// Update UI with sync activity
            //[self animateUIForSync:YES withStatusMessage:menuItemActivityLabelSyncing syncActivityCount:self.syncItemsQueueCount];
    
            //// This order of these methods is important do not change!! btchs
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
    
            //// Display user notification
            //[[CLUIActivityService sharedService] displayUserNotificationForSyncEvents:self.activeSyncQueue];
    
            //// Remove active items from queue (if any left)
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
            //TODO: Implemenet UI.
            //[self animateUIForSync:YES withStatusMessage:menuItemActivityLabelIndexing syncActivityCount:0];

            // Sorting, bitches.
            //NSDictionary *sortedEvents = [self sortSyncEventsByType:events];
            Dictionary<string, object> sortedEvents = SortSyncEventsByType(events);

            // Preprocess Index
            //sortedEvents = [self indexSortedEvents:sortedEvents];
            sortedEvents = IndexSortedEvents(sortedEvents);

            // Process Sync Events

            // Adding objects to our active sync queue
            //[self.activeSyncQueue addObjectsFromArray:[sortedEvents objectForKey:CLEventTypeAdd]];
            //[self.activeSyncQueue addObjectsFromArray:[sortedEvents objectForKey:CLEventTypeModify]];
            //[self.activeSyncQueue addObjectsFromArray:[sortedEvents objectForKey:CLEventTypeRenameMove]];
            //[self.activeSyncQueue addObjectsFromArray:[sortedEvents objectForKey:CLEventTypeDelete]];
            _activeSyncQueue.AddRange((List<CLEvent>)sortedEvents[CLDefinitions.CLEventTypeAdd]);
            _activeSyncQueue.AddRange((List<CLEvent>)sortedEvents[CLDefinitions.CLEventTypeModify]);
            _activeSyncQueue.AddRange((List<CLEvent>)sortedEvents[CLDefinitions.CLEventTypeRenameMove]);
            _activeSyncQueue.AddRange((List<CLEvent>)sortedEvents[CLDefinitions.CLEventTypeDelete]);

            // Get separated file and folder events
            //self.activeSyncFolderQueue = [self separateFolderFromFileForActiveEvents:self.activeSyncQueue wantsFolderEvents:YES];
            //self.activeSyncFileQueue = [self separateFolderFromFileForActiveEvents:self.activeSyncQueue wantsFolderEvents:NO];
            _activeSyncFolderQueue = SeparateFolderFromFileForActiveEvents_WantsFolderEvents(_activeSyncQueue, wantsFolderEvents: true);
            _activeSyncFileQueue = SeparateFolderFromFileForActiveEvents_WantsFolderEvents(_activeSyncQueue, wantsFolderEvents: false);

            // Get total object count in sync queue
            //self.syncItemsQueueCount = [self.activeSyncQueue count];
            _syncItemsQueueCount = _activeSyncQueue.Count;

            // Update UI with sync activity
            //TODO: Implement this UI.
            //[self animateUIForSync:YES withStatusMessage:menuItemActivityLabelSyncing syncActivityCount:self.syncItemsQueueCount];

            // This order of these methods is important do not change!! btchs
            // Delete Files and Folders
            //[self processDeleteSyncEvents:[sortedEvents objectForKey:CLEventTypeDelete]];
            ProcessDeleteSyncEvents((List<CLEvent>)sortedEvents[CLDefinitions.CLEventTypeDelete]);

            // Add Folders
            //[self processAddFolderSyncEvents:[sortedEvents objectForKey:CLEventTypeAdd]];
            ProcessAddFolderSyncEvents((List<CLEvent>)sortedEvents[CLDefinitions.CLEventTypeAdd]);

            // Rename/Move Folders
            //[self processRenameMoveFolderSyncEvents:[sortedEvents objectForKey:CLEventTypeRenameMove]];
            ProcessRenameMoveFolderSyncEvents((List<CLEvent>)sortedEvents[CLDefinitions.CLEventTypeRenameMove]);

            // Add Files
            //[self processAddFileSyncEvents:[sortedEvents objectForKey:CLEventTypeAdd]];
            processAddFileSyncEvents((List<CLEvent>)sortedEvents[CLDefinitions.CLEventTypeAdd]);

            // Modify Files
            //[self processModifyFileSyncEvents:[sortedEvents objectForKey:CLEventTypeModify]];
            processModifyFileSyncEvents((List<CLEvent>)sortedEvents[CLDefinitions.CLEventTypeModify]);

            // Rename/Move Files
            //[self processRenameMoveFileSyncEvents:[sortedEvents objectForKey:CLEventTypeRenameMove]];
            processRenameMoveFileSyncEvents((List<CLEvent>)sortedEvents[CLDefinitions.CLEventTypeRenameMove]);

            // Add Links
            //[self processAddLinkSyncEvents:[sortedEvents objectForKey:CLEventTypeAdd]];
            ProcessAddLinkSyncEvents((List<CLEvent>)sortedEvents[CLDefinitions.CLEventTypeAdd]);

            // Modify Links
            //[self processModifyLinkSyncEvents:[sortedEvents objectForKey:CLEventTypeModify]];
            ProcessModifyLinkSyncEvents((List<CLEvent>)sortedEvents[CLDefinitions.CLEventTypeModify]);

            // Rename/Move Links
            //[self processRenameMoveLinkSyncEvents:[sortedEvents objectForKey:CLEventTypeRenameMove]];
            ProcessRenameLinkSyncEvents((List<CLEvent>)sortedEvents[CLDefinitions.CLEventTypeRenameMove]);

            // Display user notification
            //TODO: Implement this UI.
            //[[CLUIActivityService sharedService] displayUserNotificationForSyncEvents:self.activeSyncQueue];

            // Remove active items from queue (if any left)
            //[self.activeSyncQueue removeAllObjects];
            _activeSyncQueue.RemoveAll((CLEvent evt) => { return true; });

            // Sync finished.
            //[self saveSyncStateWithSID:[ids objectForKey:CLSyncID] andEID:[ids objectForKey:CLSyncEventID]];
            SaveSyncStateWithSIDAndEID((string)ids[CLDefinitions.CLSyncID], (ulong) ids[CLDefinitions.CLSyncEventID]);

            // Update UI with activity.
            //TODO: Implement this UI.
            //[self animateUIForSync:NO withStatusMessage:menuItemActivityLabelSynced syncActivityCount:0];

            //TODO: Implement this to save index changes if necessary.
            //if ([[NSManagedObjectContext defaultContext] hasChanges]){
            //    [CLIndexingServices saveDataInContext:[NSManagedObjectContext defaultContext]];
            //}
            CLIndexingService.Instance.SaveDataInContext();

            //if (self.needSyncFromCloud == YES) {
            if (_needSyncFromCloud)
            {
                //TODO: Notify?
                // [self notificationService:nil didReceivePushNotificationFromServer:nil];
            }
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
                            success = CLFSDispatcher.Instance.MoveItemAtPath_to_error(fromPath, toPath, out error);
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

        void processAddFileSyncEvents(List<CLEvent> events)
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

        void processModifyFileSyncEvents(List<CLEvent> events)
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

        void processRenameMoveFileSyncEvents(List<CLEvent> events)
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

        //- (void)processAddLinkSyncEvents:(NSArray *)events
        void ProcessAddLinkSyncEvents(List<CLEvent> events)
        {
            // Merged 7/4/12
            // NSLog(@"%s", __FUNCTION__);
    
            // // Add File events.
            // [events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
        
            //     CLEvent *event = obj;
        
            //     if (event.isMDSEvent) {
            //         NSString *actionType = event.syncHeader.action;
            //         NSString *status = event.syncHeader.status;
            
            //         // link events only.
            //         if ([actionType isEqualToString:CLEventTypeAddLink]) {
                
            //             BOOL success = YES; // assume true for events we originated (since they already happened), override value for MDS execution.
            //             if (status == nil) { // MDS origin, Philis told us we need to do this.
                    
            //                 success = [[CLFSDispatcher defaultDispatcher] createSymbLinkAtPath:event.metadata.path withTarget:event.metadata.targetPath];
            //             }
                
            //             // update ui
            //             [self performUpdateForSyncEvent:event success:success];
            //         }
            //     }
        
            // }];
            //&&&&

            // NSLog(@"%s", __FUNCTION__);
            _trace.writeToLog(9, "CLSyncService: ProcessAddLinkSyncEvents: Entry.");

            // Add File events.
            // [events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
            foreach (CLEvent evt in events)
            {
                // if (event.isMDSEvent) {
                if (evt.IsMDSEvent)
                {
                    // NSString *actionType = event.syncHeader.action;
                    // NSString *status = event.syncHeader.status;
                    string actionType = evt.SyncHeader.Action;
                    string status = evt.SyncHeader.Status;


                    // if ([actionType isEqualToString:CLEventTypeAddLink]) {
                    if (actionType.Equals(CLDefinitions.CLEventTypeAddLink, StringComparison.InvariantCulture))
                    {

                        // BOOL success = YES; // assume true for events we originated (since they already happened), override value for MDS execution.
                        bool success = true;

                        // if (status == nil) { // MDS origin, Philis told us we need to do this.
                        if (status == String.Empty)
                        {
                            // success = [[CLFSDispatcher defaultDispatcher] createSymbLinkAtPath:event.metadata.path withTarget:event.metadata.targetPath];
                            CLFSDispatcher.Instance.CreateSymbLinkAtPath_withTarget(evt.Metadata.Path, evt.Metadata.TargetPath);
                        }

                        // Update ui
                        //TODO: Implement this UI.
                        // [self performUpdateForSyncEvent:event success:success];
                    }
                }
            }

        }

        //- (void)processModifyLinkSyncEvents:(NSArray *)events
        void ProcessModifyLinkSyncEvents(List<CLEvent> events)
        {
            // Merged 7/4/12
            // NSLog(@"%s", __FUNCTION__);
    
            // // Modify File events.
            // [events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
        
            //     CLEvent *event = obj;
            //     NSString *actionType = event.syncHeader.action;
            //     NSString *status = event.syncHeader.status;
        
            //     // modify events only.
            //     if ([actionType isEqualToString:CLEventTypeModifyLink]) {
            
            //         BOOL success = YES; // assume true for events we originated (since they already happened), override value for MDS execution.
            //         if (status == nil) { // MDS origin, Philis told us we need to do this.
                
            //             success = [[CLFSDispatcher defaultDispatcher] createSymbLinkAtPath:event.metadata.path withTarget:event.metadata.targetPath];
            //         }
            
            //         // update ui.
            //         [self performUpdateForSyncEvent:event success:success];
            //     }
        
            // }];
            //&&&&

            // NSLog(@"%s", __FUNCTION__);
            _trace.writeToLog(9, "CLSyncService: ProcessModifyLinkSyncEvents: Entry.");

            // // Modify File events.
            // [events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
            foreach (CLEvent evt in events)
            {
                // NSString *actionType = event.syncHeader.action;
                // NSString *status = event.syncHeader.status;
                string actionType = evt.SyncHeader.Action;
                string status = evt.SyncHeader.Status;

                // modify events only.
                // if ([actionType isEqualToString:CLEventTypeModifyLink]) {
                if (actionType.Equals(CLDefinitions.CLEventTypeModifyLink, StringComparison.InvariantCulture))
                {
                    // BOOL success = YES; // assume true for events we originated (since they already happened), override value for MDS execution.
                    bool success = true;

                    // if (status == nil) { // MDS origin, Philis told us we need to do this.
                    if (status == String.Empty)
                    {
                        // success = [[CLFSDispatcher defaultDispatcher] createSymbLinkAtPath:event.metadata.path withTarget:event.metadata.targetPath];
                        success = CLFSDispatcher.Instance.CreateSymbLinkAtPath_withTarget(evt.Metadata.Path, evt.Metadata.TargetPath);
                    }

                    // Update ui.
                    //TODO: Implement this UI.
                    // [self performUpdateForSyncEvent:event success:success];
                }

            }
        }


        //- (void)processRenameMoveLinkSyncEvents:(NSArray *)events
        void ProcessRenameMoveLinkSyncEvents(List<CLEvent> events)
        {
            // Merged 7/4/12
            // NSLog(@"%s", __FUNCTION__);
    
            // // Rename/Move Link events.
            // [events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
        
            //     CLEvent *event = obj;
            //     NSString *actionType = event.syncHeader.action;
            //     NSString *status = event.syncHeader.status;
            //     NSString *toPath = event.metadata.toPath;
            //     NSString *fromPath = event.metadata.fromPath;
        
            //     // folder events first.
            //     if ([actionType rangeOfString:CLEventTypeLinkRange].location != NSNotFound) {
            
            //         if ([actionType rangeOfString:CLEventTypeRenameRange].location != NSNotFound ||
            //             [actionType rangeOfString:CLEventTypeMoveRange].location != NSNotFound ) {
                
            //             BOOL success = YES; // assume true for events we originated (since they already happened), override value for MDS execution.
                
            //             if (status == nil) { // MDS origin, Philis told us we need to do this.
            //                 success = [[CLFSDispatcher defaultDispatcher] moveItemAtPath:fromPath to:toPath error:nil];
            //             }
                
            //             // update ui.
            //             [self performUpdateForSyncEvent:event success:success];
            //         }
            //     }
        
            // }];
            //&&&&

            // NSLog(@"%s", __FUNCTION__);
            _trace.writeToLog(9, "CLSyncService: ProcessRenameMoveLinkSyncEvents: Entry.");
            
            // Rename/Move Link events.
            // [events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
            foreach (CLEvent evt in events)
            {
                // CLEvent *event = obj;
                // NSString *actionType = event.syncHeader.action;
                // NSString *status = event.syncHeader.status;
                // NSString *toPath = event.metadata.toPath;
                // NSString *fromPath = event.metadata.fromPath;
                string actionType = evt.SyncHeader.Action;
                string status = evt.SyncHeader.Status;
                string toPath = evt.Metadata.ToPath;
                string fromPath = evt.Metadata.FromPath;

                // Folder events first.
                // if ([actionType rangeOfString:CLEventTypeLinkRange].location != NSNotFound) {
                if (actionType.Contains(CLDefinitions.CLEventTypeLinkRange))
                {
                    // if ([actionType rangeOfString:CLEventTypeRenameRange].location != NSNotFound ||
                    //    [actionType rangeOfString:CLEventTypeMoveRange].location != NSNotFound ) {
                    if (actionType.Contains(CLDefinitions.CLEventTypeRenameRange) ||
                        actionType.Contains(CLDefinitions.CLEventTypeMoveRange))
                    {
                        // BOOL success = YES; // assume true for events we originated (since they already happened), override value for MDS execution.
                        bool success = true;

                        // if (status == nil) { // MDS origin, Philis told us we need to do this.
                        if (status == String.Empty)
                        {
                            // success = [[CLFSDispatcher defaultDispatcher] moveItemAtPath:fromPath to:toPath error:nil];
                            CLError err = null;
                            success = CLFSDispatcher.Instance.MoveItemAtPath_to_error(fromPath, toPath, out err);

                            //TODO: Handle error?
                        }

                        // Update ui.
                        //TODO: Implement this UI.
                        // [self performUpdateForSyncEvent:event success:success];
                    }
                }
            }
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
            _trace.writeToLog(9," CLSyncService: DispatchUploadEvents: Entry.");

            //NSMutableArray *operations = [NSMutableArray array];
            List<CLSptNSOperation> operations = new List<CLSptNSOperation>();

            //if (self.uploadOperationQueue == nil) {
            //    self.uploadOperationQueue = [[CLOperationQueue alloc] init];
            //    self.uploadOperationQueue.maxConcurrentOperationCount = 6;
            //}
            if (_uploadOperationQueue == null)
            {
                _uploadOperationQueue = new CLSptNSOperationQueue(maxConcurrentTasks: 6);
            }

            //NSLog(@"Number of uploads to start: %lu", [events count]);
            _trace.writeToLog(9, " CLSyncService: DispatchUploadEvents: Number of uploads to start: {0}.", events.Count);

            //__block NSInteger totalExpectedUploadBytes = 0;
            //__block NSInteger totalUploadedBytes = 0;
            //__block NSTimeInterval start;
            long totalExpectedUploadBytes = 0;
            long totalUploadedBytes = 0;
            DateTime start;
            
            //[events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
            //    CLEvent *event = obj;
            events.ForEach(obj =>
            {
                // __block CLEvent *event = obj;
                CLEvent evt = obj;

                // NSString *path = event.metadata.path;
                string path = evt.Metadata.Path;

                // NSString *storageKey = event.metadata.storage_key;
                string storageKey = evt.Metadata.Storage_key;

                // NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:path];
                string fileSystemPath = Settings.Instance.CloudFolderPath + path;

                // totalExpectedUploadBytes = totalExpectedUploadBytes +[event.metadata.size integerValue];
                totalExpectedUploadBytes += Convert.ToInt64(evt.Metadata.Size);

                // NSLog(@"File to be uploaded: %@, Storage Key: %@", path, storageKey);
                _trace.writeToLog(9, " CLSyncService: DispatchUploadEvents: File to be uploadedNumber of uploads to start: {0}.", events.Count);

                // __block CLHTTPConnectionOperation *uploadOperation = [self.restClient streamingUploadOperationForStorageKey:storageKey withFileSystemPath:fileSystemPath fileSize:event.metadata.size andMD5Hash:event.metadata.hash];
                CLHTTPConnectionOperation uploadOperation = _restClient.StreamingUploadOperationForStorageKey_WithFileSystemPath_FileSize_AndMd5Hash(storageKey, fileSystemPath, evt.Metadata.Size, evt.Metadata.Hash);

                //TODO: Implement this progress UI.
                // [uploadOperation setUploadProgressBlock:^(NSInteger bytesWritten, NSInteger totalBytesWritten, NSInteger totalBytesExpectedToWrite) {
                // totalUploadedBytes = totalUploadedBytes + bytesWritten;
                // NSTimeInterval now = [NSDate timeIntervalSinceReferenceDate];
                // double elapsedSeconds = now - start;
                // double secondsLeft = (((double)totalExpectedUploadBytes - (double)totalUploadedBytes) / ((double)totalUploadedBytes / elapsedSeconds));
                // double progress = (double)totalUploadedBytes / (double)totalExpectedUploadBytes;

                // //NSLog(@"Sent %ld of %ld bytes - Progress: %f", totalUploadedBytes, totalExpectedUploadBytes, progress);

                // [[CLUIActivityService sharedService] updateActivityViewWithProgress:progress
                //                                                       timeLeft:secondsLeft
                //                                                                      bytes:(double)totalUploadedBytes
                //                                                               ofTotalBytes:(double)totalExpectedUploadBytes
                //                                                                  fileCount:[self.uploadOperationQueue operationCount]
                //                                                            andActivityType:activityViewLabelUpload];
                // }];

                // [uploadOperation setOperationCompletionBlock:^(CLHTTPConnectionOperation *operation, id responseObject, NSError *error) {
                uploadOperation.SetOperationCompletionBlock((CLHTTPConnectionOperation operation, CLError error) =>
                {

                    // NSLog(@"Upload Status: %li", [operation.response statusCode]);
                    _trace.writeToLog(9, " CLSyncService: DispatchUploadEvents: Upload Status: {0}.", operation.Response.StatusCode);

                    // if ([operation.response statusCode] == 201) {
                    if (operation.Response.StatusCode == HttpStatusCode.Created)  // 201
                    {
                        // NSLog(@"Upload Completed for File: %@", path);
                        // NSLog(@"Opperations remaining: %lu", [[self.uploadOperationQueue operations] count]);
                        _trace.writeToLog(9, " CLSyncService: DispatchUploadEvents: Upload completed for file: {0}.", path);
                        _trace.writeToLog(9, " CLSyncService: DispatchUploadEvents: Operations remaining: {0}.", _uploadOperationQueue.OperationCount );

                        // update index and ui.
                        // [self performUpdateForSyncEvent:event success:YES];
                        PerformUpdateForSyncEventSuccess(evt, success: true);
                    }
                    // } else if ([operation.response statusCode] == 304){
                    else if (operation.Response.StatusCode == HttpStatusCode.NotModified)  // 304
                    {
                        // NSLog(@"The file already exists on the server");
                        _trace.writeToLog(9, " CLSyncService: DispatchUploadEvents: The file already exists on the server: {0}.", path);

                        // update index and ui.
                        // [self performUpdateForSyncEvent:event success:YES];
                        PerformUpdateForSyncEventSuccess(evt, success: true);
                    }
                    // }else {
                    else
                    {
                        // NSLog(@"Upload Failed with status:%li for File: %@",[operation.response statusCode], path);
                        _trace.writeToLog(9, " CLSyncService: DispatchUploadEvents: Upload Failed with status: {0} for File: {1}.", operation.Response.StatusCode, path);
                    }

                    // Handle a potential error
                    if (error != null) {

                        // Error handler (back processor). Likely to happen due to network interruptions.
                        // TODO: Handle the upload failure -- for now update the index to not pending.. we need to handle the error!!
                        // NSLog(@"Failed to Upload File: %@. Error: %@, Code: %ld", path, [error localizedDescription], [error code]);
                        // NSLog(@"Opperations remaining: %lu", [[self.uploadOperationQueue operations] count]);
                        _trace.writeToLog(9, " CLSyncService: DispatchUploadEvents: Failed to Upload File: {0}. Error: {1}, Code: {2}.", path, error.errorDescription, error.errorCode);
                        _trace.writeToLog(9, " CLSyncService: DispatchUploadEvents: Operations remaining: {0}.", _uploadOperationQueue.OperationCount);

                        // Update index and ui.
                        // [self performUpdateForSyncEvent:event success:NO];
                        PerformUpdateForSyncEventSuccess(evt, success: false);
                    }

                    // Update the UI
                    // if ([self.uploadOperationQueue operationCount] <= 0) {
                    if (_uploadOperationQueue.OperationCount <= 0)
                    {
                        //TODO: Implement this UI status.
                        // [[CLUIActivityService sharedService] updateActivityViewWithProgress:1 timeLeft:0 bytes:0 ofTotalBytes:0 fileCount:0 andActivityType:activityViewLabelSynced];
                    }

                });

                // [operations addObject:uploadOperation];
                operations.Add(uploadOperation);
            });

            //NSLog(@"Starting Upload Operarions");
            _trace.writeToLog(9, " CLSyncService: DispatchUploadEvents: Starting upload operations.");

            //start = [NSDate timeIntervalSinceReferenceDate];
            start = DateTime.Now;

            //[self.uploadOperationQueue addOperations:operations waitUntilFinished:YES];
            _uploadOperationQueue.AddOperations(operations);
            _uploadOperationQueue.WaitUntilFinished();

            //NSLog(@"Finished Upload Operarions");
            _trace.writeToLog(9, " CLSyncService: DispatchUploadEvents: Finished upload operations.");
        }

        void DispatchDownloadEvents(List<CLEvent> events)
        {
            //NSLog(@"%s", __FUNCTION__);

            //NSMutableArray *operations = [NSMutableArray array];

            //if (self.downloadOperationQueue == nil) {
            //    self.downloadOperationQueue = [[CLOperationQueue alloc] init];
            //    self.downloadOperationQueue.maxConcurrentOperationCount = 6;
            //}

            //__block NSInteger totalExpectedDownloadBytes = 0;
            //__block NSInteger totalDownloadedBytes = 0;
            //__block NSTimeInterval start;
            //[events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
        
            //    __block CLEvent *event = obj;
            //    NSString *path = event.metadata.path;
            //    NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:path];
        
            //    totalExpectedDownloadBytes = totalExpectedDownloadBytes +[event.metadata.size integerValue];
        
            //    NSLog(@"File to be downloaded: %@, Storage Key: %@", path, event.metadata.storage_key);
        
            //    __block CLHTTPConnectionOperation *downloadOperation = [self.restClient streamingDownloadOperationForStorageKey:event.metadata.storage_key
            //                                                                                                 withFileSystemPath:fileSystemPath
            //                                                                                                           fileSize:event.metadata.size
            //                                                                                                         andMD5Hash:event.metadata.hash];
        
            //    [downloadOperation setDownloadProgressBlock:^(NSInteger bytesRead, NSInteger totalBytesRead, NSInteger totalBytesExpectedToRead) {
            
            //        totalDownloadedBytes = totalDownloadedBytes + bytesRead;
            
            //        NSTimeInterval now = [NSDate timeIntervalSinceReferenceDate];
            //        double progress = (double)totalDownloadedBytes / (double)totalExpectedDownloadBytes;
            //        double elapsedSeconds = now - start;
            //        double secondsLeft = (((double)totalExpectedDownloadBytes - (double)totalDownloadedBytes) / ((double)totalDownloadedBytes / elapsedSeconds));
            //        [[CLUIActivityService sharedService] updateActivityViewWithProgress:progress
            //                                                                   timeLeft:secondsLeft
            //                                                                      bytes:(double)totalDownloadedBytes
            //                                                               ofTotalBytes:(double)totalExpectedDownloadBytes
            //                                                                  fileCount:[self.downloadOperationQueue operationCount]
            //                                                            andActivityType:activityViewLabelDownload];
            //    }];
        
            //    [downloadOperation setOperationCompletionBlock:^(CLHTTPConnectionOperation *operation, id responseObject, NSError *error) {
            
            //        if (!error) {
            //            if ([operation.response statusCode] == 200) {
                    
            //                NSLog(@"Download Completed for file: %@", path);
            //                NSLog(@"Opperations remaining: %lu", [[self.downloadOperationQueue operations] count]);
                    
            //                NSError *attributesError;
            //                BOOL attributesSet = [[CLFSDispatcher defaultDispatcher] updateAttributesUsingMetadata:event.metadata forItemAtPath:fileSystemPath error:&attributesError];
            //                if (attributesSet) {
            //                    if (attributesError) {
            //                        NSLog(@"%s - %@", __FUNCTION__, [attributesError description]);
            //                    }
            //                }else {
            //                    NSLog(@"Failed to update attributes in: %s", __FUNCTION__);
            //                }
                    
            //                [self performUpdateForSyncEvent:event success:YES];
                    
            //            } else {
            //                NSLog(@"%s - Download returned code: %ld", __FUNCTION__, [operation.response statusCode]);
            //               [self retryEvent:event isDownload:YES];
            //            }
                
            //        }else {
            //            // update index and ui.
            //            [self performUpdateForSyncEvent:event success:NO];
                
            //            [self retryEvent:event isDownload:YES];
                
                
            //            // Error handler (back processor). Likely to happen due to network interruptions.
            //            NSLog(@"Failed to Download File: %@. Error: %@, Code: %ld", path, [error localizedDescription], [error code]);
            //        }
            
            //        [self.activeDownloadQueue removeObject:event];
            
            
            //        if ([self.downloadOperationQueue operationCount] <= 0) {
            //            [[CLUIActivityService sharedService] updateActivityViewWithProgress:1 timeLeft:0 bytes:0 ofTotalBytes:0 fileCount:0 andActivityType:activityViewLabelSynced];
            //        }
            
            //        responseObject = nil;
            
            //    }];
        
            //    [operations addObject:downloadOperation];
            //    [self.activeDownloadQueue addObject:event];
            //}];
    
            //NSLog(@"Starting Download Operarions");
            //start = [NSDate timeIntervalSinceReferenceDate];
            //[self.downloadOperationQueue addOperations:operations waitUntilFinished:YES];
            //NSLog(@"Finished Download Operarions");
            //&&&&&

            //NSLog(@"%s", __FUNCTION__);
            _trace.writeToLog(9, " CLSyncService: DispatchDownloadEvents: Entry.");
    
            //NSMutableArray *operations = [NSMutableArray array];
            List<CLSptNSOperation> operations = new List<CLSptNSOperation>();
    
            //if (self.downloadOperationQueue == nil) {
            //    self.downloadOperationQueue = [[CLOperationQueue alloc] init];
            //    self.downloadOperationQueue.maxConcurrentOperationCount = 6;
            //}
            if (_downloadOperationQueue == null)
            {
                _downloadOperationQueue = new CLSptNSOperationQueue(maxConcurrentTasks: 6);
            }
    
            //__block NSInteger totalExpectedDownloadBytes = 0;
            //__block NSInteger totalDownloadedBytes = 0;
            //__block NSTimeInterval start;
            long totalExpectedDownloadBytes = 0;
            long totalDownloadedBytes = 0;
            DateTime start;

            //[events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
            events.ForEach(obj =>
            {
                // __block CLEvent *event = obj;
                CLEvent evt = obj;

                // NSString *path = event.metadata.path;
                string path = evt.Metadata.Path;

                // NSString *fileSystemPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:path];
                string fileSystemPath = Settings.Instance.CloudFolderPath + path;
                string storageKey = evt.Metadata.Storage_key;

                // totalExpectedDownloadBytes = totalExpectedDownloadBytes +[event.metadata.size integerValue];
                totalExpectedDownloadBytes += Convert.ToInt64(evt.Metadata.Size);

                // NSLog(@"File to be downloaded: %@, Storage Key: %@", path, event.metadata.storage_key);
                _trace.writeToLog(9, " CLSyncService: DispatchDownloadEvents: File to be downloaded: {0}. Storage key: {1}.", events.Count, storageKey);

                // __block CLHTTPConnectionOperation *downloadOperation = [self.restClient streamingDownloadOperationForStorageKey:event.metadata.storage_key
                //                                                                                                 withFileSystemPath:fileSystemPath
                //                                                                                                           fileSize:event.metadata.size
                //                                                                                                         andMD5Hash:event.metadata.hash];
                CLHTTPConnectionOperation downloadOperation = _restClient.StreamingDownloadOperationForStorageKey_WithFileSystemPath_FileSize_AndMd5Hash(storageKey, fileSystemPath, evt.Metadata.Size, evt.Metadata.Hash);

                //TODO: Implement this progress UI.
                // [downloadOperation setDownloadProgressBlock:^(NSInteger bytesRead, NSInteger totalBytesRead, NSInteger totalBytesExpectedToRead) {
                //     totalDownloadedBytes = totalDownloadedBytes + bytesRead;
                //     NSTimeInterval now = [NSDate timeIntervalSinceReferenceDate];
                //     double progress = (double)totalDownloadedBytes / (double)totalExpectedDownloadBytes;
                //     double elapsedSeconds = now - start;
                //     double secondsLeft = (((double)totalExpectedDownloadBytes - (double)totalDownloadedBytes) / ((double)totalDownloadedBytes / elapsedSeconds));
                //     [[CLUIActivityService sharedService] updateActivityViewWithProgress:progress
                //                                                                   timeLeft:secondsLeft
                //                                                                      bytes:(double)totalDownloadedBytes
                //                                                               ofTotalBytes:(double)totalExpectedDownloadBytes
                //                                                                  fileCount:[self.downloadOperationQueue operationCount]
                //                                                            andActivityType:activityViewLabelDownload];
                // }];

                // [downloadOperation setOperationCompletionBlock:^(CLHTTPConnectionOperation *operation, id responseObject, NSError *error) {
                downloadOperation.SetOperationCompletionBlock((CLHTTPConnectionOperation operation, CLError error) =>
                {
                    // if (!error) {
                    if (error == null)
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
                            PerformUpdateForSyncEventSuccess(evt, success: true);
                        }
                        else
                        {
                            // NSLog(@"%s - Download returned code: %ld", __FUNCTION__, [operation.response statusCode]);
                            _trace.writeToLog(1, " CLSyncService: DispatchDownloadEvents: ERROR: Download returned code {0} for file {1}.", operation.Response.StatusCode, path);

                            // [self retryEvent:event isDownload:YES];
                            RetryEvent(evt, isDownload: true);
                        }
                    }
                    else
                    {

                        // Update index and ui.
                        // [self performUpdateForSyncEvent:event success:NO];
                        PerformUpdateForSyncEventSuccess(evt, success: false);

                        // [self retryEvent:event isDownload:YES];
                        RetryEvent(evt, isDownload: true);

                        // Error handler (back processor). Likely to happen due to network interruptions.
                        // NSLog(@"Failed to Download File: %@. Error: %@, Code: %ld", path, [error localizedDescription], [error code]);
                        _trace.writeToLog(1, " CLSyncService: DispatchDownloadEvents: ERROR: Failed to download file {0}, error: {1}, code: {2}.", path, error.errorDescription, error.errorCode);
                    }

                    // [self.activeDownloadQueue removeObject:event];
                    _activeDownloadQueue.Remove(evt);

                    // if ([self.downloadOperationQueue operationCount] <= 0) {
                    if (_downloadOperationQueue.OperationCount <= 0)
                    {
                        //TODO: Implement this UI progress code.
                        // [[CLUIActivityService sharedService] updateActivityViewWithProgress:1 timeLeft:0 bytes:0 ofTotalBytes:0 fileCount:0 andActivityType:activityViewLabelSynced];
                    }

                    // responseObject = nil;
                    //????
                });

                // [operations addObject:downloadOperation];
                operations.Add(downloadOperation);

                // [self.activeDownloadQueue addObject:event];
                _activeDownloadQueue.Add(evt);
            });

            //NSLog(@"Starting Download Operarions");
            _trace.writeToLog(9, " CLSyncService: DispatchUploadEvents: Starting download operations.");

            //start = [NSDate timeIntervalSinceReferenceDate];
            start = DateTime.Now;

            //[self.downloadOperationQueue addOperations:operations waitUntilFinished:YES];
            _downloadOperationQueue.AddOperations(operations);
            _downloadOperationQueue.WaitUntilFinished();

            //NSLog(@"Finished Download Operarions");
            _trace.writeToLog(9, " CLSyncService: DispatchUploadEvents: Finished download operations.");
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
                _trace.writeToLog(9, "CLSyncService: SaveSyncStateWithSIDAndEID: Current SID stack contains the sid we are saving: {0}.", sid)

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
                if (sid != String.Empty)
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
            if (evt.Metadata.ToPath.Equals(String.Empty, StringComparison.InvariantCulture))
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
                if (folderPath.Equals("", StringComparison.InvariantCulture))
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
                        if (!fileEvent.Metadata.ToPath.Equals(String.Empty, StringComparison.InvariantCulture))
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
                            if (!folderEvent.Metadata.ToPath.Equals(String.Empty, StringComparison.InvariantCulture))
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
                    if (!activeEvent.Metadata.ToPath.Equals(String.Empty, StringComparison.InvariantCulture))
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
            CLUIActivityService.Instance.BadgeIconAtPath_withStatus(fileSystemPath, badge);
        }

        //- (void)retryEvent:(CLEvent *)event isDownload:(BOOL)isDownload
        void RetryEvent(CLEvent evt, bool isDownload)
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
                            _activeDownloadQueue.Add(downloadOperation);
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


