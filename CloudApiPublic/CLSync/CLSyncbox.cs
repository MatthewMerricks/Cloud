//
// CLSyncbox.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.CLSync;
using Cloud.CLSync.CLSyncboxParameters;
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

namespace Cloud
{
    #region Public Enums
    
    /// <summary>
    /// Status of creation of <see cref="CLSyncbox"/>
    /// </summary>
    public enum CLSyncboxCreationStatus : byte
    {
        Success = 0,
        ErrorNullCredentials = 1,
        ErrorUnknown = 2,
        ErrorCreatingRestClient = 3,
        ErrorSyncboxIdZero = 4,
        ErrorPathNotSpecified = 5,
    }

    #endregion

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
        private readonly Helpers.ReplaceExpiredCredentials _getNewCredentialsCallback = null;
        private readonly object _getNewCredentialsCallbackUserState = null;

        #endregion  // end Private Fields

        #region Internal Properties

        /// <summary>
        /// Contains authentication information required for all communication and services
        /// </summary>
        internal CLCredentials Credentials
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
        /// true: The user is busy in a public API call that is modifying the syncbox.
        /// </summary>
        internal bool IsModifyingSyncboxViaPublicAPICalls
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
                        return false;
                    }
                    return setPathHolder.HttpRestClient.IsModifyingSyncboxViaPublicAPICalls;
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

        #endregion  // end Internal Properties

        #region Public Properties
        
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
        /// The full path on the disk associated with this syncbox.
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
        /// Internal client for passing HTTP REST calls to the server
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public CLHttpRest HttpRestClient
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

        /// <summary>
        /// lock on _startLocker for modifications or retrieval
        /// </summary>
        private CLSyncEngine _syncEngine = null;

        private SetPathProperties setPathHolder;

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

        #endregion

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
        /// <param name="path">(optional) The full path on disk of the folder to associate with this syncbox.</param>
        /// <param name="settings">(optional) The settings to use.</param>
        /// <param name="getNewCredentialsCallback">(optional) The delegate to call for getting new temporary credentials.</param>
        /// <param name="getNewCredentialsCallbackUserState">(optional) The user state to pass to the delegate above.</param>
        private CLSyncbox(
            long syncboxId,
            CLCredentials credentials,
            string path = null,
            ICLSyncSettings settings = null,
            Helpers.ReplaceExpiredCredentials getNewCredentialsCallback = null,
            object getNewCredentialsCallbackUserState = null)
        {
            // check input parameters

            if (syncboxId <= 0)
            {
                throw new ArgumentException("syncboxId must be specified");  //&&&& fix
            }

            if (credentials == null)
            {
                throw new NullReferenceException("Credentials cannot be null");  //&&&& fix
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

            // Initialize trace in case it is not already initialized.
            CLTrace.Initialize(this._copiedSettings.TraceLocation, "Cloud", "log", this._copiedSettings.TraceLevel, this._copiedSettings.LogErrors);
            _trace.writeToLog(1, Resources.CLSyncboxConstructing);

            // Set up the syncbox
            lock (_startLocker)
            {

                // Save the parameters in properties.
                this.Credentials = credentials;
                this._syncboxId = syncboxId;
                this._getNewCredentialsCallback = getNewCredentialsCallback;
                this._getNewCredentialsCallbackUserState = getNewCredentialsCallbackUserState;

                if (path == null)
                {
                    setPathLocker = new object();
                }
                else
                {
                    setPathLocker = null;
                    CLError setPathError = UpdatePathInternal(path, shouldUupdateSyncboxStatusFromServer: true);
                    if (setPathError != null)
                    {
                        throw new CLException(CLExceptionCode.Syncbox_Initializing, "Error initializing the syncbox", setPathError.Exceptions);
                    }
                }
            }
        }

        /// <summary>
        /// Private constructor to create a functional CLSyncbox object from a JsonContracts.Syncbox.
        /// </summary>
        /// <param name="syncboxContract">The syncbox contract to use.</param>
        /// <param name="credentials">The credentials to use.</param>
        /// <param name="path">(optional) The full path on the local disk of the folder to associate with this syncbox.</param>
        /// <param name="settings">(optional) The settings to use.</param>
        /// <param name="getNewCredentialsCallback">(optional) The delegate to call for getting new temporary credentials.</param>
        /// <param name="getNewCredentialsCallbackUserState">(optional) The user state to pass to the delegate above.</param>
        private CLSyncbox(
            JsonContracts.Syncbox syncboxContract,
            CLCredentials credentials,
            string path = null,
            ICLSyncSettings settings = null,
            Helpers.ReplaceExpiredCredentials getNewCredentialsCallback = null,
            object getNewCredentialsCallbackUserState = null)
        {
            CheckDisposed();

            // check input parameters

            if (syncboxContract == null)
            {
                throw new NullReferenceException("syncboxContract must not be null");  //&&&& fix
            }
            if (syncboxContract.Id == null)
            {
                throw new NullReferenceException("syncboxContract Id must not be null");  //&&&& fix
            }
            if (syncboxContract.PlanId == null)
            {
                throw new NullReferenceException("syncboxContract Id must not be null");  //&&&& fix
            }
            if (credentials == null)
            {
                throw new NullReferenceException("credentials must not be null");  //&&&& fix
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

            // Initialize trace in case it is not already initialized.
            CLTrace.Initialize(this._copiedSettings.TraceLocation, "Cloud", "log", this._copiedSettings.TraceLevel, this._copiedSettings.LogErrors);
            _trace.writeToLog(1, "CLSyncbox: Constructing from contract...");

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

                if (path == null)
                {
                    setPathLocker = new object();
                }
                else
                {
                    setPathLocker = null;
                    CLError setPathError = UpdatePathInternal(path, shouldUupdateSyncboxStatusFromServer: false);  // the information in the server response filled in the current syncbox status.
                    if (setPathError != null)
                    {
                        //&&&& Put all strings in resources.
                        throw new CLException(CLExceptionCode.Syncbox_Initializing, "Error initializing the syncbox", setPathError.Exceptions);
                    }
                }
            }
        }

        #endregion  // end Private Constructors

        #region Public CLSyncbox Factory

        /// <summary>
        /// Asynchronously begins the factory process to construct an instance of CLSyncbox, initialize it and fill in its properties from the cloud, and associates the cloud syncbox with a folder on the local disk.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes.  Can be null.</param>
        /// <param name="callbackUserState">Userstate to pass to the callback when it is fired.  Can be null.</param>
        /// <param name="syncboxId">The cloud syncbox ID to use.</param>
        /// <param name="credentials">The credentials to use with this request.</param>
        /// <param name="path">The full path of the folder on disk to associate with this syncbox. If this parameter is null, the syncbox local disk directory will be %USEERPROFILE%\Cloud. </param>
        /// <param name="settings">(optional) settings to use with this method.</param>
        /// <param name="getNewCredentialsCallback">(optional) A delegate which will be called to retrieve a new set of credentials when credentials have expired.</param>
        /// <param name="getNewCredentialsCallbackUserState">(optional) The user state to pass as a parameter to the delegate above.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public static IAsyncResult BeginAllocAndInit(
            AsyncCallback callback,
            object callbackUserState,
            long syncboxId,
            CLCredentials credentials,
            string path,
            ICLSyncSettings settings = null,
            Helpers.ReplaceExpiredCredentials getNewCredentialsCallback = null,
            object getNewCredentialsCallbackUserState = null)
        {
            Helpers.CheckHalted();

            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxAllocAndInitResult>(
                        callback,
                        callbackUserState),
                    syncboxId = syncboxId,
                    credentials = credentials,
                    path = path,
                    settings = settings,
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
        /// <param name="aResult">The asynchronous result provided upon starting asynchronous request.</param>
        /// <param name="result">(output) The result from the asynchronous request.</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public static CLError EndAllocAndInit(IAsyncResult aResult, out SyncboxAllocAndInitResult result)
        {
            Helpers.CheckHalted();
            return Helpers.EndAsyncOperation<SyncboxAllocAndInitResult>(aResult, out result);
        }

        /// <summary>
        /// Creates and initializes a CLSyncbox object which represents a Syncbox in Cloud, and associates the syncbox with a folder on the local disk.
        /// </summary>
        /// <param name="syncboxId">Unique ID of the syncbox generated by Cloud</param>
        /// <param name="credentials">Credentials to use with this request.</param>
        /// <param name="syncbox">(output) Created local object representation of the Syncbox</param>
        /// <param name="path">The full path of the folder on disk to associate with this syncbox. If this parameter is null, the syncbox local disk directory will be %USEERPROFILE%\Cloud. </param>
        /// <param name="settings">(optional) Settings to use with this request</param>
        /// <param name="getNewCredentialsCallback">(optional) A delegate that will be called to provide new credentials when the current credentials token expires.</param>
        /// <param name="getNewCredentialsCallbackUserState">(optional) The user state that will be passed back to the getNewCredentialsCallback delegate.</param>
        /// <returns>Returns any error which occurred during object allocation or initialization, if any, or null.</returns>
        public static CLError AllocAndInit(
            long syncboxId,
            CLCredentials credentials,
            out CLSyncbox syncbox,
            string path,
            ICLSyncSettings settings = null,
            Helpers.ReplaceExpiredCredentials getNewCredentialsCallback = null,
            object getNewCredentialsCallbackUserState = null)
        {
            Helpers.CheckHalted();

            try
            {
                if (Helpers.AllHaltedOnUnrecoverableError)
                {
                    throw new InvalidOperationException("Cannot do anything with the Cloud SDK if Helpers.AllHaltedOnUnrecoverableError is set");  //&&&& fix
                }
                if (path == null)
                {
                    throw new CLArgumentNullException(CLExceptionCode.Syncbox_BadPath, "path must not be null");
                }

                syncbox = new CLSyncbox(
                    syncboxId: syncboxId,
                    credentials: credentials,
                    path: path,
                    settings: settings,
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
        /// <remarks>Note that only SyncMode.CLSyncModeLive is currently supported.</remarks>
        /// <param name="mode">The sync mode to start.</param>
        /// <returns></returns>
        public CLError StartLiveSync(
                CLSyncMode mode,
                System.Threading.WaitCallback syncStatusChangedCallback = null,
                object syncStatusChangedCallbackUserState = null)
        {
            CheckDisposed();

            bool startExceptionLogged = false;

            try
            {
                lock (_startLocker)
                {
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

                    // Create the sync engine for this syncbox instance
                    _syncEngine = new CLSyncEngine(this, debugDependenciesValue, copyDatabaseBetweenChangesValue, debugFileMonitorMemoryValue); // syncbox to sync (contains required settings)

                    try
                    {
                        //// OnDemand mode does not start\stop sync, so it is not a valid CLSyncMode anyways (it was removed from that enumeration)
                        //
                        //if (mode == CLSyncMode.CLSyncModeOnDemand)
                        //{
                        //    throw new CLNotSupportedException(CLExceptionCode.Syncbox_GeneralStart, Resources.ExceptionCLSyncboxBeginSyncOnDemandNotSupported);
                        //}

                        _syncMode = mode;

                        // Start the sync engine
                        CLError syncEngineStartError = _syncEngine.Start(
                            statusUpdated: syncStatusChangedCallback, // called when sync status is updated
                            statusUpdatedUserState: syncStatusChangedCallbackUserState); // the user state passed to the callback above

                        if (syncEngineStartError != null)
                        {
                            _trace.writeToLog(1, "Error starting sync engine. Msg: {0}. Code: {1}.", syncEngineStartError.PrimaryException.Message, syncEngineStartError.PrimaryException.Code);
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
                }
            }
            catch (Exception ex)
            {
                CLError toReturn = ex;
                if (!startExceptionLogged)
                {
                    toReturn.Log(_copiedSettings.TraceLocation, _copiedSettings.LogErrors);
                    _trace.writeToLog(1, "CLSyncbox: StartSync: ERROR.  Exception.  Msg: {0}. Code: {1}.", toReturn.PrimaryException.Message, toReturn.PrimaryException.Code);
                }
                return toReturn;
            }

            return null;
        }

        /// <summary>
        /// Stop syncing.
        /// </summary>
        /// <remarks>Note that after stopping it is possible to call BeginSync() again to restart syncing.</remarks>
        public void StopLiveSync()
        {
            CheckDisposed();

            try
            {
                lock (_startLocker)
                {
                    if (!_isStarted
                        || _syncEngine == null)
                    {
                        return;
                    }

                    try
                    {
                        // Stop the sync engine.
                        _syncEngine.Stop();
                    }
                    catch
                    {
                    }

                    // Remove this engine from the tracking list.
                    _startedSyncEngines.Remove(_syncEngine);

                    _isStarted = false;
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.Log(_copiedSettings.TraceLocation, _copiedSettings.LogErrors);
                _trace.writeToLog(1, "CLSyncbox: StopSync: ERROR.  Exception.  Msg: <{0}>. Code: {1}.", error.PrimaryException.Message, error.PrimaryException.Code);
            }
        }

        /// <summary>
        /// Return true if LiveSync is started.  Otherwise, false.
        /// </summary>
        /// <returns>true: LiveSync is started.</returns>
        public bool IsLiveSyncStarted()
        {
            return _isStarted;
        }

        /// <summary>
        /// Reset sync.  Sync must be stopped before calling this method.  Starting sync after resetting sync will merge the
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
                        _trace.writeToLog(1, "CLSyncbox: ResetLocalCache: ERROR: From syncEngine.SyncReset: Msg: {0}. Code {1}.", resetSyncError.PrimaryException.Message, resetSyncError.PrimaryException.Code);
                        resetSyncError.Log(_copiedSettings.TraceLocation, _copiedSettings.LogErrors);
                        resetSyncErrorLogged = true;
                        throw new CLException(CLExceptionCode.Syncing_Database, "Error resetting syncing database", resetSyncError.Exceptions);
                    }
                }
            }
            catch (Exception ex)
            {
                CLError toReturn = ex;
                if (!resetSyncErrorLogged)
                {
                    toReturn.Log(_copiedSettings.TraceLocation, _copiedSettings.LogErrors);
                    _trace.writeToLog(1, "CLSyncbox: ResetLocalCache: ERROR.  Exception.  Msg: {0}. Code: {1}.", toReturn.PrimaryException.Message, toReturn.PrimaryException.Code);
                }
                return toReturn;
            }

            return null;
        }

        /// <summary>
        /// Output the current status of syncing
        /// </summary>
        /// <returns>Returns any error which occurred in retrieving the sync status, if any</returns>
        public CLError GetSyncboxCurrentStatus(out CLSyncCurrentStatus status)
        {
            CheckDisposed();

            try
            {
                if (Helpers.AllHaltedOnUnrecoverableError)
                {
                    throw new InvalidOperationException(Resources.CLCredentialHelpersAllHaltedOnUnrecoverableErrorIsSet);
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

        /// <summary>l
        /// Call when application is shutting down.
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

        #region CreateSyncbox (create a syncbox in the syncbox)

        /// <summary>
        /// Asynchronously starts creating a new Syncbox in the syncbox.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes.  Can be null.</param>
        /// <param name="callbackUserState">Userstate to pass to the callback when it is fired.  Can be null.</param>
        /// <param name="plan">The storage plan to use with this Syncbox.</param>
        /// <param name="credentials">The credentials to use with this request.</param>
        /// <param name="path">The path on the local disk to associate with this syncbox.</param>
        /// <param name="friendlyName">(optional) The friendly name of the Syncbox.</param>
        /// <param name="settings">(optional) Settings to use with this method.</param>
        /// <param name="getNewCredentialsCallback">(optional) The callback function that will provide new credentials with temporary credentials expire.</param>
        /// <param name="getNewCredentialsCallbackUserState">(optional) The user state that will be passed as a parameter to the callback function above.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public static IAsyncResult BeginCreateSyncbox(
                    AsyncCallback callback,
                    object callbackUserState,
                    CLStoragePlan plan,
                    CLCredentials credentials,
                    string path,
                    string friendlyName = null,
                    ICLSyncSettings settings = null,
                    Helpers.ReplaceExpiredCredentials getNewCredentialsCallback = null,
                    object getNewCredentialsCallbackUserState = null)
        {
            Helpers.CheckHalted();

            // Check the parameters
            if (plan == null)
            {
                throw new ArgumentNullException("plan must not be null");  //&&&& fix
            }
            if (credentials == null)
            {
                throw new ArgumentNullException("credentials must not be null");  //&&&& fix
            }

            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxCreateResult>(
                        callback,
                        callbackUserState),
                    plan = plan,
                    friendlyName = friendlyName,
                    credentials = credentials,
                    settings = settings,
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
                        CLError processError = CreateSyncbox(
                            Data.plan,
                            Data.credentials,
                            path,
                            out response,
                            Data.friendlyName,
                            Data.settings,
                            Data.getNewCredentialsCallback,
                            Data.getNewCredentialsCallbackUserState);

                        Data.toReturn.Complete(
                            new SyncboxCreateResult(
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
        /// Finishes creating a Syncbox in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting creating the syncbox</param>
        /// <param name="result">(output) The result from creating the syncbox</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public static CLError EndCreateSyncbox(IAsyncResult aResult, out CreateSyncboxResult result)
        {
            return Helpers.EndAsyncOperation<CreateSyncboxResult>(aResult, out result);
        }

        /// <summary>
        /// Create a Syncbox in the syncbox for the current application.  This is a synchronous method.
        /// </summary>
        /// <param name="plan">The storage plan to use with this Syncbox.</param>
        /// <param name="credentials">The credentials to use for this request.</param>
        /// <param name="path">The path on the local disk to associate with this syncbox.</param>
        /// <param name="syncbox">(output) Response object from communication</param>
        /// <param name="friendlyName">(optional) The friendly name of the Syncbox.</param>
        /// <param name="settings">(optional) The settings to use with this method</param>
        /// <param name="getNewCredentialsCallback">(optional) The callback function that will provide new credentials with temporary credentials expire.</param>
        /// <param name="getNewCredentialsCallbackUserState">(optional) The user state that will be passed as a parameter to the callback function above.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public static CLError CreateSyncbox(
                    CLStoragePlan plan,
                    CLCredentials credentials,
                    string path,
                    out CLSyncbox syncbox,
                    string friendlyName = null,
                    ICLSyncSettings settings = null,
                    Helpers.ReplaceExpiredCredentials getNewCredentialsCallback = null,
                    object getNewCredentialsCallbackUserState = null)
        {
            Helpers.CheckHalted();

            // try/catch to process the metadata query, on catch return the error
            try
            {
                // Check the input parameters.
                if (plan == null)
                {
                    throw new ArgumentNullException("plan must not be null");  //&&&& fix
                }
                if (credentials == null)
                {
                    throw new ArgumentNullException("credentials must not be null");  //&&&& fix
                }
                if (path == null)
                {
                    throw new CLArgumentNullException(CLExceptionCode.Syncbox_BadPath, "path must not be null");
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
                        FriendlyName = (string.IsNullOrWhiteSpace(friendlyName)
                            ? null
                            : friendlyName),
                        PlanId = plan.Id
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
                    SyncboxId: null, 
                    isOneOff: false);

                // Check the server response.
                if (responseFromServer == null)
                {
                    throw new NullReferenceException("Response from server must not be null");  //&&&& fix
                }
                if (responseFromServer.Syncbox == null)
                {
                    throw new NullReferenceException("Server response syncbox must not be null");  //&&&& fix
                }

                // Convert the response object to a CLSyncbox and return that.
                syncbox =  new CLSyncbox(
                    syncboxContract: responseFromServer.Syncbox,
                    credentials: credentials,
                    path: path,
                    settings: copiedSettings,
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
        #endregion

        #region DeleteSyncbox (delete a syncbox in the syncbox)

        /// <summary>
        /// Asynchronously starts deleting a new Syncbox in the syncbox.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes.  Can be null.</param>
        /// <param name="callbackUserState">Userstate to pass as a parameter to the callback when it is fired.  Can be null.</param>
        /// <param name="syncboxId">The ID of syncbox to delete.</param>
        /// <param name="credentials">The credentials to use with this request.</param>
        /// <param name="settings">(optional) settings to use with this method.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public static IAsyncResult BeginDeleteSyncbox(
            AsyncCallback callback,
            object callbackUserState,
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
                        callback,
                        callbackUserState),
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
                        // declare the specific type of result for this operation
                        JsonContracts.SyncboxDeleteResponse response;
                        // Call the synchronous version of this method.
                        CLError processError = DeleteSyncbox(
                            Data.syncboxId,
                            Data.credentials,
                            out response,
                            Data.settings);

                        Data.toReturn.Complete(
                            new SyncboxDeleteResult(
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
        /// Finishes deleting a Syncbox in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the asynchronous operation</param>
        /// <param name="result">(output) The result from the asynchronous operation</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public static CLError EndDeleteSyncbox(IAsyncResult aResult, out SyncboxDeleteResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxDeleteResult>(aResult, out result);
        }

        /// <summary>
        /// Delete a Syncbox in the syncbox.  This is a synchronous method.
        /// </summary>
        /// <param name="syncboxId">the ID of the syncbox to delete.
        /// <param name="credentials">The credentials to use for this request.</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="settings">(optional) the settings to use with this method</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public static CLError DeleteSyncbox(
                    long syncboxId,
                    CLCredentials credentials,
                    out JsonContracts.SyncboxDeleteResponse response,
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

                // check input parameters
                JsonContracts.SyncboxIdOnly inputBox = new JsonContracts.SyncboxIdOnly()
                    {
                        Id = syncboxId
                    };

                response = Helpers.ProcessHttp<JsonContracts.SyncboxDeleteResponse>(
                    requestContent: inputBox,
                    serverUrl: CLDefinitions.CLPlatformAuthServerURL,
                    serverMethodPath: CLDefinitions.MethodPathAuthDeleteSyncbox,
                    method: Helpers.requestMethod.post,
                    timeoutMilliseconds: copiedSettings.HttpTimeoutMilliseconds,
                    uploadDownload: null, // not an upload nor download
                    validStatusCodes: Helpers.HttpStatusesOkAccepted,
                    CopiedSettings: copiedSettings,
                    Credentials: credentials,
                    SyncboxId: null, 
                    isOneOff: false);

            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.SyncboxDeleteResponse>();
                return ex;
            }
            return null;
        }
        #endregion

        #region ListAllSyncboxesWithCredentials (list syncboxes in the syncbox)

        /// <summary>
        /// Asynchronously starts listing syncboxes in the syncbox.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes.  Can be null.</param>
        /// <param name="callbackUserState">Userstate to pass as a parameter to the callback when it is fired.  Can be null.</param>
        /// <param name="credentials">The credentials to use with this request.</param>
        /// <param name="settings">(optional) settings to use with this method.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public static IAsyncResult BeginListAllSyncboxesWithCredentials(
            AsyncCallback callback,
            object callbackUserState,
            CLCredentials credentials,
            ICLCredentialsSettings settings = null)
        {
            Helpers.CheckHalted();

            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxListResult>(
                        callback,
                        callbackUserState),
                    credentials = credentials,
                    settings = settings
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of result for this operation
                        CLSyncbox [] response;
                        // Call the synchronous version of this method.
                        CLError processError = ListAllSyncboxesWithCredentials(
                            Data.credentials,
                            out response,
                            Data.settings);

                        Data.toReturn.Complete(
                            new SyncboxListResult(
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
        /// Finishes listing syncboxes in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the asynchronous operation</param>
        /// <param name="result">(output) The result from the asynchronous operation</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public static CLError EndListAllSyncboxesWithCredentials(IAsyncResult aResult, out SyncboxListResult result)
        {
            Helpers.CheckHalted();
            return Helpers.EndAsyncOperation<SyncboxListResult>(aResult, out result);
        }

        /// <summary>
        /// List syncboxes in the syncbox for these credentials.  This is a synchronous method.
        /// </summary>
        /// <param name="credentials">The credentials to use for this request.</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="settings">(optional) the settings to use with this method</param>
        /// <param name="getNewCredentialsCallback">The delegate to call for getting new temporary credentials.</param>
        /// <param name="getNewCredentialsCallbackUserState">The user state to pass to the delegate above.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        /// <remarks>The response array may be null, empty, or may contain null items.</remarks>
        public static CLError ListAllSyncboxesWithCredentials(
                    CLCredentials credentials,
                    out CLSyncbox[] response,
                    ICLCredentialsSettings settings = null,
                    Helpers.ReplaceExpiredCredentials getNewCredentialsCallback = null,
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
                    SyncboxId: null, 
                    isOneOff: false);

                // Convert the server response to a list of initialized CLSyncboxes.
                if (responseFromServer != null && responseFromServer.Syncboxes != null)
                {
                    List<CLSyncbox> listSyncboxes = new List<CLSyncbox>();
                    foreach (JsonContracts.Syncbox syncbox in responseFromServer.Syncboxes)
                    {
                        if (syncbox != null)
                        {
                            listSyncboxes.Add(new CLSyncbox(syncbox, credentials, null, copiedSettings, getNewCredentialsCallback, getNewCredentialsCallbackUserState));
                        }
                        else
                        {
                            listSyncboxes.Add(null);
                        }
                    }
                    response = listSyncboxes.ToArray();
                }
                else
                {
                    throw new NullReferenceException(Resources.ExceptionCLHttpRestWithoutSessions);  //&&&& fix
                }

            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<CLSyncbox[]>();
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
        /// <param name="asyncCallbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="itemCompletionCallback">Delegate which will be fired upon successful communication for every response item.</param>
        /// <param name="itemCompletionCallbackUserState">Userstate to be passed whenever the completion delegate is fired.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginRootFolder(AsyncCallback asyncCallback, object asyncCallbackUserState, CLFileItemCompletion itemCompletionCallback, object itemCompletionCallbackUserState)
        {
            CheckDisposed();

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginItemForPath(asyncCallback, asyncCallbackUserState, itemCompletionCallback, itemCompletionCallbackUserState, this.Path);
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
            CheckDisposed();

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndItemForPath(asyncResult, out result);
        }

        /// <summary>
        /// Queries the syncbox for an item at the syncbox root path.
        /// </summary>
        /// <param name="itemCompletionCallback">Delegate which will be fired upon successful communication for every response item.</param>
        /// <param name="itemCompletionCallbackUserState">Userstate to be passed whenever the completion delegate is fired.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError RootFolder(CLFileItemCompletion itemCompletionCallback, object itemCompletionCallbackUserState)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.ItemForPath(itemCompletionCallback, itemCompletionCallbackUserState, this.Path);
        }

        #endregion  // end GetItemAtPath (Queries the cloud for the item at a particular path)

        #region ItemForPath (Queries the syncbox for the item at a particular path)
        /// <summary>
        /// Asynchronously starts querying the syncbox for an item at a given path (must be specified) for existing metadata at that path; outputs a CLFileItem object.
        /// Check for Deleted flag being true in case the metadata represents a deleted item.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when operation completes</param>
        /// <param name="asyncCallbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="itemCompletionCallback">Delegate which will be fired upon successful communication for every response item.</param>
        /// <param name="itemCompletionCallbackUserState">Userstate to be passed whenever the completion delegate is fired.</param>
        /// <param name="path">Full path to where file or folder would exist in the syncbox locally on disk.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginItemForPath(AsyncCallback asyncCallback, object asyncCallbackUserState, CLFileItemCompletion itemCompletionCallback, object itemCompletionCallbackUserState, string path)
        {
            CheckDisposed();

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginItemForPath(asyncCallback, asyncCallbackUserState, itemCompletionCallback, itemCompletionCallbackUserState, path);
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
            CheckDisposed();

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndItemForPath(asyncResult, out result);
        }

        /// <summary>
        /// Queries the syncbox at a given file or folder path (must be specified) for existing item metadata at that path.
        /// Check for Deleted flag being true in case the metadata represents a deleted item.
        /// </summary>
        /// <param name="itemCompletionCallback">Delegate which will be fired upon successful communication for every response item.</param>
        /// <param name="itemCompletionCallbackUserState">Userstate to be passed whenever the completion delegate is fired.</param>
        /// <param name="path">Full path to where file or folder would exist in the syncbox locally on disk.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError ItemForPath(CLFileItemCompletion itemCompletionCallback, object itemCompletionCallbackUserState, string path)
        {
            CheckDisposed(isOneOff: true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.ItemForPath(itemCompletionCallback, itemCompletionCallbackUserState, path);
        }

        #endregion  // end GetItemAtPath (Queries the cloud for the item at a particular path)

        #region RenameFiles (Rename files in-place in the syncbox)
        /// <summary>
        /// Asynchronously starts renaming files in-place in the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">Userstate to pass when firing the async callback above.</param>
        /// <param name="itemCompletionCallback">Delegate which will be fired upon successful communication for every response item.</param>
        /// <param name="itemCompletionCallbackUserState">Userstate to be passed whenever the completion delegate is fired.</param>
        /// <param name="itemsToRename">One or more pairs of items to rename and the new name of each item (just the filename.ext).</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginRenameFiles(AsyncCallback asyncCallback, object asyncCallbackUserState, CLFileItemCompletion itemCompletionCallback, object itemCompletionCallbackUserState, params RenameItemParams[] itemsToRename)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginRenameFiles(asyncCallback, asyncCallbackUserState, itemCompletionCallback, itemCompletionCallbackUserState, itemsToRename);
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
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndRenameFiles(asyncResult, out result);
        }

        /// <summary>
        /// Renames files in-place in the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="itemCompletionCallback">Delegate which will be fired upon successful communication for every response item.</param>
        /// <param name="itemCompletionCallbackUserState">Userstate to be passed whenever the completion delegate is fired.</param>
        /// <param name="itemsToRename">One or more pairs of items to rename and the new name of each item (just the filename.ext).</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError RenameFiles(CLFileItemCompletion itemCompletionCallback, object itemCompletionCallbackUserState, params RenameItemParams[] itemsToRename)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.RenameFiles(itemCompletionCallback, itemCompletionCallbackUserState, itemsToRename);
        }

        #endregion  // end RenameFiles (Rename files in-place in the syncbox)

        #region RenameFolders (Rename folders in-place in the syncbox)
        /// <summary>
        /// Asynchronously starts renaming folders in-place in the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">Userstate to pass when firing the async callback above.</param>
        /// <param name="itemCompletionCallback">Delegate which will be fired upon successful communication for every response item.</param>
        /// <param name="itemCompletionCallbackUserState">Userstate to be passed whenever the completion delegate is fired.</param>
        /// <param name="itemsToRename">One or more pairs of items to rename and the new name of each item (just the last token in the path).</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginRenameFolders(AsyncCallback asyncCallback, object asyncCallbackUserState, CLFileItemCompletion itemCompletionCallback, object itemCompletionCallbackUserState, params RenameItemParams[] itemsToRename)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginRenameFolders(asyncCallback, asyncCallbackUserState, itemCompletionCallback, itemCompletionCallbackUserState, itemsToRename);
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
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndRenameFolders(asyncResult, out result);
        }

        /// <summary>
        /// Renames folders in-place in the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="itemCompletionCallback">Delegate which will be fired upon successful communication for every response item.</param>
        /// <param name="itemCompletionCallbackUserState">Userstate to be passed whenever the completion delegate is fired.</param>
        /// <param name="itemsToRename">An array of pairs of items to rename and the new name of each item (just the last token in the path).</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError RenameFolders(CLFileItemCompletion itemCompletionCallback, object itemCompletionCallbackUserState, params RenameItemParams[] itemsToRename)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.RenameFolders(itemCompletionCallback, itemCompletionCallbackUserState, itemsToRename);
        }

        #endregion  // end RenameFolders (Rename folders in the syncbox)

        #region MoveFiles (Move files in the syncbox)
        /// <summary>
        /// Asynchronously starts moving files in the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">Userstate to pass when firing the async callback above.</param>
        /// <param name="itemCompletionCallback">Delegate which will be fired upon successful communication for every response item.</param>
        /// <param name="itemCompletionCallbackUserState">Userstate to be passed whenever the completion delegate is fired.</param>
        /// <param name="itemsToMove">One or more pairs of items to move and the new full path of each item.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginMoveFiles(AsyncCallback asyncCallback, object asyncCallbackUserState, CLFileItemCompletion itemCompletionCallback, object itemCompletionCallbackUserState, params MoveItemParams[] itemsToMove)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginMoveFiles(asyncCallback, asyncCallbackUserState, itemCompletionCallback, itemCompletionCallbackUserState, itemsToMove);
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
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndMoveFiles(asyncResult, out result);
        }

        /// <summary>
        /// Moves files in the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="itemCompletionCallback">Delegate which will be fired upon successful communication for every response item.</param>
        /// <param name="itemCompletionCallbackUserState">Userstate to be passed whenever the completion delegate is fired.</param>
        /// <param name="itemsToMove">One or more pairs of items to move and the new full path of each item.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError MoveFiles(CLFileItemCompletion itemCompletionCallback, object itemCompletionCallbackUserState, params MoveItemParams[] itemsToMove)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.MoveFiles(itemCompletionCallback, itemCompletionCallbackUserState, itemsToMove);
        }

        #endregion  // end MoveFiles (Move files in the syncbox)

        #region MoveFolders (Move folders in the syncbox)
        /// <summary>
        /// Asynchronously starts moving folders in the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">Userstate to pass when firing the async callback above.</param>
        /// <param name="itemCompletionCallback">Delegate which will be fired upon successful communication for every response item.</param>
        /// <param name="itemCompletionCallbackUserState">Userstate to be passed whenever the completion delegate is fired.</param>
        /// <param name="itemsToMove">One or more pairs of items to move and the new full path each item.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginMoveFolders(AsyncCallback asyncCallback, object asyncCallbackUserState, CLFileItemCompletion itemCompletionCallback, object itemCompletionCallbackUserState, params MoveItemParams[] itemsToMove)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginMoveFolders(asyncCallback, asyncCallbackUserState, itemCompletionCallback, itemCompletionCallbackUserState, itemsToMove);
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
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndMoveFolders(asyncResult, out result);
        }

        /// <summary>
        /// Moves folders in the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="itemCompletionCallback">Delegate which will be fired upon successful communication for every response item.</param>
        /// <param name="itemCompletionCallbackUserState">Userstate to be passed whenever the completion delegate is fired.</param>
        /// <param name="itemsToMove">An array of pairs of items to rename and the new name of each item (just the last token in the path).</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError MoveFolders(CLFileItemCompletion itemCompletionCallback, object itemCompletionCallbackUserState, params MoveItemParams[] itemsToMove)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.MoveFolders(itemCompletionCallback, itemCompletionCallbackUserState, itemsToMove);
        }

        #endregion  // end RenameFolders (Rename folders in the syncbox)

        #region DeleteFiles (Delete files in the syncbox)
        /// <summary>
        /// Asynchronously starts deleting files in the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">Userstate to pass when firing the async callback above.</param>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">Userstate to be passed whenever the item completion callback above is fired.</param>
        /// <param name="itemsToDelete">One or more file items to delete.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginDeleteFiles(AsyncCallback asyncCallback, object asyncCallbackUserState, CLFileItemCompletion itemCompletionCallback, object itemCompletionCallbackUserState, params CLFileItem[] itemsToDelete)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginDeleteFiles(asyncCallback, asyncCallbackUserState, itemCompletionCallback, itemCompletionCallbackUserState, itemsToDelete);
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
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndDeleteFiles(asyncResult, out result);
        }

        /// <summary>
        /// Deletes files in the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">Userstate to be passed whenever the item completion callback above is fired.</param>
        /// <param name="itemsToDelete">One or more file items to delete.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError DeleteFiles(CLFileItemCompletion itemCompletionCallback, object itemCompletionCallbackUserState, params CLFileItem[] itemsToDelete)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.DeleteFiles(itemCompletionCallback, itemCompletionCallbackUserState, itemsToDelete);
        }

        #endregion  // end DeleteFiles (Delete files in the syncbox)

        #region DeleteFolders (Delete folders in the syncbox)
        /// <summary>
        /// Asynchronously starts deleting folders in the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">Userstate to pass when firing the async callback above.</param>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">Userstate to be passed whenever the item completion callback above is fired.</param>
        /// <param name="itemsToDelete">One or more folder items to delete.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginDeleteFolders(AsyncCallback asyncCallback, object asyncCallbackUserState, CLFileItemCompletion itemCompletionCallback, object itemCompletionCallbackUserState, params CLFileItem[] itemsToDelete)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginDeleteFolders(asyncCallback, asyncCallbackUserState, itemCompletionCallback, itemCompletionCallbackUserState, itemsToDelete);
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
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndDeleteFolders(asyncResult, out result);
        }

        /// <summary>
        /// Deletes folders in the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">Userstate to be passed whenever the item completion callback above is fired.</param>
        /// <param name="itemsToDelete">One or more folder items to delete.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError DeleteFolders(CLFileItemCompletion itemCompletionCallback, object itemCompletionCallbackUserState, params CLFileItem[] itemsToDelete)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.DeleteFolders(itemCompletionCallback, itemCompletionCallbackUserState, itemsToDelete);
        }

        #endregion  // end DeleteFolders (Delete folders in the syncbox)

        #region AddFolders (Add folders to the syncbox)
        /// <summary>
        /// Asynchronously starts adding folders to the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">Userstate to pass when firing the async callback above.</param>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">Userstate to be passed whenever the item completion callback above is fired.</param>
        /// <param name="folderItemsToAdd">One or more pairs of folder parent item and name of the new folder to add.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginAddFolders(AsyncCallback asyncCallback, object asyncCallbackUserState, CLFileItemCompletion itemCompletionCallback, object itemCompletionCallbackUserState, params AddItemParams[] folderItemsToAdd)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginAddFolders(asyncCallback, asyncCallbackUserState, itemCompletionCallback, itemCompletionCallbackUserState, folderItemsToAdd);
        }

        /// <summary>
        /// Finishes adding folders to the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndDeleteFiles(IAsyncResult asyncResult, out SyncboxAddFoldersResult result)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndAddFolders(asyncResult, out result);
        }

        /// <summary>
        /// Adds folders to the syncbox.  Each item completion will fire an asynchronous callback with the completion status or error for that item.
        /// </summary>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">Userstate to be passed whenever the item completion callback above is fired.</param>
        /// <param name="itemsToDelete">One or more file items to delete.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError DeleteFiles(CLFileItemCompletion itemCompletionCallback, object itemCompletionCallbackUserState, params AddItemParams[] folderItemsToAdd)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.AddFolders(itemCompletionCallback, itemCompletionCallbackUserState, folderItemsToAdd);
        }

        #endregion  // end AddFolders (Add folders to the syncbox)

        #region AddFile (Adds a file in the syncbox)
        /// <summary>
        /// Asynchronously starts adding a file in the syncbox; outputs a CLFileItem object.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="path">Full path to where the file would exist locally on disk.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginAddFile(AsyncCallback callback, object callbackUserState, string path)
        {
            CheckDisposed(true);
            string[] paths = new string[1] { path };

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginAddFiles(callback, callbackUserState, paths);
        }

        /// <summary>
        /// Finishes adding a file in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) The result from the metadata query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndAddFile(IAsyncResult aResult, out SyncboxAddFileResult result)
        {
            CheckDisposed(true);

            // Complete the async operation.
            SyncboxAddFilesResult results;

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            CLError error = httpRestClient.EndAddFiles(aResult, out results);

            // Return resulting error or item
            if (error != null)
            {
                // We got an overall error.  Return it.
                result = null;
                return error;
            }
            // error == null  (no overall error)
            else if (results == null)
            {
                // No overall error, but also no results.  Return an error.
                result = null;
                return new CLError(new CLException(CLExceptionCode.OnDemand_FileAddNoServerResponsesOrErrors, "No error or responses from server results null"));
            }
            // error == null && results != null  (no overall error, and we got a results object)
            else if (results.Errors != null && results.Errors.Length >= 1)
            {
                // No overall error, got a results object, and it has an error.  Return that error.
                result = null;
                return results.Errors[0];
            }
            // (error == null && results != null) && (results.Errors == null || results.Errors.Length == 0)  (no overall error, we got a results object, and there are no errors in results)
            else if (results.FileItems != null && results.FileItems.Length >= 1)
            {
                // No overall error, got a results object, is has no errors, and it has a delete response.  This is the normal case.  Return that delete response as the result.
                result = new SyncboxAddFileResult(error: null, fileItem: results.FileItems[0]);
                return null;        // normal condition
            }
            // ((error == null && results != null) && (results.Errors == null || results.Errors.Length == 0)) && (results.Responses == null || results.Responses.Length == 0)
            else
            {
                // No error, got a results object, but there were no errors and no delete responses inside.  Return an error.
                result = null;
                return new CLError(new CLException(CLExceptionCode.OnDemand_FileAddNoServerResponsesOrErrors, "No error or responses from server"));
            }
        }

        /// <summary>
        /// Adds a file in the syncbox.
        /// </summary>
        /// <param name="path">Full path to where the file would exist locally on disk</param>
        /// <param name="fileItem">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError AddFile(string path, out CLFileItem fileItem)
        {
            CheckDisposed(true);
            string[] paths = new string[1] { path };

            // Communicate and get the results.
            CLError[] outErrors;
            CLFileItem[] outItems;

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            CLError error = httpRestClient.AddFiles(paths, out outItems, out outErrors);

            // Return resulting error or item
            if (error != null)
            {
                // There was an overall error.  Return it
                fileItem = null;
                return error;
            }
            // error == null
            else if (outErrors != null && outErrors.Length >= 1)
            {
                // No overall error, but there was an item error.  Return it.
                fileItem = null;
                return outErrors[0];
            }
            // error == null && (outErrors == null || outErrors.Length == 0)
            else if (outItems != null && outItems.Length >= 1)
            {
                // No overall error, no item errors, and we have an item.  Return it.  This is the normal condition
                fileItem = outItems[0];
                return null;
            }
            // (error == null && (outErrors == null || outErrors.Length == 0)) && (outItems == null || outItems.Length == 0)
            else
            {
                // No overall error, no item errors, and no items.  No responses from server.  Return error.
                fileItem = null;
                return new CLError(new CLException(CLExceptionCode.OnDemand_FileAddNoServerResponsesOrErrors, "No responses or status from serer"));
            }
        }

        #endregion  // end AddFile (Adds a file in the syncbox)

        #region AddFiles (Add files in the syncbox)
        /// <summary>
        /// Asynchronously starts adding files in the syncbox; outputs an array of  CLFileItem objects, and possibly an array of CLError objects.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="paths">An array of full paths to where the files would exist locally on disk.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginAddFiles(AsyncCallback callback, object callbackUserState, string[] paths)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginAddFiles(callback, callbackUserState, paths);
        }

        /// <summary>
        /// Finishes adding files in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) The result from the metadata query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndAddFiles(IAsyncResult aResult, out SyncboxAddFilesResult result)
        {
            CheckDisposed(true);
            SyncboxAddFilesResult deleteResult;

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            CLError error = httpRestClient.EndAddFiles(aResult, out deleteResult);

            if (error != null)
            {
                result = null;
                return error;
            }

            result = new SyncboxAddFilesResult(deleteResult.OverallError, deleteResult.Errors, deleteResult.FileItems);
            return error;
        }

        /// <summary>
        /// Adds files in the syncbox.
        /// </summary>
        /// <param name="paths">An array of full paths to where the files would exist locally on disk.</param>
        /// <param name="fileItems">(output) response object from communication</param>
        /// <param name="errors">(output) Any returned errors, or null.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError AddFiles(string[] paths, out CLFileItem[] fileItems, out CLError[] errors)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.AddFiles(paths, out fileItems, out errors);
        }

        #endregion  // end AddFiles (Add files in the syncbox)

        #region GetAllPending
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        /// <summary>
        /// Asynchronously starts querying for all pending files
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetAllPending(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetAllPending(aCallback, aState, timeoutMilliseconds);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        /// <summary>
        /// Finishes a query for all pending files if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the pending query</param>
        /// <param name="result">(output) The result from the pending query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetAllPending(IAsyncResult aResult, out GetAllPendingResult result)
        {
            CheckDisposed(true);
            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndGetAllPending(aResult, out result);
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
            CheckDisposed(true);

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
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFileVersions(AsyncCallback aCallback,
            object aState,
            string fileServerId,
            int timeoutMilliseconds)
        {
            CheckDisposed();

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetFileVersions(aCallback,
                aState,
                fileServerId,
                timeoutMilliseconds);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        /// <summary>
        /// Asynchronously starts querying the server for all versions of a given file
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="includeDeletedVersions">(optional) whether to include file versions which are deleted</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFileVersions(AsyncCallback aCallback,
            object aState,
            string fileServerId,
            int timeoutMilliseconds,
            bool includeDeletedVersions)
        {
            CheckDisposed();

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetFileVersions(aCallback,
                aState,
                fileServerId,
                timeoutMilliseconds,
                includeDeletedVersions);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        /// <summary>
        /// Asynchronously starts querying the server for all versions of a given file
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Full path to the file where it would be placed locally within the sync root</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFileVersions(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            FilePath pathToFile)
        {
            CheckDisposed();

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetFileVersions(aCallback, aState, timeoutMilliseconds, pathToFile);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        /// <summary>
        /// Asynchronously starts querying the server for all versions of a given file
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Full path to the file where it would be placed locally within the sync root</param>
        /// <param name="includeDeletedVersions">(optional) whether to include file versions which are deleted</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFileVersions(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            FilePath pathToFile,
            bool includeDeletedVersions)
        {
            CheckDisposed();

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetFileVersions(aCallback, aState, timeoutMilliseconds, pathToFile, includeDeletedVersions);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        /// <summary>
        /// Asynchronously starts querying the server for all versions of a given file
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Full path to the file where it would be placed locally within the sync root</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFileVersions(AsyncCallback aCallback,
            object aState,
            string fileServerId,
            int timeoutMilliseconds,
            FilePath pathToFile)
        {
            CheckDisposed();

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetFileVersions(aCallback, aState, fileServerId, timeoutMilliseconds, pathToFile);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        /// <summary>
        /// Asynchronously starts querying the server for all versions of a given file
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Full path to the file where it would be placed locally within the sync root</param>
        /// <param name="includeDeletedVersions">(optional) whether to include file versions which are deleted</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFileVersions(AsyncCallback aCallback,
            object aState,
            string fileServerId,
            int timeoutMilliseconds,
            FilePath pathToFile,
            bool includeDeletedVersions)
        {
            CheckDisposed();

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetFileVersions(aCallback, aState, fileServerId, timeoutMilliseconds, pathToFile, includeDeletedVersions);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        /// <summary>
        /// Finishes querying for all versions of a given file if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting undoing the deletion</param>
        /// <param name="result">(output) The result from undoing the deletion</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetFileVersions(IAsyncResult aResult, out GetFileVersionsResult result)
        {
            CheckDisposed();

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndGetFileVersions(aResult, out result);
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
            CheckDisposed();

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
            CheckDisposed();

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
            CheckDisposed();

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
            CheckDisposed();

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
            CheckDisposed();

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
            CheckDisposed();

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.GetFileVersions(fileServerId, timeoutMilliseconds, pathToFile, out response, includeDeletedVersions);
        }
        #endregion

        #region GetAllImageItems (Gets all of the image items from the cloud for this syncbox)
        /// <summary>
        /// Asynchronously starts querying the server for image items.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetAllImageItems(AsyncCallback callback, object callbackUserState)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetAllImageItems(callback, callbackUserState);
        }

        /// <summary>
        /// Finishes querying the server for image items, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the pictures query</param>
        /// <param name="result">(output) The result from the pictures query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetAllImageItems(IAsyncResult aResult, out SyncboxGetAllImageItemsResult result)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndGetAllImageItems(aResult, out result);
        }

        /// <summary>
        /// Queries the server for pictures
        /// </summary>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetAllImageItems(out CLFileItem[] response)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.GetAllImageItems(out response);
        }
        #endregion  // end GetAllImageItems (Gets all of the image items from the cloud for this syncbox)

        #region GetAllVideoItems  (Gets all of the video items from the cloud for this syncbox)
        /// <summary>
        /// Asynchronously starts querying the server for video file items.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetAllVideoItems(AsyncCallback callback, object callbackUserState)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetAllVideoItems(callback, callbackUserState);
        }

        /// <summary>
        /// Finishes querying the server for video file items, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the videos query</param>
        /// <param name="result">(output) The result from the videos query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetAllVideoItems(IAsyncResult aResult, out SyncboxGetAllVideoItemsResult result)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndGetAllVideoItems(aResult, out result);
        }

        /// <summary>
        /// Queries the server for videos
        /// </summary>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetGetAllVideoItems(out CLFileItem[] response)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.GetAllVideoItems(out response);
        }
        #endregion  // end GetAllVideoItems  (Gets all of the video items from the cloud for this syncbox)

        #region GetAllAudioItems (Gets all of the audio items from the cloud for this syncbox)
        /// <summary>
        /// Asynchronously starts querying the server for audio items.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetAllAudioItems(AsyncCallback callback, object callbackUserState)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetAllAudioItems(callback, callbackUserState);
        }

        /// <summary>
        /// Finishes querying for audio items, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the audios query</param>
        /// <param name="result">(output) The result from the audios query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetAllAudioItems(IAsyncResult aResult, out SyncboxGetAllAudioItemsResult result)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndGetAllAudioItems(aResult, out result);
        }

        /// <summary>
        /// Queries the server for audio items.
        /// </summary>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetAllAudioItems(CLFileItem[] response)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.GetAllAudioItems(out response);
        }
        #endregion  // end GetAllAudioItems (Gets all of the audio items from the cloud for this syncbox)

        #region GetAllDocumentItems  (Gets all of the document items from the cloud for this syncbox)
        /// <summary>
        /// Asynchronously starts querying the server for document items.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetAllDocumentItems(AsyncCallback callback, object callbackUserState)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetAllDocumentItems(callback, callbackUserState);
        }

        /// <summary>
        /// Finishes querying for document items, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the audios query</param>
        /// <param name="result">(output) The result from the audios query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetAllDocumentItems(IAsyncResult aResult, out SyncboxGetAllDocumentItemsResult result)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndGetAllDocumentItems(aResult, out result);
        }

        /// <summary>
        /// Queries the server for document items.
        /// </summary>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetAllDocumentItems(out CLFileItem[] response)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.GetAllDocumentItems(out response);
        }
        #endregion  // end GetAllDocumentItems  (Gets all of the document items from the cloud for this syncbox)

        #region GetAllPresentationItems  (Gets all of the presentation items from the cloud for this syncbox)
        /// <summary>
        /// Asynchronously starts querying the server for presentation items.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetAllPresentationItems(AsyncCallback callback, object callbackUserState)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetAllPresentationItems(callback, callbackUserState);
        }

        /// <summary>
        /// Finishes querying for presentation items, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the audios query</param>
        /// <param name="result">(output) The result from the audios query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetAllPresentationItems(IAsyncResult aResult, out SyncboxGetAllPresentationItemsResult result)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndGetAllPresentationItems(aResult, out result);
        }

        /// <summary>
        /// Queries the server for presentation items.
        /// </summary>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetAllPresentationItems(out CLFileItem[] response)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.GetAllPresentationItems(out response);
        }
        #endregion  // end GetAllPresentationItems  (Gets all of the presentation items from the cloud for this syncbox)

        #region GetAllTextItems  (Gets all of the text items from the cloud for this syncbox)
        /// <summary>
        /// Asynchronously starts querying the server for text items.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetAllTextItems(AsyncCallback callback, object callbackUserState)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetAllTextItems(callback, callbackUserState);
        }

        /// <summary>
        /// Finishes querying for text items, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the audios query</param>
        /// <param name="result">(output) The result from the audios query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetAllTextItems(IAsyncResult aResult, out SyncboxGetAllTextItemsResult result)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndGetAllTextItems(aResult, out result);
        }

        /// <summary>
        /// Queries the server for text items.
        /// </summary>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetAllTextItems(out CLFileItem[] response)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.GetAllTextItems(out response);
        }
        #endregion  // end GetAllTextItems  (Gets all of the text items from the cloud for this syncbox)

        #region GetAllArchiveItems  (Gets all of the archive items from the cloud for this syncbox)
        /// <summary>
        /// Asynchronously starts querying the server for archive items.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetAllArchiveItems(AsyncCallback callback, object callbackUserState)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetAllArchiveItems(callback, callbackUserState);
        }

        /// <summary>
        /// Finishes querying for archive items, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the audios query</param>
        /// <param name="result">(output) The result from the audios query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetAllArchiveItems(IAsyncResult aResult, out SyncboxGetAllArchiveItemsResult result)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndGetAllArchiveItems(aResult, out result);
        }

        /// <summary>
        /// Queries the server for archive items.
        /// </summary>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetAllArchiveItems(out CLFileItem[] response)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.GetAllArchiveItems(out response);
        }
        #endregion  // end GetAllArchiveItems  (Gets all of the archive items from the cloud for this syncbox)

        #region GetRecentFilesSinceDateWithLimit (get a list of the recent files starting at a particular time)
        /// <summary>
        /// Asynchronously starts querying the server for recents
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="sinceDate">null to retrieve all of the recents, or specify a date to retrieve items since that date.</param>
        /// <param name="returnLimit">null to retrieve all of the recents, or specify a limit for the number of items to be returned.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetRecentFilesSinceDateWithLimit(
            AsyncCallback callback,
            object callbackUserState,
            Nullable<DateTime> sinceDate,
            Nullable<int> returnLimit)

        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetRecents(callback, callbackUserState, sinceDate, returnLimit);
        }

        /// <summary>
        /// Finishes querying for recents if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the recents query</param>
        /// <param name="result">(output) The result from the recents query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetRecentFilesSinceDateWithLimit(IAsyncResult aResult, out SyncboxGetRecentsResult result)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndGetRecents(aResult, out result);
        }

        /// <summary>
        /// Queries the server for recents
        /// </summary>
        /// <param name="sinceDate">null to retrieve all of the recents, or specify a date to retrieve items since that date.</param>
        /// <param name="returnLimit">null to retrieve all of the recents, or specify a limit for the number of items to be returned.</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetRecentFilesSinceDateWithLimit(
            Nullable<DateTime> sinceDate,
            Nullable<int> returnLimit,
            out CLFileItem[] response)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.GetRecents(sinceDate, returnLimit, out response);
        }
        #endregion  // end GetRecentFilesSinceDateWithLimit (get a list of the recent files starting at a particular time)

        #region GetFolderContentsAtPath (Query the cloud for the contents of a syncbox folder at a path)
        /// <summary>
        /// Asynchronously starts querying folder contents from the cloud at a particular path.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="path">Full path of the folder that would be on disk in the syncbox.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFolderContents(AsyncCallback callback, object callbackUserState, string path)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetFolderContentsAtPath(callback, callbackUserState, path);
        }

        /// <summary>
        /// Finishes getting folder contents if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting getting folder contents</param>
        /// <param name="result">(output) The result from folder contents</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetFolderContentsAtPath(IAsyncResult aResult, out SyncboxGetFolderContentsAtPathResult result)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndGetFolderContents(aResult, out result);
        }

        /// <summary>
        /// Queries server for folder contents with an optional path and an optional depth limit
        /// </summary>
        /// <param name="path">The full path of the folder that would be on disk in the local syncbox folder.</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderContents(
            string path,
            out CLFileItem[] response)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.GetFolderContentsAtPath(path, out response);
        }

        #endregion  // end GetFolderContentsAtPath (Query the cloud for the contents of a syncbox folder at a path)

        #region GetFolderHierarchy
        /// <summary>
        /// Asynchronously starts querying folder hierarchy with optional path
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFolderHierarchy(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetFolderHierarchy(aCallback, aState, timeoutMilliseconds);
        }

        /// <summary>
        /// Asynchronously starts querying folder hierarchy with optional path
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="hierarchyRoot">(optional) root path of hierarchy query</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFolderHierarchy(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            FilePath hierarchyRoot)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetFolderHierarchy(aCallback, aState, timeoutMilliseconds, hierarchyRoot);
        }
        
        /// <summary>
        /// Finishes getting folder hierarchy if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting getting folder hierarchy</param>
        /// <param name="result">(output) The result from folder hierarchy</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetFolderHierarchy(IAsyncResult aResult, out GetFolderHierarchyResult result)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndGetFolderHierarchy(aResult, out result);
        }

        /// <summary>
        /// Queries server for folder hierarchy with an optional path
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderHierarchy(int timeoutMilliseconds,out JsonContracts.FoldersResponse response)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.GetFolderHierarchy(timeoutMilliseconds, out response);
        }

        /// <summary>
        /// Queries server for folder hierarchy with an optional path
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="hierarchyRoot">(optional) root path of hierarchy query</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderHierarchy(int timeoutMilliseconds, out JsonContracts.FoldersResponse response, FilePath hierarchyRoot)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.GetFolderHierarchy(timeoutMilliseconds, out response, hierarchyRoot);
        }
        #endregion

        #region GetDataUsage (get the usage information for this syncbox from the cloud)
        /// <summary>
        /// Asynchronously starts getting the usage information for this syncbox from the cloud.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass as a parameter when firing async callback</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetDataUsage(AsyncCallback callback, object callbackUserState)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetSyncboxUsage(callback, callbackUserState, _copiedSettings.HttpTimeoutMilliseconds);
        }

        /// <summary>
        /// Finishes getting syncbox usage if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the asynchronous request.</param>
        /// <param name="result">(output) The result from the asynchronous request.</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetDataUsage(IAsyncResult aResult, out SyncboxUsageResult result)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndGetSyncboxUsage(aResult, out result);
        }

        /// <summary>
        /// Queries the cloud for syncbox usage information.  This method is synchronous.
        /// </summary>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetDataUsage(out JsonContracts.SyncboxUsageResponse response)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.GetSyncboxUsage(_copiedSettings.HttpTimeoutMilliseconds, out response);
        }
        #endregion  // end GetDataUsage (get the usage information for this syncbox from the cloud)

        #region UpdateSyncboxExtendedMetadata
        /// <summary>
        /// Asynchronously updates the extended metadata on a sync box
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="metadata">string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginUpdateSyncboxExtendedMetadata<T>(AsyncCallback aCallback,
            object aState,
            IDictionary<string, T> metadata,
            int timeoutMilliseconds)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginUpdateSyncboxExtendedMetadata(aCallback, aState, metadata, timeoutMilliseconds);
        }

        /// <summary>
        /// Asynchronously updates the extended metadata on a sync box
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="metadata">string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginUpdateSyncboxExtendedMetadata(AsyncCallback aCallback,
            object aState,
            JsonContracts.MetadataDictionary metadata,
            int timeoutMilliseconds)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginUpdateSyncboxExtendedMetadata(aCallback, aState, metadata, timeoutMilliseconds);
        }

        /// <summary>
        /// Finishes updating the extended metadata on a sync box if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting updating extended metadata</param>
        /// <param name="result">(output) The result from updating extended metadata</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndUpdateSyncboxExtendedMetadata(IAsyncResult aResult, out SyncboxUpdateExtendedMetadataResult result)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndUpdateSyncboxExtendedMetadata(aResult, out result);
        }

        /// <summary>
        /// Updates the extended metadata on a sync box
        /// </summary>
        /// <param name="metadata">string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError UpdateSyncboxExtendedMetadata<T>(IDictionary<string, T> metadata, int timeoutMilliseconds, out JsonContracts.SyncboxResponse response)
        {
            CheckDisposed(true);

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
        public CLError SyncboxUpdateExtendedMetadata(JsonContracts.MetadataDictionary metadata, int timeoutMilliseconds, out JsonContracts.SyncboxResponse response)
        {
            CheckDisposed(true);

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
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass as a parameter when firing async callback</param>
        /// <param name="storagePlan">The new storage plan to use for this syncbox)</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginUpdateStoragePlan(AsyncCallback callback, object callbackUserState, CLStoragePlan storagePlan)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginSyncboxUpdateStoragePlan(
                callback, 
                callbackUserState, 
                storagePlan.Id, 
                _copiedSettings.HttpTimeoutMilliseconds, 
                ReservedForActiveSync,
                new Action<JsonContracts.SyncboxUpdateStoragePlanResponse, object>(OnUpdateStoragePlanCompletion), 
                null);
        }

        /// <summary>
        /// Finishes updating the storage plan for this syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) The result from completing the request</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndUpdateStoragePlan(IAsyncResult aResult, out SyncboxUpdateStoragePlanResult result)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndSyncboxUpdateStoragePlan(aResult, out result);
        }

        /// <summary>
        /// Updates the storage plan for a syncbox in the syncbox.  This is a synchronous method.
        /// Updates this object's StoragePlanId property.
        /// </summary>
        /// <param name="storagePlan">The storage plan to set (new storage plan to use for this syncbox)</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError UpdateStoragePlan(CLStoragePlan storagePlan, out JsonContracts.SyncboxUpdateStoragePlanResponse response)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.UpdateSyncboxStoragePlan(
                storagePlan.Id, 
                _copiedSettings.HttpTimeoutMilliseconds, 
                out response, 
                ReservedForActiveSync,
                new Action<JsonContracts.SyncboxUpdateStoragePlanResponse, object>(OnUpdateStoragePlanCompletion), 
                null);
        }

        /// <summary>
        /// Called back when the HTTP request completes.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="userState"></param>
        private void OnUpdateStoragePlanCompletion(JsonContracts.SyncboxUpdateStoragePlanResponse response, object userState)
        {
            if (response == null)
            {
                throw new NullReferenceException("response cannot be null");  //&&&& fix
            }
            if (response.Syncbox == null)
            {
                throw new NullReferenceException("response Syncbox cannot be null");  //&&&& fix
            }
            if (response.Syncbox.PlanId == null)
            {
                throw new NullReferenceException("response Syncbox PlanId cannot be null");  //&&&& fix
            }

            // Update this object's properties atomically.
            this._propertyChangeLocker.EnterWriteLock();
            try
            {
                this._storagePlanId = (long)response.Syncbox.PlanId;
            }
            finally
            {
                this._propertyChangeLocker.ExitWriteLock();
            }
        }
        #endregion  // end (changes the storage plan associated with this syncbox in the syncbox)

        #region GetCurrentStatus (update the status of this syncbox from the cloud)
        /// <summary>
        /// Asynchronously gets the status of this Syncbox.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetCurrentSyncboxStatus(AsyncCallback callback, object callbackUserState)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.BeginGetSyncboxStatus(callback, callbackUserState, _copiedSettings.HttpTimeoutMilliseconds, new Action<JsonContracts.SyncboxStatusResponse, object>(OnStatusCompletion), null);
        }
        
        /// <summary>
        /// Finishes the asynchronous request, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error).
        /// The local object status is updated with the server results.
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) The result from the request</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetCurrentSyncboxStatus(IAsyncResult aResult, out SyncboxStatusResult result)
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            return httpRestClient.EndGetSyncboxStatus(aResult, out result);
        }

        /// <summary>
        /// Gets the status of this Syncbox.  This is a synchronous method.
        /// The local object status is updated with the server results.
        /// </summary>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetCurrentSyncboxStatus()
        {
            CheckDisposed(true);

            CLHttpRest httpRestClient;
            GetInstanceRestClient(out httpRestClient);
            JsonContracts.SyncboxStatusResponse response;
            return httpRestClient.GetSyncboxStatus(_copiedSettings.HttpTimeoutMilliseconds, out response, new Action<JsonContracts.SyncboxStatusResponse, object>(OnStatusCompletion), null);
        }


        /// <summary>
        /// Called back when the HTTP request completes.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="userState"></param>
        private void OnStatusCompletion(JsonContracts.SyncboxStatusResponse response, object userState)
        {
            if (response == null)
            {
                throw new NullReferenceException("response cannot be null");  //&&&& fix
            }
            if (response.Syncbox == null)
            {
                throw new NullReferenceException("response Syncbox cannot be null");  //&&&& fix
            }
            if (response.Syncbox.PlanId == null)
            {
                throw new NullReferenceException("response Syncbox PlanId cannot be null");  //&&&& fix
            }

            this._propertyChangeLocker.EnterWriteLock();
            try
            {
                this._friendlyName = response.Syncbox.FriendlyName;
                this._storagePlanId = (long)response.Syncbox.PlanId;
            }
            finally
            {
                this._propertyChangeLocker.ExitWriteLock();
            }
        }
        #endregion  // end GetCurrentStatus (update the status of this syncbox from the cloud)

        /// <summary>
        /// Sets the full path on the local disk that is associated with this Syncbox.  This method does not communicate with the server.
        /// </summary>
        /// <returns>Returns any error that occurred, or null</returns>
        /// <remarks>The path may be set only once.</remarks>
        public CLError UpdatePath(string path)
        {
            CLError errorFromSet = UpdatePathInternal(path, shouldUupdateSyncboxStatusFromServer: true);
            return errorFromSet;
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
                    throw new CLNullReferenceException(CLExceptionCode.Syncbox_BadPath, "path must be set first");
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
        /// an array of CLSyncbox instances, but the information from the server does not include the local syncbox paths.  The instances are unusable in this state
        /// and any operation that requires the local syncbox path will throw an error.  It is up to the app to provide the local syncbox path before using the instance.
        /// This is done via SetSyncboxPath().  SetSyncboxPath() defers to this internal version.  This function acts like an extension of the constructor, and it
        /// if called by constructors to create the CLHttpRest client, fill in missing information from the server, and to create an instance of CLSyncEngine that will
        /// be used by this syncbox instance.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="shouldUupdateSyncboxStatusFromServer"></param>
        /// <returns></returns>
        private CLError UpdatePathInternal(string path, bool shouldUupdateSyncboxStatusFromServer)
        {
            if (path == null)
            {
                return new CLArgumentNullException(CLExceptionCode.Syncbox_BadPath, "path must not be null");
            }

            if (setPathLocker != null)
            {
                Monitor.Enter(setPathLocker);
            }
            try
            {
                if (setPathHolder != null)
                {
                    throw new CLException(CLExceptionCode.Syncbox_PathAlreadySet, Resources.ExceptionOnDemandSyncboxPathAlreadySet);
                }

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
                    throw new CLArgumentException(errorPathTooLong.PrimaryException.Code, string.Format("syncbox path is too long by {0} characters.", nOutTooLongChars), errorPathTooLong.Exceptions);
                }

                CLError errorBadPath = Helpers.CheckForBadPath(path);
                if (errorBadPath != null)
                {
                    throw new CLArgumentException(errorBadPath.PrimaryException.Code, "syncbox path contains invalid characters.", errorBadPath.Exceptions);
                }


                // Set the path early because the CLHttpRest factory needs it.
                setPathHolder = new SetPathProperties(path, null);

                // Create an instance of the CLHttpRest client for this syncbox
                CLHttpRest localRestClient;
                // Create the http rest client
                _trace.writeToLog(9, Resources.CLSyncboxStartCreateRestClient);
                CLError createRestClientError = CLHttpRest.CreateAndInitialize(
                                credentials: this.Credentials,
                                syncbox: this,
                                client: out localRestClient,
                                settings: this._copiedSettings,
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
                    const string nullRestClient = "Unknown error creating HTTP REST client";
                    _trace.writeToLog(1, Resources.CLSyncboxConstructionErrorMsg0, nullRestClient);

                    throw new CLNullReferenceException(CLExceptionCode.Syncbox_CreateRestClient, nullRestClient);
                }
                
                // after removing CLSyncEngine from SetPathProperties, can now set the final setPathHolder here
                setPathHolder = new SetPathProperties(path, localRestClient);

                if (shouldUupdateSyncboxStatusFromServer)
                {
                    //// removed CLSyncEngine from SetPathProperties, so can completely set setPathHolder before this statement
                    //
                    //// OLD CODE:
                    //// Set the rest client early too because GetCurrentSyncboxStatus neeeds it.
                    //setPathHolder = new SetPathProperties(path, localRestClient, null);

                    // We need to validate the syncbox ID with the server with these credentials.  We will also retrieve the other syncbox
                    // properties from the server and set them into this local object's properties.
                    CLError errorFromStatus = GetCurrentSyncboxStatus();
                    if (errorFromStatus != null)
                    {
                        throw new CLException(CLExceptionCode.Syncbox_InitialStatus, Resources.ExceptionSyncboxStartStatus, errorFromStatus.Exceptions);
                    }
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                if (setPathLocker != null)
                {
                    Monitor.Exit(setPathLocker);
                }
            }

            return null;
        }

        internal bool ReservedForActiveSync
        {
            get
            {
                lock (_reservedForActiveSync)
                {
                    return _reservedForActiveSync.Value;
                }
            }
        }
        internal bool TryReserveForActiveSync()
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
        internal void ResetReserveForActiveSync()
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
        internal void OnPushNotificationConnectionError(object sender, NotificationErrorEventArgs e)
        {
            try
            {
                if (e.ErrorStillDisconnectedPing == null)
                {
                    if (e.ErrorWebSockets == null)
                    {
                        _trace.writeToLog(1, "CLSyncbox: OnPushNotificationConnectionError: Entry. ERROR: No errors.");
                        try
                        {
                            throw new CLInvalidOperationException(CLExceptionCode.General_Invalid, "Push notification connection error event fired without an error");
                        }
                        catch (Exception ex)
                        {
                            ((CLError)ex).Log(_copiedSettings.TraceLocation, _copiedSettings.LogErrors);
                        }
                    }
                    else
                    {
                        _trace.writeToLog(1, "CLSyncbox: OnPushNotificationConnectionError: Entry. ERROR: Web socket error message: <{0}>. Web socket error code: {1}.", e.ErrorWebSockets.PrimaryException.Message, e.ErrorWebSockets.PrimaryException.Code);
                        e.ErrorWebSockets.Log(_copiedSettings.TraceLocation, _copiedSettings.LogErrors);
                    }
                }
                else if (e.ErrorWebSockets == null)
                {
                    _trace.writeToLog(1, "CLSyncbox: OnPushNotificationConnectionError: Entry. ERROR: Manual poll error message: <{0}>. Manual poll error code: {1}.", e.ErrorStillDisconnectedPing.PrimaryException.Message, e.ErrorStillDisconnectedPing.PrimaryException.Code);
                    e.ErrorStillDisconnectedPing.Log(_copiedSettings.TraceLocation, _copiedSettings.LogErrors);
                }
                else
                {
                    _trace.writeToLog(1, "CLSyncbox: OnPushNotificationConnectionError: Entry. ERROR: Web socket error message: <{0}>. Web socket error code: {1}. Manual poll error message: <{2}>. Manual poll error code: {3}.", e.ErrorWebSockets.PrimaryException.Message, e.ErrorWebSockets.PrimaryException.Code, e.ErrorStillDisconnectedPing.PrimaryException.Message, e.ErrorStillDisconnectedPing.PrimaryException.Code);
                    e.ErrorWebSockets.Log(_copiedSettings.TraceLocation, _copiedSettings.LogErrors);
                    e.ErrorStillDisconnectedPing.Log(_copiedSettings.TraceLocation, _copiedSettings.LogErrors);
                }

                // Tell the application
                if (PushNotificationError != null)
                {
                    _trace.writeToLog(1, "CLSyncbox: OnPushNotificationConnectionError: Notify the application.");
                    PushNotificationError(this, e);
                }
            }
            catch
            {
            }
        }

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
                throw new Exception("Object disposed");  //&&&& fix
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
                throw new Exception("Object disposed");  //&&&& fix
            }
            if (!isOneOff)
            {
                Helpers.CheckHalted();
            }
        }
        // Disposing this object provides no user functionality, so we are hiding Dispose behind its interface.
        ///// <summary>
        ///// Call this to cleanup FileSystemWatchers such as on application shutdown,
        ///// do not start the same monitor instance after it has been disposed 
        ///// </summary>
        //public CLError Dispose()
        //{
        //    try
        //    {
        //        ((IDisposable)this).Dispose();
        //    }
        //    catch (Exception ex)
        //    {
        //        return ex;
        //    }
        //    return null;
        //}

        // Standard IDisposable implementation based on MSDN System.IDisposable
        void IDisposable.Dispose()
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