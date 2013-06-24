//
// CLSyncbox.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Interfaces;
using Cloud.Model;
using Cloud.Model.EventMessages.ErrorInfo;
using Cloud.PushNotification;
using Cloud.REST;
using Cloud.Static;
using Cloud.Support;
using Cloud.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using Cloud.Parameters;
using Cloud.Callbacks;
using Newtonsoft.Json.Linq;

namespace Cloud
{
    #region Public CLSyncbox Class
    /// <summary>
    /// Represents a Syncbox in Cloud where everything is stored
    /// </summary>
    public sealed class CLSyncbox : IDisposable
    {
        #region hidden debug properties

        // following flag should always be false except for when debugging dependencies
        private readonly GenericHolder<bool> debugDependencies = new GenericHolder<bool>(false);

        #region hidden Dependencies debug
        //// --------- adding \cond and \endcond makes the section in between hidden from doxygen

        // \cond
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool DependenciesDebug
        {
            get
            {
                lock (debugDependencies)
                {
                    return debugDependencies.Value;
                }
            }
            set
            {
                lock (debugDependencies)
                {
                    debugDependencies.Value = value;
                }
            }
        }
        // \endcond
        #endregion

        // following flag should always be false except for when debugging database by copying on every change
        private readonly GenericHolder<bool> copyDatabaseBetweenChanges = new GenericHolder<bool>(false);

        #region hidden copy database debug
        //// --------- adding \cond and \endcond makes the section in between hidden from doxygen

        // \cond
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool DebugCopyDatabase
        {
            get
            {
                lock (copyDatabaseBetweenChanges)
                {
                    return copyDatabaseBetweenChanges.Value;
                }
            }
            set
            {
                lock (copyDatabaseBetweenChanges)
                {
                    copyDatabaseBetweenChanges.Value = value;
                }
            }
        }
        // \endcond
        #endregion

        // following flag should always be false except for when debugging FileMonitor memory
        private readonly GenericHolder<bool> debugFileMonitorMemory = new GenericHolder<bool>(false);

        #region hidden FileMonitor debug
        //// --------- adding \cond and \endcond makes the section in between hidden from doxygen

        // \cond
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool FileMonitorMemoryDebug
        {
            get
            {
                lock (debugFileMonitorMemory)
                {
                    return debugFileMonitorMemory.Value;
                }
            }
            set
            {
                lock (debugFileMonitorMemory)
                {
                    if (debugFileMonitorMemory.Value
                        && !value)
                    {
                        FileMonitor.MonitorAgent.memoryDebugger.Instance.wipeMemory();
                    }

                    debugFileMonitorMemory.Value = value;
                }
            }
        }
        // \endcond

        // \cond
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public string FileMonitorMemory
        {
            get
            {
                lock (debugFileMonitorMemory)
                {
                    if (!debugFileMonitorMemory.Value)
                    {
                        return null;
                    }
                }

                return FileMonitor.MonitorAgent.memoryDebugger.Instance.serializeMemory();
            }
        }
        // \endcond

        // \cond
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool WipeFileMonitorDebugMemory
        {
            set
            {
                if (value)
                {
                    bool needsWipe;

                    lock (debugFileMonitorMemory)
                    {
                        needsWipe = debugFileMonitorMemory.Value;
                    }

                    if (needsWipe)
                    {
                        FileMonitor.MonitorAgent.memoryDebugger.Instance.wipeMemory();
                    }
                }
            }
        }
        // \endcond
        #endregion

        #endregion

        #region Private Fields

        private static readonly CLTrace _trace = CLTrace.Instance;
        private static readonly List<CLSyncEngine> _startedSyncEngines = new List<CLSyncEngine>();
        private static readonly object _startLocker = new object();

        private bool _isStarted = false;
        private bool Disposed = false;   // This stores if this current instance has been disposed (defaults to not disposed)
        private readonly ReaderWriterLockSlim _propertyChangeLocker = new ReaderWriterLockSlim();  // for locking any reads and writes to the changeable properties.
        private readonly Helpers.ReplaceExpiredCredentialsCallback _getNewCredentialsCallback = null;
        private readonly object _getNewCredentialsCallbackUserState = null;
        private readonly IEventMessageReceiver _liveSyncStatusReceiver = null;

        #endregion  // end Private Fields

        #region Internal properties
	// \cond
        /// <summary>
        /// Internal client for passing HTTP REST calls to the server
        /// </summary>
        internal CLHttpRest HttpRestClient
        {
            get
            {
                if (setPathLocker != null)
                {
                    Monitor.Enter(setPathLocker);
                }
                try
                {
                    if (setPathHolder == null)
                    {
                        return null;
                    }
                    return setPathHolder.HttpRestClient;
                }
                finally
                {
                    if (setPathLocker != null)
                    {
                        Monitor.Exit(setPathLocker);
                    }
                }
            }
        }
        // \endcond
        #endregion

        #region Public Properties

        /// <summary>
        /// Contains authentication information required for all communication and services
        /// </summary>
        public CLCredentials Credentials
        {
            get
            {
                lock (_credentialsHolder)
                {
                    return _credentialsHolder.Value;
                }
            }
            set
            {
                lock (_credentialsHolder)
                {
                    _credentialsHolder.Value = value;
                }
            }
        }
        private readonly GenericHolder<CLCredentials> _credentialsHolder = new GenericHolder<CLCredentials>();

        /// <summary>
        /// The unique ID of this Syncbox assigned by Cloud
        /// </summary>
        public long SyncboxId
        {
            get
            {
                return _syncboxId;
            }
        }
        private readonly long _syncboxId;

        /// <summary>
        /// The friendly name of this Syncbox in the syncbox.
        /// </summary>
        public string FriendlyName
        {
            get
            {
                _propertyChangeLocker.EnterReadLock();
                try
                {
                    return _friendlyName;
                }
                finally
                {
                    _propertyChangeLocker.ExitReadLock();
                }
            }
        }
        private string _friendlyName;

        #region properties set on SetPath

        private readonly object setPathLocker;

        /// <summary>
        /// The full path on the disk associated with this syncbox.  Used only for live sync.
        /// </summary>
        public string Path
        {
            get
            {
                if (setPathLocker != null)
                {
                    Monitor.Enter(setPathLocker);
                }
                try
                {
                    if (setPathHolder == null)
                    {
                        return null;
                    }
                    return setPathHolder.Path;
                }
                finally
                {
                    if (setPathLocker != null)
                    {
                        Monitor.Exit(setPathLocker);
                    }
                }
            }
        }

        /// <summary>
        /// lock on _startLocker for modifications or retrieval
        /// </summary>
        private CLSyncEngine _syncEngine = null;

        // this effectively sets path as null and sets the rest client as null
        private SetPathProperties setPathHolder = null;

        private sealed class SetPathProperties
        {
            public string Path
            {
                get
                {
                    return _path;
                }
            }
            private readonly string _path;

            public CLHttpRest HttpRestClient
            {
                get
                {
                    return _httpRestClient;
                }
            }
            private readonly CLHttpRest _httpRestClient;

            public SetPathProperties(string path, CLHttpRest httpRestClient)
            {
                this._path = path;
                this._httpRestClient = httpRestClient;
            }
        }

        #endregion  // end properties set on SetPath

        /// <summary>
        /// The ID of the storage plan to use for this syncbox.
        /// </summary>
        public long StoragePlanId
        {
            get
            {
                _propertyChangeLocker.EnterReadLock();
                try
                {
                    return _storagePlanId;
                }
                finally
                {
                    _propertyChangeLocker.ExitReadLock();
                }
            }
        }
        private long _storagePlanId;

        /// <summary>
        /// The UTC time that this syncbox was created.
        /// </summary>
        public DateTime CreatedDate
        {
            get
            {
                _propertyChangeLocker.EnterReadLock();
                try
                {
                    return _createdDate;
                }
                finally
                {
                    _propertyChangeLocker.ExitReadLock();
                }
            }
        }
        private DateTime _createdDate;

        /// <summary>
        /// The number of bytes currently used within this syncbox's storage quota.
        /// </summary>
        public Nullable<long> QuotaUsage
        {
            get
            {
                _propertyChangeLocker.EnterReadLock();
                try
                {
                    return _quotaUsage;
                }
                finally
                {
                    _propertyChangeLocker.ExitReadLock();
                }
            }
        }
        private Nullable<long> _quotaUsage;

        /// <summary>
        /// The maximum storage bytes supported by the storage plan associated with this syncbox.
        /// </summary>
        public Nullable<long> StorageQuota
        {
            get
            {
                _propertyChangeLocker.EnterReadLock();
                try
                {
                    return _storageQuota;
                }
                finally
                {
                    _propertyChangeLocker.ExitReadLock();
                }
            }
        }
        private Nullable<long> _storageQuota;

        /// <summary>
        /// The sync mode used with this syncbox.
        /// </summary>
        public CLSyncMode SyncMode
        {
            get
            {
                return _syncMode;
            }
        }
        private CLSyncMode _syncMode;

        /// <summary>
        /// Settings copied upon creation of this Syncbox
        /// </summary>
        public ICLSyncSettingsAdvanced CopiedSettings
        {
            get
            {
                return _copiedSettings;
            }
        }
        private readonly ICLSyncSettingsAdvanced _copiedSettings;

        #endregion  // end Public Properties

        #region Public Events
        
        /// <summary>
        /// Event fired when a serious notification error has occurred.  Push notification is
        /// no longer functional.
        /// </summary>
        public event EventHandler<NotificationErrorEventArgs> PushNotificationError;

       #endregion  // end Public Events

        #region Private Constructors

        /// <summary>
        /// Private constructor to create a syncbox object.  Called from public member AllocAndInit.
        /// </summary>
        /// <param name="syncboxId">The syncbox ID.</param>
        /// <param name="credentials">The credentials to use to create this syncbox.</param>
        /// <param name="path">(optional) The full path on disk of the folder to associate with this syncbox. The path is used only for live sync only.</param>
        /// <param name="settings">(optional) The settings to use.</param>
        /// <param name="liveSyncStatusReceiver">(optional) The object to receive live sync status event.</param>
        /// <param name="getNewCredentialsCallback">(optional) The delegate to call for getting new temporary credentials.</param>
        /// <param name="getNewCredentialsCallbackUserState">(optional) The user state to pass to the delegate above.</param>
        private CLSyncbox(
            long syncboxId,
            CLCredentials credentials,
            string path = null,
            ICLSyncSettings settings = null,
            IEventMessageReceiver liveSyncStatusReceiver = null,
            Helpers.ReplaceExpiredCredentialsCallback getNewCredentialsCallback = null,
            object getNewCredentialsCallbackUserState = null)
        {

            // Fix up the path.  Use String.Empty if the user passed null.
            if (path == null)
            {
                path = String.Empty;
            }

            // check input parameters
            if (syncboxId <= 0)
            {
                throw new CLArgumentNullException(CLExceptionCode.Syncbox_SyncboxId, Resources.ExceptionSyncboxSyncboxIdMustBeSpecified);
            }

            if (credentials == null)
            {
                throw new CLArgumentNullException(CLExceptionCode.Syncbox_BadCredentials, Resources.ExceptionSyncboxCredentialsMustNotBeNull);
            }

            // Copy the settings so the user can't change them.
            // copy settings so they don't change while processing; this also defaults some values
            this._copiedSettings = (settings == null
                ? AdvancedSyncSettings.CreateDefaultSettings()
                : settings.CopySettings());
            if (_copiedSettings.DeviceId == null)
            {
                throw new CLArgumentNullException(CLExceptionCode.Syncbox_DeviceId, Resources.CLHttpRestDeviceIDCannotBeNull);
            }

            this._liveSyncStatusReceiver = liveSyncStatusReceiver;

            // Initialize trace in case it is not already initialized.
            CLTrace.Initialize(this._copiedSettings.TraceLocation, Resources.Cloud, Resources.IconOverlayLog, this._copiedSettings.TraceLevel, this._copiedSettings.LogErrors);
            _trace.writeToLog(1, Resources.CLSyncboxConstructing);

            // Set up the syncbox
            lock (_startLocker)
            {

                // Save the parameters in properties.
                this.Credentials = credentials;
                this._syncboxId = syncboxId;
                this._getNewCredentialsCallback = getNewCredentialsCallback;
                this._getNewCredentialsCallbackUserState = getNewCredentialsCallbackUserState;

                //// calling UpdatePathInternal now occurs regardless of whether path was set, therefore no need to allow updating the path later (with a locker)
                //
                //if (String.IsNullOrEmpty(path))
                //{
                //    setPathLocker = new object();
                //}
                //else
                //{
                    setPathLocker = null;
                //}

                // InitializeInternal throws exception as-is, no wrapping as CLError
                InitializeInternal(path, shouldUpdateSyncboxStatusFromServer: true);
            }
        }

        /// <summary>
        /// Private constructor to create a functional CLSyncbox object from a JsonContracts.Syncbox.
        /// </summary>
        /// <param name="syncboxContract">The syncbox contract to use.</param>
        /// <param name="credentials">The credentials to use.</param>
        /// <param name="settings">(optional) The settings to use.</param>
        /// <param name="liveSyncStatusReceiver">(optional) The object to receive live sync status event.</param>
        /// <param name="getNewCredentialsCallback">(optional) The delegate to call for getting new temporary credentials.</param>
        /// <param name="getNewCredentialsCallbackUserState">(optional) The user state to pass to the delegate above.</param>
        private CLSyncbox(
            JsonContracts.Syncbox syncboxContract,
            CLCredentials credentials,
            ICLSyncSettings settings = null,
            IEventMessageReceiver liveSyncStatusReceiver = null,
            Helpers.ReplaceExpiredCredentialsCallback getNewCredentialsCallback = null,
            object getNewCredentialsCallbackUserState = null)
        {
            Helpers.CheckHalted();

            // check input parameters

            if (syncboxContract == null)
            {
                throw new CLArgumentNullException(CLExceptionCode.Syncbox_ArgumentMissing, Resources.ExceptionSyncboxSyncboxContractMustNotBeNull);
            }
            if (syncboxContract.Id == null)
            {
                throw new CLArgumentNullException(CLExceptionCode.Syncbox_ArgumentMissing, Resources.ExceptionSyncboxSyncboxContractIdMustNotBeNull);
            }
            if (syncboxContract.PlanId == null)
            {
                throw new CLArgumentNullException(CLExceptionCode.Syncbox_ArgumentMissing, Resources.ExceptionSyncboxSyncboxContractPlanIdMustNotBeNull);
            }
            if (credentials == null)
            {
                throw new CLArgumentNullException(CLExceptionCode.Syncbox_ArgumentMissing, Resources.ExceptionSyncboxCredentialsMustNotBeNull);
            }

            // Copy the settings so the user can't change them.
            if (settings == null)
            {
                this._copiedSettings = AdvancedSyncSettings.CreateDefaultSettings();
            }
            else
            {
                this._copiedSettings = settings.CopySettings();
            }

            this._liveSyncStatusReceiver = liveSyncStatusReceiver;

            // Initialize trace in case it is not already initialized.
            CLTrace.Initialize(this._copiedSettings.TraceLocation, Resources.Cloud, Resources.IconOverlayLog, this._copiedSettings.TraceLevel, this._copiedSettings.LogErrors);
            _trace.writeToLog(1, Resources.TraceCLSyncboxCLSyncboxConstructingFromContract);

