using CloudApiPublic;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
using CloudApiPublic.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TestHttpRest.Settings;

namespace TestHttpRest.Tests
{
    public static class TestHttpCalls
    {
        private static CLTrace _trace = CLTrace.Instance;
        public static void Run(InputParams paramSet, GenericHolder<CLError> ProcessingErrorHolder)
        {
            ManualResetEvent _mreTest = new ManualResetEvent(false);
            CLCredential _credentialTest = null;
            CLSyncBox _syncboxTest = null;
            long _planIdTest = 0;
            CLSyncBox syncBox = null;

            if (!String.IsNullOrWhiteSpace(paramSet.ActiveSync_TraceFolder))
            {
                CLTrace.Initialize(paramSet.ActiveSync_TraceFolder, "HttpTest", "log", paramSet.TraceLevel, true, willForceReset: true);
            }

            if (paramSet.ActiveSyncBoxID == null)
            {
                _trace.writeToLog(1, "TestHttpCalls: Run: ERROR: Throw: SyncBoxId cannot be null.");
                throw new MissingFieldException("SyncBoxId cannot be null");
            }
            else
            {
                // create credential
                CLCredential syncCredential;
                CLCredentialCreationStatus syncCredentialStatus;
                CLError errorCreateSyncCredential = CLCredential.CreateAndInitialize(
                    paramSet.API_Key,
                    paramSet.API_Secret,
                    out syncCredential,
                    out syncCredentialStatus,
                    paramSet.Token);

                if (errorCreateSyncCredential != null)
                {
                    _trace.writeToLog(1, "TestHttpCalls: Run: ERROR: From CLCredential.CreateAndInitialize: Msg: <{0}>.", errorCreateSyncCredential.errorDescription);
                }
                if (syncCredentialStatus != CLCredentialCreationStatus.Success)
                {
                    string msg = "syncCredentialStatus: " + syncCredentialStatus.ToString() + ":" + Environment.NewLine + errorCreateSyncCredential.errorDescription;
                    _trace.writeToLog(1, "TestHttpCalls: Run: ERROR: Throw: Msg: {0}.", msg);
                    throw new MissingFieldException(msg);
                }
                else
                {
                    //&&&&&&&&&&&&&& DEBUG CODE.  REMOVE &&&&&&&&&&&&&&&&&&&
                    // Synchronous list plans
                    CLHttpRestStatus status;
                    CloudApiPublic.JsonContracts.ListPlansResponse responseListPlans;
                    CLError error = syncCredential.ListPlans(5000, out status, out responseListPlans, SettingsAdvancedImpl.Instance);

                    // Synchronous list sessions
                    CloudApiPublic.JsonContracts.ListSessionsResponse responseListSessions;
                    error = syncCredential.ListSessions(5000, out status, out responseListSessions, SettingsAdvancedImpl.Instance);

                    // Synchronous delete session
                    if (error == null && responseListSessions.Sessions.Length > 0)
                    {
                        CloudApiPublic.JsonContracts.SessionDeleteResponse responseDeleteSession;
                        error = syncCredential.DeleteSession(5000, out status, out responseDeleteSession,
                                responseListSessions.Sessions[responseListSessions.Sessions.Length - 1].Key, SettingsAdvancedImpl.Instance);
                    }

                    // Synchronous show session
                    CloudApiPublic.JsonContracts.SessionShowResponse responseShowSession;
                    error = syncCredential.ShowSession(5000, out status, out responseShowSession,
                            "e440c16b86e762d16996c849673bbe0b250ce3f165598e1142c84e7031938cdc", SettingsAdvancedImpl.Instance);

                    // Synchronous /1/auth/session/create tests.
                    // Create a new session associating it with 3 syncbox IDs.  Set the token duration to 60 hours.
                    CloudApiPublic.JsonContracts.SessionCreateResponse responseSessionCreate;
                    HashSet<long> syncBoxIds = new HashSet<long>() { 4, 38, 50 };
                    CLError errorSessionCreate = syncCredential.CreateSession(5000, out status, out responseSessionCreate,
                            syncBoxIds, 60 * 60, SettingsAdvancedImpl.Instance);

                    // Create a new session associating it with all syncboxes in the application.  Set the token duration to 70 hours.
                    CloudApiPublic.JsonContracts.SessionCreateResponse responseSessionCreate2;
                    CLError errorSessionCreate2 = syncCredential.CreateSession(5000, out status, out responseSessionCreate2,
                            null, 70 * 60, SettingsAdvancedImpl.Instance);

                    // Create a new session associating it with a single syncbox.  Let the token duration default to 120 hours.
                    HashSet<long> syncBoxIds2 = new HashSet<long>() { 4 };
                    CloudApiPublic.JsonContracts.SessionCreateResponse responseSessionCreate3;
                    CLError errorSessionCreate3 = syncCredential.CreateSession(5000, out status, out responseSessionCreate3,
                            syncBoxIds2, null, SettingsAdvancedImpl.Instance);

                    // Remember the current credential for the aynchronous tests.
                    _credentialTest = syncCredential;

                    // Asynchronous ShowSession test.
                    _mreTest.Reset();
                    IAsyncResult aResult = syncCredential.BeginShowSession(MyShowSessionCallback, this, 5000,
                                        "e440c16b86e762d16996c849673bbe0b250ce3f165598e1142c84e7031938cdc", SettingsAdvancedImpl.Instance);
                    _mreTest.WaitOne();

                    // Asynchronous ListSessions test.
                    _mreTest.Reset();
                    aResult = syncCredential.BeginListSessions(MyListSessionsCallback, this, 5000, SettingsAdvancedImpl.Instance);
                    _mreTest.WaitOne();

                    // Asynchronous DeleteSession test.
                    if (error == null && responseListSessions.Sessions.Length > 0)
                    {
                        _mreTest.Reset();
                        aResult = syncCredential.BeginDeleteSession(MyDeleteSessionCallback, this, 5000,
                                    responseListSessions.Sessions[responseListSessions.Sessions.Length - 1].Key, SettingsAdvancedImpl.Instance);
                        _mreTest.WaitOne();
                    }


                    // Asynchronous /1/auth/session/create tests.
                    // Create a new session associating it with 3 syncbox IDs.  Set the token duration to 60 hours.
                    HashSet<long> syncBoxIds3 = new HashSet<long>() { 4, 38, 50 };
                    _mreTest.Reset();
                    aResult = syncCredential.BeginCreateSession(MySessionCreateCallback, this, 5000,
                                syncBoxIds3, 60 * 60, SettingsAdvancedImpl.Instance);
                    _mreTest.WaitOne();

                    // Create a new session associating it with all syncboxes in the application.  Set the token duration to 70 hours.
                    _mreTest.Reset();
                    aResult = syncCredential.BeginCreateSession(MySessionCreateCallback, this, 5000,
                                null, 70 * 60, SettingsAdvancedImpl.Instance);
                    _mreTest.WaitOne();

                    // Create a new session associating it with a single syncbox.  Let the token duration default to 120 hours.
                    HashSet<long> syncBoxIds4 = new HashSet<long>() { 4 };
                    _mreTest.Reset();
                    aResult = syncCredential.BeginCreateSession(MySessionCreateCallback, this, 5000,
                                syncBoxIds4, null, SettingsAdvancedImpl.Instance);
                    _mreTest.WaitOne();

                    // Asyncrhonous list plans
                    _mreTest.Reset();
                    aResult = syncCredential.BeginListPlans(MyTestListPlansCallback, this, 5000, SettingsAdvancedImpl.Instance);
                    _mreTest.WaitOne();

                    CloudApiPublic.JsonContracts.SyncBoxHolder responseSyncBoxHolder;
                    error = syncCredential.AddSyncBoxOnServer(5000, out status, out responseSyncBoxHolder, null, responseListPlans.Plans[1].Id, SettingsAdvancedImpl.Instance);

                    _mreTest.Reset();
                    aResult = syncCredential.BeginAddSyncBoxOnServer(MyCreateSyncBoxCallback, this, 5000, "Brand new SyncBox", responseListPlans.Plans[2].Id, SettingsAdvancedImpl.Instance);
                    _mreTest.WaitOne();

                    _planIdTest = (long)responseListPlans.Plans[2].Id;




                    //&&&&&&&&&&&&&& DEBUG CODE.  REMOVE &&&&&&&&&&&&&&&&&&&

                    // create a SyncBox from an existing SyncBoxId
                    CLSyncBoxCreationStatus syncBoxStatus;
                    CLError errorCreateSyncBox = CLSyncBox.CreateAndInitialize(
                        syncCredential,
                        (long)SettingsAdvancedImpl.Instance.SyncBoxId,
                        out syncBox,
                        out syncBoxStatus,
                        SettingsAdvancedImpl.Instance);

                    if (errorCreateSyncBox != null)
                    {
                        _trace.writeToLog(1, "TestHttpCalls: Run: ERROR: From CLSyncBox.CreateAndInitialize: Msg: <{0}>.", errorCreateSyncBox.errorDescription);
                    }
                    if (syncBoxStatus != CLSyncBoxCreationStatus.Success)
                    {
                        if (NotifyException != null)
                        {
                            NotifyException(this, new NotificationEventArgs<CLError>()
                            {
                                Data = errorCreateSyncBox,
                                Message = "syncBoxStatus: " + syncBoxStatus.ToString() + ":" + Environment.NewLine +
                                    errorCreateSyncBox.errorDescription
                            });
                        }
                    }
                    else
                    {
                        //&&&&&&&&&&&&&&&&&&&&&& DEBUG ONLY.  REMOVE.

                        _syncboxTest = syncBox;

                        CLHttpRestStatus myStatus;

                        CloudApiPublic.JsonContracts.SyncBoxHolder syncBoxHolderResponse;
                        CLError errorSyncBoxUpdate = _syncboxTest.UpdateSyncBox("My New Friendly Name", 5000, out myStatus, out syncBoxHolderResponse);

                        _mreTest.Reset();
                        IAsyncResult arResult = _syncboxTest.BeginUpdateSyncBox(MySyncBoxUpdateCallback, this, "My New Friendly Name 2", 5000);
                        _mreTest.WaitOne();

                        CloudApiPublic.JsonContracts.SyncBoxUpdatePlanResponse myResponse;
                        CLError errorSyncBoxUpdatePlan = _syncboxTest.UpdateSyncBoxPlan(_planIdTest, 5000, out myStatus, out myResponse);

                        _mreTest.Reset();
                        arResult = _syncboxTest.BeginUpdateSyncBoxPlan(MySyncBoxUpdatePlanCallback, this, _planIdTest, 5000);
                        _mreTest.WaitOne();

                        //&&&&&&&&&&&&&&&&&&&&&& DEBUG ONLY.  REMOVE.
                    }
                }
            }
        }

    }
}
