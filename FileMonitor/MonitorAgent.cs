﻿//
// Helpers.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
// the following linq namespace is used only if the optional initialization parameter for processing logging is passed as true
using System.Xml.Linq;

namespace FileMonitor
{
    public class MonitorAgent : IDisposable
    {
        #region public property
        /// <summary>
        /// Retrieves running status of monitor as enum for each part (file and folder)
        /// </summary>
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
        #endregion

        #region private fields and property
        // stores the optional FileChange queueing callback intialization parameter
        private Action<MonitorAgent, FileChange> OnQueueing;

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
        private Dictionary<string, FileMetadata> AllPaths = new Dictionary<string, FileMetadata>();

        // Storage of changes queued to process (QueuedChanges used as the locker for both and keyed by file path, QueuedChangesByMetadata keyed by the hashable metadata properties)
        private Dictionary<string, FileChange> QueuedChanges = new Dictionary<string, FileChange>();
        private static readonly FileMetadataHashableComparer QueuedChangesMetadataComparer = new FileMetadataHashableComparer();// Comparer has improved hashing by using only the fastest changing bits
        private Dictionary<FileMetadataHashableProperties, FileChange> QueuedChangesByMetadata = new Dictionary<FileMetadataHashableProperties, FileChange>(QueuedChangesMetadataComparer);// Use custom comparer for improved hashing

        /// <summary>
        /// Global hashing provider for MD5 checksums
        /// </summary>
        private static MD5 MD5Hasher
        {
            get
            {
                lock (MD5HasherLocker)
                {
                    if (_mD5Hasher == null)
                    {
                        _mD5Hasher = new MD5CryptoServiceProvider();
                    }
                }
                return _mD5Hasher;
            }
        }
        private static MD5 _mD5Hasher;
        private static object MD5HasherLocker = new object();
        #endregion

        /// <summary>
        /// Create and initialize the MonitorAgent with the root folder to be monitored (Cloud Directory),
        /// requires running Start() method to begin monitoring and then, when available, load
        /// the initial index list to begin processing via BeginProcessing(initialList)
        /// </summary>
        /// <param name="folderPath">path of root folder to be monitored</param>
        /// <param name="newAgent">returned MonitorAgent</param>
        /// <param name="onQueueingCallback">(optional) action to be executed evertime a FileChange would be queued for processing</param>
        /// <param name="logProcessing">(optional) if set, logs FileChange objects when their processing callback fires</param>
        /// <returns></returns>
        public static CLError CreateNewAndInitialize(string folderPath, out MonitorAgent newAgent, Action<MonitorAgent, FileChange> onQueueingCallback = null, bool logProcessing = false)
        {
            try
            {
                newAgent = new MonitorAgent(folderPath, onQueueingCallback, logProcessing);
            }
            catch (Exception ex)
            {
                newAgent = (MonitorAgent)Helpers.DefaultForType(typeof(MonitorAgent));
                return ex;
            }
            return null;
        }
        private MonitorAgent(string folderPath, Action<MonitorAgent, FileChange> onQueueingCallback, bool logProcessing)
        {
            if (string.IsNullOrEmpty(folderPath))
                throw new Exception("Folder path cannot be null nor empty");
            if (!(new DirectoryInfo(folderPath)).Exists)
                throw new Exception("Folder not found at provided folder path");
            // Initialize folder paths
            this.CurrentFolderPath = this.InitialFolderPath = folderPath;
            
            // assign local fields with optional initialization parameters
            this.OnQueueing = onQueueingCallback;
            this.LogProcessingFileChanges = logProcessing;
        }

