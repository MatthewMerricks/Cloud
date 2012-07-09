//
// IndexingAgent.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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
                newIndexer = (IndexingAgent)Helpers.DefaultForType(typeof(IndexingAgent));
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
                                .Include(((MemberExpression)((Expression<Func<SQLEnum, EnumCategory>>)(parent => parent.EnumCategory)).Body).Member.Name)
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
                        syncStates = (FilePathDictionary<SyncedObject>)Helpers.DefaultForType(typeof(FilePathDictionary<SyncedObject>));
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
                                .Include(((MemberExpression)((Expression<Func<SyncState, FileSystemObject>>)(parent => parent.FileSystemObject)).Body).Member.Name)
                                .Include(((MemberExpression)((Expression<Func<SyncState, FileSystemObject>>)(parent => parent.ServerLinkedFileSystemObject)).Body).Member.Name)
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
                                            currentSyncState.FileSystemObject.LastTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                            currentSyncState.FileSystemObject.CreationTime ??  new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                            currentSyncState.FileSystemObject.Size)
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
                    Nullable<int> lastSyncCounter = (lastSync == null
                        ? (Nullable<int>)null
                        : lastSync.SyncCounter);

                    // Create the output list
                    changeEvents = new List<KeyValuePair<FilePath,FileChange>>();

                    // Loop through all the events in the database after the last sync (if any)
                    foreach (Event currentChange in
                        indexDB.Events
                            .Include(((MemberExpression)((Expression<Func<Event, FileSystemObject>>)(parent => parent.FileSystemObject)).Body).Member.Name)
                            .Where(currentChange => (currentChange.SyncCounter == null && lastSyncCounter == null)
                                || (currentChange.SyncCounter == lastSyncCounter))
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
                                        currentChange.FileSystemObject.LastTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                        currentChange.FileSystemObject.CreationTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                        currentChange.FileSystemObject.Size)
                                }
                            }));
                    }
                }
            }
            catch (Exception ex)
            {
                changeEvents = (List<KeyValuePair<FilePath, FileChange>>)Helpers.DefaultForType(typeof(List<KeyValuePair<FilePath, FileChange>>));
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
                        Nullable<int> lastSyncCounter = (lastSync == null
                            ? (Nullable<int>)null
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
                                Size = newEvent.Metadata.HashableProperties.Size
                            },
                            PreviousPath = newEvent.OldPath == null
                                ? null
                                : newEvent.OldPath.ToString()
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
        public CLError RemoveEventById(int eventId)
        {
            try
            {
                using (IndexDBEntities indexDB = new IndexDBEntities())
                {
                    // Find the existing event for the given id
                    Event toDelete = indexDB.Events.FirstOrDefault(currentEvent => currentEvent.EventId == eventId);
                    // Throw exception if an existing event does not exist
                    if (toDelete == null)
                    {
                        throw new Exception("Event not found to delete");
                    }
                    // Remove the found event from the database
                    indexDB.DeleteObject(toDelete);
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
        public CLError RemoveEventsByIds(IEnumerable<int> eventIds)
        {
            try
            {
                // copy event id collection to array, defaulting to an empty array
                int[] eventIdsArray = (eventIds == null
                    ? new int[0]
                    : eventIds.ToArray());
                using (IndexDBEntities indexDB = new IndexDBEntities())
                {
                    // Create list to copy event ids from database objects,
                    // used to ensure all event ids to be deleted were found
                    List<int> orderedDBIds = new List<int>();
                    // Grab all events with ids in the specified range
                    Event[] deleteEvents = indexDB.Events
                        .Where(currentEvent => eventIdsArray.Contains(currentEvent.EventId))
                        .OrderBy(currentEvent => currentEvent.EventId)
                        .ToArray();
                    // Delete each event that was returned
                    Array.ForEach(deleteEvents, toDelete =>
                        {
                            orderedDBIds.Add(toDelete.EventId);
                            indexDB.DeleteObject(toDelete);
                        });
                    // Check all event ids intended for delete and make sure they were actually deleted,
                    // otherwise throw exception
                    foreach (int deletedEventId in eventIdsArray)
                    {
                        if (orderedDBIds.BinarySearch(deletedEventId) < 0)
                        {
                            throw new Exception("Event with id " + deletedEventId + " not found to delete");
                        }
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
        public CLError RecordCompletedSync(string syncId, IEnumerable<int> syncedEventIds, out int syncCounter, string newRootPath = null)
        {
            // Default the output sync counter
            syncCounter = (int)Helpers.DefaultForType(typeof(int));
            try
            {
                // Copy event ids completed in sync to array, defaulting to an empty array
                int[] syncedEventIdsEnumerated = (syncedEventIds == null
                    ? new int[0]
                    : syncedEventIds.ToArray());

                using (IndexDBEntities indexDB = new IndexDBEntities())
                {
                    // Run entire sync completion database operation set within a transaction to ensure
                    // automatic rollback on failure
                    using (TransactionScope completionSync = new TransactionScope())
                    {
                        // Retrieve last sync if it exists
                        Sync lastSync = indexDB.Syncs
                            .OrderByDescending(currentSync => currentSync.SyncCounter)
                            .FirstOrDefault();
                        // Store last sync counter value or null for no last sync
                        Nullable<int> lastSyncCounter = (lastSync == null
                            ? (Nullable<int>)null
                            : lastSync.SyncCounter);
                        // Default root path from last sync if it was not passed in
                        newRootPath = string.IsNullOrEmpty(newRootPath)
                            ? lastSync.RootPath
                            : newRootPath;

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
                                .Include(((MemberExpression)((Expression<Func<SyncState, FileSystemObject>>)(parent => parent.FileSystemObject)).Body).Member.Name)
                                .Include(((MemberExpression)((Expression<Func<SyncState, FileSystemObject>>)(parent => parent.ServerLinkedFileSystemObject)).Body).Member.Name)
                                .Where(currentSync => currentSync.SyncCounter == (int)lastSyncCounter))
                            {
                                // Check if previous syncstate had a server-remapped path to store
                                if (currentState.ServerLinkedFileSystemObject != null)
                                {
                                    serverRemappedPaths.Add(currentState.FileSystemObject.Path,
                                        currentState.ServerLinkedFileSystemObject.Path);
                                }

                                // Add the previous sync state to the dictionary as the baseline before changes
                                newSyncStates.Add(currentState.FileSystemObject.Path,
                                    new FileMetadata()
                                    {
                                        HashableProperties = new FileMetadataHashableProperties(currentState.FileSystemObject.IsFolder,
                                            currentState.FileSystemObject.LastTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                            currentState.FileSystemObject.CreationTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                            currentState.FileSystemObject.Size)
                                    });
                            }
                        }

                        // Grab all events from the database since the previous sync, ordering by id to ensure correct processing logic
                        Event[] existingEvents = indexDB.Events
                            .Include(((MemberExpression)((Expression<Func<Event, FileSystemObject>>)(parent => parent.FileSystemObject)).Body).Member.Name)
                            .Where(currentEvent => currentEvent.SyncCounter == lastSyncCounter)
                            .OrderBy(currentEvent => currentEvent.EventId)
                            .ToArray();

                        // Loop through existing events to process into the new sync states
                        foreach (Event previousEvent in existingEvents)
                        {
                            // If the current database event is in the list of events that are completed,
                            // the syncstates have to be modified appropriately to include the change
                            if (syncedEventIdsEnumerated.Contains(previousEvent.EventId))
                            {
                                switch (changeEnums[previousEvent.FileChangeTypeEnumId])
                                {
                                    case FileChangeType.Created:
                                        newSyncStates.Add(previousEvent.FileSystemObject.Path,
                                            new FileMetadata()
                                            {
                                                HashableProperties = new FileMetadataHashableProperties(previousEvent.FileSystemObject.IsFolder,
                                                    previousEvent.FileSystemObject.LastTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                                    previousEvent.FileSystemObject.CreationTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                                    previousEvent.FileSystemObject.Size)
                                            });
                                        break;
                                    case FileChangeType.Deleted:
                                        newSyncStates.Remove(previousEvent.FileSystemObject.Path);
                                        break;
                                    case FileChangeType.Modified:
                                        if (newSyncStates.ContainsKey(previousEvent.FileSystemObject.Path))
                                        {
                                            newSyncStates[previousEvent.FileSystemObject.Path].HashableProperties = new FileMetadataHashableProperties(previousEvent.FileSystemObject.IsFolder,
                                                    previousEvent.FileSystemObject.LastTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                                    previousEvent.FileSystemObject.CreationTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                                    previousEvent.FileSystemObject.Size);
                                        }
                                        else
                                        {
                                            newSyncStates.Add(previousEvent.FileSystemObject.Path,
                                                new FileMetadata()
                                                {
                                                    HashableProperties = new FileMetadataHashableProperties(previousEvent.FileSystemObject.IsFolder,
                                                        previousEvent.FileSystemObject.LastTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                                        previousEvent.FileSystemObject.CreationTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                                        previousEvent.FileSystemObject.Size)
                                                });
                                        }
                                        break;
                                    case FileChangeType.Renamed:
                                        if (newSyncStates.ContainsKey(previousEvent.FileSystemObject.Path))
                                        {
                                            if (newSyncStates.ContainsKey(previousEvent.PreviousPath))
                                            {
                                                newSyncStates.Remove(previousEvent.PreviousPath);
                                            }
                                            newSyncStates[previousEvent.FileSystemObject.Path].HashableProperties = new FileMetadataHashableProperties(previousEvent.FileSystemObject.IsFolder,
                                                    previousEvent.FileSystemObject.LastTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                                    previousEvent.FileSystemObject.CreationTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                                    previousEvent.FileSystemObject.Size);
                                        }
                                        else if (newSyncStates.ContainsKey(previousEvent.PreviousPath))
                                        {
                                            newSyncStates.Rename(previousEvent.PreviousPath, previousEvent.FileSystemObject.Path);

                                            newSyncStates[previousEvent.FileSystemObject.Path].HashableProperties = new FileMetadataHashableProperties(previousEvent.FileSystemObject.IsFolder,
                                                    previousEvent.FileSystemObject.LastTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                                    previousEvent.FileSystemObject.CreationTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                                    previousEvent.FileSystemObject.Size);
                                        }
                                        else
                                        {
                                            newSyncStates.Add(previousEvent.FileSystemObject.Path,
                                                new FileMetadata()
                                                {
                                                    HashableProperties = new FileMetadataHashableProperties(previousEvent.FileSystemObject.IsFolder,
                                                        previousEvent.FileSystemObject.LastTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                                        previousEvent.FileSystemObject.CreationTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                                        previousEvent.FileSystemObject.Size)
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
                                Size = newSyncState.Value.HashableProperties.Size
                            };
                            indexDB.FileSystemObjects.AddObject(newSyncedObject);

                            // If the file/folder path is remapped on the server, add the file/folder object for the server-mapped state
                            Nullable<int> serverRemappedObjectId = null;
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
                                    Size = newSyncState.Value.HashableProperties.Size
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
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Method to merge event into database,
        /// used when events are modified or replaced with new events
        /// </summary>
        /// <param name="mergedEvent">Event with latest file or folder metadata, pass null to only delete the old event</param>
        /// <param name="eventToRemove">Previous event to set if an old event is being replaced in the process</param>
        /// <returns>Returns an error from merging the events, if any</returns>
        public CLError MergeEventIntoDatabase(FileChange mergedEvent, FileChange eventToRemove)
        {
            try
            {
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
                Nullable<int> eventIdToUpdate = null;

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
                            .Include(((MemberExpression)((Expression<Func<Event, FileSystemObject>>)(parent => parent.FileSystemObject)).Body).Member.Name)
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
                throw (Exception)indexPathCreationError.errorInfo[CLError.ErrorInfo_Exception];
            }

            using (IndexDBEntities indexDB = new IndexDBEntities())
            {
                // Grab the most recent sync from the database to pull sync states
                Sync lastSync = indexDB.Syncs
                    .OrderByDescending(currentSync => currentSync.SyncCounter)
                    .FirstOrDefault();
                // Store the sync counter from the last sync, defaulting to null
                Nullable<int> lastSyncCounter = (lastSync == null
                    ? (Nullable<int>)null
                    : lastSync.SyncCounter);

                // Create a list for changes that need to be processed after the last sync
                List<FileChange> changeList = new List<FileChange>();

                // If there was a previous sync, use it as the basis for the starting index before calculating changes to apply
                if (lastSync != null)
                {
                    // Loop through the sync states for the last sync
                    foreach (SyncState currentSyncState in
                        indexDB.SyncStates
                            .Include(((MemberExpression)((Expression<Func<SyncState, FileSystemObject>>)(parent => parent.FileSystemObject)).Body).Member.Name)
                            .Where(syncState => (syncState.SyncCounter == null && lastSync.SyncId == null)
                                || (syncState.SyncCounter == lastSyncCounter)))
                    {
                        // Add the previous sync state to the initial index
                        indexPaths.Add(currentSyncState.FileSystemObject.Path,
                            new FileMetadata()
                            {
                                HashableProperties = new FileMetadataHashableProperties(currentSyncState.FileSystemObject.IsFolder,
                                    currentSyncState.FileSystemObject.LastTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                    currentSyncState.FileSystemObject.CreationTime ??  new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                    currentSyncState.FileSystemObject.Size)
                            });
                    }
                }

                lock (changeEnumsLocker)
                {
                    // Loop through database events since the last sync to add changes
                    foreach (Event currentEvent in
                        indexDB.Events
                            .Include(((MemberExpression)((Expression<Func<Event, FileSystemObject>>)(parent => parent.FileSystemObject)).Body).Member.Name)
                            .Where(currentEvent => (currentEvent.SyncCounter == null && lastSyncCounter == null)
                                || (currentEvent.SyncCounter == lastSyncCounter))
                            .OrderBy(currentEvent => currentEvent.EventId))
                    {
                        // Add database event to list of changes
                        changeList.Add(new FileChange()
                        {
                            Metadata = new FileMetadata()
                            {
                                HashableProperties = new FileMetadataHashableProperties(currentEvent.FileSystemObject.IsFolder,
                                    currentEvent.FileSystemObject.LastTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                    currentEvent.FileSystemObject.CreationTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                    currentEvent.FileSystemObject.Size)
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
        private static IEnumerable<string> RecurseIndexDirectory(List<FileChange> changeList, DirectoryInfo currentDirectory, FilePathDictionary<FileMetadata> indexPaths, Func<int, CLError> RemoveEventCallback, Dictionary<FilePath, LinkedList<FileChange>> uncoveredChanges = null)
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
                    // last time is the greater of the the last access time and the last write time
                    DateTime.Compare(subDirectory.LastAccessTimeUtc, subDirectory.LastWriteTimeUtc) > 0
                        ? subDirectory.LastAccessTimeUtc
                        : subDirectory.LastWriteTimeUtc,
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
                        // last time is the greater of the the last access time and the last write time
                        DateTime.Compare(currentFile.LastAccessTimeUtc, currentFile.LastWriteTimeUtc) > 0
                            ? currentFile.LastAccessTimeUtc
                            : currentFile.LastWriteTimeUtc,
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
                            // If the file has changed (different metadata), then process a file modification change
                            if (!FilePathComparer.Equals(compareProperties, indexPaths[currentFile].HashableProperties))
                            {
                                changeList.Add(new FileChange()
                                {
                                    NewPath = currentFile,
                                    Type = FileChangeType.Modified,
                                    Metadata = new FileMetadata()
                                    {
                                        HashableProperties = compareProperties
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