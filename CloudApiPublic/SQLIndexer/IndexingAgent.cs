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
using System.Data.SqlServerCe;
using System.Globalization;
using System.Windows;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
using CloudApiPublic.SQLIndexer.SqlModel;
using CloudApiPublic.SQLIndexer.Migrations;
using CloudApiPublic.SQLIndexer.Static;
using CloudApiPublic.SQLIndexer.Model;
using SqlSync = CloudApiPublic.SQLIndexer.SqlModel.Sync;

namespace CloudApiPublic.SQLIndexer
{
    public sealed class IndexingAgent : IDisposable
    {
        #region private fields
        // store the path that represents the root of indexing
        private string indexedPath = null;

        #region SQL CE
        private string indexDBLocation;
        private const string indexDBPassword = "Q29weXJpZ2h0Q2xvdWQuY29tQ3JlYXRlZEJ5RGF2aWRCcnVjaw=="; // <-- if you change this password, you will likely break all clients with older databases
        private static string getDecodedIndexDBPassword()
        {
            byte[] decodeChars = Convert.FromBase64String(indexDBPassword);
            return Encoding.ASCII.GetString(decodeChars);
        }
        private const string connectionStringFormatter = "data source={0};password={1};lcid=1033;case sensitive=TRUE;default lock timeout=300000"; // 1033 is Locale ID for English - United States
        private static string buildConnectionString(string indexDBLocation)
        {
            return string.Format(connectionStringFormatter, indexDBLocation, getDecodedIndexDBPassword());
        }
        private const string indexScriptsResourceFolder = ".SQLIndexer.IndexDBScripts.";

        public readonly ReaderWriterLockSlim CELocker = new ReaderWriterLockSlim();
        #endregion

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
        public readonly ReaderWriterLockSlim LastSyncLocker = new ReaderWriterLockSlim();
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
                        bool needToMakeDB = true;

                        if (File.Exists(newAgent.indexDBLocation))
                        {
                            try
                            {
                                using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(newAgent.indexDBLocation)))
                                {
                                    indexDB.Open();

                                    int versionBeforeUpdate;
                                    using (SqlCeCommand versionCommand = indexDB.CreateCommand())
                                    {
                                        versionCommand.CommandText = "SELECT [Version].[Version] FROM [Version] WHERE [Version].[TrueKey] = 1";
                                        versionBeforeUpdate = Helpers.ConvertTo<int>(versionCommand.ExecuteScalar());
                                    }

                                    if (versionBeforeUpdate == 0)
                                    {
                                        int newVersion = 1;

                                        foreach (KeyValuePair<int, IMigration> currentDBMigration in MigrationList.GetMigrationsAfterVersion(versionBeforeUpdate))
                                        {
                                            currentDBMigration.Value.Apply(indexDB, getDecodedIndexDBPassword());

                                            newVersion = currentDBMigration.Key;
                                        }

                                        using (SqlCeCommand updateVersionCommand = indexDB.CreateCommand())
                                        {
                                            updateVersionCommand.CommandText = "UPDATE [Version] SET [Version].[Version] = " + newVersion.ToString() + " WHERE [Version].[TrueKey] = 1";
                                            updateVersionCommand.ExecuteNonQuery();
                                        }
                                    }
                                }

