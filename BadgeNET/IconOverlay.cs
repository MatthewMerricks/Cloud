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
using CloudApiPrivate.Model.Settings;
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
        public static CLError Initialize(IEnumerable<KeyValuePair<FilePath, cloudAppIconBadgeType>> initialList = null)
        {
            try
            {
                return Instance.pInitialize(initialList);
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: Initialize: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                return ex;
            }
        }
        private CLError pInitialize(IEnumerable<KeyValuePair<FilePath, cloudAppIconBadgeType>> initialList = null)
        {
            try
            {
                // ensure IconOverlay is only ever initialized once
                lock (this)
                {
                    if (isInitialized)
                        throw new Exception("IconOverlay Instance already initialized");
                    isInitialized = true;
                }

                bool initialListContainsItem = false;

                //FilePathDictionary<GenericHolder<cloudAppIconBadgeType>> newBadges;
                //// Allocate the badging dictionary.  This is a hierarchical dictionary.
                //CLError error = FilePathDictionary<GenericHolder<cloudAppIconBadgeType>>.CreateAndInitialize(
                //    rootPath: Settings.Instance.CloudFolderPath,
                //    pathDictionary: out newBadges,               
                //    recursiveDeleteCallback: null,          //TODO: Implement this?
                //    recursiveRenameCallback: null);         //TODO: Implement this?
                //if (error != null)
                //{
                //    throw new Exception(String.Format("IconOverlay: pInitialize: ERROR from CreateAndInitialize: <{0}>, Code: {1}", error.errorDescription, error.errorCode));
                //}

                //if (newBadges == null)
                //{
                //    throw new Exception("IconOverlay error creating badging dictionary");
                //}

                ////&&&& TODO: TEST ONLY:  Remove
                //newBadges.Add(Settings.Instance.CloudFolderPath + "\\A\\B\\C.txt", new GenericHolder<cloudAppIconBadgeType>(cloudAppIconBadgeType.cloudAppBadgeSyncing));
                //newBadges.Rename(Settings.Instance.CloudFolderPath + "\\A\\B", Settings.Instance.CloudFolderPath + "\\A\\D");

                //FilePathHierarchicalNode<GenericHolder<cloudAppIconBadgeType>> childrenHierarchy;
                //error = newBadges.GrabHierarchyForPath(Settings.Instance.CloudFolderPath + "\\A", out childrenHierarchy);


                // I don't want to enumerate the initialList for both counting and copying, so I define an array for storage
                KeyValuePair<FilePath, cloudAppIconBadgeType>[] initialListArray;
                // initial list contained values for badging; preload dictionary and notify system of global change
                if (initialList != null
                    && (initialListArray = initialList.ToArray()).Length > 0)
                {
                    // store that initial list contained an item so system can be notified later
                    initialListContainsItem = true;

                    // loop through initial list for badged objects to add to local dictionary
                    for (int initialListCounter = 0; initialListCounter < initialListArray.Length; initialListCounter++)
                    {
                        // only keep track of badges that are not "none"
                        if (initialListArray[initialListCounter].Value != cloudAppIconBadgeType.cloudAppBadgeNone)
                        {
                            // populate each initial badged object into local dictionary
                            // throws exception if file path (Key) is null or empty
                            // do not need to lock on allBadges since listening threads don't start until after this
                            allBadges.Add(initialListArray[initialListCounter].Key,
                                new BadgedObject(initialListArray[initialListCounter].Key,
                                    initialListArray[initialListCounter].Value,
                                    BadgedObject_PropertyChanged));
                        }
                    }
                }

                StartBadgeCOMPipes();

                // initial list contained an item so notify the system to update
                if (initialListContainsItem)
                {
                    //Parameterless call to notify will force OS to update all icons
                    NotifySystemForBadgeUpdate();
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: pInitialize: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                return ex;
            }
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
        public static CLError InitializeOrReplace(IEnumerable<KeyValuePair<FilePath, cloudAppIconBadgeType>> initialList)
        {
            try
            {
                return Instance.pInitializeOrReplace(initialList);
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: InitializeOrReplace: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                return ex;
            }
        }
        private CLError pInitializeOrReplace(IEnumerable<KeyValuePair<FilePath, cloudAppIconBadgeType>> initialList)
        {
            try
            {
                bool listProcessed = false;

                // ensure IconOverlay is only ever initialized once
                lock (this)
                {
                    // run initialize instead if it has not been run
                    if (!isInitialized)
                    {
                        Initialize(initialList);
                        // store that list was already processed by initialization
                        listProcessed = true;
                    }
                }

                // if list was not already processed by initialization
                if (!listProcessed)
                {
                    // lock internal list during modification
                    lock (allBadges)
                    {
                        // empty list before adding in all replacement items
                        allBadges.Clear();
                        foreach (KeyValuePair<FilePath, cloudAppIconBadgeType> currentReplacedItem in initialList ?? new KeyValuePair<FilePath, cloudAppIconBadgeType>[0])
                        {
                            // only keep track of badges that are not "none"
                            if (currentReplacedItem.Value != cloudAppIconBadgeType.cloudAppBadgeNone)
                            {
                                // populate each replaced badged object into local dictionary
                                // throws exception if file path (Key) is null or empty
                                allBadges.Add(currentReplacedItem.Key,
                                    new BadgedObject(currentReplacedItem.Key,
                                        currentReplacedItem.Value,
                                        BadgedObject_PropertyChanged));
                            }
                        }
                    }

                    //Parameterless call to notify will force OS to update all icons
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
        public static CLError setBadgeType(Nullable<cloudAppIconBadgeType> type, string forFileAtPath)
        {
            try
            {
                return Instance.pSetBadgeType(forFileAtPath, type == cloudAppIconBadgeType.cloudAppBadgeNone ? null : type);
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: pInitializeOrReplace: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                return ex;
            }
        }
        private CLError pSetBadgeType(string filePath, Nullable<cloudAppIconBadgeType> newType)
        {
            try
            {
                // ensure this is initialized
                lock (this)
                {
                    if (!isInitialized)
                        throw new Exception("IconOverlay must be initialized before setting badges");
                }

                // store whether system needs to be notified
                bool notifySystemOfFileUpdate = false;

                // lock internal list during modification
                lock (allBadges)
                {
                    // newType is null means remove badge
                    if (newType == null)
                    {
                        // if internal list contains the badge to be removed, remove it
                        if (allBadges.ContainsKey(filePath))
                        {
                            // remove badge
                            allBadges.Remove(filePath);
                            // system needs to be notified
                            notifySystemOfFileUpdate = true;
                        }
                    }
                    // newType is not null, set badge
                    else
                    {
                        // badge already exists in internal list, needs to be updated
                        if (allBadges.ContainsKey(filePath))
                        {
                            // grab badge object from internal list
                            BadgedObject existingObject = allBadges[filePath];
                            // check if badge type needs to be changed
                            if (!existingObject.Type.Equals((cloudAppIconBadgeType)newType))
                            {
                                // badge type needs to be changed, this automatically will notify system for update
                                existingObject.Type = (cloudAppIconBadgeType)newType;
                            }
                        }
                        // badge is new, needs to be added to internal list
                        else
                        {
                            // populate a new badged object into local dictionary
                            // throws exception if file path (Key) is null or empty
                            allBadges.Add(filePath,
                                new BadgedObject(filePath,
                                    (cloudAppIconBadgeType)newType,
                                    BadgedObject_PropertyChanged));

                            // system needs to be notified
                            notifySystemOfFileUpdate = true;
                        }
                    }
                }

                // notify system as necessary for current badge
                if (notifySystemOfFileUpdate)
                {
                    NotifySystemForBadgeUpdate(filePath);
                }
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
        public static CLError getBadgeTypeForFileAtPath(string path, out cloudAppIconBadgeType badgeType)
        {
            try
            {
                return Instance.pFindBadge(path, out badgeType);
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: getBadgeTypeForFileAtPath: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                badgeType = Helpers.DefaultForType<cloudAppIconBadgeType>();
                return ex;
            }
        }
        private CLError pFindBadge(string filePath, out cloudAppIconBadgeType badgeType)
        {
            try
            {
                // lock on dictionary so it is not modified during lookup
                lock (allBadges)
                {
                    // return badgetype if it exists, otherwise null
                    badgeType = allBadges.ContainsKey(filePath)
                        ? allBadges[filePath].Type
                        : cloudAppIconBadgeType.cloudAppBadgeNone;
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: pFindBadge: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                badgeType = Helpers.DefaultForType<cloudAppIconBadgeType>();
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
                lock (this)
                {
                    // monitor is now set as disposed which will produce errors if startup is called later
                    Disposed = true;
                }

                // Run dispose on inner managed objects based on disposing condition
                if (disposing)
                {
                    // locks on this in case initialization is occurring simultaneously
                    lock (this)
                    {
                        // only need to shutdown if it was initialized
                        if (isInitialized)
                        {
                            // lock on object containing intial pipe connection running state
                            lock (pipeLocker)
                            {
                                // set runningstate to off
                                pipeLocker.pipeRunning = false;

                                // Dispose the badging streams
                                foreach (KeyValuePair<cloudAppIconBadgeType, NamedPipeServerStream> currentStreamToKill in pipeServerStreams)
                                {
                                    try
                                    {
                                        // cleanup initial pipe connection

                                        try
                                        {
                                            currentStreamToKill.Value.Dispose();
                                        }
                                        catch
                                        {
                                        }

                                        //The following is not a fallacy in logic:
                                        //disposing a server stream is not guaranteed to stop a thread stuck on WaitForConnection
                                        //so we try to connect to it just in case (will most likely give an unauthorized exception)
                                        using (NamedPipeClientStream connectionKillerStream = new NamedPipeClientStream(".",
                                            Environment.UserName + "/" + PipeName + currentStreamToKill.Key,
                                            PipeDirection.Out,
                                            PipeOptions.None))
                                        {
                                            connectionKillerStream.Connect(100);
                                        }
                                    }
                                    catch
                                    {
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
        /// The hierarhical dictionary that holds all of the badges.  Nodes with null values are assumed to be synced.
        /// </summary>
        //FilePathDictionary<GenericHolder<cloudAppIconBadgeType>> allBadges;
        Dictionary<FilePath, BadgedObject> allBadges;

        /// <summary>
        /// EventHandler for BadgeObject PropertyChanged, to be passed on creation of each BadgedObject
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BadgedObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            BadgedObject castSender = (BadgedObject)sender;

            // Immediately notify system that a badge needs to be updated
            NotifySystemForBadgeUpdate(castSender.FilePath.Name);
        }

        #region notify system of change
        // important (including everything inside)
        /// <summary>
        /// Uses Win32 API call to refresh all icons (if filePath is not specified)
        /// or a single icon (if filePath is specified)
        /// </summary>
        /// <param name="filePath">Filepath for refresing single icon (case-sensitive)</param>
        private static void NotifySystemForBadgeUpdate(string filePath = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                //The following will refresh all icons, does not force OS to reload relevant COM objects
                //    (for now I test after restarting explorer.exe)
                SHChangeNotify(HChangeNotifyEventID.SHCNE_ASSOCCHANGED,
                    HChangeNotifyFlags.SHCNF_IDLIST,
                    IntPtr.Zero,
                    IntPtr.Zero);
            }
            else
            {
                //Instantiate IntPtr outside of try so it can be cleaned up
                IntPtr filePtr = IntPtr.Zero;
                try
                {
                    //Set IntPtr to null-terminated ANSI string of full file path
                    //I tried StringToHGlobalUni but it did not work
                    //Also, StringtoHGlobalAuto does not work either
                    filePtr = Marshal.StringToHGlobalAnsi(filePath);

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
                        Marshal.FreeHGlobal(filePtr);
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
                foreach (KeyValuePair<cloudAppIconBadgeType, NamedPipeServerStream> currentStreamToProcess in pipeServerStreams)
                {
                    // important
                    // store a userstate for the thread that processes initial pipe connections with pipe server
                    // and a lockable object containing running state
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
                            // expect exactly 20 bytes from client (packetId<10> + filepath byte length<10>)
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
                                lock (allBadges)
                                {
                                    // use overlay if filepath exists in internal list and it matches the current badgetype
                                    setOverlay = allBadges.ContainsKey(filePath)
                                        && allBadges[filePath].Type.Equals(pipeParams.currentBadgeType);
                                }

                                // create userstate object for the thread returning the result to the client
                                // with the unique pipename to use and the data itself
                                returnPipeHolder threadState = new returnPipeHolder()
                                {
                                    fullPipeName = PipeName + pipeParams.currentBadgeType + badgeId,
                                    returnData = setOverlay ? (byte)1 : (byte)0
                                };

                                // return result to badge COM object
                                (new Thread(() => RunReturnPipe(threadState))).Start();
                            }
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
                _trace.writeToLog(1, "IconOverlay: RunServerPipe: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
            }
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
                string target = Settings.Instance.CloudFolderPath + "\\" + filenameExt;

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
                returnPipeRunningHolder returnRunningHolder = new returnPipeRunningHolder()
                {
                    connectionAchieved = false,
                    fullPipeName = returnParams.fullPipeName
                };

                // define the unique server pipe for the return communication (sends only one byte ever)
                // it is located in the same object that was sent to the cleanup timer
                returnRunningHolder.returnStream = new NamedPipeServerStream(returnParams.fullPipeName,
                    PipeDirection.Out,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.None);

                // start cleaning thread in case WaitForConnection locks and does not complete
                (new Thread(() => CleanReturnPipe(returnRunningHolder))).Start();

                // wait for client to connect
                returnRunningHolder.returnStream.WaitForConnection();

                // client successfully connected
                // lock on the oject shared with the cleanup timer
                // if the clearning thread did not already attempt to stop the connection,
                //    write the data to be returned to the client
                // mark connection achieved so cleanup thread won't cleanup
                lock (returnRunningHolder)
                {
                    if (!returnRunningHolder.connectionAchieved)
                        returnRunningHolder.returnStream.WriteByte(returnParams.returnData);
                    returnRunningHolder.connectionAchieved = true;
                }

                // normal cleanup of successful return of data
                // will not attempt a repeat dispose if already disposed (and set to null) by cleaning thread
                if (returnRunningHolder.returnStream != null)
                {
                    returnRunningHolder.returnStream.Dispose();
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: RunReturnPipe: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
            }
        }

        /// <summary>
        /// thread used to host the unique pipeserver for return response
        /// (will self-terminate after 5 seconds if no connections were made,
        ///    though this self-termination is not clean and you may see a handled first-chance exception)
        /// </summary>
        /// <param name="cleanParams"></param>
        private void CleanReturnPipe(returnPipeRunningHolder cleanParams)
        {
            Thread.Sleep(5000);// wait 5 seconds before cleaning

            try
            {
                lock (cleanParams)
                {
                    if (!cleanParams.connectionAchieved)
                    {
                        // connection was 'not' already achieved, but this prevents the normal communication process
                        // from sending data over the pipe while its being disposed here
                        cleanParams.connectionAchieved = true;

                        try
                        {
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
        }
        #endregion
    }
}
