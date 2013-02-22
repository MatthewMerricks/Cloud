using CloudApiPublic;
using CloudApiPublic.Model;
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
        public static long RunCreatePlan(InputParams paramSet, SmokeTask smokeTask, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            long planId = 0;
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
            throw new NotImplementedException("Implement Exisiting Code for List Plans");
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
