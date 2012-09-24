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
using CloudApiPublic.Model;
using CloudApiPublic.Static;
using CloudApiPublic.Support;
using System.Globalization;
using System.Security.Cryptography;
using System.Threading;
// the following linq namespace is used only if the optional initialization parameter for processing logging is passed as true
using System.Xml.Linq;
using CloudApiPrivate.Model.Settings;
using System.Windows;
using System.Transactions;
using FileMonitor.Static;

namespace FileMonitor
{
    /// <summary>
    /// Delegate to match MonitorAgent's method ProcessFileListForSyncProcessing which pulls changes for Sync
    /// </summary>
    /// <param name="outputChanges">Output array of FileChanges to process</param>
    /// <param name="outputChangesInError">Output array of FileChanges with observed errors for requeueing, may be empty but never null</param>
    /// <returns>Returns error(s) that occurred while pulling processed changes, if any</returns>
    public delegate CLError GrabProcessedChanges(IEnumerable<KeyValuePair<bool, FileChange>> initialFailures,
        out IEnumerable<KeyValuePair<FileChange, FileStream>> outputChanges,
        out IEnumerable<KeyValuePair<bool, FileChange>> outputChangesInError);

    public delegate CLError DependencyAssignments(IEnumerable<KeyValuePair<FileChange, FileStream>> toAssign,
        IEnumerable<FileChange> currentFailures,
        out IEnumerable<KeyValuePair<FileChange, FileStream>> outputChanges,
        out IEnumerable<FileChange> outputFailures);

    public delegate CLError MetadataByPathAndRevision(string path, string revision, out FileMetadata metadata);

    /// <summary>
    /// Class to cover file monitoring; created with delegates to connect to the SQL indexer and to start Sync communication for new events
    /// </summary>
    public sealed class MonitorAgent : IDisposable
    {
        #region public properties
        /// <summary>
        /// Retrieves running status of monitor as enum for each part (file and folder)
        /// </summary>
        /// <param name="status">Returned running status</param>
        /// <returns>Error while retrieving status, if any</returns>
        public CLError GetRunningStatus(out MonitorRunning status)
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
        #endregion

        #region private fields and property
        // stores the optional FileChange queueing callback intialization parameter
        private Action<MonitorAgent, FileChange> OnQueueing;

        // stores the callback used to process a group of events.  Passed via an intialization parameter
        private Func<GrabProcessedChanges,
            Func<IEnumerable<KeyValuePair<FileChange, FileChange>>, bool, CLError>,
            Func<IEnumerable<FileChange>, bool, GenericHolder<List<FileChange>>, CLError>,
            bool,
            Func<string, IEnumerable<long>, string, CLError>,
            Func<string>,
            DependencyAssignments,
            Func<string>,
            Func<FileChange, CLError>,
            Func<long, CLError>,
            MetadataByPathAndRevision,
            Func<string>,
            CLError> SyncRun;
        private GenericHolder<bool> SyncRunLocker = new GenericHolder<bool>(false);
        private GenericHolder<bool> NextSyncQueued = new GenericHolder<bool>(false);

        private Func<string> GetDeviceName;

        private Func<ReaderWriterLockSlim> GetCELocker;

        // stores the callback used to add the processed event to the SQL index
        /// <summary>
        /// First parameter is merged event, second parameter is event to remove
        /// </summary>
        private Func<IEnumerable<KeyValuePair<FileChange, FileChange>>, bool, CLError> ProcessMergeToSQL;

        // stores the callback used to complete a sync with a list of completed events
        /// <summary>
        /// First parameter is syncId, second parameter is successfulEventIds, third parameter is newSyncRoot, returns any error that occurred
        /// </summary>
        private Func<string, IEnumerable<long>, string, CLError> ProcessCompletedSync;


        // Returns the last sync Id, should be tied to the SQLIndexer IndexingAgent's LastSyncId property under a locker
        private Func<string> GetLastSyncId;

        private MetadataByPathAndRevision GetMetadataByPathAndRevision;

        private Func<long, CLError> CompleteSingleEvent;

        // store the optional logging boolean initialization parameter
        private bool LogProcessingFileChanges;

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

        // Storage of current file indexes, keyed by file path
        private FilePathDictionary<FileMetadata> AllPaths;

        // Storage of changes queued to process (QueuedChanges used as the locker for both and keyed by file path, QueuedChangesByMetadata keyed by the hashable metadata properties)
        private Dictionary<FilePath, FileChange> QueuedChanges = new Dictionary<FilePath, FileChange>(FilePathComparer.Instance);
        private Dictionary<FilePath, FilePath> OldToNewPathRenames = new Dictionary<FilePath, FilePath>(FilePathComparer.Instance);
        private static readonly FileMetadataHashableComparer QueuedChangesMetadataComparer = new FileMetadataHashableComparer();// Comparer has improved hashing by using only the fastest changing bits
        private Dictionary<FileMetadataHashableProperties, FileChange> QueuedChangesByMetadata = new Dictionary<FileMetadataHashableProperties, FileChange>(QueuedChangesMetadataComparer);// Use custom comparer for improved hashing

        // Queue of file monitor events that occur while initial index is processing
        private Queue<ChangesQueueHolder> ChangesQueueForInitialIndexing = new Queue<ChangesQueueHolder>();
        // Storage class for required parameters to the CheckMetadataAgainstFile method
        private class ChangesQueueHolder
        {
            public string newPath { get; set; }
            public string oldPath { get; set; }
            public WatcherChangeTypes changeType { get; set; }
            public bool folderOnly { get; set; }
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
        #endregion

