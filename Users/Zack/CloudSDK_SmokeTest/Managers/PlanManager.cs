using CloudApiPublic;
using CloudApiPublic.Model;
using CloudSDK_SmokeTest.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Managers
{
    public class PlanManager
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
    }
}
