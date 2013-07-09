//
// IconOverlay.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

// Enable the following definition to trace the badging dictionaries.
//#define TRACE_BADGING_DICTIONARIES

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO.Pipes;
using System.Threading;
using System.Windows.Threading;
using Cloud.Model;
using Cloud.Static;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.IO;
using Cloud.Support;
using System.Runtime.InteropServices;
using BadgeCOMLib;
using Cloud.Interfaces;
using Cloud.Sync;
using Cloud.Model.EventMessages.ErrorInfo;
using System.Reflection;

namespace Cloud.BadgeNET
{
    /// <summary>
    /// IconOverlay is responsible for keeping a list of badges and synchronizing them with BadgeCOM (the Windows shell extensions for icon overlays)
    /// </summary>
    // \cond
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] //Hide From Intellisense
    public sealed class IconOverlay : IDisposable
    {
        private CLTrace _trace;
        private ICLSyncSettingsAdvanced _syncSettings;
        private Guid _guidPublisher;

        /// <summary>
        /// Public constructor.
        /// </summary>
        public IconOverlay()
        {
            _trace = CLTrace.Instance;

            // Allocate the badging current state flat dictionary.  This dictionary is used to determine the badge path to remove when the
            // badge type for that path changes.  We send a _kEvent_BadgeNet_AddBadgePath event to the BadgeCom "new" type, and a
            // _kEvent_BadgeNet_RemoveBadgePath event to the BadgeCom "old" type.
            _currentBadges = new Dictionary<FilePath, GenericHolder<cloudAppIconBadgeType>>(FilePathComparer.Instance);

            _guidPublisher = Guid.NewGuid();
            _trace.writeToLog(9, Resources.IconOverlayGUIDForThisPublisher, _guidPublisher.ToString());
        }

        /// <summary>
        /// Public destructor
        /// </summary>
        ~IconOverlay()
        {
            try
            {
                this.Dispose(false);
            }
            catch
            {
            }
        }

        #region public methods
        /// <summary>
        /// Initialize IconOverlay badge COM object processing with or without initial list
        /// (initial list can be added later by a call to InitializeOrReplace)
        /// ¡¡ Do not call this method a second time nor after InitializeOrReplace has been called !!
        /// </summary>
        /// <param name="initialList">(optional) list to start with for badged objects, filepaths in keys must not be null nor empty</param>
        /// <param name="syncSettings">The settings to use for this instance.</param>
        /// <param name="syncbox">The syncbox to use for this instance.</param>
        public CLError Initialize(ICLSyncSettings syncSettings, CLSyncbox syncbox, IEnumerable<KeyValuePair<FilePath, GenericHolder<cloudAppIconBadgeType>>> initialList = null)
        {
            try
            {
                if (syncSettings == null)
                {
                    throw new NullReferenceException(Resources.IconOverlaySyncSettingsCannotBeNull);
                }

                // Copy sync settings in case third party attempts to change values without restarting sync 
                _syncSettings = SyncSettingsExtensions.CopySettings(syncSettings);

                // Initialize trace in case it is not already initialized.
                CLTrace.Initialize(_syncSettings.TraceLocation, Resources.IconOverlayCloud, Resources.IconOverlayLog, _syncSettings.TraceLevel, _syncSettings.LogErrors);

                // Just exit if badging is not enabled.
                if (!(syncbox.SyncMode == CLSyncMode.CLSyncModeLiveWithBadgingEnabled))
                {
                    _trace.writeToLog(9, Resources.IconOverlayBadgingIsNotEnabledExit);
                    return null;
                }

                _trace.writeToLog(9, Resources.IconOverlayInitializeEntry);
                return pInitialize(syncbox, initialList);
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.Log(_syncSettings.TraceLocation, _syncSettings.LogErrors);
                _trace.writeToLog(1,
                    Resources.IconOverlayIntializeERRORExceptionMsg0Code1,
                    error.PrimaryException.Message,
                    error.PrimaryException.Code);
                return ex;
            }
        }
        private CLError pInitialize(CLSyncbox syncbox, IEnumerable<KeyValuePair<FilePath, GenericHolder<cloudAppIconBadgeType>>> initialList = null)
        {
            try
            {
                string cloudRoot = syncbox.Path;
                if (String.IsNullOrWhiteSpace(cloudRoot))
                {
                    throw new ArgumentException(Resources.IconOverlayCloudRootMustNotBeNullOrEmpty);
                }

                // ensure IconOverlay is only ever initialized once
                lock (this)
                {
                    if (isInitialized)
                    {
                        _trace.writeToLog(1, Resources.IconOverlayInstanceERRORTHROWAlreadyInitialized);
                        throw new Exception(Resources.IconOverlayInstanceAlreadyInitialized);
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
                    var threadInitDelegate = DelegateAndDataHolderBase.Create(
                        new
                        {
                            commonThisOverlay = this,
                            waitHandleInitialize = new AutoResetEvent(false),
                            initializeSuccessHolder = new GenericHolder<bool>(false),
                            innerSyncbox = syncbox
                        },
                        (Data, errorToAccumulate) =>
                        {
                            try
                            {
                                Data.commonThisOverlay._trace.writeToLog(9, Resources.IconOverlaythreadInitEntry);
                                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.MTA)
                                {
                                    Data.commonThisOverlay._trace.writeToLog(9, Resources.IconOverlaythreadInitERRORWrongThreadingModel);
                                    throw new Exception(Resources.IconOverlayWrongThreadingModel);
                                }

                                Data.commonThisOverlay._trace.writeToLog(9, Resources.IconOverlayThreadInitInstantiateBadgeComPubSubEvents);
                                Data.commonThisOverlay._badgeComPubSubEvents = new BadgeComPubSubEvents();
                                Data.commonThisOverlay._badgeComPubSubEvents.Initialize(Data.commonThisOverlay._syncSettings);
                                Data.commonThisOverlay._badgeComPubSubEvents.BadgeComInitialized += Data.commonThisOverlay.BadgeComPubSubEvents_OnBadgeComInitialized;
                                Data.commonThisOverlay._badgeComPubSubEvents.BadgeComInitializedSubscriptionFailed += Data.commonThisOverlay._badgeComPubSubEvents_OnBadgeComInitializationSubscriptionFailed;

                                Data.initializeSuccessHolder.Value = true;
                                Data.waitHandleInitialize.Set();
                            }
                            catch (Exception ex)
                            {
                                CLError error = ex;
                                error.Log(Data.commonThisOverlay._syncSettings.TraceLocation, Data.commonThisOverlay._syncSettings.LogErrors);
                                Data.commonThisOverlay._trace.writeToLog(1, Resources.IconOverlaypInitializeERRORthreadInitExceptionMsg0, ex.Message);

                                Data.initializeSuccessHolder.Value = false;
                                Data.waitHandleInitialize.Set();

                                Data.commonThisOverlay._trace.writeToLog(9, Resources.IconOverlaythreadInitExitThread2);
                                return;
                            }

                            try
                            {
                                // Start listening for BadgeCom initialization events.
                                Data.commonThisOverlay._trace.writeToLog(9, Resources.IconOverlaythreadInitSubscribeBadgeComInitEvents);
                                bool fIsSubscribed = Data.commonThisOverlay._badgeComPubSubEvents.SubscribeToBadgeComInitializationEvents();
                                if (!fIsSubscribed)
                                {
                                    throw new Exception(Resources.IconOverlaySubscriptionFailed);
                                }

                                // Send our badging dictionary to the BadgeCom subscribers.  Send it to each of the badge type instances.
                                Data.commonThisOverlay._trace.writeToLog(9, Resources.IconOverlaythreadInitSendBadgingDictionary);
                                foreach (EnumCloudAppIconBadgeType type in Enum.GetValues(typeof(EnumCloudAppIconBadgeType)).Cast<EnumCloudAppIconBadgeType>())
                                {
                                    if (type != EnumCloudAppIconBadgeType.cloudAppBadgeNone)
                                    {
                                        BadgeComPubSubEvents.BadgeTypeEventArgs args = new BadgeComPubSubEvents.BadgeTypeEventArgs();
                                        args.BadgeType = type;
                                        Data.commonThisOverlay.BadgeComPubSubEvents_OnBadgeComInitialized(this, args);
                                    }
                                }
                                Data.commonThisOverlay._trace.writeToLog(9, Resources.IconOverlaythreadInitFinishedSendingBadgingDictionary);
                            }
                            catch (Exception ex)
                            {
                                CLError error = ex;
                                error.Log(Data.commonThisOverlay._syncSettings.TraceLocation, Data.commonThisOverlay._syncSettings.LogErrors);
                                Data.commonThisOverlay._trace.writeToLog(1, Resources.IconOverlaypInitializeERRORthreadInitExceptionMsg0, ex.Message);

                                MessageEvents.FireNewEventMessage(
                                    Message: Resources.IconOverlayExplorerIconBadgingHasFailed,
                                    Level: EventMessageLevel.Important,
                                    Error: new GeneralErrorInfo(),
                                    Syncbox: Data.innerSyncbox,
                                    DeviceId: Data.commonThisOverlay._syncSettings.DeviceId);
                            }
                            Data.commonThisOverlay._trace.writeToLog(9, Resources.IconOverlayThreadInitExitThread);
                        },
                        null);

                    Thread threadInit = new Thread(new ThreadStart(threadInitDelegate.VoidProcess));
                    threadInit.SetApartmentState(ApartmentState.MTA);

                    threadInit.Start();

                    if (!threadInitDelegate.TypedData.waitHandleInitialize.WaitOne(60000)
                        || !threadInitDelegate.TypedData.initializeSuccessHolder.Value)
                    {
                        _trace.writeToLog(1, Resources.IconOverlaypInitializeERRORThreadInitWasNotStarted);
                        throw new CLException(CLExceptionCode.ShellExt_ExtensionInitialize,
                            Resources.IconOverlayThreadInitWasNotStarted);
                    }
                    else
                    {
                        _trace.writeToLog(9, Resources.IconOverlaypInitializethreadInitCompletedOK);
                    }
                }

                // Allocate the badging dictionary.  This is a hierarchical dictionary.
                CLError errorCreatingBadgingDictionary = FilePathDictionary<GenericHolder<cloudAppIconBadgeType>>.CreateAndInitialize(
                    rootPath: _filePathCloudDirectory,
                    pathDictionary: out allBadges,
                    recursiveDeleteCallback: OnAllBadgesRecursiveDelete,
                    recursiveRenameCallback: OnAllBadgesRecursiveRename);
                if (errorCreatingBadgingDictionary != null)
                {
                    string errorCreatingBadgingDictionaryString = Resources.IconOverlaypInitializeERRORTHROWErrorfromCreateAndInitializeAllBadges;

                    _trace.writeToLog(1, errorCreatingBadgingDictionaryString);
                    throw new CLException(CLExceptionCode.ShellExt_CreateBadgingDictionary,
                        errorCreatingBadgingDictionaryString,
                        errorCreatingBadgingDictionary.Exceptions);
                }

                if (allBadges == null)
                {
                    _trace.writeToLog(1, Resources.IconOverlaypInitializeERRORTHROWErrorFromCreatingBadgingDictionary);
                    throw new Exception(Resources.IconOverlayErrorCreatingBadgingDictionary);
                }

                lock (_currentBadgesSyncedLocker)
                {
                    CLError createCurrentBadgesSynced = FilePathDictionary<object>.CreateAndInitialize(
                        rootPath: _filePathCloudDirectory,
                        pathDictionary: out _currentBadgesSynced,
                        recursiveDeleteCallback: OnCurrentBadgesSyncedRecursiveDelete,
                        recursiveRenameCallback: OnCurrentBadgesSyncedRecursiveRename);
                    if (createCurrentBadgesSynced != null)
                    {
                        string createCurrentBadgesSyncedErrorString = Resources.IconOverlaypInitializeERRORTHROWErrorfromCreateAndInitializeCurrentBadgesSynced;

                        _trace.writeToLog(1, createCurrentBadgesSyncedErrorString);
                        throw new CLException(CLExceptionCode.ShellExt_CreateBadgingDictionary,
                            createCurrentBadgesSyncedErrorString,
                            createCurrentBadgesSynced.Exceptions);
                    }
                }

                // I don't want to enumerate the initialList for both counting and copying, so I define an array for storage
                KeyValuePair<FilePath, GenericHolder<cloudAppIconBadgeType>>[] initialListArray;
                // initial list contained values for badging; preload dictionary and notify system of global change
                if (initialList != null
                    && (initialListArray = initialList.ToArray()).Length > 0)
                {
                    // loop through initial list for badged objects to add to local dictionary
                    _trace.writeToLog(9, Resources.IconOverlaypInitializeGotInitialList);
                    for (int initialListCounter = 0; initialListCounter < initialListArray.Length; initialListCounter++)
                    {
                         // only keep track of badges that are not "synced"
                        if (initialListArray[initialListCounter].Value.Value != cloudAppIconBadgeType.cloudAppBadgeSynced)
                        {
                            // populate each initial badged object into local dictionary
                            // throws exception if file path (Key) is null or empty
                            // do not need to lock on allBadges since listening threads don't start until after this
                            _trace.writeToLog(9, Resources.IconOverlayAddBadgeForPath0Value1, initialListArray[initialListCounter].Key.ToString(), initialListArray[initialListCounter].Value.Value.ToString());
                            allBadges[initialListArray[initialListCounter].Key] = initialListArray[initialListCounter].Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.Log(_syncSettings.TraceLocation, _syncSettings.LogErrors);
                _trace.writeToLog(1, Resources.IconOverlaypInitializeERRORExceptionMsg0, ex.Message);

                // Attempt to clean up.  We may be partially initialized.
                Shutdown();

                // Uninitialized now.
                isInitialized = false;

                _trace.writeToLog(9, Resources.IconOverlaypInitializeTellUITheBadgingHasFailed);
                MessageEvents.FireNewEventMessage(
                    Message: Resources.IconOverlayExplorerIconBadgingHasFailed,
                    Level: EventMessageLevel.Important,
                    Error: null,
                    Syncbox: syncbox,
                    DeviceId: _syncSettings.DeviceId);
                return ex;
            }
            _trace.writeToLog(9, Resources.IconOverlaypInitializeReturnSuccess);
            return null;
        }

        /// <summary>
        /// The BadgeCom initialization event watcher threads died.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _badgeComPubSubEvents_OnBadgeComInitializationSubscriptionFailed(object sender, EventArgs e)
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
                        _trace.writeToLog(9, Resources.IconOverlaybadgeComPubSubEventsOnBadgeComInitializationSubscriptionFailedThreadInitEntry);
                        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.MTA)
                        {
                            _trace.writeToLog(9, Resources.IconOverlaybadgeComPubSubEventsOnBadgeComInitializationSubscriptionFailedERRORWrongThreadingModel);
                        }

                        if (_badgeComPubSubEvents != null)
                        {
                            // Kill the subscriptions and dispose the object
                            _trace.writeToLog(1, Resources.IconOverlaybadgeComPubSubEventsOnBadgeComInitializationSubscriptionFailedKillBadgeComPubSubEvents);
                            _badgeComPubSubEvents.BadgeComInitialized -= BadgeComPubSubEvents_OnBadgeComInitialized;
                            _badgeComPubSubEvents.BadgeComInitializedSubscriptionFailed -= _badgeComPubSubEvents_OnBadgeComInitializationSubscriptionFailed;
                            _badgeComPubSubEvents.Dispose();
                        }

                        // Restart
                        _trace.writeToLog(1, Resources.IconOverlaybadgeComPubSubEventsOnBadgeComInitializationSubscriptionFailedRestartBadgeComPubSubEvents);
                        _badgeComPubSubEvents = new BadgeComPubSubEvents();
                        _badgeComPubSubEvents.Initialize(_syncSettings);
                        _badgeComPubSubEvents.BadgeComInitialized += BadgeComPubSubEvents_OnBadgeComInitialized;
                        _badgeComPubSubEvents.BadgeComInitializedSubscriptionFailed += _badgeComPubSubEvents_OnBadgeComInitializationSubscriptionFailed;

                        // Start listening for BadgeCom initialization events.
                        _trace.writeToLog(9, Resources.IconOverlaybadgeComPubSubEventsOnBadgeComInitializationSubscriptionFailedSubscribeToBadgeComPubSubEvents);
                        bool fIsSubscribed = _badgeComPubSubEvents.SubscribeToBadgeComInitializationEvents();
                        if (!fIsSubscribed)
                        {
                            throw new Exception(Resources.IconOverlaySubscriptionFailed);
                        }

                        // Send our badging database to all of the BadgeCom subscribers.
                        _trace.writeToLog(9, Resources.IconOverlaybadgeComPubSubEventsOnBadgeComInitializationSubscriptionFailedSendBadingDatabase);
                        BadgeComPubSubEvents_OnBadgeComInitialized(null, null);
                    }
                    catch (Exception ex)
                    {
                        CLError error = ex;
                        error.Log(_syncSettings.TraceLocation, _syncSettings.LogErrors);
                        _trace.writeToLog(1, Resources.IconOverlaybadgeComPubSubEventsOnBadgeComInitializationSubscriptionFailedERRORthreadRestartexceptionMessage0, ex.Message);
                        isInitialized = false;
                    }
                }));

                // Start the thread, but don't wait for it to complete because the current thread is the CBadgeComPubSubEvents Watcher thread, and that thread must be killed by the restart logic above.
                if (isInitialized)
                {
                    threadRestart.SetApartmentState(ApartmentState.MTA);
                    threadRestart.Start();
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.Log(_syncSettings.TraceLocation, _syncSettings.LogErrors);
                _trace.writeToLog(1, Resources.IconOverlaybadgeComPubSubEventsOnBadgeComWatcherFailedERRORExceptionMessage0, ex.Message);
                isInitialized = false;
            }
        }

        /// <summary>
        /// Callback indicating that we received a BadgeCom initialization event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BadgeComPubSubEvents_OnBadgeComInitialized(object sender, BadgeComPubSubEvents.BadgeTypeEventArgs e)
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

                _trace.writeToLog(9, Resources.IconOverlayBadgeComPubSubEventsOnBadgeComInitializedEntryBadgeType0, e.BadgeType);
                if (_badgeComPubSubEvents != null)
                {
                    // Publish the remove Syncbox folder path event back to all BadgeCom instances.  This will clear any dictionaries involving those folder paths.  Each instance will clear only the path added by this process and _guidPublisher.
                    _badgeComPubSubEvents.PublishEventToBadgeCom(EnumEventType.BadgeNet_To_BadgeCom, EnumEventSubType.BadgeNet_RemoveSyncboxFolderPath, e.BadgeType, _filePathCloudDirectory.ToString(), _guidPublisher);

                    // Publish the add Syncbox folder path event back to all BadgeCom instances.
                    _badgeComPubSubEvents.PublishEventToBadgeCom(EnumEventType.BadgeNet_To_BadgeCom, EnumEventSubType.BadgeNet_AddSyncboxFolderPath, e.BadgeType, _filePathCloudDirectory.ToString(), _guidPublisher);

                    // Do not want to process COM publishing logic while holding up the lock, so make a copy under the lock instead.
                    Func<Dictionary<FilePath, GenericHolder<cloudAppIconBadgeType>>, object,
                        IEnumerable<KeyValuePair<FilePath, GenericHolder<cloudAppIconBadgeType>>>> copyBadgesUnderLock = (badgeDict, locker) =>
                        {
                            lock (locker)
                            {
                                return badgeDict.ToArray();
                            }
                        };

                    // Set the root directory badge to synced.  We do this to have the syncbox root directory badged in case that is the only directory (with no contents).
                    // Other badges below may cause the badge to be changed.
                    CLError errorFromSetBadgeType = setBadgeType(new GenericHolder<cloudAppIconBadgeType>(cloudAppIconBadgeType.cloudAppBadgeSynced), this._filePathCloudDirectory, alreadyCheckedInitialized: true);
                    if (errorFromSetBadgeType != null)
                    {
                        throw new AggregateException(Resources.IconOverlayErrorFromSettingSyncboxRootBadge, errorFromSetBadgeType.Exceptions);
                    }

                    // Iterate over the current badge state dictionary and send the badges to BadgeCom. This will populate the BadgeCom instance that just initialized.
                    // Other instances will update from these events only if necessary.
                    foreach (KeyValuePair<FilePath, GenericHolder<cloudAppIconBadgeType>> item in copyBadgesUnderLock(_currentBadges, _currentBadgesLocker))
                    {
                        // Publish a badge add path event to BadgeCom.
                        _badgeComPubSubEvents.PublishEventToBadgeCom(EnumEventType.BadgeNet_To_BadgeCom, EnumEventSubType.BadgeNet_AddBadgePath, (EnumCloudAppIconBadgeType)item.Value.Value, item.Key.ToString(), _guidPublisher);
                    }
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.Log(_syncSettings.TraceLocation, _syncSettings.LogErrors);
                _trace.writeToLog(1, Resources.IconOverlayBadgeComPubSubEventsOnBadgeComInitializedERRORExceptionMsg0, ex.Message);
            }
        }

        private void MessageEvents_BadgePathRenamed(object sender, BadgePathRenamedArgs e)
        {
            // Just return if we aren't initialized.
            lock (this)
            {
                if (!isInitialized)
                {
                    return;
                }
            }

            QueueRenameBadge(e.RenameBadgePath.FromPath, e.RenameBadgePath.ToPath);

            e.MarkHandled();
        }

        private void MessageEvents_BadgePathDeleted(object sender, BadgePathDeletedArgs e)
        {
            // Just return if we aren't initialized.
            lock (this)
            {
                if (!isInitialized)
                {
                    return;
                }
            }

            EventWaitHandle synchronizeDeleteHandle = new EventWaitHandle(initialState: false, mode: EventResetMode.ManualReset);

            QueueDeleteBadge(e.DeleteBadgePath.PathToDelete,
                onIsDeletedState => onIsDeletedState.MarkDeleted(),
                onIsDeletedState: e,
                completionHandle: synchronizeDeleteHandle);

            synchronizeDeleteHandle.WaitOne();

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

            _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayMessageEventsQueueSetBadgeChangedState0Path1, e.SetBadge.BadgeState.ToString(), e.SetBadge.PathToBadge));
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

            _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayMessageEventsFileChangeMergeToStateChangedCallQueueNewEventsBadge));
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

            _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayMessageEventsPathStateChangedState0Path1, e.SetBadge.BadgeState.ToString(), e.SetBadge.PathToBadge));
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
                    throw new ArgumentException(Resources.IconOverlayUnknownState + state.ToString());
            }
            return convertedState;
        }

        /// <summary>
        /// Returns whether IconOverlay is already initialized. If it is initialized, do not initialize it again.
        /// </summary>
        /// <param name="isInitialized">Return value</param>
        /// <returns>Error if it exists</returns>
        public CLError IsBadgingInitialized(out bool isInitialized)
        {
            try
            {
                isInitialized = this.IsInitialized;
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.Log(_syncSettings.TraceLocation, _syncSettings.LogErrors);
                _trace.writeToLog(1,
                    Resources.IconOverlayIsBadgingInitializedERRORExceptionMsg0Code1,
                    error.PrimaryException.Message,
                    error.PrimaryException.Code);
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
                lock (this)
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
        /// <param name="syncbox">The Syncbox.</param>
        public CLError InitializeOrReplace(string pathRootDirectory, CLSyncbox syncbox, IEnumerable<KeyValuePair<FilePath, GenericHolder<cloudAppIconBadgeType>>> initialList)
        {
            try
            {
                bool listProcessed = false;

                // ensure IconOverlay is only ever initialized once
                _trace.writeToLog(9, Resources.IconOverlayInitializeOrReplaceEntry);
                lock (this)
                {
                    // run initialize instead if it has not been run
                    if (!isInitialized)
                    {
                        _trace.writeToLog(9, Resources.IconOverlayInitializeOrReplaceNotInitializedYetInitialize);
                        pInitialize(syncbox, initialList);
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
                    _trace.writeToLog(9, Resources.IconOverlayInitializeOrReplaceListNotProcessed);
                    lock (_currentBadgesLocker)
                    {
                        // empty list before adding in all replacement items
                        _trace.writeToLog(9, Resources.IconOverlayInitializeOrReplaceClearAllBadges);
                        allBadges.Clear();
                        foreach (KeyValuePair<FilePath, GenericHolder<cloudAppIconBadgeType>> currentReplacedItem in initialList ?? new KeyValuePair<FilePath, GenericHolder<cloudAppIconBadgeType>>[0])
                        {
                            // only keep track of badges that are not "synced"
                            if (currentReplacedItem.Value.Value != cloudAppIconBadgeType.cloudAppBadgeSynced)
                            {
                                // populate each replaced badged object into local dictionary
                                // throws exception if file path (Key) is null or empty
                                _trace.writeToLog(9, Resources.IconOverlayInitializeOrReplaceAddThisItemToDictionary);
                                allBadges.Add(currentReplacedItem.Key, currentReplacedItem.Value);
                            }                        }
                    }
                }
            }
            catch (Exception ex)
            {
                isInitialized = false;
                CLError error = ex;
                error.Log(_syncSettings.TraceLocation, _syncSettings.LogErrors);
                _trace.writeToLog(1,
                    Resources.IconOverlayInitializeOrReplaceERRORExceptionMsg0Code1,
                    error.PrimaryException.Message,
                    error.PrimaryException.Code);
                return ex;
            }
            return null;
        }

        private void OnCurrentBadgesSyncedRecursiveDelete(FilePath recursivePathBeingDeleted, object notUsed, FilePath originalDeletedPath)
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

                _currentBadges.Remove(recursivePathBeingDeleted);
                SendRemoveBadgePathEvent(recursivePathBeingDeleted);
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.Log(_syncSettings.TraceLocation, _syncSettings.LogErrors);
                _trace.writeToLog(1, Resources.IconOverlayOnCurrentBadgesSyncedRecursiveDeleteERRORExceptionMsg0, ex.Message);
            }
        }

        private void OnCurrentBadgesSyncedRecursiveRename(FilePath recursiveOldPath, FilePath recursiveRebuiltNewPath, object notUsed, FilePath originalOldPath, FilePath originalNewPath)
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

                GenericHolder<cloudAppIconBadgeType> existingSyncedHolder;
                if (_currentBadges.TryGetValue(recursiveOldPath, out existingSyncedHolder))
                {
                    _currentBadges.Remove(recursiveOldPath);
                    if (existingSyncedHolder.Value != cloudAppIconBadgeType.cloudAppBadgeSynced)
                    {
                        existingSyncedHolder = new GenericHolder<cloudAppIconBadgeType>(cloudAppIconBadgeType.cloudAppBadgeSynced);
                    }
                }
                else
                {
                    existingSyncedHolder = new GenericHolder<cloudAppIconBadgeType>(cloudAppIconBadgeType.cloudAppBadgeSynced);
                }

                SendRemoveBadgePathEvent(recursiveOldPath);
                SendRemoveBadgePathEvent(recursiveRebuiltNewPath);
                SendAddBadgePathEvent(recursiveRebuiltNewPath, existingSyncedHolder);
                _currentBadges[recursiveRebuiltNewPath] = existingSyncedHolder;
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.Log(_syncSettings.TraceLocation, _syncSettings.LogErrors);
                _trace.writeToLog(1, Resources.IconOverlayOnCurrentBadgesSyncedRecursiveRenameERRORExceptionMsg0, ex.Message);
            }
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
                CLError error = ex;
                error.Log(_syncSettings.TraceLocation, _syncSettings.LogErrors);
                _trace.writeToLog(1, Resources.IconOverlayOnAllBadgesRecursiveDeleteERRORExceptionMsg0, ex.Message);
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
                _currentBadges[recursiveRebuiltNewPath] = recursiveOldPathBadgeType;
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.Log(_syncSettings.TraceLocation, _syncSettings.LogErrors);
                _trace.writeToLog(1, Resources.IconOverlayOnAllBadgesRecursiveRenameERRORExceptionMsg0, ex.Message);
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
        public CLError DeleteBadgePath(FilePath toDelete, out bool isDeleted, bool isPending = false, bool alreadyCheckedInitialized = false)
        {
            try
            {
                if (!alreadyCheckedInitialized)
                {
                    lock (this)
                    {
                        if (!isInitialized)
                        {
                            _trace.writeToLog(9, Resources.IconOverlayDeleteBadgePathERRORTHROWMustBeInitializedBeforeRenamingBadgePaths);
                            throw new Exception(Resources.IconOverlayMustBeInitializedBeforeRenamingBadgePaths);
                        }
                    }
                }

                // Trace the badging dictionaries
                TraceBadgingDictionaries(Resources.IconOverlayDeleteBadgePathBefore + toDelete);

                // lock internal list during modification
                lock (_currentBadgesLocker)
                {
                    // Simply pass this action on to the badge dictionary.  The dictionary will pass recursive deletes back to us.  The recursive
                    // delete will not happen for the toDelete node.
                    _trace.writeToLog(9, Resources.IconOverlayDeleteBadgePathPassThisDeleteToDictionary);
                    isDeleted = allBadges.Remove(toDelete);

                    // If this event is pending, we need to add it back to the hierarchical badge dictionary so it will show "syncing" until the event is completed.
                    if (isPending)
                    {
                        setBadgeType(new GenericHolder<cloudAppIconBadgeType>(cloudAppIconBadgeType.cloudAppBadgeSyncing), toDelete, alreadyCheckedInitialized: true);
                    }

                    lock (_currentBadgesSynced)
                    {
                        if (_currentBadgesSynced != null)
                        {
                            _currentBadgesSynced.Remove(toDelete);
                        }
                    }

                    // Update badges for anything changed up the tree.  The nodes below will be deleted so we don't need to process those..
                    UpdateBadgeStateUpTreeStartingWithParentOfNode(toDelete, _filePathCloudDirectory.ToString());

                    // Update the badge for this specific node.
                    UpdateBadgeStateAtPath(toDelete);

                    // Actually delete the badge on all of the badgecom instances.
                    SendRemoveBadgePathEvent(toDelete);
 
                    // Remove the badge from the curerent badges list.
                    _currentBadges.Remove(toDelete);

                    // Trace the badging dictionaries
                    TraceBadgingDictionaries(Resources.IconOverlayDeleteBadgePathAfter + toDelete);
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.Log(_syncSettings.TraceLocation, _syncSettings.LogErrors);
                isDeleted = Helpers.DefaultForType<bool>();
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Trace the badging dictionaries
        /// </summary>
        private void TraceBadgingDictionaries(string whereCalled)
        {
#if TRACE_BADGING_DICTIONARIES
            lock (_currentBadgesLocker)
            {
                lock (_currentBadgesSyncedLocker)
                {
                    // Trace the current badging flat dictionary
                    _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayTraceBadgingDictionariesCurrentBadgesCalledFrom0, whereCalled));
                    foreach (KeyValuePair<FilePath, GenericHolder<cloudAppIconBadgeType>> badge in _currentBadges)
                    {
                        _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayBadgeType0Path1, badge.Value.Value.ToString(), badge.Key));
                    }

                    // Trace the current badging "synced" hierarchical dictionary
                    _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayTraceBadgingDictionariesCurrentSyncedBadges));
                    foreach (KeyValuePair<FilePath, object> badge in _currentBadgesSynced)
                    {
                        _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayPath0, badge.Key));
                    }
                }
            }

