using CloudApiPublic;
using CloudApiPublic.Interfaces;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
using CloudSDK_SmokeTest.Events.CLEventArgs;
using CloudSDK_SmokeTest.Events.ManagerEventArgs;
using CloudSDK_SmokeTest.Helpers;
using CloudSDK_SmokeTest.Interfaces;
using CloudSDK_SmokeTest.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Managers
{
    public class SyncBoxManager : ISmokeTaskManager
    {
        #region Public
        public static int RunCreateSyncBoxTask(InputParams paramSet, ref SmokeTask smokeTask, ref StringBuilder reportBuilder, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            int responseCode = 0;
            long? newBoxId =0;
            CreateSyncBox createTask = smokeTask as CreateSyncBox;
            if (createTask != null && createTask.CreateNew == true)
            {
                reportBuilder.AppendLine("Preapring to create new SyncBoxs.");
                int iterations = 1;
                if (createTask.Count > 0)
                    iterations = createTask.Count;
                for (int x = 0; x < iterations; x++)
                {
                    newBoxId = SyncBoxManager.AddNewSyncBox(paramSet, createTask, ref ProcessingErrorHolder);
                    if (newBoxId == (long)0)
                    {
                        Exception newSyncBoxException = new Exception("There was an error creating a new Sync Box.");
                        lock (ProcessingErrorHolder)
                        {
                            ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + newSyncBoxException;
                        }
                        responseCode = (int)FileManagerResponseCodes.InitializeSynBoxError;
                    }
                    else
                    {
                        reportBuilder.AppendLine(string.Format("Successfully Created SyncBox with ID: {0}", newBoxId));
                        SyncBoxMapper.SyncBoxes.Add(SyncBoxMapper.SyncBoxes.Count(), newBoxId.Value);
                        createTask.SelectedSyncBoxID = newBoxId.Value;
                    }
                }
            }
            else
            {
                SyncBoxMapper.SyncBoxes.Add(SyncBoxMapper.SyncBoxes.Count(), paramSet.ManualSyncBoxID);
                newBoxId = paramSet.ManualSyncBoxID;
            }
            return responseCode;//newBoxId.HasValue ? newBoxId.Value : 0;
        }

        public static int RunSyncBoxDeletionTask(InputParams paramSet, SmokeTask smokeTask, ref StringBuilder reportBuilder, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            int deleteTaskResponseCode = 0;
            Deletion deleteTask = smokeTask as Deletion;
            if (deleteTask == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;
            long syncBoxId = 0;
            if (deleteTask.ID > 0)
                syncBoxId = deleteTask.ID;
            reportBuilder.AppendLine(string.Format("Preparing to Delete SyncBox Task with ID: {0}", syncBoxId));
            bool success = SyncBoxManager.DeleteSyncBox(paramSet, syncBoxId, ref ProcessingErrorHolder);
            if (success)
                reportBuilder.AppendLine(string.Format("Successfully Deleted SyncBox {0}.", syncBoxId));
            if (!success)
            {
                reportBuilder.AppendLine(string.Format("There was an Issue Deleting SyncBox {0}.", syncBoxId));
                deleteTaskResponseCode = -1;
            }
            reportBuilder.AppendLine("Exiting Delete SyncBox Task");
            return deleteTaskResponseCode;
        }

        public static int RunSyncBoxRenameTask(InputParams paramSet, SmokeTask smokeTask, ref StringBuilder reportBuilder, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            int responseCode = 0;
            Rename renameTask = smokeTask as Rename;
            if (renameTask == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;

            
            ICLCredentialSettings credSettings;
            CLError initializeCredsError;
            TaskEventArgs eventArgs = new TaskEventArgs()
            {
                CurrentTask = smokeTask,
                ParamSet = paramSet,
                ProcessingErrorHolder = ProcessingErrorHolder,
                ReportBuilder = reportBuilder,

            };
            bool success = CredentialHelper.InitializeCreds(ref eventArgs, out credSettings, out initializeCredsError);
            if (!success)
                return (int)FileManagerResponseCodes.InitializeCredsError;

            reportBuilder.AppendLine("Begin SyncBox Rename...");
            responseCode = RenameSyncBox(eventArgs);
            if (responseCode == 0)
                reportBuilder.AppendLine("Successfully Exiting Rename SyncBox...");
            else
                reportBuilder.AppendLine(string.Format("There was an error Renaming SyncBox: {0}", renameTask.ServerID));
           

            return responseCode;
        }

        public static int RunCompareSyncResults(InputParams paramSet, SmokeTask smokeTask, ref StringBuilder reportBuilder, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            int responseCode = 0;
            Comparison comparisonTask = smokeTask as Comparison;
            if (comparisonTask == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;


            ICLCredentialSettings credSettings;
            CLError initializeCredsError;
            TaskEventArgs eventArgs = new TaskEventArgs()
            {
                CurrentTask = smokeTask,
                ParamSet = paramSet,
                ProcessingErrorHolder = ProcessingErrorHolder,
                ReportBuilder = reportBuilder,
            };
            bool success = CredentialHelper.InitializeCreds(ref eventArgs, out credSettings, out initializeCredsError);
            if (!success)
                return (int)FileManagerResponseCodes.InitializeCredsError;

            reportBuilder.AppendLine("Begin SyncBox Compare Sync Results Task...");
            bool areIdentical = CompareSyncBoxFolders(eventArgs);
            reportBuilder.AppendLine(string.Format("SyncBox content is identical: {0}", areIdentical.ToString()));
            reportBuilder.AppendLine("Ending Compare SyncBoxFolders...");
            return responseCode;
        }

        public static long? AddNewSyncBox(InputParams paramSet, CreateSyncBox createBoxTask, ref GenericHolder<CLError> ProcessingExceptionHolder)
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

        public static int InitilizeSyncBox(SmokeTestManagerEventArgs e,  out CLSyncBox syncBox)
        {
            int initResponse = 0;
            GenericHolder<CLError> refHolder = e.ProcessingErrorHolder;
            CLSyncBoxCreationStatus boxCreateStatus;
            ICLSyncSettings syncSettings = new AdvancedSyncSettings(e.ParamSet.ManualSync_Folder.Replace("\"", ""));
            long syncBoxId = SyncBoxMapper.SyncBoxes.Count > 0 ? SyncBoxMapper.SyncBoxes[0] : e.ParamSet.ManualSyncBoxID;
            CLError initSyncBoxError = CLSyncBox.CreateAndInitialize(e.Creds, syncBoxId, out syncBox, out boxCreateStatus, syncSettings);
            if (initSyncBoxError != null || boxCreateStatus != CLSyncBoxCreationStatus.Success)
            {
                ManualSyncManager.HandleFailure(initSyncBoxError, null, boxCreateStatus, "SyncBoxManager.RunSyncboxRename - Init Box Failure", ref refHolder);
                return (int)FileManagerResponseCodes.InitializeSynBoxError;
            }
            return initResponse;
        }

        private static int RenameSyncBox(TaskEventArgs eventArgs)
        {
            Rename renameTask = eventArgs.CurrentTask as Rename;
            if (renameTask == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;
            GenericHolder<CLError> refHolder = eventArgs.ProcessingErrorHolder;
            CLSyncBox syncBox;
            CLSyncBoxCreationStatus boxCreateStatus;
            ICLSyncSettings syncSettings = new AdvancedSyncSettings(eventArgs.ParamSet.ManualSync_Folder.Replace("\"", ""));
            CLError initSyncBoxError = CLSyncBox.CreateAndInitialize(eventArgs.Creds, renameTask.ServerID, out syncBox, out boxCreateStatus, syncSettings);
            if (initSyncBoxError != null || boxCreateStatus != CLSyncBoxCreationStatus.Success)
            {
                ManualSyncManager.HandleFailure(initSyncBoxError, null, boxCreateStatus, "SyncBoxManager.RunSyncboxRename - Init Box Failure", ref refHolder);
                return (int)FileManagerResponseCodes.InitializeSynBoxError;
            }
            eventArgs.boxCreationStatus = boxCreateStatus;
            eventArgs.SyncBox = syncBox;
            CLHttpRestStatus restStatus;
            CloudApiPublic.JsonContracts.SyncBoxHolder responseHolder;
            CLError updateBoxError = syncBox.UpdateSyncBox(renameTask.NewName, ManagerConstants.TimeOutMilliseconds, out restStatus, out responseHolder);
            if (updateBoxError != null || restStatus != CLHttpRestStatus.Success)
            {
                ManualSyncManager.HandleFailure(updateBoxError, restStatus, null, "SyncBoxManger.RunSyncBoxRename - Update Box Failure", ref refHolder);
                return (int)FileManagerResponseCodes.UnknownError;
            }
            return 0;
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

        public static bool CompareSyncBoxFolders(TaskEventArgs e)
        {
            bool areItemsItedntical = false;
            string manualSyncFolderPath = e.ParamSet.ManualSync_Folder.Replace("\"", "");
            string activeSyncFolderPath = e.ParamSet.ActiveSync_Folder.Replace("\"","");
            DirectoryInfo activeFolder = new DirectoryInfo(activeSyncFolderPath);
            DirectoryInfo manualFolder = new DirectoryInfo(manualSyncFolderPath);
            FileSystemInfo[] activeFolderStructure = activeFolder.GetFileSystemInfos();
            FileSystemInfo[] manualFolderStructure = manualFolder.GetFileSystemInfos();
            foreach (FileSystemInfo fileOrFolder in activeFolderStructure)
            { 
                string comparePath = fileOrFolder.FullName.Replace(activeSyncFolderPath, manualSyncFolderPath);
                IEnumerable<FileSystemInfo> compareTo = manualFolderStructure.Where(fso => fso.FullName.Contains(comparePath));
                if (compareTo.Count() == 0 || compareTo.Count() > 1)
                {
                    e.ReportBuilder.Append(string.Format("The comparison for {0} failed due to an unexpected count of {1}", comparePath, compareTo.Count().ToString()));
                    areItemsItedntical = false;
                    break;
                }
                FileSystemInfo manualCompare = compareTo.ElementAt(0);
                FileInfo manualFileItem = manualCompare as FileInfo;
                // If its not a folder 
                if (manualFileItem != null)
                {
                    areItemsItedntical = CompareFiles(manualFileItem, fileOrFolder);
                }
                else
                { 
                    DirectoryInfo manualFolderItem = manualCompare as DirectoryInfo;
                    areItemsItedntical = CompareFolders(manualFolderItem, fileOrFolder);
                }
                //For folders do not Compare modified date 
                //For All Others Compare:
                //Creation Date 
                //Last Modified Date
                //FileSize 
            }
            return areItemsItedntical;
        }

        #endregion

        #region Interface Implementation
        public int Create(SmokeTestManagerEventArgs e)
        {
            int createResponseCode = 0;
            GenericHolder<CLError> refHolder = e.ProcessingErrorHolder;
            long? newBoxId = 0;
            CreateSyncBox createTask = e.CurrentTask as CreateSyncBox;
            if (createTask != null && createTask.CreateNew == true)
            {
                Console.WriteLine("Preapring to create new SyncBoxs.");
                int iterations = 1;
                if (createTask.Count > 0)
                    iterations = createTask.Count;
                for (int x = 0; x < iterations; x++)
                {
                    newBoxId = SyncBoxManager.AddNewSyncBox(e.ParamSet, createTask, ref refHolder);
                    if (newBoxId == (long)0)
                    {
                        createResponseCode = (int)FileManagerResponseCodes.InitializeSynBoxError;
                        ExceptionManagerEventArgs failArgs = new ExceptionManagerEventArgs()
                        {
                             OpperationName= "SyncBoxManager.CreateSyncBox",
                              ProcessingErrorHolder = e.ProcessingErrorHolder,
                               Error = new Exception("There was an error creating a new Sync Box."),

                        };
                        SmokeTaskManager.HandleFailure(failArgs);
                        
                    }
                    else
                    {
                        Console.WriteLine(string.Format("Successfully Created SyncBox with ID: {0}", newBoxId));
                        SyncBoxMapper.SyncBoxes.Add(SyncBoxMapper.SyncBoxes.Count(), newBoxId.Value);
                    }
                }
            }
            else
            {
                SyncBoxMapper.SyncBoxes.Add(SyncBoxMapper.SyncBoxes.Count(), e.ParamSet.ManualSyncBoxID);
                newBoxId = e.ParamSet.ManualSyncBoxID;
            }
            //ZW Replace
            //e.CurrentTask.SyncBoxes.Add(newBoxId.Value, null);
            return createResponseCode;
            //return (int)RunCreateSyncBoxTask(e.ParamSet, e.CurrentTask,ref refHolder); 
        }

        public int Rename(SmokeTestManagerEventArgs e)
        {
            GenericHolder<CLError> refHolder = e.ProcessingErrorHolder;
            StringBuilder refReportBuilder = e.ReportBuilder;
            return (int)RunSyncBoxRenameTask(e.ParamSet, e.CurrentTask, ref refReportBuilder, ref refHolder); 
        }

        public int Delete(SmokeTestManagerEventArgs e)
        {
            GenericHolder<CLError> refHolder = e.ProcessingErrorHolder;
            StringBuilder sb = e.ReportBuilder;
            return (int)RunSyncBoxDeletionTask(e.ParamSet, e.CurrentTask, ref sb, ref refHolder); 
        }

        public int UnDelete(SmokeTestManagerEventArgs e)
        {
            GenericHolder<CLError> refHolder = e.ProcessingErrorHolder;
            Exception ex = new NotImplementedException("Can Not UnDelete a SyncBox");
            AddException(ex, ref refHolder);
            return (int)FileManagerResponseCodes.InvalidTaskType;
        }

        public int Download(SmokeTestManagerEventArgs e)
        {
            GenericHolder<CLError> refHolder = e.ProcessingErrorHolder;
            Exception ex = new NotImplementedException("Can Not UnDelete a SyncBox");
            AddException(ex, ref refHolder);
            return (int)FileManagerResponseCodes.InvalidTaskType;
        }

        public int ListItems(SmokeTestManagerEventArgs e)
        {
            throw new NotImplementedException("Implement List Items Methodology for SyncBoxes");
        }
        #endregion 

        #region Private
        private static bool CompareFiles(FileInfo manualFile, FileSystemInfo fso)
        {
            bool areIdentical = true;
            FileInfo fi = fso as FileInfo;
            
            if (fi == null)
                return false;
            if (manualFile.LastWriteTimeUtc != fi.LastWriteTimeUtc)
                areIdentical = false;
            else if (manualFile.CreationTimeUtc != fi.CreationTimeUtc)
                areIdentical = false;
            else if (fi.Length != manualFile.Length)
                areIdentical = false;

            return areIdentical;
        }

        private static bool CompareFolders(DirectoryInfo manualDirectory, FileSystemInfo fso)
        {
            bool areIdentical = true;

            DirectoryInfo di = fso as DirectoryInfo;
            if(di == null)
                return false;
            if (manualDirectory.CreationTimeUtc != di.CreationTimeUtc)
                areIdentical = false;

            return areIdentical;
        }

        private static void AddExceptions(CLCredentialCreationStatus? credsStatus, CLHttpRestStatus? restStatus, CLSyncBoxCreationStatus? boxCreateStatus, string opperationName, CLError error, ref GenericHolder<CLError> ProcessingErrorHolder)
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
            if (boxCreateStatus != null && boxCreateStatus != CLSyncBoxCreationStatus.Success)
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
