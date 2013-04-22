//
// CLStoragePlan.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Interfaces;
using Cloud.REST;
using Cloud.Static;
using System;

namespace Cloud.Model
{
    /// <summary>
    /// Represents a Cloud storage plan."/>
    /// </summary>
    public sealed class CLStoragePlan
    {
        #region Public Properties

        /// <summary>
        /// The ID of this storage plan in the cloud.
        /// </summary>
        public long Id { get; private set; }

        /// <summary>
        /// The name of this storage plan.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The ID of the tier that this storage plan belongs to.
        /// </summary>
        public long Tier { get; private set; }

        /// <summary>
        /// The client application ID associated with this storage plan.
        /// </summary>
        public long ClientApplicationId { get; private set; }

        /// <summary>
        /// The maximum bandwidth allowed for this plan.
        /// </summary>
        public long BandwidthQuota { get; private set; }

        /// <summary>
        /// The maximum storage allowed for this plan.
        /// </summary>
        public long StorageQuota { get; private set; }

        /// <summary>
        /// Indicates whether this is the default plan for this application.
        /// </summary>
        public bool IsDefaultPlan { get; private set; }

        /// <summary>
        /// UTC time when this storage plan was created in the cloud.
        /// </summary>
        public DateTime PlanCreatedAt { get; private set; }

        /// <summary>
        /// Last UTC time when this storage plan was updated in the cloud.
        /// </summary>
        public DateTime PlanUpdatedAt { get; private set; }

        #endregion  // end Public Properties

        #region Constructors

        /// <summary>
        /// The default constructor is not supported.  Use 
        /// </summary>
        public CLStoragePlan()
        {
            throw new NotSupportedException("Default constructor not supported");
        }

        /// <summary>
        /// Private constructor to set all of the fields. Use CLStoragePlan.List or CLStoragePlan.Default to create a CLStoragePlan object.
        /// </summary>
        /// <param name="id">ID of this storage plan in the cloud.</param>
        /// <param name="name">Name of this storage plan.</param>
        /// <param name="tier">ID of the tier associated with this storage plan.</param>
        /// <param name="clientApplicationId">ID of the application associated with this storage plan.</param>
        /// <param name="bandwidthQuota">Maximum bandwidth allowed for this storage plan.</param>
        /// <param name="storageQuota">Maximum storage allowed for this storage plan.</param>
        /// <param name="isDefaultPlan">true: This is the default storage plan for this application.</param>
        /// <param name="planCreatedAt">UTC time created.</param>
        /// <param name="planUpdatedAt">UTC time last updated.</param>
        private CLStoragePlan(
            long id,
            string name,
            long tier,
            long clientApplicationId,
            long bandwidthQuota,
            long storageQuota,
            bool isDefaultPlan,
            DateTime planCreatedAt,
            DateTime planUpdatedAt)
        {
            Id = id;
            Name = name;
            Tier = tier;
            ClientApplicationId = clientApplicationId;
            BandwidthQuota = bandwidthQuota;
            StorageQuota = storageQuota;
            IsDefaultPlan = isDefaultPlan;
            PlanCreatedAt = planCreatedAt;
            PlanUpdatedAt = planUpdatedAt;
        }

        /// <summary>
        /// Internal constructor to create a storage plan from a JsonContracts.CLStoragePlanResponse.
        /// </summary>
        /// <param name="response">The HTTP REST response to use to create the object.</param>
        internal CLStoragePlan(JsonContracts.StoragePlanResponse response)
        {
            Id = response.Id ?? -1;
            Name = response.Name;
            Tier = response.Tier ?? -1;
            ClientApplicationId = response.ClientApplicationId ?? -1;
            BandwidthQuota = response.BandwidthQuota ?? -1; ;
            StorageQuota = response.StorageQuota ?? -1;
            IsDefaultPlan = response.IsDefaultPlan ?? false;
            PlanCreatedAt = response.PlanCreatedAt ?? DateTime.MinValue;
            PlanUpdatedAt = response.PlanUpdatedAt ?? DateTime.MinValue;
        }

        #endregion  // end Constructors

        #region Public Factories
        #region List (lists the cloud storage plans for this application)

        /// <summary>
        /// Asynchronously starts listing the plans on the server for the current application
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="settings">(optional) settings to use with this request</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public IAsyncResult BeginList(AsyncCallback callback, object callbackUserState, ICLCredentialsSettings settings = null)
        {
            // create the asynchronous result to return
            GenericAsyncResult<ListStoragePlansResult> toReturn = new GenericAsyncResult<ListStoragePlansResult>(
                callback,
                callbackUserState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<ListStoragePlansResult>, int, ICLCredentialsSettings> asyncParams =
                new Tuple<GenericAsyncResult<ListStoragePlansResult>, int, ICLCredentialsSettings>(
                    toReturn,
                    timeoutMilliseconds,
                    settings);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<ListStoragePlansResult>, int, ICLCredentialsSettings> castState = state as Tuple<GenericAsyncResult<ListStoragePlansResult>, int, ICLCredentialsSettings>;
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
                        JsonContracts.ListStoragePlansResponse result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = ListPlans(
                            castState.Item2,
                            out status,
                            out result,
                            castState.Item3);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new ListStoragePlansResult(
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
        /// Finishes listing plans on the server for the current application if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting listing the plans</param>
        /// <param name="result">(output) The result from listing the plans</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndList(IAsyncResult aResult, out ListStoragePlansResult result)
        {
            // declare the specific type of asynchronous result for plan listing
            GenericAsyncResult<ListStoragePlansResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for plan listing and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for listing plans
                castAResult = aResult as GenericAsyncResult<ListStoragePlansResult>;

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
                result = Helpers.DefaultForType<ListPlansResult>();
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
        /// Lists the plans on the server for the current application
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError List(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.ListStoragePlansResponse response, ICLCredentialsSettings settings = null)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the metadata query, on catch return the error
            try
            {
                // copy settings so they don't change while processing; this also defaults some values
                ICLSyncSettingsAdvanced copiedSettings = (settings == null
                    ? NullSyncRoot.Instance.CopySettings()
                    : settings.CopySettings());

                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                response = Helpers.ProcessHttp<JsonContracts.ListPlansResponse>(
                    null, // no request body for listing plans
                    CLDefinitions.CLPlatformAuthServerURL,
                    CLDefinitions.MethodPathAuthListPlans,
                    Helpers.requestMethod.get,
                    timeoutMilliseconds,
                    null, // not an upload nor download
                    Helpers.HttpStatusesOkAccepted,
                    ref status,
                    copiedSettings,
                    this,
                    null);
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.ListPlansResponse>();
                return ex;
            }
            return null;
        }

        #endregion  // end (lists the cloud storage plans for this application)

        #endregion // end Public Factories
    }
}