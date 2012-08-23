//
// IndexingAgent.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Data.Objects.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
using FileMonitor;

namespace SQLIndexer
{
    public class IndexingAgent
    {
        #region private fields
        // store the path that represents the root of indexing
        private string indexedPath = null;

        // store dictionaries to convert between the FileChangetype enumeration and its integer value in the database,
        // will be filled in during startup
        private static Dictionary<int, FileChangeType> changeEnums = null;
        private static Dictionary<FileChangeType, int> changeEnumsBackward = null;

        // category in SQL that represents the Enumeration type FileChangeType
        private static int changeCategoryId = 0;
        // locker for reading/writing the change enumerations
        private static object changeEnumsLocker = new object();
        #endregion

        #region public properties
        /// <summary>
        /// Store the last Sync Id, starts null before indexing; lock on the IndexingAgent instance for all reads/writes
        /// </summary>
        public string LastSyncId { get; private set; }
        #endregion

        /// <summary>
        /// Creates the SQL indexing service and outputs it,
        /// must be started afterwards with StartInitialIndexing
        /// </summary>
        /// <param name="newIndexer">Output indexing agent</param>
        /// <returns>Returns the error that occurred during creation, if any</returns>
        public static CLError CreateNewAndInitialize(out IndexingAgent newIndexer)
        {
            // Fill in output with constructor
            IndexingAgent newAgent;
            try
            {
                newIndexer = newAgent = new IndexingAgent();
            }
            catch (Exception ex)
            {
                newIndexer = Helpers.DefaultForType<IndexingAgent>();
                return ex;
            }

            // Fill in change enumerations if they have not been filled in yet
            try
            {
                lock (changeEnumsLocker)
                {
                    if (changeEnums == null)
                    {
                        int changeEnumsCount = System.Enum.GetNames(typeof(FileChangeType)).Length;
                        changeEnums = new Dictionary<int, FileChangeType>(changeEnumsCount);
                        changeEnumsBackward = new Dictionary<FileChangeType, int>(changeEnumsCount);
                        using (IndexDBEntities indexDB = new IndexDBEntities())
                        {
                            foreach (SQLEnum currentChangeEnum in indexDB.SQLEnums
                                .Include(parent => parent.EnumCategory)
                                .Where(currentEnum => currentEnum.EnumCategory.Name == typeof(FileChangeType).Name))
                            {
                                changeCategoryId = currentChangeEnum.EnumCategoryId;
                                int forwardKey = currentChangeEnum.EnumId;
                                FileChangeType forwardValue = (FileChangeType)System.Enum.Parse(typeof(FileChangeType), currentChangeEnum.Name);
                                changeEnums.Add(forwardKey,
                                    forwardValue);
                                changeEnumsBackward.Add(forwardValue,
                                    forwardKey);
                            }
                        }
                        if (changeEnums.Count != changeEnumsCount)
                        {
                            throw new Exception("FileChangeType enumerations are not all found in the database");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        #region public methods
        /// <summary>
        /// Starts the indexing process on an indexing agent which will resolve the last events and changes to the file system since the last time
        /// the file monitor was running to produce the initial in-memory index and changes to process,
        /// spins off a user work thread for the actual processing and returns immediately
        /// </summary>
        /// <param name="indexCompletionCallback">FileMonitor method to call upon completion of the index (should trigger normal processing of file events)</param>
        /// <param name="getPath">FileMonitor method which returns the path to be indexed (so that the indexing and monitor are tied together)</param>
        /// <returns>Returns an error that occurred during startup, if any</returns>
        public CLError StartInitialIndexing(Action<IEnumerable<KeyValuePair<FilePath, FileMetadata>>, IEnumerable<FileChange>> indexCompletionCallback,
            Func<string> getPath)
        {
            try
            {
                this.indexedPath = getPath();
                ThreadPool.QueueUserWorkItem(state => this.BuildIndex((Action<IEnumerable<KeyValuePair<FilePath, FileMetadata>>, IEnumerable<FileChange>>)state),
                    indexCompletionCallback);
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Retrieve the complete file system state at the time of the last sync
        /// </summary>
        /// <param name="syncStates">Outputs the file system state</param>
        /// <returns>Returns an error that occurred retrieving the file system state, if any</returns>
        public CLError GetLastSyncStates(out FilePathDictionary<SyncedObject> syncStates)
        {
            try
            {
                using (IndexDBEntities indexDB = new IndexDBEntities())
                {
                    // Pull the last sync from the database
                    Sync lastSync = indexDB.Syncs
                        .OrderByDescending(currentSync => currentSync.SyncCounter)
                        .FirstOrDefault();

                    // Default the sync states (to null) if there was never a sync
                    if (lastSync == null)
                    {
                        syncStates = Helpers.DefaultForType<FilePathDictionary<SyncedObject>>();
                    }
                    // If there was a sync, continue on to build the sync state
                    else
                    {
                        // Create the dictionary of sync states to output
                        CLError createDictError = FilePathDictionary<SyncedObject>.CreateAndInitialize(lastSync.RootPath,
                            out syncStates);
                        if (createDictError != null)
                        {
                            return createDictError;
                        }

                        // Loop through all sync states for the last sync
                        foreach (SyncState currentSyncState in
                            indexDB.SyncStates
                                .Include(parent => parent.FileSystemObject)
                                .Include(parent => parent.ServerLinkedFileSystemObject)
                                .Where(innerState => innerState.SyncCounter == lastSync.SyncCounter))
                        {
                            // Add the current sync state from the last sync to the output dictionary
                            syncStates.Add(currentSyncState.FileSystemObject.Path,
                                new SyncedObject()
                                {
                                    ServerLinkedPath = currentSyncState.ServerLinkedFileSystemObject == null
                                        ? null
                                        : currentSyncState.ServerLinkedFileSystemObject.Path,
                                    Metadata = new FileMetadata()
                                    {
                                        HashableProperties = new FileMetadataHashableProperties(currentSyncState.FileSystemObject.IsFolder,
                                            currentSyncState.FileSystemObject.LastTime,
                                            currentSyncState.FileSystemObject.CreationTime,
                                            currentSyncState.FileSystemObject.Size),
                                        LinkTargetPath = currentSyncState.FileSystemObject.TargetPath,
                                        Revision = currentSyncState.FileSystemObject.Revision,
                                        StorageKey = currentSyncState.FileSystemObject.StorageKey
                                    }
                                });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                syncStates = null;
                return ex;
            }
            return null;
        }

        public CLError GetMetadataByPathAndRevision(string path, string revision, out FileMetadata metadata)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    throw new NullReferenceException("path cannot be null");
                }
                if (string.IsNullOrEmpty(revision))
                {
                    throw new NullReferenceException("revision cannot be null");
                }

                using (IndexDBEntities indexDB = new IndexDBEntities())
                {
                    // Grab the most recent sync from the database to pull sync states
                    Sync lastSync = indexDB.Syncs
                        .OrderByDescending(currentSync => currentSync.SyncCounter)
                        .FirstOrDefault();

                    if (lastSync == null)
                    {
                        metadata = null;
                    }
                    else
                    {
                        SyncState foundSync = indexDB.SyncStates
                            .Include(parent => parent.FileSystemObject)
                            // the following LINQ to Entities where clause compares the checksums of the sync state's NewPath
                            .Where(parent => parent.SyncCounter == lastSync.SyncCounter
                                && parent.FileSystemObject.PathChecksum == SqlFunctions.Checksum(path)
                                && parent.FileSystemObject.Revision.Equals(revision, StringComparison.InvariantCultureIgnoreCase))
                            .AsEnumerable() // transfers from LINQ to Entities IQueryable into LINQ to Objects IEnumerable (evaluates SQL)
                            .Where(parent => parent.FileSystemObject.Path == path) // run in memory since Path field is not indexable
                            .FirstOrDefault();

                        if (foundSync != null)
                        {
                            metadata = new FileMetadata()
                            {
                                HashableProperties = new FileMetadataHashableProperties(foundSync.FileSystemObject.IsFolder,
                                    foundSync.FileSystemObject.LastTime,
                                    foundSync.FileSystemObject.CreationTime,
                                    foundSync.FileSystemObject.Size),
                                LinkTargetPath = foundSync.FileSystemObject.TargetPath,
                                Revision = revision,
                                StorageKey = foundSync.FileSystemObject.StorageKey
                            };
                        }
                        else
                        {
                            metadata = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                metadata = Helpers.DefaultForType<FileMetadata>();
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Retrieves all unprocessed events that occurred since the last sync
        /// </summary>
        /// <param name="changeEvents">Outputs the unprocessed events</param>
        /// <returns>Returns an error that occurred filling the unprocessed events, if any</returns>
        public CLError GetEventsSinceLastSync(out List<KeyValuePair<FilePath, FileChange>> changeEvents)
        {
            try
            {
                using (IndexDBEntities indexDB = new IndexDBEntities())
                {
                    // Pull the last sync from the database
                    Sync lastSync = indexDB.Syncs
                        .OrderByDescending(currentSync => currentSync.SyncCounter)
                        .FirstOrDefault();
                    // Fill in a nullable id for the last sync
                    Nullable<long> lastSyncCounter = (lastSync == null
                        ? (Nullable<long>)null
                        : lastSync.SyncCounter);

                    // Create the output list
                    changeEvents = new List<KeyValuePair<FilePath,FileChange>>();

                    // Loop through all the events in the database after the last sync (if any)
                    foreach (Event currentChange in
                        indexDB.Events
                            .Include(parent => parent.FileSystemObject)
                            .Where(currentChange => (currentChange.SyncCounter == null && lastSyncCounter == null)
                                || currentChange.SyncCounter == lastSyncCounter)
                            .OrderBy(currentChange => currentChange.SyncCounter))
                    {
                        // For each event since the last sync (if any), add to the output dictionary
                        changeEvents.Add(new KeyValuePair<FilePath,FileChange>(currentChange.FileSystemObject.Path,
                            new FileChange()
                            {
                                NewPath = currentChange.FileSystemObject.Path,
                                OldPath = currentChange.PreviousPath,
                                Type = changeEnums[currentChange.FileChangeTypeEnumId],
                                Metadata = new FileMetadata()
                                {
                                    HashableProperties = new FileMetadataHashableProperties(currentChange.FileSystemObject.IsFolder,
                                        currentChange.FileSystemObject.LastTime,
                                        currentChange.FileSystemObject.CreationTime,
                                        currentChange.FileSystemObject.Size),
                                    Revision = currentChange.FileSystemObject.Revision,
                                    StorageKey = currentChange.FileSystemObject.StorageKey,
                                    LinkTargetPath = currentChange.FileSystemObject.TargetPath
                                },
                                Direction = (currentChange.SyncFrom ? SyncDirection.From : SyncDirection.To)
                            }));
                    }
                }
            }
            catch (Exception ex)
            {
                changeEvents = Helpers.DefaultForType<List<KeyValuePair<FilePath, FileChange>>>();
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Adds an unprocessed change since the last sync as a new event to the database,
        /// EventId property of the input event is set after database update
        /// </summary>
        /// <param name="newEvent">Change to add</param>
        /// <returns>Returns error that occurred when adding the event to database, if any</returns>
        public CLError AddEvent(FileChange newEvent)
        {
            try
            {
                // Ensure input parameter is set
                if (newEvent == null)
                {
                    throw new NullReferenceException("newEvent cannot be null");
                }
                if (newEvent.Metadata == null)
                {
                    throw new NullReferenceException("The Metadata property of newEvent cannot be null");
                }

                // If change is marked for adding to SQL,
                // then process database addition
                if (!newEvent.DoNotAddToSQLIndex)
                {
                    using (IndexDBEntities indexDB = new IndexDBEntities())
                    {
                        // Grab the last sync from the database
                        Sync lastSync = indexDB.Syncs
                            .OrderByDescending(currentSync => currentSync.SyncCounter)
                            .FirstOrDefault();
                        // Fill in a nullable id of the last sync
                        Nullable<long> lastSyncCounter = (lastSync == null
                            ? (Nullable<long>)null
                            : lastSync.SyncCounter);

                        // Define the new event to add for the unprocessed change
                        Event toAdd = new Event()
                        {
                            SyncCounter = lastSyncCounter,
                            FileChangeTypeCategoryId = changeCategoryId,
                            FileChangeTypeEnumId = changeEnumsBackward[newEvent.Type],
                            FileSystemObject = new FileSystemObject()
                            {
                                CreationTime = newEvent.Metadata.HashableProperties.CreationTime.Ticks == FileConstants.InvalidUtcTimeTicks
                                    ? (Nullable<DateTime>)null
                                    : newEvent.Metadata.HashableProperties.CreationTime,
                                IsFolder = newEvent.Metadata.HashableProperties.IsFolder,
                                LastTime = newEvent.Metadata.HashableProperties.LastTime.Ticks == FileConstants.InvalidUtcTimeTicks
                                    ? (Nullable<DateTime>)null
                                    : newEvent.Metadata.HashableProperties.LastTime,
                                Path = newEvent.NewPath.ToString(),
                                Size = newEvent.Metadata.HashableProperties.Size,
                                Revision = newEvent.Metadata.Revision,
                                StorageKey = newEvent.Metadata.StorageKey,
                                TargetPath = (newEvent.Metadata.LinkTargetPath == null ? null : newEvent.Metadata.LinkTargetPath.ToString())
                            },
                            PreviousPath = (newEvent.OldPath == null
                                ? null
                                : newEvent.OldPath.ToString())
                        };

                        // Add the new event to the database
                        indexDB.Events.AddObject(toAdd);
                        indexDB.SaveChanges();

                        // Store the new event id to the change and return it
                        newEvent.EventId = toAdd.EventId;
                    }
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Removes a single event by its id
        /// </summary>
        /// <param name="eventId">Id of event to remove</param>
        /// <returns>Returns an error in removing the event, if any</returns>
        public CLError RemoveEventById(long eventId)
        {
            try
            {
                using (IndexDBEntities indexDB = new IndexDBEntities())
                {
                    // Find the existing event for the given id
                    Event toDelete = indexDB.Events
                        .Include(parent => parent.FileSystemObject)
                        .FirstOrDefault(currentEvent => currentEvent.EventId == eventId);
                    // Throw exception if an existing event does not exist
                    if (toDelete == null)
                    {
                        throw new Exception("Event not found to delete");
                    }
                    // Remove the found event from the database
                    indexDB.FileSystemObjects.DeleteObject(toDelete.FileSystemObject);
                    indexDB.Events.DeleteObject(toDelete);
                    indexDB.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Removes a collection of events by their ids
        /// </summary>
        /// <param name="eventIds">Ids of events to remove</param>
        /// <returns>Returns an error in removing events, if any</returns>
        public CLError RemoveEventsByIds(IEnumerable<long> eventIds)
        {
            try
            {
                // copy event id collection to array, defaulting to an empty array
                long[] eventIdsArray = (eventIds == null
                    ? new long[0]
                    : eventIds.ToArray());
                using (IndexDBEntities indexDB = new IndexDBEntities())
                {
                    // Create list to copy event ids from database objects,
                    // used to ensure all event ids to be deleted were found
                    List<long> orderedDBIds = new List<long>();
                    // Grab all events with ids in the specified range
                    Event[] deleteEvents = indexDB.Events
                        .Include(parent => parent.FileSystemObject)
                        .Where(currentEvent => eventIdsArray.Contains(currentEvent.EventId))
                        .OrderBy(currentEvent => currentEvent.EventId)
                        .ToArray();
                    // Delete each event that was returned
                    Array.ForEach(deleteEvents, toDelete =>
                        {
                            orderedDBIds.Add(toDelete.EventId);
                            indexDB.FileSystemObjects.DeleteObject(toDelete.FileSystemObject);
                            indexDB.Events.DeleteObject(toDelete);
                        });
                    // Check all event ids intended for delete and make sure they were actually deleted,
                    // otherwise throw exception
                    CLError notFoundErrors = null;
                    foreach (long deletedEventId in eventIdsArray)
                    {
                        if (orderedDBIds.BinarySearch(deletedEventId) < 0)
                        {
                            notFoundErrors += new Exception("Event with id " + deletedEventId + " not found to delete");
                        }
                    }
                    if (notFoundErrors != null)
                    {
                        return notFoundErrors;
                    }
                    // Save to database
                    indexDB.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Writes a new set of sync states to the database after a sync completes,
        /// requires newRootPath to be set on the first sync or on any sync with a new root path
        /// </summary>
        /// <param name="syncId">New sync Id from server</param>
        /// <param name="syncedEventIds">Enumerable of event ids processed in sync</param>
        /// <param name="syncCounter">Output sync counter local identity</param>
        /// <param name="newRootPath">Optional new root path for location of sync root, must be set on first sync</param>
        /// <returns>Returns an error that occurred during recording the sync, if any</returns>
        public CLError RecordCompletedSync(string syncId, IEnumerable<long> syncedEventIds, out long syncCounter, string newRootPath = null)
        {
            // Default the output sync counter
            syncCounter = Helpers.DefaultForType<long>();
            try
            {
                // Copy event ids completed in sync to array, defaulting to an empty array
                long[] syncedEventIdsEnumerated = (syncedEventIds == null
                    ? new long[0]
                    : syncedEventIds.OrderBy(currentEventId => currentEventId).ToArray());

                // Run entire sync completion database operation set within a transaction to ensure
                // automatic rollback on failure
                using (TransactionScope completionSync = new TransactionScope())
                {
                    using (IndexDBEntities indexDB = new IndexDBEntities())
                    {
                        // Retrieve last sync if it exists
                        Sync lastSync = indexDB.Syncs
                            .OrderByDescending(currentSync => currentSync.SyncCounter)
                            .FirstOrDefault();
                        // Store last sync counter value or null for no last sync
                        Nullable<long> lastSyncCounter = (lastSync == null
                            ? (Nullable<long>)null
                            : lastSync.SyncCounter);
                        // Default root path from last sync if it was not passed in
                        newRootPath = string.IsNullOrEmpty(newRootPath)
                            ? (lastSync == null ? null : lastSync.RootPath)
                            : newRootPath;

                        if (string.IsNullOrEmpty(newRootPath))
                        {
                            throw new Exception("Path cannot be found for sync root");
                        }

                        FilePath previousRoot = (lastSync == null ? null : lastSync.RootPath);
                        FilePath newRoot = newRootPath;

                        if (string.IsNullOrWhiteSpace(syncId))
                        {
                            if (lastSync != null)
                            {
                                syncId = lastSync.SyncId;
                            }
                            else
                            {
                                throw new Exception("Could not find a sync id");
                            }
                        }

                        bool syncStatesNeedRemap = previousRoot != null
                            && !FilePathComparer.Instance.Equals(previousRoot, newRoot);

                        // Create the new sync database object
                        Sync newSync = new Sync()
                        {
                            SyncId = syncId,
                            RootPath = newRootPath
                        };

                        // Add the new sync to the database and store the new counter
                        indexDB.Syncs.AddObject(newSync);
                        indexDB.SaveChanges();
                        syncCounter = newSync.SyncCounter;

                        // Create the dictionary for new sync states, returning an error if it occurred
                        FilePathDictionary<FileMetadata> newSyncStates;
                        CLError newSyncStatesError = FilePathDictionary<FileMetadata>.CreateAndInitialize(newRootPath,
                            out newSyncStates);
                        if (newSyncStatesError != null)
                        {
                            return newSyncStatesError;
                        }

                        // Create a dictionary to store remapped server paths in case they exist
                        Dictionary<string, string> serverRemappedPaths = new Dictionary<string, string>();

                        // If there was a previous sync, pull the previous sync states to modify
                        if (lastSyncCounter != null)
                        {
                            // Loop through previous sync states
                            foreach (SyncState currentState in indexDB.SyncStates
                                .Include(parent => parent.FileSystemObject)
                                .Include(parent => parent.ServerLinkedFileSystemObject)
                                .Where(currentSync => currentSync.SyncCounter == (long)lastSyncCounter))
                            {
                                string localPath = currentState.FileSystemObject.Path;
                                string serverPath = (currentState.ServerLinkedFileSystemObject == null ? null : currentState.ServerLinkedFileSystemObject.Path);

                                if (syncStatesNeedRemap)
                                {
                                    FilePath originalLocalPath = localPath;
                                    FilePath originalServerPath = serverPath;

                                    FilePath overlappingLocal = originalLocalPath.FindOverlappingPath(previousRoot);
                                    if (overlappingLocal != null)
                                    {
                                        FilePath renamedOverlapChild = originalLocalPath;
                                        FilePath renamedOverlap = renamedOverlapChild.Parent;

                                        while (renamedOverlap != null)
                                        {
                                            if (FilePathComparer.Instance.Equals(renamedOverlap, previousRoot))
                                            {
                                                renamedOverlapChild.Parent = newRoot;
                                                localPath = originalLocalPath.ToString();
                                                break;
                                            }

                                            renamedOverlapChild = renamedOverlap;
                                            renamedOverlap = renamedOverlap.Parent;
                                        }
                                    }

                                    if (originalServerPath != null)
                                    {
                                        FilePath overlappingServer = originalServerPath.FindOverlappingPath(previousRoot);
                                        if (overlappingServer != null)
                                        {
                                            FilePath renamedOverlapChild = overlappingServer;
                                            FilePath renamedOverlap = renamedOverlapChild.Parent;

                                            while (renamedOverlap != null)
                                            {
                                                if (FilePathComparer.Instance.Equals(renamedOverlap, previousRoot))
                                                {
                                                    renamedOverlapChild.Parent = newRoot;
                                                    serverPath = overlappingServer.ToString();
                                                    break;
                                                }

                                                renamedOverlapChild = renamedOverlap;
                                                renamedOverlap = renamedOverlap.Parent;
                                            }
                                        }
                                    }
                                }

                                // Check if previous syncstate had a server-remapped path to store
                                if (currentState.ServerLinkedFileSystemObject != null)
                                {
                                    serverRemappedPaths.Add(localPath,
                                        serverPath);
                                }

                                // Add the previous sync state to the dictionary as the baseline before changes
                                newSyncStates.Add(localPath,
                                    new FileMetadata()
                                    {
                                        HashableProperties = new FileMetadataHashableProperties(currentState.FileSystemObject.IsFolder,
                                            currentState.FileSystemObject.LastTime,
                                            currentState.FileSystemObject.CreationTime,
                                            currentState.FileSystemObject.Size),
                                        LinkTargetPath = currentState.FileSystemObject.TargetPath,
                                        Revision = currentState.FileSystemObject.Revision,
                                        StorageKey = currentState.FileSystemObject.StorageKey
                                    });
                            }
                        }

                        // Grab all events from the database since the previous sync, ordering by id to ensure correct processing logic
                        Event[] existingEvents = indexDB.Events
                            .Include(parent => parent.FileSystemObject)
                            .Where(currentEvent => (currentEvent.SyncCounter == null && lastSyncCounter == null)
                                || currentEvent.SyncCounter == lastSyncCounter)
                            .OrderBy(currentEvent => currentEvent.EventId)
                            .ToArray();

                        // Loop through existing events to process into the new sync states
                        foreach (Event previousEvent in existingEvents)
                        {
                            string newPath = previousEvent.FileSystemObject.Path;
                            string oldPath = previousEvent.PreviousPath;

                            if (syncStatesNeedRemap)
                            {
                                FilePath originalNewPath = newPath;
                                FilePath originalOldPath = oldPath;

                                FilePath overlappingLocal = originalNewPath.FindOverlappingPath(previousRoot);
                                if (overlappingLocal != null)
                                {
                                    FilePath renamedOverlapChild = originalNewPath;
                                    FilePath renamedOverlap = renamedOverlapChild.Parent;

                                    while (renamedOverlap != null)
                                    {
                                        if (FilePathComparer.Instance.Equals(renamedOverlap, previousRoot))
                                        {
                                            renamedOverlapChild.Parent = newRoot;
                                            newPath = originalNewPath.ToString();
                                            break;
                                        }

                                        renamedOverlapChild = renamedOverlap;
                                        renamedOverlap = renamedOverlap.Parent;
                                    }
                                }

                                if (originalOldPath != null)
                                {
                                    FilePath overlappingServer = originalOldPath.FindOverlappingPath(previousRoot);
                                    if (overlappingServer != null)
                                    {
                                        FilePath renamedOverlapChild = overlappingServer;
                                        FilePath renamedOverlap = renamedOverlapChild.Parent;

                                        while (renamedOverlap != null)
                                        {
                                            if (FilePathComparer.Instance.Equals(renamedOverlap, previousRoot))
                                            {
                                                renamedOverlapChild.Parent = newRoot;
                                                oldPath = overlappingServer.ToString();
                                                break;
                                            }

                                            renamedOverlapChild = renamedOverlap;
                                            renamedOverlap = renamedOverlap.Parent;
                                        }
                                    }
                                }
                            }

                            // If the current database event is in the list of events that are completed,
                            // the syncstates have to be modified appropriately to include the change
                            if (Array.BinarySearch(syncedEventIdsEnumerated, previousEvent.EventId) >= 0)
                            {
                                switch (changeEnums[previousEvent.FileChangeTypeEnumId])
                                {
                                    case FileChangeType.Created:
                                        newSyncStates.Add(newPath,
                                            new FileMetadata()
                                            {
                                                HashableProperties = new FileMetadataHashableProperties(previousEvent.FileSystemObject.IsFolder,
                                                    previousEvent.FileSystemObject.LastTime,
                                                    previousEvent.FileSystemObject.CreationTime,
                                                    previousEvent.FileSystemObject.Size),
                                                LinkTargetPath = previousEvent.FileSystemObject.TargetPath,
                                                Revision = previousEvent.FileSystemObject.Revision,
                                                StorageKey = previousEvent.FileSystemObject.StorageKey
                                            });
                                        break;
                                    case FileChangeType.Deleted:
                                        newSyncStates.Remove(newPath);
                                        break;
                                    case FileChangeType.Modified:
                                        if (newSyncStates.ContainsKey(newPath))
                                        {
                                            FileMetadata modifiedMetadata = newSyncStates[newPath];
                                            modifiedMetadata.HashableProperties = new FileMetadataHashableProperties(previousEvent.FileSystemObject.IsFolder,
                                                    previousEvent.FileSystemObject.LastTime,
                                                    previousEvent.FileSystemObject.CreationTime,
                                                    previousEvent.FileSystemObject.Size);
                                            modifiedMetadata.LinkTargetPath = previousEvent.FileSystemObject.TargetPath;
                                            modifiedMetadata.Revision = previousEvent.FileSystemObject.Revision;
                                            modifiedMetadata.StorageKey = previousEvent.FileSystemObject.StorageKey;
                                        }
                                        else
                                        {
                                            newSyncStates.Add(newPath,
                                                new FileMetadata()
                                                {
                                                    HashableProperties = new FileMetadataHashableProperties(previousEvent.FileSystemObject.IsFolder,
                                                        previousEvent.FileSystemObject.LastTime,
                                                        previousEvent.FileSystemObject.CreationTime,
                                                        previousEvent.FileSystemObject.Size),
                                                    LinkTargetPath = previousEvent.FileSystemObject.TargetPath,
                                                    Revision = previousEvent.FileSystemObject.Revision,
                                                    StorageKey = previousEvent.FileSystemObject.StorageKey
                                                });
                                        }
                                        break;
                                    case FileChangeType.Renamed:
                                        if (newSyncStates.ContainsKey(newPath))
                                        {
                                            if (newSyncStates.ContainsKey(oldPath))
                                            {
                                                newSyncStates.Remove(oldPath);
                                            }
                                            FileMetadata renameMetadata = newSyncStates[newPath];
                                            renameMetadata.HashableProperties = new FileMetadataHashableProperties(previousEvent.FileSystemObject.IsFolder,
                                                    previousEvent.FileSystemObject.LastTime,
                                                    previousEvent.FileSystemObject.CreationTime,
                                                    previousEvent.FileSystemObject.Size);
                                            renameMetadata.LinkTargetPath = previousEvent.FileSystemObject.TargetPath;
                                            renameMetadata.Revision = previousEvent.FileSystemObject.Revision;
                                            renameMetadata.StorageKey = previousEvent.FileSystemObject.StorageKey;
                                        }
                                        else if (newSyncStates.ContainsKey(oldPath))
                                        {
                                            newSyncStates.Rename(oldPath, newPath);

                                            FileMetadata renameMetadata = newSyncStates[newPath];
                                            renameMetadata.HashableProperties = new FileMetadataHashableProperties(previousEvent.FileSystemObject.IsFolder,
                                                    previousEvent.FileSystemObject.LastTime,
                                                    previousEvent.FileSystemObject.CreationTime,
                                                    previousEvent.FileSystemObject.Size);
                                            renameMetadata.LinkTargetPath = previousEvent.FileSystemObject.TargetPath;
                                            renameMetadata.Revision = previousEvent.FileSystemObject.Revision;
                                            renameMetadata.StorageKey = previousEvent.FileSystemObject.StorageKey;
                                        }
                                        else
                                        {
                                            newSyncStates.Add(newPath,
                                                new FileMetadata()
                                                {
                                                    HashableProperties = new FileMetadataHashableProperties(previousEvent.FileSystemObject.IsFolder,
                                                        previousEvent.FileSystemObject.LastTime,
                                                        previousEvent.FileSystemObject.CreationTime,
                                                        previousEvent.FileSystemObject.Size),
                                                    LinkTargetPath = previousEvent.FileSystemObject.TargetPath,
                                                    Revision = previousEvent.FileSystemObject.Revision,
                                                    StorageKey = previousEvent.FileSystemObject.StorageKey
                                                });
                                        }
                                        break;
                                }
                            }
                            // Else if the previous database event is not in the list of completed events,
                            // The event will get moved to after the current sync so it will be processed later
                            else
                            {
                                previousEvent.SyncCounter = syncCounter;
                                if (syncStatesNeedRemap)
                                {
                                    previousEvent.FileSystemObject.Path = newPath;
                                    previousEvent.PreviousPath = oldPath;
                                }
                            }
                        }

                        // Loop through modified set of sync states (including new changes) and add the matching database objects
                        foreach (KeyValuePair<FilePath, FileMetadata> newSyncState in newSyncStates)
                        {
                            // Add the file/folder object for the current sync state
                            FileSystemObject newSyncedObject = new FileSystemObject()
                            {
                                CreationTime = (newSyncState.Value.HashableProperties.CreationTime.Ticks == FileConstants.InvalidUtcTimeTicks
                                    ? (Nullable<DateTime>)null
                                    : newSyncState.Value.HashableProperties.CreationTime),
                                IsFolder = newSyncState.Value.HashableProperties.IsFolder,
                                LastTime = (newSyncState.Value.HashableProperties.LastTime.Ticks == FileConstants.InvalidUtcTimeTicks
                                    ? (Nullable<DateTime>)null
                                    : newSyncState.Value.HashableProperties.LastTime),
                                Path = newSyncState.Key.ToString(),
                                Size = newSyncState.Value.HashableProperties.Size,
                                TargetPath = (newSyncState.Value.LinkTargetPath == null ? null : newSyncState.Value.LinkTargetPath.ToString()),
                                Revision = newSyncState.Value.Revision,
                                StorageKey = newSyncState.Value.StorageKey
                            };
                            indexDB.FileSystemObjects.AddObject(newSyncedObject);

                            // If the file/folder path is remapped on the server, add the file/folder object for the server-mapped state
                            Nullable<long> serverRemappedObjectId = null;
                            if (serverRemappedPaths.ContainsKey(newSyncedObject.Path))
                            {
                                FileSystemObject serverSyncedObject = new FileSystemObject()
                                {
                                    CreationTime = (newSyncState.Value.HashableProperties.CreationTime.Ticks == FileConstants.InvalidUtcTimeTicks
                                        ? (Nullable<DateTime>)null
                                        : newSyncState.Value.HashableProperties.CreationTime),
                                    IsFolder = newSyncState.Value.HashableProperties.IsFolder,
                                    LastTime = (newSyncState.Value.HashableProperties.LastTime.Ticks == FileConstants.InvalidUtcTimeTicks
                                        ? (Nullable<DateTime>)null
                                        : newSyncState.Value.HashableProperties.LastTime),
                                    Path = serverRemappedPaths[newSyncedObject.Path],
                                    Size = newSyncState.Value.HashableProperties.Size,
                                    TargetPath = (newSyncState.Value.LinkTargetPath == null ? null : newSyncState.Value.LinkTargetPath.ToString()),
                                    Revision = newSyncState.Value.Revision,
                                    StorageKey = newSyncState.Value.StorageKey
                                };
                                indexDB.FileSystemObjects.AddObject(serverSyncedObject);
                                indexDB.SaveChanges();
                                serverRemappedObjectId = serverSyncedObject.FileSystemObjectId;
                            }
                            indexDB.SaveChanges();

                            // Add the sync state database object tied to its file/folder objects and the current sync
                            indexDB.SyncStates.AddObject(new SyncState()
                            {
                                FileSystemObjectId = newSyncedObject.FileSystemObjectId,
                                ServerLinkedFileSystemObjectId = serverRemappedObjectId,
                                SyncCounter = syncCounter
                            });
                        }

                        // Finish writing any unsaved changes to the database
                        indexDB.SaveChanges();
                        // No errors occurred, so the transaction can be completed
                        completionSync.Complete();
                    }

                    // update the exposed last sync id upon sync completion
                    lock (this)
                    {
                        this.LastSyncId = syncId;
                    }
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// ¡¡ Call this carefully, completely wipes index database (use when user deletes local repository or relinks) !!
        /// </summary>
        /// <returns></returns>
        public CLError WipeIndex(string newRootPath)
        {
            try
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    using (IndexDBEntities indexDB = new IndexDBEntities())
                    {
                        Sync emptySync = new Sync()
                        {
                            SyncId = IdForEmptySync,
                            RootPath = newRootPath
                        };
                        indexDB.Syncs.AddObject(emptySync);
                        indexDB.SaveChanges();
                        scope.Complete();
                    }
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }
        private const string IdForEmptySync = "0";
        
        /// <summary>
        /// Writes a new set of sync states to the database after a sync completes,
        /// requires newRootPath to be set on the first sync or on any sync with a new root path
        /// </summary>
        /// <param name="syncId">New sync Id from server</param>
        /// <param name="syncedEventIds">Enumerable of event ids processed in sync</param>
        /// <param name="syncCounter">Output sync counter local identity</param>
        /// <param name="newRootPath">Optional new root path for location of sync root, must be set on first sync</param>
        /// <returns>Returns an error that occurred during recording the sync, if any</returns>
        public CLError RecordCompletedSync(string syncId, IEnumerable<long> syncedEventIds, out long syncCounter, FilePath newRootPath = null)
        {
            return RecordCompletedSync(syncId, syncedEventIds, out syncCounter, newRootPath == null ? null : newRootPath.ToString());
        }

        /// <summary>
        /// Method to merge event into database,
        /// used when events are modified or replaced with new events
        /// </summary>
        /// <param name="mergedEvent">Event with latest file or folder metadata, pass null to only delete the old event</param>
        /// <param name="eventToRemove">Previous event to set if an old event is being replaced in the process</param>
        /// <returns>Returns an error from merging the events, if any</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public CLError MergeEventIntoDatabase(FileChange mergedEvent, FileChange eventToRemove)
        {
            try
            {
                // Return out if boolean set indicating not to add to SQL
                if (mergedEvent.DoNotAddToSQLIndex)
                {
                    return null;
                }

                // Ensure input variables have proper references set
                if (mergedEvent == null)
                {
                    // null merge events are only valid if there is an oldEvent to remove
                    if (eventToRemove == null)
                    {
                        throw new NullReferenceException("mergedEvent cannot be null");
                    }
                }
                else if (mergedEvent.Metadata == null)
                {
                    throw new NullReferenceException("mergedEvent cannot have null Metadata");
                }

                // Define field for the event id that needs updating in the database,
                // defaulting to none
                Nullable<long> eventIdToUpdate = null;

                using (IndexDBEntities indexDB = new IndexDBEntities())
                {
                    // If the mergedEvent already has an id (exists in database),
                    // then the database event will be updated at the mergedEvent id;
                    // also, if the oldEvent exists in the database, it needs to be removed
                    if (mergedEvent == null
                        || mergedEvent.EventId > 0)
                    {
                        if (eventToRemove != null
                            && eventToRemove.EventId > 0
                            // added the following condition in case both events to merge together share a single database event
                            // which should not be removed
                            && (mergedEvent == null || mergedEvent.EventId != eventToRemove.EventId))
                        {
                            // Find the existing event for the given id
                            Event toDelete = indexDB.Events.FirstOrDefault(currentEvent => currentEvent.EventId == eventToRemove.EventId);
                            // Throw exception if an existing event does not exist
                            if (toDelete == null)
                            {
                                throw new Exception("Event not found to delete");
                            }
                            // Remove the found event from the database
                            indexDB.DeleteObject(toDelete);
                        }

                        // If the mergedEvent it null and the oldEvent is set with a valid eventId,
                        // then save only the deletion of the oldEvent and return
                        if (mergedEvent == null)
                        {
                            indexDB.SaveChanges();
                            return null;
                        }

                        eventIdToUpdate = mergedEvent.EventId;
                    }
                    // Else if the mergedEvent does not have an id in the database
                    // and the oldEvent exists and has an id in the database,
                    // then the database event will be updated at the oldEvent id
                    // and the event id should be moved to the mergedEvent
                    else if (eventToRemove != null
                        && eventToRemove.EventId > 0)
                    {
                        mergedEvent.EventId = eventToRemove.EventId;

                        eventIdToUpdate = eventToRemove.EventId;
                    }

                    // If an id for the database event already exists,
                    // then update the object in the database with the latest properties from mergedEvent
                    if (eventIdToUpdate != null)
                    {
                        // Find the existing event for the given id
                        Event toModify = indexDB.Events
                            .Include(parent => parent.FileSystemObject)
                            .FirstOrDefault(currentEvent => currentEvent.EventId == mergedEvent.EventId);
                        // Throw exception if an existing event does not exist
                        if (toModify == null)
                        {
                            throw new Exception("Event not found to delete");
                        }
                        // Throw exception if existing event does not have a FileSystemObject to modify
                        if (toModify.FileSystemObject == null)
                        {
                            throw new Exception("Event does not have required FileSystemObject");
                        }

                        // Update database object with latest event properties
                        toModify.FileChangeTypeEnumId = changeEnumsBackward[mergedEvent.Type];
                        toModify.PreviousPath = (mergedEvent.OldPath == null
                            ? null
                            : mergedEvent.OldPath.ToString());
                        toModify.FileSystemObject.CreationTime = (mergedEvent.Metadata.HashableProperties.CreationTime.Ticks == FileConstants.InvalidUtcTimeTicks
                            ? (Nullable<DateTime>)null
                            : mergedEvent.Metadata.HashableProperties.CreationTime);
                        toModify.FileSystemObject.IsFolder = mergedEvent.Metadata.HashableProperties.IsFolder;
                        toModify.FileSystemObject.LastTime = (mergedEvent.Metadata.HashableProperties.LastTime.Ticks == FileConstants.InvalidUtcTimeTicks
                            ? (Nullable<DateTime>)null
                            : mergedEvent.Metadata.HashableProperties.LastTime);
                        toModify.FileSystemObject.Path = mergedEvent.NewPath.ToString();
                        toModify.FileSystemObject.Size = mergedEvent.Metadata.HashableProperties.Size;
                        toModify.FileSystemObject.TargetPath = (mergedEvent.Metadata.LinkTargetPath == null ? null : mergedEvent.Metadata.LinkTargetPath.ToString());
                        toModify.FileSystemObject.Revision = mergedEvent.Metadata.Revision;
                        toModify.FileSystemObject.StorageKey = mergedEvent.Metadata.StorageKey;

                        // Save event update (and possibly old event removal) to database
                        indexDB.SaveChanges();
                    }
                }

                // If an id for the database event does not already exist,
                // then process the event as a new one;
                // this is done outside the database entities using statement since
                // AddEvent has it's own entities context
                if (eventIdToUpdate == null)
                {
                    return AddEvent(mergedEvent);
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Includes an event in the last set of sync states,
        /// or in other words processes it as complete
        /// (event will no longer be included in GetEventsSinceLastSync)
        /// </summary>
        /// <param name="eventId">Primary key value of the event to process</param>
        /// <returns>Returns an error that occurred marking the event complete, if any</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public CLError MarkEventAsCompletedOnPreviousSync(long eventId)
        {
            try
            {
                // Runs the event completion operations within a transaction so
                // it can be rolled back on failure
                using (TransactionScope transaction = new TransactionScope())
                {
                    using (IndexDBEntities indexDB = new IndexDBEntities())
                    {
                        // grab the event from the database by provided id
                        Event currentEvent = indexDB.Events
                            .Include(parent => parent.FileSystemObject)
                            .FirstOrDefault(dbEvent => dbEvent.EventId == eventId);
                        // ensure an event was found
                        if (currentEvent == null)
                        {
                            throw new KeyNotFoundException("Previous event not found for given id");
                        }

                        // Retrieve last syncs if they exists
                        Sync[] lastSyncs = indexDB.Syncs
                            .OrderByDescending(currentSync => currentSync.SyncCounter)
                            .Take(2)
                            .ToArray();
                        // ensure a previous sync was found
                        //&&&&if (lastSyncs.Count() == 0)
                        if (lastSyncs.Length == 0)
                        {
                            throw new Exception("Previous sync not found for completed event");
                        }

                        if (lastSyncs[0].SyncCounter != currentEvent.SyncCounter)
                        {
                            throw new Exception("Previous event is not pending");
                        }

                        // declare fields to store server paths in case they are different than the local paths,
                        // defaulting to null
                        string serverRemappedNewPath = null;
                        string serverRemappedOldPath = null;

                        // pull the sync states for the new path of the current event
                        Sync firstLastSync = lastSyncs[0];
                        SyncState[] newPathStates = indexDB.SyncStates
                            .Include(parent => parent.FileSystemObject)
                            .Include(parent => parent.ServerLinkedFileSystemObject)

                            // the following LINQ to Entities where clause compares the checksums of the event's NewPath
                            // (may have duplicate checksums even when paths differ)
                            .Where(currentSyncState => currentSyncState.SyncCounter == firstLastSync.SyncCounter
                                && (((currentSyncState.FileSystemObject.PathChecksum == null && SqlFunctions.Checksum(currentEvent.FileSystemObject.Path) == null)
                                    || currentSyncState.FileSystemObject.PathChecksum == SqlFunctions.Checksum(currentEvent.FileSystemObject.Path))))

                            .AsEnumerable() // transfers from LINQ to Entities IQueryable into LINQ to Objects IEnumerable (evaluates SQL)
                            .Where(currentSyncState => currentSyncState.FileSystemObject.Path == currentEvent.FileSystemObject.Path) // run in memory since Path field is not indexable
                            .ToArray();

                        // declare field for the index of the latest sync state found at the new path
                        Nullable<int> latestNewPathSyncStateArrayIndex = null;
                        // declare field for the highest sync state id found at the new path
                        long latestNewPathSyncStateId = 0;
                        // loop through the sync states found at the new path
                        for (int newPathStateIndex = 0; newPathStateIndex < newPathStates.Length; newPathStateIndex++)
                        {
                            // if the current sync state found at the new path has the highest sync state id checked so far,
                            // record its ids and replace the server remapped new path with the current one
                            if (newPathStates[newPathStateIndex].SyncStateId > latestNewPathSyncStateId)
                            {
                                serverRemappedNewPath = (newPathStates[newPathStateIndex].ServerLinkedFileSystemObject == null
                                    ? null
                                    : newPathStates[newPathStateIndex].ServerLinkedFileSystemObject.Path);
                                latestNewPathSyncStateId = newPathStates[newPathStateIndex].SyncStateId;
                                latestNewPathSyncStateArrayIndex = newPathStateIndex;
                            }
                            // remove the current new path sync state (by first removing its associated FileSystemObjects)
                            indexDB.FileSystemObjects.DeleteObject(newPathStates[newPathStateIndex].FileSystemObject);
                            if (newPathStates[newPathStateIndex].ServerLinkedFileSystemObject != null)
                            {
                                indexDB.FileSystemObjects.DeleteObject(newPathStates[newPathStateIndex].ServerLinkedFileSystemObject);
                            }
                            indexDB.SyncStates.DeleteObject(newPathStates[newPathStateIndex]);
                        }
                        // latestNewPathSyncStateId is now the highest of the new path sync state ids,
                        // and latestNewPathSyncStateArrayIndex represents the index of the same sync state in newPathStates;
                        // also serverRemappedNewPath is set as the remapped path of the latest change

                        // declare the field for the sync states for the old path
                        SyncState[] oldPathStates;
                        // if the change type of the current event is a rename and it has a previous path,
                        // then fill in the old path sync states from the database
                        if (changeEnums[currentEvent.FileChangeTypeEnumId] == FileChangeType.Renamed
                            && currentEvent.PreviousPath != null)
                        {
                            // fill in the old path sync states
                            oldPathStates = indexDB.SyncStates
                                .Include(parent => parent.FileSystemObject)
                                .Include(parent => parent.ServerLinkedFileSystemObject)

                                // the following LINQ to Entities where clause compares the checksums of the event's NewPath
                                // (may have duplicate checksums even when paths differ
                                .Where(currentSyncState => currentSyncState.SyncCounter == lastSyncs[0].SyncCounter
                                    && (((currentSyncState.FileSystemObject.PathChecksum == null && SqlFunctions.Checksum(currentEvent.PreviousPath) == null)
                                        || currentSyncState.FileSystemObject.PathChecksum == SqlFunctions.Checksum(currentEvent.PreviousPath))))

                                .AsEnumerable() // transfers from LINQ to Entities IQueryable into LINQ to Objects IEnumerable (evaluates SQL)
                                .Where(currentSyncState => currentSyncState.FileSystemObject.Path == currentEvent.PreviousPath) // run in memory since Path field is not indexable
                                .ToArray();
                        }
                        // else if the change type of the current event is not a rename or it does not have a previous path,
                        // then old path sync states should be an empty array
                        else
                        {
                            oldPathStates = new SyncState[0];
                        }

                        // declare field for the index of the latest sync state found at the old path
                        Nullable<int> latestOldPathSyncStateArrayIndex = null;
                        // declare field for the highest sync state id found at the old path
                        long latestOldPathSyncStateId = 0;
                        // loop through the sync states found at the old path
                        for (int oldPathStateIndex = 0; oldPathStateIndex < oldPathStates.Length; oldPathStateIndex++)
                        {
                            // if the current sync state found at the old path has the highest sync state id checked so far,
                            // record its ids and replace the server remapped old path with the current one
                            if (oldPathStates[oldPathStateIndex].SyncStateId > latestOldPathSyncStateId)
                            {
                                serverRemappedOldPath = (oldPathStates[oldPathStateIndex].ServerLinkedFileSystemObject == null
                                    ? null
                                    : oldPathStates[oldPathStateIndex].ServerLinkedFileSystemObject.Path);
                                latestOldPathSyncStateId = oldPathStates[oldPathStateIndex].SyncStateId;
                                latestOldPathSyncStateArrayIndex = oldPathStateIndex;
                            }
                            // remove the current old path sync state (by first removing its associated FileSystemObjects)
                            indexDB.FileSystemObjects.DeleteObject(oldPathStates[oldPathStateIndex].FileSystemObject);
                            if (oldPathStates[oldPathStateIndex].ServerLinkedFileSystemObject != null)
                            {
                                indexDB.FileSystemObjects.DeleteObject(oldPathStates[oldPathStateIndex].ServerLinkedFileSystemObject);
                            }
                            indexDB.SyncStates.DeleteObject(oldPathStates[oldPathStateIndex]);
                        }
                        // latestOldPathSyncStateId is now the highest of the old path sync state ids,
                        // and latestOldPathSyncStateArrayIndex represents the index of the same sync state in oldPathStates;
                        // also serverRemappedOldPath is set as the remapped path of the latest change

                        // if the change type of the current event is not a deletion,
                        // then a new sync state needs to be created for the event and added to the database
                        if (changeEnums[currentEvent.FileChangeTypeEnumId] != FileChangeType.Deleted)
                        {
                            // function to produce a new file system object to store in the database for the event's object;
                            // dynamically sets the path from input
                            Func<string, FileSystemObject> getNewFileSystemObject = fileSystemPath =>
                                {
                                    return new FileSystemObject()
                                    {
                                        CreationTime = currentEvent.FileSystemObject.CreationTime,
                                        IsFolder = currentEvent.FileSystemObject.IsFolder,
                                        LastTime = currentEvent.FileSystemObject.LastTime,
                                        Path = fileSystemPath,
                                        Size = currentEvent.FileSystemObject.Size,
                                        TargetPath = currentEvent.FileSystemObject.TargetPath,
                                        Revision = currentEvent.FileSystemObject.Revision,
                                        StorageKey = currentEvent.FileSystemObject.StorageKey
                                    };
                                };

                            // create the file system object for the local path for the event
                            FileSystemObject eventFileSystemObject = getNewFileSystemObject(currentEvent.FileSystemObject.Path);
                            // create the file system object for the server remapped path for the event,
                            // or set it to null for no path
                            FileSystemObject serverRemappedFileSystemObject = (serverRemappedNewPath == null
                                ? null
                                : getNewFileSystemObject(serverRemappedNewPath));
                            // add the file system object(s) to the database
                            indexDB.FileSystemObjects.AddObject(eventFileSystemObject);
                            if (serverRemappedFileSystemObject != null)
                            {
                                indexDB.FileSystemObjects.AddObject(serverRemappedFileSystemObject);
                            }
                            // save changes now so the file system object(s) will have ids
                            indexDB.SaveChanges();

                            // create the sync state for the current event for the latest sync with the new file system object(s) id(s)
                            SyncState eventSyncState = new SyncState()
                            {
                                FileSystemObjectId = eventFileSystemObject.FileSystemObjectId,
                                ServerLinkedFileSystemObjectId = (serverRemappedFileSystemObject == null
                                    ? (Nullable<long>)null
                                    : serverRemappedFileSystemObject.FileSystemObjectId),
                                SyncCounter = lastSyncs[0].SyncCounter
                            };

                            // add new sync state to database
                            indexDB.SyncStates.AddObject(eventSyncState);
                        }

                        // move the current event back one sync
                        // (or to a null sync if one does not exist)
                        currentEvent.SyncCounter = (lastSyncs.Length > 1
                            ? lastSyncs[1].SyncCounter
                            : (Nullable<long>)null);

                        // Finish writing any unsaved changes to the database
                        indexDB.SaveChanges();
                        // No errors occurred, so the transaction can be completed
                        transaction.Complete();
                    }
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }
        #endregion

        #region private methods
        /// <summary>
        /// Private constructor to ensure IndexingAgent is created through public static initializer (to return a CLError)
        /// </summary>
        private IndexingAgent() { }

        /// <summary>
        /// Action fired on a user worker thread which traverses the root path to build an initial index on application startup
        /// </summary>
        /// <param name="indexCompletionCallback">Callback should be the BeginProcessing method of the FileMonitor to forward the initial index</param>
        private void BuildIndex(Action<IEnumerable<KeyValuePair<FilePath, FileMetadata>>, IEnumerable<FileChange>> indexCompletionCallback)
        {
            // Create the initial index dictionary, throwing any exceptions that occurred in the process
            FilePathDictionary<FileMetadata> indexPaths;
            CLError indexPathCreationError = FilePathDictionary<FileMetadata>.CreateAndInitialize(indexedPath,
                out indexPaths);
            if (indexPathCreationError != null)
            {
                throw indexPathCreationError.GrabFirstException();
            }

            using (IndexDBEntities indexDB = new IndexDBEntities())
            {
                // Grab the most recent sync from the database to pull sync states
                Sync lastSync = indexDB.Syncs
                    .OrderByDescending(currentSync => currentSync.SyncCounter)
                    .FirstOrDefault();
                // Store the sync counter from the last sync, defaulting to null
                Nullable<long> lastSyncCounter = (lastSync == null
                    ? (Nullable<long>)null
                    : lastSync.SyncCounter);

                // Update the exposed last sync id string under a lock
                lock (this)
                {
                    this.LastSyncId = (lastSync == null
                        ? null
                        : lastSync.SyncId);
                }

                // Create a list for changes that need to be processed after the last sync
                List<FileChange> changeList = new List<FileChange>();

                // If there was a previous sync, use it as the basis for the starting index before calculating changes to apply
                if (lastSync != null)
                {
                    // Loop through the sync states for the last sync
                    foreach (SyncState currentSyncState in
                        indexDB.SyncStates
                            .Include(parent => parent.FileSystemObject)
                            .Where(syncState => (syncState.SyncCounter == null && lastSync.SyncId == null)
                                || syncState.SyncCounter == lastSyncCounter))
                    {
                        // Add the previous sync state to the initial index
                        indexPaths.Add(currentSyncState.FileSystemObject.Path,
                            new FileMetadata()
                            {
                                HashableProperties = new FileMetadataHashableProperties(currentSyncState.FileSystemObject.IsFolder,
                                    currentSyncState.FileSystemObject.LastTime,
                                    currentSyncState.FileSystemObject.CreationTime,
                                    currentSyncState.FileSystemObject.Size),
                                LinkTargetPath = currentSyncState.FileSystemObject.TargetPath,
                                Revision = currentSyncState.FileSystemObject.Revision,
                                StorageKey = currentSyncState.FileSystemObject.StorageKey
                            });
                    }
                }

                lock (changeEnumsLocker)
                {
                    // Loop through database events since the last sync to add changes
                    foreach (Event currentEvent in
                        indexDB.Events
                            .Include(parent => parent.FileSystemObject)
                            .Where(currentEvent => (currentEvent.SyncCounter == null && lastSyncCounter == null)
                                || currentEvent.SyncCounter == lastSyncCounter)
                            .OrderBy(currentEvent => currentEvent.EventId))
                    {
                        // Add database event to list of changes
                        changeList.Add(new FileChange()
                        {
                            Metadata = new FileMetadata()
                            {
                                HashableProperties = new FileMetadataHashableProperties(currentEvent.FileSystemObject.IsFolder,
                                    currentEvent.FileSystemObject.LastTime,
                                    currentEvent.FileSystemObject.CreationTime,
                                    currentEvent.FileSystemObject.Size),
                                LinkTargetPath = currentEvent.FileSystemObject.TargetPath,
                                Revision = currentEvent.FileSystemObject.Revision,
                                StorageKey = currentEvent.FileSystemObject.StorageKey
                            },
                            NewPath = currentEvent.FileSystemObject.Path,
                            OldPath = currentEvent.PreviousPath,
                            Type = changeEnums[currentEvent.FileChangeTypeEnumId],
                            DoNotAddToSQLIndex = true,
                            EventId = currentEvent.EventId
                        });
                    }
                }

                // Define DirectoryInfo at current path which will be traversed
                DirectoryInfo indexRootPath = new DirectoryInfo(indexedPath);

                // Loop through the paths that previously existed in the index, but were not found when traversing the indexed path
                foreach (string deletedPath in
                    indexPaths.Select(currentIndex => currentIndex.Key.ToString())
                        // RecurseIndexDirectory both adds the new changes to the list that are found on disc
                        // and returns a list of all paths traversed for comparison to the existing index
                        .Except(RecurseIndexDirectory(changeList, indexRootPath, indexPaths, this.RemoveEventById),
                            StringComparer.InvariantCulture))
                {
                    // For the path that existed previously in the index but is no longer found on disc, process as a deletion
                    changeList.Add(new FileChange()
                    {
                        NewPath = deletedPath,
                        Type = FileChangeType.Deleted,
                        Metadata = indexPaths[deletedPath]
                    });
                }

                // Callback on initial index completion
                // (will process detected changes and begin normal folder monitor processing)
                indexCompletionCallback(indexPaths,
                    changeList);
            }
        }

        // Define comparer to use locally for file/folder metadata
        private static FileMetadataHashableComparer fileComparer = new FileMetadataHashableComparer();

        /// <summary>
        /// Process changes found on disc that are different from the initial index to produce FileChanges
        /// and return the enumeration of paths traversed; recurses on self for inner folders
        /// </summary>
        /// <param name="changeList">List of FileChanges to add/update with new changes</param>
        /// <param name="currentDirectory">Current directory to scan</param>
        /// <param name="indexPaths">Initial index</param>
        /// <param name="AddEventCallback">Callback to fire if a database event needs to be added</param>
        /// <param name="uncoveredChanges">Optional list of changes which no longer have a corresponding local path, only set when self-recursing</param>
        /// <returns>Returns the list of paths traversed</returns>
        private static IEnumerable<string> RecurseIndexDirectory(List<FileChange> changeList, DirectoryInfo currentDirectory, FilePathDictionary<FileMetadata> indexPaths, Func<long, CLError> RemoveEventCallback, Dictionary<FilePath, LinkedList<FileChange>> uncoveredChanges = null)
        {
            // Store whether the current method call is outermost or a recursion,
            // only the outermost method call has a null uncoveredChanges parameter
            bool outermostMethodCall = (uncoveredChanges == null);

            // If current method call is not a self-recursion,
            // build the uncoveredChanges dictionary with initial values from the values in changeList
            if (outermostMethodCall)
            {
                uncoveredChanges = new Dictionary<FilePath, LinkedList<FileChange>>(FilePathComparer.Instance);
                    //new Dictionary<FilePath, FileChange>(changeList.Count,
                    //FilePathComparer.Instance);
                foreach (FileChange currentChange in changeList)
                {
                    if (uncoveredChanges.ContainsKey(currentChange.NewPath))
                    {
                        uncoveredChanges[currentChange.NewPath].AddFirst(currentChange);
                    }
                    else
                    {
                        uncoveredChanges.Add(currentChange.NewPath, new LinkedList<FileChange>(new FileChange[] { currentChange }));
                    }
                }
            }

            // Current path traversed, remove from uncoveredChanges
            uncoveredChanges.Remove(currentDirectory.FullName);

            // Create a list of the traversed paths at or below the current level
            List<string> filePathsFound = new List<string>();

            // Loop through all subdirectories under the current directory
            foreach (DirectoryInfo subDirectory in currentDirectory.EnumerateDirectories())
            {
                // Store current subdirectory path as traversed
                filePathsFound.Add(subDirectory.FullName);
                // Create properties for the current subdirectory
                FileMetadataHashableProperties compareProperties = new FileMetadataHashableProperties(true,
                    subDirectory.LastWriteTimeUtc,
                    subDirectory.CreationTimeUtc,
                    null);
                // Grab the last event that matches the current directory path, if any
                FileChange existingEvent = changeList.LastOrDefault(currentChange => FilePathComparer.Instance.Equals(currentChange.NewPath, (FilePath)subDirectory));
                // If there is no existing event, the directory has to be checked for changes
                if (existingEvent == null)
                {
                    // If the index did not include the current subdirectory, it needs to be added as a creation
                    if (!indexPaths.ContainsKey(subDirectory))
                    {
                        changeList.Add(new FileChange()
                        {
                            NewPath = subDirectory.FullName,
                            Type = FileChangeType.Created,
                            Metadata = new FileMetadata()
                            {
                                HashableProperties = compareProperties
                            }
                        });
                    }
                }
                // Add the inner paths to the output list by recursing (which will also process inner changes)
                filePathsFound.AddRange(RecurseIndexDirectory(changeList,
                    subDirectory,
                    indexPaths,
                    RemoveEventCallback,
                    uncoveredChanges));
            }
            // Loop through all files under the current directory
            foreach (FileInfo currentFile in currentDirectory.EnumerateFiles())
            {
                // Remove file from list of changes which have not yet been traversed (since it has been traversed)
                uncoveredChanges.Remove(currentFile.FullName);

                // define value for file size to be used in metadata, defaulting to an impossible value -1
                long currentFileLength = -1;
                // presume finding the file length did not fail until it fails
                bool fileLengthFailed = false;
                try
                {
                    // store the length of the current file
                    currentFileLength = currentFile.Length;
                }
                catch
                {
                    // finding the file length failed
                    // (probably because the file was deleted in the time between reading the current directory and reading the current file)
                    fileLengthFailed = true;
                }
                // If finding the file length did not fail and the length itself is a sensible value,
                // check the file against the index and existing events and mark it traversed
                if (!fileLengthFailed
                    && currentFileLength >= 0)
                {
                    // Add file path to traversed output list
                    filePathsFound.Add(currentFile.FullName);
                    // Find file properties
                    FileMetadataHashableProperties compareProperties = new FileMetadataHashableProperties(false,
                        currentFile.LastWriteTimeUtc,
                        currentFile.CreationTimeUtc,
                        currentFileLength);
                    // Find the latest change at the current file path, if any exist
                    FileChange existingEvent = changeList.LastOrDefault(currentChange => FilePathComparer.Instance.Equals(currentChange.NewPath, (FilePath)currentFile));
                    // If a change does not already exist for the current file path,
                    // check if file has changed since last index to process changes
                    if (existingEvent == null)
                    {
                        // If the index already contained a file at the current path,
                        // then check if a file modification needs to be processed
                        if (indexPaths.ContainsKey(currentFile))
                        {
                            FileMetadata existingIndexPath = indexPaths[currentFile];

                            // If the file has changed (different metadata), then process a file modification change
                            if (!fileComparer.Equals(compareProperties, existingIndexPath.HashableProperties))
                            {
                                changeList.Add(new FileChange()
                                {
                                    NewPath = currentFile,
                                    Type = FileChangeType.Modified,
                                    Metadata = new FileMetadata()
                                    {
                                        HashableProperties = compareProperties,
                                        LinkTargetPath = existingIndexPath.LinkTargetPath,//Todo: needs to check again for new target path
                                        Revision = existingIndexPath.Revision,
                                        StorageKey = existingIndexPath.StorageKey
                                    }
                                });
                            }
                        }
                        // else if index doesn't contain the current path, then the file has been created
                        else
                        {
                            changeList.Add(new FileChange()
                            {
                                NewPath = currentFile,
                                Type = FileChangeType.Created,
                                Metadata = new FileMetadata()
                                {
                                    HashableProperties = compareProperties
                                }
                            });
                        }
                    }
                    // else if a change exists at the current file path and the file has changed
                    else if (!fileComparer.Equals(compareProperties, existingEvent.Metadata.HashableProperties))
                    {
                        // mark that SQL can be updated again with the changes to the metadata
                        existingEvent.DoNotAddToSQLIndex = false;
                    }
                }
            }

            // If this method call was the outermost one (not recursed),
            // then the uncoveredChanges list was depleted of all traversed paths leaving
            // only file changes that no longer match anything existing on the disc
            // (meaning the change needs to be reversed since the file/folder was deleted)
            if (outermostMethodCall)
            {
                // Loop through the uncovered file changes
                foreach (KeyValuePair<FilePath, LinkedList<FileChange>> uncoveredChange in uncoveredChanges)
                {
                    // Take all the changes at a path which no longer has a file/folder and
                    // either remove all the events (if the last sync index did not contain the folder)
                    // or turn all changes into a single deletion change (if the last sync index did contain the folder)
                    bool existingDeletion = false;
                    LinkedListNode<FileChange> currentUncoveredChange = uncoveredChange.Value.First;
                    bool existsInIndex = indexPaths.ContainsKey(uncoveredChange.Key);
                    // Continue checking the linked list nodes until it is past the end (thus null)
                    while (currentUncoveredChange != null)
                    {
                        // Only keep the first deletion event and only if there is a path in the index for the corresponding delete
                        if (existsInIndex
                            && !existingDeletion
                            && currentUncoveredChange.Value.Type == FileChangeType.Deleted)
                        {
                            existingDeletion = true;
                        }
                        else
                        {
                            changeList.Remove(currentUncoveredChange.Value);
                            if (currentUncoveredChange.Value.EventId > 0)
                            {
                                RemoveEventCallback(currentUncoveredChange.Value.EventId);
                            }
                        }

                        // Move to the next FileChange in the linked list
                        currentUncoveredChange = currentUncoveredChange.Next;
                    }
                }
            }

            // return the list of all traversed paths at or below the current directory
            return filePathsFound;
        }
        #endregion private methods
    }
}