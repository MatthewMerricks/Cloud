using CloudApiPublic;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
using CloudSDK_SmokeTest.Helpers;
using CloudSDK_SmokeTest.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Managers
{
    public class SyncBoxManager
    {

        public static long? StartCreateNewSyncBox(InputParams paramSet, ref GenericHolder<CLError> ProcessingExceptionHolder)
        {
            long? syncBoxId = null;
            CLCredential creds; CLCredentialCreationStatus credsStatus;
            string token = null;
            if(!string.IsNullOrEmpty(paramSet.Token))
                token = paramSet.Token;
            CLError error = CLCredential.CreateAndInitialize(paramSet.API_Key, paramSet.API_Secret, out creds, out credsStatus, token);
            if (credsStatus != CLCredentialCreationStatus.Success || error != null)
            {
                AddExceptions(credsStatus, null,  error, ref ProcessingExceptionHolder);
                return null;
            }

            syncBoxId = CreateNewSyncBoxAndAddToDictionary(paramSet, creds, ref ProcessingExceptionHolder);
            return syncBoxId;
        }

        private static void AddExceptions(CLCredentialCreationStatus? credsStatus, CLHttpRestStatus? restStatus, CLError error, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            Exception exception;
            if (credsStatus != null && credsStatus != CLCredentialCreationStatus.Success)
            {
                exception = new Exception("There was an error Initializing the Credentials for a new SyncBox in SyncBoxManager.CreateNewSyncBox");
                ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
            }
            if (error != null)
            {
                IEnumerable<Exception> exceptions = error.GrabExceptions();
                foreach (Exception exc in exceptions)
                {
                    lock (ProcessingErrorHolder)
                    {
                        ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exc;
                    }
                }
            }
        }

        private static long? CreateNewSyncBoxAndAddToDictionary(InputParams paramSet, CloudApiPublic.CLCredential creds, ref GenericHolder<CLError> ProcessingExceptionHolder)
        {
            long? returnValue = 0;
            CloudApiPublic.Static.CLHttpRestStatus restStatus;
            CloudApiPublic.JsonContracts.SyncBoxHolder syncBox;
            string strippedFolderPath = paramSet.ManualSync_Folder.Replace("\"", "");
            AdvancedSyncSettings settings = new AdvancedSyncSettings(strippedFolderPath);
            CLError newBoxError = creds.AddSyncBoxOnServer(ManagerConstants.TimeOutMilliseconds, out restStatus, out syncBox, settings, null);

            if (restStatus != CLHttpRestStatus.Success || newBoxError != null)
            {
                AddExceptions(null, restStatus, newBoxError, ref ProcessingExceptionHolder);
            }

            if (syncBox != null && syncBox.SyncBox != null)
            {
                returnValue = syncBox.SyncBox.Id;
            }
            int key = SyncBoxMapper.SyncBoxes.Count();
            if(returnValue > 0)
                SyncBoxMapper.SyncBoxes.Add(key, (long)returnValue);
            return returnValue;
        }
    }
}
