//
// MonitorAgent.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cloud.Model;
using Cloud.Static;
using Cloud.Support;
using System.Globalization;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Threading;
// the following linq namespace is used only if the optional initialization parameter for processing logging is passed as true
using System.Xml.Linq;
using System.Transactions;
using Cloud.FileMonitor.SyncImplementation;
using Cloud.Interfaces;
using Cloud.SQLIndexer;
using JsonContracts = Cloud.JsonContracts;
using Cloud.Sync;
using Cloud.REST;
using Cloud.Model.EventMessages.ErrorInfo;
using Cloud.SQLIndexer.Model;

/// <summary>
/// Monitor a local file system folder as a Syncbox.
/// </summary>
namespace Cloud.FileMonitor
{
    /// <summary>
    /// Class to cover file monitoring; created with delegates to connect to the SQL indexer and to start Sync communication for new events
    /// </summary>
    internal sealed class MonitorAgent : IDisposable
    {
        #region public properties
        /// <summary>
        /// Retrieves running status of monitor as enum for each part (file and folder)
        /// </summary>
        /// <param name="status">Returned running status</param>
        /// <returns>Error while retrieving status, if any</returns>
        private CLError GetRunningStatus(out MonitorRunning status)
        {
            try
            {
                status = this.RunningStatus;
            }
            catch (Exception ex)
            {
                status = Helpers.DefaultForType<MonitorRunning>();
                return ex;
            }
            return null;
        }
        private MonitorRunning RunningStatus
        {
            get
            {
                // Bitwise combine whether the Folder or File watchers are running into the returned Enum
                return (FolderWatcher == null
                    ? MonitorRunning.NotRunning
                    : MonitorRunning.FolderOnlyRunning)
                    | (FileWatcher == null
                        ? MonitorRunning.NotRunning
                        : MonitorRunning.FileOnlyRunning);
            }
        }
        /// <summary>
        /// Retrieves current folder path of monitored root
        /// </summary>
        /// <returns>Root path</returns>
        public string GetCurrentPath()
        {
            return CurrentFolderPath;
        }

        private readonly ReaderWriterLockSlim InitialIndexLocker = new ReaderWriterLockSlim();

        public ISyncDataObject SyncData
        {
            get
            {
                return _syncData;
            }
        }
        #endregion

        #region private fields and property
        // stores the optional FileChange queueing callback intialization parameter
        private Action<MonitorAgent, FileChange> OnQueueing;

        private Func<bool, CLError> SyncRun = null;
        private Func<string, KeyValuePair<FilePathDictionary<List<FileChange>>, CLError>> GetUploadDownloadTransfersInProgress = null;

        private GenericHolder<bool> SyncRunLocker = new GenericHolder<bool>(false);
        private GenericHolder<bool> NextSyncQueued = new GenericHolder<bool>(false);

        private readonly IndexingAgent Indexer;
        private readonly ISyncDataObject _syncData;

        // store the optional logging boolean initialization parameter
        private bool LogProcessingFileChanges;

        private static readonly CLTrace _trace = CLTrace.Instance;

        // file extension for shortcuts
        private const string ShortcutExtension = "lnk";

        // Store initial folder path, its length is used to rebuild paths so they are consistent after root folder is moved/renamed
        private string InitialFolderPath;

        // Store currently monitored folder path, append to the relative paths of files/folders for the correct path
        private string CurrentFolderPath;

        // Locker allowing simultaneous reads on CurrentFolderPath and only locking on rare condition when root folder path is changed
        private ReaderWriterLockSlim CurrentFolderPathLocker = new ReaderWriterLockSlim();

        // System objects that runs the file system monitoring (FolderWatcher for folder renames, FileWatcher for all files and folders that aren't renamed):
        private FileSystemWatcher FolderWatcher = null;
        private FileSystemWatcher FileWatcher = null;

        // This sets whether the monitoring will ignore modify events on folders (which occur only when files inside the folders change, but not the folder itself)
        private const bool IgnoreFolderModifies = true;

        // This sets the processing delay for file changes, use 0 for no delay
        private const int ProcessingDelayInMilliseconds = 2000;

        // This sets the amount of resets that can be performed on a file change before it gets processed anyways, use a negative number for unlimited, use 0 to process immediately on reset trigger
        private const int ProcessingDelayMaxResets = 500;

        // This stores if this current monitor instance has been disposed (defaults to not disposed)
        private bool Disposed = false;

        #region Storage of current file indexes, keyed by file path

        private FilePathDictionary<FileMetadata> AllPaths;
        private readonly Dictionary<long, FilePath[]> FolderCreationTimeUtcTicksToPath = new Dictionary<long, FilePath[]>();

        private enum ChangeAllPathsType : byte
        {
            Add,
            Remove,
            Rename,
            Clear,
            IndexSet
        }
        /// <summary>
        /// Must already be holding lock on AllPaths
        /// </summary>
        private abstract class ChangeAllPathsBase
        {
            protected ChangeAllPathsBase(ChangeAllPathsType Type, MonitorAgent Agent, FilePath OldPath = null, FilePath NewPath = null, FileMetadata Metadata = null/*, Nullable<FileMetadataHashableProperties> NewHashables = null*/) // NewHashables commented out because ChangeMetadataHashableProperties was not needed
            {
                switch (Type)
                {
                    case ChangeAllPathsType.Add:
                        Agent.AllPaths.Add(NewPath, Metadata);

                        if (Metadata.HashableProperties.IsFolder)
                        {
                            long createItemCreationUtcTicks = Metadata.HashableProperties.CreationTime.ToUniversalTime().Ticks;
                            FilePath[] existingAddFolderPaths;
                            if (Agent.FolderCreationTimeUtcTicksToPath.TryGetValue(createItemCreationUtcTicks, out existingAddFolderPaths))
                            {
                                FilePath[] appendExistingPaths = new FilePath[existingAddFolderPaths.Length + 1];
                                Array.Copy(existingAddFolderPaths, appendExistingPaths, existingAddFolderPaths.Length);
                                appendExistingPaths[appendExistingPaths.Length - 1] = NewPath;
                                Agent.FolderCreationTimeUtcTicksToPath[createItemCreationUtcTicks] = appendExistingPaths;
                            }
                            else
                            {
                                Agent.FolderCreationTimeUtcTicksToPath.Add(createItemCreationUtcTicks, new[] { NewPath });
                            }
                        }
                        break;

                    //// no cases where the hashable properties are replaced (and thus the creation time) for a folder in AllPaths, so this case was not needed
                    //
                    //case ChangeAllPathsType.ChangeMetadataHashableProperties:
                    //    FileMetadataHashableProperties previousProperties = Metadata.HashableProperties;
                    //    Metadata.HashableProperties = (FileMetadataHashableProperties)NewHashables;

                    //    if (Metadata.HashableProperties.IsFolder)
                    //    {
                    //        long previousHashablesCreationUtcTicks = previousProperties.CreationTime.ToUniversalTime().Ticks;
                    //        FilePath[] previousHashablesPaths;
                    //        if (Agent.FolderCreationTimeUtcTicksToPath.TryGetValue(previousHashablesCreationUtcTicks, out previousHashablesPaths))
                    //        {
                    //            if (previousHashablesPaths.Length == 1)
                    //            {
                    //                if (FilePathComparer.Instance.Equals(NewPath, previousHashablesPaths[0]))
                    //                {
                    //                    Agent.FolderCreationTimeUtcTicksToPath.Remove(previousHashablesCreationUtcTicks);
                    //                }
                    //            }
                    //            else
                    //            {
                    //                for (int currentDeleteIndex = 0; currentDeleteIndex < previousHashablesPaths.Length; currentDeleteIndex++)
                    //                {
                    //                    if (FilePathComparer.Instance.Equals(NewPath, previousHashablesPaths[currentDeleteIndex]))
                    //                    {
                    //                        FilePath[] pathsMinusDeleted = new FilePath[previousHashablesPaths.Length - 1];
                    //                        if (currentDeleteIndex != 0)
                    //                        {
                    //                            Array.Copy(previousHashablesPaths, 0, pathsMinusDeleted, 0, currentDeleteIndex);
                    //                        }

                    //                        if (currentDeleteIndex != previousHashablesPaths.Length - 1)
                    //                        {
                    //                            Array.Copy(previousHashablesPaths, currentDeleteIndex + 1, pathsMinusDeleted, currentDeleteIndex, pathsMinusDeleted.Length - currentDeleteIndex);
                    //                        }

                    //                        Agent.FolderCreationTimeUtcTicksToPath[previousHashablesCreationUtcTicks] = pathsMinusDeleted;
                    //                        break; // presumes only a single folder path will ever be in FolderCreationTimeUtcTicksToPath, therefore stop checking since we found a path
                    //                    }
                    //                }
                    //            }
                    //        }
                            
                    //        long newHashablesUtcTicks = Metadata.HashableProperties.CreationTime.ToUniversalTime().Ticks;
                    //        FilePath[] newHashablesPaths;
                    //        if (Agent.FolderCreationTimeUtcTicksToPath.TryGetValue(newHashablesUtcTicks, out newHashablesPaths))
                    //        {
                    //            FilePath[] appendExistingPaths = new FilePath[newHashablesPaths.Length + 1];
                    //            Array.Copy(newHashablesPaths, appendExistingPaths, newHashablesPaths.Length);
                    //            appendExistingPaths[appendExistingPaths.Length - 1] = NewPath;
                    //            Agent.FolderCreationTimeUtcTicksToPath[newHashablesUtcTicks] = appendExistingPaths;
                    //        }
                    //        else
                    //        {
                    //            Agent.FolderCreationTimeUtcTicksToPath.Add(newHashablesUtcTicks, Helpers.EnumerateSingleItem(NewPath));
                    //        }
                    //    }
                    //    break;

                    case ChangeAllPathsType.Clear:
                        Agent.AllPaths.Clear();
                        Agent.FolderCreationTimeUtcTicksToPath.Clear();
                        break;

                    case ChangeAllPathsType.IndexSet:
                        FileMetadata previousMetadata;
                        if ((Metadata != null
                                && !Metadata.HashableProperties.IsFolder)
                            || !Agent.AllPaths.TryGetValue(NewPath, out previousMetadata)
                            || (Metadata == null && !previousMetadata.HashableProperties.IsFolder))
                        {
                            previousMetadata = null;
                        }

                        Agent.AllPaths[NewPath] = Metadata;

                        if (Metadata == null
                            ? previousMetadata != null
                            : Metadata.HashableProperties.IsFolder)
                        {
                            if (previousMetadata != null)
                            {
                                long previousMetadataCreationUtcTicks = previousMetadata.HashableProperties.CreationTime.ToUniversalTime().Ticks;
                                FilePath[] previousHashablesPaths;
                                if (Agent.FolderCreationTimeUtcTicksToPath.TryGetValue(previousMetadataCreationUtcTicks, out previousHashablesPaths))
                                {
                                    if (previousHashablesPaths.Length == 1)
                                    {
                                        if (FilePathComparer.Instance.Equals(NewPath, previousHashablesPaths[0]))
                                        {
                                            Agent.FolderCreationTimeUtcTicksToPath.Remove(previousMetadataCreationUtcTicks);
                                        }
                                    }
                                    else
                                    {
                                        for (int currentDeleteIndex = 0; currentDeleteIndex < previousHashablesPaths.Length; currentDeleteIndex++)
                                        {
                                            if (FilePathComparer.Instance.Equals(NewPath, previousHashablesPaths[currentDeleteIndex]))
                                            {
                                                FilePath[] pathsMinusDeleted = new FilePath[previousHashablesPaths.Length - 1];
                                                if (currentDeleteIndex != 0)
                                                {
                                                    Array.Copy(previousHashablesPaths, 0, pathsMinusDeleted, 0, currentDeleteIndex);
                                                }

                                                if (currentDeleteIndex != previousHashablesPaths.Length - 1)
                                                {
                                                    Array.Copy(previousHashablesPaths, currentDeleteIndex + 1, pathsMinusDeleted, currentDeleteIndex, pathsMinusDeleted.Length - currentDeleteIndex);
                                                }

                                                Agent.FolderCreationTimeUtcTicksToPath[previousMetadataCreationUtcTicks] = pathsMinusDeleted;
                                                break; // presumes only a single folder path will ever be in FolderCreationTimeUtcTicksToPath, therefore stop checking since we found a path
                                            }
                                        }
                                    }
                                }
                            }

                            if (Metadata != null)
                            {
                                long createItemCreationUtcTicks = Metadata.HashableProperties.CreationTime.ToUniversalTime().Ticks;
                                FilePath[] existingAddFolderPaths;
                                if (Agent.FolderCreationTimeUtcTicksToPath.TryGetValue(createItemCreationUtcTicks, out existingAddFolderPaths))
                                {
                                    FilePath[] appendExistingPaths = new FilePath[existingAddFolderPaths.Length + 1];
                                    Array.Copy(existingAddFolderPaths, appendExistingPaths, existingAddFolderPaths.Length);
                                    appendExistingPaths[appendExistingPaths.Length - 1] = NewPath;
                                    Agent.FolderCreationTimeUtcTicksToPath[createItemCreationUtcTicks] = appendExistingPaths;
                                }
                                else
                                {
                                    Agent.FolderCreationTimeUtcTicksToPath.Add(createItemCreationUtcTicks, new[] { NewPath });
                                }
                            }
                        }
                        break;

                    case ChangeAllPathsType.Remove:
                        FileMetadata removeMetadata;
                        if (!Agent.AllPaths.TryGetValue(NewPath, out removeMetadata))
                        {
                            removeMetadata = null;
                        }

                        Agent.AllPaths.Remove(NewPath);

                        if (removeMetadata != null
                            && removeMetadata.HashableProperties.IsFolder)
                        {
                            long previousMetadataCreationUtcTicks = removeMetadata.HashableProperties.CreationTime.ToUniversalTime().Ticks;
                            FilePath[] previousHashablesPaths;
                            if (Agent.FolderCreationTimeUtcTicksToPath.TryGetValue(previousMetadataCreationUtcTicks, out previousHashablesPaths))
                            {
                                if (previousHashablesPaths.Length == 1)
                                {
                                    if (FilePathComparer.Instance.Equals(NewPath, previousHashablesPaths[0]))
                                    {
                                        Agent.FolderCreationTimeUtcTicksToPath.Remove(previousMetadataCreationUtcTicks);
                                    }
                                }
                                else
                                {
                                    for (int currentDeleteIndex = 0; currentDeleteIndex < previousHashablesPaths.Length; currentDeleteIndex++)
                                    {
                                        if (FilePathComparer.Instance.Equals(NewPath, previousHashablesPaths[currentDeleteIndex]))
                                        {
                                            FilePath[] pathsMinusDeleted = new FilePath[previousHashablesPaths.Length - 1];
                                            if (currentDeleteIndex != 0)
                                            {
                                                Array.Copy(previousHashablesPaths, 0, pathsMinusDeleted, 0, currentDeleteIndex);
                                            }

                                            if (currentDeleteIndex != previousHashablesPaths.Length - 1)
                                            {
                                                Array.Copy(previousHashablesPaths, currentDeleteIndex + 1, pathsMinusDeleted, currentDeleteIndex, pathsMinusDeleted.Length - currentDeleteIndex);
                                            }

                                            Agent.FolderCreationTimeUtcTicksToPath[previousMetadataCreationUtcTicks] = pathsMinusDeleted;
                                            break; // presumes only a single folder path will ever be in FolderCreationTimeUtcTicksToPath, therefore stop checking since we found a path
                                        }
                                    }
                                }
                            }
                        }
                        break;

                    case ChangeAllPathsType.Rename:
                        FileMetadata movedMetadata;
                        if (!Agent.AllPaths.TryGetValue(OldPath, out movedMetadata))
                        {
                            movedMetadata = null;
                        }

                        Agent.AllPaths.Rename(OldPath, NewPath);
                        
                        if (movedMetadata != null
                            && movedMetadata.HashableProperties.IsFolder)
                        {
                            long movedMetadataCreationUtcTicks = movedMetadata.HashableProperties.CreationTime.ToUniversalTime().Ticks;
                            FilePath[] movedHashablesPaths;
                            if (Agent.FolderCreationTimeUtcTicksToPath.TryGetValue(movedMetadataCreationUtcTicks, out movedHashablesPaths))
                            {
                                for (int currentDeleteIndex = 0; currentDeleteIndex < movedHashablesPaths.Length; currentDeleteIndex++)
                                {
                                    if (FilePathComparer.Instance.Equals(OldPath, movedHashablesPaths[currentDeleteIndex]))
                                    {
                                        movedHashablesPaths[currentDeleteIndex] = NewPath;
                                        break; // presumes only a single folder path will ever be in FolderCreationTimeUtcTicksToPath, therefore stop checking since we found a path
                                    }
                                    else if (currentDeleteIndex == movedHashablesPaths.Length - 1) // not found to replace, need to add
                                    {
                                        FilePath[] appendMoved = new FilePath[movedHashablesPaths.Length + 1];
                                        Array.Copy(movedHashablesPaths, appendMoved, movedHashablesPaths.Length);
                                        appendMoved[appendMoved.Length - 1] = NewPath;
                                        Agent.FolderCreationTimeUtcTicksToPath[movedMetadataCreationUtcTicks] = appendMoved;
                                    }
                                }
                            }
                            else
                            {
                                Agent.FolderCreationTimeUtcTicksToPath.Add(movedMetadataCreationUtcTicks, new[] { NewPath });
                            }
                        }
                        break;
                }
            }
        }
        private sealed class ChangeAllPathsAdd : ChangeAllPathsBase
        {
            public ChangeAllPathsAdd(MonitorAgent Agent, FilePath NewPath, FileMetadata Metadata)
                : base(ChangeAllPathsType.Add, Agent, NewPath: NewPath, Metadata: Metadata) { }
        }
        private sealed class ChangeAllPathsRemove : ChangeAllPathsBase
        {
            public ChangeAllPathsRemove(MonitorAgent Agent, FilePath NewPath)
                : base(ChangeAllPathsType.Remove, Agent, NewPath: NewPath) { }
        }
        private sealed class ChangeAllPathsRename : ChangeAllPathsBase
        {
            public ChangeAllPathsRename(MonitorAgent Agent, FilePath OldPath, FilePath NewPath)
                : base(ChangeAllPathsType.Rename, Agent, OldPath: OldPath, NewPath: NewPath) { }
        }
        private sealed class ChangeAllPathsClear : ChangeAllPathsBase
        {
            public ChangeAllPathsClear(MonitorAgent Agent)
                : base(ChangeAllPathsType.Clear, Agent) { }
        }
        private sealed class ChangeAllPathsIndexSet : ChangeAllPathsBase
        {
            public ChangeAllPathsIndexSet(MonitorAgent Agent, FilePath NewPath, FileMetadata Metadata)
                : base(ChangeAllPathsType.IndexSet, Agent, NewPath: NewPath, Metadata: Metadata) { }
        }

        #endregion

        // Storage of changes queued to process (QueuedChanges used as the locker for both and keyed by file path, QueuedChangesByMetadata keyed by the hashable metadata properties)
        private Dictionary<FilePath, FileChange> QueuedChanges = new Dictionary<FilePath, FileChange>(FilePathComparer.Instance);
        private Dictionary<FilePath, FilePath> OldToNewPathRenames = new Dictionary<FilePath, FilePath>(FilePathComparer.Instance);
        private static readonly FileMetadataHashableComparer QueuedChangesMetadataComparer = new FileMetadataHashableComparer();// Comparer has improved hashing by using only the fastest changing bits
        private Dictionary<FileMetadataHashableProperties, FileChange> QueuedChangesByMetadata = new Dictionary<FileMetadataHashableProperties, FileChange>(QueuedChangesMetadataComparer);// Use custom comparer for improved hashing

        private readonly bool DependencyDebugging;

        // Queue of file monitor events that occur while initial index is processing
        private Queue<ChangesQueueHolder> ChangesQueueForInitialIndexing = new Queue<ChangesQueueHolder>();
        // Storage class for required parameters to the CheckMetadataAgainstFile method
        private sealed class ChangesQueueHolder
        {
            public string newPath { get; set; }
            public string oldPath { get; set; }
            public WatcherChangeTypes changeType { get; set; }
            public bool folderOnly { get; set; }
        }

        private class DisposeCheckingHolder
        {
            public object Value
            {
                get
                {
                    return _value;
                }
            }
            private object _value = null;

            public bool IsDisposed
            {
                get
                {
                    if (GetDisposed == null)
                    {
                        return GetDisposedParameterless();
                    }
                    else
                    {
                        return GetDisposed(_value);
                    }
                }
            }
            private Func<bool> GetDisposedParameterless = null;
            private Func<object, bool> GetDisposed = null;

            public DisposeCheckingHolder(Func<bool> checkDisposed, object toCheck)
            {
                if (checkDisposed == null)
                {
                    throw new NullReferenceException("checkDisposed cannot be null");
                }
                GetDisposedParameterless = checkDisposed;
                _value = toCheck;
            }

            public DisposeCheckingHolder(Func<object, bool> checkDisposed, object toCheck)
            {
                if (checkDisposed == null)
                {
                    throw new NullReferenceException("checkDisposed cannot be null");
                }
                GetDisposed = checkDisposed;
                _value = toCheck;
            }
        }

        private LinkedList<FileChange> ProcessingChanges = new LinkedList<FileChange>();
        private const int MaxProcessingChangesBeforeTrigger = 499;
        // Field to store timer for queue processing,
        // initialized on construction
        private ProcessingQueuesTimer QueuesTimer;

        // Stores FileChanges that come off ProcessFileChange so they can be batched for merge
        private readonly Queue<FileChange> NeedsMergeToSql = new Queue<FileChange>();
        private bool MergingToSql = false;

        /// <summary>
        /// Stores whether initial indexing has yet to complete,
        /// lock on InitialIndexLocker
        /// </summary>
        private bool IsInitialIndex = true;
        private readonly CLSyncbox _syncbox;
        private readonly long MaxUploadFileSize;
        #endregion

        #region memory debug
        private readonly bool debugMemory;

        private void initiazeMemoryDebug()
        {
            if (debugMemory)
            {
                memoryDebugger.Initialize();
            }
        }

        public sealed class memoryDebugger
        {
            public static void Initialize()
            {
                lock (initializeLocker)
                {
                    if (_instance == null)
                    {
                        _instance = new memoryDebugger();
                    }
                }
            }
            private static readonly object initializeLocker = new object();

            public static memoryDebugger Instance
            {
                get
                {
                    if (_instance == null)
                    {
                        throw new NullReferenceException("Instance not available until after Initialize is called");
                    }
                    return _instance;
                }
            }
            private static memoryDebugger _instance = null;

            private memoryDebugger() { }

            public string serializeMemory()
            {
                lock (watcherEntries)
                {
                    if (memorySerializer == null)
                    {
                        memorySerializer = new System.Xml.Serialization.XmlSerializer(typeof(FileMonitorMemory));
                    }

                    using (MemoryStream serializeStream = new MemoryStream())
                    {
                        System.Xml.XmlWriterSettings memoryWriterSettings = new System.Xml.XmlWriterSettings()
                        {
                            Encoding = Encoding.UTF8,
                            Indent = true
                        };

                        using (TextWriter memoryWriter = new StreamWriter(serializeStream))
                        {
                            using (System.Xml.XmlWriter memoryXmlWriter = System.Xml.XmlWriter.Create(memoryWriter, memoryWriterSettings))
                            {
                                memorySerializer.Serialize(memoryXmlWriter,
                                    new FileMonitorMemory()
                                    {
                                        Copyright = new Copyright()
                                        {
                                            FileName = "InMemoryOnly",
                                            Creator = "DavidBruck"
                                        },
                                        Entries = watcherEntries.ToArray()
                                    });
                            }
                        }

                        return Encoding.UTF8.GetString(serializeStream.ToArray());
                    }
                }
            }
            private System.Xml.Serialization.XmlSerializer memorySerializer = null;

            public void wipeMemory()
            {
                lock (watcherEntries)
                {
                    watcherEntries.Clear();
                }
            }

            private readonly List<Entry> watcherEntries = new List<Entry>();

            public void WatcherChanged(string oldPath, string newPath, WatcherChangeTypes watcherTypes, bool folderOnly)
            {
                WatcherChangeType[] toAddTypes = new WatcherChangeType[Helpers.NumberOfSetBits((int)watcherTypes)];

                int toAddTypesIdx = 0;
                if ((watcherTypes & WatcherChangeTypes.Created) == WatcherChangeTypes.Created)
                {
                    toAddTypes[toAddTypesIdx++] = new WatcherChangeCreated();
                }
                if ((watcherTypes & WatcherChangeTypes.Deleted) == WatcherChangeTypes.Deleted)
                {
                    toAddTypes[toAddTypesIdx++] = new WatcherChangeDeleted();
                }
                if ((watcherTypes & WatcherChangeTypes.Changed) == WatcherChangeTypes.Changed)
                {
                    toAddTypes[toAddTypesIdx++] = new WatcherChangeChanged();
                }
                if ((watcherTypes & WatcherChangeTypes.Renamed) == WatcherChangeTypes.Renamed)
                {
                    toAddTypes[toAddTypesIdx++] = new WatcherChangeRenamed();
                }

                WatcherChangedEntry toAdd = new WatcherChangedEntry()
                {
                    FolderOnly = folderOnly,
                    NewPath = newPath,
                    OldPath = oldPath,
                    Types = toAddTypes
                };

                lock (watcherEntries)
                {
                    watcherEntries.Add(toAdd);
                }
            }

            public void AddCheckMetadata(CheckMetadataEntry toAdd)
            {
                lock (watcherEntries)
                {
                    watcherEntries.Add(toAdd);
                }
            }
        }
        #endregion

