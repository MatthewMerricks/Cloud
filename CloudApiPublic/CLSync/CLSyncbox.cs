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
        #region Private Fields
        
        private static readonly CLTrace _trace = CLTrace.Instance;
        private static readonly List<CLSyncEngine> _startedSyncEngines = new List<CLSyncEngine>();
        private static readonly object _startLocker = new object();

        private readonly CLSyncEngine _syncEngine;
        private bool _isStarted = false;
        private bool Disposed = false;   // This stores if this current instance has been disposed (defaults to not disposed)
        private readonly ReaderWriterLockSlim _propertyChangeLocker = new ReaderWriterLockSlim();  // for locking any reads and writes to the changeable properties.


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
                return _httpRestClient.IsModifyingSyncboxViaPublicAPICalls;
            }
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        /// <summary>
        /// Internal client for passing HTTP REST calls to the server
        /// </summary>
        public CLHttpRest HttpRestClient
        {
            get
            {
                return _httpRestClient;
            }
        }
        private readonly CLHttpRest _httpRestClient;

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
        /// The friendly name of this Syncbox in the cloud.
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

        /// <summary>
        /// The full path on the disk associated with this syncbox.
        /// </summary>
        public string Path
        {
            get
            {
                return _path;
            }
        }
        private string _path;

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

        private CLSyncbox(
            long syncboxId,
            CLCredentials credentials,
            string path = null,
            ICLSyncSettings settings = null,
            Helpers.ReplaceExpiredCredentials getNewCredentialsCallback = null,
            object getNewCredentialsCallbackUserState = null)
        {
            // check input parameters

            if (syncboxId == 0)
            {
                throw new ArgumentException("syncboxId must be specified");
            }

            if (credentials == null)
            {
                throw new NullReferenceException("Credentials cannot be null");
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

            // Set up the syncbox
            lock (_startLocker)
            {
                // Save the parameters in properties.
                this.Credentials = credentials;
                this._syncboxId = syncboxId;
                this._path = path;

                // Initialize trace in case it is not already initialized.
                CLTrace.Initialize(this._copiedSettings.TraceLocation, "Cloud", "log", this._copiedSettings.TraceLevel, this._copiedSettings.LogErrors);
                _trace.writeToLog(1, "CLSyncbox: Constructing...");

                // Create the http rest client
                _trace.writeToLog(9, "CLSyncbox: Start: Create rest client.");
                CLError createRestClientError = CLHttpRest.CreateAndInitialize(
                                credentials: this.Credentials,
                                syncbox: this,
                                client: out _httpRestClient,
                                settings: this._copiedSettings,
                                getNewCredentialsCallback: getNewCredentialsCallback,
                                getNewCredentialsCallbackUserState: getNewCredentialsCallbackUserState);
                if (createRestClientError != null)
                {
                    _trace.writeToLog(1, "CLSyncbox: Construction: ERROR: Msg: {0}. Code: {1}.", createRestClientError.PrimaryException.Message, createRestClientError.PrimaryException.Code.ToString());
                    throw new CLException(CLExceptionCode.Syncbox_CreateRestClient, "Error creating REST HTTP client", createRestClientError.Exceptions);
                }
                if (_httpRestClient == null)
                {
                    const string nullRestClient = "Unknown error creating HTTP REST client";
                    _trace.writeToLog(1, "CLSyncbox: Construction: ERROR: Msg: {0}.", nullRestClient);
                    throw new CLNullReferenceException(CLExceptionCode.Syncbox_CreateRestClient, nullRestClient);
                }

                // We need to validate the syncbox ID with the server with these credentials.  We will also retrieve the other syncbox
                // properties from the server and set them into this local object's properties.
                CLError errorFromStatus = GetCurrentSyncboxStatus();
                if (errorFromStatus != null)
                {
                    throw new CLException(CLExceptionCode.Syncbox_InitialStatus, "Error getting syncbox status from Cloud", errorFromStatus.Exceptions);
                }

                // Create the sync engine
                _syncEngine = new CLSyncEngine();
            }
        }

        /// <summary>
        /// Private constructor to create a functional CLSyncbox object from a JsonContracts.Syncbox.
        /// </summary>
        /// <param name="syncboxContract">The syncbox contract to use.</param>
        /// <param name="credentials">The credentials to use.</param>
        /// <param name="settings">The settings to use.</param>
        /// <param name="getNewCredentialsCallback">The delegate to call for getting new temporary credentials.</param>
        /// <param name="getNewCredentialsCallbackUserState">The user state to pass to the delegate above.</param>
        private CLSyncbox(
            JsonContracts.Syncbox syncboxContract,
            CLCredentials credentials,
            ICLSyncSettings settings,
            Helpers.ReplaceExpiredCredentials getNewCredentialsCallback = null,
            object getNewCredentialsCallbackUserState = null)
        {
            CheckDisposed();

            // check input parameters

            if (syncboxContract == null)
            {
                throw new NullReferenceException("syncboxContract must not be null");
            }
            if (syncboxContract.Id == null)
            {
                throw new NullReferenceException("syncboxContract Id must not be null");
            }
            if (syncboxContract.PlanId == null)
            {
                throw new NullReferenceException("syncboxContract Id must not be null");
            }
            if (credentials == null)
            {
                throw new NullReferenceException("credentials must not be null");
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

            // Set up the syncbox
            lock (_startLocker)
            {
                // Save the parameters in properties.
                this.Credentials = credentials;
                this._syncboxId = (long)syncboxContract.Id;
                this._path = null;      // the server doesn't know the local path.  The user must provide that later.
                this._storagePlanId = (long)syncboxContract.PlanId;
                this._friendlyName = syncboxContract.FriendlyName;

                // Initialize trace in case it is not already initialized.
                CLTrace.Initialize(this._copiedSettings.TraceLocation, "Cloud", "log", this._copiedSettings.TraceLevel, this._copiedSettings.LogErrors);
                _trace.writeToLog(1, "CLSyncbox: Constructing from contract...");

                // Create the http rest client
                _trace.writeToLog(9, "CLSyncbox: CLSyncbox(contract): Create rest client.");
                CLError createRestClientError = CLHttpRest.CreateAndInitialize(
                                credentials: this.Credentials,
                                syncbox: this,
                                client: out _httpRestClient,
                                settings: this._copiedSettings,
                                getNewCredentialsCallback: getNewCredentialsCallback,
                                getNewCredentialsCallbackUserState: getNewCredentialsCallbackUserState);
                if (createRestClientError != null)
                {
                    _trace.writeToLog(1, "CLSyncbox: CLSyncbox(contract): ERROR: Msg: {0}. Code: {1}.", createRestClientError.PrimaryException.Message, createRestClientError.PrimaryException.Code);
                    throw new CLException(CLExceptionCode.Syncbox_CreateRestClient, "Error creating REST HTTP client", createRestClientError.Exceptions);
                }
                if (_httpRestClient == null)
                {
                    const string nullRestClient = "Unknown error creating HTTP REST client";
                    _trace.writeToLog(1, "CLSyncbox: CLSyncbox(contract): ERROR: Msg: {0}.", nullRestClient);
                    throw new CLNullReferenceException(CLExceptionCode.Syncbox_CreateRestClient, nullRestClient);
                }

                // Create the sync engine
                _syncEngine = new CLSyncEngine();
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
        /// <param name="path">(optional) The full path of the folder on disk to associate with this syncbox. If this parameter is null, the syncbox local disk directory will be %USEERPROFILE%\Cloud. </param>
        /// <param name="settings">(optional) settings to use with this method.</param>
        /// <param name="getNewCredentialsCallback">(optional) A delegate which will be called to retrieve a new set of credentials when credentials have expired.</param>
        /// <param name="getNewCredentialsCallbackUserState">(optional) The user state to pass as a parameter to the delegate above.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public static IAsyncResult BeginAllocAndInit(
            AsyncCallback callback,
            object callbackUserState,
            long syncboxId,
            CLCredentials credentials,
            string path = null,
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
        /// <param name="path">(optional) The full path of the folder on disk to associate with this syncbox. If this parameter is null, the syncbox local disk directory will be %USEERPROFILE%\Cloud. </param>
        /// <param name="settings">(optional) Settings to use with this request</param>
        /// <param name="getNewCredentialsCallback">(optional) A delegate that will be called to provide new credentials when the current credentials token expires.</param>
        /// <param name="getNewCredentialsCallbackUserState">(optional) The user state that will be passed back to the getNewCredentialsCallback delegate.</param>
        /// <returns>Returns any error which occurred during object allocation or initialization, if any, or null.</returns>
        public static CLError AllocAndInit(
            long syncboxId,
            CLCredentials credentials,
            out CLSyncbox syncbox,
            string path = null,
            ICLSyncSettings settings = null,
            Helpers.ReplaceExpiredCredentials getNewCredentialsCallback = null,
            object getNewCredentialsCallbackUserState = null)
        {
            Helpers.CheckHalted();

            try
            {
                if (Helpers.AllHaltedOnUnrecoverableError)
                {
                    throw new InvalidOperationException("Cannot do anything with the Cloud SDK if Helpers.AllHaltedOnUnrecoverableError is set");
                }

                syncbox = new CLSyncbox(
                    syncboxId: syncboxId,
                    credentials: credentials,
                    path: path,
                    settings: settings,
                    getNewCredentialsCallback: getNewCredentialsCallback,
                    getNewCredentialsCallbackUserState: getNewCredentialsCallbackUserState
                    );
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
        public CLError BeginSync(
                CLSyncMode mode,
                string path = null,
                System.Threading.WaitCallback syncStatusChangedCallback = null,
                object syncStatusChangedCallbackUserState = null)
        {
            CheckDisposed();

            bool startExceptionLogged = false;

            try
            {
                lock (_startLocker)
                {
                    if (_syncEngine == null)
                    {
                        throw new NullReferenceException("syncEngine must not be null");
                    }
                    if (mode == CLSyncMode.CLSyncModeOnDemand)
                    {
                        throw new ArgumentException("CLSyncMode.CLSyncModeOnDemand is not supported");
                    }


                    if (path != null)
                    {
                        int nOutTooLongChars;
                        CLError errorPathTooLong = Helpers.CheckSyncRootLength(_path, out nOutTooLongChars);
                        if (errorPathTooLong != null)
                        {
                            throw new CLArgumentException(errorPathTooLong.PrimaryException.Code, string.Format("syncbox path is too long by {0} characters.", nOutTooLongChars), errorPathTooLong.Exceptions);
                        }

                        CLError errorBadPath = Helpers.CheckForBadPath(_path);
                        if (errorBadPath != null)
                        {
                            throw new CLArgumentException(errorBadPath.PrimaryException.Code, "syncbox path contains invalid characters.", errorBadPath.Exceptions);
                        }
                        _path = path;
                    }

                    _syncMode = mode;

                    // Start the sync engine
                    CLSyncStartStatus startStatus;
                    CLError syncEngineStartError = _syncEngine.Start(
                        Syncbox: this, // syncbox to sync (contains required settings)
                        Status: out startStatus, // The completion status of the Start() function
                        StatusUpdated: syncStatusChangedCallback, // called when sync status is updated
                        StatusUpdatedUserState: syncStatusChangedCallbackUserState); // the user state passed to the callback above

                    if (syncEngineStartError != null)
                    {
                        _trace.writeToLog(1, "Error starting sync engine. Msg: {0}. Code: {1}.", syncEngineStartError.PrimaryException.Message, syncEngineStartError.PrimaryException.Code);
                        syncEngineStartError.Log(_copiedSettings.TraceLocation, _copiedSettings.LogErrors);
                        startExceptionLogged = true;
                        throw new CLException(syncEngineStartError.PrimaryException.Code, "Error starting active syncing", syncEngineStartError.Exceptions);
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
        public void EndSync()
        {
            CheckDisposed();

            try
            {
                lock (_startLocker)
                {
                    if (!_isStarted)
                    {
                        return;
                    }

                    if (_syncEngine == null)
                    {
                        return;
                    }

                    // Stop the sync engine.
                    _syncEngine.Stop();

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
                        throw new CLInvalidOperationException(CLExceptionCode.General_Invalid, "Stop the syncbox first.");
                    }

                    // Reset the sync engine
                    CLError resetSyncError = _syncEngine.SyncReset(this);
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
                    throw new InvalidOperationException("Cannot do anything with the Cloud SDK if Helpers.AllHaltedOnUnrecoverableError is set");
                }

                lock (_startLocker)
                {
                    if (!_isStarted)
                    {
                        throw new InvalidOperationException("Start the syncbox first.");
                    }

                    if (_syncEngine == null)
                    {
                        //throw new NullReferenceException("Sync not started");
                        status = new CLSyncCurrentStatus(CLSyncCurrentState.Idle, null);
                        return null;
                    }
                    else
                    {
                        return _syncEngine.GetCurrentStatus(out status);
                    }
                }
            }
            catch (Exception ex)
            {
                status = Helpers.DefaultForType<CLSyncCurrentStatus>();
                return ex;
            }
        }

        /// <summary>
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

        #region CreateSyncbox (create a syncbox in the cloud)

        /// <summary>
        /// Asynchronously starts creating a new Syncbox in the cloud.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes.  Can be null.</param>
        /// <param name="callbackUserState">Userstate to pass to the callback when it is fired.  Can be null.</param>
        /// <param name="plan">The storage plan to use with this Syncbox.</param>
        /// <param name="credentials">The credentials to use with this request.</param>
        /// <param name="friendlyName">(optional) The friendly name of the Syncbox.</param>
        /// <param name="settings">(optional) Settings to use with this method.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public static IAsyncResult BeginCreateSyncbox(
                    AsyncCallback callback,
                    object callbackUserState,
                    CLStoragePlan plan,
                    CLCredentials credentials,
                    string friendlyName = null,
                    ICLSyncSettings settings = null,
                    Helpers.ReplaceExpiredCredentials getNewCredentialsCallback = null,
                    object getNewCredentialsCallbackUserState = null)
        {
            Helpers.CheckHalted();

            // Check the parameters
            if (plan == null)
            {
                throw new ArgumentNullException("plan must not be null");
            }
            if (credentials == null)
            {
                throw new ArgumentNullException("credentials must not be null");
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
        /// Finishes creating a Syncbox in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
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
        /// Create a Syncbox in the cloud for the current application.  This is a synchronous method.
        /// </summary>
        /// <param name="plan">The storage plan to use with this Syncbox.</param>
        /// <param name="credentials">The credentials to use for this request.</param>
        /// <param name="syncbox">(output) Response object from communication</param>
        /// <param name="name">(optional) The friendly name of the Syncbox.</param>
        /// <param name="settings">(optional) The settings to use with this method</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public static CLError CreateSyncbox(
                    CLStoragePlan plan,
                    CLCredentials credentials,
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
                    throw new ArgumentNullException("plan must not be null");
                }
                if (credentials == null)
                {
                    throw new ArgumentNullException("credentials must not be null");
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
                    SyncboxId: null);

                // Check the server response.
                if (responseFromServer == null)
                {
                    throw new NullReferenceException("Response from server must not be null");
                }
                if (responseFromServer.Syncbox == null)
                {
                    throw new NullReferenceException("Server response syncbox must not be null");
                }

                // Convert the response object to a CLSyncbox and return that.
                syncbox = new CLSyncbox(responseFromServer.Syncbox, credentials, copiedSettings, getNewCredentialsCallback, getNewCredentialsCallbackUserState);
            }
            catch (Exception ex)
            {
                syncbox = Helpers.DefaultForType<CLSyncbox>();
                return ex;
            }
            return null;
        }
        #endregion

        #region DeleteSyncbox (delete a syncbox in the cloud)

        /// <summary>
        /// Asynchronously starts deleting a new Syncbox in the cloud.
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
        /// Finishes deleting a Syncbox in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
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
        /// Delete a Syncbox in the cloud.  This is a synchronous method.
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
                    SyncboxId: null);

            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.SyncboxDeleteResponse>();
                return ex;
            }
            return null;
        }
        #endregion

        #region ListAllSyncboxesWithCredentials (list syncboxes in the cloud)

        /// <summary>
        /// Asynchronously starts listing syncboxes in the cloud.
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
        /// Finishes listing syncboxes in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
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
        /// List syncboxes in the cloud for these credentials.  This is a synchronous method.
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
                    out CLSyncbox [] response,
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
                    SyncboxId: null);

                // Convert the server response to a list of initialized CLSyncboxes.
                if (responseFromServer != null && responseFromServer.Syncboxes != null)
                {
                    List<CLSyncbox> listSyncboxes = new List<CLSyncbox>();
                    foreach (JsonContracts.Syncbox syncbox in responseFromServer.Syncboxes)
                    {
                        if (syncbox != null)
                        {
                            listSyncboxes.Add(new CLSyncbox(syncbox, credentials, copiedSettings, getNewCredentialsCallback, getNewCredentialsCallbackUserState));
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
                    throw new NullReferenceException("Server responded without an array of Sessions");
                }

            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<CLSyncbox[]>();
                return ex;
            }
            return null;
        }
        #endregion  // end List (list syncboxes in the cloud)

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

        #region GetItemAtPath (Queries the cloud for the item at a particular path)
        /// <summary>
        /// Asynchronously starts querying the server at a given path (must be specified) for existing metadata at that path; outputs a CLFileItem object.
        /// Check for Deleted flag being true in case the metadata represents a deleted item
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="path">Full path to where file or folder would exist locally on disk.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetItemAtPath(AsyncCallback callback, object callbackUserState, string path)
        {
            CheckDisposed();
            return _httpRestClient.BeginGetItemAtPath(callback, callbackUserState, path);
        }

        /// <summary>
        /// Finishes a metadata query, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) The result from the metadata query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetItemAtPath(IAsyncResult aResult, out SyncboxGetItemAtPathResult result)
        {
            CheckDisposed();
            return _httpRestClient.EndGetItemAtPath(aResult, out result);
        }

        /// <summary>
        /// Queries the server at a given file or folder path (must be specified) for existing metadata at that path
        /// Check for Deleted flag being true in case the metadata represents a deleted item
        /// </summary>
        /// <param name="path">Full path to where file or folder would exist locally on disk</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetItemAtPath(string path, CLFileItem response)
        {
            CheckDisposed();
            return _httpRestClient.GetItemAtPath(path, out response);
        }

        #endregion  // end GetItemAtPath (Queries the cloud for the item at a particular path)

        #region RenameFile (Renames a file in the cloud)
        /// <summary>
        /// Asynchronously starts renaming a file in the cloud; outputs a CLFileItem object.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="path">Full path to where the file would exist locally on disk.</param>
        /// <param name="newPath">Full path to the new location of the file.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginRenameFile(AsyncCallback callback, object callbackUserState, string path, string newPath)
        {
            CheckDisposed();
            string[] paths = new string[1] { path };
            string[] newPaths = new string[1] { newPath };
            return _httpRestClient.BeginRenameFiles(callback, callbackUserState, paths, newPaths);
        }

        /// <summary>
        /// Finishes renaming a file in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) The result from the metadata query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndRenameFile(IAsyncResult aResult, out SyncboxRenameFileResult result)
        {
            CheckDisposed();

            // Complete the async operation.
            SyncboxRenameFilesResult results;
            CLError error = _httpRestClient.EndRenameFiles(aResult, out results);

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
                return new CLError(new CLException(CLExceptionCode.Rest_Syncbox_File_Rename_No_Server_Responses_Or_Errors, "No error or responses from server results null"));
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
                // No overall error, got a results object, is has no errors, and it has a rename response.  This is the normal case.  Return that rename response as the result.
                result = new SyncboxRenameFileResult(error: null, _fileItem: results.FileItems[0]);
                return null;        // normal condition
            }
            // ((error == null && results != null) && (results.Errors == null || results.Errors.Length == 0)) && (results.Responses == null || results.Responses.Length == 0)
            else
            {
                // No error, got a results object, but there were no errors and no rename responses inside.  Return an error.
                result = null;
                return new CLError(new CLException(CLExceptionCode.Rest_Syncbox_File_Rename_No_Server_Responses_Or_Errors, "No error or responses from server"));
            }
        }

        /// <summary>
        /// Renames a file in the cloud.
        /// </summary>
        /// <param name="path">Full path to where the file would exist locally on disk</param>
        /// <param name="newPath">Full path to the new location of the file.</param>
        /// <param name="fileItem">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError RenameFile(string path, string newPath, out CLFileItem fileItem)
        {
            CheckDisposed();
            string[] paths = new string[1] { path };
            string[] newPaths = new string[1] { newPath };

            // Communicate and get the results.
            CLError[] outErrors;
            CLFileItem[] outItems;
            CLError error = _httpRestClient.RenameFiles(paths, newPaths, out outItems, out outErrors);

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
                return new CLError(new CLException(CLExceptionCode.Rest_Syncbox_File_Rename_No_Server_Responses_Or_Errors, "No responses or status from serer"));
            }
        }

        #endregion  // end GetItemAtPath (Queries the cloud for the item at a particular path)

        #region RenameFiles (Rename files in the cloud)
        /// <summary>
        /// Asynchronously starts renaming files in the cloud; outputs an array of  CLFileItem objects, and possibly an array of CLError objects.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="paths">An array of full paths to where the files would exist locally on disk.</param>
        /// <param name="newPaths">An array of full paths to the new location of the files, corresponding to the paths array.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginRenameFiles(AsyncCallback callback, object callbackUserState, string [] paths, string [] newPaths)
        {
            CheckDisposed();
            return _httpRestClient.BeginRenameFiles(callback, callbackUserState, paths, newPaths);
        }

        /// <summary>
        /// Finishes renaming files in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) The result from the metadata query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndRenameFiles(IAsyncResult aResult, out SyncboxRenameFilesResult result)
        {
            CheckDisposed();
            return _httpRestClient.EndRenameFiles(aResult, out result);
        }

        /// <summary>
        /// Renames files in the cloud.
        /// </summary>
        /// <param name="paths">An array of full paths to where the files would exist locally on disk.</param>
        /// <param name="newPaths">An array of full paths to the new location of the files, corresponding to the paths array.</param>
        /// <param name="fileItems">(output) response object from communication</param>
        /// <param name="errors">(output) Any errors that occur, or null</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError RenameFiles(string [] paths, string [] newPaths, out CLFileItem [] fileItems, out CLError [] errors)
        {
            CheckDisposed();
            return _httpRestClient.RenameFiles(paths, newPaths, out fileItems, out errors);
        }

        #endregion  // end GetItemAtPath (Queries the cloud for the item at a particular path)

        #region RenameFolder (Renames a folder in the cloud)
        /// <summary>
        /// Asynchronously starts renaming a folder in the cloud; outputs a CLFileItem object.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="path">Full path to where the folder would exist locally on disk.</param>
        /// <param name="newPath">Full path to the new location of the folder.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginRenameFolder(AsyncCallback callback, object callbackUserState, string path, string newPath)
        {
            CheckDisposed();
            string[] paths = new string[1] { path };
            string[] newPaths = new string[1] { newPath };
            return _httpRestClient.BeginRenameFolders(callback, callbackUserState, paths, newPaths);
        }

        /// <summary>
        /// Finishes renaming a folder in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) The result from the metadata query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndRenameFolder(IAsyncResult aResult, out SyncboxRenameFolderResult result)
        {
            CheckDisposed();

            // Complete the async operation.
            SyncboxRenameFoldersResult results;
            CLError error = _httpRestClient.EndRenameFolders(aResult, out results);

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
                return new CLError(new CLException(CLExceptionCode.Rest_Syncbox_Folder_Rename_No_Server_Responses_Or_Errors, "No error or responses from server results null"));
            }
            // error == null && results != null  (no overall error, and we got a results object)
            else if (results.Errors != null && results.Errors.Length >= 1)
            {
                // No overall error, got a results object, and it has an error.  Return that error.
                result = null;
                return results.Errors[0];
            }
            // (error == null && results != null) && (results.Errors == null || results.Errors.Length == 0)  (no overall error, we got a results object, and there are no errors in results)
            else if (results.FolderItems != null && results.FolderItems.Length >= 1)
            {
                // No overall error, got a results object, is has no errors, and it has a rename response.  This is the normal case.  Return that rename response as the result.
                result = new SyncboxRenameFolderResult(error: null, folderItem: results.FolderItems[0]);
                return null;        // normal condition
            }
            // ((error == null && results != null) && (results.Errors == null || results.Errors.Length == 0)) && (results.Responses == null || results.Responses.Length == 0)
            else
            {
                // No error, got a results object, but there were no errors and no rename responses inside.  Return an error.
                result = null;
                return new CLError(new CLException(CLExceptionCode.Rest_Syncbox_Folder_Rename_No_Server_Responses_Or_Errors, "No error or responses from server"));
            }
        }

        /// <summary>
        /// Renames a folder in the cloud.
        /// </summary>
        /// <param name="path">Full path to where the folder would exist locally on disk</param>
        /// <param name="newPath">Full path to the new location of the folder.</param>
        /// <param name="folderItem">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError RenameFolder(string path, string newPath, out CLFileItem folderItem)
        {
            CheckDisposed();
            string[] paths = new string[1] { path };
            string[] newPaths = new string[1] { newPath };

            // Communicate and get the results.
            CLError[] outErrors;
            CLFileItem[] outItems;
            CLError error = _httpRestClient.RenameFolders(paths, newPaths, out outItems, out outErrors);

            // Return resulting error or item
            if (error != null)
            {
                // There was an overall error.  Return it
                folderItem = null;
                return error;
            }
            // error == null
            else if (outErrors != null && outErrors.Length >= 1)
            {
                // No overall error, but there was an item error.  Return it.
                folderItem = null;
                return outErrors[0];
            }
            // error == null && (outErrors == null || outErrors.Length == 0)
            else if (outItems != null && outItems.Length >= 1)
            {
                // No overall error, no item errors, and we have an item.  Return it.  This is the normal condition
                folderItem = outItems[0];
                return null;
            }
            // (error == null && (outErrors == null || outErrors.Length == 0)) && (outItems == null || outItems.Length == 0)
            else
            {
                // No overall error, no item errors, and no items.  No responses from server.  Return error.
                folderItem = null;
                return new CLError(new CLException(CLExceptionCode.Rest_Syncbox_Folder_Rename_No_Server_Responses_Or_Errors, "No responses or status from serer"));
            }
        }

        #endregion  // end RenameFolder (Renames a folder in the cloud)

        #region RenameFolders (Rename folders in the cloud)
        /// <summary>
        /// Asynchronously starts renaming folders in the cloud; outputs an array of  CLFileItem objects, and possibly an array of CLError objects.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="paths">An array of full paths to where the folders would exist locally on disk.</param>
        /// <param name="newPaths">An array of full paths to the new location of the folders, corresponding to the paths array.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginRenameFolders(AsyncCallback callback, object callbackUserState, string[] paths, string[] newPaths)
        {
            CheckDisposed();
            return _httpRestClient.BeginRenameFolders(callback, callbackUserState, paths, newPaths);
        }

        /// <summary>
        /// Finishes renaming folders in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) The result from the metadata query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndRenameFolders(IAsyncResult aResult, out SyncboxRenameFoldersResult result)
        {
            CheckDisposed();
            return _httpRestClient.EndRenameFolders(aResult, out result);
        }

        /// <summary>
        /// Renames folders in the cloud.
        /// </summary>
        /// <param name="paths">An array of full paths to where the folders would exist locally on disk.</param>
        /// <param name="newPaths">An array of full paths to the new location of the folders, corresponding to the paths array.</param>
        /// <param name="folderItems">(output) response object from communication</param>
        /// <param name="errors">(output) Any errors that occur, or null.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError RenameFolders(string[] paths, string[] newPaths, out CLFileItem[] folderItems, out CLError[] errors)
        {
            CheckDisposed();
            return _httpRestClient.RenameFolders(paths, newPaths, out folderItems, out errors);
        }

        #endregion  // end RenameFolders (Rename folders in the cloud)

        #region MoveFile (Moves a file in the cloud)
        /// <summary>
        /// Asynchronously starts renaming a file in the cloud; outputs a CLFileItem object.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="path">Full path to where the file would exist locally on disk.</param>
        /// <param name="newPath">Full path to the new location of the file.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginMoveFile(AsyncCallback callback, object callbackUserState, string path, string newPath)
        {
            CheckDisposed();
            string[] paths = new string[1] { path };
            string[] newPaths = new string[1] { newPath };
            return _httpRestClient.BeginRenameFiles(callback, callbackUserState, paths, newPaths);
        }

        /// <summary>
        /// Finishes renaming a file in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) The result from the metadata query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndMoveFile(IAsyncResult aResult, out SyncboxMoveFileResult result)
        {
            CheckDisposed();

            // Complete the async operation.
            SyncboxRenameFilesResult results;
            CLError error = _httpRestClient.EndRenameFiles(aResult, out results);

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
                return new CLError(new CLException(CLExceptionCode.Rest_Syncbox_File_Move_No_Server_Responses_Or_Errors, "No error or responses from server results null"));
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
                // No overall error, got a results object, is has no errors, and it has a rename response.  This is the normal case.  Return that rename response as the result.
                result = new SyncboxMoveFileResult(error: null, fileItem: results.FileItems[0]);
                return null;        // normal condition
            }
            // ((error == null && results != null) && (results.Errors == null || results.Errors.Length == 0)) && (results.Responses == null || results.Responses.Length == 0)
            else
            {
                // No error, got a results object, but there were no errors and no rename responses inside.  Return an error.
                result = null;
                return new CLError(new CLException(CLExceptionCode.Rest_Syncbox_File_Move_No_Server_Responses_Or_Errors, "No error or responses from server"));
            }
        }

        /// <summary>
        /// Moves a file in the cloud.
        /// </summary>
        /// <param name="path">Full path to where the file would exist locally on disk</param>
        /// <param name="newPath">Full path to the new location of the file.</param>
        /// <param name="fileItem">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError MoveFile(string path, string newPath, out CLFileItem fileItem)
        {
            CheckDisposed();
            string[] paths = new string[1] { path };
            string[] newPaths = new string[1] { newPath };

            // Communicate and get the results.
            CLError[] outErrors;
            CLFileItem[] outItems;
            CLError error = _httpRestClient.RenameFiles(paths, newPaths, out outItems, out outErrors);

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
                return new CLError(new CLException(CLExceptionCode.Rest_Syncbox_File_Move_No_Server_Responses_Or_Errors, "No responses or status from serer"));
            }
        }

        #endregion  // end GetItemAtPath (Queries the cloud for the item at a particular path)

        #region MoveFiles (Move files in the cloud)
        /// <summary>
        /// Asynchronously starts renaming files in the cloud; outputs an array of  CLFileItem objects, and possibly an array of CLError objects.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="paths">An array of full paths to where the files would exist locally on disk.</param>
        /// <param name="newPaths">An array of full paths to the new location of the files, corresponding to the paths array.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginMoveFiles(AsyncCallback callback, object callbackUserState, string[] paths, string[] newPaths)
        {
            CheckDisposed();
            return _httpRestClient.BeginRenameFiles(callback, callbackUserState, paths, newPaths);
        }

        /// <summary>
        /// Finishes renaming files in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) The result from the metadata query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndMoveFiles(IAsyncResult aResult, out SyncboxMoveFilesResult result)
        {
            CheckDisposed();
            SyncboxRenameFilesResult renameResult;
            CLError error = _httpRestClient.EndRenameFiles(aResult, out renameResult);

            if (error != null)
            {
                result = null;
                return error;
            }

            result = new SyncboxMoveFilesResult(renameResult.OverallError, renameResult.Errors, renameResult.FileItems);
            return error;
        }

        /// <summary>
        /// Moves files in the cloud.
        /// </summary>
        /// <param name="paths">An array of full paths to where the files would exist locally on disk.</param>
        /// <param name="newPaths">An array of full paths to the new location of the files, corresponding to the paths array.</param>
        /// <param name="fileItems">(output) response object from communication</param>
        /// <param name="errors">(output) Any errors that occur, or null</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError MoveFiles(string[] paths, string[] newPaths, out CLFileItem[] fileItems, out CLError[] errors)
        {
            CheckDisposed();
            return _httpRestClient.RenameFiles(paths, newPaths, out fileItems, out errors);
        }

        #endregion  // end GetItemAtPath (Queries the cloud for the item at a particular path)

        #region MoveFolder (Moves a folder in the cloud)
        /// <summary>
        /// Asynchronously starts renaming a folder in the cloud; outputs a CLFileItem object.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="path">Full path to where the folder would exist locally on disk.</param>
        /// <param name="newPath">Full path to the new location of the folder.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginMoveFolder(AsyncCallback callback, object callbackUserState, string path, string newPath)
        {
            CheckDisposed();
            string[] paths = new string[1] { path };
            string[] newPaths = new string[1] { newPath };
            return _httpRestClient.BeginRenameFolders(callback, callbackUserState, paths, newPaths);
        }

        /// <summary>
        /// Finishes renaming a folder in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) The result from the metadata query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndMoveFolder(IAsyncResult aResult, out SyncboxMoveFolderResult result)
        {
            CheckDisposed();

            // Complete the async operation.
            SyncboxRenameFoldersResult results;
            CLError error = _httpRestClient.EndRenameFolders(aResult, out results);

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
                return new CLError(new CLException(CLExceptionCode.Rest_Syncbox_Folder_Move_No_Server_Responses_Or_Errors, "No error or responses from server results null"));
            }
            // error == null && results != null  (no overall error, and we got a results object)
            else if (results.Errors != null && results.Errors.Length >= 1)
            {
                // No overall error, got a results object, and it has an error.  Return that error.
                result = null;
                return results.Errors[0];
            }
            // (error == null && results != null) && (results.Errors == null || results.Errors.Length == 0)  (no overall error, we got a results object, and there are no errors in results)
            else if (results.FolderItems != null && results.FolderItems.Length >= 1)
            {
                // No overall error, got a results object, is has no errors, and it has a rename response.  This is the normal case.  Return that rename response as the result.
                result = new SyncboxMoveFolderResult(error: null, folderItem: results.FolderItems[0]);
                return null;        // normal condition
            }
            // ((error == null && results != null) && (results.Errors == null || results.Errors.Length == 0)) && (results.Responses == null || results.Responses.Length == 0)
            else
            {
                // No error, got a results object, but there were no errors and no rename responses inside.  Return an error.
                result = null;
                return new CLError(new CLException(CLExceptionCode.Rest_Syncbox_Folder_Move_No_Server_Responses_Or_Errors, "No error or responses from server"));
            }
        }

        /// <summary>
        /// Moves a folder in the cloud.
        /// </summary>
        /// <param name="path">Full path to where the folder would exist locally on disk</param>
        /// <param name="newPath">Full path to the new location of the folder.</param>
        /// <param name="folderItem">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError MoveFolder(string path, string newPath, out CLFileItem folderItem)
        {
            CheckDisposed();
            string[] paths = new string[1] { path };
            string[] newPaths = new string[1] { newPath };

            // Communicate and get the results.
            CLError[] outErrors;
            CLFileItem[] outItems;
            CLError error = _httpRestClient.RenameFolders(paths, newPaths, out outItems, out outErrors);

            // Return resulting error or item
            if (error != null)
            {
                // There was an overall error.  Return it
                folderItem = null;
                return error;
            }
            // error == null
            else if (outErrors != null && outErrors.Length >= 1)
            {
                // No overall error, but there was an item error.  Return it.
                folderItem = null;
                return outErrors[0];
            }
            // error == null && (outErrors == null || outErrors.Length == 0)
            else if (outItems != null && outItems.Length >= 1)
            {
                // No overall error, no item errors, and we have an item.  Return it.  This is the normal condition
                folderItem = outItems[0];
                return null;
            }
            // (error == null && (outErrors == null || outErrors.Length == 0)) && (outItems == null || outItems.Length == 0)
            else
            {
                // No overall error, no item errors, and no items.  No responses from server.  Return error.
                folderItem = null;
                return new CLError(new CLException(CLExceptionCode.Rest_Syncbox_Folder_Move_No_Server_Responses_Or_Errors, "No responses or status from serer"));
            }
        }

        #endregion  // end MoveFolder (Moves a folder in the cloud)

        #region MoveFolders (Move folders in the cloud)
        /// <summary>
        /// Asynchronously starts renaming folders in the cloud; outputs an array of  CLFileItem objects, and possibly an array of CLError objects.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="paths">An array of full paths to where the folders would exist locally on disk.</param>
        /// <param name="newPaths">An array of full paths to the new location of the folders, corresponding to the paths array.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginMoveFolders(AsyncCallback callback, object callbackUserState, string[] paths, string[] newPaths)
        {
            CheckDisposed();
            return _httpRestClient.BeginRenameFolders(callback, callbackUserState, paths, newPaths);
        }

        /// <summary>
        /// Finishes renaming folders in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) The result from the metadata query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndMoveFolders(IAsyncResult aResult, out SyncboxMoveFoldersResult result)
        {
            CheckDisposed();
            SyncboxRenameFoldersResult renameResult;
            CLError error = _httpRestClient.EndRenameFolders(aResult, out renameResult);

            if (error != null)
            {
                result = null;
                return error;
            }

            result = new SyncboxMoveFoldersResult(renameResult.OverallError, renameResult.Errors, renameResult.FolderItems);
            return error;
        }

        /// <summary>
        /// Moves folders in the cloud.
        /// </summary>
        /// <param name="paths">An array of full paths to where the folders would exist locally on disk.</param>
        /// <param name="newPaths">An array of full paths to the new location of the folders, corresponding to the paths array.</param>
        /// <param name="folderItems">(output) response object from communication</param>
        /// <param name="errors">(output) Any returned errors, or null.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError MoveFolders(string[] paths, string[] newPaths, out CLFileItem[] folderItems, out CLError[] errors)
        {
            CheckDisposed();
            return _httpRestClient.RenameFolders(paths, newPaths, out folderItems, out errors);
        }

        #endregion  // end MoveFolders (Move folders in the cloud)

        #region DeleteFile (Deletes a file in the cloud)
        /// <summary>
        /// Asynchronously starts deleting a file in the cloud; outputs a CLFileItem object.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="path">Full path to where the file would exist locally on disk.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginDeleteFile(AsyncCallback callback, object callbackUserState, string path)
        {
            CheckDisposed();
            string[] paths = new string[1] { path };
            return _httpRestClient.BeginDeleteFiles(callback, callbackUserState, paths);
        }

        /// <summary>
        /// Finishes deleting a file in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) The result from the metadata query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndDeleteFile(IAsyncResult aResult, out SyncboxDeleteFileResult result)
        {
            CheckDisposed();

            // Complete the async operation.
            SyncboxDeleteFilesResult results;
            CLError error = _httpRestClient.EndDeleteFiles(aResult, out results);

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
                return new CLError(new CLException(CLExceptionCode.Rest_Syncbox_File_Delete_No_Server_Responses_Or_Errors, "No error or responses from server results null"));
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
                result = new SyncboxDeleteFileResult(error: null, fileItem: results.FileItems[0]);
                return null;        // normal condition
            }
            // ((error == null && results != null) && (results.Errors == null || results.Errors.Length == 0)) && (results.Responses == null || results.Responses.Length == 0)
            else
            {
                // No error, got a results object, but there were no errors and no delete responses inside.  Return an error.
                result = null;
                return new CLError(new CLException(CLExceptionCode.Rest_Syncbox_File_Delete_No_Server_Responses_Or_Errors, "No error or responses from server"));
            }
        }

        /// <summary>
        /// Deletes a file in the cloud.
        /// </summary>
        /// <param name="path">Full path to where the file would exist locally on disk</param>
        /// <param name="fileItem">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError DeleteFile(string path, out CLFileItem fileItem)
        {
            CheckDisposed();
            string[] paths = new string[1] { path };

            // Communicate and get the results.
            CLError[] outErrors;
            CLFileItem[] outItems;
            CLError error = _httpRestClient.DeleteFiles(paths, out outItems, out outErrors);

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
                return new CLError(new CLException(CLExceptionCode.Rest_Syncbox_File_Delete_No_Server_Responses_Or_Errors, "No responses or status from serer"));
            }
        }

        #endregion  // end GetItemAtPath (Queries the cloud for the item at a particular path)

        #region DeleteFiles (Delete files in the cloud)
        /// <summary>
        /// Asynchronously starts deleting files in the cloud; outputs an array of  CLFileItem objects, and possibly an array of CLError objects.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="paths">An array of full paths to where the files would exist locally on disk.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginDeleteFiles(AsyncCallback callback, object callbackUserState, string[] paths)
        {
            CheckDisposed();
            return _httpRestClient.BeginDeleteFiles(callback, callbackUserState, paths);
        }

        /// <summary>
        /// Finishes deleting files in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) The result from the metadata query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndDeleteFiles(IAsyncResult aResult, out SyncboxDeleteFilesResult result)
        {
            CheckDisposed();
            SyncboxDeleteFilesResult deleteResult;
            CLError error = _httpRestClient.EndDeleteFiles(aResult, out deleteResult);

            if (error != null)
            {
                result = null;
                return error;
            }

            result = new SyncboxDeleteFilesResult(deleteResult.OverallError, deleteResult.Errors, deleteResult.FileItems);
            return error;
        }

        /// <summary>
        /// Deletes files in the cloud.
        /// </summary>
        /// <param name="paths">An array of full paths to where the files would exist locally on disk.</param>
        /// <param name="fileItems">(output) response object from communication</param>
        /// <param name="errors">(output) Any errors that occur, or null</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError DeleteFiles(string[] paths, out CLFileItem[] fileItems, out CLError[] errors)
        {
            CheckDisposed();
            return _httpRestClient.DeleteFiles(paths, out fileItems, out errors);
        }

        #endregion  // end DeleteFiles (Delete files in the cloud)

        #region DeleteFolder (Deletes a folder in the cloud)
        /// <summary>
        /// Asynchronously starts deleting a folder in the cloud; outputs a CLFileItem object.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="path">Full path to where the folder would exist locally on disk.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginDeleteFolder(AsyncCallback callback, object callbackUserState, string path)
        {
            CheckDisposed();
            string[] paths = new string[1] { path };
            return _httpRestClient.BeginDeleteFolders(callback, callbackUserState, paths);
        }

        /// <summary>
        /// Finishes deleting a folder in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) The result from the metadata query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndDeleteFolder(IAsyncResult aResult, out SyncboxDeleteFolderResult result)
        {
            CheckDisposed();

            // Complete the async operation.
            SyncboxDeleteFoldersResult results;
            CLError error = _httpRestClient.EndDeleteFolders(aResult, out results);

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
                return new CLError(new CLException(CLExceptionCode.Rest_Syncbox_Folder_Delete_No_Server_Responses_Or_Errors, "No error or responses from server results null"));
            }
            // error == null && results != null  (no overall error, and we got a results object)
            else if (results.Errors != null && results.Errors.Length >= 1)
            {
                // No overall error, got a results object, and it has an error.  Return that error.
                result = null;
                return results.Errors[0];
            }
            // (error == null && results != null) && (results.Errors == null || results.Errors.Length == 0)  (no overall error, we got a results object, and there are no errors in results)
            else if (results.FolderItems != null && results.FolderItems.Length >= 1)
            {
                // No overall error, got a results object, is has no errors, and it has a delete response.  This is the normal case.  Return that delete response as the result.
                result = new SyncboxDeleteFolderResult(error: null, folderItem: results.FolderItems[0]);
                return null;        // normal condition
            }
            // ((error == null && results != null) && (results.Errors == null || results.Errors.Length == 0)) && (results.Responses == null || results.Responses.Length == 0)
            else
            {
                // No error, got a results object, but there were no errors and no delete responses inside.  Return an error.
                result = null;
                return new CLError(new CLException(CLExceptionCode.Rest_Syncbox_Folder_Delete_No_Server_Responses_Or_Errors, "No error or responses from server"));
            }
        }

        /// <summary>
        /// Deletes a folder in the cloud.
        /// </summary>
        /// <param name="path">Full path to where the folder would exist locally on disk</param>
        /// <param name="folderItem">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError DeleteFolder(string path, out CLFileItem folderItem)
        {
            CheckDisposed();
            string[] paths = new string[1] { path };

            // Communicate and get the results.
            CLError[] outErrors;
            CLFileItem[] outItems;
            CLError error = _httpRestClient.DeleteFolders(paths, out outItems, out outErrors);

            // Return resulting error or item
            if (error != null)
            {
                // There was an overall error.  Return it
                folderItem = null;
                return error;
            }
            // error == null
            else if (outErrors != null && outErrors.Length >= 1)
            {
                // No overall error, but there was an item error.  Return it.
                folderItem = null;
                return outErrors[0];
            }
            // error == null && (outErrors == null || outErrors.Length == 0)
            else if (outItems != null && outItems.Length >= 1)
            {
                // No overall error, no item errors, and we have an item.  Return it.  This is the normal condition
                folderItem = outItems[0];
                return null;
            }
            // (error == null && (outErrors == null || outErrors.Length == 0)) && (outItems == null || outItems.Length == 0)
            else
            {
                // No overall error, no item errors, and no items.  No responses from server.  Return error.
                folderItem = null;
                return new CLError(new CLException(CLExceptionCode.Rest_Syncbox_Folder_Delete_No_Server_Responses_Or_Errors, "No responses or status from serer"));
            }
        }

        #endregion  // end DeleteFolder (Deletes a folder in the cloud)

        #region DeleteFolders (Delete folders in the cloud)
        /// <summary>
        /// Asynchronously starts deleting folders in the cloud; outputs an array of  CLFileItem objects, and possibly an array of CLError objects.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="paths">An array of full paths to where the folders would exist locally on disk.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginDeleteFolders(AsyncCallback callback, object callbackUserState, string[] paths)
        {
            CheckDisposed();
            return _httpRestClient.BeginDeleteFolders(callback, callbackUserState, paths);
        }

        /// <summary>
        /// Finishes deleting folders in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) The result from the metadata query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndDeleteFolders(IAsyncResult aResult, out SyncboxDeleteFoldersResult result)
        {
            CheckDisposed();
            SyncboxDeleteFoldersResult deleteResult;
            CLError error = _httpRestClient.EndDeleteFolders(aResult, out deleteResult);

            if (error != null)
            {
                result = null;
                return error;
            }

            result = new SyncboxDeleteFoldersResult(deleteResult.OverallError, deleteResult.Errors, deleteResult.FolderItems);
            return error;
        }

        /// <summary>
        /// Deletes folders in the cloud.
        /// </summary>
        /// <param name="paths">An array of full paths to where the folders would exist locally on disk.</param>
        /// <param name="folderItems">(output) response object from communication</param>
        /// <param name="errors">(output) Any returned errors, or null.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError DeleteFolders(string[] paths, out CLFileItem[] folderItems, out CLError[] errors)
        {
            CheckDisposed();
            return _httpRestClient.DeleteFolders(paths, out folderItems, out errors);
        }

        #endregion  // end DeleteFolders (Delete folders in the cloud)

        #region AddFolder (Adds a folder in the cloud)
        /// <summary>
        /// Asynchronously starts adding a folder in the cloud; outputs a CLFileItem object.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="path">Full path to where the folder would exist locally on disk.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginAddFolder(AsyncCallback callback, object callbackUserState, string path)
        {
            CheckDisposed();
            string[] paths = new string[1] { path };
            return _httpRestClient.BeginAddFolders(callback, callbackUserState, paths);
        }

        /// <summary>
        /// Finishes adding a folder in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) The result from the metadata query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndAddFolder(IAsyncResult aResult, out SyncboxAddFolderResult result)
        {
            CheckDisposed();

            // Complete the async operation.
            SyncboxAddFoldersResult results;
            CLError error = _httpRestClient.EndAddFolders(aResult, out results);

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
                return new CLError(new CLException(CLExceptionCode.Rest_Syncbox_Folder_Add_No_Server_Responses_Or_Errors, "No error or responses from server results null"));
            }
            // error == null && results != null  (no overall error, and we got a results object)
            else if (results.Errors != null && results.Errors.Length >= 1)
            {
                // No overall error, got a results object, and it has an error.  Return that error.
                result = null;
                return results.Errors[0];
            }
            // (error == null && results != null) && (results.Errors == null || results.Errors.Length == 0)  (no overall error, we got a results object, and there are no errors in results)
            else if (results.FolderItems != null && results.FolderItems.Length >= 1)
            {
                // No overall error, got a results object, is has no errors, and it has a delete response.  This is the normal case.  Return that delete response as the result.
                result = new SyncboxAddFolderResult(error: null, folderItem: results.FolderItems[0]);
                return null;        // normal condition
            }
            // ((error == null && results != null) && (results.Errors == null || results.Errors.Length == 0)) && (results.Responses == null || results.Responses.Length == 0)
            else
            {
                // No error, got a results object, but there were no errors and no delete responses inside.  Return an error.
                result = null;
                return new CLError(new CLException(CLExceptionCode.Rest_Syncbox_Folder_Add_No_Server_Responses_Or_Errors, "No error or responses from server"));
            }
        }

        /// <summary>
        /// Adds a folder in the cloud.
        /// </summary>
        /// <param name="path">Full path to where the folder would exist locally on disk</param>
        /// <param name="folderItem">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError AddFolder(string path, out CLFileItem folderItem)
        {
            CheckDisposed();
            string[] paths = new string[1] { path };

            // Communicate and get the results.
            CLError[] outErrors;
            CLFileItem[] outItems;
            CLError error = _httpRestClient.AddFolders(paths, out outItems, out outErrors);

            // Return resulting error or item
            if (error != null)
            {
                // There was an overall error.  Return it
                folderItem = null;
                return error;
            }
            // error == null
            else if (outErrors != null && outErrors.Length >= 1)
            {
                // No overall error, but there was an item error.  Return it.
                folderItem = null;
                return outErrors[0];
            }
            // error == null && (outErrors == null || outErrors.Length == 0)
            else if (outItems != null && outItems.Length >= 1)
            {
                // No overall error, no item errors, and we have an item.  Return it.  This is the normal condition
                folderItem = outItems[0];
                return null;
            }
            // (error == null && (outErrors == null || outErrors.Length == 0)) && (outItems == null || outItems.Length == 0)
            else
            {
                // No overall error, no item errors, and no items.  No responses from server.  Return error.
                folderItem = null;
                return new CLError(new CLException(CLExceptionCode.Rest_Syncbox_Folder_Add_No_Server_Responses_Or_Errors, "No responses or status from serer"));
            }
        }

        #endregion  // end AddFolder (Adds a folder in the cloud)

        #region AddFolders (Add folders in the cloud)
        /// <summary>
        /// Asynchronously starts adding folders in the cloud; outputs an array of  CLFileItem objects, and possibly an array of CLError objects.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="paths">An array of full paths to where the folders would exist locally on disk.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginAddFolders(AsyncCallback callback, object callbackUserState, string[] paths)
        {
            CheckDisposed();
            return _httpRestClient.BeginAddFolders(callback, callbackUserState, paths);
        }

        /// <summary>
        /// Finishes adding folders in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) The result from the metadata query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndAddFolders(IAsyncResult aResult, out SyncboxAddFoldersResult result)
        {
            CheckDisposed();
            SyncboxAddFoldersResult deleteResult;
            CLError error = _httpRestClient.EndAddFolders(aResult, out deleteResult);

            if (error != null)
            {
                result = null;
                return error;
            }

            result = new SyncboxAddFoldersResult(deleteResult.OverallError, deleteResult.Errors, deleteResult.FolderItems);
            return error;
        }

        /// <summary>
        /// Adds folders in the cloud.
        /// </summary>
        /// <param name="paths">An array of full paths to where the folders would exist locally on disk.</param>
        /// <param name="folderItems">(output) response object from communication</param>
        /// <param name="errors">(output) Any returned errors, or null.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError AddFolders(string[] paths, out CLFileItem[] folderItems, out CLError[] errors)
        {
            CheckDisposed();
            return _httpRestClient.AddFolders(paths, out folderItems, out errors);
        }

        #endregion  // end AddFolders (Add folders in the cloud)

        #region AddFile (Adds a file in the cloud)
        /// <summary>
        /// Asynchronously starts adding a file in the cloud; outputs a CLFileItem object.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="path">Full path to where the file would exist locally on disk.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginAddFile(AsyncCallback callback, object callbackUserState, string path)
        {
            CheckDisposed();
            string[] paths = new string[1] { path };
            return _httpRestClient.BeginAddFiles(callback, callbackUserState, paths);
        }

        /// <summary>
        /// Finishes adding a file in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) The result from the metadata query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndAddFile(IAsyncResult aResult, out SyncboxAddFileResult result)
        {
            CheckDisposed();

            // Complete the async operation.
            SyncboxAddFilesResult results;
            CLError error = _httpRestClient.EndAddFiles(aResult, out results);

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
                return new CLError(new CLException(CLExceptionCode.Rest_Syncbox_File_Add_No_Server_Responses_Or_Errors, "No error or responses from server results null"));
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
                return new CLError(new CLException(CLExceptionCode.Rest_Syncbox_File_Add_No_Server_Responses_Or_Errors, "No error or responses from server"));
            }
        }

        /// <summary>
        /// Adds a file in the cloud.
        /// </summary>
        /// <param name="path">Full path to where the file would exist locally on disk</param>
        /// <param name="fileItem">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError AddFile(string path, out CLFileItem fileItem)
        {
            CheckDisposed();
            string[] paths = new string[1] { path };

            // Communicate and get the results.
            CLError[] outErrors;
            CLFileItem[] outItems;
            CLError error = _httpRestClient.AddFiles(paths, out outItems, out outErrors);

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
                return new CLError(new CLException(CLExceptionCode.Rest_Syncbox_File_Add_No_Server_Responses_Or_Errors, "No responses or status from serer"));
            }
        }

        #endregion  // end AddFile (Adds a file in the cloud)

        #region AddFiles (Add files in the cloud)
        /// <summary>
        /// Asynchronously starts adding files in the cloud; outputs an array of  CLFileItem objects, and possibly an array of CLError objects.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="paths">An array of full paths to where the files would exist locally on disk.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginAddFiles(AsyncCallback callback, object callbackUserState, string[] paths)
        {
            CheckDisposed();
            return _httpRestClient.BeginAddFiles(callback, callbackUserState, paths);
        }

        /// <summary>
        /// Finishes adding files in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) The result from the metadata query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndAddFiles(IAsyncResult aResult, out SyncboxAddFilesResult result)
        {
            CheckDisposed();
            SyncboxAddFilesResult deleteResult;
            CLError error = _httpRestClient.EndAddFiles(aResult, out deleteResult);

            if (error != null)
            {
                result = null;
                return error;
            }

            result = new SyncboxAddFilesResult(deleteResult.OverallError, deleteResult.Errors, deleteResult.FileItems);
            return error;
        }

        /// <summary>
        /// Adds files in the cloud.
        /// </summary>
        /// <param name="paths">An array of full paths to where the files would exist locally on disk.</param>
        /// <param name="fileItems">(output) response object from communication</param>
        /// <param name="errors">(output) Any returned errors, or null.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError AddFiles(string[] paths, out CLFileItem[] fileItems, out CLError[] errors)
        {
            CheckDisposed();
            return _httpRestClient.AddFiles(paths, out fileItems, out errors);
        }

        #endregion  // end AddFiles (Add files in the cloud)

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
            CheckDisposed();
            return _httpRestClient.BeginGetAllPending(aCallback, aState, timeoutMilliseconds);
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
            CheckDisposed();
            return _httpRestClient.EndGetAllPending(aResult, out result);
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
            CheckDisposed();
            return _httpRestClient.GetAllPending(timeoutMilliseconds, out response);
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
            return _httpRestClient.BeginGetFileVersions(aCallback,
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
            return _httpRestClient.BeginGetFileVersions(aCallback,
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
            return _httpRestClient.BeginGetFileVersions(aCallback, aState, timeoutMilliseconds, pathToFile);
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
            return _httpRestClient.BeginGetFileVersions(aCallback, aState, timeoutMilliseconds, pathToFile, includeDeletedVersions);
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
            return _httpRestClient.BeginGetFileVersions(aCallback, aState, fileServerId, timeoutMilliseconds, pathToFile);
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
            return _httpRestClient.BeginGetFileVersions(aCallback, aState, fileServerId, timeoutMilliseconds, pathToFile, includeDeletedVersions);
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
            return _httpRestClient.EndGetFileVersions(aResult, out result);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        /// <summary>
        /// Queries the server for all versions of a given file
        /// </summary>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFileVersions(string fileServerId, int timeoutMilliseconds, out JsonContracts.FileVersion[] response)
        {
            CheckDisposed();
            return _httpRestClient.GetFileVersions(fileServerId, timeoutMilliseconds, out response);
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
        public CLError GetFileVersions(string fileServerId, int timeoutMilliseconds, out JsonContracts.FileVersion[] response, bool includeDeletedVersions)
        {
            CheckDisposed();
            return _httpRestClient.GetFileVersions(fileServerId, timeoutMilliseconds, out response, includeDeletedVersions);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        /// <summary>
        /// Queries the server for all versions of a given file
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Full path to the file where it would be placed locally within the sync root</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFileVersions(int timeoutMilliseconds, FilePath pathToFile, out JsonContracts.FileVersion[] response)
        {
            CheckDisposed();
            return _httpRestClient.GetFileVersions(timeoutMilliseconds, pathToFile, out response);
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
        public CLError GetFileVersions(int timeoutMilliseconds, FilePath pathToFile, out JsonContracts.FileVersion[] response, bool includeDeletedVersions)
        {
            CheckDisposed();
            return _httpRestClient.GetFileVersions(timeoutMilliseconds, pathToFile, out response, includeDeletedVersions);
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
        public CLError GetFileVersions(string fileServerId, int timeoutMilliseconds, FilePath pathToFile, out JsonContracts.FileVersion[] response)
        {
            CheckDisposed();
            return _httpRestClient.GetFileVersions(fileServerId, timeoutMilliseconds, pathToFile, out response);
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
        public CLError GetFileVersions(string fileServerId, int timeoutMilliseconds, FilePath pathToFile, out JsonContracts.FileVersion[] response, bool includeDeletedVersions)
        {
            CheckDisposed();
            return _httpRestClient.GetFileVersions(fileServerId, timeoutMilliseconds, pathToFile, out response, includeDeletedVersions);
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
            CheckDisposed();
            return _httpRestClient.BeginGetAllImageItems(callback, callbackUserState);
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
            CheckDisposed();
            return _httpRestClient.EndGetAllImageItems(aResult, out result);
        }

        /// <summary>
        /// Queries the server for pictures
        /// </summary>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetAllImageItems(out CLFileItem[] response)
        {
            CheckDisposed();
            return _httpRestClient.GetAllImageItems(out response);
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
            CheckDisposed();
            return _httpRestClient.BeginGetAllVideoItems(callback, callbackUserState);
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
            CheckDisposed();
            return _httpRestClient.EndGetAllVideoItems(aResult, out result);
        }

        /// <summary>
        /// Queries the server for videos
        /// </summary>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetGetAllVideoItems(CLFileItem[] response)
        {
            CheckDisposed();
            return _httpRestClient.GetAllVideoItems(out response);
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
            CheckDisposed();
            return _httpRestClient.BeginGetAllAudioItems(callback, callbackUserState);
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
            CheckDisposed();
            return _httpRestClient.EndGetAllAudioItems(aResult, out result);
        }

        /// <summary>
        /// Queries the server for audio items.
        /// </summary>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetAllAudioItems(CLFileItem [] response)
        {
            CheckDisposed();
            return _httpRestClient.GetAllAudioItems(out response);
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
            CheckDisposed();
            return _httpRestClient.BeginGetAllDocumentItems(callback, callbackUserState);
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
            CheckDisposed();
            return _httpRestClient.EndGetAllDocumentItems(aResult, out result);
        }

        /// <summary>
        /// Queries the server for document items.
        /// </summary>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetAllDocumentItems(CLFileItem[] response)
        {
            CheckDisposed();
            return _httpRestClient.GetAllDocumentItems(out response);
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
            CheckDisposed();
            return _httpRestClient.BeginGetAllPresentationItems(callback, callbackUserState);
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
            CheckDisposed();
            return _httpRestClient.EndGetAllPresentationItems(aResult, out result);
        }

        /// <summary>
        /// Queries the server for presentation items.
        /// </summary>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetAllPresentationItems(CLFileItem[] response)
        {
            CheckDisposed();
            return _httpRestClient.GetAllPresentationItems(out response);
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
            CheckDisposed();
            return _httpRestClient.BeginGetAllTextItems(callback, callbackUserState);
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
            CheckDisposed();
            return _httpRestClient.EndGetAllTextItems(aResult, out result);
        }

        /// <summary>
        /// Queries the server for text items.
        /// </summary>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetAllTextItems(CLFileItem[] response)
        {
            CheckDisposed();
            return _httpRestClient.GetAllTextItems(out response);
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
            CheckDisposed();
            return _httpRestClient.BeginGetAllArchiveItems(callback, callbackUserState);
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
            CheckDisposed();
            return _httpRestClient.EndGetAllArchiveItems(aResult, out result);
        }

        /// <summary>
        /// Queries the server for archive items.
        /// </summary>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetAllArchiveItems(CLFileItem[] response)
        {
            CheckDisposed();
            return _httpRestClient.GetAllArchiveItems(out response);
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
            CheckDisposed();
            return _httpRestClient.BeginGetRecents(callback, callbackUserState, sinceDate, returnLimit);
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
            CheckDisposed();
            return _httpRestClient.EndGetRecents(aResult, out result);
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
            out CLFileItem [] response)
        {
            CheckDisposed();
            return _httpRestClient.GetRecents(sinceDate, returnLimit, out response);
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
            CheckDisposed();
            return _httpRestClient.BeginGetFolderContentsAtPath(callback, callbackUserState, path);
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
            CheckDisposed();
            return _httpRestClient.EndGetFolderContents(aResult, out result);
        }

        /// <summary>
        /// Queries server for folder contents with an optional path and an optional depth limit
        /// </summary>
        /// <param name="path">The full path of the folder that would be on disk in the local syncbox folder.</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderContents(
            string path,
            out CLFileItem [] response)
        {
            CheckDisposed();
            return _httpRestClient.GetFolderContentsAtPath(path, out response);
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
            CheckDisposed();
            return _httpRestClient.BeginGetFolderHierarchy(aCallback, aState, timeoutMilliseconds);
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
            CheckDisposed();
            return _httpRestClient.BeginGetFolderHierarchy(aCallback, aState, timeoutMilliseconds, hierarchyRoot);
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
            CheckDisposed();
            return _httpRestClient.EndGetFolderHierarchy(aResult, out result);
        }

        /// <summary>
        /// Queries server for folder hierarchy with an optional path
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderHierarchy(int timeoutMilliseconds,out JsonContracts.Folders response)
        {
            CheckDisposed();
            return _httpRestClient.GetFolderHierarchy(timeoutMilliseconds, out response);
        }

        /// <summary>
        /// Queries server for folder hierarchy with an optional path
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="hierarchyRoot">(optional) root path of hierarchy query</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderHierarchy(int timeoutMilliseconds, out JsonContracts.Folders response, FilePath hierarchyRoot)
        {
            CheckDisposed();
            return _httpRestClient.GetFolderHierarchy(timeoutMilliseconds, out response, hierarchyRoot);
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
            CheckDisposed();
            return _httpRestClient.BeginGetSyncboxUsage(callback, callbackUserState, _copiedSettings.HttpTimeoutMilliseconds);
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
            CheckDisposed();
            return _httpRestClient.EndGetSyncboxUsage(aResult, out result);
        }

        /// <summary>
        /// Queries the cloud for syncbox usage information.  This method is synchronous.
        /// </summary>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetDataUsage(out JsonContracts.SyncboxUsageResponse response)
        {
            CheckDisposed();
            return _httpRestClient.GetSyncboxUsage(_copiedSettings.HttpTimeoutMilliseconds, out response);
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
            CheckDisposed();
            return _httpRestClient.BeginUpdateSyncboxExtendedMetadata(aCallback, aState, metadata, timeoutMilliseconds);
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
            CheckDisposed();
            return _httpRestClient.BeginUpdateSyncboxExtendedMetadata(aCallback, aState, metadata, timeoutMilliseconds);
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
            CheckDisposed();
            return _httpRestClient.EndUpdateSyncboxExtendedMetadata(aResult, out result);
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
            CheckDisposed();
            return _httpRestClient.UpdateSyncboxExtendedMetadata(metadata, timeoutMilliseconds, out response);
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
            CheckDisposed();
            return _httpRestClient.UpdateSyncboxExtendedMetadata(metadata, timeoutMilliseconds, out response);
        }
        #endregion

        #region UpdateStoragePlan (changes the storage plan associated with this syncbox in the cloud)
        /// <summary>
        /// Asynchronously updates the storage plan for a syncbox in the cloud.
        /// Updates this object's StoragePlanId property.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass as a parameter when firing async callback</param>
        /// <param name="storagePlan">The new storage plan to use for this syncbox)</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginUpdateStoragePlan(AsyncCallback callback, object callbackUserState, CLStoragePlan storagePlan)
        {
            CheckDisposed();
            return _httpRestClient.BeginSyncboxUpdateStoragePlan(
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
            CheckDisposed();
            return _httpRestClient.EndSyncboxUpdateStoragePlan(aResult, out result);
        }

        /// <summary>
        /// Updates the storage plan for a syncbox in the cloud.  This is a synchronous method.
        /// Updates this object's StoragePlanId property.
        /// </summary>
        /// <param name="storagePlan">The storage plan to set (new storage plan to use for this syncbox)</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError UpdateStoragePlan(CLStoragePlan storagePlan, out JsonContracts.SyncboxUpdateStoragePlanResponse response)
        {
            CheckDisposed();
            return _httpRestClient.UpdateSyncboxStoragePlan(
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
                throw new NullReferenceException("response cannot be null");
            }
            if (response.Syncbox == null)
            {
                throw new NullReferenceException("response Syncbox cannot be null");
            }
            if (response.Syncbox.PlanId == null)
            {
                throw new NullReferenceException("response Syncbox PlanId cannot be null");
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
        #endregion  // end (changes the storage plan associated with this syncbox in the cloud)

        #region GetCurrentStatus (update the status of this syncbox from the cloud)
        /// <summary>
        /// Asynchronously gets the status of this Syncbox.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetCurrentSyncboxStatus(AsyncCallback callback, object callbackUserState)
        {
            CheckDisposed();
            return _httpRestClient.BeginGetSyncboxStatus(callback, callbackUserState, _copiedSettings.HttpTimeoutMilliseconds, new Action<JsonContracts.SyncboxStatusResponse, object>(OnStatusCompletion), null);
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
            CheckDisposed();
            return _httpRestClient.EndGetSyncboxStatus(aResult, out result);
        }

        /// <summary>
        /// Gets the status of this Syncbox.  This is a synchronous method.
        /// The local object status is updated with the server results.
        /// </summary>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetCurrentSyncboxStatus()
        {
            CheckDisposed();
            JsonContracts.SyncboxStatusResponse response;
            return _httpRestClient.GetSyncboxStatus(_copiedSettings.HttpTimeoutMilliseconds, out response, new Action<JsonContracts.SyncboxStatusResponse, object>(OnStatusCompletion), null);
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
                throw new NullReferenceException("response cannot be null");
            }
            if (response.Syncbox == null)
            {
                throw new NullReferenceException("response Syncbox cannot be null");
            }
            if (response.Syncbox.PlanId == null)
            {
                throw new NullReferenceException("response Syncbox PlanId cannot be null");
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

        #endregion  // end Public Instance HTTP REST Methods

        #region Private Instance Support Functions

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
            if (!_httpRestClient.IsModifyingSyncboxViaPublicAPICalls)
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
                throw new Exception("Object disposed");
            }

            Helpers.CheckHalted();
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