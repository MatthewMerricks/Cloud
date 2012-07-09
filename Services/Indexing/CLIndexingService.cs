//
//  CLIndexingService.cs
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

namespace win_client.Services.Indexing
{
    public enum CLEventOrigin
    {
        CLEventOriginMDS = 0,
        CLEventOriginFSM = 1,    
    };

    public enum CLIndexEventType
    {
        CLIndexEventTypeAdd = 0,
        CLIndexEventTypeModify,
        CLIndexEventTypeRenameMove,
        CLIndexEventTypeDelete,    
     };

    public sealed class CLIndexingService
    {
        private static CLIndexingService _instance = null;
        private static object _instanceLocker = new object();
        private static CLTrace _trace;

        /// <summary>
        /// Access Instance to get the singleton object.
        /// Then call methods on that instance.
        /// </summary>
        public static CLIndexingService Instance
        {
            get
            {
                lock (_instanceLocker)
                {
                    if (_instance == null)
                    {
                        _instance = new CLIndexingService();

                        // Initialize at first Instance access here
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// This is a private constructor, meaning no outsiders have access.
        /// </summary>
        private CLIndexingService()
        {
            // Initialize members, etc. here (at static initialization time).
            _trace = CLTrace.Instance;
        }

        /// <summary>
        /// Start the indexing service.
        /// </summary>
        public void StartIndexingService()
        {

        }

        //+ (CLMetadata *)metadataForItemAtCloudPath:(NSString *)path
        public CLMetadata MetadataForItemAtCloudPath(string path)
        {
            //TODO: Implement this method
            return null;
        }

        //+ (CLMetadata *)indexedMetadataForEvent:(CLEvent *)event
        public static CLMetadata IndexedMetadataForEvent(CLEvent evt)
        {
            // Merged 7/4/12
            // FileSystemItem *fileSystemItem = [self fileSystemItemForEvent:event];
            // CLMetadata *item;
            // if (fileSystemItem) {
            //       item = [[CLMetadata alloc] initWithFileSystemItem:fileSystemItem];
            // }    
            // return item;
            //&&&&

            // FileSystemItem *fileSystemItem = [self fileSystemItemForEvent:event];
            FileSystemItem fileSystemItem = FileSystemItemForEvent(evt);

            // CLMetadata *item;
            // if (fileSystemItem) {
            //       item = [[CLMetadata alloc] initWithFileSystemItem:fileSystemItem];
            // }    
            CLMetadata item = null;
            if (fileSystemItem != null)
            {
                item = new  CLMetadata(fileSystemItem);
            }

            // return item;
            return item;
        }

        //+ (FileSystemItem *)fileSystemItemForEvent:(CLEvent *)event
        FileSystemItem fileSystemItemForEvent(CLEvent evt)
        {
            // Merged 7/5/12
            // __block FileSystemItem *fileSystemItem;
            // __block NSManagedObjectContext *managedObjectContext = [[CLCoreDataController defaultController] managedObjectContext];
   
            // CLPathType pathType = CLPathStaticPath;
            // if (event.metadata.fromPath != nil){
            //     pathType = CLPathFromPath;
            // }
    
            // [managedObjectContext performBlockAndWait:^{
            //     NSManagedObjectID *objectID = [self objectIDforEvent:event typeOfPath:pathType];
        
            //     if (objectID != nil) {
            //         fileSystemItem = (FileSystemItem *)[managedObjectContext objectWithID:objectID];
            //     }
            // }];
            // return fileSystemItem;
            //&&&&

            //TODO: Implement this as a call to SQLIndexer.
            // __block FileSystemItem *fileSystemItem;
            // __block NSManagedObjectContext *managedObjectContext = [[CLCoreDataController defaultController] managedObjectContext];

            // CLPathType pathType = CLPathStaticPath;
            // if (event.metadata.fromPath != nil){
            //     pathType = CLPathFromPath;
            // }

            // [managedObjectContext performBlockAndWait:^{
            //     NSManagedObjectID *objectID = [self objectIDforEvent:event typeOfPath:pathType];

            //     if (objectID != nil) {
            //         fileSystemItem = (FileSystemItem *)[managedObjectContext objectWithID:objectID];
            //     }
            // }];
            // return fileSystemItem;
        }
    }
}