        /// <summary>
        /// Create and initialize the MonitorAgent with the root folder to be monitored (Cloud Directory),
        /// requires running Start() method to begin monitoring and then, when available, load
        /// the initial index list to begin processing via BeginProcessing(initialList)
        /// </summary>
        /// <param name="syncbox">Syncbox to monitor</param>
        /// <param name="indexer">Created and initialized but not started SQLIndexer</param>
        /// <param name="httpRestClient">Client for Http REST communication</param>
        /// <param name="StatusUpdated">Callback to fire upon update of the running status</param>
        /// <param name="StatusUpdatedUserState">Userstate to pass to the statusUpdated callback</param>
        /// <param name="newAgent">(output) the return MonitorAgent created by this method</param>
        /// <param name="syncEngine">(output) the return SyncEngine which is also created with the combination of the input SQLIndexer indexer and the output MonitorAgent</param>
        /// <param name="debugMemory">Whether memory of the FileMonitor will be debugged</param>
        /// <param name="onQueueingCallback">(optional) action to be executed every time a FileChange would be queued for processing</param>
        /// <param name="logProcessing">(optional) if set, logs FileChange objects when their processing callback fires</param>
        /// <returns>Returns any error that occurred, or null.</returns>
        public static CLError CreateNewAndInitialize(CLSyncbox syncbox,
            IndexingAgent indexer,
            CLHttpRest httpRestClient,
            bool DependencyDebugging,
            System.Threading.WaitCallback StatusUpdated,
            object StatusUpdatedUserState,
            out MonitorAgent newAgent,
            out SyncEngine syncEngine,
            bool debugMemory,
            Action<MonitorAgent, FileChange> onQueueingCallback = null,
            bool logProcessing = false,
            long MaxUploadFileSize = FileConstants.MaxUploadFileSize)
        {

            try
            {
                newAgent = new MonitorAgent(indexer, syncbox, debugMemory, DependencyDebugging, MaxUploadFileSize);
            }
            catch (Exception ex)
            {
                newAgent = Helpers.DefaultForType<MonitorAgent>();
                syncEngine = Helpers.DefaultForType<SyncEngine>();
                return ex;
            }

            try
            {
                // Create sync engine
                CLError createSyncEngineError = SyncEngine.CreateAndInitialize(
                    newAgent._syncData,
                    syncbox,
                    httpRestClient,
                    out syncEngine,
                    DependencyDebugging,
                    StatusUpdated,
                    StatusUpdatedUserState);
                if (createSyncEngineError != null)
                {
                    return createSyncEngineError;
                }
            }
            catch (Exception ex)
            {
                syncEngine = Helpers.DefaultForType<SyncEngine>();
                return ex;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(syncbox.CopiedSettings.SyncRoot))
                {
                    throw new ArgumentException("Folder path cannot be null or empty");
                }

                DirectoryInfo folderInfo = new DirectoryInfo(syncbox.CopiedSettings.SyncRoot);
                if (!folderInfo.Exists)
                {
                    throw new Exception("Folder not found at provided folder path");
                }

                // Initialize current, in-memory index
                CLError allPathsError = FilePathDictionary<FileMetadata>.CreateAndInitialize(folderInfo,
                    out newAgent.AllPaths,
                    newAgent.MetadataPath_RecursiveDelete,
                    newAgent.MetadataPath_RecursiveRename);
                if (allPathsError != null)
                {
                    return allPathsError;
                }

                // Initialize folder paths
                newAgent.CurrentFolderPath = newAgent.InitialFolderPath = syncbox.CopiedSettings.SyncRoot;

                // assign local fields with optional initialization parameters
                newAgent.OnQueueing = onQueueingCallback;
                newAgent.SyncRun = syncEngine.Run;
                newAgent.GetUploadDownloadTransfersInProgress = syncEngine.GetUploadDownloadTransfersInProgress;
                newAgent.LogProcessingFileChanges = logProcessing;

                // assign timer object that is used for processing the FileChange queues in batches
                CLError queueTimerError = ProcessingQueuesTimer.CreateAndInitializeProcessingQueuesTimer(state =>
                    {
                        object[] castState = state as object[];
                        bool parametersMatched = false;

                        if (castState.Length == 2)
                        {
                            Action<bool> ProcessQueuesAfterTimer = castState[0] as Action<bool>;
                            LinkedList<FileChange> ProcessingChanges = castState[1] as LinkedList<FileChange>;

                            if (ProcessQueuesAfterTimer != null
                                && ProcessingChanges != null)
                            {
                                parametersMatched = true;

                                ProcessQueuesAfterTimer(ProcessingChanges.Count == 0);
                            }
                        }

                        if (!parametersMatched)
                        {
                            throw new InvalidOperationException("Parameters not matched");
                        }
                    },
                    1000, // Collect items in queue for 1 second before batch processing
                    out newAgent.QueuesTimer,
                    new object[] { (Action<bool>)newAgent.ProcessQueuesAfterTimer, newAgent.ProcessingChanges });
                if (queueTimerError != null)
                {
                    return queueTimerError;
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        private MonitorAgent(IndexingAgent Indexer, CLSyncbox syncbox, bool debugMemory, bool DependencyDebugging, long MaxUploadFileSize) 
        {
            // check input parameters

            if (syncbox == null)
            {
                throw new NullReferenceException("syncbox cannot be null");
            }

            // Initialize Cloud trace in case it is not already initialized.
            CLTrace.Initialize(syncbox.CopiedSettings.TraceLocation, "Cloud", "log", syncbox.CopiedSettings.TraceLevel, syncbox.CopiedSettings.LogErrors);
            CLTrace.Instance.writeToLog(9, "MonitorAgent: CreateNewAndInitialize: Entry");

            if (Indexer == null)
            {
                throw new NullReferenceException("Indexer cannot be null");
            }
            this.MaxUploadFileSize = MaxUploadFileSize;
            this._syncbox = syncbox;
            this.Indexer = Indexer;
            this._syncData = new SyncData(this, Indexer);
            this.debugMemory = debugMemory;
            this.DependencyDebugging = DependencyDebugging;
            this.initiazeMemoryDebug();
        }
        // Standard IDisposable implementation based on MSDN System.IDisposable
        ~MonitorAgent()
        {
            this.Dispose(false);
        }

        #region public methods
        /// <summary>
        /// A push notification has been received.
        /// Starts the queue timer to start sync processing,
        /// if it is not already started for other events
        /// </summary>
        public void PushNotification(JsonContracts.NotificationResponse notification)
        {
            lock (QueuesTimer.TimerRunningLocker)
            {
                QueuesTimer.StartTimerIfNotRunning();
            }
        }

        /// <summary>
        /// Applies a Sync From FileChange to the local file system i.e. a folder creation would cause the local FileSystem to create a folder locally;
        /// changes in-memory index first to prevent firing Sync To events
        /// </summary>
        /// <param name="toApply">FileChange to apply to the local file system</param>
        /// <returns>Returns any error occurred applying the FileChange, if any</returns>
        internal CLError ApplySyncFromFileChange(FileChange toApply)
        {
            return ApplySyncFromFileChange<object>(toApply, null, null, null, null);
        }

        /// <summary>
        /// Applies a Sync From FileChange to the local file system i.e. a folder creation would cause the local FileSystem to create a folder locally;
        /// changes in-memory index first to prevent firing Sync To events
        /// </summary>
        /// <param name="toApply">FileChange to apply to the local file system</param>
        /// <returns>Returns any error occurred applying the FileChange, if any</returns>
        internal CLError ApplySyncFromFileChange<T>(FileChange toApply, Action<T> onAllPathsLock, Action<T> onBeforeAllPathsUnlock, T userState, object lockerInsideAllPaths)
        {
            try
            {
                if (toApply.Direction == SyncDirection.To)
                {
                    throw new ArgumentException("Cannot apply a Sync To FileChange locally");
                }
                if (toApply.Metadata.HashableProperties.IsFolder
                    && toApply.Type == FileChangeType.Modified)
                {
                    throw new ArgumentException("Cannot apply a modification to a folder");
                }
                if (!toApply.Metadata.HashableProperties.IsFolder
                    && (toApply.Type == FileChangeType.Created
                        || toApply.Type == FileChangeType.Modified))
                {
                    throw new ArgumentException("Cannot download a file in MonitorAgent, it needs to be downloaded through Sync");
                }

                string rootPathString;
                FilePath rootPath = rootPathString = CurrentFolderPath;

                var fillAndReturnUpDowns = DelegateAndDataHolder.Create(
                    new
                    {
                        upDownsHolder = new GenericHolder<FilePathDictionary<List<FileChange>>>(null),
                        thisAgent = this,
                        innerRootPath = new GenericHolder<string>(null)
                    },
                    (Data, errorToAccumulate) =>
                    {
                        if (Data.upDownsHolder.Value == null)
                        {
                            KeyValuePair<FilePathDictionary<List<FileChange>>, CLError> upDownsPair = Data.thisAgent.GetUploadDownloadTransfersInProgress(Data.innerRootPath.Value);
                            if (upDownsPair.Value != null)
                            {
                                throw new AggregateException("Error in GetUploadDownloadTransfersInProgress", upDownsPair.Value.GrabExceptions());
                            }
                            if (upDownsPair.Key == null)
                            {
                                throw new NullReferenceException("GetUploadDownloadTransfersInProgress return cannot have a null Key");
                            }
                            Data.upDownsHolder.Value = upDownsPair.Key;
                        }
                        return Data.upDownsHolder.Value;
                    },
                    null);

                var recurseFolderCreationToRoot = DelegateAndDataHolder.Create(
                    new
                    {
                        thisAgent = this,
                        thisDelegate = new GenericHolder<DelegateAndDataHolder>(null),
                        toCreate = new GenericHolder<FilePath>(null),
                        root = rootPath,
                        creationTime = new GenericHolder<Nullable<DateTime>>(null),
                        lastTime = new GenericHolder<Nullable<DateTime>>(null),
                        serverUid = new GenericHolder<string>(null)
                    },
                    (Data, errorToAccumulate) =>
                    {
                        FilePath storeToCreate = Data.toCreate.Value;
                        Nullable<DateTime> storeCreationTime = Data.creationTime.Value;
                        Nullable<DateTime> storeLastTime = Data.lastTime.Value;
                        string storeServerUid = Data.serverUid.Value;

                        if (!FilePathComparer.Instance.Equals(storeToCreate, Data.root))
                        {
                            Data.toCreate.Value = storeToCreate.Parent;
                            Data.creationTime.Value = null;
                            Data.lastTime.Value = null;
                            Data.serverUid.Value = null;

                            Data.thisDelegate.Value.Process();

                            FileMetadata existingPath;
                            Nullable<DateTime> createdLastWriteUtc;
                            Nullable<DateTime> createdCreationUtc;

                            if (Data.thisAgent.AllPaths.TryGetValue(storeToCreate, out existingPath))
                            {
                                if (!Directory.Exists(storeToCreate.ToString()))
                                {
                                    CreateDirectoryWithAttributes(storeToCreate, existingPath.HashableProperties.CreationTime, existingPath.HashableProperties.LastTime, out createdLastWriteUtc, out createdCreationUtc);
                                }
                            }
                            else
                            {
                                CreateDirectoryWithAttributes(storeToCreate, storeCreationTime, storeLastTime, out createdLastWriteUtc, out createdCreationUtc);

                                new ChangeAllPathsAdd(Data.thisAgent, storeToCreate,
                                    new FileMetadata()
                                    {
                                        HashableProperties = new FileMetadataHashableProperties(true,
                                            createdLastWriteUtc,
                                            createdCreationUtc,
                                            null),
                                        ServerUid = storeServerUid
                                    });
                            }
                        }
                    },
                    null);
                recurseFolderCreationToRoot.TypedData.thisDelegate.Value = recurseFolderCreationToRoot;

                var recurseHierarchyAndAddSyncFromsToHashSet = DelegateAndDataHolder.Create(
                    new
                    {
                        thisDelegate = new GenericHolder<DelegateAndDataHolder>(null),
                        matchedDowns = new HashSet<FileChange>(),
                        innerHierarchy = new GenericHolder<FilePathHierarchicalNode<List<FileChange>>>(null)
                    },
                    (Data, errorToAccumulate) =>
                    {
                        FilePathHierarchicalNodeWithValue<List<FileChange>> castHierarchy = Data.innerHierarchy.Value as FilePathHierarchicalNodeWithValue<List<FileChange>>;
                        if (castHierarchy != null)
                        {
                            foreach (FileChange innerUpDown in (castHierarchy.Value.Value ?? Enumerable.Empty<FileChange>()))
                            {
                                if (innerUpDown.Direction == SyncDirection.From)
                                {
                                    Data.matchedDowns.Add(innerUpDown);
                                }
                            }
                        }
                        if (Data.innerHierarchy.Value != null)
                        {
                            foreach (FilePathHierarchicalNode<List<FileChange>> recurseHierarchicalNode in (Data.innerHierarchy.Value.Children ?? Enumerable.Empty<FilePathHierarchicalNode<List<FileChange>>>()))
                            {
                                Data.innerHierarchy.Value = recurseHierarchicalNode;
                                Data.thisDelegate.Value.Process();
                            }
                        }
                    },
                    null);
                recurseHierarchyAndAddSyncFromsToHashSet.TypedData.thisDelegate.Value = recurseHierarchyAndAddSyncFromsToHashSet;

                lock (AllPaths)
                {
                    lock (lockerInsideAllPaths ?? new object())
                    {
                        if (onAllPathsLock != null)
                        {
                            onAllPathsLock(userState);
                        }

                        Exception exOnMainSwitch = null;
                        try
                        {
                            if (toApply.NewPath == null)
                            {
                                return null;
                            }

                            fillAndReturnUpDowns.TypedData.innerRootPath.Value = rootPathString;
                            if (!toApply.NewPath.Contains(rootPath))
                            {
                                throw new ArgumentException("FileChange's NewPath does not fall within the root directory");
                            }

                            switch (toApply.Type)
                            {
                                case FileChangeType.Created:
                                    recurseFolderCreationToRoot.TypedData.toCreate.Value = toApply.NewPath;
                                    recurseFolderCreationToRoot.TypedData.creationTime.Value = toApply.Metadata.HashableProperties.CreationTime;
                                    recurseFolderCreationToRoot.TypedData.lastTime.Value = toApply.Metadata.HashableProperties.LastTime;
                                    recurseFolderCreationToRoot.TypedData.serverUid.Value = toApply.Metadata.ServerUid;
                                    recurseFolderCreationToRoot.Process();

                                    Exception creationToRethrow = null;
                                    try
                                    {
                                        string creationPathString = toApply.NewPath.ToString();
                                        DateTime actualCreationTime = Directory.GetCreationTimeUtc(creationPathString);
                                        if (actualCreationTime.Ticks != FileConstants.InvalidUtcTimeTicks
                                            && (actualCreationTime.ToUniversalTime()).Ticks != FileConstants.InvalidUtcTimeTicks
                                            && DateTime.Compare(actualCreationTime, toApply.Metadata.HashableProperties.CreationTime) != 0)
                                        {
                                            toApply.Metadata.HashableProperties = new FileMetadataHashableProperties(
                                                /* isFolder */ true,
                                                toApply.Metadata.HashableProperties.LastTime,
                                                actualCreationTime,
                                                /* size */ null);

                                            CLError updateCreationTimeError = SyncData.mergeToSql(Helpers.EnumerateSingleItem(new FileChangeMerge(toApply)));
                                            if (updateCreationTimeError != null)
                                            {
                                                try
                                                {
                                                    Directory.Delete(creationPathString);
                                                    try
                                                    {
                                                        throw new AggregateException("Error updating creation time for folder to event in database", updateCreationTimeError.GrabExceptions());
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        creationToRethrow = ex;
                                                        throw ex;
                                                    }
                                                }
                                                catch
                                                {
                                                    if (creationToRethrow != null)
                                                    {
                                                        throw creationToRethrow;
                                                    }
                                                }
                                            }

                                            new ChangeAllPathsIndexSet(this, toApply.NewPath,
                                                new FileMetadata()
                                                {
                                                    HashableProperties = toApply.Metadata.HashableProperties,
                                                    ServerUid = toApply.Metadata.ServerUid,
                                                    Revision = toApply.Metadata.Revision
                                                });
                                        }
                                    }
                                    catch
                                    {
                                        if (creationToRethrow != null)
                                        {
                                            throw creationToRethrow;
                                        }
                                    }
                                    break;
                                case FileChangeType.Deleted:
                                    FilePathDictionary<List<FileChange>> upDownsForDeleted = fillAndReturnUpDowns.TypedProcess();

                                    FilePathHierarchicalNode<List<FileChange>> deletedHierarchy;
                                    CLError deletedHierarchyError = upDownsForDeleted.GrabHierarchyForPath(toApply.NewPath, out deletedHierarchy, suppressException: true);
                                    if (deletedHierarchyError != null)
                                    {
                                        throw new AggregateException("Error grabbing hierarchy from upDownsForDeleted", deletedHierarchyError.GrabExceptions());
                                    }

                                    recurseHierarchyAndAddSyncFromsToHashSet.TypedData.innerHierarchy.Value = deletedHierarchy;
                                    recurseHierarchyAndAddSyncFromsToHashSet.Process();

                                    foreach (FileChange matchedDown in recurseHierarchyAndAddSyncFromsToHashSet.TypedData.matchedDowns)
                                    {
                                        if (matchedDown.fileDownloadMoveLocker != null)
                                        {
                                            Monitor.Enter(matchedDown.fileDownloadMoveLocker);
                                        }
                                    }

                                    try
                                    {
                                        bool deleteHappened;
                                        if (toApply.Metadata.HashableProperties.IsFolder)
                                        {
                                            try
                                            {
                                                Directory.Delete(toApply.NewPath.ToString(), true);
                                                deleteHappened = true;
                                            }
                                            catch (DirectoryNotFoundException)
                                            {
                                                deleteHappened = false;
                                            }
                                        }
                                        else
                                        {
                                            try
                                            {
                                                File.Delete(toApply.NewPath.ToString());
                                                deleteHappened = true;
                                            }
                                            catch (FileNotFoundException)
                                            {
                                                deleteHappened = false;
                                            }
                                        }

                                        if (deleteHappened)
                                        {
                                            foreach (FileChange matchedDown in recurseHierarchyAndAddSyncFromsToHashSet.TypedData.matchedDowns)
                                            {
                                                matchedDown.NewPath = null; // set volatile path on this alternate thread which is checked as a way to cancel a download
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        recurseHierarchyAndAddSyncFromsToHashSet.TypedData.matchedDowns.Reverse(); // not sure if reversal is necessary, but other types of locks should be exited in reverse order
                                        foreach (FileChange matchedDown in recurseHierarchyAndAddSyncFromsToHashSet.TypedData.matchedDowns)
                                        {
                                            if (matchedDown.fileDownloadMoveLocker != null)
                                            {
                                                Monitor.Exit(matchedDown.fileDownloadMoveLocker);
                                            }
                                        }
                                    }

                                    new ChangeAllPathsRemove(this, toApply.NewPath);
                                    break;
                                case FileChangeType.Renamed:
                                    recurseFolderCreationToRoot.TypedData.toCreate.Value = toApply.NewPath.Parent;
                                    recurseFolderCreationToRoot.Process();

                                    // check if move old path is outside of the cloud directory (temp download directory) in order to bypass updown checking and locking
                                    if (toApply.Metadata.HashableProperties.IsFolder
                                        || toApply.OldPath.Contains(rootPath, insensitiveNameSearch: true))
                                    {
                                        FilePathDictionary<List<FileChange>> upDownsForRenamed = fillAndReturnUpDowns.TypedProcess();

                                        FilePathHierarchicalNode<List<FileChange>> renamedHierarchy;
                                        CLError renamedHierarchyError = upDownsForRenamed.GrabHierarchyForPath(toApply.OldPath, out renamedHierarchy, suppressException: true);
                                        if (renamedHierarchyError != null)
                                        {
                                            throw new AggregateException("Error grabbing hierarchy from upDownsForRenamed", renamedHierarchyError.GrabExceptions());
                                        }

                                        recurseHierarchyAndAddSyncFromsToHashSet.TypedData.innerHierarchy.Value = renamedHierarchy;
                                        recurseHierarchyAndAddSyncFromsToHashSet.Process();
                                    }

                                    foreach (FileChange matchedDown in recurseHierarchyAndAddSyncFromsToHashSet.TypedData.matchedDowns)
                                    {
                                        if (matchedDown.fileDownloadMoveLocker != null)
                                        {
                                            Monitor.Enter(matchedDown.fileDownloadMoveLocker);
                                        }
                                    }

                                    try
                                    {
                                        if (toApply.Metadata.HashableProperties.IsFolder)
                                        {
                                            string oldPathString = toApply.OldPath.ToString();
                                            string newPathString = toApply.NewPath.ToString();
                                            DateTime oldPathCreationTime = Directory.GetCreationTimeUtc(oldPathString);

                                            try
                                            {
                                                Directory.Move(oldPathString, newPathString);
                                            }
                                            catch (DirectoryNotFoundException ex)
                                            {
                                                try
                                                {
                                                    if (Directory.Exists(toApply.NewPath.ToString()))
                                                    {
                                                        toApply.NotFoundForStreamCounter++;
                                                    }
                                                }
                                                catch (Exception innerEx)
                                                {
                                                    throw new AggregateException("Error on applying sync from directory move and an error checking if directory exists at new path",
                                                        ex,
                                                        innerEx);
                                                }
                                                throw ex;
                                            }
                                            catch (Exception ex)
                                            {
                                                if (oldPathCreationTime.Ticks != FileConstants.InvalidUtcTimeTicks
                                                    && oldPathCreationTime.ToUniversalTime().Ticks != FileConstants.InvalidUtcTimeTicks
                                                    && Directory.Exists(newPathString)
                                                    && (newPathString == oldPathString || !Directory.Exists(oldPathString))
                                                    && DateTime.Compare(Directory.GetCreationTimeUtc(newPathString), oldPathCreationTime) == 0)
                                                {
                                                    // folder move actually worked even though it threw an exception
                                                    // silence exception
                                                }
                                                else
                                                {
                                                    throw ex;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // File rename.
                                            string newPathString = toApply.NewPath.ToString();
                                            string oldPathString = toApply.OldPath.ToString();
                                            string backupLocation = Helpers.GetTempFileDownloadPath(_syncbox.CopiedSettings, _syncbox.SyncboxId) + "\\" + Guid.NewGuid().ToString();

                                            Helpers.RunActionWithRetries(
                                                fileMoveState =>
                                                {

                                                    // To preserve the DACL in the target file (newPathString) from being overwritten with the DACL of the temp file (oldPathString):
                                                    //      collect the original target file external ACEs;
                                                    //      collect the temporary file external ACEs;
                                                    //      after the file has been moved, reset the target file DACL by adding the original external ACEs and removing the temoporary file external ACEs;
                                                    //  Note that reseting the DACL will merge the correct inherited ACEs into the target file DACL;

                                                    // NOTE: NULL DACL (grants full access to everyone) in C# is represented as AuthorizationRuleCollection([Allow, Everyone (S-1-1-0)]);
                                                    //       Empty DACL (grants no access to anyone) in C# is represented as empty AuthorizationRuleCollection();

                                                    AuthorizationRuleCollection targetExplicitRules = null;
                                                    try
                                                    {
                                                        if (File.Exists(fileMoveState.newPathString))
                                                        {
                                                            targetExplicitRules = File.GetAccessControl(fileMoveState.newPathString)
                                                                                        .GetAccessRules(true, false, typeof(System.Security.Principal.SecurityIdentifier));
                                                        }
                                                    }
                                                    catch
                                                    {
                                                        //noop;
                                                    }

                                                    AuthorizationRuleCollection tempExplicitRules = null;
                                                    try
                                                    {
                                                        if (File.Exists(fileMoveState.oldPathString))
                                                        {
                                                            tempExplicitRules = File.GetAccessControl(fileMoveState.oldPathString)
                                                                                        .GetAccessRules(true, false, typeof(System.Security.Principal.SecurityIdentifier));
                                                        }
                                                    }
                                                    catch
                                                    {
                                                        //noop;
                                                    }

                                                    FileInfo oldPathInfo = new FileInfo(fileMoveState.oldPathString);
                                                    FileInfo newPathInfo = new FileInfo(fileMoveState.newPathString);
                                                    DateTime oldPathCreation = oldPathInfo.CreationTimeUtc;
                                                    DateTime oldPathWrite = oldPathInfo.LastWriteTimeUtc;
                                                    long oldPathSize = oldPathInfo.Length;
                                                    
                                                    try
                                                    {
                                                        if (newPathInfo.Exists)
                                                        {
                                                            try
                                                            {
                                                                oldPathInfo.Replace(
                                                                    fileMoveState.newPathString,
                                                                    fileMoveState.backupLocation,
                                                                    ignoreMetadataErrors: true);
                                                                try
                                                                {
                                                                    if (File.Exists(fileMoveState.backupLocation))
                                                                    {
                                                                        File.Delete(fileMoveState.backupLocation);
                                                                    }
                                                                }
                                                                catch
                                                                {
                                                                }
                                                            }
                                                            // File.Replace not supported on non-NTFS drives, must use traditional move
                                                            catch (PlatformNotSupportedException)
                                                            {
                                                                if (newPathInfo.Exists)
                                                                {
                                                                    newPathInfo.Delete();
                                                                }
                                                                oldPathInfo.MoveTo(fileMoveState.newPathString);
                                                            }
                                                            // Some strange condition on specific files which does not make sense can throw an error on replace but may still succeed on move
                                                            catch (IOException)
                                                            {
                                                                if (newPathInfo.Exists)
                                                                {
                                                                    newPathInfo.Delete();
                                                                }
                                                                oldPathInfo.MoveTo(fileMoveState.newPathString);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            oldPathInfo.MoveTo(fileMoveState.newPathString);
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        if (oldPathCreation.Ticks != FileConstants.InvalidUtcTimeTicks
                                                            && oldPathCreation.ToUniversalTime().Ticks != FileConstants.InvalidUtcTimeTicks

                                                            && oldPathWrite.Ticks != FileConstants.InvalidUtcTimeTicks
                                                            && oldPathWrite.ToUniversalTime().Ticks != FileConstants.InvalidUtcTimeTicks

                                                            && newPathInfo.Exists

                                                            && DateTime.Compare(newPathInfo.CreationTimeUtc, oldPathCreation) == 0

                                                            && DateTime.Compare(newPathInfo.LastWriteTimeUtc, oldPathWrite) == 0

                                                            && oldPathSize == newPathInfo.Length)
                                                        {
                                                            // file move (or replace) actually worked even though it threw an exception
                                                            // silence exception
                                                        }
                                                        else
                                                        {
                                                            throw ex;
                                                        }
                                                    }


                                                    FileSecurity fileSecurity = new FileSecurity(fileMoveState.newPathString, AccessControlSections.Access);
                                                    if (tempExplicitRules != null && tempExplicitRules.Count > 0)
                                                    {
                                                        foreach (FileSystemAccessRule rule in tempExplicitRules)
                                                        {
                                                            fileSecurity.RemoveAccessRule(rule);
                                                        }
                                                    }
                                                    if (targetExplicitRules != null && targetExplicitRules.Count > 0)
                                                    {
                                                        foreach (FileSystemAccessRule rule in targetExplicitRules)
                                                        {
                                                            fileSecurity.ResetAccessRule(rule);
                                                        }
                                                    }
                                                    if ((tempExplicitRules == null || tempExplicitRules.Count == 0) &&
                                                        (targetExplicitRules == null || targetExplicitRules.Count == 0))
                                                    {
                                                        // Note: File.SetAccessControl() won't change the target file DACL if fileSecurity has not been modified;
                                                        //          the following line formally "modifies" fileSecurity
                                                        fileSecurity.SetSecurityDescriptorBinaryForm(fileSecurity.GetSecurityDescriptorBinaryForm(), AccessControlSections.Access);
                                                    }

                                                    try
                                                    {
                                                        File.SetAccessControl(fileMoveState.newPathString, fileSecurity);
                                                    }
                                                    catch
                                                    {
                                                        //noop; 
                                                    }

                                                },
                                                new
                                                    {
                                                        oldPathString = oldPathString,
                                                        newPathString = newPathString,
                                                        backupLocation = backupLocation
                                                    },
                                                throwExceptionOnFailure: true);  // end RunActionWithRetries
                                        }  // end else file rename

                                        foreach (FileChange matchedDown in recurseHierarchyAndAddSyncFromsToHashSet.TypedData.matchedDowns)
                                        {
                                            FilePath previousNewPath = matchedDown.NewPath;
                                            if (previousNewPath != null)
                                            {
                                                FilePath rebuiltNewPath = previousNewPath.Copy();
                                                FilePath.ApplyRename(rebuiltNewPath, toApply.OldPath, toApply.NewPath);
                                                matchedDown.NewPath = rebuiltNewPath;
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        recurseHierarchyAndAddSyncFromsToHashSet.TypedData.matchedDowns.Reverse(); // not sure if reversal is necessary, but other types of locks should be exited in reverse order
                                        foreach (FileChange matchedDown in recurseHierarchyAndAddSyncFromsToHashSet.TypedData.matchedDowns)
                                        {
                                            if (matchedDown.fileDownloadMoveLocker != null)
                                            {
                                                Monitor.Exit(matchedDown.fileDownloadMoveLocker);
                                            }
                                        }
                                    }

                                    new ChangeAllPathsRemove(this, toApply.OldPath);
                                    new ChangeAllPathsIndexSet(this, toApply.NewPath,
                                        new FileMetadata(toApply.Metadata.RevisionChanger)
                                        {
                                            ServerUid = toApply.Metadata.ServerUid,
                                            HashableProperties = toApply.Metadata.HashableProperties,
                                            Revision = toApply.Metadata.Revision
                                        });
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            exOnMainSwitch = ex;
                            throw ex;
                        }
                        finally
                        {
                            try
                            {
                                if (onBeforeAllPathsUnlock != null)
                                {
                                    onBeforeAllPathsUnlock(userState);
                                }
                            }
                            catch (Exception ex)
                            {
                                if (exOnMainSwitch != null)
                                {
                                    throw new AggregateException("Exception on main ApplySyncFromFileChange switch and exception on onBeforeAllPathsUnlock",
                                        exOnMainSwitch,
                                        ex);
                                }
                                throw ex;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }  // end ApplySyncFromFileChange

        // helper method to create a directory at a given path and set the time attributes for creation/last modified
        private static void CreateDirectoryWithAttributes(FilePath toCreate, Nullable<DateTime> creationTime, Nullable<DateTime> lastTime, out Nullable<DateTime> createdLastWriteUtc, out Nullable<DateTime> createdCreationUtc)
        {
            GenericHolder<DirectoryInfo> createdDirectoryHolder = new GenericHolder<DirectoryInfo>(null);
            Helpers.RunActionWithRetries(actionState => actionState.Value.Value = Directory.CreateDirectory(actionState.Key),
                new KeyValuePair<string, GenericHolder<DirectoryInfo>>(toCreate.ToString(), createdDirectoryHolder),
                true);

            try
            {
                if (creationTime != null)
                {
                    Helpers.RunActionWithRetries(actionState => actionState.Value.CreationTimeUtc = actionState.Key,
                        new KeyValuePair<DateTime, DirectoryInfo>((DateTime)creationTime, createdDirectoryHolder.Value),
                        true);
                }
                if (lastTime != null)
                {
                    Helpers.RunActionWithRetries(actionState => actionState.Value.LastAccessTimeUtc = actionState.Key,
                        new KeyValuePair<DateTime, DirectoryInfo>((DateTime)lastTime, createdDirectoryHolder.Value),
                        true);
                    Helpers.RunActionWithRetries(actionState => actionState.Value.LastWriteTimeUtc = actionState.Key,
                        new KeyValuePair<DateTime, DirectoryInfo>((DateTime)lastTime, createdDirectoryHolder.Value),
                        true);
                }
            }
            catch
            {
                Helpers.RunActionWithRetries(actionState => actionState.Delete(),
                    createdDirectoryHolder.Value,
                    true);
                throw;
            }

            if (lastTime == null)
            {
                createdLastWriteUtc = new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc);
                GenericHolder<Nullable<DateTime>> successLastWriteTime = new GenericHolder<Nullable<DateTime>>(null);
                Helpers.RunActionWithRetries(actionState => actionState.Key.Value = actionState.Value.LastWriteTimeUtc,
                    new KeyValuePair<GenericHolder<Nullable<DateTime>>, DirectoryInfo>(successLastWriteTime, createdDirectoryHolder.Value),
                    false);
                createdLastWriteUtc = successLastWriteTime.Value;
            }
            else
            {
                createdLastWriteUtc = lastTime;
            }

            if (creationTime == null)
            {
                createdCreationUtc = new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc);
                GenericHolder<Nullable<DateTime>> successCreationTime = new GenericHolder<Nullable<DateTime>>(null);
                Helpers.RunActionWithRetries(actionState => actionState.Key.Value = actionState.Value.CreationTimeUtc,
                    new KeyValuePair<GenericHolder<Nullable<DateTime>>, DirectoryInfo>(successCreationTime, createdDirectoryHolder.Value),
                    false);
                createdCreationUtc = successCreationTime.Value;
            }
            else
            {
                createdCreationUtc = creationTime;
            }
        }

        /// <summary>
        /// Adds a FileChange to the ProcessingQueue;
        /// will also trigger a sync if one isn't already scheduled to run
        /// </summary>
        /// <param name="toAdd">FileChange to queue</param>
        /// <param name="insertAtTop">Send true for the FileChange to be processed first on the queue, otherwise it will be last</param>
        /// <returns>Returns an error that occurred queueing the FileChange, if any</returns>
        internal CLError AddFileChangeToProcessingQueue(FileChange toAdd, bool insertAtTop, GenericHolder<List<FileChange>> errorHolder)
        {
            try
            {
                if (toAdd == null)
                {
                    throw new NullReferenceException("toAdd cannot be null");
                }
                return AddFileChangesToProcessingQueue(new FileChange[] { toAdd }, insertAtTop, errorHolder);
            }
            catch (Exception ex)
            {
                if (toAdd != null)
                {
                    errorHolder.Value = new List<FileChange>(1)
                    {
                        toAdd
                    };
                }
                return ex;
            }
        }

        /// <summary>
        /// Adds FileChanges to the ProcessingQueue;
        /// will also trigger a sync if one isn't alredy scheduled to run
        /// </summary>
        /// <param name="toAdd">FileChanges to queue</param>
        /// <param name="insertAtTop">Send true for the FileChanges to be processed first on the queue, otherwise they will be last</param>
        /// <returns>Returns an error that occurred queueing the FileChanges, if any</returns>
        internal CLError AddFileChangesToProcessingQueue(IEnumerable<FileChange> toAdd, bool insertAtTop, GenericHolder<List<FileChange>> errorHolder)
        {
            CLError toReturn = null;
            try
            {
                if (errorHolder == null)
                {
                    throw new NullReferenceException("errorHolder cannot be null");
                }

                if (toAdd == null)
                {
                    toAdd = Enumerable.Empty<FileChange>();
                }

                // if items are to be inserted at the top,
                // they must first be reversed to process in the original order
                if (insertAtTop)
                {
                    toAdd = toAdd.Reverse();
                }

                lock (QueuesTimer.TimerRunningLocker)
                {
                    bool itemAdded = false;

                    // loop through the FileChanges to add
                    foreach (FileChange currentToAdd in toAdd)
                    {
                        itemAdded = true;

                        try
                        {
                            // add the current FileChange to either the top or bottom of the queue
                            if (insertAtTop)
                            {
                                ProcessingChanges.AddFirst(currentToAdd);
                            }
                            else
                            {
                                ProcessingChanges.AddLast(currentToAdd);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (errorHolder.Value == null)
                            {
                                errorHolder.Value = new List<FileChange>();
                            }
                            errorHolder.Value.Add(currentToAdd);
                            toReturn += ex;
                        }
                    }

                    if (itemAdded)
                    {
                        // start the processing timer (or trigger immediately if the queue limit is reached)
                        if (ProcessingChanges.Count > MaxProcessingChangesBeforeTrigger)
                        {
                            QueuesTimer.TriggerTimerCompletionImmediately();
                        }
                        else
                        {
                            QueuesTimer.StartTimerIfNotRunning();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (errorHolder != null
                    && toAdd != null)
                {
                    if (errorHolder.Value == null)
                    {
                        errorHolder.Value = new List<FileChange>();
                    }
                    foreach (FileChange currentChange in toAdd)
                    {
                        if (!errorHolder.Value.Contains(currentChange))
                        {
                            errorHolder.Value.Add(currentChange);
                        }
                    }
                }
                toReturn += ex;
            }
            return toReturn;
        }

        /// <summary>
        /// Notify completion of indexing;
        /// sets IsInitialIndex to false,
        /// combines index with queued changes,
        /// and processes changes
        /// </summary>
        /// <param name="initialList">FileMetadata to use as the initial index</param>
        /// <param name="newChanges">FileChanges that need to be immediately processed as new changes</param>
        public void BeginProcessing(IEnumerable<KeyValuePair<FilePath, FileMetadata>> initialList, IEnumerable<FileChange> newChanges = null)
        {
            // Locks all new file system events from being processed until the initial index is processed,
            // afterwhich they will no longer queue up and instead process normally going forward
            InitialIndexLocker.EnterWriteLock();

            try
            {
                List<GenericHolder<Nullable<KeyValuePair<Action<DisposeCheckingHolder>, DisposeCheckingHolder>>>> startProcessingActions = new List<GenericHolder<Nullable<KeyValuePair<Action<DisposeCheckingHolder>, DisposeCheckingHolder>>>>();

                // lock to prevent the current, in-memory index from being seperately read/modified
                lock (AllPaths)
                {
                    // a null enumerable would cause an error so null-coallesce to an empty array
                    foreach (KeyValuePair<FilePath, FileMetadata> currentItem in initialList ?? new KeyValuePair<FilePath, FileMetadata>[0])
                    {
                        // add each initially indexed item to current, in-memory index
                        AllPaths.Add(currentItem);
                    }

                    // lock to prevent the queue of changes to process from being seperately read/modified
                    lock (QueuedChanges)
                    {
                        // Store a boolean whether to trigger an initial sync operation in case
                        // no changes occurred that would otherwise trigger sync
                        // bool triggerSyncWithNoChanges = true;  RKS: Removed.  Push notification will trigger the initial sync from.
                        bool triggerSyncWithNoChanges = false;

                        // only need to process new changes if the list exists
                        if (newChanges != null)
                        {
                            // loop through new changes to process
                            foreach (FileChange currentChange in newChanges)
                            {
                                // A file change will be processed which will trigger an initial sync later
                                triggerSyncWithNoChanges = false;

                                // take the new change to process and update the current, in-memory index;
                                // also queue it for processing

                                switch (currentChange.Type)
                                {
                                    case FileChangeType.Created:
                                    case FileChangeType.Modified:
                                        AllPaths[currentChange.NewPath] = currentChange.Metadata;
                                        break;
                                    case FileChangeType.Deleted:
                                        AllPaths.Remove(currentChange.NewPath);
                                        break;
                                    case FileChangeType.Renamed:
                                        AllPaths.Remove(currentChange.OldPath);
                                        AllPaths[currentChange.NewPath] = currentChange.Metadata;
                                        break;
                                }

                                GenericHolder<Nullable<KeyValuePair<Action<DisposeCheckingHolder>, DisposeCheckingHolder>>> newProcessingAction = new GenericHolder<Nullable<KeyValuePair<Action<DisposeCheckingHolder>, DisposeCheckingHolder>>>();
                                startProcessingActions.Add(newProcessingAction);

                                QueueFileChange(new FileChange(QueuedChanges,
                                    ((currentChange.Direction == SyncDirection.From && (currentChange.Type == FileChangeType.Created || currentChange.Type == FileChangeType.Modified))
                                        ? new object()
                                        : null))
                                    {
                                        NewPath = currentChange.NewPath,
                                        OldPath = currentChange.OldPath,
                                        Metadata = currentChange.Metadata,
                                        Type = currentChange.Type,
                                        DoNotAddToSQLIndex = currentChange.DoNotAddToSQLIndex,
                                        EventId = currentChange.EventId,
                                        Direction = currentChange.Direction
                                    }, newProcessingAction);
                            }
                        }

                        foreach (KeyValuePair<FilePath, FileMetadata> currentItem in AllPaths)
                        {
                            if (currentItem.Value.HashableProperties.IsFolder)
                            {
                                long createItemAddFolderCreationUtcTicks = currentItem.Value.HashableProperties.CreationTime.ToUniversalTime().Ticks;
                                FilePath[] existingAddFolderPaths;
                                if (FolderCreationTimeUtcTicksToPath.TryGetValue(createItemAddFolderCreationUtcTicks, out existingAddFolderPaths))
                                {
                                    FilePath[] appendExistingAddFolderPaths = new FilePath[existingAddFolderPaths.Length + 1];
                                    Array.Copy(existingAddFolderPaths, appendExistingAddFolderPaths, existingAddFolderPaths.Length);
                                    appendExistingAddFolderPaths[appendExistingAddFolderPaths.Length - 1] = currentItem.Key;
                                    FolderCreationTimeUtcTicksToPath[createItemAddFolderCreationUtcTicks] = appendExistingAddFolderPaths;
                                }
                                else
                                {
                                    FolderCreationTimeUtcTicksToPath.Add(createItemAddFolderCreationUtcTicks, new[] { currentItem.Key });
                                }
                            }
                        }

                        // If there were no file changes that will trigger a sync later,
                        // then trigger it now as an initial sync with an empty dictionary
                        if (triggerSyncWithNoChanges)
                        {
                            PushNotification(null);
                        }
                    }
                }

                // set initial indexing to false now so that dequeued events during initial indexing
                // will process again without infinitely queueing/dequeueing
                IsInitialIndex = false;

                // dequeue through the list of file system events that were queued during initial indexing
                while (ChangesQueueForInitialIndexing.Count > 0)
                {
                    // take the currently dequeued file system event and run it back through for processing

                    GenericHolder<Nullable<KeyValuePair<Action<DisposeCheckingHolder>, DisposeCheckingHolder>>> newProcessingAction = new GenericHolder<Nullable<KeyValuePair<Action<DisposeCheckingHolder>, DisposeCheckingHolder>>>();
                    startProcessingActions.Add(newProcessingAction);

                    ChangesQueueHolder currentChange = ChangesQueueForInitialIndexing.Dequeue();
                    CheckMetadataAgainstFile(currentChange.newPath,
                        currentChange.oldPath,
                        currentChange.changeType,
                        currentChange.folderOnly,
                        true,
                        newProcessingAction);
                }

                // null the pointer for the initial index queue so it can be cleared from memory
                ChangesQueueForInitialIndexing = null;

                foreach (GenericHolder<Nullable<KeyValuePair<Action<DisposeCheckingHolder>, DisposeCheckingHolder>>> startProcessing in startProcessingActions)
                {
                    if (startProcessing.Value != null
                        && !((KeyValuePair<Action<DisposeCheckingHolder>, DisposeCheckingHolder>)startProcessing.Value).Value.IsDisposed)
                    {
                        ((KeyValuePair<Action<DisposeCheckingHolder>, DisposeCheckingHolder>)startProcessing.Value).Key(((KeyValuePair<Action<DisposeCheckingHolder>, DisposeCheckingHolder>)startProcessing.Value).Value);
                    }
                }
            }
            finally
            {
                InitialIndexLocker.ExitWriteLock();
            }
        }

        private CLError AssignDependencies(KeyValuePair<FileChangeSource, FileChangeWithDependencies>[] dependencyChanges, Dictionary<FileChangeWithDependencies, KeyValuePair<FileChange, FileChangeSource>> OriginalFileChangeMappings, out HashSet<FileChangeWithDependencies> PulledChanges, originalQueuedChangesIndexesByInMemoryIdsBase originalQueuedChangesIndexesByInMemoryIds)
        {
            CLError toReturn = null;
            try
            {
                List<FileChange> removeFromSql = new List<FileChange>();

                using (SQLTransactionalBase sqlTran = Indexer.GetNewTransaction())
                {
                    try
                    {
                        PulledChanges = new HashSet<FileChangeWithDependencies>();

                        for (int outerChangeIndex = 0; outerChangeIndex < dependencyChanges.Length; outerChangeIndex++)
                        {
                            KeyValuePair<FileChangeSource, FileChangeWithDependencies> OuterChangePair = dependencyChanges[outerChangeIndex];
                            FileChangeWithDependencies OuterFileChange = OuterChangePair.Value;

                            if (OuterChangePair.Key != FileChangeSource.QueuedChanges
                                && !PulledChanges.Contains(OuterFileChange))
                            {
                                for (int innerChangeIndex = outerChangeIndex + 1; innerChangeIndex < dependencyChanges.Length; innerChangeIndex++)
                                {
                                    FileChangeWithDependencies InnerFileChange = dependencyChanges[innerChangeIndex].Value;

                                    if (!PulledChanges.Contains(InnerFileChange))
                                    {
                                        List<FileChangeWithDependencies> DisposeChanges;
                                        bool ContinueProcessing;

                                        switch (InnerFileChange.Type)
                                        {
                                            case FileChangeType.Created:
                                            case FileChangeType.Modified:
                                            CLError creationModificationCheckError = CreationModificationDependencyCheck(OuterFileChange, InnerFileChange, PulledChanges, out DisposeChanges, sqlTran);
                                                ContinueProcessing = true;
                                                if (creationModificationCheckError != null)
                                                {
                                                    toReturn += new AggregateException("Error in CreationModificationDependencyCheck", creationModificationCheckError.GrabExceptions());
                                                }
                                                break;
                                            case FileChangeType.Renamed:
                                                CLError renameCheckError = RenameDependencyCheck(OuterFileChange, InnerFileChange, PulledChanges, out DisposeChanges, out ContinueProcessing, sqlTran);
                                                if (renameCheckError != null)
                                                {
                                                    toReturn += new AggregateException("Error in RenameDependencyCheck", renameCheckError.GrabExceptions());
                                                }
                                                break;
                                            case FileChangeType.Deleted:
                                                CLError deleteCheckError = DeleteDependencyCheck(OuterFileChange, InnerFileChange, PulledChanges, out DisposeChanges, out ContinueProcessing, sqlTran);
                                                if (deleteCheckError != null)
                                                {
                                                    toReturn += new AggregateException("Error in DeleteDependencyCheck", deleteCheckError.GrabExceptions());
                                                }
                                                break;
                                            default:
                                                throw new InvalidOperationException("Unknown FileChangeType for InnerFileChange: " + InnerFileChange.Type.ToString());
                                        }

                                        if (DisposeChanges != null)
                                        {
                                            foreach (FileChangeWithDependencies CurrentDisposal in DisposeChanges)
                                            {
                                            	_trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: AssignDependencies: CurrentDisposal: Direction: {0}. Type: {1}. OldPath: {2}. NewPath: {3}.",
                                                    CurrentDisposal.Direction.ToString(),
                                                    CurrentDisposal.Type.ToString(),
                                                    CurrentDisposal.OldPath != null ? CurrentDisposal.OldPath : "NoOldPath",
                                                    CurrentDisposal.NewPath != null ? CurrentDisposal.NewPath : "NoNewPath"));
                                                KeyValuePair<FileChange, FileChangeSource> CurrentOriginalMapping;
                                                if (OriginalFileChangeMappings != null
                                                    && OriginalFileChangeMappings.TryGetValue(CurrentDisposal, out CurrentOriginalMapping))
                                                {
                                                	_trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: AssignDependencies: CurrentOriginalMapping: Direction: {0}. Type: {1}. OldPath: {2}. NewPath: {3}.",
                                                        CurrentOriginalMapping.Key.Direction.ToString(),
                                                        CurrentOriginalMapping.Key.Type.ToString(),
                                                        CurrentOriginalMapping.Key.OldPath != null ? CurrentOriginalMapping.Key.OldPath : "NoOldPath",
                                                        CurrentOriginalMapping.Key.NewPath != null ? CurrentOriginalMapping.Key.NewPath : "NoNewPath"));
                                                    CurrentOriginalMapping.Key.Dispose();

                                                	if (CurrentOriginalMapping.Value == FileChangeSource.QueuedChanges)
                                                	{
                                                    	_trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: AssignDependencies: CurrentOriginalMapping: Remove this CurrentOriginalMapping from originalQueuedChangesIndexesByInMemoryIds."));
                                                    	RemoveFileChangeFromQueuedChanges(CurrentOriginalMapping.Key, originalQueuedChangesIndexesByInMemoryIds);
                                                	}

                                                    if (CurrentDisposal.EventId != 0)
                                                    {
                                                        _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: AssignDependencies: CurrentOriginalMapping: Add CurrentDisposal to removeFromSql."));
                                                        removeFromSql.Add(CurrentDisposal);
                                                    }
                                                }
                                            	else
                                            	{
                                                	_trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: AssignDependencies: ERROR: CurrentOriginalMapping not found."));
                                                }
                                            }
                                        }

                                        if (!ContinueProcessing)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        if (removeFromSql.Count > 0)
                        {
                            try
                            {
                                CLError updateSQLError = Indexer.MergeEventsIntoDatabase(
                                    removeFromSql.Select(currentToRemove => new FileChangeMerge(null, currentToRemove)),
                                    sqlTran);
                                if (updateSQLError != null)
                                {
                                	// condition for all the exceptions being keys which were not found to delete (possible if we end up deleting the same event id twice, which isn't really an error)
                                	if (!updateSQLError.GrabExceptions()
                                        /* ! */.All(currentAggregate => currentAggregate is AggregateException && ((AggregateException)currentAggregate).InnerExceptions.All(currentInnerException => currentInnerException is KeyNotFoundException)))
                                	{
                                    	toReturn += new AggregateException("Error updating SQL", updateSQLError.GrabExceptions());
                                	}
                                }
                            }
                            catch (Exception ex)
                            {
                                toReturn += new Exception("Error updating SQL", ex);
                            }
                        }

                        sqlTran.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                PulledChanges = Helpers.DefaultForType<HashSet<FileChangeWithDependencies>>();
                toReturn += ex;
            }
            return toReturn;
        }

        private CLError CreationModificationDependencyCheck(FileChangeWithDependencies EarlierChange, FileChangeWithDependencies LaterChange, HashSet<FileChangeWithDependencies> PulledChanges, out List<FileChangeWithDependencies> DisposeChanges, SQLTransactionalBase sqlTran)
        {
            CLError toReturn = null;
            DisposeChanges = null;
            try
            {
                foreach (FileChangeWithDependencies CurrentEarlierChange in EnumerateDependenciesFromFileChangeDeepestLevelsFirst(EarlierChange)
                    .OfType<FileChangeWithDependencies>())
                {
                    if (LaterChange.NewPath.Contains(CurrentEarlierChange.NewPath))
                    {
                        if (FilePathComparer.Instance.Equals(LaterChange.NewPath, CurrentEarlierChange.NewPath))
                        {
                            if (EarlierChange.Type == FileChangeType.Created
                                && LaterChange.Type == FileChangeType.Modified)
                            {
                                LaterChange.Type = FileChangeType.Created;

                                CLError updateLaterChangeAsCreatedError = Indexer.MergeEventsIntoDatabase(new[] { new FileChangeMerge(LaterChange) }, sqlTran);
                                if (updateLaterChangeAsCreatedError != null)
                                {
                                    throw new AggregateException("Error updating LaterChange in CreationModificationDependencyCheck to Created Type", updateLaterChangeAsCreatedError.GrabExceptions());
                                }
                            }

                            if (DisposeChanges == null)
                            {
                                DisposeChanges = new List<FileChangeWithDependencies>(new FileChangeWithDependencies[] { EarlierChange });
                            }
                            else
                            {
                                DisposeChanges.Add(EarlierChange);
                            }

                            PulledChanges.Add(EarlierChange);

                            foreach (FileChangeWithDependencies laterParent in EnumerateDependenciesFromFileChangeDeepestLevelsFirst(LaterChange)
                                .OfType<FileChangeWithDependencies>()
                                .Where(currentParentCheck => currentParentCheck.Dependencies.Contains(EarlierChange)))
                            {
                                laterParent.RemoveDependency(EarlierChange);
                            }
                        }
                        else
                        {
                            CurrentEarlierChange.AddDependency(LaterChange);
                            if (DependencyDebugging)
                            {
                                Helpers.CheckFileChangeDependenciesForDuplicates(CurrentEarlierChange);
                            }
                            PulledChanges.Add(LaterChange);
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                toReturn += ex;
            }
            return toReturn;
        }

        private CLError RenameDependencyCheck(FileChangeWithDependencies EarlierChange, FileChangeWithDependencies LaterChange, HashSet<FileChangeWithDependencies> PulledChanges, out List<FileChangeWithDependencies> DisposeChanges, out bool ContinueProcessing, SQLTransactionalBase sqlTran)
        {
            CLError toReturn = null;
            try
            {
                bool DependenciesAddedToLaterChange = false;
                DisposeChanges = null;
                HashSet<FileChangeWithDependencies> RenamePathSearches = null;

                _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: RenameDependencyCheck: LaterChange: Direction: {0}. Type: {1}. OldPath: {2}. NewPath: {3}.",
                            LaterChange.Direction.ToString(),
                            LaterChange.Type.ToString(),
                            LaterChange.OldPath != null ? LaterChange.OldPath : "NoOldPath",
                            LaterChange.NewPath != null ? LaterChange.NewPath : "NoNewPath"));
                foreach (FileChangeWithDependencies CurrentEarlierChange in EnumerateDependenciesFromFileChangeDeepestLevelsFirst(EarlierChange)
                    .Reverse()
                    .OfType<FileChangeWithDependencies>())
                {
                    bool breakOutOfEnumeration = false;
                    _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: RenameDependencyCheck: CurrentEarlierChange: Direction: {0}. Type: {1}. OldPath: {2}. NewPath: {3}.", 
                                CurrentEarlierChange.Direction.ToString(),
                                CurrentEarlierChange.Type.ToString(),
                                CurrentEarlierChange.OldPath != null ? CurrentEarlierChange.OldPath : "NoOldPath",
                                CurrentEarlierChange.NewPath != null ? CurrentEarlierChange.NewPath : "NoNewPath"));
                    switch (CurrentEarlierChange.Type)
                    {
                        case FileChangeType.Renamed:
                            _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: RenameDependencyCheck: Earlier change is renamed."));
                            if (!DependenciesAddedToLaterChange
                                && (RenamePathSearches == null || !RenamePathSearches.Contains(CurrentEarlierChange)))
                            {
                                _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: RenameDependencyCheck: Loop thru onlyRenamePathsFromTop."));
                                foreach (FileChangeWithDependencies CurrentInnerRename in EnumerateDependenciesFromFileChangeDeepestLevelsFirst(CurrentEarlierChange, onlyRenamePathsFromTop: true)
                                    .OfType<FileChangeWithDependencies>())
                                {
                                    _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: RenameDependencyCheck: CurrentInnerRename: Direction: {0}. OldPath: {1}. NewPath: {2}.",
                                                CurrentInnerRename.Direction.ToString(),
                                                CurrentInnerRename.OldPath != null ? CurrentInnerRename.OldPath : "NoOldPath",
                                                CurrentInnerRename.NewPath != null ? CurrentInnerRename.NewPath : "NoNewPath"));
                                    if (RenamePathSearches == null)
                                    {
                                        _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: RenameDependencyCheck: RenamePathSearches is null."));
                                        RenamePathSearches = new HashSet<FileChangeWithDependencies>(new FileChangeWithDependencies[] { CurrentInnerRename });
                                    }
                                    else
                                    {
                                        _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: RenameDependencyCheck: RenamePathSearches not null. Add this inner rename to RenamePathSearches."));
                                        RenamePathSearches.Add(CurrentInnerRename);
                                    }
                                    if (CurrentInnerRename.NewPath.Contains(LaterChange.OldPath)
                                        || LaterChange.OldPath.Contains(CurrentInnerRename.NewPath))
                                    {
                                        _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: RenameDependencyCheck: Inner new path contains laterChange old path, or laterChange old path contains inner new path."));
                                        foreach (FileChangeWithDependencies dependencyToMove in CurrentInnerRename.Dependencies)
                                        {
                                            _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: RenameDependencyCheck: Remove dependency dependencyToMove: Direction: {0}. OldPath: {1}. NewPath: {2}.",
                                                        dependencyToMove.Direction.ToString(),
                                                        dependencyToMove.OldPath != null ? dependencyToMove.OldPath : "NoOldPath",
                                                        dependencyToMove.NewPath != null ? dependencyToMove.NewPath : "NoNewPath"));
                                            CurrentInnerRename.RemoveDependency(dependencyToMove);
                                            LaterChange.AddDependency(dependencyToMove);
                                        }
                                        DependenciesAddedToLaterChange = true;

                                        CurrentInnerRename.AddDependency(LaterChange);
                                        if (DependencyDebugging)
                                        {
                                            _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: RenameDependencyCheck: Call CheckFileChangeDependenciesForDuplicates on inner rename."));
                                            Helpers.CheckFileChangeDependenciesForDuplicates(CurrentInnerRename);
                                        }
                                        PulledChanges.Add(LaterChange);
                                        break;
                                    }
                                }
                            }
                            break;
                        case FileChangeType.Created:
                        case FileChangeType.Modified:
                            _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: RenameDependencyCheck: Earlier change is Created or Modified."));
                            if (CurrentEarlierChange.NewPath.Contains(LaterChange.OldPath))
                            {
                                _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: RenameDependencyCheck: Earlier change contains OldPath."));
                                if (FilePathComparer.Instance.Equals(CurrentEarlierChange.NewPath, LaterChange.OldPath))
                                {
                                    _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: RenameDependencyCheck: NewPath equals OldPath."));
                                    CurrentEarlierChange.NewPath = LaterChange.NewPath;
                                    CLError updateSqlError = Indexer.MergeEventsIntoDatabase(Helpers.EnumerateSingleItem(new FileChangeMerge(CurrentEarlierChange)), sqlTran);
                                    if (updateSqlError != null)
                                    {
                                        toReturn += new AggregateException("Error updating SQL after replacing NewPath", updateSqlError.GrabExceptions());
                                    }

                                    if (CurrentEarlierChange.Type == FileChangeType.Created)
                                    {
                                        _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: RenameDependencyCheck: Earlier change is Created."));
                                        if (DisposeChanges == null)
                                        {
                                            DisposeChanges = new List<FileChangeWithDependencies>(Helpers.EnumerateSingleItem(LaterChange));
                                        }
                                        else
                                        {
                                            _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: RenameDependencyCheck: Add later change to changes to dispose."));
                                            DisposeChanges.Add(LaterChange);
                                        }

                                        PulledChanges.Add(LaterChange);

                                        foreach (FileChangeWithDependencies laterParent in EnumerateDependenciesFromFileChangeDeepestLevelsFirst(EarlierChange)
                                            .OfType<FileChangeWithDependencies>()
                                            .Where(currentParentCheck => currentParentCheck.Dependencies.Contains(LaterChange)))
                                        {
                                            _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: RenameDependencyCheck: Remove dependency from laterParent: LaterChange: Direction: {0}. OldPath: {1}. NewPath: {2}.",
                                                        LaterChange.Direction.ToString(),
                                                        LaterChange.OldPath != null ? LaterChange.OldPath : "NoOldPath",
                                                        LaterChange.NewPath != null ? LaterChange.NewPath : "NoNewPath"));
                                            laterParent.RemoveDependency(LaterChange);
                                        }

                                        DependenciesAddedToLaterChange = true;
                                    }
                                    else if (!DependenciesAddedToLaterChange)
                                    {
                                        if (LaterChange.EventId == 0)
                                        {
                                            Indexer.MergeEventsIntoDatabase(Helpers.EnumerateSingleItem(new FileChangeMerge(LaterChange)), sqlTran);
                                        }
                                        Indexer.SwapOrderBetweenTwoEventIds(CurrentEarlierChange.EventId, LaterChange.EventId, sqlTran);

                                        _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: RenameDependencyCheck: No DependenciesAddeedToLaterChange (2)."));
                                        LaterChange.AddDependency(CurrentEarlierChange);
                                        if (DependencyDebugging)
                                        {
                                            Helpers.CheckFileChangeDependenciesForDuplicates(LaterChange);
                                        }
                                        PulledChanges.Add(CurrentEarlierChange);
                                        DependenciesAddedToLaterChange = true;
                                    }
                                }
                                else
                                {
                                    // child of path of overlap of CurrentEarlierChange's NewPath with LaterChange's OldPath
                                    // (whose parent will be replaced by the change of LaterChange's NewPath
                                    FilePath renamedOverlapChild = CurrentEarlierChange.NewPath;
                                    // variable for recursive checking against the rename's OldPath
                                    FilePath renamedOverlap = renamedOverlapChild.Parent;

                                    // loop till recursing parent of current path level is null
                                    _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: RenameDependencyCheck: NewPath does not equal OldPath. Loop until recursing parent is null. renamedOverlapChild: {0}.", renamedOverlapChild.Name));
                                    while (renamedOverlap != null)
                                    {
                                        // when the rename's OldPath matches the current recursive path parent level,
                                        // replace the child's parent with the rename's NewPath and break out of the checking loop
                                        _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: RenameDependencyCheck: Process renamedOverlap: {0}.", renamedOverlap.Name));
                                        if (FilePathComparer.Instance.Equals(renamedOverlap, LaterChange.OldPath))
                                        {
                                            _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: RenameDependencyCheck: renamedOverlap equals later change OldPath: {0}. Merge earlier change to SQL.", LaterChange.OldPath.Name));
                                            renamedOverlapChild.Parent = LaterChange.NewPath;
                                            CLError replacePathPortionError = Indexer.MergeEventsIntoDatabase(Helpers.EnumerateSingleItem(new FileChangeMerge(CurrentEarlierChange)), sqlTran);
                                            if (replacePathPortionError != null)
                                            {
                                                toReturn += new AggregateException("Error replacing a portion of the path of CurrentEarlierChange", replacePathPortionError.GrabExceptions());
                                            }

                                            if (LaterChange.EventId == 0)
                                            {
                                                Indexer.MergeEventsIntoDatabase(Helpers.EnumerateSingleItem(new FileChangeMerge(LaterChange)), sqlTran);
                                            }
                                            Indexer.SwapOrderBetweenTwoEventIds(CurrentEarlierChange.EventId, LaterChange.EventId, sqlTran);
                                            break;
                                        }

                                        // set recursing path variables one level higher
                                        renamedOverlapChild = renamedOverlap;
                                        renamedOverlap = renamedOverlap.Parent;
                                    }
                                    
                                    if (!DependenciesAddedToLaterChange)
                                    {
                                        _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: RenameDependencyCheck: No DependenciesAddeedToLaterChange."));
                                        LaterChange.AddDependency(CurrentEarlierChange);
                                        if (DependencyDebugging)
                                        {
                                            Helpers.CheckFileChangeDependenciesForDuplicates(LaterChange);
                                        }
                                        PulledChanges.Add(CurrentEarlierChange);
                                        DependenciesAddedToLaterChange = true;
                                    }
                                }
                            }
                            // case when something is already synced with the server and a folder is created seperately and then the thing that already existed is moved into that new folder;
                            // needs to make the thing moved into the new folder dependent on the creation of the folder
                            else if (EarlierChange.Type == FileChangeType.Created
                                && EarlierChange.Metadata != null
                                && EarlierChange.Metadata.HashableProperties.IsFolder
                                && LaterChange.NewPath.Contains(EarlierChange.NewPath))
                            {
                                EarlierChange.AddDependency(LaterChange);
                                if (DependencyDebugging)
                                {
                                    Helpers.CheckFileChangeDependenciesForDuplicates(EarlierChange);
                                }
                                PulledChanges.Add(LaterChange);
                            }
                            break;
                        case FileChangeType.Deleted:// possible error condition, I am not sure this case should ever hit
                            _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: RenameDependencyCheck: Earlier change is Deleted."));
                            if (LaterChange.OldPath.Contains(CurrentEarlierChange.NewPath))
                            {
                                _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: RenameDependencyCheck: Later old path contains earlier new path."));
                                breakOutOfEnumeration = true;
                            }
                            break;
                        default:
                            throw new InvalidOperationException("Unknown FileChangeType for CurrentEarlierChange: " + CurrentEarlierChange.Type.ToString());
                    }
                    if (breakOutOfEnumeration)
                    {
                        _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: RenameDependencyCheck: Break out of enumeration."));
                        break;
                    }
                }

                bool localContinueProcessing = !PulledChanges.Contains(EarlierChange);
                ContinueProcessing = localContinueProcessing;
                _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: RenameDependencyCheck: Continue processing: {0}.", localContinueProcessing));
            }
            catch (Exception ex)
            {
                DisposeChanges = Helpers.DefaultForType<List<FileChangeWithDependencies>>();
                ContinueProcessing = Helpers.DefaultForType<bool>();
                toReturn += ex;
            }

            if (toReturn != null)
            {
                _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: RenameDependencyCheck: ERROR: {0}.", toReturn.errorDescription));
            }
            return toReturn;
        }

        private CLError DeleteDependencyCheck(FileChangeWithDependencies EarlierChange, FileChangeWithDependencies LaterChange, HashSet<FileChangeWithDependencies> PulledChanges, out List<FileChangeWithDependencies> DisposeChanges, out bool ContinueProcessing, SQLTransactionalBase sqlTran)
        {
            CLError toReturn = null;
            try
            {
                DisposeChanges = null;

                foreach (FileChangeWithDependencies CurrentEarlierChange in EnumerateDependenciesFromFileChangeDeepestLevelsFirst(EarlierChange))
                {
                    if (CurrentEarlierChange.NewPath.Contains(LaterChange.NewPath))
                    {
                        if (DisposeChanges == null)
                        {
                            DisposeChanges = new List<FileChangeWithDependencies>(new FileChangeWithDependencies[] { CurrentEarlierChange });
                        }
                        else
                        {
                            DisposeChanges.Add(CurrentEarlierChange);
                        }

                        PulledChanges.Add(CurrentEarlierChange);
                        
                        foreach (FileChangeWithDependencies laterParent in EnumerateDependenciesFromFileChangeDeepestLevelsFirst(EarlierChange)
                            .OfType<FileChangeWithDependencies>()
                            .Where(currentParentCheck => currentParentCheck.Dependencies.Contains(CurrentEarlierChange)))
                        {
                            laterParent.RemoveDependency(CurrentEarlierChange);
                        }

                        if (CurrentEarlierChange.Type == FileChangeType.Created
                            && FilePathComparer.Instance.Equals(CurrentEarlierChange.NewPath, LaterChange.NewPath))
                        {
                            DisposeChanges.Add(LaterChange);
                            PulledChanges.Add(LaterChange);
                            
                            foreach (FileChangeWithDependencies laterParent in EnumerateDependenciesFromFileChangeDeepestLevelsFirst(EarlierChange)
                                .OfType<FileChangeWithDependencies>()
                                .Where(currentParentCheck => currentParentCheck.Dependencies.Contains(LaterChange)))
                            {
                                laterParent.RemoveDependency(LaterChange);
                            }

                            break;
                        }
                    }

                    if (CurrentEarlierChange.Type == FileChangeType.Renamed
                        && LaterChange.NewPath.Contains(CurrentEarlierChange.NewPath))
                    {
                        FilePath newLaterChangeNewPath = LaterChange.NewPath.Copy();
                        FilePath.ApplyRename(newLaterChangeNewPath, CurrentEarlierChange.NewPath, CurrentEarlierChange.OldPath);
                        if (!FilePathComparer.Instance.Equals(newLaterChangeNewPath, LaterChange.NewPath))
                        {
                            LaterChange.NewPath = newLaterChangeNewPath;
                            CLError replacePathPortionError = Indexer.MergeEventsIntoDatabase(Helpers.EnumerateSingleItem(new FileChangeMerge(LaterChange)), sqlTran);
                            if (replacePathPortionError != null)
                            {
                                toReturn += new AggregateException("Error replacing a portion of the path of CurrentEarlierChange", replacePathPortionError.GrabExceptions());
                            }
                        }
                    }
                }

                ContinueProcessing = !PulledChanges.Contains(EarlierChange);
            }
            catch (Exception ex)
            {
                DisposeChanges = Helpers.DefaultForType<List<FileChangeWithDependencies>>();
                ContinueProcessing = Helpers.DefaultForType<bool>();
                toReturn += ex;
            }
            return toReturn;
        }

        internal CLError AssignDependencies(IEnumerable<PossiblyStreamableFileChange> toAssign,
            IEnumerable<FileChange> currentFailures,
            out IEnumerable<PossiblyStreamableFileChange> outputChanges,
            out IEnumerable<FileChange> outputFailures,
            List<FileChange> failedOutChanges)
        {
            CLError toReturn = null;
            try
            {
                HashSet<FileChangeWithDependencies> PulledChanges;
                Func<FileChangeSource, PossiblyStreamableFileChange, Dictionary<FileChangeWithDependencies, KeyValuePair<GenericHolder<bool>, StreamContext>>, Dictionary<FileChangeWithDependencies, KeyValuePair<FileChange, FileChangeSource>>, FileChangeWithDependencies> convertChange = (originalSource, inputChange, streamMappings, originalChangeMappings) =>
                    {
                        if (inputChange.FileChange is FileChangeWithDependencies)
                        {
                            streamMappings[(FileChangeWithDependencies)inputChange.FileChange] = new KeyValuePair<GenericHolder<bool>, StreamContext>(new GenericHolder<bool>(false), inputChange.StreamContext);
                            originalChangeMappings[(FileChangeWithDependencies)inputChange.FileChange] = new KeyValuePair<FileChange, FileChangeSource>(inputChange.FileChange, originalSource);
                            return (FileChangeWithDependencies)inputChange.FileChange;
                        }

                        FileChangeWithDependencies outputChange;
                        CLError conversionError = FileChangeWithDependencies.CreateAndInitialize(
                            inputChange.FileChange,
                            /* initialDependencies */ null,
                            out outputChange,
                            fileDownloadMoveLocker: inputChange.FileChange.fileDownloadMoveLocker);
                        if (conversionError != null)
                        {
                            throw new AggregateException("Error converting FileChange to FileChangeWithDependencies", conversionError.GrabExceptions());
                        }
                        originalChangeMappings[outputChange] = new KeyValuePair<FileChange, FileChangeSource>(inputChange.FileChange, originalSource);
                        streamMappings[outputChange] = new KeyValuePair<GenericHolder<bool>, StreamContext>(new GenericHolder<bool>(false), inputChange.StreamContext);
                        return outputChange;
                    };
                Dictionary<FileChangeWithDependencies, KeyValuePair<GenericHolder<bool>, StreamContext>> originalFileStreams = new Dictionary<FileChangeWithDependencies, KeyValuePair<GenericHolder<bool>, StreamContext>>();

                Dictionary<FileChangeWithDependencies, KeyValuePair<FileChange, FileChangeSource>> OriginalFileChangeMappings = new Dictionary<FileChangeWithDependencies, KeyValuePair<FileChange, FileChangeSource>>();

                KeyValuePair<FileChangeSource, FileChangeWithDependencies>[] assignmentsWithDependencies = toAssign
                    .Select(currentToAssign => new KeyValuePair<FileChangeSource, FileChangeWithDependencies>(FileChangeSource.ProcessingChanges, convertChange(FileChangeSource.ProcessingChanges, currentToAssign, originalFileStreams, OriginalFileChangeMappings)))
                    .Concat(currentFailures.Select(currentFailure => new KeyValuePair<FileChangeSource, FileChangeWithDependencies>(FileChangeSource.FailureQueue, convertChange(FileChangeSource.FailureQueue, new PossiblyStreamableFileChange(currentFailure, null), originalFileStreams, OriginalFileChangeMappings))))
                    .Concat((failedOutChanges ?? Enumerable.Empty<FileChange>()).Select(currentFailedOut => new KeyValuePair<FileChangeSource, FileChangeWithDependencies>(FileChangeSource.FailedOutList, convertChange(FileChangeSource.FailedOutList, new PossiblyStreamableFileChange(currentFailedOut, null), originalFileStreams, OriginalFileChangeMappings))))
                    .OrderBy(currentSourcedChange => currentSourcedChange.Value.EventId)
                    .ToArray();
                toReturn = AssignDependencies(assignmentsWithDependencies,
                    OriginalFileChangeMappings,
                    out PulledChanges,
                    originalQueuedChangesIndexesByInMemoryIds: null);

                List<PossiblyStreamableFileChange> outputChangeList = new List<PossiblyStreamableFileChange>();
                List<FileChange> outputFailureList = new List<FileChange>();

                foreach (KeyValuePair<FileChangeSource, FileChangeWithDependencies> currentAssignment in assignmentsWithDependencies)
                {
                    if (PulledChanges == null
                        || !PulledChanges.Contains(currentAssignment.Value))
                    {
                        if (currentAssignment.Key == FileChangeSource.FailureQueue)
                        {
                            outputFailureList.Add(currentAssignment.Value);
                        }
                        else if (currentAssignment.Key == FileChangeSource.FailedOutList)
                        {
                            // no need for null-check failedOutChanges since there would be no FileChangeSource for FailedOutList if there was no FailedOut changes
                            failedOutChanges.Add(currentAssignment.Value);
                        }
                        else
                        {
                            KeyValuePair<GenericHolder<bool>, StreamContext> originalStreamContext;
                            if (originalFileStreams.TryGetValue(currentAssignment.Value, out originalStreamContext))
                            {
                                originalStreamContext.Key.Value = true;
                                outputChangeList.Add(new PossiblyStreamableFileChange(currentAssignment.Value, originalStreamContext.Value));
                            }
                            else
                            {
                                outputChangeList.Add(new PossiblyStreamableFileChange(currentAssignment.Value, null));
                            }
                        }
                    }
                }

                foreach (KeyValuePair<GenericHolder<bool>, StreamContext> streamValue in originalFileStreams.Values)
                {
                    if (streamValue.Key.Value == false
                        && streamValue.Value != null)
                    {
                        try
                        {
                            streamValue.Value.Dispose();
                        }
                        catch (Exception ex)
                        {
                            toReturn += ex;
                        }
                    }
                }

                outputChanges = outputChangeList;
                outputFailures = outputFailureList;
            }
            catch (Exception ex)
            {
                outputChanges = Helpers.DefaultForType<IEnumerable<PossiblyStreamableFileChange>>();
                outputFailures = Helpers.DefaultForType<IEnumerable<FileChange>>();
                toReturn += ex;
            }
            return toReturn;
        }

        /// <summary>
        /// Method to be called within the context of the main lock of the Sync service
        /// which locks the changed files, updates metadata, and outputs a sorted FileChange array for processing
        /// </summary>
        /// <param name="initialFailures">Input FileChanges from FailureQueue to integrate into dependency checking</param>
        /// <param name="outputChanges">Output array of FileChanges to process</param>
        /// <param name="outputChangesInError">Output array of FileChanges with observed errors for requeueing, may be empty but never null</param>
        /// <param name="nullChangeFound">(output) Whether a null FileChange was found in the processing queue (which does not get output)</param>
        /// <param name="failedOutChanges">The possibly null queue containing failed out changes which should be locked if it exists by the method caller</param>
        /// <returns>Returns error(s) that occurred finalizing the FileChange array, if any</returns>
        internal CLError GrabPreprocessedChanges(IEnumerable<PossiblyPreexistingFileChangeInError> initialFailures,
            out IEnumerable<PossiblyStreamableFileChange> outputChanges,
            out int outputChangesCount,
            out IEnumerable<PossiblyPreexistingFileChangeInError> outputChangesInError,
            out int outputChangesInErrorCount,
            out bool nullChangeFound,
            List<FileChange> failedOutChanges)
        {
            CLError toReturn = null;
            List<KeyValuePair<FileChangeMerge, FileChange>> queuedChangesNeedMergeToSql = new List<KeyValuePair<FileChangeMerge, FileChange>>();
            try
            {
                lock (QueuedChanges)
                {
                    lock (QueuesTimer.TimerRunningLocker)
                    {
                        Func<KeyValuePair<FileChangeSource, KeyValuePair<bool, FileChange>>, FileChangeWithDependencies> convertChange = toConvert =>
                            {
                                FileChangeWithDependencies converted;
                                CLError conversionError = FileChangeWithDependencies.CreateAndInitialize(
                                    toConvert.Value.Value,
                                    ((toConvert.Value.Value is FileChangeWithDependencies) ? ((FileChangeWithDependencies)toConvert.Value.Value).Dependencies : null),
                                    out converted,
                                    fileDownloadMoveLocker: toConvert.Value.Value.fileDownloadMoveLocker);
                                if (conversionError != null)
                                {
                                    throw new AggregateException("Error converting FileChange to FileChangeWithDependencies", conversionError.GrabExceptions());
                                }
                                else if (DependencyDebugging
                                    && (toConvert.Value.Value is FileChangeWithDependencies)
                                    && ((FileChangeWithDependencies)toConvert.Value.Value).DependenciesCount > 0)
                                {
                                    Helpers.CheckFileChangeDependenciesForDuplicates(converted);
                                }
                                if (converted.EventId == 0
                                    && toConvert.Key != FileChangeSource.QueuedChanges)
                                {
                                    throw new ArgumentException("Cannot communicate FileChange without EventId; FileChangeSource: " + toConvert.Key.ToString());
                                }
                                return converted;
                            };

                        GenericHolder<bool> nullFound = new GenericHolder<bool>(false);
                        Func<object, GenericHolder<bool>, bool> nullCheckAndMarkFound = (referenceToCheck, nullFoundHolder) =>
                            {
                                if (referenceToCheck == null)
                                {
                                    nullFoundHolder.Value = true;
                                    return false;
                                }
                                return true;
                            };

                        Dictionary<long, FilePath> originalQueuedChangesIndexesByInMemoryIds = new Dictionary<long, FilePath>();
                        originalQueuedChangesIndexesByInMemoryIdsBase originalQueuedChangesIndexesByInMemoryIdsWrapped = new originalQueuedChangesIndexesByInMemoryIdsFromDictionary(originalQueuedChangesIndexesByInMemoryIds);
                        Func<KeyValuePair<FilePath, FileChange>, Dictionary<long, FilePath>, KeyValuePair<FileChangeSource, KeyValuePair<bool, FileChange>>> reselectQueuedChangeAndAddToMapping =
                            (queuedChange, queuedMappings) =>
                            {
                                queuedMappings[queuedChange.Value.InMemoryId] = queuedChange.Key;
                                return new KeyValuePair<FileChangeSource, KeyValuePair<bool, FileChange>>(FileChangeSource.QueuedChanges, new KeyValuePair<bool, FileChange>(false, queuedChange.Value));
                            };

                        var AllFileChanges = (ProcessingChanges.DequeueAll()
                            .Where(currentProcessingChange => nullCheckAndMarkFound(currentProcessingChange, nullFound)) // added nullable FileChange so that syncing can be triggered by queueing a null
                            .Select(currentProcessingChange => new KeyValuePair<FileChangeSource, KeyValuePair<bool, FileChange>>(FileChangeSource.ProcessingChanges, new KeyValuePair<bool, FileChange>(false, currentProcessingChange)))
                            .Concat(initialFailures.Select(currentInitialFailure => new KeyValuePair<FileChangeSource, KeyValuePair<bool, FileChange>>(FileChangeSource.FailureQueue, new KeyValuePair<bool, FileChange>(currentInitialFailure.IsPreexisting, currentInitialFailure.FileChange)))))
                            .Concat((failedOutChanges ?? Enumerable.Empty<FileChange>()).Select(currentFailedOut => new KeyValuePair<FileChangeSource, KeyValuePair<bool, FileChange>>(FileChangeSource.FailedOutList, new KeyValuePair<bool,FileChange>(false, currentFailedOut))))
                            .OrderBy(eventOrdering => eventOrdering.Value.Value.EventId)
                            .Concat(QueuedChanges
                                .OrderBy(memoryIdOrdering => memoryIdOrdering.Value.InMemoryId)
                                .Select(queuedChange => reselectQueuedChangeAndAddToMapping(queuedChange, originalQueuedChangesIndexesByInMemoryIds)))
                            .Select(currentFileChange => new
                            {
                                ExistingError = currentFileChange.Value.Key,
                                OriginalFileChange = currentFileChange.Value.Value,
                                DependencyFileChange = convertChange(currentFileChange),
                                SourceType = currentFileChange.Key
                            })
                            .ToArray();

                        if (failedOutChanges != null)
                        {
                            failedOutChanges.Clear();
                        }

                        nullChangeFound = nullFound.Value;

                        Dictionary<FileChangeWithDependencies, KeyValuePair<FileChange, FileChangeSource>> OriginalFileChangeMappings = AllFileChanges.ToDictionary(keySelector => keySelector.DependencyFileChange,
                            valueSelector => new KeyValuePair<FileChange, FileChangeSource>(valueSelector.OriginalFileChange, valueSelector.SourceType));

                        HashSet<FileChangeWithDependencies> PulledChanges;
                        CLError assignmentError = AssignDependencies(AllFileChanges.Select(currentFileChange => new KeyValuePair<FileChangeSource, FileChangeWithDependencies>(currentFileChange.SourceType, currentFileChange.DependencyFileChange)).ToArray(),
                            OriginalFileChangeMappings,
                            out PulledChanges,
                            originalQueuedChangesIndexesByInMemoryIdsWrapped);
                        List<PossiblyStreamableFileChange> OutputChangesList = new List<PossiblyStreamableFileChange>();
                        List<PossiblyPreexistingFileChangeInError> OutputFailuresList = new List<PossiblyPreexistingFileChangeInError>();

                        for (int currentChangeIndex = 0; currentChangeIndex < AllFileChanges.Length; currentChangeIndex++)
                        {
                            var CurrentDependencyTree = AllFileChanges[currentChangeIndex];

                            if (!PulledChanges.Contains(CurrentDependencyTree.DependencyFileChange))
                            {
                                Action<List<KeyValuePair<FileChangeMerge, FileChange>>> removeQueuedChangesFromDependencyTree = changesToAdd =>
                                {
                                    IEnumerable<KeyValuePair<int, FileChange>> queuedChangesEnumerable = EnumerateDependencies(CurrentDependencyTree.DependencyFileChange);
                                    foreach (KeyValuePair<int, FileChange> currentQueuedChange in queuedChangesEnumerable)
                                    {
                                        FileChangeWithDependencies castEnumeratedQueuedChange;
                                        KeyValuePair<FileChange, FileChangeSource> mappedOriginalQueuedChange;
                                        if ((castEnumeratedQueuedChange = currentQueuedChange.Value as FileChangeWithDependencies) != null
                                            && OriginalFileChangeMappings.TryGetValue(castEnumeratedQueuedChange,
                                                out mappedOriginalQueuedChange)
                                            && mappedOriginalQueuedChange.Value == FileChangeSource.QueuedChanges)
                                        {
                                            changesToAdd.Add(new KeyValuePair<FileChangeMerge, FileChange>(
                                                new FileChangeMerge(currentQueuedChange.Value, null),
                                                    mappedOriginalQueuedChange.Key));
                                        }
                                    }
                                };

                                if (CurrentDependencyTree.SourceType == FileChangeSource.FailureQueue)
                                {
                                    removeQueuedChangesFromDependencyTree(queuedChangesNeedMergeToSql);

                                    OutputFailuresList.Add(new PossiblyPreexistingFileChangeInError(AllFileChanges[currentChangeIndex].ExistingError, CurrentDependencyTree.DependencyFileChange));
                                }
                                else
                                {
                                    bool nonQueuedChangeFound = false;
                                    bool nonFailedOutChangeFound = false;
                                    
                                    IEnumerable<KeyValuePair<int, FileChange>> nonQueuedChangesEnumerable = EnumerateDependencies(CurrentDependencyTree.DependencyFileChange);
                                    foreach (KeyValuePair<int, FileChange> currentNonQueuedChange in nonQueuedChangesEnumerable)
                                    {
                                        FileChangeWithDependencies castEnumeratedNonQueuedChange;
                                        KeyValuePair<FileChange, FileChangeSource> mappedOriginalNonQueuedChange;
                                        if ((castEnumeratedNonQueuedChange = currentNonQueuedChange.Value as FileChangeWithDependencies) != null
                                            && OriginalFileChangeMappings.TryGetValue(castEnumeratedNonQueuedChange,
                                                out mappedOriginalNonQueuedChange))
                                        {
                                            if (mappedOriginalNonQueuedChange.Value != FileChangeSource.QueuedChanges)
                                            {
                                                nonQueuedChangeFound = true;

                                                if (mappedOriginalNonQueuedChange.Value != FileChangeSource.FailedOutList)
                                                {
                                                    nonFailedOutChangeFound = true;
                                                }

                                                if (nonFailedOutChangeFound)
                                                {
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                nonFailedOutChangeFound = true;

                                                if (nonQueuedChangeFound)
                                                {
                                                    break;
                                                }
                                            }
                                        }
                                    }

                                    if (nonQueuedChangeFound)
                                    {
                                        removeQueuedChangesFromDependencyTree(queuedChangesNeedMergeToSql);

                                        if (nonFailedOutChangeFound)
                                        {
                                            FileStream OutputStream = null;
                                            byte[][] intermediateHashes = null;
                                            byte[] newMD5Bytes = null;

                                            // Note: file size can change during hashing since the file is open with share write 
                                            Nullable<long> finalFileSize = null;

                                            bool CurrentFailed = false;
                                            if (CurrentDependencyTree.DependencyFileChange.Metadata != null
                                                && !CurrentDependencyTree.DependencyFileChange.Metadata.HashableProperties.IsFolder
                                                && (CurrentDependencyTree.DependencyFileChange.Type == FileChangeType.Created
                                                    || CurrentDependencyTree.DependencyFileChange.Type == FileChangeType.Modified)
                                                && CurrentDependencyTree.DependencyFileChange.Direction == SyncDirection.To)
                                            {
                                                try
                                                {
                                                    if (MaxUploadFileSize != -1) // there is a max upload file size threshold
                                                    {
                                                        // Note: file size can change during hashing since the file is open with share write
                                                        long initialFileSize = new FileInfo(CurrentDependencyTree.DependencyFileChange.NewPath.ToString()).Length;

                                                        CurrentDependencyTree.DependencyFileChange.FileIsTooBig = (MaxUploadFileSize < initialFileSize);

                                                        if (CurrentDependencyTree.DependencyFileChange.FileIsTooBig)
                                                        {
                                                            String maxUploadFileSizeText = MaxUploadFileSize > (1024 * 1024) ? String.Format("{0}M", MaxUploadFileSize / (1024 * 1024)) :
                                                                                            MaxUploadFileSize > 1024 ? String.Format("{0}K", MaxUploadFileSize / 1024) :
                                                                                            String.Format("{0} bytes", MaxUploadFileSize);
                                                            throw new AggregateException(String.Format("Files bigger than {0} are not yet supported; File: {1}, Size: {2}",
                                                                                                        MaxUploadFileSize, CurrentDependencyTree.DependencyFileChange.NewPath.ToString(), initialFileSize));
                                                        }
                                                    }

                                                    try
                                                    {
                                                        OutputStream = new FileStream(CurrentDependencyTree.DependencyFileChange.NewPath.ToString(), FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Write | FileShare.Delete);
                                                    }
                                                    catch (FileNotFoundException)
                                                    {
                                                        CurrentDependencyTree.DependencyFileChange.NotFoundForStreamCounter++;
                                                        throw;
                                                    }
                                                    byte[] previousMD5Bytes;
                                                    CLError retrieveMD5Error = CurrentDependencyTree.DependencyFileChange.GetMD5Bytes(out previousMD5Bytes);
                                                    if (retrieveMD5Error != null)
                                                    {
                                                        throw new AggregateException("Error retrieving previousMD5Bytes", retrieveMD5Error.GrabExceptions());
                                                    }

                                                    Model.Md5Hasher hasher = new Model.Md5Hasher(FileConstants.MaxUploadIntermediateHashBytesSize);

                                                    try
                                                    {
                                                        byte[] fileBuffer = new byte[FileConstants.BufferSize];
                                                        int fileReadBytes;
                                                        long countFileSize = 0;

                                                        while ((fileReadBytes = OutputStream.Read(fileBuffer, 0, FileConstants.BufferSize)) > 0)
                                                        {
                                                            countFileSize += fileReadBytes;

                                                            hasher.Update(fileBuffer, fileReadBytes);
                                                        }

                                                        hasher.FinalizeHashes();
                                                        newMD5Bytes = hasher.Hash;
                                                        intermediateHashes = hasher.IntermediateHashes;
                                                        finalFileSize = countFileSize; // file size as counted through a successfull hashing

                                                        string pathString = CurrentDependencyTree.DependencyFileChange.NewPath.ToString();
                                                        FileInfo uploadInfo = new FileInfo(pathString);
                                                        DateTime newCreationTime = uploadInfo.CreationTimeUtc.DropSubSeconds();
                                                        DateTime newWriteTime = uploadInfo.LastWriteTimeUtc.DropSubSeconds();

                                                        if (newCreationTime.CompareTo(CurrentDependencyTree.DependencyFileChange.Metadata.HashableProperties.CreationTime) != 0 // creation time changed
                                                            || newWriteTime.CompareTo(CurrentDependencyTree.DependencyFileChange.Metadata.HashableProperties.LastTime) != 0 // or last write time changed
                                                            || CurrentDependencyTree.DependencyFileChange.Metadata.HashableProperties.Size == null // or previous size was not set
                                                            || ((long)CurrentDependencyTree.DependencyFileChange.Metadata.HashableProperties.Size) == countFileSize // or size changed
                                                            || !Helpers.IsEqualHashes(previousMD5Bytes, newMD5Bytes))
                                                        {
                                                            CLError setMD5Error = CurrentDependencyTree.DependencyFileChange.SetMD5(newMD5Bytes);
                                                            if (setMD5Error != null)
                                                            {
                                                                throw new AggregateException("Error setting DependenyFileChange MD5", setMD5Error.GrabExceptions());
                                                            }

                                                            CurrentDependencyTree.DependencyFileChange.Metadata.HashableProperties = new FileMetadataHashableProperties(false,
                                                                newWriteTime,
                                                                newCreationTime,
                                                                countFileSize);

                                                            CLError writeNewMetadataError = Indexer.MergeEventsIntoDatabase(Helpers.EnumerateSingleItem(new FileChangeMerge(CurrentDependencyTree.DependencyFileChange)));
                                                            if (writeNewMetadataError != null)
                                                            {
                                                                throw new AggregateException("Error writing updated file upload metadata to SQL", writeNewMetadataError.GrabExceptions());
                                                            }
                                                        }
                                                    }
                                                    finally
                                                    {
                                                        try
                                                        {
                                                            if (OutputStream != null)
                                                            {
                                                                OutputStream.Seek(0, SeekOrigin.Begin);
                                                            }
                                                        }
                                                        catch
                                                        {
                                                        }

                                                        hasher.Dispose();
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    CurrentFailed = true;
                                                    toReturn += ex;
                                                }
                                            }

                                            if (CurrentFailed)
                                            {
                                                OutputFailuresList.Add(new PossiblyPreexistingFileChangeInError(false, CurrentDependencyTree.DependencyFileChange));
                                            }
                                            else
                                            {
                                                OutputChangesList.Add(new PossiblyStreamableFileChange(CurrentDependencyTree.DependencyFileChange, UploadStreamContext.Create(OutputStream, intermediateHashes, newMD5Bytes, finalFileSize)));
                                            }
                                        }
                                        else/* if (failedOutChanges != null)*/ // not necessary to check for null failed out changes list since if it was null this else condition could never be reached
                                        {
                                            failedOutChanges.Add(CurrentDependencyTree.DependencyFileChange);
                                        }
                                    }
                                }
                            }
                        }

                        CLError queuedChangesSqlError = Indexer.MergeEventsIntoDatabase(queuedChangesNeedMergeToSql.Select(currentQueuedChangeToSql => currentQueuedChangeToSql.Key));
                        if (queuedChangesSqlError != null)
                        {
                            toReturn += new AggregateException("Error adding QueuedChanges within processing/failed changes dependency tree to SQL", queuedChangesSqlError.GrabExceptions());
                        }
                        foreach (KeyValuePair<FileChangeMerge, FileChange> mergedToSql in queuedChangesNeedMergeToSql)
                        {
                            try
                            {
                                if (mergedToSql.Key.MergeTo.EventId == 0)
                                {
                                    throw new ArgumentException("Cannot communicate FileChange without EventId; FileChangeSource: QueuedChanges");
                                }
                                else
                                {
                                    mergedToSql.Value.Dispose();
                                    if (!RemoveFileChangeFromQueuedChanges(mergedToSql.Value, originalQueuedChangesIndexesByInMemoryIdsWrapped))
                                    {
                                        throw new KeyNotFoundException("Unable to remove FileChange from QueuedChanges after merging to SQL");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                toReturn += ex;
                            }
                        }

                        outputChanges = OutputChangesList;
                        outputChangesCount = OutputChangesList.Count;
                        outputChangesInError = OutputFailuresList;
                        outputChangesInErrorCount = OutputFailuresList.Count;
                    }
                }
            }
            catch (Exception ex)
            {
                outputChanges = Helpers.DefaultForType<IEnumerable<PossiblyStreamableFileChange>>();
                outputChangesCount = Helpers.DefaultForType<int>();
                outputChangesInError = Helpers.DefaultForType<IEnumerable<PossiblyPreexistingFileChangeInError>>();
                outputChangesInErrorCount = Helpers.DefaultForType<int>();
                nullChangeFound = false;
                toReturn += ex;
            }

            if ((_syncbox.CopiedSettings.TraceType & TraceType.FileChangeFlow) == TraceType.FileChangeFlow)
            {
                ComTrace.LogFileChangeFlow(_syncbox.CopiedSettings.TraceLocation, _syncbox.CopiedSettings.DeviceId, _syncbox.SyncboxId, FileChangeFlowEntryPositionInFlow.GrabChangesQueuedChangesAddedToSQL, queuedChangesNeedMergeToSql.Select(currentQueuedChange => ((Func<FileChange, FileChange>)(removeDependencies =>
                    {
                        FileChangeWithDependencies selectedWithoutDependencies;
                        // don't need to set optional parameter (fileDownloadMoveLocker: removeDependencies.fileDownloadMoveLocker) because the returned changes are only used for logging
                        CLError createSelectedError = FileChangeWithDependencies.CreateAndInitialize(removeDependencies, /* initialDependencies */ null, out selectedWithoutDependencies);
                        if (createSelectedError != null)
                        {
                            throw new AggregateException("Creating selectedWithDependencies returned an error", createSelectedError.GrabExceptions());
                        }
                        return selectedWithoutDependencies;
                    }))(currentQueuedChange.Key.MergeTo)));
                ComTrace.LogFileChangeFlow(_syncbox.CopiedSettings.TraceLocation, _syncbox.CopiedSettings.DeviceId, _syncbox.SyncboxId, FileChangeFlowEntryPositionInFlow.GrabChangesOutputChanges, (outputChanges ?? Enumerable.Empty<PossiblyStreamableFileChange>()).Select(currentOutputChange => currentOutputChange.FileChange));
                ComTrace.LogFileChangeFlow(_syncbox.CopiedSettings.TraceLocation, _syncbox.CopiedSettings.DeviceId, _syncbox.SyncboxId, FileChangeFlowEntryPositionInFlow.GrabChangesOutputChangesInError, (outputChangesInError ?? Enumerable.Empty<PossiblyPreexistingFileChangeInError>()).Select(currentOutputChange => currentOutputChange.FileChange));
            }

            return toReturn;
        }
        private enum FileChangeSource : byte
        {
            QueuedChanges,
            FailureQueue,
            ProcessingChanges,
            FailedOutList
        }
        private IEnumerable<KeyValuePair<int, FileChange>> EnumerateDependencies(FileChange toEnumerate, int currentLevelDeep = 0, bool onlyRenamePathsFromTop = false)
        {
            if (currentLevelDeep == 0)
            {
                List<KeyValuePair<int, FileChange>> toReturn = new List<KeyValuePair<int, FileChange>>();

                foreach (KeyValuePair<int, FileChange> currentInnerNode in EnumerateDependencies(toEnumerate, 1, onlyRenamePathsFromTop))
                {
                    toReturn.Add(currentInnerNode);
                }

                return toReturn;
            }
            else if (toEnumerate != null
                && (!onlyRenamePathsFromTop
                    || toEnumerate.Type == FileChangeType.Renamed))
            {
                KeyValuePair<int, FileChange>[] currentEnumerated = new KeyValuePair<int, FileChange>[] { new KeyValuePair<int, FileChange>(currentLevelDeep, toEnumerate) };

                FileChangeWithDependencies castEnumerate = toEnumerate as FileChangeWithDependencies;
                if (castEnumerate != null && castEnumerate.DependenciesCount > 0)
                {
                    return currentEnumerated.Concat(castEnumerate.Dependencies.SelectMany(innerDependency => EnumerateDependencies(innerDependency, currentLevelDeep + 1, onlyRenamePathsFromTop)));
                }
                else
                {
                    return currentEnumerated;
                }
            }
            else
            {
                return Enumerable.Empty<KeyValuePair<int, FileChange>>();
            }
        }

        private IEnumerable<FileChange> EnumerateDependenciesFromFileChangeDeepestLevelsFirst(FileChange toEnumerate, bool onlyRenamePathsFromTop = false)
        {
            if (toEnumerate == null)
            {
                return null;
            }

            List<List<FileChange>> levelsOfDependencies = new List<List<FileChange>>();

            IEnumerable<KeyValuePair<int, FileChange>> dependencyEnumerable = EnumerateDependencies(toEnumerate, onlyRenamePathsFromTop: onlyRenamePathsFromTop);
            foreach (KeyValuePair<int, FileChange> currentDependency in dependencyEnumerable)
            {
                if (levelsOfDependencies.Count < currentDependency.Key)
                {
                    levelsOfDependencies.Add(new List<FileChange>(new FileChange[] { currentDependency.Value }));
                }
                else
                {
                    levelsOfDependencies[currentDependency.Key - 1].Add(currentDependency.Value);
                }
            }

            IEnumerable<FileChange> toReturn = Enumerable.Empty<FileChange>();
            for (int reversedLevelIndex = levelsOfDependencies.Count - 1; reversedLevelIndex >= 0; reversedLevelIndex--)
            {
                toReturn = toReturn.Concat(levelsOfDependencies[reversedLevelIndex]);
            }
            return toReturn;
        }

        /// <summary>
        /// Call this first to start monitoring file system while initial indexing/synchronization occur,
        /// BeginProcessing(initialList) must be called before monitored events begin processing
        /// (call BeginProcessing again after calling Stop() and Start() as well)
        /// </summary>
        /// <param name="status">Returns whether monitor is started or if it had already been started</param>
        /// <returns>Error if it occurred</returns>
        public CLError Start(out MonitorStatus status)
        {
            try
            {
                // lock on current object for changing RunningStatus so it cannot be stopped/started simultaneously
                lock (this)
                {
                    // throw error if monitor has previously been disposed
                    if (Disposed)
                    {
                        // disposed exception
                        throw new Exception("Cannot start monitor after it has been disposed");
                    }

                    // only start if monitor is not already running
                    if (RunningStatus == MonitorRunning.NotRunning)
                    {
                        // lock on current index storage to clear it out
                        lock (AllPaths)
                        {
                            // clear current index storage
                            new ChangeAllPathsClear(this);
                        }

                        // protect root directory from changes such as deletion
                        setDirectoryAccessControl(true);

                        // create watcher for all files and folders that aren't renamed at current path
                        FileWatcher = new FileSystemWatcher(CurrentFolderPath);
                        // increase watcher buffer to maximum
                        FileWatcher.InternalBufferSize = ushort.MaxValue;
                        // include recursive subdirectories
                        FileWatcher.IncludeSubdirectories = true;
                        // set changes to monitor (all but DirectoryName)
                        FileWatcher.NotifyFilter = NotifyFilters.Attributes
                            | NotifyFilters.CreationTime
                            | NotifyFilters.FileName
                            | NotifyFilters.LastAccess
                            | NotifyFilters.LastWrite
                            | NotifyFilters.Security
                            | NotifyFilters.Size;
                        // attach handlers for all watcher events to file-specific handlers
                        FileWatcher.Changed += fileWatcher_Changed;
                        FileWatcher.Created += fileWatcher_Changed;
                        FileWatcher.Deleted += fileWatcher_Changed;
                        FileWatcher.Renamed += fileWatcher_Changed;
                        // start receiving change events
                        FileWatcher.EnableRaisingEvents = true;

                        // create watcher for folders that are renamed at the current path
                        FolderWatcher = new FileSystemWatcher(CurrentFolderPath);
                        // increase watcher buffer to maximum
                        FolderWatcher.InternalBufferSize = ushort.MaxValue;
                        // include recursive subdirectories
                        FolderWatcher.IncludeSubdirectories = true;
                        // set changes to monitor (only DirectoryName)
                        FolderWatcher.NotifyFilter = NotifyFilters.DirectoryName;
                        // attach handlers for all watcher events to folder-specific handlers
                        FolderWatcher.Changed += folderWatcher_Changed;
                        FolderWatcher.Created += folderWatcher_Changed;
                        FolderWatcher.Deleted += folderWatcher_Changed;
                        FolderWatcher.Renamed += folderWatcher_Changed;
                        // start receiving change events
                        FolderWatcher.EnableRaisingEvents = true;

                        // return with 'Started'
                        status = MonitorStatus.Started;
                    }
                    // monitor was already running
                    else
                    {
                        // return with 'AlreadyStarted'
                        status = MonitorStatus.AlreadyStarted;
                    }
                }
            }
            catch (Exception ex)
            {
                status = Helpers.DefaultForType<MonitorStatus>();
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Call this to stop file monitoring and processing
        /// </summary>
        /// <param name="status">Returns whether monitor has stopped or if it had already been stopped</param>
        /// <returns>Error if it occurred</returns>
        public CLError Stop(out MonitorStatus status)
        {
            try
            {
                // lock on current object for changing RunningStatus so it cannot be stopped/started simultaneously
                lock (this)
                {
                    // only stop if monitor is currently running (either the FileWatcher or FolderWatcher)
                    if (RunningStatus != MonitorRunning.NotRunning)
                    {
                        // stop watching files/folders
                        StopWatchers();

                        // return with 'Stopped'
                        status = MonitorStatus.Stopped;
                    }
                    // monitor was not already running
                    else
                    {
                        // return with 'AlreadyStopped'
                        status = MonitorStatus.AlreadyStopped;
                    }
                }
            }
            catch (Exception ex)
            {
                status = Helpers.DefaultForType<MonitorStatus>();
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Call this function when an interprocess receiver gets a call from a
        /// CopyHookHandler COM object that the root folder has moved or been renamed;
        /// no need to stop and start the file monitor
        /// </summary>
        /// <param name="newPath">new location of root folder</param>
        public CLError NotifyRootRename(string newPath)
        {
            try
            {
                // lock on current object for changing RunningStatus so it cannot be stopped/started simultaneously
                lock (this)
                {
                    // enter locker for CurrentFolderPath (rare event, should rarely lock)
                    CurrentFolderPathLocker.EnterWriteLock();
                    try
                    {
                        // alter path
                        CurrentFolderPath = newPath;
                    }
                    finally
                    {
                        // exit locker for CurrentFolderPath
                        CurrentFolderPathLocker.ExitWriteLock();
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
        /// Call this to cleanup FileSystemWatchers such as on application shutdown,
        /// do not start the same monitor instance after it has been disposed 
        /// </summary>
        public CLError Dispose()
        {
            try
            {
                ((IDisposable)this).Dispose();
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }
        #endregion

        #region IDisposable member
        // Standard IDisposable implementation based on MSDN System.IDisposable
        void IDisposable.Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region private methods
        // Standard IDisposable implementation based on MSDN System.IDisposable
        private void Dispose(bool disposing)
        {
            if (!this.Disposed)
            {
                // lock on current object for changing RunningStatus so it cannot be stopped/started simultaneously
                lock (this)
                {
                    // monitor is now set as disposed which will produce errors if startup is called later
                    Disposed = true;
                }

                // Run dispose on inner managed objects based on disposing condition
                if (disposing)
                {
                    // cleanup FileSystemWatchers
                    StopWatchers();

                    InitialIndexLocker.Dispose();
                }

                lock (QueuesTimer.TimerRunningLocker)
                {
                    QueuesTimer.TriggerTimerCompletionImmediately();
                }
                
                // Dispose local unmanaged resources last
            }
        }

        /// <summary>
        /// Todo: notify system that root monitored folder is protected from changes like deletion;
        /// Will forward call to interprocess receiver (Todo) which talks to a
        /// CopyHookHandler COM object that captures root events (Todo)
        /// </summary>
        /// <param name="newLockState"></param>
        private void setDirectoryAccessControl(bool newLockState)
        {
            //TODO: Need to implement:
            //http://msdn.microsoft.com/en-us/library/cc144063(VS.85).aspx
            // Copy Hook Handler will interface similar to BadgeCOM to BadgeNET;
            // it can store the watched path after it is retrieved so it can ignore any
            // changes made to other paths; if we allow moving/renaming the Cloud folder
            // (possible to offer modal dialog confirmation) it can notify Cloud to update
            // folder location; if we allow deleting the Cloud folder it can notify Cloud
            // to stop running until a new location is selected
        }

        /// <summary>
        /// folder-specific EventHandler for file system watcher,
        /// call is forwarded to watcher_Changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void folderWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            // forward event with folder-specificity as boolean
            watcher_Changed(sender, e, true);
        }

        /// <summary>
        /// file-specific EventHandler for file system watcher,
        /// call is forwarded to watcher_Changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            // forward event with file-specificity as boolean
            watcher_Changed(sender, e, false);
        }

        private readonly Queue<KeyValuePair<FileSystemEventArgs, bool>> watcher_ChangedQueue = new Queue<KeyValuePair<FileSystemEventArgs,bool>>();
        private bool watcher_ChangedQueueProcessing = false;

        /// <summary>
        /// Combined EventHandler for file and folder changes
        /// </summary>
        /// <param name="sender">FileSystemWatcher</param>
        /// <param name="e">Event arguments for the change</param>
        /// <param name="folderOnly">Value of folder-specificity from routed event</param>
        private void watcher_Changed(object sender, FileSystemEventArgs e, bool folderOnly)
        {
            lock (watcher_ChangedQueue)
            {
                if (watcher_ChangedQueueProcessing)
                {
                    watcher_ChangedQueue.Enqueue(new KeyValuePair<FileSystemEventArgs, bool>(e, folderOnly));
                }
                else
                {
                    watcher_ChangedQueueProcessing = true;
                    ThreadPool.UnsafeQueueUserWorkItem(ProcessWatcher_ChangedQueue, new KeyValuePair<MonitorAgent, KeyValuePair<FileSystemEventArgs, bool>>(this,
                        new KeyValuePair<FileSystemEventArgs, bool>(e, folderOnly)));
                }
            }
        }

        /// <summary>
        /// Refactored processing logic for watcher_Changed eventhandler so it can process on a seperate reader thread (thus clearing out the FileSystemWatcher buffer quicker)
        /// </summary>
        /// <param name="sender">Contains the MonitorAgent, the FileSystemEventArgs of the initial change, and a bool for whether the initial change was from FolderWatcher (as opposed to FileWatcher)</param>
        private static void ProcessWatcher_ChangedQueue(object sender)
        {
            Nullable<KeyValuePair<MonitorAgent, KeyValuePair<FileSystemEventArgs, bool>>> castSender = sender as Nullable<KeyValuePair<MonitorAgent, KeyValuePair<FileSystemEventArgs, bool>>>;
            if (castSender == null)
            {
                MessageEvents.FireNewEventMessage(
                    "Error starting Cloud, ProcessWatcher_ChangedQueue sender is not of type KeyValuePair<MonitorAgent, KeyValuePair<FileSystemEventArgs, bool>>",
                    EventMessageLevel.Important,
                    new HaltAllOfCloudSDKErrorInfo());
            }
            else
            {
                MonitorAgent currentAgent = ((KeyValuePair<MonitorAgent, KeyValuePair<FileSystemEventArgs, bool>>)castSender).Key;

                KeyValuePair<FileSystemEventArgs, bool> currentToProcess = ((KeyValuePair<MonitorAgent, KeyValuePair<FileSystemEventArgs, bool>>)castSender).Value;

                Func<bool> continueProcessing = () =>
                    {
                        lock (currentAgent)
                        {
                            if (currentAgent.Disposed)
                            {
                                return false;
                            }
                        }

                        lock (currentAgent.watcher_ChangedQueue)
                        {
                            if (currentAgent.watcher_ChangedQueue.Count == 0)
                            {
                                return currentAgent.watcher_ChangedQueueProcessing = false;
                            }
                            else
                            {
                                currentToProcess = currentAgent.watcher_ChangedQueue.Dequeue();
                                return true;
                            }
                        }
                    };

                do
                {
                    // Enter read lock of CurrentFolderPath (doesn't lock other threads unless lock is entered for write on rare condition of path changing)
                    currentAgent.CurrentFolderPathLocker.EnterReadLock();
                    try
                    {
                        // rebuild filePath from current root path and the relative path portion of the change event
                        string newPath = currentAgent.CurrentFolderPath + currentToProcess.Key.FullPath.Substring(currentAgent.InitialFolderPath.Length);
                        // previous path for renames only
                        string oldPath;
                        // set previous path only if change is a rename
                        if ((currentToProcess.Key.ChangeType & WatcherChangeTypes.Renamed) == WatcherChangeTypes.Renamed)
                        {
                            // cast args to the appropriate type containing previous path
                            RenamedEventArgs renamedArgs = (RenamedEventArgs)currentToProcess.Key;
                            // rebuild oldPath from current root path and the relative path portion of the change event;
                            // should not be a problem pulling the relative path out of the renamed args 'OldFullPath' when the
                            // file was moved from a directory outside the monitored root because move events don't come across as 'Renamed'
                            oldPath = currentAgent.CurrentFolderPath + renamedArgs.OldFullPath.Substring(currentAgent.InitialFolderPath.Length);
                        }
                        else
                        {
                            // no old path for Created/Deleted/Modified events
                            oldPath = null;
                        }
                        
                        if (currentAgent.debugMemory)
                        {
                            memoryDebugger.Instance.WatcherChanged(oldPath, newPath, currentToProcess.Key.ChangeType, folderOnly: currentToProcess.Value);
                        }

                        // Processes the file system event against the file data and current file index
                        currentAgent.CheckMetadataAgainstFile(newPath, oldPath, currentToProcess.Key.ChangeType, currentToProcess.Value);
                    }
                    catch
                    {
                    }
                    finally
                    {
                        // Exit read lock of CurrentFolderPath
                        currentAgent.CurrentFolderPathLocker.ExitReadLock();
                    }
                } while (continueProcessing());
            }
        }

        /// <summary>
        /// Resolve changes to queue from file system events against the file data and current file index
        /// </summary>
        /// <param name="newPath">Path where the change was observed</param>
        /// <param name="oldPath">Previous path if change was a rename</param>
        /// <param name="changeType">Type of file system event</param>
        /// <param name="folderOnly">Specificity from routing of file system event</param>
        /// <param name="alreadyHoldingIndexLock">Optional param only to be set (as true) from BeginProcessing method</param>
        private void CheckMetadataAgainstFile(string newPath, string oldPath, WatcherChangeTypes changeType, bool folderOnly, bool alreadyHoldingIndexLock = false, GenericHolder<Nullable<KeyValuePair<Action<DisposeCheckingHolder>, DisposeCheckingHolder>>> startProcessingAction = null)
        {
            // File system events come through here to resolve the combination of current change, existing metadata, and actual file information on disk;
            // When the file monitoring is first started, it waits for the completion of an initial indexing (which will process differences as new file events);
            // During the initial indexing, file system events are added to a queue to be processed after indexing completes;
            // Except for initial indexing, file system events are processed normally

            // Enter the index lock unless this method is being called from BeginProcessing (which is already holding the write lock)
            if (!alreadyHoldingIndexLock)
            {
                InitialIndexLocker.EnterReadLock();
            }

            try
            {
                // If the initial indexing is running, enqueue changes to process later, otherwise process normally

                if (IsInitialIndex)
                {
                    ChangesQueueForInitialIndexing.Enqueue(new ChangesQueueHolder()
                    {
                        newPath = newPath,
                        oldPath = oldPath,
                        changeType = changeType,
                        folderOnly = folderOnly
                    });
                }
                else
                {
                    // most paths modify the list of current indexes, so lock it from other reads/changes
                    lock (AllPaths)
                    {
                        // object for gathering folder info at current path
                        FilePath pathObject;
                        DirectoryInfo folder;
                        pathObject = folder = new DirectoryInfo(newPath);

                        // object for gathering file info (set conditionally later in case we know change is not a file)
                        FileInfo file = null;
                        // field used to determine if change is a file or folder
                        bool isFolder;
                        // field used to determine if file/folder exists
                        bool exists;
                        // field for file length
                        Nullable<long> fileLength = null;

                        // if file system event was folder-specific, store that change is folder and determine if folder exists
                        if (folderOnly)
                        {
                            // store that change is a folder
                            isFolder = true;
                            // check and store if folder exists
                            exists = folder.Exists;
                        }
                        // if file system event was not folder-specific, but a folder exists at the specified path anyways
                        // then store that change is a folder and that the folder exists
                        else if (folder.Exists)
                        {
                            // store that change is a folder
                            isFolder = true;
                            // store that folder exists
                            exists = true;
                        }
                        // if change was not folder-specific and a folder didn't exist at the specified path,
                        // set object for gathering file info and use that object to determine if file exists
                        else
                        {
                            // since we did not find a folder, assume change is on a file
                            isFolder = false;
                            // set object for gathering file info at current path
                            file = new FileInfo(newPath);
                            // check and store if file exists
                            exists = file.Exists;
                            // ran into a condition where the file was moved between checking if it existed and finding its length,
                            // fixed by storing the length inside a try/catch and handling the not found exception by flipping exists
                            if (exists)
                            {
                                try
                                {
                                    fileLength = file.Length;
                                }
                                catch (FileNotFoundException)
                                {
                                    exists = false;
                                }
                                catch
                                {
                                }
                            }
                        }

                        // folder move handling: both delete then create or create then delete, we only check on the first event that comes in, the next one should short the more complex processing
                        // move is accomplished by changing the input param references so we now have a renamed change with their required oldpath\newpath combination
                        if (isFolder)
                        {
                            switch (changeType)
                            {
                                case WatcherChangeTypes.Created:
                                    if (exists
                                        && !AllPaths.ContainsKey(pathObject))
                                    {
                                        DateTime addFolderCreationTime = folder.CreationTimeUtc;
                                        long addFolderCreationTimeUtcTicks = addFolderCreationTime.Ticks;
                                        FilePath[] addFolderAlreadyExistingCreationTimePaths;
                                        if (FolderCreationTimeUtcTicksToPath.TryGetValue(addFolderCreationTimeUtcTicks, out addFolderAlreadyExistingCreationTimePaths))
                                        {
                                            int matchedDelete = -1;
                                            for (int movedFolderIdx = 0; movedFolderIdx < addFolderAlreadyExistingCreationTimePaths.Length; movedFolderIdx++)
                                            {
                                                FilePath currentDeletedPath = addFolderAlreadyExistingCreationTimePaths[movedFolderIdx];
                                                if (!FilePathComparer.Instance.Equals(currentDeletedPath, pathObject)
                                                    && AllPaths.ContainsKey(currentDeletedPath)
                                                    && !Directory.Exists(currentDeletedPath.ToString()))
                                                {
                                                    matchedDelete = movedFolderIdx;
                                                    if (currentDeletedPath.Name == pathObject.Name)
                                                    {
                                                        break; // best match found, break out now to use this matched path for the folder move
                                                    }
                                                }
                                            }

                                            if (matchedDelete != -1)
                                            {
                                                oldPath = addFolderAlreadyExistingCreationTimePaths[matchedDelete].ToString();
                                                changeType = WatcherChangeTypes.Renamed;
                                            }
                                        }
                                    }
                                    break;

                                case WatcherChangeTypes.Deleted:
                                    FileMetadata deletedMetadata;

                                    if (!exists
                                        && AllPaths.TryGetValue(pathObject, out deletedMetadata))
                                    {
                                        bool rootError;
                                        // horribly inefficient (does a full index of every folder on disk)...but I found no way to do a WMI query on winmgmts:\\.\root\cimv2\Win32_Directory for all recursive folders within the sync root with a matching CreationDate
                                        IList<SQLIndexer.Model.FindFileResult> outermostSearch = SQLIndexer.Model.FindFileResult.RecursiveDirectorySearch(
                                                GetCurrentPath(), // start search in sync root
                                                (FileAttributes.Hidden // ignore hidden files
                                                    | FileAttributes.Offline // ignore offline files (data is not available on them)
                                                    | FileAttributes.System // ignore system files
                                                    | FileAttributes.Temporary), // ignore temporary files
                                                out rootError,
                                                returnFoldersOnly: true);

                                        GenericHolder<SQLIndexer.Model.FindFileResult> firstFoundMatchingTime = new GenericHolder<SQLIndexer.Model.FindFileResult>();

                                        Action<SQLIndexer.Model.FindFileResult, DateTime, FilePathDictionary<FileMetadata>, GenericHolder<SQLIndexer.Model.FindFileResult>, object> checkThroughFolderResults =
                                            (currentNodeToCheck, creationTimeOfDeletedMetadata, innerAllPaths, innerFoundMatchingTime, thisAction) =>
                                            {
                                                Action<SQLIndexer.Model.FindFileResult, DateTime, FilePathDictionary<FileMetadata>, GenericHolder<SQLIndexer.Model.FindFileResult>, object> castAction =
                                                    thisAction as Action<SQLIndexer.Model.FindFileResult, DateTime, FilePathDictionary<FileMetadata>, GenericHolder<SQLIndexer.Model.FindFileResult>, object>;

                                                if (castAction == null)
                                                {
                                                    MessageEvents.FireNewEventMessage(
                                                        "Unable to cast thisAction as Action<SQLIndexer.Model.FindFileResult, DateTime, FilePathDictionary<FileMetadata>, GenericHolder<SQLIndexer.Model.FindFileResult>, object>",
                                                        EventMessageLevel.Important,
                                                        new HaltAllOfCloudSDKErrorInfo());
                                                }
                                                else
                                                {
                                                    if (currentNodeToCheck.CreationTime != null
                                                        && DateTime.Compare(creationTimeOfDeletedMetadata, ((DateTime)currentNodeToCheck.CreationTime).ToUniversalTime()) == 0
                                                        && !innerAllPaths.ContainsKey(currentNodeToCheck.FullName))
                                                    {
                                                        innerFoundMatchingTime.Value = currentNodeToCheck;
                                                    }
                                                    else if (currentNodeToCheck.Children != null)
                                                    {
                                                        for (int currentInnerResultIdx = 0; currentInnerResultIdx < currentNodeToCheck.Children.Count; currentInnerResultIdx++)
                                                        {
                                                            castAction(currentNodeToCheck.Children[currentInnerResultIdx], creationTimeOfDeletedMetadata, innerAllPaths, innerFoundMatchingTime, thisAction);

                                                            if (innerFoundMatchingTime.Value != null)
                                                            {
                                                                break;
                                                            }
                                                        }
                                                    }
                                                }
                                            };

                                        if (outermostSearch != null)
                                        {
                                            for (int folderInRootIdx = 0; folderInRootIdx < outermostSearch.Count; folderInRootIdx++)
                                            {
                                                checkThroughFolderResults(outermostSearch[folderInRootIdx], deletedMetadata.HashableProperties.CreationTime, AllPaths, firstFoundMatchingTime, checkThroughFolderResults);

                                                if (firstFoundMatchingTime.Value != null)
                                                {
                                                    break;
                                                }
                                            }
                                        }

                                        if (firstFoundMatchingTime.Value != null)
                                        {
                                            oldPath = newPath;
                                            newPath = firstFoundMatchingTime.Value.FullName;
                                            pathObject = folder = new DirectoryInfo(newPath);
                                            changeType = WatcherChangeTypes.Renamed;
                                            exists = true; // exists was previously calculated as the 'deletion' of 'new path' which is now the old path, so exists should now be true since we found a folder at the 'renamed' 'new path'
                                        }
                                    }
                                    break;
                            }
                        }

                        CheckMetadataEntry debugEntry;
                        if (debugMemory)
                        {
                            debugEntry = new CheckMetadataEntry()
                            {
                                IsFolder = isFolder,
                                NewExists = exists,
                                OldPath = oldPath,
                                NewPath = newPath,
                                OldChangeType =
                                    (changeType == WatcherChangeTypes.Changed
                                        ? new WatcherChangeChanged()
                                        : (changeType == WatcherChangeTypes.Created
                                            ? new WatcherChangeCreated()
                                            : (changeType == WatcherChangeTypes.Deleted
                                                ? new WatcherChangeDeleted()
                                                : (changeType == WatcherChangeTypes.Renamed
                                                    ? new WatcherChangeRenamed()
                                                    : new WatcherChangeType())))),
                                Size = fileLength ?? 0,
                                SizeSpecified = fileLength != null
                            };
                        }
                        else
                        {
                            debugEntry = null;
                        }

                        try
                        {
                            // Only process file/folder event if it does not exist or if its FileAttributes does not contain any unwanted attributes
                            // Also ensure if it is a file that the file is not a shortcut
                            if (!exists// file/folder does not exist so no need to check attributes
                                || ((FileAttributes)0 == // compare bitwise and of FileAttributes and all unwanted attributes to '0'
                                    ((isFolder // need to grab FileAttributes based on whether change is on a file or folder
                                    ? folder.Attributes // change is on folder, grab folder attributes
                                    : file.Attributes) // change is on file, grab file attributes
                                        & (FileAttributes.Hidden // ignore hidden files
                                            | FileAttributes.Offline // ignore offline files (data is not available on them)
                                            | FileAttributes.System // ignore system files
                                            | FileAttributes.Temporary)) // ignore temporary files
                                    //RKSCHANGE:&& (isFolder ? true : !FileIsShortcut(file)))) // allow change if it is a folder or if it is a file that is not a shortcut
                                    ))
                            {
                                DateTime lastTime;
                                DateTime creationTime;
                                if (exists)
                                {
                                    _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: CheckMetadataAgainstFile: File or folder exists: {0}.", newPath));
                                    // set last time and creation time from appropriate info based on whether change is on a folder or file
                                    if (isFolder)
                                    {
                                        lastTime = folder.LastWriteTimeUtc;
                                        creationTime = folder.CreationTimeUtc;
                                    }
                                    // change was not a folder, grab times based on file
                                    else
                                    {
                                        _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: CheckMetadataAgainstFile: File exists."));
                                        lastTime = file.LastWriteTimeUtc.DropSubSeconds();
                                        creationTime = file.CreationTimeUtc.DropSubSeconds();
                                    }
                                }
                                else
                                {
                                    _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: CheckMetadataAgainstFile: File or folder does not exist."));
                                    creationTime = lastTime = new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc);
                                }

                                if (debugMemory)
                                {
                                    debugEntry.LastTime = lastTime;
                                    debugEntry.LastTimeSpecified = lastTime.Ticks != FileConstants.InvalidUtcTimeTicks;
                                    debugEntry.CreationTime = creationTime;
                                    debugEntry.CreationTimeSpecified = creationTime.Ticks != FileConstants.InvalidUtcTimeTicks;
                                }

                                #region file system event, current file status, and current recorded index state flow

                                // for file system events marked as file/folder changes or additions
                                if ((changeType & WatcherChangeTypes.Changed) == WatcherChangeTypes.Changed
                                    || (changeType & WatcherChangeTypes.Created) == WatcherChangeTypes.Created)
                                {
                                    _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: CheckMetadataAgainstFile: Changed or Created event."));
                                    // if file/folder actually exists
                                    if (exists)
                                    {
                                        // if index exists at specified path
                                        if (AllPaths.ContainsKey(pathObject))
                                        {
                                            _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: CheckMetadataAgainstFile: AllPaths contains this item: {0}.", pathObject.Name));
                                            if (debugMemory)
                                            {
                                                debugEntry.NewIndexed = true;
                                                debugEntry.NewIndexedSpecified = true;
                                            }

                                            // No need to send modified events for folders
                                            // so check if event is on a file or if folder modifies are not ignored
                                            if (!isFolder
                                                || !IgnoreFolderModifies)
                                            {
                                                _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: CheckMetadataAgainstFile: Not folder, and not ignoring folder modifies. Call ReplacementMetadataIfDifferent."));
                                                // retrieve stored index
                                                FileMetadata previousMetadata = AllPaths[pathObject];
                                                // compare stored index with values from file info
                                                FileMetadata newMetadata = ReplacementMetadataIfDifferent(previousMetadata,
                                                    isFolder,
                                                    lastTime,
                                                    creationTime,
                                                    fileLength);
                                                // if new metadata came back after comparison, queue file change for modify
                                                if (newMetadata != null)
                                                {
                                                    _trace.writeToMemory(() => _trace.trcFmtStr(2, "MonitorAgent: CheckMetadataAgainstFile: Metadata different."));
                                                    if (debugMemory)
                                                    {
                                                        debugEntry.NewChangeType = new WatcherChangeChanged();
                                                    }

                                                    // replace index at current path
                                                    new ChangeAllPathsIndexSet(this, pathObject, newMetadata);
                                                    // queue file change for modify
                                                    QueueFileChange(new FileChange(QueuedChanges)
                                                    {
                                                        NewPath = pathObject,
                                                        Metadata = newMetadata,
                                                        Type = FileChangeType.Modified,
                                                        Direction = SyncDirection.To // detected that a file or folder was modified locally, so Sync To to update server
                                                    }, startProcessingAction);
                                                }
                                            }
                                        }
                                        // if index did not already exist
                                        else
                                        {
                                            if (debugMemory)
                                            {
                                                debugEntry.NewIndexedSpecified = true;
                                                debugEntry.NewChangeType = new WatcherChangeCreated();
                                            }

                                            FileMetadata addedMetadata =
                                                new FileMetadata()
                                                {
                                                    HashableProperties = new FileMetadataHashableProperties(isFolder,
                                                        lastTime,
                                                        creationTime,
                                                        fileLength)
                                                };
                                            // add new index
                                            new ChangeAllPathsAdd(this, pathObject, addedMetadata);
                                            // queue file change for create
                                            QueueFileChange(new FileChange(QueuedChanges)
                                            {
                                                NewPath = pathObject,
                                                Metadata = addedMetadata,
                                                Type = FileChangeType.Created,
                                                Direction = SyncDirection.To // detected that a file or folder was created locally, so Sync To to update server
                                            }, startProcessingAction);
                                        }
                                    }
                                    // if file file does not exist, but an index exists
                                    else if (AllPaths.ContainsKey(pathObject))
                                    {
                                        if (debugMemory)
                                        {
                                            debugEntry.NewIndexed = true;
                                            debugEntry.NewIndexedSpecified = true;
                                            debugEntry.NewChangeType = new WatcherChangeDeleted();
                                        }

                                        // queue file change for delete
                                        QueueFileChange(new FileChange(QueuedChanges)
                                        {
                                            NewPath = pathObject,
                                            Metadata = AllPaths[pathObject],
                                            Type = FileChangeType.Deleted,
                                            Direction = SyncDirection.To // detected that a file or folder was deleted locally, so Sync To to update server
                                        }, startProcessingAction);
                                        // remove index
                                        new ChangeAllPathsRemove(this, pathObject);
                                    }
                                    else if (debugMemory)
                                    {
                                        debugEntry.NewIndexedSpecified = true;
                                    }
                                }
                                // for file system events marked as rename
                                else if ((changeType & WatcherChangeTypes.Renamed) == WatcherChangeTypes.Renamed)
                                {
                                    FilePath oldPathObject = oldPath;

                                    // store a boolean which may be set to true to notify a condition when a rename operation may need to be queued
                                    bool possibleRename = false;
                                    // if index exists at the previous path
                                    if (AllPaths.ContainsKey(oldPathObject))
                                    {
                                        // if a file or folder exists at the previous path
                                        if (File.Exists(oldPath)
                                            || Directory.Exists(oldPath))
                                        {
                                            if (debugMemory)
                                            {
                                                debugEntry.OldIndexed = true;
                                                debugEntry.OldIndexedSpecified = true;
                                                debugEntry.PossibleRenameSpecified = true;
                                            }

                                            // recurse once on this current function to process the previous path as a file system modified event
                                            CheckMetadataAgainstFile(oldPath, null, WatcherChangeTypes.Changed, false);
                                        }
                                        // if no file nor folder exists at the previous path and a file or folder does exist at the current path
                                        else if (exists)
                                        {
                                            if (debugMemory)
                                            {
                                                debugEntry.OldIndexed = true;
                                                debugEntry.OldIndexedSpecified = true;
                                                debugEntry.PossibleRename = true;
                                                debugEntry.PossibleRenameSpecified = true;
                                            }

                                            // set precursor condition for queueing a file change for rename
                                            possibleRename = true;
                                        }
                                        // if no file nor folder exists at either the previous or current path
                                        else
                                        {
                                            if (debugMemory)
                                            {
                                                debugEntry.OldIndexed = true;
                                                debugEntry.OldIndexedSpecified = true;
                                                debugEntry.NewChangeType = new WatcherChangeDeleted();
                                                debugEntry.PossibleRenameSpecified = true;
                                            }

                                            // queue file change for delete at previous path
                                            QueueFileChange(new FileChange(QueuedChanges)
                                            {
                                                NewPath = oldPathObject,
                                                Metadata = AllPaths[oldPathObject],
                                                Type = FileChangeType.Deleted,
                                                Direction = SyncDirection.To // detected that a file or folder was deleted locally, so Sync To to update server
                                            }, startProcessingAction);
                                            // remove index at previous path
                                            new ChangeAllPathsRemove(this, oldPathObject);
                                        }
                                    }
                                    else if (debugMemory)
                                    {
                                        debugEntry.OldIndexedSpecified = true;
                                        debugEntry.PossibleRenameSpecified = true;
                                    }

                                    // if index exists at current path (irrespective of last condition on previous path index)
                                    if (AllPaths.ContainsKey(pathObject))
                                    {
                                        // if file or folder exists at the current path
                                        if (exists)
                                        {
                                            // No need to send modified events for folders
                                            // so check if event is on a file or if folder modifies are not ignored
                                            if (!isFolder
                                                || !IgnoreFolderModifies)
                                            {
                                                // retrieve stored index at current path
                                                FileMetadata previousMetadata = AllPaths[pathObject];
                                                // compare stored index with values from file info
                                                FileMetadata newMetadata = ReplacementMetadataIfDifferent(previousMetadata,
                                                    isFolder,
                                                    lastTime,
                                                    creationTime,
                                                    fileLength);
                                                // if new metadata came back after comparison, queue file change for modify
                                                if (newMetadata != null)
                                                {
                                                    if (debugMemory)
                                                    {
                                                        debugEntry.NewIndexed = true;
                                                        debugEntry.NewIndexedSpecified = true;
                                                        if (debugEntry.NewChangeType != null)
                                                        {
                                                            memoryDebugger.Instance.AddCheckMetadata(debugEntry);
                                                            debugEntry = new CheckMetadataEntry()
                                                            {
                                                                CreationTime = debugEntry.CreationTime,
                                                                CreationTimeSpecified = debugEntry.CreationTimeSpecified,
                                                                IsFolder = debugEntry.IsFolder,
                                                                LastTime = debugEntry.LastTime,
                                                                LastTimeSpecified = debugEntry.LastTimeSpecified,
                                                                NewExists = debugEntry.NewExists,
                                                                NewIndexed = debugEntry.NewIndexed,
                                                                NewIndexedSpecified = debugEntry.NewIndexedSpecified,
                                                                NewPath = debugEntry.NewPath,
                                                                OldChangeType = debugEntry.OldChangeType,
                                                                OldExists = debugEntry.OldExists,
                                                                OldExistsSpecified = debugEntry.OldExistsSpecified,
                                                                OldIndexed = debugEntry.OldIndexed,
                                                                OldIndexedSpecified = debugEntry.OldIndexedSpecified,
                                                                OldPath = debugEntry.OldPath,
                                                                PossibleRename = debugEntry.PossibleRename,
                                                                PossibleRenameSpecified = debugEntry.PossibleRenameSpecified,
                                                                Size = debugEntry.Size,
                                                                SizeSpecified = debugEntry.SizeSpecified
                                                            };
                                                        }
                                                        debugEntry.NewChangeType = new WatcherChangeChanged();
                                                    }

                                                    // replace index at current path
                                                    new ChangeAllPathsIndexSet(this, pathObject, newMetadata);
                                                    // queue file change for modify
                                                    QueueFileChange(new FileChange(QueuedChanges)
                                                    {
                                                        NewPath = pathObject,
                                                        Metadata = newMetadata,
                                                        Type = FileChangeType.Modified,
                                                        Direction = SyncDirection.To // detected that a file or folder was modified locally, so Sync To to update server
                                                    }, startProcessingAction);
                                                }
                                                else if (debugMemory)
                                                {
                                                    debugEntry.NewIndexed = true;
                                                    debugEntry.NewIndexedSpecified = true;
                                                }
                                            }
                                        }
                                        // else file does not exist
                                        else
                                        {
                                            if (debugMemory)
                                            {
                                                debugEntry.NewIndexed = true;
                                                debugEntry.NewIndexedSpecified = true;
                                                if (debugEntry.NewChangeType != null)
                                                {
                                                    memoryDebugger.Instance.AddCheckMetadata(debugEntry);
                                                    debugEntry = new CheckMetadataEntry()
                                                    {
                                                        CreationTime = debugEntry.CreationTime,
                                                        CreationTimeSpecified = debugEntry.CreationTimeSpecified,
                                                        IsFolder = debugEntry.IsFolder,
                                                        LastTime = debugEntry.LastTime,
                                                        LastTimeSpecified = debugEntry.LastTimeSpecified,
                                                        NewExists = debugEntry.NewExists,
                                                        NewIndexed = debugEntry.NewIndexed,
                                                        NewIndexedSpecified = debugEntry.NewIndexedSpecified,
                                                        NewPath = debugEntry.NewPath,
                                                        OldChangeType = debugEntry.OldChangeType,
                                                        OldExists = debugEntry.OldExists,
                                                        OldExistsSpecified = debugEntry.OldExistsSpecified,
                                                        OldIndexed = debugEntry.OldIndexed,
                                                        OldIndexedSpecified = debugEntry.OldIndexedSpecified,
                                                        OldPath = debugEntry.OldPath,
                                                        PossibleRename = debugEntry.PossibleRename,
                                                        PossibleRenameSpecified = debugEntry.PossibleRenameSpecified,
                                                        Size = debugEntry.Size,
                                                        SizeSpecified = debugEntry.SizeSpecified
                                                    };
                                                }
                                                debugEntry.NewChangeType = new WatcherChangeDeleted();
                                            }

                                            // queue file change for delete at new path
                                            QueueFileChange(new FileChange(QueuedChanges)
                                            {
                                                NewPath = pathObject,
                                                Metadata = AllPaths[pathObject],
                                                Type = FileChangeType.Deleted,
                                                Direction = SyncDirection.To // detected that a file or folder was deleted locally, so Sync To to update server
                                            }, startProcessingAction);
                                            // remove index for new path
                                            new ChangeAllPathsRemove(this, pathObject);

                                            // no need to continue and check possibeRename since it required exists to be true, return now
                                            return;
                                        }
                                        // if precursor condition was set for a file change for rename
                                        // (but an index already exists at the new path)
                                        if (possibleRename)
                                        {
                                            if (debugMemory)
                                            {
                                                debugEntry.NewIndexed = true;
                                                debugEntry.NewIndexedSpecified = true;
                                            }

                                            // queue file change for delete at previous path
                                            QueueFileChange(new FileChange(QueuedChanges)
                                            {
                                                NewPath = oldPathObject,
                                                Metadata = AllPaths[oldPathObject],
                                                Type = FileChangeType.Deleted,
                                                Direction = SyncDirection.To // detected that a file or folder was deleted locally, so Sync To to update server
                                            }, startProcessingAction);
                                            // remove index at the previous path
                                            new ChangeAllPathsRemove(this, oldPathObject);
                                        }
                                    }
                                    // if precursor condition was set for a file change for rename
                                    // and an index does not exist at the new path
                                    else if (possibleRename)
                                    {
                                        if (debugMemory)
                                        {
                                            debugEntry.NewIndexedSpecified = true;
                                            // the following condition should never be met since possibleRename is set 'instead' of queueing a change
                                            if (debugEntry.NewChangeType != null)
                                            {
                                                memoryDebugger.Instance.AddCheckMetadata(debugEntry);
                                                debugEntry = new CheckMetadataEntry()
                                                {
                                                    CreationTime = debugEntry.CreationTime,
                                                    CreationTimeSpecified = debugEntry.CreationTimeSpecified,
                                                    IsFolder = debugEntry.IsFolder,
                                                    LastTime = debugEntry.LastTime,
                                                    LastTimeSpecified = debugEntry.LastTimeSpecified,
                                                    NewExists = debugEntry.NewExists,
                                                    NewIndexed = debugEntry.NewIndexed,
                                                    NewIndexedSpecified = debugEntry.NewIndexedSpecified,
                                                    NewPath = debugEntry.NewPath,
                                                    OldChangeType = debugEntry.OldChangeType,
                                                    OldExists = debugEntry.OldExists,
                                                    OldExistsSpecified = debugEntry.OldExistsSpecified,
                                                    OldIndexed = debugEntry.OldIndexed,
                                                    OldIndexedSpecified = debugEntry.OldIndexedSpecified,
                                                    OldPath = debugEntry.OldPath,
                                                    PossibleRename = debugEntry.PossibleRename,
                                                    PossibleRenameSpecified = debugEntry.PossibleRenameSpecified,
                                                    Size = debugEntry.Size,
                                                    SizeSpecified = debugEntry.SizeSpecified
                                                };
                                            }
                                            debugEntry.NewChangeType = new WatcherChangeRenamed();
                                        }

                                        // retrieve index at previous path
                                        FileMetadata previousMetadata = AllPaths[oldPathObject];
                                        // compare stored index from previous path with values from current change
                                        FileMetadata newMetadata = ReplacementMetadataIfDifferent(previousMetadata,
                                            isFolder,
                                            lastTime,
                                            creationTime,
                                            fileLength);

                                        //// wouldn't remove and adding cause data to be lost which should have been moved over?
                                        //
                                        //// remove index at the previous path
                                        //AllPaths.Remove(oldPath);
                                        //// add an index for the current path either from the changed metadata if it exists otherwise the previous metadata
                                        //AllPaths.Add(pathObject, newMetadata ?? previousMetadata);
                                        //
                                        //// switched to the following instead:
                                        FilePathHierarchicalNode<FileMetadata> oldPathHierarchy;
                                        CLError existingHierarchyError = AllPaths.GrabHierarchyForPath(oldPathObject, out oldPathHierarchy, suppressException: true);
                                        if (existingHierarchyError != null)
                                        {
                                            throw new AggregateException("Error grabbing hierarchy for oldPath from AllPaths", existingHierarchyError.GrabExceptions());
                                        }
                                        if (oldPathHierarchy != null)
                                        {
                                            MoveOldPathsToNewPaths(oldPathHierarchy, oldPathObject, pathObject);
                                        }
                                        new ChangeAllPathsIndexSet(this, pathObject, newMetadata ?? previousMetadata);

                                        // queue file change for rename (use changed metadata if it exists otherwise the previous metadata)
                                        QueueFileChange(new FileChange(QueuedChanges)
                                        {
                                            NewPath = pathObject,
                                            OldPath = oldPathObject,
                                            Metadata = newMetadata ?? previousMetadata,
                                            Type = FileChangeType.Renamed,
                                            Direction = SyncDirection.To // detected that a file or folder was renamed locally, so Sync To to update server
                                        }, startProcessingAction);
                                    }
                                    // if index does not exist at either the old nor new paths and the file exists
                                    else
                                    {
                                        if (debugMemory)
                                        {
                                            debugEntry.NewIndexedSpecified = true;
                                            if (debugEntry.NewChangeType != null)
                                            {
                                                memoryDebugger.Instance.AddCheckMetadata(debugEntry);
                                                debugEntry = new CheckMetadataEntry()
                                                {
                                                    CreationTime = debugEntry.CreationTime,
                                                    CreationTimeSpecified = debugEntry.CreationTimeSpecified,
                                                    IsFolder = debugEntry.IsFolder,
                                                    LastTime = debugEntry.LastTime,
                                                    LastTimeSpecified = debugEntry.LastTimeSpecified,
                                                    NewExists = debugEntry.NewExists,
                                                    NewIndexed = debugEntry.NewIndexed,
                                                    NewIndexedSpecified = debugEntry.NewIndexedSpecified,
                                                    NewPath = debugEntry.NewPath,
                                                    OldChangeType = debugEntry.OldChangeType,
                                                    OldExists = debugEntry.OldExists,
                                                    OldExistsSpecified = debugEntry.OldExistsSpecified,
                                                    OldIndexed = debugEntry.OldIndexed,
                                                    OldIndexedSpecified = debugEntry.OldIndexedSpecified,
                                                    OldPath = debugEntry.OldPath,
                                                    PossibleRename = debugEntry.PossibleRename,
                                                    PossibleRenameSpecified = debugEntry.PossibleRenameSpecified,
                                                    Size = debugEntry.Size,
                                                    SizeSpecified = debugEntry.SizeSpecified
                                                };
                                            }
                                            debugEntry.NewChangeType = new WatcherChangeCreated();
                                        }

                                        // add new index at new path
                                        new ChangeAllPathsAdd(this, pathObject,
                                            new FileMetadata()
                                            {
                                                HashableProperties = new FileMetadataHashableProperties(isFolder,
                                                    lastTime,
                                                    creationTime,
                                                    fileLength)
                                            });
                                        // queue file change for create for new path
                                        QueueFileChange(new FileChange(QueuedChanges)
                                        {
                                            NewPath = pathObject,
                                            Metadata = AllPaths[pathObject],
                                            Type = FileChangeType.Created,
                                            Direction = SyncDirection.To // detected that a file or folder was created locally, so Sync To to update server
                                        }, startProcessingAction);
                                    }
                                }
                                // for file system events marked as delete
                                else if ((changeType & WatcherChangeTypes.Deleted) == WatcherChangeTypes.Deleted)
                                {
                                    // if file or folder exists
                                    if (exists)
                                    {
                                        // if index exists and check for folder modify passes
                                        if (AllPaths.ContainsKey(pathObject))
                                        {
                                            if (
                                                // No need to send modified events for folders
                                                // so check if event is on a file or if folder modifies are not ignored
                                                !isFolder
                                                    || !IgnoreFolderModifies)
                                            {
                                                // retrieve stored index at current path
                                                FileMetadata previousMetadata = AllPaths[pathObject];
                                                // compare stored index with values from file info
                                                FileMetadata newMetadata = ReplacementMetadataIfDifferent(previousMetadata,
                                                    isFolder,
                                                    lastTime,
                                                    creationTime,
                                                    fileLength);
                                                // if new metadata came back after comparison, queue file change for modify
                                                if (newMetadata != null)
                                                {
                                                    if (debugMemory)
                                                    {
                                                        debugEntry.NewIndexed = true;
                                                        debugEntry.NewIndexedSpecified = true;
                                                        debugEntry.NewChangeType = new WatcherChangeChanged();
                                                    }

                                                    // replace index at current path
                                                    new ChangeAllPathsIndexSet(this, pathObject, newMetadata);
                                                    // queue file change for modify
                                                    QueueFileChange(new FileChange(QueuedChanges)
                                                    {
                                                        NewPath = pathObject,
                                                        Metadata = newMetadata,
                                                        Type = FileChangeType.Modified,
                                                        Direction = SyncDirection.To // detected that a file or folder was modified locally, so Sync To to update server
                                                    }, startProcessingAction);
                                                }
                                                else if (debugMemory)
                                                {
                                                    debugEntry.NewIndexed = true;
                                                    debugEntry.NewIndexedSpecified = true;
                                                }
                                            }
                                        }
                                        else if (debugMemory)
                                        {
                                            debugEntry.NewIndexedSpecified = true;
                                        }
                                    }
                                    // if file or folder does not exist but index exists for current path
                                    else if (AllPaths.ContainsKey(pathObject))
                                    {
                                        if (debugMemory)
                                        {
                                            debugEntry.NewIndexed = true;
                                            debugEntry.NewIndexedSpecified = true;
                                            debugEntry.NewChangeType = new WatcherChangeDeleted();
                                        }

                                        // queue file change for delete
                                        QueueFileChange(new FileChange(QueuedChanges)
                                        {
                                            NewPath = pathObject,
                                            Metadata = AllPaths[pathObject],
                                            Type = FileChangeType.Deleted,
                                            Direction = SyncDirection.To // detected that a file or folder was deleted locally, so Sync To to update server
                                        }, startProcessingAction);

                                        // remove index
                                        new ChangeAllPathsRemove(this, pathObject);
                                    }
                                    else if (debugMemory)
                                    {
                                        debugEntry.NewIndexedSpecified = true;
                                    }
                                }

                                #endregion
                            }
                        }
                        finally
                        {
                            if (debugMemory)
                            {
                                memoryDebugger.Instance.AddCheckMetadata(debugEntry);
                            }
                        }

                        // If the current change is on a directory that was created,
                        // need to recursively traverse inner objects to also create
                        if (isFolder
                            && exists
                            && (changeType & WatcherChangeTypes.Created) == WatcherChangeTypes.Created)
                        {
                            // Recursively traverse inner directories
                            try
                            {
                                foreach (DirectoryInfo subDirectory in folder.EnumerateDirectories())
                                {
                                    CheckMetadataAgainstFile(subDirectory.FullName,
                                        null,
                                        WatcherChangeTypes.Created,
                                        true,
                                        true);
                                }
                            }
                            catch
                            {
                            }

                            // Recurse one more level deep for each inner file
                            try
                            {
                                foreach (FileInfo innerFile in folder.EnumerateFiles())
                                {
                                    CheckMetadataAgainstFile(innerFile.FullName,
                                        null,
                                        WatcherChangeTypes.Created,
                                        false,
                                        true);
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
            finally
            {
                if (!alreadyHoldingIndexLock)
                {
                    InitialIndexLocker.ExitReadLock();
                }
            }
        }

        private void MoveOldPathsToNewPaths(FilePathHierarchicalNode<FileMetadata> oldPathHierarchy, FilePath oldPath, FilePath newPath)
        {
            FilePathHierarchicalNodeWithValue<FileMetadata> oldPathHierarchyWithValue = oldPathHierarchy as FilePathHierarchicalNodeWithValue<FileMetadata>;
            if (oldPathHierarchyWithValue != null)
            {
                new ChangeAllPathsIndexSet(this, oldPathHierarchyWithValue.Value.Key, null);

                if (!FilePathComparer.Instance.Equals(oldPathHierarchyWithValue.Value.Key, oldPath))
                {
                    FilePath rebuiltNewPath = oldPathHierarchyWithValue.Value.Key.Copy();
                    FilePath.ApplyRename(rebuiltNewPath, oldPath, newPath);
                    new ChangeAllPathsIndexSet(this, rebuiltNewPath, oldPathHierarchyWithValue.Value.Value);
                }
            }

            if (oldPathHierarchy.Children != null)
            {
                foreach (FilePathHierarchicalNode<FileMetadata> innerPathHierarchy in oldPathHierarchy.Children)
                {
                    MoveOldPathsToNewPaths(innerPathHierarchy, oldPath, newPath);
                }
            }
        }

        /// <summary>
        /// Determine if a given file is a shortcut (used to ignore file system events on shortcuts)
        /// </summary>
        /// <param name="toCheck">File to check</param>
        /// <returns>Returns true if file is shortcut, otherwise false</returns>
        private bool FileIsShortcut(FileInfo toCheck)
        {
            // if there is an issue establishing the Win32 shell, just assume that the ".lnk" extension means it was a shortcut;
            // so the following boolean will be set true once the file extension is found to be ".lnk" and set back to false once
            // the shell object has been instantiated (if it throws an exception while it is still true, then we cannot verify the
            // shortcut using Shell32 and have to assume it is a valid shortcut)
            bool shellCodeFailed = false;
            try
            {
                // shortcuts must have shortcut extension for OS to treat it like a shortcut
                if (toCheck.Extension.TrimStart('.').Equals(ShortcutExtension, StringComparison.InvariantCultureIgnoreCase))
                {
                    // set boolean so if Shell32 fails to retrive, assume the ".lnk" is sufficient to ensure file is a shortcut
                    shellCodeFailed = true;

                    // Get Shell interface inside try/catch cause an error means we cannot use Shell32.dll for determining shortcut status
                    // (presumes .lnk will be a shortcut in that case)
                    
                    // Shell interface needed to verify shortcut validity
                    Interop.Shell32.Shell shell32 = new Interop.Shell32.Shell();
                    if (shell32 == null)
                    {
                        throw new Exception("System does not support Shell32, file will be assumed to be a valid shortcut");
                    }

                    // set boolean back to false since Shell32 was successfully retrieved,
                    // so it if fails after this point then the file is not a valid shortcut
                    shellCodeFailed = false;

                    // The following code will either succeed and process the boolean for a readable shortcut, or it will fail (not a valid shortcut)
                    var lnkDirectory = shell32.NameSpace(toCheck.DirectoryName);
                    var lnkItem = lnkDirectory.Items().Item(toCheck.Name);
                    var lnk = (Interop.Shell32.ShellLinkObject)lnkItem.GetLink;
                    return !string.IsNullOrEmpty(lnk.Target.Path);
                }
            }
            catch
            {
                // returns true if file is a ".lnk" and either Shell32 failed to retrieve or Shell32 determined shortcut to be valid,
                // otherwise returns false
                return shellCodeFailed;
            }
            // not a ".lnk" shortcut filetype
            return false;
        }

        /// <summary>
        /// Compares a previous index with new values and returns metadata for a new index if a difference is found
        /// </summary>
        /// <param name="previousMetadata">Metadata from existing index</param>
        /// <param name="isFolder">True for folder or false for file</param>
        /// <param name="lastTime">The greater of the times for last accessed and last written for file or folder</param>
        /// <param name="creationTime">Time of creation of file or folder</param>
        /// <param name="size">File size for file or null for folder</param>
        /// <param name="targetPath">The target path for a shortcut file, if any</param>
        /// <returns>Returns null if a difference was not found, otherwise the new metadata to use</returns>
        private FileMetadata ReplacementMetadataIfDifferent(FileMetadata previousMetadata,
            bool isFolder,
            DateTime lastTime,
            DateTime creationTime,
            Nullable<long> size)
        {
            // Segment out the properties that are used for comparison (before recalculating the MD5)
            FileMetadataHashableProperties forCompare = new FileMetadataHashableProperties(isFolder,
                    lastTime,
                    creationTime,
                    size);

            // If metadata hashable properties differ at all, new metadata will be created and returned
            if (!FileMetadataHashableComparer.Default.Equals(previousMetadata.HashableProperties, forCompare))
            {
                // metadata change detected
                return new FileMetadata(previousMetadata.RevisionChanger)
                {
                    ServerUid = previousMetadata.ServerUid,
                    HashableProperties = forCompare,
                    Revision = previousMetadata.Revision
                };
            }
            return null;
        }

        /// <summary>
        /// Insert new file change into a synchronized queue and begin its delay timer for processing
        /// </summary>
        /// <param name="toChange">New file change</param>
        private void QueueFileChange(FileChange toChange, GenericHolder<Nullable<KeyValuePair<Action<DisposeCheckingHolder>, DisposeCheckingHolder>>> startProcessingAction = null)
        {
            if ((_syncbox.CopiedSettings.TraceType & TraceType.FileChangeFlow) == TraceType.FileChangeFlow)
            {
                ComTrace.LogFileChangeFlow(_syncbox.CopiedSettings.TraceLocation, _syncbox.CopiedSettings.DeviceId, _syncbox.SyncboxId, FileChangeFlowEntryPositionInFlow.FileMonitorAddingToQueuedChanges, Helpers.EnumerateSingleItem(toChange));
            }

            // lock on queue to prevent conflicting updates/reads
            lock (QueuedChanges)
            {
                // define FileChange for rename if a previous change needs to be compared
                FileChange matchedFileChangeForRename;

                // function to move the file change to the metadata-keyed queue and start the delayed processing
                Action<FileChange, GenericHolder<Nullable<KeyValuePair<Action<DisposeCheckingHolder>, DisposeCheckingHolder>>>> StartDelay = (toDelay, runActionExternal) =>
                {
                    // move the file change to the metadata-keyed queue if it does not already exist
                    if (!QueuedChangesByMetadata.ContainsKey(toDelay.Metadata.HashableProperties))
                    {
                        // add file change to metadata-keyed queue
                        QueuedChangesByMetadata.Add(toDelay.Metadata.HashableProperties, toDelay);
                    }

                    // If onQueueingCallback was set on initialization of the monitor agent, fire the callback with the new FileChange
                    if (OnQueueing != null)
                    {
                        OnQueueing(this, toDelay);
                    }

                    if (runActionExternal != null)
                    {
                        runActionExternal.Value = new KeyValuePair<Action<DisposeCheckingHolder>, DisposeCheckingHolder>(state =>
                            {
                                Tuple<FileChange,
                                    Action<FileChange, object, int>,
                                    int,
                                    int> castState = state.Value as Tuple<FileChange, Action<FileChange, object, int>, int, int>;

                                if (castState != null)
                                {
                                    // start delayed processing of file change
                                    castState.Item1.ProcessAfterDelay(
                                        castState.Item2,// Callback which fires on process timer completion (on a new thread)
                                        null,// Userstate if needed on callback (unused)
                                        castState.Item3,// processing delay to wait for more events on this file
                                        castState.Item4);// number of processing delay resets before it will process the file anyways
                                }
                            }, new DisposeCheckingHolder(() => toDelay.DelayCompleted,
                                new Tuple<FileChange,
                                    Action<FileChange, object, int>,
                                    int,
                                    int>(toDelay, // file change to delay-process
                                    ProcessFileChange,// Callback which fires on process timer completion (on a new thread)
                                    ProcessingDelayInMilliseconds,// processing delay to wait for more events on this file
                                    ProcessingDelayMaxResets)));// number of processing delay resets before it will process the file anyways
                    }
                    else
                    {
                        // start delayed processing of file change
                        toDelay.ProcessAfterDelay(
                            ProcessFileChange,// Callback which fires on process timer completion (on a new thread)
                            null,// Userstate if needed on callback (unused)
                            ProcessingDelayInMilliseconds,// processing delay to wait for more events on this file
                            ProcessingDelayMaxResets);// number of processing delay resets before it will process the file anyways
                    }
                };
                // if queue already contains a file change at the same path,
                // either replace change and start the new one if the old one is currently processing
                // otherwise change the existing file change properties to match the new ones and restart the delay timer
                if (QueuedChanges.ContainsKey(toChange.NewPath))
                {
                    // grab existing file change from the queue
                    FileChange previousChange = QueuedChanges[toChange.NewPath];
                    // DoNotAddToSQLIndex should only be true for events already in the SQL index,
                    // and existing events from the index only reprocess once therefore second events
                    // mean a new change has occurred to add to database
                    toChange.DoNotAddToSQLIndex = previousChange.DoNotAddToSQLIndex = false;
                    // if the file change is already marked that it started processing,
                    // need to replace the file change in the queue (which will start it's own processing later on a new delay)
                    if (previousChange.DelayCompleted)
                    {
                        // replace file change in the queue at the same location with the new change
                        QueuedChanges[toChange.NewPath] = toChange;

                        // add old/new path pairs for recursive rename processing
                        if (previousChange.Type == FileChangeType.Renamed)
                        {
                            if (toChange.Type == FileChangeType.Renamed)
                            {
                                if (!FilePathComparer.Instance.Equals(previousChange.OldPath, toChange.OldPath)
                                    || !FilePathComparer.Instance.Equals(previousChange.NewPath, toChange.NewPath))
                                {
                                    OldToNewPathRenames[toChange.OldPath] = toChange.NewPath;
                                }
                            }
                        }
                        else if (toChange.Type == FileChangeType.Renamed)
                        {
                            OldToNewPathRenames[toChange.OldPath] = toChange.NewPath;
                        }

                        // call method that starts the FileChange delayed-processing
                        StartDelay(toChange, startProcessingAction);
                    }
                    // file change has not already started processing
                    else
                    {
                        // FileChange already exists
                        // Instead of starting a new processing delay, update the FileChange information
                        // Then restart the delay timer

                        #region state flow of how to modify existing file changes when a new change comes in
                        switch (toChange.Type)
                        {
                            case FileChangeType.Created:
                                switch (previousChange.Type)
                                {
                                    case FileChangeType.Created:
                                        // error condition
                                        break;
                                    case FileChangeType.Deleted:
                                        // if the path represents a folder, the delete and create must both be processed because their contents might differ
                                        if (previousChange.Metadata.HashableProperties.IsFolder)
                                        {
                                            FileChange changeForPreviousMetadata;
                                            if (QueuedChangesByMetadata.TryGetValue(previousChange.Metadata.HashableProperties, out changeForPreviousMetadata)
                                                && changeForPreviousMetadata.Equals(previousChange))
                                            {
                                                QueuedChangesByMetadata.Remove(previousChange.Metadata.HashableProperties); // the previous change will be allowed to process as-is, clear out its metadata for future checking
                                            }

                                            FileChange toCompareForNewMetadata;
                                            if (!QueuedChangesByMetadata.TryGetValue(toChange.Metadata.HashableProperties, out toCompareForNewMetadata)
                                                || !toCompareForNewMetadata.Equals(toChange))
                                            {
                                                QueuedChangesByMetadata[toChange.Metadata.HashableProperties] = toChange;
                                            }

                                            QueuedChanges[toChange.NewPath] = toChange; // the previous folder deletion change will now be removed from the queued changes queue, and nothing will stop it from continuing to process

                                            StartDelay(toChange, startProcessingAction);
                                        }
                                        // else if the path does not represent a folder,
                                        // discard the deletion change for files which have been deleted and created again with the same metadata
                                        else if (QueuedChangesMetadataComparer.Equals(previousChange.Metadata.HashableProperties, toChange.Metadata.HashableProperties))
                                        {
                                            FileChange toCompare;
                                            if (QueuedChangesByMetadata.TryGetValue(previousChange.Metadata.HashableProperties, out toCompare)
                                                && toCompare.Equals(previousChange))
                                            {
                                                QueuedChangesByMetadata.Remove(previousChange.Metadata.HashableProperties);
                                            }
                                            QueuedChanges.Remove(previousChange.NewPath);
                                            previousChange.Dispose();

                                            // delete caused AllPaths to lose metadata fields, but since we're cancelling the delete, they need to be put back
                                            // since all cases from CheckMetadataAgainstFile which led to this creation change assigned Metadata directly from AllPaths, we can change the fields here to propagate back

                                            toChange.Metadata.MimeType = previousChange.Metadata.MimeType;
                                            toChange.Metadata.Revision = previousChange.Metadata.Revision;
                                            toChange.Metadata.ServerUid = previousChange.Metadata.ServerUid;
                                            toChange.Metadata.StorageKey = previousChange.Metadata.StorageKey;
                                        }
                                        // For files with different metadata, process as a modify
                                        else
                                        {
                                            previousChange.Type = FileChangeType.Modified;
                                            previousChange.Metadata = toChange.Metadata;
                                            previousChange.SetDelayBackToInitialValue();

                                            // delete caused AllPaths to lose metadata fields, but since we're cancelling the delete, they need to be put back
                                            // since all cases from CheckMetadataAgainstFile which led to this creation change assigned Metadata directly from AllPaths, we can change the fields here to propagate back

                                            toChange.Metadata.MimeType = previousChange.Metadata.MimeType;
                                            toChange.Metadata.Revision = previousChange.Metadata.Revision;
                                            toChange.Metadata.ServerUid = previousChange.Metadata.ServerUid;
                                            toChange.Metadata.StorageKey = previousChange.Metadata.StorageKey;
                                        }
                                        break;
                                    case FileChangeType.Modified:
                                        // error condition
                                        break;
                                    case FileChangeType.Renamed:
                                        // error condition
                                        break;
                                }
                                break;
                            case FileChangeType.Deleted:
                                switch (previousChange.Type)
                                {
                                    case FileChangeType.Created:
                                        FileChange toCompare;
                                        if (QueuedChangesByMetadata.TryGetValue(previousChange.Metadata.HashableProperties, out toCompare)
                                            && toCompare.Equals(previousChange))
                                        {
                                            QueuedChangesByMetadata.Remove(previousChange.Metadata.HashableProperties);
                                        }
                                        QueuedChanges.Remove(toChange.NewPath);
                                        previousChange.Dispose();
                                        break;
                                    case FileChangeType.Deleted:
                                        // error condition
                                        break;
                                    case FileChangeType.Modified:
                                        previousChange.PreviouslyModified = true;

                                        previousChange.Type = FileChangeType.Deleted;
                                        previousChange.Metadata = toChange.Metadata;
                                        previousChange.SetDelayBackToInitialValue();
                                        break;
                                    case FileChangeType.Renamed:
                                        previousChange.NewPath = previousChange.OldPath;
                                        previousChange.OldPath = null;
                                        previousChange.Type = FileChangeType.Deleted;
                                        previousChange.Metadata = toChange.Metadata;
                                        previousChange.SetDelayBackToInitialValue();
                                        // remove the old/new path pair for a rename
                                        OldToNewPathRenames.Remove(previousChange.NewPath);
                                        break;
                                }
                                break;
                            case FileChangeType.Modified:
                                switch (previousChange.Type)
                                {
                                    case FileChangeType.Created:
                                        previousChange.Metadata = toChange.Metadata;
                                        previousChange.SetDelayBackToInitialValue();
                                        break;
                                    case FileChangeType.Deleted:
                                        // error condition
                                        break;
                                    case FileChangeType.Modified:
                                        previousChange.Metadata = toChange.Metadata;
                                        previousChange.SetDelayBackToInitialValue();
                                        break;
                                    case FileChangeType.Renamed:
                                        // updating a rename with new metadata will not cause the server to process both modification and rename,
                                        // so need to split the changes into two
                                        
                                        FileChange changeForPreviousMetadata;
                                        if (QueuedChangesByMetadata.TryGetValue(previousChange.Metadata.HashableProperties, out changeForPreviousMetadata)
                                            && changeForPreviousMetadata.Equals(previousChange))
                                        {
                                            QueuedChangesByMetadata.Remove(previousChange.Metadata.HashableProperties); // the previous change will be allowed to process as-is, clear out its metadata for future checking
                                        }

                                        FileChange toCompareForNewMetadata;
                                        if (!QueuedChangesByMetadata.TryGetValue(toChange.Metadata.HashableProperties, out toCompareForNewMetadata)
                                            || !toCompareForNewMetadata.Equals(toChange))
                                        {
                                            QueuedChangesByMetadata[toChange.Metadata.HashableProperties] = toChange;
                                        }

                                        QueuedChanges[toChange.NewPath] = toChange; // the previous file rename change will now be removed from the queued changes queue, and nothing will stop it from continuing to process

                                        StartDelay(toChange, startProcessingAction);
                                        break;
                                }
                                break;
                            case FileChangeType.Renamed:
                                switch (previousChange.Type)
                                {
                                    case FileChangeType.Created:
                                        // error condition
                                        break;
                                    case FileChangeType.Deleted:
                                        // TODO: check if this condition requires setting the ServerUid and Revision fields in toChange.Metadata back to the values from
                                        // previous change like we did with delete followed by create (which in turn sets the fields back appropriately in AllPaths)

                                        if (QueuedChangesMetadataComparer.Equals(previousChange.Metadata.HashableProperties, toChange.Metadata.HashableProperties))
                                        {
                                            previousChange.NewPath = toChange.OldPath;
                                            previousChange.Metadata = toChange.Metadata;
                                            previousChange.SetDelayBackToInitialValue();
                                        }
                                        else
                                        {
                                            previousChange.Metadata = toChange.Metadata;
                                            previousChange.Type = FileChangeType.Modified;
                                            previousChange.SetDelayBackToInitialValue();

                                            FileChange oldLocationDelete = new FileChange(QueuedChanges)
                                                {
                                                    NewPath = toChange.OldPath,
                                                    Type = FileChangeType.Deleted,
                                                    Metadata = toChange.Metadata,
                                                    Direction = SyncDirection.To // detected that a file or folder was deleted locally, so Sync To to update server
                                                };
                                            if (QueuedChanges.ContainsKey(toChange.OldPath))
                                            {
                                                FileChange previousOldPathChange = QueuedChanges[toChange.OldPath];

                                                FileChange toCompare;
                                                if (!QueuedChangesByMetadata.TryGetValue(previousOldPathChange.Metadata.HashableProperties, out toCompare)
                                                    || !toCompare.Equals(previousOldPathChange))
                                                {
                                                    QueuedChangesByMetadata[previousOldPathChange.Metadata.HashableProperties] = previousOldPathChange;
                                                }

                                                previousOldPathChange.Metadata = toChange.Metadata;
                                                previousOldPathChange.Type = FileChangeType.Deleted;
                                                previousOldPathChange.SetDelayBackToInitialValue();
                                            }
                                            else
                                            {
                                                QueuedChanges.Add(toChange.OldPath,
                                                    oldLocationDelete);
                                            }
                                            StartDelay(oldLocationDelete, startProcessingAction);
                                        }
                                        break;
                                    case FileChangeType.Modified:
                                        // error condition
                                        break;
                                    case FileChangeType.Renamed:
                                        // error condition
                                        break;
                                }
                                break;
                        }
                        #endregion
                    }
                }

                // System does not generate proper rename WatcherTypes if a file/folder is moved
                // The following two 'else ifs' checks for matching metadata between creation/deletion events to associate together as a rename

                // Existing FileChange is a Deleted event and the incoming event is a matching Created event which has not yet completed
                else if (toChange.Type == FileChangeType.Created
                        && QueuedChangesByMetadata.ContainsKey(toChange.Metadata.HashableProperties)
                        && (matchedFileChangeForRename = QueuedChangesByMetadata[toChange.Metadata.HashableProperties]).Type == FileChangeType.Deleted
                        && !matchedFileChangeForRename.DelayCompleted)
                {
                    FilePath removeFromQueuedChanges = matchedFileChangeForRename.NewPath;
                    if (matchedFileChangeForRename.PreviouslyModified)
                    {
                        matchedFileChangeForRename.Type = FileChangeType.Modified;

                        toChange.Type = FileChangeType.Renamed;
                        toChange.OldPath = matchedFileChangeForRename.NewPath;
                    }
                    else
                    {
                        // FileChange already exists
                        // Instead of starting a new processing delay, update the FileChange information
                        // Then restart the delay timer
                        matchedFileChangeForRename.Type = FileChangeType.Renamed;
                        matchedFileChangeForRename.OldPath = matchedFileChangeForRename.NewPath;
                        matchedFileChangeForRename.NewPath = toChange.NewPath;
                    }

                    // if the new created change is missing required fields to process a rename (such as ServerUid and maybe Revision),
                    // then try and pull them from the previous deletion change before replacing the metadata
                    if (toChange.Metadata != null
                        && matchedFileChangeForRename.Metadata != null)
                    {
                        if (toChange.Metadata.ServerUid == null)
                        {
                            toChange.Metadata.ServerUid = matchedFileChangeForRename.Metadata.ServerUid;
                        }

                        if (toChange.Metadata.Revision == null)
                        {
                            toChange.Metadata.Revision = matchedFileChangeForRename.Metadata.Revision;
                        }
                    }

                    matchedFileChangeForRename.Metadata = toChange.Metadata;

                    QueuedChanges.Remove(removeFromQueuedChanges);

                    if (matchedFileChangeForRename.PreviouslyModified)
                    {
                        StartDelay(toChange, startProcessingAction);

                        QueuedChanges[toChange.NewPath] = toChange;
                    }
                    else
                    {
                        matchedFileChangeForRename.SetDelayBackToInitialValue();

                        QueuedChanges.Add(matchedFileChangeForRename.NewPath,
                            matchedFileChangeForRename);
                    }

                    // add old/new path pairs for recursive rename processing
                    OldToNewPathRenames[removeFromQueuedChanges] = toChange.NewPath;
                }
                // Existing FileChange is a Created event and the incoming event is a matching Deleted event which has not yet completed
                else if (toChange.Type == FileChangeType.Deleted
                        && QueuedChangesByMetadata.ContainsKey(toChange.Metadata.HashableProperties)
                        && (matchedFileChangeForRename = QueuedChangesByMetadata[toChange.Metadata.HashableProperties]).Type == FileChangeType.Created
                        && !matchedFileChangeForRename.DelayCompleted)
                {
                    // FileChange already exists
                    // Instead of starting a new processing delay, update the FileChange information
                    // Then restart the delay timer
                    matchedFileChangeForRename.Type = FileChangeType.Renamed;
                    matchedFileChangeForRename.OldPath = toChange.NewPath;

                    // the later deletion caused the metatadata to be lost in AllPaths,
                    // so need to add it back here;
                    // already under a lock on AllPaths since QueueFileChange must be called under such lock
                    if (AllPaths.ContainsKey(matchedFileChangeForRename.OldPath))
                    {
                        AllPaths[matchedFileChangeForRename.NewPath] = null; // ensure no error with the rename
                        AllPaths.Rename(matchedFileChangeForRename.OldPath, matchedFileChangeForRename.NewPath); // should move the existing metadata at path forwards
                    }


                    if (QueuedChanges.ContainsKey(matchedFileChangeForRename.NewPath))
                    {
                        if (QueuedChanges[matchedFileChangeForRename.NewPath] != matchedFileChangeForRename)
                        {
                            QueuedChanges[matchedFileChangeForRename.NewPath] = matchedFileChangeForRename;
                        }
                    }
                    else
                    {
                        QueuedChanges.Add(matchedFileChangeForRename.NewPath,
                            matchedFileChangeForRename);
                    }

                    matchedFileChangeForRename.SetDelayBackToInitialValue();
                    // add old/new path pairs for recursive rename processing
                    OldToNewPathRenames[matchedFileChangeForRename.OldPath] = matchedFileChangeForRename.NewPath;
                }

                // if file change does not exist in the queue at the same file path and the change was not marked to be converted to a rename
                else
                {
                    // add file change to the queue
                    QueuedChanges.Add(toChange.NewPath, toChange);

                    StartDelay(toChange, startProcessingAction);

                    if (toChange.Type == FileChangeType.Renamed)
                    {
                        // add old/new path pairs for recursive rename processing
                        OldToNewPathRenames[toChange.OldPath] = toChange.NewPath;
                    }
                }
            }
        }

        //// confirm the following: (after the delay-change processing is now all handled together on a single timer processing thread)
        // Comes in on a new thread every time
        /// <summary>
        /// EventHandler for processing a file change after its delay completed
        /// </summary>
        /// <param name="sender">The file change itself</param>
        /// <param name="state">Userstate, if provided before the delayed processing</param>
        /// <param name="remainingOperations">Number of operations remaining across all FileChange (via DelayProcessable)</param>
        private void ProcessFileChange(FileChange sender, object state, int remainingOperations)
        {
            RemoveFileChangeFromQueuedChanges(sender, new originalQueuedChangesIndexesByInMemoryIdsOneValue(sender.InMemoryId, sender.NewPath));

            if (remainingOperations == 0) // flush remaining operations before starting processing timer
            {
                lock (NeedsMergeToSql)
                {
                    if (MergingToSql)
                    {
                        NeedsMergeToSql.Enqueue(sender);
                        return;
                    }
                    MergingToSql = true;
                }

                List<FileChange> mergeAll = new List<FileChange>();

                List<FileChange> mergeBatch = new List<FileChange>();

                Func<bool> operationsRemaining = () =>
                    {
                        lock (NeedsMergeToSql)
                        {
                            if (NeedsMergeToSql.Count == 0)
                            {
                                return (MergingToSql = false); // set and return false
                            }
                            return true;
                        }
                    };

                FileChange senderToAdd;
                if (sender == null
                    || sender.Type != FileChangeType.Renamed
                    || sender.NewPath == null // shouldn't be a case here
                    || sender.OldPath == null
                    || !FilePathComparer.Instance.Equals(sender.OldPath, sender.NewPath)) // check for same path rename, only other events should be processed
                {
                    senderToAdd = sender;
                    //mergeBatch.Add(sender);
                    //mergeAll.Add(sender);
                }
                else
                {
                    senderToAdd = null;
                }

                var recurseHierarchyAndAddSyncFromsToHashSet = DelegateAndDataHolder.Create(
                    new
                    {
                        thisDelegate = new GenericHolder<DelegateAndDataHolder>(null),
                        innerHierarchy = new GenericHolder<FilePathHierarchicalNode<List<FileChange>>>(null),
                        matchedChanges = new HashSet<FileChange>()
                    },
                    (Data, errorToAccumulate) =>
                    {
                        FilePathHierarchicalNodeWithValue<List<FileChange>> castHierarchy = Data.innerHierarchy.Value as FilePathHierarchicalNodeWithValue<List<FileChange>>;
                        if (castHierarchy != null)
                        {
                            foreach (FileChange innerUpDown in (castHierarchy.Value.Value ?? Enumerable.Empty<FileChange>()))
                            {
                                if (innerUpDown.Direction == SyncDirection.From)
                                {
                                    Data.matchedChanges.Add(innerUpDown);
                                }
                            }
                        }
                        if (Data.innerHierarchy.Value != null)
                        {
                            foreach (FilePathHierarchicalNode<List<FileChange>> recurseHierarchicalNode in (Data.innerHierarchy.Value.Children ?? Enumerable.Empty<FilePathHierarchicalNode<List<FileChange>>>()))
                            {
                                Data.innerHierarchy.Value = recurseHierarchicalNode;
                                Data.thisDelegate.Value.Process();
                            }
                        }
                    },
                    null);
                recurseHierarchyAndAddSyncFromsToHashSet.TypedData.thisDelegate.Value = recurseHierarchyAndAddSyncFromsToHashSet;

                do
                {
                    lock (NeedsMergeToSql)
                    {
                        while (NeedsMergeToSql.Count > 0)
                        {
                            FileChange nextMerge = NeedsMergeToSql.Dequeue();

                            if (nextMerge == null
                                || nextMerge.Type != FileChangeType.Renamed
                                || nextMerge.NewPath == null
                                || nextMerge.OldPath == null
                                || !FilePathComparer.Instance.Equals(nextMerge.OldPath, nextMerge.NewPath)) // check for same path rename, only other events should be processed
                            {
                                mergeBatch.Add(nextMerge);
                                mergeAll.Add(nextMerge);
                            }
                        }
                    }

                    KeyValuePair<FilePathDictionary<List<FileChange>>, CLError> upDownsWrapped = GetUploadDownloadTransfersInProgress(CurrentFolderPath);
                    if (upDownsWrapped.Value == null
                        && upDownsWrapped.Key != null)
                    {
                        FilePathDictionary<List<FileChange>> upDowns = upDownsWrapped.Key;

                        // add the stored change for the current call to this method, must be added to then end of the batch;
                        // before, it was being added to the beginning which was out of order
                        if (senderToAdd != null)
                        {
                            mergeBatch.Add(senderToAdd);
                            mergeAll.Add(senderToAdd);

                            senderToAdd = null;
                        }

                        foreach (FileChange currentMerge in mergeBatch)
                        {
                            if (currentMerge.Direction == SyncDirection.To
                                && (currentMerge.Type == FileChangeType.Deleted
                                    || currentMerge.Type == FileChangeType.Renamed))
                            {
                                FilePathHierarchicalNode<List<FileChange>> matchedHierarchy;
                                CLError matchedHierarchyError = upDowns.GrabHierarchyForPath(
                                    (currentMerge.Type == FileChangeType.Deleted
                                        ? currentMerge.NewPath
                                        : /* currentMerge.Type == FileChangeType.Renamed */ currentMerge.OldPath),
                                    out matchedHierarchy,
                                    suppressException: true);
                                if (matchedHierarchyError == null)
                                {
                                    recurseHierarchyAndAddSyncFromsToHashSet.TypedData.innerHierarchy.Value = matchedHierarchy;
                                    recurseHierarchyAndAddSyncFromsToHashSet.Process();

                                    foreach (FileChange currentMatchedDownload in recurseHierarchyAndAddSyncFromsToHashSet.TypedData.matchedChanges)
                                    {
                                        if (currentMatchedDownload.fileDownloadMoveLocker != null)
                                        {
                                            Monitor.Enter(currentMatchedDownload.fileDownloadMoveLocker);
                                        }

                                        try
                                        {
                                            if (currentMerge.Type == FileChangeType.Renamed)
                                            {
                                                FilePath previousNewPath = currentMatchedDownload.NewPath;
                                                if (previousNewPath != null)
                                                {
                                                    FilePath rebuiltNewPath = previousNewPath.Copy();
                                                    FilePath.ApplyRename(rebuiltNewPath, currentMerge.OldPath, currentMerge.NewPath);
                                                    currentMatchedDownload.NewPath = rebuiltNewPath;
                                                }
                                            }
                                            else /* currentMerge.Type == FileChangeType.Deleted */
                                            {
                                                currentMatchedDownload.NewPath = null; // will trigger donwload to stop and not to copy to the final location
                                            }
                                        }
                                        catch
                                        {
                                        }
                                        finally
                                        {
                                            if (currentMatchedDownload.fileDownloadMoveLocker != null)
                                            {
                                                Monitor.Exit(currentMatchedDownload.fileDownloadMoveLocker);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    MessageEvents.FireNewEventMessage(
                                        "Error grabbing hierarchy from uploads and downloads in progress before merging new events to the database: " + matchedHierarchyError.errorDescription,
                                        EventMessageLevel.Important,
                                        new GeneralErrorInfo());
                                }
                            }
                        }
                    }
                    else
                    {
                        MessageEvents.FireNewEventMessage(
                            "An error occurred checking against uploads or downloads in progress before merging new events into the database: " + (upDownsWrapped.Value == null ? "{null}" : upDownsWrapped.Value.errorDescription),
                            EventMessageLevel.Important,
                            new GeneralErrorInfo());
                    }

                    CLError mergeError = Indexer.MergeEventsIntoDatabase(mergeBatch.Select(currentMerge => new FileChangeMerge(currentMerge, null)));
                    if (mergeError != null)
                    {
                        // forces logging even if the setting is turned off in the severe case since a message box had to appear
                        mergeError.LogErrors(_syncbox.CopiedSettings.TraceLocation, true);

                        // errors may be more common now that our database is hierarchichal and simple event ordering problems could throw an error adding to database (file before parent folder),
                        // TODO: better error recovery instead of halting whole SDK
                        MessageEvents.FireNewEventMessage(
                            "An error occurred adding a file system event to the database:" + Environment.NewLine +
                                string.Join(Environment.NewLine,
                                    mergeError.GrabExceptions().Select(currentError => (currentError is AggregateException
                                        ? string.Join(Environment.NewLine, ((AggregateException)currentError).Flatten().InnerExceptions.Select(innerError => innerError.Message).ToArray())
                                        : currentError.Message)).ToArray()),
                            EventMessageLevel.Important,
                            new HaltAllOfCloudSDKErrorInfo());
                    }

                    if ((_syncbox.CopiedSettings.TraceType & TraceType.FileChangeFlow) == TraceType.FileChangeFlow)
                    {
                        ComTrace.LogFileChangeFlow(_syncbox.CopiedSettings.TraceLocation, _syncbox.CopiedSettings.DeviceId, _syncbox.SyncboxId, FileChangeFlowEntryPositionInFlow.FileMonitorAddingBatchToSQL, mergeBatch);
                    }

                    // clear out batch for merge for next set of remaining operations
                    mergeBatch.Clear();
                }
                while (operationsRemaining()); // flush remaining operations before starting processing timer

                if (mergeAll.Count > 0)
                {
                    lock (QueuesTimer.TimerRunningLocker)
                    {
                        foreach (FileChange nextMerge in mergeAll)
                        {
                            if (nextMerge.EventId == 0)
                            {
                                string noEventIdErrorMessage = "EventId was zero on a FileChange to queue to ProcessingChanges: " +
                                    nextMerge.ToString() + " " + (nextMerge.NewPath == null ? "nullPath" : nextMerge.NewPath.ToString());

                                // forces logging even if the setting is turned off in the severe case since a message box had to appear
                                ((CLError)new Exception(noEventIdErrorMessage)).LogErrors(_syncbox.CopiedSettings.TraceLocation, true);

                                MessageEvents.FireNewEventMessage(
                                    noEventIdErrorMessage,
                                    EventMessageLevel.Important,
                                    new HaltAllOfCloudSDKErrorInfo());
                            }

                            ProcessingChanges.AddLast(nextMerge);
                        }

                        if (ProcessingChanges.Count > MaxProcessingChangesBeforeTrigger)
                        {
                            QueuesTimer.TriggerTimerCompletionImmediately();
                        }
                        else
                        {
                            QueuesTimer.StartTimerIfNotRunning();
                        }
                    }
                }
            }
            else
            {
                lock (NeedsMergeToSql)
                {
                    NeedsMergeToSql.Enqueue(sender);
                }
            }
        }

        // Finds the FileChange in QueuedChanges in the same Dictionary as well as any other associated dictionaries,
        // and removes it
        private bool RemoveFileChangeFromQueuedChanges(FileChange toRemove, originalQueuedChangesIndexesByInMemoryIdsBase originalQueuedChangesIndexesByInMemoryIds)
        {
            if (originalQueuedChangesIndexesByInMemoryIds == null)
            {
                throw new NullReferenceException("originalQueuedChangesIndexesByInMemoryIds cannot be null, perhaps AssignDependencies private helper was called through from the internal AssignDependencies");
            }

            // If the change is a rename that is being removed, take off its old/new path pair
            if (toRemove.Type == FileChangeType.Renamed)
            {
                if (OldToNewPathRenames.ContainsKey(toRemove.OldPath)
                    && FilePathComparer.Instance.Equals(toRemove.NewPath, OldToNewPathRenames[toRemove.OldPath]))
                {
                    OldToNewPathRenames.Remove(toRemove.OldPath);
                }
            }

            // remove file changes from each queue if they exist
            FileChange metadataIndexedChange;
            if (QueuedChangesByMetadata.TryGetValue(toRemove.Metadata.HashableProperties, out metadataIndexedChange)
                && FilePathComparer.Instance.Equals(toRemove.NewPath, metadataIndexedChange.NewPath)) // added check on final path of metadata-matched change in case more renames are in process on the same file\folder
            {
                QueuedChangesByMetadata.Remove(toRemove.Metadata.HashableProperties);
            }
            FilePath originalNewPath;
            if (!originalQueuedChangesIndexesByInMemoryIds.TryGetValue(toRemove.InMemoryId, out originalNewPath))
            {
                return false;
            }

            if (QueuedChanges.ContainsKey(originalNewPath)
                && QueuedChanges[originalNewPath] == toRemove)
            {
                return QueuedChanges.Remove(originalNewPath);
            }
            return false;
        }
        private abstract class originalQueuedChangesIndexesByInMemoryIdsBase
        {
            public abstract bool TryGetValue(long key, out FilePath value);
        }
        private sealed class originalQueuedChangesIndexesByInMemoryIdsOneValue : originalQueuedChangesIndexesByInMemoryIdsBase
        {
            public override bool TryGetValue(long key, out FilePath value)
            {
                if (key != oneKey)
                {
                    value = Helpers.DefaultForType<FilePath>();
                    return false;
                }

                value = oneValue;
                return true;
            }
            private readonly long oneKey;
            private readonly FilePath oneValue;

            public originalQueuedChangesIndexesByInMemoryIdsOneValue(long key, FilePath value)
            {
                this.oneKey = key;
                this.oneValue = value;
            }
        }
        private sealed class originalQueuedChangesIndexesByInMemoryIdsFromDictionary : originalQueuedChangesIndexesByInMemoryIdsBase
        {
            public override bool TryGetValue(long key, out FilePath value)
            {
                return dict.TryGetValue(key, out value);
            }
            private readonly Dictionary<long, FilePath> dict;

            public originalQueuedChangesIndexesByInMemoryIdsFromDictionary(Dictionary<long, FilePath> dict)
            {
                this.dict = dict;
            }
        }

        /// <summary>
        /// Path to write processed FileChange log
        /// </summary>
        private const string testFilePath = "C:\\Users\\Public\\Documents\\MonitorAgentOutput.xml";
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        /// <summary>
        /// Writes the xml string generated after processing a FileChange event to the log file at 'testFilePath' location
        /// </summary>
        /// <param name="toWrite"></param>
        private static void AppendFileChangeProcessedLogXmlString(string toWrite)
        {
            // If file does not exist, create it with an xml declaration and the beginning of a root tag
            if (!File.Exists(testFilePath))
            {
                File.AppendAllText(testFilePath, "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine + "<root>" + Environment.NewLine);
            }
            // Append current xml string to log file
            File.AppendAllText(testFilePath, toWrite);
        }

        /// <summary>
        /// Cleans up FileSystemWatchers
        /// </summary>
        private void StopWatchers()
        {
            // lock on current object for changing RunningStatus so it cannot be stopped/started simultaneously
            lock (this)
            {
                // if running status includes the file watcher running flag, dispose the watcher and clear the instance reference
                if ((RunningStatus & MonitorRunning.FileOnlyRunning) == MonitorRunning.FileOnlyRunning)
                {
                    // dispose the file system watcher for files
                    DisposeFileSystemWatcher(FileWatcher, false);
                    // clear the instance reference
                    FileWatcher = null;
                }

                // if running status includes the folder watcher running flag, dispose the watcher and clear the instance reference
                if ((RunningStatus & MonitorRunning.FolderOnlyRunning) == MonitorRunning.FolderOnlyRunning)
                {
                    // dispose the file system watcher for folders
                    DisposeFileSystemWatcher(FolderWatcher, true);
                    // clear the instance reference
                    FolderWatcher = null;
                }

                // remove protection on root directory from changes such as deletion
                setDirectoryAccessControl(false);
            }
        }

        /// <summary>
        /// Disposes a file system watcher after removing its attached eventhandlers and stopping new events from being raised
        /// </summary>
        /// <param name="toDispose">FileSystemWatcher to dispose</param>
        /// <param name="folderOnly">True if the FileSystemWatcher was the one for folders, otherwise false</param>
        private void DisposeFileSystemWatcher(FileSystemWatcher toDispose, bool folderOnly)
        {
            // stop new events from being raised
            toDispose.EnableRaisingEvents = false;
            // check if watcher was for folders to remove eventhandlers appropriately
            if (folderOnly)
            {
                // remove eventhandlers for folder watcher
                toDispose.Changed -= folderWatcher_Changed;
                toDispose.Created -= folderWatcher_Changed;
                toDispose.Deleted -= folderWatcher_Changed;
                toDispose.Renamed -= folderWatcher_Changed;
            }
            // watcher was for files
            else
            {
                // remove eventhandlers for file watcher
                toDispose.Changed -= fileWatcher_Changed;
                toDispose.Created -= fileWatcher_Changed;
                toDispose.Deleted -= fileWatcher_Changed;
                toDispose.Renamed -= fileWatcher_Changed;
            }
            // dispose watcher
            toDispose.Dispose();
        }

        /// <summary>
        /// Callback fired when a subsequent delete operation occurs in AllPaths
        /// </summary>
        /// <param name="deletePath">Path where file or folder was deleted</param>
        /// <param name="value">Previous value at deleted path</param>
        /// <param name="changeRoot">Original FilePath removed/cleared that triggered recursion</param>
        private void MetadataPath_RecursiveDelete(FilePath deletePath, FileMetadata value, FilePath changeRoot)
        {
            if (value.HashableProperties.IsFolder)
            {
                long createItemAddFolderCreationUtcTicks = value.HashableProperties.CreationTime.ToUniversalTime().Ticks;
                FilePath[] existingAddFolderPaths;
                if (FolderCreationTimeUtcTicksToPath.TryGetValue(createItemAddFolderCreationUtcTicks, out existingAddFolderPaths))
                {
                    if (existingAddFolderPaths.Length == 1)
                    {
                        if (FilePathComparer.Instance.Equals(deletePath, existingAddFolderPaths[0]))
                        {
                            FolderCreationTimeUtcTicksToPath.Remove(createItemAddFolderCreationUtcTicks);
                        }
                    }
                    else
                    {
                        for (int currentDeleteIndex = 0; currentDeleteIndex < existingAddFolderPaths.Length; currentDeleteIndex++)
                        {
                            if (FilePathComparer.Instance.Equals(deletePath, existingAddFolderPaths[currentDeleteIndex]))
                            {
                                FilePath[] addFolderPathsMinusDeleted = new FilePath[existingAddFolderPaths.Length - 1];
                                if (currentDeleteIndex != 0)
                                {
                                    Array.Copy(existingAddFolderPaths, 0, addFolderPathsMinusDeleted, 0, currentDeleteIndex);
                                }

                                if (currentDeleteIndex != existingAddFolderPaths.Length - 1)
                                {
                                    Array.Copy(existingAddFolderPaths, currentDeleteIndex + 1, addFolderPathsMinusDeleted, currentDeleteIndex, addFolderPathsMinusDeleted.Length - currentDeleteIndex);
                                }

                                FolderCreationTimeUtcTicksToPath[createItemAddFolderCreationUtcTicks] = addFolderPathsMinusDeleted;
                                break; // presumes only a single folder path will ever be in FolderCreationTimeUtcTicksToPath, therefore stop checking since we found a path
                            }
                        }
                    }
                }
            }

            // If a filepath is deleted, that means a delete operation may occur in the sync as well for the same path;
            // If the final change to be processed will be a delete,
            // all previous changes below the deleted path should be disposed since they don't need to fire (will be overridden with deletion)

            lock (QueuedChanges)
            {
                if (QueuedChanges.ContainsKey(changeRoot))
                {
                    FileChange rootChange = QueuedChanges[changeRoot];
                    Action<FileChange> toProcess = innerRoot =>
                        {
                            if (innerRoot.Type == FileChangeType.Deleted)
                            {
                                if (QueuedChanges.ContainsKey(deletePath))
                                {
                                    QueuedChanges[deletePath].Dispose();
                                    QueuedChanges.Remove(deletePath);
                                }
                            }
                        };
                    if (rootChange.DelayCompleted)
                    {
                        toProcess(rootChange);
                    }
                    else
                    {
                        rootChange.EnqueuePreprocessingAction(toProcess);
                    }
                }
            }
        }

        /// <summary>
        /// Callback fired when a subsequent rename operation occurs in AllPaths
        /// </summary>
        /// <param name="oldPath">Previous path of file or folder</param>
        /// <param name="newPath">New path of file or folder</param>
        /// <param name="value">Value for renamed file or folder</param>
        /// <param name="changeRootOld">Original old FilePath renamed that triggered recursion</param>
        /// <param name="changeRootNew">Original new FilePath renamed to that triggered recursion</param>
        private void MetadataPath_RecursiveRename(FilePath oldPath, FilePath newPath, FileMetadata value, FilePath changeRootOld, FilePath changeRootNew)
        {
            if (value.HashableProperties.IsFolder)
            {
                long movedMetadataCreationUtcTicks = value.HashableProperties.CreationTime.ToUniversalTime().Ticks;
                FilePath[] movedHashablesPaths;
                if (FolderCreationTimeUtcTicksToPath.TryGetValue(movedMetadataCreationUtcTicks, out movedHashablesPaths))
                {
                    for (int currentDeleteIndex = 0; currentDeleteIndex < movedHashablesPaths.Length; currentDeleteIndex++)
                    {
                        if (FilePathComparer.Instance.Equals(oldPath, movedHashablesPaths[currentDeleteIndex]))
                        {
                            movedHashablesPaths[currentDeleteIndex] = newPath;
                            break; // presumes only a single folder path will ever be in FolderCreationTimeUtcTicksToPath, therefore stop checking since we found a path
                        }
                        else if (currentDeleteIndex == movedHashablesPaths.Length - 1) // not found to replace, need to add
                        {
                            FilePath[] appendMoved = new FilePath[movedHashablesPaths.Length + 1];
                            Array.Copy(movedHashablesPaths, appendMoved, movedHashablesPaths.Length);
                            appendMoved[appendMoved.Length - 1] = newPath;
                            FolderCreationTimeUtcTicksToPath[movedMetadataCreationUtcTicks] = appendMoved;
                        }
                    }
                }
                else
                {
                    FolderCreationTimeUtcTicksToPath.Add(movedMetadataCreationUtcTicks, new[] { newPath });
                }
            }

            // If allpaths gets a rename, queuedchanges may get a rename FileChange to sync;
            // If the final change to be processed will be a rename,
            // all previous changes that had a old or new path under the change root need to have
            // that path updated appropriately if the change root gets processed first
            lock (QueuedChanges)
            {
                if (QueuedChanges.ContainsKey(changeRootNew))
                {
                    FileChange rootChange = QueuedChanges[changeRootNew];
                    Action<FileChange> toProcess = innerRoot =>
                        {
                            if (innerRoot.Type == FileChangeType.Renamed
                                && FilePathComparer.Instance.Equals(innerRoot.OldPath, changeRootOld))
                            {
                                if (QueuedChanges.ContainsKey(oldPath))
                                {
                                    FileChange foundRecurseChange = QueuedChanges[oldPath];
                                    if (foundRecurseChange.Type == FileChangeType.Renamed)
                                    {
                                        FilePath renamedOverlapChild = foundRecurseChange.OldPath;
                                        FilePath renamedOverlap = renamedOverlapChild.Parent;

                                        while (renamedOverlap != null)
                                        {
                                            if (FilePathComparer.Instance.Equals(renamedOverlap, changeRootOld))
                                            {
                                                renamedOverlapChild.Parent = changeRootNew;
                                                break;
                                            }

                                            renamedOverlapChild = renamedOverlap;
                                            renamedOverlap = renamedOverlap.Parent;
                                        }
                                    }
                                    foundRecurseChange.NewPath = newPath;
                                    QueuedChanges.Remove(oldPath);
                                    QueuedChanges.Add(newPath, foundRecurseChange);
                                }
                                else if (OldToNewPathRenames.ContainsKey(oldPath))
                                {
                                    FilePath existingNewPath = OldToNewPathRenames[oldPath];
                                    if (QueuedChanges.ContainsKey(existingNewPath))
                                    {
                                        FileChange existingChangeAtNewPath = QueuedChanges[existingNewPath];
                                        if (existingChangeAtNewPath.Type == FileChangeType.Renamed)
                                        {
                                            existingChangeAtNewPath.OldPath = newPath;
                                        }
                                    }
                                }
                            }
                        };
                    if (rootChange.DelayCompleted)
                    {
                        toProcess(rootChange);
                    }
                    else
                    {
                        rootChange.EnqueuePreprocessingAction(toProcess);
                    }
                }
            }
        }

        private void ProcessQueuesAfterTimer(bool emptyProcessingQueue)
        {
            lock (this)
            {
                if (this.Disposed)
                {
                    return;
                }
            }

            lock (SyncRunLocker)
            {
                if (SyncRunLocker.Value)
                {
                    NextSyncQueued.Value = true;
                }
                else
                {
                    SyncRunLocker.Value = true;

                    // run Sync
                    (new Thread(new ParameterizedThreadStart(RunOnProcessEventGroupCallback)))
                        .Start(new object[]
                        {
                            emptyProcessingQueue,
                            this.SyncRun,
                            SyncRunLocker,
                            NextSyncQueued
                        });
                }
            }
        }

        private static void RunOnProcessEventGroupCallback(object state)
        {
            object[] castState = state as object[];
            bool matchedParameters = false;

            if (castState != null
                && castState.Length == 4)
            {
                Nullable<bool> argOneNullable = castState[0] as Nullable<bool>;

                Func<bool, CLError> RunSyncRun = castState[1] as Func<bool, CLError>;

                GenericHolder<bool> RunLocker = castState[2] as GenericHolder<bool>;
                GenericHolder<bool> NextRunQueued = castState[3] as GenericHolder<bool>;

                if (argOneNullable != null
                    && RunSyncRun != null
                    && RunLocker != null
                    && NextRunQueued != null)
                {
                    matchedParameters = true;

                    Func<GenericHolder<bool>, GenericHolder<bool>, bool> runAgain = (runLock, nextQueue) =>
                        {
                            lock (runLock)
                            {
                                if (nextQueue.Value)
                                {
                                    nextQueue.Value = false;
                                    return true;
                                }
                                else
                                {
                                    runLock.Value = false;
                                    return false;
                                }
                            }
                        };

                    do
                    {
                        RunSyncRun((bool)argOneNullable);
                    } while (runAgain(RunLocker, NextRunQueued));
                }
            }

            if (!matchedParameters)
            {
                throw new InvalidOperationException("Unable to fire OnProcessEventGroupCallback due to parameter mismatch");
            }
        }
        #endregion
    }
}