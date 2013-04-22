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
        
        private static CLTrace _trace = CLTrace.Instance;
        private static readonly List<CLSyncEngine> _startedSyncEngines = new List<CLSyncEngine>();
        private static readonly object _startLocker = new object();

        private CLSyncEngine _syncEngine = null;
        private bool _isStarted = false;
        private bool Disposed = false;   // This stores if this current instance has been disposed (defaults to not disposed)
        private ReaderWriterLockSlim _propertyChangeLocker = new ReaderWriterLockSlim();  // for locking any reads and writes to the changeable properties.


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
                string toReturn = _friendlyNameHolder.Value;
                _propertyChangeLocker.ExitReadLock();

                return toReturn;
            }
        }
        private readonly GenericHolder<string> _friendlyNameHolder;

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
        private readonly string _path;

        /// <summary>
        /// The ID of the storage plan to use for this syncbox.
        /// </summary>
        public long StoragePlanId
        {
            get
            {
                return _storagePlanIdHolder.Value;
            }
        }
        private readonly GenericHolder<long> _storagePlanIdHolder;

        /// <summary>
        /// The sync mode used with this syncbox.
        /// </summary>
        public CLSyncMode SyncMode
        {
            get
            {
                return _syncModeHolder.Value;
            }
        }
        private readonly GenericHolder<CLSyncMode> _syncModeHolder;

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
            string path,
            ref CLHttpRestStatus status,
            ICLSyncSettings Settings,
            Helpers.ReplaceExpiredCredentials getNewCredentialsCallback = null,
            object getNewCredentialsCallbackUserState = null
            )
        {
            // check input parameters

            if (syncboxId == 0)
            {
                status = CLHttpRestStatus.BadRequest;  ///&&&&&&& fix this
                throw new ArgumentException("syncboxId must not be null.");
            }

            if (credentials == null)
            {
                status = CLHttpRestStatus.BadRequest;  ///&&&&&&& fix this
                throw new NullReferenceException("Credentials cannot be null");
            }

            // Copy the settings so the user can't change them.
            if (Settings == null)
            {
                this._copiedSettings = AdvancedSyncSettings.CreateDefaultSettings();
            }
            else
            {
                this._copiedSettings = Settings.CopySettings();
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
                    _trace.writeToLog(1, "CLSyncbox: Construction: ERROR: Msg: {0}. Code: {1}.", createRestClientError.errorDescription, ((int)createRestClientError.code).ToString());
                    status = CLHttpRestStatus.BadRequest;  ///&&&&&&& fix this
                    throw new AggregateException("Error creating REST HTTP client", createRestClientError.GrabExceptions());
                }
                if (_httpRestClient == null)
                {
                    const string nullRestClient = "Unknown error creating HTTP REST client";
                    _trace.writeToLog(1, "CLSyncbox: Construction: ERROR: Msg: {0}.", nullRestClient);
                    status = CLHttpRestStatus.BadRequest;  ///&&&&&&& fix this
                    throw new NullReferenceException(nullRestClient);
                }

                // We need to validate the syncbox ID with the server with these credentials.  We will also retrieve the other syncbox
                // properties from the server and set them into this local object's properties.
                CLHttpRestStatus statusFromStatus;
                JsonContracts.SyncboxResponse response;
                CLError errorFromStatus = GetStatus(out statusFromStatus, out response);
                if (errorFromStatus != null)
                {
                    throw new AggregateException("Error getting syncbox status from Cloud", errorFromStatus.GrabExceptions());
                }

                // Create the sync engine
                _syncEngine = new CLSyncEngine();
            }
        }

        /// <summary>
        /// Private constructor to create a functional CLSyncbox object from a server response.
        /// </summary>
        /// <param name="serverResponse">The server response to use.</param>
        /// <remarks>All parameters must be tested before calling this constructor.</remarks>
        private CLSyncbox(
                    JsonContracts.SyncboxResponse serverResponse,
                    CLCredentials credentials, 
                    ref CLHttpRestStatus status, 
                    ICLSyncSettings settings,
                    Helpers.ReplaceExpiredCredentials getNewCredentialsCallback,
                    object getNewCredentialsCallbackUserState
            )
            : this
            (
                syncboxId: (long)serverResponse.Syncbox.Id,
                credentials: credentials,
                path: null,
                status: ref status,
                Settings: settings,
                getNewCredentialsCallback: getNewCredentialsCallback,
                getNewCredentialsCallbackUserState: getNewCredentialsCallbackUserState
            )
        {
        }

        #endregion  // end Private Constructors

        #region Public Factory

        /// <summary>
        /// Asynchronously begins the factory process to construct an instance of CLSyncbox, initialize it and fill in its properties from the cloud, and associates the cloud syncbox with a folder on the local disk.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes.  Can be null.</param>
        /// <param name="callbackUserState">Userstate to pass to the callback when it is fired.  Can be null.</param>
        /// <param name="syncboxId">The cloud syncbox ID to use.</param>
        /// <param name="credentials">The credentials to use with this request.</param>
        /// <param name="path">The full path of the folder on disk to associate with this syncbox.</param>
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
            var asyncThread = DelegateAndDataHolder.Create(
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
                        // declare the output status for communication
                        CLSyncboxCreationStatus status;
                        // declare the specific type of result for this operation
                        CLSyncbox response;
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError processError = AllocAndInit(
                            Data.syncboxId,
                            Data.credentials,
                            Data.path,
                            out response,
                            out status,
                            Data.settings,
                            Data.getNewCredentialsCallback,
                            Data.getNewCredentialsCallbackUserState);

                        Data.toReturn.Complete(
                            new SyncboxAllocAndInitResult(
                                processError, // any error that may have occurred during processing
                                status, // the output status of communication
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
            // declare the specific type of asynchronous result
            GenericAsyncResult<SyncboxAllocAndInitResult> castAResult;

            // try/catch to try casting the asynchronous result as the specific result type and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the specific result type
                castAResult = aResult as GenericAsyncResult<SyncboxAllocAndInitResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException("aResult does not match expected internal type");
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<SyncboxAllocAndInitResult>();
                return ex;
            }

            // try/catch to finish the asynchronous operation if necessary, re-pull the result for output, and rethrow any exception which may have occurred; on catch, return the error
            try
            {
                // This method assumes that only 1 thread calls the End* method for this object
                if (!castAResult.IsCompleted)
                {
                    // If the operation isn't done, wait for it
                    castAResult.AsyncWaitHandle.WaitOne();
                    castAResult.AsyncWaitHandle.Close();
                }

                // re-pull the result for output in case it was not completed when it was pulled before
                result = castAResult.Result;

                // Operation is done: if an exception occurred, return it
                if (castAResult.Exception != null)
                {
                    return castAResult.Exception;
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Creates and initializes a CLSyncbox object which represents a Syncbox in Cloud, and associates the syncbox with a folder on the local disk.
        /// </summary>
        /// <param name="syncboxId">Unique ID of the syncbox generated by Cloud</param>
        /// <param name="credentials">Credentials to use with this request.</param>
        /// <param name="path">The full path of the folder on the local disk to associate with this syncbox.</param>
        /// <param name="syncbox">(output) Created local object representation of the Syncbox</param>
        /// <param name="status">(output) Status of creation, should be checked for success</param>
        /// <param name="settings">(optional) Settings to use with this request</param>
        /// <param name="getNewCredentialsCallback">(optional) A delegate that will be called to provide new credentials when the current credentials token expires.</param>
        /// <param name="getNewCredentialsCallbackUserState">(optional) The user state that will be passed back to the getNewCredentialsCallback delegate.</param>
        /// <returns>Returns any error which occurred during object allocation or initialization, if any, or null.</returns>
        public static CLError AllocAndInit(
            long syncboxId,
            CLCredentials credentials,
            string path,
            out CLSyncbox syncbox,
            out CLSyncboxCreationStatus status,
            ICLSyncSettings settings = null,
            Helpers.ReplaceExpiredCredentials getNewCredentialsCallback = null,
            object getNewCredentialsCallbackUserState = null)
        {
            status = CLSyncboxCreationStatus.ErrorUnknown;

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
                    status: ref status,
                    Settings: settings,
                    getNewCredentialsCallback: getNewCredentialsCallback,
                    getNewCredentialsCallbackUserState: getNewCredentialsCallbackUserState
                    );
            }
            catch (Exception ex)
            {
                syncbox = Helpers.DefaultForType<CLSyncbox>();
                return ex;
            }

            status = CLSyncboxCreationStatus.Success;
            return null;
        }

        #endregion  // end Public Factory

        #region Public Instance Life Cycle Methods
        
        /// <summary>
        /// Start syncing according to the requested sync mode.
        /// </summary>
        /// <remarks>Note that only SyncMode.CLSyncModeLive is currently supported.</remarks>
        /// <param name="mode">The sync mode to start.</param>
        /// <returns></returns>
        public CLError BeginSync(CLSyncMode mode,
                System.Threading.WaitCallback syncStatusChangedCallback = null,
                object syncStatusChangedCallbackUserState = null)
        {
            CLError toReturn = null;

            CheckDisposed();

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
                    if (String.IsNullOrWhiteSpace(_path))
                    {
                        throw new ArgumentException("path must be specified.");
                    }

                    int nOutTooLongChars;
                    CLError errorPathTooLong = Helpers.CheckSyncRootLength(_path, out nOutTooLongChars);
                    if (errorPathTooLong != null)
                    {
                        throw new AggregateException(String.Format("syncbox path is too long by {0} characters.", nOutTooLongChars), errorPathTooLong.GrabExceptions());
                    }

                    CLError errorBadPath = Helpers.CheckForBadPath(_path);
                    if (errorBadPath != null)
                    {
                        throw new AggregateException("syncbox path contains invalid characters.", errorBadPath.GrabExceptions());
                    }

                    _syncModeHolder.Value = mode;

                    // Start the sync engine
                    CLSyncStartStatus startStatus;
                    toReturn = _syncEngine.Start(
                        Syncbox: this, // syncbox to sync (contains required settings)
                        Status: out startStatus, // The completion status of the Start() function
                        StatusUpdated: syncStatusChangedCallback, // called when sync status is updated
                        StatusUpdatedUserState: syncStatusChangedCallbackUserState); // the user state passed to the callback above

                    if (toReturn != null)
                    {
                        _trace.writeToLog(1, "Error starting sync engine. Msg: {0}. Reason: {0}.", toReturn.errorDescription, startStatus.ToString());
                        toReturn.LogErrors(_copiedSettings.TraceLocation, _copiedSettings.LogErrors);
                    }
                    else
                    {
                        // The sync engines started with syncboxes must be tracked statically so we can stop them all when the application terminates (in the ShutDown) method.
                        _startedSyncEngines.Add(_syncEngine);
                        _isStarted = true;
                    }
                }
            }
            catch (Exception ex)
            {
                toReturn += ex;
                toReturn.LogErrors(_copiedSettings.TraceLocation, _copiedSettings.LogErrors);
                _trace.writeToLog(1, "CLSyncbox: StartSync: ERROR.  Exception.  Msg: {0}. Code: {1}.", toReturn.errorDescription, ((int)toReturn.code).ToString());
            }

            return toReturn;
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
                    _syncEngine = null;
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(_copiedSettings.TraceLocation, _copiedSettings.LogErrors);
                _trace.writeToLog(1, "CLSyncbox: StopSync: ERROR.  Exception.  Msg: <{0}>. Code: {1}.", error.errorDescription, ((int)error.code).ToString());
            }
        }

        /// <summary>
        /// Reset sync.  Sync must be stopped before calling this method.  Starting sync after resetting sync will merge the
        /// syncbox folder with the server syncbox contents.
        /// </summary>
        public CLError ResetLocalCache()
        {
            CLError toReturn = null;

            CheckDisposed();

            try
            {
                lock (_startLocker)
                {
                    if (_isStarted)
                    {
                        throw new InvalidOperationException("Stop the syncbox first.");
                    }

                    // Reset the sync engine
                    toReturn = _syncEngine.SyncReset(this);
                    if (toReturn != null)
                    {
                        _trace.writeToLog(1, "CLSyncbox: ResetLocalCache: ERROR: From syncEngine.SyncReset: Msg: {0}.", toReturn.errorDescription);
                        toReturn.LogErrors(_copiedSettings.TraceLocation, _copiedSettings.LogErrors);
                    }
                }
            }
            catch (Exception ex)
            {
                toReturn += ex;
                toReturn.LogErrors(_copiedSettings.TraceLocation, _copiedSettings.LogErrors);
                _trace.writeToLog(1, "CLSyncbox: ResetLocalCache: ERROR.  Exception.  Msg: {0}. Code: {1}.", toReturn.errorDescription, ((int)toReturn.code).ToString());
            }

            return toReturn;
        }

        /// <summary>
        /// Output the current status of syncing
        /// </summary>
        /// <param name="status">(output) Current status of syncing</param>
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

        #region Create (create a syncbox in the cloud)

        /// <summary>
        /// Asynchronously starts creating a new Syncbox in the cloud.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes.  Can be null.</param>
        /// <param name="callbackUserState">Userstate to pass to the callback when it is fired.  Can be null.</param>
        /// <param name="plan">The storage plan to use with this Syncbox.</param>
        /// <param name="friendlyName">The friendly name of the Syncbox.</param>
        /// <param name="credentials">The credentials to use with this request.</param>
        /// <param name="settings">(optional) settings to use with this method.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public static IAsyncResult BeginCreate(
                    AsyncCallback callback,
                    object callbackUserState,
                    CLStoragePlan plan,
                    string friendlyName,
                    CLCredentials credentials,
                    ICLSyncSettings settings = null,
                    Helpers.ReplaceExpiredCredentials getNewCredentialsCallback = null,
                    object getNewCredentialsCallbackUserState = null)
        {
            // Check the parameters
            if (plan == null)
            {
                throw new ArgumentNullException("plan must not be null");
            }
            if (String.IsNullOrWhiteSpace(friendlyName))
            {
                throw new ArgumentException("friendlyName must be specified");
            }
            if (credentials == null)
            {
                throw new ArgumentNullException("credentials must not be null");
            }

            var asyncThread = DelegateAndDataHolder.Create(
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
                        // declare the output status for communication
                        CLSyncboxCreationStatus status;
                        // declare the specific type of result for this operation
                        CLSyncbox response;
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError processError = Create(
                            Data.plan,
                            Data.friendlyName,
                            Data.credentials,
                            out status,
                            out response,
                            Data.settings,
                            Data.getNewCredentialsCallback,
                            Data.getNewCredentialsCallbackUserState);

                        Data.toReturn.Complete(
                            new SyncboxCreateResult(
                                processError, // any error that may have occurred during processing
                                status, // the output status of communication
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
            //&&&&&&&&&&&&&&&&&&&
            // create the asynchronous result to return
            GenericAsyncResult<CreateSyncboxResult> toReturn = new GenericAsyncResult<CreateSyncboxResult>(
                callback,
                callbackUserState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<CreateSyncboxResult>, CLStoragePlan, string, CLCredentials, ICLCredentialsSettings/*, JsonContracts.MetadataDictionary*/> asyncParams =
                new Tuple<GenericAsyncResult<CreateSyncboxResult>, CLStoragePlan, string, CLCredentials, ICLCredentialsSettings/*, JsonContracts.MetadataDictionary*/>(
                    toReturn,
                    plan,
                    friendlyName,
                    credentials,
                    settings/*,
                    metadata*/);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<CreateSyncboxResult>, CLStoragePlan, string, CLCredentials, ICLCredentialsSettings/*, JsonContracts.MetadataDictionary*/> castState =
                    state as Tuple<GenericAsyncResult<CreateSyncboxResult>, CLStoragePlan, string, CLCredentials, ICLCredentialsSettings/*, JsonContracts.MetadataDictionary*/>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        "Cannot cast state as " + Helpers.GetTypeNameEvenForNulls(castState),
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());
                }
                // else if the try cast did not fail, then start processing with the input parameters
                else
                {
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the output status for communication
                        CLHttpRestStatus status;
                        // declare the specific type of result for this operation
                        JsonContracts.SyncboxResponse response;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = Create(
                            castState.Item2,  // plan
                            castState.Item3,  // friendlyName
                            castState.Item4,  // credentials
                            out status,  // CLHttpRestStatus
                            out response,  // HTTP response
                            castState.Item5);  // settings

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new CreateSyncboxResult(
                                    processError, // any error that may have occurred during processing
                                    status, // the output status of communication
                                    response), // the specific type of result for this operation
                                    sCompleted: false); // processing did not complete synchronously
                        }
                    }
                    catch (Exception ex)
                    {
                        // if there was an asynchronous result in the parameters, then pass through the exception to it
                        if (castState.Item1 != null)
                        {
                            castState.Item1.HandleException(
                                ex, // the exception which was not handled correctly by the CLError wrapping
                                sCompleted: false); // processing did not complete synchronously
                        }
                    }
                }
            }))).Start(asyncParams); // start the asynchronous processing thread with the input parameters object

            // return the asynchronous result
            return toReturn;
        }

        /// <summary>
        /// Finishes creating a Syncbox in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting creating the syncbox</param>
        /// <param name="result">(output) The result from creating the syncbox</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public static CLError EndCreate(IAsyncResult aResult, out CreateSyncboxResult result)
        {
            // declare the specific type of asynchronous result
            GenericAsyncResult<CreateSyncboxResult> castAResult;

            // try/catch to try casting the asynchronous result as the specific result type and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the specific result type
                castAResult = aResult as GenericAsyncResult<CreateSyncboxResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException("aResult does not match expected internal type");
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<CreateSyncboxResult>();
                return ex;
            }

            // try/catch to finish the asynchronous operation if necessary, re-pull the result for output, and rethrow any exception which may have occurred; on catch, return the error
            try
            {
                // This method assumes that only 1 thread calls EndInvoke 
                // for this object
                if (!castAResult.IsCompleted)
                {
                    // If the operation isn't done, wait for it
                    castAResult.AsyncWaitHandle.WaitOne();
                    castAResult.AsyncWaitHandle.Close();
                }

                // re-pull the result for output in case it was not completed when it was pulled before
                result = castAResult.Result;

                // Operation is done: if an exception occurred, return it
                if (castAResult.Exception != null)
                {
                    return castAResult.Exception;
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Create a Syncbox in the cloud for the current application.  This is a synchronous method.
        /// </summary>
        /// <param name="plan">The storage plan to use with this Syncbox.</param>
        /// <param name="name"The friendly name of the Syncbox.</param>
        /// <param name="credentials">The credentials to use for this request.</param>
        /// <param name="status">(output) Success/failure status of communication</param>
        /// <param name="response">(output) Response object from communication</param>
        /// <param name="settings">(optional) The settings to use with this method</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public static CLError Create(
                    CLStoragePlan plan,
                    string friendlyName,
                    CLCredentials credentials,
                    out CLHttpRestStatus status,
                    out CLSyncbox response,
                    ICLSyncSettings settings = null,
                    Helpers.ReplaceExpiredCredentials getNewCredentialsCallback = null,
                    object getNewCredentialsCallbackUserState = null)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
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
                    status: ref status,
                    CopiedSettings: copiedSettings,
                    Credentials: credentials,
                    SyncboxId: null);

                // Check the server response.
                if (responseFromServer == null)
                {
                    throw new NullReferenceException("response from server must not be null");
                }
                if (responseFromServer.Syncbox == null)
                {
                    throw new NullReferenceException("server response.Syncbox must not be null");
                }


                // Convert the response object to a CLSyncbox and return that.
                response = new CLSyncbox(
                    responseFromServer,
                    credentials,
                    ref status,
                    settings,
                    getNewCredentialsCallback,
                    getNewCredentialsCallbackUserState);

            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.SyncboxResponse>();
                return ex;
            }
            return null;
        }
        #endregion

        #region Delete (delete a syncbox in the cloud)

        /// <summary>
        /// Asynchronously starts deleting a new Syncbox in the cloud.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes.  Can be null.</param>
        /// <param name="callbackUserState">Userstate to pass as a parameter to the callback when it is fired.  Can be null.</param>
        /// <param name="syncboxId">The ID of syncbox to delete.</param>
        /// <param name="credentials">The credentials to use with this request.</param>
        /// <param name="settings">(optional) settings to use with this method.</param>
        ////
        //// The following metadata parameter was temporarily removed until the server checks for it for this call
        ////
        ///// <param name="metadata">(optional) string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public static IAsyncResult BeginDelete(
            AsyncCallback callback,
            object callbackUserState,
            long syncboxId,
            CLCredentials credentials,
            ICLCredentialsSettings settings = null
            /*,  \/ last parameter temporarily removed since the server is not checking for it for this call; add back wherever commented out within this method when it works
            JsonContracts.MetadataDictionary metadata = null*/)
        {
            // create the asynchronous result to return
            GenericAsyncResult<DeleteSyncboxResult> toReturn = new GenericAsyncResult<DeleteSyncboxResult>(
                callback,
                callbackUserState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<DeleteSyncboxResult>, long, CLCredentials, ICLCredentialsSettings/*, JsonContracts.MetadataDictionary*/> asyncParams =
                new Tuple<GenericAsyncResult<DeleteSyncboxResult>, long, CLCredentials, ICLCredentialsSettings/*, JsonContracts.MetadataDictionary*/>(
                    toReturn,
                    syncboxId,
                    credentials,
                    settings/*,
                    metadata*/);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<DeleteSyncboxResult>, long, CLCredentials, ICLCredentialsSettings/*, JsonContracts.MetadataDictionary*/> castState =
                    state as Tuple<GenericAsyncResult<DeleteSyncboxResult>, long, CLCredentials, ICLCredentialsSettings/*, JsonContracts.MetadataDictionary*/>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        "Cannot cast state as " + Helpers.GetTypeNameEvenForNulls(castState),
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());
                }
                // else if the try cast did not fail, then start processing with the input parameters
                else
                {
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the output status for communication
                        CLHttpRestStatus status;
                        // declare the specific type of result for this operation
                        JsonContracts.SyncboxResponse response;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = Delete(
                            castState.Item2,  // syncboxId
                            castState.Item3,  // credentials
                            out status,  // CLHttpRestStatus
                            out response,  // HTTP response
                            castState.Item4);  // settings

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new DeleteSyncboxResult(
                                    processError, // any error that may have occurred during processing
                                    status, // the output status of communication
                                    response), // the specific type of result for this operation
                                    sCompleted: false); // processing did not complete synchronously
                        }
                    }
                    catch (Exception ex)
                    {
                        // if there was an asynchronous result in the parameters, then pass through the exception to it
                        if (castState.Item1 != null)
                        {
                            castState.Item1.HandleException(
                                ex, // the exception which was not handled correctly by the CLError wrapping
                                sCompleted: false); // processing did not complete synchronously
                        }
                    }
                }
            }))).Start(asyncParams); // start the asynchronous processing thread with the input parameters object

            // return the asynchronous result
            return toReturn;
        }

        /// <summary>
        /// Finishes deleting a Syncbox in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the asynchronous operation</param>
        /// <param name="result">(output) The result from the asynchronous operation</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public static CLError EndDelete(IAsyncResult aResult, out DeleteSyncboxResult result)
        {
            // declare the specific type of asynchronous result
            GenericAsyncResult<DeleteSyncboxResult> castAResult;

            // try/catch to try casting the asynchronous result as the specific result type and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the specific result type
                castAResult = aResult as GenericAsyncResult<DeleteSyncboxResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException("aResult does not match expected internal type");
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<DeleteSyncboxResult>();
                return ex;
            }

            // try/catch to finish the asynchronous operation if necessary, re-pull the result for output, and rethrow any exception which may have occurred; on catch, return the error
            try
            {
                // This method assumes that only 1 thread calls EndInvoke 
                // for this object
                if (!castAResult.IsCompleted)
                {
                    // If the operation isn't done, wait for it
                    castAResult.AsyncWaitHandle.WaitOne();
                    castAResult.AsyncWaitHandle.Close();
                }

                // re-pull the result for output in case it was not completed when it was pulled before
                result = castAResult.Result;

                // Operation is done: if an exception occurred, return it
                if (castAResult.Exception != null)
                {
                    return castAResult.Exception;
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Delete a Syncbox in the cloud.  This is a synchronous method.
        /// </summary>
        /// <param name="syncboxId">the ID of the syncbox to delete.
        /// <param name="credentials">The credentials to use for this request.</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="settings">(optional) the settings to use with this method</param>
        //
        //// The following metadata parameter was temporarily removed until the server checks for it for this call
        //
        ///// <param name="metadata">(optional) string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public static CLError Delete(
                    long syncboxId,
                    CLCredentials credentials,
                    out CLHttpRestStatus status,
                    out JsonContracts.SyncboxResponse response,
                    ICLCredentialsSettings settings = null/*, JsonContracts.MetadataDictionary metadata = null*/)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the metadata query, on catch return the error
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

                response = Helpers.ProcessHttp<JsonContracts.SyncboxResponse>(
                    requestContent: inputBox,
                    serverUrl: CLDefinitions.CLPlatformAuthServerURL,
                    serverMethodPath: CLDefinitions.MethodPathAuthDeleteSyncbox,
                    method: Helpers.requestMethod.post,
                    timeoutMilliseconds: copiedSettings.HttpTimeoutMilliseconds,
                    uploadDownload: null, // not an upload nor download
                    validStatusCodes: Helpers.HttpStatusesOkAccepted,
                    status: ref status,
                    CopiedSettings: copiedSettings,
                    Credentials: credentials,
                    SyncboxId: null);

            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.SyncboxResponse>();
                return ex;
            }
            return null;
        }
        #endregion

        #region List (list syncboxes in the cloud)

        /// <summary>
        /// Asynchronously starts listing syncboxes in the cloud.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes.  Can be null.</param>
        /// <param name="callbackUserState">Userstate to pass as a parameter to the callback when it is fired.  Can be null.</param>
        /// <param name="credentials">The credentials to use with this request.</param>
        /// <param name="settings">(optional) settings to use with this method.</param>
        ////
        //// The following metadata parameter was temporarily removed until the server checks for it for this call
        ////
        ///// <param name="metadata">(optional) string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public static IAsyncResult BeginList(
            AsyncCallback callback,
            object callbackUserState,
            CLCredentials credentials,
            ICLCredentialsSettings settings = null)
        {
            // create the asynchronous result to return
            GenericAsyncResult<ListSyncboxesResult> toReturn = new GenericAsyncResult<ListSyncboxesResult>(
                callback,
                callbackUserState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<ListSyncboxesResult>, CLCredentials, ICLCredentialsSettings/*, JsonContracts.MetadataDictionary*/> asyncParams =
                new Tuple<GenericAsyncResult<ListSyncboxesResult>, CLCredentials, ICLCredentialsSettings/*, JsonContracts.MetadataDictionary*/>(
                    toReturn,
                    credentials,
                    settings/*,
                    metadata*/);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<ListSyncboxesResult>, CLCredentials, ICLCredentialsSettings> castState =
                    state as Tuple<GenericAsyncResult<ListSyncboxesResult>, CLCredentials, ICLCredentialsSettings>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        "Cannot cast state as " + Helpers.GetTypeNameEvenForNulls(castState),
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());
                }
                // else if the try cast did not fail, then start processing with the input parameters
                else
                {
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the output status for communication
                        CLHttpRestStatus status;
                        // declare the specific type of result for this operation
                        JsonContracts.ListSyncboxes response;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = List(
                            castState.Item2,  // credentials
                            out status,  // CLHttpRestStatus
                            out response,  // HTTP response
                            castState.Item3);  // settings

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new ListSyncboxesResult(
                                    processError, // any error that may have occurred during processing
                                    status, // the output status of communication
                                    response), // the specific type of result for this operation
                                    sCompleted: false); // processing did not complete synchronously
                        }
                    }
                    catch (Exception ex)
                    {
                        // if there was an asynchronous result in the parameters, then pass through the exception to it
                        if (castState.Item1 != null)
                        {
                            castState.Item1.HandleException(
                                ex, // the exception which was not handled correctly by the CLError wrapping
                                sCompleted: false); // processing did not complete synchronously
                        }
                    }
                }
            }))).Start(asyncParams); // start the asynchronous processing thread with the input parameters object

            // return the asynchronous result
            return toReturn;
        }

        /// <summary>
        /// Finishes listing syncboxes in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the asynchronous operation</param>
        /// <param name="result">(output) The result from the asynchronous operation</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public static CLError EndList(IAsyncResult aResult, out ListSyncboxesResult result)
        {
            // declare the specific type of asynchronous result
            GenericAsyncResult<ListSyncboxesResult> castAResult;

            // try/catch to try casting the asynchronous result as the specific result type and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the specific result type
                castAResult = aResult as GenericAsyncResult<ListSyncboxesResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException("aResult does not match expected internal type");
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<ListSyncboxesResult>();
                return ex;
            }

            // try/catch to finish the asynchronous operation if necessary, re-pull the result for output, and rethrow any exception which may have occurred; on catch, return the error
            try
            {
                // This method assumes that only 1 thread calls EndInvoke 
                // for this object
                if (!castAResult.IsCompleted)
                {
                    // If the operation isn't done, wait for it
                    castAResult.AsyncWaitHandle.WaitOne();
                    castAResult.AsyncWaitHandle.Close();
                }

                // re-pull the result for output in case it was not completed when it was pulled before
                result = castAResult.Result;

                // Operation is done: if an exception occurred, return it
                if (castAResult.Exception != null)
                {
                    return castAResult.Exception;
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// List syncboxes in the cloud for these credentials.  This is a synchronous method.
        /// </summary>
        /// <param name="credentials">The credentials to use for this request.</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="settings">(optional) the settings to use with this method</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public static CLError List(
                    CLCredentials credentials,
                    out CLHttpRestStatus status,
                    out JsonContracts.ListSyncboxes response,
                    ICLCredentialsSettings settings = null)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the metadata query, on catch return the error
            try
            {
                // copy settings so they don't change while processing; this also defaults some values
                ICLSyncSettingsAdvanced copiedSettings = (settings == null
                    ? AdvancedSyncSettings.CreateDefaultSettings()
                    : settings.CopySettings());

                response = Helpers.ProcessHttp<JsonContracts.ListSyncboxes>(
                    requestContent: null,
                    serverUrl: CLDefinitions.CLPlatformAuthServerURL,
                    serverMethodPath: CLDefinitions.MethodPathAuthListSyncboxes,
                    method: Helpers.requestMethod.post,
                    timeoutMilliseconds: copiedSettings.HttpTimeoutMilliseconds,
                    uploadDownload: null, // not an upload nor download
                    validStatusCodes: Helpers.HttpStatusesOkAccepted,
                    status: ref status,
                    CopiedSettings: copiedSettings,
                    Credentials: credentials,
                    SyncboxId: null);

            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.ListSyncboxes>();
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

        #region GetMetadata
        /// <summary>
        /// Asynchronously starts querying the server at a given file or folder path (must be specified) for existing metadata at that path; outputs CLHttpRestStatus.NoContent for status if not found on server;
        /// Check for Deleted flag being true in case the metadata represents a deleted item
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="fullPath">Full path to where file or folder would exist locally on disk</param>
        /// <param name="isFolder">Whether the query is for a folder (as opposed to a file/link)</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetMetadata(AsyncCallback aCallback,
            object aState,
            FilePath fullPath,
            bool isFolder,
            int timeoutMilliseconds)
        {
            return _httpRestClient.BeginGetMetadata(aCallback,
                aState,
                fullPath,
                isFolder,
                timeoutMilliseconds);
        }

        /// <summary>
        /// Asynchronously starts querying the server at a given file or folder server id (must be specified) for existing metadata at that id; outputs CLHttpRestStatus.NoContent for status if not found on server;
        /// Check for Deleted flag being true in case the metadata represents a deleted item
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="isFolder">Whether the query is for a folder (as opposed to a file/link)</param>
        /// <param name="serverId">Unique id of the item on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetMetadata(AsyncCallback aCallback,
            object aState,
            bool isFolder,
            string serverId,
            int timeoutMilliseconds)
        {
            return _httpRestClient.BeginGetMetadata(aCallback,
                aState,
                isFolder,
                serverId,
                timeoutMilliseconds);
        }
        
        /// <summary>
        /// Finishes a metadata query if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) The result from the metadata query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetMetadata(IAsyncResult aResult, out GetMetadataResult result)
        {
            return _httpRestClient.EndGetMetadata(aResult, out result);
        }
        
        /// <summary>
        /// Queries the server at a given file or folder path (must be specified) for existing metadata at that path; outputs CLHttpRestStatus.NoContent for status if not found on server;
        /// Check for Deleted flag being true in case the metadata represents a deleted item
        /// </summary>
        /// <param name="fullPath">Full path to where file or folder would exist locally on disk</param>
        /// <param name="isFolder">Whether the query is for a folder (as opposed to a file/link)</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetMetadata(FilePath fullPath, bool isFolder, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.Metadata response)
        {
            return _httpRestClient.GetMetadata(fullPath, isFolder, timeoutMilliseconds, out status, out response);
        }

        /// <summary>
        /// Queries the server at a given file or folder server id (must be specified) for existing metadata at that id; outputs CLHttpRestStatus.NoContent for status if not found on server;
        /// Check for Deleted flag being true in case the metadata represents a deleted item
        /// </summary>
        /// <param name="isFolder">Whether the query is for a folder (as opposed to a file/link)</param>
        /// <param name="serverId">Unique id of the item on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetMetadata(bool isFolder, string serverId, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.Metadata response)
        {
            return _httpRestClient.GetMetadata(isFolder, serverId, timeoutMilliseconds, out status, out response);
        }
        #endregion

        #region GetAllPending
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
            return _httpRestClient.BeginGetAllPending(aCallback, aState, timeoutMilliseconds);
        }

        /// <summary>
        /// Finishes a query for all pending files if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the pending query</param>
        /// <param name="result">(output) The result from the pending query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetAllPending(IAsyncResult aResult, out GetAllPendingResult result)
        {
            return _httpRestClient.EndGetAllPending(aResult, out result);
        }

        /// <summary>
        /// Queries the server for a given sync box and device to get all files which are still pending upload
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetAllPending(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.PendingResponse response)
        {
            return _httpRestClient.GetAllPending(timeoutMilliseconds, out status, out response);
        }
        #endregion

        #region GetFileVersions
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
            return _httpRestClient.BeginGetFileVersions(aCallback,
                aState,
                fileServerId,
                timeoutMilliseconds);
        }
        
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
            return _httpRestClient.BeginGetFileVersions(aCallback,
                aState,
                fileServerId,
                timeoutMilliseconds,
                includeDeletedVersions);
        }
        
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
            return _httpRestClient.BeginGetFileVersions(aCallback, aState, timeoutMilliseconds, pathToFile);
        }

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
            return _httpRestClient.BeginGetFileVersions(aCallback, aState, timeoutMilliseconds, pathToFile, includeDeletedVersions);
        }
        
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
            return _httpRestClient.BeginGetFileVersions(aCallback, aState, fileServerId, timeoutMilliseconds, pathToFile);
        }

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
            return _httpRestClient.BeginGetFileVersions(aCallback, aState, fileServerId, timeoutMilliseconds, pathToFile, includeDeletedVersions);
        }

        /// <summary>
        /// Finishes querying for all versions of a given file if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting undoing the deletion</param>
        /// <param name="result">(output) The result from undoing the deletion</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetFileVersions(IAsyncResult aResult, out GetFileVersionsResult result)
        {
            return _httpRestClient.EndGetFileVersions(aResult, out result);
        }
        
        /// <summary>
        /// Queries the server for all versions of a given file
        /// </summary>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFileVersions(string fileServerId, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.FileVersion[] response)
        {
            return _httpRestClient.GetFileVersions(fileServerId, timeoutMilliseconds, out status, out response);
        }

        /// <summary>
        /// Queries the server for all versions of a given file
        /// </summary>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="includeDeletedVersions">(optional) whether to include file versions which are deleted</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFileVersions(string fileServerId, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.FileVersion[] response, bool includeDeletedVersions)
        {
            return _httpRestClient.GetFileVersions(fileServerId, timeoutMilliseconds, out status, out response, includeDeletedVersions);
        }

        /// <summary>
        /// Queries the server for all versions of a given file
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Full path to the file where it would be placed locally within the sync root</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFileVersions(int timeoutMilliseconds, FilePath pathToFile, out CLHttpRestStatus status, out JsonContracts.FileVersion[] response)
        {
            return _httpRestClient.GetFileVersions(timeoutMilliseconds, pathToFile, out status, out response);
        }

        /// <summary>
        /// Queries the server for all versions of a given file
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Full path to the file where it would be placed locally within the sync root</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="includeDeletedVersions">(optional) whether to include file versions which are deleted</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFileVersions(int timeoutMilliseconds, FilePath pathToFile, out CLHttpRestStatus status, out JsonContracts.FileVersion[] response, bool includeDeletedVersions)
        {
            return _httpRestClient.GetFileVersions(timeoutMilliseconds, pathToFile, out status, out response, includeDeletedVersions);
        }

        /// <summary>
        /// Queries the server for all versions of a given file
        /// </summary>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Full path to the file where it would be placed locally within the sync root</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFileVersions(string fileServerId, int timeoutMilliseconds, FilePath pathToFile, out CLHttpRestStatus status, out JsonContracts.FileVersion[] response)
        {
            return _httpRestClient.GetFileVersions(fileServerId, timeoutMilliseconds, pathToFile, out status, out response);
        }

        /// <summary>
        /// Queries the server for all versions of a given file
        /// </summary>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Full path to the file where it would be placed locally within the sync root</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="includeDeletedVersions">(optional) whether to include file versions which are deleted</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFileVersions(string fileServerId, int timeoutMilliseconds, FilePath pathToFile, out CLHttpRestStatus status, out JsonContracts.FileVersion[] response, bool includeDeletedVersions)
        {
            return _httpRestClient.GetFileVersions(fileServerId, timeoutMilliseconds, pathToFile, out status, out response, includeDeletedVersions);
        }
        #endregion

        #region GetPictures
        /// <summary>
        /// Asynchronously starts querying the server for pictures
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetPictures(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds)
        {
            return _httpRestClient.BeginGetPictures(aCallback, aState, timeoutMilliseconds);
        }

        /// <summary>
        /// Finishes querying for pictures if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the pictures query</param>
        /// <param name="result">(output) The result from the pictures query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetPictures(IAsyncResult aResult, out GetPicturesResult result)
        {
            return _httpRestClient.EndGetPictures(aResult, out result);
        }

        /// <summary>
        /// Queries the server for pictures
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetPictures(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.Pictures response)
        {
            return _httpRestClient.GetPictures(timeoutMilliseconds, out status, out response);
        }
        #endregion

        #region GetVideos
        /// <summary>
        /// Asynchronously starts querying the server for videos
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetVideos(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds)
        {
            return _httpRestClient.BeginGetVideos(aCallback, aState, timeoutMilliseconds);
        }

        /// <summary>
        /// Finishes querying for videos if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the videos query</param>
        /// <param name="result">(output) The result from the videos query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetVideos(IAsyncResult aResult, out GetVideosResult result)
        {
            return _httpRestClient.EndGetVideos(aResult, out result);
        }

        /// <summary>
        /// Queries the server for videos
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetVideos(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.Videos response)
        {
            return _httpRestClient.GetVideos(timeoutMilliseconds, out status, out response);
        }
        #endregion

        #region GetAudios
        /// <summary>
        /// Asynchronously starts querying the server for audios
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetAudios(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds)
        {
            return _httpRestClient.BeginGetAudios(aCallback, aState, timeoutMilliseconds);
        }

        /// <summary>
        /// Finishes querying for audios if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the audios query</param>
        /// <param name="result">(output) The result from the audios query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetAudios(IAsyncResult aResult, out GetAudiosResult result)
        {
            return _httpRestClient.EndGetAudios(aResult, out result);
        }

        /// <summary>
        /// Queries the server for audios
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetAudios(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.Audios response)
        {
            return _httpRestClient.GetAudios(timeoutMilliseconds, out status, out response);
        }
        #endregion

        #region GetArchives
        /// <summary>
        /// Asynchronously starts querying the server for archives
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetArchives(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds)
        {
            return _httpRestClient.BeginGetArchives(aCallback, aState, timeoutMilliseconds);
        }

        /// <summary>
        /// Finishes querying for archives if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the archives query</param>
        /// <param name="result">(output) The result from the archives query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetArchives(IAsyncResult aResult, out GetArchivesResult result)
        {
            return _httpRestClient.EndGetArchives(aResult, out result);
        }

        /// <summary>
        /// Queries the server for archives
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetArchives(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.Archives response)
        {
            return _httpRestClient.GetArchives(timeoutMilliseconds, out status, out response);
        }
        #endregion

        #region GetRecents
        /// <summary>
        /// Asynchronously starts querying the server for recents
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetRecents(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds)
        {
            return _httpRestClient.BeginGetRecents(aCallback, aState, timeoutMilliseconds);
        }

        /// <summary>
        /// Finishes querying for recents if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the recents query</param>
        /// <param name="result">(output) The result from the recents query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetRecents(IAsyncResult aResult, out GetRecentsResult result)
        {
            return _httpRestClient.EndGetRecents(aResult, out result);
        }

        /// <summary>
        /// Queries the server for recents
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetRecents(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.Recents response)
        {
            return _httpRestClient.GetRecents(timeoutMilliseconds, out status, out response);
        }
        #endregion

        #region GetFolderContents
        /// <summary>
        /// Asynchronously starts querying folder contents with optional path and optional depth limit
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFolderContents(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds)
        {
            return _httpRestClient.BeginGetFolderContents(aCallback, aState, timeoutMilliseconds);
        }

        /// <summary>
        /// A simple object for holding a boolean in order to differentiate overloads on <see cref="CLSyncbox.BeginGetFolderContents"/>
        /// </summary>
        public sealed class SpecialBoolParameter
        {
            public bool Value
            {
                get
                {
                    return _value;
                }
            }
            private readonly bool _value;

            public SpecialBoolParameter(bool Value)
            {
                this._value = Value;
            }
        }
        
        /// <summary>
        /// Asynchronously starts querying folder contents with optional path and optional depth limit
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="includeCount">(optional) whether to include counts of items inside each folder in the response object</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFolderContents(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            SpecialBoolParameter includeCount)
        {
            if (includeCount == null)
            {
                return _httpRestClient.BeginGetFolderContents(aCallback, aState, timeoutMilliseconds);
            }
            else
            {
                return _httpRestClient.BeginGetFolderContents(aCallback, aState, timeoutMilliseconds,
                    includeCount: includeCount.Value);
            }
        }

        /// <summary>
        /// Asynchronously starts querying folder contents with optional path and optional depth limit
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="contentsRoot">(optional) root path of contents query</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFolderContents(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            FilePath contentsRoot)
        {
            return _httpRestClient.BeginGetFolderContents(aCallback, aState, timeoutMilliseconds,
                contentsRoot: contentsRoot);
        }

        /// <summary>
        /// Asynchronously starts querying folder contents with optional path and optional depth limit
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="includeCount">(optional) whether to include counts of items inside each folder in the response object</param>
        /// <param name="contentsRoot">(optional) root path of contents query</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFolderContents(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            bool includeCount,
            FilePath contentsRoot)
        {
            return _httpRestClient.BeginGetFolderContents(aCallback, aState, timeoutMilliseconds,
                includeCount: includeCount,
                contentsRoot: contentsRoot);
        }

        /// <summary>
        /// Asynchronously starts querying folder contents with optional path and optional depth limit
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="depthLimit">(optional) how many levels deep to search from the root or provided path, use {null} to return everything</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFolderContents(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            Nullable<byte> depthLimit)
        {
            return _httpRestClient.BeginGetFolderContents(aCallback, aState, timeoutMilliseconds,
                depthLimit: depthLimit);
        }

        /// <summary>
        /// Asynchronously starts querying folder contents with optional path and optional depth limit
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="includeCount">(optional) whether to include counts of items inside each folder in the response object</param>
        /// <param name="depthLimit">(optional) how many levels deep to search from the root or provided path, use {null} to return everything</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFolderContents(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            bool includeCount,
            Nullable<byte> depthLimit)
        {
            return _httpRestClient.BeginGetFolderContents(aCallback, aState, timeoutMilliseconds,
                includeCount: includeCount,
                depthLimit: depthLimit);
        }

        /// <summary>
        /// Asynchronously starts querying folder contents with optional path and optional depth limit
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="contentsRoot">(optional) root path of contents query</param>
        /// <param name="depthLimit">(optional) how many levels deep to search from the root or provided path, use {null} to return everything</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFolderContents(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            FilePath contentsRoot,
            Nullable<byte> depthLimit)
        {
            return _httpRestClient.BeginGetFolderContents(aCallback, aState, timeoutMilliseconds,
                contentsRoot: contentsRoot,
                depthLimit: depthLimit);
        }

        /// <summary>
        /// Asynchronously starts querying folder contents with optional path and optional depth limit
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="includeCount">(optional) whether to include counts of items inside each folder in the response object</param>
        /// <param name="contentsRoot">(optional) root path of contents query</param>
        /// <param name="depthLimit">(optional) how many levels deep to search from the root or provided path, use {null} to return everything</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFolderContents(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            bool includeCount,
            FilePath contentsRoot,
            Nullable<byte> depthLimit)
        {
            return _httpRestClient.BeginGetFolderContents(aCallback, aState, timeoutMilliseconds,
                includeCount: includeCount,
                contentsRoot: contentsRoot,
                depthLimit: depthLimit);
        }

        /// <summary>
        /// Asynchronously starts querying folder contents with optional path and optional depth limit
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="includeDeleted">(optional) whether to include changes which are marked deleted</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFolderContents(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            bool includeDeleted)
        {
            return _httpRestClient.BeginGetFolderContents(aCallback, aState, timeoutMilliseconds,
                includeDeleted: includeDeleted);
        }

        /// <summary>
        /// Asynchronously starts querying folder contents with optional path and optional depth limit
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="includeCount">(optional) whether to include counts of items inside each folder in the response object</param>
        /// <param name="includeDeleted">(optional) whether to include changes which are marked deleted</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFolderContents(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            bool includeCount,
            bool includeDeleted)
        {
            return _httpRestClient.BeginGetFolderContents(aCallback, aState, timeoutMilliseconds,
                includeCount: includeCount,
                includeDeleted: includeDeleted);
        }

        /// <summary>
        /// Asynchronously starts querying folder contents with optional path and optional depth limit
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="contentsRoot">(optional) root path of contents query</param>
        /// <param name="includeDeleted">(optional) whether to include changes which are marked deleted</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFolderContents(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            FilePath contentsRoot,
            bool includeDeleted)
        {
            return _httpRestClient.BeginGetFolderContents(aCallback, aState, timeoutMilliseconds,
                contentsRoot: contentsRoot,
                includeDeleted: includeDeleted);
        }

        /// <summary>
        /// Asynchronously starts querying folder contents with optional path and optional depth limit
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="includeCount">(optional) whether to include counts of items inside each folder in the response object</param>
        /// <param name="contentsRoot">(optional) root path of contents query</param>
        /// <param name="includeDeleted">(optional) whether to include changes which are marked deleted</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFolderContents(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            bool includeCount,
            FilePath contentsRoot,
            bool includeDeleted)
        {
            return _httpRestClient.BeginGetFolderContents(aCallback, aState, timeoutMilliseconds,
                includeCount: includeCount,
                contentsRoot: contentsRoot,
                includeDeleted: includeDeleted);
        }

        /// <summary>
        /// Asynchronously starts querying folder contents with optional path and optional depth limit
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="depthLimit">(optional) how many levels deep to search from the root or provided path, use {null} to return everything</param>
        /// <param name="includeDeleted">(optional) whether to include changes which are marked deleted</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFolderContents(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            Nullable<byte> depthLimit,
            bool includeDeleted)
        {
            return _httpRestClient.BeginGetFolderContents(aCallback, aState, timeoutMilliseconds,
                depthLimit: depthLimit,
                includeDeleted: includeDeleted);
        }

        /// <summary>
        /// Asynchronously starts querying folder contents with optional path and optional depth limit
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="includeCount">(optional) whether to include counts of items inside each folder in the response object</param>
        /// <param name="depthLimit">(optional) how many levels deep to search from the root or provided path, use {null} to return everything</param>
        /// <param name="includeDeleted">(optional) whether to include changes which are marked deleted</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFolderContents(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            bool includeCount,
            Nullable<byte> depthLimit,
            bool includeDeleted)
        {
            return _httpRestClient.BeginGetFolderContents(aCallback, aState, timeoutMilliseconds,
                includeCount: includeCount,
                depthLimit: depthLimit,
                includeDeleted: includeDeleted);
        }

        /// <summary>
        /// Asynchronously starts querying folder contents with optional path and optional depth limit
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="contentsRoot">(optional) root path of contents query</param>
        /// <param name="depthLimit">(optional) how many levels deep to search from the root or provided path, use {null} to return everything</param>
        /// <param name="includeDeleted">(optional) whether to include changes which are marked deleted</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFolderContents(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            FilePath contentsRoot,
            Nullable<byte> depthLimit,
            bool includeDeleted)
        {
            return _httpRestClient.BeginGetFolderContents(aCallback, aState, timeoutMilliseconds,
                contentsRoot: contentsRoot,
                depthLimit: depthLimit,
                includeDeleted: includeDeleted);
        }

        /// <summary>
        /// Asynchronously starts querying folder contents with optional path and optional depth limit
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="includeCount">(optional) whether to include counts of items inside each folder in the response object</param>
        /// <param name="contentsRoot">(optional) root path of contents query</param>
        /// <param name="depthLimit">(optional) how many levels deep to search from the root or provided path, use {null} to return everything</param>
        /// <param name="includeDeleted">(optional) whether to include changes which are marked deleted</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFolderContents(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            bool includeCount,
            FilePath contentsRoot,
            Nullable<byte> depthLimit,
            bool includeDeleted)
        {
            return _httpRestClient.BeginGetFolderContents(aCallback, aState, timeoutMilliseconds,
                includeCount: includeCount,
                contentsRoot: contentsRoot,
                depthLimit: depthLimit,
                includeDeleted: includeDeleted);
        }
        
        /// <summary>
        /// Finishes getting folder contents if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting getting folder contents</param>
        /// <param name="result">(output) The result from folder contents</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetFolderContents(IAsyncResult aResult, out GetFolderContentsResult result)
        {
            return _httpRestClient.EndGetFolderContents(aResult, out result);
        }

        /// <summary>
        /// Queries server for folder contents with an optional path and an optional depth limit
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderContents(
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            out JsonContracts.FolderContents response)
        {
            return _httpRestClient.GetFolderContents(timeoutMilliseconds, out status, out response);
        }

        /// <summary>
        /// Queries server for folder contents with an optional path and an optional depth limit
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="includeCount">(optional) whether to include counts of items inside each folder in the response object</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderContents(
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            out JsonContracts.FolderContents response,
            SpecialBoolParameter includeCount)
        {
            if (includeCount == null)
            {
                return _httpRestClient.GetFolderContents(timeoutMilliseconds, out status, out response);
            }
            else
            {
                return _httpRestClient.GetFolderContents(timeoutMilliseconds, out status, out response,
                    includeCount: includeCount.Value);
            }
        }

        /// <summary>
        /// Queries server for folder contents with an optional path and an optional depth limit
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="contentsRoot">(optional) root path of hierarchy query</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderContents(
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            out JsonContracts.FolderContents response,
            FilePath contentsRoot)
        {
            return _httpRestClient.GetFolderContents(timeoutMilliseconds, out status, out response,
                contentsRoot: contentsRoot);
        }

        /// <summary>
        /// Queries server for folder contents with an optional path and an optional depth limit
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="includeCount">(optional) whether to include counts of items inside each folder in the response object</param>
        /// <param name="contentsRoot">(optional) root path of hierarchy query</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderContents(
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            out JsonContracts.FolderContents response,
            bool includeCount,
            FilePath contentsRoot)
        {
            return _httpRestClient.GetFolderContents(timeoutMilliseconds, out status, out response,
                includeCount: includeCount,
                contentsRoot: contentsRoot);
        }

        /// <summary>
        /// Queries server for folder contents with an optional path and an optional depth limit
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="depthLimit">(optional) how many levels deep to search from the root or provided path, use {null} to return everything</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderContents(
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            out JsonContracts.FolderContents response,
            Nullable<byte> depthLimit)
        {
            return _httpRestClient.GetFolderContents(timeoutMilliseconds, out status, out response,
                depthLimit: depthLimit);
        }

        /// <summary>
        /// Queries server for folder contents with an optional path and an optional depth limit
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="includeCount">(optional) whether to include counts of items inside each folder in the response object</param>
        /// <param name="depthLimit">(optional) how many levels deep to search from the root or provided path, use {null} to return everything</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderContents(
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            out JsonContracts.FolderContents response,
            bool includeCount,
            Nullable<byte> depthLimit)
        {
            return _httpRestClient.GetFolderContents(timeoutMilliseconds, out status, out response,
                includeCount: includeCount,
                depthLimit: depthLimit);
        }

        /// <summary>
        /// Queries server for folder contents with an optional path and an optional depth limit
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="contentsRoot">(optional) root path of hierarchy query</param>
        /// <param name="depthLimit">(optional) how many levels deep to search from the root or provided path, use {null} to return everything</param>\
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderContents(
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            out JsonContracts.FolderContents response,
            FilePath contentsRoot,
            Nullable<byte> depthLimit)
        {
            return _httpRestClient.GetFolderContents(timeoutMilliseconds, out status, out response,
                contentsRoot: contentsRoot,
                depthLimit: depthLimit);
        }

        /// <summary>
        /// Queries server for folder contents with an optional path and an optional depth limit
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="includeCount">(optional) whether to include counts of items inside each folder in the response object</param>
        /// <param name="contentsRoot">(optional) root path of hierarchy query</param>
        /// <param name="depthLimit">(optional) how many levels deep to search from the root or provided path, use {null} to return everything</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderContents(
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            out JsonContracts.FolderContents response,
            bool includeCount,
            FilePath contentsRoot,
            Nullable<byte> depthLimit)
        {
            return _httpRestClient.GetFolderContents(timeoutMilliseconds, out status, out response,
                includeCount: includeCount,
                contentsRoot: contentsRoot,
                depthLimit: depthLimit);
        }

        /// <summary>
        /// Queries server for folder contents with an optional path and an optional depth limit
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="includeDeleted">(optional) whether to include changes which are marked deleted</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderContents(
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            out JsonContracts.FolderContents response,
            bool includeDeleted)
        {
            return _httpRestClient.GetFolderContents(timeoutMilliseconds, out status, out response,
                includeDeleted: includeDeleted);
        }

        /// <summary>
        /// Queries server for folder contents with an optional path and an optional depth limit
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="includeCount">(optional) whether to include counts of items inside each folder in the response object</param>
        /// <param name="includeDeleted">(optional) whether to include changes which are marked deleted</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderContents(
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            out JsonContracts.FolderContents response,
            bool includeCount,
            bool includeDeleted)
        {
            return _httpRestClient.GetFolderContents(timeoutMilliseconds, out status, out response,
                includeCount: includeCount,
                includeDeleted: includeDeleted);
        }

        /// <summary>
        /// Queries server for folder contents with an optional path and an optional depth limit
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="contentsRoot">(optional) root path of hierarchy query</param>
        /// <param name="includeDeleted">(optional) whether to include changes which are marked deleted</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderContents(
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            out JsonContracts.FolderContents response,
            FilePath contentsRoot,
            bool includeDeleted)
        {
            return _httpRestClient.GetFolderContents(timeoutMilliseconds, out status, out response,
                contentsRoot: contentsRoot,
                includeDeleted: includeDeleted);
        }

        /// <summary>
        /// Queries server for folder contents with an optional path and an optional depth limit
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="includeCount">(optional) whether to include counts of items inside each folder in the response object</param>
        /// <param name="contentsRoot">(optional) root path of hierarchy query</param>
        /// <param name="includeDeleted">(optional) whether to include changes which are marked deleted</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderContents(
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            out JsonContracts.FolderContents response,
            bool includeCount,
            FilePath contentsRoot,
            bool includeDeleted)
        {
            return _httpRestClient.GetFolderContents(timeoutMilliseconds, out status, out response,
                includeCount: includeCount,
                contentsRoot: contentsRoot,
                includeDeleted: includeDeleted);
        }

        /// <summary>
        /// Queries server for folder contents with an optional path and an optional depth limit
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="depthLimit">(optional) how many levels deep to search from the root or provided path, use {null} to return everything</param>
        /// <param name="includeDeleted">(optional) whether to include changes which are marked deleted</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderContents(
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            out JsonContracts.FolderContents response,
            Nullable<byte> depthLimit,
            bool includeDeleted)
        {
            return _httpRestClient.GetFolderContents(timeoutMilliseconds, out status, out response,
                depthLimit: depthLimit,
                includeDeleted: includeDeleted);
        }

        /// <summary>
        /// Queries server for folder contents with an optional path and an optional depth limit
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="includeCount">(optional) whether to include counts of items inside each folder in the response object</param>
        /// <param name="depthLimit">(optional) how many levels deep to search from the root or provided path, use {null} to return everything</param>
        /// <param name="includeDeleted">(optional) whether to include changes which are marked deleted</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderContents(
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            out JsonContracts.FolderContents response,
            bool includeCount,
            Nullable<byte> depthLimit,
            bool includeDeleted)
        {
            return _httpRestClient.GetFolderContents(timeoutMilliseconds, out status, out response,
                includeCount: includeCount,
                depthLimit: depthLimit,
                includeDeleted: includeDeleted);
        }

        /// <summary>
        /// Queries server for folder contents with an optional path and an optional depth limit
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="contentsRoot">(optional) root path of hierarchy query</param>
        /// <param name="depthLimit">(optional) how many levels deep to search from the root or provided path, use {null} to return everything</param>
        /// <param name="includeDeleted">(optional) whether to include changes which are marked deleted</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderContents(
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            out JsonContracts.FolderContents response,
            FilePath contentsRoot,
            Nullable<byte> depthLimit,
            bool includeDeleted)
        {
            return _httpRestClient.GetFolderContents(timeoutMilliseconds, out status, out response,
                contentsRoot: contentsRoot,
                depthLimit: depthLimit,
                includeDeleted: includeDeleted);
        }

        /// <summary>
        /// Queries server for folder contents with an optional path and an optional depth limit
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="includeCount">(optional) whether to include counts of items inside each folder in the response object</param>
        /// <param name="contentsRoot">(optional) root path of hierarchy query</param>
        /// <param name="depthLimit">(optional) how many levels deep to search from the root or provided path, use {null} to return everything</param>
        /// <param name="includeDeleted">(optional) whether to include changes which are marked deleted</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderContents(
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            out JsonContracts.FolderContents response,
            bool includeCount,
            FilePath contentsRoot,
            Nullable<byte> depthLimit,
            bool includeDeleted)
        {
            return _httpRestClient.GetFolderContents(timeoutMilliseconds, out status, out response,
                includeCount: includeCount,
                contentsRoot: contentsRoot,
                depthLimit: depthLimit,
                includeDeleted: includeDeleted);
        }
        #endregion
        
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
            return _httpRestClient.EndGetFolderHierarchy(aResult, out result);
        }

        /// <summary>
        /// Queries server for folder hierarchy with an optional path
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderHierarchy(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.Folders response)
        {
            return _httpRestClient.GetFolderHierarchy(timeoutMilliseconds, out status, out response);
        }

        /// <summary>
        /// Queries server for folder hierarchy with an optional path
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="hierarchyRoot">(optional) root path of hierarchy query</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderHierarchy(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.Folders response, FilePath hierarchyRoot)
        {
            return _httpRestClient.GetFolderHierarchy(timeoutMilliseconds, out status, out response, hierarchyRoot);
        }
        #endregion

        #region Usage (get the usage information for this syncbox from the cloud)
        /// <summary>
        /// Asynchronously starts getting the usage information for this syncbox from the cloud.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass as a parameter when firing async callback</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginUsage(AsyncCallback callback, object callbackUserState)
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
        public CLError EndUsage(IAsyncResult aResult, out SyncboxUsageResult result)
        {
            CheckDisposed();
            return _httpRestClient.EndGetSyncboxUsage(aResult, out result);
        }

        /// <summary>
        /// Queries the cloud for syncbox usage information.  This method is synchronous.
        /// </summary>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError Usage(out CLHttpRestStatus status, out JsonContracts.SyncboxUsageResponse response)
        {
            CheckDisposed();
            return _httpRestClient.GetSyncboxUsage(_copiedSettings.HttpTimeoutMilliseconds, out status, out response);
        }
        #endregion  // end (get the usage information for this syncbox from the cloud)

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
            return _httpRestClient.EndUpdateSyncboxExtendedMetadata(aResult, out result);
        }

        /// <summary>
        /// Updates the extended metadata on a sync box
        /// </summary>
        /// <param name="metadata">string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError UpdateSyncboxExtendedMetadata<T>(IDictionary<string, T> metadata, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SyncboxResponse response)
        {
            return _httpRestClient.UpdateSyncboxExtendedMetadata(metadata, timeoutMilliseconds, out status, out response);
        }

        /// <summary>
        /// Updates the extended metadata on a sync box
        /// </summary>
        /// <param name="metadata">string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError SyncboxUpdateExtendedMetadata(JsonContracts.MetadataDictionary metadata, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SyncboxResponse response)
        {
            return _httpRestClient.UpdateSyncboxExtendedMetadata(metadata, timeoutMilliseconds, out status, out response);
        }
        #endregion

        #region UpdateSyncboxQuota (deprecated)
        ///// <summary>
        ///// Asynchronously updates the storage quota on a sync box
        ///// </summary>
        ///// <param name="aCallback">Callback method to fire when operation completes</param>
        ///// <param name="aState">Userstate to pass when firing async callback</param>
        ///// <param name="quotaSize">How many bytes big to make the storage quota</param>
        ///// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        ///// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        //public IAsyncResult BeginUpdateSyncboxQuota(AsyncCallback aCallback,
        //    object aState,
        //    long quotaSize,
        //    int timeoutMilliseconds)
        //{
        //    return _httpRestClient.BeginUpdateSyncboxQuota(aCallback, aState, quotaSize, timeoutMilliseconds, ReservedForActiveSync);
        //}

        ///// <summary>
        ///// Finishes updating the storage quota on a sync box if it has not already finished via its asynchronous result and outputs the result,
        ///// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        ///// </summary>
        ///// <param name="aResult">The asynchronous result provided upon starting updating storage quota</param>
        ///// <param name="result">(output) The result from updating storage quota</param>
        ///// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        //public CLError EndUpdateSyncboxQuota(IAsyncResult aResult, out SyncboxUpdateQuotaResult result)
        //{
        //    return _httpRestClient.EndUpdateSyncboxQuota(aResult, out result);
        //}

        ///// <summary>
        ///// Updates the storage quota on a sync box
        ///// </summary>
        ///// <param name="quotaSize">How many bytes big to make the storage quota</param>
        ///// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        ///// <param name="status">(output) success/failure status of communication</param>
        ///// <param name="response">(output) response object from communication</param>
        ///// <returns>Returns any error that occurred during communication, if any</returns>
        //public CLError UpdateSyncboxQuota(long quotaSize, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SyncboxHolder response)
        //{
        //    return _httpRestClient.UpdateSyncboxQuota(quotaSize, timeoutMilliseconds, out status, out response, ReservedForActiveSync);
        //}
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
            return _httpRestClient.BeginUpdateSyncboxPlan(callback, callbackUserState, storagePlan.Id, _copiedSettings.HttpTimeoutMilliseconds, ReservedForActiveSync);
        }

        /// <summary>
        /// Finishes updating the storage plan for this syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) The result from completing the request</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndUpdateStoragePlan(IAsyncResult aResult, out SyncboxUpdatePlanResult result)
        {
            CheckDisposed();
            CLError toReturn = _httpRestClient.EndUpdateSyncboxPlan(aResult, out result);
            if (toReturn == null 
                && result != null 
                && result.Response != null 
                && result.Response.Syncbox != null
                && result.Response.Syncbox.PlanId != null)
            {
                this._storagePlanIdHolder.Value = result.Response.Syncbox.PlanId ?? 0;
            }
            return toReturn;
        }

        /// <summary>
        /// Updates the storage plan for a syncbox in the cloud.  This is a synchronous method.
        /// Updates this object's StoragePlanId property.
        /// </summary>
        /// <param name="storagePlan">The storage plan to set (new storage plan to use for this syncbox)</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError UpdateStoragePlan(CLStoragePlan storagePlan, out CLHttpRestStatus status, out JsonContracts.SyncboxUpdatePlanResponse response)
        {
            CheckDisposed();
            CLError toReturn =  _httpRestClient.UpdateSyncboxPlan(storagePlan.Id, _copiedSettings.HttpTimeoutMilliseconds, out status, out response, ReservedForActiveSync);
            if (toReturn == null 
                && response != null 
                && response.Plan != null
                && response.Plan.Id != null)
            {
                this._storagePlanIdHolder.Value = response.Plan.Id ?? 0;
            }
            return toReturn;
        }
        #endregion  // end (changes the storage plan associated with this syncbox in the cloud)

        // We won't publish this.
        //#region UpdateFriendlyName (Update the friendly name for a syncbox in the cloud)
        ///// <summary>
        ///// Asynchronously updates the friendly name of this syncbox in the cloud.
        ///// </summary>
        ///// <param name="callback">Callback method to fire when operation completes</param>
        ///// <param name="callbackUserState">User state to pass when firing async callback</param>
        ///// <param name="friendlyName">The new friendly name of this syncbox.</param>
        ///// <returns>Returns the asynchronous result which is used to retrieve the response</returns>
        ///// <remarks>The FriendlyName property of this object will also be updated on success.</remarks>
        //public IAsyncResult BeginUpdateFriendlyName(AsyncCallback callback, object callbackUserState, string friendlyName)
        //{
        //    CheckDisposed();
        //    return _httpRestClient.BeginUpdateSyncbox(callback, callbackUserState, friendlyName, _copiedSettings.HttpTimeoutMilliseconds, ReservedForActiveSync);
        //}

        ///// <summary>
        ///// Finishes updating the friendly name of this syncbox in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
        ///// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        ///// </summary>
        ///// <param name="aResult">The asynchronous result provided upon starting the operation</param>
        ///// <param name="result">(output) The result from the asynchronous operation.</param>
        ///// <returns>Returns the error that occurred while finishing and/or outputting the result, if any</returns>
        //public CLError EndUpdateFriendlyName(IAsyncResult aResult, out SyncboxUpdateFriendlyNameResult result)
        //{
        //    CheckDisposed();
        //    CLError toReturn =  _httpRestClient.EndUpdateSyncbox(aResult, out result);
        //    if (toReturn == null && result != null && result.Result != null && result.Result.Syncbox != null)
        //    {
        //        _friendlyNameHolder.Value = result.Result.Syncbox.FriendlyName;   // update our property too
        //    }
        //    return toReturn;
        //}

        ///// <summary>
        ///// Updates the properties of a syncbox in the cloud.  This is a synchronous operation.
        ///// </summary>
        ///// <param name="friendlyName">The friendly name of this syncbox.</param>
        ///// <param name="status">(output) success/failure status of communication</param>
        ///// <param name="response">(output) response object from communication</param>
        ///// <returns>Returns any error that occurred during communication, if any</returns>
        ///// <remarks>The FriendlyName property of this object will also be updated on success.</remarks>
        //public CLError UpdateFriendlyName(string friendlyName, out CLHttpRestStatus status, out JsonContracts.SyncboxResponse response)
        //{
        //    CheckDisposed();
        //    CLError toReturn = _httpRestClient.UpdateSyncbox(friendlyName, _copiedSettings.HttpTimeoutMilliseconds, out status, out response, ReservedForActiveSync);
        //    if (toReturn == null && response != null)
        //    {
        //        _friendlyNameHolder.Value = response.Syncbox.FriendlyName;       // update our local copy
        //    }
        //    return toReturn;
        //}
        //#endregion  // end UpdateFriendlyName (Update the friendly namd for a syncbox in the cloud)

        #region DeleteSyncbox
        /// <summary>l
        /// ¡¡ Do not use lightly !! Asynchronously deletes a sync box
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginDeleteSyncbox(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds)
        {
            return _httpRestClient.BeginDeleteSyncbox(aCallback, aState, timeoutMilliseconds, ReservedForActiveSync);
        }

        /// <summary>
        /// Finishes deleting a sync box if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting deleting the sync box</param>
        /// <param name="result">(output) The result from deleting the sync box</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndDeleteSyncbox(IAsyncResult aResult, out DeleteSyncboxResult result)
        {
            return _httpRestClient.EndDeleteSyncbox(aResult, out result);
        }

        /// <summary>
        /// ¡¡ Do not use lightly !! Deletes a sync box
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError DeleteSyncbox(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SyncboxResponse response)
        {
            return _httpRestClient.DeleteSyncbox(timeoutMilliseconds, out status, out response, ReservedForActiveSync);
        }
        #endregion

        #region Status (update the status of this syncbox from the cloud)
        /// <summary>
        /// Asynchronously gets the status of this Syncbox.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetStatus(AsyncCallback callback, object callbackUserState)
        {
            return _httpRestClient.BeginGetSyncboxStatus(callback, callbackUserState, _copiedSettings.HttpTimeoutMilliseconds, new Action<JsonContracts.SyncboxResponse, object>(StatusCompletion), null);
        }
        
        /// <summary>
        /// Finishes the asynchronous request, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error).
        /// The local object status is updated with the server results.
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) The result from the request</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetStatus(IAsyncResult aResult, out SyncboxStatusResult result)
        {
            return _httpRestClient.EndGetSyncboxStatus(aResult, out result);
        }

        /// <summary>
        /// Gets the status of this Syncbox.  This is a synchronous method.
        /// The local object status is updated with the server results.
        /// </summary>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetStatus(out CLHttpRestStatus status, out JsonContracts.SyncboxResponse response)
        {
            return _httpRestClient.GetSyncboxStatus(_copiedSettings.HttpTimeoutMilliseconds, out status, out response, new Action<JsonContracts.SyncboxResponse, object>(StatusCompletion), null);
        }

        private void StatusCompletion(JsonContracts.SyncboxResponse response, object userState)
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

            this._friendlyNameHolder.Value = response.Syncbox.FriendlyName;
            this._storagePlanIdHolder.Value = (long)response.Syncbox.PlanId;

            this._propertyChangeLocker.ExitWriteLock();
        }
        #endregion  // end (update the status of this syncbox from the cloud)

        #region SendFileChanges (Sends syncbox file and folder sync operations to the cloud)
        /// <summary>
        /// Asynchronously starts posting a single FileChange to the server
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="toCommunicate">Single FileChange to send</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginSendFileChange(AsyncCallback aCallback,
            object aState,
            FileChange toCommunicate)
        {
            CheckDisposed();

            // create the asynchronous result to return
            GenericAsyncResult<FileChangeResult> toReturn = new GenericAsyncResult<FileChangeResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<FileChangeResult>, FileChange, int> asyncParams =
                new Tuple<GenericAsyncResult<FileChangeResult>, FileChange, int>(
                    toReturn,
                    toCommunicate,
                    _copiedSettings.HttpTimeoutMilliseconds);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<FileChangeResult>, FileChange, int> castState = state as Tuple<GenericAsyncResult<FileChangeResult>, FileChange, int>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        "Cannot cast state as " + Helpers.GetTypeNameEvenForNulls(castState),
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());
                }
                // else if the try cast did not fail, then start processing with the input parameters
                else
                {
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the output status for communication
                        CLHttpRestStatus status;
                        // declare the specific type of result for this operation
                        JsonContracts.FileChangeResponse result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = SendFileChange(
                            castState.Item2,
                            castState.Item3,
                            out status,
                            out result);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new FileChangeResult(
                                    processError, // any error that may have occurred during processing
                                    status, // the output status of communication
                                    result), // the specific type of result for this operation
                                    sCompleted: false); // processing did not complete synchronously
                        }
                    }
                    catch (Exception ex)
                    {
                        // if there was an asynchronous result in the parameters, then pass through the exception to it
                        if (castState.Item1 != null)
                        {
                            castState.Item1.HandleException(
                                ex, // the exception which was not handled correctly by the CLError wrapping
                                sCompleted: false); // processing did not complete synchronously
                        }
                    }
                }
            }))).Start(asyncParams); // start the asynchronous processing thread with the input parameters object

            // return the asynchronous result
            return toReturn;
        }

        /// <summary>
        /// Finishes posting a FileChange if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the FileChange post</param>
        /// <param name="result">(output) The result from the FileChange post</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndSendFileChange(IAsyncResult aResult, out FileChangeResult result)
        {
            CheckDisposed();

            // declare the specific type of asynchronous result for FileChange post
            GenericAsyncResult<FileChangeResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for FileChange post and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for FileChange post
                castAResult = aResult as GenericAsyncResult<FileChangeResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException("aResult does not match expected internal type");
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<FileChangeResult>();
                return ex;
            }

            // try/catch to finish the asynchronous operation if necessary, re-pull the result for output, and rethrow any exception which may have occurred; on catch, return the error
            try
            {
                // This method assumes that only 1 thread calls EndInvoke 
                // for this object
                if (!castAResult.IsCompleted)
                {
                    // If the operation isn't done, wait for it
                    castAResult.AsyncWaitHandle.WaitOne();
                    castAResult.AsyncWaitHandle.Close();
                }

                // re-pull the result for output in case it was not completed when it was pulled before
                result = castAResult.Result;

                // Operation is done: if an exception occurred, return it
                if (castAResult.Exception != null)
                {
                    return castAResult.Exception;
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Posts a single FileChange to the server to update the sync box in the cloud.
        /// May still require uploading a file with a returned storage key if the Header.Status property in response is "upload" or "uploading".
        /// Check Header.Status property in response for errors or conflict.
        /// </summary>
        /// <param name="toCommunicate">Single FileChange to send</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError SendFileChange(FileChange toCommunicate, out CLHttpRestStatus status, out JsonContracts.FileChangeResponse response)
        {
            CheckDisposed();

            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the file change post, on catch return the error
            try
            {
                // check input parameters

                if (toCommunicate == null)
                {
                    throw new NullReferenceException("toCommunicate cannot be null");
                }
                if (toCommunicate.Direction == SyncDirection.From)
                {
                    throw new ArgumentException("toCommunicate Direction is not To the server");
                }
                if (toCommunicate.Metadata == null)
                {
                    throw new NullReferenceException("toCommunicate Metadata cannot be null");
                }
                if (toCommunicate.Type == FileChangeType.Modified
                    && toCommunicate.Metadata.HashableProperties.IsFolder)
                {
                    throw new ArgumentException("toCommunicate cannot be both a folder and of type Modified");
                }
                if (_copiedSettings.DeviceId == null)
                {
                    throw new NullReferenceException("settings DeviceId cannot be null");
                }
                if (_syncbox.Path == null)
                {
                    throw new NullReferenceException("settings SyncRoot cannot be null");
                }
                if (!(_copiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // build the location of the one-off method on the server dynamically
                string serverMethodPath;
                object requestContent;

                // set server method path and the request content dynamically based on whether change is a file or folder and based on the type of change
                switch (toCommunicate.Type)
                {
                    // file or folder created
                    case FileChangeType.Created:

                        // check additional parameters for file or folder creation

                        if (toCommunicate.NewPath == null)
                        {
                            throw new NullReferenceException("toCommunicate NewPath cannot be null");
                        }

                        // if change is a folder, set path and create request content for folder creation
                        if (toCommunicate.Metadata.HashableProperties.IsFolder)
                        {
                            serverMethodPath = CLDefinitions.MethodPathOneOffFolderCreate;

                            requestContent = new JsonContracts.FolderAdd()
                            {
                                CreatedDate = toCommunicate.Metadata.HashableProperties.CreationTime,
                                DeviceId = _copiedSettings.DeviceId,
                                RelativePath = toCommunicate.NewPath.GetRelativePath(_syncbox.Path, true) + "/",
                                SyncboxId = _syncbox.SyncboxId
                            };
                        }
                        // else if change is a file, set path and create request content for file creation
                        else
                        {
                            string addHashString;
                            CLError addHashStringError = toCommunicate.GetMD5LowercaseString(out addHashString);
                            if (addHashStringError != null)
                            {
                                throw new AggregateException("Error retrieving toCommunicate MD5 lowercase string", addHashStringError.GrabExceptions());
                            }

                            // check additional parameters for file creation

                            if (string.IsNullOrEmpty(addHashString))
                            {
                                throw new NullReferenceException("MD5 lowercase string retrieved from toCommunicate cannot be null, set via toCommunicate.SetMD5");
                            }
                            if (toCommunicate.Metadata.HashableProperties.Size == null)
                            {
                                throw new NullReferenceException("toCommunicate Metadata HashableProperties Size cannot be null");
                            }

                            serverMethodPath = CLDefinitions.MethodPathOneOffFileCreate;

                            requestContent = new JsonContracts.FileAdd()
                            {
                                CreatedDate = toCommunicate.Metadata.HashableProperties.CreationTime,
                                DeviceId = _copiedSettings.DeviceId,
                                Hash = addHashString,
                                MimeType = toCommunicate.Metadata.MimeType,
                                ModifiedDate = toCommunicate.Metadata.HashableProperties.LastTime,
                                RelativePath = toCommunicate.NewPath.GetRelativePath(_syncbox.Path, true),
                                Size = toCommunicate.Metadata.HashableProperties.Size,
                                SyncboxId = _syncbox.SyncboxId
                            };
                        }
                        break;

                    case FileChangeType.Deleted:

                        // check additional parameters for file or folder deletion

                        if (toCommunicate.NewPath == null
                            && string.IsNullOrEmpty(toCommunicate.Metadata.ServerUid))
                        {
                            throw new NullReferenceException("Either toCommunicate NewPath must not be null or toCommunicate Metadata ServerId must not be null or both must not be null");
                        }

                        // file deletion and folder deletion share a json contract object for deletion
                        requestContent = new JsonContracts.FileOrFolderDelete()
                        {
                            DeviceId = _copiedSettings.DeviceId,
                            RelativePath = (toCommunicate.NewPath == null
                                ? null
                                : toCommunicate.NewPath.GetRelativePath(_syncbox.Path, true) +
                                    (toCommunicate.Metadata.HashableProperties.IsFolder ? "/" : string.Empty)),
                            ServerUid = toCommunicate.Metadata.ServerUid,
                            SyncboxId = _syncbox.SyncboxId
                        };

                        // server method path switched from whether change is a folder or not
                        serverMethodPath = (toCommunicate.Metadata.HashableProperties.IsFolder
                            ? CLDefinitions.MethodPathOneOffFolderDelete
                            : CLDefinitions.MethodPathOneOffFileDelete);
                        break;

                    case FileChangeType.Modified:

                        // grab MD5 hash string and rethrow any error that occurs

                        string modifyHashString;
                        CLError modifyHashStringError = toCommunicate.GetMD5LowercaseString(out modifyHashString);
                        if (modifyHashStringError != null)
                        {
                            throw new AggregateException("Error retrieving toCommunicate MD5 lowercase string", modifyHashStringError.GrabExceptions());
                        }

                        // check additional parameters for file modification

                        if (string.IsNullOrEmpty(modifyHashString))
                        {
                            throw new NullReferenceException("MD5 lowercase string retrieved from toCommunicate cannot be null, set via toCommunicate.SetMD5");
                        }
                        if (toCommunicate.Metadata.HashableProperties.Size == null)
                        {
                            throw new NullReferenceException("toCommunicate Metadata HashableProperties Size cannot be null");
                        }
                        if (toCommunicate.NewPath == null
                            && string.IsNullOrEmpty(toCommunicate.Metadata.ServerUid))
                        {
                            throw new NullReferenceException("Either toCommunicate NewPath must not be null or toCommunicate Metadata ServerId must not be null or both must not be null");
                        }
                        if (string.IsNullOrEmpty(toCommunicate.Metadata.Revision))
                        {
                            throw new NullReferenceException("toCommunicate Metadata Revision cannot be null");
                        }

                        // there is no folder modify, so json contract object and server method path for modify are only for files

                        requestContent = new JsonContracts.FileModify()
                        {
                            CreatedDate = toCommunicate.Metadata.HashableProperties.CreationTime,
                            DeviceId = _copiedSettings.DeviceId,
                            Hash = modifyHashString,
                            MimeType = toCommunicate.Metadata.MimeType,
                            ModifiedDate = toCommunicate.Metadata.HashableProperties.LastTime,
                            RelativePath = (toCommunicate.NewPath == null
                                ? null
                                : toCommunicate.NewPath.GetRelativePath(_syncbox.Path, true)),
                            Revision = toCommunicate.Metadata.Revision,
                            ServerUid = toCommunicate.Metadata.ServerUid,
                            Size = toCommunicate.Metadata.HashableProperties.Size,
                            SyncboxId = _syncbox.SyncboxId
                        };

                        serverMethodPath = CLDefinitions.MethodPathOneOffFileModify;
                        break;

                    case FileChangeType.Renamed:

                        // check additional parameters for file or folder move (rename)

                        if (toCommunicate.NewPath == null
                            && string.IsNullOrEmpty(toCommunicate.Metadata.ServerUid))
                        {
                            throw new NullReferenceException("Either toCommunicate NewPath must not be null or toCommunicate Metadata ServerId must not be null or both must not be null");
                        }
                        if (toCommunicate.OldPath == null)
                        {
                            throw new NullReferenceException("toCommunicate OldPath cannot be null");
                        }

                        // file move (rename) and folder move (rename) share a json contract object for move (rename)
                        requestContent = new JsonContracts.FileOrFolderMove()
                        {
                            DeviceId = _copiedSettings.DeviceId,
                            RelativeFromPath = toCommunicate.OldPath.GetRelativePath(_syncbox.Path, true) +
                                (toCommunicate.Metadata.HashableProperties.IsFolder ? "/" : string.Empty),
                            RelativeToPath = (toCommunicate.NewPath == null
                                ? null
                                : toCommunicate.NewPath.GetRelativePath(_syncbox.Path, true)
                                    + (toCommunicate.Metadata.HashableProperties.IsFolder ? "/" : string.Empty)),
                            ServerUid = toCommunicate.Metadata.ServerUid,
                            SyncboxId = _syncbox.SyncboxId
                        };

                        // server method path switched on whether change is a folder or not
                        serverMethodPath = (toCommunicate.Metadata.HashableProperties.IsFolder
                            ? CLDefinitions.MethodPathOneOffFolderMove
                            : CLDefinitions.MethodPathOneOffFileMove);
                        break;

                    default:
                        throw new ArgumentException("toCommunicate Type is an unknown FileChangeType: " + toCommunicate.Type.ToString());
                }

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.FileChangeResponse>(requestContent, // dynamic type of request content based on method path
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.post, // one-off methods are all posts
                    _copiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status, // reference to update the output success/failure status for the communication
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo);   // pass the optional parameters to support temporary token reallocation.
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.FileChangeResponse>();
                return ex;
            }
            return null;
        }
        #endregion  // end (Sends syncbox file and folder sync operations to the cloud)

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
        /// A serious notification error has occurred.  Push notification is no longer functioning.
        /// </summary>
        /// <param name="sender">The sending object.</param>
        /// <param name="e">Arguments including the manual poll and/or web sockets errors (possibly aggregated).</param>
        internal void OnPushNotificationConnectionError(object sender, NotificationErrorEventArgs e)
        {
            try
            {
                // Tell the application
                _trace.writeToLog(1, "CLSyncbox: OnPushNotificationConnectionError: Entry. ERROR: Manual poll error: <{0}>. Web socket error: <{1}>.", e.ErrorStillDisconnectedPing.errorDescription, e.ErrorWebSockets.errorDescription);
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
                        _propertyChangeLocker.Dispose();
                        _propertyChangeLocker = null;
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