#endif // TRACE_BADGING_DICTIONARIES
        }

        /// <summary>
        /// Invoked by the indexer through queued events to update the hierarchical badge path dictionary for this rename event.
        /// This also updates the BadgeCom badges and maintains the matching current badge state flat dictionary.
        /// </summary>
        /// <param name="oldPath">The old path.</param>
        /// <param name="newPath">The new path.</param>
        /// <returns></returns>
        public CLError RenameBadgePath(FilePath oldPath, FilePath newPath)
        {
            CLError errorToReturn = null;
            try
            {
                // ensure this is initialized
                _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathOldPath0NewPath1, oldPath.ToString(), newPath.ToString()));
                lock (this)
                {
                    if (!isInitialized)
                    {
                        _trace.writeToLog(9, Resources.IconOverlayRenameBadgePathERRORTHROWMustBeInitializedBeforeRenamingBadgePaths);
                        throw new Exception(Resources.IconOverlayMustBeInitializedBeforeRenamingBadgePaths);
                    }
                }

                // Trace the badging dictionaries
                TraceBadgingDictionaries(Resources.IconOverlayRenameBadgePathBeforeOldPath + oldPath.ToString() + Resources.IconOverlayNewPath + newPath.ToString() + Resources.GreaterThanPeriod);

                // lock internal list during modification
                lock (_currentBadgesLocker)
                {
                    // Simply pass this action on to the badge dictionary.  The dictionary will pass recursive renames back to us
                    // as the rename is processes, and those recursive renames will cause the badges to be adjusted.
                    // Put in a check if both paths already have values so we can overwrite at the new path.
                    _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathPassThisRenameToTheDictionary));
                    if (allBadges.ContainsKey(oldPath))
                    {
                        _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathContainsOldPath));
                        if (allBadges.ContainsKey(newPath))
                        {
                            _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathRemoveNewPath));
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
                            _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathERRORRenamingInAllBadgesMsg0Code1, error.PrimaryException.Message, error.PrimaryException.Code));

                            FilePathHierarchicalNode<GenericHolder<cloudAppIconBadgeType>> tree;
                            CLError errGrab = allBadges.GrabHierarchyForPath(oldPath, out tree, suppressException: true);
                            if (errGrab == null
                                && tree != null)
                            {
                                // Delete the oldPath.
                                bool isDeleted;
                                _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathDeleteOldPath));
                                DeleteBadgePath(oldPath, out isDeleted, isPending: false, alreadyCheckedInitialized: true);

                                // Chase the hierarchy grabbed above and manually apply all of the renames.  Also set the BadgeCom and
                                // current badge flat dictionary badge states as we go.  This essentially adds back the deleted tree
                                // using the new names.
                                _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathCallManuallyApplyRenamesInHeirarchyAndUpdateBadgeState));
                                ManuallyApplyRenamesInHierarchyAndUpdateBadgeState(tree, oldPath, newPath);

                                lock (_currentBadgesSyncedLocker)
                                {
                                    _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadePathInCurrentBadgesSyncedLocker));
                                    if (_currentBadgesSynced != null)
                                    {
                                        object notUsed;
                                        _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathCurrentBadgesSyncedNotNull));
                                        if (_currentBadgesSynced.TryGetValue(oldPath, out notUsed))
                                        {
                                            _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathGotCurrentBadgeSyncedOldPathItem));
                                            GenericHolder<cloudAppIconBadgeType> existingSyncedHolder;
                                            if (_currentBadges.TryGetValue(oldPath, out existingSyncedHolder))
                                            {
                                                _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathGotCurrentBadgesOldPathItemRemoveOldPath));
                                                _currentBadges.Remove(oldPath);
                                                if (existingSyncedHolder.Value != cloudAppIconBadgeType.cloudAppBadgeSynced)
                                                {
                                                    _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathExistingSynchedHolderIsSynced));
                                                    existingSyncedHolder = new GenericHolder<cloudAppIconBadgeType>(cloudAppIconBadgeType.cloudAppBadgeSynced);
                                                }
                                            }
                                            else
                                            {
                                                _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathExistingSynchedHolderIsSynced2));
                                                existingSyncedHolder = new GenericHolder<cloudAppIconBadgeType>(cloudAppIconBadgeType.cloudAppBadgeSynced);
                                            }

                                            _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathRemoveBadgeOldPath0, oldPath));
                                            SendRemoveBadgePathEvent(oldPath);
                                            _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathRemoveBadgeNewPath0, newPath));
                                            SendRemoveBadgePathEvent(newPath);
                                            _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathAddBadgeAtNewPath0Badge1, newPath, existingSyncedHolder.Value.ToString()));
                                            SendAddBadgePathEvent(newPath, existingSyncedHolder);
                                            _currentBadges[newPath] = existingSyncedHolder;
                                        }

                                        _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathRenameOldPathToNewPathInCurrentBadgesSynched));
                                        _currentBadgesSynced.Rename(oldPath, newPath);

                                        // Update badges for anything changed up the tree
                                        _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathUpdateBadgeStateUpTheTreeFromOldPath));
                                        UpdateBadgeStateUpTreeStartingWithParentOfNode(oldPath, _filePathCloudDirectory.ToString());

                                        // Update badges for anything changed up the tree
                                        _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathUpdateBadgeStateUpTheTreeFromNewPath));
                                        UpdateBadgeStateUpTreeStartingWithParentOfNode(newPath, _filePathCloudDirectory.ToString());
                                    }
                                }

                                _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathSetReturnStatusOK));
                                errorToReturn = null;
                            }
                            else
                            {
                                _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathERRORGrabbingHeirarchyOrNothingGrabbed));
                                if (errGrab == null)
                                {
                                    errGrab = new Exception(Resources.IconOverlayERRORGrabbingOldPathHeirarchyTreeIsNull);
                                }
                                _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathERRORGrabbingOldPathHeirarchyMsg0Code1, errGrab.PrimaryException.Message, errGrab.PrimaryException.Code));
                                errorToReturn = errGrab;
                            }
                        }
                        else
                        {
                            _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathSuccessOnAllBadgesRename));
                            // No rename error.  Update the badge state for anything changed up the tree.
                            //TODO: This could be optimized by processing either of these (oldPath or newPath) first, then processing
                            // the other path only up to the point one below the overlapping root path.
                            UpdateBadgeStateUpTreeStartingWithParentOfNode(newPath, _filePathCloudDirectory.ToString());
                            UpdateBadgeStateUpTreeStartingWithParentOfNode(oldPath, _filePathCloudDirectory.ToString());

                            // Update the badge for this node
                            GenericHolder<cloudAppIconBadgeType> newBadgeType;
                            getBadgeTypeForFileAtPath(newPath, out newBadgeType);        // always returns a badgeType, even if an error occurs.
                            SendRemoveBadgePathEvent(oldPath);
                            _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathBadgeNewPathWith0, newBadgeType.Value.ToString()));
                            SendAddBadgePathEvent(newPath, newBadgeType);
                            _currentBadges.Remove(oldPath);
                            _currentBadges[newPath] = newBadgeType;
                            
                            lock (_currentBadgesSyncedLocker)
                            {
                                _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadePathInCurrentBadgesSyncedLocker));
                                if (_currentBadgesSynced != null)
                                {
                                    _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathCurrentBadgesSyncedNotNull)); 

                                    FilePathHierarchicalNode<object> oldPathSyncedHierarchy;
                                    CLError grabOldPathSyncedHierarchyError = _currentBadgesSynced.GrabHierarchyForPath(oldPath, out oldPathSyncedHierarchy, suppressException: true);
                                    if (grabOldPathSyncedHierarchyError == null
                                        && oldPathSyncedHierarchy != null)
                                    {
                                        _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathOldPathFoundInCurrentBadgesSynedRename));
                                        _currentBadgesSynced.Remove(newPath);
                                        _currentBadgesSynced.Rename(oldPath, newPath);
                                    }

                                    _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathSetNewPathToCurrentBadgesSyncedValue0, newBadgeType.Value.ToString()));
                                    _currentBadgesSynced[newPath] = (newBadgeType.Value == cloudAppIconBadgeType.cloudAppBadgeSynced
                                        ? _syncedDictionaryValue
                                        : null);
                                }
                            }

                            _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathSetReturnStatusOK2));
                            errorToReturn = null;
                        }
                    }
                    else
                    {
                        _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathSetERRORCouldNotFindPathToRenameAtOldPath));
                        errorToReturn = new KeyNotFoundException(Resources.CouldNotFindPathToRenameOldPath);
                    }
                }

                // Trace the badging dictionaries
                TraceBadgingDictionaries(Resources.IconOverlayRenameBadgePathAfterOldPath + oldPath.ToString() + Resources.IconOverlayNewPath + newPath.ToString() + Resources.GreaterThanPeriod);
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.Log(_syncSettings.TraceLocation, _syncSettings.LogErrors);
                _trace.writeToLog(1, Resources.IconOverlayRenameBadgePathERRORExceptionMessage0Code1, error.PrimaryException.Message, error.PrimaryException.Code);
                _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayRenameBadgePathERRORExceptionMessage0Code1, error.PrimaryException.Message, error.PrimaryException.Code));
                errorToReturn = error;
            }

            return errorToReturn;
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
                throw new Exception(Resources.IconOverlayNodeMustNotBeNull);
            }
            if (oldPath == null)
            {
                throw new Exception(Resources.IconOverlayOldPathMustNotBeNull);
            }
            if (newPath == null)
            {
                throw new Exception(Resources.IconOverlayNewPathMustNotNotBeNull);
            }

            // Loop through all of the node's children
            foreach (FilePathHierarchicalNode<GenericHolder<cloudAppIconBadgeType>> child in node.Children)
            {
                ManuallyApplyRenamesInHierarchyAndUpdateBadgeState(child, oldPath, newPath);
            }

            // Process this node.  First apply the rename.
            FilePath.ApplyRename(node.Value.Key, oldPath, newPath);

            // Now add the badge back in and notify BadgeCom, etc.
            setBadgeType(node.Value.Value, node.Value.Key, alreadyCheckedInitialized: true);
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
                throw new Exception(Resources.IconOverlayNodeMustNotBeNull);
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
        public CLError setBadgeType(GenericHolder<cloudAppIconBadgeType> newType, FilePath filePath, bool alreadyCheckedInitialized = false)
        {
            try
            {
                _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlaySetBadgeTypeEntryNewType0FilePath1, newType.Value.ToString(), filePath));
                if (!filePath.Contains(this._filePathCloudDirectory))
                {
                    return null;
                }

                if (!alreadyCheckedInitialized)
                {
                    // ensure this is initialized
                    //_trace.writeToLog(9, "IconOverlay: setBadgeType. Path: {0}, Type: {1}.", filePath.ToString(), newType.Value.ToString());
                    lock (this)
                    {
                        if (!isInitialized)
                        {
                            //_trace.writeToLog(9, "IconOverlay: setBadgeType. ERROR: THROW: Must be initialized before setting badges.");
                            throw new Exception(Resources.IconOverlayMustBeInitializedBeforeSettingBadges);
                        }
                    }
                }

                // lock internal list during modification
                lock (_currentBadgesLocker)
                {
                    // Retrieve the hierarchy below if this node is selective.
                    _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlaySetBadgeTypeInCurrentBadgesLocker));
                    FilePathHierarchicalNode<GenericHolder<cloudAppIconBadgeType>> selectiveTree = null;
                    if (newType.Equals(new GenericHolder<cloudAppIconBadgeType>(cloudAppIconBadgeType.cloudAppBadgeSyncSelective)))
                    {
                        _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlaySetBadgeTypeNewIsSelective));
                        CLError errGrab = allBadges.GrabHierarchyForPath(filePath, out selectiveTree, suppressException: true);
                        if (errGrab != null
                            || selectiveTree == null)
                        {
                            if (errGrab == null)
                            {
                                errGrab = new Exception(Resources.IconOverlayERRORGrabbingfilePathHeirarchyTreeIsNull);
                            }
                            _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlaySetBadgeTypeERRORGrabbingFilePathHeirarchyMsg0Code1, errGrab.PrimaryException.Message, errGrab.PrimaryException.Code));
                            _trace.writeToLog(1, Resources.IconOverlaySetBadgeTypeERRORGrabbingFilePathHeirarchyMsg0Code1, errGrab.PrimaryException.Message, errGrab.PrimaryException.Code);
                            return errGrab;
                        }

                    }

                    // newType is null means synced.  If the type is synced, newType will be null.  Set it whatever it is.
                    //_trace.writeToLog(9, "IconOverlay: setBadgeType. Add this type to the dictionary.");
                    if (newType.Value == cloudAppIconBadgeType.cloudAppBadgeSynced)
                    {
                        _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlaySetBadgeTypeNewTypeIsSynced));
                        allBadges[filePath] = null;
                    }
                    else
                    {
                        _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlaySetBadgeTypeNewTypeIs0, newType.Value.ToString()));
                        allBadges[filePath] = newType;
                    }

                    // Update badges for anything changed up the tree
                    _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlaySetBadgeTypeCallUpdateBadgeStateUpTreeStartingWithParentOfNode));
                    UpdateBadgeStateUpTreeStartingWithParentOfNode(filePath, _filePathCloudDirectory.ToString());

                    // Potentially update badges in this node's children
                    _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlaySetBadgeTypeBackFromUpdateBadgeStateUpTreeStartingWithParentOfNode));
                    if (selectiveTree != null)
                    {
                        // Update the badge state in the grabbed selective tree.
                        _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlaySetBadgeTypeGotSelectiveTree));
                        UpdateBadgeStateInHierarchy(selectiveTree);
                    }

                    // Update the badge for this node
                    _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlaySetBadgeTypeCallUpdateBadgeStateAtPath));
                    UpdateBadgeStateAtPath(filePath);
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.Log(_syncSettings.TraceLocation, _syncSettings.LogErrors);
                _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlaySetBadgeTypeERRORExceptionMsg0Code1, error.PrimaryException.Message, error.PrimaryException.Code));
                _trace.writeToLog(1, Resources.IconOverlaySetBadgeTypeERRORExceptionMsg0Code1, error.PrimaryException.Message, error.PrimaryException.Code);
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Returns badge type for a given file path, if it exists
        /// </summary>
        /// <param name="filePath">path of file to check</param>
        /// <returns></returns>
        public CLError getBadgeTypeForFileAtPath(FilePath path, out GenericHolder<cloudAppIconBadgeType> badgeType)
        {
            try
            {
                _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayGetBadgeTypeForFileAtPathEntryPath0, path.ToString()));

                // ensure this is initialized
                lock (this)
                {
                    if (!isInitialized)
                    {
                        throw new Exception(Resources.IconOverlayMustBeInitializedBeforeSettingBadges);
                    }
                }

                // lock on dictionary so it is not modified during lookup
                lock (_currentBadgesLocker)
                {
                    _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayGetBadgeTypeForFileAtPathInCurrentBadgesLocker));
                    foreach (cloudAppIconBadgeType currentBadge in Enum.GetValues(typeof(cloudAppIconBadgeType)).Cast<cloudAppIconBadgeType>())
                    {
                        _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayCallShouldIconBeBadgedAtThisPathCurrentBadge0, currentBadge.ToString()));
                        if (ShouldIconBeBadged(currentBadge, path.ToString()))
                        {
                            _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayReturnThisBadgeType));
                            badgeType = new GenericHolder<cloudAppIconBadgeType>(currentBadge);
                            return null;
                        }
                    }

                    _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayReturnNullNoBadgeType));
                    badgeType = null;
                    return null;
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.Log(_syncSettings.TraceLocation, _syncSettings.LogErrors);
                _trace.writeToLog(1, Resources.IconOverlaygetBadgeTypeForFileAtPathERRORExceptionMsg0Code1, error.PrimaryException.Message, error.PrimaryException.Code);
                _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlaygetBadgeTypeForFileAtPathERRORExceptionMsg0Code1, error.PrimaryException.Message, error.PrimaryException.Code));
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
        public CLError Shutdown()
        {
            try
            {
                ((IDisposable)this).Dispose();
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.Log(_syncSettings.TraceLocation, _syncSettings.LogErrors);
                _trace.writeToLog(1, Resources.IconOverlayPShutdownERRORExceptionMsg0Code1, error.PrimaryException.Message, error.PrimaryException.Code);
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
                _trace.writeToLog(9, Resources.IconOverlayDisposeLock);
                lock (this)
                {
                    // monitor is now set as disposed which will produce errors if startup is called later
                    _trace.writeToLog(9, Resources.IconOverlayDisposeSetDisposed);
                    Disposed = true;
                }

                // Run dispose on inner managed objects based on disposing condition
                if (disposing)
                {
                    // locks on this in case initialization is occurring simultaneously
                    _trace.writeToLog(9, Resources.IconOverlayDisposeDisposing);
                    bool shouldCleanUp = false;
                    lock (this)
                    {
                        // only need to clean up if it was initialized
                        if (isInitialized)
                        {
                            shouldCleanUp = true;
                            isInitialized = false;

                        }
                    }

                    if (shouldCleanUp)
                    {
                        // Tell BadgeCom instances that we are going down.
                        try
                        {
                            if (_badgeComPubSubEvents != null)
                            {
                                _trace.writeToLog(9, Resources.IconOverlayDisposeSendBadgeNetRemoveSyncbicFolderPathEvent);
                                _badgeComPubSubEvents.PublishEventToBadgeCom(EnumEventType.BadgeNet_To_BadgeCom, EnumEventSubType.BadgeNet_RemoveSyncboxFolderPath, EnumCloudAppIconBadgeType.cloudAppBadgeNone /* clear all badge types */, _filePathCloudDirectory.ToString(), _guidPublisher);
                            }
                        }
                        catch (Exception ex)
                        {
                            CLError error = ex;
                            error.Log(_syncSettings.TraceLocation, _syncSettings.LogErrors);
                            _trace.writeToLog(1, Resources.IconOverlayDisposedERRORExceptionSendingBadgeNetRemoveSyncboxFolderPathEventMsg0, ex.Message);
                        }

                        // Terminate the BadgeCom initialization watcher
                        try
                        {
                            if (_badgeComPubSubEvents != null)
                            {
                                _trace.writeToLog(9, Resources.IconOverlayDisposeUnsubscribeFromBadgeComEvents);
                                _badgeComPubSubEvents.BadgeComInitialized -= BadgeComPubSubEvents_OnBadgeComInitialized;
                                _badgeComPubSubEvents.BadgeComInitializedSubscriptionFailed -= _badgeComPubSubEvents_OnBadgeComInitializationSubscriptionFailed;
                                _badgeComPubSubEvents.Dispose();
                                _badgeComPubSubEvents = null;
                            }
                        }
                        catch (Exception ex)
                        {
                            CLError error = ex;
                            error.Log(_syncSettings.TraceLocation, _syncSettings.LogErrors);
                            _trace.writeToLog(1, Resources.IconOverlayDisposeERRORExceptionTerminatingTheBadgeComInitializationWatcherMsg0, ex.Message);
                        }

                        // Clear other references.
                        lock (_currentBadgesLocker)
                        {
                            if (_currentBadges != null)
                            {
                                _trace.writeToLog(9, Resources.IconOverlayDisposeClearCurrentBadges);
                                _currentBadges.Clear();
                                _currentBadges = null;
                            }
                        }

                        lock (_currentBadgesSyncedLocker)
                        {
                            if (_currentBadgesSynced != null)
                            {
                                _trace.writeToLog(9, Resources.IconOverlayDisposeClearCurrentSyncedBadges);
                                _currentBadgesSynced.Clear();
                                _currentBadgesSynced = null;
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
        private FilePathDictionary<object> _currentBadgesSynced;
        private readonly object _currentBadgesSyncedLocker = new object();
        private static readonly object _syncedDictionaryValue = new object();

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
        /// BadgeComInitWatcher threads subscribe and monitor initialization events from BadgeCom (Explorer shell extension).
        /// </summary>
        private BadgeComPubSubEvents _badgeComPubSubEvents = null;

        #endregion

        /// <summary>
        /// Determine whether this icon should be badged by this badge handler.
        /// </summary>
        /// <param name="badgeType">The type of the badge.</param>
        /// <param name="filePath">The path to badge.</param>
        /// <returns></returns>
        private bool ShouldIconBeBadged(
                                    cloudAppIconBadgeType badgeType,
                                    string filePath)
        {
            try
            {
                _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayShouldIconBeBadgedEntrybadgeType0FilePath1, badgeType.ToString(), filePath));
                // Convert the badgetype and filepath to objects.
                FilePath objFilePath = filePath;
                GenericHolder<cloudAppIconBadgeType> objBadgeType = new GenericHolder<cloudAppIconBadgeType>(badgeType);

                // Lock and query the in-memory database.
                lock (_currentBadgesLocker)
                {
                    // There will be no badge if the path doesn't contain Cloud root
                    _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayShouldIconBeBadgedLocked));
                    if (objFilePath.Contains(_filePathCloudDirectory))
                    {
                        // If the value at this path is set and it is our type, then badge.
                        _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayShouldIconBeBadgedContainsCloudRoot));
                        GenericHolder<cloudAppIconBadgeType> tempBadgeType;
                        bool success = allBadges.TryGetValue(objFilePath, out tempBadgeType);
                        if (success)
                        {
                            bool rc = (tempBadgeType.Value == objBadgeType.Value);
                            _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayShouldIconBeBadgedGotValueFromAllBadgesReturn0, rc));
                            return rc;
                        }
                        else
                        {
                            // This specific node wasn't found, so it is assumed to be synced, but we won't actually badge as synced.
                            // Instead, we will search the children and determine a badge state from the children.
                            // 
                            // If an item is marked selective, then none of its children (whole hierarchy of children) should be badged.
                            _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayShouldIconBeBadgedTryGetValueNotSuccessful));
                            if (!FilePathComparer.Instance.Equals(objFilePath, _filePathCloudDirectory))
                            {
                                // Recurse through parents of this node up to and including the CloudPath.
                                _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayShouldIconBeBadgedRecurseThruParents));
                                FilePath node = objFilePath;
                                while (node != null)
                                {
                                    // Return false if the value of this node is not null, and is marked SyncSelective
                                    _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayShouldIconBeBadgedGetTheTypeForPath0, node.ToString()));
                                    success = allBadges.TryGetValue(node, out tempBadgeType);
                                    if (success && tempBadgeType != null)
                                    {
                                        // Got the badge type at this level.
                                        _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayShouldIconBeBadgedGotType0, tempBadgeType.Value.ToString()));
                                        if (tempBadgeType.Value == cloudAppIconBadgeType.cloudAppBadgeSyncSelective)
                                        {
                                            _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayShouldIconBeBadgedReturnFalse));
                                            return false;
                                        }
                                    }

                                    // Quit if we are at the Cloud directory root
                                    _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayShouldIconBeBadgedHaveWeReachedCloudRoot));
                                    if (FilePathComparer.Instance.Equals(node, _filePathCloudDirectory))
                                    {
                                        _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayShouldIconBeBadgedBreakToDeterminBadgeStatusFromChildrenOfNode));
                                        break;
                                    }

                                    // Chain up
                                    _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayShouldIconBeBadgedChainUp));
                                    node = node.Parent;
                                }
                            }

                            // Determine the badge type from the hierarchy at this path
                            bool toReturn = DetermineBadgeStatusFromHierarchyOfChildrenOfThisNode(badgeType, allBadges, objFilePath);
                            _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayShouldIconBeBadgedReturn20, toReturn));
                            return toReturn;
                        }
                    }
                    else
                    {
                        // This path is not in the Cloud folder.  Don't badge.
                        _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayShouldIconBeBadgedNotInTheCloudFolderReturnDontBadge));
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.Log(_syncSettings.TraceLocation, _syncSettings.LogErrors);
                _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayShouldIconBeBadgedExceptionNormalMsg0Code1, error.PrimaryException.Message, error.PrimaryException.Code));
                _trace.writeToLog(1, Resources.IconOverlayShouldIconBeBadgedExceptionNormalMsg0Code1, error.PrimaryException.Message, error.PrimaryException.Code);
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
                _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayDetermineBadgeStatusFromHierarchyOfChildrenOfThisNodeGetTheHierarchyForPath0, objFilePath.ToString()));
                FilePathHierarchicalNode<GenericHolder<cloudAppIconBadgeType>> tree;
                CLError error = AllBadges.GrabHierarchyForPath(objFilePath, out tree, suppressException: true);
                if (error == null
                    && tree != null)
                {
                    _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayShouldIconBeBadgedSuccessfulGettingTheHeirarchyCallGetDesiredBadgeTypeViaRecursivePostorderTraversal));
                    // Chase the children hierarchy using recursive postorder traversal to determine the desired badge type.
                    cloudAppIconBadgeType desiredBadgeType = GetDesiredBadgeTypeViaRecursivePostorderTraversal(tree);
                    bool rc = (badgeType == desiredBadgeType);
                    _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayShouldIconBeBadgedReturn20, rc));
                    return rc;
                }
                else
                {
                    bool rc = (badgeType == cloudAppIconBadgeType.cloudAppBadgeSynced);
                    _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayShouldIconBeBadgedReturn30, rc));
                    return rc;
                }
            }
            catch
            {
                bool rc = (badgeType == cloudAppIconBadgeType.cloudAppBadgeSynced);
                _trace.writeToLog(1, Resources.IconOverlayShouldIconBeBadgedERRORExceptionReturn40, rc);
                _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayShouldIconBeBadgedERRORExceptionReturn40, rc));
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
            _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayGetDesiredBadgeTypeViaRecursivePostorderTraversalEntryNodePath0NodeBadgeType1, 
                        node != null ? (node.HasValue ? node.Value.Key.ToString() : Resources.IconOverlayNoNodeValue) : Resources.IconOverlayNodeNull,
                        node != null ? (node.HasValue ? node.Value.Value.Value.ToString() : Resources.IconOverlayNoNodeValue2) : Resources.IconOverlayNodeNull2
                        ));
            if (node == null)
            {
                _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayGetDesiredBadgeTypeViaRecursivePostorderTraversalNodeIsNullReturnSynced));
                return cloudAppIconBadgeType.cloudAppBadgeSynced;
            }

            // Loop through all of the node's children
            foreach (FilePathHierarchicalNode<GenericHolder<cloudAppIconBadgeType>> child in node.Children)
            {
                cloudAppIconBadgeType returnBadgeType = GetDesiredBadgeTypeViaRecursivePostorderTraversal(child);
                _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayGetDesiredBadgeTypeViaRecursivePostorderTraversalGotReturnBadgeType0, returnBadgeType.ToString()));
                if (returnBadgeType != cloudAppIconBadgeType.cloudAppBadgeSynced)
                {
                    _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayGetDesiredBadgeTypeViaRecursivePostorderTraversalreturnThatBadgeType));
                    return returnBadgeType;
                }
            }

            // Process by whether the node has a value.  If not, it is synced.
            if (node.HasValue)
            {
                _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayGetDesiredBadgeTypeViaRecursivePostorderTraversalHasValue));
                switch (node.Value.Value.Value)
                {
                    case cloudAppIconBadgeType.cloudAppBadgeSynced:
                        _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayGetDesiredBadgeTypeViaRecursivePostorderTraversalReturnSynced));
                        return cloudAppIconBadgeType.cloudAppBadgeSynced;
                    case cloudAppIconBadgeType.cloudAppBadgeSyncing:
                        _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayGetDesiredBadgeTypeViaRecursivePostorderTraversalReturnSyncing));
                        return cloudAppIconBadgeType.cloudAppBadgeSyncing;
                    case cloudAppIconBadgeType.cloudAppBadgeFailed:             // the current node had no explicit value, and this child is failed, so the parent is syncing.
                        _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayGetDesiredBadgeTypeViaRecursivePostorderTraversalReturnSyncing2));
                        return cloudAppIconBadgeType.cloudAppBadgeSyncing;
                    case cloudAppIconBadgeType.cloudAppBadgeSyncSelective:      // the current node had no explicit value, and this child is selective, return synced (which is actually the default value, and will cause the recursion to continue looking).
                        _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayGetDesiredBadgeTypeViaRecursivePostorderTraversalReturnSynced2));
                        return cloudAppIconBadgeType.cloudAppBadgeSynced;
                }
            }
            else
            {
                _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayGetDesiredBadgeTypeViaRecursivePostorderTraversalReturnSynced3));
                return cloudAppIconBadgeType.cloudAppBadgeSynced;
            }

            _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayGetDesiredBadgeTypeViaRecursivePostorderTraversalReturnSynced4));
            return cloudAppIconBadgeType.cloudAppBadgeSynced;
        }

        /// <summary>
        /// Update the badging state to match the hierarchical badge dictionary, working from the parent of a node up to the root.
        /// </summary>
        /// <remarks>Assumes the lock is already held by the caller on _currentBadges.</remarks>
        private void UpdateBadgeStateUpTreeStartingWithParentOfNode(FilePath nodePath, FilePath rootPath)
        {
            _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayUpdateBadgeStateUpTreeStartingWithParentOfNodeEntryNodePath0, nodePath.ToString()));
            if (rootPath == null)
            {
                throw new NullReferenceException(Resources.IconOverlayRootPathCannotBeNull);
            }

            // Loop up the tree starting with the parent of the parm node.
            FilePath node = nodePath;
            while (node != null
                && node.Parent != null
                && !FilePathComparer.Instance.Equals(node, rootPath))
            {
                // Update the badging state at this node.  This will send events to BadgeCom, and will keep the current badge flat dictionary up to date.
                _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayUpdateBadgeStateUpTreeStartingWithParentOfNodeCallUpdateBadgeStateAtPath));
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
            _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayUpdateBadgeStateAtPathEntryNodePath0, nodePath.ToString()));
            GenericHolder<cloudAppIconBadgeType> hierarchicalBadgeType;
            getBadgeTypeForFileAtPath(nodePath, out hierarchicalBadgeType);        // always returns a badgeType, even if an error occurs.
            _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayUpdateBadgeStateAtPathGetBadgeTypeForFileAtPathReturned0, hierarchicalBadgeType != null ? hierarchicalBadgeType.Value.ToString() : Resources.IconOverlayHeirarchicalBadgeTypeNull));

            // Get the current badge type for this nodePath from the flat dictionary.
            GenericHolder<cloudAppIconBadgeType> flatBadgeType;
            _currentBadges.TryGetValue(nodePath, out flatBadgeType);
            _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayUpdateBadgeStateAtPathCurrentBadgesReturned0, flatBadgeType != null ? flatBadgeType.Value.ToString() : Resources.IconOverlayFlatBadgeTypeNull));

            // Only process if they are different
            if ((flatBadgeType == null && hierarchicalBadgeType != null)
                || (flatBadgeType != null && !flatBadgeType.Equals(hierarchicalBadgeType)))
            {
                _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayUpdateBadgeStateAtPathDifferent));
                if (flatBadgeType != null)
                {
                    _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayUpdateBadgeStateAtPathHaveFlatBadgeType));
                    if (hierarchicalBadgeType != null)
                    {
                        // They are different and both specified.  Remove and add.
                        _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayUpdateBadgeStateAtPathHaveHeirarchicalBadgeType));
                        SendRemoveBadgePathEvent(nodePath);
                        SendAddBadgePathEvent(nodePath, hierarchicalBadgeType);
                        _currentBadges[nodePath] = hierarchicalBadgeType;
                        lock (_currentBadgesSyncedLocker)
                        {
                            _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayUpdateBadgeStateAtPathInCurrentBadgeSyncedLocker));
                            if (_currentBadgesSynced != null)
                            {
                                if (hierarchicalBadgeType.Value == cloudAppIconBadgeType.cloudAppBadgeSynced)
                                {
                                    _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayUpdateBadgeStateAtPathSetCurrentBadgesSyncedForThisPath));
                                    _currentBadgesSynced[nodePath] = _syncedDictionaryValue;
                                }
                                else
                                {
                                    _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayUpdateBadgeStateAtPathResetCurrentBadgesSyncedForThisPath));
                                    _currentBadgesSynced[nodePath] = null;
                                }
                            }
                        }
                    }
                    else
                    {
                        // They are different and there is no hierarchical badge.  Remove.
                        _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayUpdateBadgeStateAtPathDifferentAndNoHeirarchicalBadgeRemove));
                        SendRemoveBadgePathEvent(nodePath);
                        _currentBadges.Remove(nodePath);
                        lock (_currentBadgesSyncedLocker)
                        {
                            if (_currentBadgesSynced != null)
                            {
                                _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayUpdateBadgeStateAtPathRemoveSyncedBadgeAtPathThisPath));
                                _currentBadgesSynced[nodePath] = null;
                            }
                        }
                    }
                }
                else
                {
                    // They are different and there is no flat badge.  Add.
                    _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayUpdateBadgeStateAtPathDifferentAndNoFlatAdd));
                    SendAddBadgePathEvent(nodePath, hierarchicalBadgeType);
                    _currentBadges.Add(nodePath, hierarchicalBadgeType);
                    if (hierarchicalBadgeType.Value == cloudAppIconBadgeType.cloudAppBadgeSynced)
                    {
                        lock (_currentBadgesSyncedLocker)
                        {
                            if (_currentBadgesSynced != null)
                            {
                                _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayUpdateBadgeStateAtPathAddSyncedBadgeAtThisPath));
                                _currentBadgesSynced[nodePath] = _syncedDictionaryValue;
                            }
                        }
                    }
                }
            }
            _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlayUpdateBadgeStateAtPathReturn));
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
                    _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlaySendAddBadgePathEventEntryPath0, nodePath.ToString()));
                    _badgeComPubSubEvents.PublishEventToBadgeCom(EnumEventType.BadgeNet_To_BadgeCom, EnumEventSubType.BadgeNet_AddBadgePath, (EnumCloudAppIconBadgeType)badgeType.Value, nodePath.ToString(), _guidPublisher);
                }
            }
            catch (global::System.Exception ex)
            {
                CLError error = ex;
                error.Log(_syncSettings.TraceLocation, _syncSettings.LogErrors);
                _trace.writeToLog(1, Resources.IconOverlaySendAddBadgePathEventExceptionMsg0, ex.Message);
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
                    _trace.writeToMemory(() => _trace.trcFmtStr(1, Resources.IconOverlaySendRemoveBadgePathEventEntryPath0, nodePath.ToString()));
                    _badgeComPubSubEvents.PublishEventToBadgeCom(EnumEventType.BadgeNet_To_BadgeCom, EnumEventSubType.BadgeNet_RemoveBadgePath, 0 /* not used */, nodePath.ToString(), _guidPublisher);
                }
            }
            catch (global::System.Exception ex)
            {
                CLError error = ex;
                error.Log(_syncSettings.TraceLocation, _syncSettings.LogErrors);
                _trace.writeToLog(1, Resources.IconOverlaySendRemoveBadgePathEventExceptionMsg0, ex.Message);
            }
        }
        #endregion

        #region badge queue
        public void QueueNewEventBadge(FileChange mergedEvent, FileChange eventToRemove)
        {
            QueueBadgeParams(new badgeParams.newEvent(mergedEvent, eventToRemove, this));
        }
        public void QueueSetBadge(cloudAppIconBadgeType badgeType, FilePath badgePath)
        {
            QueueBadgeParams(new badgeParams.genericSetter(badgeType, badgePath, this));
        }
        public void QueueDeleteBadge<T>(FilePath toDelete, Action<T> onIsDeleted, T onIsDeletedState, EventWaitHandle completionHandle)
        {
            QueueBadgeParams(new badgeParams.deletePath<T>(toDelete, onIsDeleted, onIsDeletedState, completionHandle, this));
        }
        public void QueueRenameBadge(FilePath oldPath, FilePath newPath)
        {
            QueueBadgeParams(new badgeParams.renamePath(oldPath, newPath, this));
        }
        private static readonly Queue<badgeParams.baseParams> setBadgeQueue = new Queue<badgeParams.baseParams>();
        private static bool badgesQueueing = false;
        private static void QueueBadgeParams(badgeParams.baseParams toQueue)
        {
            lock (toQueue.thisOverlay)
            {
                if (toQueue.thisOverlay.Disposed)
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
                lock (setBadgeQueue)
                {
                    if (setBadgeQueue.Count == 0)
                    {
                        badgesQueueing = false;
                        return null;
                    }

                    badgeParams.baseParams toReturn = setBadgeQueue.Dequeue();

                    lock (toReturn.thisOverlay)
                    {
                        if (toReturn.thisOverlay.Disposed)
                        {
                            return null;
                        }
                    }

                    return toReturn;
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
                internal IconOverlay thisOverlay
                {
                    get
                    {
                        return _thisOverlay;
                    }
                }
                private readonly IconOverlay _thisOverlay;

                protected baseParams(IconOverlay thisOverlay)
                {
                    this._thisOverlay = thisOverlay;
                }

                public abstract void Process();
            }

            public sealed class genericSetter : baseParams
            {
                private readonly cloudAppIconBadgeType badgeType;
                private readonly FilePath badgePath;

                public genericSetter(cloudAppIconBadgeType badgeType, FilePath badgePath, IconOverlay thisOverlay)
                    : base(thisOverlay)
                {
                    thisOverlay._trace.writeToMemory(() => thisOverlay._trace.trcFmtStr(1, Resources.IconOverlayGenericSetterbadgeType0Path1, badgeType.ToString(), badgePath));
                    thisOverlay._trace.writeToMemory(() => thisOverlay._trace.trcFmtStr(1, Resources.IconOverlayGenericSetterStackTrace0, Environment.StackTrace));
                    this.badgeType = badgeType;
                    this.badgePath = badgePath;
                }

                public override void Process()
                {
                    // Trace the badging dictionaries
                    thisOverlay.TraceBadgingDictionaries(Resources.IconOverlyGenericSetterProcessBeforeBadgeType + badgeType.ToString() + Resources.IconOverlayPath + badgePath.ToString() + Resources.GreaterThanPeriod);

                    thisOverlay.setBadgeType(new GenericHolder<cloudAppIconBadgeType>(badgeType), badgePath);

                    // Trace the badging dictionaries
                    thisOverlay.TraceBadgingDictionaries(Resources.IconOverlayGenricSetterProcessAfterBadgeType + badgeType.ToString() + Resources.IconOverlayPath + badgePath.ToString() + Resources.GreaterThanPeriod);
                }
            }

            public sealed class newEvent : baseParams
            {
                private readonly FileChange mergedEvent;
                private readonly FileChange eventToRemove;

                public newEvent(FileChange mergedEvent, FileChange eventToRemove, IconOverlay thisOverlay)
                    : base(thisOverlay)
                {
                    if (mergedEvent != null)
                    {
                        thisOverlay._trace.writeToMemory(() => thisOverlay._trace.trcFmtStr(1, Resources.IconOverlayNewEventMergedEventType0Old1New2, mergedEvent.Type.ToString(), mergedEvent.OldPath, mergedEvent.NewPath));
                    }
                    if (eventToRemove != null)
                    {
                        thisOverlay._trace.writeToMemory(() => thisOverlay._trace.trcFmtStr(1, Resources.IconOverlayNewEventEventToRemoveType0Old1New2, eventToRemove.Type.ToString(), eventToRemove.OldPath, eventToRemove.NewPath));
                    }
                    thisOverlay._trace.writeToMemory(() => thisOverlay._trace.trcFmtStr(1, Resources.IconOverlayNewEventStackTrace0, Environment.StackTrace));
                    this.mergedEvent = mergedEvent;
                    this.eventToRemove = eventToRemove;
                }

                public override void Process()
                {
                    Action<cloudAppIconBadgeType, FilePath> setBadge = (badgeType, badgePath) =>
                    {
                        // Trace the badging dictionaries
                        thisOverlay.TraceBadgingDictionaries(Resources.IconOverlayNewEventProcessBeforeBadgeType + badgeType.ToString() + Resources.IconOverlayPath + badgePath.ToString() + Resources.GreaterThanPeriod);

                        thisOverlay.setBadgeType(new GenericHolder<cloudAppIconBadgeType>(badgeType), badgePath);

                        // Trace the badging dictionaries
                        thisOverlay.TraceBadgingDictionaries(Resources.IconOverlayNewEventProcessAfterBadgeType + badgeType.ToString() + Resources.IconOverlayPath + badgePath.ToString() + Resources.GreaterThanPeriod);
                    };

                    // Update the badges for this merged event.
                    //TODO: Do we need to do anything with the eventToRemove?
                    if (mergedEvent != null)
                    {
                        thisOverlay._trace.writeToMemory(() => thisOverlay._trace.trcFmtStr(1, Resources.IconOverlayProcessMergedEventType0MergedEventDirection1mergedEventOldPath2MergedEventNewPath3, mergedEvent.Type, mergedEvent.Direction, mergedEvent.OldPath, mergedEvent.NewPath));
                        switch (mergedEvent.Type)
                        {
                            case FileChangeType.Deleted:
                                switch (mergedEvent.Direction)
                                {
                                    case SyncDirection.From:
                                        thisOverlay._trace.writeToMemory(() => thisOverlay._trace.trcFmtStr(1, Resources.IconOverlayProcessDeletedDirFromSetNewPathToSyncing));
                                        setBadge(cloudAppIconBadgeType.cloudAppBadgeSyncing, mergedEvent.NewPath);
                                        break;
                                    case SyncDirection.To:
                                        bool isDeleted;
                                        thisOverlay.DeleteBadgePath(mergedEvent.NewPath, out isDeleted, true);
                                        thisOverlay._trace.writeToMemory(() => thisOverlay._trace.trcFmtStr(1, Resources.IconOverlayProcessDeletedDirToDeleteBadgePathAtNewPathIsDeleted0, isDeleted));
                                        break;
                                    default:
                                        throw new NotSupportedException(Resources.IconOverlayUnknownMergedEventDirection + mergedEvent.Direction.ToString());
                                }
                                break;
                            case FileChangeType.Created:
                            case FileChangeType.Modified:
                                thisOverlay._trace.writeToMemory(() => thisOverlay._trace.trcFmtStr(1, Resources.IconOverlayProcessCreatedModifiedSetBadgeSyncingAtNewPath));
                                setBadge(cloudAppIconBadgeType.cloudAppBadgeSyncing, mergedEvent.NewPath);
                                break;
                            case FileChangeType.Renamed:
                                switch (mergedEvent.Direction)
                                {
                                    case SyncDirection.From:
                                        thisOverlay._trace.writeToMemory(() => thisOverlay._trace.trcFmtStr(1, Resources.IconOverlayProcessRenamedDirFromSetBadgeSyncingAtOldPath));
                                        setBadge(cloudAppIconBadgeType.cloudAppBadgeSyncing, mergedEvent.OldPath);
                                        break;
                                    case SyncDirection.To:

                                        thisOverlay._trace.writeToMemory(() => thisOverlay._trace.trcFmtStr(1, Resources.IconOverlayProcessRenamedDirTosetBadgeSyncingAtOldPath));
                                        setBadge(cloudAppIconBadgeType.cloudAppBadgeSyncing, mergedEvent.OldPath);
                                        
                                        thisOverlay._trace.writeToMemory(() => thisOverlay._trace.trcFmtStr(1, Resources.IconOverlayProcessRenamedDirToCallRenameBadgePath));
                                        thisOverlay.RenameBadgePath(mergedEvent.OldPath, mergedEvent.NewPath);
                                        break;
                                    default:
                                        throw new NotSupportedException(Resources.IconOverlayUnknownMergedEventDirection + mergedEvent.Direction.ToString());
                                }
                                break;
                            default:
                                throw new NotSupportedException(Resources.IconOverlayMergedEventType + mergedEvent.Type.ToString());
                        }

                        if (eventToRemove != null)
                        {
                            throw new NotImplementedException(Resources.HaveNotHandledBadgingWhenTwoEventsMergedTogether);
                        }
                    }
                    else if (eventToRemove != null)
                    {
                        setBadge(cloudAppIconBadgeType.cloudAppBadgeSynced, eventToRemove.NewPath);
                    }
                }
            }

            public sealed class deletePath<T> : baseParams
            {
                private readonly FilePath toDelete;
                private readonly Action<T> onIsDeleted;
                private readonly T onIsDeletedState;
                private readonly EventWaitHandle completionHandle;

                public deletePath(FilePath toDelete, Action<T> onIsDeleted, T onIsDeletedState, EventWaitHandle completionHandle, IconOverlay thisOverlay)
                    : base(thisOverlay)
                {
                    this.toDelete = toDelete;
                    this.onIsDeleted = onIsDeleted;
                    this.onIsDeletedState = onIsDeletedState;
                    this.completionHandle = completionHandle;
                }

                public override void Process()
                {
                    bool isDeleted;
                    CLError deleteError = thisOverlay.DeleteBadgePath(toDelete, out isDeleted);

                    if (deleteError != null)
                    {
                        thisOverlay._trace.writeToLog(1,
                            Resources.IconOverlayMessageEventsBadgePathERRORMsg0Code1,
                            deleteError.PrimaryException.Message,
                            deleteError.PrimaryException.Code);
                    }
                    else if (isDeleted && onIsDeleted != null)
                    {
                        try
                        {
                            onIsDeleted(onIsDeletedState);
                        }
                        catch
                        {
                        }
                    }

                    if (completionHandle != null)
                    {
                        completionHandle.Set();
                    }
                }
            }

            public sealed class renamePath : baseParams
            {
                private readonly FilePath oldPath;
                private readonly FilePath newPath;

                public renamePath(FilePath oldPath, FilePath newPath, IconOverlay thisOverlay)
                    : base(thisOverlay)
                {
                    this.oldPath = oldPath;
                    this.newPath = newPath;
                }

                public override void Process()
                {
                    CLError renameError = thisOverlay.RenameBadgePath(oldPath, newPath);
                    if (renameError != null)
                    {
                        thisOverlay._trace.writeToLog(1,
                            Resources.IconOverlayMessageEventsBadgePathRenamedERRORThrowMas0Code1FromPath2ToPath3,
                            renameError.PrimaryException.Message,
                            renameError.PrimaryException.Code,
                            oldPath,
                            newPath);
                    }
                }
            }
        }
        #endregion
    }
    // \endcond
}