        #region public methods
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
                string filePath = CurrentFolderPath + e.FullPath.Substring(InitialFolderPath.Length);
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
                CheckMetadataAgainstFile(filePath, oldPath, e.ChangeType, folderOnly);
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
        /// <param name="filePath">Path where the change was observed</param>
        /// <param name="oldPath">Previous path if change was a rename</param>
        /// <param name="changeType">Type of file system event</param>
        /// <param name="folderOnly">Specificity from routing of file system event</param>
        private void CheckMetadataAgainstFile(string filePath, string oldPath, WatcherChangeTypes changeType, bool folderOnly)
        {
            // object for gathering folder info at current path
            DirectoryInfo folder = new DirectoryInfo(filePath);
            // object for gathering file info (set conditionally later in case we know change is not a file)
            FileInfo file = null;
            // field used to determine if change is a file or folder
            bool isFolder;
            // field used to determine if file/folder exists
            bool exists;

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
                file = new FileInfo(filePath);
                // check and store if file exists
                exists = file.Exists;
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
                            if (AllPaths.ContainsKey(filePath))
                            {
                                // No need to send modified events for folders
                                // so check if event is on a file or if folder modifies are not ignored
                                if (!isFolder
                                    || !IgnoreFolderModifies)
                                {
                                    // retrieve stored index
                                    FileMetadata previousMetadata = AllPaths[filePath];
                                    // store if error occurred retrieving MD5 checksum
                                    bool md5Error;
                                    // compare stored index with values from file info
                                    FileMetadata newMetadata = ReplacementMetadataIfDifferent(previousMetadata,
                                        filePath,
                                        isFolder,
                                        lastTime,
                                        creationTime,
                                        isFolder ? (Nullable<long>)null : file.Length,
                                        out md5Error);
                                    // if new metadata came back after comparison, queue file change for modify
                                    if (newMetadata != null)
                                    {
                                        // replace index at current path
                                        AllPaths[filePath] = newMetadata;
                                        // queue file change for modify
                                        QueueFileChange(new FileChange()
                                        {
                                            NewPath = filePath,
                                            Metadata = newMetadata,
                                            Type = md5Error ? FileChangeType.ModifiedWithError : FileChangeType.Modified
                                        });
                                    }
                                }
                            }
                            // if index did not already exist
                            else
                            {
                                // store if error occurred retrieving file MD5
                                bool md5Error;
                                // add new index
                                AllPaths.Add(filePath,
                                    new FileMetadata()
                                    {
                                        HashableProperties = new FileMetadataHashableProperties(isFolder,
                                            lastTime,
                                            creationTime,
                                            isFolder ? (Nullable<long>)null : file.Length),
                                        MD5 = isFolder ? NullByteArrayWithFalseOutput(out md5Error) : GetMD5(filePath, out md5Error)
                                    });
                                // queue file change for create
                                QueueFileChange(new FileChange()
                                {
                                    NewPath = filePath,
                                    Metadata = AllPaths[filePath],
                                    Type = md5Error ? FileChangeType.CreatedWithError : FileChangeType.Created
                                });
                            }
                        }
                        // if file file does not exist, but an index exists
                        else if (AllPaths.ContainsKey(filePath))
                        {
                            // queue file change for delete
                            QueueFileChange(new FileChange()
                            {
                                NewPath = filePath,
                                Metadata = AllPaths[filePath],
                                Type = FileChangeType.Deleted
                            });
                            // remove index
                            AllPaths.Remove(filePath);
                        }
                    }
                    // for file system events marked as rename
                    else if ((changeType & WatcherChangeTypes.Renamed) == WatcherChangeTypes.Renamed)
                    {
                        // store a boolean which may be set to true to notify a condition when a rename operation may need to be queued
                        bool possibleRename = false;
                        // if index exists at the previous path
                        if (AllPaths.ContainsKey(oldPath))
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
                                QueueFileChange(new FileChange()
                                {
                                    NewPath = oldPath,
                                    Metadata = AllPaths[oldPath],
                                    Type = FileChangeType.Deleted
                                });
                                // remove index at previous path
                                AllPaths.Remove(oldPath);
                            }
                        }
                        // if index exists at current path (irrespective of last condition on previous path index)
                        if (AllPaths.ContainsKey(filePath))
                        {
                            // if file or folder exists
                            if (exists)
                            {
                                // No need to send modified events for folders
                                // so check if event is on a file or if folder modifies are not ignored
                                if (!isFolder
                                    || !IgnoreFolderModifies)
                                {
                                    // retrieve stored index at current path
                                    FileMetadata previousMetadata = AllPaths[filePath];
                                    // store if error occurred retrieving MD5 checksum
                                    bool md5Error;
                                    // compare stored index with values from file info
                                    FileMetadata newMetadata = ReplacementMetadataIfDifferent(previousMetadata,
                                        filePath,
                                        isFolder,
                                        lastTime,
                                        creationTime,
                                        isFolder ? (Nullable<long>)null : file.Length,
                                        out md5Error);
                                    // if new metadata came back after comparison, queue file change for modify
                                    if (newMetadata != null)
                                    {
                                        // replace index at current path
                                        AllPaths[filePath] = newMetadata;
                                        // queue file change for modify
                                        QueueFileChange(new FileChange()
                                        {
                                            NewPath = filePath,
                                            Metadata = newMetadata,
                                            Type = md5Error ? FileChangeType.ModifiedWithError : FileChangeType.Modified
                                        });
                                    }
                                }
                            }
                            // else file does not exist
                            else
                            {
                                // queue file change for delete at new path
                                QueueFileChange(new FileChange()
                                {
                                    NewPath = filePath,
                                    Metadata = AllPaths[filePath],
                                    Type = FileChangeType.Deleted
                                });
                                // remove index for new path
                                AllPaths.Remove(filePath);

                                // no need to continue and check possibeRename since it required exists to be true, return now
                                return;
                            }
                            // if precursor condition was set for a file change for rename
                            // (but an index already exists at the new path)
                            if (possibleRename)
                            {
                                // queue file change for delete at previous path
                                QueueFileChange(new FileChange()
                                {
                                    NewPath = oldPath,
                                    Metadata = AllPaths[oldPath],
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
                            // store if error occurred retrieving MD5 checksum
                            bool md5Error;
                            // compare stored index from previous path with values from current change
                            FileMetadata newMetadata = ReplacementMetadataIfDifferent(previousMetadata,
                                filePath,
                                isFolder,
                                lastTime,
                                creationTime,
                                isFolder ? (Nullable<long>)null : file.Length,
                                out md5Error);
                            // remove index at the previous path
                            AllPaths.Remove(oldPath);
                            // add an index for the current path either from the changed metadata if it exists otherwise the previous metadata
                            AllPaths.Add(filePath, newMetadata ?? previousMetadata);
                            // queue file change for rename (use changed metadata if it exists otherwise the previous metadata)
                            QueueFileChange(new FileChange()
                            {
                                NewPath = filePath,
                                OldPath = oldPath,
                                Metadata = newMetadata ?? previousMetadata,
                                Type = md5Error ? FileChangeType.RenamedWithError : FileChangeType.Renamed
                            });
                        }
                        // if index does not exist at either the old nor new paths and the file exists
                        else
                        {

                            // store if error occurred retrieving file MD5
                            bool md5Error;
                            // add new index at new path
                            AllPaths.Add(filePath,
                                new FileMetadata()
                                {
                                    HashableProperties = new FileMetadataHashableProperties(isFolder,
                                        lastTime,
                                        creationTime,
                                        isFolder ? (Nullable<long>)null : file.Length),
                                    MD5 = isFolder ? NullByteArrayWithFalseOutput(out md5Error) : GetMD5(filePath, out md5Error)
                                });
                            // queue file change for create for new path
                            QueueFileChange(new FileChange()
                            {
                                NewPath = filePath,
                                Metadata = AllPaths[filePath],
                                Type = md5Error ? FileChangeType.CreatedWithError : FileChangeType.Created
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
                            if (AllPaths.ContainsKey(filePath)
                                &&
                                // No need to send modified events for folders
                                // so check if event is on a file or if folder modifies are not ignored
                                (!isFolder
                                    || !IgnoreFolderModifies))
                            {
                                // retrieve stored index at current path
                                FileMetadata previousMetadata = AllPaths[filePath];
                                // store if error occurred retrieving MD5 checksum
                                bool md5Error;
                                // compare stored index with values from file info
                                FileMetadata newMetadata = ReplacementMetadataIfDifferent(previousMetadata,
                                    filePath,
                                    isFolder,
                                    lastTime,
                                    creationTime,
                                    isFolder ? (Nullable<long>)null : file.Length,
                                    out md5Error);
                                // if new metadata came back after comparison, queue file change for modify
                                if (newMetadata != null)
                                {
                                    // replace index at current path
                                    AllPaths[filePath] = newMetadata;
                                    // queue file change for modify
                                    QueueFileChange(new FileChange()
                                    {
                                        NewPath = filePath,
                                        Metadata = newMetadata,
                                        Type = md5Error ? FileChangeType.ModifiedWithError : FileChangeType.Modified
                                    });
                                }
                            }
                        }
                        // if file or folder does not exist but index exists for current path
                        else if (AllPaths.ContainsKey(filePath))
                        {
                            // queue file change for delete
                            QueueFileChange(new FileChange()
                            {
                                NewPath = filePath,
                                Metadata = AllPaths[filePath],
                                Type = FileChangeType.Deleted
                            });
                            // remove index
                            AllPaths.Remove(filePath);
                        }
                    }

                    #endregion
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
            string filePath,
            bool isFolder,
            DateTime lastTime,
            DateTime creationTime,
            Nullable<long> size,
            out bool md5ErrorOccurred)
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
                    HashableProperties = forCompare,
                    MD5 = isFolder ? NullByteArrayWithFalseOutput(out md5ErrorOccurred) : GetMD5(filePath, out md5ErrorOccurred)
                };
            }
            // No metadata change detected, thus no error occurred, also return null
            md5ErrorOccurred = false;
            return null;
        }
        /// <summary>
        /// Used to simultaneously set a false out parameter and return null
        /// </summary>
        /// <param name="willBeFalse"></param>
        /// <returns></returns>
        private byte[] NullByteArrayWithFalseOutput(out bool willBeFalse)
        {
            willBeFalse = false;
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
                // store that the delay needs to be started (leading to the processing action)
                bool startDelay = true;
                // if queue already contains a file change at the same path,
                // either replace change and start the new one if the old one is currently processing
                // otherwise change the existing file change properties to match the new ones and restart the delay timer
                if (QueuedChanges.ContainsKey(toChange.NewPath))
                {
                    // grab existing file change from the queue
                    FileChange previousChange = QueuedChanges[toChange.NewPath];
                    // if the file change is already marked that it started processing,
                    // need to replace the file change in the queue (which will start it's own processing later on a new delay)
                    if (previousChange.DelayCompleted)
                    {
                        // replace file change in the queue at the same location with the new change
                        QueuedChanges[toChange.NewPath] = toChange;
                    }
                    // file change has not already started processing
                    else
                    {
                        // FileChange already exists
                        // Instead of starting a new processing delay, update the FileChange information
                        // Then restart the delay timer
                        startDelay = false;
                        previousChange.NewPath = toChange.NewPath;
                        previousChange.OldPath = toChange.OldPath;
                        previousChange.Metadata = toChange.Metadata;
                        previousChange.Type = toChange.Type;
                        QueuedChanges[toChange.NewPath].SetDelayBackToInitialValue();
                    }
                }

                // System does not generate proper rename WatcherTypes if a file/folder is moved
                // The following two 'else ifs' checks for matching metadata between creation/deletion events to associate together as a rename

                // Existing FileChange is a Deleted event and the incoming event is a matching Created event which has not yet completed
                else if ((toChange.Type == FileChangeType.Created || toChange.Type == FileChangeType.CreatedWithError)
                        && QueuedChangesByMetadata.ContainsKey(toChange.Metadata.HashableProperties)
                        && (matchedFileChangeForRename = QueuedChangesByMetadata[toChange.Metadata.HashableProperties]).Type == FileChangeType.Deleted
                        && !matchedFileChangeForRename.DelayCompleted)
                {
                    // FileChange already exists
                    // Instead of starting a new processing delay, update the FileChange information
                    // Then restart the delay timer
                    startDelay = false;
                    if (toChange.Type == FileChangeType.CreatedWithError)
                    {
                        matchedFileChangeForRename.Type = FileChangeType.RenamedWithError;
                    }
                    else
                    {
                        matchedFileChangeForRename.Type = FileChangeType.Renamed;
                    }
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
                }
                // Existing FileChange is a Created event and the incoming event is a matching Deleted event which has not yet completed
                else if (toChange.Type == FileChangeType.Deleted
                        && QueuedChangesByMetadata.ContainsKey(toChange.Metadata.HashableProperties)
                        && ((matchedFileChangeForRename = QueuedChangesByMetadata[toChange.Metadata.HashableProperties]).Type == FileChangeType.Created
                            || matchedFileChangeForRename.Type == FileChangeType.CreatedWithError)
                        && !matchedFileChangeForRename.DelayCompleted)
                {
                    // FileChange already exists
                    // Instead of starting a new processing delay, update the FileChange information
                    // Then restart the delay timer
                    startDelay = false;
                    if (matchedFileChangeForRename.Type == FileChangeType.CreatedWithError)
                    {
                        matchedFileChangeForRename.Type = FileChangeType.RenamedWithError;
                    }
                    else
                    {
                        matchedFileChangeForRename.Type = FileChangeType.Renamed;
                    }
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
                }
                
                // if file change does not exist in the queue at the same file path and the change was not marked to be converted to a rename
                else
                {
                    // add file change to the queue
                    QueuedChanges.Add(toChange.NewPath, toChange);
                }

                // unless the delay was explicitly changed to not start, move the file change to the metadata-keyed queue and start the delayed processing
                if (startDelay)
                {
                    // move the file change to the metadata-keyed queue if it does not already exist
                    if (!QueuedChangesByMetadata.ContainsKey(toChange.Metadata.HashableProperties))
                    {
                        // add file change to metadata-keyed queue
                        QueuedChangesByMetadata.Add(toChange.Metadata.HashableProperties, toChange);
                    }

                    // If onQueueingCallback was set on initialization of the monitor agent, fire the callback with the new FileChange
                    if (OnQueueing != null)
                    {
                        OnQueueing(this, toChange);
                    }

                    // start delayed processing of file change
                    toChange.ProcessAfterDelay(ProcessFileChange,// Callback which fires on process timer completion (on a new thread)
                        null,// Userstate if needed on callback (unused)
                        ProcessingDelayInMilliseconds,// processing delay to wait for more events on this file
                        ProcessingDelayMaxResets,// number of processing delay resets before it will process the file anyways
                        QueuedChanges);// timer thread needs to lock on the parent dictionary prevent an event change simultaneously with processing, locked code sets DelayCompleted property to true
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
            // lock on queue to prevent conflicting updates/reads
            lock (QueuedChanges)
            {
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
            }

            // Todo: Put in the code to handle processing a file change through the sync service

            // if optional initialization parameter for logging was passed as true, log an xml file describing the processed FileChange
            if (LogProcessingFileChanges)
            {
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
                //  <!--Only present if the change has a file size-->
                //  <Size>[hex string of MD5 file checksum]</Size>
                //
                //</FileChange>
                AppendFileChangeProcessedLogXmlString(new XElement("FileChange",
                    new XElement("NewPath", new XText(sender.NewPath)),
                    sender.OldPath == null ? null : new XElement("OldPath", new XText(sender.OldPath)),
                    new XElement("IsFolder", new XText(sender.Metadata.HashableProperties.IsFolder.ToString())),
                    new XElement("Type", new XText(sender.Type.ToString())),
                    new XElement("LastTime", new XText(sender.Metadata.HashableProperties.LastTime.Ticks.ToString())),
                    new XElement("CreationTime", new XText(sender.Metadata.HashableProperties.CreationTime.Ticks.ToString())),
                    sender.Metadata.HashableProperties.Size == null ? null : new XElement("Size", new XText(sender.Metadata.HashableProperties.Size.Value.ToString())),
                    sender.Metadata.MD5 == null ? null : new XElement("MD5", new XText(sender.Metadata.MD5
                        .Select(md5Byte => string.Format("{0:x2}", md5Byte))
                        .Aggregate((previousBytes, newByte) => previousBytes + newByte)))).ToString() + Environment.NewLine);
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
        /// Retrieves MD5 checksum for a given file path
        /// </summary>
        /// <param name="filePath">Location of file to generate checksum</param>
        /// <returns>Returns byte[16] representing the MD5 data</returns>
        private byte[] GetMD5(string filePath, out bool md5ErrorOccurred)
        {
            try
            {
                // Filestream will fail if reading is blocked or permissions don't allow read,
                // exception will bubble up to CheckMetadataAgainstFile for handling
                using (FileStream mD5Stream = new FileStream(filePath,
                    FileMode.Open))
                {
                    // compute hash and return using static instance of MD5
                    byte[] toReturn = MD5Hasher.ComputeHash(mD5Stream);
                    // an error did not occur, output as such and return
                    md5ErrorOccurred = false;
                    return toReturn;
                }
            }
            catch
            {
                // error did occur

                // output that error occurred
                md5ErrorOccurred = true;
                bool badgingIsInitialized;
                // check if badging is initialized, if so then set badge as failed
                // error not handled, although the output boolean will be false on error so it will not run attempt to badge
                CLError isInitializedError = BadgeNET.IconOverlay.IsBadgingInitialized(out badgingIsInitialized);
                if (badgingIsInitialized)
                {
                    // set badge at path for error
                    // error not handled
                    CLError badgingError = BadgeNET.IconOverlay.setBadgeType(BadgeNET.cloudAppIconBadgeType.cloudAppBadgeFailed, filePath);
                }
                // return nothing (common occurance when file is locked externally)
                return null;
            }
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
        #endregion
    }
}
