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
    public class PlanManager : ISmokeTaskManager
    {
        public static int RunCreatePlan(InputParams paramSet, SmokeTask smokeTask, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            int planId = 0;
            throw new NotImplementedException("Implement Run Create Plan Method In Smoke Test helper Class.");
            return planId;
        }

        public static int RunPlanDeletionTask(InputParams paramSet, SmokeTask smokeTask, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            int returnValue = 0;
            throw new NotImplementedException("RunPlanDeletion Method is not implemented.");
            return returnValue;
        }

        public static bool AddNewPlan(CLCredential creds)
        {
            bool successful = false;
            return successful;
        }

        public static bool DeletePlan()
        {
            bool successful = false;

            return successful;
        }


        #region Ineterface Implementation 
        public int Create(Events.ManagerEventArgs.SmokeTestManagerEventArgs e)
        {
            GenericHolder<CLError> refHolder = e.ProcessingErrorHolder;
            Exception ex = new NotImplementedException("Can Not Create a Plan");
            AddException(ex, ref refHolder);
            return (int)FileManagerResponseCodes.InvalidTaskType;
        }

        public int Rename(Events.ManagerEventArgs.SmokeTestManagerEventArgs e)
        {
            GenericHolder<CLError> refHolder = e.ProcessingErrorHolder;
            Exception ex = new NotImplementedException("Can Not Rename a Plan");
            AddException(ex, ref refHolder);
            return (int)FileManagerResponseCodes.InvalidTaskType;
        }

        public int Delete(Events.ManagerEventArgs.SmokeTestManagerEventArgs e)
        {
            GenericHolder<CLError> refHolder = e.ProcessingErrorHolder;
            Exception ex = new NotImplementedException("Can Not Delete a Plan");
            AddException(ex, ref refHolder);
            return (int)FileManagerResponseCodes.InvalidTaskType;
        }

        public int UnDelete(Events.ManagerEventArgs.SmokeTestManagerEventArgs e)
        {
            GenericHolder<CLError> refHolder = e.ProcessingErrorHolder;
            Exception ex = new NotImplementedException("Can Not UnDelete a Plan");
            AddException(ex, ref refHolder);
            return (int)FileManagerResponseCodes.InvalidTaskType;
        }

        public int Download(Events.ManagerEventArgs.SmokeTestManagerEventArgs e)
        {
            GenericHolder<CLError> refHolder = e.ProcessingErrorHolder;
            Exception ex = new NotImplementedException("Can Not Download a Plan");
            AddException(ex, ref refHolder);
            return (int)FileManagerResponseCodes.InvalidTaskType;
        }

        public int ListItems(Events.ManagerEventArgs.SmokeTestManagerEventArgs e)
        {
            ListItems listTask = e.CurrentTask as ListItems;
            if (listTask == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;

            StringBuilder newBuilder = new StringBuilder();
            ICLCredentialSettings settings = new AdvancedSyncSettings(e.ParamSet.ManualSync_Folder.Replace("\"", ""));
            CLError initializeCredsError;
            TaskEventArgs taskArgs = e as TaskEventArgs;
            bool success = CredentialHelper.InitializeCreds(ref taskArgs, out settings, out initializeCredsError);
            if (!success)
                return (int)FileManagerResponseCodes.InitializeCredsError;

            GenericHolder<CLError> refHolder = e.ProcessingErrorHolder;
            int getListResponseCode = -1;
            CLHttpRestStatus restStatus;
            ListPlansResponse plansList = null; ;
            CLError getPlansError;

            getPlansError = e.Creds.ListPlans(ManagerConstants.TimeOutMilliseconds, out restStatus, out plansList, settings);
            if (getPlansError != null || restStatus != CLHttpRestStatus.Success)
            {
                ExceptionManagerEventArgs failArgs = new ExceptionManagerEventArgs() 
                {
                    Error = getPlansError, 
                    OpperationName = "PlanManager.ListItems", 
                    RestStatus = restStatus, 
                    ProcessingErrorHolder = e.ProcessingErrorHolder 
                };
                SmokeTaskManager.HandleFailure(failArgs);                
                return (int)FileManagerResponseCodes.UnknownError;
            }
            

            if (listTask.ExpectedCount > 0)
            {
                if (plansList != null && plansList.Plans.Count() != listTask.ExpectedCount)
                    return (int)FileManagerResponseCodes.ExpectedItemMatchFailure;
            }
            ItemsListManager listManager = ItemsListManager.GetInstance();
            if (plansList == null || plansList.Plans.Count() == 0)
                newBuilder.AppendLine("There are no Plans to be listed.");
            else
            {
                foreach (CloudApiPublic.JsonContracts.Plan plan in plansList.Plans)
                {
                    listManager.Plans.Add(plan);
                    newBuilder.AppendLine(string.Format("The Plan Name:{0} ID:{1} was retrieved and added to ItemsListManager's List of Plans", plan.FriendlyPlanName, plan.Id));
                    getListResponseCode = 0;
                }
            }
            e.StringBuilderList.Add(new StringBuilder(newBuilder.ToString()));
            return getListResponseCode;
        }
        #endregion 

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
