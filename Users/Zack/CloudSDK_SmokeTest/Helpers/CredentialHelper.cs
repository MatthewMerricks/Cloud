using Cloud;
using Cloud.Interfaces;
using Cloud.Model;
using CloudSDK_SmokeTest.Events.CLEventArgs;
using CloudSDK_SmokeTest.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Helpers
{
    public class CredentialHelper
    {
        public static bool InitializeCreds(ref TaskEventArgs taskEventArgs, out ICLCredentialSettings settings, out CLError initializeCredsError)
        {
            bool canContinue = true;
            CLCredential creds = taskEventArgs.Creds;
            CLCredentialCreationStatus credsCreateStatus;
            settings = new AdvancedSyncSettings(taskEventArgs.ParamSet.ManualSync_Folder.Replace("\"", ""));
            GenericHolder<CLError> refHolder = taskEventArgs.ProcessingErrorHolder;

            initializeCredsError = CLCredential.CreateAndInitialize(taskEventArgs.ParamSet.API_Key, taskEventArgs.ParamSet.API_Secret, out creds, out credsCreateStatus);
            if (initializeCredsError != null || credsCreateStatus != CLCredentialCreationStatus.Success)
            {
                ItemsListHelper.HandleFailure(initializeCredsError, null, credsCreateStatus, "CreateAndInitialize Credentials", ref refHolder);
                canContinue = false;
            }
            taskEventArgs.Creds = creds;
            return canContinue;
        }
    }
}
