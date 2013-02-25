using CloudApiPublic;
using CloudApiPublic.Static;
using CloudSDK_SmokeTest.Events.ManagerEventArgs;
using CloudSDK_SmokeTest.Interfaces;
using CloudSDK_SmokeTest.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Managers
{
    public static class SmokeTaskManager  
    {
        public static ISmokeTaskManager SelectManager(SmokeTask task)
        {

            ISmokeTaskManager manager = null;
            switch (task.ObjectType.type)
            { 
                case ModificationObjectType.SyncBox:
                    manager = new SyncBoxManager();
                    break;
                case ModificationObjectType.File:
                    manager = new FileManager();
                    break;
                case ModificationObjectType.Folder:
                    manager = new FileManager();
                    break;
                case ModificationObjectType.Plan:
                    manager = new PlanManager();
                    break;
                case ModificationObjectType.Session:
                    manager = new SessionManager();
                    break;
            }
            return manager;
        }

        public static long GetOpperationSyncBoxID(SmokeTestManagerEventArgs e)
        {
            long syncBoxID = 0;
            if (e.CurrentTask.SelectedSyncBoxID > 0)
                syncBoxID = e.CurrentTask.SelectedSyncBoxID;
            //ZW Replace
            //else if (e.CurrentTask.SyncBoxes.Count > 0)
            //    syncBoxID = e.CurrentTask.SyncBoxes.Keys.FirstOrDefault();
            else
                syncBoxID = e.ParamSet.ManualSyncBoxID;
            return syncBoxID;
        }

        public static void HandleFailure(ExceptionManagerEventArgs failArgs)
        {
            Exception exception;
            if (failArgs.CredsCreateStatus != null && failArgs.CredsCreateStatus != CLCredentialCreationStatus.Success)
            {
                exception = new Exception(string.Format("There was an error Initializing the Credentials for a new SyncBox in SyncBoxManager {0}",  failArgs.OpperationName));
                failArgs.ProcessingErrorHolder.Value = failArgs.ProcessingErrorHolder.Value + exception;
            }
            if ( failArgs.RestStatus != null &&  failArgs.RestStatus != CLHttpRestStatus.Success)
            {
                exception = new Exception(string.Format("The Rest Status Returned From {0} is {1}",  failArgs.OpperationName,  failArgs.RestStatus.ToString()));
                 failArgs.ProcessingErrorHolder.Value =  failArgs.ProcessingErrorHolder.Value + exception;

            }
            if (failArgs.SyncBoxCreateStatus != null && failArgs.SyncBoxCreateStatus != CLSyncBoxCreationStatus.Success)
            {
                exception = new Exception(string.Format("There was an error initializing the SyncBox for {0} the Creation Status is {1}", failArgs.OpperationName, failArgs.SyncBoxCreateStatus.ToString()));
                 failArgs.ProcessingErrorHolder.Value =  failArgs.ProcessingErrorHolder.Value + exception;

            }
            if (failArgs.Error != null)
            {
                IEnumerable<Exception> exceptions = failArgs.Error.GrabExceptions();
                foreach (Exception exc in exceptions)
                {
                    lock ( failArgs.ProcessingErrorHolder)
                    {
                         failArgs.ProcessingErrorHolder.Value =  failArgs.ProcessingErrorHolder.Value + exc;
                    }
                }
            }
        }
    }
}
