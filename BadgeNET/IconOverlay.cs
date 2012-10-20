//
// IconOverlay.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO.Pipes;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.IO;
using CloudApiPublic.Support;
using CloudApiPrivate.Common;
using System.Runtime.InteropServices;
using BadgeNET.Static;

namespace BadgeNET
{
    /// <summary>
    /// IconOverlay is responsible for keeping a list of badges and synchronizing them with BadgeCOM (the Windows shell extensions for icon overlays)
    /// </summary>
    public sealed class IconOverlay : IDisposable
    {
        #region Singleton pattern
        /// <summary>
        /// Access all IconOverlay public methods via this object reference
        /// </summary>
        private static IconOverlay Instance
        {
            get
            {
                // ensure instance is only created once
                lock (InstanceLocker)
                {
                    if (_instance == null)
                    {
                        _instance = new IconOverlay();
                    }
                    return _instance;
                }
            }
        }
        private static IconOverlay _instance = null;
        private static object InstanceLocker = new object();
        private static CLTrace _trace = CLTrace.Instance;

        // Constructor for Singleton pattern is private
        private IconOverlay() { }

        // Standard IDisposable implementation based on MSDN System.IDisposable
        ~IconOverlay()
        {
            this.Dispose(false);
        }
        #endregion

        #region public methods
        /// <summary>
        /// Initialize IconOverlay badge COM object processing with or without initial list
        /// (initial list can be added later by a call to InitializeOrReplace)
        /// ¡¡ Do not call this method a second time nor after InitializeOrReplace has been called !!
        /// </summary>
        /// <param name="initialList">(optional) list to start with for badged objects, filepaths in keys must not be null nor empty</param>
        /// <param name="pathRootDirectory">The full path of the Cloud root directory.</param>
        public static CLError Initialize(string pathRootDirectory, IEnumerable<KeyValuePair<FilePath, GenericHolder<cloudAppIconBadgeType>>> initialList = null)
        {
            try
            {
                _trace.writeToLog(9, "IconOverlay: Initialize: Entry.");
                return Instance.pInitialize(pathRootDirectory, initialList);
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: Initialize: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                return ex;
            }
        }
        private CLError pInitialize(string pathRootDirectory, IEnumerable<KeyValuePair<FilePath, GenericHolder<cloudAppIconBadgeType>>> initialList = null)
        {
            try
            {
                // ensure IconOverlay is only ever initialized once
                lock (this)
                {
                    if (isInitialized)
                    {
                        _trace.writeToLog(1, "IconOverlay: pInitialize: ERROR: THROW: Instance already initailized.");
                        throw new Exception("IconOverlay Instance already initialized");
                    }
                    isInitialized = true;
                }

                MessageEvents.PathStateChanged += MessageEvents_PathStateChanged;

                bool initialListContainsItem = false;

                // Capture the Cloud directory path for performance.
                filePathCloudDirectory = pathRootDirectory;

                // Allocate the badging dictionary.  This is a hierarchical dictionary.
                CLError error = FilePathDictionary<GenericHolder<cloudAppIconBadgeType>>.CreateAndInitialize(
                    rootPath: filePathCloudDirectory,
                    pathDictionary: out allBadges,
                    recursiveDeleteCallback: OnAllBadgesRecursiveDelete,
                    recursiveRenameCallback: OnAllBadgesRecursiveRename);
                if (error != null)
                {
                    _trace.writeToLog(1, "IconOverlay: pInitialize: ERROR: THROW: Error from CreateAndInitialize.");
                    throw new Exception(String.Format("IconOverlay: pInitialize: ERROR from CreateAndInitialize: <{0}>, Code: {1}", error.errorDescription, error.errorCode));
                }

                if (allBadges == null)
                {
                    _trace.writeToLog(1, "IconOverlay: pInitialize: ERROR: THROW: Error creating badging dictionary.");
                    throw new Exception("IconOverlay error creating badging dictionary");
                }

                // I don't want to enumerate the initialList for both counting and copying, so I define an array for storage
                KeyValuePair<FilePath, GenericHolder<cloudAppIconBadgeType>>[] initialListArray;
                // initial list contained values for badging; preload dictionary and notify system of global change
                if (initialList != null
                    && (initialListArray = initialList.ToArray()).Length > 0)
                {
                    // store that initial list contained an item so system can be notified later
                    _trace.writeToLog(9, "IconOverlay: pInitialize: Got initial list.");
                    initialListContainsItem = true;

                    // loop through initial list for badged objects to add to local dictionary
                    for (int initialListCounter = 0; initialListCounter < initialListArray.Length; initialListCounter++)
                    {
                        // only keep track of badges that are not "synced"
                        if (initialListArray[initialListCounter].Value.Value != cloudAppIconBadgeType.cloudAppBadgeSynced)
                        {
                            // populate each initial badged object into local dictionary
                            // throws exception if file path (Key) is null or empty
                            // do not need to lock on allBadges since listening threads don't start until after this
                            _trace.writeToLog(9, "IconOverlay: Add badge for path {0}, value {1}.", initialListArray[initialListCounter].Key.ToString(), initialListArray[initialListCounter].Value.Value.ToString());
                            allBadges[initialListArray[initialListCounter].Key] = initialListArray[initialListCounter].Value;
                        }
                    }
                }

                _trace.writeToLog(9, "IconOverlay: StartBadgeCOMPipes.");
                StartBadgeCOMPipes();

                // initial list contained an item so notify the system to update
                if (initialListContainsItem)
                {
                    //Parameterless call to notify will force OS to update all icons
                    _trace.writeToLog(9, "IconOverlay: Initial list contains items.  Notify Explorer to repaint all icons.");
                    NotifySystemForBadgeUpdate();
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: pInitialize: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                return ex;
            }
            _trace.writeToLog(9, "IconOverlay: Return success.");
            return null;
        }

        private void MessageEvents_PathStateChanged(object sender, UpdatePathArgs e)
        {
            cloudAppIconBadgeType convertedState;
            switch (e.State)
            {
                case PathState.Failed:
                    convertedState = cloudAppIconBadgeType.cloudAppBadgeFailed;
                    break;
                case PathState.None:
                    convertedState = cloudAppIconBadgeType.cloudAppBadgeNone;
                    break;
                case PathState.Selective:
                    convertedState = cloudAppIconBadgeType.cloudAppBadgeSyncSelective;
                    break;
                case PathState.Synced:
                    convertedState = cloudAppIconBadgeType.cloudAppBadgeSynced;
                    break;
                case PathState.Syncing:
                    convertedState = cloudAppIconBadgeType.cloudAppBadgeSyncing;
                    break;
                default:
                    throw new ArgumentException("Unknown PathState: " + e.State.ToString());
            }

            QueueSetBadge(convertedState, e.Path);

            e.MarkHandled();
        }

        /// <summary>
        /// Returns whether IconOverlay is already initialized. If it is initialized, do not initialize it again.
        /// </summary>
        /// <param name="isInitialized">Return value</param>
        /// <returns>Error if it exists</returns>
        public static CLError IsBadgingInitialized(out bool isInitialized)
        {
            try
            {
                return Instance.pIsBadgingInitialized(out isInitialized);
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: IsBadgingInitialized: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                isInitialized = Helpers.DefaultForType<bool>();
                return ex;
            }
        }
        private CLError pIsBadgingInitialized(out bool isInitialized)
        {
            try
            {
                isInitialized = this.IsInitialized;
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: pIsBadgingInitialized: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                isInitialized = Helpers.DefaultForType<bool>();
                return ex;
            }
            return null;
        }
        private bool isInitialized = false;
        private bool IsInitialized 
        { 
            get
            {
                bool rc;
                lock(this)
                {
                    rc = isInitialized;
                }
                return rc;
            }
        }

        /// <summary>
        /// Runs Initialize if IconOverlay has not started processing with the provided list
        /// or replaces existing list with provided list. Can be run multiple times
        /// </summary>
        /// <param name="initialList">list to start with for badged objects, all filepaths in keys must not be null nor empty</param>
        /// <param name="pathRootDirectory">The full path to the Cloud root directory.</param>
        public static CLError InitializeOrReplace(string pathRootDirectory, IEnumerable<KeyValuePair<FilePath, GenericHolder<cloudAppIconBadgeType>>> initialList)
        {
            try
            {
                _trace.writeToLog(9, "IconOverlay: InitializeOrReplace. Entry.");
                return Instance.pInitializeOrReplace(pathRootDirectory, initialList);
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: InitializeOrReplace: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                return ex;
            }
        }
        private CLError pInitializeOrReplace(string pathRootDirectory, IEnumerable<KeyValuePair<FilePath, GenericHolder<cloudAppIconBadgeType>>> initialList)
        {
            try
            {
                bool listProcessed = false;

                // ensure IconOverlay is only ever initialized once
                _trace.writeToLog(9, "IconOverlay: pInitializeOrReplace. Entry.");
                lock (this)
                {
                    // run initialize instead if it has not been run
                    if (!isInitialized)
                    {
                        _trace.writeToLog(9, "IconOverlay: pInitializeOrReplace. Not initialized yet.  Initialize.");
                        Initialize(pathRootDirectory, initialList);
                        // store that list was already processed by initialization
                        listProcessed = true;
                    }
                }

                // Capture the Cloud directory path for performance.
                filePathCloudDirectory = pathRootDirectory;

                // if list was not already processed by initialization
                if (!listProcessed)
                {
                    // lock internal list during modification
                    _trace.writeToLog(9, "IconOverlay: pInitializeOrReplace. List not processed.");
                    lock (allBadges)
                    {
                        // empty list before adding in all replacement items
                        _trace.writeToLog(9, "IconOverlay: pInitializeOrReplace. Clear all badges.");
                        allBadges.Clear();
                        foreach (KeyValuePair<FilePath, GenericHolder<cloudAppIconBadgeType>> currentReplacedItem in initialList ?? new KeyValuePair<FilePath, GenericHolder<cloudAppIconBadgeType>>[0])
                        {
                            // only keep track of badges that are not "synced"
                            _trace.writeToLog(9, "IconOverlay: pInitializeOrReplace. currentReplaceItem. Path {0}, Type: {1}).", currentReplacedItem.Key.ToString(), currentReplacedItem.Value.Value.ToString());
                            if (currentReplacedItem.Value.Value != cloudAppIconBadgeType.cloudAppBadgeSynced)
                            {
                                // populate each replaced badged object into local dictionary
                                // throws exception if file path (Key) is null or empty
                                _trace.writeToLog(9, "IconOverlay: pInitializeOrReplace. Add this item to the dictionary.");
                                allBadges.Add(currentReplacedItem.Key, currentReplacedItem.Value);
                            }
                        }
                    }

                    //Parameterless call to notify will force OS to update all icons
                    _trace.writeToLog(9, "IconOverlay: pInitializeOrReplace. Notify Explorer to refresh all icons.");
                    NotifySystemForBadgeUpdate();
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: pInitializeOrReplace: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Callback driven when deletes occur in allBadges
        /// <param name="recursivePathBeingDeleted">The recursive path being deleted as a result of the originalDeletedPath being deleted.</param>
        /// <param name="forBadgeType">The recursive associated badge type.</param>
        /// <param name="originalDeletedPath">The original path that was deleted, potentially causing a series of other node deletes.</param>
        /// </summary>
        private void OnAllBadgesRecursiveDelete(FilePath recursivePathBeingDeleted, GenericHolder<cloudAppIconBadgeType> forBadgeType, FilePath originalDeletedPath)
        {
            // No need to notify system because the files are already gone
        }

        /// <summary>
        /// Callback driven when renames occur in allBadges.
        /// </summary>
        /// <param name="recursiveOldPath">The recursive path being renamed (old path) as a result of the originalOldPath being renamed.</param>
        /// <param name="recursiveRebuiltNewPath">The recursive renamed path (new path) as a result of the originalOldPath being renamed.</param>
        /// <param name="recursiveOldPathBadgeType">The associated badge type of the recursive path being renamed.</param>
        /// <param name="originalOldPath">The original old path being renamed that caused this recursive rename.</param>
        /// <param name="originalNewPath">The original new path that caused this recursive rename.</param>
        private void OnAllBadgesRecursiveRename(FilePath recursiveOldPath, FilePath recursiveRebuiltNewPath, GenericHolder<cloudAppIconBadgeType> recursiveOldPathBadgeType, FilePath originalOldPath, FilePath originalNewPath)
        {
            // Need to notify operating system that inner files/folders get their badges updated
            NotifySystemForBadgeUpdate(recursiveRebuiltNewPath);
        }

        public static CLError DeleteBadgePath(FilePath toDelete, out bool isDeleted)
        {
            try
            {
                return Instance.pDeleteBadgePath(toDelete, out isDeleted);
            }
            catch (Exception ex)
            {
                isDeleted = Helpers.DefaultForType<bool>();
                return ex;
            }
        }
        private CLError pDeleteBadgePath(FilePath toDelete, out bool isDeleted)
        {
            try
            {

                lock (this)
                {
                    if (!isInitialized)
                    {
                        _trace.writeToLog(9, "IconOverlay: pRenameBadgePath. ERROR: THROW: Must be initialized before renaming badge paths.");
                        throw new Exception("IconOverlay must be initialized before renaming badge paths");
                    }

                    // lock internal list during modification
                    lock (allBadges)
                    {
                        // Simply pass this action on to the badge dictionary.  The dictionary will pass recursive deletes back to us,
                        // but if the children do not exist we do not need to notify the system anyways
                        _trace.writeToLog(9, "IconOverlay: pRenameBadgePath. Pass this delete to the dictionary.");
                        isDeleted = allBadges.Remove(toDelete);
                    }
                }
            }
            catch (Exception ex)
            {
                isDeleted = Helpers.DefaultForType<bool>();
                return ex;
            }
            return null;
        }

        /// <summary>
        /// When a path is renamed, the badges must be adjusted to reflect the new path.
        /// </summary>
        /// <param name="oldPath">The old path.</param>
        /// <param name="newPath">The new path.</param>
        /// <returns></returns>
        public static CLError RenameBadgePath(FilePath oldPath, FilePath newPath)
        {
            try
            {
                _trace.writeToLog(9, "IconOverlay: RenameBadgePath. Old path: {0}, New path: {1}.", oldPath.ToString(), newPath.ToString());
                return Instance.pRenameBadgePath(oldPath, newPath);
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: RenameBadgePath: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                return error;
            }
        }
        private CLError pRenameBadgePath(FilePath oldPath, FilePath newPath)
        {
            try
            {
                // ensure this is initialized
                _trace.writeToLog(9, "IconOverlay: pRenameBadgePath. Old path: {0}, New path: {1}.", oldPath.ToString(), newPath.ToString());
                lock (this)
                {
                    if (!isInitialized)
                    {
                        _trace.writeToLog(9, "IconOverlay: pRenameBadgePath. ERROR: THROW: Must be initialized before renaming badge paths.");
                        throw new Exception("IconOverlay must be initialized before renaming badge paths");
                    }
                }

                // lock internal list during modification
                lock (allBadges)
                {
                    // Simply pass this action on to the badge dictionary.  The dictionary will pass recursive renames back to us
                    // as the rename is processes, and those recursive renames will cause the badges to be adjusted.
                    _trace.writeToLog(9, "IconOverlay: pRenameBadgePath. Pass this rename to the dictionary.");
                    // Put in a check if both paths already have values so we can overwrite at the new path
                    if (allBadges.ContainsKey(oldPath)
                        && allBadges.ContainsKey(newPath))
                    {
                        allBadges.Remove(newPath);
                    }
                    return allBadges.Rename(oldPath, newPath);
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: pRenameBadgePath: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                return error;
            }
        }

        /// <summary>
        /// Changes badge displayed on icon overlay to new type or removes badge. IconOverlay must be initialized first
        /// </summary>
        /// <param name="filePath">path of file to badge/unbadge, must not be null nor empty</param>
        /// <param name="newType">new badge type (use null to remove badge)</param>
        public static CLError setBadgeType(GenericHolder<cloudAppIconBadgeType> type, FilePath forFileAtPath)
        {
            try
            {
                //_trace.writeToLog(9, "IconOverlay: setBadgeType. Path: {0}, Type: {1}.", forFileAtPath.ToString(), type.Value.ToString());
                return Instance.pSetBadgeType(forFileAtPath, type);
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: setBadgeType: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                return ex;
            }
        }
        private CLError pSetBadgeType(FilePath filePath, GenericHolder<cloudAppIconBadgeType> newType)
        {
            try
            {
                // ensure this is initialized
                //_trace.writeToLog(9, "IconOverlay: pSetBadgeType. Path: {0}, Type: {1}.", filePath.ToString(), newType.Value.ToString());
                lock (this)
                {
                    if (!isInitialized)
                    {
                        //_trace.writeToLog(9, "IconOverlay: pSetBadgeType. ERROR: THROW: Must be initialized before setting badges.");
                        throw new Exception("IconOverlay must be initialized before setting badges");
                    }
                }

                // lock internal list during modification
                lock (allBadges)
                {
                    // newType is null means synced.  If the type is synced, newType will be null.  Set it whatever it is.
                    //_trace.writeToLog(9, "IconOverlay: pSetBadgeType. Add this type to the dictionary.");
                    if (newType.Value == cloudAppIconBadgeType.cloudAppBadgeSynced)
                    {
                        allBadges[filePath] = null;
                    }
                    else
                    {
                        allBadges[filePath] = newType;
                    }
                }

                // Notify this node, and all of the parents until the node is null, or equal to the Cloud path.
                FilePath node = filePath;
                while (node != null)
                {
                    // Notify the file system that this icon needs to be updated.
                    //_trace.writeToLog(9, "IconOverlay: pSetBadgeType. Notify Explorer for path {0}.", node.ToString());
                    NotifySystemForBadgeUpdate(node);

                    // Quit if we notified the Cloud directory root
                    if (FilePathComparer.Instance.Equals(node, filePathCloudDirectory))
                    {
                        //_trace.writeToLog(9, "IconOverlay: pSetBadgeType. Break.  At Cloud root.");
                        break;
                    }

                    // Chain up
                    node = node.Parent;
                }
                //_trace.writeToLog(9, "IconOverlay: pSetBadgeType. Exit.");
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: pSetBadgeType: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Returns badge type for a given file path, if it exists
        /// </summary>
        /// <param name="filePath">path of file to check</param>
        /// <returns></returns>
        public static CLError getBadgeTypeForFileAtPath(FilePath path, out GenericHolder<cloudAppIconBadgeType> badgeType)
        {
            try
            {
                return Instance.pFindBadge(path, out badgeType);
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: getBadgeTypeForFileAtPath: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                badgeType = new GenericHolder<cloudAppIconBadgeType>(cloudAppIconBadgeType.cloudAppBadgeSynced);
                return ex;
            }
        }
        private CLError pFindBadge(FilePath filePath, out GenericHolder<cloudAppIconBadgeType> badgeType)
        {
            try
            {
                // lock on dictionary so it is not modified during lookup
                lock (allBadges)
                {
                    foreach (cloudAppIconBadgeType currentBadge in Enum.GetValues(typeof(cloudAppIconBadgeType)).Cast<cloudAppIconBadgeType>())
                    {
                        if (ShouldIconBeBadged(currentBadge, filePath.ToString()))
                        {
                            badgeType = new GenericHolder<cloudAppIconBadgeType>(currentBadge);
                            return null;
                        }
                    }

                    badgeType = null;
                    return null;
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: pFindBadge: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                badgeType = new GenericHolder<cloudAppIconBadgeType>(cloudAppIconBadgeType.cloudAppBadgeSynced);
                return ex;
            }
        }

        // Add public rename event here which takes path and badgeType; it will run rename on allPaths FilePathDictionary, if exception is caught add at new location

        // The functionality of clearAllBadges is implemented by shutting down the badge service (confirmed with Gus/Steve that badging only stops when service is killed)
        /// <summary>
        /// Call this on application shutdown to clean out open named pipes to badge COM objects
        /// and to notify the system immediately to remove badges. Do not initialize again after shutting down
        /// </summary>
        public static CLError Shutdown()
        {
            _trace.writeToLog(9, "IconOverlay: Shutdown.  Entry.");
            return Instance.pShutdown();
        }
        public CLError pShutdown()
        {
            try
            {
                ((IDisposable)this).Dispose();
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: pShutdown: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
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
        // Standard IDisposable implementation based on MSDN System.IDisposable
        private void Dispose(bool disposing)
        {
            if (!this.Disposed)
            {
                // lock on current object for changing RunningStatus so it cannot be stopped/started simultaneously
                _trace.writeToLog(9, "IconOverlay: Dispose.  Lock.");
                lock (this)
                {
                    // monitor is now set as disposed which will produce errors if startup is called later
                    _trace.writeToLog(9, "IconOverlay: Dispose.  Set Disposed.");
                    Disposed = true;
                }

                // Run dispose on inner managed objects based on disposing condition
                if (disposing)
                {
                    // locks on this in case initialization is occurring simultaneously
                    _trace.writeToLog(9, "IconOverlay: Dispose. Disposing.");
                    lock (this)
                    {
                        // only need to shutdown if it was initialized
                        if (isInitialized)
                        {
                            // lock on object containing intial pipe connection running state
                            _trace.writeToLog(9, "IconOverlay: Dispose. Initialized.");
                            lock (pipeLocker)
                            {
                                // set runningstate to off
                                _trace.writeToLog(9, "IconOverlay: Dispose. PipeLocker.");
                                pipeLocker.pipeRunning = false;

                                // Dispose the badging streams
                                foreach (KeyValuePair<cloudAppIconBadgeType, NamedPipeServerBadge> currentStreamToKill in pipeBadgeServers)
                                {
                                    try
                                    {
                                        // cleanup initial pipe connection
                                        try
                                        {
                                            _trace.writeToLog(9, "IconOverlay: Dispose. Stop NamedPipeBadge for badge type {0}.", currentStreamToKill.Key.ToString());
                                            currentStreamToKill.Value.Stop();
                                        }
                                        catch
                                        {
                                            _trace.writeToLog(1, "IconOverlay: Dispose. ERROR: Exception stopping NamedPipeBadge for badge type {0}.", currentStreamToKill.Key.ToString());
                                        }
                                    }
                                    catch
                                    {
                                        _trace.writeToLog(1, "IconOverlay: Dispose. ERROR: Exception (2).");
                                    }
                                }
                                pipeBadgeServers.Clear();

                                // Dispose the context menu stream
                                try
                                {
                                    // cleanup initial pipe connection

                                    try
                                    {
                                        pipeContextMenuServer.Stop();
                                    }
                                    catch
                                    {
                                        _trace.writeToLog(1, "IconOverlay: Dispose. ERROR: Exception stopping NamedPipeServerContextMenu for context menu.");
                                    }
                                }
                                catch
                                {
                                    _trace.writeToLog(1, "IconOverlay: Dispose. ERROR: Exception (3).");
                                }
                            }
                        }
                    }
                }

                // Dispose local unmanaged resources last
                NotifySystemForBadgeUpdate();
            }
        }

        private bool Disposed = false;

        /// <summary>
        /// The Cloud directory path captured as a FilePath at initialization.
        /// </summary>
        private FilePath filePathCloudDirectory { get; set; }

        /// <summary>
        /// The hierarhical dictionary that holds all of the badges.  Nodes with null values are assumed to be synced.
        /// </summary>
        FilePathDictionary<GenericHolder<cloudAppIconBadgeType>> allBadges;

        #region notify system of change
        // important (including everything inside)
        /// <summary>
        /// Uses Win32 API call to refresh all icons (if filePath is not specified)
        /// or a single icon (if filePath is specified)
        /// </summary>
        /// <param name="filePath">Filepath for refresing single icon (case-sensitive)</param>
        private static void NotifySystemForBadgeUpdate(FilePath filePath = null)
        {
            //_trace.writeToLog(9, "IconOverlay: NotifySystemForBadgeUpdate. Entry.");
            if (filePath == null)
            {
                //The following will refresh all icons, does not force OS to reload relevant COM objects
                //    (for now I test after restarting explorer.exe)
                //_trace.writeToLog(9, "IconOverlay: NotifySystemForBadgeUpdate. Refresh all icons.");
                NativeMethods.SHChangeNotify(NativeMethods.HChangeNotifyEventID.SHCNE_ASSOCCHANGED,
                    NativeMethods.HChangeNotifyFlags.SHCNF_IDLIST,
                    IntPtr.Zero,
                    IntPtr.Zero);
            }
            else
            {
                //Instantiate IntPtr outside of try so it can be cleaned up
                //_trace.writeToLog(9, "IconOverlay: NotifySystemForBadgeUpdate. Notify for path: {0}.", filePath.ToString());
                IntPtr filePtr = IntPtr.Zero;
                try
                {
                    //Set IntPtr to null-terminated ANSI string of full file path
                    //I tried StringToHGlobalUni but it did not work
                    //Also, StringtoHGlobalAuto does not work either
                    filePtr = Marshal.StringToHGlobalAnsi(filePath.ToString());

                    //Notify that attributes have changed on the file at the path provided by the IntPtr (which updates its icon)
                    //_trace.writeToLog(9, "IconOverlay: NotifySystemForBadgeUpdate. Call SHChangeNotify.");
                    //SHChangeNotify(HChangeNotifyEventID.SHCNE_UPDATEITEM,
                    //    HChangeNotifyFlags.SHCNF_PATHA,
                    //    filePtr,
                    //    IntPtr.Zero);
                    NativeMethods.SHChangeNotify(NativeMethods.HChangeNotifyEventID.SHCNE_ATTRIBUTES,
                        NativeMethods.HChangeNotifyFlags.SHCNF_PATHA,
                        filePtr,
                        IntPtr.Zero);
                }
                finally
                {
                    //If IntPtr was not zero, free its memory
                    if (filePtr.ToInt64() != 0L)
                    {
                        //_trace.writeToLog(9, "IconOverlay: NotifySystemForBadgeUpdate. Free global memory.");
                        Marshal.FreeHGlobal(filePtr);
                    }
                }
            }
        }
        #endregion

        #region methods to interface with BadgeCOM
        #region variables, constants, and local classes
        /// <summary>
        /// Constant pipename for initial badging connections and appended for unique return connections
        /// (must match pipename used by COM objects)
        /// </summary>
        private const string PipeName = "BadgeCOM";

        /// <summary>
        /// Lockable object used to store running state of the initial badging connection pipe
        /// </summary>
        private pipeRunningHolder pipeLocker = new pipeRunningHolder()
        {
            pipeRunning = true
        };

        /// <summary>
        /// Creates the named pipe server streams to handle badge type communications from the COM object icon overlays
        /// </summary>
        private Dictionary<cloudAppIconBadgeType, NamedPipeServerBadge> pipeBadgeServers = new Dictionary<cloudAppIconBadgeType, NamedPipeServerBadge>()
        {
            { cloudAppIconBadgeType.cloudAppBadgeSyncing, null },
            { cloudAppIconBadgeType.cloudAppBadgeSynced, null },
            { cloudAppIconBadgeType.cloudAppBadgeSyncSelective, null },
            { cloudAppIconBadgeType.cloudAppBadgeFailed, null }
        };

        /// <summary>
        /// Creates the named pipe server stream for the shell extension context menu support.
        /// </summary>
        private NamedPipeServerContextMenu pipeContextMenuServer = null;

        /// <summary>
        /// Object type of pipeLocker
        /// (Lockable object storing running state of the initial badging connection pipe)
        /// </summary>
        private class pipeRunningHolder
        {
            public bool pipeRunning { get; set; }
        }
        #endregion

        /// <summary>
        /// Initializes listener threads for NamedPipeServerStreams to talk to BadgeCOM objects
        /// </summary>
        private void StartBadgeCOMPipes()
        {
            try
            {
                // create the processing threads for each server stream (one for each badge type)
                _trace.writeToLog(9, "IconOverlay: StartBadgeCOMPipes. Entry.");
                foreach (cloudAppIconBadgeType currentBadgeType in new List<cloudAppIconBadgeType>(pipeBadgeServers.Keys))
                {
                    // Create a thread to handle this badge type
                    NamedPipeServerBadge serverBadge = new NamedPipeServerBadge();
                    serverBadge.UserState = new NamedPipeServerBadge_UserState { BadgeType = currentBadgeType, ShouldIconBeBadged = this.ShouldIconBeBadged, FilePathCloudDirectory = filePathCloudDirectory };
                    serverBadge.PipeName = Environment.UserName + "/" + PipeName + currentBadgeType;
                    serverBadge.Run();

                    // Remember this thread for Dispose.
                    pipeBadgeServers[currentBadgeType] = serverBadge;
                }

                // Set up the thread params to start the pipe to listen to shell extension context menu messages
                _trace.writeToLog(9, "IconOverlay: StartBadgeCOMPipes. Start new server pipe for the context menu.");
                NamedPipeServerContextMenu serverContextMenu = new NamedPipeServerContextMenu();
                serverContextMenu.UserState = new NamedPipeServerContextMenu_UserState { FilePathCloudDirectory = filePathCloudDirectory };
                serverContextMenu.PipeName = Environment.UserName + "/" + PipeName + "/ContextMenu";
                serverContextMenu.Run();

                // Remember this thread for Dispose
                pipeContextMenuServer = serverContextMenu;
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: StartBadgeCOMPipes: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
            }
        }

        /// <summary>
        /// Determine whether this icon should be badged by this badge handler.
        /// </summary>
        /// <param name="pipeParams"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private bool ShouldIconBeBadged(
                                    cloudAppIconBadgeType badgeType,
                                    string filePath)
        {
            try
            {
                //_trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Entry.");
                // Convert the badgetype and filepath to objects.
                FilePath objFilePath = filePath;
                GenericHolder<cloudAppIconBadgeType> objBadgeType = new GenericHolder<cloudAppIconBadgeType>(badgeType);

                // Lock and query the in-memory database.
                lock (allBadges)
                {
                    // There will be no badge if the path doesn't contain Cloud root
                    //_trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Locked.");
                    if (objFilePath.Contains(filePathCloudDirectory))
                    {
                        // If the value at this path is set and it is our type, then badge.
                        //_trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Contains Cloud root.");
                        GenericHolder<cloudAppIconBadgeType> tempBadgeType;
                        bool success = allBadges.TryGetValue(objFilePath, out tempBadgeType);
                        if (success)
                        {
                            bool rc = (tempBadgeType.Value == objBadgeType.Value);
                            //_trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Return: {0}.", rc);
                            return rc;
                        }
                        else
                        {
                            // If an item is marked selective, then none of its children (whole hierarchy of children) should be badged.
                            //_trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. TryGetValue not successful.");
                            if (!FilePathComparer.Instance.Equals(objFilePath, filePathCloudDirectory))
                            {
                                // Recurse through parents of this node up to and including the CloudPath.
                                //_trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Recurse thru parents.");
                                FilePath node = objFilePath;
                                while (node != null)
                                {
                                    // Return false if the value of this node is not null, and is marked SyncSelective
                                    //_trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Get the type for path: {0}.", node.ToString());
                                    success = allBadges.TryGetValue(node, out tempBadgeType);
                                    if (success && tempBadgeType != null)
                                    {
                                        // Got the badge type at this level.
                                        //_trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Got type {0}.", tempBadgeType.Value.ToString());
                                        if (tempBadgeType.Value == cloudAppIconBadgeType.cloudAppBadgeSyncSelective)
                                        {
                                            //_trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Return false.");
                                            return false;
                                        }
                                    }

                                    // Quit if we notified the Cloud directory root
                                    //_trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Have we reached the Cloud root?");
                                    if (FilePathComparer.Instance.Equals(node, filePathCloudDirectory))
                                    {
                                        //_trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Break to determine the badge status from the children of this node.");
                                        break;
                                    }

                                    // Chain up
                                    //_trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Chain up.");
                                    node = node.Parent;
                                }
                            }

                            // Determine the badge type from the hierarchy at this path
                            return DetermineBadgeStatusFromHierarchyOfChildrenOfThisNode(badgeType, allBadges, objFilePath);
                        }
                    }
                    else
                    {
                        // This path is not in the Cloud folder.  Don't badge.
                        //_trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Not in the Cloud folder.  Don't badge.");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Exception.  Normal? Msg: {0}, Code: (1).", error.errorDescription, error.errorCode);
                return false;
            }
        }

        /// <summary>
        /// Determine whether we should badge with this badge type at this path.
        /// </summary>
        /// <param name="badgeType">The badge type.</param>
        /// <param name="AllBadges">The current badge dictionary.</param>
        /// <param name="objFilePath">The path to test.</param>
        /// <returns></returns>
        private bool DetermineBadgeStatusFromHierarchyOfChildrenOfThisNode(cloudAppIconBadgeType badgeType, FilePathDictionary<GenericHolder<cloudAppIconBadgeType>> AllBadges, FilePath objFilePath)
        {
            try
            {
                // Get the hierarchy of children of this node.
                //_trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Get the hierarchy for path: {0}.", objFilePath.ToString());
                FilePathHierarchicalNode<GenericHolder<cloudAppIconBadgeType>> tree;
                CLError error = AllBadges.GrabHierarchyForPath(objFilePath, out tree, suppressException: true);
                if (error == null
                    && tree != null)
                {
                    //_trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Successful getting the hierarcy.  Call GetDesiredBadgeTypeViaRecursivePostorderTraversal.");
                    // Chase the children hierarchy using recursive postorder traversal to determine the desired badge type.
                    cloudAppIconBadgeType desiredBadgeType = GetDesiredBadgeTypeViaRecursivePostorderTraversal(tree);
                    bool rc = (badgeType == desiredBadgeType);
                    //_trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Return(2): {0}.", rc);
                    return rc;
                }
                else
                {
                    bool rc = (badgeType == cloudAppIconBadgeType.cloudAppBadgeSynced);
                    //_trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Return(3): {0}.", rc);
                    return rc;
                }
            }
            catch
            {
                bool rc = (badgeType == cloudAppIconBadgeType.cloudAppBadgeSynced);
                _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. ERROR: Exception. Return(4): {0}.", rc);
                return rc;
            }
        }

        /// <summary>
        /// Determine the desired badge type of a node based on the badging state of its children.
        /// </summary>
        /// <param name="node">The selected node.</param>
        /// <returns>cloudAddIconBadgeType: The desired badge type.</returns>
        private cloudAppIconBadgeType GetDesiredBadgeTypeViaRecursivePostorderTraversal(FilePathHierarchicalNode<GenericHolder<cloudAppIconBadgeType>> node)
        {
            // If the node doesn't exist, that means synced
            if (node == null)
            {
                return cloudAppIconBadgeType.cloudAppBadgeSynced;
            }

            // Loop through all of the node's children
            foreach (FilePathHierarchicalNode<GenericHolder<cloudAppIconBadgeType>> child in node.Children)
            {
                cloudAppIconBadgeType returnBadgeType = GetDesiredBadgeTypeViaRecursivePostorderTraversal(child);
                if (returnBadgeType != cloudAppIconBadgeType.cloudAppBadgeSynced)
                {
                    return returnBadgeType;
                }
            }

            // Process by whether the node has a value.  If not, it is synced.
            if (node.HasValue)
            {
                switch (node.Value.Value.Value)
                {
                    case cloudAppIconBadgeType.cloudAppBadgeSynced:
                        return cloudAppIconBadgeType.cloudAppBadgeSynced;
                    case cloudAppIconBadgeType.cloudAppBadgeSyncing:
                        return cloudAppIconBadgeType.cloudAppBadgeSyncing;
                    case cloudAppIconBadgeType.cloudAppBadgeFailed:
                        return cloudAppIconBadgeType.cloudAppBadgeSyncing;
                    case cloudAppIconBadgeType.cloudAppBadgeSyncSelective:
                        return cloudAppIconBadgeType.cloudAppBadgeSynced;
                }
            }
            else
            {
                return cloudAppIconBadgeType.cloudAppBadgeSynced;
            }

            return cloudAppIconBadgeType.cloudAppBadgeSynced;
        }
        #endregion

        #region badge queue
        public static void QueueNewEventBadge(FileChange mergedEvent, FileChange eventToRemove)
        {
            QueueBadgeParams(new badgeParams.newEvent(mergedEvent, eventToRemove));
        }
        public static void QueueSetBadge(cloudAppIconBadgeType badgeType, FilePath badgePath)
        {
            QueueBadgeParams(new badgeParams.genericSetter(badgeType, badgePath));
        }
        private static readonly Queue<badgeParams.baseParams> setBadgeQueue = new Queue<badgeParams.baseParams>();
        private static bool badgesQueueing = false;
        private static void QueueBadgeParams(badgeParams.baseParams toQueue)
        {
            lock (Instance)
            {
                if (_instance.Disposed)
                {
                    return;
                }
            }

            lock (setBadgeQueue)
            {
                if (badgesQueueing)
                {
                    setBadgeQueue.Enqueue(toQueue);
                }
                else
                {
                    badgesQueueing = true;
                    ThreadPool.UnsafeQueueUserWorkItem(BadgeParamsProcessor, toQueue);
                }
            }
        }
        private static void BadgeParamsProcessor(object firstBadge)
        {
            badgeParams.baseParams firstBadgeCast = firstBadge as badgeParams.baseParams;

            if (firstBadgeCast != null)
            {
                firstBadgeCast.Process();
            }

            Func<badgeParams.baseParams> keepProcessing = () =>
            {
                lock (Instance)
                {
                    if (_instance.Disposed)
                    {
                        return null;
                    }
                }

                lock (setBadgeQueue)
                {
                    if (setBadgeQueue.Count == 0)
                    {
                        badgesQueueing = false;
                        return null;
                    }
                    return setBadgeQueue.Dequeue();
                }
            };

            badgeParams.baseParams currentToProcess;
            while ((currentToProcess = keepProcessing()) != null)
            {
                currentToProcess.Process();
            }
        }
        private static class badgeParams
        {
            public abstract class baseParams
            {
                protected baseParams() { }

                public abstract void Process();
            }

            public class genericSetter : baseParams
            {
                private cloudAppIconBadgeType badgeType;
                private FilePath badgePath;

                public genericSetter(cloudAppIconBadgeType badgeType, FilePath badgePath)
                    : base()
                {
                    this.badgeType = badgeType;
                    this.badgePath = badgePath;
                }

                public override void Process()
                {
                    IconOverlay.setBadgeType(new GenericHolder<cloudAppIconBadgeType>(badgeType), badgePath);
                }
            }

            public class newEvent : baseParams
            {
                private FileChange mergedEvent;
                private FileChange eventToRemove;

                public newEvent(FileChange mergedEvent, FileChange eventToRemove)
                    : base()
                {
                    this.mergedEvent = mergedEvent;
                    this.eventToRemove = eventToRemove;
                }

                public override void Process()
                {
                    Action<cloudAppIconBadgeType, FilePath> setBadge = (badgeType, badgePath) =>
                    {
                        IconOverlay.setBadgeType(new GenericHolder<cloudAppIconBadgeType>(badgeType), badgePath);
                    };

                    // Update the badges for this merged event.
                    //TODO: Do we need to do anything with the eventToRemove?
                    if (mergedEvent != null)
                    {
                        switch (mergedEvent.Type)
                        {
                            case FileChangeType.Deleted:
                                switch (mergedEvent.Direction)
                                {
                                    case SyncDirection.From:
                                        setBadge(cloudAppIconBadgeType.cloudAppBadgeSyncing, mergedEvent.NewPath);
                                        break;
                                    case SyncDirection.To:
                                        bool isDeleted;
                                        IconOverlay.DeleteBadgePath(mergedEvent.NewPath, out isDeleted);
                                        break;
                                    default:
                                        throw new NotSupportedException("Unknown mergedEvent.Direction: " + mergedEvent.Direction.ToString());
                                }
                                break;
                            case FileChangeType.Created:
                            case FileChangeType.Modified:
                                setBadge(cloudAppIconBadgeType.cloudAppBadgeSyncing, mergedEvent.NewPath);
                                break;
                            case FileChangeType.Renamed:
                                switch (mergedEvent.Direction)
                                {
                                    case SyncDirection.From:
                                        setBadge(cloudAppIconBadgeType.cloudAppBadgeSyncing, mergedEvent.OldPath);
                                        break;
                                    case SyncDirection.To:
                                        IconOverlay.RenameBadgePath(mergedEvent.OldPath, mergedEvent.NewPath);

                                        setBadge(cloudAppIconBadgeType.cloudAppBadgeSyncing, mergedEvent.NewPath);
                                        break;
                                    default:
                                        throw new NotSupportedException("Unknown mergedEvent.Direction: " + mergedEvent.Direction.ToString());
                                }
                                break;
                            default:
                                throw new NotSupportedException("Unknown mergedEvent.Type: " + mergedEvent.Type.ToString());
                        }

                        if (eventToRemove != null)
                        {
                            throw new NotImplementedException("Have not handled badging when two events are merged together");
                        }
                    }
                    else if (eventToRemove != null)
                    {
                        setBadge(cloudAppIconBadgeType.cloudAppBadgeSynced, eventToRemove.NewPath);
                    }
                }
            }
        }
        #endregion
    }
}
