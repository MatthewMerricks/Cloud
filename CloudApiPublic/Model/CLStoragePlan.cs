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
using System.Collections.Generic;
using System.Threading;

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
        public Nullable<long> Tier { get; private set; }

        /// <summary>
        /// The client application ID associated with this storage plan.
        /// </summary>
        public long ClientApplicationId { get; private set; }

        /// <summary>
        /// The maximum bandwidth allowed for this plan.
        /// </summary>
        public Nullable<long> BandwidthQuota { get; private set; }

        /// <summary>
        /// The maximum storage allowed for this plan.
        /// </summary>
        public Nullable<long> StorageQuota { get; private set; }

        /// <summary>
        /// Indicates whether this is the default plan for this application.
        /// </summary>
        public bool IsDefaultPlan { get; private set; }

        /// <summary>
        /// UTC time when this storage plan was created in the cloud.
        /// </summary>
        public Nullable<DateTime> PlanCreatedAt { get; private set; }

        /// <summary>
        /// Last UTC time when this storage plan was updated in the cloud.
        /// </summary>
        public Nullable<DateTime> PlanUpdatedAt { get; private set; }

        #endregion  // end Public Properties

        #region Constructors

        /// <summary>
        /// The default constructor is not supported.  Use 
        /// </summary>
        public CLStoragePlan()
        {
            throw new NotSupportedException(Resources.CLNotificationManualPollingEngineDefaultConstructorNotSupported);
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
            Nullable<long> tier,
            long clientApplicationId,
            Nullable<long> bandwidthQuota,
            Nullable<long> storageQuota,
            bool isDefaultPlan,
            Nullable<DateTime> planCreatedAt,
            Nullable<DateTime> planUpdatedAt)
        {
            Helpers.CheckHalted();

            if (id == 0)
            {
                throw new NullReferenceException("id must not be zero");
            }
            if (clientApplicationId == 0)
            {
                throw new NullReferenceException("clientApplicationId must not be zero");
            }

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
            if (response.Id == null)
            {
                throw new NullReferenceException("response Id must not be null");
            }
            if (response.ClientApplicationId == null)
            {
                throw new NullReferenceException("response ClientApplicationId must not be null");
            }


            Id = (long)response.Id;
            Name = response.Name;
            Tier = response.Tier;
            ClientApplicationId = (long)response.ClientApplicationId;
            BandwidthQuota = response.BandwidthQuota;
            StorageQuota = response.StorageQuota;
            IsDefaultPlan = response.IsDefaultPlan ?? false;
            PlanCreatedAt = response.PlanCreatedAt;
            PlanUpdatedAt = response.PlanUpdatedAt;
        }

        #endregion  // end Constructors

        #region Public Factories
        #region List (lists the cloud storage plans for this application)

        /// <summary>
        /// Asynchronously starts listing the plans on the server for the current application
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="credentials">The credentials to use with this request.</param>
        /// <param name="settings">(optional) settings to use with this request</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public static IAsyncResult BeginListStoragePlansWithCredentials(
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
                    toReturn = new GenericAsyncResult<ListStoragePlansResult>(
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
                        CLStoragePlan[] response;
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError processError = ListStoragePlansWithCredentials(
                            Data.credentials,
                            out response,
                            Data.settings);
                         
                        Data.toReturn.Complete(
                            new ListStoragePlansResult(
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
        /// Finishes listing plans on the server for the current application if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting listing the plans</param>
        /// <param name="result">(output) The result from listing the plans</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public static CLError EndListStoragePlansWithCredentials(IAsyncResult aResult, out ListStoragePlansResult result)
        {
            Helpers.CheckHalted();

            return Helpers.EndAsyncOperation<ListStoragePlansResult>(aResult, out result);
        }

        /// <summary>
        /// Lists the plans on the server for the current application
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) An array of storage plans from the cloud.</param>
        /// <param name="credentials">The credentials to use with this request.</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        /// <remarks>The output response array may be null, empty, or may contain null items.</remarks>
        public static CLError ListStoragePlansWithCredentials(CLCredentials credentials, out CLStoragePlan[] response, ICLCredentialsSettings settings = null)
        {
            Helpers.CheckHalted();

            // try/catch to process the metadata query, on catch return the error
            try
            {
                // copy settings so they don't change while processing; this also defaults some values
                ICLSyncSettingsAdvanced copiedSettings = (settings == null
                    ? AdvancedSyncSettings.CreateDefaultSettings()
                    : settings.CopySettings());

                // check input parameters
                if (credentials == null)
                {
                    throw new ArgumentNullException("credentials must not be null");
                }

                if (!(copiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLMSTimeoutMustBeGreaterThanZero);
                }

                // Query the server and get the response.
                JsonContracts.StoragePlanListResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.StoragePlanListResponse>(
                    null, // no request body for listing plans
                    CLDefinitions.CLPlatformAuthServerURL,
                    CLDefinitions.MethodPathAuthListPlans,
                    Helpers.requestMethod.get,
                    copiedSettings.HttpTimeoutMilliseconds,
                    null, // not an upload nor download
                    Helpers.HttpStatusesOkAccepted,
                    copiedSettings,
                    credentials,
                    null, 
                    true);

                // Convert the server response to the output response.
                if (responseFromServer != null && responseFromServer.Plans != null)
                {
                    List<CLStoragePlan> listPlans = new List<CLStoragePlan>();
                    foreach (JsonContracts.StoragePlanResponse plan in responseFromServer.Plans)
                    {
                        if (plan != null)
                        {
                            listPlans.Add(new CLStoragePlan(plan));
                        }
                        else
                        {
                            listPlans.Add(null);
                        }
                    }
                    response = listPlans.ToArray();
                }
                else
                {
                    throw new NullReferenceException(Resources.ExceptionCLHttpRestWithoutPlans);
                }
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<CLStoragePlan []>();
                return ex;
            }
            return null;
        }

        #endregion  // end (lists the cloud storage plans for this application)

        #endregion // end Public Factories
    }
}