            // Set up the syncbox
            lock (_startLocker)
            {
                // Save the parameters in properties.
                this.Credentials = credentials;
                this._syncboxId = (long)syncboxContract.Id;
                this._storagePlanId = (long)syncboxContract.PlanId;
                this._friendlyName = syncboxContract.FriendlyName;
                this._getNewCredentialsCallback = getNewCredentialsCallback;
                this._getNewCredentialsCallbackUserState = getNewCredentialsCallbackUserState;

                setPathLocker = new object();
            }
        }

        #endregion  // end Private Constructors

        #region Public CLSyncbox Factory

        /// <summary>
        /// Asynchronously begins the factory process to construct an instance of CLSyncbox, initializes it and fill in its properties from the cloud, and optionally associates the cloud syncbox with a folder on the local disk.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when operation completes.  Can be null.</param>
        /// <param name="asyncCallbackUserState">User state to pass to the callback when it is fired.  Can be null.</param>
        /// <param name="syncboxId">The cloud syncbox ID to use.</param>
        /// <param name="credentials">The credentials to use with this request.</param>
        /// <param name="path">(optional) The full path of the folder on disk to associate with this syncbox.  The path is used only for live sync.</param>
        /// <param name="settings">(optional) settings to use with this method.</param>
        /// <param name="liveSyncStatusReceiver">(optional) The object to receive live sync status event.</param>
        /// <param name="getNewCredentialsCallback">(optional) A delegate which will be called to retrieve a new set of credentials when credentials have expired.</param>
        /// <param name="getNewCredentialsCallbackUserState">(optional) The user state to pass as a parameter to the delegate above.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public static IAsyncResult BeginAllocAndInit(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            long syncboxId,
            CLCredentials credentials,
            string path = null,
            ICLSyncSettings settings = null,
            IEventMessageReceiver liveSyncStatusReceiver = null,
            Helpers.ReplaceExpiredCredentialsCallback getNewCredentialsCallback = null,
            object getNewCredentialsCallbackUserState = null)
        {
            Helpers.CheckHalted();

            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxAllocAndInitResult>(
                        asyncCallback,
                        asyncCallbackUserState),
                    syncboxId = syncboxId,
                    credentials = credentials,
                    path = path,
                    settings = settings,
                    liveSyncStatusReceiver = liveSyncStatusReceiver,
                    getNewCredentialsCallback = getNewCredentialsCallback,
                    getNewCredentialsCallbackUserState = getNewCredentialsCallbackUserState
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of result for this operation
                        CLSyncbox response;
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError processError = AllocAndInit(
                            Data.syncboxId,
                            Data.credentials,
                            out response,
                            Data.path,
                            Data.settings,
                            Data.liveSyncStatusReceiver,
                            Data.getNewCredentialsCallback,
                            Data.getNewCredentialsCallbackUserState);

