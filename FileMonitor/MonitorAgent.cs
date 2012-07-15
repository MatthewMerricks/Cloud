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
using System.Threading;
using System.Threading.Tasks;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
using CloudApiPublic.Support;
using System.Globalization;
// the following linq namespace is used only if the optional initialization parameter for processing logging is passed as true
using System.Xml.Linq;
using System.Security.Cryptography;

namespace FileMonitor
{
    public class MonitorAgent : IDisposable
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
                status = (MonitorRunning)Helpers.DefaultForType(typeof(MonitorRunning));
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
        /// <summary>
        /// Retrieves locker for the initial indexing
        /// (before file monitor changes process)
        /// </summary>
        /// <param name="initialIndexLocker">Returned index locker</param>
        /// <returns>Error while returning locker, if any</returns>
        public CLError GetInitialIndexLocker(out ReaderWriterLockSlim initialIndexLocker)
        {
            try
            {
                initialIndexLocker = InitialIndexLocker;
            }
            catch (Exception ex)
            {
                initialIndexLocker = (ReaderWriterLockSlim)Helpers.DefaultForType(typeof(ReaderWriterLockSlim));
                return ex;
            }
            return null;
        }

        private FileChange[] _currentFileChanges;
        public FileChange[] CurrentFileChanges
        {
            get { return _currentFileChanges; }
            private set { _currentFileChanges = value; }
        }


        private ReaderWriterLockSlim InitialIndexLocker = new ReaderWriterLockSlim();
        #endregion

        #region private fields and property
        // stores the optional FileChange queueing callback intialization parameter
        private Action<MonitorAgent, FileChange> OnQueueing;

        // stores the callback used to process a group of events.  Passed via an intialization parameter
        private Action<Dictionary<string, object>> OnProcessEventGroupCallback;

        // stores the callback used to add the processed event to the SQL index
        /// <summary>
        /// First parameter is merged event, second parameter is event to remove
        /// </summary>
        private Func<FileChange, FileChange, CLError> ProcessMergeToSQL;

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

        private Queue<FileChange> ProcessingChanges = new Queue<FileChange>();
        /// <summary>
        /// Class to handle queueing up processing changes on a configurable timer,
        /// must be externally locked on property TimerRunningLocker for all access
        /// </summary>
        private class ProcessingQueuesTimer
        {
            public bool TimerRunning
            {
                get
                {
                    return _timerRunning;
                }
            }
            private bool _timerRunning = false;
            public readonly object TimerRunningLocker = new object();
            private Action OnTimeout;
            private int MillisecondTime;

            private ManualResetEvent SleepEvent = new ManualResetEvent(false);

            public ProcessingQueuesTimer(Action onTimeout, int millisecondTime)
            {
                this.OnTimeout = onTimeout;
                this.MillisecondTime = millisecondTime;
            }

            public void StartTimerIfNotRunning()
            {
                if (!_timerRunning)
                {
                    _timerRunning = true;
                    (new Thread(() =>
                        {
                            bool SleepEventNeedsReset = SleepEvent.WaitOne(this.MillisecondTime);
                            lock (TimerRunningLocker)
                            {
                                if (SleepEventNeedsReset)
                                {
                                    SleepEvent.Reset();
                                }
                                _timerRunning = false;
                                OnTimeout();
                            }
                        })).Start();
                }
            }

