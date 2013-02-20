using CloudApiPublic;
using CloudApiPublic.Interfaces;
using CloudApiPublic.Model;
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
        public static bool InitializeCreds(ref ItemListHelperEventArgs itemListHelperArgs, out ICLCredentialSettings settings, out CLError initializeCredsError)
        {
            bool canContinue = true;
            CLCredential creds = itemListHelperArgs.Creds;
            CLCredentialCreationStatus credsCreateStatus;
            settings = new AdvancedSyncSettings(itemListHelperArgs.ParamSet.ManualSync_Folder.Replace("\"", ""));
            GenericHolder<CLError> refHolder = itemListHelperArgs.ProcessingErrorHolder;

            initializeCredsError = CLCredential.CreateAndInitialize(itemListHelperArgs.ParamSet.API_Key, itemListHelperArgs.ParamSet.API_Secret, out creds, out credsCreateStatus);
            if (initializeCredsError != null || credsCreateStatus != CLCredentialCreationStatus.Success)
            {
                ItemsListHelper.HandleFailure(initializeCredsError, null, credsCreateStatus, "CreateAndInitialize Credentials", ref refHolder);
                canContinue = false;
            }
            itemListHelperArgs.Creds = creds;
            return canContinue;
        }
    }
}
