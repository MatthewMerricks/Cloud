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
using System.Runtime.InteropServices;
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

namespace BadgeNET
{
    /// <summary>
    /// IconOverlay is responsible for keeping a list of badges and synchronizing them with BadgeCOM (the Windows shell extensions for icon overlays)
    /// </summary>
    public class IconOverlay : IDisposable
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
                            allBadges.Add(initialListArray[initialListCounter].Key, initialListArray[initialListCounter].Value);
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
            // This path node is deleted.  Set its badge type to synced, which is the default and the same as not being there.
            setBadgeType(new GenericHolder<cloudAppIconBadgeType>(cloudAppIconBadgeType.cloudAppBadgeSynced), recursivePathBeingDeleted);
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
            // Remove the badge at the old location.
            setBadgeType(recursiveOldPathBadgeType, recursiveOldPath);

            // Add the same badge at the new location.
            setBadgeType(recursiveOldPathBadgeType, recursiveRebuiltNewPath);
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
            CLError error = null;

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
                    error = allBadges.Rename(oldPath, newPath);
                    return error;
                }
            }
            catch (Exception ex)
            {
                error += ex;
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
                _trace.writeToLog(9, "IconOverlay: setBadgeType. Path: {0}, Type: {1}.", forFileAtPath.ToString(), type.Value.ToString());
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
                _trace.writeToLog(9, "IconOverlay: pSetBadgeType. Path: {0}, Type: {1}.", filePath.ToString(), newType.Value.ToString());
                lock (this)
                {
                    if (!isInitialized)
                    {
                        _trace.writeToLog(9, "IconOverlay: pSetBadgeType. ERROR: THROW: Must be initialized before setting badges.");
                        throw new Exception("IconOverlay must be initialized before setting badges");
                    }
                }

                // lock internal list during modification
                lock (allBadges)
                {
                    // newType is null means synced.  If the type is synced, newType will be null.  Set it whatever it is.
                    _trace.writeToLog(9, "IconOverlay: pSetBadgeType. Add this type to the dictionary.");
                    allBadges[filePath] = newType;
                }

                // Notify this node, and all of the parents until the node is null, or equal to the Cloud path.
                FilePath node = filePath;
                while (node != null)
                {
                    // Notify the file system that this icon needs to be updated.
                    _trace.writeToLog(9, "IconOverlay: pSetBadgeType. Notify Explorer for path {0}.", node.ToString());
                    NotifySystemForBadgeUpdate(node);

                    // Quit if we notified the Cloud directory root
                    if (FilePathComparer.Instance.Equals(node, filePathCloudDirectory))
                    {
                        _trace.writeToLog(9, "IconOverlay: pSetBadgeType. Break.  At Cloud root.");
                        break;
                    }

                    // Chain up
                    node = node.Parent;
                }
                _trace.writeToLog(9, "IconOverlay: pSetBadgeType. Exit.");
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
                    // return badgetype if it exists, otherwise null
                    GenericHolder<cloudAppIconBadgeType> tempBadgeType;
                    bool success = allBadges.TryGetValue(filePath, out tempBadgeType);
                    if (success)
                    {
                        badgeType = tempBadgeType;
                    }
                    else
                    {
                        badgeType = new GenericHolder<cloudAppIconBadgeType>(cloudAppIconBadgeType.cloudAppBadgeSynced);
                    }
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: pFindBadge: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                badgeType = new GenericHolder<cloudAppIconBadgeType>(cloudAppIconBadgeType.cloudAppBadgeSynced);
                return ex;
            }
            return null;
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
            _trace.writeToLog(9, "IconOverlay: NotifySystemForBadgeUpdate. Entry.");
            if (string.IsNullOrEmpty(filePath.ToString()))
            {
                //The following will refresh all icons, does not force OS to reload relevant COM objects
                //    (for now I test after restarting explorer.exe)
                _trace.writeToLog(9, "IconOverlay: NotifySystemForBadgeUpdate. Refresh all icons.");
                SHChangeNotify(HChangeNotifyEventID.SHCNE_ASSOCCHANGED,
                    HChangeNotifyFlags.SHCNF_IDLIST,
                    IntPtr.Zero,
                    IntPtr.Zero);
            }
            else
            {
                //Instantiate IntPtr outside of try so it can be cleaned up
                _trace.writeToLog(9, "IconOverlay: NotifySystemForBadgeUpdate. Notify for path: {0}.", filePath.ToString());
                IntPtr filePtr = IntPtr.Zero;
                try
                {
                    //Set IntPtr to null-terminated ANSI string of full file path
                    //I tried StringToHGlobalUni but it did not work
                    //Also, StringtoHGlobalAuto does not work either
                    filePtr = Marshal.StringToHGlobalAnsi(filePath.ToString());

                    //Notify that attributes have changed on the file at the path provided by the IntPtr (which updates its icon)
                    _trace.writeToLog(9, "IconOverlay: NotifySystemForBadgeUpdate. Call SHChangeNotify.");
                    SHChangeNotify(HChangeNotifyEventID.SHCNE_UPDATEITEM,
                        HChangeNotifyFlags.SHCNF_PATHA,
                        filePtr,
                        IntPtr.Zero);
                    //SHChangeNotify(HChangeNotifyEventID.SHCNE_ATTRIBUTES,
                    //    HChangeNotifyFlags.SHCNF_PATHA,
                    //    filePtr,
                    //    IntPtr.Zero);
                }
                finally
                {
                    //If IntPtr was not zero, free its memory
                    if (filePtr.ToInt64() != 0L)
                    {
                        _trace.writeToLog(9, "IconOverlay: NotifySystemForBadgeUpdate. Free global memory.");
                        Marshal.FreeHGlobal(filePtr);
                    }
                }
            }
        }

        #region shell32.dll SHChangeNotify
        /// <summary>
        /// This is the Win32 API call to force a refresh (for all icons or for a single one)
        /// </summary>
        /// <param name="wEventId">Use SHCNE_ASSOCCHANGED for all icons (makes everything blink) or SHCNE_ATTRIBUTES for one icon</param>
        /// <param name="uFlags">Check which value to use based on previous param (HChangeNotifyEventID wEventId)</param>
        /// <param name="dwItem1">Points to the single item or nothing for all icons</param>
        /// <param name="dwItem2">Points to nothing</param>
        [DllImport("shell32.dll")]
        static extern void SHChangeNotify(HChangeNotifyEventID wEventId,
            HChangeNotifyFlags uFlags,
            IntPtr dwItem1,
            IntPtr dwItem2);
        #region enum HChangeNotifyEventID
        /// <summary>
        /// Describes the event that has occurred. 
        /// Typically, only one event is specified at a time. 
        /// If more than one event is specified, the values contained 
        /// in the <i>dwItem1</i> and <i>dwItem2</i> 
        /// parameters must be the same, respectively, for all specified events. 
        /// This parameter can be one or more of the following values. 
        /// </summary>
        /// <remarks>
        /// <para><b>Windows NT/2000/XP:</b> <i>dwItem2</i> contains the index 
        /// in the system image list that has changed. 
        /// <i>dwItem1</i> is not used and should be <see langword="null"/>.</para>
        /// <para><b>Windows 95/98:</b> <i>dwItem1</i> contains the index 
        /// in the system image list that has changed. 
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.</para>
        /// </remarks>
        [Flags]
        private enum HChangeNotifyEventID
        {
            /// <summary>
            /// All events have occurred. 
            /// </summary>
            SHCNE_ALLEVENTS = 0x7FFFFFFF,

            /// <summary>
            /// A file type association has changed. <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> 
            /// must be specified in the <i>uFlags</i> parameter. 
            /// <i>dwItem1</i> and <i>dwItem2</i> are not used and must be <see langword="null"/>. 
            /// </summary>
            SHCNE_ASSOCCHANGED = 0x08000000,

            /// <summary>
            /// The attributes of an item or folder have changed. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the item or folder that has changed. 
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
            /// </summary>
            SHCNE_ATTRIBUTES = 0x00000800,

            /// <summary>
            /// A nonfolder item has been created. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the item that was created. 
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
            /// </summary>
            SHCNE_CREATE = 0x00000002,

            /// <summary>
            /// A nonfolder item has been deleted. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the item that was deleted. 
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
            /// </summary>
            SHCNE_DELETE = 0x00000004,

            /// <summary>
            /// A drive has been added. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the root of the drive that was added. 
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
            /// </summary>
            SHCNE_DRIVEADD = 0x00000100,

            /// <summary>
            /// A drive has been added and the Shell should create a new window for the drive. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the root of the drive that was added. 
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
            /// </summary>
            SHCNE_DRIVEADDGUI = 0x00010000,

            /// <summary>
            /// A drive has been removed. <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the root of the drive that was removed.
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
            /// </summary>
            SHCNE_DRIVEREMOVED = 0x00000080,

            /// <summary>
            /// Not currently used. 
            /// </summary>
            SHCNE_EXTENDED_EVENT = 0x04000000,

            /// <summary>
            /// The amount of free space on a drive has changed. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the root of the drive on which the free space changed.
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
            /// </summary>
            SHCNE_FREESPACE = 0x00040000,

            /// <summary>
            /// Storage media has been inserted into a drive. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the root of the drive that contains the new media. 
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
            /// </summary>
            SHCNE_MEDIAINSERTED = 0x00000020,

            /// <summary>
            /// Storage media has been removed from a drive. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the root of the drive from which the media was removed. 
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
            /// </summary>
            SHCNE_MEDIAREMOVED = 0x00000040,

            /// <summary>
            /// A folder has been created. <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> 
            /// or <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the folder that was created. 
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
            /// </summary>
            SHCNE_MKDIR = 0x00000008,

            /// <summary>
            /// A folder on the local computer is being shared via the network. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the folder that is being shared. 
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
            /// </summary>
            SHCNE_NETSHARE = 0x00000200,

            /// <summary>
            /// A folder on the local computer is no longer being shared via the network. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the folder that is no longer being shared. 
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
            /// </summary>
            SHCNE_NETUNSHARE = 0x00000400,

            /// <summary>
            /// The name of a folder has changed. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the previous pointer to an item identifier list (PIDL) or name of the folder. 
            /// <i>dwItem2</i> contains the new PIDL or name of the folder. 
            /// </summary>
            SHCNE_RENAMEFOLDER = 0x00020000,

            /// <summary>
            /// The name of a nonfolder item has changed. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the previous PIDL or name of the item. 
            /// <i>dwItem2</i> contains the new PIDL or name of the item. 
            /// </summary>
            SHCNE_RENAMEITEM = 0x00000001,

            /// <summary>
            /// A folder has been removed. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the folder that was removed. 
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
            /// </summary>
            SHCNE_RMDIR = 0x00000010,

            /// <summary>
            /// The computer has disconnected from a server. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the server from which the computer was disconnected. 
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
            /// </summary>
            SHCNE_SERVERDISCONNECT = 0x00004000,

            /// <summary>
            /// The contents of an existing folder have changed, 
            /// but the folder still exists and has not been renamed. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the folder that has changed. 
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
            /// If a folder has been created, deleted, or renamed, use SHCNE_MKDIR, SHCNE_RMDIR, or 
            /// SHCNE_RENAMEFOLDER, respectively, instead. 
            /// </summary>
            SHCNE_UPDATEDIR = 0x00001000,

            /// <summary>
            /// An image in the system image list has changed. 
            /// <see cref="HChangeNotifyFlags.SHCNF_DWORD"/> must be specified in <i>uFlags</i>. 
            /// </summary>
            SHCNE_UPDATEIMAGE = 0x00008000,

            SHCNE_UPDATEITEM = 0x00002000,

        }
        #endregion // enum HChangeNotifyEventID
        #region public enum HChangeNotifyFlags
        /// <summary>
        /// Flags that indicate the meaning of the <i>dwItem1</i> and <i>dwItem2</i> parameters. 
        /// The uFlags parameter must be one of the following values.
        /// </summary>
        [Flags]
        private enum HChangeNotifyFlags
        {
            /// <summary>
            /// The <i>dwItem1</i> and <i>dwItem2</i> parameters are DWORD values. 
            /// </summary>
            SHCNF_DWORD = 0x0003,
            /// <summary>
            /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of ITEMIDLIST structures that 
            /// represent the item(s) affected by the change. 
            /// Each ITEMIDLIST must be relative to the desktop folder. 
            /// </summary>
            SHCNF_IDLIST = 0x0000,
            /// <summary>
            /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings of 
            /// maximum length MAX_PATH that contain the full path names 
            /// of the items affected by the change. 
            /// </summary>
            SHCNF_PATHA = 0x0001,
            /// <summary>
            /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings of 
            /// maximum length MAX_PATH that contain the full path names 
            /// of the items affected by the change. 
            /// </summary>
            SHCNF_PATHW = 0x0005,
            /// <summary>
            /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings that 
            /// represent the friendly names of the printer(s) affected by the change. 
            /// </summary>
            SHCNF_PRINTERA = 0x0002,
            /// <summary>
            /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings that 
            /// represent the friendly names of the printer(s) affected by the change. 
            /// </summary>
            SHCNF_PRINTERW = 0x0006,
            /// <summary>
            /// The function should not return until the notification 
            /// has been delivered to all affected components. 
            /// As this flag modifies other data-type flags, it cannot by used by itself.
            /// </summary>
            SHCNF_FLUSH = 0x1000,
            /// <summary>
            /// The function should begin delivering notifications to all affected components 
            /// but should return as soon as the notification process has begun. 
            /// As this flag modifies other data-type flags, it cannot by used by itself.
            /// </summary>
            SHCNF_FLUSHNOWAIT = 0x2000
        }
        #endregion // enum HChangeNotifyFlags
        #endregion
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
                    serverBadge.UserState = new NamedPipeServerBadge_UserState {BadgeType = currentBadgeType, AllBadges = allBadges, FilePathCloudDirectory = filePathCloudDirectory};
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
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: StartBadgeCOMPipes: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
            }
        }

        #endregion
    }
}
