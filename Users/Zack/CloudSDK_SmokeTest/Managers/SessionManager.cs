using CloudApiPublic;
using CloudApiPublic.Interfaces;
using CloudApiPublic.JsonContracts;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
using CloudSDK_SmokeTest.Events.CLEventArgs;
using CloudSDK_SmokeTest.Events.ManagerEventArgs;
using CloudSDK_SmokeTest.Helpers;
using CloudSDK_SmokeTest.Interfaces;
using CloudSDK_SmokeTest.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Managers
{
    public class SessionManager : ISmokeTaskManager 
    {
        public static CloudApiPublic.JsonContracts.Session ReturnSessionAtIndex(ItemListHelperEventArgs eventArgs, int index)
        {
            CloudApiPublic.JsonContracts.Session returnValue = null;
            CLCredential creds;
            CLCredentialCreationStatus credsStatus;
            GenericHolder<CLError> refHolder = eventArgs.ProcessingErrorHolder;
            CLError credsError = CLCredential.CreateAndInitialize(eventArgs.ParamSet.API_Key, eventArgs.ParamSet.API_Secret, out creds, out credsStatus);
            if (credsError != null || credsStatus != CLCredentialCreationStatus.Success)
            {
                ExceptionManagerEventArgs exArgs = new ExceptionManagerEventArgs()
                {
                    CredsCreateStatus = credsStatus,
                    OpperationName = "SessionManager.RestunSessionAtIndex ",
                    Error = credsError,
                    ProcessingErrorHolder = eventArgs.ProcessingErrorHolder,
                };
                SmokeTaskManager.HandleFailure(exArgs);
            }

            eventArgs.Creds = creds;

            int responseCode = ItemsListHelper.RunListSessions(eventArgs, false, false);

            ItemsListManager mgr = ItemsListManager.GetInstance();
            if (mgr.Sessions.Count() >= index)
                returnValue = mgr.Sessions[index];
            else
                returnValue = mgr.Sessions.FirstOrDefault();

            if (returnValue == null)
            {
                lock (refHolder)
                {
                    Exception ex = ExceptionManager.ReturnException("Return Session At Index", "Session Could Not Be Selected, and May not exist.");
                    refHolder.Value = refHolder.Value + ex;
                }
            }

            return returnValue;
           
        }

        #region Interface Implementation

        #region Create 
        public int Create(SmokeTestManagerEventArgs e)
        {
            Creation createSessionTask = e.CurrentTask as Creation;
            if (createSessionTask == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;

            int initialCount;
            int response = ItemsListHelper.GetSessionCount(e, out initialCount);
            int responseCode = 0;
            StringBuilder newBuilder = new StringBuilder();
            GenericHolder<CLError> refHolder = e.ProcessingErrorHolder;
            string newSessionKey = string.Empty;
            newBuilder.AppendLine("Preparing to Create Session.");
            ItemsListManager mgr = ItemsListManager.GetInstance();
            int prior = mgr.Sessions.Count;
            int iterations = 1;
            if (createSessionTask.Count > 1)
                iterations = createSessionTask.Count;
            for (int x = 0; x < iterations; x++)
            {
                if (responseCode == 0)
                {
                    responseCode = CreateSession(e, out newSessionKey);
                    newBuilder.AppendLine("Create Sesssion Results:");
                    if (string.IsNullOrEmpty(newSessionKey))
                        newBuilder.AppendLine("Create Sesssion Failed: Returned Session ID is 0:");
                    else
                        newBuilder.AppendLine(string.Format("Created Session with Key {0}", newSessionKey));
                }
            }
            int currentCount;
            response = ItemsListHelper.GetSessionCount(e, out currentCount);
            int expectedCount = initialCount + iterations;
            StringBuilder explanation = new StringBuilder();
            SmokeTaskManager.BuildResults(out explanation, "Session", initialCount, expectedCount, currentCount);
            if (currentCount == expectedCount)
                explanation.AppendLine("Successfully Completed Create Session Task");
            else
            {
                explanation.AppendLine("Create Sync Box Task was Expecting a different result.");
                responseCode = (int)FileManagerResponseCodes.ExpectedItemMatchFailure;
            }
            newBuilder.AppendLine(explanation.ToString());
            e.StringBuilderList.Add(new StringBuilder(newBuilder.ToString()));
            return responseCode;
        }

        #region Private 
        private int CreateSession(SmokeTestManagerEventArgs e,  out string sessionKey)
        {
            int createSessionResponseCode = 0;
            sessionKey = string.Empty;
            ICLCredentialSettings settings;
            CLError initCredsError = new CLError();
            TaskEventArgs args = new TaskEventArgs()
            {
                ParamSet = e.ParamSet,
                ProcessingErrorHolder = e.ProcessingErrorHolder,
            };
            bool success = CredentialHelper.InitializeCreds(ref args, out settings, out initCredsError);
            if (!success)
                return (int)FileManagerResponseCodes.InitializeCredsError;

            CLHttpRestStatus restStatus;
            CloudApiPublic.JsonContracts.SessionCreateResponse sessionCreateResponse;
            CLError createSessionError = args.Creds.CreateSession(ManagerConstants.TimeOutMilliseconds, out restStatus, out sessionCreateResponse);
            if (createSessionError != null || restStatus != CLHttpRestStatus.Success)
            {
                ExceptionManagerEventArgs exArgs = new ExceptionManagerEventArgs()
                {
                    RestStatus = restStatus,
                    OpperationName = "Credential.Create Session ",
                    Error = createSessionError,
                    ProcessingErrorHolder = e.ProcessingErrorHolder,
                };
                SmokeTaskManager.HandleFailure(exArgs);
                return (int)FileManagerResponseCodes.UnknownError;
            }
            sessionKey = sessionCreateResponse.Session.Key;
            ItemsListManager mgr = ItemsListManager.GetInstance();
            if (!mgr.Sessions.Contains(sessionCreateResponse.Session))
                mgr.Sessions.Add(sessionCreateResponse.Session);
            if (!mgr.SessionsCreatedDynamically.Contains(sessionCreateResponse.Session.Token))
                mgr.SessionsCreatedDynamically.Add(sessionCreateResponse.Session.Token);

            return createSessionResponseCode;
        }
        #endregion 
        #endregion Create

        #region Rename
        public int Rename(SmokeTestManagerEventArgs e)
        {
            GenericHolder<CLError> refHolder = e.ProcessingErrorHolder;
            Exception ex =  new NotImplementedException("Can Not Rename a Session");
            AddException(ex, ref refHolder);
            return (int)FileManagerResponseCodes.InvalidTaskType;
        }
        #endregion Rename

        #region Delete
        public int Delete(SmokeTestManagerEventArgs e)
        {
            
            int deleteSessionResponseCode = 0;
            Deletion deleteTask = e.CurrentTask as Deletion;
            if (deleteTask == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;

            
            List<CloudApiPublic.JsonContracts.Session> toDelete = new List<CloudApiPublic.JsonContracts.Session>();
            GenericHolder<CLError> refHolder = e.ProcessingErrorHolder;
            
            deleteSessionResponseCode = BeginDelete(e);
            
            return deleteSessionResponseCode;
        }

        #region Private
        private int BeginDelete(SmokeTestManagerEventArgs e)
        {

            StringBuilder newBuilder = new StringBuilder();
            newBuilder.AppendLine("Entering Delete Session Task ...");
            int deleteSessionResponseCode = 0;
            ICLCredentialSettings settings;
            CLError initCredsError = new CLError();
            TaskEventArgs args = new TaskEventArgs()
            {
                ParamSet = e.ParamSet,
                ProcessingErrorHolder = e.ProcessingErrorHolder,
            };

            bool success = CredentialHelper.InitializeCreds(ref args, out settings, out initCredsError);
            if (!success)
                return (int)FileManagerResponseCodes.InitializeCredsError;

            int initialCount;
            int response = ItemsListHelper.GetSessionCount(e, out initialCount);

            DeleteSessionEventArgs deleteEventArgs = new DeleteSessionEventArgs()
            {
                Creds = args.Creds,
                ParamSet = e.ParamSet,
                ProcessingErrorHolder = e.ProcessingErrorHolder,
            };

            ListSessionsResponse sessionsList = GetSessionList(e);
            if (sessionsList == null)
                return (int)FileManagerResponseCodes.UnknownError;

            List<Session> toDelete = AddSessionsToDeletionList(e.CurrentTask as Deletion, sessionsList);
            ItemsListManager mgr = ItemsListManager.GetInstance();
            foreach (Session session in toDelete)
            {
                if (deleteSessionResponseCode == 0)
                {
                    deleteEventArgs.Session = session;
                    deleteSessionResponseCode = ExecuteDelete(deleteEventArgs, ref newBuilder);
                    if (deleteSessionResponseCode == 0)
                        mgr.RemoveSession(session);
                }
                else
                    break;
            }

            int currentCount;
            response = ItemsListHelper.GetSessionCount(e, out currentCount);
            int expectedCount = initialCount - toDelete.Count;
            if (expectedCount < 0)
                expectedCount = 0;

            StringBuilder explanation = new StringBuilder(string.Format("Session Count Before Delete {0}.", initialCount));
            explanation.AppendLine("Results:");
            explanation.AppendLine(string.Format("Expected Count: {0}", expectedCount.ToString()));
            explanation.AppendLine(string.Format("Actual Count  : {0}", currentCount.ToString()));
            if (currentCount == expectedCount)
                explanation.AppendLine("Successfully Completed Delete Session Task");
            else
            {
                explanation.AppendLine("Delete Session Task was Expecting a different result.");
                deleteSessionResponseCode = (int)FileManagerResponseCodes.ExpectedItemMatchFailure;
            }
            newBuilder.AppendLine(explanation.ToString());
            newBuilder.AppendLine("Exiting Deletion Session Task...");
            e.StringBuilderList.Add(new StringBuilder(newBuilder.ToString()));
            return deleteSessionResponseCode;
        }

        private int ExecuteDelete(DeleteSessionEventArgs deleteEventArgs, ref StringBuilder builder)
        {
            int deleteSessionResponse = 0;

            GenericHolder<CLError> refHolder = deleteEventArgs.ProcessingErrorHolder;
            CLHttpRestStatus restStatus = CLHttpRestStatus.BadRequest;
            CloudApiPublic.JsonContracts.SessionDeleteResponse sessionDeleteResponse;
            CLError deleteSessionError = null;
            if (deleteEventArgs.Session != null && !string.IsNullOrEmpty(deleteEventArgs.Session.Key))
            {
                builder.AppendLine(string.Format("Entering Delete  Individual Session with Key: {0}", deleteEventArgs.Session.Key));
                deleteSessionError = deleteEventArgs.Creds.DeleteSession(ManagerConstants.TimeOutMilliseconds, out restStatus, out sessionDeleteResponse, deleteEventArgs.Session.Key);
                
            }
            if (deleteSessionError != null || restStatus != CLHttpRestStatus.Success)
            {
                ExceptionManagerEventArgs exArgs = new ExceptionManagerEventArgs()
                {
                    RestStatus = restStatus,
                    OpperationName = "Credential.Delete Session ",
                    Error = deleteSessionError,
                    ProcessingErrorHolder = deleteEventArgs.ProcessingErrorHolder,
                };
                SmokeTaskManager.HandleFailure(exArgs);
                return (int)FileManagerResponseCodes.UnknownError;
            }
            builder.AppendLine("Exiting Delete Individual Session ");
            return deleteSessionResponse;
        }

        private List<Session> AddSessionsToDeletionList(Deletion deleteTask, ListSessionsResponse sessionList)
        {
            List<Session> toDelete = new List<Session>();
            if (deleteTask.DeleteAllSpecified && deleteTask.DeleteAll)
            {
                foreach (CloudApiPublic.JsonContracts.Session session in sessionList.Sessions)
                    toDelete.Add(session);
            }
            else if (deleteTask.DeleteCountSpecified && deleteTask.DeleteCount > 0)
            {
                IEnumerable<Session> sessions = sessionList.Sessions.Take(deleteTask.DeleteCount);
                foreach (Session session in sessions)
                    toDelete.Add(session);
            }
            else
            {
                if (deleteTask.IDSpecified && deleteTask.ID > 0)
                    toDelete.Add(sessionList.Sessions.Where(s => s.Key == deleteTask.ID.ToString()).FirstOrDefault());
                else
                {
                    Session session = sessionList.Sessions.FirstOrDefault();
                    toDelete.Add(session);
                }
            }
            return toDelete;
        }
        #endregion 
        #endregion Delete        

        #region Undelete
        public int UnDelete(SmokeTestManagerEventArgs e)
        {
            GenericHolder<CLError> refHolder = e.ProcessingErrorHolder;
            Exception ex = new NotImplementedException("Can Not Undelete a Session");
            AddException(ex, ref refHolder);
            return (int)FileManagerResponseCodes.InvalidTaskType;
        }
        #endregion Undelete

        #region Download
        public int Download(SmokeTestManagerEventArgs e)
        {
            GenericHolder<CLError> refHolder = e.ProcessingErrorHolder;
            Exception ex = new NotImplementedException("Can Not Download a Session");
            AddException(ex, ref refHolder);
            return (int)FileManagerResponseCodes.InvalidTaskType;
        }
        #endregion Download

        #region ListItems
        public int ListItems(SmokeTestManagerEventArgs e)
        {
            ListItems listTask = e.CurrentTask as ListItems;
            StringBuilder newBuilder = new StringBuilder();
            if(listTask == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;

            int getListResponseCode = -1;
            ICLCredentialSettings settings = new AdvancedSyncSettings(e.ParamSet.ManualSync_Folder.Replace("\"", ""));
            CLError initializeCredsError;
            TaskEventArgs taskArgs = e as TaskEventArgs;
            bool success = CredentialHelper.InitializeCreds(ref taskArgs, out settings, out initializeCredsError);
            if (!success)
                return (int)FileManagerResponseCodes.InitializeCredsError;

            GenericHolder<CLError> refHolder = e.ProcessingErrorHolder;
            CLHttpRestStatus restStatus;
            ListSessionsResponse sessionList = null;

            CLError getSessisonsError = e.Creds.ListSessions(ManagerConstants.TimeOutMilliseconds, out restStatus, out sessionList, settings);
            if (getSessisonsError != null || restStatus != CLHttpRestStatus.Success)
            {
                 ExceptionManagerEventArgs failArgs = new ExceptionManagerEventArgs()
                 {
                     Error = getSessisonsError, 
                     RestStatus  =restStatus, 
                     ProcessingErrorHolder = e.ProcessingErrorHolder, 
                     OpperationName = "SessionManager.ListItems"
                 };
                SmokeTaskManager.HandleFailure(failArgs);
                return (int)FileManagerResponseCodes.UnknownError;
            }

            if (listTask.ExpectedCountSpecified && listTask.ExpectedCount > 0)
            {
                if (sessionList.Sessions.Count() != listTask.ExpectedCount)
                    return (int)FileManagerResponseCodes.ExpectedItemMatchFailure;
            }

            ItemsListManager listManager = ItemsListManager.GetInstance();
            listManager.Sessions.Clear();
            if (sessionList.Sessions.Count() == 0)
            {
                newBuilder.AppendLine("Session Count: 0");
            }
            else
            {
                AddSessionsToManager(sessionList, ref listManager, true, ref newBuilder);
            }
            e.StringBuilderList.Add(new StringBuilder(newBuilder.ToString()));
            return getListResponseCode;
        }

        #region Private
        private String AddSessionsToManager(ListSessionsResponse sessionList, ref ItemsListManager mgr, bool printValues, ref StringBuilder builder)
        {
            if (printValues)
            {
                builder.AppendLine("Listing Sessions....");
            }
            int iterations = 0;
            foreach (Session sesh in sessionList.Sessions)
            {
                iterations++;
                builder.AppendLine();
                if (!mgr.Sessions.Contains(sesh))
                    mgr.Sessions.Add(sesh);
                if (printValues)
                {
                    builder.AppendLine(string.Format("Session {0}:", iterations.ToString()));
                    builder.AppendLine(string.Format("  Token   {0} ", sesh.Token));
                    builder.AppendLine(string.Format("  Expires {0} ", sesh.ExpiresAt));
                    builder.AppendLine();
                }
            }
            return builder.ToString();
        }

        private ListSessionsResponse GetSessionList(SmokeTestManagerEventArgs e)
        {
            CLHttpRestStatus restStatus;
            ListSessionsResponse sessionList;
            ICLCredentialSettings settings = new AdvancedSyncSettings(e.ParamSet.ManualSync_Folder.Replace("\"", ""));
            CLError getSessisonsError = e.Creds.ListSessions(ManagerConstants.TimeOutMilliseconds, out restStatus, out sessionList, settings);
            if (getSessisonsError != null || restStatus != CLHttpRestStatus.Success)
            {
                ExceptionManagerEventArgs failArgs = new ExceptionManagerEventArgs()
                {
                    Error = getSessisonsError,
                    RestStatus = restStatus,
                    ProcessingErrorHolder = e.ProcessingErrorHolder,
                    OpperationName = "SessionManager.ListItems"
                };
                SmokeTaskManager.HandleFailure(failArgs);
                return null;
            }
            return sessionList;
        }
        #endregion 

        #endregion List Items

        #endregion Interface Implementation

        #region Private

        private void AddException(Exception ex, ref GenericHolder<CLError> processingErrorHolder)
        {
            lock (processingErrorHolder)
            {
                processingErrorHolder.Value = processingErrorHolder.Value + ex;
            }
        }
        #endregion 
    }
}
