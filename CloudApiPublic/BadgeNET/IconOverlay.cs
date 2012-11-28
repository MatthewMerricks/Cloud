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
using System.Runtime.InteropServices;
using BadgeCOMLib;
using CloudApiPublic.Interfaces;
using CloudApiPublic.Sync;

namespace CloudApiPublic.BadgeNET
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
        private static ISyncSettingsAdvanced _syncSettings;

        // Constructor for Singleton pattern is private
        private IconOverlay()
        {
            // Allocate the badging current state flat dictionary.  This dictionary is used to determine the badge path to remove when the
            // badge type for that path changes.  We send a _kEvent_BadgeNet_AddBadgePath event to the BadgeCom "new" type, and a
            // _kEvent_BadgeNet_RemoveBadgePath event to the BadgeCom "old" type.
            _currentBadges = new Dictionary<FilePath, GenericHolder<cloudAppIconBadgeType>>(FilePathComparer.Instance);
        }

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
        public static CLError Initialize(ISyncSettings syncSettings, IEnumerable<KeyValuePair<FilePath, GenericHolder<cloudAppIconBadgeType>>> initialList = null)
        {
            try
            {
                if (syncSettings == null)
                {
                    throw new NullReferenceException("syncSettings cannot be null");
                }

                // Copy sync settings in case third party attempts to change values without restarting sync 
                _syncSettings = SyncSettingsExtensions.CopySettings(syncSettings);

                // Initialize trace in case it is not already initialized.
                CLTrace.Initialize(_syncSettings.TraceLocation, "Cloud", "log", _syncSettings.TraceLevel);

                _trace.writeToLog(9, "IconOverlay: Initialize: Entry.");
                return Instance.pInitialize(syncSettings.CloudRoot, initialList);
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: Initialize: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                return ex;
            }
        }
        private CLError pInitialize(string cloudRoot, IEnumerable<KeyValuePair<FilePath, GenericHolder<cloudAppIconBadgeType>>> initialList = null)
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
                MessageEvents.FileChangeMergeToStateChanged += MessageEvents_FileChangeMergeToStateChanged;
                MessageEvents.SetBadgeQueued += MessageEvents_QueueSetBadgeChanged;
                MessageEvents.BadgePathDeleted += MessageEvents_BadgePathDeleted;
                MessageEvents.BadgePathRenamed += MessageEvents_BadgePathRenamed;

                // Capture the Cloud directory path for performance.
                _filePathCloudDirectory = cloudRoot;

                // Start a thread to create and an instance of CBadgeNetPubSubEvents.  This is necessary because the current
                // thread is a STA thread, and we must instantiate CBadgeNetPubSubEvents as an MTA thread.
                if (_badgeComPubSubEvents == null)
                {
                    Thread threadInit = new Thread(new ThreadStart(() => 
                    {
                        try
                        {
                            _trace.writeToLog(9, "IconOverlay: pInitialize: threadInit entry.");
                            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.MTA)
                            {
                                _trace.writeToLog(9, "IconOverlay: pInitialize: ERROR.  Wrong threading model.");
                                throw new Exception("Wrong threading model");
                            }

                            _trace.writeToLog(9, "IconOverlay: pInitialize: Instantiate BadgeComPubSubEvents.");
                            _badgeComPubSubEvents = new BadgeComPubSubEvents();
                            _badgeComPubSubEvents.Initialize();
                            _badgeComPubSubEvents.BadgeComInitialized += BadgeComPubSubEvents_OnBadgeComInitialized;
                            _badgeComPubSubEvents.BadgeComInitializedSubscriptionFailed += _badgeComPubSubEvents_OnBadgeComInitializationSubscriptionFailed;

                            // Start listening for BadgeCom initialization events.
                            _trace.writeToLog(9, "IconOverlay: pInitialize: Subscribe to BadgeCom init events.");
                            _badgeComPubSubEvents.SubscribeToBadgeComInitializationEvents();

                            // Send our badging dictionary to the BadgeCom subscribers.
                            _trace.writeToLog(9, "IconOverlay: pInitialize: Send badging dictionary.");
                            BadgeComPubSubEvents_OnBadgeComInitialized(null, null);

                        }
                        catch (Exception ex)
                        {
                            _trace.writeToLog(1, "IconOverlay: pInitialize: ERROR: threadInit exception: Msg: <{0}>, Code: {1}.", ex.Message);
                            throw;
                        }

                    }));
                    threadInit.SetApartmentState(ApartmentState.MTA);
                    threadInit.Start();
                    bool started = threadInit.Join(5000);
                    if (!started)
                    {
                        _trace.writeToLog(1, "IconOverlay: pInitialize: ERROR: threadInit was not started.");
                        throw new Exception("threadInit was not started.");
                    }
                    else
                    {
                        _trace.writeToLog(9, "IconOverlay: pInitialize: threadInit completed OK.");
                    }
                }

                // Allocate the badging dictionary.  This is a hierarchical dictionary.
                CLError error = FilePathDictionary<GenericHolder<cloudAppIconBadgeType>>.CreateAndInitialize(
                    rootPath: _filePathCloudDirectory,
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
                    // loop through initial list for badged objects to add to local dictionary
                    _trace.writeToLog(9, "IconOverlay: pInitialize: Got initial list.");
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
#if TRASH
                _trace.writeToLog(9, "IconOverlay: StartBadgeCOMPipes.");
                StartBadgeCOMPipes();
#endif // TRASH &&&&
            }
            catch (Exception ex)
            {
                isInitialized = false;
                _trace.writeToLog(1, "IconOverlay: pInitialize: ERROR: Exception: Msg: <{0}>, Code: {1}.", ex.Message);
                return ex;
            }
            _trace.writeToLog(9, "IconOverlay: pInitialize: Return success.");
            return null;
        }

        /// <summary>
        /// The BadgeCom initialization event watcher threads died.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _badgeComPubSubEvents_OnBadgeComInitializationSubscriptionFailed(object sender, EventArgs e)
        {
            try
            {
                // Just return if we aren't initialized.
                lock (this)
                {
                    if (!isInitialized)
                    {
                        return;
                    }
                }

                // Start a thread to kill the current instance of CBadgeNetPubSubEvents and to create a new instance of CBadgeNetPubSubEvents.
                // This is necessary because the current thread is a STA thread, and we must instantiate CBadgeNetPubSubEvents as an MTA thread.
                Thread threadRestart = new Thread(new ThreadStart(() => 
                {
                    try
                    {
                        _trace.writeToLog(9, "IconOverlay: _badgeComPubSubEvents_OnBadgeComInitializationSubscriptionFailed: threadInit entry.");
                        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.MTA)
                        {
                            _trace.writeToLog(9, "IconOverlay: _badgeComPubSubEvents_OnBadgeComInitializationSubscriptionFailed: ERROR.  Wrong threading model.");
                        }

                        if (_badgeComPubSubEvents != null)
                        {
                            // Kill the subscriptions and dispose the object
                            _trace.writeToLog(1, "IconOverlay: _badgeComPubSubEvents_OnBadgeComInitializationSubscriptionFailed: Entry.  Kill BadgeComPubSubEvents.");
                            _badgeComPubSubEvents.BadgeComInitialized -= BadgeComPubSubEvents_OnBadgeComInitialized;
                            _badgeComPubSubEvents.BadgeComInitializedSubscriptionFailed -= _badgeComPubSubEvents_OnBadgeComInitializationSubscriptionFailed;
                            _badgeComPubSubEvents.Dispose();
                        }

                        // Restart
                        _trace.writeToLog(1, "IconOverlay: _badgeComPubSubEvents_OnBadgeComInitializationSubscriptionFailed: Entry.  Restart BadgeComPubSubEvents.");
                        _badgeComPubSubEvents = new BadgeComPubSubEvents();
                        _badgeComPubSubEvents.Initialize();
                        _badgeComPubSubEvents.BadgeComInitialized += BadgeComPubSubEvents_OnBadgeComInitialized;
                        _badgeComPubSubEvents.BadgeComInitializedSubscriptionFailed += _badgeComPubSubEvents_OnBadgeComInitializationSubscriptionFailed;

                        // Start listening for BadgeCom initialization events.
                        _trace.writeToLog(9, "IconOverlay: _badgeComPubSubEvents_OnBadgeComInitializationSubscriptionFailed: Subscribe to BadgeCom init events.");
                        _badgeComPubSubEvents.SubscribeToBadgeComInitializationEvents();

                        // Send our badging database to all of the BadgeCom subscribers.
                        _trace.writeToLog(9, "IconOverlay: _badgeComPubSubEvents_OnBadgeComInitializationSubscriptionFailed: Send badging database.");
                        BadgeComPubSubEvents_OnBadgeComInitialized(null, null);
                    }
                    catch (Exception ex)
                    {
                        _trace.writeToLog(1, "IconOverlay: _badgeComPubSubEvents_OnBadgeComInitializationSubscriptionFailed: ERROR: threadRestart exception: Msg: <{0}>, Code: {1}.", ex.Message);
                    }

                }));

                // Start the thread, but don't wait for it to complete because the current thread is the CBadgeComPubSubEvents Watcher thread, and that thread must be killed by the restart logic above.
                threadRestart.SetApartmentState(ApartmentState.MTA);
                threadRestart.Start();
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "IconOverlay: _badgeComPubSubEvents_OnBadgeComWatcherFailed: ERROR: Exception: Msg: <{0}>, Code: {1}.", ex.Message);
            }
        }

        /// <summary>
        /// Callback indicating that we received a BadgeCom initialization event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BadgeComPubSubEvents_OnBadgeComInitialized(object sender, EventArgs e)
        {
            try
            {
                // Just return if we aren't initialized.
                lock (this)
                {
                    if (!isInitialized)
                    {
                        return;
                    }
                }

                _trace.writeToLog(9, "IconOverlay: BadgeComPubSubEvents_OnBadgeComInitialized: Entry.");
                if (_badgeComPubSubEvents != null)
                {
                    // Publish the remove SyncBox folder path event back to all BadgeCom instances.  This will clear any dictionaries involving those folder paths.
                    _badgeComPubSubEvents.PublishEventToBadgeCom(EnumEventType.BadgeNet_To_BadgeCom, EnumEventSubType.BadgeNet_RemoveSyncBoxFolderPath, 0 /* not used */, _filePathCloudDirectory.ToString());

                    // Publish the add SyncBox folder path event back to all BadgeCom instances.
                    _badgeComPubSubEvents.PublishEventToBadgeCom(EnumEventType.BadgeNet_To_BadgeCom, EnumEventSubType.BadgeNet_AddSyncBoxFolderPath, 0 /* not used */, _filePathCloudDirectory.ToString());
                    
                    // Do not want to process COM publishing logic while holding up the lock, so make a copy under the lock instead.
                    Func<Dictionary<FilePath, GenericHolder<cloudAppIconBadgeType>>, object, 
                        IEnumerable<KeyValuePair<FilePath, GenericHolder<cloudAppIconBadgeType>>>> copyBadgesUnderLock = (badgeDict, locker) =>
                        {
                            lock (locker)
                            {
                                return badgeDict.ToArray();
                            }
                        };

                    // Iterate over the current badge state dictionary and send the badges to BadgeCom. This will populate the BadgeCom instance that just initialized.
                    // Other instances will update from these events only if necessary.
                    foreach (KeyValuePair<FilePath, GenericHolder<cloudAppIconBadgeType>> item in copyBadgesUnderLock(_currentBadges, _currentBadgesLocker))
                    {
                        // Publish a badge add path event to BadgeCom.
                        _badgeComPubSubEvents.PublishEventToBadgeCom(EnumEventType.BadgeNet_To_BadgeCom, EnumEventSubType.BadgeNet_AddBadgePath, (EnumCloudAppIconBadgeType)item.Value.Value, item.Key.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "IconOverlay: BadgeComPubSubEvents_OnBadgeComInitialized: ERROR: Exception: Msg: <{0}>.", ex.Message);
            }
        }

        void MessageEvents_BadgePathRenamed(object sender, BadgePathRenamedArgs e)
        {
            // Just return if we aren't initialized.
            lock (this)
            {
                if (!isInitialized)
                {
                    return;
                }
            }

            CLError error = RenameBadgePath(e.RenameBadgePath.FromPath, e.RenameBadgePath.ToPath);
            if (error != null)
            {
                _trace.writeToLog(1, "IconOverlay: MessageEvents_BadgePathRenamed: ERROR: Throw. Msg: <{0}>, Code: {1}. FromPath: {2}. ToPath: {3}.", error.errorDescription, error.errorCode, e.RenameBadgePath.FromPath, e.RenameBadgePath.ToPath);
            }
            e.MarkHandled();
        }

        void MessageEvents_BadgePathDeleted(object sender, BadgePathDeletedArgs e)
        {
            // Just return if we aren't initialized.
            lock (this)
            {
                if (!isInitialized)
                {
                    return;
                }
            }

            bool isDeleted;
            CLError error = DeleteBadgePath(e.DeleteBadgePath.PathToDelete, out isDeleted);
            if (error != null)
            {
                _trace.writeToLog(1, "IconOverlay: MessageEvents_BadgePathDeleted: ERROR: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
            }
            else if (isDeleted)
            {
                e.MarkDeleted();
            }
            e.MarkHandled();
        }

        private void MessageEvents_QueueSetBadgeChanged(object sender, SetBadgeQueuedArgs e)
        {
            // Just return if we aren't initialized.
            lock (this)
            {
                if (!isInitialized)
                {
                    return;
                }
            }

            cloudAppIconBadgeType convertedState = ConvertBadgeState(e.SetBadge.BadgeState);
            QueueSetBadge(convertedState, e.SetBadge.PathToBadge);
            e.MarkHandled();
        }

        private void MessageEvents_FileChangeMergeToStateChanged(object sender, FileChangeMergeToStateArgs e)
        {
            // Just return if we aren't initialized.
            lock (this)
            {
                if (!isInitialized)
                {
                    return;
                }
            }

            QueueNewEventBadge(e.MergedFileChanges.MergeTo, e.MergedFileChanges.MergeFrom);
            e.MarkHandled();
        }

        private void MessageEvents_PathStateChanged(object sender, SetBadgeQueuedArgs e)
        {
            // Just return if we aren't initialized.
            lock (this)
            {
                if (!isInitialized)
                {
                    return;
                }
            }

            cloudAppIconBadgeType convertedState = ConvertBadgeState(e.SetBadge.BadgeState);
            QueueSetBadge(convertedState, e.SetBadge.PathToBadge);
            e.MarkHandled();
        }

        private static cloudAppIconBadgeType ConvertBadgeState(PathState state)
        {
            cloudAppIconBadgeType convertedState;
            switch (state)
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
                    throw new ArgumentException("Unknown PathState: " + state.ToString());
            }
            return convertedState;
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
                        pInitialize(pathRootDirectory, initialList);
                        // store that list was already processed by initialization
                        listProcessed = true;
                    }
                }

                // Capture the Cloud directory path for performance.
                _filePathCloudDirectory = pathRootDirectory;

                // if list was not already processed by initialization
                if (!listProcessed)
                {
                    // lock internal list during modification
                    _trace.writeToLog(9, "IconOverlay: pInitializeOrReplace. List not processed.");
                    lock (_currentBadgesLocker)
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
                }
            }
            catch (Exception ex)
            {
                isInitialized = false;
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
        /// <remarks>Assumes the lock is already held on _currentBadges.</remarks>
        /// </summary>
        private void OnAllBadgesRecursiveDelete(FilePath recursivePathBeingDeleted, GenericHolder<cloudAppIconBadgeType> forBadgeType, FilePath originalDeletedPath)
        {
            try
            {
                // Just return if we aren't initialized.
                lock (this)
                {
                    if (!isInitialized)
                    {
                        return;
                    }
                }

                // Remove this path from the current flat dictionary if it exists, and send a remove badge path event to BadgeCom.
                if (_currentBadges.ContainsKey(recursivePathBeingDeleted))
                {
                    SendRemoveBadgePathEvent(recursivePathBeingDeleted);
                    _currentBadges.Remove(recursivePathBeingDeleted);
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "IconOverlay: OnAllBadgesRecursiveDelete: ERROR: Exception: Msg: <{0}>.", ex.Message);
            }
        }

        /// <summary>
        /// Callback driven when renames occur in allBadges.
        /// </summary>
        /// <param name="recursiveOldPath">The recursive path being renamed (old path) as a result of the originalOldPath being renamed.</param>
        /// <param name="recursiveRebuiltNewPath">The recursive renamed path (new path) as a result of the originalOldPath being renamed.</param>
        /// <param name="recursiveOldPathBadgeType">The associated badge type of the recursive path being renamed.</param>
        /// <param name="originalOldPath">The original old path being renamed that caused this recursive rename.</param>
        /// <param name="originalNewPath">The original new path that caused this recursive rename.</param>
        /// <remarks>Assumes the lock is already held on _currentBadges.</remarks>
        private void OnAllBadgesRecursiveRename(FilePath recursiveOldPath, FilePath recursiveRebuiltNewPath, GenericHolder<cloudAppIconBadgeType> recursiveOldPathBadgeType, FilePath originalOldPath, FilePath originalNewPath)
        {
            try
            {
                // Just return if we aren't initialized.
                lock (this)
                {
                    if (!isInitialized)
                    {
                        return;
                    }
                }

                // Remove the old path if it exists, and update BadgeCom.
                if (_currentBadges.ContainsKey(recursiveOldPath))
                {
                    SendRemoveBadgePathEvent(recursiveOldPath);
                    _currentBadges.Remove(recursiveOldPath);
                }

                // Send an add badge path at the new path, and update the flat dictionary
                SendAddBadgePathEvent(recursiveRebuiltNewPath, recursiveOldPathBadgeType);
                _currentBadges[recursiveRebuiltNewPath] =  recursiveOldPathBadgeType;

                            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "IconOverlay: OnAllBadgesRecursiveRename: ERROR: Exception: Msg: <{0}>.", ex.Message);
            }
        }

        /// <summary>
        /// Invoked by the indexer through queued events to delete a badge path in the hierarchical dictionary.
        /// This will also communicate the changes to BadgeCom and maintain the flat current badges dictionary.
        /// </summary>
        /// <param name="toDelete">The path being deleted.</param>
        /// <param name="isDeleted">(output) true: the path was deleted.</param>
        /// <param name="isPending">True: the event is pending.</param>
        /// <returns></returns>
        public static CLError DeleteBadgePath(FilePath toDelete, out bool isDeleted, bool isPending = false)
        {
            try
            {
                return Instance.pDeleteBadgePath(toDelete, out isDeleted, isPending);
            }
            catch (Exception ex)
            {
                isDeleted = Helpers.DefaultForType<bool>();
                return ex;
            }
        }
        // Private instance method to delete the badge path.
        private CLError pDeleteBadgePath(FilePath toDelete, out bool isDeleted, bool isPending = false, bool alreadyCheckedInitialized = false)
        {
            try
            {
                if (!alreadyCheckedInitialized)
                {
                    lock (this)
                    {
                        if (!isInitialized)
                        {
                            _trace.writeToLog(9, "IconOverlay: pRenameBadgePath. ERROR: THROW: Must be initialized before renaming badge paths.");
                            throw new Exception("IconOverlay must be initialized before renaming badge paths");
                        }
                    }
                }

                // lock internal list during modification
                lock (_currentBadgesLocker)
                {
                    // Simply pass this action on to the badge dictionary.  The dictionary will pass recursive deletes back to us.  The recursive
                    // delete will not happen for the toDelete node.
                    _trace.writeToLog(9, "IconOverlay: pRenameBadgePath. Pass this delete to the dictionary.");
                    isDeleted = allBadges.Remove(toDelete);

                    // If this event is pending, we need to add it back to the hierarchical badge dictionary so it will show "syncing" until the event is completed.
                    if (isPending)
                    {
                        pSetBadgeType(toDelete, new GenericHolder<cloudAppIconBadgeType>(cloudAppIconBadgeType.cloudAppBadgeSyncing), alreadyCheckedInitialized: true);
                    }

                    // Update badges for anything changed up the tree.  The nodes below will be deleted so we don't need to process those..
                    UpdateBadgeStateUpTreeStartingWithParentOfNode(toDelete, _filePathCloudDirectory.ToString());

                    // Update the badge for this specific node.
                    UpdateBadgeStateAtPath(toDelete);
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
        /// Invoked by the indexer through queued events to update the hierarchical badge path dictionary for this rename event.
        /// This also updates the BadgeCom badges and maintains the matching current badge state flat dictionary.
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
        // Private instance method to rename the badge path.
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
                lock (_currentBadgesLocker)
                {
                    // Simply pass this action on to the badge dictionary.  The dictionary will pass recursive renames back to us
                    // as the rename is processes, and those recursive renames will cause the badges to be adjusted.
                    // Put in a check if both paths already have values so we can overwrite at the new path.
                    _trace.writeToLog(9, "IconOverlay: pRenameBadgePath. Pass this rename to the dictionary.");
                    if (allBadges.ContainsKey(oldPath))
                    {
                        if (allBadges.ContainsKey(newPath))
                        {
                            allBadges.Remove(newPath);
                        }

                        // Perform the rename.  This may cause an error.  If it does, there will be no changes in the badge
                        // hierarchical dictionary.  Recover from the error by manually performing the rename.  Grab the hierarchy at and
                        // below the oldPath.  Then delete the oldPath (which will delete the oldPath node and its children).  Then
                        // loop through the hierarchy and manually apply each of the renames.  Set the badge and update the BadgeCom
                        // badges, and the current badge flat dictionary.
                        CLError error = allBadges.Rename(oldPath, newPath);
                        if (error != null)
                        {
                            _trace.writeToLog(9, "IconOverlay: pRenameBadgePath. ERROR: Renaming in allBadges. Msg: <{0}>.  Code: {1}.", error.errorDescription, error.errorCode);

                            FilePathHierarchicalNode<GenericHolder<cloudAppIconBadgeType>> tree;
                            CLError errGrab = allBadges.GrabHierarchyForPath(oldPath, out tree, suppressException: true);
                            if (errGrab == null
                                && tree != null)
                            {
                                // Delete the oldPath.
                                bool isDeleted;
                                pDeleteBadgePath(oldPath, out isDeleted, isPending: false, alreadyCheckedInitialized: true);

                                // Chase the hierarchy grabbed above and manually apply all of the renames.  Also set the BadgeCom and
                                // current badge flat dictionary badge states as we go.  This essentially adds back the deleted tree
                                // using the new names.
                                ManuallyApplyRenamesInHierarchyAndUpdateBadgeState(tree, oldPath, newPath);
                                return null;
                            }
                            else
                            {
                                if (errGrab == null)
                                {
                                    errGrab = new Exception("ERROR: Grabbing oldPath hierarchy: tree is null");
                                }
                                _trace.writeToLog(9, "IconOverlay: pRenameBadgePath. ERROR: Grabbing oldPath hierarchy. Msg: <{0}>.  Code: {1}.", errGrab.errorDescription, errGrab.errorCode);
                                return errGrab;
                            }
                        }
                        else
                        {
                            // No rename error.  Update the badge state for anything changed up the tree.
                            //TODO: This could be optimized by processing either of these (oldPath or newPath) first, then processing
                            // the other path only up to the point one below the overlapping root path.
                            UpdateBadgeStateUpTreeStartingWithParentOfNode(newPath, _filePathCloudDirectory.ToString());
                            UpdateBadgeStateUpTreeStartingWithParentOfNode(oldPath, _filePathCloudDirectory.ToString());

                            // Update the badge for this node
                            GenericHolder<cloudAppIconBadgeType> newBadgeType;
                            pFindBadge(newPath, out newBadgeType);        // always returns a badgeType, even if an error occurs.
                            SendRemoveBadgePathEvent(oldPath);
                            SendAddBadgePathEvent(newPath, newBadgeType);
                            _currentBadges.Remove(oldPath);
                            _currentBadges[newPath] = newBadgeType;

                            return null;
                        }
                    }
                    else
                    {
                        return new KeyNotFoundException("Could not find path to rename at oldPath");
                    }
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
        /// Recursively apply a rename to a node and all of its children.
        /// </summary>
        /// <param name="node">The top node in a hierarchy.</param>
        /// <param name="oldPath">The old path.</param>
        /// <param name="newPath">The new path.</param>
        private void ManuallyApplyRenamesInHierarchyAndUpdateBadgeState(FilePathHierarchicalNode<GenericHolder<cloudAppIconBadgeType>> node, FilePath oldPath, FilePath newPath)
        {
            // Error if no parms.
            if (node == null)
            {
                throw new Exception("node must not be null");
            }
            if (oldPath == null)
            {
                throw new Exception("oldPath must not be null");
            }
            if (newPath == null)
            {
                throw new Exception("newPath must not be null");
            }

            // Loop through all of the node's children
            foreach (FilePathHierarchicalNode<GenericHolder<cloudAppIconBadgeType>> child in node.Children)
            {
                ManuallyApplyRenamesInHierarchyAndUpdateBadgeState(child, oldPath, newPath);
            }

            // Process this node.  First apply the rename.
            FilePath.ApplyRename(node.Value.Key, oldPath, newPath);

            // Now add the badge back in and notify BadgeCom, etc.
            pSetBadgeType(node.Value.Key, node.Value.Value, alreadyCheckedInitialized: true);
        }

        /// <summary>
        /// Recursively update the badge state for a node and all of its children.
        /// </summary>
        /// <param name="node">The top node in a hierarchy.</param>
        /// <remarks>Assumes the lock is already held by the caller on _currentBadges.</remarks>
        private void UpdateBadgeStateInHierarchy(FilePathHierarchicalNode<GenericHolder<cloudAppIconBadgeType>> node)
        {
            // Error if no parms.
            if (node == null)
            {
                throw new Exception("node must not be null");
            }

            // Loop through all of the node's children
            foreach (FilePathHierarchicalNode<GenericHolder<cloudAppIconBadgeType>> child in node.Children)
            {
                UpdateBadgeStateInHierarchy(child);
            }

            // Process this node.  First apply the rename.
            UpdateBadgeStateAtPath(node.Value.Key);
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
        private CLError pSetBadgeType(FilePath filePath, GenericHolder<cloudAppIconBadgeType> newType, bool alreadyCheckedInitialized = false)
        {
            try
            {
                if (!alreadyCheckedInitialized)
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
                }

                // lock internal list during modification
                lock (_currentBadgesLocker)
                {
                    // Retrieve the hierarchy below if this node is selective.
                    FilePathHierarchicalNode<GenericHolder<cloudAppIconBadgeType>> selectiveTree = null;
                    if (newType.Equals(new GenericHolder<cloudAppIconBadgeType>(cloudAppIconBadgeType.cloudAppBadgeSyncSelective)))
                    {
                        CLError errGrab = allBadges.GrabHierarchyForPath(filePath, out selectiveTree, suppressException: true);
                        if (errGrab != null
                            || selectiveTree == null)
                        {
                            if (errGrab == null)
                            {
                                errGrab = new Exception("ERROR: Grabbing filePath hierarchy: tree is null");
                            }
                            _trace.writeToLog(9, "IconOverlay: pRenameBadgePath. ERROR: Grabbing filePath hierarchy. Msg: <{0}>.  Code: {1}.", errGrab.errorDescription, errGrab.errorCode);
                            return errGrab;
                        }

                    }

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

                    // Update badges for anything changed up the tree
                    UpdateBadgeStateUpTreeStartingWithParentOfNode(filePath, _filePathCloudDirectory.ToString());

                    // Potentially update badges in this node's children
                    if (selectiveTree != null)
                    {
                        // Update the badge state in the grabbed selective tree.
                        UpdateBadgeStateInHierarchy(selectiveTree);
                    }

                    // Update the badge for this node
                    UpdateBadgeStateAtPath(filePath);
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
                // ensure this is initialized
                lock (this)
                {
                    if (!isInitialized)
                    {
                        throw new Exception("IconOverlay must be initialized before setting badges");
                    }
                }

                // lock on dictionary so it is not modified during lookup
                lock (_currentBadgesLocker)
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

                                // Dispose the context menu stream
#if TRASH
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
#endif // TRASH &&&&
                            }

                            // Tell BadgeCom instances that we are going down.
                            try
                            {
                                if (_badgeComPubSubEvents != null)
                                {
                                    _badgeComPubSubEvents.PublishEventToBadgeCom(EnumEventType.BadgeNet_To_BadgeCom, EnumEventSubType.BadgeNet_RemoveSyncBoxFolderPath, 0 /* not used */, _filePathCloudDirectory.ToString());
                                }
                            }
                            catch (Exception ex)
                            {
                                _trace.writeToLog(1, "IconOverlay: Dispose. ERROR: Exception sending BadgeNet_RemoveSyncBoxFolderPath event. Msg: <{0}>.", ex.Message);
                            }

                            // Terminate the BadgeCom initialization watcher
                            try
                            {
                                if (_badgeComPubSubEvents != null)
                                {
                                    _badgeComPubSubEvents.BadgeComInitialized -= BadgeComPubSubEvents_OnBadgeComInitialized;
                                    _badgeComPubSubEvents.BadgeComInitializedSubscriptionFailed -= _badgeComPubSubEvents_OnBadgeComInitializationSubscriptionFailed;
                                    _badgeComPubSubEvents.Dispose();
                                    _badgeComPubSubEvents = null;
                                }
                            }
                            catch (Exception ex)
                            {
                                _trace.writeToLog(1, "IconOverlay: Dispose. ERROR: Exception terminating the BadgeCom initialzation watcher. Msg: <{0}>.", ex.Message);
                            }

                            // Clear other references.
                            lock (_currentBadgesLocker)
                            {
                                if (_currentBadges != null)
                                {
                                    _currentBadges.Clear();
                                    _currentBadges = null;
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
        private FilePath _filePathCloudDirectory { get; set; }

        /// <summary>
        /// The dictionary that holds the current state of all of the badges.
        /// </summary>
        private Dictionary<FilePath, GenericHolder<cloudAppIconBadgeType>> _currentBadges;
        private readonly object _currentBadgesLocker = new object();

        /// <summary>
        /// The hierarhical dictionary that holds all of the badges.  Nodes with null values are assumed to be synced.
        /// </summary>
        private FilePathDictionary<GenericHolder<cloudAppIconBadgeType>> allBadges;

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
        /// Creates the named pipe server stream for the shell extension context menu support.
        /// </summary>
#if TRASH        
        private NamedPipeServerContextMenu pipeContextMenuServer = null;
#endif  // TRASH &&&&&

        /// <summary>
        /// BadgeComInitWatcher threads subscribe and monitor initialization events from BadgeCom (Explorer shell extension).
        /// </summary>
        private BadgeComPubSubEvents _badgeComPubSubEvents = null;

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
#if TRASH         
        private void StartBadgeCOMPipes()
        {
            try
            {
                // Set up the thread params to start the pipe to listen to shell extension context menu messages
                _trace.writeToLog(9, "IconOverlay: StartBadgeCOMPipes. Start new server pipe for the context menu.");
                NamedPipeServerContextMenu serverContextMenu = new NamedPipeServerContextMenu();
                serverContextMenu.UserState = new NamedPipeServerContextMenu_UserState { FilePathCloudDirectory = _filePathCloudDirectory };
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
#endif // TRASH &&&&

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
                lock (_currentBadgesLocker)
                {
                    // There will be no badge if the path doesn't contain Cloud root
                    //_trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Locked.");
                    if (objFilePath.Contains(_filePathCloudDirectory))
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
                            // This specific node wasn't found, so it is assumed to be synced, but we won't actually badge as synced.
                            // Instead, we will search the children and determine a badge state from the children.
                            // 
                            // If an item is marked selective, then none of its children (whole hierarchy of children) should be badged.
                            //_trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. TryGetValue not successful.");
                            if (!FilePathComparer.Instance.Equals(objFilePath, _filePathCloudDirectory))
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

                                    // Quit if we are at the Cloud directory root
                                    //_trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Have we reached the Cloud root?");
                                    if (FilePathComparer.Instance.Equals(node, _filePathCloudDirectory))
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
                // Get the hierarchy of children of this node, including this node.
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
                    case cloudAppIconBadgeType.cloudAppBadgeFailed:             // the current node had no explicit value, and this child is failed, so the parent is syncing.
                        return cloudAppIconBadgeType.cloudAppBadgeSyncing;
                    case cloudAppIconBadgeType.cloudAppBadgeSyncSelective:      // the current node had no explicit value, and this child is selective, return synced (which is actually the default value, and will cause the recursion to continue looking).
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
        /// Update the badging state to match the hierarchical badge dictionary, working from the parent of a node up to the root.
        /// </summary>
        /// <remarks>Assumes the lock is already held by the caller on _currentBadges.</remarks>
        private void UpdateBadgeStateUpTreeStartingWithParentOfNode(FilePath nodePath, FilePath rootPath)
        {
            if (rootPath == null)
            {
                throw new NullReferenceException("rootPath cannot be null");
            }

            // Loop up the tree starting with the parent of the parm node.
            FilePath node = nodePath;
            while (node != null
                && node.Parent != null
                && !FilePathComparer.Instance.Equals(node, rootPath))
            {
                // Update the badging state at this node.  This will send events to BadgeCom, and will keep the current badge flat dictionary up to date.
                UpdateBadgeStateAtPath(node.Parent);

                // Chain up
                node = node.Parent;
            }
        }

        /// <summary>
        /// Update the badging state in all BadgeCom instances.  Also maintain the current badge status in a flat dictionary (_currentBadges).
        /// The new status is read from the allBadges hierarchichal dictionary.  If the new status is different than the
        /// current status, badging events are sent and the current badge status flat dictionary is updated.
        /// </summary>
        /// <remarks>Assumes the lock is already held by the caller on _currentBadges.</remarks>
        private void UpdateBadgeStateAtPath(FilePath nodePath)
        {
            // Get the new badge type for this nodePath from the hierarchical dictionary.
            GenericHolder<cloudAppIconBadgeType> hierarchicalBadgeType;
            pFindBadge(nodePath, out hierarchicalBadgeType);        // always returns a badgeType, even if an error occurs.

            // Get the current badge type for this nodePath from the flat dictionary.
            GenericHolder<cloudAppIconBadgeType> flatBadgeType;
            _currentBadges.TryGetValue(nodePath, out flatBadgeType);

            // Only process if they are different
            if ((flatBadgeType == null && hierarchicalBadgeType != null)
                || (flatBadgeType != null && !flatBadgeType.Equals(hierarchicalBadgeType)))
            {
                if (flatBadgeType != null)
                {
                    if (hierarchicalBadgeType != null)
                    {
                        // They are different and both specified.  Remove and add.
                        SendRemoveBadgePathEvent(nodePath);
                        SendAddBadgePathEvent(nodePath, hierarchicalBadgeType);
                        _currentBadges[nodePath] = hierarchicalBadgeType;
                    }
                    else
                    {
                        // They are different and there is no hierarchical badge.  Remove.
                        SendRemoveBadgePathEvent(nodePath);
                        _currentBadges.Remove(nodePath);
                    }
                }
                else
                {
                    // They are different and there is no flat badge.  Add.
                    SendAddBadgePathEvent(nodePath, hierarchicalBadgeType);
                    _currentBadges.Add(nodePath, hierarchicalBadgeType);
                }
            }
        }

        
        /// <summary>
        /// Send an add badge path event to all BadgeCom instances.
        /// </summary>
        /// <param name="nodePath">The path to add.</param>
        /// <param name="badgeType">The relevant badgeType.  Only the instances that filter for this type will process the event.</param>
        private void SendAddBadgePathEvent(FilePath nodePath, GenericHolder<cloudAppIconBadgeType> badgeType)
        {
            try 
	        {
                if (_badgeComPubSubEvents != null)
                {
                    _trace.writeToLog(9, "IconOverlay: SendAddBadgePathEvent. Entry.  Path: {0}.", nodePath.ToString());
                    _badgeComPubSubEvents.PublishEventToBadgeCom(EnumEventType.BadgeNet_To_BadgeCom, EnumEventSubType.BadgeNet_AddBadgePath, (EnumCloudAppIconBadgeType)badgeType.Value, nodePath.ToString());
                }
	        }
	        catch (global::System.Exception ex)
	        {
                _trace.writeToLog(1, "IconOverlay: SendAddBadgePathEvent. Exception.  Msg: {0}.", ex.Message);
	        }
        }

        /// <summary>
        /// Send a remove badge path event to all BadgeCom instances.
        /// </summary>
        /// <param name="nodePath">The path to remove.</param>
        private void SendRemoveBadgePathEvent(FilePath nodePath)
        {
            try 
	        {
                if (_badgeComPubSubEvents != null)
                {
                    _trace.writeToLog(9, "IconOverlay: SendRemoveBadgePathEvent. Entry.  Path: {0}.", nodePath.ToString());
                    _badgeComPubSubEvents.PublishEventToBadgeCom(EnumEventType.BadgeNet_To_BadgeCom, EnumEventSubType.BadgeNet_RemoveBadgePath, 0 /* not used */, nodePath.ToString());
                }
	        }
	        catch (global::System.Exception ex)
	        {
                _trace.writeToLog(1, "IconOverlay: SendRemoveBadgePathEvent. Exception.  Msg: {0}.", ex.Message);
	        }
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
                                        IconOverlay.DeleteBadgePath(mergedEvent.NewPath, out isDeleted, true);
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
