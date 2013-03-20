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
using System.Globalization;
using Cloud.Model;
using Cloud.Static;
using Cloud.SQLIndexer.SqlModel;
using Cloud.SQLIndexer.Migrations;
using Cloud.SQLIndexer.Model;
using SqlSync = Cloud.SQLIndexer.SqlModel.Sync;
using Cloud.Interfaces;
using Cloud.Support;
using Cloud.Model.EventMessages.ErrorInfo;
using Cloud.SQLProxies;

namespace Cloud.SQLIndexer
{
    internal sealed class IndexingAgent : IDisposable
    {
        #region private fields
        private static CLTrace _trace = CLTrace.Instance;
        // store the path that represents the root of indexing
        private string indexedPath = null;
        private readonly CLSyncBox syncBox;
        private long rootFileSystemObjectId = 0;

        #region SQLite
        private readonly string indexDBLocation;
        private const string indexDBPassword = "Q29weXJpZ2h0Q2xvdWQuY29tQ3JlYXRlZEJ5RGF2aWRCcnVjaw=="; // <-- if you change this password, you will likely break all clients with older databases
        private const string indexScriptsResourceFolder = ".SQLIndexer.IndexDBScripts.";

        public readonly ReaderWriterLockSlim ExternalSQLLocker = new ReaderWriterLockSlim();
        #endregion

        // store dictionaries to convert between the FileChangetype enumeration and its integer value in the database,
        // will be filled in during startup
        private static Dictionary<long, FileChangeType> changeEnums = null;
        private static Dictionary<FileChangeType, long> changeEnumsBackward = null;

        // category in SQL that represents the Enumeration type FileChangeType
        private static long changeCategoryId = 0;
        // locker for reading/writing the change enumerations
        private static object changeEnumsLocker = new object();
        #endregion

        #region public properties
        /// <summary>
        /// Store the last Sync Id, starts null before indexing; lock on the IndexingAgent instance for all reads/writes
        /// </summary>
        public string LastSyncId { get; private set; }
        public readonly ReaderWriterLockSlim LastSyncLocker = new ReaderWriterLockSlim();
        #endregion

