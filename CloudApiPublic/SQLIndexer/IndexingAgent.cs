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
        private static readonly CLTrace _trace = CLTrace.Instance;
        // store the path that represents the root of indexing
        private string indexedPath = null;
        private readonly CLSyncbox syncbox;
        private long rootFileSystemObjectId = 0;

        #region SQLite
        private readonly string indexDBLocation;
        private const string indexDBPassword = "Q29weXJpZ2h0Q2xvdWQuY29tQ3JlYXRlZEJ5RGF2aWRCcnVjaw=="; // <-- if you change this password, you will likely break all clients with older databases
        private const string indexScriptsResourceFolder = ".SQLIndexer.IndexDBScripts.";
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
        /// <param name="syncbox">Syncbox to index</param>
        /// <returns>Returns the error that occurred during creation, if any</returns>
        public static CLError CreateNewAndInitialize(out IndexingAgent newIndexer, CLSyncbox syncbox)
        {
            // Fill in output with constructor
            IndexingAgent newAgent;
            try
            {
                newIndexer = newAgent = new IndexingAgent(syncbox); // this double instance setting is required for some reason to prevent a "does not exist in the current context" compiler error
            }
            catch (Exception ex)
            {
                newIndexer = Helpers.DefaultForType<IndexingAgent>();
                return ex;
            }

            try
            {
                newIndexer.InitializeDatabase(syncbox.CopiedSettings.SyncRoot);
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
            try
            {
                if (eventId <= 0)
                {
                    throw new ArgumentException("eventId cannot be equal to or less than zero");
                }

                using (ISQLiteConnection indexDB = CreateAndOpenCipherConnection())
                {
                    bool resultFound = false;
                    queryResult = null;
                    isPending = false;

                    foreach (Event existingEvent in SqlAccessor<Event>.SelectResultSet(
                            indexDB,
                            "SELECT " +
                                SqlAccessor<Event>.GetSelectColumns() + ", " +
                                SqlAccessor<FileSystemObject>.GetSelectColumns("FileSystemObject") + ", " +
                                SqlAccessor<FileSystemObject>.GetSelectColumns("Previous", "Previouses") +
                                " FROM Events" +
                                " INNER JOIN FileSystemObjects ON Events.EventId = FileSystemObjects.EventId" +
                                " LEFT OUTER JOIN FileSystemObjects Previouses ON Events.PreviousId = FileSystemObjects.FileSystemObjectId" +
                                " WHERE Events.EventId = ?" +
                                " ORDER BY" +
                                " CASE WHEN FileSystemObjects.EventOrder IS NULL" +
                                " THEN 0" +
                                " ELSE FileSystemObjects.EventOrder" +
                                " END DESC",
                            new[]
                            {
                                "FileSystemObject",
                                "Previous"
                            },
                            selectParameters: Helpers.EnumerateSingleItem(eventId)))
                    {
                        if (resultFound)
                        {
                            status = FileChangeQueryStatus.ErrorMultipleResults;
                            return SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Multiple objects found for given eventId");
                        }

                        resultFound = true;

                        queryResult = new FileChange()
                        {
                            Direction = (existingEvent.SyncFrom ? SyncDirection.From : SyncDirection.To),
                            EventId = existingEvent.EventId,
                            Metadata = new FileMetadata()
                            {
                                EventTime = new DateTime(existingEvent.FileSystemObject.EventTimeUTCTicks, DateTimeKind.Utc),
                                HashableProperties = new FileMetadataHashableProperties(
                                    existingEvent.FileSystemObject.IsFolder,
                                    (existingEvent.FileSystemObject.LastTimeUTCTicks == null
                                        ? (Nullable<DateTime>)null
                                        : new DateTime((long)existingEvent.FileSystemObject.LastTimeUTCTicks, DateTimeKind.Utc)),
                                    (existingEvent.FileSystemObject.CreationTimeUTCTicks == null
                                        ? (Nullable<DateTime>)null
                                        : new DateTime((long)existingEvent.FileSystemObject.CreationTimeUTCTicks, DateTimeKind.Utc)),
                                    existingEvent.FileSystemObject.Size),
                                IsShare = existingEvent.FileSystemObject.IsShare,
                                MimeType = existingEvent.FileSystemObject.MimeType,
                                Permissions = (existingEvent.FileSystemObject.Permissions == null
                                    ? (Nullable<POSIXPermissions>)null
                                    : (POSIXPermissions)((int)existingEvent.FileSystemObject.Permissions)),
                                Revision = existingEvent.FileSystemObject.Revision,
                                ServerUid = existingEvent.FileSystemObject.ServerUid,
                                StorageKey = existingEvent.FileSystemObject.StorageKey,
                                Version = existingEvent.FileSystemObject.Version
                            },
                            NewPath = existingEvent.FileSystemObject.CalculatedFullPath,
                            OldPath = (existingEvent.Previous == null
                                ? null
                                : existingEvent.Previous.CalculatedFullPath),
                            Type = changeEnums[existingEvent.FileChangeTypeEnumId]
                        };
                        queryResult.SetMD5(existingEvent.FileSystemObject.MD5);
                        isPending = existingEvent.FileSystemObject.Pending;
                    }

                    if (!resultFound)
                    {
                        status = FileChangeQueryStatus.ErrorNotFound;
                    }
                    else
                    {
                        status = FileChangeQueryStatus.Success;
                    }
                }
            }
            catch (Exception ex)
            {
                queryResult = Helpers.DefaultForType<FileChange>();
                isPending = Helpers.DefaultForType<bool>();
                status = FileChangeQueryStatus.ErrorUnknown;
                return ex;
            }
            return null;
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

        public void SwapOrderBetweenTwoEventIds(long eventIdA, long eventIdB, SQLTransactionalBase requiredTransaction)
        {
            if (requiredTransaction == null)
            {
                throw new NullReferenceException("requiredTransaction cannot be null");
            }

            SQLTransactionalImplementation castTransaction = requiredTransaction as SQLTransactionalImplementation;

            if (castTransaction == null)
            {
                throw new NullReferenceException("existingTransaction is not implemented as private derived type. It should be retrieved via method GetNewTransaction method. Creating a new transaction instead which will be committed immediately.");
            }
            if (!(eventIdA > 0))
            {
                throw new ArgumentException("eventIdA was not the positive integer created from adding a new Event to the databse");
            }
            if (!(eventIdB > 0))
            {
                throw new ArgumentException("eventIdB was not the positive integer created from adding a new Event to the database");
            }
            if (eventIdA == eventIdB)
            {
                throw new ArgumentException("Cannot swap two events with the same ID");
            }

            FileSystemObject eventAObject = null;
            FileSystemObject eventBObject = null;

            foreach (FileSystemObject matchedEventObject in SqlAccessor<FileSystemObject>.SelectResultSet(
                castTransaction.sqlConnection,
                "SELECT *" +
                    "FROM FileSystemObjects " +
                    "WHERE FileSystemObjects.EventId = ? " + // <-- parameter 1
                    "OR FileSystemObjects.EventId = ?", // <-- paremeter 2
                transaction: castTransaction.sqlTransaction,
                selectParameters: new[] { eventIdA, eventIdB }))
            {
                if (matchedEventObject.EventId == eventIdA)
                {
                    if (eventAObject != null)
                    {
                        throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Query for FileSystemObjects by eventIdA and eventIdB returned more than one Event for eventIdA");
                    }

                    eventAObject = matchedEventObject;
                }
                else if (matchedEventObject.EventId == eventIdB)
                {
                    if (eventBObject != null)
                    {
                        throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Query for FileSystemObjects by eventIdA and eventIdB returned more than one Event for eventIdB");
                    }

                    eventBObject = matchedEventObject;
                }
                else
                {
                    throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Query for FileSystemObjects by eventIdA and eventIdB returned an event which matches neither ID");
                }
            }

            if (eventAObject == null)
            {
                throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Query for FileSystemObjects by eventIdA and eventIdB did not return any Event for eventIdA");
            }
            if (eventBObject == null)
            {
                throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Query for FileSystemObjects by eventIdA and eventIdB did not return any Event for eventIdB");
            }

            using (ISQLiteCommand swapEventOrders = castTransaction.sqlConnection.CreateCommand())
            {
                swapEventOrders.Transaction = castTransaction.sqlTransaction;

                swapEventOrders.CommandText = "UPDATE FileSystemObjects " +
                    "SET EventOrder = ? " + // <-- parameter 1
                    "WHERE FileSystemObjectId = ?; " + // <-- parameter 2
                    "UPDATE FileSystemObjects " +
                    "SET EventOrder = ? " + // <-- parameter 3
                    "WHERE FileSystemObjectId = ?;";// <-- parameter 4

                ISQLiteParameter eventBOrderParam = swapEventOrders.CreateParameter();
                eventBOrderParam.Value = eventBObject.EventOrder;
                swapEventOrders.Parameters.Add(eventBOrderParam);

                ISQLiteParameter eventAIdParam = swapEventOrders.CreateParameter();
                eventAIdParam.Value = eventAObject.FileSystemObjectId;
                swapEventOrders.Parameters.Add(eventAIdParam);

                ISQLiteParameter eventAOrderParam = swapEventOrders.CreateParameter();
                eventAOrderParam.Value = eventAObject.EventOrder;
                swapEventOrders.Parameters.Add(eventAOrderParam);

                ISQLiteParameter eventBIdParam = swapEventOrders.CreateParameter();
                eventBIdParam.Value = eventBObject.FileSystemObjectId;
                swapEventOrders.Parameters.Add(eventBIdParam);

                swapEventOrders.ExecuteNonQuery();
            }
        }

        public CLError GetCalculatedFullPathByServerUid(string serverUid, out string calculatedFullPath, Nullable<long> excludedEventId = null)
        {
            try
            {
                if (serverUid == null)
                {
                    throw new NullReferenceException("serverUid cannot be null");
                }

                using (ISQLiteConnection indexDB = CreateAndOpenCipherConnection())
                {
                    // prefers the latest rename which is pending,
                    // otherwise prefers non-pending,
                    // last take most recent event
                    if (!SqlAccessor<object>.TrySelectScalar<string>(
                        indexDB,
                        "SELECT FileSystemObjects.CalculatedFullPath " +
                            "FROM FileSystemObjects " +
                            "INNER JOIN (SELECT ? AS ExcludedEventId) ConstantJoin " + // <-- parameter 1
                            "LEFT OUTER JOIN Events ON FileSystemObjects.EventId = Events.EventId " +
                            "WHERE FileSystemObjects.ServerUid = ? " + // <-- parameter 2
                            "AND (ConstantJoin.ExcludedEventId IS NULL OR FileSystemObjects.EventId IS NULL OR ConstantJoin.ExcludedEventId <> FileSystemObjects.EventId) " +
                            "ORDER BY " +
                            "CASE WHEN FileSystemObjects.EventId IS NOT NULL " +
                            "AND Events.FileChangeTypeEnumId = " + changeEnumsBackward[FileChangeType.Renamed].ToString() +
                            " AND FileSystemObjects.Pending = 1 " +
                            "THEN 0 " +
                            "ELSE 1 " +
                            "END ASC, " +
                            "FileSystemObjects.Pending ASC, " +
                            "CASE WHEN FileSystemObjects.EventOrder IS NULL " +
                            "THEN 0 " +
                            "ELSE FileSystemObjects.EventOrder " +
                            "END DESC " +
                            "LIMIT 1",
                        out calculatedFullPath,
                        selectParameters: new[] { excludedEventId, (object)serverUid }))
                    {
                        calculatedFullPath = null;
                    }
                }
            }
            catch (Exception ex)
            {
                calculatedFullPath = Helpers.DefaultForType<string>();
                return ex;
            }
            return null;
        }

        public CLError GetServerUidByNewPath(string newPath, out string serverUid)
        {
            try
            {
                if (newPath == null)
                {
                    throw new NullReferenceException("newPath cannot be null");
                }

                using (ISQLiteConnection indexDB = CreateAndOpenCipherConnection())
                {
                    // prefers latest event even if pending
                    if (!SqlAccessor<object>.TrySelectScalar<string>(
                        indexDB,
                        "SELECT FileSystemObjects.ServerUid " +
                        "FROM FileSystemObjects " +
                        "WHERE FileSystemObjects.CalculatedFullPath = ? " +
                        "ORDER BY " +
                        "CASE WHEN FileSystemObjects.EventOrder IS NULL " +
                        "THEN 0 " +
                        "ELSE FileSystemObjects.EventOrder " +
                        "END DESC " +
                        "LIMIT 1",
                        out serverUid,
                        selectParameters: Helpers.EnumerateSingleItem(newPath)))
                    {
                        serverUid = null;
                    }
                }
            }
            catch (Exception ex)
            {
                serverUid = Helpers.DefaultForType<string>();
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
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    throw new NullReferenceException("path cannot be null");
                }

                using (ISQLiteConnection indexDB = CreateAndOpenCipherConnection())
                {
                    FileSystemObject existingNonPending = SqlAccessor<FileSystemObject>.SelectResultSet(
                            indexDB,
                            "SELECT * " +
                                "FROM FileSystemObjects " +
                                "WHERE CalculatedFullPath = ? " + // <-- parameter 1
                                (revision == null
                                    ? string.Empty
                                    : "AND Revision = ?") + // <-- conditional parameter 2
                                "ORDER BY " +
                                "CASE WHEN FileSystemObjects.EventOrder IS NULL " +
                                "THEN 0 " +
                                "ELSE FileSystemObjects.EventOrder " +
                                "END DESC " +
                                "LIMIT 1",
                                selectParameters: (revision == null ? Helpers.EnumerateSingleItem(path) : new[] { path, revision }))
                        .SingleOrDefault();

                    if (existingNonPending == null)
                    {
                        throw new KeyNotFoundException("Unable to find existing FileSystemObject by path" + (revision == null ? string.Empty : " and revision"));
                    }

                    metadata = new FileMetadata()
                    {
                        EventTime = new DateTime(existingNonPending.EventTimeUTCTicks, DateTimeKind.Utc),
                        HashableProperties = new FileMetadataHashableProperties(
                            existingNonPending.IsFolder,
                            (existingNonPending.LastTimeUTCTicks == null
                                ? (Nullable<DateTime>)null
                                : new DateTime((long)existingNonPending.LastTimeUTCTicks, DateTimeKind.Utc)),
                            (existingNonPending.CreationTimeUTCTicks == null
                                ? (Nullable<DateTime>)null
                                : new DateTime((long)existingNonPending.CreationTimeUTCTicks, DateTimeKind.Utc)),
                            existingNonPending.Size),
                        IsShare = existingNonPending.IsShare,
                        MimeType = existingNonPending.MimeType,
                        Permissions = (existingNonPending.Permissions == null
                            ? (Nullable<POSIXPermissions>)null
                            : (POSIXPermissions)((int)existingNonPending.Permissions)),
                        Revision = existingNonPending.Revision,
                        ServerUid = existingNonPending.ServerUid,
                        StorageKey = existingNonPending.StorageKey,
                        Version = existingNonPending.Version
                    };
                }
            }
            catch (Exception ex)
            {
                metadata = Helpers.DefaultForType<FileMetadata>();
                return ex;
            }
            return null;
        }

        ///// <summary>
        ///// Retrieves all unprocessed events that occurred since the last sync
        ///// </summary>
        ///// <param name="changeEvents">Outputs the unprocessed events</param>
        ///// <returns>Returns an error that occurred filling the unprocessed events, if any</returns>
        //public CLError GetPendingEvents(out List<KeyValuePair<FilePath, FileChange>> changeEvents)
        //{
        //    ExternalSQLLocker.EnterReadLock();
        //    try
        //    {
        //        using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(this.indexDBLocation)))
        //        {
        //            // Create the output list
        //            changeEvents = new List<KeyValuePair<FilePath, FileChange>>();

        //            // Loop through all the events in the database after the last sync (if any)
        //            foreach (Event currentChange in
        //                SqlAccessor<Event>
        //                    .SelectResultSet(indexDB,
        //                        "SELECT " +
        //                        SqlAccessor<Event>.GetSelectColumns() + ", " +
        //                        SqlAccessor<FileSystemObject>.GetSelectColumns(FileSystemObject.Name) +
        //                        "FROM [Events] " +
        //                        "INNER JOIN [FileSystemObjects] ON [Events].[EventId] = [FileSystemObjects].[EventId] " +
        //                        "WHERE [FileSystemObjects].[SyncCounter] IS NULL " +
        //                        "ORDER BY [Events].[EventId]",
        //                        new string[]
        //                        {
        //                            FileSystemObject.Name
        //                        }))
        //            {
        //                // For each event since the last sync (if any), add to the output dictionary
        //                changeEvents.Add(new KeyValuePair<FilePath, FileChange>(currentChange.FileSystemObject.Path,
        //                    new FileChange()
        //                    {
        //                        NewPath = currentChange.FileSystemObject.Path,
        //                        OldPath = currentChange.PreviousPath,
        //                        Type = changeEnums[currentChange.FileChangeTypeEnumId],
        //                        Metadata = new FileMetadata()
        //                        {
        //                            // TODO: add server id
        //                            HashableProperties = new FileMetadataHashableProperties(currentChange.FileSystemObject.IsFolder,
        //                                currentChange.FileSystemObject.LastTime,
        //                                currentChange.FileSystemObject.CreationTime,
        //                                currentChange.FileSystemObject.Size),
        //                            Revision = currentChange.FileSystemObject.Revision,
        //                            StorageKey = currentChange.FileSystemObject.StorageKey,
        //                            LinkTargetPath = currentChange.FileSystemObject.TargetPath
        //                        },
        //                        Direction = (currentChange.SyncFrom ? SyncDirection.From : SyncDirection.To)
        //                    }));
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        changeEvents = Helpers.DefaultForType<List<KeyValuePair<FilePath, FileChange>>>();
        //        return ex;
        //    }
        //    finally
        //    {
        //        ExternalSQLLocker.ExitReadLock();
        //    }
        //    return null;
        //}

        /// <summary>
        /// Adds an unprocessed change since the last sync as a new event to the database,
        /// EventId property of the input event is set after database update
        /// </summary>
        /// <param name="newEvents">Change to add</param>
        /// <returns>Returns error that occurred when adding the event to database, if any</returns>
        public CLError AddEvents(IEnumerable<FileChange> newEvents, SQLTransactionalBase existingTransaction = null)
        {
            return AddEvents(null, newEvents, existingTransaction);
        }
        private CLError AddEvents(Nullable<long> syncCounter, IEnumerable<FileChange> newEvents, SQLTransactionalBase existingTransaction)
        {
            CLError toReturn = null;
            SQLTransactionalImplementation castTransaction = existingTransaction as SQLTransactionalImplementation;
            if (existingTransaction != null
                && castTransaction == null)
            {
                try
                {
                    throw new NullReferenceException("existingTransaction is not implemented as private derived type. It should be retrieved via method GetNewTransaction method. Creating a new transaction instead which will be committed immediately.");
                }
                catch (Exception ex)
                {
                    toReturn += ex;
                }
            }

            bool inputTransactionSet = castTransaction != null;
            try
            {
                // Ensure input parameter is set
                if (newEvents == null)
                {
                    throw new NullReferenceException("newEvents cannot be null");
                }

                FileChange[] newEventsArray;
                {
                    List<FileChange> newEventsList = new List<FileChange>();
                    foreach (FileChange currentEvent in newEvents)
                    {
                        if (currentEvent.Metadata == null)
                        {
                            throw new NullReferenceException("The Metadata property of every newEvent cannot be null");
                        }

                        newEventsList.Add(currentEvent);
                    }
                    newEventsArray = newEventsList.ToArray();
                }

                if (castTransaction == null)
                {
                    ISQLiteConnection indexDB;
                    castTransaction = new SQLTransactionalImplementation(
                        indexDB = CreateAndOpenCipherConnection(),
                        indexDB.BeginTransaction(System.Data.IsolationLevel.Serializable));
                }

                int lastHighestChangeIndex;
                int currentChangeIndex = 0;

                // template is only used to define the type structure needed to add a FileChange to the database
                // the values from this specific object instance are never copied anywhere and are never read
                var batchedItemTemplate = new
                {
                    change = (FileChange)null, // force FileChange type
                    parentFolderId = 0L, // force signed 64-bit integer type
                    previousId = (Nullable<long>)null // force nullable of signed 64-bit integer type
                };

                //// template is only used to define the type structure needed to track paths as the result of FileChanges;
                //// the values from this specific object instance are never copied anywhere and are never read
                //var batchedPathTrackingValueTemplate = new
                //    {
                //        previousPath = (FilePath)null, // force FilePath type; null value will represent that the object had not already existed in the database (such as on a create event in the same batch)
                //        objectId = 0L // force signed 64-bit integer type
                //    };

                FilePath indexedPathObject = indexedPath;

                do
                {
                    lastHighestChangeIndex = currentChangeIndex;

                    var currentBatchToAddList = Helpers.CreateEmptyListFromTemplate(batchedItemTemplate);
                    FilePathDictionary<object> currentBatchTrackPathChanges;
                    CLError createCurrentBatchTrackPathChangesError = FilePathDictionary<object>.CreateAndInitialize(
                        indexedPathObject,
                        out currentBatchTrackPathChanges);
                    //var currentBatchTrackPathChangesPair = Helpers.CreateEmptyFilePathDictionaryFromTemplate(
                    //    indexedPathObject,
                    //    batchedPathTrackingValueTemplate);

                    if (createCurrentBatchTrackPathChangesError != null)
                    {
                        throw new AggregateException("Unable to make a FilePathDictionary with current indexedPath", createCurrentBatchTrackPathChangesError.Exceptions);
                    }
                    //if (currentBatchTrackPathChangesPair.Value != null)
                    //{
                    //    throw new AggregateException("Unable to make a FilePathDictionary from anonymous type template batchedPathTrackingValueTemplate with current indexedPath", currentBatchTrackPathChangesPair.Value.Exceptions);
                    //}

                    //var currentBatchTrackPathChanges = currentBatchTrackPathChangesPair.Key;

                    for (/* currentChangeIndex defined above */ ; currentChangeIndex < newEventsArray.Length; currentChangeIndex++)
                    {
                        try
                        {
                            FileChange currentObjectToBatch = newEventsArray[currentChangeIndex];

                            if (currentObjectToBatch.DoNotAddToSQLIndex) // skip adding current event since it was marked DoNotAddToSQLIndex
                            {
                                continue;
                            }

                            long parentFolderId;
                            Nullable<long> previousId;

                            // prefers the latest rename which is pending,
                            // otherwise prefers non-pending,
                            // last take most recent event
                            const string objectIdByPathSelectPart1 =
                                "SELECT FileSystemObjects.FileSystemObjectId " +
                                    "FROM FileSystemObjects " +
                                    "LEFT OUTER JOIN Events ON FileSystemObjects.EventId = Events.EventId " +
                                    "WHERE FileSystemObjects.CalculatedFullPath = ? " + // <-- parameter 1
                                    "ORDER BY " +
                                    "CASE WHEN FileSystemObjects.EventId IS NOT NULL " +
                                    "AND Events.FileChangeTypeEnumId = ";
                            // parts to be seperated by: changeEnumsBackward[FileChangeType.Renamed].ToString()
                            const string objectIdByPathSelectPart2 =
                                " AND FileSystemObjects.Pending = 1 " +
                                    "THEN 0 " +
                                    "ELSE 1 " +
                                    "END ASC, " +
                                    "FileSystemObjects.Pending ASC, " +
                                    "CASE WHEN FileSystemObjects.EventOrder IS NULL " +
                                    "THEN 0 " +
                                    "ELSE FileSystemObjects.EventOrder " +
                                    "END DESC " +
                                    "LIMIT 1";

                            const string missingParentErrorMessage =
                                "Unable to add a new FileSystemObject without a parent folder ID";

                            const string missingPreviousErrorMessage =
                                "Unable to add a rename Event without a previous ID";

                            bool foundExistingPathInSearch = false;
                            //long searchResultObjectId = 0;
                            //FilePath searchResultPreviousPath = null;

                            FilePath currentObjectParentPathSearch = currentObjectToBatch.NewPath.Parent;
                            while (!FilePathComparer.Instance.Equals(currentObjectParentPathSearch, indexedPathObject))
                            {
                                var pathSearchResult = Helpers.DictionaryTryGetValue(currentBatchTrackPathChanges, currentObjectParentPathSearch);

                                if (pathSearchResult.Success)
                                {
                                    foundExistingPathInSearch = true;
                                    //searchResultObjectId = pathSearchResult.Value.objectId;
                                    //searchResultPreviousPath = pathSearchResult.Value.previousPath;

                                    break;
                                }

                                currentObjectParentPathSearch = currentObjectParentPathSearch.Parent;
                            }

                            if (foundExistingPathInSearch)
                            {
                                // existing event along the current change's parent paths has not yet been committed to database so we break this batch until the previous batch is added
                                break;

                                //if (searchResultPreviousPath == null)
                                //{
                                //    // existing event along the current change's parent paths has not yet been committed to database so we break this batch until the previous batch is added
                                //    break;
                                //}
                                //else if (FilePathComparer.Instance.Equals(currentObjectParentPathSearch, currentObjectToBatch.NewPath.Parent))
                                //{
                                //    parentFolderId = (long)searchResultObjectId;
                                //}
                                //else
                                //{
                                //    FilePath renamedNewPathParent = currentObjectToBatch.NewPath.Parent.Copy();
                                //    FilePath.ApplyRename(renamedNewPathParent, currentObjectParentPathSearch, searchResultPreviousPath);

                                //    if (!SqlAccessor<object>.TrySelectScalar(
                                //        castTransaction.sqlConnection,
                                //        objectIdByPathSelect,
                                //        out parentFolderId,
                                //        castTransaction.sqlTransaction,
                                //        selectParameters: Helpers.EnumerateSingleItem(renamedNewPathParent)))
                                //    {
                                //        throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, missingParentErrorMessage);
                                //    }
                                //}
                            }
                            else if (!SqlAccessor<object>.TrySelectScalar(
                                castTransaction.sqlConnection,
                                objectIdByPathSelectPart1 + changeEnumsBackward[FileChangeType.Renamed].ToString() + objectIdByPathSelectPart2,
                                out parentFolderId,
                                castTransaction.sqlTransaction,
                                selectParameters: Helpers.EnumerateSingleItem(currentObjectToBatch.NewPath.Parent.ToString())))
                            {
                                throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, missingParentErrorMessage);
                            }

                            if (currentObjectToBatch.OldPath == null)
                            {
                                previousId = null;
                            }
                            else
                            {
                                foundExistingPathInSearch = false;
                                //searchResultObjectId = 0;
                                //searchResultPreviousPath = null;

                                currentObjectParentPathSearch = currentObjectToBatch.OldPath;
                                while (!FilePathComparer.Instance.Equals(currentObjectParentPathSearch, indexedPathObject))
                                {
                                    var pathSearchResult = Helpers.DictionaryTryGetValue(currentBatchTrackPathChanges, currentObjectParentPathSearch);

                                    if (pathSearchResult.Success)
                                    {
                                        foundExistingPathInSearch = true;
                                        //searchResultObjectId = pathSearchResult.Value.objectId;
                                        //searchResultPreviousPath = pathSearchResult.Value.previousPath;

                                        break;
                                    }

                                    currentObjectParentPathSearch = currentObjectParentPathSearch.Parent;
                                }

                                long previousIdNotNull;

                                if (foundExistingPathInSearch)
                                {
                                    // existing event along the current change's previous path and its parents has not yet been committed to database so we break this batch until the previous batch is added
                                    break;

                                    //if (searchResultPreviousPath == null)
                                    //{
                                    //    // existing event along the current change's previous path and its parents has not yet been committed to database so we break this batch until the previous batch is added
                                    //    break;
                                    //}
                                    //else if (FilePathComparer.Instance.Equals(currentObjectParentPathSearch, currentObjectToBatch.OldPath))
                                    //{
                                    //    previousId = searchResultObjectId;
                                    //}
                                    //else
                                    //{
                                    //    FilePath renamedNewPathParent = currentObjectToBatch.OldPath.Copy();
                                    //    FilePath.ApplyRename(renamedNewPathParent, currentObjectParentPathSearch, searchResultPreviousPath);

                                    //    if (!SqlAccessor<object>.TrySelectScalar(
                                    //        castTransaction.sqlConnection,
                                    //        objectIdByPathSelect,
                                    //        out previousIdNotNull,
                                    //        castTransaction.sqlTransaction,
                                    //        selectParameters: Helpers.EnumerateSingleItem(renamedNewPathParent)))
                                    //    {
                                    //        throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, missingPreviousErrorMessage);
                                    //    }

                                    //    previousId = previousIdNotNull;
                                    //}
                                }
                                else if (!SqlAccessor<object>.TrySelectScalar(
                                    castTransaction.sqlConnection,
                                    objectIdByPathSelectPart1 + changeEnumsBackward[FileChangeType.Renamed].ToString() + objectIdByPathSelectPart2,
                                    out previousIdNotNull,
                                    castTransaction.sqlTransaction,
                                    selectParameters: Helpers.EnumerateSingleItem(currentObjectToBatch.OldPath.ToString())))
                                {
                                    throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, missingPreviousErrorMessage);
                                }
                                else
                                {
                                    previousId = previousIdNotNull;
                                }
                            }

                            currentBatchToAddList.Add(new
                            {
                                change = currentObjectToBatch,
                                parentFolderId = parentFolderId,
                                previousId = previousId
                            });

                            switch (currentObjectToBatch.Type)
                            {
                                case FileChangeType.Created:
                                    currentBatchTrackPathChanges.Add(
                                        currentObjectToBatch.NewPath,
                                        new object());
                                    //new
                                    //{
                                    //    previousPath = (FilePath)null,
                                    //    objectId = 0L
                                    //});
                                    break;

                                case FileChangeType.Deleted:
                                    currentBatchTrackPathChanges.Remove(currentObjectToBatch.NewPath);
                                    break;

                                case FileChangeType.Renamed:
                                    var existingRenamePair = Helpers.DictionaryTryGetValue(currentBatchTrackPathChanges, currentObjectToBatch.OldPath);

                                    FilePathHierarchicalNode<object> oldPathHierarchy;
                                    CLError oldPathHierarchyError = currentBatchTrackPathChanges.GrabHierarchyForPath(currentObjectToBatch.OldPath, out oldPathHierarchy, suppressException: true);

                                    if (oldPathHierarchyError == null
                                        && oldPathHierarchy != null)
                                    {
                                        currentBatchTrackPathChanges.Rename(currentObjectToBatch.OldPath, currentObjectToBatch.NewPath);
                                    }

                                    if (!existingRenamePair.Success)
                                    {
                                        currentBatchTrackPathChanges.Add(
                                            currentObjectToBatch.NewPath,
                                            new object());
                                        //new
                                        //{
                                        //    previousPath = currentObjectToBatch.OldPath,
                                        //    objectId = ?? <-- this is where I realized there is nothing to track forward through renames except the path key in the dictionary
                                        //});
                                    }
                                    break;

                                //case FileChangeType.Modified: // <-- don't do anything with modified since it doesn't affect the FileSystemObjectId at any path
                            }
                        }
                        catch (Exception ex)
                        {
                            toReturn += ex;
                        }
                    }

                    List<Event> eventsToAdd = new List<Event>();
                    Guid eventGroup = Guid.NewGuid();
                    int eventCounter = 0;
                    Dictionary<int, KeyValuePair<FileChange, GenericHolder<long>>> orderToChange = new Dictionary<int, KeyValuePair<FileChange, GenericHolder<long>>>();

                    // If change is marked for adding to SQL,
                    // then process database addition
                    foreach (var newEvent in currentBatchToAddList)
                    {
                        eventCounter++;
                        orderToChange.Add(eventCounter, new KeyValuePair<FileChange, GenericHolder<long>>(newEvent.change, new GenericHolder<long>()));

                        DateTime storeCreationTimeUTC;
                        DateTime storeLastTimeUTC;

                        byte[] getMD5 = newEvent.change.MD5;

                        // Define the new event to add for the unprocessed change
                        eventsToAdd.Add(new Event()
                        {
                            FileChangeTypeCategoryId = changeCategoryId,
                            FileChangeTypeEnumId = changeEnumsBackward[newEvent.change.Type],
                            FileSystemObject = new FileSystemObject()
                            {
                                CreationTimeUTCTicks = ((newEvent.change.Metadata.HashableProperties.CreationTime.Ticks == FileConstants.InvalidUtcTimeTicks
                                        || (storeCreationTimeUTC = newEvent.change.Metadata.HashableProperties.CreationTime.ToUniversalTime()).Ticks == FileConstants.InvalidUtcTimeTicks)
                                    ? (Nullable<long>)0
                                    : storeCreationTimeUTC.Ticks),
                                EventTimeUTCTicks = DateTime.UtcNow.Ticks,
                                IsFolder = newEvent.change.Metadata.HashableProperties.IsFolder,
                                IsShare = newEvent.change.Metadata.IsShare,
                                LastTimeUTCTicks = ((newEvent.change.Metadata.HashableProperties.LastTime.Ticks == FileConstants.InvalidUtcTimeTicks
                                        || (storeLastTimeUTC = newEvent.change.Metadata.HashableProperties.LastTime.ToUniversalTime()).Ticks == FileConstants.InvalidUtcTimeTicks)
                                    ? (Nullable<long>)0
                                    : storeLastTimeUTC.Ticks),
                                MD5 = getMD5,
                                MimeType = newEvent.change.Metadata.MimeType,
                                Name = newEvent.change.NewPath.Name,
                                ParentFolderId = newEvent.parentFolderId,
                                Pending = true,
                                Permissions = (newEvent.change.Metadata.Permissions == null ? (Nullable<int>)null : (int)((POSIXPermissions)newEvent.change.Metadata.Permissions)),
                                Revision = newEvent.change.Metadata.Revision,
                                //ServerName = newEvent.change.ServerPath // <-- need to add server paths to FileChange
                                ServerUid = newEvent.change.Metadata.ServerUid,
                                Size = newEvent.change.Metadata.HashableProperties.Size,
                                StorageKey = newEvent.change.Metadata.StorageKey,
                                SyncCounter = syncCounter,
                                Version = newEvent.change.Metadata.Version
                            },
                            GroupId = eventGroup,
                            GroupOrder = eventCounter,
                            PreviousId = newEvent.previousId,
                            SyncFrom = (newEvent.change.Direction == SyncDirection.From)
                        });
                    }

                    if (eventsToAdd.Count > 0)
                    {
                        SqlAccessor<Event>.InsertRows(
                            castTransaction.sqlConnection,
                            eventsToAdd,
                            transaction: castTransaction.sqlTransaction);

                        Dictionary<int, long> groupOrderToId = new Dictionary<int, long>();
                        foreach (Event createdEvent in SqlAccessor<Event>.SelectResultSet(
                            castTransaction.sqlConnection,
                            "SELECT * FROM Events WHERE Events.GroupId = ?",
                            transaction: castTransaction.sqlTransaction,
                            selectParameters: Helpers.EnumerateSingleItem(eventGroup)))
                        {
                            groupOrderToId.Add((int)createdEvent.GroupOrder, createdEvent.EventId);
                        }

                        Func<Event, FileSystemObject> setIdAndGrabObject = currentEvent =>
                        {
                            currentEvent.FileSystemObject.EventId = orderToChange[(int)currentEvent.GroupOrder].Value.Value = groupOrderToId[(int)currentEvent.GroupOrder];
                            return currentEvent.FileSystemObject;
                        };

                        SqlAccessor<FileSystemObject>.InsertRows(
                            castTransaction.sqlConnection,
                            eventsToAdd.Select(setIdAndGrabObject),
                            transaction: castTransaction.sqlTransaction);

                        foreach (KeyValuePair<FileChange, GenericHolder<long>> currentAddedEvent in orderToChange.Values)
                        {
                            currentAddedEvent.Key.EventId = currentAddedEvent.Value.Value;
                            _trace.writeToMemory(() => _trace.trcFmtStr(2, "IndexingAgent: AddEvents: Call MessageEvents.ApplyFileChangeMergeToChangeState."));
                            MessageEvents.ApplyFileChangeMergeToChangeState(this, new FileChangeMerge(currentAddedEvent.Key, null));   // Message to invoke BadgeNet.IconOverlay.QueueNewEventBadge(currentAddedEvent.Key, null)
                        }
                    }
                }
                while (currentChangeIndex != lastHighestChangeIndex);
            }
            catch (Exception ex)
            {
                toReturn += ex;
            }
            finally
            {
                if (!inputTransactionSet
                    && castTransaction != null)
                {
                    castTransaction.Commit();

                    castTransaction.Dispose();
                }
            }
            return toReturn;
        }

        /// <summary>
        /// Removes a single event by its id
        /// </summary>
        /// <param name="eventId">Id of event to remove</param>
        /// <returns>Returns an error in removing the event, if any</returns>
        public CLError RemoveEventById(long eventId, SQLTransactionalBase existingTransaction = null)
        {
            return RemoveEventsByIds(Helpers.EnumerateSingleItem(eventId), existingTransaction);
        }

        /// <summary>
        /// Removes a collection of events by their ids
        /// </summary>
        /// <param name="eventIds">Ids of events to remove</param>
        /// <returns>Returns an error in removing events, if any</returns>
        public CLError RemoveEventsByIds(IEnumerable<long> eventIds, SQLTransactionalBase existingTransaction = null)
        {
            if (eventIds == null)
            {
                try
                {
                    throw new NullReferenceException("eventIds cannot be null");
                }
                catch (Exception ex)
                {
                    return ex;
                }
            }

            CLError toReturn = null;
            SQLTransactionalImplementation castTransaction = existingTransaction as SQLTransactionalImplementation;
            if (existingTransaction != null
                && castTransaction == null)
            {
                try
                {
                    throw new NullReferenceException("existingTransaction is not implemented as private derived type. It should be retrieved via method GetNewTransaction method. Creating a new transaction instead which will be committed immediately.");
                }
                catch (Exception ex)
                {
                    toReturn += ex;
                }
            }

            bool inputTransactionSet = castTransaction != null;
            try
            {
                if (castTransaction == null)
                {
                    ISQLiteConnection indexDB;
                    castTransaction = new SQLTransactionalImplementation(
                        indexDB = CreateAndOpenCipherConnection(),
                        indexDB.BeginTransaction(System.Data.IsolationLevel.Serializable));
                }

                Func<Exception> notFoundException = () => new KeyNotFoundException("Event not found to delete");

                // Find the existing objects for the given ids
                List<long> toDeleteIds = new List<long>();

                StringBuilder multipleDeleteQuery = null;
                HashSet<long> deleteIdsToFind = null;

                // special enumerator processing so we can iterate the event ids to delete just once, but also be able to know and handle having only one event id
                Nullable<long> storeLastDelete = null;
                using (IEnumerator<long> deleteEnumerator = eventIds.GetEnumerator())
                {
                    bool lastDelete;
                    while (!(lastDelete = !deleteEnumerator.MoveNext()) || storeLastDelete != null)
                    {
                        if (storeLastDelete != null)
                        {
                            if (lastDelete
                                && multipleDeleteQuery == null)
                            {
                                // single delete

                                long toDeleteId;
                                if (!SqlAccessor<object>.TrySelectScalar(
                                    castTransaction.sqlConnection,
                                    "SELECT FileSystemObjects.FileSystemObjectId " +
                                        "FROM FileSystemObjects " +
                                        "WHERE FileSystemObjects.EventId = ? " + // <-- parameter 1
                                        "ORDER BY FileSystemObjects.FileSystemObjectId DESC " +
                                        "LIMIT 1",
                                    out toDeleteId,
                                    castTransaction.sqlTransaction,
                                    Helpers.EnumerateSingleItem((long)storeLastDelete)))
                                {
                                    throw notFoundException();
                                }

                                if (!SqlAccessor<FileSystemObject>.DeleteRow(
                                    castTransaction.sqlConnection,
                                    new FileSystemObject() { FileSystemObjectId = toDeleteId },
                                    castTransaction.sqlTransaction))
                                {
                                    throw notFoundException();
                                }
                            }
                            else
                            {
                                if (multipleDeleteQuery == null)
                                {
                                    // start multiple delete query

                                    deleteIdsToFind = new HashSet<long>();
                                    deleteIdsToFind.Add((long)storeLastDelete);

                                    multipleDeleteQuery = new StringBuilder(
                                        "SELECT FileSystemObjects.* " +
                                            "FROM FileSystemObjects " +
                                            "INNER JOIN " +
                                            "(" +
                                            "SELECT EventId, MAX(FileSystemObjectId) AS MaxFileSystemObjectId " +
                                            "FROM FileSystemObjects " +
                                            "WHERE EventId IN (?" /*"[event ids]) " +
                                            "GROUP BY EventId" +
                                            ") InnerFileSystemObjects " +
                                            "WHERE InnerFileSystemObjects.EventId = FileSystemObjects.EventId " +
                                            "AND InnerFileSystemObjects.MaxFileSystemObjectId = FileSystemObjects.FileSystemObjectId" */);
                                }
                                else
                                {
                                    if (deleteIdsToFind.Add((long)storeLastDelete))
                                    {
                                        // append current item
                                        multipleDeleteQuery.Append(",?");
                                    }
                                }
                            }
                        }

                        storeLastDelete = (lastDelete
                            ? (Nullable<long>)null
                            : deleteEnumerator.Current);
                    }
                }

                if (multipleDeleteQuery != null)
                {
                    multipleDeleteQuery.Append(") " +
                        "GROUP BY EventId" +
                        ") InnerFileSystemObjects " +
                        "WHERE InnerFileSystemObjects.EventId = FileSystemObjects.EventId " +
                        "AND InnerFileSystemObjects.MaxFileSystemObjectId = FileSystemObjects.FileSystemObjectId");

                    List<long> fileSystemObjectIdsToDelete = new List<long>(deleteIdsToFind.Count);

                    foreach (FileSystemObject currentMatchedDelete in SqlAccessor<FileSystemObject>.SelectResultSet(
                        castTransaction.sqlConnection,
                        multipleDeleteQuery.ToString(),
                        transaction: castTransaction.sqlTransaction,
                        selectParameters: deleteIdsToFind))
                    {
                        try
                        {
                            if (!deleteIdsToFind.Remove((long)currentMatchedDelete.EventId))
                            {
                                throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Query of FileSystemObjectIds to delete returned a row with an EventId not in the query list or which was already marked found");
                            }
                            else
                            {
                                fileSystemObjectIdsToDelete.Add(currentMatchedDelete.FileSystemObjectId);
                            }
                        }
                        catch (Exception ex)
                        {
                            toReturn += ex;
                        }
                    }

                    foreach (long deleteIdNotFound in deleteIdsToFind)
                    {
                        try
                        {
                            throw new KeyNotFoundException("Unable to find FileSystemObject with EventId " + deleteIdsToFind.ToString() + " to delete");
                        }
                        catch (Exception ex)
                        {
                            toReturn += ex;
                        }
                    }

                    IEnumerable<int> unableToFindIndexes;
                    SqlAccessor<FileSystemObject>.DeleteRows(
                        castTransaction.sqlConnection,
                        fileSystemObjectIdsToDelete.Select(fileSystemObjectId => new FileSystemObject() { FileSystemObjectId = fileSystemObjectId }),
                        out unableToFindIndexes,
                        castTransaction.sqlTransaction);

                    // if it is normal to throw an exception below due to trigger-recursed deletes, then just comment out the exception-throwing below
                    if (unableToFindIndexes != null)
                    {
                        foreach (int unableToFindIndex in unableToFindIndexes)
                        {
                            try
                            {
                                throw new KeyNotFoundException("Unable to find FileSystemObject by Id " + fileSystemObjectIdsToDelete[unableToFindIndex].ToString() + " even after confirming existing record; row possibly deleted by recursive trigger beforehand");
                            }
                            catch (Exception ex)
                            {
                                toReturn += ex;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                toReturn += ex;
            }
            finally
            {
                if (!inputTransactionSet
                    && castTransaction != null)
                {
                    castTransaction.Commit();

                    castTransaction.Dispose();
                }
            }
            return toReturn;
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
        public CLError RecordCompletedSync(IEnumerable<PossiblyChangedFileChange> communicatedChanges, string syncId, IEnumerable<long> syncedEventIds, out long syncCounter, string rootFolderUID = null)
        {
            try
            {
                using (SQLTransactionalImplementation connAndTran = GetNewTransactionPrivate())
                {
                    SqlSync newSync = new SqlSync()
                    {
                        SID = syncId
                    };

                    syncCounter = newSync.SyncCounter = SqlAccessor<SqlSync>.InsertRow<long>(connAndTran.sqlConnection, newSync, transaction: connAndTran.sqlTransaction);

                    if (rootFolderUID != null)
                    {
                        using (ISQLiteCommand updateRootFolderUID = connAndTran.sqlConnection.CreateCommand())
                        {
                            updateRootFolderUID.Transaction = connAndTran.sqlTransaction;

                            updateRootFolderUID.CommandText = "UPDATE FileSystemObjects " +
                                "SET ServerUid = ?, " + // <-- parameter 1
                                "SyncCounter = ?" + // <-- parameter 2
                                "WHERE FileSystemObjectId = ?"; // <-- parameter 3

                            ISQLiteParameter rootUID = updateRootFolderUID.CreateParameter();
                            rootUID.Value = rootFolderUID;
                            updateRootFolderUID.Parameters.Add(rootUID);

                            ISQLiteParameter firstSyncCounter = updateRootFolderUID.CreateParameter();
                            firstSyncCounter.Value = syncCounter;
                            updateRootFolderUID.Parameters.Add(firstSyncCounter);

                            ISQLiteParameter rootPK = updateRootFolderUID.CreateParameter();
                            rootPK.Value = rootFileSystemObjectId;
                            updateRootFolderUID.Parameters.Add(rootPK);

                            updateRootFolderUID.ExecuteNonQuery();
                        }
                    }

                    if (communicatedChanges != null)
                    {
                        List<long> notMarkedAsChanged = new List<long>();

                        CLError mergeChangedError = MergeEventsIntoDatabase(
                            newSync.SyncCounter,
                            communicatedChanges.OrderBy(currentCommunicatedChange => currentCommunicatedChange.ResultOrder)
                                .Where(currentCommunicatedChange =>
                                {
                                    if (currentCommunicatedChange.Changed)
                                    {
                                        currentCommunicatedChange.FileChange.DoNotAddToSQLIndex = false;
                                        return true;
                                    }

                                    notMarkedAsChanged.Add(currentCommunicatedChange.FileChange.EventId);
                                    return false;
                                })
                                .Select(currentCommunicatedChange => new FileChangeMerge(currentCommunicatedChange.FileChange))
                                .ToArray(), // ToArray prevents multiple enumeration from running select logic a second time
                            connAndTran);

                        if (mergeChangedError != null)
                        {
                            throw new AggregateException("An error occurred merging a batch of communicated changes before completing a new sync", mergeChangedError.Exceptions);
                        }

                        if (notMarkedAsChanged.Count > 0)
                        {
                            using (ISQLiteCommand updateSyncCounterOnly = connAndTran.sqlConnection.CreateCommand())
                            {
                                updateSyncCounterOnly.Transaction = connAndTran.sqlTransaction;

                                ISQLiteParameter newSyncCounter = updateSyncCounterOnly.CreateParameter();
                                newSyncCounter.Value = newSync.SyncCounter;
                                updateSyncCounterOnly.Parameters.Add(newSyncCounter);

                                StringBuilder updateSyncCounterOnlyText = null;

                                foreach (long currentToUpdate in notMarkedAsChanged)
                                {
                                    if (updateSyncCounterOnlyText == null)
                                    {
                                        updateSyncCounterOnlyText = new StringBuilder("UPDATE FileSystemObjects " +
                                            "SET SyncCounter = ? " +
                                            "WHERE SyncCounter IS NULL " +
                                            "AND EventId IN (?");
                                    }
                                    else
                                    {
                                        updateSyncCounterOnlyText.Append(",?");
                                    }

                                    ISQLiteParameter currentEventId = updateSyncCounterOnly.CreateParameter();
                                    currentEventId.Value = currentToUpdate;
                                    updateSyncCounterOnly.Parameters.Add(currentEventId);
                                }

                                updateSyncCounterOnlyText.Append(")");

                                updateSyncCounterOnly.CommandText = updateSyncCounterOnlyText.ToString();

                                updateSyncCounterOnly.ExecuteNonQuery();
                            }
                        }
                    }

                    foreach (long synchronouslyCompletedEventId in syncedEventIds ?? Enumerable.Empty<long>())
                    {
                        CLError markCompletionError = MarkEventAsCompletedOnPreviousSync(synchronouslyCompletedEventId, connAndTran);
                        if (markCompletionError != null)
                        {
                            throw new AggregateException("Error marking Event at synchronouslyCompletedEventId completed on RecordCompleted", markCompletionError.Exceptions);
                        }
                    }

                    LastSyncId = syncId;

                    connAndTran.Commit();
                }
            }
            catch (Exception ex)
            {
                syncCounter = Helpers.DefaultForType<long>();
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
                InitializeDatabase(newRootPath, createEvenIfExisting: true);
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Creates a new transactional object which can be passed back into database access calls and externalizes the ability to dispose or commit the transaction
        /// </summary>
        public SQLTransactionalBase GetNewTransaction()
        {
            return GetNewTransactionPrivate();
        }

        private SQLTransactionalImplementation GetNewTransactionPrivate()
        {
            ISQLiteConnection indexDB;
            return new SQLTransactionalImplementation(
                indexDB = CreateAndOpenCipherConnection(),
                indexDB.BeginTransaction(System.Data.IsolationLevel.Serializable));
        }

        /// <summary>
        /// Method to merge event into database,
        /// used when events are modified or replaced with new events
        /// </summary>
        /// <returns>Returns an error from merging the events, if any</returns>
        public CLError MergeEventsIntoDatabase(IEnumerable<FileChangeMerge> mergeToFroms, SQLTransactionalBase existingTransaction = null)
        {
            return MergeEventsIntoDatabase(null, mergeToFroms, existingTransaction);
        }
        private CLError MergeEventsIntoDatabase(Nullable<long> syncCounter, IEnumerable<FileChangeMerge> mergeToFroms, SQLTransactionalBase existingTransaction)
        {
            // no point trying to perform multiple simultaneous merges since they will block each other via the SQLite transaction
            lock (MergeEventsLocker)
            {
                CLError toReturn = null;
                SQLTransactionalImplementation castTransaction = existingTransaction as SQLTransactionalImplementation;
                if (existingTransaction != null
                    && castTransaction == null)
                {
                    try
                    {
                        throw new NullReferenceException("existingTransaction is not implemented as private derived type. It should be retrieved via method GetNewTransaction method. Creating a new transaction instead which will be committed immediately.");
                    }
                    catch (Exception ex)
                    {
                        toReturn += ex;
                    }
                }

                bool inputTransactionSet = castTransaction != null;
                try
                {
                    if (mergeToFroms != null)
                    {
                        HashSet<long> updatedIds = new HashSet<long>();
                        HashSet<long> deletedIds = new HashSet<long>();

                        List<FileChange> toAddList = new List<FileChange>();
                        List<long> toDeleteList = new List<long>();

                        // special enumerator processing so we can know when we're processing the last item since we cannot simply queue its item for batch accumulation
                        Nullable<FileChangeMerge> storeLastMerge = null;
                        using (IEnumerator<FileChangeMerge> mergeEnumerator = mergeToFroms.GetEnumerator())
                        {
                            bool finalMergeEvent;
                            while (!(finalMergeEvent = !mergeEnumerator.MoveNext()) || storeLastMerge != null)
                            {
                                try
                                {
                                    FileChange toAdd;
                                    long toDelete;
                                    FileChange toUpdate;

                                    if (storeLastMerge != null)
                                    {
                                        FileChangeMerge currentMerge = (FileChangeMerge)storeLastMerge;

                                        try
                                        {
                                            // Continue to next iteration if boolean set indicating not to add to SQL
                                            if (currentMerge.MergeTo != null
                                                && currentMerge.MergeTo.DoNotAddToSQLIndex
                                                && currentMerge.MergeTo.EventId != 0)
                                            {
                                                MessageEvents.ApplyFileChangeMergeToChangeState(this, new FileChangeMerge(currentMerge.MergeTo, currentMerge.MergeFrom));   // Message to invoke BadgeNet.IconOverlay.QueueNewEventBadge(currentMergeToFrom.MergeTo, currentMergeToFrom.MergeFrom)

                                                // normally we assign the next event to process at the end of the looping section, but since we short circuit it with continue, need to assign next event now
                                                storeLastMerge = (finalMergeEvent
                                                    ? (Nullable<FileChangeMerge>)null
                                                    : mergeEnumerator.Current);

                                                continue;
                                            }

                                            // Ensure input variables have proper references set
                                            if (currentMerge.MergeTo == null)
                                            {
                                                // null merge events are only valid if there is an oldEvent to remove
                                                if (currentMerge.MergeFrom == null)
                                                {
                                                    throw new NullReferenceException("currentMerge.MergeTo cannot be null");
                                                }
                                            }
                                            else if (currentMerge.MergeTo.Metadata == null)
                                            {
                                                throw new NullReferenceException("currentMerge.MergeTo cannot have null Metadata");
                                            }
                                            else if (currentMerge.MergeTo.NewPath == null)
                                            {
                                                throw new NullReferenceException("currentMerge.MergeTo cannot have null NewPath");
                                            }

                                            if (castTransaction == null)
                                            {
                                                ISQLiteConnection indexDB;
                                                castTransaction = new SQLTransactionalImplementation(
                                                    indexDB = CreateAndOpenCipherConnection(),
                                                    indexDB.BeginTransaction(System.Data.IsolationLevel.Serializable));
                                            }

                                            ////possibilities for old event:
                                            ////none,
                                            ////not in database, <-- causes old to be ignored (acts like none)
                                            ////exists in database
                                            //
                                            //
                                            ////possibilities for new event:
                                            ////none,
                                            ////not in database, (new event)
                                            ////exists in database
                                            //
                                            //
                                            ////mutually exclusive:
                                            ////none and none
                                            //
                                            //
                                            ////if there is an old exists and a new none, then delete old row
                                            //
                                            ////if old does not exists and a new none, do nothing (already not in database)
                                            //
                                            ////if old none
                                            ////    if new not in database, add new to database
                                            ////    else if new in database, update new
                                            //
                                            ////if there is an old exists and new not in database, update old row with new data
                                            //
                                            ////if there is an old exists and new in database and neither match, delete new row and update old row with new data
                                            //
                                            ////if there is an old exists and new in database and they do match by row primary key (EventId), update new in database
                                            //
                                            ////(ignore old:)
                                            ////if old does not exist and new new not in database, add new to database
                                            //
                                            ////(ignore old:)
                                            ////if old does not exist and new exists in database, update new in database


                                            // byte definitions:
                                            // 0 = null
                                            // 1 = not in database (EventId == 0)
                                            // 2 = exists in database (EventId > 0)

                                            byte oldEventState = (currentMerge.MergeFrom == null
                                                ? (byte)0
                                                : (currentMerge.MergeFrom.EventId > 0
                                                    ? (byte)2
                                                    : (byte)1));

                                            byte newEventState = (currentMerge.MergeTo == null
                                                ? (byte)0
                                                : (currentMerge.MergeTo.EventId > 0
                                                    ? (byte)2
                                                    : (byte)1));

                                            switch (oldEventState)
                                            {
                                                // old event is null or not null but does not already exist in database
                                                case (byte)0:
                                                case (byte)1: // <-- not in database treated like null for old event
                                                    switch (newEventState)
                                                    {
                                                        // 0 for new event is only possible if old event was 1 (null and null are mutually excluded via exceptions above)
                                                        case (byte)0:
                                                            // already not in database, do nothing
                                                            toAdd = null;
                                                            toUpdate = null;
                                                            toDelete = 0;
                                                            break;

                                                        case (byte)1:
                                                            // nothing to delete for the old row since it never existed in database;
                                                            // new row doesn't exist in database so it will be added
                                                            toAdd = currentMerge.MergeTo;
                                                            toUpdate = null;
                                                            toDelete = 0;
                                                            break;

                                                        default: //case (byte)2:
                                                            // nothing to delete for old row since it never existeed in database;
                                                            // new row exists in database so update it
                                                            toAdd = null;
                                                            toUpdate = currentMerge.MergeTo;
                                                            toDelete = 0;
                                                            break;
                                                    }
                                                    break;

                                                // old event already exists in database
                                                default: //case (byte)2:
                                                    switch (newEventState)
                                                    {
                                                        case (byte)0:
                                                            // old row exists in database but merging it into nothingness, simply delete old row
                                                            toAdd = null;
                                                            toUpdate = null;
                                                            toDelete = currentMerge.MergeFrom.EventId;
                                                            break;

                                                        case (byte)1:
                                                            // old row exists in database and needs to be updated with latest metadata which is not in an existing new row
                                                            currentMerge.MergeTo.EventId = currentMerge.MergeFrom.EventId; // replace merge to event id with the one from the sync from

                                                            toAdd = null;
                                                            toUpdate = currentMerge.MergeTo;
                                                            toDelete = 0;
                                                            break;

                                                        default: //case (byte)2:
                                                            // old row exists in database and a new row exists

                                                            // if the rows match, then update the new row only
                                                            if (currentMerge.MergeFrom.EventId == currentMerge.MergeTo.EventId)
                                                            {
                                                                toAdd = null;
                                                                toUpdate = currentMerge.MergeTo;
                                                                toDelete = 0;
                                                            }
                                                            // else if the rows do not match, then delete the new row, and put the new metadata in the old row (prefers keeping lowest EventId in database for dependency hierarchy reasons)
                                                            else
                                                            {
                                                                // set toDelete first since the event Id at the reference we are grabbing is going to be changed in between setting toDelete and toUpdate

                                                                toDelete = currentMerge.MergeTo.EventId;

                                                                currentMerge.MergeTo.EventId = currentMerge.MergeFrom.EventId; // replace merge to event id with the one from the sync from

                                                                toAdd = null;
                                                                toUpdate = currentMerge.MergeTo;
                                                            }
                                                            break;
                                                    }
                                                    break;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            toDelete = 0;
                                            toUpdate = null;
                                            toAdd = null;

                                            toReturn += ex;
                                        }
                                    }
                                    else
                                    {
                                        toDelete = 0;
                                        toUpdate = null;
                                        toAdd = null;
                                    }

                                    // determine if a previous batch has finished, if there will be no more events (process any existing batch as final), or if there is an update to process immediately,
                                    // and create an action priority to perform operations by the original event order

                                    // changeType byte enum:
                                    // 0 = deletion action
                                    // 1 = addition action
                                    // 2 = update action

                                    List<byte> actionOrder = new List<byte>();

                                    if (toDeleteList.Count > 0)
                                    {
                                        // if the current event cannot be appended to the delete list, then the delete list must process first
                                        if (toUpdate != null
                                            || toAdd != null)
                                        {
                                            actionOrder.Add((byte)0);
                                        }
                                    }

                                    if (toAddList.Count > 0)
                                    {
                                        // if the current event cannot be appended to the add list, then the add list must process first
                                        if (toDelete > 0
                                            || toUpdate != null)
                                        {
                                            actionOrder.Add((byte)1);
                                        }
                                    }

                                    // process the current event; deletes and adds will be added to a batch to process, but update is processed by itself
                                    if (toDelete > 0)
                                    {
                                        deletedIds.Add(toDelete);

                                        toDeleteList.Add(toDelete);

                                        // if last event, process what's in the delete batch now
                                        if (finalMergeEvent)
                                        {
                                            actionOrder.Add((byte)0);
                                        }

                                        // possible to have both a delete and an update if the rows are being merged
                                        if (toUpdate != null)
                                        {
                                            actionOrder.Add((byte)2);
                                        }
                                    }
                                    else if (toAdd != null)
                                    {
                                        toAddList.Add(toAdd);

                                        // if last event, process what's in the add batch now
                                        if (finalMergeEvent)
                                        {
                                            actionOrder.Add((byte)1);
                                        }
                                    }
                                    else if (toUpdate != null)
                                    {
                                        // always process every update one at a time
                                        actionOrder.Add((byte)2);
                                    }

                                    foreach (byte currentAction in actionOrder)
                                    {
                                        switch (currentAction)
                                        {
                                            // action is delete
                                            case (byte)0:
                                                CLError removeBatchError = RemoveEventsByIds(toDeleteList, castTransaction);

                                                if (removeBatchError != null)
                                                {
                                                    toReturn += new AggregateException("One or more errors occurred removing a batch of events by ids", removeBatchError.Exceptions);
                                                }

                                                // no point wasting effort to clear the list for future batches if there will be no future batches
                                                if (!finalMergeEvent)
                                                {
                                                    toDeleteList.Clear();
                                                }
                                                break;

                                            // action is add
                                            case (byte)1:
                                                CLError addBatchError = AddEvents(syncCounter, toAddList, castTransaction);

                                                if (addBatchError != null)
                                                {
                                                    toReturn += new AggregateException("One or more errors occurred adding a batch of new events");
                                                }

                                                // no point wasting effort to clear the list for future batches if there will be no future batches
                                                if (!finalMergeEvent)
                                                {
                                                    toAddList.Clear();
                                                }
                                                break;

                                            // action is update
                                            default: //case (byte)2:
                                                FileSystemObject existingRow = SqlAccessor<FileSystemObject>.SelectResultSet(
                                                        castTransaction.sqlConnection,
                                                        "SELECT " +
                                                            SqlAccessor<FileSystemObject>.GetSelectColumns() + ", " +
                                                            SqlAccessor<Event>.GetSelectColumns("Event") + ", " +
                                                            SqlAccessor<FileSystemObject>.GetSelectColumns("Event.Previous", "Previouses") +
                                                            " FROM FileSystemObjects" +
                                                            " INNER JOIN Events ON FileSystemObjects.EventId = Events.EventId" +
                                                            " LEFT OUTER JOIN FileSystemObjects Previouses ON Events.PreviousId = Previouses.FileSystemObjectId" +
                                                            " WHERE Events.EventId = ?" + // <-- parameter 1
                                                            " AND FileSystemObjects.ParentFolderId IS NOT NULL" +
                                                            " LIMIT 1",
                                                        new[]
                                                        {
                                                            "Event",
                                                            "Event.Previous"
                                                        },
                                                        castTransaction.sqlTransaction,
                                                        Helpers.EnumerateSingleItem((long)toUpdate.EventId))
                                                    .SingleOrDefault();

                                                if (existingRow == null)
                                                {
                                                    // couldn't find existing row to update, add a new one instead (will overwrite the EventId)
                                                    toAdd = toUpdate;
                                                }
                                                else
                                                {
                                                    if (existingRow.ParentFolderId == null)
                                                    {
                                                        throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Existing FileSystemObject to update did not have a parent folder");
                                                    }

                                                    long toUpdateParentFolderId;
                                                    Nullable<long> toUpdatePreviousId;

                                                    FilePath previousRowPath = existingRow.CalculatedFullPath;
                                                    if (previousRowPath != null
                                                        && FilePathComparer.Instance.Equals(previousRowPath.Parent, toUpdate.NewPath.Parent))
                                                    {
                                                        toUpdateParentFolderId = (long)existingRow.ParentFolderId;
                                                    }
                                                    // prefer latest event even if pending
                                                    else if (!SqlAccessor<object>.TrySelectScalar(
                                                        castTransaction.sqlConnection,
                                                        "SELECT FileSystemObjects.FileSystemObjectId " +
                                                            "FROM FileSystemObjects " +
                                                            "WHERE CalculatedFullPath = ? " + // <-- parameter 1
                                                            "ORDER BY " +
                                                            "CASE WHEN FileSystemObjects.EventOrder IS NULL " +
                                                            "THEN 0 " +
                                                            "ELSE FileSystemObjects.EventOrder " +
                                                            "END DESC " +
                                                            "LIMIT 1",
                                                        out toUpdateParentFolderId,
                                                        castTransaction.sqlTransaction,
                                                        selectParameters: Helpers.EnumerateSingleItem(toUpdate.NewPath.Parent.ToString())))
                                                    {
                                                        throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Unable to find FileSystemObject with path of parent folder to use as containing folder");
                                                    }

                                                    if (toUpdate.OldPath == null)
                                                    {
                                                        toUpdatePreviousId = null;
                                                    }
                                                    else if (existingRow.Event.Previous == null
                                                        || !FilePathComparer.Instance.Equals(existingRow.Event.Previous.CalculatedFullPath, toUpdate.OldPath))
                                                    {
                                                        long previousIdNotNull;

                                                        // prefers the latest rename which is pending,
                                                        // otherwise prefers non-pending,
                                                        // last take most recent event
                                                        if (!SqlAccessor<object>.TrySelectScalar(
                                                            castTransaction.sqlConnection,
                                                            "SELECT FileSystemObjects.FileSystemObjectId " +
                                                                "FROM FileSystemObjects " +
                                                                "LEFT OUTER JOIN Events ON FileSystemObjects.EventId = Events.EventId " +
                                                                "WHERE FileSystemObjects.CalculatedFullPath = ? " + // <-- parameter 1
                                                                "ORDER BY " +
                                                                "CASE WHEN FileSystemObjects.EventId IS NOT NULL " +
                                                                "AND Events.FileChangeTypeEnumId = " + changeEnumsBackward[FileChangeType.Renamed].ToString() +
                                                                " AND FileSystemObjects.Pending = 1 " +
                                                                "THEN 0 " +
                                                                "ELSE 1 " +
                                                                "END ASC, " +
                                                                "FileSystemObjects.Pending ASC, " +
                                                                "CASE WHEN FileSystemObjects.EventOrder IS NULL " +
                                                                "THEN 0 " +
                                                                "ELSE FileSystemObjects.EventOrder " +
                                                                "END DESC " +
                                                                "LIMIT 1",
                                                            result: out previousIdNotNull,
                                                            transaction: castTransaction.sqlTransaction,
                                                            selectParameters: Helpers.EnumerateSingleItem(toUpdate.OldPath.ToString())))
                                                        {
                                                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Unable to find FileSystemObject with old path of toUpdate before rename\\move operation");
                                                        }

                                                        toUpdatePreviousId = previousIdNotNull;
                                                    }
                                                    else
                                                    {
                                                        toUpdatePreviousId = existingRow.Event.PreviousId;
                                                    }

                                                    #region update fields in FileSystemObject

                                                    // only associate an event to a sync counter once, later events should get new objects with a new SyncCounter anyways
                                                    if (existingRow.SyncCounter == null)
                                                    {
                                                        existingRow.SyncCounter = syncCounter;
                                                    }

                                                    if (toUpdate.Metadata.HashableProperties.CreationTime.Ticks == FileConstants.InvalidUtcTimeTicks)
                                                    {
                                                        existingRow.CreationTimeUTCTicks = null;
                                                    }
                                                    else
                                                    {
                                                        DateTime creationTimeUTC = toUpdate.Metadata.HashableProperties.CreationTime.ToUniversalTime();

                                                        existingRow.CreationTimeUTCTicks = (creationTimeUTC.Ticks == FileConstants.InvalidUtcTimeTicks
                                                            ? (Nullable<long>)null
                                                            : creationTimeUTC.Ticks);
                                                    }
                                                    existingRow.EventTimeUTCTicks = DateTime.UtcNow.Ticks;
                                                    existingRow.IsFolder = toUpdate.Metadata.HashableProperties.IsFolder;
                                                    existingRow.IsShare = toUpdate.Metadata.IsShare;
                                                    if (toUpdate.Metadata.HashableProperties.LastTime.Ticks == FileConstants.InvalidUtcTimeTicks)
                                                    {
                                                        existingRow.LastTimeUTCTicks = null;
                                                    }
                                                    else
                                                    {
                                                        DateTime lastTimeUTC = toUpdate.Metadata.HashableProperties.LastTime.ToUniversalTime();

                                                        existingRow.LastTimeUTCTicks = (lastTimeUTC.Ticks == FileConstants.InvalidUtcTimeTicks
                                                            ? (Nullable<long>)null
                                                            : lastTimeUTC.Ticks);
                                                    }
                                                    byte[] getMD5 = toUpdate.MD5;
                                                    existingRow.MD5 = getMD5;
                                                    existingRow.MimeType = toUpdate.Metadata.MimeType;
                                                    existingRow.Name = toUpdate.NewPath.Name;
                                                    existingRow.ParentFolderId = toUpdateParentFolderId;
                                                    //existingRow.Pending = true; // <-- true on insert, no need to update here
                                                    existingRow.Permissions = (toUpdate.Metadata.Permissions == null
                                                        ? (Nullable<int>)null
                                                        : (int)((POSIXPermissions)toUpdate.Metadata.Permissions));
                                                    existingRow.Revision = toUpdate.Metadata.Revision;
                                                    //existingRow.ServerName // <-- add support for server name
                                                    existingRow.ServerUid = toUpdate.Metadata.ServerUid;
                                                    existingRow.Size = toUpdate.Metadata.HashableProperties.Size;
                                                    existingRow.StorageKey = toUpdate.Metadata.StorageKey;
                                                    existingRow.Version = toUpdate.Metadata.Version;
                                                    #endregion

                                                    #region update fields in Event
                                                    //existingRow.Event.FileChangeTypeCategoryId = changeCategoryId; // <-- changeCategoryId on insert, no need to update here
                                                    existingRow.Event.FileChangeTypeEnumId = changeEnumsBackward[toUpdate.Type];
                                                    existingRow.Event.PreviousId = toUpdatePreviousId;
                                                    existingRow.Event.SyncFrom = (toUpdate.Direction == SyncDirection.From);
                                                    #endregion

                                                    if (!SqlAccessor<Event>.UpdateRow(castTransaction.sqlConnection, existingRow.Event, castTransaction.sqlTransaction))
                                                    {
                                                        toAdd = toUpdate;
                                                    }
                                                    if (!SqlAccessor<FileSystemObject>.UpdateRow(castTransaction.sqlConnection, existingRow, castTransaction.sqlTransaction))
                                                    {
                                                        toAdd = toUpdate;
                                                    }
                                                }

                                                updatedIds.Add(toUpdate.EventId);
                                                break;
                                        }
                                    }

                                    storeLastMerge = (finalMergeEvent
                                        ? (Nullable<FileChangeMerge>)null
                                        : mergeEnumerator.Current);
                                }
                                catch (Exception ex)
                                {
                                    toReturn += ex;
                                }
                            }
                        }

                        foreach (FileChangeMerge currentMergeToFrom in mergeToFroms)
                        {
                            // If mergedEvent was not processed in AddEvents,
                            // then process badging (AddEvents processes badging for the rest)
                            if (currentMergeToFrom.MergeTo == null
                                || updatedIds.Contains(currentMergeToFrom.MergeTo.EventId)
                                || deletedIds.Contains(currentMergeToFrom.MergeTo.EventId))
                            {
                                MessageEvents.ApplyFileChangeMergeToChangeState(this, new FileChangeMerge(currentMergeToFrom.MergeTo, currentMergeToFrom.MergeFrom));   // Message to invoke BadgeNet.IconOverlay.QueueNewEventBadge(currentMergeToFrom.MergeTo, currentMergeToFrom.MergeFrom)
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    toReturn += ex;
                }
                finally
                {
                    if (!inputTransactionSet
                        && castTransaction != null)
                    {
                        castTransaction.Commit();

                        castTransaction.Dispose();
                    }
                }
                return toReturn;
            }
        }
        private readonly object MergeEventsLocker = new object();

        /// <summary>
        /// The way completing an event works has changed. The following comments may be wrong: Includes an event in the last set of sync states,
        /// or in other words processes it as complete
        /// (event will no longer be included in GetEventsSinceLastSync)
        /// </summary>
        /// <param name="eventId">Primary key value of the event to process</param>
        /// <returns>Returns an error that occurred marking the event complete, if any</returns>
        public CLError MarkEventAsCompletedOnPreviousSync(long eventId, SQLTransactionalBase existingTransaction = null)
        {
            CLError toReturn = null;
            SQLTransactionalImplementation castTransaction = existingTransaction as SQLTransactionalImplementation;
            if (existingTransaction != null
                && castTransaction == null)
            {
                try
                {
                    throw new NullReferenceException("existingTransaction is not implemented as private derived type. It should be retrieved via method GetNewTransaction method. Creating a new transaction instead which will be committed immediately.");
                }
                catch (Exception ex)
                {
                    toReturn += ex;
                }
            }

            bool inputTransactionSet = castTransaction != null;
            FileChangeType storeExistingChangeType;
            string storeNewPath;
            string storeOldPath;
            bool storeWhetherEventIsASyncFrom;

            try
            {
                if (castTransaction == null)
                {
                    ISQLiteConnection indexDB;
                    castTransaction = new SQLTransactionalImplementation(
                        indexDB = CreateAndOpenCipherConnection(),
                        indexDB.BeginTransaction(System.Data.IsolationLevel.Serializable));
                }

                //// don't think I need to change the SyncCounter ever when just completing an event, it should already be set
                //
                //long lastSyncCount;
                //if (!SqlAccessor<object>.TrySelectScalar(
                //    indexDB,
                //    "SELECT Syncs.SyncCounter " +
                //    "FROM Syncs " +
                //    "ORDER BY Syncs.SyncCounter DESC " +
                //    "LIMIT 1",
                //    out lastSyncCount,
                //    indexTran))
                //{
                //    throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Cannot complete an event without a previous sync point");
                //}

                GenericHolder<CLError> moveObjectsToNewParentError = new GenericHolder<CLError>(null);
                var moveObjectsToNewParent = DelegateAndDataHolder.Create(
                    new
                    {
                        castTransaction = castTransaction,
                        oldId = new GenericHolder<long>(),
                        newId = new GenericHolder<long>()
                    },
                    (Data, errorToAccumulate) =>
                    {
                        try
                        {
                            using (ISQLiteCommand moveChildrenCommand = Data.castTransaction.sqlConnection.CreateCommand())
                            {
                                moveChildrenCommand.Transaction = Data.castTransaction.sqlTransaction;
                                moveChildrenCommand.CommandText = "UPDATE FileSystemObjects " +
                                    "SET ParentFolderId = ? " +
                                    "WHERE ParentFolderId = ?";

                                ISQLiteParameter newObjectId = moveChildrenCommand.CreateParameter();
                                newObjectId.Value = Data.newId.Value;
                                moveChildrenCommand.Parameters.Add(newObjectId);

                                ISQLiteParameter oldObjectId = moveChildrenCommand.CreateParameter();
                                oldObjectId.Value = Data.oldId.Value;
                                moveChildrenCommand.Parameters.Add(oldObjectId);

                                moveChildrenCommand.ExecuteNonQuery();
                            }
                        }
                        catch (Exception ex)
                        {
                            errorToAccumulate.Value += ex;
                        }
                    },
                    moveObjectsToNewParentError);

                FileSystemObject existingEventObject = SqlAccessor<FileSystemObject>.SelectResultSet(
                        castTransaction.sqlConnection,
                        "SELECT " +
                        SqlAccessor<FileSystemObject>.GetSelectColumns() + ", " +
                        SqlAccessor<Event>.GetSelectColumns("Event") + ", " +
                        SqlAccessor<FileSystemObject>.GetSelectColumns("Event.Previous", "Previouses") +
                        " FROM FileSystemObjects" +
                        " INNER JOIN Events ON FileSystemObjects.EventId = Events.EventId" +
                        " LEFT OUTER JOIN FileSystemObjects Previouses ON Events.PreviousId = Previouses.FileSystemObjectId" +
                        " WHERE FileSystemObjects.EventId = ?" + // <-- parameter 1
                        " ORDER BY FileSystemObjects.FileSystemObjectId DESC" +
                        " LIMIT 1",
                        new[]
                        {
                            "Event",
                            "Event.Previous"
                        },
                        castTransaction.sqlTransaction,
                        Helpers.EnumerateSingleItem(eventId))
                    .SingleOrDefault();

                if (existingEventObject == null)
                {
                    throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Unable to find existing event to complete");
                }
                if (!existingEventObject.Pending)
                {
                    throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Existing event already not pending");
                }
                if (existingEventObject.ParentFolderId == null)
                {
                    throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "The root folder object should never have been pending to complete");
                }

                storeExistingChangeType = changeEnums[existingEventObject.Event.FileChangeTypeEnumId];
                storeNewPath = existingEventObject.CalculatedFullPath;
                storeOldPath = (existingEventObject.Event.Previous == null ? null : existingEventObject.Event.Previous.CalculatedFullPath);
                storeWhetherEventIsASyncFrom = existingEventObject.Event.SyncFrom;

                long existingNonPendingIdToMerge;
                if (SqlAccessor<object>.TrySelectScalar(
                    castTransaction.sqlConnection,
                    "SELECT FileSystemObjects.FileSystemObjectId " +
                    "FROM FileSystemObjects " +
                    "WHERE FileSystemObjects.ParentFolderId = ? " + // <-- parameter 1
                    "AND FileSystemObjects.Name = ? " + // <-- parameter 2
                    "AND FileSystemObjects.Pending = 0 " +
                    "ORDER BY FileSystemObjects.FileSystemObjectId DESC " +
                    "LIMIT 1",
                    out existingNonPendingIdToMerge,
                    castTransaction.sqlTransaction,
                    (new[] { (long)existingEventObject.ParentFolderId, (object)existingEventObject.Name })))
                {
                    //// The following cases below seemed to happen under normal use and we don't wish to kill the sync engine,
                    //// it would be better if we fixed the causes of the conditions below from happening
                    //
                    //switch (storeExistingChangeType)
                    //{
                    //    case FileChangeType.Created:
                    //        throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Should not have an existing object with the same name under the same parent already not pending if this pending event represents a create");

                    //    case FileChangeType.Renamed:
                    //        throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Should not have an existing object with the same name under the same parent already not pending if this pending event represents a rename");
                    //}

                    moveObjectsToNewParent.TypedData.oldId.Value = existingNonPendingIdToMerge;
                    moveObjectsToNewParent.TypedData.newId.Value = existingEventObject.FileSystemObjectId;
                    moveObjectsToNewParent.Process();
                    if (moveObjectsToNewParentError.Value != null)
                    {
                        throw new AggregateException("An error occurred moving objects to new parent", moveObjectsToNewParentError.Value.Exceptions);
                    }

                    using (ISQLiteCommand movePreviousesCommand = castTransaction.sqlConnection.CreateCommand())
                    {
                        movePreviousesCommand.Transaction = castTransaction.sqlTransaction;
                        movePreviousesCommand.CommandText = "UPDATE Events " +
                            "SET PreviousId = ? " +
                            "WHERE PreviousId = ? ";

                        ISQLiteParameter newPreviousId = movePreviousesCommand.CreateParameter();
                        newPreviousId.Value = existingEventObject.FileSystemObjectId;
                        movePreviousesCommand.Parameters.Add(newPreviousId);

                        ISQLiteParameter oldPreviousId = movePreviousesCommand.CreateParameter();
                        oldPreviousId.Value = existingNonPendingIdToMerge;
                        movePreviousesCommand.Parameters.Add(oldPreviousId);
                    }

                    SqlAccessor<FileSystemObject>.DeleteRow(
                        castTransaction.sqlConnection,
                        new FileSystemObject()
                        {
                            FileSystemObjectId = existingNonPendingIdToMerge
                        },
                        castTransaction.sqlTransaction);
                }
                //// The following cases below seemed to happen under normal use and we don't wish to kill the sync engine,
                //// it would be better if we fixed the causes of the conditions below from happening
                //
                //else
                //{
                //    switch (storeExistingChangeType)
                //    {
                //        case FileChangeType.Modified:
                //            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Must have an existing object with the same name under the same parent already not pending if this pending event represents a modify");

                //        case FileChangeType.Deleted:
                //            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Must have an existing object with the same name under the same parent already not pending if this pending event represents a delete");
                //    }
                //}

                switch (storeExistingChangeType)
                {
                    case FileChangeType.Created:
                    case FileChangeType.Modified:
                        existingEventObject.Pending = false;
                        existingEventObject.EventTimeUTCTicks = DateTime.UtcNow.Ticks;
                        if (!SqlAccessor<FileSystemObject>.UpdateRow(
                            castTransaction.sqlConnection,
                            existingEventObject,
                            castTransaction.sqlTransaction))
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Unable to update existing event to not be pending");
                        }
                        break;

                    case FileChangeType.Deleted:
                        if (!SqlAccessor<FileSystemObject>.DeleteRow(
                            castTransaction.sqlConnection,
                            existingEventObject,
                            castTransaction.sqlTransaction))
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Unable to apply deletion to complete a delete event");
                        }
                        break;

                    case FileChangeType.Renamed:
                        existingEventObject.Pending = false;
                        existingEventObject.EventTimeUTCTicks = DateTime.UtcNow.Ticks;
                        if (existingEventObject.Event.PreviousId == null)
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Rename event cannot have a null PreviousId");
                        }
                        else if (existingEventObject.Event.Previous == null)
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Rename event has a PreviousId, but the previous object was not retrieved");
                        }

                        long storePreviousId = (long)existingEventObject.Event.PreviousId; // store previous id, since we are about to nullify the event value but still need it to delete\move children

                        using (ISQLiteCommand moveOtherMatchingOldNames = castTransaction.sqlConnection.CreateCommand())
                        {
                            moveOtherMatchingOldNames.Transaction = castTransaction.sqlTransaction;

                            moveOtherMatchingOldNames.CommandText = "UPDATE FileSystemObjects " +
                                "SET ParentFolderId = ?, " + // <-- parameter 1
                                "Name = ? " + // <-- parameter 2
                                "WHERE ParentFolderId = ? " + // <-- parameter 3
                                "AND Name = ?"; // <-- parameter 4

                            ISQLiteParameter newParentParam = moveOtherMatchingOldNames.CreateParameter();
                            newParentParam.Value = existingEventObject.ParentFolderId;
                            moveOtherMatchingOldNames.Parameters.Add(newParentParam);

                            ISQLiteParameter newNameParam = moveOtherMatchingOldNames.CreateParameter();
                            newNameParam.Value = existingEventObject.Name;
                            moveOtherMatchingOldNames.Parameters.Add(newNameParam);

                            ISQLiteParameter oldParentParam = moveOtherMatchingOldNames.CreateParameter();
                            oldParentParam.Value = existingEventObject.Event.Previous.ParentFolderId;
                            moveOtherMatchingOldNames.Parameters.Add(oldParentParam);

                            ISQLiteParameter oldNameParam = moveOtherMatchingOldNames.CreateParameter();
                            oldNameParam.Value = existingEventObject.Event.Previous.Name;
                            moveOtherMatchingOldNames.Parameters.Add(oldNameParam);

                            moveOtherMatchingOldNames.ExecuteNonQuery();
                        }

                        existingEventObject.Event.PreviousId = null; // allows us to delete the FileSystemObject for the previous location so we don't have two of them non-pending to represent the same item
                        if (!SqlAccessor<Event>.UpdateRow(
                            castTransaction.sqlConnection,
                            existingEventObject.Event,
                            castTransaction.sqlTransaction))
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Unable to disconnect rename event from previous id in order to delete it");
                        }

                        moveObjectsToNewParent.TypedData.oldId.Value = storePreviousId;
                        moveObjectsToNewParent.TypedData.newId.Value = existingEventObject.FileSystemObjectId;
                        moveObjectsToNewParent.Process();
                        if (moveObjectsToNewParentError.Value != null)
                        {
                            throw new AggregateException("An error occurred moving objects to new parent", moveObjectsToNewParentError.Value.Exceptions);
                        }

                        if (!SqlAccessor<FileSystemObject>.DeleteRow(
                            castTransaction.sqlConnection,
                            new FileSystemObject()
                            {
                                FileSystemObjectId = storePreviousId
                            },
                            castTransaction.sqlTransaction))
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Unable to delete previous object for rename event");
                        }

                        if (!SqlAccessor<FileSystemObject>.UpdateRow(
                            castTransaction.sqlConnection,
                            existingEventObject,
                            castTransaction.sqlTransaction))
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Unable to update existing event to not be pending");
                        }
                        break;

                    default:
                        throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Existing event object had a FileChangeTypeEnumId which did not match to a known FileChangeType");
                }
            }
            catch (Exception ex)
            {
                storeExistingChangeType = Helpers.DefaultForType<FileChangeType>();
                storeNewPath = Helpers.DefaultForType<string>();
                storeOldPath = Helpers.DefaultForType<string>();
                storeWhetherEventIsASyncFrom = Helpers.DefaultForType<bool>();
                toReturn += ex;
            }
            finally
            {
                if (!inputTransactionSet
                    && castTransaction != null)
                {
                    castTransaction.Commit();

                    castTransaction.Dispose();
                }
            }

            if (toReturn == null)
            {
                try
                {
                    MarkBadgeSyncedAfterEventCompletion(storeExistingChangeType, storeNewPath, storeOldPath, storeWhetherEventIsASyncFrom);
                }
                catch (Exception ex)
                {
                    toReturn += ex;
                }
            }

            return toReturn;
        }

        /// <summary>
        /// Call this when the location of the sync folder has changed (while syncing is stopped) to update the entire index to all new paths based in the new root folder
        /// </summary>
        public CLError ChangeSyncRoot(string newSyncRoot)
        {
            try
            {
                if (string.IsNullOrEmpty(newSyncRoot))
                {
                    throw new NullReferenceException("newSyncRoot cannot be null");
                }

                // initializing the database may create the database starting at the newSyncRoot so no setting is required;
                // otherwise, still need to set the root
                if (!InitializeDatabase(newSyncRoot))
                {
                    using (ISQLiteConnection indexDB = CreateAndOpenCipherConnection())
                    {
                        using (ISQLiteCommand changeRoot = indexDB.CreateCommand())
                        {
                            changeRoot.CommandText = "UPDATE FileSystemObjects " +
                                "SET Name = ? " + // <-- parameter 1
                                "WHERE ParentFolderId IS NULL"; // condition for root folder object

                            ISQLiteParameter newRootParam = changeRoot.CreateParameter();
                            newRootParam.Value = newSyncRoot;
                            changeRoot.Parameters.Add(newRootParam);

                            changeRoot.ExecuteNonQuery();
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
        #endregion

        #region private methods

        /// <summary>
        /// Private constructor to ensure IndexingAgent is created through public static initializer (to return a CLError)
        /// </summary>
        /// <param name="syncbox">Syncbox to index</param>
        private IndexingAgent(CLSyncbox syncbox)
        {
            if (syncbox == null)
            {
                throw new NullReferenceException("syncbox cannot be null");
            }
            if (string.IsNullOrEmpty(syncbox.CopiedSettings.DeviceId))
            {
                throw new NullReferenceException("settings DeviceId cannot be null");
            }

            this.indexDBLocation = (string.IsNullOrEmpty(syncbox.CopiedSettings.DatabaseFolder)
                ? Helpers.GetDefaultDatabasePath(syncbox.CopiedSettings.DeviceId, syncbox.SyncboxId) + "\\" + CLDefinitions.kSyncDatabaseFileName
                : syncbox.CopiedSettings.DatabaseFolder + "\\" + CLDefinitions.kSyncDatabaseFileName);

            this.syncbox = syncbox;
        }

        private bool InitializeDatabase(string syncRoot, bool createEvenIfExisting = false)
        {
            FileInfo dbInfo = new FileInfo(indexDBLocation);

            bool deleteDB;
            bool createDB;

            if (dbInfo.Exists)
            {
                if (createEvenIfExisting)
                {
                    deleteDB = true;
                    createDB = true;
                }
                else
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
                                // database was never finalized (version is changed from 1 to [current database version] via the last initialization script, which identifies successful creation)
                                // the very first implementation of this database will be version 2 so we can compare on less than 2

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
                            syncbox.SyncboxId,
                            syncbox.CopiedSettings.DeviceId);

                        deleteDB = true;
                        createDB = true;
                    }
                }
            }
            else
            {
                MessageEvents.FireNewEventMessage(
                    "Existing database not found, possibly due to new SyncboxId\\DeviceId combination. Starting fresh.",
                    EventMessageLevel.Minor,
                    SyncboxId: syncbox.SyncboxId,
                    DeviceId: syncbox.CopiedSettings.DeviceId);

                createDB = true;
                deleteDB = false;
            }

            if (deleteDB)
            {
                dbInfo.Delete();
            }

            if (createDB)
            {
                FileInfo indexDBInfo = new FileInfo(indexDBLocation);
                if (!indexDBInfo.Directory.Exists)
                {
                    indexDBInfo.Directory.Create();
                }

                using (ISQLiteConnection newDBConnection = CreateAndOpenCipherConnection(enforceForeignKeyConstraints: false)) // circular reference between Events and FileSystemObjects tables
                {
                    // read creation scripts in here

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
                        using (IEnumerator<string> insertEnumerator = indexDBScripts.OrderBy(scriptPair => scriptPair.Key)
                            .Select(scriptPair => scriptPair.Value)
                            .GetEnumerator())
                        {
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
            }

            lock (changeEnumsLocker)
            {
                if (changeEnums == null)
                {
                    try
                    {
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
                    catch
                    {
                        changeEnums = null; // used as condition to rebuild the static dictionaries

                        throw;
                    }
                }
            }

            return createDB;
        }

        private void MarkBadgeSyncedAfterEventCompletion(FileChangeType storeExistingChangeType, string storeNewPath, string storeOldPath, bool storeWhetherEventIsASyncFrom)
        {
            Action<FilePath> setBadgeSynced = syncedPath =>
            {
                MessageEvents.QueueSetBadge(this, new SetBadge(PathState.Synced, syncedPath));   // Message to invoke BadgeNet.IconOverlay.QueueSetBadge(PathState.Synced, syncedPath);
            };

            // Adjust the badge for this completed event.
            switch (storeExistingChangeType)
            {
                case FileChangeType.Created:
                case FileChangeType.Modified:
                    setBadgeSynced(storeNewPath);
                    break;
                case FileChangeType.Deleted:
                    if (storeWhetherEventIsASyncFrom)
                    {
                        bool isDeleted;
                        MessageEvents.DeleteBadgePath(this, new DeleteBadgePath(storeNewPath), out isDeleted);   // Message to invoke BadgeNet.IconOverlay.DeleteBadgePath(currentEvent.FileSystemObject.Path, out isDeleted);
                    }
                    else
                    {
                        setBadgeSynced(storeNewPath);
                    }
                    break;
                case FileChangeType.Renamed:
                    if (storeWhetherEventIsASyncFrom)
                    {
                        MessageEvents.RenameBadgePath(this, new RenameBadgePath(storeOldPath, storeNewPath));   // Message to invoke BadgeNet.IconOverlay.RenameBadgePath(currentEvent.PreviousPath, currentEvent.FileSystemObject.Path);
                    }

                    setBadgeSynced(storeNewPath);
                    break;
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
                throw indexPathCreationError.PrimaryException;
            }

            FilePathDictionary<FileMetadata> combinedIndexPlusChanges;
            CLError combinedIndexCreationError = FilePathDictionary<FileMetadata>.CreateAndInitialize(baseComparePath,
                out combinedIndexPlusChanges);
            if (combinedIndexCreationError != null)
            {
                throw combinedIndexCreationError.PrimaryException;
            }

            FilePathDictionary<GenericHolder<bool>> pathDeletions;
            CLError pathDeletionsCreationError = FilePathDictionary<GenericHolder<bool>>.CreateAndInitialize(baseComparePath,
                out pathDeletions);
            if (pathDeletionsCreationError != null)
            {
                throw pathDeletionsCreationError.PrimaryException;
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

                Dictionary<long, string> objectIdsToFullPath = new Dictionary<long, string>();
                SortedDictionary<KeyValuePair<bool, long>, FileSystemObject> sortedFileSystemObjects = new SortedDictionary<KeyValuePair<bool, long>, FileSystemObject>(pendingThenIdComparer.Instance);
                long missingOrderAppend = 0;

                foreach (FileSystemObject combinedPendingNonPending in SqlAccessor<FileSystemObject>.SelectResultSet(
                    indexDB,
                    "SELECT " +
                        SqlAccessor<FileSystemObject>.GetSelectColumns() + ", " +
                        SqlAccessor<FileSystemObject>.GetSelectColumns("Event") + ", " +
                        SqlAccessor<FileSystemObject>.GetSelectColumns("Parent", "Parents") +
                        " FROM FileSystemObjects" +
                        " LEFT OUTER JOIN Events ON " +
                        "(" +
                        "  FileSystemObjects.EventId = Events.EventId" +
                        "  AND FileSystemObjects.Pending = 1" +
                        ")" +
                        " LEFT OUTER JOIN FileSystemObjects Parents ON " +
                        "(" +
                        "  FileSystemObjects.ParentFolderId = Parents.FileSystemObjectId" +
                        "  AND FileSystemObjects.Pending = 1" +
                        ")",
                    new[] { "Event", "Parent" }))
                {
                    if (combinedPendingNonPending.ParentFolderId == null)
                    {
                        // set the root metadata
                        combinedIndexPlusChanges[null] = indexPaths[null] = new FileMetadata()
                        {
                            EventTime = new DateTime(0, DateTimeKind.Utc),
                            HashableProperties = new FileMetadataHashableProperties(
                                isFolder: true,
                                lastTime: null,
                                creationTime: null,
                                size: null),
                            ServerUid = combinedPendingNonPending.ServerUid
                        };
                    }
                    else
                    {
                        objectIdsToFullPath.Add(combinedPendingNonPending.FileSystemObjectId, combinedPendingNonPending.CalculatedFullPath);
                        sortedFileSystemObjects.Add(
                            new KeyValuePair<bool, long>(
                                combinedPendingNonPending.Pending,
                                combinedPendingNonPending.EventOrder ?? ((missingOrderAppend++) + Int64.MinValue)),
                            combinedPendingNonPending);
                    }
                }

                List<FileChange> pendingsWithSyncCounter = new List<FileChange>();
                List<FileChange> pendingsWithoutSyncCounter = new List<FileChange>();

                foreach (KeyValuePair<KeyValuePair<bool, long>, FileSystemObject> currentObject in sortedFileSystemObjects)
                {
                    if (currentObject.Key.Key) // true for pending
                    {
                        FileChange currentChange = new FileChange()
                        {
                            Direction = (currentObject.Value.Event.SyncFrom ? SyncDirection.From : SyncDirection.To),
                            EventId = currentObject.Value.Event.EventId,
                            Metadata = new FileMetadata()
                            {
                                EventTime = new DateTime(currentObject.Value.EventTimeUTCTicks, DateTimeKind.Utc),
                                HashableProperties = new FileMetadataHashableProperties(
                                    isFolder: currentObject.Value.IsFolder,
                                    lastTime: (currentObject.Value.LastTimeUTCTicks == null
                                        ? (Nullable<DateTime>)null
                                        : new DateTime((long)currentObject.Value.LastTimeUTCTicks, DateTimeKind.Utc)),
                                    creationTime: (currentObject.Value.CreationTimeUTCTicks == null
                                        ? (Nullable<DateTime>)null
                                        : new DateTime((long)currentObject.Value.CreationTimeUTCTicks, DateTimeKind.Utc)),
                                    size: currentObject.Value.Size),
                                IsShare = currentObject.Value.IsShare,
                                MimeType = currentObject.Value.MimeType,
                                Permissions = (currentObject.Value.Permissions == null ? (Nullable<POSIXPermissions>)null : (POSIXPermissions)((int)currentObject.Value.Permissions)),
                                Revision = currentObject.Value.Revision,
                                ServerUid = currentObject.Value.ServerUid,
                                ParentFolderServerUid = (currentObject.Value.Parent == null ? null : currentObject.Value.Parent.ServerUid),
                                StorageKey = currentObject.Value.StorageKey,
                                Version = currentObject.Value.Version
                            },
                            NewPath = currentObject.Value.CalculatedFullPath,
                            OldPath = (currentObject.Value.Event.PreviousId == null
                                ? null
                                : objectIdsToFullPath[(long)currentObject.Value.Event.PreviousId]),
                            Type = changeEnums[currentObject.Value.Event.FileChangeTypeEnumId]
                        };

                        CLError setCurrentChangeMD5Error = currentChange.SetMD5(currentObject.Value.MD5);
                        if (setCurrentChangeMD5Error != null)
                        {
                            throw new AggregateException("Error setting currentChange MD5", setCurrentChangeMD5Error.Exceptions);
                        }

                        (currentObject.Value.SyncCounter == null
                                ? pendingsWithoutSyncCounter
                                : pendingsWithSyncCounter)
                            .Add(currentChange);
                    }
                    else
                    {
                        FileMetadata currentToAdd = new FileMetadata()
                        {
                            EventTime = new DateTime(currentObject.Value.EventTimeUTCTicks, DateTimeKind.Utc),
                            HashableProperties = new FileMetadataHashableProperties(
                                isFolder: currentObject.Value.IsFolder,
                                lastTime: (currentObject.Value.LastTimeUTCTicks == null
                                    ? (Nullable<DateTime>)null
                                    : new DateTime((long)currentObject.Value.LastTimeUTCTicks, DateTimeKind.Utc)),
                                creationTime: (currentObject.Value.CreationTimeUTCTicks == null
                                    ? (Nullable<DateTime>)null
                                    : new DateTime((long)currentObject.Value.CreationTimeUTCTicks, DateTimeKind.Utc)),
                                size: currentObject.Value.Size),
                            IsShare = currentObject.Value.IsShare,
                            MimeType = currentObject.Value.MimeType,
                            Permissions = (currentObject.Value.Permissions == null ? (Nullable<POSIXPermissions>)null : (POSIXPermissions)((int)currentObject.Value.Permissions)),
                            Revision = currentObject.Value.Revision,
                            ServerUid = currentObject.Value.ServerUid,
                            ParentFolderServerUid = (currentObject.Value.Parent == null ? null : currentObject.Value.Parent.ServerUid),
                            StorageKey = currentObject.Value.StorageKey,
                            Version = currentObject.Value.Version
                        };

                        FilePath currentPath = currentObject.Value.CalculatedFullPath;
                        indexPaths.Add(currentPath, currentToAdd);
                        combinedIndexPlusChanges.Add(currentPath, currentToAdd);
                    }
                }

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
                    Helpers.EnumerateSingleItem(indexedPath)
                        .Concat(
                            RecurseIndexDirectory(
                                changeList,
                                indexPaths,
                                combinedIndexPlusChanges,
                                new Func<long, CLError>(
                                    delegate(long eventId)
                                    {
                                        return this.RemoveEventById(eventId);
                                    }),
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
                    FilePath deletedPathObject = deletedPath;

                    FileMetadata parentFolderMetadata;
                    string parentFolderServerUid;
                    if (combinedIndexPlusChanges.TryGetValue(deletedPathObject.Parent, out parentFolderMetadata))
                    {
                        parentFolderServerUid = parentFolderMetadata.ServerUid;
                    }
                    else
                    {
                        parentFolderServerUid = null;
                    }

                    // For the path that existed previously in the index but is no longer found on disc, process as a deletion
                    possibleDeletions.Add(new FileChange()
                    {
                        NewPath = deletedPath,
                        Type = FileChangeType.Deleted,
                        Metadata = indexPaths[deletedPathObject],
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
                        changeList.Insert(0, possibleDeletion);
                    }
                }

                foreach (FilePath initiallySyncedBadge in indexPaths.Keys)
                {
                    _trace.writeToMemory(() => _trace.trcFmtStr(1, "IndexingAgent: BuildIndex: Call MessageEvents.SetPathState synced."));
                    MessageEvents.SetPathState(this, new SetBadge(PathState.Synced, initiallySyncedBadge));
                }

                // Callback on initial index completion
                // (will process detected changes and begin normal folder monitor processing)
                indexCompletionCallback(indexPaths,
                    changeList);
            }
        }
        /// <summary>
        /// bool in KeyValuePair Key should be true for pending, the long Value should be the FileSystemObjectId;
        /// use in a SortedList or SortedDictionary should give all non-pendings first then all pendings; within each group, they are ascending sorted by id
        /// </summary>
        private sealed class pendingThenIdComparer : IComparer<KeyValuePair<bool, long>>
        {
            public static pendingThenIdComparer Instance = new pendingThenIdComparer();

            private pendingThenIdComparer() { }

            int IComparer<KeyValuePair<bool, long>>.Compare(KeyValuePair<bool, long> x, KeyValuePair<bool, long> y)
            {
                if (x.Key == y.Key)
                {
                    return (x.Value == y.Value
                        ? 0
                        : (x.Value > y.Value
                            ? 1
                            : -1));
                }
                else if (x.Key)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
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
                    // sync will determine that there are no files in the Syncbox folder, and it will actually delete all of the files on the server.
                    // We have to stop this thread dead in its tracks, and do it in such a way that it is not recoverable.
                    CLError error = new Exception("Unable to find cloud directory at path: " + currentDirectoryFullPath);
                    error.Log(_trace.TraceLocation, _trace.LogErrors);
                    _trace.writeToLog(1, "IndexingAgent: RecursiveIndexDirectory: ERROR: Exception: Msg: <{0}>.", error.PrimaryException.Message);

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
                        FileMetadata parentFolderMetadata;
                        string parentFolderServerUid;
                        if (combinedIndexPlusChanges.TryGetValue(subDirectoryPathObject.Parent, out parentFolderMetadata))
                        {
                            parentFolderServerUid = parentFolderMetadata.ServerUid;
                        }
                        else
                        {
                            parentFolderServerUid = null;
                        }

                        FileMetadata newDirectoryMetadata = new FileMetadata()
                        {
                            HashableProperties = compareProperties,
                            ParentFolderServerUid = parentFolderServerUid
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
                            FileMetadata parentFolderMetadata;
                            string parentFolderServerUid;
                            if (combinedIndexPlusChanges.TryGetValue(currentFilePathObject.Parent, out parentFolderMetadata))
                            {
                                parentFolderServerUid = parentFolderMetadata.ServerUid;
                            }
                            else
                            {
                                parentFolderServerUid = null;
                            }

                            FileMetadata modifiedMetadata = new FileMetadata()
                            {
                                ServerUid = existingFileMetadata.ServerUid,
                                HashableProperties = compareProperties,
                                Revision = existingFileMetadata.Revision,
                                ParentFolderServerUid = parentFolderServerUid/*,
                                    StorageKey = existingFileMetadata.StorageKey*/
                                // DO NOT copy StorageKey because this metadata is for a modified change which would therefore require a new StorageKey
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
                        FileMetadata parentFolderMetadata;
                        string parentFolderServerUid;
                        if (combinedIndexPlusChanges.TryGetValue(currentFilePathObject.Parent, out parentFolderMetadata))
                        {
                            parentFolderServerUid = parentFolderMetadata.ServerUid;
                        }
                        else
                        {
                            parentFolderServerUid = null;
                        }

                        FileMetadata fileCreatedMetadata = new FileMetadata()
                        {
                            HashableProperties = compareProperties,
                            ParentFolderServerUid = parentFolderServerUid//,
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
                    // TODO: may not wish to cause the entire SDK to halt here, instead this should only halt the current engine
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
                }
            }
        }
        #endregion

        #region SQLTransactionalBase implementation
        private sealed class SQLTransactionalImplementation : SQLTransactionalBase
        {
            public readonly ISQLiteConnection sqlConnection;
            public readonly ISQLiteTransaction sqlTransaction;

            private readonly GenericHolder<bool> transactionCommitted;

            public SQLTransactionalImplementation(ISQLiteConnection sqlConnection, ISQLiteTransaction sqlTransaction)
            {
                this.sqlConnection = sqlConnection;
                this.sqlTransaction = sqlTransaction;
                if (sqlTransaction != null)
                {
                    transactionCommitted = new GenericHolder<bool>(false);
                }
            }

            #region SQLTransactionalBase overrides
            public override void Commit()
            {
                base.CheckDisposed();

                if (sqlTransaction != null)
                {
                    lock (transactionCommitted)
                    {
                        if (transactionCommitted.Value)
                        {
                            throw new NotSupportedException("Cannot commit same database transaction more than once");
                        }

                        sqlTransaction.Commit();

                        transactionCommitted.Value = true;
                    }
                }
            }

            protected override bool _disposed
            {
                get
                {
                    return _localDisposed;
                }
            }
            private bool _localDisposed = false;

            protected override void Dispose(bool disposing)
            {
                // Check to see if Dispose has already been called. 
                if (!this._localDisposed)
                {
                    // If disposing equals true, dispose all managed 
                    // and unmanaged resources. 
                    if (disposing)
                    {
                        // Dispose managed resources.
                        if (sqlTransaction != null)
                        {
                            lock (transactionCommitted)
                            {
                                if (!transactionCommitted.Value)
                                {
                                    try
                                    {
                                        sqlTransaction.Rollback();
                                    }
                                    catch
                                    {
                                    }
                                }
                            }

                            try
                            {
                                sqlTransaction.Dispose();
                            }
                            catch
                            {
                            }
                        }

                        if (sqlConnection != null)
                        {
                            try
                            {
                                sqlConnection.Dispose();
                            }
                            catch
                            {
                            }
                        }
                    }

                    // Call the appropriate methods to clean up 
                    // unmanaged resources here. 
                    // If disposing is false, 
                    // only the following code is executed.

                    /* [ My code here ] */

                    // Note disposing has been done.
                    this._localDisposed = true;
                }
            }
            #endregion
        }
        #endregion
    }
}