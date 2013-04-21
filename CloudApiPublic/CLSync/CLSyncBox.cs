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
    public sealed class CLSyncbox
    {
        #region Private Fields
        
        private static CLTrace _trace = CLTrace.Instance;
        private static readonly List<CLSyncEngine> _startedSyncEngines = new List<CLSyncEngine>();
        private static readonly object _startLocker = new object();

        private CLSyncEngine _syncEngine = null;
		private readonly System.Threading.WaitCallback _statusChangedCallback = null;
        private readonly object _statusChangedCallbackUserState = null;
        private bool _isStarted = false;

        #endregion  // end Private Fields

        #region Internal Fields

        internal bool IsModifyingSyncboxViaPublicAPICalls
        {
            get
            {
                return _httpRestClient.IsModifyingSyncboxViaPublicAPICalls;
            }
        }
        
        #endregion  // end Internal Fields

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

        #region Private Constructor

        private CLSyncbox(
            long syncboxId,
            CLCredentials credentials,
            string path,
            ref CLSyncboxCreationStatus status,
            ICLSyncSettings Settings,
            Helpers.ReplaceExpiredCredentials getNewCredentialsCallback = null,
            object getNewCredentialsCallbackUserState = null,
            System.Threading.WaitCallback statusChangedCallback = null,
            object statusChangedCallbackUserState = null
            )
        {
            // check input parameters

            if (syncboxId == 0)
            {
                status = CLSyncboxCreationStatus.ErrorSyncboxIdZero;
                throw new ArgumentException("syncboxId must not be null.");
            }
            if (String.IsNullOrWhiteSpace(path))
            {
                status = CLSyncboxCreationStatus.ErrorPathNotSpecified;
                throw new ArgumentException("path must be specified.");
            }
            if (credentials == null)
            {
                status = CLSyncboxCreationStatus.ErrorNullCredentials;
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
                this._credentialsHolder.Value = credentials;
                this._syncboxId = syncboxId;
                this._path = path;
                this._statusChangedCallback = statusChangedCallback;
                this._statusChangedCallbackUserState = statusChangedCallbackUserState;

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
                    status = CLSyncboxCreationStatus.ErrorCreatingRestClient;
                    throw new AggregateException("Error creating REST HTTP client", createRestClientError.GrabExceptions());
                }
                if (_httpRestClient == null)
                {
                    const string nullRestClient = "Unknown error creating HTTP REST client";
                    _trace.writeToLog(1, "CLSyncbox: Construction: ERROR: Msg: {0}.", nullRestClient);
                    status = CLSyncboxCreationStatus.ErrorCreatingRestClient;
                    throw new NullReferenceException(nullRestClient);
                }

                // Create the sync engine
                _syncEngine = new CLSyncEngine();
            }
        }

        #endregion  // end Private Constructor

        #region Public Factory

        /// <summary>
        /// Creates an object which represents a Syncbox in Cloud, and associates the syncbox with a folder on the local disk.
        /// </summary>
        /// <param name="credentials">Credentials to authenticate communication</param>
        /// <param name="syncboxId">Unique ID of the Syncbox generated by Cloud</param>
        /// <param name="path">The full path of the folder on the local disk to associate with this syncbox.</param>
        /// <param name="syncbox">(output) Created local object representation of Syncbox</param>
        /// <param name="status">(output) Status of creation, should be checked for success</param>
        /// <param name="settings">(optional) Settings to allow use of the Syncbox in CLSyncEngine for active syncing and/or to allow tracing or other options</param>
        /// <param name="getNewCredentialsCallback">(optional) A delegate that will be called to provide new credentials when the current credentials token expires.</param>
        /// <param name="getNewCredentialsCallbackUserState">(optional) The user state that will be passed back to the getNewCredentialsCallback delegate.</param>
        /// <param name="statusChangedCallback">(optional) A delegate that will be called to provide an indication that status has changed in the syncbox.</param>
        /// <param name="statusChangedCallbackUserState">(optional) The user state that will be passed back to the statusChangedCallback delegate.</param>
        /// <returns>Returns any error which occurred during creation, if any, or null.</returns>
        public static CLError AllocAndInit(
            long syncboxId,
            CLCredentials credentials,
            string path,
            out CLSyncbox syncbox,
            out CLSyncboxCreationStatus status,
            ICLSyncSettings settings = null,
            Helpers.ReplaceExpiredCredentials getNewCredentialsCallback = null,
            object getNewCredentialsCallbackUserState = null,
			System.Threading.WaitCallback statusChangedCallback = null,
			object statusChangedCallbackUserState = null)
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
                    getNewCredentialsCallbackUserState: getNewCredentialsCallbackUserState,
                    statusChangedCallback: statusChangedCallback,
                    statusChangedCallbackUserState: statusChangedCallbackUserState
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
        public CLError BeginSync(CLSyncMode mode)
        {
            CLError toReturn = null;

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

                    _syncMode = mode;

                    // Start the sync engine
                    CLSyncStartStatus startStatus;
                    toReturn = _syncEngine.Start(
                        Syncbox: this, // syncbox to sync (contains required settings)
                        Status: out startStatus, // The completion status of the Start() function
                        StatusUpdated: this._statusChangedCallback, // called when sync status is updated
                        StatusUpdatedUserState: this._statusChangedCallbackUserState); // the user state passed to the callback above

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
        /// Asynchronously starts creating a new Syncbox in the cloud for the current application
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes.  Can be null.</param>
        /// <param name="callbackUserState">Userstate to pass to the callback when it is fired.  Can be null.</param>
        /// <param name="planId">The ID of the plan to use with this Syncbox.  If null, the default plan will be used.</param>
        /// <param name="name">Name of the Syncbox.  If null, a default name will be created.</param>
        /// <param name="credentials">The credentials to use with this request.</param>
        /// <param name="settings">(optional) settings to use with this method.</param>
        //
        //// The following metadata parameter was temporarily removed until the server checks for it for this call
        //
        ///// <param name="metadata">(optional) string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public static IAsyncResult BeginCreate(
            AsyncCallback callback,
            object callbackUserState,
            Nullable<long> planId,
            string name,
            CLCredentials credentials,
            ICLCredentialsSettings settings = null
            /*,  \/ last parameter temporarily removed since the server is not checking for it for this call; add back wherever commented out within this method when it works
            JsonContracts.MetadataDictionary metadata = null*/)
        {
            // create the asynchronous result to return
            GenericAsyncResult<CreateSyncboxResult> toReturn = new GenericAsyncResult<CreateSyncboxResult>(
                callback,
                callbackUserState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<CreateSyncboxResult>, Nullable<long>, string, CLCredentials, ICLCredentialsSettings/*, JsonContracts.MetadataDictionary*/> asyncParams =
                new Tuple<GenericAsyncResult<CreateSyncboxResult>, Nullable<long>, string, CLCredentials, ICLCredentialsSettings/*, JsonContracts.MetadataDictionary*/>(
                    toReturn,
                    planId,
                    name,
                    credentials,
                    settings/*,
                    metadata*/);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<CreateSyncboxResult>, Nullable<long>, string, CLCredentials, ICLCredentialsSettings/*, JsonContracts.MetadataDictionary*/> castState =
                    state as Tuple<GenericAsyncResult<CreateSyncboxResult>, Nullable<long>, string, CLCredentials, ICLCredentialsSettings/*, JsonContracts.MetadataDictionary*/>;
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
                        JsonContracts.SyncboxHolder response;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = Create(
                            castState.Item2,  // planId
                            castState.Item3,  // name
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
        /// Finishes creating a Syncbox in the cloud for the current application if it has not already finished via its asynchronous result and outputs the result,
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
        /// <param name="planId">the ID of the plan to use with this Syncbox.  Specify null for the default name.</param>
        /// <param name="name">the name of the Syncbox.  Specify null for the default name.</param>
        /// <param name="credentials">The credentials to use for this request.</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="settings">(optional) the settings to use with this method</param>
        //
        //// The following metadata parameter was temporarily removed until the server checks for it for this call
        //
        ///// <param name="metadata">(optional) string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public static CLError Create(
                    Nullable<long> planId,
                    string name,
                    CLCredentials credentials,
                    out CLHttpRestStatus status,
                    out JsonContracts.SyncboxHolder response,
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
                JsonContracts.SyncboxHolder inputBox = (/*metadata == null
                        && */string.IsNullOrEmpty(name)
                        && planId == null
                    ? null
                    : new JsonContracts.SyncboxHolder()
                    {
                        Syncbox = new JsonContracts.Syncbox()
                        {
                            FriendlyName = (string.IsNullOrEmpty(name)
                                ? null
                                : name),
                            PlanId = planId/*,
                            Metadata = metadata*/
                        }
                    });

                response = Helpers.ProcessHttp<JsonContracts.SyncboxHolder>(
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

            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.SyncboxHolder>();
                return ex;
            }
            return null;
        }
        #endregion

        #region Delete (delete a syncbox in the cloud)

        /// <summary>
        /// Asynchronously starts deleting a new Syncbox in the cloud for the current application
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes.  Can be null.</param>
        /// <param name="callbackUserState">Userstate to pass to the callback when it is fired.  Can be null.</param>
        /// <param name="syncboxId">The ID of syncbox to delete.</param>
        /// <param name="credentials">The credentials to use with this request.</param>
        /// <param name="settings">(optional) settings to use with this method.</param>
        //
        //// The following metadata parameter was temporarily removed until the server checks for it for this call
        //
        ///// <param name="metadata">(optional) string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public static IAsyncResult BeginDelete(
            AsyncCallback callback,
            object callbackUserState,
            Nullable<long> syncboxId,
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
            Tuple<GenericAsyncResult<DeleteSyncboxResult>, Nullable<long>, CLCredentials, ICLCredentialsSettings/*, JsonContracts.MetadataDictionary*/> asyncParams =
                new Tuple<GenericAsyncResult<DeleteSyncboxResult>, Nullable<long>, CLCredentials, ICLCredentialsSettings/*, JsonContracts.MetadataDictionary*/>(
                    toReturn,
                    syncboxId,
                    credentials,
                    settings/*,
                    metadata*/);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<DeleteSyncboxResult>, Nullable<long>, CLCredentials, ICLCredentialsSettings/*, JsonContracts.MetadataDictionary*/> castState =
                    state as Tuple<GenericAsyncResult<DeleteSyncboxResult>, Nullable<long>, CLCredentials, ICLCredentialsSettings/*, JsonContracts.MetadataDictionary*/>;
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
                        JsonContracts.SyncboxHolder response;
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
        /// Finishes deleting a Syncbox in the cloud for the current application if it has not already finished via its asynchronous result and outputs the result,
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
                    Nullable<long> syncboxId,
                    CLCredentials credentials,
                    out CLHttpRestStatus status,
                    out JsonContracts.SyncboxHolder response,
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
                JsonContracts.SyncboxIdOnly inputBox = (/*metadata == null
                        && */syncboxId == null
                    ? null
                    : new JsonContracts.SyncboxIdOnly()
                    {
                        Id = syncboxId
                    });

                response = Helpers.ProcessHttp<JsonContracts.SyncboxHolder>(
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
                response = Helpers.DefaultForType<JsonContracts.SyncboxHolder>();
                return ex;
            }
            return null;
        }
        #endregion

        #region List (list syncboxes in the cloud)

        /// <summary>
        /// Asynchronously starts listing syncboxes in the cloud for the current application
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes.  Can be null.</param>
        /// <param name="callbackUserState">Userstate to pass to the callback when it is fired.  Can be null.</param>
        /// <param name="credentials">The credentials to use with this request.</param>
        /// <param name="settings">(optional) settings to use with this method.</param>
        //
        //// The following metadata parameter was temporarily removed until the server checks for it for this call
        //
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
        /// Finishes listing syncboxes in the cloud for the current application if it has not already finished via its asynchronous result and outputs the result,
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

        #region GetSyncboxUsage
        /// <summary>
        /// Asynchronously starts getting sync box usage
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetSyncboxUsage(AsyncCallback aCallback, object aState, int timeoutMilliseconds)
        {
            return _httpRestClient.BeginGetSyncboxUsage(aCallback, aState, timeoutMilliseconds);
        }

        /// <summary>
        /// Finishes getting sync box usage if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting getting sync box usage</param>
        /// <param name="result">(output) The result from getting sync box usage</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetSyncboxUsage(IAsyncResult aResult, out GetSyncboxUsageResult result)
        {
            return _httpRestClient.EndGetSyncboxUsage(aResult, out result);
        }

        /// <summary>
        /// Queries the server for sync box usage
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetSyncboxUsage(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SyncboxUsage response)
        {
            return _httpRestClient.GetSyncboxUsage(timeoutMilliseconds, out status, out response);
        }
        #endregion

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
        public CLError UpdateSyncboxExtendedMetadata<T>(IDictionary<string, T> metadata, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SyncboxHolder response)
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
        public CLError SyncboxUpdateExtendedMetadata(JsonContracts.MetadataDictionary metadata, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SyncboxHolder response)
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

        #region UpdateSyncboxPlan
        /// <summary>
        /// Asynchronously updates the plan on a sync box
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="planId">The ID of the plan to set</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginUpdateSyncboxPlan(AsyncCallback aCallback,
            object aState,
            long planId,
            int timeoutMilliseconds)
        {
            return _httpRestClient.BeginUpdateSyncboxPlan(aCallback, aState, planId, timeoutMilliseconds, ReservedForActiveSync);
        }

        /// <summary>
        /// Finishes updating the storage plan on a sync box if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting updating the plan</param>
        /// <param name="result">(output) The result from updating the plan</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndUpdateSyncboxPlan(IAsyncResult aResult, out SyncboxUpdatePlanResult result)
        {
            return _httpRestClient.EndUpdateSyncboxPlan(aResult, out result);
        }

        /// <summary>
        /// Updates the plan on a sync box
        /// </summary>
        /// <param name="planId">The ID of the plan to set</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError UpdateSyncboxPlan(long planId, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SyncboxUpdatePlanResponse response)
        {
            return _httpRestClient.UpdateSyncboxPlan(planId, timeoutMilliseconds, out status, out response, ReservedForActiveSync);
        }
        #endregion

        #region UpdateSyncbox
        /// <summary>
        /// Asynchronously updates the sync box properties.
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="friendlyName">The friendly name of the syncbox to set</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginUpdateSyncbox(AsyncCallback aCallback,
            object aState,
            string friendlyName,
            int timeoutMilliseconds)
        {
            return _httpRestClient.BeginUpdateSyncbox(aCallback, aState, friendlyName, timeoutMilliseconds, ReservedForActiveSync);
        }

        /// <summary>
        /// Finishes updating the properties of a sync box if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting updating the syncbox properties</param>
        /// <param name="result">(output) The result from updating the properties of the syncbox</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndUpdateSyncbox(IAsyncResult aResult, out SyncboxUpdateResult result)
        {
            return _httpRestClient.EndUpdateSyncbox(aResult, out result);
        }

        /// <summary>
        /// Updates the properties of a sync box
        /// </summary>
        /// <param name="friendlyName">The friendly name of the syncbox to set</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError UpdateSyncbox(string friendlyName, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SyncboxHolder response)
        {
            return _httpRestClient.UpdateSyncbox(friendlyName, timeoutMilliseconds, out status, out response, ReservedForActiveSync);
        }
        #endregion

        #region DeleteSyncbox
        /// <summary>
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
        public CLError DeleteSyncbox(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SyncboxHolder response)
        {
            return _httpRestClient.DeleteSyncbox(timeoutMilliseconds, out status, out response, ReservedForActiveSync);
        }
        #endregion

        #region GetSyncboxStatus
        /// <summary>
        /// Asynchronously gets the status of this Syncbox
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetSyncboxStatus(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds)
        {
            return _httpRestClient.BeginGetSyncboxStatus(aCallback, aState, timeoutMilliseconds);
        }
        
        /// <summary>
        /// Finishes getting sync box status if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting getting sync box status</param>
        /// <param name="result">(output) The result from getting sync box status</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetSyncboxStatus(IAsyncResult aResult, out GetSyncboxStatusResult result)
        {
            return _httpRestClient.EndGetSyncboxStatus(aResult, out result);
        }

        /// <summary>
        /// Gets the status of this Syncbox
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetSyncboxStatus(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SyncboxHolder response)
        {
            return _httpRestClient.GetSyncboxStatus(timeoutMilliseconds, out status, out response);
        }
        #endregion

        #endregion  // end Public Instance HTTP REST Methods
    }

    #endregion  // end Public CLSyncbox Class
}