        /// <summary>
        /// Creates the SQL indexing service and outputs it,
        /// must be started afterwards with StartInitialIndexing
        /// </summary>
        /// <param name="newIndexer">Output indexing agent</param>
        /// <param name="syncBox">SyncBox to index</param>
        /// <returns>Returns the error that occurred during creation, if any</returns>
        public static CLError CreateNewAndInitialize(out IndexingAgent newIndexer, CLSyncBox syncBox)
        {
            // Fill in output with constructor
            IndexingAgent newAgent;
            try
            {
                newIndexer = newAgent = new IndexingAgent(syncBox); // this double instance setting is required for some reason to prevent a "does not exist in the current context" compiler error
            }
            catch (Exception ex)
            {
                newIndexer = Helpers.DefaultForType<IndexingAgent>();
                return ex;
            }

            try
            {
                newIndexer.InitializeDatabase(syncBox.CopiedSettings.SyncRoot);
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        #region public methods
        /// <summary>
        /// Queries database by eventId to return latest metadata and path as a FileChange and whether or not the event is still pending
        /// </summary>
        /// <param name="eventId">EventId key to lookup</param>
        /// <param name="queryResult">(output) Result FileChange from EventId lookup</param>
        /// <param name="isPending">(output) Result whether event is pending from EventId lookup</param>
        /// <param name="status">(output) Status of quering the database</param>
        /// <returns>Returns any error which occurred querying the database, if any</returns>
        public CLError QueryFileChangeByEventId(long eventId, out FileChange queryResult, out bool isPending, out FileChangeQueryStatus status)
        {
            throw new NotImplementedException("1");
            //try
            //{
            //    using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(this.indexDBLocation)))
            //    {
            //        FileSystemObject[] resultSet = SqlAccessor<FileSystemObject>.SelectResultSet(
            //            indexDB,
            //            "SELECT " +
            //                SqlAccessor<FileSystemObject>.GetSelectColumns() + ", " +
            //                SqlAccessor<Event>.GetSelectColumns("Event") + " " +
            //                "FROM [FileSystemObjects] " +
            //                "INNER JOIN [Events] ON [FileSystemObjects].[EventId] = [Events].[EventId] " +
            //                "WHERE [Events].[EventId] = " + eventId.ToString() +
            //                "ORDER BY [FileSystemObjects].[FileSystemObjectId] DESC",
            //            includes: new[]
            //            {
            //                "Event"
            //            }).ToArray();

            //        if (resultSet.Length == 0)
            //        {
            //            status = FileChangeQueryStatus.ErrorNotFound;
            //            queryResult = null;
            //            isPending = false;
            //        }
            //        else
            //        {
            //            string previousSyncRoot;
            //            if (resultSet[0].SyncCounter == null)
            //            {
            //                previousSyncRoot = indexedPath;
            //            }
            //            else
            //            {
            //                previousSyncRoot = 
            //                    SqlAccessor<SqlSync>.SelectResultSet(
            //                        indexDB,
            //                        "SELECT * FROM [Syncs] " +
            //                        "WHERE [Syncs].[SyncCounter] = " + ((long)resultSet[0].SyncCounter).ToString())
            //                        .SingleOrDefault().RootPath;
            //            }

            //            queryResult = new FileChange()
            //            {
            //                Direction = (resultSet[0].Event.SyncFrom ? SyncDirection.From : SyncDirection.To),
            //                EventId = eventId,
            //                Metadata = new FileMetadata()
            //                {
            //                    // TODO: add server id
            //                    HashableProperties = new FileMetadataHashableProperties(resultSet[0].IsFolder,
            //                        resultSet[0].LastTime,
            //                        resultSet[0].CreationTime,
            //                        resultSet[0].Size),
            //                    LinkTargetPath = resultSet[0].TargetPath,
            //                    Revision = resultSet[0].Revision,
            //                    StorageKey = resultSet[0].StorageKey,
            //                },
            //                NewPath = indexedPath + ((FilePath)resultSet[0].Path).GetRelativePath(previousSyncRoot, false),
            //                OldPath = (string.IsNullOrEmpty(resultSet[0].Event.PreviousPath)
            //                    ? null
            //                    : indexedPath + ((FilePath)resultSet[0].Event.PreviousPath).GetRelativePath(previousSyncRoot, false)),
            //                Type = changeEnums[resultSet[0].Event.FileChangeTypeEnumId],
            //            };

            //            if (resultSet.Length == 1)
            //            {
            //                isPending = resultSet[0].SyncCounter == null;
            //                status = FileChangeQueryStatus.Success;
            //            }
            //            else
            //            {
            //                isPending = resultSet.Any(currentResult => currentResult.SyncCounter == null);
            //                status = FileChangeQueryStatus.ErrorMultipleResults;
            //            }
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    status = FileChangeQueryStatus.ErrorUnknown;
            //    queryResult = Helpers.DefaultForType<FileChange>();
            //    isPending = Helpers.DefaultForType<bool>();
            //    return ex;
            //}
            //return null;
        }

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

        //// whole method removed because SyncedObject class was removed (no more server-linked sync states since ServerName is a property of FileSystemObject)
        //
        ///// <summary>
        ///// Retrieve the complete file system state at the time of the last sync
        ///// </summary>
        ///// <param name="syncStates">Outputs the file system state</param>
        ///// <returns>Returns an error that occurred retrieving the file system state, if any</returns>
        //public CLError GetLastSyncStates(out FilePathDictionary<SyncedObject> syncStates)
        //{
        //    throw new NotImplementedException("2");
        //    //ExternalSQLLocker.EnterReadLock();
        //    //try
        //    //{
        //    //    using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(this.indexDBLocation)))
        //    //    {
        //    //        // Pull the last sync from the database
        //    //        SqlSync lastSync = SqlAccessor<SqlSync>
        //    //            .SelectResultSet(indexDB,
        //    //                "SELECT TOP 1 * FROM [Syncs] ORDER BY [Syncs].[SyncCounter] DESC")
        //    //            .SingleOrDefault();

        //    //        // Default the sync states (to null) if there was never a sync
        //    //        if (lastSync == null)
        //    //        {
        //    //            syncStates = Helpers.DefaultForType<FilePathDictionary<SyncedObject>>();
        //    //        }
        //    //        // If there was a sync, continue on to build the sync state
        //    //        else
        //    //        {
        //    //            // Create the dictionary of sync states to output
        //    //            CLError createDictError = FilePathDictionary<SyncedObject>.CreateAndInitialize(lastSync.RootPath,
        //    //                out syncStates);
        //    //            if (createDictError != null)
        //    //            {
        //    //                return createDictError;
        //    //            }

        //    //            Dictionary<long, KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>> mappedSyncStates = new Dictionary<long,KeyValuePair<GenericHolder<FileSystemObject>,GenericHolder<FileSystemObject>>>();

        //    //            // Loop through all sync states for the last sync
        //    //            foreach (FileSystemObject currentSyncState in SqlAccessor<FileSystemObject>
        //    //                .SelectResultSet(indexDB,
        //    //                    "SELECT * FROM [FileSystemObjects] WHERE [FileSystemObjects].[SyncCounter] = " + lastSync.SyncCounter.ToString()))
        //    //            {
        //    //                if (mappedSyncStates.ContainsKey(currentSyncState.FileSystemObjectId))
        //    //                {
        //    //                    if (currentSyncState.ServerLinked)
        //    //                    {
        //    //                        mappedSyncStates[currentSyncState.FileSystemObjectId].Value.Value = currentSyncState;
        //    //                    }
        //    //                    else
        //    //                    {
        //    //                        mappedSyncStates[currentSyncState.FileSystemObjectId].Key.Value = currentSyncState;
        //    //                    }
        //    //                }
        //    //                else if (currentSyncState.ServerLinked)
        //    //                {
        //    //                    mappedSyncStates.Add(currentSyncState.FileSystemObjectId,
        //    //                        new KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>(
        //    //                            new GenericHolder<FileSystemObject>(),
        //    //                            new GenericHolder<FileSystemObject>(currentSyncState)));
        //    //                }
        //    //                else
        //    //                {
        //    //                    mappedSyncStates.Add(currentSyncState.FileSystemObjectId,
        //    //                        new KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>(
        //    //                            new GenericHolder<FileSystemObject>(currentSyncState),
        //    //                            new GenericHolder<FileSystemObject>()));
        //    //                }
        //    //            }

        //    //            foreach (KeyValuePair<long, KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>> currentSyncState in mappedSyncStates)
        //    //            {
        //    //                // Add the current sync state from the last sync to the output dictionary
        //    //                syncStates.Add(currentSyncState.Value.Key.Value.Path,
        //    //                    new SyncedObject()
        //    //                    {
        //    //                        ServerLinkedPath = currentSyncState.Value.Value.Value == null
        //    //                            ? null
        //    //                            : currentSyncState.Value.Value.Value.Path,
        //    //                        Metadata = new FileMetadata()
        //    //                        {
        //    //                            // TODO: add server id
        //    //                            HashableProperties = new FileMetadataHashableProperties(currentSyncState.Value.Key.Value.IsFolder,
        //    //                                currentSyncState.Value.Key.Value.LastTime,
        //    //                                currentSyncState.Value.Key.Value.CreationTime,
        //    //                                currentSyncState.Value.Key.Value.Size),
        //    //                            LinkTargetPath = currentSyncState.Value.Key.Value.TargetPath,
        //    //                            Revision = currentSyncState.Value.Key.Value.Revision,
        //    //                            StorageKey = currentSyncState.Value.Key.Value.StorageKey
        //    //                        }
        //    //                    });
        //    //            }
        //    //        }
        //    //    }
        //    //}
        //    //catch (Exception ex)
        //    //{
        //    //    syncStates = null;
        //    //    return ex;
        //    //}
        //    //finally
        //    //{
        //    //    ExternalSQLLocker.ExitReadLock();
        //    //}
        //    //return null;
        //}

        public CLError GetMetadataByPathAndRevision(string path, string revision, out FileMetadata metadata)
        {
            throw new NotImplementedException("3");
            //ExternalSQLLocker.EnterReadLock();
            //try
            //{
            //    if (string.IsNullOrEmpty(path))
            //    {
            //        throw new NullReferenceException("path cannot be null");
            //    }

            //    using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(this.indexDBLocation)))
            //    {
            //        // Grab the most recent sync from the database to pull sync states
            //        SqlSync lastSync = SqlAccessor<SqlSync>
            //            .SelectResultSet(indexDB,
            //                "SELECT TOP 1 * FROM [Syncs] ORDER BY [Syncs].[SyncCounter] DESC")
            //            .SingleOrDefault();

            //        if (lastSync == null)
            //        {
            //            metadata = null;
            //        }
            //        else
            //        {
            //            int pathCRC = StringCRC.Crc(path);

            //            // TODO: need to add back the null check on revision below when server fixes the issue with the 'revision' field on file renames

            //            FileSystemObject foundSync = SqlAccessor<FileSystemObject>
            //                .SelectResultSet(indexDB,
            //                    "SELECT TOP 1 * " +
            //                    "FROM [FileSystemObjects] " +
            //                    "WHERE [FileSystemObjects].[SyncCounter] = " + lastSync.SyncCounter.ToString() + " " +
            //                    "AND [FileSystemObjects].[PathChecksum] = " + pathCRC.ToString() + " " +
            //                    (revision == null
            //                        ? string.Empty//"AND [FileSystemObjects].[Revision] IS NULL " <--- temporarily removed since server stopped sending 'revision' in metadata on file renames
            //                        : "AND [FileSystemObjects].[Revision] = '" + revision.Replace("'", "''").ToLowerInvariant() + "'") +
            //                    " ORDER BY [FileSystemObjects].[EventId] DESC")
            //                .SingleOrDefault(parent => parent.Path == path); // run in memory since Path field is not indexable

            //            if (foundSync != null)
            //            {
            //                metadata = new FileMetadata()
            //                {
            //                    // TODO: add server id
            //                    HashableProperties = new FileMetadataHashableProperties(foundSync.IsFolder,
            //                        foundSync.LastTime,
            //                        foundSync.CreationTime,
            //                        foundSync.Size),
            //                    LinkTargetPath = foundSync.TargetPath,
            //                    Revision = foundSync.Revision,
            //                    StorageKey = foundSync.StorageKey
            //                };
            //            }
            //            else
            //            {
            //                metadata = null;
            //            }
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    metadata = Helpers.DefaultForType<FileMetadata>();
            //    return ex;
            //}
            //finally
            //{
            //    ExternalSQLLocker.ExitReadLock();
            //}
            //return null;
        }

        /// <summary>
        /// Retrieves all unprocessed events that occurred since the last sync
        /// </summary>
        /// <param name="changeEvents">Outputs the unprocessed events</param>
        /// <returns>Returns an error that occurred filling the unprocessed events, if any</returns>
        public CLError GetPendingEvents(out List<KeyValuePair<FilePath, FileChange>> changeEvents)
        {
            throw new NotImplementedException("4");
            //ExternalSQLLocker.EnterReadLock();
            //try
            //{
            //    using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(this.indexDBLocation)))
            //    {
            //        // Create the output list
            //        changeEvents = new List<KeyValuePair<FilePath, FileChange>>();

            //        // Loop through all the events in the database after the last sync (if any)
            //        foreach (Event currentChange in
            //            SqlAccessor<Event>
            //                .SelectResultSet(indexDB,
            //                    "SELECT " +
            //                    SqlAccessor<Event>.GetSelectColumns() + ", " +
            //                    SqlAccessor<FileSystemObject>.GetSelectColumns(FileSystemObject.Name) +
            //                    "FROM [Events] " +
            //                    "INNER JOIN [FileSystemObjects] ON [Events].[EventId] = [FileSystemObjects].[EventId] " +
            //                    "WHERE [FileSystemObjects].[SyncCounter] IS NULL " +
            //                    "ORDER BY [Events].[EventId]",
            //                    new string[]
            //                    {
            //                        FileSystemObject.Name
            //                    }))
            //        {
            //            // For each event since the last sync (if any), add to the output dictionary
            //            changeEvents.Add(new KeyValuePair<FilePath, FileChange>(currentChange.FileSystemObject.Path,
            //                new FileChange()
            //                {
            //                    NewPath = currentChange.FileSystemObject.Path,
            //                    OldPath = currentChange.PreviousPath,
            //                    Type = changeEnums[currentChange.FileChangeTypeEnumId],
            //                    Metadata = new FileMetadata()
            //                    {
            //                        // TODO: add server id
            //                        HashableProperties = new FileMetadataHashableProperties(currentChange.FileSystemObject.IsFolder,
            //                            currentChange.FileSystemObject.LastTime,
            //                            currentChange.FileSystemObject.CreationTime,
            //                            currentChange.FileSystemObject.Size),
            //                        Revision = currentChange.FileSystemObject.Revision,
            //                        StorageKey = currentChange.FileSystemObject.StorageKey,
            //                        LinkTargetPath = currentChange.FileSystemObject.TargetPath
            //                    },
            //                    Direction = (currentChange.SyncFrom ? SyncDirection.From : SyncDirection.To)
            //                }));
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    changeEvents = Helpers.DefaultForType<List<KeyValuePair<FilePath, FileChange>>>();
            //    return ex;
            //}
            //finally
            //{
            //    ExternalSQLLocker.ExitReadLock();
            //}
            //return null;
        }

        /// <summary>
        /// Adds an unprocessed change since the last sync as a new event to the database,
        /// EventId property of the input event is set after database update
        /// </summary>
        /// <param name="newEvents">Change to add</param>
        /// <returns>Returns error that occurred when adding the event to database, if any</returns>
        public CLError AddEvents(IEnumerable<FileChange> newEvents, bool alreadyObtainedLock = false)
        {
            throw new NotImplementedException("5");
            //if (!alreadyObtainedLock)
            //{
            //    ExternalSQLLocker.EnterReadLock();
            //}
            //try
            //{
            //    // Ensure input parameter is set
            //    if (newEvents == null)
            //    {
            //        throw new NullReferenceException("newEvents cannot be null");
            //    }
            //    if (newEvents.Any(newEvent => newEvent.Metadata == null))
            //    {
            //        throw new NullReferenceException("The Metadata property of every newEvent cannot be null");
            //    }

            //    List<Event> eventsToAdd = new List<Event>();
            //    Guid eventGroup = Guid.NewGuid();
            //    int eventCounter = 0;
            //    Dictionary<int, KeyValuePair<FileChange, GenericHolder<long>>> orderToChange = new Dictionary<int, KeyValuePair<FileChange, GenericHolder<long>>>();

            //    // If change is marked for adding to SQL,
            //    // then process database addition
            //    foreach (FileChange newEvent in newEvents.Where(newEvent => newEvent.EventId == 0 || !newEvent.DoNotAddToSQLIndex))
            //    {
            //        string newPathString = newEvent.NewPath.ToString();

            //        eventCounter++;
            //        orderToChange.Add(eventCounter, new KeyValuePair<FileChange, GenericHolder<long>>(newEvent, new GenericHolder<long>()));

            //        // Define the new event to add for the unprocessed change
            //        eventsToAdd.Add(new Event()
            //        {
            //            FileChangeTypeCategoryId = changeCategoryId,
            //            FileChangeTypeEnumId = changeEnumsBackward[newEvent.Type],
            //            FileSystemObject = new FileSystemObject()
            //            {
            //                // TODO: add server id
            //                CreationTime = newEvent.Metadata.HashableProperties.CreationTime.Ticks == FileConstants.InvalidUtcTimeTicks
            //                    ? (Nullable<DateTime>)null
            //                    : newEvent.Metadata.HashableProperties.CreationTime,
            //                IsFolder = newEvent.Metadata.HashableProperties.IsFolder,
            //                LastTime = newEvent.Metadata.HashableProperties.LastTime.Ticks == FileConstants.InvalidUtcTimeTicks
            //                    ? (Nullable<DateTime>)null
            //                    : newEvent.Metadata.HashableProperties.LastTime,
            //                Path = newPathString,
            //                Size = newEvent.Metadata.HashableProperties.Size,
            //                Revision = newEvent.Metadata.Revision,
            //                StorageKey = newEvent.Metadata.StorageKey,
            //                TargetPath = (newEvent.Metadata.LinkTargetPath == null ? null : newEvent.Metadata.LinkTargetPath.ToString()),
            //                SyncCounter = null,
            //                ServerLinked = false,

            //                // SQL CE does not support computed columns, so no "AS CHECKSUM(Path)"
            //                PathChecksum = StringCRC.Crc(newPathString)
            //            },
            //            PreviousPath = (newEvent.OldPath == null
            //                ? null
            //                : newEvent.OldPath.ToString()),
            //            GroupId = eventGroup,
            //            GroupOrder = eventCounter,
            //            SyncFrom = (newEvent.Direction == SyncDirection.From)
            //        });
            //    }

            //    if (eventsToAdd.Count > 0)
            //    {
            //        using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(this.indexDBLocation)))
            //        {
            //            SqlAccessor<Event>.InsertRows(indexDB, eventsToAdd);
            //        }

            //        using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(this.indexDBLocation)))
            //        {
            //            Dictionary<int, long> groupOrderToId = new Dictionary<int, long>();
            //            foreach (Event createdEvent in SqlAccessor<Event>.SelectResultSet(indexDB,
            //                "SELECT * FROM [Events] WHERE [Events].[GroupId] = '" + eventGroup.ToString() + "'"))
            //            {
            //                groupOrderToId.Add((int)createdEvent.GroupOrder, createdEvent.EventId);
            //            }

            //            Func<Event, FileSystemObject> setIdAndGrabObject = currentEvent =>
            //                {
            //                    currentEvent.FileSystemObject.EventId = orderToChange[(int)currentEvent.GroupOrder].Value.Value = groupOrderToId[(int)currentEvent.GroupOrder];
            //                    return currentEvent.FileSystemObject;
            //                };

            //            SqlAccessor<FileSystemObject>.InsertRows(indexDB, eventsToAdd.Select(setIdAndGrabObject));
            //        }

            //        foreach (KeyValuePair<FileChange, GenericHolder<long>> currentAddedEvent in orderToChange.Values)
            //        {
            //            currentAddedEvent.Key.EventId = currentAddedEvent.Value.Value;
            //            MessageEvents.ApplyFileChangeMergeToChangeState(this, new FileChangeMerge(currentAddedEvent.Key, null));   // Message to invoke BadgeNet.IconOverlay.QueueNewEventBadge(currentAddedEvent.Key, null)
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    return ex;
            //}
            //finally
            //{
            //    if (!alreadyObtainedLock)
            //    {
            //        ExternalSQLLocker.ExitReadLock();
            //    }
            //}
            //return null;
        }

        /// <summary>
        /// Removes a single event by its id
        /// </summary>
        /// <param name="eventId">Id of event to remove</param>
        /// <returns>Returns an error in removing the event, if any</returns>
        public CLError RemoveEventById(long eventId)
        {
            throw new NotImplementedException("6");
            //ExternalSQLLocker.EnterReadLock();
            //try
            //{
            //    using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(this.indexDBLocation)))
            //    {
            //        // Find the existing object for the given id
            //        FileSystemObject toDelete = SqlAccessor<FileSystemObject>.SelectResultSet(indexDB,
            //            "SELECT TOP 1 * FROM [FileSystemObjects] WHERE [FileSystemObjects].[EventId] = " + eventId.ToString())
            //            .SingleOrDefault();

            //        Func<Exception> notFoundException = () => new Exception("Event not found to delete");

            //        // Throw exception if an existing event does not exist
            //        if (toDelete == null)
            //        {
            //            throw notFoundException();
            //        }

            //        // Remove the found event from the database
            //        if (!SqlAccessor<FileSystemObject>.DeleteRow(indexDB,
            //                toDelete)
            //            || !SqlAccessor<Event>.DeleteRow(indexDB,
            //                new Event()
            //                {
            //                    EventId = eventId
            //                }))
            //        {
            //            throw notFoundException();
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    return ex;
            //}
            //finally
            //{
            //    ExternalSQLLocker.ExitReadLock();
            //}
            //return null;
        }

        /// <summary>
        /// Removes a collection of events by their ids
        /// </summary>
        /// <param name="eventIds">Ids of events to remove</param>
        /// <returns>Returns an error in removing events, if any</returns>
        public CLError RemoveEventsByIds(IEnumerable<long> eventIds, bool alreadyObtainedLock = false)
        {
            throw new NotImplementedException("7");
            //CLError notFoundErrors = null;

            //if (!alreadyObtainedLock)
            //{
            //    ExternalSQLLocker.EnterReadLock();
            //}
            //try
            //{
            //    // copy event id collection to array, defaulting to an empty array
            //    long[] eventIdsArray = (eventIds == null
            //        ? new long[0]
            //        : eventIds.ToArray());

            //    if (eventIdsArray.Length > 0)
            //    {
            //        using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(this.indexDBLocation)))
            //        {
            //            // Create list to copy event ids from database objects,
            //            // used to ensure all event ids to be deleted were found
            //            List<long> orderedDBIds = new List<long>();
            //            // Grab all objects with ids in the specified range
            //            FileSystemObject[] deleteObjects = SqlAccessor<FileSystemObject>.SelectResultSet(indexDB,
            //                "SELECT * " +
            //                "FROM [FileSystemObjects] " +
            //                "WHERE [FileSystemObjects].[EventId] IN (" + string.Join(", ", eventIdsArray.Select(currentId => currentId.ToString()).ToArray()) + ") ")
            //                .ToArray();

            //            IEnumerable<int> unableToFindIndexes;
            //            SqlAccessor<FileSystemObject>.DeleteRows(indexDB,
            //                deleteObjects,
            //                out unableToFindIndexes);
                        
            //            // Check all event ids intended for delete and make sure they were actually deleted,
            //            // otherwise create exception
            //            if (unableToFindIndexes != null)
            //            {
            //                foreach (int notDeletedIndex in unableToFindIndexes)
            //                {
            //                    notFoundErrors += new Exception("Event with id " + eventIdsArray[notDeletedIndex].ToString() + " not found to delete");
            //                }
            //            }

            //            unableToFindIndexes = new HashSet<int>(unableToFindIndexes ?? Enumerable.Empty<int>());

            //            SqlAccessor<Event>.DeleteRows(indexDB,
            //                deleteObjects.Where((deleteObject, objectIndex) => !((HashSet<int>)unableToFindIndexes).Contains(objectIndex))
            //                    .Select(deleteObject => new Event()
            //                    {
            //                        EventId = (long)deleteObject.EventId
            //                    }), out unableToFindIndexes);

            //            if (unableToFindIndexes != null)
            //            {
            //                foreach (int notDeletedIndex in unableToFindIndexes)
            //                {
            //                    notFoundErrors += new Exception("Event with id " + eventIdsArray[notDeletedIndex].ToString() + " not found to delete");
            //                }
            //            }
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    notFoundErrors += ex;
            //}
            //finally
            //{
            //    if (!alreadyObtainedLock)
            //    {
            //        ExternalSQLLocker.ExitReadLock();
            //    }
            //}
            //return notFoundErrors;
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
        public CLError RecordCompletedSync(string syncId, IEnumerable<long> syncedEventIds, out long syncCounter, FilePath newRootPath = null)
        {
            return RecordCompletedSync(syncId, syncedEventIds, out syncCounter, newRootPath == null ? null : newRootPath.ToString());
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
            throw new NotImplementedException("8");
            //// Default the output sync counter
            //syncCounter = Helpers.DefaultForType<long>();
            //try
            //{
            //    // Copy event ids completed in sync to array, defaulting to an empty array
            //    long[] syncedEventIdsEnumerated = (syncedEventIds == null
            //        ? new long[0]
            //        : syncedEventIds.OrderBy(currentEventId => currentEventId).ToArray());

            //    ExternalSQLLocker.EnterWriteLock();
            //    try
            //    {
            //        using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(this.indexDBLocation)))
            //        {
            //            indexDB.Open();

            //            using (SqlCeTransaction indexTransaction = indexDB.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
            //            {
            //                try
            //                {
            //                    // Retrieve last sync if it exists
            //                    SqlSync lastSync = SqlAccessor<SqlSync>.SelectResultSet(indexDB,
            //                        "SELECT TOP 1 * FROM [Syncs] ORDER BY [Syncs].[SyncCounter] DESC",
            //                        transaction: indexTransaction)
            //                        .SingleOrDefault();
            //                    // Store last sync counter value or null for no last sync
            //                    Nullable<long> lastSyncCounter = (lastSync == null
            //                        ? (Nullable<long>)null
            //                        : lastSync.SyncCounter);
            //                    // Default root path from last sync if it was not passed in
            //                    newRootPath = string.IsNullOrEmpty(newRootPath)
            //                        ? (lastSync == null ? null : lastSync.RootPath)
            //                        : newRootPath;

            //                    if (string.IsNullOrEmpty(newRootPath))
            //                    {
            //                        throw new Exception("Path cannot be found for sync root");
            //                    }

            //                    FilePath previousRoot = (lastSync == null ? null : lastSync.RootPath);
            //                    FilePath newRoot = newRootPath;

            //                    if (string.IsNullOrWhiteSpace(syncId))
            //                    {
            //                        if (lastSync != null)
            //                        {
            //                            syncId = lastSync.SyncId;
            //                        }
            //                        else
            //                        {
            //                            throw new Exception("Could not find a sync id");
            //                        }
            //                    }

            //                    bool syncStatesNeedRemap = previousRoot != null
            //                        && !FilePathComparer.Instance.Equals(previousRoot, newRoot);

            //                    // Create the new sync database object
            //                    SqlSync newSync = new SqlSync()
            //                    {
            //                        SyncId = syncId,
            //                        RootPath = newRootPath
            //                    };

            //                    // Add the new sync to the database and store the new counter
            //                    syncCounter = SqlAccessor<SqlSync>.InsertRow<long>(indexDB, newSync, transaction: indexTransaction);

            //                    // Create the dictionary for new sync states, returning an error if it occurred
            //                    FilePathDictionary<Tuple<long, Nullable<long>, FileMetadata>> newSyncStates;
            //                    CLError newSyncStatesError = FilePathDictionary<Tuple<long, Nullable<long>, FileMetadata>>.CreateAndInitialize(newRootPath,
            //                        out newSyncStates);
            //                    if (newSyncStatesError != null)
            //                    {
            //                        return newSyncStatesError;
            //                    }

            //                    // Create a dictionary to store remapped server paths in case they exist
            //                    Dictionary<string, string> serverRemappedPaths = new Dictionary<string, string>();

            //                    Dictionary<long, KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>> mappedSyncStates = null;

            //                    // If there was a previous sync, pull the previous sync states to modify
            //                    if (lastSyncCounter != null)
            //                    {
            //                        mappedSyncStates = new Dictionary<long, KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>>();

            //                        // Loop through all sync states for the last sync
            //                        foreach (FileSystemObject currentSyncState in SqlAccessor<FileSystemObject>
            //                            .SelectResultSet(indexDB,
            //                                "SELECT * FROM [FileSystemObjects] WHERE [FileSystemObjects].[SyncCounter] = " + ((long)lastSyncCounter).ToString(),
            //                            transaction: indexTransaction))
            //                        {
            //                            if (mappedSyncStates.ContainsKey(currentSyncState.FileSystemObjectId))
            //                            {
            //                                if (currentSyncState.ServerLinked)
            //                                {
            //                                    mappedSyncStates[currentSyncState.FileSystemObjectId].Value.Value = currentSyncState;
            //                                }
            //                                else
            //                                {
            //                                    mappedSyncStates[currentSyncState.FileSystemObjectId].Key.Value = currentSyncState;
            //                                }
            //                            }
            //                            else if (currentSyncState.ServerLinked)
            //                            {
            //                                mappedSyncStates.Add(currentSyncState.FileSystemObjectId,
            //                                    new KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>(
            //                                        new GenericHolder<FileSystemObject>(),
            //                                        new GenericHolder<FileSystemObject>(currentSyncState)));
            //                            }
            //                            else
            //                            {
            //                                mappedSyncStates.Add(currentSyncState.FileSystemObjectId,
            //                                    new KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>(
            //                                        new GenericHolder<FileSystemObject>(currentSyncState),
            //                                        new GenericHolder<FileSystemObject>()));
            //                            }
            //                        }

            //                        // Loop through previous sync states
            //                        foreach (KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>> currentState in mappedSyncStates.Values)
            //                        {
            //                            string localPath = currentState.Key.Value.Path;
            //                            string serverPath = (currentState.Value.Value == null ? null : currentState.Value.Value.Path);

            //                            if (syncStatesNeedRemap)
            //                            {
            //                                FilePath originalLocalPath = localPath;
            //                                FilePath originalServerPath = serverPath;

            //                                FilePath overlappingLocal = originalLocalPath.FindOverlappingPath(previousRoot);
            //                                if (overlappingLocal != null)
            //                                {
            //                                    FilePath renamedOverlapChild = originalLocalPath;
            //                                    FilePath renamedOverlap = renamedOverlapChild.Parent;

            //                                    while (renamedOverlap != null)
            //                                    {
            //                                        if (FilePathComparer.Instance.Equals(renamedOverlap, previousRoot))
            //                                        {
            //                                            renamedOverlapChild.Parent = newRoot;
            //                                            localPath = originalLocalPath.ToString();
            //                                            break;
            //                                        }

            //                                        renamedOverlapChild = renamedOverlap;
            //                                        renamedOverlap = renamedOverlap.Parent;
            //                                    }
            //                                }

            //                                if (originalServerPath != null)
            //                                {
            //                                    FilePath overlappingServer = originalServerPath.FindOverlappingPath(previousRoot);
            //                                    if (overlappingServer != null)
            //                                    {
            //                                        FilePath renamedOverlapChild = overlappingServer;
            //                                        FilePath renamedOverlap = renamedOverlapChild.Parent;

            //                                        while (renamedOverlap != null)
            //                                        {
            //                                            if (FilePathComparer.Instance.Equals(renamedOverlap, previousRoot))
            //                                            {
            //                                                renamedOverlapChild.Parent = newRoot;
            //                                                serverPath = overlappingServer.ToString();
            //                                                break;
            //                                            }

            //                                            renamedOverlapChild = renamedOverlap;
            //                                            renamedOverlap = renamedOverlap.Parent;
            //                                        }
            //                                    }
            //                                }
            //                            }

            //                            // Check if previous syncstate had a server-remapped path to store
            //                            if (currentState.Value.Value != null)
            //                            {
            //                                serverRemappedPaths.Add(localPath,
            //                                    serverPath);
            //                            }

            //                            // Add the previous sync state to the dictionary as the baseline before changes
            //                            newSyncStates[localPath] = new Tuple<long, Nullable<long>, FileMetadata>(currentState.Key.Value.FileSystemObjectId,
            //                                currentState.Key.Value.EventId,
            //                                new FileMetadata()
            //                                {
            //                                    // TODO: add server id
            //                                    HashableProperties = new FileMetadataHashableProperties(currentState.Key.Value.IsFolder,
            //                                        currentState.Key.Value.LastTime,
            //                                        currentState.Key.Value.CreationTime,
            //                                        currentState.Key.Value.Size),
            //                                    LinkTargetPath = currentState.Key.Value.TargetPath,
            //                                    Revision = currentState.Key.Value.Revision,
            //                                    StorageKey = currentState.Key.Value.StorageKey
            //                                });
            //                        }
            //                    }

            //                    // Grab all events from the database since the previous sync, ordering by id to ensure correct processing logic
            //                    Event[] existingEvents = SqlAccessor<Event>.SelectResultSet(indexDB,
            //                            "SELECT " +
            //                            SqlAccessor<Event>.GetSelectColumns() + ", " +
            //                            SqlAccessor<FileSystemObject>.GetSelectColumns(FileSystemObject.Name) + " " +
            //                            "FROM [Events] " +
            //                            "INNER JOIN [FileSystemObjects] ON [Events].[EventId] = [FileSystemObjects].[EventId] " +
            //                            "WHERE [FileSystemObjects].[SyncCounter] IS NULL " +
            //                            "ORDER BY [Events].[EventId]",
            //                            new string[]
            //                            {
            //                                FileSystemObject.Name
            //                            },
            //                            indexTransaction)
            //                        .ToArray();

            //                    Action<PathState, FilePath> setBadge = (badgeType, badgePath) =>
            //                    {
            //                        MessageEvents.QueueSetBadge(this, new SetBadge(badgeType, badgePath));   // Message to invoke BadgeNet.IconOverlay.QueueSetBadge(badgeType, badgePath);

            //                    };

            //                    List<Event> eventsToUpdate = new List<Event>();
            //                    List<FileSystemObject> objectsToUpdate = new List<FileSystemObject>();
            //                    List<FileSystemObject> objectsToMoveToLastSync = new List<FileSystemObject>();

            //                    // Loop through existing events to process into the new sync states
            //                    foreach (Event previousEvent in existingEvents)
            //                    {
            //                        string newPath = previousEvent.FileSystemObject.Path;
            //                        string oldPath = previousEvent.PreviousPath;

            //                        if (syncStatesNeedRemap)
            //                        {
            //                            FilePath originalNewPath = newPath;
            //                            FilePath originalOldPath = oldPath;

            //                            FilePath overlappingLocal = originalNewPath.FindOverlappingPath(previousRoot);
            //                            if (overlappingLocal != null)
            //                            {
            //                                FilePath renamedOverlapChild = originalNewPath;
            //                                FilePath renamedOverlap = renamedOverlapChild.Parent;

            //                                while (renamedOverlap != null)
            //                                {
            //                                    if (FilePathComparer.Instance.Equals(renamedOverlap, previousRoot))
            //                                    {
            //                                        renamedOverlapChild.Parent = newRoot;
            //                                        newPath = originalNewPath.ToString();
            //                                        break;
            //                                    }

            //                                    renamedOverlapChild = renamedOverlap;
            //                                    renamedOverlap = renamedOverlap.Parent;
            //                                }
            //                            }

            //                            if (originalOldPath != null)
            //                            {
            //                                FilePath overlappingServer = originalOldPath.FindOverlappingPath(previousRoot);
            //                                if (overlappingServer != null)
            //                                {
            //                                    FilePath renamedOverlapChild = overlappingServer;
            //                                    FilePath renamedOverlap = renamedOverlapChild.Parent;

            //                                    while (renamedOverlap != null)
            //                                    {
            //                                        if (FilePathComparer.Instance.Equals(renamedOverlap, previousRoot))
            //                                        {
            //                                            renamedOverlapChild.Parent = newRoot;
            //                                            oldPath = overlappingServer.ToString();
            //                                            break;
            //                                        }

            //                                        renamedOverlapChild = renamedOverlap;
            //                                        renamedOverlap = renamedOverlap.Parent;
            //                                    }
            //                                }
            //                            }
            //                        }

            //                        // If the current database event is in the list of events that are completed,
            //                        // the syncstates have to be modified appropriately to include the change
            //                        if (Array.BinarySearch(syncedEventIdsEnumerated, previousEvent.EventId) >= 0)
            //                        {
            //                            switch (changeEnums[previousEvent.FileChangeTypeEnumId])
            //                            {
            //                                case FileChangeType.Created:
            //                                    Tuple<long, Nullable<long>, FileMetadata> previousCreatedState;
            //                                    KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>> previousCreatedObjects;
            //                                    if (lastSyncCounter != null
            //                                        && newSyncStates.TryGetValue(newPath, out previousCreatedState)
            //                                        && previousCreatedState.Item1 != previousEvent.FileSystemObject.FileSystemObjectId
            //                                        && mappedSyncStates.TryGetValue(previousCreatedState.Item1, out previousCreatedObjects))
            //                                    {
            //                                        if (previousCreatedObjects.Key.Value != null)
            //                                        {
            //                                            objectsToMoveToLastSync.Add(previousCreatedObjects.Key.Value);
            //                                        }
            //                                        if (previousCreatedObjects.Value.Value != null)
            //                                        {
            //                                            objectsToMoveToLastSync.Add(previousCreatedObjects.Value.Value);
            //                                        }
            //                                    }

            //                                    newSyncStates[newPath] = new Tuple<long, Nullable<long>, FileMetadata>(previousEvent.FileSystemObject.FileSystemObjectId,
            //                                        previousEvent.EventId,
            //                                        new FileMetadata()
            //                                        {
            //                                            // TODO: add server id
            //                                            HashableProperties = new FileMetadataHashableProperties(previousEvent.FileSystemObject.IsFolder,
            //                                                previousEvent.FileSystemObject.LastTime,
            //                                                previousEvent.FileSystemObject.CreationTime,
            //                                                previousEvent.FileSystemObject.Size),
            //                                            LinkTargetPath = previousEvent.FileSystemObject.TargetPath,
            //                                            Revision = previousEvent.FileSystemObject.Revision,
            //                                            StorageKey = previousEvent.FileSystemObject.StorageKey
            //                                        });

            //                                    if (!existingEvents.Any(existingEvent => Array.BinarySearch(syncedEventIdsEnumerated, existingEvent.EventId) < 0
            //                                        && existingEvent.FileSystemObject.Path == newPath.ToString()))
            //                                    {
            //                                        setBadge(PathState.Synced, newPath);
            //                                    }
            //                                    break;
            //                                case FileChangeType.Deleted:
            //                                    newSyncStates.Remove(newPath);

            //                                    if (previousEvent.SyncFrom)
            //                                    {
            //                                        bool isDeleted;
            //                                        MessageEvents.DeleteBadgePath(this, new DeleteBadgePath(newPath), out isDeleted);   // Message to invoke BadgeNet.IconOverlay.DeleteBadgePath(newPath, out isDeleted);
            //                                    }

            //                                    // If any of the previous pending events from the database are both not in the list of completed events and match the current completed event's path,
            //                                    // then badge that path as syncing, because there is at least one remaining pended event.
            //                                    if (existingEvents.Any(existingEvent => Array.BinarySearch(syncedEventIdsEnumerated, existingEvent.EventId) < 0
            //                                        && existingEvent.FileSystemObject.Path == newPath.ToString()))
            //                                    {
            //                                        setBadge(PathState.Syncing, newPath);
            //                                    }
            //                                    else if (!previousEvent.SyncFrom)
            //                                    {
            //                                        setBadge(PathState.Synced, newPath);
            //                                    }
            //                                    break;
            //                                case FileChangeType.Modified:
            //                                    Tuple<long, Nullable<long>, FileMetadata> previousModifiedState;
            //                                    KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>> previousModifiedObjects;
            //                                    if (lastSyncCounter != null
            //                                        && newSyncStates.TryGetValue(newPath, out previousModifiedState)
            //                                        && previousModifiedState.Item1 != previousEvent.FileSystemObject.FileSystemObjectId
            //                                        && mappedSyncStates.TryGetValue(previousModifiedState.Item1, out previousModifiedObjects))
            //                                    {
            //                                        if (previousModifiedObjects.Key.Value != null)
            //                                        {
            //                                            objectsToMoveToLastSync.Add(previousModifiedObjects.Key.Value);
            //                                        }
            //                                        if (previousModifiedObjects.Value.Value != null)
            //                                        {
            //                                            objectsToMoveToLastSync.Add(previousModifiedObjects.Value.Value);
            //                                        }
            //                                    }

            //                                    newSyncStates[newPath] =
            //                                        new Tuple<long, Nullable<long>, FileMetadata>(previousEvent.FileSystemObject.FileSystemObjectId,
            //                                            previousEvent.EventId,
            //                                            new FileMetadata()
            //                                            {
            //                                                // TODO: add server id
            //                                                HashableProperties = new FileMetadataHashableProperties(previousEvent.FileSystemObject.IsFolder,
            //                                                    previousEvent.FileSystemObject.LastTime,
            //                                                    previousEvent.FileSystemObject.CreationTime,
            //                                                    previousEvent.FileSystemObject.Size),
            //                                                LinkTargetPath = previousEvent.FileSystemObject.TargetPath,
            //                                                Revision = previousEvent.FileSystemObject.Revision,
            //                                                StorageKey = previousEvent.FileSystemObject.StorageKey
            //                                            });

            //                                    if (!existingEvents.Any(existingEvent => Array.BinarySearch(syncedEventIdsEnumerated, existingEvent.EventId) < 0
            //                                        && existingEvent.FileSystemObject.Path == newPath.ToString()))
            //                                    {
            //                                        setBadge(PathState.Synced, newPath);
            //                                    }
            //                                    break;
            //                                case FileChangeType.Renamed:
            //                                    Tuple<long, Nullable<long>, FileMetadata> previousNewPathState;
            //                                    Tuple<long, Nullable<long>, FileMetadata> previousOldPathState;
            //                                    if (newSyncStates.TryGetValue(newPath, out previousNewPathState))
            //                                    {
            //                                        if (newSyncStates.ContainsKey(oldPath))
            //                                        {
            //                                            newSyncStates.Remove(oldPath);
            //                                        }

            //                                        KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>> previousNewPathObjects;

            //                                        if (lastSyncCounter != null
            //                                            && previousNewPathState.Item1 != previousEvent.FileSystemObject.FileSystemObjectId
            //                                            && mappedSyncStates.TryGetValue(previousNewPathState.Item1, out previousNewPathObjects))
            //                                        {
            //                                            if (previousNewPathObjects.Key.Value != null)
            //                                            {
            //                                                objectsToMoveToLastSync.Add(previousNewPathObjects.Key.Value);
            //                                            }
            //                                            if (previousNewPathObjects.Value.Value != null)
            //                                            {
            //                                                objectsToMoveToLastSync.Add(previousNewPathObjects.Value.Value);
            //                                            }
            //                                        }
            //                                    }
            //                                    else if (newSyncStates.TryGetValue(oldPath, out previousOldPathState))
            //                                    {
            //                                        newSyncStates.Rename(oldPath, newPath);

            //                                        KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>> previousOldPathObjects;

            //                                        if (lastSyncCounter != null
            //                                            && previousOldPathState.Item1 != previousEvent.FileSystemObject.FileSystemObjectId
            //                                            && mappedSyncStates.TryGetValue(previousOldPathState.Item1, out previousOldPathObjects))
            //                                        {
            //                                            if (previousOldPathObjects.Key.Value != null)
            //                                            {
            //                                                objectsToMoveToLastSync.Add(previousOldPathObjects.Key.Value);
            //                                            }
            //                                            if (previousOldPathObjects.Value.Value != null)
            //                                            {
            //                                                objectsToMoveToLastSync.Add(previousOldPathObjects.Value.Value);
            //                                            }
            //                                        }
            //                                    }

            //                                    newSyncStates[newPath] =
            //                                        new Tuple<long, Nullable<long>, FileMetadata>(previousEvent.FileSystemObject.FileSystemObjectId,
            //                                            previousEvent.EventId,
            //                                            new FileMetadata()
            //                                            {
            //                                                // TODO: add server id
            //                                                HashableProperties = new FileMetadataHashableProperties(previousEvent.FileSystemObject.IsFolder,
            //                                                    previousEvent.FileSystemObject.LastTime,
            //                                                    previousEvent.FileSystemObject.CreationTime,
            //                                                    previousEvent.FileSystemObject.Size),
            //                                                LinkTargetPath = previousEvent.FileSystemObject.TargetPath,
            //                                                Revision = previousEvent.FileSystemObject.Revision,
            //                                                StorageKey = previousEvent.FileSystemObject.StorageKey
            //                                            });

            //                                    if (previousEvent.SyncFrom)
            //                                    {
            //                                        MessageEvents.RenameBadgePath(this, new RenameBadgePath(oldPath, newPath));   // Message to invoke BadgeNet.IconOverlay.RenameBadgePath(oldPath, newPath);
            //                                    }

            //                                    // If there are no other events pending at this same path, mark the renamed path as synced.
            //                                    if (!existingEvents.Any(existingEvent => Array.BinarySearch(syncedEventIdsEnumerated, existingEvent.EventId) < 0
            //                                        && existingEvent.FileSystemObject.Path == newPath.ToString()))
            //                                    {
            //                                        setBadge(PathState.Synced, newPath);
            //                                    }
            //                                    break;
            //                            }
            //                        }
            //                        // Else if the previous database event is not in the list of completed events,
            //                        // The event will get moved to after the current sync so it will be processed later
            //                        else
            //                        {
            //                            if (syncStatesNeedRemap)
            //                            {
            //                                if (!FilePathComparer.Instance.Equals(previousEvent.FileSystemObject.Path, newPath))
            //                                {
            //                                    previousEvent.FileSystemObject.Path = newPath;

            //                                    objectsToUpdate.Add(previousEvent.FileSystemObject);
            //                                }
            //                                previousEvent.PreviousPath = oldPath;

            //                                eventsToUpdate.Add(previousEvent);
            //                            }
            //                        }
            //                    }

            //                    //// what was this for?
            //                    //bool atLeastOneServerLinked = false;

            //                    // Loop through modified set of sync states (including new changes) and add the matching database objects
            //                    foreach (KeyValuePair<FilePath, Tuple<long, Nullable<long>, FileMetadata>> newSyncState in newSyncStates)
            //                    {
            //                        string newPathString = newSyncState.Key.ToString();

            //                        // Add the file/folder object for the current sync state
            //                        objectsToUpdate.Add(new FileSystemObject()
            //                        {
            //                            // TODO: add server id
            //                            CreationTime = (newSyncState.Value.Item3.HashableProperties.CreationTime.Ticks == FileConstants.InvalidUtcTimeTicks
            //                                ? (Nullable<DateTime>)null
            //                                : newSyncState.Value.Item3.HashableProperties.CreationTime),
            //                            IsFolder = newSyncState.Value.Item3.HashableProperties.IsFolder,
            //                            LastTime = (newSyncState.Value.Item3.HashableProperties.LastTime.Ticks == FileConstants.InvalidUtcTimeTicks
            //                                ? (Nullable<DateTime>)null
            //                                : newSyncState.Value.Item3.HashableProperties.LastTime),
            //                            Path = newPathString,
            //                            Size = newSyncState.Value.Item3.HashableProperties.Size,
            //                            TargetPath = (newSyncState.Value.Item3.LinkTargetPath == null ? null : newSyncState.Value.Item3.LinkTargetPath.ToString()),
            //                            Revision = newSyncState.Value.Item3.Revision,
            //                            StorageKey = newSyncState.Value.Item3.StorageKey,
            //                            SyncCounter = syncCounter,
            //                            ServerLinked = false,
            //                            FileSystemObjectId = newSyncState.Value.Item1,
            //                            EventId = newSyncState.Value.Item2,

            //                            // SQL CE does not support computed columns, so no "AS CHECKSUM(Path)"
            //                            PathChecksum = StringCRC.Crc(newPathString)
            //                        });

            //                        // If the file/folder path is remapped on the server, add the file/folder object for the server-mapped state
            //                        if (serverRemappedPaths.ContainsKey(newPathString))
            //                        {
            //                            //// what was this for?
            //                            //atLeastOneServerLinked = true;

            //                            objectsToUpdate.Add(new FileSystemObject()
            //                            {
            //                                // TODO: add server id
            //                                CreationTime = (newSyncState.Value.Item3.HashableProperties.CreationTime.Ticks == FileConstants.InvalidUtcTimeTicks
            //                                    ? (Nullable<DateTime>)null
            //                                    : newSyncState.Value.Item3.HashableProperties.CreationTime),
            //                                IsFolder = newSyncState.Value.Item3.HashableProperties.IsFolder,
            //                                LastTime = (newSyncState.Value.Item3.HashableProperties.LastTime.Ticks == FileConstants.InvalidUtcTimeTicks
            //                                    ? (Nullable<DateTime>)null
            //                                    : newSyncState.Value.Item3.HashableProperties.LastTime),
            //                                Path = serverRemappedPaths[newPathString],
            //                                Size = newSyncState.Value.Item3.HashableProperties.Size,
            //                                TargetPath = (newSyncState.Value.Item3.LinkTargetPath == null ? null : newSyncState.Value.Item3.LinkTargetPath.ToString()),
            //                                Revision = newSyncState.Value.Item3.Revision,
            //                                StorageKey = newSyncState.Value.Item3.StorageKey,
            //                                SyncCounter = syncCounter,
            //                                ServerLinked = true,
            //                                FileSystemObjectId = newSyncState.Value.Item1,
            //                                EventId = newSyncState.Value.Item2,

            //                                // SQL CE does not support computed columns, so no "AS CHECKSUM(Path)"
            //                                PathChecksum = StringCRC.Crc(serverRemappedPaths[newPathString])
            //                            });
            //                        }
            //                    }

            //                    // Define field that will be output for indexes not updated
            //                    IEnumerable<int> unableToFindIndexes;

            //                    // Update Events that were queued for modification
            //                    SqlAccessor<Event>.UpdateRows(indexDB,
            //                        eventsToUpdate,
            //                        out unableToFindIndexes,
            //                        transaction: indexTransaction);

            //                    // Update FileSystemObjects that were queued for modification
            //                    SqlAccessor<FileSystemObject>.UpdateRows(indexDB,
            //                        objectsToUpdate,
            //                        out unableToFindIndexes,
            //                        transaction: indexTransaction);

            //                    // If any FileSystemObjects were not found to update,
            //                    // then they need to be inserted
            //                    if (unableToFindIndexes != null
            //                        && unableToFindIndexes.Count() > 0)
            //                    {
            //                        // Insert new FileSystemObjects with IDENTITY_INSERT ON (will force server-linked sync state to have a matching identity to the non-server-linked sync state)
            //                        SqlAccessor<FileSystemObject>.InsertRows(indexDB,
            //                            unableToFindIndexes.Select(currentInsert => objectsToUpdate[currentInsert]),
            //                            true,
            //                            transaction: indexTransaction);
            //                    }

            //                    // need to check for any input syncedEventIds which do not correspond to any row which will be created/updated in the database and ensure they have a sync counter
            //                    HashSet<long> idsToUpdatedObjects = new HashSet<long>(objectsToUpdate
            //                        .Concat(objectsToMoveToLastSync)
            //                        .Where(updatedObject => updatedObject.EventId != null)
            //                        .Select(updatedObject => ((long)updatedObject.EventId)));
            //                    List<long> notFoundEventIds = new List<long>(syncedEventIds.Where(syncedEventId => !idsToUpdatedObjects.Contains(syncedEventId)));
            //                    if (notFoundEventIds.Count > 0)
            //                    {
            //                        objectsToMoveToLastSync.AddRange( // add not found, completed events to list which will update the database with sync counter moves
            //                            SqlAccessor<FileSystemObject>.SelectResultSet(indexDB,
            //                                "SELECT * FROM [FileSystemObjects] WHERE [FileSystemObjects].[EventId] IN (" +
            //                                    string.Join(",", notFoundEventIds) +
            //                                    ")")
            //                                .Select(((Func<FileSystemObject, FileSystemObject>)(needsLastCounter =>
            //                                {
            //                                    needsLastCounter.SyncCounter = lastSyncCounter; // put non-pending event which won't get updated in the database under the last sync (probably a deleted change)
            //                                    return needsLastCounter;
            //                                }))));
            //                    }

            //                    if (objectsToMoveToLastSync.Count > 0)
            //                    {
            //                        FileSystemObject[] needsMove = objectsToMoveToLastSync.Where(currentObjectToMove => lastSyncCounter == null
            //                            || currentObjectToMove.SyncCounter == lastSyncCounter).ToArray();

            //                        if (needsMove.Length > 0)
            //                        {
            //                            SqlAccessor<FileSystemObject>.UpdateRows(indexDB,
            //                                needsMove.Select(currentObjectToMove =>
            //                                        ((Func<FileSystemObject, Nullable<long>, FileSystemObject>)((toMove, lastCounter) =>
            //                                        {
            //                                            if (lastCounter == null)
            //                                            {
            //                                                toMove.SyncCounter = null;
            //                                                toMove.EventId = null;
            //                                            }
            //                                            else
            //                                            {
            //                                                toMove.SyncCounter = lastCounter;
            //                                            }
            //                                            return toMove;
            //                                        }))(currentObjectToMove, lastSyncCounter)),
            //                                    out unableToFindIndexes,
            //                                    transaction: indexTransaction);
            //                        }
            //                    }

            //                    indexTransaction.Commit(CommitMode.Immediate);
            //                }
            //                catch
            //                {
            //                    indexTransaction.Rollback();

            //                    throw;
            //                }
            //            }
            //        }

            //        // update the exposed last sync id upon sync completion
            //        LastSyncLocker.EnterWriteLock();
            //        try
            //        {
            //            this.LastSyncId = syncId;
            //        }
            //        finally
            //        {
            //            LastSyncLocker.ExitWriteLock();
            //        }
            //    }
            //    finally
            //    {
            //        ExternalSQLLocker.ExitWriteLock();
            //    }
            //}
            //catch (Exception ex)
            //{
            //    return ex;
            //}
            //return null;
        }

        /// <summary>
        /// ¡¡ Call this carefully, completely wipes index database (use when user deletes local repository or relinks) !!
        /// </summary>
        /// <returns></returns>
        public CLError WipeIndex(string newRootPath)
        {
            throw new NotImplementedException("9");
            //try
            //{
            //    ExternalSQLLocker.EnterWriteLock();
            //    try
            //    {
            //        using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(this.indexDBLocation)))
            //        {
            //            indexDB.Open();

            //            using (SqlCeTransaction indexTransaction = indexDB.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
            //            {
            //                // It does not matter functionally if there was an error deleting an event (it may be orphaned without causing a problem),
            //                // so we delete events outside of the main transaction which can be allowed to fail
            //                IEnumerable<Event> deleteEvents;
            //                    IEnumerable<int> unableToFindIndexes;

            //                try
            //                {
            //                    IEnumerable<FileSystemObject> pendingToDelete = SqlAccessor<FileSystemObject>.SelectResultSet(indexDB,
            //                        "SELECT " + SqlAccessor<FileSystemObject>.GetSelectColumns() + ", " +
            //                            SqlAccessor<Event>.GetSelectColumns("Event") + " " +
            //                            "FROM [FileSystemObjects] " +
            //                            "LEFT OUTER JOIN [Events] ON [FileSystemObjects].[EventId] = [Events].[EventId] " +
            //                            "WHERE  [FileSystemObjects].[SyncCounter] IS NULL ",
            //                        new string[] { "Event" },
            //                        indexTransaction).ToArray();

            //                    SqlAccessor<FileSystemObject>.DeleteRows(indexDB,
            //                        pendingToDelete,
            //                        out unableToFindIndexes,
            //                        transaction: indexTransaction);

            //                    deleteEvents = pendingToDelete.Where(toDelete => toDelete.EventId != null)
            //                        .Select(toDelete => toDelete.Event);

            //                    SqlAccessor<SqlSync>.InsertRow(indexDB,
            //                        new SqlSync()
            //                        {
            //                            SyncId = IdForEmptySync,
            //                            RootPath = newRootPath
            //                        },
            //                        transaction: indexTransaction);

            //                    indexTransaction.Commit(CommitMode.Immediate);
            //                }
            //                catch
            //                {
            //                    indexTransaction.Rollback();

            //                    throw;
            //                }

            //                SqlAccessor<Event>.DeleteRows(indexDB,
            //                    deleteEvents,
            //                    out unableToFindIndexes);
            //            }
            //        }
            //    }
            //    finally
            //    {
            //        ExternalSQLLocker.ExitWriteLock();
            //    }
            //}
            //catch (Exception ex)
            //{
            //    return ex;
            //}
            //return null;
        }
        private const string IdForEmptySync = "0";

        /// <summary>
        /// Method to merge event into database,
        /// used when events are modified or replaced with new events
        /// </summary>
        /// <param name="mergedEvent">Event with latest file or folder metadata, pass null to only delete the old event</param>
        /// <param name="eventToRemove">Previous event to set if an old event is being replaced in the process</param>
        /// <returns>Returns an error from merging the events, if any</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public CLError MergeEventsIntoDatabase(IEnumerable<FileChangeMerge> mergeToFroms, bool alreadyObtainedLock = false)
        {
            CLError toReturn = null;
            if (mergeToFroms != null)
            {
                try
                {
                    List<FileChange> toAdd = new List<FileChange>();
                    Dictionary<long, FileChange> toUpdate = new Dictionary<long, FileChange>();
                    HashSet<long> toDelete = new HashSet<long>();

                    foreach (FileChangeMerge currentMergeToFrom in mergeToFroms)
                    {
                        // Continue to next iteration if boolean set indicating not to add to SQL
                        if (currentMergeToFrom.MergeTo != null
                            && currentMergeToFrom.MergeTo.DoNotAddToSQLIndex
                            && currentMergeToFrom.MergeTo.EventId != 0)
                        {
                            MessageEvents.ApplyFileChangeMergeToChangeState(this, new FileChangeMerge(currentMergeToFrom.MergeTo, currentMergeToFrom.MergeFrom));   // Message to invoke BadgeNet.IconOverlay.QueueNewEventBadge(currentMergeToFrom.MergeTo, currentMergeToFrom.MergeFrom)
                            continue;
                        }

                        // Ensure input variables have proper references set
                        if (currentMergeToFrom.MergeTo == null)
                        {
                            // null merge events are only valid if there is an oldEvent to remove
                            if (currentMergeToFrom.MergeFrom == null)
                            {
                                throw new NullReferenceException("currentMergeToFrom.MergeTo cannot be null");
                            }
                        }
                        else if (currentMergeToFrom.MergeTo.Metadata == null)
                        {
                            throw new NullReferenceException("currentMergeToFrom.MergeTo cannot have null Metadata");
                        }
                        else if (currentMergeToFrom.MergeTo.NewPath == null)
                        {
                            throw new NullReferenceException("currentMergeToFrom.MergeTo cannot have null NewPath");
                        }

                        // Define field for the event id that needs updating in the database,
                        // defaulting to none
                        Nullable<long> eventIdToUpdate = null;

                        // If the mergedEvent already has an id (exists in database),
                        // then the database event will be updated at the mergedEvent id;
                        // also, if the oldEvent exists in the database, it needs to be removed
                        if (currentMergeToFrom.MergeTo == null
                            || currentMergeToFrom.MergeTo.EventId > 0)
                        {
                            if (currentMergeToFrom.MergeTo != null
                                && currentMergeToFrom.MergeTo.EventId > 0
                                // added the following condition in case both events to merge together share a single database event
                                // which should not be removed
                                && (currentMergeToFrom.MergeTo == null || currentMergeToFrom.MergeTo.EventId != currentMergeToFrom.MergeTo.EventId))
                            {
                                toDelete.Add(currentMergeToFrom.MergeTo.EventId);
                            }

                            // If the mergedEvent it null and the oldEvent is set with a valid eventId,
                            // then save only the deletion of the oldEvent and continue to next iteration
                            if (currentMergeToFrom.MergeTo == null)
                            {
                                continue;
                            }

                            eventIdToUpdate = currentMergeToFrom.MergeTo.EventId;
                        }
                        // Else if the mergedEvent does not have an id in the database
                        // and the oldEvent exists and has an id in the database,
                        // then the database event will be updated at the oldEvent id
                        // and the event id should be moved to the mergedEvent
                        else if (currentMergeToFrom.MergeFrom != null
                            && currentMergeToFrom.MergeFrom.EventId > 0)
                        {
                            currentMergeToFrom.MergeTo.EventId = currentMergeToFrom.MergeFrom.EventId;

                            eventIdToUpdate = currentMergeToFrom.MergeTo.EventId;
                        }

                        // If an id for the database event already exists,
                        // then update the object in the database with the latest properties from mergedEvent
                        if (eventIdToUpdate != null)
                        {
                            toUpdate[(long)eventIdToUpdate] = currentMergeToFrom.MergeTo;
                        }
                        // Else the database event does not already exist,
                        // then add it
                        else
                        {
                            toAdd.Add(currentMergeToFrom.MergeTo);
                        }
                    }

                    if (toAdd.Count > 0
                        || toUpdate.Count > 0
                        || toDelete.Count > 0)
                    {
                        if (!alreadyObtainedLock)
                        {
                            ExternalSQLLocker.EnterReadLock();
                        }
                        try
                        {
                            using (ISQLiteConnection indexDB = CreateAndOpenCipherConnection())
                            {
                                if (toDelete.Count > 0)
                                {
                                    CLError deleteErrors = RemoveEventsByIds(toDelete, true);

                                    if (deleteErrors != null)
                                    {
                                        toReturn += new AggregateException("Error deleting some events", deleteErrors.GrabExceptions());
                                    }
                                }

                                if (toUpdate.Count > 0
                                    || toAdd.Count > 0)
                                {
                                    // find all relevent files and folders directly under the root which are equal to any of the current paths or contained within current paths (will be all of them)
                                    // grab all their FileSystemObjectIds and store them with the paths
                                    // repeat for all inner levels until all ids are found for all levels

                                    // two FileSystemObjectIds could represent each path, so have to build all combination of all paths down to the paths we're searching and search until the output links to the event id,
                                    // or if the event id is not known then create a new FileSystemObject with a parent FileSystemObject that has the most recent EventId

                                    FilePath rootPathObject = indexedPath;

                                    HashSet<FileChange> changesLeftToFind = new HashSet<FileChange>(
                                        toUpdate.Values
                                            .Concat(toAdd));

                                    Dictionary<FilePath, long[]> lastPathsFound = new Dictionary<FilePath, long[]>(FilePathComparer.Instance)
                                    {
                                        { rootPathObject, new[] { rootFileSystemObjectId } }
                                    };

                                    bool continueSearching;
                                    do
                                    {
                                        continueSearching = false;

                                        List<long> idsToSearch = new List<long>();

                                        foreach (FileChange changeLeftToFind in changesLeftToFind)
                                        {
                                            long[] idsForPath;
                                            if (lastPathsFound.TryGetValue(changeLeftToFind.NewPath.Parent, out idsForPath))
                                            {
                                                continueSearching = true;

                                                idsToSearch.AddRange(idsForPath);

                                                //changesUnderCurrentPath.Add(
                                            }

                                            if (firstSearch && FilePathComparer.Instance.Equals(changeLeftToFind.NewPath.Parent, rootPathObject))
                                            {
                                                changesUnderCurrentPath.Add(new KeyValuePair<FileChange, long>(changeLeftToFind, rootFileSystemObjectId));
                                            }
                                            else
                                            {

                                            }
                                        }
                                    }
                                    while (continueSearching);
                                }

                                if (toUpdate.Count > 0)
                                {
                                    Dictionary<long, KeyValuePair<FileChange, GenericHolder<bool>>> findOriginal = toUpdate.ToDictionary(currentToUpdate => currentToUpdate.Key,
                                        currentToUpdate => new KeyValuePair<FileChange, GenericHolder<bool>>(currentToUpdate.Value, new GenericHolder<bool>(false)));

                                    // If any event is not found to update, then it can be added via identity insert to its provided event id key, this list stores those keys
                                    HashSet<long> missingKeys = new HashSet<long>();

                                    // Find the existing event for the given id
                                    Event[] toModify = SqlAccessor<Event>.SelectResultSet(indexDB,
                                        "SELECT " +
                                        SqlAccessor<Event>.GetSelectColumns() + ", " +
                                        SqlAccessor<FileSystemObject>.GetSelectColumns(FileSystemObject.Name) + " " +
                                        "FROM [Events] " +
                                        "INNER JOIN [FileSystemObjects] ON [Events].[EventId] = [FileSystemObjects].[EventId] " +
                                        "WHERE [Events].[EventId] IN (" +
                                        string.Join(", ", findOriginal.Keys.Select(currentToUpdate => currentToUpdate.ToString()).ToArray()) + ")",
                                        new string[]
                                    {
                                        FileSystemObject.Name
                                    }).ToArray();

                                    // Record exception if an existing event does not exist
                                    if (toModify == null
                                        || !toModify.Any())
                                    {
                                        foreach (long missingEventId in findOriginal.Keys)
                                        {
                                            missingKeys.Add(missingEventId);

                                            //toReturn += new Exception("Unable to find event with id " + missingEventId.ToString() + " to update");
                                        }
                                    }
                                    else
                                    {
                                        foreach (Event currentToModify in toModify)
                                        {
                                            KeyValuePair<FileChange, GenericHolder<bool>> mergedPair = findOriginal[currentToModify.EventId];
                                            mergedPair.Value.Value = true;
                                            FileChange mergedEvent = mergedPair.Key;

                                            // Update database object with latest event properties
                                            // TODO: add server id
                                            currentToModify.FileChangeTypeEnumId = changeEnumsBackward[mergedEvent.Type];
                                            currentToModify.PreviousPath = (mergedEvent.OldPath == null
                                                ? null
                                                : mergedEvent.OldPath.ToString());
                                            currentToModify.FileSystemObject.CreationTime = (mergedEvent.Metadata.HashableProperties.CreationTime.Ticks == FileConstants.InvalidUtcTimeTicks
                                                ? (Nullable<DateTime>)null
                                                : mergedEvent.Metadata.HashableProperties.CreationTime);
                                            currentToModify.FileSystemObject.IsFolder = mergedEvent.Metadata.HashableProperties.IsFolder;
                                            currentToModify.FileSystemObject.LastTime = (mergedEvent.Metadata.HashableProperties.LastTime.Ticks == FileConstants.InvalidUtcTimeTicks
                                                ? (Nullable<DateTime>)null
                                                : mergedEvent.Metadata.HashableProperties.LastTime);
                                            currentToModify.FileSystemObject.Path = mergedEvent.NewPath.ToString();
                                            currentToModify.FileSystemObject.Size = mergedEvent.Metadata.HashableProperties.Size;
                                            currentToModify.FileSystemObject.TargetPath = (mergedEvent.Metadata.LinkTargetPath == null ? null : mergedEvent.Metadata.LinkTargetPath.ToString());
                                            currentToModify.FileSystemObject.Revision = mergedEvent.Metadata.Revision;
                                            currentToModify.FileSystemObject.StorageKey = mergedEvent.Metadata.StorageKey;
                                        }

                                        foreach (KeyValuePair<FileChange, GenericHolder<bool>> mergedPair in findOriginal.Values)
                                        {
                                            if (!mergedPair.Value.Value)
                                            {
                                                missingKeys.Add(mergedPair.Key.EventId);

                                                //toReturn += new Exception("Unable to find event with id " + mergedPair.Key.EventId.ToString() + " to update");
                                            }
                                        }

                                        IEnumerable<int> unableToFindIndexes;

                                        SqlAccessor<Event>.UpdateRows(indexDB,
                                            toModify,
                                            out unableToFindIndexes);

                                        if (unableToFindIndexes != null
                                            && unableToFindIndexes.Any())
                                        {
                                            foreach (long missingEventId in
                                                unableToFindIndexes.Select(currentUnableToFind => toModify[currentUnableToFind].EventId)
                                                    .Distinct())// Possible to get back multiple Event objects with the same EventId if two or more FileSystemObjects have the same EventId (which is an error)
                                            {
                                                missingKeys.Add(missingEventId);

                                                //toReturn += new Exception("Unable to find event with id " + missingEventId.ToString() + " to update");
                                            }
                                        }

                                        SqlAccessor<FileSystemObject>.UpdateRows(indexDB,
                                            toModify.Select(currentToModify => currentToModify.FileSystemObject),
                                            out unableToFindIndexes);

                                        if (unableToFindIndexes != null)
                                        {
                                            foreach (FileSystemObject currentMissingFileSystemObject in unableToFindIndexes.Select(currentUnableToFind => toModify[currentUnableToFind].FileSystemObject))
                                            {
                                                if (currentMissingFileSystemObject.EventId == null || !missingKeys.Contains((long)currentMissingFileSystemObject.EventId))
                                                {
                                                    toReturn += new Exception("Unable to find file system object with id " + currentMissingFileSystemObject.FileSystemObjectId.ToString() + " to update");
                                                }
                                            }
                                        }
                                    }

                                    if (missingKeys.Count > 0)
                                    {
                                        Event[] identityInsertChanges = missingKeys.Select(currentMissingKey => findOriginal[currentMissingKey].Key)
                                            .Select(identityInsertChange => new KeyValuePair<FileChange, string>(identityInsertChange, identityInsertChange.NewPath.ToString()))
                                            .Select(identityInsertChange => new Event()
                                            {
                                                EventId = identityInsertChange.Key.EventId,
                                                FileChangeTypeCategoryId = changeCategoryId,
                                                FileChangeTypeEnumId = changeEnumsBackward[identityInsertChange.Key.Type],
                                                FileSystemObject = new FileSystemObject()
                                                {
                                                    // TODO: add server id
                                                    EventId = identityInsertChange.Key.EventId,
                                                    CreationTime = identityInsertChange.Key.Metadata.HashableProperties.CreationTime.Ticks == FileConstants.InvalidUtcTimeTicks
                                                        ? (Nullable<DateTime>)null
                                                        : identityInsertChange.Key.Metadata.HashableProperties.CreationTime,
                                                    IsFolder = identityInsertChange.Key.Metadata.HashableProperties.IsFolder,
                                                    LastTime = identityInsertChange.Key.Metadata.HashableProperties.LastTime.Ticks == FileConstants.InvalidUtcTimeTicks
                                                        ? (Nullable<DateTime>)null
                                                        : identityInsertChange.Key.Metadata.HashableProperties.LastTime,
                                                    Path = identityInsertChange.Value,
                                                    Size = identityInsertChange.Key.Metadata.HashableProperties.Size,
                                                    Revision = identityInsertChange.Key.Metadata.Revision,
                                                    StorageKey = identityInsertChange.Key.Metadata.StorageKey,
                                                    TargetPath = (identityInsertChange.Key.Metadata.LinkTargetPath == null ? null : identityInsertChange.Key.Metadata.LinkTargetPath.ToString()),
                                                    SyncCounter = null,
                                                    ServerLinked = false,

                                                    // SQL CE does not support computed columns, so no "AS CHECKSUM(Path)"
                                                    PathChecksum = StringCRC.Crc(identityInsertChange.Value)
                                                },
                                                PreviousPath = (identityInsertChange.Key.OldPath == null
                                                    ? null
                                                    : identityInsertChange.Key.OldPath.ToString()),
                                                SyncFrom = (identityInsertChange.Key.Direction == SyncDirection.From)
                                            }).ToArray();

                                        SqlAccessor<Event>.InsertRows(indexDB,
                                            identityInsertChanges,
                                            identityInsert: true);

                                        SqlAccessor<FileSystemObject>.InsertRows(indexDB,
                                            identityInsertChanges.Select(identityInsertChange => identityInsertChange.FileSystemObject));
                                    }
                                }

                                if (toAdd.Count > 0)
                                {
                                    CLError addError = AddEvents(toAdd, true);

                                    if (addError != null)
                                    {
                                        toReturn += new AggregateException("Error in adding events", addError.GrabExceptions());
                                    }
                                }
                            }
                        }
                        finally
                        {
                            if (!alreadyObtainedLock)
                            {
                                ExternalSQLLocker.ExitReadLock();
                            }
                        }
                    }

                    foreach (FileChangeMerge currentMergeToFrom in mergeToFroms)
                    {
                        // If mergedEvent was not processed in AddEvents,
                        // then process badging (AddEvents processes badging for the rest)
                        if (currentMergeToFrom.MergeTo == null
                            || toUpdate.ContainsKey(currentMergeToFrom.MergeTo.EventId)
                            || toDelete.Contains(currentMergeToFrom.MergeTo.EventId))
                        {
                            MessageEvents.ApplyFileChangeMergeToChangeState(this, new FileChangeMerge(currentMergeToFrom.MergeTo, currentMergeToFrom.MergeFrom));   // Message to invoke BadgeNet.IconOverlay.QueueNewEventBadge(currentMergeToFrom.MergeTo, currentMergeToFrom.MergeFrom)
                        }
                    }
                }
                catch (Exception ex)
                {
                    toReturn += ex;
                }
            }
            return toReturn;
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
            throw new NotImplementedException("11");
            //try
            //{
            //    Event currentEvent = null;              // scope outside for badging reference
            //    ExternalSQLLocker.EnterWriteLock();
            //    try
            //    {
            //        using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(this.indexDBLocation)))
            //        {
            //            indexDB.Open();

            //            using (SqlCeTransaction indexTransaction = indexDB.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
            //            {
            //                try
            //                {
            //                    // grab the event from the database by provided id
            //                    currentEvent = SqlAccessor<Event>.SelectResultSet(indexDB,
            //                            "SELECT TOP 1 " +
            //                            SqlAccessor<Event>.GetSelectColumns() + ", " +
            //                            SqlAccessor<FileSystemObject>.GetSelectColumns(FileSystemObject.Name) + " " +
            //                            "FROM [Events] " +
            //                            "INNER JOIN [FileSystemObjects] ON [Events].[EventId] = [FileSystemObjects].[EventId] " +
            //                            "WHERE [FileSystemObjects].[SyncCounter] IS NULL " +
            //                            "AND [Events].[EventId] = " + eventId.ToString(),
            //                            new string[]
            //                            {
            //                                FileSystemObject.Name
            //                            },
            //                            indexTransaction)
            //                        .SingleOrDefault();
            //                    // ensure an event was found
            //                    if (currentEvent == null)
            //                    {
            //                        throw new KeyNotFoundException("Previous event not found or not pending for given id: " + eventId.ToString());
            //                    }

            //                    // Grab the most recent sync from the database to pull sync states
            //                    SqlSync lastSync = SqlAccessor<SqlSync>.SelectResultSet(indexDB,
            //                            "SELECT TOP 1 * FROM [Syncs] ORDER BY [Syncs].[SyncCounter] DESC",
            //                            transaction: indexTransaction)
            //                        .SingleOrDefault();

            //                    // ensure a previous sync was found
            //                    if (lastSync == null)
            //                    {
            //                        throw new Exception("Previous sync not found for completed event");
            //                    }

            //                    // declare fields to store server paths in case they are different than the local paths,
            //                    // defaulting to null
            //                    string serverRemappedNewPath = null;
            //                    string serverRemappedOldPath = null;

            //                    int crcInt = StringCRC.Crc(currentEvent.FileSystemObject.Path);
            //                    Nullable<int> crcIntOld = (currentEvent.FileChangeTypeEnumId == changeEnumsBackward[FileChangeType.Renamed]
            //                            && currentEvent.PreviousPath != null
            //                        ? StringCRC.Crc(currentEvent.PreviousPath)
            //                        : (Nullable<int>)null);

            //                    // pull the sync states for the new path of the current event
            //                    IEnumerable<FileSystemObject> existingObjectsAtPath = SqlAccessor<FileSystemObject>.SelectResultSet(indexDB,
            //                                "SELECT * " +
            //                                "FROM [FileSystemObjects] " +
            //                                "WHERE [FileSystemObjects].[SyncCounter] = " + lastSync.SyncCounter.ToString() + " " +
            //                                "AND [FileSystemObjects].[ServerLinked] = 0 AND " +
            //                                (crcIntOld == null
            //                                    ? "[FileSystemObjects].[PathChecksum] = " + crcInt.ToString()
            //                                    : "([FileSystemObjects].[PathChecksum] = " + crcInt.ToString() + " OR [FileSystemObjects].[PathChecksum] = " + ((int)crcIntOld).ToString() + ")"),
            //                            transaction: indexTransaction)
            //                        .Where(currentSyncState =>
            //                            (crcIntOld == null
            //                                ? currentSyncState.Path == currentEvent.FileSystemObject.Path
            //                                : (currentSyncState.Path == currentEvent.FileSystemObject.Path || currentSyncState.Path == currentEvent.PreviousPath))); // run in memory since Path field is not indexable

            //                    if (existingObjectsAtPath != null
            //                        && existingObjectsAtPath.Any())
            //                    {
            //                        // add in server-linked objects
            //                        existingObjectsAtPath = existingObjectsAtPath.Concat(SqlAccessor<FileSystemObject>.SelectResultSet(indexDB,
            //                                "SELECT * FROM [FileSystemObjects] WHERE [FileSystemObjects].[ServerLinked] = 1 AND [FileSystemObjects].[FileSystemObjectId] IN (" +
            //                                string.Join(", ", existingObjectsAtPath.Select(currentExistingObject => currentExistingObject.FileSystemObjectId.ToString()).ToArray()) + ")",
            //                            transaction: indexTransaction));
            //                    }

            //                    Dictionary<long, KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>> newPathStates = new Dictionary<long, KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>>();

            //                    foreach (FileSystemObject currentExistingObject in existingObjectsAtPath)
            //                    {
            //                        if (newPathStates.ContainsKey(currentExistingObject.FileSystemObjectId))
            //                        {
            //                            if (currentExistingObject.ServerLinked)
            //                            {
            //                                newPathStates[currentExistingObject.FileSystemObjectId].Value.Value = currentExistingObject;
            //                            }
            //                            else
            //                            {
            //                                newPathStates[currentExistingObject.FileSystemObjectId].Key.Value = currentExistingObject;
            //                            }
            //                        }
            //                        else if (currentExistingObject.ServerLinked)
            //                        {
            //                            newPathStates.Add(currentExistingObject.FileSystemObjectId,
            //                                new KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>(
            //                                    new GenericHolder<FileSystemObject>(),
            //                                    new GenericHolder<FileSystemObject>(currentExistingObject)));
            //                        }
            //                        else
            //                        {
            //                            newPathStates.Add(currentExistingObject.FileSystemObjectId,
            //                                new KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>(
            //                                    new GenericHolder<FileSystemObject>(currentExistingObject),
            //                                    new GenericHolder<FileSystemObject>()));
            //                        }
            //                    }

            //                    List<FileSystemObject> toDelete = new List<FileSystemObject>();

            //                    foreach (KeyValuePair<long, KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>> currentPreviousState in
            //                        newPathStates.OrderBy(orderState => orderState.Key))
            //                    {
            //                        if (currentPreviousState.Value.Key.Value.Path == currentEvent.FileSystemObject.Path)
            //                        {
            //                            if (currentPreviousState.Value.Value.Value != null)
            //                            {
            //                                serverRemappedNewPath = currentPreviousState.Value.Value.Value.Path;

            //                                toDelete.Add(currentPreviousState.Value.Value.Value);
            //                            }
            //                        }
            //                        else if (currentPreviousState.Value.Value.Value != null)
            //                        {
            //                            serverRemappedOldPath = currentPreviousState.Value.Value.Value.Path;

            //                            toDelete.Add(currentPreviousState.Value.Value.Value);
            //                        }

            //                        toDelete.Add(currentPreviousState.Value.Key.Value);
            //                    }

            //                    if (toDelete.Count > 0)
            //                    {
            //                        // remove the current sync state(s)
            //                        IEnumerable<int> unableToFindIndexes;
            //                        SqlAccessor<FileSystemObject>.DeleteRows(indexDB, toDelete, out unableToFindIndexes, transaction: indexTransaction);
            //                    }

            //                    // if the change type of the current event is not a deletion,
            //                    // then a new sync state needs to be created for the event and added to the database
            //                    if (changeEnums[currentEvent.FileChangeTypeEnumId] != FileChangeType.Deleted)
            //                    {
            //                        // function to produce a new file system object to store in the database for the event's object;
            //                        // dynamically sets the path from input
            //                        Func<string, bool, FileSystemObject> getNewFileSystemObject = (fileSystemPath, serverLinked) =>
            //                            {
            //                                return new FileSystemObject()
            //                                {
            //                                    CreationTime = currentEvent.FileSystemObject.CreationTime,
            //                                    IsFolder = currentEvent.FileSystemObject.IsFolder,
            //                                    LastTime = currentEvent.FileSystemObject.LastTime,
            //                                    Path = fileSystemPath,
            //                                    Size = currentEvent.FileSystemObject.Size,
            //                                    TargetPath = currentEvent.FileSystemObject.TargetPath,
            //                                    Revision = currentEvent.FileSystemObject.Revision,
            //                                    StorageKey = currentEvent.FileSystemObject.StorageKey,
            //                                    EventId = eventId,
            //                                    FileSystemObjectId = currentEvent.FileSystemObject.FileSystemObjectId,
            //                                    ServerLinked = serverLinked,
            //                                    SyncCounter = lastSync.SyncCounter,

            //                                    // SQL CE does not support computed columns, so no "AS CHECKSUM(Path)"
            //                                    PathChecksum = StringCRC.Crc(fileSystemPath)
            //                                };
            //                            };

            //                        // create the file system object for the local path for the event
            //                        FileSystemObject eventFileSystemObject = getNewFileSystemObject(currentEvent.FileSystemObject.Path, false);

            //                        if (serverRemappedNewPath != null)
            //                        {
            //                            // create the file system object for the server remapped path for the event
            //                            FileSystemObject serverRemappedFileSystemObject = getNewFileSystemObject(serverRemappedNewPath, true);

            //                            // apply the events to the Sync
            //                            IEnumerable<int> unableToFindIndexes;
            //                            SqlAccessor<FileSystemObject>.UpdateRows(indexDB, new FileSystemObject[] { eventFileSystemObject, serverRemappedFileSystemObject }, out unableToFindIndexes, transaction: indexTransaction);
            //                        }
            //                        else
            //                        {
            //                            // apply the event to the Sync
            //                            SqlAccessor<FileSystemObject>.UpdateRow(indexDB, eventFileSystemObject, transaction: indexTransaction);
            //                        }
            //                    }

            //                    indexTransaction.Commit(CommitMode.Immediate);
            //                }
            //                catch
            //                {
            //                    indexTransaction.Rollback();

            //                    throw;
            //                }
            //            }
            //        }
            //    }
            //    finally
            //    {
            //        ExternalSQLLocker.ExitWriteLock();
            //    }

            //    Action<FilePath> setBadgeSynced = syncedPath =>
            //        {
            //            MessageEvents.QueueSetBadge(this, new SetBadge(PathState.Synced, syncedPath));   // Message to invoke BadgeNet.IconOverlay.QueueSetBadge(PathState.Synced, syncedPath);
            //        };

            //    // Adjust the badge for this completed event.
            //    if (currentEvent != null)
            //    {
            //        switch (changeEnums[currentEvent.FileChangeTypeEnumId])
            //        {
            //            case FileChangeType.Created:
            //            case FileChangeType.Modified:
            //                setBadgeSynced(currentEvent.FileSystemObject.Path);
            //                break;
            //            case FileChangeType.Deleted:
            //                if (currentEvent.SyncFrom)
            //                {
            //                    bool isDeleted;
            //                    MessageEvents.DeleteBadgePath(this, new DeleteBadgePath(currentEvent.FileSystemObject.Path), out isDeleted);   // Message to invoke BadgeNet.IconOverlay.DeleteBadgePath(currentEvent.FileSystemObject.Path, out isDeleted);
            //                }
            //                else
            //                {
            //                    setBadgeSynced(currentEvent.FileSystemObject.Path);
            //                }
            //                break;
            //            case FileChangeType.Renamed:
            //                if (currentEvent.SyncFrom)
            //                {
            //                    MessageEvents.RenameBadgePath(this, new RenameBadgePath(currentEvent.PreviousPath, currentEvent.FileSystemObject.Path));   // Message to invoke BadgeNet.IconOverlay.RenameBadgePath(currentEvent.PreviousPath, currentEvent.FileSystemObject.Path);
            //                }

            //                setBadgeSynced(currentEvent.FileSystemObject.Path);
            //                break;
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    return ex;
            //}
            //return null;
        }
        #endregion

        #region private methods

        /// <summary>
        /// Private constructor to ensure IndexingAgent is created through public static initializer (to return a CLError)
        /// </summary>
        /// <param name="syncBox">SyncBox to index</param>
        private IndexingAgent(CLSyncBox syncBox)
        {
            if (syncBox == null)
            {
                throw new NullReferenceException("syncBox cannot be null");
            }

            this.indexDBLocation = (string.IsNullOrEmpty(syncBox.CopiedSettings.DatabaseFolder)
                ? Helpers.GetDefaultDatabasePath(syncBox.CopiedSettings.DeviceId, syncBox.SyncBoxId) + "\\" + CLDefinitions.kSyncDatabaseFileName
                : syncBox.CopiedSettings.DatabaseFolder + "\\" + CLDefinitions.kSyncDatabaseFileName);

            this.syncBox = syncBox;
        }

        private void InitializeDatabase(string syncRoot)
        {
            FileInfo dbInfo = new FileInfo(indexDBLocation);

            bool deleteDB;
            bool createDB;

            if (dbInfo.Exists)
            {
                try
                {
                    using (ISQLiteConnection verifyAndUpdateConnection = CreateAndOpenCipherConnection())
                    {
                        CheckIntegrity(verifyAndUpdateConnection);

                        int existingVersion;

                        using (ISQLiteCommand getVersionCommand = verifyAndUpdateConnection.CreateCommand())
                        {
                            getVersionCommand.CommandText = "PRAGMA user_version;";
                            existingVersion = Convert.ToInt32(getVersionCommand.ExecuteScalar());
                        }

                        if (existingVersion < 2)
                        {
                            createDB = true;
                            deleteDB = true;
                        }
                        else
                        {
                            int newVersion = -1;

                            foreach (KeyValuePair<int, IMigration> currentDBMigration in MigrationList.GetMigrationsAfterVersion(existingVersion))
                            {
                                currentDBMigration.Value.Apply(
                                    verifyAndUpdateConnection,
                                    indexDBPassword);

                                newVersion = currentDBMigration.Key;
                            }

                            if (newVersion > existingVersion)
                            {
                                using (ISQLiteCommand updateVersionCommand = verifyAndUpdateConnection.CreateCommand())
                                {
                                    updateVersionCommand.CommandText = "PRAGMA user_version = " + newVersion.ToString();
                                    updateVersionCommand.ExecuteNonQuery();
                                }
                            }

                            createDB = false;
                            deleteDB = false;
                        }
                    }
                }
                catch (SQLiteExceptionBase ex)
                {
                    // notify database replaced due to corruption
                    MessageEvents.FireNewEventMessage(
                        "Database corruption found on initializing index. Replacing database with a fresh one. Files and folders changed while offline will be grabbed again from server. Error message: " + ex.Message,
                        EventMessageLevel.Important,
                        new GeneralErrorInfo(),
                        syncBox.SyncBoxId,
                        syncBox.CopiedSettings.DeviceId);

                    deleteDB = true;
                    createDB = true;
                }
            }
            else
            {
                MessageEvents.FireNewEventMessage(
                    "Existing database not found, possibly due to new SyncBoxId\\DeviceId combination. Starting fresh.",
                    EventMessageLevel.Minor,
                    SyncBoxId: syncBox.SyncBoxId,
                    DeviceId: syncBox.CopiedSettings.DeviceId);

                createDB = true;
                deleteDB = false;
            }

            if (deleteDB)
            {
                dbInfo.Delete();
            }

            if (createDB)
            {
                using (ISQLiteConnection newDBConnection = CreateAndOpenCipherConnection(enforceForeignKeyConstraints: false)) // circular reference between Events and FileSystemObjects tables
                {
                    // read creation scripts in here

                    FileInfo indexDBInfo = new FileInfo(indexDBLocation);
                    if (!indexDBInfo.Directory.Exists)
                    {
                        indexDBInfo.Directory.Create();
                    }

                    System.Reflection.Assembly indexingAssembly = System.Reflection.Assembly.GetAssembly(typeof(IndexingAgent));

                    List<KeyValuePair<int, string>> indexDBScripts = new List<KeyValuePair<int, string>>();

                    string scriptDirectory = indexingAssembly.GetName().Name + indexScriptsResourceFolder;

                    Encoding ansiEncoding = Encoding.GetEncoding(1252); //ANSI saved from NotePad on a US-EN Windows machine

                    foreach (string currentScriptName in indexingAssembly.GetManifestResourceNames()
                        .Where(resourceName => resourceName.StartsWith(scriptDirectory)))
                    {
                        if (!string.IsNullOrWhiteSpace(currentScriptName)
                            && currentScriptName.Length >= 5 // length of 1+-digit number plus ".sql" file extension
                            && currentScriptName.EndsWith(".sql", StringComparison.InvariantCultureIgnoreCase))
                        {
                            int numChars = 0;
                            for (int numberCharIndex = scriptDirectory.Length; numberCharIndex < currentScriptName.Length; numberCharIndex++)
                            {
                                if (!char.IsDigit(currentScriptName[numberCharIndex]))
                                {
                                    numChars = numberCharIndex - scriptDirectory.Length;
                                    break;
                                }
                            }
                            if (numChars > 0)
                            {
                                string nameNumberPortion = currentScriptName.Substring(scriptDirectory.Length, numChars);
                                int nameNumber;
                                if (int.TryParse(nameNumberPortion, out nameNumber))
                                {
                                    using (Stream resourceStream = indexingAssembly.GetManifestResourceStream(currentScriptName))
                                    {
                                        using (StreamReader resourceReader = new StreamReader(resourceStream, ansiEncoding))
                                        {
                                            indexDBScripts.Add(new KeyValuePair<int, string>(nameNumber, resourceReader.ReadToEnd()));
                                        }
                                    }
                                }
                            }
                        }
                    }

                    using (ISQLiteConnection creationConnection = CreateAndOpenCipherConnection(enforceForeignKeyConstraints: false)) // do not enforce constraints since part of the creation scripts are to create two tables which foreign key reference each other
                    {
                        // special enumerator processing so we can inject an operation immediately before processing the last script:
                        // we need to add the root FileSystemObject before updating the user versions via PRAGMA (which should be the last SQL script)
                        string storeLastScript = null;
                        IEnumerator<string> insertEnumerator = indexDBScripts.OrderBy(scriptPair => scriptPair.Key)
                            .Select(scriptPair => scriptPair.Value)
                            .GetEnumerator();
                        bool lastInsert;
                        while (!(lastInsert = !insertEnumerator.MoveNext()) || storeLastScript != null)
                        {
                            if (storeLastScript != null)
                            {
                                if (lastInsert)
                                {
                                    rootFileSystemObjectId = SqlAccessor<FileSystemObject>.InsertRow<long>
                                        (creationConnection,
                                            new FileSystemObject()
                                            {
                                                EventTimeUTCTicks = 0, // never need to show the root folder in recents, so it should have the oldest event time
                                                IsFolder = true,
                                                Name = syncRoot,
                                                Pending = false
                                            });
                                }
                                
                                using (ISQLiteCommand scriptCommand = creationConnection.CreateCommand())
                                {
                                    scriptCommand.CommandText = Helpers.DecryptString(storeLastScript,
                                        Encoding.ASCII.GetString(
                                            Convert.FromBase64String(indexDBPassword)));
                                    scriptCommand.ExecuteNonQuery();
                                }
                            }

                            storeLastScript = (lastInsert
                                ? null
                                : insertEnumerator.Current);
                        }
                    }
                }
            }

            int changeEnumsCount = System.Enum.GetNames(typeof(FileChangeType)).Length;
            changeEnums = new Dictionary<long, FileChangeType>(changeEnumsCount);
            changeEnumsBackward = new Dictionary<FileChangeType, long>(changeEnumsCount);

            using (ISQLiteConnection indexDB = CreateAndOpenCipherConnection())
            {
                long storeCategoryId = -1;
                foreach (EnumCategory currentCategory in SqlAccessor<EnumCategory>
                    .SelectResultSet(indexDB,
                        "SELECT * FROM EnumCategories WHERE Name = '" + typeof(FileChangeType).Name.Replace("'", "''") + "'"))
                {
                    if (storeCategoryId == -1)
                    {
                        storeCategoryId = currentCategory.EnumCategoryId;
                    }
                    else
                    {
                        throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "More than one type with name FileChangeType found");
                    }
                }

                if (storeCategoryId == -1)
                {
                    throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "No EnumCategory found with name FileChangeType");
                }

                foreach (SqlEnum currentChangeEnum in SqlAccessor<SqlEnum>
                    .SelectResultSet(indexDB,
                        "SELECT * FROM Enums WHERE Enums.EnumCategoryId = " + storeCategoryId.ToString()))
                {
                    changeCategoryId = currentChangeEnum.EnumCategoryId;
                    long forwardKey = currentChangeEnum.EnumId;

                    FileChangeType forwardValue;
                    if (!System.Enum.TryParse<FileChangeType>(currentChangeEnum.Name, out forwardValue))
                    {
                        throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Name of Enum for FileChangeType EnumCategory does not parse as a FileChangeType");
                    }

                    changeEnums.Add(forwardKey,
                        forwardValue);
                    changeEnumsBackward.Add(forwardValue,
                        forwardKey);
                }
            }

            if (changeEnums.Count != changeEnumsCount)
            {
                throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "FileChangeType enumerated values do not match count with names in the database");
            }
        }

