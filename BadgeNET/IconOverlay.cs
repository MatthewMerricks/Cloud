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
                    recursiveDeleteCallback: null,          //TODO: Implement this?
                    recursiveRenameCallback: null);         //TODO: Implement this?
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
                            _trace.writeToLog(9, "IconOverlay: Add badge for path {0}, value {0}.", initialListArray[initialListCounter].Key, initialListArray[initialListCounter].Value);
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
                            _trace.writeToLog(9, "IconOverlay: pInitializeOrReplace. currentReplaceItem. Path {0}, Type: {1}).", currentReplacedItem.Key.ToString(), currentReplacedItem.Value.ToString());
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
        /// Changes badge displayed on icon overlay to new type or removes badge. IconOverlay must be initialized first
        /// </summary>
        /// <param name="filePath">path of file to badge/unbadge, must not be null nor empty</param>
        /// <param name="newType">new badge type (use null to remove badge)</param>
        public static CLError setBadgeType(GenericHolder<cloudAppIconBadgeType> type, FilePath forFileAtPath)
        {
            try
            {
                _trace.writeToLog(9, "IconOverlay: setBadgeType. Path: {0}, Type: {1}.", forFileAtPath.ToString(), type.ToString());
                return Instance.pSetBadgeType(forFileAtPath, type);
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: pInitializeOrReplace: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                return ex;
            }
        }
        private CLError pSetBadgeType(FilePath filePath, GenericHolder<cloudAppIconBadgeType> newType)
        {
            try
            {
                // ensure this is initialized
                _trace.writeToLog(9, "IconOverlay: pSetBadgeType. Path: {0}, Type: {1}.", filePath.ToString(), newType.ToString());
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
                                foreach (KeyValuePair<cloudAppIconBadgeType, NamedPipeServerStream> currentStreamToKill in pipeServerStreams)
                                {
                                    try
                                    {
                                        // cleanup initial pipe connection

                                        try
                                        {
                                            _trace.writeToLog(9, "IconOverlay: Dispose. Dispose NamedPipeStream for badge type {0}.", currentStreamToKill.Key.ToString());
                                            currentStreamToKill.Value.Dispose();
                                        }
                                        catch
                                        {
                                            _trace.writeToLog(1, "IconOverlay: Dispose. ERROR: Exception disposing NamedPipeStream for badge type {0}.", currentStreamToKill.Key.ToString());
                                        }

                                        //The following is not a fallacy in logic:
                                        //disposing a server stream is not guaranteed to stop a thread stuck on WaitForConnection
                                        //so we try to connect to it just in case (will most likely give an unauthorized exception)
                                        using (NamedPipeClientStream connectionKillerStream = new NamedPipeClientStream(".",
                                            Environment.UserName + "/" + PipeName + currentStreamToKill.Key,
                                            PipeDirection.Out,
                                            PipeOptions.None))
                                        {
                                            _trace.writeToLog(9, "IconOverlay: Dispose. Call connectionKillerStream.Connect.");
                                            connectionKillerStream.Connect(100);
                                        }
                                    }
                                    catch
                                    {
                                        _trace.writeToLog(1, "IconOverlay: Dispose. ERROR: Exception (2).");
                                    }
                                }
                                pipeServerStreams.Clear();

                                // Dispose the context menu stream
                                try
                                {
                                    // cleanup initial pipe connection

                                    try
                                    {
                                        pipeServerStreamContextMenu.Dispose();
                                    }
                                    catch
                                    {
                                        _trace.writeToLog(1, "IconOverlay: Dispose. ERROR: Exception disposing NamedPipeStream for context menu.");
                                    }

                                    //The following is not a fallacy in logic:
                                    //disposing a server stream is not guaranteed to stop a thread stuck on WaitForConnection
                                    //so we try to connect to it just in case (will most likely give an unauthorized exception)
                                    using (NamedPipeClientStream connectionKillerStream = new NamedPipeClientStream(".",
                                        Environment.UserName + "/" + PipeName + "/ContextMenu",
                                        PipeDirection.Out,
                                        PipeOptions.None))
                                    {
                                        connectionKillerStream.Connect(100);
                                    }
                                }
                                catch
                                {
                                    _trace.writeToLog(1, "IconOverlay: Dispose. ERROR: Exception (2).");
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
                _trace.writeToLog(9, "IconOverlay: NotifySystemForBadgeUpdate. Entry.  Path: {0}.", filePath.ToString());
                IntPtr filePtr = IntPtr.Zero;
                try
                {
                    //Set IntPtr to null-terminated ANSI string of full file path
                    //I tried StringToHGlobalUni but it did not work
                    //Also, StringtoHGlobalAuto does not work either
                    filePtr = Marshal.StringToHGlobalAnsi(filePath.ToString());

                    //Notify that attributes have changed on the file at the path provided by the IntPtr (which updates its icon)
                    SHChangeNotify(HChangeNotifyEventID.SHCNE_ATTRIBUTES,
                        HChangeNotifyFlags.SHCNF_PATHA,
                        filePtr,
                        IntPtr.Zero);
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
        private readonly Dictionary<cloudAppIconBadgeType, NamedPipeServerStream> pipeServerStreams = new Dictionary<cloudAppIconBadgeType, NamedPipeServerStream>()
        {
            { cloudAppIconBadgeType.cloudAppBadgeSyncing, new NamedPipeServerStream(Environment.UserName + "/" + PipeName + cloudAppIconBadgeType.cloudAppBadgeSyncing,
                PipeDirection.In,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.None) },
            { cloudAppIconBadgeType.cloudAppBadgeSynced, new NamedPipeServerStream(Environment.UserName + "/" + PipeName + cloudAppIconBadgeType.cloudAppBadgeSynced,
                PipeDirection.In,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.None) },
            { cloudAppIconBadgeType.cloudAppBadgeSyncSelective, new NamedPipeServerStream(Environment.UserName + "/" + PipeName + cloudAppIconBadgeType.cloudAppBadgeSyncSelective,
                PipeDirection.In,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.None) },
            { cloudAppIconBadgeType.cloudAppBadgeFailed, new NamedPipeServerStream(Environment.UserName + "/" + PipeName + cloudAppIconBadgeType.cloudAppBadgeFailed,
                PipeDirection.In,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.None) }
        };

        /// <summary>
        /// Creates the named pipe server stream for the shell extension context menu support.
        /// </summary>
        private readonly NamedPipeServerStream pipeServerStreamContextMenu = new NamedPipeServerStream(Environment.UserName + "/" + PipeName + "/ContextMenu",
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.None);

        /// <summary>
        /// Used for initial badging connection pipe thread as userstate
        /// </summary>
        private class pipeThreadParams
        {
            public NamedPipeServerStream serverStream { get; set; }
            public pipeRunningHolder serverLocker { get; set; }
            public cloudAppIconBadgeType currentBadgeType { get; set; }
        }

        /// <summary>
        /// Used for initial context menu connection pipe thread as userstate
        /// </summary>
        private class pipeThreadParamsContextMenu
        {
            public NamedPipeServerStream serverStream { get; set; }
            public pipeRunningHolder serverLocker { get; set; }
        }

        /// <summary>
        /// Object type of pipeLocker
        /// (Lockable object storing running state of the initial badging connection pipe)
        /// </summary>
        private class pipeRunningHolder
        {
            public bool pipeRunning { get; set; }
        }

        /// <summary>
        /// Used for unique return connection pipe threads as userstates
        /// </summary>
        private class returnPipeHolder
        {
            public string fullPipeName { get; set; }
            public byte returnData { get; set; }
        }

        /// <summary>
        /// Shared between the return connection pipe thread and its cleaning thread (as userstate)
        /// </summary>
        private class returnPipeRunningHolder
        {
            public bool connectionAchieved { get; set; }
            public NamedPipeServerStream returnStream { get; set; }
            public string fullPipeName { get; set; }
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
                foreach (KeyValuePair<cloudAppIconBadgeType, NamedPipeServerStream> currentStreamToProcess in pipeServerStreams)
                {
                    // important
                    // store a userstate for the thread that processes initial pipe connections with pipe server
                    // and a lockable object containing running state
                    _trace.writeToLog(9, "IconOverlay: StartBadgeCOMPipes. Start new server pipe for badge type: {0}.", currentStreamToProcess.Key.ToString());
                    pipeThreadParams threadParams = new pipeThreadParams()
                    {
                        serverStream = currentStreamToProcess.Value,
                        serverLocker = pipeLocker,
                        currentBadgeType = currentStreamToProcess.Key
                    };

                    // start a thread to process initial pipe connections, pass relevant userstate
                    (new Thread(() => RunServerPipe(threadParams))).Start();
                }

                // Set up the thread params to start the pipe to listen to shell extension context menu messages
                _trace.writeToLog(9, "IconOverlay: StartBadgeCOMPipes. Start new server pipe for the context menu.");
                pipeThreadParamsContextMenu threadParamsContextMenu = new pipeThreadParamsContextMenu()
                {
                    serverStream = pipeServerStreamContextMenu,
                    serverLocker = pipeLocker,
                };

                // start a thread to process initial pipe connections, pass relevant userstate
                (new Thread(() => RunServerPipeContextMenu(threadParamsContextMenu))).Start();
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: StartBadgeCOMPipes: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
            }
        }

        /// <summary>
        /// Processes a receiving server pipe to communicate with a BadgeCOM object
        /// </summary>
        /// <param name="pipeParams"></param>
        private void RunServerPipe(pipeThreadParams pipeParams)
        {
            // try/catch which silences errors and stops badging functionality (should never error here)
            try
            {
                // define locked function for checking running state
                _trace.writeToLog(9, "IconOverlay: RunServerPipe. Entry.");
                Func<pipeRunningHolder, bool> getPipeRunning = (runningHolder) =>
                {
                    lock (runningHolder)
                    {
                        _trace.writeToLog(9, "IconOverlay: RunServerPipe. Func<pipeRunningHolder returning {0}.", runningHolder.pipeRunning);
                        return runningHolder.pipeRunning;
                    }
                };
                // check running state with locked function, repeat until running state is false
                while (getPipeRunning(pipeParams.serverLocker))
                {
                    // running state was true so wait for next client connection
                    _trace.writeToLog(9, "IconOverlay: RunServerPipe. In serverLocker.");
                    pipeParams.serverStream.WaitForConnection();
                    if (pipeParams.serverStream != null)
                    {
                        // try/catch which silences errors, disconnects and but allows while loop to continue
                        try
                        {
                            // expect exactly 20 bytes from client (packetId<10> + filepath byte length<10>)
                            _trace.writeToLog(9, "IconOverlay: RunServerPipe. Data ready to read.");
                            byte[] pipeBuffer = new byte[20];
                            // read from client into buffer
                            pipeParams.serverStream.Read(pipeBuffer,
                                0,
                                20);

                            // pull out badgeId from first ten bytes (each byte is an ASCII character)
                            string badgeId = new string(pipeBuffer.Take(10).Select(currentCharByte => (char)currentCharByte).ToArray());
                            // pull out filepath byte length from last ten bytes (each byte is an ASCII character)
                            string pathSize = new string(pipeBuffer.Skip(10).Take(10).Select(currentCharByte => (char)currentCharByte).ToArray());

                            // ensure data from client was readable by checking if the filepath byte length is parsable to int
                            int pathSizeParsed;
                            if (int.TryParse(pathSize, out pathSizeParsed))
                            {
                                // create buffer for second read from client with dynamic size equal to the filepath byte length
                                pipeBuffer = new byte[int.Parse(pathSize)];
                                // read filepath from client into buffer
                                pipeParams.serverStream.Read(pipeBuffer,
                                    0,
                                    pipeBuffer.Length);

                                // convert unicode bytes from buffer into string
                                string filePath = Encoding.Unicode.GetString(pipeBuffer);

                                // define bool to send back to client:
                                // --true means use overlay
                                // --false means don't use overlay
                                bool setOverlay;

                                // lock on internal list so it is not modified while being read
                                _trace.writeToLog(9, "IconOverlay: RunServerPipe. Call ShouldIconBeBadged.");
                                setOverlay = ShouldIconBeBadged(pipeParams.currentBadgeType, filePath);
                                _trace.writeToLog(9, "IconOverlay: RunServerPipe. Process path: {0}, type: {1}, WillBadge: {3}.", filePath, pipeParams.currentBadgeType.ToString(), setOverlay);

                                // create userstate object for the thread returning the result to the client
                                // with the unique pipename to use and the data itself
                                _trace.writeToLog(9, "IconOverlay: RunServerPipe. Build thread state.");
                                returnPipeHolder threadState = new returnPipeHolder()
                                {
                                    fullPipeName = Environment.UserName + "/" + PipeName + pipeParams.currentBadgeType + badgeId,
                                    returnData = setOverlay ? (byte)1 : (byte)0
                                };

                                // return result to badge COM object
                                _trace.writeToLog(9, "IconOverlay: RunServerPipe. Start the return pipe.");
                                (new Thread(() => RunReturnPipe(threadState))).Start();
                            }
                            else
                            {
                                _trace.writeToLog(9, "IconOverlay: RunServerPipe. ERROR: pathSize not parsed.");
                            }
                        }
                        finally
                        {
                            // read operation complete, disconnect so next badge COM object can connect
                            _trace.writeToLog(9, "IconOverlay: RunServerPipe. Disconnect the serverStream.");
                            pipeParams.serverStream.Disconnect();
                        }
                    }
                }
                // running state was set to false causing listening loop to break, dispose of stream if it still exists
                _trace.writeToLog(9, "IconOverlay: RunServerPipe. Check serverStream for close.");
                if (pipeParams.serverStream != null)
                {
                    _trace.writeToLog(9, "IconOverlay: RunServerPipe. Close the serverStream.");
                    pipeParams.serverStream.Close();
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: RunServerPipe: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
            }
            _trace.writeToLog(9, "IconOverlay: RunServerPipe. Exit thread.");
        }

        /// <summary>
        /// Determine whether this icon should be badged by this badge handler.
        /// </summary>
        /// <param name="pipeParams"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private bool ShouldIconBeBadged(cloudAppIconBadgeType badgeType, string filePath)
        {
            try
            {
                _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Entry.");
                // Convert the badgetype and filepath to objects.
                FilePath objFilePath = filePath;
                GenericHolder<cloudAppIconBadgeType> objBadgeType = new GenericHolder<cloudAppIconBadgeType>(badgeType);

                // Lock and query the in-memory database.
                lock (allBadges)
                {
                    // There will be no badge if the path doesn't contain Cloud root
                    _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Entry.");
                    if (objFilePath.Contains(filePathCloudDirectory))
                    {
                        // If the value at this path is set and it is our type, then badge.
                        _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Contains Cloud root.");
                        GenericHolder<cloudAppIconBadgeType> tempBadgeType;
                        bool success = allBadges.TryGetValue(objFilePath, out tempBadgeType);
                        if (success)
                        {
                            bool rc = (tempBadgeType.Value == objBadgeType.Value);
                            _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Return: {0}.", rc);
                            return rc;
                        }
                        else
                        {
                            // If an item is marked selective, then none of its children (whole hierarchy of children) should be badged.
                            _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. TryGetValue not successful.");
                            if (FilePathComparer.Instance.Equals(objFilePath, filePathCloudDirectory))
                            {
                                // Recurse through parents of this node up to and including the CloudPath.
                                _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Recurse thru parents.");
                                FilePath node = objFilePath;
                                while (node != null)
                                {
                                    // Return false if the value of this node is not null, and is marked SyncSelective
                                    _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Get the type for path: {0}.", node.ToString());
                                    GenericHolder<cloudAppIconBadgeType> thisNodeBadgeType = allBadges[node];
                                    _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Got type {0}.", thisNodeBadgeType.ToString());
                                    if (thisNodeBadgeType != null && thisNodeBadgeType.Value == cloudAppIconBadgeType.cloudAppBadgeSyncSelective)
                                    {
                                        _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Return false.");
                                        return false;
                                    }

                                    // Quit if we notified the Cloud directory root
                                    _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Have we reached the Cloud root?");
                                    if (FilePathComparer.Instance.Equals(node, filePathCloudDirectory))
                                    {
                                        _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Yes.  Break.");
                                        break;
                                    }

                                    // Chain up
                                    _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Chain up.");
                                    node = node.Parent;
                                }
                            }

                            // Determine the badge type from the hierarchy at this path
                            try
                            {
                                // Get the hierarchy of children of this node.
                                _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Get the hierarchy for path: {0}.", objFilePath.ToString());
                                FilePathHierarchicalNode<GenericHolder<cloudAppIconBadgeType>> tree;
                                CLError error = allBadges.GrabHierarchyForPath(objFilePath, out tree);
                                if (error == null)
                                {
                                    _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Successful getting the hierarcy.  Call GetDesiredBadgeTypeViaRecursivePostorderTraversal.");
                                    // Chase the children hierarchy using recursive postorder traversal to determine the desired badge type.
                                    cloudAppIconBadgeType desiredBadgeType = GetDesiredBadgeTypeViaRecursivePostorderTraversal(tree);
                                    bool rc = (badgeType == desiredBadgeType);
                                    _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Return(2): {0}.", rc);
                                    return rc;
                                }
                                else
                                {
                                    bool rc = (badgeType == cloudAppIconBadgeType.cloudAppBadgeSynced);
                                    _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Return(3): {0}.", rc);
                                    return rc;
                                }
                            }
                            catch
                            {
                                bool rc = (badgeType == cloudAppIconBadgeType.cloudAppBadgeSynced);
                                _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Return(4): {0}.", rc);
                                return rc;
                            }
                        }
                    }
                    else
                    {
                        // This path is not in the Cloud folder.  Don't badge.
                        _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Not in the Cloud folder.  Don't badge.");
                        return false;
                    }
                }
            }
            catch(Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Exception.  Normal? Msg: {0}, Code: (1).", error.errorDescription, error.errorCode);
                return false;
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

        /// <summary>
        /// Processes a receiving server pipe to communicate with a BadgeCOM object for the context menu support
        /// </summary>
        /// <param name="pipeParams"></param>
        private void RunServerPipeContextMenu(pipeThreadParamsContextMenu pipeParams)
        {
            // try/catch which silences errors and stops badging functionality (should never error here)
            try
            {
                // define locked function for checking running state
                Func<pipeRunningHolder, bool> getPipeRunning = (runningHolder) =>
                {
                    lock (runningHolder)
                    {
                        return runningHolder.pipeRunning;
                    }
                };
                // check running state with locked function, repeat until running state is false
                while (getPipeRunning(pipeParams.serverLocker))
                {
                    // running state was true so wait for next client connection
                    pipeParams.serverStream.WaitForConnection();
                    if (pipeParams.serverStream != null)
                    {
                        // try/catch which silences errors, disconnects and but allows while loop to continue
                        try
                        {
                            // We got a connection.  Read the JSON from the pipe and deserialize it to a POCO.
                            StreamReader reader = new StreamReader(pipeParams.serverStream);
                            ContextMenuObject msg = JsonConvert.DeserializeObject<ContextMenuObject>(reader.ReadLine());

                            // Copy the files to the Cloud root directory.
                            ContextMenuCopyFiles(msg);

                        }
                        catch (Exception ex)
                        {
                            CLError err = ex;
                            _trace.writeToLog(1, "IconOverlay: RunServerPipeContextMenu: ERROR: Exception. Msg: <{0}>, Code: {1}.", err.errorDescription, err.errorCode);
                        }
                        finally
                        {
                            // read operation complete, disconnect so next badge COM object can connect
                            pipeParams.serverStream.Disconnect();
                        }
                    }
                }
                // running state was set to false causing listening loop to break, dispose of stream if it still exists
                if (pipeParams.serverStream != null)
                    pipeParams.serverStream.Close();
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: RunServerPipeContextMenu: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
            }
        }

        /// <summary>
        /// Copy the selected files to the Cloud root directory.
        /// </summary>
        /// <param name="returnParams"></param>
        private void ContextMenuCopyFiles(ContextMenuObject msg)
        {
            foreach (string path in msg.asSelectedPaths)
            {
                // Remove any trailing backslash
                string source = path.TrimEnd(new char[]{'\\', '/'});

                // Get the filename.ext of the source path.
                string filenameExt = Path.GetFileName(source);

                // Build the target path
                string target = filePathCloudDirectory.ToString() + "\\" + filenameExt;

                // Copy it.
                Dispatcher mainDispatcher = Application.Current.Dispatcher;
                mainDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    CLCopyFiles.CopyFileOrDirectoryWithUi(source, target);
                }));
            }
        }

        /// <summary>
        /// Processes return pipe to send data back to the BadgeCOM object
        /// </summary>
        /// <param name="returnParams"></param>
        private void RunReturnPipe(returnPipeHolder returnParams)
        {
            // try/catch which silences errors
            try
            {
                // Clean up return pipe on a timer in case connection did not occur
                // Requires defining a userstate to pass with whether the connection was achieved
                // and the pipename to check
                _trace.writeToLog(9, "IconOverlay: RunReturnPipe. Entry: PipeName: {0}, BadgeIt?: {1}.", returnParams.fullPipeName, returnParams.returnData);
                returnPipeRunningHolder returnRunningHolder = new returnPipeRunningHolder()
                {
                    connectionAchieved = false,
                    fullPipeName = returnParams.fullPipeName
                };

                // define the unique server pipe for the return communication (sends only one byte ever)
                // it is located in the same object that was sent to the cleanup timer
                _trace.writeToLog(9, "IconOverlay: RunReturnPipe. Create the return pipe and start the thread to CleanReturnPipe.");
                returnRunningHolder.returnStream = new NamedPipeServerStream(returnParams.fullPipeName,
                    PipeDirection.Out,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.None);

                // start cleaning thread in case WaitForConnection locks and does not complete
                (new Thread(() => CleanReturnPipe(returnRunningHolder))).Start();

                // wait for client to connect
                _trace.writeToLog(9, "IconOverlay: RunReturnPipe. Wait for the client to connect.");
                returnRunningHolder.returnStream.WaitForConnection();

                // client successfully connected
                // lock on the oject shared with the cleanup timer
                // if the clearning thread did not already attempt to stop the connection,
                //    write the data to be returned to the client
                // mark connection achieved so cleanup thread won't cleanup
                _trace.writeToLog(9, "IconOverlay: RunReturnPipe. Client connected.");
                lock (returnRunningHolder)
                {
                    if (!returnRunningHolder.connectionAchieved)
                    {
                        _trace.writeToLog(9, "IconOverlay: RunReturnPipe. Return the data.");
                        returnRunningHolder.returnStream.WriteByte(returnParams.returnData);
                    }
                    returnRunningHolder.connectionAchieved = true;
                }

                // normal cleanup of successful return of data
                // will not attempt a repeat dispose if already disposed (and set to null) by cleaning thread
                if (returnRunningHolder.returnStream != null)
                {
                    _trace.writeToLog(9, "IconOverlay: RunReturnPipe. Dispose the return stream.");
                    returnRunningHolder.returnStream.Dispose();
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: RunReturnPipe: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
            }
            _trace.writeToLog(9, "IconOverlay: RunReturnPipe. Exit the thread.");
        }

        /// <summary>
        /// thread used to host the unique pipeserver for return response
        /// (will self-terminate after 5 seconds if no connections were made,
        ///    though this self-termination is not clean and you may see a handled first-chance exception)
        /// </summary>
        /// <param name="cleanParams"></param>
        private void CleanReturnPipe(returnPipeRunningHolder cleanParams)
        {
            _trace.writeToLog(9, "IconOverlay: CleanReturnPipe. Entry.  Wait 5 seconds.");
            Thread.Sleep(5000);// wait 5 seconds before cleaning

            try
            {
                lock (cleanParams)
                {
                    _trace.writeToLog(9, "IconOverlay: CleanReturnPipe. Start.");
                    if (!cleanParams.connectionAchieved)
                    {
                        // connection was 'not' already achieved, but this prevents the normal communication process
                        // from sending data over the pipe while its being disposed here
                        _trace.writeToLog(9, "IconOverlay: CleanReturnPipe. Connection achieved.");
                        cleanParams.connectionAchieved = true;

                        try
                        {
                            _trace.writeToLog(9, "IconOverlay: CleanReturnPipe. Dispose the return stream.");
                            cleanParams.returnStream.Dispose();
                            cleanParams.returnStream = null;
                        }
                        catch(Exception ex)
                        {
                            CLError error = ex;
                            _trace.writeToLog(1, "IconOverlay: CleanReturnPipe(1): ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                        }

                        //The following is not a fallacy in logic:
                        //disposing a server stream is not guaranteed to stop a thread stuck on WaitForConnection
                        //so we try to connect to it just in case (will most likely give an unauthorized exception)
                        using (NamedPipeClientStream connectionKillerStream = new NamedPipeClientStream(".",
                            cleanParams.fullPipeName,
                            PipeDirection.Out,
                            PipeOptions.None))
                        {
                            _trace.writeToLog(9, "IconOverlay: CleanReturnPipe. Attempt to connect using the killer stream.");
                            connectionKillerStream.Connect(100);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: CleanReturnPipe(2): ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
            }
            _trace.writeToLog(9, "IconOverlay: CleanReturnPipe. Exit the thread.");
        }
        #endregion
    }
}