                        Data.toReturn.Complete(
                            new SyncboxAllocAndInitResult(
                                processError, // any error that may have occurred during processing
                                response), // the specific type of result for this operation
                            sCompleted: false); // processing did not complete synchronously
                    }
                    catch (Exception ex)
                    {
                        Data.toReturn.HandleException(
                            ex, // the exception which was not handled correctly by the CLError wrapping
                            sCompleted: false); // processing did not complete synchronously
                    }
                },
                null);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ThreadStart(asyncThread.VoidProcess))).Start(); // start the asynchronous processing thread which is attached to its data

            // return the asynchronous result
            return asyncThread.TypedData.toReturn;
        }

        /// <summary>
        /// Finishes creating and initializing a CLSyncbox instance, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting asynchronous request.</param>
        /// <param name="result">(output) The result from the asynchronous request.</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public static CLError EndAllocAndInit(IAsyncResult asyncResult, out SyncboxAllocAndInitResult result)
        {
            Helpers.CheckHalted();
            return Helpers.EndAsyncOperation<SyncboxAllocAndInitResult>(asyncResult, out result);
        }

        /// <summary>
        /// Creates and initializes a CLSyncbox object which represents a Syncbox in Cloud, initializes it and fill in its properties from the cloud, and optionally associates the syncbox with a folder on the local disk.
        /// </summary>
        /// <param name="syncboxId">Unique ID of the syncbox generated by Cloud</param>
        /// <param name="credentials">Credentials to use with this request.</param>
        /// <param name="syncbox">(output) Created local object representation of the Syncbox</param>
        /// <param name="path">(optional) The full path of the folder on disk to associate with this syncbox.  The path is used only for live sync.</param>
        /// <param name="settings">(optional) Settings to use with this request.</param>
        /// <param name="liveSyncStatusReceiver">(optional) The object to receive live sync status event.</param>
        /// <param name="getNewCredentialsCallback">(optional) A delegate that will be called to provide new credentials when the current credentials token expires.</param>
        /// <param name="getNewCredentialsCallbackUserState">(optional) The user state that will be passed back to the getNewCredentialsCallback delegate.</param>
        /// <returns>Returns any error which occurred during object allocation or initialization, if any, or null.</returns>
        public static CLError AllocAndInit(
            long syncboxId,
            CLCredentials credentials,
            out CLSyncbox syncbox,
            string path = null,
            ICLSyncSettings settings = null,
            IEventMessageReceiver liveSyncStatusReceiver = null,
            Helpers.ReplaceExpiredCredentialsCallback getNewCredentialsCallback = null,
            object getNewCredentialsCallbackUserState = null)
        {
            Helpers.CheckHalted();

            try
            {
                // Fix up the path.  Use String.Empty if the user passed null.
                if (path == null)
                {
                    path = String.Empty;
                }

                if (Helpers.AllHaltedOnUnrecoverableError)
                {
                    throw new CLInvalidOperationException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLCredentialHelpersAllHaltedOnUnrecoverableErrorIsSet);
                }

                syncbox = new CLSyncbox(
                    syncboxId: syncboxId,
                    credentials: credentials,
                    path: path,
                    settings: settings,
                    liveSyncStatusReceiver: liveSyncStatusReceiver,
                    getNewCredentialsCallback: getNewCredentialsCallback,
                    getNewCredentialsCallbackUserState: getNewCredentialsCallbackUserState);
            }
            catch (Exception ex)
            {
                syncbox = Helpers.DefaultForType<CLSyncbox>();
                return ex;
            }

            return null;
        }

        #endregion  // end Public CLSyncbox Factory

        #region Public Instance Life Cycle Methods

        /// <summary>
        /// Start syncing according to the requested sync mode.
        /// </summary>
        /// <param name="mode">The sync mode to start.</param>
        /// <param name="syncStatusChangedCallback">Callback method that will be fired when the status changes in the syncbox.</param>
        /// <param name="syncStatusChangedCallbackUserState">User state to pass to the callback method above.</param>
        /// <returns></returns>
        public CLError StartLiveSync(
                CLSyncMode mode,
                Helpers.SyncStatusChangedCallback syncStatusChangedCallback = null,
                object syncStatusChangedCallbackUserState = null)
        {
            CheckDisposed();

            bool startExceptionLogged = false;

            try
            {
                lock (_startLocker)
                {
                    if (String.IsNullOrEmpty(this.Path))
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestSyncboxBadPath);
                    }
                    else
                    {
                        CLError syncRootError = Helpers.CheckForBadPath(this.Path);
                        if (syncRootError != null)
                        {
                            throw new CLInvalidOperationException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestSyncboxBadPath, syncRootError.Exceptions);
                        }
                    }

                    if (_isStarted)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.Syncbox_AlreadyStarted, Resources.CLSyncEngineAlreadyStarted);
                    }

                    if (this._syncEngine != null)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.Syncbox_AlreadyStarted, Resources.ExceptionCLSyncboxBeginSyncExistingEngine);
                    }

                    bool debugDependenciesValue;
                    lock (debugDependencies)
                    {
                        debugDependenciesValue = debugDependencies.Value;
                    }
                    bool copyDatabaseBetweenChangesValue;
                    lock (copyDatabaseBetweenChanges)
                    {
                        copyDatabaseBetweenChangesValue = copyDatabaseBetweenChanges.Value;
                    }
                    bool debugFileMonitorMemoryValue;
                    lock (debugFileMonitorMemory)
                    {
                        debugFileMonitorMemoryValue = debugFileMonitorMemory.Value;
                    }

                    // Subscribe to the live sync events if the user wants them.
                    if (_liveSyncStatusReceiver != null)
                    {
                        CLError errorFromSubscribe = MessageEvents.SubscribeMessageReceiver(this.SyncboxId, _copiedSettings.DeviceId, _liveSyncStatusReceiver);
                        if (errorFromSubscribe != null)
                        {
                            throw new CLInvalidOperationException(CLExceptionCode.Syncbox_SubscribingToLiveSyncStatusReceiver, Resources.ExceptionSyncboxErrorSubscribingToLiveSyncStatusMessages);
                        }
                    }

                    try
                    {
                        // Create the sync engine for this syncbox instance
                        _syncEngine = new CLSyncEngine(this, debugDependenciesValue, copyDatabaseBetweenChangesValue, debugFileMonitorMemoryValue); // syncbox to sync (contains required settings)

                    Nullable<long> copyStorageQuota;
                    long copyQuotaUsage;

                    _propertyChangeLocker.EnterReadLock();
                    try
                    {
                        copyStorageQuota = _storageQuota;
                        copyQuotaUsage = _quotaUsage ?? 0;
                    }
                    finally
                    {
                        _propertyChangeLocker.ExitReadLock();
                    }

                    if (copyStorageQuota == null)
                    {
                        throw new CLNullReferenceException(CLExceptionCode.Syncbox_StorageQuotaUnknown, Resources.ExceptionCLSyncboxNullStorageQuota);
                    }

                        try
                        {
                            _syncMode = mode;

                            // Start the sync engine
                            CLError syncEngineStartError = _syncEngine.Start(
                                copyQuotaUsage,
                                (long)copyStorageQuota,
                                new SyncEngine.OnGetDataUsageCompletionDelegate(OnGetDataUsageCompletion),
                                statusUpdated: syncStatusChangedCallback, // called when sync status is updated
                                statusUpdatedUserState: syncStatusChangedCallbackUserState); // the user state passed to the callback above

                            if (syncEngineStartError != null)
                            {
                                _trace.writeToLog(1, Resources.TraceCLSyncboxStartLiveSyncErrorStartingEngineMsg0Msg1, syncEngineStartError.PrimaryException.Message, syncEngineStartError.PrimaryException.Code);
                                syncEngineStartError.Log(_copiedSettings.TraceLocation, _copiedSettings.LogErrors);
                                startExceptionLogged = true;
                                throw new CLException(syncEngineStartError.PrimaryException.Code, Resources.ExceptionCLSyncboxBeginSyncStartEngine, syncEngineStartError.Exceptions);
                            }
                        }
                        catch
                        {
                            if (this._syncEngine != null)
                            {
                                try
                                {
                                    this._syncEngine.Stop();
                                }
                                catch
                                {
                                }
                                this._syncEngine = null;
                            }

                            throw;
                        }

                        // The sync engines started with syncboxes must be tracked statically so we can stop them all when the application terminates (in the ShutDown) method.
                        _startedSyncEngines.Add(_syncEngine);
                        _isStarted = true;

                        // Fire the event to the subscribers.
                        MessageEvents.DetectedSyncboxDidStartLiveSyncChange(
                            this,
                            SyncboxId: this.SyncboxId,
                            DeviceId: this.CopiedSettings.DeviceId);
                    }
                    catch
                    {
                        // Unsubscribe from live sync status messages.
                        try
                        {
                            // Subscribe to the live sync events if the user wants them.
                            if (_liveSyncStatusReceiver != null)
                            {
                                CLError errorFromUnsubscribe = MessageEvents.UnsubscribeMessageReceiver(this.SyncboxId, _copiedSettings.DeviceId);
                                if (errorFromUnsubscribe != null)
                                {
                                    _trace.writeToLog(1, Resources.TraceCLSyncboxStartLiveSyncErrorUnsubscribeMsgRcvrMsg0Msg1, errorFromUnsubscribe.PrimaryException.Message, errorFromUnsubscribe.PrimaryException.Code);
                                }
                            }
                        }
                        catch
                        {
                        }
                        
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                CLError toReturn = ex;
                if (!startExceptionLogged)
                {
                    toReturn.Log(_copiedSettings.TraceLocation, _copiedSettings.LogErrors);
                    _trace.writeToLog(1, Resources.TraceCLSyncboxStartLiveErrorExceptionMsg0Msg1, toReturn.PrimaryException.Message, toReturn.PrimaryException.Code);
                }
                return toReturn;
            }

            return null;
        }

        /// <summary>
        /// Stop live sync.
        /// </summary>
        /// <returns>Returns any error that may occur, or null.</returns>
        /// <remarks>Note that after stopping it is possible to call SartLiveSync() again to restart syncing.</remarks>
        public CLError StopLiveSync()
        {
            try
            {
                CheckDisposed();

                lock (_startLocker)
                {
                    if (!_isStarted
                        || _syncEngine == null)
                    {
                        return null;
                    }

                    try
                    {
                        // Stop the sync engine.
                        _syncEngine.Stop();

                        // Fire the event to the subscribers.
                        MessageEvents.DetectedSyncboxDidStopLiveSyncChange(
                            this,
                            SyncboxId: this.SyncboxId,
                            DeviceId: this.CopiedSettings.DeviceId);
                    }
                    catch
                    {
                    }

                    // Remove this engine from the tracking list.
                    _syncEngine = null;
                    _startedSyncEngines.Remove(_syncEngine);

                    // Unsubscribe from live sync status messages.
                    try
                    {
                        // Subscribe to the live sync events if the user wants them.
                        if (_liveSyncStatusReceiver != null)
                        {
                            CLError errorFromUnsubscribe = MessageEvents.UnsubscribeMessageReceiver(this.SyncboxId, _copiedSettings.DeviceId);
                            if (errorFromUnsubscribe != null)
                            {
                                _trace.writeToLog(1, Resources.TraceCLSyncboxStopLiveSyncErrorUnsubscribeMsgRcvrMsg0Msg1, errorFromUnsubscribe.PrimaryException.Message, errorFromUnsubscribe.PrimaryException.Code);
                            }
                        }
                    }
                    catch
                    {
                    }

                    _isStarted = false;
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.Log(_copiedSettings.TraceLocation, _copiedSettings.LogErrors);
                _trace.writeToLog(1, Resources.TraceCLSyncboxStopLiveErrorExceptionMsg0Msg1, error.PrimaryException.Message, error.PrimaryException.Code);
                return error;
            }
            return null;
        }

        /// <summary>
        /// Return true if Live Sync is started.  Otherwise, false.
        /// </summary>
        /// <returns>true: Live Sync is started.</returns>
        public bool IsLiveSyncStarted()
        {
            return _isStarted;
        }

        /// <summary>
        /// Reset sync.  Live sync must be stopped before calling this method.  Starting live sync after resetting live sync will merge the
        /// syncbox folder with the server syncbox contents.
        /// </summary>
        public CLError ResetLocalCache()
        {
            CheckDisposed();

            bool resetSyncErrorLogged = false;

            try
            {
                lock (_startLocker)
                {
                    if (_isStarted)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.Syncbox_AlreadyStarted, Resources.ExceptionCLSyncboxResetLocalCacheAlreadyStarted);
                    }

                    if (_syncEngine != null)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.Syncbox_AlreadyStarted, Resources.ExceptionCLSyncboxBeginSyncExistingEngine);
                    }

                    bool debugDependenciesValue;
                    lock (debugDependencies)
                    {
                        debugDependenciesValue = debugDependencies.Value;
                    }
                    bool copyDatabaseBetweenChangesValue;
                    lock (copyDatabaseBetweenChanges)
                    {
                        copyDatabaseBetweenChangesValue = copyDatabaseBetweenChanges.Value;
                    }
                    bool debugFileMonitorMemoryValue;
                    lock (debugFileMonitorMemory)
                    {
                        debugFileMonitorMemoryValue = debugFileMonitorMemory.Value;
                    }

                    // Create the sync engine for this syncbox instance
                    CLSyncEngine tempSyncEngine = new CLSyncEngine(this, debugDependenciesValue, copyDatabaseBetweenChangesValue, debugFileMonitorMemoryValue); // syncbox to sync (contains required settings)

                    // Reset the sync engine
                    CLError resetSyncError = tempSyncEngine.SyncReset(this);
                    if (resetSyncError != null)
                    {
                        _trace.writeToLog(1, Resources.TraceCLSyncboxResetLocalCacheErrorSyncResetMsg0Msg1, resetSyncError.PrimaryException.Message, resetSyncError.PrimaryException.Code);
                        resetSyncError.Log(_copiedSettings.TraceLocation, _copiedSettings.LogErrors);
                        resetSyncErrorLogged = true;
                        throw new CLException(CLExceptionCode.Syncing_Database, Resources.ExceptionCLSyncboxErrorResettingSyncDatabase, resetSyncError.Exceptions);
                    }
                }
            }
            catch (Exception ex)
            {
                CLError toReturn = ex;
                if (!resetSyncErrorLogged)
                {
                    toReturn.Log(_copiedSettings.TraceLocation, _copiedSettings.LogErrors);
                    _trace.writeToLog(1, Resources.TraceCLSyncboxResetLocalCacheErrorExceptionMsg0Msg1, toReturn.PrimaryException.Message, toReturn.PrimaryException.Code);
                }
                return toReturn;
            }

            return null;
        }

        /// <summary>
        /// Output the current status of live sync.
        /// </summary>
        /// <returns>Returns any error which occurred in retrieving the sync status, if any</returns>
        public CLError GetSyncCurrentStatus(out CLSyncCurrentStatus status)
        {
            CheckDisposed();

            try
            {
                if (Helpers.AllHaltedOnUnrecoverableError)
                {
                    throw new CLInvalidOperationException(CLExceptionCode.Syncbox_Initializing, Resources.CLCredentialHelpersAllHaltedOnUnrecoverableErrorIsSet);
                }

                lock (_startLocker)
                {
                    if (!_isStarted)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.Syncbox_NotStarted, Resources.ExceptionCLSyncboxNotStarted);
                    }

                    if (_syncEngine == null)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.Syncbox_NotStarted, Resources.ExceptionCLSyncboxEngineNotFound);
                    }

                    return _syncEngine.GetCurrentStatus(out status);
                }
            }
            catch (Exception ex)
            {
                status = Helpers.DefaultForType<CLSyncCurrentStatus>();
                return ex;
            }
        }

        /// <summary>
        /// Call when then application is shutting down.
        /// </summary>
        public static void Shutdown()
        {
            try
            {
                // Stop all of the active sync engines
                lock (_startLocker)
                {
                    foreach (CLSyncEngine engine in _startedSyncEngines)
                    {
                        try
                        {
                            engine.Stop();
                        }
                        catch
                        {
                        }
                    }
                }

                // Write out any development debug traces
                try
                {
                    _trace.closeMemoryTrace();
                }
                catch
                {
                }

                // Shuts down the HttpScheduler; after shutdown it cannot be used again
                try
                {
                    HttpScheduler.DisposeBothSchedulers();
                }
                catch
                {
                }

                // Shuts down the sync FileChange delay processing
                try
                {
                    DelayProcessable<FileChange>.TerminateAllProcessing();
                }
                catch
                {
                }

                // Stops network change monitoring
                try
                {
                    NetworkMonitor.DisposeInstance();
                }
                catch
                {
                }
            }
            catch
            {
            }
            finally
            {
                lock (_startLocker)
                {
                    if (_startedSyncEngines != null)
                    {
                        _startedSyncEngines.Clear();
                    }
                }
            }
        }

        #endregion  // end Public Instance Life Cycle Methods

        #region Public Static HTTP REST Methods

        #region CreateSyncbox (create a syncbox in the Cloud.)

        /// <summary>
        /// Asynchronously starts creating a new Syncbox in the Cloud.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.  Can be null.</param>
        /// <param name="asyncCallbackUserState">User state to pass to the async callback when it is fired.  Can be null.</param>
        /// <param name="plan">The storage plan to use with this Syncbox.</param>
        /// <param name="credentials">The credentials to use with this request.</param>
        /// <param name="path">(optional) The full path of the folder on disk to associate with this syncbox.  The path is used only for live sync.</param>
        /// <param name="friendlyName">(optional) The friendly name of the Syncbox.</param>
        /// <param name="settings">(optional) Settings to use with this method.</param>
        /// <param name="liveSyncStatusReceiver">(optional) The object to receive live sync status event.</param>
        /// <param name="getNewCredentialsCallback">(optional) The callback function that will provide new credentials with temporary credentials expire.</param>
        /// <param name="getNewCredentialsCallbackUserState">(optional) The user state that will be passed as a parameter to the callback function above.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public static IAsyncResult BeginCreateSyncbox(
                    AsyncCallback asyncCallback,
                    object asyncCallbackUserState,
                    CLStoragePlan plan,
                    CLCredentials credentials,
                    string path = null,
                    string friendlyName = null,
                    ICLSyncSettings settings = null,
                    IEventMessageReceiver liveSyncStatusReceiver = null,
                    Helpers.ReplaceExpiredCredentialsCallback getNewCredentialsCallback = null,
                    object getNewCredentialsCallbackUserState = null)
        {
            Helpers.CheckHalted();

            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<CreateSyncboxResult>(
                        asyncCallback,
                        asyncCallbackUserState),
                    plan = plan,
                    friendlyName = friendlyName,
                    credentials = credentials,
                    settings = settings,
                    liveSyncStatusReceiver = liveSyncStatusReceiver,
                    getNewCredentialsCallback = getNewCredentialsCallback,
                    getNewCredentialsCallbackUserState = getNewCredentialsCallbackUserState
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLSyncbox syncbox;
                        CLError processError = CreateSyncbox(
                            Data.plan,
                            Data.credentials,
                            out syncbox,
                            path,
                            Data.friendlyName,
                            Data.settings,
                            Data.liveSyncStatusReceiver,
                            Data.getNewCredentialsCallback,
                            Data.getNewCredentialsCallbackUserState);

                        Data.toReturn.Complete(
                            new CreateSyncboxResult(
                                processError,  // any error that may have occurred during processing
                                syncbox), 
                            sCompleted: false); // processing did not complete synchronously
                    }
                    catch (Exception ex)
                    {
                        Data.toReturn.HandleException(
                            ex, // the exception which was not handled correctly by the CLError wrapping
                            sCompleted: false); // processing did not complete synchronously
                    }
                },
                null);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ThreadStart(asyncThread.VoidProcess))).Start(); // start the asynchronous processing thread which is attached to its data

            // return the asynchronous result
            return asyncThread.TypedData.toReturn;
        }

        /// <summary>
        /// Finishes creating a Syncbox in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting creating the syncbox</param>
        /// <param name="result">(output) The result from creating the syncbox</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public static CLError EndCreateSyncbox(IAsyncResult asyncResult, out CreateSyncboxResult result)
        {
            return Helpers.EndAsyncOperation<CreateSyncboxResult>(asyncResult, out result);
        }

        /// <summary>
        /// Create a Syncbox in the syncbox for the current application.  This is a synchronous method.
        /// </summary>
        /// <param name="plan">The storage plan to use with this Syncbox.</param>
        /// <param name="credentials">The credentials to use for this request.</param>
        /// <param name="syncbox">(output) The created syncbox object.</param>
        /// <param name="path">(optional) The path on the local disk to associate with this syncbox.  The path is used only for live sync.</param>
        /// <param name="friendlyName">(optional) The friendly name of the Syncbox.</param>
        /// <param name="settings">(optional) The settings to use with this method</param>
        /// <param name="liveSyncStatusReceiver">(optional) The object to receive live sync status event.</param>
        /// <param name="getNewCredentialsCallback">(optional) The callback function that will provide new credentials with temporary credentials expire.</param>
        /// <param name="getNewCredentialsCallbackUserState">(optional) The user state that will be passed as a parameter to the callback function above.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public static CLError CreateSyncbox(
                    CLStoragePlan plan,
                    CLCredentials credentials,
                    out CLSyncbox syncbox,
                    string path = null,
                    string friendlyName = null,
                    ICLSyncSettings settings = null,
                    IEventMessageReceiver liveSyncStatusReceiver = null,
                    Helpers.ReplaceExpiredCredentialsCallback getNewCredentialsCallback = null,
                    object getNewCredentialsCallbackUserState = null)
        {
            Helpers.CheckHalted();

            // try/catch to process the metadata query, on catch return the error
            try
            {
                // Check the input parameters.
                if (plan == null)
                {
                    throw new CLArgumentNullException(CLExceptionCode.Syncbox_ArgumentMissing, Resources.ExceptionSyncboxPlanMustNotBeNull);
                }
                if (credentials == null)
                {
                    throw new CLArgumentNullException(CLExceptionCode.Syncbox_ArgumentMissing, Resources.ExceptionSyncboxCredentialsMustNotBeNull);
                }

                // copy settings so they don't change while processing; this also defaults some values
                ICLSyncSettingsAdvanced copiedSettings = (settings == null
                    ? AdvancedSyncSettings.CreateDefaultSettings()
                    : settings.CopySettings());

                // Check input parameters and build the query parameters.
                JsonContracts.SyncboxCreateRequest inputBox = (string.IsNullOrWhiteSpace(friendlyName) && plan == null
                    ? null
                    : new JsonContracts.SyncboxCreateRequest()
                    {
                        Syncbox = new JsonContracts.SyncboxCreateRequestDetails
                        {
                            FriendlyName = (string.IsNullOrWhiteSpace(friendlyName)
                                ? null
                                : friendlyName),
                            PlanId = plan.PlanId
                        }
                    });

                // Create the syncbox on the server and get the response object.
                JsonContracts.SyncboxResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxResponse>(
                    requestContent: inputBox,
                    serverUrl: CLDefinitions.CLPlatformAuthServerURL,
                    serverMethodPath: CLDefinitions.MethodPathAuthCreateSyncbox,
                    method: Helpers.requestMethod.post,
                    timeoutMilliseconds: copiedSettings.HttpTimeoutMilliseconds,
                    uploadDownload: null, // not an upload nor download
                    validStatusCodes: Helpers.HttpStatusesOkAccepted,
                    CopiedSettings: copiedSettings,
                    Credentials: credentials,
                    Syncbox: null, 
                    isOneOff: false);

                // Check the server response.
                if (responseFromServer == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_NoServerResponse, Resources.ExceptionOnDemandCreateSyncboxNullServerResponse);
                }
                if (responseFromServer.Syncbox == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_ServerResponseNoSyncbox, Resources.ExceptionOnDemandCreateSyncboxNoSyncboxInServerResponse);
                }

                // Convert the response object to a CLSyncbox and return that.
                syncbox =  new CLSyncbox(
                    syncboxContract: responseFromServer.Syncbox,
                    credentials: credentials,
                    settings: copiedSettings,
                    liveSyncStatusReceiver: liveSyncStatusReceiver,
                    getNewCredentialsCallback: getNewCredentialsCallback,
                    getNewCredentialsCallbackUserState: getNewCredentialsCallbackUserState);

                // CreateSyncbox call both creates a CLSyncbox via data from the server (like ListAllSyncboxes) (via the call immediately above),
                // and this CreateSyncbox call also takes a path from the user to start initialized,
                // so the following call was added to finish this initilization (unlike ListAllSyncboxes)
                syncbox.InitWithPath(path);
            }
            catch (Exception ex)
            {
                syncbox = Helpers.DefaultForType<CLSyncbox>();
                return ex;
            }
            return null;
        }
        #endregion

        #region DeleteSyncbox (delete a syncbox in the syncbox)

        /// <summary>
        /// Asynchronously starts deleting a new Syncbox in the syncbox.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when operation completes.  Can be null.</param>
        /// <param name="asyncCallbackUserState">User state to pass as a parameter to the callback when it is fired.  Can be null.</param>
        /// <param name="syncboxId">The ID of syncbox to delete.</param>
        /// <param name="credentials">The credentials to use with this request.</param>
        /// <param name="settings">(optional) settings to use with this method.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public static IAsyncResult BeginDeleteSyncbox(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            long syncboxId,
            CLCredentials credentials,
            ICLCredentialsSettings settings = null)
        {
            Helpers.CheckHalted();

            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxDeleteResult>(
                        asyncCallback,
                        asyncCallbackUserState),
                    syncboxId = syncboxId,
                    credentials = credentials,
                    settings = settings
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // Call the synchronous version of this method.
                        CLError processError = DeleteSyncbox(
                            Data.syncboxId,
                            Data.credentials,
                            Data.settings);

                        Data.toReturn.Complete(
                            new SyncboxDeleteResult(
                                processError), // any error that may have occurred during processing
                            sCompleted: false); // processing did not complete synchronously
                    }
                    catch (Exception ex)
                    {
                        Data.toReturn.HandleException(
                            ex, // the exception which was not handled correctly by the CLError wrapping
                            sCompleted: false); // processing did not complete synchronously
                    }
                },
                null);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ThreadStart(asyncThread.VoidProcess))).Start(); // start the asynchronous processing thread which is attached to its data

            // return the asynchronous result
            return asyncThread.TypedData.toReturn;
        }

        /// <summary>
        /// Finishes deleting a Syncbox in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the asynchronous operation</param>
        /// <param name="result">(output) The result from the asynchronous operation</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public static CLError EndDeleteSyncbox(IAsyncResult asyncResult, out SyncboxDeleteResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxDeleteResult>(asyncResult, out result);
        }

        /// <summary>
        /// Delete a Syncbox in the syncbox.  This is a synchronous method.
        /// </summary>
        /// <param name="syncboxId">the ID of the syncbox to delete.</param>
        /// <param name="credentials">The credentials to use for this request.</param>
        /// <param name="settings">(optional) the settings to use with this method</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public static CLError DeleteSyncbox(
                    long syncboxId,
                    CLCredentials credentials,
                    ICLCredentialsSettings settings = null)
        {
            Helpers.CheckHalted();

            // try/catch to process the query, on catch return the error
            try
            {
                // copy settings so they don't change while processing; this also defaults some values
                ICLSyncSettingsAdvanced copiedSettings = (settings == null
                    ? AdvancedSyncSettings.CreateDefaultSettings()
                    : settings.CopySettings());

                // Body of the post
                JsonContracts.SyncboxIdOnly inputBox = new JsonContracts.SyncboxIdOnly()
                    {
                        Id = syncboxId
                    };

                JsonContracts.SyncboxDeleteResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxDeleteResponse>(
                    requestContent: inputBox,
                    serverUrl: CLDefinitions.CLPlatformAuthServerURL,
                    serverMethodPath: CLDefinitions.MethodPathAuthDeleteSyncbox,
                    method: Helpers.requestMethod.post,
                    timeoutMilliseconds: copiedSettings.HttpTimeoutMilliseconds,
                    uploadDownload: null, // not an upload nor download
                    validStatusCodes: Helpers.HttpStatusesOkAccepted,
                    CopiedSettings: copiedSettings,
                    Credentials: credentials,
                    Syncbox: null, 
                    isOneOff: false);

                // Check the server response.
                if (responseFromServer == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_NoServerResponse, Resources.ExceptionOnDemandNullServerResponse);
                }
                if (String.IsNullOrEmpty(responseFromServer.Status))
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_ServerResponseNoStatus, Resources.ExceptionOnDemandServerResponseStatusNull);
                }

                // Convert the response object to a CLSyncbox and return that.
                if (responseFromServer.Status != CLDefinitions.RESTResponseStatusSuccess)
                {
                    throw new CLException(CLExceptionCode.OnDemand_DeleteSyncbox, Resources.ExceptionOnDemandDeleteSyncboxErrorMsg0);
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }
        #endregion

        #region ListAllSyncboxes (list syncboxes in the syncbox)

        /// <summary>
        /// Asynchronously starts listing syncboxes in the syncbox.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass as a parameter to the callback when it is fired.</param>
        /// <param name="credentials">The credentials to use with this request.</param>
        /// <param name="settings">(optional) settings to use with this method.</param>
        /// <param name="liveSyncStatusReceiver">(optional) The object to receive live sync status event.</param>
        /// <param name="getNewCredentialsCallback">(optional) The delegate to call for getting new temporary credentials.</param>
        /// <param name="getNewCredentialsCallbackUserState">(optional) The user state to pass to the delegate above.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public static IAsyncResult BeginListAllSyncboxes(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            CLCredentials credentials,
            ICLCredentialsSettings settings = null,
            IEventMessageReceiver liveSyncStatusReceiver = null,
            Helpers.ReplaceExpiredCredentialsCallback getNewCredentialsCallback = null,
            object getNewCredentialsCallbackUserState = null)
        {
            Helpers.CheckHalted();

            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxListResult>(
                        asyncCallback,
                        asyncCallbackUserState),
                    credentials = credentials,
                    settings = settings,
                    liveSyncStatusReceiver = liveSyncStatusReceiver,
                    getNewCredentialsCallback = getNewCredentialsCallback,
                    getNewCredentialsCallbackUserState = getNewCredentialsCallbackUserState,
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // Call the synchronous version of this method.
                        CLSyncbox[] returnedSyncboxes;
                        CLError processError = ListAllSyncboxes(
                            Data.credentials,
                            out returnedSyncboxes,
                            Data.settings,
                            Data.liveSyncStatusReceiver,
                            Data.getNewCredentialsCallback,
                            Data.getNewCredentialsCallbackUserState);

                        Data.toReturn.Complete(
                            new SyncboxListResult(
                                processError, // any error that may have occurred during processing
                                returnedSyncboxes),  // the returned data
                            sCompleted: false); // processing did not complete synchronously
                    }
                    catch (Exception ex)
                    {
                        Data.toReturn.HandleException(
                            ex, // the exception which was not handled correctly by the CLError wrapping
                            sCompleted: false); // processing did not complete synchronously
                    }
                },
                null);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ThreadStart(asyncThread.VoidProcess))).Start(); // start the asynchronous processing thread which is attached to its data

            // return the asynchronous result
            return asyncThread.TypedData.toReturn;
        }

        /// <summary>
        /// Finishes listing syncboxes in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the asynchronous operation</param>
        /// <param name="result">(output) The result from the asynchronous operation</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public static CLError EndListAllSyncboxes(IAsyncResult asyncResult, out SyncboxListResult result)
        {
            Helpers.CheckHalted();
            return Helpers.EndAsyncOperation<SyncboxListResult>(asyncResult, out result);
        }

        /// <summary>
        /// List syncboxes in the syncbox for these credentials.  This is a synchronous method.
        /// </summary>
        /// <param name="credentials">The credentials to use for this request.</param>
        /// <param name="returnedSyncboxes">(output) The returned array of syncboxes.</param>
        /// <param name="settings">(optional) the settings to use with this method</param>
        /// <param name="liveSyncStatusReceiver">(optional) The object to receive live sync status event.</param>
        /// <param name="getNewCredentialsCallback">(optional) The delegate to call for getting new temporary credentials.</param>
        /// <param name="getNewCredentialsCallbackUserState">(optional) The user state to pass to the delegate above.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        /// <remarks>The response array may be null, empty, or may contain null items.</remarks>
        public static CLError ListAllSyncboxes(
            CLCredentials credentials,
            out CLSyncbox[] returnedSyncboxes,
            ICLCredentialsSettings settings = null,
            IEventMessageReceiver liveSyncStatusReceiver = null,
            Helpers.ReplaceExpiredCredentialsCallback getNewCredentialsCallback = null,
            object getNewCredentialsCallbackUserState = null)
        {
            Helpers.CheckHalted();

            // try/catch to process the query, on catch return the error
            try
            {
                // copy settings so they don't change while processing; this also defaults some values
                ICLSyncSettingsAdvanced copiedSettings = (settings == null
                    ? AdvancedSyncSettings.CreateDefaultSettings()
                    : settings.CopySettings());

                // Communicate with the server.
                JsonContracts.SyncboxListResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxListResponse>(
                    requestContent: null,
                    serverUrl: CLDefinitions.CLPlatformAuthServerURL,
                    serverMethodPath: CLDefinitions.MethodPathAuthListSyncboxes,
                    method: Helpers.requestMethod.post,
                    timeoutMilliseconds: copiedSettings.HttpTimeoutMilliseconds,
                    uploadDownload: null, // not an upload nor download
                    validStatusCodes: Helpers.HttpStatusesOkAccepted,
                    CopiedSettings: copiedSettings,
                    Credentials: credentials,
                    Syncbox: null, 
                    isOneOff: false);

                // Convert the server response to a list of initialized CLSyncboxes.
                if (responseFromServer != null && responseFromServer.Syncboxes != null)
                {
                    List<CLSyncbox> listSyncboxes = new List<CLSyncbox>();
                    foreach (JsonContracts.Syncbox syncbox in responseFromServer.Syncboxes)
                    {
                        if (syncbox != null)
                        {
                            listSyncboxes.Add(
                                new CLSyncbox(syncbox,
                                    credentials: credentials,
                                    settings: copiedSettings,
                                    liveSyncStatusReceiver: liveSyncStatusReceiver,
                                    getNewCredentialsCallback: getNewCredentialsCallback,
                                    getNewCredentialsCallbackUserState: getNewCredentialsCallbackUserState));
                        }
                        else
                        {
                            throw new CLNullReferenceException(CLExceptionCode.OnDemand_NoServerResponse, Resources.ExceptionCLHttpRestWithoutMetadata);
                        }
                    }

                    // Return the results.
                    returnedSyncboxes = listSyncboxes.ToArray();
                }
                else
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_NoServerResponse, Resources.ExceptionCLHttpRestWithoutSessions);
                }

            }
            catch (Exception ex)
            {
                returnedSyncboxes = Helpers.DefaultForType<CLSyncbox[]>();
                return ex;
            }
            return null;
        }
        #endregion  // end List (list syncboxes in the syncbox)

        #endregion  // end Public Static HTTP REST Methods

        #region Public Instance Methods

        /// <summary>
        /// Update the credentials for this syncbox.
        /// </summary>
        /// <param name="credentials">The new credentials.</param>
        /// <returns>Nothing</returns>
        public void UpdateCredentials(CLCredentials credentials)
        {
            CheckDisposed();

            Credentials = credentials;
        }

        #endregion // end Public Instance Methods

        #region Public Instance HTTP REST Methods

        #region RootFolder (Queries the syncbox for the item at the root path)
        /// <summary>
        /// Asynchronously starts querying the syncbox for an item at the syncbox root path; outputs a CLFileItem object.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when operation completes</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginRootFolder(AsyncCallback asyncCallback, object asyncCallbackUserState)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginItemForPath(asyncCallback, asyncCallbackUserState, (/* '/' */ ((char)0x2f)).ToString());
        }

        /// <summary>
        /// Finishes querying the syncbox for an item at the syncbox root path, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) The result from the metadata query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndRootFolder(IAsyncResult asyncResult, out SyncboxGetItemAtPathResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndItemForPath(asyncResult, out result);

        }

        /// <summary>
        /// Queries the syncbox for an item at the syncbox root path.
        /// </summary>
        /// <param name="item">(output) The returned item.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError RootFolder(out CLFileItem item)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.ItemForPath((/* '/' */ ((char)0x2f)).ToString(), out item);
        }

        #endregion  // end RootFolder (Queries the syncbox for the item at the root path)

        #region ItemForItemUid (Returns a CLFileItem for the syncbox item with the given UID.)

        /// <summary>
        /// Asynchronously starts querying the syncbox for an item with the given UID. Outputs a CLFileItem object.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        /// <param name="itemUid">The UID to use in the query.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginItemForItemUid(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            string itemUid)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginItemForItemUid(asyncCallback, asyncCallbackUserState, itemUid);
        }

        /// <summary>
        /// Finishes getting an item in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndItemForItemUid(IAsyncResult asyncResult, out SyncboxGetItemAtItemUidResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndItemForItemUid(asyncResult, out result);
        }

        /// <summary>
        /// Query the syncbox for an item with the given UID. Outputs a CLFileItem object.
        /// </summary>
        /// <param name="itemUid">The UID to use in the query.</param>
        /// <param name="item">(output) The returned item.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError ItemForItemUid(string itemUid, out CLFileItem item)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.ItemForItemUid(itemUid, out item);
        }

        #endregion  // end ItemForItemUid (Returns a CLFileItem for the syncbox item with the given UID.)

        #region ItemForPath (Queries the syncbox for the item at a particular path)
        /// <summary>
        /// Asynchronously starts querying the syncbox for an item at a given path (must be specified) for existing metadata at that path; outputs a CLFileItem object.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when operation completes</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        /// <param name="relativePath">Relative path in the syncbox.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginItemForPath(
            AsyncCallback asyncCallback, 
            object asyncCallbackUserState, 
            string relativePath)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginItemForPath(asyncCallback, asyncCallbackUserState, relativePath);
        }

        /// <summary>
        /// Finishes quering the syncbox for an item at a path, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) The result from the metadata query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndItemForPath(IAsyncResult asyncResult, out SyncboxGetItemAtPathResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndItemForPath(asyncResult, out result);
        }

        /// <summary>
        /// Queries the syncbox at a given file or folder path (must be specified) for existing item metadata at that path.
        /// </summary>
        /// <param name="relativePath">Relative path in the syncbox.</param>
        /// <param name="item">(output) The returned item.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError ItemForPath(string relativePath, out CLFileItem item)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.ItemForPath(relativePath, out item);
        }

        #endregion  // end ItemForPath (Queries the syncbox for the item at a particular path)

        #region ItemsForPath (Queries the syncbox for the contents of the folder item at a particular path)
        /// <summary>
        /// Asynchronously starts querying the syncbox for the contents of a folder item at a given relative path in the syncbox. Outputs an array of CLFileItem objects.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when operation completes</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        /// <param name="relativePath">Relative path in the syncbox.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginItemsForPath(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            string relativePath)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginItemsForPath(asyncCallback, asyncCallbackUserState, relativePath);
        }

        /// <summary>
        /// Finishes quering the syncbox for an item at a path, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) The result from the metadata query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndItemsForPath(IAsyncResult asyncResult, out SyncboxItemsAtPathResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndItemsForPath(asyncResult, out result);
        }

        /// <summary>
        /// Queries the syncbox at a given file or folder path (must be specified) for existing item metadata at that path.
        /// </summary>
        /// <param name="relativePath">Relative path in the syncbox.</param>
        /// <param name="items">(output) The returned items.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError ItemsForPath(string relativePath, out CLFileItem[] items)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.ItemsForPath(relativePath, out items);
        }

        #endregion  // end ItemsForPath (Queries the syncbox for the contents of the folder item at a particular path)

        #region ItemsForFolderItem (Queries the syncbox for the contents of the folder item)
        /// <summary>
        /// Asynchronously starts querying the syncbox for the contents of a folder at the given folder item. Outputs an array of CLFileItem objects.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when operation completes</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        /// <param name="folderItem">The CLFileItem representing the folder to query.  If folderItem is null, the contents of the synbox root folder will be returned.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginItemsForFolderItem(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            CLFileItem folderItem)
        {
            return BeginItemsForFolderItem(asyncCallback, asyncCallbackUserState, folderItem, includePending: false, includeDeleted: false);
        }

        // \cond

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public IAsyncResult BeginItemsForFolderItem(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            CLFileItem folderItem,
            bool includePending,
            bool includeDeleted)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginItemsForFolderItem(asyncCallback, asyncCallbackUserState, folderItem, includePending, includeDeleted);
        }

        // \endcond

        /// <summary>
        /// Finishes quering the syncbox for the contents of a folder at the given folder item, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) The result from the metadata query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndItemsForFolderItem(IAsyncResult asyncResult, out SyncboxItemsForFolderItemResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndItemsForFolderItem(asyncResult, out result);
        }

        /// <summary>
        /// Queries the syncbox for the contents of a folder at the given folder item.
        /// </summary>
        /// <param name="folderItem">The CLFileItem representing the folder to query.  If folderItem is null, the contents of the synbox root folder will be returned.</param>
        /// <param name="items">(output) The returned items.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError ItemsForFolderItem(CLFileItem folderItem, out CLFileItem[] items)
        {
            return ItemsForFolderItem(folderItem, out items, includePending: false, includeDeleted: false);
        }

        // \cond
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public CLError ItemsForFolderItem(CLFileItem folderItem,
            out CLFileItem[] items,
            bool includePending,
            bool includeDeleted)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.ItemsForFolderItem(folderItem, out items, includePending, includeDeleted);
        }
        // \endcond

        #endregion  // end ItemsForFolderItem (Queries the syncbox for the contents of the folder item)

        #region HierarchyOfFolderAtPath (Queries the syncbox for the folder hierarchy under the given folder path)
        /// <summary>
        /// Asynchronously starts getting the syncbox items that represent the specified folder's folder hierarchy.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when operation completes</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        /// <param name="relativePath">(optional) relative root path of contents query.  If this is null or empty, the syncbox root folder will be queried.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginHierarchyOfFolderAtPath(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            string relativePath = null)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginHierarchyOfFolderAtPath(asyncCallback, asyncCallbackUserState, relativePath);
        }

        /// <summary>
        /// Finishes getting the folder hierarchy, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting getting folder contents</param>
        /// <param name="result">(output) The result from folder contents</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndHierarchyOfFolderAtPath(IAsyncResult asyncResult, out SyncboxHierarchyOfFolderAtPathResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndHierarchyOfFolderAtPath(asyncResult, out result);
        }

        /// <summary>
        /// Gets the syncbox items that represent the specified folder's folder hierarchy.
        /// </summary>
        /// <param name="relativePath">(optional) relative root path of contents query.  If this is null or empty, the syncbox root folder will be queried.</param>
        /// <param name="items">(output) resulting items.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError HierarchyOfFolderAtPath(
            string relativePath,
            out CLFileItem[] items)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.HierarchyOfFolderAtPath(relativePath, out items);
        }

        #endregion  // end HierarchyOfFolderAtPath (Queries the syncbox for the folder hierarchy under the given folder path)

        #region HierarchyOfFolderAtFolderItem (Query the server for the folder hierarchy at a folder item)
        /// <summary>
        /// Asynchronously starts querying the syncbox folder hierarchy at a particular folder item.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when operation completes</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        /// <param name="folderItem">The CLFileItem representing the folder to query.  If folderItem is null, the hierarchy of the synbox root folder will be returned.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginHierarchyOfFolderAtFolderItem(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            CLFileItem folderItem)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginHierarchyOfFolderAtFolderItem(asyncCallback, asyncCallbackUserState, folderItem);
        }

        /// <summary>
        /// Finishes getting the folder hierarchy if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting getting folder contents</param>
        /// <param name="result">(output) The result from folder contents</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndHierarchyOfFolderAtFolderItem(IAsyncResult asyncResult, out SyncboxHierarchyOfFolderAtFolderItemResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndHierarchyOfFolderAtFolderItem(asyncResult, out result);
        }

        /// <summary>
        /// Queries the syncbox folder hierarchy at a particular folder item.
        /// </summary>
        /// <param name="folderItem">The CLFileItem representing the folder to query.  If folderItem is null, the syncbox root folder will be queried.</param>
        /// <param name="items">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError HierarchyOfFolderAtFolderItem(
            CLFileItem folderItem,
            out CLFileItem[] items)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.HierarchyOfFolderAtFolderItem(folderItem, out items);
        }

        #endregion  // end HierarchyOfFolderAtFolderItem (Query the server for the folder hierarchy at a folder item)

        #region RenameFiles (Rename files in-place in the syncbox)
        /// <summary>
        /// Asynchronously starts renaming files in-place in the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="itemCompletionCallback">Delegate which will be fired upon successful communication for every response item.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the completion delegate is fired.</param>
        /// <param name="itemsToRename">One or more pairs of items to rename and the new name of each item (just the filename.ext).</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginRenameFiles(AsyncCallback asyncCallback, object asyncCallbackUserState, CLFileItemCompletionCallback itemCompletionCallback, object itemCompletionCallbackUserState, params RenameItemParams[] itemsToRename)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginRenameFiles(asyncCallback, asyncCallbackUserState, ReservedForActiveSync, itemCompletionCallback, itemCompletionCallbackUserState, itemsToRename);
        }

        /// <summary>
        /// Finishes renaming files in-place in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndRenameFiles(IAsyncResult asyncResult, out SyncboxRenameFilesResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndRenameFiles(asyncResult, out result);
        }

        /// <summary>
        /// Renames files in-place in the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="itemCompletionCallback">Delegate which will be fired upon successful communication for every response item.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the completion delegate is fired.</param>
        /// <param name="itemsToRename">One or more pairs of items to rename and the new name of each item (just the filename.ext).</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError RenameFiles(CLFileItemCompletionCallback itemCompletionCallback, object itemCompletionCallbackUserState, params RenameItemParams[] itemsToRename)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.RenameFiles(ReservedForActiveSync, itemCompletionCallback, itemCompletionCallbackUserState, itemsToRename);
        }

        #endregion  // end RenameFiles (Rename files in-place in the syncbox)

        #region RenameFolders (Rename folders in-place in the syncbox)
        /// <summary>
        /// Asynchronously starts renaming folders in-place in the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="itemCompletionCallback">Delegate which will be fired upon successful communication for every response item.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the completion delegate is fired.</param>
        /// <param name="itemsToRename">One or more pairs of items to rename and the new name of each item (just the last token in the path).</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginRenameFolders(AsyncCallback asyncCallback, object asyncCallbackUserState, CLFileItemCompletionCallback itemCompletionCallback, object itemCompletionCallbackUserState, params RenameItemParams[] itemsToRename)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginRenameFolders(asyncCallback, asyncCallbackUserState, ReservedForActiveSync, itemCompletionCallback, itemCompletionCallbackUserState, itemsToRename);
        }

        /// <summary>
        /// Finishes renaming folders in-place in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndRenameFolders(IAsyncResult asyncResult, out SyncboxRenameFoldersResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndRenameFolders(asyncResult, out result);
        }

        /// <summary>
        /// Renames folders in-place in the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="itemCompletionCallback">Delegate which will be fired upon successful communication for every response item.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the completion delegate is fired.</param>
        /// <param name="itemsToRename">An array of pairs of items to rename and the new name of each item (just the last token in the path).</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError RenameFolders(CLFileItemCompletionCallback itemCompletionCallback, object itemCompletionCallbackUserState, params RenameItemParams[] itemsToRename)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.RenameFolders(ReservedForActiveSync, itemCompletionCallback, itemCompletionCallbackUserState, itemsToRename);
        }

        #endregion  // end RenameFolders (Rename folders in the syncbox)

        #region MoveFiles (Move files in the syncbox)
        /// <summary>
        /// Asynchronously starts moving files in the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="itemCompletionCallback">Delegate which will be fired upon successful communication for every response item.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the completion delegate is fired.</param>
        /// <param name="itemsToMove">One or more pairs of item to move and a folder item representing the new parent of the item being moved.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginMoveFiles(
            AsyncCallback asyncCallback, 
            object asyncCallbackUserState, 
            CLFileItemCompletionCallback itemCompletionCallback, 
            object itemCompletionCallbackUserState, 
            params MoveItemParams[] itemsToMove)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginMoveFiles(asyncCallback, asyncCallbackUserState, ReservedForActiveSync, itemCompletionCallback, itemCompletionCallbackUserState, itemsToMove);
        }

        /// <summary>
        /// Finishes moving files in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndMoveFiles(IAsyncResult asyncResult, out SyncboxMoveFilesResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndMoveFiles(asyncResult, out result);
        }

        /// <summary>
        /// Moves files in the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="itemCompletionCallback">Delegate which will be fired upon successful communication for every response item.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the completion delegate is fired.</param>
        /// <param name="itemsToMove">One or more pairs of item to move and a folder item representing the new parent of the item being moved.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError MoveFiles(CLFileItemCompletionCallback itemCompletionCallback, object itemCompletionCallbackUserState, params MoveItemParams[] itemsToMove)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.MoveFiles(ReservedForActiveSync, itemCompletionCallback, itemCompletionCallbackUserState, itemsToMove);
        }

        #endregion  // end MoveFiles (Move files in the syncbox)

        #region MoveFolders (Move folders in the syncbox)
        /// <summary>
        /// Asynchronously starts moving folders in the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="itemCompletionCallback">Delegate which will be fired upon successful communication for every response item.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the completion delegate is fired.</param>
        /// <param name="itemsToMove">One or more pairs of item to move and a folder item representing the new parent of the item being moved.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginMoveFolders(AsyncCallback asyncCallback, object asyncCallbackUserState, CLFileItemCompletionCallback itemCompletionCallback, object itemCompletionCallbackUserState, params MoveItemParams[] itemsToMove)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginMoveFolders(asyncCallback, asyncCallbackUserState, ReservedForActiveSync, itemCompletionCallback, itemCompletionCallbackUserState, itemsToMove);
        }

        /// <summary>
        /// Finishes moving folders in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndMoveFolders(IAsyncResult asyncResult, out SyncboxMoveFoldersResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndMoveFolders(asyncResult, out result);
        }

        /// <summary>
        /// Moves folders in the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="itemCompletionCallback">Delegate which will be fired upon successful communication for every response item.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the completion delegate is fired.</param>
        /// <param name="itemsToMove">One or more pairs of item to move and a folder item representing the new parent of the item being moved.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError MoveFolders(CLFileItemCompletionCallback itemCompletionCallback, object itemCompletionCallbackUserState, params MoveItemParams[] itemsToMove)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.MoveFolders(ReservedForActiveSync, itemCompletionCallback, itemCompletionCallbackUserState, itemsToMove);
        }

        #endregion  // end RenameFolders (Rename folders in the syncbox)

        #region DeleteFiles (Delete files in the syncbox)
        /// <summary>
        /// Asynchronously starts deleting files in the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="itemsToDelete">One or more file items to delete.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginDeleteFiles(AsyncCallback asyncCallback, object asyncCallbackUserState, CLFileItemCompletionCallback itemCompletionCallback, object itemCompletionCallbackUserState, params CLFileItem[] itemsToDelete)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginDeleteFiles(asyncCallback, asyncCallbackUserState, ReservedForActiveSync, itemCompletionCallback, itemCompletionCallbackUserState, itemsToDelete);
        }

        /// <summary>
        /// Finishes deleting files in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndDeleteFiles(IAsyncResult asyncResult, out SyncboxDeleteFilesResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndDeleteFiles(asyncResult, out result);
        }

        /// <summary>
        /// Deletes files in the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="itemsToDelete">One or more file items to delete.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError DeleteFiles(CLFileItemCompletionCallback itemCompletionCallback, object itemCompletionCallbackUserState, params CLFileItem[] itemsToDelete)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.DeleteFiles(ReservedForActiveSync, itemCompletionCallback, itemCompletionCallbackUserState, itemsToDelete);
        }

        #endregion  // end DeleteFiles (Delete files in the syncbox)

        #region DeleteFolders (Delete folders in the syncbox)
        /// <summary>
        /// Asynchronously starts deleting folders in the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="itemsToDelete">One or more folder items to delete.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginDeleteFolders(AsyncCallback asyncCallback, object asyncCallbackUserState, CLFileItemCompletionCallback itemCompletionCallback, object itemCompletionCallbackUserState, params CLFileItem[] itemsToDelete)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginDeleteFolders(asyncCallback, asyncCallbackUserState, ReservedForActiveSync, itemCompletionCallback, itemCompletionCallbackUserState, itemsToDelete);
        }

        /// <summary>
        /// Finishes deleting folders in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndDeleteFolders(IAsyncResult asyncResult, out SyncboxDeleteFoldersResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndDeleteFolders(asyncResult, out result);
        }

        /// <summary>
        /// Deletes folders in the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="itemsToDelete">One or more folder items to delete.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError DeleteFolders(CLFileItemCompletionCallback itemCompletionCallback, object itemCompletionCallbackUserState, params CLFileItem[] itemsToDelete)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.DeleteFolders(ReservedForActiveSync, itemCompletionCallback, itemCompletionCallbackUserState, itemsToDelete);
        }

        #endregion  // end DeleteFolders (Delete folders in the syncbox)

        #region PurgePendingFiles (Delete files that have not yet been uploaded in the syncbox.)
        /// <summary>
        /// Asynchronously starts purging the pending files in the syncbox.  Pending files are files whose metadata has been uploaded, but the file data upload itself has not started or completed.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="itemsToPurge">One or more file items to purge.  If this parameter is null, all of the pending files will be purged in this syncbox.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginPurgePendingFiles(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            CLFileItemCompletionCallback itemCompletionCallback,
            object itemCompletionCallbackUserState,
            params CLFileItem[] itemsToPurge)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginPurgePendingFiles(asyncCallback, asyncCallbackUserState, ReservedForActiveSync, itemCompletionCallback, itemCompletionCallbackUserState, itemsToPurge);
        }

        /// <summary>
        /// Finishes purging the pending files in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndPurgePendingFiles(IAsyncResult asyncResult, out SyncboxPurgePendingFilesResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndPurgePendingFiles(asyncResult, out result);
        }

        /// <summary>
        /// Purge pending files in the syncbox.  Pending files are files whose metadata has been uploaded, but the file data upload itself has not started or completed.
        /// </summary>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="itemsToPurge">One or more file items to purge.  If this parameter is null, all of the pending files will be purged in this syncbox.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError PurgePendingFiles(
            CLFileItemCompletionCallback itemCompletionCallback,
            object itemCompletionCallbackUserState,
            params CLFileItem[] itemsToPurge)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.PurgePendingFiles(ReservedForActiveSync, itemCompletionCallback, itemCompletionCallbackUserState, itemsToPurge);
        }

        #endregion  // end PurgePendingFiles (Delete files that have not yet been uploaded in the syncbox.)

        #region AddFolders (Add folders to the syncbox)
        /// <summary>
        /// Asynchronously starts adding folders to the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="folderItemsToAdd">One or more pairs of folder parent item and name of the new folder to add.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginAddFolders(AsyncCallback asyncCallback, object asyncCallbackUserState, CLFileItemCompletionCallback itemCompletionCallback, object itemCompletionCallbackUserState, params AddFolderItemParams[] folderItemsToAdd)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginAddFolders(asyncCallback, asyncCallbackUserState, ReservedForActiveSync, itemCompletionCallback, itemCompletionCallbackUserState, folderItemsToAdd);
        }

        /// <summary>
        /// Finishes adding folders to the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndAddFolders(IAsyncResult asyncResult, out SyncboxAddFoldersResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndAddFolders(asyncResult, out result);
        }

        /// <summary>
        /// Adds folders to the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="itemsToDelete">One or more file items to delete.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError AddFolders(CLFileItemCompletionCallback itemCompletionCallback, object itemCompletionCallbackUserState, params AddFolderItemParams[] folderItemsToAdd)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.AddFolders(ReservedForActiveSync, itemCompletionCallback, itemCompletionCallbackUserState, folderItemsToAdd);
        }

        #endregion  // end AddFolders (Add folders to the syncbox)

        #region AddFiles (Adds files to the syncbox)
        /// <summary>
        /// Asynchronously starts adding files in the syncbox.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="transferStatusCallback">Callback method which will be fired when the transfer progress changes for upload.  Can be null</param>
        /// <param name="transferStatusCallbackUserState">User state to be passed whenever the transfer progress callback is fired.  Can be null.</param>
        /// <param name="cancellationSource">An optional cancellation token which may be used to cancel uploads in progress immediately.  Can be null</param>
        /// <param name="filesToAdd">(params) An array of information for each file to add (full path of the file, parent folder in the syncbox and the name of the file in the syncbox).</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginAddFiles(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            CLFileItemCompletionCallback itemCompletionCallback,
            object itemCompletionCallbackUserState,
            CLFileUploadTransferStatusCallback transferStatusCallback,
            object transferStatusCallbackUserState,
            CancellationTokenSource cancellationSource,
            params AddFileItemParams[] filesToAdd)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginAddFiles(asyncCallback, asyncCallbackUserState, ReservedForActiveSync, itemCompletionCallback, itemCompletionCallbackUserState, transferStatusCallback, transferStatusCallbackUserState, cancellationSource, filesToAdd);
        }

        /// <summary>
        /// Finishes adding files in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) The result from the request</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndAddFiles(IAsyncResult asyncResult, out SyncboxAddFilesResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndAddFiles(asyncResult, out result);
        }

        /// <summary>
        /// Add files in the syncbox.  Uploads the files to the Cloud.
        /// </summary>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="transferStatusCallback">Callback method which will be fired when the transfer progress changes for upload.  Can be null</param>
        /// <param name="transferStatusCallbackUserState">User state to be passed whenever the transfer progress callback is fired.  Can be null.</param>
        /// <param name="cancellationSource">An optional cancellation token which may be used to cancel uploads in progress immediately.  Can be null</param>
        /// <param name="filesToAdd">(params) An array of information for each file to add (full path of the file, parent folder in the syncbox and the name of the file in the syncbox).</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError AddFiles(
            CLFileItemCompletionCallback itemCompletionCallback,
            object itemCompletionCallbackUserState,
            CLFileUploadTransferStatusCallback transferStatusCallback,
            object transferStatusCallbackUserState,
            CancellationTokenSource cancellationSource,
            params AddFileItemParams[] filesToAdd)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.AddFiles(ReservedForActiveSync, itemCompletionCallback, itemCompletionCallbackUserState, transferStatusCallback, transferStatusCallbackUserState, cancellationSource, filesToAdd);
        }

        #endregion  // end AddFiles (Adds files to the syncbox)

        #region ModifyFiles (Uploads modified files to the syncbox)
        /// <summary>
        /// Asynchronously starts modifying files in the syncbox.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="transferStatusCallback">Callback method which will be fired when the transfer progress changes for upload.  Can be null</param>
        /// <param name="transferStatusCallbackUserState">User state to be passed whenever the transfer progress callback is fired.  Can be null.</param>
        /// <param name="cancellationSource">An optional cancellation token which may be used to cancel uploads in progress immediately.  Can be null</param>
        /// <param name="filesToModify">(params) An array of CLFileItems to modify.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginModifyFiles(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            CLFileItemCompletionCallback itemCompletionCallback,
            object itemCompletionCallbackUserState,
            CLFileUploadTransferStatusCallback transferStatusCallback,
            object transferStatusCallbackUserState,
            CancellationTokenSource cancellationSource,
            params ModifyFileItemParams[] filesToModify)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginModifyFiles(asyncCallback, asyncCallbackUserState, ReservedForActiveSync, itemCompletionCallback, itemCompletionCallbackUserState, transferStatusCallback, transferStatusCallbackUserState, cancellationSource, filesToModify);
        }

        /// <summary>
        /// Finishes modifying files in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) The result from the request</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndModifyFiles(IAsyncResult asyncResult, out SyncboxModifyFilesResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndModifyFiles(asyncResult, out result);
        }

        /// <summary>
        /// Modifies files in the syncbox.  Uploads the modified files to the Cloud.
        /// </summary>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="transferStatusCallback">Callback method which will be fired when the transfer progress changes for upload.  Can be null</param>
        /// <param name="transferStatusCallbackUserState">User state to be passed whenever the transfer progress callback is fired.  Can be null.</param>
        /// <param name="cancellationSource">An optional cancellation token which may be used to cancel uploads in progress immediately.  Can be null</param>
        /// <param name="filesToModify">(params) An array of CLFileItems to modify.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError ModifyFiles(
            CLFileItemCompletionCallback itemCompletionCallback,
            object itemCompletionCallbackUserState,
            CLFileUploadTransferStatusCallback transferStatusCallback,
            object transferStatusCallbackUserState,
            CancellationTokenSource cancellationSource,
            params ModifyFileItemParams[] filesToModify)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.ModifyFiles(ReservedForActiveSync, itemCompletionCallback, itemCompletionCallbackUserState, transferStatusCallback, transferStatusCallbackUserState, cancellationSource, filesToModify);
        }

        #endregion  // end AddFiles (Adds files to the syncbox)

        #region GetAllPending
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        /// <summary>
        /// Asynchronously starts querying for all pending files
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">User state to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginGetAllPending(AsyncCallback asyncCallback,
            object asyncCallbackState,
            int timeoutMilliseconds)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetAllPending(asyncCallback, asyncCallbackState, timeoutMilliseconds);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        /// <summary>
        /// Finishes a query for all pending files if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the pending query</param>
        /// <param name="result">(output) The result from the pending query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetAllPending(IAsyncResult asyncResult, out GetAllPendingResult result)
        {
            CheckDisposed(isOneOff: true);
            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndGetAllPending(asyncResult, out result);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        /// <summary>
        /// Queries the server for a given sync box and device to get all files which are still pending upload
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetAllPending(int timeoutMilliseconds, out JsonContracts.PendingResponse response)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.GetAllPending(timeoutMilliseconds, out response);
        }
        #endregion

        #region GetFileVersions
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        /// <summary>
        /// Asynchronously starts querying the server for all versions of a given file
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">User state to pass when firing async callback</param>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginGetFileVersions(AsyncCallback asyncCallback,
            object asyncCallbackState,
            string fileServerId,
            int timeoutMilliseconds)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetFileVersions(asyncCallback,
                asyncCallbackState,
                fileServerId,
                timeoutMilliseconds);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        /// <summary>
        /// Asynchronously starts querying the server for all versions of a given file
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">User state to pass when firing async callback</param>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="includeDeletedVersions">(optional) whether to include file versions which are deleted</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginGetFileVersions(AsyncCallback asyncCallback,
            object asyncCallbackState,
            string fileServerId,
            int timeoutMilliseconds,
            bool includeDeletedVersions)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetFileVersions(asyncCallback,
                asyncCallbackState,
                fileServerId,
                timeoutMilliseconds,
                includeDeletedVersions);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        /// <summary>
        /// Asynchronously starts querying the server for all versions of a given file
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">User state to pass when firing async callback</param>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Full path to the file where it would be placed locally within the sync root</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginGetFileVersions(AsyncCallback asyncCallback,
            object asyncCallbackState,
            int timeoutMilliseconds,
            FilePath pathToFile)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetFileVersions(asyncCallback, asyncCallbackState, timeoutMilliseconds, pathToFile);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        /// <summary>
        /// Asynchronously starts querying the server for all versions of a given file
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">User state to pass when firing async callback</param>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Full path to the file where it would be placed locally within the sync root</param>
        /// <param name="includeDeletedVersions">(optional) whether to include file versions which are deleted</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginGetFileVersions(AsyncCallback asyncCallback,
            object asyncCallbackState,
            int timeoutMilliseconds,
            FilePath pathToFile,
            bool includeDeletedVersions)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetFileVersions(asyncCallback, asyncCallbackState, timeoutMilliseconds, pathToFile, includeDeletedVersions);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        /// <summary>
        /// Asynchronously starts querying the server for all versions of a given file
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">User state to pass when firing async callback</param>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Full path to the file where it would be placed locally within the sync root</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginGetFileVersions(AsyncCallback asyncCallback,
            object asyncCallbackState,
            string fileServerId,
            int timeoutMilliseconds,
            FilePath pathToFile)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetFileVersions(asyncCallback, asyncCallbackState, fileServerId, timeoutMilliseconds, pathToFile);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        /// <summary>
        /// Asynchronously starts querying the server for all versions of a given file
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">User state to pass when firing async callback</param>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Full path to the file where it would be placed locally within the sync root</param>
        /// <param name="includeDeletedVersions">(optional) whether to include file versions which are deleted</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginGetFileVersions(AsyncCallback asyncCallback,
            object asyncCallbackState,
            string fileServerId,
            int timeoutMilliseconds,
            FilePath pathToFile,
            bool includeDeletedVersions)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetFileVersions(asyncCallback, asyncCallbackState, fileServerId, timeoutMilliseconds, pathToFile, includeDeletedVersions);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        /// <summary>
        /// Finishes querying for all versions of a given file if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting undoing the deletion</param>
        /// <param name="result">(output) The result from undoing the deletion</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetFileVersions(IAsyncResult asyncResult, out GetFileVersionsResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndGetFileVersions(asyncResult, out result);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        /// <summary>
        /// Queries the server for all versions of a given file
        /// </summary>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFileVersions(string fileServerId, int timeoutMilliseconds, out JsonContracts.FileVersions response)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.GetFileVersions(fileServerId, timeoutMilliseconds, out response);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        /// <summary>
        /// Queries the server for all versions of a given file
        /// </summary>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="includeDeletedVersions">(optional) whether to include file versions which are deleted</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFileVersions(string fileServerId, int timeoutMilliseconds, out JsonContracts.FileVersions response, bool includeDeletedVersions)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.GetFileVersions(fileServerId, timeoutMilliseconds, out response, includeDeletedVersions);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        /// <summary>
        /// Queries the server for all versions of a given file
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Full path to the file where it would be placed locally within the sync root</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFileVersions(int timeoutMilliseconds, FilePath pathToFile, out JsonContracts.FileVersions response)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.GetFileVersions(timeoutMilliseconds, pathToFile, out response);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        /// <summary>
        /// Queries the server for all versions of a given file
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Full path to the file where it would be placed locally within the sync root</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="includeDeletedVersions">(optional) whether to include file versions which are deleted</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFileVersions(int timeoutMilliseconds, FilePath pathToFile, out JsonContracts.FileVersions response, bool includeDeletedVersions)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.GetFileVersions(timeoutMilliseconds, pathToFile, out response, includeDeletedVersions);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        /// <summary>
        /// Queries the server for all versions of a given file
        /// </summary>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Full path to the file where it would be placed locally within the sync root</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFileVersions(string fileServerId, int timeoutMilliseconds, FilePath pathToFile, out JsonContracts.FileVersions response)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.GetFileVersions(fileServerId, timeoutMilliseconds, pathToFile, out response);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        /// <summary>
        /// Queries the server for all versions of a given file
        /// </summary>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Full path to the file where it would be placed locally within the sync root</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="includeDeletedVersions">(optional) whether to include file versions which are deleted</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFileVersions(string fileServerId, int timeoutMilliseconds, FilePath pathToFile, out JsonContracts.FileVersions response, bool includeDeletedVersions)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.GetFileVersions(fileServerId, timeoutMilliseconds, pathToFile, out response, includeDeletedVersions);
        }
        #endregion

        #region AllImageItems (Gets image items from this syncbox)

        /// <summary>
        /// Asynchronously starts querying image items from the syncbox.  The resulting set of image items is returned
        /// via the completion callback.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginAllImageItems(AsyncCallback asyncCallback, object asyncCallbackUserState, long pageNumber, long itemsPerPage)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginAllImageItems(asyncCallback, asyncCallbackUserState, pageNumber, itemsPerPage);
        }

        /// <summary>
        /// Finishes getting image items from the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndAllImageItems(IAsyncResult asyncResult, out SyncboxAllImageItemsResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndAllImageItems(asyncResult, out result);
        }

        /// <summary>
        /// Query image items from the syncbox.
        /// </summary>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <param name="items">The resulting file items.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError AllImageItems(long pageNumber, long itemsPerPage, out CLFileItem[] items)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.AllImageItems(pageNumber, itemsPerPage, out items);
        }

        #endregion  // end AllImageItems (Gets image items from this syncbox)

        #region AllVideoItems (Gets video items from this syncbox)

        /// <summary>
        /// Asynchronously starts querying video items from the syncbox.  The resulting set of items is returned
        /// via the completion callback.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginAllVideoItems(AsyncCallback asyncCallback, object asyncCallbackUserState, long pageNumber, long itemsPerPage)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginAllVideoItems(asyncCallback, asyncCallbackUserState, pageNumber, itemsPerPage);
        }

        /// <summary>
        /// Finishes getting video items from the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndAllVideoItems(IAsyncResult asyncResult, out SyncboxAllVideoItemsResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndAllVideoItems(asyncResult, out result);
        }

        /// <summary>
        /// Query video items from the syncbox.
        /// </summary>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <param name="items">The resulting file items.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError AllVideoItems(long pageNumber, long itemsPerPage, out CLFileItem[] items)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.AllVideoItems(pageNumber, itemsPerPage, out items);
        }

        #endregion  // end AllVideoItems (Gets video items from this syncbox)

        #region AllAudioItems (Gets audio items from this syncbox)

        /// <summary>
        /// Asynchronously starts querying audio items from the syncbox.  The resulting set of items is returned
        /// via the completion callback.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginAllAudioItems(AsyncCallback asyncCallback, object asyncCallbackUserState, long pageNumber, long itemsPerPage)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginAllAudioItems(asyncCallback, asyncCallbackUserState, pageNumber, itemsPerPage);
        }

        /// <summary>
        /// Finishes getting audio items from the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndAllAudioItems(IAsyncResult asyncResult, out SyncboxAllAudioItemsResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndAllAudioItems(asyncResult, out result);
        }

        /// <summary>
        /// Query audio items from the syncbox.
        /// </summary>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <param name="items">The returned items.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError AllAudioItems(long pageNumber, long itemsPerPage, out CLFileItem[] items)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.AllAudioItems(pageNumber, itemsPerPage, out items);
        }

        #endregion  // end AllAudioItems (Gets audio items from this syncbox)

        #region AllDocumentItems (Gets document items from this syncbox)

        /// <summary>
        /// Asynchronously starts querying document items from the syncbox.  The resulting set of items is returned
        /// via the completion callback.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginAllDocumentItems(AsyncCallback asyncCallback, object asyncCallbackUserState, long pageNumber, long itemsPerPage)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginAllDocumentItems(asyncCallback, asyncCallbackUserState, pageNumber, itemsPerPage);
        }

        /// <summary>
        /// Finishes getting document items from the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndAllDocumentItems(IAsyncResult asyncResult, out SyncboxAllDocumentItemsResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndAllDocumentItems(asyncResult, out result);
        }

        /// <summary>
        /// Query document items from the syncbox.
        /// </summary>
        /// <param name="completionCallback">Callback method to fire when a page of items is complete.  Returns the result.</param>
        /// <param name="completionCallbackUserState">User state to be passed whenever the completion callback above is fired.</param>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <param name="items">(output) The resulting file items.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError AllDocumentItems(long pageNumber, long itemsPerPage, out CLFileItem[] items)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.AllDocumentItems(pageNumber, itemsPerPage, out items);
        }

        #endregion  // end AllDocumentItems (Gets document items from this syncbox)

        #region AllPresentationItems (Gets presentation items from this syncbox)

        /// <summary>
        /// Asynchronously starts querying presentation items from the syncbox.  The resulting set of items is returned
        /// via the completion callback.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginAllPresentationItems(AsyncCallback asyncCallback, object asyncCallbackUserState, long pageNumber, long itemsPerPage)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginAllPresentationItems(asyncCallback, asyncCallbackUserState, pageNumber, itemsPerPage);
        }

        /// <summary>
        /// Finishes getting presentation items from the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndAllPresentationItems(IAsyncResult asyncResult, out SyncboxAllPresentationItemsResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndAllPresentationItems(asyncResult, out result);
        }

        /// <summary>
        /// Query presentation items from the syncbox.
        /// </summary>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <param name="items">(output) The resulting file items.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError AllPresentationItems(long pageNumber, long itemsPerPage, out CLFileItem[] items)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.AllPresentationItems(pageNumber, itemsPerPage, out items);
        }

        #endregion  // end AllPresentationItems (Gets presentation items from this syncbox)

        #region AllPlainTextItems (Gets text items from this syncbox)

        /// <summary>
        /// Asynchronously starts querying text items from the syncbox.  The resulting set of items is returned
        /// via the completion callback.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginAllPlainTextItems(AsyncCallback asyncCallback, object asyncCallbackUserState, long pageNumber, long itemsPerPage)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginAllPlainTextItems(asyncCallback, asyncCallbackUserState, pageNumber, itemsPerPage);
        }

        /// <summary>
        /// Finishes getting text items from the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndAllPlainTextItems(IAsyncResult asyncResult, out SyncboxAllTextItemsResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndAllPlainTextItems(asyncResult, out result);
        }

        /// <summary>
        /// Query text items from the syncbox.
        /// </summary>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <param name="items">(output) The resulting file items.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError AllPlainTextItems(long pageNumber, long itemsPerPage, out CLFileItem[] items)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.AllPlainTextItems(pageNumber, itemsPerPage, out items);
        }

        #endregion  // end AllTextItems (Gets text items from this syncbox)

        #region AllArchiveItems (Gets archive items from this syncbox)

        /// <summary>
        /// Asynchronously starts querying archive items from the syncbox.  The resulting set of items is returned
        /// via the completion callback.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginAllArchivetItems(AsyncCallback asyncCallback, object asyncCallbackUserState, long pageNumber, long itemsPerPage)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginAllArchiveItems(asyncCallback, asyncCallbackUserState, pageNumber, itemsPerPage);
        }

        /// <summary>
        /// Finishes getting archive items from the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndAllArchiveItems(IAsyncResult asyncResult, out SyncboxAllArchiveItemsResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndAllArchiveItems(asyncResult, out result);
        }

        /// <summary>
        /// Query archive items from the syncbox.
        /// </summary>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <param name="items">(output) The resulting file items.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError AllArchiveItems(long pageNumber, long itemsPerPage, out CLFileItem[] items)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.AllArchiveItems(pageNumber, itemsPerPage, out items);
        }

        #endregion  // end AllArchiveItems (Gets archive items from this syncbox)

        #region AllItemsOfTypes (Get file items with various extensions from this syncbox)

        /// <summary>
        /// Asynchronously starts retrieving the <CLFileItems>s of all of the file items contained in the syncbox that have the specified file extensions.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <param name="extensions">The array of file extensions the item type should belong to. I.E txt, jpg, pdf, etc.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginAllItemsOfTypes(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            long pageNumber,
            long itemsPerPage,
            params string[] extensions)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginAllItemsOfTypes(asyncCallback, asyncCallbackUserState, pageNumber, itemsPerPage, extensions);
        }

        /// <summary>
        /// Finishes retrieving the <CLFileItems>s of all of the file items contained in the syncbox that have the specified file extensions, 
        /// if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndAllItemsOfTypes(IAsyncResult asyncResult, out SyncboxAllItemsOfTypesResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndAllItemsOfTypes(asyncResult, out result);
        }

        /// <summary>
        /// Retrieves the <CLFileItems>s of all of the file items contained in the syncbox that have the specified file extensions.
        /// </summary>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <param name="extensions">The array of file extensions the item type should belong to. I.E txt, jpg, pdf, etc.</param>
        /// <param name="items">(output) The resulting file items.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError AllItemsOfTypes(long pageNumber, long itemsPerPage, out CLFileItem[] items, params string[] extensions)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.AllItemsOfTypes(pageNumber, itemsPerPage, out items, extensions);
        }

        #endregion  // end AllItemsOfTypes (Get file items with various extensions from this syncbox)

        #region RecentFilesSinceDate (Retrieves the specified number of recently modified <CLFileItems>s.)
        /// <summary>
        /// Asynchronously starts retrieving the recently modified files (<CLFileItems>s) from the syncbox since a particular date.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <param name="sinceDate">(optional) null to retrieve all of the recents, or specify a date to retrieve items from that date forward.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginRecentFilesSinceDate(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            long pageNumber,
            long itemsPerPage,
            Nullable<DateTime> sinceDate = null)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginRecentFilesSinceDate(asyncCallback, asyncCallbackUserState, pageNumber, itemsPerPage, sinceDate);
        }

        /// <summary>
        /// Finishes retrieving recent file items from the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndRecentFilesSinceDate(IAsyncResult asyncResult, out SyncboxRecentFilesSinceDateResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndRecentFilesSinceDate(asyncResult, out result);
        }

        /// <summary>
        /// Rretrieve the recently modified files (<CLFileItems>s) from the syncbox since a particular date.
        /// </summary>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <param name="items">(output) The retrieved items.</param>
        /// <param name="sinceDate">(optional) null to retrieve all of the recents, or specify a date to retrieve items from that date forward.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError RecentFilesSinceDate(long pageNumber, long itemsPerPage, out CLFileItem[] items, Nullable<DateTime> sinceDate = null)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.RecentFilesSinceDate(pageNumber, itemsPerPage, out items, sinceDate);
        }

        #endregion  // end RecentFilesSincDate (Retrieves the specified number of recently modified <CLFileItems>s.)

        #region RecentFiles (Retrieves the specified number of recently modified <CLFileItems>s.)
        /// <summary>
        /// Asynchronously starts retrieving up to the given number of recently modified syncbox files.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="returnLimit">The maximum number of file items to retrieve.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginRecentFiles(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            long returnLimit)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginRecentFiles(asyncCallback, asyncCallbackUserState, returnLimit);
        }

        /// <summary>
        /// Finishes retrieving recent file items from the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndRecentFiles(IAsyncResult asyncResult, out SyncboxRecentFilesResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndRecentFiles(asyncResult, out result);
        }

        /// <summary>
        /// Retrieve up to the given number of recently modified syncbox files.
        /// </summary>
        /// <param name="returnLimit">The maximum number of file items to retrieve.</param>
        /// <param name="items">(output) The retrieved items.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError RecentFiles(long returnLimit, out CLFileItem[] items)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.RecentFiles(returnLimit, out items);
        }

        #endregion  // end RecentFiles (Retrieves the specified number of recently modified <CLFileItems>s.)

        #region GetDataUsage (get the usage information for this syncbox from the cloud)
        /// <summary>
        /// Asynchronously starts getting the usage information for this syncbox from the cloud.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when operation completes</param>
        /// <param name="asyncCallbackUserState">User state to pass as a parameter when firing async callback</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginGetDataUsage(AsyncCallback asyncCallback, object asyncCallbackUserState)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetDataUsage(
                asyncCallback, 
                asyncCallbackUserState,
                new Action<JsonContracts.SyncboxUsageResponse, object>(OnGetDataUsageCompletion),
                this);
        }

        /// <summary>
        /// Finishes getting syncbox usage if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the asynchronous request.</param>
        /// <param name="result">(output) The result from the asynchronous request.</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetDataUsage(IAsyncResult asyncResult, out SyncboxUsageResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndGetDataUsage(asyncResult, out result);
        }

        /// <summary>
        /// Queries the cloud for syncbox usage information.  The current syncbox properties are updated with the result.  This method is synchronous.
        /// </summary>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetDataUsage()
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.GetDataUsage(new Action<JsonContracts.SyncboxUsageResponse, object>(OnGetDataUsageCompletion), this);
        }

        /// <summary>
        /// Called back when the HTTP request completes.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="userState"></param>
        private void OnGetDataUsageCompletion(JsonContracts.SyncboxUsageResponse response, object userState)
        {
            // Update this object's properties atomically.
            this._propertyChangeLocker.EnterWriteLock();
            try
            {
                this._storageQuota = response.Limit;
                this._quotaUsage = response.Local;
            }
            finally
            {
                this._propertyChangeLocker.ExitWriteLock();
            }
        }
        #endregion  // end GetDataUsage (get the usage information for this syncbox from the cloud)

        #region UpdateSyncboxExtendedMetadata
        /// <summary>
        /// Asynchronously updates the extended metadata on a sync box
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when operation completes</param>
        /// <param name="asyncCallbackState">User state to pass when firing async callback</param>
        /// <param name="metadata">string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        internal IAsyncResult BeginUpdateSyncboxExtendedMetadata<T>(AsyncCallback asyncCallback,
            object asyncCallbackState,
            IDictionary<string, T> metadata,
            int timeoutMilliseconds)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginUpdateSyncboxExtendedMetadata(asyncCallback, asyncCallbackState, metadata, timeoutMilliseconds);
        }

        /// <summary>
        /// Asynchronously updates the extended metadata on a sync box
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when operation completes</param>
        /// <param name="asyncCallbackState">User state to pass when firing async callback</param>
        /// <param name="metadata">string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        internal IAsyncResult BeginUpdateSyncboxExtendedMetadata(AsyncCallback asyncCallback,
            object asyncCallbackState,
            JsonContracts.MetadataDictionary metadata,
            int timeoutMilliseconds)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginUpdateSyncboxExtendedMetadata(asyncCallback, asyncCallbackState, metadata, timeoutMilliseconds);
        }

        /// <summary>
        /// Finishes updating the extended metadata on a sync box if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting updating extended metadata</param>
        /// <param name="result">(output) The result from updating extended metadata</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndUpdateSyncboxExtendedMetadata(IAsyncResult asyncResult, out SyncboxUpdateExtendedMetadataResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndUpdateSyncboxExtendedMetadata(asyncResult, out result);
        }

        /// <summary>
        /// Updates the extended metadata on a sync box
        /// </summary>
        /// <param name="metadata">string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError UpdateSyncboxExtendedMetadata<T>(IDictionary<string, T> metadata, int timeoutMilliseconds, out JsonContracts.SyncboxResponse response)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.UpdateSyncboxExtendedMetadata(metadata, timeoutMilliseconds, out response);
        }

        /// <summary>
        /// Updates the extended metadata on a sync box
        /// </summary>
        /// <param name="metadata">string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError SyncboxUpdateExtendedMetadata(JsonContracts.MetadataDictionary metadata, int timeoutMilliseconds, out JsonContracts.SyncboxResponse response)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.UpdateSyncboxExtendedMetadata(metadata, timeoutMilliseconds, out response);
        }
        #endregion

        #region UpdateStoragePlan (changes the storage plan associated with this syncbox in the syncbox)
        /// <summary>
        /// Asynchronously updates the storage plan for a syncbox in the syncbox.
        /// Updates this object's StoragePlanId property.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when operation completes</param>
        /// <param name="asyncCallbackUserState">User state to pass as a parameter when firing async callback</param>
        /// <param name="storagePlan">The new storage plan to use for this syncbox)</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginUpdateStoragePlan(AsyncCallback asyncCallback, object asyncCallbackUserState, CLStoragePlan storagePlan)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginUpdateStoragePlan(
                asyncCallback, 
                asyncCallbackUserState, 
                new Action<JsonContracts.SyncboxUpdateStoragePlanResponse, object>(OnUpdateStoragePlanCompletion), 
                this,
                ReservedForActiveSync,
                storagePlan);
        }

        /// <summary>
        /// Finishes updating the storage plan for this syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) The result from completing the request</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndUpdateStoragePlan(IAsyncResult asyncResult, out SyncboxUpdateStoragePlanResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndUpdateStoragePlan(asyncResult, out result);
        }

        /// <summary>
        /// Updates the storage plan for a syncbox in the syncbox.  This is a synchronous method.
        /// Updates this object's StoragePlanId property.
        /// </summary>
        /// <param name="storagePlan">The storage plan to set (new storage plan to use for this syncbox)</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError UpdateStoragePlan(CLStoragePlan storagePlan)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.UpdateStoragePlan(
                new Action<JsonContracts.SyncboxUpdateStoragePlanResponse, object>(OnUpdateStoragePlanCompletion), 
                this,
                ReservedForActiveSync,
                storagePlan);
        }

        /// <summary>
        /// Called back when the HTTP request completes.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="userState"></param>
        private void OnUpdateStoragePlanCompletion(JsonContracts.SyncboxUpdateStoragePlanResponse response, object userState)
        {
            // Update this object's properties atomically.
            this._propertyChangeLocker.EnterWriteLock();
            try
            {
                this._storagePlanId = (long)response.Syncbox.PlanId;
                this._storageQuota = response.Syncbox.StorageQuota;
            }
            finally
            {
                this._propertyChangeLocker.ExitWriteLock();
            }
        }
        #endregion  // end (changes the storage plan associated with this syncbox in the syncbox)

        #region UpdateFriendlyName (change the friendly name of this syncbox)
        /// <summary>
        /// Asynchronously starts changing the friendly name of this syncbox.  Updates the information in this syncbox object.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="friendlyName">The new friendly name of this syncbox)</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginUpdateFriendlyName<T>(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            string friendlyName)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginUpdateFriendlyName(
                asyncCallback,
                asyncCallbackUserState,
                ReservedForActiveSync,
                new Action<JsonContracts.SyncboxResponse, object>(OnUpdateFriendlyNameCompletion),
                this,
                friendlyName);
        }

        /// <summary>
        /// Finishes changing the friendly name of this syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndUpdateFriendlyName(IAsyncResult asyncResult, out SyncboxUpdateFriendlyNameResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndUpdateFriendlyName(asyncResult, out result);
        }

        /// <summary>
        /// Changes the friendly name of this syncbox.  Updates the information in this syncbox object.
        /// </summary>
        /// <param name="friendlyName">The new friendly name of this syncbox)</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError UpdateFriendlyName<T>(
            string friendlyName)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.UpdateFriendlyName(
                ReservedForActiveSync,
                new Action<JsonContracts.SyncboxResponse, object>(OnUpdateFriendlyNameCompletion),
                this,
                friendlyName);
        }

        /// <summary>
        /// Called back when the HTTP request completes.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="userState"></param>
        private void OnUpdateFriendlyNameCompletion(JsonContracts.SyncboxResponse response, object userState)
        {
            // Update this object's properties atomically.
            this._propertyChangeLocker.EnterWriteLock();
            try
            {
                this._friendlyName = response.Syncbox.FriendlyName;
            }
            finally
            {
                this._propertyChangeLocker.ExitWriteLock();
            }
        }
        #endregion  // end UpdateFriendlyName (change the friendly name of this syncbox)

        #region GetCurrentStatus (update the status of this syncbox from the cloud)
        /// <summary>
        /// Asynchronously gets the status of this Syncbox.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when operation completes</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginGetCurrentStatus(AsyncCallback asyncCallback, object asyncCallbackUserState)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetCurrentStatus(asyncCallback, asyncCallbackUserState, new Action<JsonContracts.SyncboxStatusResponse, object>(OnStatusCompletion), this);
        }
        
        /// <summary>
        /// Finishes the asynchronous request, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error).
        /// The local object status is updated with the server results.
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) The result from the request</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetCurrentStatus(IAsyncResult asyncResult, out SyncboxStatusResult result)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndGetCurrentStatus(asyncResult, out result);
        }

        /// <summary>
        /// Gets the status of this Syncbox.  This is a synchronous method.
        /// The local object status is updated with the server results.
        /// </summary>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetCurrentStatus()
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.GetCurrentStatus(new Action<JsonContracts.SyncboxStatusResponse, object>(OnStatusCompletion), null);
        }


        /// <summary>
        /// Called back when the HTTP request completes.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="userState"></param>
        private void OnStatusCompletion(JsonContracts.SyncboxStatusResponse response, object userState)
        {
            this._propertyChangeLocker.EnterWriteLock();
            try
            {
                this._friendlyName = response.Syncbox.FriendlyName;
                this._storagePlanId = (long)response.Syncbox.PlanId;
                this._createdDate = (DateTime)response.Syncbox.CreatedAt;
                this._storageQuota = response.Syncbox.StorageQuota;
            }
            finally
            {
                this._propertyChangeLocker.ExitWriteLock();
            }
        }
        #endregion  // end GetCurrentStatus (update the status of this syncbox from the cloud)

        /// <summary>
        /// Initializes a CLSyncbox which was created by the SDK. For example, ListAllSyncboxes allocates instances of CLSyncbox, but does not initialize them for usage.
        /// </summary>
        /// <param name="path">(optional) The full path of the folder on disk to associate with this syncbox.  The path is used only for live sync.</param>
        /// <returns>Returns any error that occurred, or null</returns>
        /// <remarks>Each instance of CLSyncbox can only be initialized once, either by AllocAndInit or by InitWithPath.</remarks>
        public CLError InitWithPath(string path = null)
        {
            CheckDisposed();

            // Fix up the path.  Use String.Empty if the user passed null.
            if (path == null)
            {
                path = String.Empty;
            }

            try
            {
                InitializeInternal(path, shouldUpdateSyncboxStatusFromServer: true);
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }

        #endregion  // end Public Instance HTTP REST Methods

        #region Private Instance Support Functions

        /// <summary>
        /// Get this syncbox's instance variables which were set via UpdatePathInternal.  Throws if anything is null.
        /// </summary>
        /// <param name="path">(output) The syncbox path.</param>
        /// <param name="httpRestClient">(output) The HTTP REST client.</param>
        private void GetInstanceVariables(out string path, out CLHttpRest httpRestClient)
        {
            if (setPathLocker != null)
            {
                Monitor.Enter(setPathLocker);
            }
            try
            {
                if (setPathHolder == null
                    || setPathHolder.Path == null
                    || setPathHolder.HttpRestClient == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.Syncbox_BadPath, Resources.ExceptionCLSyncboxPathNotSet);
                }

                path = setPathHolder.Path;
                httpRestClient = setPathHolder.HttpRestClient;
            }
            catch (Exception ex)
            {
                path = null;
                httpRestClient = null;
                throw ex;
            }
            finally
            {
                if (setPathLocker != null)
                {
                    Monitor.Exit(setPathLocker);
                }
            }
        }

        /// <summary>
        /// Get this syncbox's HTTP REST instance.  Throws if null.
        /// </summary>
        /// <param name="httpRestClient">(output) The HTTP REST client.</param>
        private void GetInstanceRestClient(out CLHttpRest httpRestClient)
        {
            if (setPathLocker != null)
            {
                Monitor.Enter(setPathLocker);
            }
            try
            {
                if (setPathHolder == null
                    || setPathHolder.HttpRestClient == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.Syncbox_BadPath, Resources.ExceptionSyncboxPathMustBeSetFirst);
                }

                httpRestClient = setPathHolder.HttpRestClient;
            }
            catch (Exception ex)
            {
                httpRestClient = null;
                throw ex;
            }
            finally
            {
                if (setPathLocker != null)
                {
                    Monitor.Exit(setPathLocker);
                }
            }
        }

        /// <summary>
        /// It is possible to create non-functional syncbox instances.  For example, listing all of the syncboxes for a set of credentials via the server constructs
        /// an array of CLSyncbox instances, but the information from the server does not include the local syncbox paths.  The instances are unusable for live sync
        /// in this state and any operation that requires the local syncbox path will throw an error.  It is up to the app to provide the local syncbox path before using the instance
        /// for live sync.  This is done via SetSyncboxPath().  SetSyncboxPath() defers to this internal version.  This function acts like an extension of the constructor, and it
        /// if called by constructors to create the CLHttpRest client, fill in missing information from the server, and to create an instance of CLSyncEngine that will
        /// be used by this syncbox instance.
        /// </summary>
        private void InitializeInternal(string path, bool shouldUpdateSyncboxStatusFromServer)
        {
            if (path == null)
            {
                throw new CLArgumentNullException(CLExceptionCode.Syncbox_BadPath, Resources.ExceptionSyncboxPathMustNotBeNull);
            }

            if (setPathLocker != null)
            {
                Monitor.Enter(setPathLocker);
            }

            // seperate this try/catch just for throwing the Syncbox_PathAlreadySet code since cleanup on the following try/catch should not cleanup an already initialized syncbox
            if (setPathHolder != null)
            {
                throw new CLException(CLExceptionCode.Syncbox_PathAlreadySet, Resources.ExceptionOnDemandSyncboxPathAlreadySet);
            }

            try
            {
                //TODO: Remove this when the sync engine support case insensitive paths.
                // This was required because OSD code was providing paths that started with a lower case drive letter.
                if (path.Length >= 2 && path[1] == ':')
                {
                    path = char.ToUpper(path[0]) + path.Substring(1);
                }

                int nOutTooLongChars;
                CLError errorPathTooLong = Helpers.CheckSyncboxPathLength(path, out nOutTooLongChars);
                if (errorPathTooLong != null)
                {
                    throw new CLArgumentException(CLExceptionCode.Syncbox_LongPath, string.Format(Resources.ExceptionSyncboxPathTooLongMsg0, nOutTooLongChars), errorPathTooLong.Exceptions);
                }

                // The syncbox path may be specified as null, which is replaced with String.Empty.  The path is actually not used for the On Demand case.
                // If the path is anything other than String.Empty, check for a valid syncbox root path.
                if (path != String.Empty)
                {
                    CLError errorBadPath = Helpers.CheckForBadPath(path);
                    if (errorBadPath != null)
                    {
                        throw new CLArgumentException(CLExceptionCode.Syncbox_BadPath, Resources.ExceptionSyncboxPathInvalidCharacters, errorBadPath.Exceptions);
                    }
                }

                // Set the path early because the CLHttpRest factory needs it.
                setPathHolder = new SetPathProperties(path, null);

                // Create an instance of the CLHttpRest client for this syncbox
                CLHttpRest localRestClient;
                // Create the http rest client
                _trace.writeToLog(9, Resources.CLSyncboxStartCreateRestClient);
                CLError createRestClientError = CLHttpRest.CreateAndInitialize(
                    syncbox: this,
                    client: out localRestClient,
                    getNewCredentialsCallback: _getNewCredentialsCallback,
                    getNewCredentialsCallbackUserState: _getNewCredentialsCallbackUserState);
                if (createRestClientError != null)
                {
                    _trace.writeToLog(1,
                        Resources.CLSyncboxConstructionErrorMsg0Code1,
                        createRestClientError.PrimaryException.Message,
                        createRestClientError.PrimaryException.Code);

                    throw new CLException(CLExceptionCode.Syncbox_CreateRestClient,
                        Resources.CLSyncboxErrorCreatingRestHTTPClient,
                        createRestClientError.Exceptions);
                }
                if (localRestClient == null)
                {
                    string nullRestClient = Resources.TraceSyncboxInitializeInternalUnknownErrorCreatingRestClient;
                    _trace.writeToLog(1, Resources.CLSyncboxConstructionErrorMsg0, nullRestClient);

                    throw new CLNullReferenceException(CLExceptionCode.Syncbox_CreateRestClient, nullRestClient);
                }
                
                // after removing CLSyncEngine from SetPathProperties, can now set the final setPathHolder here
                setPathHolder = new SetPathProperties(path, localRestClient);

                if (shouldUpdateSyncboxStatusFromServer)
                {
                    // We need to validate the syncbox ID with the server with these credentials.  We will also retrieve the other syncbox
                    // properties from the server and set them into this local object's properties.
                    CLError errorFromStatus = this.GetCurrentStatus();
                    if (errorFromStatus != null)
                    {
                        switch (errorFromStatus.PrimaryException.Code)
                        {
                            case CLExceptionCode.Http_NotAuthorized:
                                throw new CLException(CLExceptionCode.Syncbox_BadCredentials, Resources.ExceptionSyncboxErrorGettingStatusDueToBadCredentials, errorFromStatus.Exceptions);

                            case CLExceptionCode.Http_NotAuthorizedExpiredCredentials:
                                throw new CLException(CLExceptionCode.Syncbox_ExpiredCredentials, Resources.ExceptionSyncboxErrorGettingStatusDueToExpiredCredentials, errorFromStatus.Exceptions);

                            case CLExceptionCode.Http_NotAuthorizedSyncboxNotFound:
                                throw new CLException(CLExceptionCode.Syncbox_NotFoundForId, Resources.ExceptionSyncboxErrorGettingStatusDueToSyncboxNotFound, errorFromStatus.Exceptions);

                            default:
                                throw new CLException(CLExceptionCode.Syncbox_InitialStatus, Resources.ExceptionSyncboxStartStatus, errorFromStatus.Exceptions);
                        }
                    }

                    // when server includes quota information in syncbox\status (called via GetCurrentStatus above), then quota needs to be updated in this object;
                    // but since the server is missing that information, fill it in via syncbox/usage here:
                    this.GetDataUsage();
                }
            }
            catch (Exception ex)
            {
                // cleanup to allow a repeated attempt to initialize
                setPathHolder = null;

                throw ex;
            }
            finally
            {
                if (setPathLocker != null)
                {
                    Monitor.Exit(setPathLocker);
                }
            }
        }
        // \cond
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] //Hide From Intellisense
        public bool ReservedForActiveSync
        {
            get
            {
                lock (_reservedForActiveSync)
                {
                    return _reservedForActiveSync.Value;
                }
            }
        }
        // \endcond

        // \cond
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] //Hide From Intellisense
        public bool TryReserveForActiveSync()
        {
            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            if (!httpRestClient.IsModifyingSyncboxViaPublicAPICalls)
            {
                lock (_reservedForActiveSync)
                {
                    if (!_reservedForActiveSync.Value)
                    {
                        _reservedForActiveSync.Value = true;
                        return true;
                    }
                }
            }
            return false;
        }
        // \endcond
        
        // \cond
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] //Hide From Intellisense
        public void ResetReserveForActiveSync()
        {
            lock (_reservedForActiveSync)
            {
                _reservedForActiveSync.Value = false;
            }
        }
        private readonly GenericHolder<bool> _reservedForActiveSync = new GenericHolder<bool>(false);

        /// <summary>
        /// A serious notification error has occurred. Push notification is no longer functioning.
        /// </summary>
        /// <param name="sender">The sending object.</param>
        /// <param name="e">Arguments including the manual poll and/or web sockets errors (possibly aggregated).</param>
        // \endcond

        // \cond
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] //Hide From Intellisense
        public void OnPushNotificationConnectionError(object sender, NotificationErrorEventArgs e)
        {
            try
            {
                if (e.ErrorStillDisconnectedPing == null)
                {
                    if (e.ErrorWebSockets == null)
                    {
                        _trace.writeToLog(1, Resources.TraceSyncboxOnPushNotifConnError);
                        try
                        {
                            throw new CLInvalidOperationException(CLExceptionCode.General_Invalid, Resources.ExceptionSyncboxPushConnErrorWithoutError);
                        }
                        catch (Exception ex)
                        {
                            ((CLError)ex).Log(_copiedSettings.TraceLocation, _copiedSettings.LogErrors);
                        }
                    }
                    else
                    {
                        _trace.writeToLog(1, Resources.TraceSyncboxOnPushConnErrorWebSocketErrMsg0Msg1, e.ErrorWebSockets.PrimaryException.Message, e.ErrorWebSockets.PrimaryException.Code);
                        e.ErrorWebSockets.Log(_copiedSettings.TraceLocation, _copiedSettings.LogErrors);
                    }
                }
                else if (e.ErrorWebSockets == null)
                {
                    _trace.writeToLog(1, Resources.TraceSyncboxOnPushConnErrorManualPollErrMsg0Msg1, e.ErrorStillDisconnectedPing.PrimaryException.Message, e.ErrorStillDisconnectedPing.PrimaryException.Code);
                    e.ErrorStillDisconnectedPing.Log(_copiedSettings.TraceLocation, _copiedSettings.LogErrors);
                }
                else
                {
                    _trace.writeToLog(1, Resources.TraceSyncboxOnPushConnErrorsAndCodesMsg0Msg1Msg2Msg3, e.ErrorWebSockets.PrimaryException.Message, e.ErrorWebSockets.PrimaryException.Code, e.ErrorStillDisconnectedPing.PrimaryException.Message, e.ErrorStillDisconnectedPing.PrimaryException.Code);
                    e.ErrorWebSockets.Log(_copiedSettings.TraceLocation, _copiedSettings.LogErrors);
                    e.ErrorStillDisconnectedPing.Log(_copiedSettings.TraceLocation, _copiedSettings.LogErrors);
                }

                // Tell the application
                if (PushNotificationError != null)
                {
                    _trace.writeToLog(1, Resources.TraceSyncboxOnPushConnErrorsNotifyApp);
                    PushNotificationError(this, e);
                }
            }
            catch
            {
            }
        }
        // \endcond
        #endregion  // end Private Instance Support Functions

        #region IDisposable Support

        /// <summary>
        /// Destructor
        /// </summary>
        ~CLSyncbox()
        {
            Dispose(false);
        }

        /// <summary>
        /// Throw an exception if already disposed
        /// </summary>
        private void CheckDisposed()
        {
            if (Disposed)
            {
                throw new CLException(CLExceptionCode.Syncbox_ObjectDisposed, Resources.ExceptionSyncboxObjectDisposed);
            }

            Helpers.CheckHalted();
        }

        /// <summary>
        /// Throw an exception if already disposed
        /// </summary>
        private void CheckDisposed(bool isOneOff)
        {
            if (Disposed)
            {
                throw new CLException(CLExceptionCode.Syncbox_ObjectDisposed, Resources.ExceptionSyncboxObjectDisposed);
            }
            if (!isOneOff)
            {
                Helpers.CheckHalted();
            }
        }

        // Standard IDisposable implementation based on MSDN System.IDisposable
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Standard IDisposable implementation based on MSDN System.IDisposable
        private void Dispose(bool disposing)
        {
            if (!this.Disposed)
            {
                // Run dispose on inner managed objects based on disposing condition
                if (disposing)
                {
                    // Stop live sync
                    StopLiveSync();

                    // cleanup inner managed objects
                    if (_propertyChangeLocker != null)
                    {
                        try
                        {
                            _propertyChangeLocker.Dispose();
                        }
                        catch
                        {
                        }
                    }
                }

                // Dispose local unmanaged resources last

                Disposed = true;
            }
        }
        #endregion // end IDisposable Support
    }

    #endregion  // end Public CLSyncbox Class
}