                                needToMakeDB = false;
                            }
                            catch
                            {
                                File.Delete(newAgent.indexDBLocation);
                            }
                        }

                        if (needToMakeDB)
                        {
                            FileInfo indexDBInfo = new FileInfo(newAgent.indexDBLocation);
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

                            using (SqlCeEngine ceEngine = new SqlCeEngine(buildConnectionString(newAgent.indexDBLocation)))
                            {
                                ceEngine.CreateDatabase();
                            }

                            SqlCeConnection creationConnection = null;

                            try
                            {
                                creationConnection = new SqlCeConnection(buildConnectionString(newAgent.indexDBLocation));
                                creationConnection.Open();

                                foreach (string indexDBScript in indexDBScripts.OrderBy(scriptPair => scriptPair.Key).Select(scriptPair => scriptPair.Value))
                                {
                                    SqlCeCommand scriptCommand = creationConnection.CreateCommand();
                                    try
                                    {
                                        scriptCommand.CommandText = Helpers.DecryptString(indexDBScript, getDecodedIndexDBPassword());
                                        scriptCommand.ExecuteNonQuery();
                                    }
                                    finally
                                    {
                                        scriptCommand.Dispose();
                                    }
                                }
                            }
                            finally
                            {
                                if (creationConnection != null)
                                {
                                    creationConnection.Dispose();
                                }
                            }
                        }

                        int changeEnumsCount = System.Enum.GetNames(typeof(FileChangeType)).Length;
                        changeEnums = new Dictionary<int, FileChangeType>(changeEnumsCount);
                        changeEnumsBackward = new Dictionary<FileChangeType, int>(changeEnumsCount);

                        using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(newAgent.indexDBLocation)))
                        {
                            int storeCategoryId = -1;
                            foreach (EnumCategory currentCategory in SqlAccessor<EnumCategory>
                                .SelectResultSet(indexDB,
                                    "SELECT * FROM [EnumCategories]"))
                            {
                                if (currentCategory.Name == typeof(FileChangeType).Name)
                                {
                                    storeCategoryId = currentCategory.EnumCategoryId;
                                }
                            }

                            foreach (SqlEnum currentChangeEnum in SqlAccessor<SqlEnum>
                                .SelectResultSet(indexDB,
                                    "SELECT * FROM [Enums] WHERE [Enums].[EnumCategoryId] = " + storeCategoryId.ToString()))
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
            CELocker.EnterReadLock();
            try
            {
                using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(this.indexDBLocation)))
                {
                    // Pull the last sync from the database
                    SqlSync lastSync = SqlAccessor<SqlSync>
                        .SelectResultSet(indexDB,
                            "SELECT TOP 1 * FROM [Syncs] ORDER BY [Syncs].[SyncCounter] DESC")
                        .SingleOrDefault();

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

                        Dictionary<long, KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>> mappedSyncStates = new Dictionary<long,KeyValuePair<GenericHolder<FileSystemObject>,GenericHolder<FileSystemObject>>>();

                        // Loop through all sync states for the last sync
                        foreach (FileSystemObject currentSyncState in SqlAccessor<FileSystemObject>
                            .SelectResultSet(indexDB,
                                "SELECT * FROM [FileSystemObjects] WHERE [FileSystemObjects].[SyncCounter] = " + lastSync.SyncCounter.ToString()))
                        {
                            if (mappedSyncStates.ContainsKey(currentSyncState.FileSystemObjectId))
                            {
                                if (currentSyncState.ServerLinked)
                                {
                                    mappedSyncStates[currentSyncState.FileSystemObjectId].Value.Value = currentSyncState;
                                }
                                else
                                {
                                    mappedSyncStates[currentSyncState.FileSystemObjectId].Key.Value = currentSyncState;
                                }
                            }
                            else if (currentSyncState.ServerLinked)
                            {
                                mappedSyncStates.Add(currentSyncState.FileSystemObjectId,
                                    new KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>(
                                        new GenericHolder<FileSystemObject>(),
                                        new GenericHolder<FileSystemObject>(currentSyncState)));
                            }
                            else
                            {
                                mappedSyncStates.Add(currentSyncState.FileSystemObjectId,
                                    new KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>(
                                        new GenericHolder<FileSystemObject>(currentSyncState),
                                        new GenericHolder<FileSystemObject>()));
                            }
                        }

                        foreach (KeyValuePair<long, KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>> currentSyncState in mappedSyncStates)
                        {
                            // Add the current sync state from the last sync to the output dictionary
                            syncStates.Add(currentSyncState.Value.Key.Value.Path,
                                new SyncedObject()
                                {
                                    ServerLinkedPath = currentSyncState.Value.Value.Value == null
                                        ? null
                                        : currentSyncState.Value.Value.Value.Path,
                                    Metadata = new FileMetadata()
                                    {
                                        HashableProperties = new FileMetadataHashableProperties(currentSyncState.Value.Key.Value.IsFolder,
                                            currentSyncState.Value.Key.Value.LastTime,
                                            currentSyncState.Value.Key.Value.CreationTime,
                                            currentSyncState.Value.Key.Value.Size),
                                        LinkTargetPath = currentSyncState.Value.Key.Value.TargetPath,
                                        Revision = currentSyncState.Value.Key.Value.Revision,
                                        StorageKey = currentSyncState.Value.Key.Value.StorageKey
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
            finally
            {
                CELocker.ExitReadLock();
            }
            return null;
        }

        public CLError GetMetadataByPathAndRevision(string path, string revision, out FileMetadata metadata)
        {
            CELocker.EnterReadLock();
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    throw new NullReferenceException("path cannot be null");
                }

                using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(this.indexDBLocation)))
                {
                    // Grab the most recent sync from the database to pull sync states
                    SqlSync lastSync = SqlAccessor<SqlSync>
                        .SelectResultSet(indexDB,
                            "SELECT TOP 1 * FROM [Syncs] ORDER BY [Syncs].[SyncCounter] DESC")
                        .SingleOrDefault();

                    if (lastSync == null)
                    {
                        metadata = null;
                    }
                    else
                    {
                        int pathCRC = StringCRC.Crc(path);

                        FileSystemObject foundSync = SqlAccessor<FileSystemObject>
                            .SelectResultSet(indexDB,
                                "SELECT TOP 1 * " +
                                "FROM [FileSystemObjects] " +
                                "WHERE [FileSystemObjects].[SyncCounter] = " + lastSync.SyncCounter.ToString() + " " +
                                "AND [FileSystemObjects].[PathChecksum] = " + pathCRC.ToString() + " " +
                                (revision == null
                                    ? "AND [FileSystemObjects].[Revision] IS NULL "
                                    : "AND [FileSystemObjects].[Revision] = '" + revision.Replace("'", "''").ToLowerInvariant() + "'"))
                            .SingleOrDefault(parent => parent.Path == path); // run in memory since Path field is not indexable

                        if (foundSync != null)
                        {
                            metadata = new FileMetadata()
                            {
                                HashableProperties = new FileMetadataHashableProperties(foundSync.IsFolder,
                                    foundSync.LastTime,
                                    foundSync.CreationTime,
                                    foundSync.Size),
                                LinkTargetPath = foundSync.TargetPath,
                                Revision = revision,
                                StorageKey = foundSync.StorageKey
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
            finally
            {
                CELocker.ExitReadLock();
            }
            return null;
        }

        /// <summary>
        /// Retrieves all unprocessed events that occurred since the last sync
        /// </summary>
        /// <param name="changeEvents">Outputs the unprocessed events</param>
        /// <returns>Returns an error that occurred filling the unprocessed events, if any</returns>
        public CLError GetPendingEvents(out List<KeyValuePair<FilePath, FileChange>> changeEvents)
        {
            CELocker.EnterReadLock();
            try
            {
                using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(this.indexDBLocation)))
                {
                    // Create the output list
                    changeEvents = new List<KeyValuePair<FilePath, FileChange>>();

                    // Loop through all the events in the database after the last sync (if any)
                    foreach (Event currentChange in
                        SqlAccessor<Event>
                            .SelectResultSet(indexDB,
                                "SELECT " +
                                SqlAccessor<Event>.GetSelectColumns() + ", " +
                                SqlAccessor<FileSystemObject>.GetSelectColumns(FileSystemObject.Name) +
                                "FROM [Events] " +
                                "INNER JOIN [FileSystemObjects] ON [Events].[EventId] = [FileSystemObjects].[EventId] " +
                                "WHERE [FileSystemObjects].[SyncCounter] IS NULL " +
                                "ORDER BY [Events].[EventId]",
                                new string[]
                                {
                                    FileSystemObject.Name
                                }))
                    {
                        // For each event since the last sync (if any), add to the output dictionary
                        changeEvents.Add(new KeyValuePair<FilePath, FileChange>(currentChange.FileSystemObject.Path,
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
            finally
            {
                CELocker.ExitReadLock();
            }
            return null;
        }

        /// <summary>
        /// Adds an unprocessed change since the last sync as a new event to the database,
        /// EventId property of the input event is set after database update
        /// </summary>
        /// <param name="newEvents">Change to add</param>
        /// <returns>Returns error that occurred when adding the event to database, if any</returns>
        public CLError AddEvents(IEnumerable<FileChange> newEvents, bool alreadyObtainedLock = false)
        {
            if (!alreadyObtainedLock)
            {
                CELocker.EnterReadLock();
            }
            try
            {
                // Ensure input parameter is set
                if (newEvents == null)
                {
                    throw new NullReferenceException("newEvents cannot be null");
                }
                if (newEvents.Any(newEvent => newEvent.Metadata == null))
                {
                    throw new NullReferenceException("The Metadata property of every newEvent cannot be null");
                }

                List<Event> eventsToAdd = new List<Event>();
                Guid eventGroup = Guid.NewGuid();
                int eventCounter = 0;
                Dictionary<int, KeyValuePair<FileChange, GenericHolder<long>>> orderToChange = new Dictionary<int, KeyValuePair<FileChange, GenericHolder<long>>>();

                // If change is marked for adding to SQL,
                // then process database addition
                foreach (FileChange newEvent in newEvents.Where(newEvent => newEvent.EventId == 0 || !newEvent.DoNotAddToSQLIndex))
                {
                    string newPathString = newEvent.NewPath.ToString();

                    eventCounter++;
                    orderToChange.Add(eventCounter, new KeyValuePair<FileChange, GenericHolder<long>>(newEvent, new GenericHolder<long>()));

                    // Define the new event to add for the unprocessed change
                    eventsToAdd.Add(new Event()
                    {
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
                            Path = newPathString,
                            Size = newEvent.Metadata.HashableProperties.Size,
                            Revision = newEvent.Metadata.Revision,
                            StorageKey = newEvent.Metadata.StorageKey,
                            TargetPath = (newEvent.Metadata.LinkTargetPath == null ? null : newEvent.Metadata.LinkTargetPath.ToString()),
                            SyncCounter = null,
                            ServerLinked = false,

                            // SQL CE does not support computed columns, so no "AS CHECKSUM(Path)"
                            PathChecksum = StringCRC.Crc(newPathString)
                        },
                        PreviousPath = (newEvent.OldPath == null
                            ? null
                            : newEvent.OldPath.ToString()),
                        GroupId = eventGroup,
                        GroupOrder = eventCounter,
                        SyncFrom = (newEvent.Direction == SyncDirection.From)
                    });
                }

                if (eventsToAdd.Count > 0)
                {
                    using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(this.indexDBLocation)))
                    {
                        SqlAccessor<Event>.InsertRows(indexDB, eventsToAdd);
                    }

                    using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(this.indexDBLocation)))
                    {
                        Dictionary<int, long> groupOrderToId = new Dictionary<int, long>();
                        foreach (Event createdEvent in SqlAccessor<Event>.SelectResultSet(indexDB,
                            "SELECT * FROM [Events] WHERE [Events].[GroupId] = '" + eventGroup.ToString() + "'"))
                        {
                            groupOrderToId.Add((int)createdEvent.GroupOrder, createdEvent.EventId);
                        }

                        Func<Event, FileSystemObject> setIdAndGrabObject = currentEvent =>
                            {
                                currentEvent.FileSystemObject.EventId = orderToChange[(int)currentEvent.GroupOrder].Value.Value = groupOrderToId[(int)currentEvent.GroupOrder];
                                return currentEvent.FileSystemObject;
                            };

                        SqlAccessor<FileSystemObject>.InsertRows(indexDB, eventsToAdd.Select(setIdAndGrabObject));
                    }

                    foreach (KeyValuePair<FileChange, GenericHolder<long>> currentAddedEvent in orderToChange.Values)
                    {
                        currentAddedEvent.Key.EventId = currentAddedEvent.Value.Value;
                        MessageEvents.ApplyFileChangeMergeToChangeState(this, new FileChangeMerge(currentAddedEvent.Key, null));   // Message to invoke BadgeNet.IconOverlay.QueueNewEventBadge(currentAddedEvent.Key, null)
                    }
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                if (!alreadyObtainedLock)
                {
                    CELocker.ExitReadLock();
                }
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
            CELocker.EnterReadLock();
            try
            {
                using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(this.indexDBLocation)))
                {
                    // Find the existing object for the given id
                    FileSystemObject toDelete = SqlAccessor<FileSystemObject>.SelectResultSet(indexDB,
                        "SELECT TOP 1 * FROM [FileSystemObjects] WHERE [FileSystemObjects].[EventId] = " + eventId.ToString())
                        .SingleOrDefault();

                    Func<Exception> notFoundException = () => new Exception("Event not found to delete");

                    // Throw exception if an existing event does not exist
                    if (toDelete == null)
                    {
                        throw notFoundException();
                    }

                    // Remove the found event from the database
                    if (!SqlAccessor<FileSystemObject>.DeleteRow(indexDB,
                            toDelete)
                        || !SqlAccessor<Event>.DeleteRow(indexDB,
                            new Event()
                            {
                                EventId = eventId
                            }))
                    {
                        throw notFoundException();
                    }
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                CELocker.ExitReadLock();
            }
            return null;
        }

        /// <summary>
        /// Removes a collection of events by their ids
        /// </summary>
        /// <param name="eventIds">Ids of events to remove</param>
        /// <returns>Returns an error in removing events, if any</returns>
        public CLError RemoveEventsByIds(IEnumerable<long> eventIds, bool alreadyObtainedLock = false)
        {
            CLError notFoundErrors = null;

            if (!alreadyObtainedLock)
            {
                CELocker.EnterReadLock();
            }
            try
            {
                // copy event id collection to array, defaulting to an empty array
                long[] eventIdsArray = (eventIds == null
                    ? new long[0]
                    : eventIds.ToArray());

                if (eventIdsArray.Length > 0)
                {
                    using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(this.indexDBLocation)))
                    {
                        // Create list to copy event ids from database objects,
                        // used to ensure all event ids to be deleted were found
                        List<long> orderedDBIds = new List<long>();
                        // Grab all objects with ids in the specified range
                        FileSystemObject[] deleteObjects = SqlAccessor<FileSystemObject>.SelectResultSet(indexDB,
                            "SELECT * " +
                            "FROM [FileSystemObjects] " +
                            "WHERE [FileSystemObjects].[EventId] IN (" + string.Join(", ", eventIdsArray.Select(currentId => currentId.ToString()).ToArray()) + ") ")
                            .ToArray();

                        IEnumerable<int> unableToFindIndexes;
                        SqlAccessor<FileSystemObject>.DeleteRows(indexDB,
                            deleteObjects,
                            out unableToFindIndexes);
                        
                        // Check all event ids intended for delete and make sure they were actually deleted,
                        // otherwise create exception
                        if (unableToFindIndexes != null)
                        {
                            foreach (int notDeletedIndex in unableToFindIndexes)
                            {
                                notFoundErrors += new Exception("Event with id " + eventIdsArray[notDeletedIndex].ToString() + " not found to delete");
                            }
                        }

                        unableToFindIndexes = new HashSet<int>(unableToFindIndexes ?? Enumerable.Empty<int>());

                        SqlAccessor<Event>.DeleteRows(indexDB,
                            deleteObjects.Where((deleteObject, objectIndex) => !((HashSet<int>)unableToFindIndexes).Contains(objectIndex))
                                .Select(deleteObject => new Event()
                                {
                                    EventId = (long)deleteObject.EventId
                                }), out unableToFindIndexes);

                        if (unableToFindIndexes != null)
                        {
                            foreach (int notDeletedIndex in unableToFindIndexes)
                            {
                                notFoundErrors += new Exception("Event with id " + eventIdsArray[notDeletedIndex].ToString() + " not found to delete");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                notFoundErrors += ex;
            }
            finally
            {
                if (!alreadyObtainedLock)
                {
                    CELocker.ExitReadLock();
                }
            }
            return notFoundErrors;
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
            // Default the output sync counter
            syncCounter = Helpers.DefaultForType<long>();
            try
            {
                // Copy event ids completed in sync to array, defaulting to an empty array
                long[] syncedEventIdsEnumerated = (syncedEventIds == null
                    ? new long[0]
                    : syncedEventIds.OrderBy(currentEventId => currentEventId).ToArray());

                CELocker.EnterWriteLock();
                try
                {
                    using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(this.indexDBLocation)))
                    {
                        indexDB.Open();

                        using (SqlCeTransaction indexTransaction = indexDB.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
                        {
                            try
                            {
                                // Retrieve last sync if it exists
                                SqlSync lastSync = SqlAccessor<SqlSync>.SelectResultSet(indexDB,
                                    "SELECT TOP 1 * FROM [Syncs] ORDER BY [Syncs].[SyncCounter] DESC",
                                    transaction: indexTransaction)
                                    .SingleOrDefault();
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
                                SqlSync newSync = new SqlSync()
                                {
                                    SyncId = syncId,
                                    RootPath = newRootPath
                                };

                                // Add the new sync to the database and store the new counter
                                syncCounter = SqlAccessor<SqlSync>.InsertRow<long>(indexDB, newSync, transaction: indexTransaction);

                                // Create the dictionary for new sync states, returning an error if it occurred
                                FilePathDictionary<Tuple<long, Nullable<long>, FileMetadata>> newSyncStates;
                                CLError newSyncStatesError = FilePathDictionary<Tuple<long, Nullable<long>, FileMetadata>>.CreateAndInitialize(newRootPath,
                                    out newSyncStates);
                                if (newSyncStatesError != null)
                                {
                                    return newSyncStatesError;
                                }

                                // Create a dictionary to store remapped server paths in case they exist
                                Dictionary<string, string> serverRemappedPaths = new Dictionary<string, string>();

                                Dictionary<long, KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>> mappedSyncStates = null;

                                // If there was a previous sync, pull the previous sync states to modify
                                if (lastSyncCounter != null)
                                {
                                    mappedSyncStates = new Dictionary<long, KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>>();

                                    // Loop through all sync states for the last sync
                                    foreach (FileSystemObject currentSyncState in SqlAccessor<FileSystemObject>
                                        .SelectResultSet(indexDB,
                                            "SELECT * FROM [FileSystemObjects] WHERE [FileSystemObjects].[SyncCounter] = " + ((long)lastSyncCounter).ToString(),
                                        transaction: indexTransaction))
                                    {
                                        if (mappedSyncStates.ContainsKey(currentSyncState.FileSystemObjectId))
                                        {
                                            if (currentSyncState.ServerLinked)
                                            {
                                                mappedSyncStates[currentSyncState.FileSystemObjectId].Value.Value = currentSyncState;
                                            }
                                            else
                                            {
                                                mappedSyncStates[currentSyncState.FileSystemObjectId].Key.Value = currentSyncState;
                                            }
                                        }
                                        else if (currentSyncState.ServerLinked)
                                        {
                                            mappedSyncStates.Add(currentSyncState.FileSystemObjectId,
                                                new KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>(
                                                    new GenericHolder<FileSystemObject>(),
                                                    new GenericHolder<FileSystemObject>(currentSyncState)));
                                        }
                                        else
                                        {
                                            mappedSyncStates.Add(currentSyncState.FileSystemObjectId,
                                                new KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>(
                                                    new GenericHolder<FileSystemObject>(currentSyncState),
                                                    new GenericHolder<FileSystemObject>()));
                                        }
                                    }

                                    // Loop through previous sync states
                                    foreach (KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>> currentState in mappedSyncStates.Values)
                                    {
                                        string localPath = currentState.Key.Value.Path;
                                        string serverPath = (currentState.Value.Value == null ? null : currentState.Value.Value.Path);

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
                                        if (currentState.Value.Value != null)
                                        {
                                            serverRemappedPaths.Add(localPath,
                                                serverPath);
                                        }

                                        // Add the previous sync state to the dictionary as the baseline before changes
                                        newSyncStates[localPath] = new Tuple<long, Nullable<long>, FileMetadata>(currentState.Key.Value.FileSystemObjectId,
                                            currentState.Key.Value.EventId,
                                            new FileMetadata()
                                            {
                                                HashableProperties = new FileMetadataHashableProperties(currentState.Key.Value.IsFolder,
                                                    currentState.Key.Value.LastTime,
                                                    currentState.Key.Value.CreationTime,
                                                    currentState.Key.Value.Size),
                                                LinkTargetPath = currentState.Key.Value.TargetPath,
                                                Revision = currentState.Key.Value.Revision,
                                                StorageKey = currentState.Key.Value.StorageKey
                                            });
                                    }
                                }

                                // Grab all events from the database since the previous sync, ordering by id to ensure correct processing logic
                                Event[] existingEvents = SqlAccessor<Event>.SelectResultSet(indexDB,
                                        "SELECT " +
                                        SqlAccessor<Event>.GetSelectColumns() + ", " +
                                        SqlAccessor<FileSystemObject>.GetSelectColumns(FileSystemObject.Name) + " " +
                                        "FROM [Events] " +
                                        "INNER JOIN [FileSystemObjects] ON [Events].[EventId] = [FileSystemObjects].[EventId] " +
                                        "WHERE [FileSystemObjects].[SyncCounter] IS NULL " +
                                        "ORDER BY [Events].[EventId]",
                                        new string[]
                                        {
                                            FileSystemObject.Name
                                        },
                                        indexTransaction)
                                    .ToArray();

                                Action<PathState, FilePath> setBadge = (badgeType, badgePath) =>
                                {
                                    MessageEvents.QueueSetBadge(this, new SetBadge(badgeType, badgePath));   // Message to invoke BadgeNet.IconOverlay.QueueSetBadge(badgeType, badgePath);

                                };

                                List<Event> eventsToUpdate = new List<Event>();
                                List<FileSystemObject> objectsToUpdate = new List<FileSystemObject>();
                                List<FileSystemObject> objectsToMoveToLastSync = new List<FileSystemObject>();

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
                                                Tuple<long, Nullable<long>, FileMetadata> previousCreatedState;
                                                KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>> previousCreatedObjects;
                                                if (newSyncStates.TryGetValue(newPath, out previousCreatedState)
                                                    && previousCreatedState.Item1 != previousEvent.FileSystemObject.FileSystemObjectId
                                                    && mappedSyncStates.TryGetValue(previousCreatedState.Item1, out previousCreatedObjects))
                                                {
                                                    if (previousCreatedObjects.Key.Value != null)
                                                    {
                                                        objectsToMoveToLastSync.Add(previousCreatedObjects.Key.Value);
                                                    }
                                                    if (previousCreatedObjects.Value.Value != null)
                                                    {
                                                        objectsToMoveToLastSync.Add(previousCreatedObjects.Value.Value);
                                                    }
                                                }

                                                newSyncStates[newPath] = new Tuple<long, Nullable<long>, FileMetadata>(previousEvent.FileSystemObject.FileSystemObjectId,
                                                    previousEvent.EventId,
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

                                                if (!existingEvents.Any(existingEvent => Array.BinarySearch(syncedEventIdsEnumerated, existingEvent.EventId) < 0
                                                    && existingEvent.FileSystemObject.Path == newPath.ToString()))
                                                {
                                                    setBadge(PathState.Synced, newPath);
                                                }
                                                break;
                                            case FileChangeType.Deleted:
                                                newSyncStates.Remove(newPath);

                                                if (previousEvent.SyncFrom)
                                                {
                                                    bool isDeleted;
                                                    MessageEvents.DeleteBadgePath(this, new DeleteBadgePath(newPath), out isDeleted);   // Message to invoke BadgeNet.IconOverlay.DeleteBadgePath(newPath, out isDeleted);
                                                }

                                                if (existingEvents.Any(existingEvent => Array.BinarySearch(syncedEventIdsEnumerated, existingEvent.EventId) < 0
                                                    && existingEvent.FileSystemObject.Path == newPath.ToString()))
                                                {
                                                    setBadge(PathState.Syncing, newPath);
                                                }
                                                break;
                                            case FileChangeType.Modified:
                                                Tuple<long, Nullable<long>, FileMetadata> previousModifiedState;
                                                KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>> previousModifiedObjects;
                                                if (newSyncStates.TryGetValue(newPath, out previousModifiedState)
                                                    && previousModifiedState.Item1 != previousEvent.FileSystemObject.FileSystemObjectId
                                                    && mappedSyncStates.TryGetValue(previousModifiedState.Item1, out previousModifiedObjects))
                                                {
                                                    if (previousModifiedObjects.Key.Value != null)
                                                    {
                                                        objectsToMoveToLastSync.Add(previousModifiedObjects.Key.Value);
                                                    }
                                                    if (previousModifiedObjects.Value.Value != null)
                                                    {
                                                        objectsToMoveToLastSync.Add(previousModifiedObjects.Value.Value);
                                                    }
                                                }

                                                newSyncStates[newPath] =
                                                    new Tuple<long, Nullable<long>, FileMetadata>(previousEvent.FileSystemObject.FileSystemObjectId,
                                                        previousEvent.EventId,
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

                                                if (!existingEvents.Any(existingEvent => Array.BinarySearch(syncedEventIdsEnumerated, existingEvent.EventId) < 0
                                                    && existingEvent.FileSystemObject.Path == newPath.ToString()))
                                                {
                                                    setBadge(PathState.Synced, newPath);
                                                }
                                                break;
                                            case FileChangeType.Renamed:
                                                Tuple<long, Nullable<long>, FileMetadata> previousNewPathState;
                                                Tuple<long, Nullable<long>, FileMetadata> previousOldPathState;
                                                if (newSyncStates.TryGetValue(newPath, out previousNewPathState))
                                                {
                                                    if (newSyncStates.ContainsKey(oldPath))
                                                    {
                                                        newSyncStates.Remove(oldPath);
                                                    }

                                                    KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>> previousNewPathObjects;

                                                    if (previousNewPathState.Item1 != previousEvent.FileSystemObject.FileSystemObjectId
                                                        && mappedSyncStates.TryGetValue(previousNewPathState.Item1, out previousNewPathObjects))
                                                    {
                                                        if (previousNewPathObjects.Key.Value != null)
                                                        {
                                                            objectsToMoveToLastSync.Add(previousNewPathObjects.Key.Value);
                                                        }
                                                        if (previousNewPathObjects.Value.Value != null)
                                                        {
                                                            objectsToMoveToLastSync.Add(previousNewPathObjects.Value.Value);
                                                        }
                                                    }
                                                }
                                                else if (newSyncStates.TryGetValue(oldPath, out previousOldPathState))
                                                {
                                                    newSyncStates.Rename(oldPath, newPath);

                                                    KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>> previousOldPathObjects;

                                                    if (previousOldPathState.Item1 != previousEvent.FileSystemObject.FileSystemObjectId
                                                        && mappedSyncStates.TryGetValue(previousOldPathState.Item1, out previousOldPathObjects))
                                                    {
                                                        if (previousOldPathObjects.Key.Value != null)
                                                        {
                                                            objectsToMoveToLastSync.Add(previousOldPathObjects.Key.Value);
                                                        }
                                                        if (previousOldPathObjects.Value.Value != null)
                                                        {
                                                            objectsToMoveToLastSync.Add(previousOldPathObjects.Value.Value);
                                                        }
                                                    }
                                                }

                                                newSyncStates[newPath] =
                                                    new Tuple<long, Nullable<long>, FileMetadata>(previousEvent.FileSystemObject.FileSystemObjectId,
                                                        previousEvent.EventId,
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

                                                if (previousEvent.SyncFrom)
                                                {
                                                    MessageEvents.RenameBadgePath(this, new RenameBadgePath(oldPath, newPath));   // Message to invoke BadgeNet.IconOverlay.RenameBadgePath(oldPath, newPath);
                                                }

                                                if (!existingEvents.Any(existingEvent => Array.BinarySearch(syncedEventIdsEnumerated, existingEvent.EventId) < 0
                                                    && existingEvent.FileSystemObject.Path == newPath.ToString()))
                                                {
                                                    setBadge(PathState.Synced, newPath);
                                                }
                                                break;
                                        }
                                    }
                                    // Else if the previous database event is not in the list of completed events,
                                    // The event will get moved to after the current sync so it will be processed later
                                    else
                                    {
                                        if (syncStatesNeedRemap)
                                        {
                                            if (!FilePathComparer.Instance.Equals(previousEvent.FileSystemObject.Path, newPath))
                                            {
                                                previousEvent.FileSystemObject.Path = newPath;

                                                objectsToUpdate.Add(previousEvent.FileSystemObject);
                                            }
                                            previousEvent.PreviousPath = oldPath;

                                            eventsToUpdate.Add(previousEvent);
                                        }
                                    }
                                }

                                //// what was this for?
                                //bool atLeastOneServerLinked = false;

                                // Loop through modified set of sync states (including new changes) and add the matching database objects
                                foreach (KeyValuePair<FilePath, Tuple<long, Nullable<long>, FileMetadata>> newSyncState in newSyncStates)
                                {
                                    string newPathString = newSyncState.Key.ToString();

                                    // Add the file/folder object for the current sync state
                                    objectsToUpdate.Add(new FileSystemObject()
                                    {
                                        CreationTime = (newSyncState.Value.Item3.HashableProperties.CreationTime.Ticks == FileConstants.InvalidUtcTimeTicks
                                            ? (Nullable<DateTime>)null
                                            : newSyncState.Value.Item3.HashableProperties.CreationTime),
                                        IsFolder = newSyncState.Value.Item3.HashableProperties.IsFolder,
                                        LastTime = (newSyncState.Value.Item3.HashableProperties.LastTime.Ticks == FileConstants.InvalidUtcTimeTicks
                                            ? (Nullable<DateTime>)null
                                            : newSyncState.Value.Item3.HashableProperties.LastTime),
                                        Path = newPathString,
                                        Size = newSyncState.Value.Item3.HashableProperties.Size,
                                        TargetPath = (newSyncState.Value.Item3.LinkTargetPath == null ? null : newSyncState.Value.Item3.LinkTargetPath.ToString()),
                                        Revision = newSyncState.Value.Item3.Revision,
                                        StorageKey = newSyncState.Value.Item3.StorageKey,
                                        SyncCounter = syncCounter,
                                        ServerLinked = false,
                                        FileSystemObjectId = newSyncState.Value.Item1,
                                        EventId = newSyncState.Value.Item2,

                                        // SQL CE does not support computed columns, so no "AS CHECKSUM(Path)"
                                        PathChecksum = StringCRC.Crc(newPathString)
                                    });

                                    // If the file/folder path is remapped on the server, add the file/folder object for the server-mapped state
                                    if (serverRemappedPaths.ContainsKey(newPathString))
                                    {
                                        //// what was this for?
                                        //atLeastOneServerLinked = true;

                                        objectsToUpdate.Add(new FileSystemObject()
                                        {
                                            CreationTime = (newSyncState.Value.Item3.HashableProperties.CreationTime.Ticks == FileConstants.InvalidUtcTimeTicks
                                                ? (Nullable<DateTime>)null
                                                : newSyncState.Value.Item3.HashableProperties.CreationTime),
                                            IsFolder = newSyncState.Value.Item3.HashableProperties.IsFolder,
                                            LastTime = (newSyncState.Value.Item3.HashableProperties.LastTime.Ticks == FileConstants.InvalidUtcTimeTicks
                                                ? (Nullable<DateTime>)null
                                                : newSyncState.Value.Item3.HashableProperties.LastTime),
                                            Path = serverRemappedPaths[newPathString],
                                            Size = newSyncState.Value.Item3.HashableProperties.Size,
                                            TargetPath = (newSyncState.Value.Item3.LinkTargetPath == null ? null : newSyncState.Value.Item3.LinkTargetPath.ToString()),
                                            Revision = newSyncState.Value.Item3.Revision,
                                            StorageKey = newSyncState.Value.Item3.StorageKey,
                                            SyncCounter = syncCounter,
                                            ServerLinked = true,
                                            FileSystemObjectId = newSyncState.Value.Item1,
                                            EventId = newSyncState.Value.Item2,

                                            // SQL CE does not support computed columns, so no "AS CHECKSUM(Path)"
                                            PathChecksum = StringCRC.Crc(serverRemappedPaths[newPathString])
                                        });
                                    }
                                }

                                // Define field that will be output for indexes not updated
                                IEnumerable<int> unableToFindIndexes;

                                // Update Events that were queued for modification
                                SqlAccessor<Event>.UpdateRows(indexDB,
                                    eventsToUpdate,
                                    out unableToFindIndexes,
                                    transaction: indexTransaction);

                                // Update FileSystemObjects that were queued for modification
                                SqlAccessor<FileSystemObject>.UpdateRows(indexDB,
                                    objectsToUpdate,
                                    out unableToFindIndexes,
                                    transaction: indexTransaction);

                                // If any FileSystemObjects were not found to update,
                                // then they need to be inserted
                                if (unableToFindIndexes != null
                                    && unableToFindIndexes.Count() > 0)
                                {
                                    // Insert new FileSystemObjects with IDENTITY_INSERT ON (will force server-linked sync state to have a matching identity to the non-server-linked sync state)
                                    SqlAccessor<FileSystemObject>.InsertRows(indexDB,
                                        unableToFindIndexes.Select(currentInsert => objectsToUpdate[currentInsert]),
                                        true,
                                        transaction: indexTransaction);
                                }

                                if (objectsToMoveToLastSync.Count > 0)
                                {
                                    FileSystemObject[] needsMove = objectsToMoveToLastSync.Where(currentObjectToMove => lastSyncCounter == null
                                        || currentObjectToMove.SyncCounter == lastSyncCounter).ToArray();

                                    if (needsMove.Length > 0)
                                    {
                                        SqlAccessor<FileSystemObject>.UpdateRows(indexDB,
                                            needsMove.Select(currentObjectToMove =>
                                                    ((Func<FileSystemObject, Nullable<long>, FileSystemObject>)((toMove, lastCounter) =>
                                                    {
                                                        if (lastCounter == null)
                                                        {
                                                            toMove.SyncCounter = null;
                                                            toMove.EventId = null;
                                                        }
                                                        else
                                                        {
                                                            toMove.SyncCounter = lastCounter;
                                                        }
                                                        return toMove;
                                                    }))(currentObjectToMove, lastSyncCounter)),
                                                out unableToFindIndexes,
                                                transaction: indexTransaction);
                                    }
                                }

                                indexTransaction.Commit(CommitMode.Immediate);
                            }
                            catch
                            {
                                indexTransaction.Rollback();

                                throw;
                            }
                        }
                    }

                    // update the exposed last sync id upon sync completion
                    LastSyncLocker.EnterWriteLock();
                    try
                    {
                        this.LastSyncId = syncId;
                    }
                    finally
                    {
                        LastSyncLocker.ExitWriteLock();
                    }
                }
                finally
                {
                    CELocker.ExitWriteLock();
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
                CELocker.EnterWriteLock();
                try
                {
                    using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(this.indexDBLocation)))
                    {
                        indexDB.Open();

                        using (SqlCeTransaction indexTransaction = indexDB.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
                        {
                            // It does not matter functionally if there was an error deleting an event (it may be orphaned without causing a problem),
                            // so we delete events outside of the main transaction which can be allowed to fail
                            IEnumerable<Event> deleteEvents;
                                IEnumerable<int> unableToFindIndexes;

                            try
                            {
                                IEnumerable<FileSystemObject> pendingToDelete = SqlAccessor<FileSystemObject>.SelectResultSet(indexDB,
                                    "SELECT " + SqlAccessor<FileSystemObject>.GetSelectColumns() + ", " +
                                        SqlAccessor<Event>.GetSelectColumns("Event") + " " +
                                        "FROM [FileSystemObjects] " +
                                        "LEFT OUTER JOIN [Events] ON [FileSystemObjects].[EventId] = [Events].[EventId] " +
                                        "WHERE  [FileSystemObjects].[SyncCounter] IS NULL ",
                                    new string[] { "Event" },
                                    indexTransaction).ToArray();

                                SqlAccessor<FileSystemObject>.DeleteRows(indexDB,
                                    pendingToDelete,
                                    out unableToFindIndexes,
                                    transaction: indexTransaction);

                                deleteEvents = pendingToDelete.Where(toDelete => toDelete.EventId != null)
                                    .Select(toDelete => toDelete.Event);

                                SqlAccessor<SqlSync>.InsertRow(indexDB,
                                    new SqlSync()
                                    {
                                        SyncId = IdForEmptySync,
                                        RootPath = newRootPath
                                    },
                                    transaction: indexTransaction);

                                indexTransaction.Commit(CommitMode.Immediate);
                            }
                            catch
                            {
                                indexTransaction.Rollback();

                                throw;
                            }

                            SqlAccessor<Event>.DeleteRows(indexDB,
                                deleteEvents,
                                out unableToFindIndexes);
                        }
                    }
                }
                finally
                {
                    CELocker.ExitWriteLock();
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
                            CELocker.EnterReadLock();
                        }
                        try
                        {
                            using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(this.indexDBLocation)))
                            {
                                if (toDelete.Count > 0)
                                {
                                    CLError deleteErrors = RemoveEventsByIds(toDelete, true);

                                    if (deleteErrors != null)
                                    {
                                        toReturn += new AggregateException("Error deleting some events", deleteErrors.GrabExceptions());
                                    }
                                }

                                if (toUpdate.Count > 0)
                                {
                                    Dictionary<long, KeyValuePair<FileChange, GenericHolder<bool>>> findOriginal = toUpdate.ToDictionary(currentToUpdate => currentToUpdate.Key,
                                        currentToUpdate => new KeyValuePair<FileChange, GenericHolder<bool>>(currentToUpdate.Value, new GenericHolder<bool>(false)));

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
                                            toReturn += new Exception("Unable to find event with id " + missingEventId.ToString() + " to update");
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
                                                toReturn += new Exception("Unable to find event with id " + mergedPair.Key.EventId.ToString() + " to update");
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
                                                toReturn += new Exception("Unable to find event with id " + missingEventId.ToString() + " to update");
                                            }
                                        }

                                        SqlAccessor<FileSystemObject>.UpdateRows(indexDB,
                                            toModify.Select(currentToModify => currentToModify.FileSystemObject),
                                            out unableToFindIndexes);

                                        if (unableToFindIndexes != null)
                                        {
                                            foreach (long missingFileSystemObjectId in unableToFindIndexes.Select(currentUnableToFind => toModify[currentUnableToFind].FileSystemObject.FileSystemObjectId))
                                            {
                                                toReturn += new Exception("Unable to find file system object with id " + missingFileSystemObjectId.ToString() + " to update");
                                            }
                                        }
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
                                CELocker.ExitReadLock();
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
            try
            {
                Event currentEvent = null;              // scope outside for badging reference
                CELocker.EnterWriteLock();
                try
                {
                    using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(this.indexDBLocation)))
                    {
                        indexDB.Open();

                        using (SqlCeTransaction indexTransaction = indexDB.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
                        {
                            try
                            {
                                // grab the event from the database by provided id
                                currentEvent = SqlAccessor<Event>.SelectResultSet(indexDB,
                                        "SELECT TOP 1 " +
                                        SqlAccessor<Event>.GetSelectColumns() + ", " +
                                        SqlAccessor<FileSystemObject>.GetSelectColumns(FileSystemObject.Name) + " " +
                                        "FROM [Events] " +
                                        "INNER JOIN [FileSystemObjects] ON [Events].[EventId] = [FileSystemObjects].[EventId] " +
                                        "WHERE [FileSystemObjects].[SyncCounter] IS NULL " +
                                        "AND [Events].[EventId] = " + eventId.ToString(),
                                        new string[]
                                        {
                                            FileSystemObject.Name
                                        },
                                        indexTransaction)
                                    .SingleOrDefault();
                                // ensure an event was found
                                if (currentEvent == null)
                                {
                                    throw new KeyNotFoundException("Previous event not found or not pending for given id: " + eventId.ToString());
                                }

                                // Grab the most recent sync from the database to pull sync states
                                SqlSync lastSync = SqlAccessor<SqlSync>.SelectResultSet(indexDB,
                                        "SELECT TOP 1 * FROM [Syncs] ORDER BY [Syncs].[SyncCounter] DESC",
                                        transaction: indexTransaction)
                                    .SingleOrDefault();

                                // ensure a previous sync was found
                                if (lastSync == null)
                                {
                                    throw new Exception("Previous sync not found for completed event");
                                }

                                // declare fields to store server paths in case they are different than the local paths,
                                // defaulting to null
                                string serverRemappedNewPath = null;
                                string serverRemappedOldPath = null;

                                int crcInt = StringCRC.Crc(currentEvent.FileSystemObject.Path);
                                Nullable<int> crcIntOld = (currentEvent.FileChangeTypeEnumId == changeEnumsBackward[FileChangeType.Renamed]
                                        && currentEvent.PreviousPath != null
                                    ? StringCRC.Crc(currentEvent.PreviousPath)
                                    : (Nullable<int>)null);

                                // pull the sync states for the new path of the current event
                                IEnumerable<FileSystemObject> existingObjectsAtPath = SqlAccessor<FileSystemObject>.SelectResultSet(indexDB,
                                            "SELECT * " +
                                            "FROM [FileSystemObjects] " +
                                            "WHERE [FileSystemObjects].[SyncCounter] = " + lastSync.SyncCounter.ToString() + " " +
                                            "AND [FileSystemObjects].[ServerLinked] = 0 AND " +
                                            (crcIntOld == null
                                                ? "[FileSystemObjects].[PathChecksum] = " + crcInt.ToString()
                                                : "([FileSystemObjects].[PathChecksum] = " + crcInt.ToString() + " OR [FileSystemObjects].[PathChecksum] = " + ((int)crcIntOld).ToString() + ")"),
                                        transaction: indexTransaction)
                                    .Where(currentSyncState =>
                                        (crcIntOld == null
                                            ? currentSyncState.Path == currentEvent.FileSystemObject.Path
                                            : (currentSyncState.Path == currentEvent.FileSystemObject.Path || currentSyncState.Path == currentEvent.PreviousPath))); // run in memory since Path field is not indexable

                                if (existingObjectsAtPath != null
                                    && existingObjectsAtPath.Any())
                                {
                                    // add in server-linked objects
                                    existingObjectsAtPath = existingObjectsAtPath.Concat(SqlAccessor<FileSystemObject>.SelectResultSet(indexDB,
                                            "SELECT * FROM [FileSystemObjects] WHERE [FileSystemObjects].[ServerLinked] = 1 AND [FileSystemObjects].[FileSystemObjectId] IN (" +
                                            string.Join(", ", existingObjectsAtPath.Select(currentExistingObject => currentExistingObject.FileSystemObjectId.ToString()).ToArray()) + ")",
                                        transaction: indexTransaction));
                                }

                                Dictionary<long, KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>> newPathStates = new Dictionary<long, KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>>();

                                foreach (FileSystemObject currentExistingObject in existingObjectsAtPath)
                                {
                                    if (newPathStates.ContainsKey(currentExistingObject.FileSystemObjectId))
                                    {
                                        if (currentExistingObject.ServerLinked)
                                        {
                                            newPathStates[currentExistingObject.FileSystemObjectId].Value.Value = currentExistingObject;
                                        }
                                        else
                                        {
                                            newPathStates[currentExistingObject.FileSystemObjectId].Key.Value = currentExistingObject;
                                        }
                                    }
                                    else if (currentExistingObject.ServerLinked)
                                    {
                                        newPathStates.Add(currentExistingObject.FileSystemObjectId,
                                            new KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>(
                                                new GenericHolder<FileSystemObject>(),
                                                new GenericHolder<FileSystemObject>(currentExistingObject)));
                                    }
                                    else
                                    {
                                        newPathStates.Add(currentExistingObject.FileSystemObjectId,
                                            new KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>(
                                                new GenericHolder<FileSystemObject>(currentExistingObject),
                                                new GenericHolder<FileSystemObject>()));
                                    }
                                }

                                List<FileSystemObject> toDelete = new List<FileSystemObject>();

                                foreach (KeyValuePair<long, KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>> currentPreviousState in
                                    newPathStates.OrderBy(orderState => orderState.Key))
                                {
                                    if (currentPreviousState.Value.Key.Value.Path == currentEvent.FileSystemObject.Path)
                                    {
                                        if (currentPreviousState.Value.Value.Value != null)
                                        {
                                            serverRemappedNewPath = currentPreviousState.Value.Value.Value.Path;

                                            toDelete.Add(currentPreviousState.Value.Value.Value);
                                        }
                                    }
                                    else if (currentPreviousState.Value.Value.Value != null)
                                    {
                                        serverRemappedOldPath = currentPreviousState.Value.Value.Value.Path;

                                        toDelete.Add(currentPreviousState.Value.Value.Value);
                                    }

                                    toDelete.Add(currentPreviousState.Value.Key.Value);
                                }

                                if (toDelete.Count > 0)
                                {
                                    // remove the current sync state(s)
                                    IEnumerable<int> unableToFindIndexes;
                                    SqlAccessor<FileSystemObject>.DeleteRows(indexDB, toDelete, out unableToFindIndexes, transaction: indexTransaction);
                                }

                                // if the change type of the current event is not a deletion,
                                // then a new sync state needs to be created for the event and added to the database
                                if (changeEnums[currentEvent.FileChangeTypeEnumId] != FileChangeType.Deleted)
                                {
                                    // function to produce a new file system object to store in the database for the event's object;
                                    // dynamically sets the path from input
                                    Func<string, bool, FileSystemObject> getNewFileSystemObject = (fileSystemPath, serverLinked) =>
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
                                                StorageKey = currentEvent.FileSystemObject.StorageKey,
                                                EventId = eventId,
                                                FileSystemObjectId = currentEvent.FileSystemObject.FileSystemObjectId,
                                                ServerLinked = serverLinked,
                                                SyncCounter = lastSync.SyncCounter,

                                                // SQL CE does not support computed columns, so no "AS CHECKSUM(Path)"
                                                PathChecksum = StringCRC.Crc(fileSystemPath)
                                            };
                                        };

                                    // create the file system object for the local path for the event
                                    FileSystemObject eventFileSystemObject = getNewFileSystemObject(currentEvent.FileSystemObject.Path, false);

                                    if (serverRemappedNewPath != null)
                                    {
                                        // create the file system object for the server remapped path for the event
                                        FileSystemObject serverRemappedFileSystemObject = getNewFileSystemObject(serverRemappedNewPath, true);

                                        // apply the events to the Sync
                                        IEnumerable<int> unableToFindIndexes;
                                        SqlAccessor<FileSystemObject>.UpdateRows(indexDB, new FileSystemObject[] { eventFileSystemObject, serverRemappedFileSystemObject }, out unableToFindIndexes, transaction: indexTransaction);
                                    }
                                    else
                                    {
                                        // apply the event to the Sync
                                        SqlAccessor<FileSystemObject>.UpdateRow(indexDB, eventFileSystemObject, transaction: indexTransaction);
                                    }
                                }

                                indexTransaction.Commit(CommitMode.Immediate);
                            }
                            catch
                            {
                                indexTransaction.Rollback();

                                throw;
                            }
                        }
                    }
                }
                finally
                {
                    CELocker.ExitWriteLock();
                }

                Action<FilePath> setBadgeSynced = syncedPath =>
                    {
                        MessageEvents.QueueSetBadge(this, new SetBadge(PathState.Synced, syncedPath));   // Message to invoke BadgeNet.IconOverlay.QueueSetBadge(PathState.Synced, syncedPath);
                    };

                // Adjust the badge for this completed event.
                if (currentEvent != null)
                {
                    switch (changeEnums[currentEvent.FileChangeTypeEnumId])
                    {
                        case FileChangeType.Created:
                        case FileChangeType.Modified:
                            setBadgeSynced(currentEvent.FileSystemObject.Path);
                            break;
                        case FileChangeType.Deleted:
                            if (currentEvent.SyncFrom)
                            {
                                bool isDeleted;
                                MessageEvents.DeleteBadgePath(this, new DeleteBadgePath(currentEvent.FileSystemObject.Path), out isDeleted);   // Message to invoke BadgeNet.IconOverlay.DeleteBadgePath(currentEvent.FileSystemObject.Path, out isDeleted);
                            }
                            break;
                        case FileChangeType.Renamed:
                            if (currentEvent.SyncFrom)
                            {
                                MessageEvents.RenameBadgePath(this, new RenameBadgePath(currentEvent.PreviousPath, currentEvent.FileSystemObject.Path));   // Message to invoke BadgeNet.IconOverlay.RenameBadgePath(currentEvent.PreviousPath, currentEvent.FileSystemObject.Path);
                            }

                            setBadgeSynced(currentEvent.FileSystemObject.Path);
                            break;
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
        private IndexingAgent()
        {
            this.indexDBLocation = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create) +
                "\\Cloud\\IndexDB.sdf";
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

            using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(this.indexDBLocation)))
            {
                // Grab the most recent sync from the database to pull sync states
                SqlSync lastSync = SqlAccessor<SqlSync>.SelectResultSet(indexDB,
                    "SELECT TOP 1 * FROM [Syncs] ORDER BY [Syncs].[SyncCounter] DESC")
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
                        : lastSync.SyncId);
                }
                finally
                {
                    LastSyncLocker.ExitWriteLock();
                }

                // Create a list for changes that need to be processed after the last sync
                List<FileChange> changeList = new List<FileChange>();

                // If there was a previous sync, use it as the basis for the starting index before calculating changes to apply
                if (lastSync != null)
                {
                    // Loop through the sync states for the last sync
                    foreach (FileSystemObject currentSyncState in SqlAccessor<FileSystemObject>.SelectResultSet(indexDB,
                        "SELECT * FROM [FileSystemObjects] WHERE [FileSystemObjects].[SyncCounter] = " + lastSync.SyncCounter.ToString()))
                    {
                        // Add the previous sync state to the initial index
                        FileMetadata currentToAdd = new FileMetadata()
                            {
                                HashableProperties = new FileMetadataHashableProperties(currentSyncState.IsFolder,
                                    currentSyncState.LastTime,
                                    currentSyncState.CreationTime,
                                    currentSyncState.Size),
                                LinkTargetPath = currentSyncState.TargetPath,
                                Revision = currentSyncState.Revision,
                                StorageKey = currentSyncState.StorageKey
                            };

                        indexPaths.Add(currentSyncState.Path, currentToAdd);
                        combinedIndexPlusChanges.Add(currentSyncState.Path, currentToAdd);
                    }
                }

                lock (changeEnumsLocker)
                {
                    // Loop through database events since the last sync to add changes
                    foreach (Event currentEvent in SqlAccessor<Event>.SelectResultSet(indexDB,
                        "SELECT " +
                        SqlAccessor<Event>.GetSelectColumns() + ", " +
                        SqlAccessor<FileSystemObject>.GetSelectColumns(FileSystemObject.Name) + " " +
                        "FROM [Events] " +
                        "INNER JOIN [FileSystemObjects] ON [Events].[EventId] = [FileSystemObjects].[EventId] " +
                        "WHERE [FileSystemObjects].[SyncCounter] IS NULL " +
                        "ORDER BY [Events].[EventId]",
                        new string[]
                        {
                            FileSystemObject.Name
                        }))
                    {
                        // Add database event to list of changes
                        FileMetadata changeMetadata = new FileMetadata()
                            {
                                HashableProperties = new FileMetadataHashableProperties(currentEvent.FileSystemObject.IsFolder,
                                    currentEvent.FileSystemObject.LastTime,
                                    currentEvent.FileSystemObject.CreationTime,
                                    currentEvent.FileSystemObject.Size),
                                LinkTargetPath = currentEvent.FileSystemObject.TargetPath,
                                Revision = currentEvent.FileSystemObject.Revision,
                                StorageKey = currentEvent.FileSystemObject.StorageKey
                            };

                        FilePath pathObject = currentEvent.FileSystemObject.Path;
                        FilePath oldPathObject = currentEvent.PreviousPath;

						changeList.Add(new FileChange()
                        {
                            Metadata = changeMetadata,
                            NewPath = pathObject,
                            OldPath = oldPathObject,
                            Type = changeEnums[currentEvent.FileChangeTypeEnumId],
                            DoNotAddToSQLIndex = true,
                            EventId = currentEvent.EventId
                        });

                        switch (changeEnums[currentEvent.FileChangeTypeEnumId])
                        {
                            case FileChangeType.Modified:
                            case FileChangeType.Created:
                                combinedIndexPlusChanges[pathObject] = changeMetadata;

                                GenericHolder<bool> reverseDeletion;
                                if (pathDeletions.TryGetValue(pathObject, out reverseDeletion))
                                {
                                    reverseDeletion.Value = false;
                                }
                                break;
                            case FileChangeType.Deleted:
                                if (combinedIndexPlusChanges.Remove(pathObject))
                                {
                                    pathDeletions.Remove(pathObject);
                                    pathDeletions.Add(pathObject, new GenericHolder<bool>(true));
                                }
                                break;
                            case FileChangeType.Renamed:
                                if (combinedIndexPlusChanges.ContainsKey(oldPathObject))
                                {
                                    FilePathHierarchicalNode<FileMetadata> newRename;
                                    CLError hierarchyError = combinedIndexPlusChanges.GrabHierarchyForPath(pathObject, out newRename, true);
                                    if (hierarchyError == null
                                        && newRename == null)
                                    {
                                        combinedIndexPlusChanges.Rename(oldPathObject, pathObject);
                                        combinedIndexPlusChanges[pathObject] = changeMetadata;

                                        FilePathHierarchicalNode<GenericHolder<bool>> newDeletion;
                                        CLError deletionHierarchyError = pathDeletions.GrabHierarchyForPath(pathObject, out newDeletion, true);

                                        if (deletionHierarchyError == null
                                            && newDeletion == null)
                                        {
                                            GenericHolder<bool> previousDeletion;
                                            if (pathDeletions.TryGetValue(oldPathObject, out previousDeletion))
                                            {
                                                previousDeletion.Value = false;
                                            }
                                            else
                                            {
                                                pathDeletions.Add(oldPathObject, new GenericHolder<bool>(false));
                                            }

                                            pathDeletions.Rename(oldPathObject, pathObject);
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }

                // Define DirectoryInfo at current path which will be traversed
                DirectoryInfo indexRootPath = new DirectoryInfo(indexedPath);

                // RecurseIndexDirectory both adds the new changes to the list that are found on disc
                // and returns a list of all paths traversed for comparison to the existing index
                string[] recursedIndexes = RecurseIndexDirectory(changeList,
                    indexPaths,
                    combinedIndexPlusChanges,
                    this.RemoveEventById,
                    indexedPath).ToArray();

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
                        Metadata = indexPaths[deletedPath]
                    });
                    pathDeletions.Remove(deletedPath);
                    pathDeletions.Add(deletedPath, new GenericHolder<bool>(true));
                }

                // Only add possible deletion if a parent wasn't already marked as deleted
                foreach (FileChange possibleDeletion in possibleDeletions)
                {
                    bool foundDeletedParent = false;
                    FilePath levelToCheck = possibleDeletion.NewPath;
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
                    MessageBox.Show("Unable to find Cloud directory at path: " + currentDirectoryFullPath, "Error Starting Cloud");
                    return filePathsFound;
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
                        (subDirectory.LastWriteTime == null ? (Nullable<DateTime>)null : ((DateTime)subDirectory.LastWriteTime).DropSubSeconds()),
                        (subDirectory.CreationTime == null ? (Nullable<DateTime>)null : ((DateTime)subDirectory.CreationTime).DropSubSeconds()),
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
                            Metadata = newDirectoryMetadata
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
                                    HashableProperties = compareProperties,
                                    LinkTargetPath = existingFileMetadata.LinkTargetPath,//Todo: needs to check again for new target path
                                    Revision = existingFileMetadata.Revision,
                                    StorageKey = existingFileMetadata.StorageKey
                                };

                            changeList.Add(new FileChange()
                            {
                                NewPath = currentFilePathObject,
                                Type = FileChangeType.Modified,
                                Metadata = modifiedMetadata
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
                            Metadata = fileCreatedMetadata
                        });

                        combinedIndexPlusChanges.Add(currentFilePathObject, fileCreatedMetadata);
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                if (outermostMethodCall)
                {
                    MessageBox.Show("Unable to scan files/folders in Cloud folder. Location not accessible:" + Environment.NewLine + ex.Message, "Error Starting Cloud");
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
                    CELocker.Dispose();
                }
            }
        }
        #endregion
    }
}