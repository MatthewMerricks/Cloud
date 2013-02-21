using CloudApiPublic;
using CloudApiPublic.Interfaces;
using CloudApiPublic.JsonContracts;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
using CloudSDK_SmokeTest.Events.CLEventArgs;
using CloudSDK_SmokeTest.Managers;
using CloudSDK_SmokeTest.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Helpers
{
    public static class ItemsListHelper
    {
        public static int RunListSubscribedPlans(ItemListHelperEventArgs itemListHelperArgs)
        {
            GenericHolder<CLError> refHolder = itemListHelperArgs.ProcessingErrorHolder;
            int getListResponseCode = -1;
            CLHttpRestStatus restStatus;
            ListPlansResponse plansList = null;
            CLCredentialCreationStatus credsCreateStatus;
            CLCredential creds;
            ICLCredentialSettings settings = new AdvancedSyncSettings(itemListHelperArgs.ParamSet.ManualSync_Folder.Replace("\"", ""));
            CLError initializeCredsError;
            TaskEventArgs taskArgs = itemListHelperArgs as TaskEventArgs;
            bool success = CredentialHelper.InitializeCreds(ref taskArgs, out settings, out initializeCredsError);
            if (!success)
                return (int)FileManagerResponseCodes.InitializeCredsError;

            CLError getPlansError;
            //ZW Parameters Change
            getPlansError = itemListHelperArgs.Creds.ListPlans(ManagerConstants.TimeOutMilliseconds, out restStatus, out plansList, settings);
            if (getPlansError != null || restStatus != CLHttpRestStatus.Success)
            {
                HandleFailure(getPlansError, restStatus, null, "RunListPlans.creds.ListPlans", ref refHolder);
                return (int)FileManagerResponseCodes.UnknownError;
            }
            if (itemListHelperArgs.ListItemsTask.ExpectedCount > 0)
            {
                if (plansList.Plans.Count() != itemListHelperArgs.ListItemsTask.ExpectedCount)
                    return (int)FileManagerResponseCodes.ExpectedItemMatchFailure;
            }
            ItemsListManager listManager = ItemsListManager.GetInstance();
            foreach (CloudApiPublic.JsonContracts.Plan plan in plansList.Plans)
            {
                listManager.Plans.Add(plan);
                Console.WriteLine(string.Format("The Plan Name:{0} ID:{1} was retrieved and added to ItemsListManager's List of Plans", plan.FriendlyPlanName, plan.Id));
                getListResponseCode = 0;
            }
            return getListResponseCode;
        }

        public static int RunListSessions(ItemListHelperEventArgs itemListHelperArgs, bool printValues, bool printSessionSyncBoxes)
        {
            int getListResponseCode = -1;
            GenericHolder<CLError> refHolder = itemListHelperArgs.ProcessingErrorHolder;
            CLHttpRestStatus restStatus;
            ListSessionsResponse sessionList = null;
            CLCredentialCreationStatus credsCreateStatus;
            CLCredential creds = null;
            ICLCredentialSettings settings = new AdvancedSyncSettings(itemListHelperArgs.ParamSet.ManualSync_Folder.Replace("\"", ""));
            CLError initializeCredsError;
            TaskEventArgs taskArgs = itemListHelperArgs as TaskEventArgs;
            bool success = CredentialHelper.InitializeCreds(ref taskArgs, out settings, out initializeCredsError);
            if (!success)
                return (int)FileManagerResponseCodes.InitializeCredsError;

            CLError getSessisonsError = itemListHelperArgs.Creds.ListSessions(ManagerConstants.TimeOutMilliseconds, out restStatus, out sessionList, settings);
            if (getSessisonsError != null || restStatus != CLHttpRestStatus.Success)
            {
                HandleFailure(getSessisonsError, restStatus, null, "ItemsListHelper.RunSessionsList", ref refHolder);
                return (int)FileManagerResponseCodes.UnknownError;
            }

            if ((itemListHelperArgs.ListItemsTask != null) && itemListHelperArgs.ListItemsTask.ExpectedCountSpecified && itemListHelperArgs.ListItemsTask.ExpectedCount > 0)
            {
                if (sessionList.Sessions.Count() != itemListHelperArgs.ListItemsTask.ExpectedCount)
                    return (int)FileManagerResponseCodes.ExpectedItemMatchFailure;
            }

            ItemsListManager listManager = ItemsListManager.GetInstance();
            listManager.Sessions.Clear();
            if (sessionList.Sessions.Count() == 0)
            {
                Console.WriteLine("Session Count: 0");
            }
            else
            {
                foreach (Session sesh in sessionList.Sessions)
                {
                    if (!listManager.Sessions.Contains(sesh))
                        listManager.Sessions.Add(sesh);
                    if (printValues)
                    {
                        Console.WriteLine(string.Format("The Session with token {0} expires at {1}  ", sesh.Token, sesh.ExpiresAt));
                    }
                    if (printSessionSyncBoxes)
                    {
                        Console.WriteLine("SyncBox IDs:");
                        foreach (long l in sesh.SyncBoxIds)
                            Console.Write(l.ToString() + ", ");
                    }
                    getListResponseCode = 0;
                }
            }

            return getListResponseCode;
        }

        public static int RunListSubscribtionSyncBoxes(ItemListHelperEventArgs itemListHelperArgs)
        {
            int getListResponseCode = -1;
            GenericHolder<CLError> refHolder = itemListHelperArgs.ProcessingErrorHolder;
            CLHttpRestStatus restStatus;
            ListSyncBoxes syncBoxList;
            CLCredentialCreationStatus credsCreateStatus;
            CLCredential creds;
            ICLCredentialSettings settings = new AdvancedSyncSettings(itemListHelperArgs.ParamSet.ManualSync_Folder.Replace("\"", ""));
            CLError initializeCredsError;
            TaskEventArgs args = itemListHelperArgs as TaskEventArgs;
            bool success = CredentialHelper.InitializeCreds(ref args, out settings, out initializeCredsError);
            if (!success)
                return (int)FileManagerResponseCodes.InitializeCredsError;

            CLError getSyncBoxesError = itemListHelperArgs.Creds.ListSyncBoxes(ManagerConstants.TimeOutMilliseconds, out restStatus, out syncBoxList, settings);
            if (getSyncBoxesError != null || restStatus != CLHttpRestStatus.Success)
            {
                HandleFailure(getSyncBoxesError, restStatus, null, "ItemsListHelper.RunListSyncBoxes", ref refHolder);
                return (int)FileManagerResponseCodes.UnknownError;
            }
            if (itemListHelperArgs.ListItemsTask.ExpectedCount > 0)
            { 
                if(syncBoxList.SyncBoxes.Count() != itemListHelperArgs.ListItemsTask.ExpectedCount)
                    return (int)FileManagerResponseCodes.ExpectedItemMatchFailure;
            }

            ItemsListManager listManager = ItemsListManager.GetInstance();
            foreach (CloudApiPublic.JsonContracts.SyncBox syncBox in syncBoxList.SyncBoxes)
            {
                listManager.SyncBoxes.Add(syncBox);
                Console.WriteLine(string.Format("The SyncBox Name:{0} with ID:{1} and PlanID:{2} was retrieved and added to ItemsListManager's List of SyncBoxes", syncBox.FriendlyName, syncBox.Id, syncBox.PlanId));
                getListResponseCode = 0;
            }

            return getListResponseCode;
        }

        public static void HandleFailure(CLError error, CLHttpRestStatus? restStatus, CLCredentialCreationStatus? credsCreateStatus, string opperationName, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            List<Exception> errors = new List<Exception>();
            if (error != null)
            {
                foreach (Exception exception in error.GrabExceptions())
                {
                    errors.Add(exception);
                }
            }
            if (restStatus.HasValue && restStatus.Value != CLHttpRestStatus.Success)
                errors.Add(ExceptionManager.ReturnException(opperationName, restStatus.ToString()));

            else if (credsCreateStatus.HasValue && credsCreateStatus.Value != CLCredentialCreationStatus.Success)
                errors.Add(ExceptionManager.ReturnException(opperationName, credsCreateStatus.ToString()));
        }
 
    }
}