        /// <summary>
        /// Create and initialize the MonitorAgent with the root folder to be monitored (Cloud Directory),
        /// requires running Start() method to begin monitoring and then, when available, load
        /// the initial index list to begin processing via BeginProcessing(initialList)
        /// </summary>
        /// <param name="folderPath">path of root folder to be monitored</param>
        /// <param name="newAgent">returned MonitorAgent</param>
        /// <param name="syncRun">delegate to be executed when a group of events is to be processed</param>
        /// <param name="onQueueingCallback">(optional) action to be executed evertime a FileChange would be queued for processing</param>
        /// <param name="logProcessing">(optional) if set, logs FileChange objects when their processing callback fires</param>
        /// <returns>Returns any error that occurred if there was one</returns>
        public static CLError CreateNewAndInitialize(string folderPath,
            out MonitorAgent newAgent,
            Func<GrabProcessedChanges,
                Func<IEnumerable<KeyValuePair<FileChange, FileChange>>, bool, CLError>,
                Func<IEnumerable<FileChange>, bool, GenericHolder<List<FileChange>>, CLError>,
                bool,
                Func<string, IEnumerable<long>, string, CLError>,
                Func<string>,
                DependencyAssignments,
                Func<string>,
                Func<FileChange, CLError>,
                Func<long, CLError>,
                MetadataByPathAndRevision,
                Func<string>,
                CLError> syncRun,
            Func<IEnumerable<KeyValuePair<FileChange, FileChange>>, bool, CLError> onProcessMergeToSQL,
            Func<string, IEnumerable<long>, string, CLError> onProcessCompletedSync,
            Func<string> getLastSyncId,
            Func<long, CLError> completeSingleEvent,
            MetadataByPathAndRevision getMetadataByPathAndRevision,
            Func<string> getDeviceName,
            Func<ReaderWriterLockSlim> getCELocker,
            Action<MonitorAgent, FileChange> onQueueingCallback = null,
            bool logProcessing = false)
        {
            try
            {
                newAgent = new MonitorAgent();
            }
            catch (Exception ex)
            {
                newAgent = Helpers.DefaultForType<MonitorAgent>();
                return ex;
            }
            try
            {
                if (string.IsNullOrEmpty(folderPath))
                {
                    throw new Exception("Folder path cannot be null nor empty");
                }
                DirectoryInfo folderInfo = new DirectoryInfo(folderPath);
                if (!folderInfo.Exists)
                {
                    throw new Exception("Folder not found at provided folder path");
                }
                if (syncRun == null)
                {
                    throw new NullReferenceException("onProcessEventGroupCallback cannot be null");
                }
                if (onProcessMergeToSQL == null)
                {
                    throw new NullReferenceException("onProcessMergeToSQL cannot be null");
                }
                if (onProcessCompletedSync == null)
                {
                    throw new NullReferenceException("onProcessCompletedSync cannot be null");
                }
                if (getLastSyncId == null)
                {
                    throw new NullReferenceException("getLastSyncId cannot be null");
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
                newAgent.CurrentFolderPath = newAgent.InitialFolderPath = folderPath;

                // assign local fields with optional initialization parameters
                newAgent.OnQueueing = onQueueingCallback;
                newAgent.SyncRun = syncRun;
                newAgent.ProcessMergeToSQL = onProcessMergeToSQL;
                newAgent.ProcessCompletedSync = onProcessCompletedSync;
                newAgent.GetLastSyncId = getLastSyncId;
                newAgent.LogProcessingFileChanges = logProcessing;
                newAgent.CompleteSingleEvent = completeSingleEvent;
                newAgent.GetMetadataByPathAndRevision = getMetadataByPathAndRevision;
                newAgent.GetDeviceName = getDeviceName;
                newAgent.GetCELocker = getCELocker;

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
                    1000,// Collect items in queue for 1 second before batch processing
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

        private MonitorAgent() { }
        // Standard IDisposable implementation based on MSDN System.IDisposable
        ~MonitorAgent()
        {
            this.Dispose(false);
        }

        #region public methods
        /// <summary>
        /// Starts the queue timer to start sync processing,
        /// if it is not already started for other events
        /// </summary>
        public void PushNotification(string notification)
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
        public CLError ApplySyncFromFileChange(FileChange toApply)
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

                FilePath rootPath = CurrentFolderPath;
                if (!toApply.NewPath.Contains(rootPath))
                {
                    throw new ArgumentException("FileChange's NewPath does not fall within the root directory");
                }

                Action<FilePath, FilePath, object, Nullable<DateTime>, Nullable<DateTime>> recurseFolderCreationToRoot = (toCreate, root, currentAction, creationTime, lastTime) =>
                    {
                        if (!FilePathComparer.Instance.Equals(toCreate, root))
                        {
                            Action<FilePath, FilePath, object, Nullable<DateTime>, Nullable<DateTime>> castAction = currentAction as Action<FilePath, FilePath, object, Nullable<DateTime>, Nullable<DateTime>>;
                            if (castAction == null)
                            {
                                throw new NullReferenceException("Unable to cast currentAction as the type of the current Action");
                            }
                            castAction(toCreate.Parent, root, castAction, null, null);

                            if (!AllPaths.ContainsKey(toCreate))
                            {
                                DirectoryInfo createdDirectory = null;
                                Helpers.RunActionWithRetries(() => createdDirectory = Directory.CreateDirectory(toCreate.ToString()), true);

                                try
                                {
                                    if (creationTime != null)
                                    {
                                        Helpers.RunActionWithRetries(() => createdDirectory.CreationTimeUtc = (DateTime)creationTime, true);
                                    }
                                    if (lastTime != null)
                                    {
                                        Helpers.RunActionWithRetries(() => createdDirectory.LastAccessTimeUtc = (DateTime)lastTime, true);
                                        Helpers.RunActionWithRetries(() => createdDirectory.LastWriteTimeUtc = (DateTime)lastTime, true);
                                    }
                                }
                                catch
                                {
                                    Helpers.RunActionWithRetries(() => createdDirectory.Delete(), true);
                                    throw;
                                }

                                DateTime createdLastWriteUtc;
                                if (lastTime == null)
                                {
                                    createdLastWriteUtc = new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc);
                                    Helpers.RunActionWithRetries(() => createdLastWriteUtc = createdDirectory.LastWriteTimeUtc, false);
                                }
                                else
                                {
                                    createdLastWriteUtc = (DateTime)lastTime;
                                }
                                DateTime createdCreationUtc;
                                if (creationTime == null)
                                {
                                    createdCreationUtc = new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc);
                                    Helpers.RunActionWithRetries(() => createdCreationUtc = createdDirectory.CreationTimeUtc, false);
                                }
                                else
                                {
                                    createdCreationUtc = (DateTime)creationTime;
                                }

                                AllPaths.Add(toCreate, new FileMetadata()
                                {
                                    HashableProperties = new FileMetadataHashableProperties(true,
                                        createdLastWriteUtc,
                                        createdCreationUtc,
                                        null)
                                });
                            }
                        }
                    };

                lock (AllPaths)
                {
                    switch (toApply.Type)
                    {
                        case FileChangeType.Created:
                            recurseFolderCreationToRoot(toApply.NewPath, rootPath, recurseFolderCreationToRoot, toApply.Metadata.HashableProperties.CreationTime, toApply.Metadata.HashableProperties.LastTime);
                            break;
                        case FileChangeType.Deleted:
                            if (toApply.Metadata.HashableProperties.IsFolder)
                            {
                                try
                                {
                                    Directory.Delete(toApply.NewPath.ToString(), true);
                                }
                                catch (DirectoryNotFoundException)
                                {
                                }
                            }
                            else
                            {
                                try
                                {
                                    File.Delete(toApply.NewPath.ToString());
                                }
                                catch (FileNotFoundException)
                                {
                                }
                            }

                            AllPaths.Remove(toApply.NewPath);
                            break;
                        case FileChangeType.Renamed:
                            recurseFolderCreationToRoot(toApply.NewPath.Parent, rootPath, recurseFolderCreationToRoot, null, null);

                            if (toApply.Metadata.HashableProperties.IsFolder)
                            {
                                Directory.Move(toApply.OldPath.ToString(), toApply.NewPath.ToString());
                            }
                            else
                            {
                                string newPathString = toApply.NewPath.ToString();
                                string oldPathString = toApply.OldPath.ToString();

                                if (File.Exists(newPathString))
                                {
                                    try
                                    {
                                        string backupLocation = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create) +
                                                "\\Cloud\\DownloadTemp\\" +
                                                Guid.NewGuid().ToString();
                                        File.Replace(oldPathString,
                                            newPathString,
                                            backupLocation,
                                            ignoreMetadataErrors: true);
                                        try
                                        {
                                            if (File.Exists(backupLocation))
                                            {
                                                File.Delete(backupLocation);
                                            }
                                        }
                                        catch
                                        {
                                        }
                                    }
                                    // File.Replace not supported on non-NTFS drives, must use traditional move
                                    catch (PlatformNotSupportedException)
                                    {
                                        if (File.Exists(newPathString))
                                        {
                                            File.Delete(newPathString);
                                        }
                                        File.Move(oldPathString, newPathString);
                                    }
                                }
                                else
                                {
                                    File.Move(oldPathString, newPathString);
                                }
                            }

                            AllPaths.Remove(toApply.OldPath);
                            AllPaths[toApply.NewPath] = new FileMetadata(toApply.Metadata.RevisionChanger)
                            {
                                HashableProperties = toApply.Metadata.HashableProperties,
                                LinkTargetPath = toApply.Metadata.LinkTargetPath,
                                Revision = toApply.Metadata.Revision
                            };
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

        /// <summary>
        /// Adds a FileChange to the ProcessingQueue;
        /// will also trigger a sync if one isn't already scheduled to run
        /// </summary>
        /// <param name="toAdd">FileChange to queue</param>
        /// <param name="insertAtTop">Send true for the FileChange to be processed first on the queue, otherwise it will be last</param>
        /// <returns>Returns an error that occurred queueing the FileChange, if any</returns>
        public CLError AddFileChangeToProcessingQueue(FileChange toAdd, bool insertAtTop, GenericHolder<List<FileChange>> errorHolder)
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
        public CLError AddFileChangesToProcessingQueue(IEnumerable<FileChange> toAdd, bool insertAtTop, GenericHolder<List<FileChange>> errorHolder)
        {
            CLError toReturn = null;
            try
            {
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
                if (toAdd != null)
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

                List<GenericHolder<Nullable<KeyValuePair<Action<object>, object>>>> startProcessingActions = new List<GenericHolder<Nullable<KeyValuePair<Action<object>, object>>>>();

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
                        bool triggerSyncWithNoChanges = true;

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

                                GenericHolder<Nullable<KeyValuePair<Action<object>, object>>> newProcessingAction = new GenericHolder<Nullable<KeyValuePair<Action<object>, object>>>();
                                startProcessingActions.Add(newProcessingAction);

                                QueueFileChange(new FileChange(QueuedChanges)
                                    {
                                        NewPath = currentChange.NewPath,
                                        OldPath = currentChange.OldPath,
                                        Metadata = currentChange.Metadata,
                                        Type = currentChange.Type,
                                        DoNotAddToSQLIndex = currentChange.DoNotAddToSQLIndex,
                                        EventId = currentChange.EventId
                                    }, newProcessingAction);
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

                    GenericHolder<Nullable<KeyValuePair<Action<object>, object>>> newProcessingAction = new GenericHolder<Nullable<KeyValuePair<Action<object>, object>>>();
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

                foreach (GenericHolder<Nullable<KeyValuePair<Action<object>, object>>> startProcessing in startProcessingActions)
                {
                    if (startProcessing.Value != null)
                    {
                        ((KeyValuePair<Action<object>, object>)startProcessing.Value).Key(((KeyValuePair<Action<object>, object>)startProcessing.Value).Value);
                    }
                }
            }
            finally
            {
                InitialIndexLocker.ExitWriteLock();
            }
        }

        private CLError AssignDependencies(KeyValuePair<FileChangeSource, FileChangeWithDependencies>[] dependencyChanges, Dictionary<FileChangeWithDependencies, KeyValuePair<FileChange, FileChangeSource>> OriginalFileChangeMappings, out HashSet<FileChangeWithDependencies> PulledChanges)
        {
            CLError toReturn = null;
            try
            {
                //// ¡¡ SQL CE does not support transactions !!
                //using (TransactionScope PreprocessingScope = new TransactionScope())
                //{
                GetCELocker().EnterWriteLock();
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
                                            CLError creationModificationCheckError = CreationModificationDependencyCheck(OuterFileChange, InnerFileChange, PulledChanges);
                                            DisposeChanges = null;
                                            ContinueProcessing = true;
                                            if (creationModificationCheckError != null)
                                            {
                                                toReturn += new AggregateException("Error in CreationModificationDependencyCheck", creationModificationCheckError.GrabExceptions());
                                            }
                                            break;
                                        case FileChangeType.Renamed:
                                            CLError renameCheckError = RenameDependencyCheck(OuterFileChange, InnerFileChange, PulledChanges, out DisposeChanges, out ContinueProcessing);
                                            if (renameCheckError != null)
                                            {
                                                toReturn += new AggregateException("Error in RenameDependencyCheck", renameCheckError.GrabExceptions());
                                            }
                                            break;
                                        case FileChangeType.Deleted:
                                            CLError deleteCheckError = DeleteDependencyCheck(OuterFileChange, InnerFileChange, PulledChanges, out DisposeChanges, out ContinueProcessing);
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
                                            KeyValuePair<FileChange, FileChangeSource> CurrentOriginalMapping;
                                            if (OriginalFileChangeMappings != null
                                                && OriginalFileChangeMappings.TryGetValue(CurrentDisposal, out CurrentOriginalMapping))
                                            {
                                                CurrentOriginalMapping.Key.Dispose();
                                                if (CurrentOriginalMapping.Value == FileChangeSource.QueuedChanges)
                                                {
                                                    RemoveFileChangeFromQueuedChanges(CurrentOriginalMapping.Key);
                                                }
                                            }
                                            CLError updateSQLError = ProcessMergeToSQL(new KeyValuePair<FileChange, FileChange>[] { new KeyValuePair<FileChange, FileChange>(null, CurrentDisposal) }, true);
                                            if (updateSQLError != null)
                                            {
                                                toReturn += new AggregateException("Error updating SQL", updateSQLError.GrabExceptions());
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
                    GetCELocker().ExitWriteLock();
                }
                //// ¡¡ SQL CE does not support transactions !!
                    //PreprocessingScope.Complete();
                //}

            }
            catch (Exception ex)
            {
                PulledChanges = Helpers.DefaultForType<HashSet<FileChangeWithDependencies>>();
                toReturn += ex;
            }
            return toReturn;
        }

        private CLError CreationModificationDependencyCheck(FileChangeWithDependencies EarlierChange, FileChangeWithDependencies LaterChange, HashSet<FileChangeWithDependencies> PulledChanges)
        {
            CLError toReturn = null;
            try
            {
                foreach (FileChangeWithDependencies CurrentEarlierChange in EnumerateDependenciesFromFileChangeDeepestLevelsFirst(EarlierChange)
                    .OfType<FileChangeWithDependencies>())
                {
                    if (LaterChange.NewPath.Contains(CurrentEarlierChange.NewPath))
                    {
                        CurrentEarlierChange.AddDependency(LaterChange);
                        PulledChanges.Add(LaterChange);
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

        private CLError RenameDependencyCheck(FileChangeWithDependencies EarlierChange, FileChangeWithDependencies LaterChange, HashSet<FileChangeWithDependencies> PulledChanges, out List<FileChangeWithDependencies> DisposeChanges, out bool ContinueProcessing)
        {
            CLError toReturn = null;
            try
            {
                bool DependenciesAddedToLaterChange = false;
                DisposeChanges = null;
                HashSet<FileChangeWithDependencies> RenamePathSearches = null;

                foreach (FileChangeWithDependencies CurrentEarlierChange in EnumerateDependenciesFromFileChangeDeepestLevelsFirst(EarlierChange)
                    .Reverse()
                    .OfType<FileChangeWithDependencies>())
                {
                    bool breakOutOfEnumeration = false;
                    switch (CurrentEarlierChange.Type)
                    {
                        case FileChangeType.Renamed:
                            if (!DependenciesAddedToLaterChange
                                && (RenamePathSearches == null || !RenamePathSearches.Contains(CurrentEarlierChange)))
                            {
                                foreach (FileChangeWithDependencies CurrentInnerRename in EnumerateDependenciesFromFileChangeDeepestLevelsFirst(CurrentEarlierChange, onlyRenamePathsFromTop: true)
                                    .OfType<FileChangeWithDependencies>())
                                {
                                    if (RenamePathSearches == null)
                                    {
                                        RenamePathSearches = new HashSet<FileChangeWithDependencies>(new FileChangeWithDependencies[] { CurrentInnerRename });
                                    }
                                    else
                                    {
                                        RenamePathSearches.Add(CurrentInnerRename);
                                    }
                                    if (CurrentInnerRename.NewPath.Contains(LaterChange.OldPath)
                                        || LaterChange.OldPath.Contains(CurrentInnerRename.NewPath))
                                    {
                                        foreach (FileChangeWithDependencies dependencyToMove in CurrentInnerRename.Dependencies)
                                        {
                                            CurrentInnerRename.RemoveDependency(dependencyToMove);
                                            LaterChange.AddDependency(dependencyToMove);
                                        }
                                        DependenciesAddedToLaterChange = true;

                                        CurrentInnerRename.AddDependency(LaterChange);
                                        PulledChanges.Add(LaterChange);
                                        break;
                                    }
                                }
                            }
                            break;
                        case FileChangeType.Created:
                        case FileChangeType.Modified:
                            if (CurrentEarlierChange.NewPath.Contains(LaterChange.OldPath))
                            {
                                if (FilePathComparer.Instance.Equals(CurrentEarlierChange.NewPath, LaterChange.OldPath))
                                {
                                    CurrentEarlierChange.NewPath = LaterChange.NewPath;
                                    CLError updateSqlError = ProcessMergeToSQL(new KeyValuePair<FileChange, FileChange>[] { new KeyValuePair<FileChange, FileChange>(CurrentEarlierChange, null) }, true);
                                    if (updateSqlError != null)
                                    {
                                        toReturn += new AggregateException("Error updating SQL after replacing NewPath", updateSqlError.GrabExceptions());
                                    }

                                    if (CurrentEarlierChange.Type == FileChangeType.Created)
                                    {
                                        if (DisposeChanges == null)
                                        {
                                            DisposeChanges = new List<FileChangeWithDependencies>(new FileChangeWithDependencies[] { LaterChange });
                                        }
                                        else
                                        {
                                            DisposeChanges.Add(LaterChange);
                                        }

                                        PulledChanges.Add(LaterChange);

                                        foreach (FileChangeWithDependencies laterParent in EnumerateDependenciesFromFileChangeDeepestLevelsFirst(EarlierChange)
                                            .OfType<FileChangeWithDependencies>()
                                            .Where(currentParentCheck => currentParentCheck.Dependencies.Contains(LaterChange)))
                                        {
                                            laterParent.RemoveDependency(LaterChange);
                                        }

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
                                    while (renamedOverlap != null)
                                    {
                                        // when the rename's OldPath matches the current recursive path parent level,
                                        // replace the child's parent with the rename's NewPath and break out of the checking loop
                                        if (FilePathComparer.Instance.Equals(renamedOverlap, LaterChange.OldPath))
                                        {
                                            renamedOverlapChild.Parent = LaterChange.NewPath;
                                            CLError replacePathPortionError = ProcessMergeToSQL(new KeyValuePair<FileChange, FileChange>[] { new KeyValuePair<FileChange, FileChange>(CurrentEarlierChange, null) }, true);
                                            if (replacePathPortionError != null)
                                            {
                                                toReturn += new AggregateException("Error replacing a portion of the path of CurrentEarlierChange", replacePathPortionError.GrabExceptions());
                                            }
                                            break;
                                        }

                                        // set recursing path variables one level higher
                                        renamedOverlapChild = renamedOverlap;
                                        renamedOverlap = renamedOverlap.Parent;
                                    }
                                    
                                    if (!DependenciesAddedToLaterChange)
                                    {
                                        LaterChange.AddDependency(CurrentEarlierChange);
                                        PulledChanges.Add(CurrentEarlierChange);
                                        DependenciesAddedToLaterChange = true;
                                    }
                                }
                            }
                            break;
                        case FileChangeType.Deleted:// possible error condition, I am not sure this case should ever hit
                            if (LaterChange.OldPath.Contains(CurrentEarlierChange.NewPath))
                            {
                                breakOutOfEnumeration = true;
                            }
                            break;
                        default:
                            throw new InvalidOperationException("Unknown FileChangeType for CurrentEarlierChange: " + CurrentEarlierChange.Type.ToString());
                    }
                    if (breakOutOfEnumeration)
                    {
                        break;
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

        private CLError DeleteDependencyCheck(FileChangeWithDependencies EarlierChange, FileChangeWithDependencies LaterChange, HashSet<FileChangeWithDependencies> PulledChanges, out List<FileChangeWithDependencies> DisposeChanges, out bool ContinueProcessing)
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
                        // child of path of overlap of CurrentEarlierChange's NewPath with LaterChange's OldPath
                        // (whose parent will be replaced by the change of LaterChange's NewPath
                        FilePath renamedOverlapChild = LaterChange.NewPath;
                        // variable for recursive checking against the rename's OldPath
                        FilePath renamedOverlap = renamedOverlapChild.Parent;

                        // loop till recursing parent of current path level is null
                        while (renamedOverlap != null)
                        {
                            // when the rename's OldPath matches the current recursive path parent level,
                            // replace the child's parent with the rename's NewPath and break out of the checking loop
                            if (FilePathComparer.Instance.Equals(renamedOverlap, CurrentEarlierChange.NewPath))
                            {
                                renamedOverlapChild.Parent = CurrentEarlierChange.OldPath;
                                CLError replacePathPortionError = ProcessMergeToSQL(new KeyValuePair<FileChange, FileChange>[] { new KeyValuePair<FileChange, FileChange>(LaterChange, null) }, false);
                                if (replacePathPortionError != null)
                                {
                                    toReturn += new AggregateException("Error replacing a portion of the path of CurrentEarlierChange", replacePathPortionError.GrabExceptions());
                                }
                                break;
                            }

                            // set recursing path variables one level higher
                            renamedOverlapChild = renamedOverlap;
                            renamedOverlap = renamedOverlap.Parent;
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

        public CLError AssignDependencies(IEnumerable<KeyValuePair<FileChange, FileStream>> toAssign,
            IEnumerable<FileChange> currentFailures,
            out IEnumerable<KeyValuePair<FileChange, FileStream>> outputChanges,
            out IEnumerable<FileChange> outputFailures)
        {
            CLError toReturn = null;
            try
            {
                HashSet<FileChangeWithDependencies> PulledChanges;
                Func<KeyValuePair<FileChange, FileStream>, Dictionary<FileChangeWithDependencies, KeyValuePair<GenericHolder<bool>, FileStream>>, FileChangeWithDependencies> convertChange = (inputChange, streamMappings) =>
                    {
                        if (inputChange.Key is FileChangeWithDependencies)
                        {
                            streamMappings[(FileChangeWithDependencies)inputChange.Key] = new KeyValuePair<GenericHolder<bool>,FileStream>(new GenericHolder<bool>(false), inputChange.Value);
                            return (FileChangeWithDependencies)inputChange.Key;
                        }

                        FileChangeWithDependencies outputChange;
                        CLError conversionError = FileChangeWithDependencies.CreateAndInitialize(inputChange.Key, null, out outputChange);
                        if (conversionError != null)
                        {
                            throw new AggregateException("Error converting FileChange to FileChangeWithDependencies", conversionError.GrabExceptions());
                        }
                        streamMappings[outputChange] = new KeyValuePair<GenericHolder<bool>,FileStream>(new GenericHolder<bool>(false), inputChange.Value);
                        return outputChange;
                    };
                Dictionary<FileChangeWithDependencies, KeyValuePair<GenericHolder<bool>, FileStream>> originalFileStreams = new Dictionary<FileChangeWithDependencies, KeyValuePair<GenericHolder<bool>, FileStream>>();
                KeyValuePair<FileChangeSource, FileChangeWithDependencies>[] assignmentsWithDependencies = toAssign
                    .Select(currentToAssign => new KeyValuePair<FileChangeSource, FileChangeWithDependencies>(FileChangeSource.ProcessingChanges, convertChange(currentToAssign, originalFileStreams)))
                    .Concat(currentFailures.Select(currentFailure => new KeyValuePair<FileChangeSource, FileChangeWithDependencies>(FileChangeSource.FailureQueue, convertChange(new KeyValuePair<FileChange,FileStream>(currentFailure, null), originalFileStreams))))
                    .ToArray();
                toReturn = AssignDependencies(assignmentsWithDependencies,
                    null,
                    out PulledChanges);
                
                List<KeyValuePair<FileChange, FileStream>> outputChangeList = new List<KeyValuePair<FileChange, FileStream>>();
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
                        else
                        {
                            KeyValuePair<GenericHolder<bool>, FileStream> originalStream;
                            if (originalFileStreams.TryGetValue(currentAssignment.Value, out originalStream))
                            {
                                originalStream.Key.Value = true;
                                outputChangeList.Add(new KeyValuePair<FileChange, FileStream>(currentAssignment.Value, originalStream.Value));
                            }
                            else
                            {
                                outputChangeList.Add(new KeyValuePair<FileChange, FileStream>(currentAssignment.Value, null));
                            }
                        }
                    }
                }

                foreach (KeyValuePair<GenericHolder<bool>, FileStream> streamValue in originalFileStreams.Values)
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
                outputChanges = Helpers.DefaultForType<IEnumerable<KeyValuePair<FileChange, FileStream>>>();
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
        /// <returns>Returns error(s) that occurred finalizing the FileChange array, if any</returns>
        public CLError GrabPreprocessedChanges(IEnumerable<KeyValuePair<bool, FileChange>> initialFailures,
            out IEnumerable<KeyValuePair<FileChange, FileStream>> outputChanges,
            out IEnumerable<KeyValuePair<bool, FileChange>> outputChangesInError)
        {
            CLError toReturn = null;
            List<KeyValuePair<KeyValuePair<FileChange, FileChange>, FileChange>> queuedChangesNeedMergeToSql = new List<KeyValuePair<KeyValuePair<FileChange, FileChange>, FileChange>>();
            try
            {
                lock (QueuedChanges)
                {
                    lock (QueuesTimer.TimerRunningLocker)
                    {
                        Func<KeyValuePair<FileChangeSource, KeyValuePair<bool, FileChange>>, FileChangeWithDependencies> convertChange = toConvert =>
                            {
                                FileChangeWithDependencies converted;
                                CLError conversionError = FileChangeWithDependencies.CreateAndInitialize(toConvert.Value.Value,
                                    ((toConvert.Value.Value is FileChangeWithDependencies) ? ((FileChangeWithDependencies)toConvert.Value.Value).Dependencies : null),
                                    out converted);
                                if (conversionError != null)
                                {
                                    throw new AggregateException("Error converting FileChange to FileChangeWithDependencies", conversionError.GrabExceptions());
                                }
                                if (converted.EventId == 0
                                    && toConvert.Key != FileChangeSource.QueuedChanges)
                                {
                                    throw new ArgumentException("Cannot communicate FileChange without EventId; FileChangeSource: " + toConvert.Key.ToString());
                                }
                                return converted;
                            };

                        var AllFileChanges = (ProcessingChanges.DequeueAll()
                            .Select(currentProcessingChange => new KeyValuePair<FileChangeSource, KeyValuePair<bool, FileChange>>(FileChangeSource.ProcessingChanges, new KeyValuePair<bool, FileChange>(false, currentProcessingChange)))
                            .Concat(initialFailures.Select(currentInitialFailure => new KeyValuePair<FileChangeSource, KeyValuePair<bool, FileChange>>(FileChangeSource.FailureQueue, new KeyValuePair<bool, FileChange>(currentInitialFailure.Key, currentInitialFailure.Value)))))
                            .OrderBy(eventOrdering => eventOrdering.Value.Value.EventId)
                            .Concat(QueuedChanges.Values
                                .OrderBy(memoryIdOrdering => memoryIdOrdering.InMemoryId)
                                .Select(currentQueuedChange => new KeyValuePair<FileChangeSource, KeyValuePair<bool, FileChange>>(FileChangeSource.QueuedChanges, new KeyValuePair<bool, FileChange>(false, currentQueuedChange))))
                            .Select(currentFileChange => new
                            {
                                ExistingError = currentFileChange.Value.Key,
                                OriginalFileChange = currentFileChange.Value.Value,
                                DependencyFileChange = convertChange(currentFileChange),
                                SourceType = currentFileChange.Key
                            })
                            .ToArray();

                        Dictionary<FileChangeWithDependencies, KeyValuePair<FileChange, FileChangeSource>> OriginalFileChangeMappings = AllFileChanges.ToDictionary(keySelector => keySelector.DependencyFileChange,
                            valueSelector => new KeyValuePair<FileChange, FileChangeSource>(valueSelector.OriginalFileChange, valueSelector.SourceType));

                        HashSet<FileChangeWithDependencies> PulledChanges;
                        CLError assignmentError = AssignDependencies(AllFileChanges.Select(currentFileChange => new KeyValuePair<FileChangeSource, FileChangeWithDependencies>(currentFileChange.SourceType, currentFileChange.DependencyFileChange)).ToArray(),
                            OriginalFileChangeMappings,
                            out PulledChanges);
                        List<KeyValuePair<FileChange, FileStream>> OutputChangesList = new List<KeyValuePair<FileChange, FileStream>>();
                        List<KeyValuePair<bool, FileChange>> OutputFailuresList = new List<KeyValuePair<bool, FileChange>>();

                        for (int currentChangeIndex = 0; currentChangeIndex < AllFileChanges.Length; currentChangeIndex++)
                        {
                            var CurrentDependencyTree = AllFileChanges[currentChangeIndex];

                            if (!PulledChanges.Contains(CurrentDependencyTree.DependencyFileChange))
                            {
                                Action<List<KeyValuePair<KeyValuePair<FileChange, FileChange>, FileChange>>> removeQueuedChangesFromDependencyTree = changesToAdd =>
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
                                            changesToAdd.Add(new KeyValuePair<KeyValuePair<FileChange, FileChange>, FileChange>(
                                                new KeyValuePair<FileChange, FileChange>(currentQueuedChange.Value, null),
                                                mappedOriginalQueuedChange.Key));
                                        }
                                    }
                                };

                                if (CurrentDependencyTree.SourceType == FileChangeSource.FailureQueue)
                                {
                                    removeQueuedChangesFromDependencyTree(queuedChangesNeedMergeToSql);

                                    OutputFailuresList.Add(new KeyValuePair<bool, FileChange>(AllFileChanges[currentChangeIndex].ExistingError, CurrentDependencyTree.DependencyFileChange));
                                }
                                else
                                {
                                    bool nonQueuedChangeFound = false;
                                    
                                    IEnumerable<KeyValuePair<int, FileChange>> nonQueuedChangesEnumerable = EnumerateDependencies(CurrentDependencyTree.DependencyFileChange);
                                    foreach (KeyValuePair<int, FileChange> currentNonQueuedChange in nonQueuedChangesEnumerable)
                                    {
                                        FileChangeWithDependencies castEnumeratedNonQueuedChange;
                                        KeyValuePair<FileChange, FileChangeSource> mappedOriginalNonQueuedChange;
                                        if ((castEnumeratedNonQueuedChange = currentNonQueuedChange.Value as FileChangeWithDependencies) != null
                                            && OriginalFileChangeMappings.TryGetValue(castEnumeratedNonQueuedChange,
                                                out mappedOriginalNonQueuedChange)
                                            && mappedOriginalNonQueuedChange.Value != FileChangeSource.QueuedChanges)
                                        {
                                            nonQueuedChangeFound = true;
                                            break;
                                        }
                                    }

                                    if (nonQueuedChangeFound)
                                    {
                                        removeQueuedChangesFromDependencyTree(queuedChangesNeedMergeToSql);

                                        FileStream OutputStream = null;
                                        bool CurrentFailed = false;
                                        if (CurrentDependencyTree.DependencyFileChange.Metadata != null
                                            && !CurrentDependencyTree.DependencyFileChange.Metadata.HashableProperties.IsFolder
                                            && (CurrentDependencyTree.DependencyFileChange.Type == FileChangeType.Created
                                                || CurrentDependencyTree.DependencyFileChange.Type == FileChangeType.Modified)
                                            && CurrentDependencyTree.DependencyFileChange.Direction == SyncDirection.To)
                                        {
                                            try
                                            {
                                                try
                                                {
                                                    OutputStream = new FileStream(CurrentDependencyTree.DependencyFileChange.NewPath.ToString(), FileMode.Open, FileAccess.Read, FileShare.Read);
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

                                                MD5 md5Hasher = MD5.Create();

                                                try
                                                {
                                                    byte[] fileBuffer = new byte[FileConstants.BufferSize];
                                                    int fileReadBytes;
                                                    long countFileSize = 0;

                                                    while ((fileReadBytes = OutputStream.Read(fileBuffer, 0, FileConstants.BufferSize)) > 0)
                                                    {
                                                        countFileSize += fileReadBytes;
                                                        md5Hasher.TransformBlock(fileBuffer, 0, fileReadBytes, fileBuffer, 0);
                                                    }

                                                    md5Hasher.TransformFinalBlock(FileConstants.EmptyBuffer, 0, 0);
                                                    byte[] newMD5Bytes = md5Hasher.Hash;

                                                    string pathString = CurrentDependencyTree.DependencyFileChange.NewPath.ToString();
                                                    FileInfo uploadInfo = new FileInfo(pathString);
                                                    DateTime newCreationTime = uploadInfo.CreationTimeUtc.DropSubSeconds();
                                                    DateTime newWriteTime = uploadInfo.LastWriteTimeUtc.DropSubSeconds();

                                                    if (newCreationTime.CompareTo(CurrentDependencyTree.DependencyFileChange.Metadata.HashableProperties.CreationTime) != 0 // creation time changed
                                                        || newWriteTime.CompareTo(CurrentDependencyTree.DependencyFileChange.Metadata.HashableProperties.LastTime) != 0 // or last write time changed
                                                        || CurrentDependencyTree.DependencyFileChange.Metadata.HashableProperties.Size == null // or previous size was not set
                                                        || ((long)CurrentDependencyTree.DependencyFileChange.Metadata.HashableProperties.Size) == countFileSize // or size changed
                                                        || !((previousMD5Bytes == null && newMD5Bytes == null)
                                                            || (previousMD5Bytes != null && newMD5Bytes != null && previousMD5Bytes.Length == newMD5Bytes.Length && NativeMethods.memcmp(previousMD5Bytes, newMD5Bytes, new UIntPtr((uint)previousMD5Bytes.Length)) == 0))) // or md5 changed
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

                                                        CLError writeNewMetadataError = ProcessMergeToSQL(new KeyValuePair<FileChange, FileChange>[] { new KeyValuePair<FileChange, FileChange>(CurrentDependencyTree.DependencyFileChange, null) }, false);
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

                                                    md5Hasher.Dispose();
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
                                            OutputFailuresList.Add(new KeyValuePair<bool, FileChange>(false, CurrentDependencyTree.DependencyFileChange));
                                        }
                                        else
                                        {
                                            OutputChangesList.Add(new KeyValuePair<FileChange, FileStream>(CurrentDependencyTree.DependencyFileChange, OutputStream));
                                        }
                                    }
                                }
                            }
                        }

                        CLError queuedChangesSqlError = ProcessMergeToSQL(queuedChangesNeedMergeToSql.Select(currentQueuedChangeToSql => currentQueuedChangeToSql.Key), false);
                        if (queuedChangesSqlError != null)
                        {
                            toReturn += new AggregateException("Error adding QueuedChanges within processing/failed changes dependency tree to SQL", queuedChangesSqlError.GrabExceptions());
                        }
                        foreach (KeyValuePair<KeyValuePair<FileChange, FileChange>, FileChange> mergedToSql in queuedChangesNeedMergeToSql)
                        {
                            try
                            {
                                if (mergedToSql.Key.Key.EventId == 0)
                                {
                                    throw new ArgumentException("Cannot communicate FileChange without EventId; FileChangeSource: QueuedChanges");
                                }
                                else
                                {
                                    mergedToSql.Value.Dispose();
                                    RemoveFileChangeFromQueuedChanges(mergedToSql.Value);
                                }
                            }
                            catch (Exception ex)
                            {
                                toReturn += ex;
                            }
                        }

                        outputChanges = OutputChangesList;
                        outputChangesInError = OutputFailuresList;
                    }
                }
            }
            catch (Exception ex)
            {
                outputChanges = Helpers.DefaultForType<IEnumerable<KeyValuePair<FileChange, FileStream>>>();
                outputChangesInError = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, FileChange>>>();
                toReturn += ex;
            }

            if ((Settings.Instance.TraceType & TraceType.FileChangeFlow) == TraceType.FileChangeFlow)
            {
                Trace.LogFileChangeFlow(Settings.Instance.TraceLocation, Settings.Instance.Udid, Settings.Instance.Uuid, FileChangeFlowEntryPositionInFlow.GrabChangesQueuedChangesAddedToSQL, queuedChangesNeedMergeToSql.Select(currentQueuedChange => ((Func<FileChange, FileChange>)(removeDependencies =>
                    {
                        FileChangeWithDependencies selectedWithoutDependencies;
                        FileChangeWithDependencies.CreateAndInitialize(removeDependencies, null, out selectedWithoutDependencies);
                        return selectedWithoutDependencies;
                    }))(currentQueuedChange.Key.Key)));
                Trace.LogFileChangeFlow(Settings.Instance.TraceLocation, Settings.Instance.Udid, Settings.Instance.Uuid, FileChangeFlowEntryPositionInFlow.GrabChangesOutputChanges, (outputChanges ?? Enumerable.Empty<KeyValuePair<FileChange, FileStream>>()).Select(currentOutputChange => currentOutputChange.Key));
                Trace.LogFileChangeFlow(Settings.Instance.TraceLocation, Settings.Instance.Udid, Settings.Instance.Uuid, FileChangeFlowEntryPositionInFlow.GrabChangesOutputChangesInError, (outputChangesInError ?? Enumerable.Empty<KeyValuePair<bool, FileChange>>()).Select(currentOutputChange => currentOutputChange.Value));
            }

            return toReturn;
        }
        private enum FileChangeSource : byte
        {
            QueuedChanges,
            FailureQueue,
            ProcessingChanges
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
                            AllPaths.Clear();
                        }

                        // protect root directory from changes such as deletion
                        setDirectoryAccessControl(true);

                        // create watcher for all files and folders that aren't renamed at current path
                        FileWatcher = new FileSystemWatcher(CurrentFolderPath);
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
            // Need to implement:
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

        /// <summary>
        /// Combined EventHandler for file and folder changes
        /// </summary>
        /// <param name="sender">FileSystemWatcher</param>
        /// <param name="e">Event arguments for the change</param>
        /// <param name="folderOnly">Value of folder-specificity from routed event</param>
        private void watcher_Changed(object sender, FileSystemEventArgs e, bool folderOnly)
        {
            // Enter read lock of CurrentFolderPath (doesn't lock other threads unless lock is entered for write on rare condition of path changing)
            CurrentFolderPathLocker.EnterReadLock();
            try
            {
                // rebuild filePath from current root path and the relative path portion of the change event
                string newPath = CurrentFolderPath + e.FullPath.Substring(InitialFolderPath.Length);
                // previous path for renames only
                string oldPath;
                // set previous path only if change is a rename
                if ((e.ChangeType & WatcherChangeTypes.Renamed) == WatcherChangeTypes.Renamed)
                {
                    // cast args to the appropriate type containing previous path
                    RenamedEventArgs renamedArgs = (RenamedEventArgs)e;
                    // rebuild oldPath from current root path and the relative path portion of the change event;
                    // should not be a problem pulling the relative path out of the renamed args 'OldFullPath' when the
                    // file was moved from a directory outside the monitored root because move events don't come across as 'Renamed'
                    oldPath = CurrentFolderPath + renamedArgs.OldFullPath.Substring(InitialFolderPath.Length);
                }
                else
                {
                    // no old path for Created/Deleted/Modified events
                    oldPath = null;
                }
                // Processes the file system event against the file data and current file index
                CheckMetadataAgainstFile(newPath, oldPath, e.ChangeType, folderOnly);
            }
            catch
            {
            }
            finally
            {
                // Exit read lock of CurrentFolderPath
                CurrentFolderPathLocker.ExitReadLock();
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
        private void CheckMetadataAgainstFile(string newPath, string oldPath, WatcherChangeTypes changeType, bool folderOnly, bool alreadyHoldingIndexLock = false, GenericHolder<Nullable<KeyValuePair<Action<object>, object>>> startProcessingAction = null)
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

                    // Only process file/folder event if it does not exist or if its FileAttributes does not contain any unwanted attributes
                    // Also ensure if it is a file that the file is not a shortcut
                    if (!exists// file/folder does not exist so no need to check attributes
                        || ((FileAttributes)0 ==// compare bitwise and of FileAttributes and all unwanted attributes to '0'
                            ((isFolder// need to grab FileAttributes based on whether change is on a file or folder
                            ? folder.Attributes// change is on folder, grab folder attributes
                            : file.Attributes)// change is on file, grab file attributes
                                & (FileAttributes.Hidden// ignore hidden files
                                    | FileAttributes.Offline// ignore offline files (data is not available on them)
                                    | FileAttributes.System// ignore system files
                                    | FileAttributes.Temporary// ignore temporary files
                                    ))
                            && (isFolder ? true : !FileIsShortcut(file))))// allow change if it is a folder or if it is a file that is not a shortcut
                    {
                        DateTime lastTime;
                        DateTime creationTime;
                        // set last time and creation time from appropriate info based on whether change is on a folder or file
                        if (isFolder)
                        {
                            lastTime = folder.LastWriteTimeUtc.DropSubSeconds();
                            creationTime = folder.CreationTimeUtc.DropSubSeconds();
                        }
                        // change was not a folder, grab times based on file
                        else
                        {
                            lastTime = file.LastWriteTimeUtc.DropSubSeconds();
                            creationTime = file.CreationTimeUtc.DropSubSeconds();
                        }

                        // most paths modify the list of current indexes, so lock it from other reads/changes
                        lock (AllPaths)
                        {
                            #region file system event, current file status, and current recorded index state flow

                            // for file system events marked as file/folder changes or additions
                            if ((changeType & WatcherChangeTypes.Changed) == WatcherChangeTypes.Changed
                                || (changeType & WatcherChangeTypes.Created) == WatcherChangeTypes.Created)
                            {
                                // if file/folder actually exists
                                if (exists)
                                {
                                    // if index exists at specified path
                                    if (AllPaths.ContainsKey(pathObject))
                                    {
                                        // No need to send modified events for folders
                                        // so check if event is on a file or if folder modifies are not ignored
                                        if (!isFolder
                                            || !IgnoreFolderModifies)
                                        {
                                            // retrieve stored index
                                            FileMetadata previousMetadata = AllPaths[pathObject];
                                            // compare stored index with values from file info
                                            FileMetadata newMetadata = ReplacementMetadataIfDifferent(previousMetadata,
                                                isFolder,
                                                lastTime,
                                                creationTime,
                                                fileLength,
                                                null);
                                            // if new metadata came back after comparison, queue file change for modify
                                            if (newMetadata != null)
                                            {
                                                // replace index at current path
                                                AllPaths[pathObject] = newMetadata;
                                                // queue file change for modify
                                                QueueFileChange(new FileChange(QueuedChanges)
                                                {
                                                    NewPath = pathObject,
                                                    Metadata = newMetadata,
                                                    Type = FileChangeType.Modified
                                                }, startProcessingAction);
                                            }
                                        }
                                    }
                                    // if index did not already exist
                                    else
                                    {
                                        // add new index
                                        AllPaths.Add(pathObject,
                                            new FileMetadata()
                                            {
                                                HashableProperties = new FileMetadataHashableProperties(isFolder,
                                                    lastTime,
                                                    creationTime,
                                                    fileLength)
                                            });
                                        // queue file change for create
                                        QueueFileChange(new FileChange(QueuedChanges)
                                        {
                                            NewPath = pathObject,
                                            Metadata = AllPaths[pathObject],
                                            Type = FileChangeType.Created
                                        }, startProcessingAction);
                                    }
                                }
                                // if file file does not exist, but an index exists
                                else if (AllPaths.ContainsKey(pathObject))
                                {
                                    // queue file change for delete
                                    QueueFileChange(new FileChange(QueuedChanges)
                                    {
                                        NewPath = pathObject,
                                        Metadata = AllPaths[pathObject],
                                        Type = FileChangeType.Deleted
                                    }, startProcessingAction);
                                    // remove index
                                    AllPaths.Remove(pathObject);
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
                                        // recurse once on this current function to process the previous path as a file system modified event
                                        CheckMetadataAgainstFile(oldPath, null, WatcherChangeTypes.Changed, false);
                                    }
                                    // if no file nor folder exists at the previous path and a file or folder does exist at the current path
                                    else if (exists)
                                    {
                                        // set precursor condition for queueing a file change for rename
                                        possibleRename = true;
                                    }
                                    // if no file nor folder exists at either the previous or current path
                                    else
                                    {
                                        // queue file change for delete at previous path
                                        QueueFileChange(new FileChange(QueuedChanges)
                                        {
                                            NewPath = oldPathObject,
                                            Metadata = AllPaths[oldPathObject],
                                            Type = FileChangeType.Deleted
                                        }, startProcessingAction);
                                        // remove index at previous path
                                        AllPaths.Remove(oldPathObject);
                                    }
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
                                                fileLength,
                                                null);
                                            // if new metadata came back after comparison, queue file change for modify
                                            if (newMetadata != null)
                                            {
                                                // replace index at current path
                                                AllPaths[pathObject] = newMetadata;
                                                // queue file change for modify
                                                QueueFileChange(new FileChange(QueuedChanges)
                                                {
                                                    NewPath = pathObject,
                                                    Metadata = newMetadata,
                                                    Type = FileChangeType.Modified
                                                }, startProcessingAction);
                                            }
                                        }
                                    }
                                    // else file does not exist
                                    else
                                    {
                                        // queue file change for delete at new path
                                        QueueFileChange(new FileChange(QueuedChanges)
                                        {
                                            NewPath = pathObject,
                                            Metadata = AllPaths[pathObject],
                                            Type = FileChangeType.Deleted
                                        }, startProcessingAction);
                                        // remove index for new path
                                        AllPaths.Remove(pathObject);

                                        // no need to continue and check possibeRename since it required exists to be true, return now
                                        return;
                                    }
                                    // if precursor condition was set for a file change for rename
                                    // (but an index already exists at the new path)
                                    if (possibleRename)
                                    {
                                        // queue file change for delete at previous path
                                        QueueFileChange(new FileChange(QueuedChanges)
                                        {
                                            NewPath = oldPath,
                                            Metadata = AllPaths[oldPathObject],
                                            Type = FileChangeType.Deleted
                                        }, startProcessingAction);
                                        // remove index at the previous path
                                        AllPaths.Remove(oldPath);
                                    }
                                }
                                // if precursor condition was set for a file change for rename
                                // and an index does not exist at the new path
                                else if (possibleRename)
                                {
                                    // retrieve index at previous path
                                    FileMetadata previousMetadata = AllPaths[oldPath];
                                    // compare stored index from previous path with values from current change
                                    FileMetadata newMetadata = ReplacementMetadataIfDifferent(previousMetadata,
                                        isFolder,
                                        lastTime,
                                        creationTime,
                                        fileLength,
                                        null);
                                    // remove index at the previous path
                                    AllPaths.Remove(oldPath);
                                    // add an index for the current path either from the changed metadata if it exists otherwise the previous metadata
                                    AllPaths.Add(pathObject, newMetadata ?? previousMetadata);
                                    // queue file change for rename (use changed metadata if it exists otherwise the previous metadata)
                                    QueueFileChange(new FileChange(QueuedChanges)
                                    {
                                        NewPath = pathObject,
                                        OldPath = oldPath,
                                        Metadata = newMetadata ?? previousMetadata,
                                        Type = FileChangeType.Renamed
                                    }, startProcessingAction);
                                }
                                // if index does not exist at either the old nor new paths and the file exists
                                else
                                {
                                    // add new index at new path
                                    AllPaths.Add(pathObject,
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
                                        Type = FileChangeType.Created
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
                                    if (AllPaths.ContainsKey(pathObject)
                                        &&
                                        // No need to send modified events for folders
                                        // so check if event is on a file or if folder modifies are not ignored
                                        (!isFolder
                                            || !IgnoreFolderModifies))
                                    {
                                        // retrieve stored index at current path
                                        FileMetadata previousMetadata = AllPaths[pathObject];
                                        // compare stored index with values from file info
                                        FileMetadata newMetadata = ReplacementMetadataIfDifferent(previousMetadata,
                                            isFolder,
                                            lastTime,
                                            creationTime,
                                            fileLength,
                                            null);
                                        // if new metadata came back after comparison, queue file change for modify
                                        if (newMetadata != null)
                                        {
                                            // replace index at current path
                                            AllPaths[pathObject] = newMetadata;
                                            // queue file change for modify
                                            QueueFileChange(new FileChange(QueuedChanges)
                                            {
                                                NewPath = pathObject,
                                                Metadata = newMetadata,
                                                Type = FileChangeType.Modified
                                            }, startProcessingAction);
                                        }
                                    }
                                }
                                // if file or folder does not exist but index exists for current path
                                else if (AllPaths.ContainsKey(pathObject))
                                {
                                    // queue file change for delete
                                    QueueFileChange(new FileChange(QueuedChanges)
                                    {
                                        NewPath = pathObject,
                                        Metadata = AllPaths[pathObject],
                                        Type = FileChangeType.Deleted
                                    }, startProcessingAction);
                                    // remove index
                                    AllPaths.Remove(pathObject);
                                }
                            }

                            #endregion
                        }
                    }

                    // If the current change is on a directory that was created,
                    // need to recursively traverse inner objects to also create
                    if (isFolder
                        && exists
                        && changeType == WatcherChangeTypes.Created)
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
            finally
            {
                if (!alreadyHoldingIndexLock)
                {
                    InitialIndexLocker.ExitReadLock();
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
                    Shell32.Shell shell32 = new Shell32.Shell();
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
                    var lnk = (Shell32.ShellLinkObject)lnkItem.GetLink;
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
        /// <param name="filePath">Path to file or folder</param>
        /// <param name="isFolder">True for folder or false for file</param>
        /// <param name="lastTime">The greater of the times for last accessed and last written for file or folder</param>
        /// <param name="creationTime">Time of creation of file or folder</param>
        /// <param name="size">File size for file or null for folder</param>
        /// <returns>Returns null if a difference was not found, otherwise the new metadata to use</returns>
        private FileMetadata ReplacementMetadataIfDifferent(FileMetadata previousMetadata,
            bool isFolder,
            DateTime lastTime,
            DateTime creationTime,
            Nullable<long> size,
            FilePath targetPath)
        {
            // Segment out the properties that are used for comparison (before recalculating the MD5)
            FileMetadataHashableProperties forCompare = new FileMetadataHashableProperties(isFolder,
                    lastTime,
                    creationTime,
                    size);

            // If metadata hashable properties differ at all, new metadata will be created and returned
            if (!FileMetadataHashableComparer.Default.Equals(previousMetadata.HashableProperties,
                forCompare)
                || (previousMetadata.LinkTargetPath == null && targetPath != null)
                || (previousMetadata.LinkTargetPath != null && targetPath == null)
                || (previousMetadata.LinkTargetPath != null && targetPath != null && !FilePathComparer.Instance.Equals(previousMetadata.LinkTargetPath, targetPath)))
            {
                // metadata change detected
                return new FileMetadata(previousMetadata.RevisionChanger)
                {
                    HashableProperties = forCompare,
                    Revision = previousMetadata.Revision,
                    LinkTargetPath = targetPath
                };
            }
            return null;
        }

        /// <summary>
        /// Insert new file change into a synchronized queue and begin its delay timer for processing
        /// </summary>
        /// <param name="toChange">New file change</param>
        private void QueueFileChange(FileChange toChange, GenericHolder<Nullable<KeyValuePair<Action<object>, object>>> startProcessingAction = null)
        {
            if ((Settings.Instance.TraceType & TraceType.FileChangeFlow) == TraceType.FileChangeFlow)
            {
                Trace.LogFileChangeFlow(Settings.Instance.TraceLocation, Settings.Instance.Udid, Settings.Instance.Uuid, FileChangeFlowEntryPositionInFlow.FileMonitorAddingToQueuedChanges, new FileChange[] { toChange });
            }

            // lock on queue to prevent conflicting updates/reads
            lock (QueuedChanges)
            {
                // define FileChange for rename if a previous change needs to be compared
                FileChange matchedFileChangeForRename;

                // function to move the file change to the metadata-keyed queue and start the delayed processing
                Action<FileChange, GenericHolder<Nullable<KeyValuePair<Action<object>, object>>>> StartDelay = (toDelay, runActionExternal) =>
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
                        runActionExternal.Value = new KeyValuePair<Action<object>, object>(state =>
                            {
                                Tuple<FileChange,
                                    Action<FileChange, object, int>,
                                    int,
                                    int> castState = state as Tuple<FileChange, Action<FileChange, object, int>, int, int>;

                                if (castState != null)
                                {
                                    // start delayed processing of file change
                                    castState.Item1.ProcessAfterDelay(
                                        castState.Item2,// Callback which fires on process timer completion (on a new thread)
                                        null,// Userstate if needed on callback (unused)
                                        castState.Item3,// processing delay to wait for more events on this file
                                        castState.Item4);// number of processing delay resets before it will process the file anyways
                                }
                            }, new Tuple<FileChange,
                                Action<FileChange, object, int>,
                                int,
                                int>(toDelay, // file change to delay-process
                                ProcessFileChange,// Callback which fires on process timer completion (on a new thread)
                                ProcessingDelayInMilliseconds,// processing delay to wait for more events on this file
                                ProcessingDelayMaxResets));// number of processing delay resets before it will process the file anyways
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
                                        if (
                                            // Folder modify events are not useful, so discard the deletion change instead
                                            previousChange.Metadata.HashableProperties.IsFolder

                                            // Also discard the deletion change for files which have been deleted and created again with the same metadata
                                            || QueuedChangesMetadataComparer.Equals(previousChange.Metadata.HashableProperties, toChange.Metadata.HashableProperties))
                                        {
                                            FileChange toCompare;
                                            if (QueuedChangesByMetadata.TryGetValue(previousChange.Metadata.HashableProperties, out toCompare)
                                                && toCompare.Equals(previousChange))
                                            {
                                                QueuedChangesByMetadata.Remove(previousChange.Metadata.HashableProperties);
                                            }
                                            QueuedChanges.Remove(previousChange.NewPath);
                                            previousChange.Dispose();
                                        }
                                        // For files with different metadata, process as a modify
                                        else
                                        {
                                            previousChange.Type = FileChangeType.Modified;
                                            previousChange.Metadata = toChange.Metadata;
                                            previousChange.SetDelayBackToInitialValue();
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
                                        previousChange.Metadata = toChange.Metadata;
                                        previousChange.SetDelayBackToInitialValue();
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
                                                    Metadata = toChange.Metadata
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
                    // FileChange already exists
                    // Instead of starting a new processing delay, update the FileChange information
                    // Then restart the delay timer
                    matchedFileChangeForRename.Type = FileChangeType.Renamed;
                    matchedFileChangeForRename.OldPath = matchedFileChangeForRename.NewPath;
                    matchedFileChangeForRename.NewPath = toChange.NewPath;
                    matchedFileChangeForRename.Metadata = toChange.Metadata;
                    if (QueuedChanges.ContainsKey(matchedFileChangeForRename.OldPath))
                    {
                        QueuedChanges.Remove(matchedFileChangeForRename.OldPath);
                    }
                    QueuedChanges.Add(matchedFileChangeForRename.NewPath,
                        matchedFileChangeForRename);
                    matchedFileChangeForRename.SetDelayBackToInitialValue();
                    // add old/new path pairs for recursive rename processing
                    OldToNewPathRenames[matchedFileChangeForRename.OldPath] = matchedFileChangeForRename.NewPath;
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

        // Comes in on a new thread every time
        /// <summary>
        /// EventHandler for processing a file change after its delay completed
        /// </summary>
        /// <param name="sender">The file change itself</param>
        /// <param name="state">Userstate, if provided before the delayed processing</param>
        /// <param name="remainingOperations">Number of operations remaining across all FileChange (via DelayProcessable)</param>
        private void ProcessFileChange(FileChange sender, object state, int remainingOperations)
        {
            RemoveFileChangeFromQueuedChanges(sender);

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
                                return MergingToSql = false;
                            }
                            return true;
                        }
                    };

                mergeBatch.Add(sender);
                mergeAll.Add(sender);

                do
                {
                    lock (NeedsMergeToSql)
                    {
                        while (NeedsMergeToSql.Count > 0)
                        {
                            FileChange nextMerge = NeedsMergeToSql.Dequeue();
                            mergeBatch.Add(nextMerge);
                            mergeAll.Add(nextMerge);
                        }
                    }

                    CLError mergeError = ProcessMergeToSQL(mergeBatch.Select(currentMerge => new KeyValuePair<FileChange, FileChange>(currentMerge, null)), false);
                    if (mergeError != null)
                    {
                        // forces logging even if the setting is turned off in the severe case since a message box had to appear
                        mergeError.LogErrors(Settings.Instance.ErrorLogLocation, true);
                        MessageBox.Show("An error occurred adding a file system event to the database:" + Environment.NewLine +
                            string.Join(Environment.NewLine,
                                mergeError.GrabExceptions().Select(currentError => (currentError is AggregateException
                                    ? string.Join(Environment.NewLine, ((AggregateException)currentError).Flatten().InnerExceptions.Select(innerError => innerError.Message).ToArray())
                                    : currentError.Message)).ToArray()));
                    }

                    if ((Settings.Instance.TraceType & TraceType.FileChangeFlow) == TraceType.FileChangeFlow)
                    {
                        Trace.LogFileChangeFlow(Settings.Instance.TraceLocation, Settings.Instance.Udid, Settings.Instance.Uuid, FileChangeFlowEntryPositionInFlow.FileMonitorAddingBatchToSQL, mergeBatch);
                    }

                    // clear out batch for merge for next set of remaining operations
                    mergeBatch.Clear();
                }
                while (operationsRemaining()); // flush remaining operations before starting processing timer
                
                lock (QueuesTimer.TimerRunningLocker)
                {
                    foreach (FileChange nextMerge in mergeAll)
                    {
                        if (nextMerge.EventId == 0)
                        {
                            string noEventIdErrorMessage = "EventId was zero on a FileChange to queue to ProcessingChanges: " +
                                nextMerge.ToString() + " " + (nextMerge.NewPath == null ? "nullPath" : nextMerge.NewPath.ToString());

                            // forces logging even if the setting is turned off in the severe case since a message box had to appear
                            ((CLError)new Exception(noEventIdErrorMessage)).LogErrors(Settings.Instance.ErrorLogLocation, true);
                            MessageBox.Show(noEventIdErrorMessage);
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
        private bool RemoveFileChangeFromQueuedChanges(FileChange toRemove)
        {
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
            if (QueuedChangesByMetadata.ContainsKey(toRemove.Metadata.HashableProperties))
            {
                QueuedChangesByMetadata.Remove(toRemove.Metadata.HashableProperties);
            }
            if (QueuedChanges.ContainsKey(toRemove.NewPath)
                && QueuedChanges[toRemove.NewPath] == toRemove)
            {
                return QueuedChanges.Remove(toRemove.NewPath);
            }
            return false;
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
                            (GrabProcessedChanges)this.GrabPreprocessedChanges,
                            this.ProcessMergeToSQL,
                            (Func<IEnumerable<FileChange>, bool, GenericHolder<List<FileChange>>, CLError>)this.AddFileChangesToProcessingQueue,
                            emptyProcessingQueue,
                            this.ProcessCompletedSync,
                            this.GetLastSyncId,
                            (DependencyAssignments)this.AssignDependencies,
                            (Func<string>)this.GetCurrentPath,
                            (Func<FileChange, CLError>)this.ApplySyncFromFileChange,
                            (Func<long, CLError>)this.CompleteSingleEvent,
                            (MetadataByPathAndRevision)this.GetMetadataByPathAndRevision,
                            this.GetDeviceName,
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
                && castState.Length == 15)
            {
                GrabProcessedChanges argOne = castState[0] as GrabProcessedChanges;
                Func<IEnumerable<KeyValuePair<FileChange, FileChange>>, bool, CLError> argTwo = castState[1] as Func<IEnumerable<KeyValuePair<FileChange, FileChange>>, bool, CLError>;
                Func<IEnumerable<FileChange>, bool, GenericHolder<List<FileChange>>, CLError> argThree = castState[2] as Func<IEnumerable<FileChange>, bool, GenericHolder<List<FileChange>>, CLError>;
                Nullable<bool> argFourNullable = castState[3] as Nullable<bool>;
                Func<string, IEnumerable<long>, string, CLError> argFive = castState[4] as Func<string, IEnumerable<long>, string, CLError>;
                Func<string> argSix = castState[5] as Func<string>;
                DependencyAssignments argSeven = castState[6] as DependencyAssignments;
                Func<string> argEight = castState[7] as Func<string>;
                Func<FileChange, CLError> argNine = castState[8] as Func<FileChange, CLError>;
                Func<long, CLError> argTen = castState[9] as Func<long, CLError>;
                MetadataByPathAndRevision argEleven = castState[10] as MetadataByPathAndRevision;
                Func<string> argTwelve = castState[11] as Func<string>;

                Func<GrabProcessedChanges,
                    Func<IEnumerable<KeyValuePair<FileChange, FileChange>>, bool, CLError>,
                    Func<IEnumerable<FileChange>, bool, GenericHolder<List<FileChange>>, CLError>,
                    bool,
                    Func<string, IEnumerable<long>, string, CLError>,
                    Func<string>,
                    DependencyAssignments,
                    Func<string>,
                    Func<FileChange, CLError>,
                    Func<long, CLError>,
                    MetadataByPathAndRevision,
                    Func<string>,
                    CLError> RunSyncRun = castState[12] as Func<GrabProcessedChanges, Func<IEnumerable<KeyValuePair<FileChange, FileChange>>, bool, CLError>, Func<IEnumerable<FileChange>, bool, GenericHolder<List<FileChange>>, CLError>, bool, Func<string, IEnumerable<long>, string, CLError>, Func<string>, DependencyAssignments, Func<string>, Func<FileChange, CLError>, Func<long, CLError>, MetadataByPathAndRevision, Func<string>, CLError>;

                GenericHolder<bool> RunLocker = castState[13] as GenericHolder<bool>;
                GenericHolder<bool> NextRunQueued = castState[14] as GenericHolder<bool>;

                if (argFourNullable != null
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
                        RunSyncRun(argOne,
                            argTwo,
                            argThree,
                            (bool)argFourNullable,
                            argFive,
                            argSix,
                            argSeven,
                            argEight,
                            argNine,
                            argTen,
                            argEleven,
                            argTwelve);
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