            public void TriggerTimerCompletionImmediately()
            {
                if (_timerRunning)
                {
                    SleepEvent.Set();
                }
                else
                {
                    OnTimeout();
                }
            }
        }
        // Field to store timer for queue processing,
        // initialized on construction
        private ProcessingQueuesTimer QueuesTimer;

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
        /// <param name="onProcessEventGroupCallback">action to be executed when a group of events is to be processed</param>
        /// <param name="onQueueingCallback">(optional) action to be executed evertime a FileChange would be queued for processing</param>
        /// <param name="logProcessing">(optional) if set, logs FileChange objects when their processing callback fires</param>
        /// <returns>Returns any error that occurred if there was one</returns>
        public static CLError CreateNewAndInitialize(string folderPath,
            out MonitorAgent newAgent,
            Action<Dictionary<string, object>> onProcessEventGroupCallback,
            Func<FileChange, FileChange, CLError> onProcessMergeToSQL,
            Action<MonitorAgent, FileChange> onQueueingCallback = null,
            bool logProcessing = false)
        {
            try
            {
                newAgent = new MonitorAgent();
            }
            catch (Exception ex)
            {
                newAgent = (MonitorAgent)Helpers.DefaultForType(typeof(MonitorAgent));
                return ex;
            }
            try
            {
                if (string.IsNullOrEmpty(folderPath))
                    throw new Exception("Folder path cannot be null nor empty");
                DirectoryInfo folderInfo = new DirectoryInfo(folderPath);
                if (!folderInfo.Exists)
                    throw new Exception("Folder not found at provided folder path");

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
                newAgent.OnProcessEventGroupCallback = onProcessEventGroupCallback;
                newAgent.ProcessMergeToSQL = onProcessMergeToSQL;
                newAgent.LogProcessingFileChanges = logProcessing;

                // assign timer object that is used for processing the FileChange queues in batches
                newAgent.QueuesTimer = new ProcessingQueuesTimer(newAgent.ProcessQueuesAfterTimer,
                    1000);// Collect items in queue for 1 second before batch processing
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        private MonitorAgent() { }

        #region public methods
        /// <summary>
        /// Starts the queue timer to start sync processing,
        /// if it is not already started for other events
        /// </summary>
        /// <returns>Returns an error that occurred while starting the timer, if any</returns>
        public CLError FireSimulatedPushNotification()
        {
            try
            {
                QueuesTimer.StartTimerIfNotRunning();
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        ///// <summary>
        ///// Notify completion of indexing;
        ///// sets IsInitialIndex to false,
        ///// combines index with queued changes,
        ///// and processes changes
        ///// </summary>
        ///// <returns></returns>
        public void BeginProcessing(IEnumerable<KeyValuePair<FilePath, FileMetadata>> initialList, IEnumerable<FileChange> newChanges = null)
        {
            // Locks all new file system events from being processed until the initial index is processed,
            // afterwhich they will no longer queue up and instead process normally going forward
            InitialIndexLocker.EnterWriteLock();

            try
            {
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

                                QueueFileChange(new FileChange(QueuedChanges)
                                    {
                                        NewPath = currentChange.NewPath,
                                        OldPath = currentChange.OldPath,
                                        Metadata = currentChange.Metadata,
                                        Type = currentChange.Type,
                                        DoNotAddToSQLIndex = currentChange.DoNotAddToSQLIndex
                                    });
                            }
                        }

                        // If there were no file changes that will trigger a sync later,
                        // then trigger it now as an initial sync with an empty dictionary
                        if (triggerSyncWithNoChanges)
                        {
                            this.OnProcessEventGroupCallback(new Dictionary<string, object>());
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

                    ChangesQueueHolder currentChange = ChangesQueueForInitialIndexing.Dequeue();
                    CheckMetadataAgainstFile(currentChange.newPath,
                        currentChange.oldPath,
                        currentChange.changeType,
                        currentChange.folderOnly,
                        true);
                }

                // null the pointer for the initial index queue so it can be cleared from memory
                ChangesQueueForInitialIndexing = null;
            }
            finally
            {
                InitialIndexLocker.ExitWriteLock();
            }
        }

        /// <summary>
        /// Method to be called within the context of the main lock of the Sync service
        /// which locks the changed files, updates metadata, and outputs a sorted FileChange array for processing
        /// </summary>
        /// <param name="inputChanges">Original array of FileChanges</param>
        /// <param name="outputChanges">Output array of FileChanges to process</param>
        /// <param name="outputChangesInError">Output array of FileChanges with observed errors for requeueing, may be empty but never null</param>
        /// <returns>Returns (an) error(s) that occurred finalizing the FileChange array, if any</returns>
        public CLError ProcessFileListForSyncProcessing(IEnumerable<FileChange> inputChanges,
            out KeyValuePair<FileChange, FileStream>[] outputChanges,
            out FileChange[] outputChangesInError)
        {
            // error collection will be appended with every item in error
            CLError errorCollection = null;

            try
            {
                #region assert parameters
                // base function of method is to process inputChanges,
                // a null input is a catastrophic failure
                if (inputChanges == null)
                {
                    throw new NullReferenceException("inputChanges cannot be null");
                }
                #endregion

                #region local variables including common actions
                // copy input changes into static array
                FileChange[] inputChangesArray = inputChanges.ToArray();

                // counter for maintaining order of input changes
                int changeCounter = 0;

                // sorted categories of FileChanges keyed by FilePath and whose value contains a counter int to rebuild the order within each sorted group;
                // remaining part of value includes the FileChange itself and possibly a FileStream for creation or modification of files
                Dictionary<FilePath, KeyValuePair<int, FileChange>> folderCreations = new Dictionary<FilePath, KeyValuePair<int, FileChange>>(FilePathComparer.Instance);
                Dictionary<FilePath, Dictionary<FilePath, KeyValuePair<int, FileChange>>> folderRenames = new Dictionary<FilePath, Dictionary<FilePath, KeyValuePair<int, FileChange>>>(FilePathComparer.Instance);
                Dictionary<FilePath, Dictionary<FilePath, KeyValuePair<int, FileChange>>> fileRenames = new Dictionary<FilePath, Dictionary<FilePath, KeyValuePair<int, FileChange>>>(FilePathComparer.Instance);
                Dictionary<FilePath, KeyValuePair<int, KeyValuePair<FileChange, FileStream>>> fileCreationsOrModifications = new Dictionary<FilePath, KeyValuePair<int, KeyValuePair<FileChange, FileStream>>>(FilePathComparer.Instance);
                Dictionary<FilePath, KeyValuePair<int, FileChange>> fileDeletions = new Dictionary<FilePath, KeyValuePair<int, FileChange>>(FilePathComparer.Instance);
                Dictionary<FilePath, KeyValuePair<int, FileChange>> folderDeletions = new Dictionary<FilePath, KeyValuePair<int, FileChange>>(FilePathComparer.Instance);

                // create list to store FileChanges in error for direct output
                List<FileChange> changesInError = new List<FileChange>();

                // create list to store FileChanges along with corresponding FileStreams for direct output
                List<KeyValuePair<FileChange, FileStream>> outputChangesList = new List<KeyValuePair<FileChange, FileStream>>();

                // define an action which scans through rename changes going forwards (until it finds a matching delete),
                // rebuilding the NewPath of a FileChange everytime the rename change's OldPath matches
                Action<FileChange, int> updateFileChangePathFromRenames = (toUpdate, toUpdateIndex) =>
                    {
                        // loop through changes starting one after the current change to check
                        for (int renameChangeIndex = toUpdateIndex + 1; renameChangeIndex < inputChangesArray.Length; renameChangeIndex++)
                        {
                            // pull next FileChange for checking
                            FileChange possibleRenameChange = inputChangesArray[renameChangeIndex];

                            // switch on type of change
                            switch (possibleRenameChange.Type)
                            {
                                // for create/modify, do nothing and keep checking
                                case FileChangeType.Created:
                                case FileChangeType.Modified:
                                    break;
                                // for delete, if it matches path return immediately (stop searching)
                                case FileChangeType.Deleted:
                                    if (FilePathComparer.Instance.Equals(possibleRenameChange.NewPath, toUpdate.NewPath))
                                    {
                                        return;
                                    }
                                    break;
                                // for rename, if OldPath matches, rebuild current change's NewPath based on rename operation, keep checking
                                case FileChangeType.Renamed:
                                    // to compare, find where the current chane's NewPath overlaps with the rename change's OldPath
                                    // and see if this overlap is the rename's OldPath itself
                                    if (FilePathComparer.Instance.Equals(toUpdate.NewPath.FindOverlappingPath(possibleRenameChange.OldPath), possibleRenameChange.OldPath))
                                    {
                                        // matching rename change found,
                                        // current change's NewPath needs to be rebuilt off the rename change

                                        // child of path of perfect overlap with rename change's OldPath
                                        // (whose parent will be replaced by the change of the rename's NewPath
                                        FilePath renamedOverlapChild = toUpdate.NewPath;
                                        // variable for recursive checking against the rename's OldPath
                                        FilePath renamedOverlap = renamedOverlapChild.Parent;

                                        // loop till recursing parent of current path level is null
                                        while (renamedOverlap != null)
                                        {
                                            // when the rename's OldPath matches the current recursive path parent level,
                                            // replace the child's parent with the rename's NewPath and break out of the checking loop
                                            if (FilePathComparer.Instance.Equals(renamedOverlap, possibleRenameChange.OldPath))
                                            {
                                                renamedOverlapChild.Parent = possibleRenameChange.NewPath;
                                                break;
                                            }

                                            // set recursing path variables one level higher
                                            renamedOverlapChild = renamedOverlap;
                                            renamedOverlap = renamedOverlap.Parent;
                                        }

                                        // since the path of the current FileChange was changed, the database Event needs to be updated
                                        CLError mergeError = ProcessMergeToSQL(toUpdate, null);
                                        // if an error occurs during the SQL update, pull out the Exception object and rethrow it
                                        if (mergeError != null)
                                        {
                                            throw (Exception)mergeError.errorInfo[CLError.ErrorInfo_Exception];
                                        }
                                    }
                                    break;
                            }
                        }
                    };
                #endregion

                #region sort and merge input changes
                // loop for all the indexes in the array of input changes
                for (int inputChangeIndex = 0; inputChangeIndex < inputChangesArray.Length; inputChangeIndex++)
                {
                    // pull out the current input change
                    FileChange currentInputChange = inputChangesArray[inputChangeIndex];
                    try
                    {
                        // split on whether the change is a file or folder
                        // and on the type of change

                        // if change represents a folder
                        if (currentInputChange.Metadata.HashableProperties.IsFolder)
                        {
                            // switch on type of change
                            switch (currentInputChange.Type)
                            {
                                case FileChangeType.Created:
                                    // folder creations require the rename path rebuild;
                                    // then filter/merge for duplicates and add to the appropropriate output dictionary

                                    updateFileChangePathFromRenames(currentInputChange, inputChangeIndex);
                                    if (folderCreations.ContainsKey(currentInputChange.NewPath))
                                    {
                                        ProcessMergeToSQL(currentInputChange, folderCreations[currentInputChange.NewPath].Value);
                                        folderCreations.Remove(currentInputChange.NewPath);
                                    }
                                    folderCreations.Add(currentInputChange.NewPath,
                                        new KeyValuePair<int, FileChange>(changeCounter++, currentInputChange));
                                    break;
                                case FileChangeType.Deleted:
                                    // filter/merge for duplicates and add to the appropropriate output dictionary

                                    if (folderDeletions.ContainsKey(currentInputChange.NewPath))
                                    {
                                        ProcessMergeToSQL(currentInputChange, folderDeletions[currentInputChange.NewPath].Value);
                                        folderDeletions.Remove(currentInputChange.NewPath);
                                    }
                                    folderDeletions.Add(currentInputChange.NewPath,
                                        new KeyValuePair<int, FileChange>(changeCounter++, currentInputChange));
                                    break;
                                case FileChangeType.Modified:
                                    // we do not observe folder modification events since they mean nothing to the server

                                    errorCollection += new Exception("Directory modify event found. It is not supposed to be recorded.");
                                    break;
                                case FileChangeType.Renamed:
                                    // filter/merge for duplicates and add to the appropropriate output dictionary

                                    Dictionary<FilePath, KeyValuePair<int, FileChange>> oldPathDict = null;

                                    if (folderRenames.ContainsKey(currentInputChange.OldPath))
                                    {
                                        oldPathDict = folderRenames[currentInputChange.OldPath];

                                        if (oldPathDict.ContainsKey(currentInputChange.NewPath))
                                        {
                                            ProcessMergeToSQL(currentInputChange, oldPathDict[currentInputChange.NewPath].Value);
                                            oldPathDict.Remove(currentInputChange.NewPath);
                                        }
                                    }

                                    if (oldPathDict == null)
                                    {
                                        oldPathDict = new Dictionary<FilePath, KeyValuePair<int, FileChange>>(FilePathComparer.Instance);
                                        folderRenames.Add(currentInputChange.OldPath, oldPathDict);
                                    }

                                    oldPathDict.Add(currentInputChange.NewPath,
                                        new KeyValuePair<int, FileChange>(changeCounter++, currentInputChange));
                                    break;
                            }
                        }
                        // else if change does not represent a folder (instead represents a file)
                        else
                        {
                            // switch on type of change
                            switch (currentInputChange.Type)
                            {
                                case FileChangeType.Created:
                                case FileChangeType.Modified:
                                    // file creations or modifications require the rename path rebuild;
                                    // then filter/merge for duplicates;
                                    // We will open a locked FileStream which will be maintained throughout the Sync service process,
                                    // and add this stream along with the file change to the appropropriate output dictionary

                                    updateFileChangePathFromRenames(currentInputChange, inputChangeIndex);

                                    FileStream inputChangeStream = null;

                                    if (fileCreationsOrModifications.ContainsKey(currentInputChange.NewPath))
                                    {
                                        KeyValuePair<int, KeyValuePair<FileChange, FileStream>> previousFileChange = fileCreationsOrModifications[currentInputChange.NewPath];
                                        inputChangeStream = previousFileChange.Value.Value;

                                        if (previousFileChange.Value.Key.Type == FileChangeType.Created
                                            && currentInputChange.Type == FileChangeType.Modified)
                                        {
                                            currentInputChange.Type = FileChangeType.Created;
                                        }
                                        ProcessMergeToSQL(currentInputChange, previousFileChange.Value.Key);
                                        fileCreationsOrModifications.Remove(currentInputChange.NewPath);
                                    }
                                    if (inputChangeStream == null)
                                    {
                                        try
                                        {
                                            inputChangeStream = new FileStream(currentInputChange.NewPath.ToString(),
                                                FileMode.Open,
                                                FileAccess.Read,
                                                FileShare.Read);// Lock all other processes from writing during sync
                                        }
                                        catch (FileNotFoundException)
                                        {
                                            // This logic will be changed!
                                            // the FileNotFoundExceptions should increment a counter up to a certain point before punting;
                                            // if that counter gets incremented, this file change should be added back to the top of the next queue to process
                                            // (the reasoning is that a rename change for the current file change's NewPath might be in the next queue to process,
                                            //  prevents a problem for false positives on file existance at the previous path since they might correlate to a later file creation event)

                                            ProcessMergeToSQL(null, currentInputChange);
                                            break;
                                        }
                                        catch
                                        {
                                            throw;
                                        }
                                    }
                                    fileCreationsOrModifications.Add(currentInputChange.NewPath,
                                        new KeyValuePair<int, KeyValuePair<FileChange, FileStream>>(changeCounter++,
                                            new KeyValuePair<FileChange, FileStream>(currentInputChange,
                                                inputChangeStream)));
                                    break;
                                case FileChangeType.Deleted:
                                    // filter/merge for duplicates and add to the appropropriate output dictionary

                                    if (fileDeletions.ContainsKey(currentInputChange.NewPath))
                                    {
                                        ProcessMergeToSQL(currentInputChange, fileDeletions[currentInputChange.NewPath].Value);
                                        fileDeletions.Remove(currentInputChange.NewPath);
                                    }
                                    fileDeletions.Add(currentInputChange.NewPath,
                                        new KeyValuePair<int, FileChange>(changeCounter++,
                                            currentInputChange));
                                    break;
                                case FileChangeType.Renamed:
                                    // filter/merge for duplicates and add to the appropropriate output dictionary

                                    Dictionary<FilePath, KeyValuePair<int, FileChange>> oldPathDict = null;

                                    if (fileRenames.ContainsKey(currentInputChange.OldPath))
                                    {
                                        oldPathDict = fileRenames[currentInputChange.OldPath];

                                        if (oldPathDict.ContainsKey(currentInputChange.NewPath))
                                        {
                                            ProcessMergeToSQL(currentInputChange, oldPathDict[currentInputChange.NewPath].Value);
                                            oldPathDict.Remove(currentInputChange.NewPath);
                                        }
                                    }

                                    if (oldPathDict == null)
                                    {
                                        oldPathDict = new Dictionary<FilePath, KeyValuePair<int, FileChange>>(FilePathComparer.Instance);
                                        fileRenames.Add(currentInputChange.OldPath, oldPathDict);
                                    }

                                    oldPathDict.Add(currentInputChange.NewPath,
                                        new KeyValuePair<int, FileChange>(changeCounter++, currentInputChange));
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // an error occurred for a specific input change;
                        // store the change in error and append the exception to return;
                        // processing will continue

                        changesInError.Add(currentInputChange);
                        errorCollection += ex;
                    }
                }
                #endregion

                #region combine the sorted file change lists and finalize file metadata (+MD5)
                // concatenate all the sorted lists in the appropriate order with each section
                // internally ordered based on their original order in the input changes
                // (which was recorded previously by the incrementing value of changeCounter)
                foreach (KeyValuePair<FileChange, FileStream> currentChangeToOutput in

                    // first take all the folder creations
                    folderCreations.Values
                    .OrderBy(currentChange => currentChange.Key)
                    .Select(currentChange => new KeyValuePair<FileChange, FileStream>(currentChange.Value, null))

                    // next take all the folder renames
                    .Concat(folderRenames.Values
                        .SelectMany(currentSetOfChanges => currentSetOfChanges.Values)
                        .OrderBy(currentChange => currentChange.Key)
                        .Select(currentChange => new KeyValuePair<FileChange, FileStream>(currentChange.Value, null)))

                    // next take all the file renames
                    .Concat(fileRenames.Values
                        .SelectMany(currentSetOfChanges => currentSetOfChanges.Values)
                        .OrderBy(currentChange => currentChange.Key)
                        .Select(currentChange => new KeyValuePair<FileChange, FileStream>(currentChange.Value, null)))

                    // next take all the file creations and file modifications
                    .Concat(fileCreationsOrModifications.Values
                        .OrderBy(currentChange => currentChange.Key)
                        .Select(currentChange => currentChange.Value))

                    // next take all the file deletions
                    .Concat(fileDeletions.Values
                        .OrderBy(currentChange => currentChange.Key)
                        .Select(currentChange => new KeyValuePair<FileChange, FileStream>(currentChange.Value, null)))

                    // lastly take all the folder deletions
                    .Concat(folderDeletions.Values
                        .OrderBy(currentChange => currentChange.Key)
                        .Select(currentChange => new KeyValuePair<FileChange, FileStream>(currentChange.Value, null))))
                {
                    try
                    {
                        // if the current change represents a folder,
                        // then there is nothing else to check for so just add to the output list
                        if (currentChangeToOutput.Key.Metadata.HashableProperties.IsFolder)
                        {
                            outputChangesList.Add(currentChangeToOutput);
                        }
                        // else if the current change does not represent a folder (represents a file),
                        // then begin checks specific to files
                        else
                        {
                            // switch on type of change for the current file
                            switch (currentChangeToOutput.Key.Type)
                            {
                                case FileChangeType.Deleted:
                                    // The current change is a file deletion;
                                    // if the file actually exists at the deletion's NewPath,
                                    // then punt the invalid change
                                    if (File.Exists(currentChangeToOutput.Key.NewPath.ToString()))
                                    {
                                        ProcessMergeToSQL(null, currentChangeToOutput.Key);
                                    }
                                    // else if the file does not exist at the deletion's NewPath,
                                    // then the deletion is valid and should be added to the output list
                                    else
                                    {
                                        outputChangesList.Add(currentChangeToOutput);
                                    }
                                    break;
                                case FileChangeType.Created:
                                case FileChangeType.Modified:
                                    // The current change is either a file creation or a file modification;
                                    // We will immediately read through the FileStream once to record the MD5 hash while recording the file size for comparison;
                                    // We seek back to the start of the FileStream so it can be reread from the beginning;
                                    // If any file metadata has changed on the file then update the database event accordingly;
                                    // Add the file change to the output list

                                    MD5 md5Hasher = MD5.Create();

                                    try
                                    {
                                        byte[] fileBuffer = new byte[FileConstants.BufferSize];
                                        int fileReadBytes;
                                        long countFileSize = 0;

                                        while ((fileReadBytes = currentChangeToOutput.Value.Read(fileBuffer, 0, FileConstants.BufferSize)) > 0)
                                        {
                                            countFileSize += fileReadBytes;
                                            md5Hasher.TransformBlock(fileBuffer, 0, fileReadBytes, fileBuffer, 0);
                                        }

                                        md5Hasher.TransformFinalBlock(FileConstants.EmptyBuffer, 0, 0);
                                        currentChangeToOutput.Key.SetMD5(md5Hasher.Hash);

                                        string filePathString = currentChangeToOutput.Key.NewPath.ToString();
                                        DateTime currentLastAccess = File.GetLastAccessTimeUtc(filePathString);
                                        DateTime currentLastWrite = File.GetLastWriteTimeUtc(filePathString);
                                        DateTime currentLastTime = DateTime.Compare(currentLastAccess, currentLastWrite) > 0
                                            ? currentLastAccess
                                            : currentLastWrite;
                                        DateTime currentCreationTime = File.GetCreationTime(filePathString);
                                        long currentSize = countFileSize;

                                        if (currentLastTime.CompareTo(currentChangeToOutput.Key.Metadata.HashableProperties.LastTime) != 0
                                            || currentCreationTime.CompareTo(currentChangeToOutput.Key.Metadata.HashableProperties.CreationTime) != 0
                                            || currentSize != (long)currentChangeToOutput.Key.Metadata.HashableProperties.Size)
                                        {
                                            currentChangeToOutput.Key.Metadata.HashableProperties = new FileMetadataHashableProperties(false,
                                                currentLastTime,
                                                currentCreationTime,
                                                currentSize);
                                            ProcessMergeToSQL(currentChangeToOutput.Key, null);
                                        }

                                        currentChangeToOutput.Value.Seek(0, SeekOrigin.Begin);
                                    }
                                    finally
                                    {
                                        md5Hasher.Dispose();
                                    }

                                    outputChangesList.Add(currentChangeToOutput);
                                    break;
                                case FileChangeType.Renamed:
                                    outputChangesList.Add(currentChangeToOutput);
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // an error occurred for a specific input change;
                        // store the change in error and append the exception to return;
                        // processing will continue

                        changesInError.Add(currentChangeToOutput.Key);
                        errorCollection += ex;
                    }
                }
                #endregion

                // set the output variables by creating array copies from the corresponding local lists
                outputChanges = outputChangesList.ToArray();
                outputChangesInError = changesInError.ToArray();

                #region optional logging
                // if optional initialization parameter for logging was passed as true, log an xml file describing the processed FileChange
                if (LogProcessingFileChanges)
                {
                    foreach (KeyValuePair<FileChange, FileStream> currentChange in outputChanges)
                    {
                        string currentChangeMD5;
                        CLError retrieveMD5StringError = currentChange.Key.GetMD5LowercaseString(out currentChangeMD5);

                        //<FileChange>
                        //  <NewPath>[path for current change]</NewPath>
                        //
                        //  <!--Only present if the previous path exists:-->
                        //  <OldPath>[path for previous location for moves/renames]</OldPath>
                        //
                        //  <IsFolder>[true for folders, false for files]</IsFolder>
                        //  <Type>[type of change that occurred]</Type>
                        //  <LastTime>[number of ticks from the DateTime of when the file/folder was last changed]</LastTime>
                        //  <CreationTime>[number of ticks from the DateTime of when the file/folder was created]</CreationTime>
                        //
                        //  <!--Only present for file creations/file modifications:-->
                        //  <MD5>[lowercase, non-seperated hexadecimal string of MD5 hash]</MD5>
                        //
                        //</FileChange>
                        AppendFileChangeProcessedLogXmlString(new XElement("FileChange",
                            new XElement("NewPath", new XText(currentChange.Key.NewPath.ToString())),
                            currentChange.Key.OldPath == null ? null : new XElement("OldPath", new XText(currentChange.Key.OldPath.ToString())),
                            new XElement("IsFolder", new XText(currentChange.Key.Metadata.HashableProperties.IsFolder.ToString())),
                            new XElement("Type", new XText(currentChange.Key.Type.ToString())),
                            new XElement("LastTime", new XText(currentChange.Key.Metadata.HashableProperties.LastTime.Ticks.ToString())),
                            new XElement("CreationTime", new XText(currentChange.Key.Metadata.HashableProperties.CreationTime.Ticks.ToString())),
                            currentChange.Key.Metadata.HashableProperties.Size == null ? null : new XElement("Size", new XText(currentChange.Key.Metadata.HashableProperties.Size.Value.ToString())),
                            retrieveMD5StringError == null
                                ? (currentChangeMD5 == null ? null : new XElement("MD5", new XText(currentChangeMD5)))
                                : new XElement("MD5", new XText(retrieveMD5StringError.errorDescription)))
                            .ToString() + Environment.NewLine);
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                // a catastrophic error occurred;
                // append the exception to return;
                // processing of further items will cease and the output lists will be set to default (null)

                outputChanges = (KeyValuePair<FileChange, FileStream>[])Helpers.DefaultForType(typeof(KeyValuePair<FileChange, FileStream>[]));
                outputChangesInError = (FileChange[])Helpers.DefaultForType(typeof(FileChange[]));
                return errorCollection + ex;
            }
            // return all recorded erro
            return errorCollection;
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
                status = (MonitorStatus)Helpers.DefaultForType(typeof(MonitorStatus));
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
                status = (MonitorStatus)Helpers.DefaultForType(typeof(MonitorStatus));
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
        /// Find a current FSM event via its eventId.
        /// </summary>
        /// <param name="relativePath">The path to search for.  It starts with "/" (for the cloud path root),
        /// uses "/" path separators, and is in lower case.
        /// </param>
        /// <returns>FileChange.  The FileChange found, or null.</returns>
        public FileChange FindFileChangeByPath(string relativePath)
        {
            FileChange returnedChange = null;
            try
            {
                // lock on current object for changing RunningStatus so it cannot be stopped/started simultaneously
                lock (this)
                {
                    // Search the current file changes for this event.
                    foreach (FileChange change in CurrentFileChanges)
                    {
                        string changeName = "/" + change.NewPath.Name;
                        if (changeName.Equals(relativePath, StringComparison.InvariantCulture))
                        {
                            returnedChange = change;
                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return returnedChange;
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
        #region IDisposable member
        void IDisposable.Dispose()
        {
            // lock on current object for changing RunningStatus so it cannot be stopped/started simultaneously
            lock (this)
            {
                // monitor is now set as disposed which will produce errors if startup is called later
                Disposed = true;
            }
            // cleanup FileSystemWatchers
            StopWatchers();
        }
        #endregion
        #endregion  

        #region private methods

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
        private void CheckMetadataAgainstFile(string newPath, string oldPath, WatcherChangeTypes changeType, bool folderOnly, bool alreadyHoldingIndexLock = false)
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
                            // last time is the greater of the the last access time and the last write time
                            lastTime = DateTime.Compare(folder.LastAccessTimeUtc, folder.LastWriteTimeUtc) > 0
                                ? folder.LastAccessTimeUtc
                                : folder.LastWriteTimeUtc;
                            // creation time is pulled directly
                            creationTime = folder.CreationTimeUtc;
                        }
                        // change was not a folder, grab times based on file
                        else
                        {
                            // last time is the greater of the the last access time and the last write time
                            lastTime = DateTime.Compare(file.LastAccessTimeUtc, file.LastWriteTimeUtc) > 0
                                ? file.LastAccessTimeUtc
                                : file.LastWriteTimeUtc;
                            // creation time is pulled directly
                            creationTime = file.CreationTimeUtc;
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
                                                fileLength);
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
                                                });
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
                                        });
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
                                    });
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
                                        });
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
                                                fileLength);
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
                                                });
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
                                        });
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
                                        });
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
                                        fileLength);
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
                                    });
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
                                    });
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
                                            fileLength);
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
                                            });
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
                                    });
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
            Nullable<long> size)
        {
            // Segment out the properties that are used for comparison (before recalculating the MD5)
            FileMetadataHashableProperties forCompare = new FileMetadataHashableProperties(isFolder,
                    lastTime,
                    creationTime,
                    size);

            // If metadata hashable properties differ at all, new metadata will be created and returned
            if (!FileMetadataHashableComparer.Default.Equals(previousMetadata.HashableProperties,
                forCompare))
            {
                // metadata change detected
                return new FileMetadata()
                {
                    HashableProperties = forCompare
                };
            }
            return null;
        }

        /// <summary>
        /// Insert new file change into a synchronized queue and begin its delay timer for processing
        /// </summary>
        /// <param name="toChange">New file change</param>
        private void QueueFileChange(FileChange toChange)
        {
            // lock on queue to prevent conflicting updates/reads
            lock (QueuedChanges)
            {
                // define FileChange for rename if a previous change needs to be compared
                FileChange matchedFileChangeForRename;

                // function to move the file change to the metadata-keyed queue and start the delayed processing
                Action<FileChange> StartDelay = toDelay =>
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

                    // Merge the new event into SQL
                    ProcessMergeToSQL(toDelay, null);

                    // start delayed processing of file change
                    toDelay.ProcessAfterDelay(ProcessFileChange,// Callback which fires on process timer completion (on a new thread)
                        null,// Userstate if needed on callback (unused)
                        ProcessingDelayInMilliseconds,// processing delay to wait for more events on this file
                        ProcessingDelayMaxResets);// number of processing delay resets before it will process the file anyways
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
                        StartDelay(toChange);
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

                                            // Remove old event from SQL
                                            ProcessMergeToSQL(null, previousChange);
                                        }
                                        // For files with different metadata, process as a modify
                                        else
                                        {
                                            previousChange.Type = FileChangeType.Modified;
                                            previousChange.Metadata = toChange.Metadata;
                                            previousChange.SetDelayBackToInitialValue();

                                            // Use previousChange as the mergedEvent, remove toChange
                                            ProcessMergeToSQL(previousChange, toChange);
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

                                        // Remove old event from SQL
                                        ProcessMergeToSQL(null, previousChange);
                                        break;
                                    case FileChangeType.Deleted:
                                        // error condition
                                        break;
                                    case FileChangeType.Modified:
                                        previousChange.Type = FileChangeType.Deleted;
                                        previousChange.Metadata = toChange.Metadata;
                                        previousChange.SetDelayBackToInitialValue();

                                        // Use previousChange as the mergedEvent, remove toChange
                                        ProcessMergeToSQL(previousChange, toChange);
                                        break;
                                    case FileChangeType.Renamed:
                                        previousChange.NewPath = previousChange.OldPath;
                                        previousChange.OldPath = null;
                                        previousChange.Type = FileChangeType.Deleted;
                                        previousChange.Metadata = toChange.Metadata;
                                        previousChange.SetDelayBackToInitialValue();
                                        // remove the old/new path pair for a rename
                                        OldToNewPathRenames.Remove(previousChange.NewPath);

                                        // Use previousChange as the mergedEvent, remove toChange
                                        ProcessMergeToSQL(previousChange, toChange);
                                        break;
                                }
                                break;
                            case FileChangeType.Modified:
                                switch (previousChange.Type)
                                {
                                    case FileChangeType.Created:
                                        previousChange.Metadata = toChange.Metadata;
                                        previousChange.SetDelayBackToInitialValue();

                                        // Use previousChange as the mergedEvent, remove toChange
                                        ProcessMergeToSQL(previousChange, toChange);
                                        break;
                                    case FileChangeType.Deleted:
                                        // error condition
                                        break;
                                    case FileChangeType.Modified:
                                        previousChange.Metadata = toChange.Metadata;
                                        previousChange.SetDelayBackToInitialValue();

                                        // Use previousChange as the mergedEvent, remove toChange
                                        ProcessMergeToSQL(previousChange, toChange);
                                        break;
                                    case FileChangeType.Renamed:
                                        previousChange.Metadata = toChange.Metadata;
                                        previousChange.SetDelayBackToInitialValue();

                                        // Use previousChange as the mergedEvent, remove toChange
                                        ProcessMergeToSQL(previousChange, toChange);
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

                                            // Use previousChange as the mergedEvent, remove toChange
                                            ProcessMergeToSQL(previousChange, toChange);
                                        }
                                        else
                                        {
                                            previousChange.Metadata = toChange.Metadata;
                                            previousChange.Type = FileChangeType.Modified;
                                            previousChange.SetDelayBackToInitialValue();

                                            // Use previousChange as the mergedEvent, remove toChange
                                            ProcessMergeToSQL(previousChange, toChange);

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
                                            StartDelay(oldLocationDelete);
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

                    // Use matchedFileChangeForRename as the mergedEvent, remove toChange
                    ProcessMergeToSQL(matchedFileChangeForRename, toChange);
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

                    // Use matchedFileChangeForRename as the mergedEvent, remove toChange
                    ProcessMergeToSQL(matchedFileChangeForRename, toChange);
                }

                // if file change does not exist in the queue at the same file path and the change was not marked to be converted to a rename
                else
                {
                    // add file change to the queue
                    QueuedChanges.Add(toChange.NewPath, toChange);

                    StartDelay(toChange);

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
        private void ProcessFileChange(FileChange sender, object state)
        {
            // If the change is a rename that is being removed, take off its old/new path pair
            if (sender.Type == FileChangeType.Renamed)
            {
                if (OldToNewPathRenames.ContainsKey(sender.OldPath)
                    && FilePathComparer.Instance.Equals(sender.NewPath, OldToNewPathRenames[sender.OldPath]))
                {
                    OldToNewPathRenames.Remove(sender.OldPath);
                }
            }

            // remove file changes from each queue if they exist
            if (QueuedChanges.ContainsKey(sender.NewPath)
                && QueuedChanges[sender.NewPath] == sender)
            {
                QueuedChanges.Remove(sender.NewPath);
            }
            if (QueuedChangesByMetadata.ContainsKey(sender.Metadata.HashableProperties))
            {
                QueuedChangesByMetadata.Remove(sender.Metadata.HashableProperties);
            }

            lock (QueuesTimer.TimerRunningLocker)
            {
                ProcessingChanges.Enqueue(sender);
                if (ProcessingChanges.Count > 499)
                {
                    QueuesTimer.TriggerTimerCompletionImmediately();
                }
                else
                {
                    QueuesTimer.StartTimerIfNotRunning();
                }
            }
        }

        private void ProcessFileChangeGroup(FileChange[] changesIn)
        {
            if (this.OnProcessEventGroupCallback != null)
            {
                // Finalize these changes
                FileChange[] changesInError;
                KeyValuePair<FileChange, FileStream>[] changesWithFileStreams;
                CLError error =  ProcessFileListForSyncProcessing(changesIn, out changesWithFileStreams, out changesInError);
                if (error != null)
                {
                    CLTrace.Instance.writeToLog(1, "MonitorAgent: ProcessFileChangeGroup: Error finalizing changes.  Msg: {0}, Code: {1}.", error.errorDescription, error.errorCode);
                }

                // Get the changes array back.  Forget the FileStreams for now TODO: Fix this.
                List<FileChange> changesList = new List<FileChange>();
                foreach (KeyValuePair<FileChange, FileStream> change in changesWithFileStreams)
                {
                    changesList.Add(change.Key);
                }
                FileChange[] changes = changesList.ToArray();

                //&&&& Save these changes to match up the events from the server.
                //TODO: Clear this reference when this group of changes has been synced.
                CurrentFileChanges = changes;

#if TRASH
                // Get the changes array back
                var changesEnumerable = (from element in changesWithFileStreams
                               select new FileChange[]
                               {
                                   element.Key
                               });
                FileChange[] changes = changesEnumerable.Cast<FileChange>().ToArray(); 
#endif // TRASH

                // Process the changes
                List<Dictionary<string, object>> eventsArray = new List<Dictionary<string,object>>();

                foreach (FileChange currentChange in changes)
                {
                    Dictionary<string, object> evt = new Dictionary<string, object>();

                    // Build an event to represent this change.
                    string action = "";
                    switch (currentChange.Type)
                    {
                        case FileChangeType.Created:
                            if (currentChange.Metadata.HashableProperties.IsFolder)
                            {
                                action = CLDefinitions.CLEventTypeAddFolder;
                            }
                            else
                            {
                                action = CLDefinitions.CLEventTypeAddFile;
                            }
                            break;
                        case FileChangeType.Deleted:
                            if (currentChange.Metadata.HashableProperties.IsFolder)
                            {
                                action = CLDefinitions.CLEventTypeDeleteFolder;
                            }
                            else
                            {
                                action = CLDefinitions.CLEventTypeDeleteFile;
                            }
                            break;
                        case FileChangeType.Modified:
                            action = CLDefinitions.CLEventTypeModifyFile;
                            break;
                        case FileChangeType.Renamed:
                            if (currentChange.Metadata.HashableProperties.IsFolder)
                            {
                                action = CLDefinitions.CLEventTypeRenameFolder;
                            }
                            else
                            {
                                action = CLDefinitions.CLEventTypeRenameFile;
                            }
                            break;
                    }

                    // Build the metadata dictionary
                    Dictionary<string, object> metadata = new Dictionary<string, object>();

                    String relativeNewPath = String.Empty;
                    String relativeOldPath = String.Empty;

                    String cloudPath = GetCurrentPath();
                    if (!String.IsNullOrWhiteSpace(cloudPath))
                    {
                        FilePath convertedCloudPath = cloudPath;
                        if (currentChange.NewPath != null)
                        {
                            FilePath cloudOverlap = convertedCloudPath.FindOverlappingPath(currentChange.NewPath);
                            if (cloudOverlap != null
                                && FilePathComparer.Instance.Equals(cloudOverlap, convertedCloudPath))
                            {
                                relativeNewPath = currentChange.NewPath.ToString().Substring(cloudOverlap.ToString().Length).Replace('\\', '/');
                            }
                        }
                        if (currentChange.OldPath != null)
                        {
                            FilePath cloudOverlap = convertedCloudPath.FindOverlappingPath(currentChange.OldPath);
                            if (cloudOverlap != null
                                && FilePathComparer.Instance.Equals(cloudOverlap, convertedCloudPath))
                            {
                                relativeOldPath = currentChange.OldPath.ToString().Substring(cloudOverlap.ToString().Length).Replace('\\', '/');
                            }
                        }
                    }

                    // Format the time like "2012-03-20T19:50:25Z"
                    metadata.Add(CLDefinitions.CLMetadataFileCreateDate, currentChange.Metadata.HashableProperties.CreationTime.ToString("o"));
                    metadata.Add(CLDefinitions.CLMetadataFileModifiedDate, currentChange.Metadata.HashableProperties.LastTime.ToString("o"));
                    metadata.Add(CLDefinitions.CLMetadataFileSize, currentChange.Metadata.HashableProperties.Size);
                    metadata.Add(CLDefinitions.CLMetadataFromPath, relativeOldPath);
                    metadata.Add(CLDefinitions.CLMetadataCloudPath, relativeNewPath);
                    metadata.Add(CLDefinitions.CLMetadataToPath, String.Empty);       // not used?
                    string md5;
                    currentChange.GetMD5LowercaseString(out md5);
                    metadata.Add(CLDefinitions.CLMetadataFileHash, md5);
                    metadata.Add(CLDefinitions.CLMetadataFileIsDirectory, currentChange.Metadata.HashableProperties.IsFolder);
                    bool isLink = false;
                    if (currentChange.LinkTargetPath != null && !String.IsNullOrWhiteSpace(currentChange.LinkTargetPath.ToString()))
                    {
                        isLink = true;
                    }
                    metadata.Add(CLDefinitions.CLMetadataFileIsLink, isLink);
                    metadata.Add(CLDefinitions.CLMetadataFileRevision, currentChange.Revision);
                    metadata.Add(CLDefinitions.CLMetadataFileCAttributes, String.Empty);
                    metadata.Add(CLDefinitions.CLMetadataItemStorageKey, currentChange.StorageKey);
                    metadata.Add(CLDefinitions.CLMetadataLastEventID, currentChange.EventId.ToString());
                    metadata.Add(CLDefinitions.CLMetadataFileTarget, isLink ? currentChange.LinkTargetPath.ToString() : String.Empty);


                    // Force server forward slash normalization
                    

                    // Add this event and its metadata to the events dictionary
                    evt.Add(CLDefinitions.CLSyncEvent, action);             // just one in the group for now.
                    evt.Add(CLDefinitions.CLSyncEventMetadata, metadata);

                    // Add the event to the array.
                    eventsArray.Add(evt);
                }

                // Build the dictionary to return.  Start by adding the last EventId and an event count of one.
                Dictionary<string, object> eventsDictionary = new Dictionary<string, object>();

                //TODO: The mac client uses the CLEEventKey as the last Mac file system event ID synced.  On restart, the Mac client will
                // go to the file system and say "replay events from the event following the last event ID synced."  On the Mac, the
                // file system event IDs are sequential and persistent.  The Windows client file system monitor performs its own
                // restart logic.
                // For now, set the CLEventKey to zero.
                // OLD: eventsDictionary.Add(CLDefinitions.CLEventKey, currentChange.EventId);
                eventsDictionary.Add(CLDefinitions.CLEventKey, 0);
                eventsDictionary.Add(CLDefinitions.CLEventCount, changes.Count());
                eventsDictionary.Add(CLDefinitions.CLSyncEvents, eventsArray);

                // Feed this group of one to the sync service.
                this.OnProcessEventGroupCallback(eventsDictionary);
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
            // If a filepath is deleted, that means a delete operation may occur in the sync as well for the same path;
            // If the final change to be processed will be a delete,
            // all previous changes below the deleted path should be disposed since they don't need to fire (will be overridden with deletion)

            lock (QueuedChanges)
            {
                if (QueuedChanges.ContainsKey(changeRoot))
                {
                    FileChange rootChange = QueuedChanges[changeRoot];
                    Action toProcess = () =>
                        {
                            if (rootChange.Type == FileChangeType.Deleted)
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
                        toProcess();
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
                    Action toProcess = () =>
                        {
                            if (rootChange.Type == FileChangeType.Renamed
                                && FilePathComparer.Instance.Equals(rootChange.OldPath, changeRootOld))
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
                        toProcess();
                    }
                    else
                    {
                        rootChange.EnqueuePreprocessingAction(toProcess);
                    }
                }
            }
        }

        private void ProcessQueuesAfterTimer()
        {
            // FileChanges are now sorted when Sync calls back to ProcessFileListForSyncProcessing in this class

            //List<FileChange> DeleteFiles = new List<FileChange>();
            //List<FileChange> DeleteFolders = new List<FileChange>();
            //List<FileChange> OtherFolderOperations = new List<FileChange>();
            //List<FileChange> OtherFileOperations = new List<FileChange>();

            //FileChange[] batchedChanges = new FileChange[ProcessingChanges.Count];

            //while (ProcessingChanges.Count > 0)
            //{
            //    FileChange currentChangeToAdd = ProcessingChanges.Dequeue();
            //    if (currentChangeToAdd.Type == FileChangeType.Deleted)
            //    {
            //        if (currentChangeToAdd.Metadata.HashableProperties.IsFolder)
            //        {
            //            DeleteFolders.Add(currentChangeToAdd);
            //        }
            //        else
            //        {
            //            DeleteFiles.Add(currentChangeToAdd);
            //        }
            //    }
            //    else if (currentChangeToAdd.Metadata.HashableProperties.IsFolder)
            //    {
            //        OtherFolderOperations.Add(currentChangeToAdd);
            //    }
            //    else
            //    {
            //        OtherFileOperations.Add(currentChangeToAdd);
            //    }
            //}

            //int batchedChangeIndex = 0;
            //foreach (FileChange currentDeleteFile in DeleteFiles)
            //{
            //    batchedChanges[batchedChangeIndex] = currentDeleteFile;
            //    batchedChangeIndex++;
            //}
            //foreach (FileChange currentDeleteFolder in DeleteFolders)
            //{
            //    batchedChanges[batchedChangeIndex] = currentDeleteFolder;
            //    batchedChangeIndex++;
            //}
            //foreach (FileChange currentOtherFolder in OtherFolderOperations)
            //{
            //    batchedChanges[batchedChangeIndex] = currentOtherFolder;
            //    batchedChangeIndex++;
            //}
            //foreach (FileChange currentOtherFile in OtherFileOperations)
            //{
            //    batchedChanges[batchedChangeIndex] = currentOtherFile;
            //    batchedChangeIndex++;
            //}

            FileChange[] batchedChanges = new FileChange[ProcessingChanges.Count];

            int currentBatchChangeIndex = 0;
            while (ProcessingChanges.Count > 0)
            {
                batchedChanges[currentBatchChangeIndex] = ProcessingChanges.Dequeue();
                currentBatchChangeIndex++;
            }

            (new Thread(() =>
                {
                    ProcessFileChangeGroup(batchedChanges);
                })).Start();
        }
        #endregion
    }
}