        private static void CheckIntegrity(ISQLiteConnection conn)
        {
            using (ISQLiteCommand integrityCheckCommand = conn.CreateCommand())
            {
                // it's possible integrity_check could be replaced with quick_check for faster performance if it doesn't risk missing any corruption
                integrityCheckCommand.CommandText = "PRAGMA integrity_check(1);"; // we don't output all the corruption results, only need to grab 1
                using (ISQLiteDataReader integrityReader = integrityCheckCommand.ExecuteReader(System.Data.CommandBehavior.SingleResult))
                {
                    if (integrityReader.Read())
                    {
                        int integrityCheckColumnOrdinal = integrityReader.GetOrdinal("integrity_check");
                        if (integrityCheckColumnOrdinal == -1)
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Corrupt, "Result from integrity_check does not contain integrity_check column");
                        }

                        if (integrityReader.IsDBNull(integrityCheckColumnOrdinal))
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Corrupt, "First result from integrity_check contains a null value");
                        }

                        string integrityCheckValue;
                        try
                        {
                            integrityCheckValue = Convert.ToString(integrityReader["integrity_check"]);
                        }
                        catch
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Corrupt, "Value of first result from integrity_check is not convertable to String");
                        }

                        if (integrityCheckValue != "ok")
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Corrupt, "Value of first result from integrity_check indicates failure. Message: " +
                                (string.IsNullOrWhiteSpace(integrityCheckValue) ? "{empty}" : integrityCheckValue));
                        }
                    }
                    else
                    {
                        throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Corrupt, "Unable to read result of integrity_check");
                    }
                }
            }
        }

        private ISQLiteConnection CreateAndOpenCipherConnection(bool enforceForeignKeyConstraints = true)
        {
            const string CipherConnectionString = "Data Source=\"{0}\";Pooling=false;Synchronous=Full;UTF8Encoding=True;Foreign Keys={1}";

            ISQLiteConnection cipherConn = SQLConstructors.SQLiteConnection(
                string.Format(
                    CipherConnectionString,
                    indexDBLocation,
                    enforceForeignKeyConstraints.ToString()));

            try
            {
                cipherConn.SetPassword(
                    Encoding.ASCII.GetString(
                        Convert.FromBase64String(indexDBPassword)));

                cipherConn.Open();

                return cipherConn;
            }
            catch
            {
                cipherConn.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Action fired on a user worker thread which traverses the root path to build an initial index on application startup
        /// </summary>
        /// <param name="indexCompletionCallback">Callback should be the BeginProcessing method of the FileMonitor to forward the initial index</param>
        private void BuildIndex(Action<IEnumerable<KeyValuePair<FilePath, FileMetadata>>, IEnumerable<FileChange>> indexCompletionCallback)
        {
            FilePath baseComparePath = indexedPath;

            // Create the initial index dictionary, throwing any exceptions that occurred in the process
            FilePathDictionary<FileMetadata> indexPaths;
            CLError indexPathCreationError = FilePathDictionary<FileMetadata>.CreateAndInitialize(baseComparePath,
                out indexPaths);
            if (indexPathCreationError != null)
            {
                throw indexPathCreationError.GrabFirstException();
            }

            FilePathDictionary<FileMetadata> combinedIndexPlusChanges;
            CLError combinedIndexCreationError = FilePathDictionary<FileMetadata>.CreateAndInitialize(baseComparePath,
                out combinedIndexPlusChanges);
            if (combinedIndexCreationError != null)
            {
                throw combinedIndexCreationError.GrabFirstException();
            }

            FilePathDictionary<GenericHolder<bool>> pathDeletions;
            CLError pathDeletionsCreationError = FilePathDictionary<GenericHolder<bool>>.CreateAndInitialize(baseComparePath,
                out pathDeletions);
            if (pathDeletionsCreationError != null)
            {
                throw pathDeletionsCreationError.GrabFirstException();
            }

            using (ISQLiteConnection indexDB = CreateAndOpenCipherConnection())
            {
                // Grab the most recent sync from the database to pull sync states
                SqlSync lastSync = SqlAccessor<SqlSync>.SelectResultSet(indexDB,
                    "SELECT * FROM Syncs ORDER BY Syncs.SyncCounter DESC LIMIT 1")
                    .SingleOrDefault();
                // Store the sync counter from the last sync, defaulting to null
                Nullable<long> lastSyncCounter = (lastSync == null
                    ? (Nullable<long>)null
                    : lastSync.SyncCounter);

                // Update the exposed last sync id string under a lock
                LastSyncLocker.EnterWriteLock();
                try
                {
                    this.LastSyncId = (lastSync == null
                        ? null
                        : lastSync.SID);
                }
                finally
                {
                    LastSyncLocker.ExitWriteLock();
                }

                // Create mappings so we can find the previous paths for rename events
                Dictionary<long, FilePath> objectIdsToFullPath = new Dictionary<long, FilePath>();

                // Pull all non-pending objects to use as starting index before calculating changes to apply
                // Store as dictionary by ParentFolderId so that full paths can be rebuilt from the root to the deepest inner directories and files

                FileSystemObject rootFolderObject = null;
                Dictionary<long, List<FileSystemObject>> parentIdsToObjects = new Dictionary<long, List<FileSystemObject>>();

                // Loop through non-pendings
                foreach (FileSystemObject currentNonPending in SqlAccessor<FileSystemObject>.SelectResultSet(indexDB,
                    "SELECT * FROM FileSystemObjects WHERE FileSystemObjects.Pending = 0"))
                {
                    if (currentNonPending.ParentFolderId == null)
                    {
                        if (rootFolderObject != null)
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "More than one non-pending FileSystemObject found with a null ParentFolderId. This should represent at most one single root folder object.");
                        }

                        rootFileSystemObjectId = (rootFolderObject = currentNonPending).FileSystemObjectId;
                    }
                    else
                    {
                        List<FileSystemObject> existingObjectsAtParentId;
                        if (!parentIdsToObjects.TryGetValue((long)currentNonPending.ParentFolderId, out existingObjectsAtParentId))
                        {
                            parentIdsToObjects.Add((long)currentNonPending.ParentFolderId, existingObjectsAtParentId = new List<FileSystemObject>());
                        }

                        existingObjectsAtParentId.Add(currentNonPending);
                    }
                }

                if (rootFolderObject == null)
                {
                    throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "FileSystemObject with a null ParentFolderId not found as not pending");
                }

                // set the root metadata
                combinedIndexPlusChanges[null] = indexPaths[null] = new FileMetadata()
                {
                    EventTime = new DateTime(rootFolderObject.EventTimeUTCTicks, DateTimeKind.Utc),
                    HashableProperties = new FileMetadataHashableProperties(
                        isFolder: rootFolderObject.IsFolder,
                        lastTime: null,
                        creationTime: null,
                        size: null),
                    ServerUid = rootFolderObject.ServerUid
                };

                // no reason to store the path of the root object in objectIdsToFullPath since a rename's PreviousId should never point to the root

                BuildIndex_navigatePathsFromRoot navigatePathsFromRoot =
                    delegate (Dictionary<long, List<FileSystemObject>> innerExistingObjects,
                        FilePath parentPath,
                        long parentId,
                        FilePathDictionary<FileMetadata> innerIndexPaths,
                        FilePathDictionary<FileMetadata> innerCombinedIndexPlusChanges,
                        Dictionary<long, FilePath> innerObjectIdsToFullPath,
                        BuildIndex_navigatePathsFromRoot thisAction)
                    {
                        List<FileSystemObject> currentChildren;
                        if (innerExistingObjects.TryGetValue(parentId, out currentChildren))
                        {
                            foreach (FileSystemObject currentChild in currentChildren)
                            {
                                FileMetadata currentToAdd = new FileMetadata()
                                {
                                    EventTime = new DateTime(currentChild.EventTimeUTCTicks, DateTimeKind.Utc),
                                    HashableProperties = new FileMetadataHashableProperties(
                                        isFolder: currentChild.IsFolder,
                                        lastTime: (currentChild.LastTimeUTCTicks == null
                                            ? (Nullable<DateTime>)null
                                            : new DateTime((long)currentChild.LastTimeUTCTicks, DateTimeKind.Utc)),
                                        creationTime: (currentChild.CreationTimeUTCTicks == null
                                            ? (Nullable<DateTime>)null
                                            : new DateTime((long)currentChild.CreationTimeUTCTicks, DateTimeKind.Utc)),
                                        size: currentChild.Size),
                                    IsShare = currentChild.IsShare,
                                    MimeType = currentChild.MimeType,
                                    Permissions = (currentChild.Permissions == null ? (Nullable<POSIXPermissions>)null : (POSIXPermissions)((int)currentChild.Permissions)),
                                    Revision = currentChild.Revision,
                                    ServerUid = currentChild.ServerUid,
                                    StorageKey = currentChild.StorageKey,
                                    Version = currentChild.Version
                                };

                                FilePath currentPath = new FilePath(currentChild.Name, parentPath.Copy());
                                indexPaths.Add(currentPath, currentToAdd);
                                combinedIndexPlusChanges.Add(currentPath, currentToAdd);

                                innerObjectIdsToFullPath[currentChild.FileSystemObjectId] = currentPath;

                                thisAction(
                                    innerExistingObjects: innerExistingObjects,
                                    parentPath: currentPath,
                                    parentId: currentChild.FileSystemObjectId,
                                    innerIndexPaths: innerIndexPaths,
                                    innerCombinedIndexPlusChanges: innerCombinedIndexPlusChanges,
                                    innerObjectIdsToFullPath: innerObjectIdsToFullPath,
                                    thisAction: thisAction);
                            }
                        }
                    };

                navigatePathsFromRoot(
                    innerExistingObjects: parentIdsToObjects,
                    parentPath: baseComparePath,
                    parentId: rootFolderObject.FileSystemObjectId,
                    innerIndexPaths: indexPaths,
                    innerCombinedIndexPlusChanges: combinedIndexPlusChanges,
                    innerObjectIdsToFullPath: objectIdsToFullPath,
                    thisAction: navigatePathsFromRoot);

                Dictionary<long, List<Event>> parentIdsToEvents = new Dictionary<long, List<Event>>();

                // Loop through pendings
                foreach (Event currentPending in SqlAccessor<Event>.SelectResultSet(
                    indexDB,
                    "SELECT " +
                    SqlAccessor<Event>.GetSelectColumns() + ", " +
                    SqlAccessor<FileSystemObject>.GetSelectColumns(Constants.FileSystemObjectName) +
                    " FROM Events" +
                    " INNER JOIN FileSystemObjects ON Events.EventId = FileSystemObjects.EventId" +
                    " WHERE FileSystemObjects.Pending = 1" +
                    " AND FileSystemObjects.ParentFolderId IS NOT NULL"))
                {
                    if (currentPending.FileSystemObject == null)
                    {
                        throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Query should have been an inner join on FileSystemObjects, but a row was joined without it");
                    }
                    if (currentPending.FileSystemObject.ParentFolderId == null)
                    {
                        throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Query should have excluded null ParentFolderIds, but one exists anyways");
                    }

                    List<Event> existingEventsAtParentId;
                    if (!parentIdsToEvents.TryGetValue((long)currentPending.FileSystemObject.ParentFolderId, out existingEventsAtParentId))
                    {
                        parentIdsToEvents.Add((long)currentPending.FileSystemObject.ParentFolderId, existingEventsAtParentId = new List<Event>());
                    }

                    existingEventsAtParentId.Add(currentPending);
                }

                BuildIndex_navigateEventsFromRoot navigateEventsFromRoot =
                    delegate(Dictionary<long, List<FileSystemObject>> innerExistingObjects,
                        Dictionary<long, List<Event>> innerPendingEvents,
                        FilePath parentPath,
                        long parentId,
                        List<FileChange> innerPendingsWithSyncCounter,
                        List<FileChange> innerPendingsWithoutSyncCounter,
                        Dictionary<long, FilePath> innerObjectIdsToFullPath,
                        List<KeyValuePair<FileChange, long>> innerRenameChangesToPreviousId,
                        BuildIndex_navigateEventsFromRoot thisAction)
                    {
                        List<Event> pendingsAtParentId;
                        if (innerPendingEvents.TryGetValue(parentId, out pendingsAtParentId))
                        {
                            foreach (Event currentChild in pendingsAtParentId)
                            {
                                // already checked that each event has a FileSystemObject

                                FilePath currentPath = new FilePath(currentChild.FileSystemObject.Name, parentPath.Copy());

                                FileChange currentChange = new FileChange()
                                {
                                    Direction = (currentChild.SyncFrom ? SyncDirection.From : SyncDirection.To),
                                    EventId = currentChild.EventId,
                                    Metadata = new FileMetadata()
                                        {
                                            EventTime = new DateTime(currentChild.FileSystemObject.EventTimeUTCTicks, DateTimeKind.Utc),
                                            HashableProperties = new FileMetadataHashableProperties(
                                                isFolder: currentChild.FileSystemObject.IsFolder,
                                                lastTime: (currentChild.FileSystemObject.LastTimeUTCTicks == null
                                                    ? (Nullable<DateTime>)null
                                                    : new DateTime((long)currentChild.FileSystemObject.LastTimeUTCTicks, DateTimeKind.Utc)),
                                                creationTime: (currentChild.FileSystemObject.CreationTimeUTCTicks == null
                                                    ? (Nullable<DateTime>)null
                                                    : new DateTime((long)currentChild.FileSystemObject.CreationTimeUTCTicks, DateTimeKind.Utc)),
                                                size: currentChild.FileSystemObject.Size),
                                            IsShare = currentChild.FileSystemObject.IsShare,
                                            MimeType = currentChild.FileSystemObject.MimeType,
                                            Permissions = (currentChild.FileSystemObject.Permissions == null ? (Nullable<POSIXPermissions>)null : (POSIXPermissions)((int)currentChild.FileSystemObject.Permissions)),
                                            Revision = currentChild.FileSystemObject.Revision,
                                            ServerUid = currentChild.FileSystemObject.ServerUid,
                                            StorageKey = currentChild.FileSystemObject.StorageKey,
                                            Version = currentChild.FileSystemObject.Version
                                        },
                                    NewPath = currentPath,
                                    /* OldPath will get set afterwards via innerRenameChangesToPreviousId */
                                    Type = changeEnums[currentChild.FileChangeTypeEnumId]
                                };

                                if (currentChild.FileSystemObject.MD5 != null)
                                {
                                    CLError setCurrentChangeMD5Error = currentChange.SetMD5(currentChild.FileSystemObject.MD5);
                                    if (setCurrentChangeMD5Error != null)
                                    {
                                        throw new AggregateException("Error setting currentChange MD5", setCurrentChangeMD5Error.GrabExceptions());
                                    }
                                }

                                (currentChild.FileSystemObject.SyncCounter == null
                                        ? innerPendingsWithoutSyncCounter
                                        : innerPendingsWithSyncCounter)
                                    .Add(currentChange);

                                if (currentChild.PreviousId != null)
                                {
                                    innerRenameChangesToPreviousId.Add(new KeyValuePair<FileChange, long>(currentChange, (long)currentChild.PreviousId));
                                }

                                innerObjectIdsToFullPath[currentChild.FileSystemObject.FileSystemObjectId] = currentPath;

                                thisAction(
                                    innerExistingObjects: innerExistingObjects,
                                    innerPendingEvents: innerPendingEvents,
                                    parentPath: currentPath,
                                    parentId: currentChild.FileSystemObject.FileSystemObjectId,
                                    innerPendingsWithSyncCounter: innerPendingsWithSyncCounter,
                                    innerPendingsWithoutSyncCounter: innerPendingsWithoutSyncCounter,
                                    innerObjectIdsToFullPath: innerObjectIdsToFullPath,
                                    innerRenameChangesToPreviousId: innerRenameChangesToPreviousId,
                                    thisAction: thisAction);
                            }
                        }

                        List<FileSystemObject> existingsAtParentId;
                        if (innerExistingObjects.TryGetValue(parentId, out existingsAtParentId))
                        {
                            foreach (FileSystemObject currentChild in existingsAtParentId)
                            {
                                FilePath currentPath = new FilePath(currentChild.Name, parentPath.Copy());

                                thisAction(
                                    innerExistingObjects: innerExistingObjects,
                                    innerPendingEvents: innerPendingEvents,
                                    parentPath: currentPath,
                                    parentId: currentChild.FileSystemObjectId,
                                    innerPendingsWithSyncCounter: innerPendingsWithSyncCounter,
                                    innerPendingsWithoutSyncCounter: innerPendingsWithoutSyncCounter,
                                    innerObjectIdsToFullPath: innerObjectIdsToFullPath,
                                    innerRenameChangesToPreviousId: innerRenameChangesToPreviousId,
                                    thisAction: thisAction);
                            }
                        }
                    };

                List<FileChange> pendingsWithSyncCounter = new List<FileChange>();
                List<FileChange> pendingsWithoutSyncCounter = new List<FileChange>();
                List<KeyValuePair<FileChange, long>> renameChangesToPreviousId = new List<KeyValuePair<FileChange, long>>();

                navigateEventsFromRoot(
                    innerExistingObjects: parentIdsToObjects,
                    innerPendingEvents: parentIdsToEvents,
                    parentPath: baseComparePath,
                    parentId: rootFolderObject.FileSystemObjectId,
                    innerPendingsWithSyncCounter: pendingsWithSyncCounter,
                    innerPendingsWithoutSyncCounter: pendingsWithoutSyncCounter,
                    innerObjectIdsToFullPath: objectIdsToFullPath,
                    innerRenameChangesToPreviousId: renameChangesToPreviousId,
                    thisAction: navigateEventsFromRoot);

                foreach (KeyValuePair<FileChange, long> renameToPreviousId in renameChangesToPreviousId)
                {
                    FilePath pullPathById;
                    if (objectIdsToFullPath.TryGetValue(renameToPreviousId.Value, out pullPathById))
                    {
                        renameToPreviousId.Key.OldPath = pullPathById.Copy();
                    }
                }

                Comparison<FileChange> eventIdAscending = new Comparison<FileChange>(delegate(FileChange first, FileChange second)
                    {
                        if (first.EventId > second.EventId)
                        {
                            return 1;
                        }

                        if (first.EventId < second.EventId)
                        {
                            return -1;
                        }

                        return 0;
                    });

                pendingsWithSyncCounter.Sort(eventIdAscending);
                pendingsWithoutSyncCounter.Sort(eventIdAscending);

                // Create a list for pending changes which need to be processed
                List<FileChange> changeList = new List<FileChange>();

                foreach (FileChange pendingWithSyncCounter in pendingsWithSyncCounter)
                {
                    changeList.Add(pendingWithSyncCounter);
                }

                foreach (FileChange pendingWithoutSyncCounter in pendingsWithoutSyncCounter)
                {
                    changeList.Add(pendingWithoutSyncCounter);

                    switch (pendingWithoutSyncCounter.Type)
                    {
                        case FileChangeType.Modified:
                        case FileChangeType.Created:
                            combinedIndexPlusChanges[pendingWithoutSyncCounter.NewPath.Copy()] = pendingWithoutSyncCounter.Metadata;

                            GenericHolder<bool> reverseDeletion;
                            if (pathDeletions.TryGetValue(pendingWithoutSyncCounter.NewPath, out reverseDeletion))
                            {
                                reverseDeletion.Value = false;
                            }
                            break;

                        case FileChangeType.Deleted:
                            if (combinedIndexPlusChanges.Remove(pendingWithoutSyncCounter.NewPath))
                            {
                                pathDeletions.Remove(pendingWithoutSyncCounter.NewPath);
                                pathDeletions.Add(pendingWithoutSyncCounter.NewPath, new GenericHolder<bool>(true));
                            }
                            break;

                        case FileChangeType.Renamed:
                            if (combinedIndexPlusChanges.ContainsKey(pendingWithoutSyncCounter.OldPath))
                            {
                                FilePathHierarchicalNode<FileMetadata> newRename;
                                CLError hierarchyError = combinedIndexPlusChanges.GrabHierarchyForPath(pendingWithoutSyncCounter.NewPath, out newRename, suppressException: true);
                                if (hierarchyError == null
                                    && newRename == null)
                                {
                                    FilePath copiedNewPath = pendingWithoutSyncCounter.NewPath.Copy();
                                    combinedIndexPlusChanges.Rename(pendingWithoutSyncCounter.OldPath, copiedNewPath);
                                    combinedIndexPlusChanges[pendingWithoutSyncCounter.NewPath.Copy()] = pendingWithoutSyncCounter.Metadata;

                                    FilePathHierarchicalNode<GenericHolder<bool>> newDeletion;
                                    CLError deletionHierarchyError = pathDeletions.GrabHierarchyForPath(pendingWithoutSyncCounter.NewPath, out newDeletion, true);

                                    if (deletionHierarchyError == null
                                        && newDeletion == null)
                                    {
                                        GenericHolder<bool> previousDeletion;
                                        if (pathDeletions.TryGetValue(pendingWithoutSyncCounter.NewPath, out previousDeletion))
                                        {
                                            previousDeletion.Value = false;
                                        }
                                        else
                                        {
                                            pathDeletions.Add(pendingWithoutSyncCounter.NewPath, new GenericHolder<bool>(false));
                                        }

                                        pathDeletions.Rename(pendingWithoutSyncCounter.OldPath, pendingWithoutSyncCounter.NewPath);
                                    }
                                }
                            }
                            break;
                    }
                }

                // Define DirectoryInfo at current path which will be traversed
                DirectoryInfo indexRootPath = new DirectoryInfo(indexedPath);

                // RecurseIndexDirectory both adds the new changes to the list that are found on disc
                // and returns a list of all paths traversed for comparison to the existing index
                string[] recursedIndexes =
                    (new[] { indexedPath })
                        .Concat(
                            RecurseIndexDirectory(
                                changeList,
                                indexPaths,
                                combinedIndexPlusChanges,
                                this.RemoveEventById,
                                indexedPath))
                        .ToArray();

                // Define a list to store indexes that previously existed in the last index, but were not found upon reindexing
                List<FileChange> possibleDeletions = new List<FileChange>();

                // Loop through the paths that previously existed in the index, but were not found when traversing the indexed path
                foreach (string deletedPath in
                    indexPaths.Select(currentIndex => currentIndex.Key.ToString())
                        .Except(recursedIndexes,
                            StringComparer.InvariantCulture))
                {
                    // For the path that existed previously in the index but is no longer found on disc, process as a deletion
                    FilePath deletedPathObject = deletedPath;
                    possibleDeletions.Add(new FileChange()
                    {
                        NewPath = deletedPath,
                        Type = FileChangeType.Deleted,
                        Metadata = indexPaths[deletedPath],
                        Direction = SyncDirection.To // detected that a file or folder was deleted locally, so Sync To to update server
                    });
                    pathDeletions.Remove(deletedPath);
                    pathDeletions.Add(deletedPath, new GenericHolder<bool>(true));
                }

                // Only add possible deletion if a parent wasn't already marked as deleted
                foreach (FileChange possibleDeletion in possibleDeletions)
                {
                    bool foundDeletedParent = false;
                    FilePath levelToCheck = possibleDeletion.NewPath.Parent;
                    while (levelToCheck.Contains(baseComparePath))
                    {
                        GenericHolder<bool> parentDeletion;
                        if (pathDeletions.TryGetValue(levelToCheck, out parentDeletion)
                            && parentDeletion.Value)
                        {
                            foundDeletedParent = true;
                            break;
                        }

                        levelToCheck = levelToCheck.Parent;
                    }
                    if (!foundDeletedParent)
                    {
                        changeList.Add(possibleDeletion);
                    }
                }

                foreach (FilePath initiallySyncedBadge in indexPaths.Keys)
                {
                    if (!FilePathComparer.Instance.Equals(initiallySyncedBadge, baseComparePath))
                    {
                        MessageEvents.SetPathState(this, new SetBadge(PathState.Synced, initiallySyncedBadge));
                    }
                }

                // Callback on initial index completion
                // (will process detected changes and begin normal folder monitor processing)
                indexCompletionCallback(indexPaths,
                    changeList);
            }
        }
        /// <summary>
        /// Used to navigate through a dictionary of parent ids to FileSystemObjects recursively to get full path at each node from the root to the deepest inner folder or file
        /// </summary>
        /// <param name="innerExistingObjects">Dictionary mapping parent ids to FileSystemObjects under that parent</param>
        /// <param name="parentPath">Full path corresponding to the current parent to search under</param>
        /// <param name="parentId">Id of the current parent to search under (used as the key to the innerExistingObject dictionary)</param>
        /// <param name="innerIndexPaths">First dictionary to store the built FileMetadata objects by the path recurse-built from the root</param>
        /// <param name="innerCombinedIndexPlusChanges">Second dictionary to store the built FileMetadata objects by the path recurse-built from the root</param>
        /// <param name="thisAction">The same delegate which will be called for recursion</param>
        private delegate void BuildIndex_navigatePathsFromRoot(Dictionary<long, List<FileSystemObject>> innerExistingObjects, FilePath parentPath, long parentId, FilePathDictionary<FileMetadata> innerIndexPaths, FilePathDictionary<FileMetadata> innerCombinedIndexPlusChanges, Dictionary<long, FilePath> innerObjectIdsToFullPath, BuildIndex_navigatePathsFromRoot thisAction);
        private delegate void BuildIndex_navigateEventsFromRoot(Dictionary<long, List<FileSystemObject>> innerExistingObjects, Dictionary<long, List<Event>> innerPendingEvents, FilePath parentPath, long parentId, List<FileChange> innerPendingsWithSyncCounter, List<FileChange> innerPendingsWithoutSyncCounter, Dictionary<long, FilePath> innerObjectIdsToFullPath, List<KeyValuePair<FileChange, long>> innerRenameChangesToPreviousId, BuildIndex_navigateEventsFromRoot thisAction);

        // Define comparer to use locally for file/folder metadata
        private static FileMetadataHashableComparer fileComparer = new FileMetadataHashableComparer();

        /// <summary>
        /// Process changes found on disc that are different from the initial index to produce FileChanges
        /// and return the enumeration of paths traversed; recurses on self for inner folders
        /// </summary>
        /// <param name="changeList">List of FileChanges to add/update with new changes</param>
        /// <param name="currentDirectory">Current directory to scan</param>
        /// <param name="indexPaths">Initial index</param>
        /// <param name="combinedIndexPlusChanges">Initial index plus all previous FileChanges in database and changes made up through current reindexing</param>
        /// <param name="AddEventCallback">Callback to fire if a database event needs to be added</param>
        /// <param name="uncoveredChanges">Optional list of changes which no longer have a corresponding local path, only set when self-recursing</param>
        /// <returns>Returns the list of paths traversed</returns>
        private static IEnumerable<string> RecurseIndexDirectory(List<FileChange> changeList, FilePathDictionary<FileMetadata> indexPaths, FilePathDictionary<FileMetadata> combinedIndexPlusChanges, Func<long, CLError> RemoveEventCallback, string currentDirectoryFullPath, FindFileResult currentDirectory = null, Dictionary<FilePath, LinkedList<FileChange>> uncoveredChanges = null)
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
            uncoveredChanges.Remove(currentDirectoryFullPath);

            // Create a list of the traversed paths at or below the current level
            List<string> filePathsFound = new List<string>();

            IEnumerable<FindFileResult> innerDirectories;
            IEnumerable<FindFileResult> innerFiles;
            if (currentDirectory == null)
            {
                bool rootNotFound;
                IList<FindFileResult> allInnerPaths = FindFileResult.RecursiveDirectorySearch(currentDirectoryFullPath,
                    (FileAttributes.Hidden// ignore hidden files
                        | FileAttributes.Offline// ignore offline files (data is not available on them)
                        | FileAttributes.System// ignore system files
                        | FileAttributes.Temporary),// ignore temporary files
                    out rootNotFound);

                if (rootNotFound)
                {
                    // the following should NOT be a HaltAll: TODO: add appropriate event bubbling to halt engine
                    MessageEvents.FireNewEventMessage(
                        "Unable to find Cloud directory at path: " + currentDirectoryFullPath,
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());

                    // This is a really bad error.  It means the connection to the file system is broken, and if we just ignore this error,
                    // sync will determine that there are no files in the SyncBox folder, and it will actually delete all of the files on the server.
                    // We have to stop this thread dead in its tracks, and do it in such a way that it is not recoverable.
                    CLError error = new Exception("Unable to find cloud directory at path: " + currentDirectoryFullPath);
                    error.LogErrors(_trace.TraceLocation, _trace.LogErrors);
                    _trace.writeToLog(1, "IndexingAgent: RecursiveIndexDirectory: ERROR: Exception: Msg: <{0}>.", error.errorDescription);

                    // root path required, blow up
                    throw new DirectoryNotFoundException("Unable to find Cloud directory at path: " + currentDirectoryFullPath);
                }

                innerDirectories = allInnerPaths.Where(currentInnerDirectory => currentInnerDirectory.IsFolder);
                innerFiles = allInnerPaths.Where(currentInnerFile => !currentInnerFile.IsFolder);
            }
            else
            {
                innerDirectories = currentDirectory.Children.Where(currentInnerDirectory => currentInnerDirectory.IsFolder);
                innerFiles = currentDirectory.Children.Where(currentInnerFile => !currentInnerFile.IsFolder);
            }

            try
            {
                // Loop through all subdirectories under the current directory
                foreach (FindFileResult subDirectory in innerDirectories)
                {
                    string subDirectoryFullPath = currentDirectoryFullPath + "\\" + subDirectory.Name;
                    FilePath subDirectoryPathObject = subDirectoryFullPath;

                    // Store current subdirectory path as traversed
                    filePathsFound.Add(subDirectoryFullPath);
                    // Create properties for the current subdirectory
                    FileMetadataHashableProperties compareProperties = new FileMetadataHashableProperties(true,
                        (subDirectory.LastWriteTime == null ? (Nullable<DateTime>)null : ((DateTime)subDirectory.LastWriteTime)),
                        (subDirectory.CreationTime == null ? (Nullable<DateTime>)null : ((DateTime)subDirectory.CreationTime)),
                        null);

                    // Grab the last metadata that matches the current directory path, if any
                    FilePathHierarchicalNode<FileMetadata> existingHierarchy;
                    CLError hierarchyError = combinedIndexPlusChanges.GrabHierarchyForPath(subDirectoryPathObject, out existingHierarchy, true);
                    // If there is no existing event, a directory was added
                    if (hierarchyError == null
                        && existingHierarchy == null)
                    {
                        FileMetadata newDirectoryMetadata = new FileMetadata()
                            {
                                HashableProperties = compareProperties
                            };

                        changeList.Add(new FileChange()
                        {
                            NewPath = subDirectoryPathObject,
                            Type = FileChangeType.Created,
                            Metadata = newDirectoryMetadata,
                            Direction = SyncDirection.To // detected that a folder was created locally, so Sync To to update server
                        });

                        combinedIndexPlusChanges.Add(subDirectoryPathObject, newDirectoryMetadata);
                    }

                    // Add the inner paths to the output list by recursing (which will also process inner changes)
                    filePathsFound.AddRange(RecurseIndexDirectory(changeList,
                        indexPaths,
                        combinedIndexPlusChanges,
                        RemoveEventCallback,
                        subDirectoryFullPath,
                        subDirectory,
                        uncoveredChanges));
                }

                // Loop through all files under the current directory
                foreach (FindFileResult currentFile in innerFiles)
                {
                    string currentFileFullPath = currentDirectoryFullPath + "\\" + currentFile.Name;
                    FilePath currentFilePathObject = currentFileFullPath;

                    // Remove file from list of changes which have not yet been traversed (since it has been traversed)
                    uncoveredChanges.Remove(currentFilePathObject);

                    // Add file path to traversed output list
                    filePathsFound.Add(currentFileFullPath);
                    // Find file properties
                    FileMetadataHashableProperties compareProperties = new FileMetadataHashableProperties(false,
                        (currentFile.LastWriteTime == null ? (Nullable<DateTime>)null : ((DateTime)currentFile.LastWriteTime).DropSubSeconds()),
                        (currentFile.CreationTime == null ? (Nullable<DateTime>)null : ((DateTime)currentFile.CreationTime).DropSubSeconds()),
                        currentFile.Size);

                    // Grab the last metadata that matches the current file path, if any
                    FileMetadata existingFileMetadata;
                    // If a change does not already exist for the current file path,
                    // check if file has changed since last index to process changes
                    if (combinedIndexPlusChanges.TryGetValue(currentFilePathObject, out existingFileMetadata))
                    {
                        // If the file has changed (different metadata), then process a file modification change
                        if (!fileComparer.Equals(compareProperties, existingFileMetadata.HashableProperties))
                        {
                            FileMetadata modifiedMetadata = new FileMetadata()
                                {
                                    ServerUid = existingFileMetadata.ServerUid,
                                    HashableProperties = compareProperties,
                                    Revision = existingFileMetadata.Revision/*,
                                    StorageKey = existingFileMetadata.StorageKey*/ // DO NOT copy StorageKey because this metadata is for a modified change which would therefore require a new StorageKey
                                };

                            changeList.Add(new FileChange()
                            {
                                NewPath = currentFilePathObject,
                                Type = FileChangeType.Modified,
                                Metadata = modifiedMetadata,
                                Direction = SyncDirection.To // detected that a file was modified locally, so Sync To to update server
                            });

                            combinedIndexPlusChanges[currentFilePathObject] = modifiedMetadata;
                        }
                    }
                    // else if index doesn't contain the current path, then the file has been created
                    else
                    {
                        FileMetadata fileCreatedMetadata = new FileMetadata()
                            {
                                HashableProperties = compareProperties//,
                                //LinkTargetPath = //Todo: needs to read target path
                            };

                        changeList.Add(new FileChange()
                        {
                            NewPath = currentFilePathObject,
                            Type = FileChangeType.Created,
                            Metadata = fileCreatedMetadata,
                            Direction = SyncDirection.To // detected that a file was created locally, so Sync To to update server
                        });

                        combinedIndexPlusChanges.Add(currentFilePathObject, fileCreatedMetadata);
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                if (outermostMethodCall)
                {
                    MessageEvents.FireNewEventMessage(
                        "Unable to scan files/folders in Cloud folder. Location not accessible:" + Environment.NewLine + ex.Message,
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());
                }
            }
            catch { }

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
                        else if (currentUncoveredChange.Value.Direction == SyncDirection.To)
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

        #region dispose
        
        #region IDisposable members
        // Standard IDisposable implementation based on MSDN System.IDisposable
        ~IndexingAgent()
        {
            Dispose(false);
        }
        // Standard IDisposable implementation based on MSDN System.IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
        
        // Standard IDisposable implementation based on MSDN System.IDisposable
        private void Dispose(bool disposing)
        {
            lock (this)
            {
                // Run dispose on inner managed objects based on disposing condition
                if (disposing)
                {
                    lock (changeEnumsLocker)
                    {
                        if (changeEnums != null)
                        {
                            changeEnums.Clear();
                            changeEnums = null;
                        }

                        if (changeEnumsBackward != null)
                        {
                            changeEnumsBackward.Clear();
                            changeEnumsBackward = null;
                        }
                    }
                    
                    ExternalSQLLocker.Dispose();
                }
            }
        }
        #endregion
    }
}