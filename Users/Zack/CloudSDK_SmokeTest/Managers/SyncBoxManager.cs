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
        #region Public
        public static long RunCreateSyncBoxTask(InputParams paramSet, SmokeTask smokeTask, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            long? newBoxId;
            CreateSyncBox createTask = smokeTask as CreateSyncBox;
            if (createTask != null && createTask.CreateNew == true)
            {
                Console.WriteLine("Preapring to create new SyncBox.");
                newBoxId = SyncBoxManager.AddNewSyncBox(paramSet, ref ProcessingErrorHolder);
                if (newBoxId == (long)0)
                {
                    Exception newSyncBoxException = new Exception("There was an error creating a new Sync Box.");
                    lock (ProcessingErrorHolder)
                    {
                        ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + newSyncBoxException;
                    }
                }
                else
                    Console.WriteLine(string.Format("Successfully Created SyncBox with ID: {0}", newBoxId));

            }
            else
            {
                SyncBoxMapper.SyncBoxes.Add(SyncBoxMapper.SyncBoxes.Count(), paramSet.ManualSyncBoxID);
                newBoxId = paramSet.ManualSyncBoxID;
            }
            return newBoxId.HasValue ? newBoxId.Value : 0;
        }

        public static int RunSyncBoxDeletionTask(InputParams paramSet, SmokeTask smokeTask, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            int deleteTaskResponseCode = 0;
            Deletion deleteTask = smokeTask as Deletion;
            if (deleteTask == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;
            long syncBoxId = 0;
            if (deleteTask.ID > 0)
                syncBoxId = deleteTask.ID;
            Console.WriteLine(string.Format("Preparing to Delete SyncBox Task with ID: {0}", syncBoxId));
            bool success = SyncBoxManager.DeleteSyncBox(paramSet, syncBoxId, ref ProcessingErrorHolder);
            Console.WriteLine("Exiting Delete SyncBox Task");
            if (success)
                Console.WriteLine("Successfully Deleted SyncBox.");
            if (!success)
                deleteTaskResponseCode = -1;
            return deleteTaskResponseCode;
        }

        public static long? AddNewSyncBox(InputParams paramSet, ref GenericHolder<CLError> ProcessingExceptionHolder)
        {
            long? syncBoxId = null;
            CLCredential creds; CLCredentialCreationStatus credsStatus;
            string token = null;
            if(!string.IsNullOrEmpty(paramSet.Token))
                token = paramSet.Token;
            CLError error = CLCredential.CreateAndInitialize(paramSet.API_Key, paramSet.API_Secret, out creds, out credsStatus, token);
            if (credsStatus != CLCredentialCreationStatus.Success || error != null)
            {
                AddExceptions(credsStatus, null, null, "SyncBox Manager Add New SyncBox", error, ref ProcessingExceptionHolder);
                return null;
            }

            syncBoxId = CreateNewSyncBoxAndAddToDictionary(paramSet, creds, ref ProcessingExceptionHolder);
            return syncBoxId;
        }

        public static bool DeleteSyncBox(InputParams paramSet, long syncBoxId, ref GenericHolder<CLError> ProcessingExceptionHolder)
        {
            bool success = false;
            CLCredential creds; CLCredentialCreationStatus credsStatus;
            string token = null;
            if (!string.IsNullOrEmpty(paramSet.Token))
                token = paramSet.Token;
            CLError error = CLCredential.CreateAndInitialize(paramSet.API_Key, paramSet.API_Secret, out creds, out credsStatus, token);
            if (credsStatus != CLCredentialCreationStatus.Success || error != null)
            {
                AddExceptions(credsStatus, null, null, "SyncBox Manager Delete SyncBox Init Creds", error, ref ProcessingExceptionHolder);
                return success;
            }

            CLSyncBox syncBox; CLSyncBoxCreationStatus boxCreateStatus;
            CLError getSyncBoxError = CLSyncBox.CreateAndInitialize(creds, syncBoxId, out syncBox,out boxCreateStatus);
            if (getSyncBoxError != null || boxCreateStatus != CLSyncBoxCreationStatus.Success)
            {
                AddExceptions(null, null, boxCreateStatus, "SyncBox Manager  Delete SyncBox Init SyncBox",getSyncBoxError, ref ProcessingExceptionHolder);
                return success;
            }

            CLHttpRestStatus restStatus;
            CloudApiPublic.JsonContracts.SyncBoxHolder response;
            CLError deleteError = syncBox.DeleteSyncBox(ManagerConstants.TimeOutMilliseconds, out restStatus, out response);
            if (deleteError != null || restStatus != CLHttpRestStatus.Success)
            { 
                AddExceptions(null, restStatus, null, "SyncBox Manager Delete SyncBox Delete Method", deleteError, ref ProcessingExceptionHolder);
            }
            try
            {
                ItemsListManager mgr = ItemsListManager.GetInstance();
                CloudApiPublic.JsonContracts.SyncBox toRemove = mgr.SyncBoxes.Where(sb => sb.Id == syncBoxId).FirstOrDefault();
                if (mgr.SyncBoxes.Contains(toRemove))
                    mgr.SyncBoxes.Remove(toRemove);
            }
            catch (Exception exception)
            {
                lock (ProcessingExceptionHolder)
                {
                    ProcessingExceptionHolder.Value = ProcessingExceptionHolder.Value + exception;
                }
            }
            
            return success;
        }
        #endregion

        #region Private
        private static void AddExceptions(CLCredentialCreationStatus? credsStatus, CLHttpRestStatus? restStatus, CLSyncBoxCreationStatus? boxCreateStatus , string opperationName, CLError error, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            Exception exception;
            if (credsStatus != null && credsStatus != CLCredentialCreationStatus.Success)
            {
                exception = new Exception(string.Format("There was an error Initializing the Credentials for a new SyncBox in SyncBoxManager {0}", opperationName));
                ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
            }
            if (restStatus != null && restStatus != CLHttpRestStatus.Success)
            {
                exception = new Exception(string.Format("The Rest Status Returned From {0} is {1}", opperationName, restStatus.Value.ToString()));
                ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;

            }
            if(boxCreateStatus != null && boxCreateStatus != CLSyncBoxCreationStatus.Success)
            {
                exception = new Exception(string.Format("There was an error initializing the SyncBox for {0} the Creation Status is {1}", opperationName, boxCreateStatus.Value.ToString()));
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
            CloudApiPublic.Static.CLHttpRestStatus restStatus = new CLHttpRestStatus();
            CloudApiPublic.JsonContracts.SyncBoxHolder syncBox = null;
            CLError newBoxError = null;
            newBoxError = creds.AddSyncBoxOnServer(ManagerConstants.TimeOutMilliseconds, out restStatus, out syncBox);

            if (restStatus != CLHttpRestStatus.Success || newBoxError != null)
            {
                AddExceptions(null, restStatus, null, "SyncBox Manager Add Sync Box", newBoxError, ref ProcessingExceptionHolder);
            }

            if (syncBox != null && syncBox.SyncBox != null)
            {
                returnValue = syncBox.SyncBox.Id;
            }
            int key = SyncBoxMapper.SyncBoxes.Count();
            if (returnValue > 0)
            {
                SyncBoxMapper.SyncBoxes.Add(key, (long)returnValue);
                ItemsListManager mgr = ItemsListManager.GetInstance();
                if (!mgr.SyncBoxes.Contains(syncBox.SyncBox))
                {
                    mgr.SyncBoxes.Add(syncBox.SyncBox);
                    mgr.SyncBoxesCreatedDynamically.Add(returnValue.Value);
                }
            }
            return returnValue;
        }
        #endregion
    }
}
