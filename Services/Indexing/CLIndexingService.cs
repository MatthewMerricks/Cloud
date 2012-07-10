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
using CloudApiPrivate.Static;
using System.IO;

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

    public enum CLPathType
    {
        CLPathToPath = 0,
        CLPathFromPath,
        CLPathStaticPath,   
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
            //TODO: Start the SQLIndexer
        }

        //+ (void)saveDataInContext:(NSManagedObjectContext *)context
        public void SaveDataInContext(object /* (NSManagedObjectContext *) */ context)
        {
            // Merged 7/9/12
            // [context performBlock: ^{
        
            //     [[NSManagedObjectContext masterContext] observeContext:context];
        
            //     BOOL saved = NO;
            //     NSError *error = nil;
        
            //     if ([context hasChanges] == YES) {
            //         //NSLog(@"psc:%@", [context persistentStoreCoordinator]);
            //         if (![context commitEditing]) {
            //             NSLog(@"%@:%@ unable to commit editing before saving", [self class], NSStringFromSelector(_cmd));
            //         }
            
            //         @autoreleasepool
            //         {
            //             saved = [context save:&error]; 
            //         }
            
            //         if (!saved) {
            //             [[context parentContext] performSelectorOnMainThread:@selector(save) withObject:nil waitUntilDone:YES];
            //             NSLog(@"%@:%@ unable to save data to managed object context", [self class], NSStringFromSelector(_cmd));
            //             if (error) {
            //                 NSLog(@"%@", [error localizedDescription]);
            //             }
            //         }
            //     }
            //     [[NSManagedObjectContext masterContext] stopObservingContext:context];
        
            //     if ([[NSManagedObjectContext masterContext] hasChanges]) {
            //         [[NSManagedObjectContext masterContext] performBlock:^{
            //             [CLIndexingServices saveDataInContext:[NSManagedObjectContext masterContext]];
            //         }];
            //     }
            // }];
            //&&&&

            //&&&&
            // Pseudo-code
#if TRASH
            perform block on context
              send the masterContext an observeContext message with the context parm
              set saved = NO
              set error = nil
              if context has changes
                commit editing on the context
                save the context and set saved with the result
                if not saved
                  save the context's parent context on the main thread and wait until it's done
                endif not saved
              endif context has changes
              tell the masterContext to stop observing changes on this context
              if the masterContext hasChanges
                performBlock on the masterContext
                  recursively call this SaveDataInContext method on masterContext
                endperformBlock on the masterContext
              endif the masterContext hasChanges
            end perform block on context
#endif  // TRASH            
            //&&&&
            // [context performBlock: ^{

            //     [[NSManagedObjectContext masterContext] observeContext:context];

            //     BOOL saved = NO;
            //     NSError *error = nil;

            //     if ([context hasChanges] == YES) {
            //         //NSLog(@"psc:%@", [context persistentStoreCoordinator]);
            //         if (![context commitEditing]) {
            //             NSLog(@"%@:%@ unable to commit editing before saving", [self class], NSStringFromSelector(_cmd));
            //         }

            //         @autoreleasepool
            //         {
            //             saved = [context save:&error]; 
            //         }

            //         if (!saved) {
            //             [[context parentContext] performSelectorOnMainThread:@selector(save) withObject:nil waitUntilDone:YES];
            //             NSLog(@"%@:%@ unable to save data to managed object context", [self class], NSStringFromSelector(_cmd));
            //             if (error) {
            //                 NSLog(@"%@", [error localizedDescription]);
            //             }
            //         }
            //     }
            //     [[NSManagedObjectContext masterContext] stopObservingContext:context];

            //     if ([[NSManagedObjectContext masterContext] hasChanges]) {
            //         [[NSManagedObjectContext masterContext] performBlock:^{
            //             [CLIndexingServices saveDataInContext:[NSManagedObjectContext masterContext]];
            //         }];
            //     }
            // }];

        }

        //+ (CLMetadata *)indexedMetadataForEvent:(CLEvent *)event
        public CLMetadata IndexedMetadataForEvent(CLEvent evt)
        {
            // Merged 7/9/12
            // FileSystemItem *fileSystemItem = [self fileSystemItemForEvent:event];
            // CLMetadata *item;
            // if (fileSystemItem) {
            //       item = [[CLMetadata alloc] initWithFileSystemItem:fileSystemItem];
            // }    
            // return item;
            //&&&&

            //&&&&
            // Pseudo-code
#if TRASH
            create a FileSystemItem for the input CLEvent
            create a CLMetadata to represent the FileSystemItem
            return the CLMetadata
#endif  // TRASH            
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
                item = new CLMetadata(fileSystemItem);
            }

            // return item;
            return item;
        }

        //+ (FileSystemItem *)fileSystemItemForEvent:(CLEvent *)event
        public static FileSystemItem FileSystemItemForEvent(CLEvent evt)
        {
            // Merged 7/9/12
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

            //&&&&
            // Pseudo-code
#if TRASH
            allocate a new FileSystemItem to hold the object to be returned
            allocate a managedObjectContext
            set the pathType to static path type (path in the Metadata Path property?)
            if event.Metadata.FromPath != null
              set the pathType to the "from path" type
            endif event.Metadata.FromPath != null
            performBlockAndWait on the managedObjectContext
              set objectId to ObjectIdForEvent_typeOfPath(event, pathType)
              if objectID != null
                retrieve the FileSystemItem from the database by key objectID
              endif objectID != null
            endperformBlockAndWait on the managedObjectContext
            return the FileSystemItem
#endif  // TRASH            
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
            return null;
        }

        //+ (void)markItemAtPath:(NSString *)path asPending:(BOOL)pending
        void MarkItemAtPath_asPending(string path, bool pending)
        {
            // Merged 7/9/12
            // // Should prob get the object ID here in the default context and then get the object for the current context to mutate?
    
            // __block NSManagedObjectContext *managedObjectContext = [[CLCoreDataController defaultController] managedObjectContext];
            // [managedObjectContext performBlockAndWait:^{

            //     FileSystemItem *item;
            //     NSManagedObjectID *objectID = [self objectIDforItemAtPath:path];
        
            //     if (objectID) {
            //         item = (FileSystemItem *)[managedObjectContext objectWithID:objectID];
            //         item.isPending = [NSNumber numberWithBool:pending];
            //     }
            // }];
            //&&&&

            //&&&&
            // Pseudo-code
#if TRASH
            get the current managedObjectContext
            performBlockAndWait on the managedObjectContext
              allocate item as a FileSystemItem
              get objectID = ObjectIdForItemAtPath(path)
              if (objectID != null)
                retrieve item from the database via key objectID
                mark item.IsPending = pending (parm)
              endif (objectID != null)
            endperformBlockAndWait on the managedObjectContext
#endif  // TRASH            
            //&&&&

            // // Should prob get the object ID here in the default context and then get the object for the current context to mutate?
    
            // __block NSManagedObjectContext *managedObjectContext = [[CLCoreDataController defaultController] managedObjectContext];
            // [managedObjectContext performBlockAndWait:^{

            //     FileSystemItem *item;
            //     NSManagedObjectID *objectID = [self objectIDforItemAtPath:path];
        
            //     if (objectID) {
            //         item = (FileSystemItem *)[managedObjectContext objectWithID:objectID];
            //         item.isPending = [NSNumber numberWithBool:pending];
            //     }
            // }];
        }

        //+ (void)markItemForEvent:(CLEvent *)event asPending:(BOOL)pending
        public void MarkItemForEvent_asPending(CLEvent evt, bool pending)
        {
            // Merged 7/9/12
            // FileSystemItem *itemToChange = [self fileSystemItemForEvent:event];
            // itemToChange.isPending = [NSNumber numberWithBool:pending];
            //&&&&

            //&&&&
            // Pseudo-code
#if TRASH
            set FileSystemItem itemToChange = FileSystemItemForEvent(event)
            set itemToChange.IsPending = pending
#endif  // TRASH            
            //&&&&

            // FileSystemItem *itemToChange = [self fileSystemItemForEvent:event];
            // itemToChange.isPending = [NSNumber numberWithBool:pending];
        }

        //+ (FileSystemItem *)folderItemForCloudPath:(NSString *)path
        FileSystemItem FolderItemForCloudPath(string path)
        {
            // Merged 7/9/12
            // NSManagedObjectContext *context = [[CLCoreDataController defaultController] managedObjectContext];
            // CLEvent *event = [[CLEvent alloc] init];
            // CLMetadata *meta = [[CLMetadata alloc] init];

            // meta.path = path;
    
            // NSManagedObjectID *objectID = [self objectIDforEvent:event typeOfPath:CLPathStaticPath];
            // if (objectID) {
            //      return (FileSystemItem *)[context objectWithID:objectID];
            // }
    
            // return nil;
            //&&&&

            //&&&&
            // Pseudo-code
#if TRASH
            get the current managedObjectContext to context
            allocate an event as CLEvent
            allocate meta as CLMetadata
            set meta.path = path
            get objectID = ObjectIdForEvent_typeOfPath(event, CLPathStaticPath)
            if objectID != null
              return FileSystemItem retrieved from database via key objectID
            endif objectID != null
            return null
#endif  // TRASH            
            //&&&&

            // NSManagedObjectContext *context = [[CLCoreDataController defaultController] managedObjectContext];
            // CLEvent *event = [[CLEvent alloc] init];
            // CLMetadata *meta = [[CLMetadata alloc] init];

            // meta.path = path;
    
            // NSManagedObjectID *objectID = [self objectIDforEvent:event typeOfPath:CLPathStaticPath];
            // if (objectID) {
            //      return (FileSystemItem *)[context objectWithID:objectID];
            // }
    
            // return nil;
            return null; //&&&& remove
        }

        //+ (NSManagedObjectID *)objectIDforItemAtPath:(NSString *)path
        object /* NSManagedObjectID * */ ObjectIdForItemAtPath(string path)
        {
            // Merged 7/9/12
            // __block NSManagedObjectID *objectID;
            // __block NSManagedObjectContext *managedObjectContext = [[CLCoreDataController defaultController] managedObjectContext];
    
            // [managedObjectContext performBlockAndWait:^{
        
            //     NSFetchRequest *request = [NSFetchRequest fetchRequestWithEntityName:@"FileSystemItem"];
            //     NSPredicate *predicate = [NSPredicate predicateWithFormat:@"path == %@", path];
            //     [request setPredicate:predicate];
            //     [request setResultType:NSManagedObjectIDResultType];
        
            //     NSError *error;
            //     NSArray *array = [managedObjectContext executeFetchRequest:request error:&error];
            //     if (error) {
            //         NSLog(@"%@", [error localizedDescription]);
            //     } else {                
            //         if ([array count]) {
            //             objectID = [array objectAtIndex:0];
            //         }
            //     }
            // }];
    
            // return objectID;
            //&&&&

            //&&&&
            // Pseudo-code
#if TRASH
            allocate objectID as a database key and set to null
            allocate managedObjectContext as current managedObjectContext
            performBlockAndWait on the managedObjectContext
              query the database for the objectID of a FileSystemItem where path==path
              if error
                log error
              else no error
                get the result of the query to objectID
              endelse no error
            performBlockAndWait on the managedObjectContext
            return objectID
#endif  // TRASH            
            //&&&&

            // __block NSManagedObjectID *objectID;
            // __block NSManagedObjectContext *managedObjectContext = [[CLCoreDataController defaultController] managedObjectContext];
    
            // [managedObjectContext performBlockAndWait:^{
        
            //     NSFetchRequest *request = [NSFetchRequest fetchRequestWithEntityName:@"FileSystemItem"];
            //     NSPredicate *predicate = [NSPredicate predicateWithFormat:@"path == %@", path];
            //     [request setPredicate:predicate];
            //     [request setResultType:NSManagedObjectIDResultType];
        
            //     NSError *error;
            //     NSArray *array = [managedObjectContext executeFetchRequest:request error:&error];
            //     if (error) {
            //         NSLog(@"%@", [error localizedDescription]);
            //     } else {                
            //         if ([array count]) {
            //             objectID = [array objectAtIndex:0];
            //         }
            //     }
            // }];
    
            // return objectID;
            return null;  //&&&& remove
        }

        ////  Pass nil as fileName if item is a folder item, else it will return a file item.
        //+ (NSManagedObjectID *)objectIDforItemAtPath:(NSString *)path andFileName:(NSString *)fileName
        object /* NSManagedObjectID * */ ObjectIdForItemAtPath_andFileName(string path, string fileName)
        {
            // Merged 7/9/12
            // __block NSManagedObjectID *objectID;
            // __block NSManagedObjectContext *managedObjectContext = [[CLCoreDataController defaultController] managedObjectContext];
    
            // [managedObjectContext performBlockAndWait:^{       
        
            //     NSFetchRequest *request = [NSFetchRequest fetchRequestWithEntityName:@"FileSystemItem"];
            //     NSPredicate *predicate = nil;
        
            //     predicate = (fileName) ? [NSPredicate predicateWithFormat:@"parent.path = %@ AND name = %@", path, fileName] : [NSPredicate predicateWithFormat:@"path == %@", path];

            //     [request setPredicate:predicate];
            //     [request setResultType:NSManagedObjectIDResultType];
        
            //     NSError *error;
            //     NSArray *array = [managedObjectContext executeFetchRequest:request error:&error];
            //     if (error) {
            //         NSLog(@"%@", [error localizedDescription]);
            //     } else {                
            //         if ([array count]) {
            //             objectID = [array objectAtIndex:0];
            //         }
            //     }
            // }];
    
            // return objectID;
            //&&&&

            //&&&&
            // Pseudo-code
#if TRASH
            allocate an objectID and set to null
            get the current managedObjectContext
            performBlockAndWait on managedObjectContext
              if fileName != null
                query the database for the object ID of a FileSystemItem where parent.path==path AND name == fileName 
              else fileName == null
                query the database for the object ID of a FileSystemItem where path==path 
              endelse fileName == null
              if error
                log error
              else no error
                get the result of the query to objectID
              endelse no error
            performBlockAndWait on managedObjectContext
            return objectID
#endif  // TRASH            
            //&&&&

            // __block NSManagedObjectID *objectID;
            // __block NSManagedObjectContext *managedObjectContext = [[CLCoreDataController defaultController] managedObjectContext];
    
            // [managedObjectContext performBlockAndWait:^{       
        
            //     NSFetchRequest *request = [NSFetchRequest fetchRequestWithEntityName:@"FileSystemItem"];
            //     NSPredicate *predicate = nil;
        
            //     predicate = (fileName) ? [NSPredicate predicateWithFormat:@"parent.path = %@ AND name = %@", path, fileName] : [NSPredicate predicateWithFormat:@"path == %@", path];

            //     [request setPredicate:predicate];
            //     [request setResultType:NSManagedObjectIDResultType];
        
            //     NSError *error;
            //     NSArray *array = [managedObjectContext executeFetchRequest:request error:&error];
            //     if (error) {
            //         NSLog(@"%@", [error localizedDescription]);
            //     } else {                
            //         if ([array count]) {
            //             objectID = [array objectAtIndex:0];
            //         }
            //     }
            // }];
    
            // return objectID;
            return null;  //&&&& remove
        }

        //+ (NSManagedObjectID *)objectIDforEvent:(CLEvent *)event typeOfPath:(CLPathType)pathType
        object /* NSManagedObjectID * */ ObjectIdForEvent_typeOfPath(CLEvent evt, CLPathType pathType)
        {
            // Merged 7/9/12
            // __block NSManagedObjectID *objectID;
            // NSString *itemPath = nil;
            // NSString *itemFilename = nil;
            // BOOL isFolder = event.metadata.isDirectory;
    
            // switch (pathType) {
            //     case CLPathFromPath:
            //         itemPath = event.metadata.fromPath;
            //         break;
            //     case CLPathStaticPath:
            //         itemPath = event.metadata.path;
            //         break;
            //     case CLPathToPath:
            //         // Try to use stringByDeletingLastPathComponent
            //         itemPath = [event.metadata.toPath stringByReplacingOccurrencesOfString:[NSString stringWithFormat:@"%@/", [event.metadata.toPath lastPathComponent]] withString:@""];
            //     default:
            //         break;
            // }
    
            // if (!isFolder) {
            //     itemFilename = [itemPath lastPathComponent];
            //     itemPath = [self parentPathForCloudPath:itemPath];
            // }
      
            // __block NSManagedObjectContext *managedObjectContext = [[CLCoreDataController defaultController] managedObjectContext];
    
            // [managedObjectContext performBlockAndWait:^{
        
            //     NSFetchRequest *request = [NSFetchRequest fetchRequestWithEntityName:@"FileSystemItem"];
            //     NSPredicate *predicate = nil;
        
            //     predicate = (!isFolder && pathType != CLPathToPath) ? [NSPredicate predicateWithFormat:@"parent.path = %@ AND name = %@", itemPath, itemFilename] : [NSPredicate predicateWithFormat:@"path == %@", itemPath];
        
            //     [request setPredicate:predicate];
            //     [request setResultType:NSManagedObjectIDResultType];
        
            //     NSError *error;
            //     NSArray *array = [managedObjectContext executeFetchRequest:request error:&error];
            //     if (error) {
            //         NSLog(@"%@", [error localizedDescription]);
            //     } else {
            //         if ([array count]) {
            //             objectID = [array objectAtIndex:0];
            //         }
            //     }
            // }];
    
            // return objectID;
            //&&&&

            //&&&&
            // Pseudo-code
#if TRASH
            allocate an objectID and set it to null
            allocate itemPath and set it to null
            allocate itemFilename and set it to null
            allocate isFolder = event.Metadata.IsDirectory
            switch (pathType)
            case CLPathFromPath:
              itemPath = event.Metadata.FromPath
            case CLPathStaticPath:
              itemPath = event.Metadata.Path
            case CLPathToPath:
              itemPath = event.Metadata.ToPath minus the filename.ext (last path component).  i.e., the directory containing the file item.
            default:
              break;
            end switch (pathType)
            if (!isFolder)
              itemFilename = itemPath.LastPathComponent
              itemPath = ParentPathForCloudPath(itemPath)
            endif (!isFolder)
            get the current managedObjectContext
            performBlockAndWait on managedObjectContext
              if not a folder, and not type CLPathToPath
                query the database for a FileSystemItem where parent.path==itemPath AND name == itemFilename
              else this is a folder, or it is type CLPathToPath
                query the database for a FileSystemItem where path==itemPath
              endelse this is a folder, or it is type CLPathToPath
              if error
                log error
              else no error
                get the result of the query to objectID
              endelse no error
            performBlockAndWait on managedObjectContext
            return objectID
#endif  // TRASH            
            //&&&&

            // __block NSManagedObjectID *objectID;
            // NSString *itemPath = nil;
            // NSString *itemFilename = nil;
            // BOOL isFolder = event.metadata.isDirectory;
    
            // switch (pathType) {
            //     case CLPathFromPath:
            //         itemPath = event.metadata.fromPath;
            //         break;
            //     case CLPathStaticPath:
            //         itemPath = event.metadata.path;
            //         break;
            //     case CLPathToPath:
            //         // Try to use stringByDeletingLastPathComponent
            //         itemPath = [event.metadata.toPath stringByReplacingOccurrencesOfString:[NSString stringWithFormat:@"%@/", [event.metadata.toPath lastPathComponent]] withString:@""];
            //     default:
            //         break;
            // }
    
            // if (!isFolder) {
            //     itemFilename = [itemPath lastPathComponent];
            //     itemPath = [self parentPathForCloudPath:itemPath];
            // }
    
    
            // __block NSManagedObjectContext *managedObjectContext = [[CLCoreDataController defaultController] managedObjectContext];
    
            // [managedObjectContext performBlockAndWait:^{
        
            //     NSFetchRequest *request = [NSFetchRequest fetchRequestWithEntityName:@"FileSystemItem"];
            //     NSPredicate *predicate = nil;
        
            //     predicate = (!isFolder && pathType != CLPathToPath) ? [NSPredicate predicateWithFormat:@"parent.path = %@ AND name = %@", itemPath, itemFilename] : [NSPredicate predicateWithFormat:@"path == %@", itemPath];
        
            //     [request setPredicate:predicate];
            //     [request setResultType:NSManagedObjectIDResultType];
        
            //     NSError *error;
            //     NSArray *array = [managedObjectContext executeFetchRequest:request error:&error];
            //     if (error) {
            //         NSLog(@"%@", [error localizedDescription]);
            //     } else {
            //         if ([array count]) {
            //             objectID = [array objectAtIndex:0];
            //         }
            //     }
            // }];
    
            // return objectID;
            return null; //&&&& remove

        }

        //+ (void)addMetedataItem:(CLMetadata *)item pending:(BOOL)pending
        public void AddMetadataItem_pending(CLMetadata item, bool pending)
        {
            // Merged 7/9/12
            // __block NSManagedObjectContext *managedObjectContext = [[CLCoreDataController defaultController] managedObjectContext];
            // [managedObjectContext performBlockAndWait:^{
        
            //     FileSystemItem *parentItem;
            //     NSString *itemParentPath = [CLIndexingServices parentPathForCloudPath:item.path];
                                    
            //     if ([itemParentPath length] && ![itemParentPath isEqualToString:item.path]) {
            //         CLEvent *event = [[CLEvent alloc] init];
            //         CLMetadata *meta = [[CLMetadata alloc] init];
            //         meta.path = itemParentPath;
            //         meta.isDirectory = YES;
            //         event.metadata = meta;
            
            //         NSManagedObjectID *objectID = [self objectIDforEvent:event typeOfPath:CLPathStaticPath];
            //         if (objectID) {
            //             parentItem = (FileSystemItem *)[managedObjectContext objectWithID:objectID]; //[self folderItemForCloudPath:itemParentPath];
            //         }
            
            //         //NSLog(@"Parent Item Path: %@", parentItem.path);
            
            //     }else{
            //         itemParentPath = nil;
            //     }
           
            //     FileSystemItem *fsItem = [NSEntityDescription insertNewObjectForEntityForName:@"FileSystemItem" inManagedObjectContext:managedObjectContext];
        
            //     if (item.isDirectory) {
            //         fsItem.path = item.path;
            //     }else {
            //         fsItem.name = [item.path lastPathComponent];
            //      }
        
            //     if (item.isLink) {
            //         fsItem.targetPath = item.targetPath;
            //     }
        
            //     fsItem.md5hash      = item.hash;
            //     fsItem.revision     = item.revision;
            //     fsItem.createDate   = item.createDate;
            //     fsItem.modifiedDate = item.modifiedDate;
            //     fsItem.size         = item.size;
            //     fsItem.targetPath   = item.targetPath;
            //     fsItem.is_Deleted   = [NSNumber numberWithBool:item.isDeleted];
            //     fsItem.is_Directory = [NSNumber numberWithBool:item.isDirectory];
            //     fsItem.isPending    = [NSNumber numberWithBool:pending];
        
            //     if (parentItem) {
            //         fsItem.parent = parentItem;
            //         NSMutableSet *children = [parentItem mutableSetValueForKey:@"children"];
            //         [children addObject:fsItem];
            //         fsItem.parent = parentItem;
            //     }
            // }];
            //&&&&

            //&&&&
            // Pseudo-code
#if TRASH
            allocate the current managedObjectContext
            performBlockAndWait on managedObjectContext
              Allocate parentItem as FileSystemItem and set to null
              allocate string itemParentPath = ParentPathForCloudPath(item.path)
              if itemParentPath.Length != 0 && itemParentPath != item.path
                allocate event as CLEvent
                allocate meta as CLMetadata
                meta.path = itemParentPath
                meta.IsDirectory = true
                event.Metadata = meta
                get objectID = ObjectIdForEvent_typeOfPath(event, CLPathStaticPath)
                if objectID != null
                  retrieve parentItem from database via key objectID
                endif objectID != null
              else itemParentPath.Length == 0 || itemParentPath == item.path
                itemParentPath = null
              endelse itemParentPath.Length == 0 || itemParentPath == item.path
              create new item fsItem as FileSystemItem in the database via managedObjectContext
              if item.IsDiretory
                fsItem.path = item.path
              else !item.IsDirectory
                fsItem.name = item.path.LastPathComponent
              endelse !item.IsDirectory
              if item.IsLink
                fsItem.TargetPath = item.TargetPath
              endif item.IsLink
              fsItem.md5hash      = item.hash;
              fsItem.revision     = item.revision;
              fsItem.createDate   = item.createDate;
              fsItem.modifiedDate = item.modifiedDate;
              fsItem.size         = item.size;
              fsItem.targetPath   = item.targetPath;
              fsItem.is_Deleted   = item.isDeleted
              fsItem.is_Directory = item.isDirectory
              fsItem.isPending    = pending
              if parentItem != null
                fsItem.parent = parentItem
                allocate a set of children and add this set to the parentItem with key "children"
                add fsItem to the children set
                set fsItem.parent = parentItem
              endif parentItem != null           
            endperformBlockAndWait on managedObjectContext
#endif  // TRASH            
            //&&&&

            // __block NSManagedObjectContext *managedObjectContext = [[CLCoreDataController defaultController] managedObjectContext];
            // [managedObjectContext performBlockAndWait:^{
        
            //     FileSystemItem *parentItem;
            //     NSString *itemParentPath = [CLIndexingServices parentPathForCloudPath:item.path];
                                    
            //     if ([itemParentPath length] && ![itemParentPath isEqualToString:item.path]) {
            //         CLEvent *event = [[CLEvent alloc] init];
            //         CLMetadata *meta = [[CLMetadata alloc] init];
            //         meta.path = itemParentPath;
            //         meta.isDirectory = YES;
            //         event.metadata = meta;
            
            //         NSManagedObjectID *objectID = [self objectIDforEvent:event typeOfPath:CLPathStaticPath];
            //         if (objectID) {
            //             parentItem = (FileSystemItem *)[managedObjectContext objectWithID:objectID]; //[self folderItemForCloudPath:itemParentPath];
            //         }
            
            //         //NSLog(@"Parent Item Path: %@", parentItem.path);
            
            //     }else{
            //         itemParentPath = nil;
            //     }
           
            //     FileSystemItem *fsItem = [NSEntityDescription insertNewObjectForEntityForName:@"FileSystemItem" inManagedObjectContext:managedObjectContext];
        
            //     if (item.isDirectory) {
            //         fsItem.path = item.path;
            //     }else {
            //         fsItem.name = [item.path lastPathComponent];
            //      }
        
            //     if (item.isLink) {
            //         fsItem.targetPath = item.targetPath;
            //     }
        
            //     fsItem.md5hash      = item.hash;
            //     fsItem.revision     = item.revision;
            //     fsItem.createDate   = item.createDate;
            //     fsItem.modifiedDate = item.modifiedDate;
            //     fsItem.size         = item.size;
            //     fsItem.targetPath   = item.targetPath;
            //     fsItem.is_Deleted   = [NSNumber numberWithBool:item.isDeleted];
            //     fsItem.is_Directory = [NSNumber numberWithBool:item.isDirectory];
            //     fsItem.isPending    = [NSNumber numberWithBool:pending];
        
            //     if (parentItem) {
            //         fsItem.parent = parentItem;
            //         NSMutableSet *children = [parentItem mutableSetValueForKey:@"children"];
            //         [children addObject:fsItem];
            //         fsItem.parent = parentItem;
            //     }
            // }];
        }

        //+ (void)updateLocalIndexItemWithEvent:(CLEvent *)event pending:(BOOL)pending
        public void UpdateLocalIndexItemWithEvent_pending(CLEvent evt, bool pending)
        {
            // Merged 7/9/12
            // __block NSManagedObjectContext *managedObjectContext = [[CLCoreDataController defaultController] managedObjectContext];

            // [managedObjectContext performBlockAndWait:^{
        
            //     BOOL isMove = ([event.syncHeader.action rangeOfString:CLEventTypeMoveRange].location != NSNotFound);
            //     BOOL isRename = ([event.syncHeader.action rangeOfString:CLEventTypeRenameRange].location != NSNotFound);
        
            //     FileSystemItem *itemFromIndex = [self fileSystemItemForEvent:event];
        
            //     FileSystemItem *newParentItem;
            //     NSManagedObjectID *newParentObjectID;
        
            //     if (itemFromIndex) {
            //         if (isMove == YES) {
            //             newParentObjectID = [self objectIDforEvent:event typeOfPath:CLPathToPath];
            //             newParentItem = (FileSystemItem *)[managedObjectContext objectWithID:newParentObjectID];
            //         }
            //         if (isMove == YES || isRename == YES) {
            //             if (event.metadata.isDirectory == YES){
            //                 itemFromIndex.path = event.metadata.toPath;
            //                 newParentObjectID = [self objectIDforEvent:event typeOfPath:CLPathToPath];
            //                 newParentItem = (FileSystemItem *)[managedObjectContext objectWithID:newParentObjectID];
            //             }
            //         }
            //         if (event.metadata.isDirectory == NO && (isMove == NO || isRename == YES)) {
            //             if ([event.syncHeader.action rangeOfString:CLEventTypeModifyRange].location == NSNotFound){
            //                 itemFromIndex.name = [event.metadata.toPath lastPathComponent];
            //             }
            //         }
        
            //         if (event.metadata.modifiedDate != nil)
            //             itemFromIndex.modifiedDate = event.metadata.modifiedDate;
            //         if (event.metadata.revision != nil)
            //             itemFromIndex.revision = event.metadata.revision;
            //         if (event.metadata.size != nil)
            //             itemFromIndex.size = event.metadata.size;
            //         if (event.metadata.hash != nil)
            //             itemFromIndex.md5hash = event.metadata.hash;
            //         if (event.metadata.targetPath != nil) {
            //             itemFromIndex.targetPath = event.metadata.targetPath;
            //         }
            
            //         if (newParentItem != nil) {
            //             NSMutableSet *children = [newParentItem mutableSetValueForKey:@"children"];
            //             [children addObject:itemFromIndex];
            //             newParentItem.children = children;
            //             itemFromIndex.parent = newParentItem;
            //         }
            
            //         itemFromIndex.isPending = [NSNumber numberWithBool:pending];
            //     }else {
            //         NSLog(@"%s - Could not find item in Index!", __FUNCTION__);
            //     }
            // }];
            //&&&&

            //&&&&
            // Pseudo-code
#if TRASH
            allocate the current managedObjectContext
            performBlockAndWait on managedObjectContext
              set bool isMove = event.SyncHeader.Action.Contains(CLEventTypeMoveRange)
              set bool isRename = event.SyncHeader.Action.Contains(CLEventTypeRenameRange)
              set FileSystemItem itemFromIndex = FileSystemItemForEvent(event)
              allocate FileSystemItem newParentItem
              allocate ObjectId newParentObjectID
              if itemFromIndex != null
                if isMove
                  set newParentObjectID = ObjectIdForEvent_typeOfPath(event, CLPathToPath)
                  set newParentItem = query FileSystemItem from database with key newParentObjectID
                endif isMove
                if isMove && isRename
                    if event.metadata.isDirectory
                        itemFromIndex.path = event.metadata.toPath;
                        newParentObjectID = ObjectIdforEvent_typeOfPath(event, CLPathToPath]
                        newParentItem = query FileSystemItem from database with key newParentObjectID
                    endif event.metadata.isDirectory
                endif isMove && isRename
                if (!event.metadata.isDirectory && (!isMove || isRename)
                    if event.syncHeader.action.Contains(CLEventTypeModifyRange)
                        itemFromIndex.name = event.metadata.toPath.lastPathComponent
                    endif event.syncHeader.action.Contains(CLEventTypeModifyRange)
                endif (!event.metadata.isDirectory && (!isMove || isRename)
                if event.metadata.modifiedDate != null
                    itemFromIndex.modifiedDate = event.metadata.modifiedDate
                endif event.metadata.modifiedDate != null
                if event.metadata.revision != null
                    itemFromIndex.revision = event.metadata.revision
                endif event.metadata.revision != null
                if event.metadata.size != null
                    itemFromIndex.size = event.metadata.size
                endif event.metadata.size != null
                if event.metadata.hash != null
                    itemFromIndex.md5hash = event.metadata.hash
                endif event.metadata.hash != null
                if event.metadata.targetPath != null
                    itemFromIndex.targetPath = event.metadata.targetPath
                endif event.metadata.targetPath != null
                if newParentItem != null
                    allocate a new set children for newParentItem, key "children"
                    add itemFromIndex to set children
                    set newParentItem.children = children
                    set itemFromIndex.parent = newParentItem
                endif newParentItem != null
                itemFromIndex.isPending = pending
              else itemFromIndex == null
                log this error
              endelse itemFromIndex == null
            endperformBlockAndWait on managedObjectContext
#endif  // TRASH            
            //&&&&

            // __block NSManagedObjectContext *managedObjectContext = [[CLCoreDataController defaultController] managedObjectContext];

            // [managedObjectContext performBlockAndWait:^{
        
            //     BOOL isMove = ([event.syncHeader.action rangeOfString:CLEventTypeMoveRange].location != NSNotFound);
            //     BOOL isRename = ([event.syncHeader.action rangeOfString:CLEventTypeRenameRange].location != NSNotFound);
        
            //     FileSystemItem *itemFromIndex = [self fileSystemItemForEvent:event];
        
            //     FileSystemItem *newParentItem;
            //     NSManagedObjectID *newParentObjectID;
        
            //     if (itemFromIndex) {
            //         if (isMove == YES) {
            //             newParentObjectID = [self objectIDforEvent:event typeOfPath:CLPathToPath];
            //             newParentItem = (FileSystemItem *)[managedObjectContext objectWithID:newParentObjectID];
            //         }
            //         if (isMove == YES || isRename == YES) {
            //             if (event.metadata.isDirectory == YES){
            //                 itemFromIndex.path = event.metadata.toPath;
            //                 newParentObjectID = [self objectIDforEvent:event typeOfPath:CLPathToPath];
            //                 newParentItem = (FileSystemItem *)[managedObjectContext objectWithID:newParentObjectID];
            //             }
            //         }
            //         if (event.metadata.isDirectory == NO && (isMove == NO || isRename == YES)) {
            //             if ([event.syncHeader.action rangeOfString:CLEventTypeModifyRange].location == NSNotFound){
            //                 itemFromIndex.name = [event.metadata.toPath lastPathComponent];
            //             }
            //         }
        
            //         if (event.metadata.modifiedDate != nil)
            //             itemFromIndex.modifiedDate = event.metadata.modifiedDate;
            //         if (event.metadata.revision != nil)
            //             itemFromIndex.revision = event.metadata.revision;
            //         if (event.metadata.size != nil)
            //             itemFromIndex.size = event.metadata.size;
            //         if (event.metadata.hash != nil)
            //             itemFromIndex.md5hash = event.metadata.hash;
            //         if (event.metadata.targetPath != nil) {
            //             itemFromIndex.targetPath = event.metadata.targetPath;
            //         }
            
            //         if (newParentItem != nil) {
            //             NSMutableSet *children = [newParentItem mutableSetValueForKey:@"children"];
            //             [children addObject:itemFromIndex];
            //             newParentItem.children = children;
            //             itemFromIndex.parent = newParentItem;
            //         }
            
            //         itemFromIndex.isPending = [NSNumber numberWithBool:pending];
            //     }else {
            //         NSLog(@"%s - Could not find item in Index!", __FUNCTION__);
            //     }
            // }];

        }

        //+ (void)removeMetadataItemWithCloudPath:(NSString *)path
        public void RemoveMetadataItemWithCloudPath(string path)
        {
            // Merged 7/9/12
            // NSManagedObjectContext *managedObjectContext = [[CLCoreDataController defaultController] managedObjectContext];
            // [managedObjectContext performBlockAndWait:^{
        
            //     NSEntityDescription *entityDescription = [NSEntityDescription entityForName:@"FileSystemItem" inManagedObjectContext:managedObjectContext];
            //     NSFetchRequest *request = [[NSFetchRequest alloc] init];
            //     [request setEntity:entityDescription];
        
            //     NSPredicate *predicate = [NSPredicate predicateWithFormat:@"path = %@", path];
            //     [request setPredicate:predicate];
        
            //     NSError *error;
            //     NSArray *array = [[NSManagedObjectContext defaultContext] executeFetchRequest:request error:&error];
        
            //     FileSystemItem *fetchedFileSystemItem;
            //     if (array == nil) {
            //         NSLog(@"%@", [error localizedDescription]);
            //     }
            //     else if((array != nil) && [array count]) {
            //         fetchedFileSystemItem = [array objectAtIndex:0];
            //         NSSet *children = fetchedFileSystemItem.children;
            //         [children enumerateObjectsUsingBlock:^(id obj, BOOL *stop) {
            //             FileSystemItem *child = obj;
            //             NSLog(@"Path of item to remove: %@",child.path);
            //          }];
            //     }
        
            //     if ([path isEqualToString:fetchedFileSystemItem.path]) {
            //         [managedObjectContext deleteObject:fetchedFileSystemItem];
            //     }
            // }];
            //&&&&

            //&&&&
            // Pseudo-code
#if TRASH
            allocate the current managedObjectContext
            performBlockAndWait on managedObjectContext
              query FileSystemItem from database where path==path
              if nothing found
                log the error
              else object found
                get the found object as fetchedFileSystemItem
                set children = fetchedFileSystemItem.children
                iterate through children
                  log every child.path as path of item to remove (but don't remove it???) Bug?
                enditerate through children
              endelse object found
              if fetchedFileSystemItem != null && fetchedFileSystemItem.path == path
                delete the fetchedFileSystemItem in the database
              endif fetchedFileSystemItem != null && fetchedFileSystemItem.path == path
            endperformBlockAndWait on managedObjectContext
#endif  // TRASH            
            //&&&&

            // NSManagedObjectContext *managedObjectContext = [[CLCoreDataController defaultController] managedObjectContext];
            // [managedObjectContext performBlockAndWait:^{
        
            //     NSEntityDescription *entityDescription = [NSEntityDescription entityForName:@"FileSystemItem" inManagedObjectContext:managedObjectContext];
            //     NSFetchRequest *request = [[NSFetchRequest alloc] init];
            //     [request setEntity:entityDescription];
        
            //     NSPredicate *predicate = [NSPredicate predicateWithFormat:@"path = %@", path];
            //     [request setPredicate:predicate];
        
            //     NSError *error;
            //     NSArray *array = [[NSManagedObjectContext defaultContext] executeFetchRequest:request error:&error];
        
            //     FileSystemItem *fetchedFileSystemItem;
            //     if (array == nil) {
            //         NSLog(@"%@", [error localizedDescription]);
            //     }
            //     else if((array != nil) && [array count]) {
            //         fetchedFileSystemItem = [array objectAtIndex:0];
            //         NSSet *children = fetchedFileSystemItem.children;
            //         [children enumerateObjectsUsingBlock:^(id obj, BOOL *stop) {
            //             FileSystemItem *child = obj;
            //             NSLog(@"Path of item to remove: %@",child.path);
            //          }];
            //     }
        
            //     if ([path isEqualToString:fetchedFileSystemItem.path]) {
            //         [managedObjectContext deleteObject:fetchedFileSystemItem];
            //     }
            // }];
        }

        //+ (void)removeItemForEvent:(CLEvent *)event
        public void RemoveItemForEvent(CLEvent evt)
        {
            // Merged 7/9/12
            // NSManagedObjectContext *defaultContext = [[CLCoreDataController defaultController] managedObjectContext];
            // NSManagedObjectID *objectIDToDelete = [self objectIDforEvent:event typeOfPath:CLPathStaticPath];
    
            // FileSystemItem *itemToDelete;
            // if (objectIDToDelete){
            //     itemToDelete = (FileSystemItem *)[defaultContext objectWithID:objectIDToDelete];
            // }
            // if (itemToDelete) {
            //      [defaultContext deleteObject:itemToDelete];
            // }
            //&&&&

            // NSManagedObjectContext *defaultContext = [[CLCoreDataController defaultController] managedObjectContext];
            // NSManagedObjectID *objectIDToDelete = [self objectIDforEvent:event typeOfPath:CLPathStaticPath];
    
            // FileSystemItem *itemToDelete;
            // if (objectIDToDelete){
            //     itemToDelete = (FileSystemItem *)[defaultContext objectWithID:objectIDToDelete];
            // }
            // if (itemToDelete) {
            //      [defaultContext deleteObject:itemToDelete];
            // }
        }

        //+ (void)moveItemAtPath:(NSString *)fromPath toPath:(NSString *)toPath
        void MoveItemAtPath_toPath(string fromPath, string toPath)
        {
            // Merged 7/9/12
            // __block NSManagedObjectContext *managedObjectContext = [[CLCoreDataController defaultController] managedObjectContext];

            // [managedObjectContext performBlockAndWait:^{
            //     NSLog(@"From Path: %@", fromPath);
            //     NSLog(@"To Path : %@", toPath);
        
            //     FileSystemItem *itemBeingMoved;
        
            //     CLEvent *event = [[CLEvent alloc] init];
            //     CLMetadata *meta = [[CLMetadata alloc] init];
            //     event.action = CLEventTypeRenameFile;
            //     meta.toPath = toPath;
            //     meta.fromPath = fromPath;
            //     meta.path = fromPath;
        
            //     event.metadata = meta;
        
            //     NSManagedObjectID *objectID =  [self objectIDforEvent:event typeOfPath:CLPathFromPath];
            //     if (objectID) {
            //         itemBeingMoved = (FileSystemItem *)[managedObjectContext objectWithID:objectID];
            //     }
        
            //     NSLog(@"ItemBeingMoved: %@", itemBeingMoved.path);
        
            //     FileSystemItem *newParentItem;

            //     NSManagedObjectID *parentObjectID = [self objectIDforEvent:event typeOfPath:CLPathToPath];
        
            //     if (parentObjectID) {
            //         newParentItem = (FileSystemItem *)[managedObjectContext objectWithID:parentObjectID];
            //         [itemBeingMoved setParent:newParentItem];
            //     }

            // }];
            //&&&&

            //&&&&
            // Pseudo-code
#if TRASH
            allocate the current managedObjectContext
            performBlockAndWait on managedObjectContext
              allocate itemBeingMoved as FileSystemItem
              allocate event as CLEvent
              allocate meta as CLMetadata
              set event.Action = CLEventTypeRenameFile
              set meta.ToPath = toPath
              set meta.FromPath = fromPath
              set meta.Path = fromPath
              set event.Metadata = meta

              find objectId = ObjectIdForEvent_typeOfPath(event, CLPathFromPath)
              if objectId != null
                set itemBeingMoved = query FileSystemItem from the database, key objectId
              endif objectId != null
              allocate newParentItem as FileSystemItem
              find parentObjectId = ObjectIdForEvent_typeOfPath(event, CLPathToPath)
              if parentObjectId != null
                set newParentItem = query database for FileSystemItem, key parentObjectId
                set itemBeingMoved.parent = newParentItem (to database via entity)
              endif parentObjectId != null
            endperformBlockAndWait on managedObjectContext
#endif  // TRASH            
            //&&&&

            // __block NSManagedObjectContext *managedObjectContext = [[CLCoreDataController defaultController] managedObjectContext];

            // [managedObjectContext performBlockAndWait:^{
            //     NSLog(@"From Path: %@", fromPath);
            //     NSLog(@"To Path : %@", toPath);
        
            //     FileSystemItem *itemBeingMoved;
        
            //     CLEvent *event = [[CLEvent alloc] init];
            //     CLMetadata *meta = [[CLMetadata alloc] init];
            //     event.action = CLEventTypeRenameFile;
            //     meta.toPath = toPath;
            //     meta.fromPath = fromPath;
            //     meta.path = fromPath;
        
            //     event.metadata = meta;
        
            //     NSManagedObjectID *objectID =  [self objectIDforEvent:event typeOfPath:CLPathFromPath];
            //     if (objectID) {
            //         itemBeingMoved = (FileSystemItem *)[managedObjectContext objectWithID:objectID];
            //     }
        
            //     NSLog(@"ItemBeingMoved: %@", itemBeingMoved.path);
        
            //     FileSystemItem *newParentItem;

            //     NSManagedObjectID *parentObjectID = [self objectIDforEvent:event typeOfPath:CLPathToPath];
        
            //     if (parentObjectID) {
            //         newParentItem = (FileSystemItem *)[managedObjectContext objectWithID:parentObjectID];
            //         [itemBeingMoved setParent:newParentItem];
            //     }

            // }];
        }

        //+ (void)renameItemFromPath:(NSString *)fromPath toPath:(NSString *)toPath
        void RenameItemFromPath_toPath(string fromPath, string toPath)
        {
            // Merged 7/9/12
            // __block NSManagedObjectContext *managedObjectContext = [[CLCoreDataController defaultController] managedObjectContext];
    
            // [managedObjectContext performBlockAndWait:^{
        
            //     CLEvent *event = [[CLEvent alloc] init];
            //     CLMetadata *meta = [[CLMetadata alloc] init];
            //     event.action = CLEventTypeRenameFile;
            //     meta.toPath = toPath;
            //     meta.fromPath = fromPath;
            //     meta.path = fromPath;
        
            //     event.metadata = meta;
        
            //     NSManagedObjectID *objectID = [self objectIDforEvent:event typeOfPath:CLPathFromPath];
            //     if (objectID) {
            //         FileSystemItem *itemBeingRenamed = (FileSystemItem *)[managedObjectContext objectWithID:objectID];
            //         itemBeingRenamed.name = [toPath lastPathComponent];
            //     }        
            // }];
            //&&&&

            //&&&&
            // Pseudo-code
#if TRASH
            allocate the current managedObjectContext
            performBlockAndWait on managedObjectContext
              allocate event as CLEvent
              allocate meta as CLMetadata
              set event.Action = CLEventTypeRenameFile
              set meta.ToPath = toPath
              set meta.FromPath = fromPath
              set meta.Path = fromPath
              set event.Metadata = meta
              find objectId = ObjectIdForEvent_typeOfPath(event, CLPathFromPath)
              if objectId != null
                allocate itemBeingRenamed as database entity FileSystemItem via key objectId
                set itemBeingRenamed.Name = toPath.LastPathComponent()
              endif objectId != null
              save to database
            endperformBlockAndWait on managedObjectContext
#endif  // TRASH            
            //&&&&

            // __block NSManagedObjectContext *managedObjectContext = [[CLCoreDataController defaultController] managedObjectContext];
    
            // [managedObjectContext performBlockAndWait:^{
        
            //     CLEvent *event = [[CLEvent alloc] init];
            //     CLMetadata *meta = [[CLMetadata alloc] init];
            //     event.action = CLEventTypeRenameFile;
            //     meta.toPath = toPath;
            //     meta.fromPath = fromPath;
            //     meta.path = fromPath;
        
            //     event.metadata = meta;
        
            //     NSManagedObjectID *objectID = [self objectIDforEvent:event typeOfPath:CLPathFromPath];
            //     if (objectID) {
            //         FileSystemItem *itemBeingRenamed = (FileSystemItem *)[managedObjectContext objectWithID:objectID];
            //         itemBeingRenamed.name = [toPath lastPathComponent];
            //     }        
            // }];
        }

        //+ (NSString *)parentPathForCloudPath:(NSString *)path
        string ParentPathForCloudPath(string path)
        {
            // Merged 7/9/12
            // NSString *itemParentPath = [path stringByDeletingLastPathComponent];
            // if ([itemParentPath isEqualToString:@"/"]) {
            //     return itemParentPath;
            // }
            // return [itemParentPath stringByAppendingString:@"/"];
            //&&&&

            // NSString *itemParentPath = [path stringByDeletingLastPathComponent];
            string itemParentPath = path.StringByDeletingLastPathComponent();

            // if ([itemParentPath isEqualToString:@"/"]) {
            if (itemParentPath.Equals("\\", StringComparison.InvariantCulture))
            {
                // return itemParentPath;
                return itemParentPath;
            }

            // return [itemParentPath stringByAppendingString:@"/"];
            return itemParentPath + "\\";
        }

        //+ (void)addInitialRootFolderItemForCloudFolderSetup
        void AddInitialRootFolderItemForCloudFolderSetup()
        {
            // Merged 7/9/12
            // CLMetadata *metadataItem = [[CLMetadata alloc] init];
            // metadataItem.path = @"/";
            // NSLog(@"Date: %@", [NSDate ISO8601DateStringFromDate:[NSDate date]]);
            // metadataItem.createDate = [NSDate ISO8601DateStringFromDate:[NSDate date]];
            // metadataItem.isDirectory = YES;
            // metadataItem.isPending = NO;
    
            // [self addMetedataItem:metadataItem pending:NO];
            //&&&&

            // CLMetadata *metadataItem = [[CLMetadata alloc] init];
            CLMetadata metadataItem = new CLMetadata();

            // metadataItem.path = @"/";
            // metadataItem.createDate = [NSDate ISO8601DateStringFromDate:[NSDate date]];
            // metadataItem.isDirectory = YES;
            // metadataItem.isPending = NO;
            metadataItem.Path = "\\";
            metadataItem.CreateDate = DateTime.Now.ToString("o");  // ISO 8601 format
            metadataItem.ModifiedDate = DateTime.Now.ToString("o");  // ISO 8601 format
            metadataItem.IsDirectory = true;
            metadataItem.IsPending = false;
    
            // [self addMetedataItem:metadataItem pending:NO];
            AddMetadataItem_pending(metadataItem, pending: false);
        }
    }
}