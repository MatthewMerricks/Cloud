using CloudApiPublic;
using CloudApiPublic.Interfaces;
using CloudApiPublic.JsonContracts;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
using CloudSDK_SmokeTest.Events.CLEventArgs;
using CloudSDK_SmokeTest.Helpers;
using CloudSDK_SmokeTest.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Managers
{
    public class SessionManager
    {

        public static long RunCreateSessionTask(InputParams paramSet, SmokeTask smokeTask, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            string newSessionKey = string.Empty;

            Console.WriteLine("Preparing to Create Session.");
            ItemListHelperEventArgs args = new ItemListHelperEventArgs() { ParamSet = paramSet, ProcessingErrorHolder = ProcessingErrorHolder };
            ItemsListHelper.RunListSessions(args, false, false);
            ItemsListManager mgr = ItemsListManager.GetInstance();
            int prior = mgr.Sessions.Count;

            int responseCode = SessionManager.CreateSession(paramSet, smokeTask, out newSessionKey, ref ProcessingErrorHolder);
            Console.WriteLine("Create Sesssion Results:");
            if (string.IsNullOrEmpty(newSessionKey))
                Console.WriteLine("Create Sesssion Failed: Returned Session ID is 0:");
            else
                Console.WriteLine(string.Format("Created Session with Key {0}", newSessionKey));

            Console.WriteLine("Session Count Before Running: {0}", prior.ToString());
            Console.WriteLine("Session Count After Running: {0}", mgr.Sessions.Count.ToString());
            return responseCode;

        }

        public static int RunSessionDeletionTask(InputParams paramSet, SmokeTask smokeTask, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            List<CloudApiPublic.JsonContracts.Session> toDelete = new List<CloudApiPublic.JsonContracts.Session>();
            int deleteSessionResponseCode = 0;
            Deletion deleteTask = smokeTask as Deletion;
            if (deleteTask == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;

            GenericHolder<CLError> refHolder = ProcessingErrorHolder;
            Console.WriteLine("Preparing Delete Session Task");
            deleteSessionResponseCode = SessionManager.DeleteSession(paramSet, deleteTask, ref refHolder);
            Console.WriteLine("Exiting Delete Session Task");
            return deleteSessionResponseCode;
        }
        
        public static int CreateSession(InputParams paramSet, SmokeTask smokeTask, out string sessionKey,  ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            int createSessionResponseCode = 0;
            sessionKey = string.Empty;
            ICLCredentialSettings settings;
            CLError initCredsError = new CLError();
            ItemListHelperEventArgs args = new ItemListHelperEventArgs()
            {
                 ParamSet = paramSet,
                 ProcessingErrorHolder = ProcessingErrorHolder,
            };
            bool success = CredentialHelper.InitializeCreds(ref args, out settings, out initCredsError);
            if (!success)
                return (int)FileManagerResponseCodes.InitializeCredsError;

            CLHttpRestStatus restStatus;
            CloudApiPublic.JsonContracts.SessionCreateResponse sessionCreateResponse;
            CLError createSessionError = args.Creds.CreateSession(ManagerConstants.TimeOutMilliseconds, out restStatus, out sessionCreateResponse);
            if (createSessionError != null || restStatus != CLHttpRestStatus.Success)
            {
                HandleFailure(null, restStatus, "Credential.Create Session ", createSessionError, ref ProcessingErrorHolder);
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

        public static int DeleteSession(InputParams paramSet, Deletion deleteTask, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            int index = 0;
            int deleteSessionResponseCode = 0;
            ICLCredentialSettings settings;
            CLError initCredsError = new CLError();
            ItemListHelperEventArgs args = new ItemListHelperEventArgs()
            {
                ParamSet = paramSet,
                ProcessingErrorHolder = ProcessingErrorHolder,
            };
            bool success = CredentialHelper.InitializeCreds(ref args, out settings, out initCredsError);
            if (!success)
                return (int)FileManagerResponseCodes.InitializeCredsError;
            
            DeleteSessionEventArgs deleteEventArgs = new DeleteSessionEventArgs() 
            {
                 Creds = args.Creds,
                 ParamSet = paramSet,
                 ProcessingErrorHolder = ProcessingErrorHolder,
            };


            int listItemsResponse = ItemsListHelper.RunListSessions(args, false, false);
            if (listItemsResponse != 0)
            {
                return (int)FileManagerResponseCodes.UnknownError;
            }

            ItemsListManager mgr = ItemsListManager.GetInstance();
            List<Session> toDelete = AddSessionsToDeletionList(deleteTask, mgr, args);
           
            foreach (Session session in toDelete)
            {
                if (deleteSessionResponseCode == 0)
                {
                    deleteEventArgs.Session = session;
                    deleteSessionResponseCode = ExecuteDeleteSession(deleteEventArgs);
                }
                else
                    break;
            }
            
            return deleteSessionResponseCode;
        }

        private static int ExecuteDeleteSession(DeleteSessionEventArgs deleteEventArgs)
        {
            int deleteSessionResponse = 0;

            GenericHolder<CLError> refHolder = deleteEventArgs.ProcessingErrorHolder;
            CLHttpRestStatus restStatus = CLHttpRestStatus.BadRequest;
            CloudApiPublic.JsonContracts.SessionDeleteResponse sessionDeleteResponse;
            CLError deleteSessionError = null;
            if (deleteEventArgs.Session != null && !string.IsNullOrEmpty(deleteEventArgs.Session.Key))
            {
                Console.WriteLine(string.Format("Entering Delete  Individual Session with Key: {0}", deleteEventArgs.Session.Key));
                deleteSessionError = deleteEventArgs.Creds.DeleteSession(ManagerConstants.TimeOutMilliseconds, out restStatus, out sessionDeleteResponse, deleteEventArgs.Session.Key);
                Console.WriteLine("Exiting Delete Individual Session ");
            }
            if (deleteSessionError != null || restStatus != CLHttpRestStatus.Success)
            {
                HandleFailure(null, restStatus, "Session Manager.DeleteSession", deleteSessionError, ref refHolder);
                return (int)FileManagerResponseCodes.UnknownError;
            }

            return deleteSessionResponse;
        }

        private static List<Session> AddSessionsToDeletionList(Deletion deleteTask, ItemsListManager mgr, ItemListHelperEventArgs args)
        {
            List<Session> toDelete = new List<Session>();
            if (deleteTask.DeleteAllSpecified && deleteTask.DeleteAll)
            { 
                foreach (CloudApiPublic.JsonContracts.Session session in mgr.Sessions)
                    toDelete.Add(session);
            }
            else if (deleteTask.DeleteCountSpecified && deleteTask.DeleteCount > 0)
            {
                IEnumerable<Session> sessions = mgr.Sessions.Take(deleteTask.DeleteCount);
                foreach (Session session in sessions)
                    toDelete.Add(session);
            }
            else
            {
                if (deleteTask.IDSpecified && deleteTask.ID > 0)
                    toDelete.Add(mgr.Sessions.Where(s => s.Key == deleteTask.ID.ToString()).FirstOrDefault());
                else
                {
                    Session session = null;
                    string key = mgr.SessionsCreatedDynamically.FirstOrDefault();
                    if (!string.IsNullOrEmpty(key))
                    {
                        session = mgr.Sessions.Where(s => s.Key == key).FirstOrDefault();
                    }
                    else
                    {
                        session = ReturnSessionAtIndex(args, 0); 
                    }
                    toDelete.Add(session);
                }
            }
            return toDelete;
        }

        public static CloudApiPublic.JsonContracts.Session ReturnSessionAtIndex(ItemListHelperEventArgs eventArgs, int index)
        {
            CloudApiPublic.JsonContracts.Session returnValue = null;
            CLCredential creds;
            CLCredentialCreationStatus credsStatus;
            GenericHolder<CLError> refHolder = eventArgs.ProcessingErrorHolder;
            CLError credsError = CLCredential.CreateAndInitialize(eventArgs.ParamSet.API_Key, eventArgs.ParamSet.API_Secret, out creds, out credsStatus);
            if (credsError != null || credsStatus != CLCredentialCreationStatus.Success)
            {
                HandleFailure(credsStatus, null, "CreateSession Init Creds", credsError, ref refHolder);
            }
            CLHttpRestStatus restStatus;
            CloudApiPublic.JsonContracts.ListSessionsResponse response;
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

        public static void HandleFailure(CLCredentialCreationStatus? credsStatus, CLHttpRestStatus? restStatus, string opperationName, CLError error, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            List<Exception> exceptionList = new List<Exception>();
            if (credsStatus.HasValue && credsStatus != CLCredentialCreationStatus.Success)
            {
                Exception exception = ExceptionManager.ReturnException(opperationName, credsStatus.Value.ToString());
                exceptionList.Add(exception);
            }
            if (restStatus.HasValue && restStatus.Value != CLHttpRestStatus.Success)
            {
                Exception exception = ExceptionManager.ReturnException(opperationName, restStatus.Value.ToString());
                exceptionList.Add(exception);
            }
            exceptionList.AddRange(error.GrabExceptions());
            lock(ProcessingErrorHolder)
            {
                foreach (Exception ex in exceptionList)
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + ex;
                
            }
                
        }
    }
}
