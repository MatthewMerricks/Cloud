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
using System.IO;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Managers
{
    public class SyncBoxManager : ISmokeTaskManager
    {
        #region Public
        //public static int RunCreateSyncBoxTask(InputParams paramSet, ref SmokeTask smokeTask, ref StringBuilder reportBuilder, ref GenericHolder<CLError> ProcessingErrorHolder)
        //{
        //    int responseCode = 0;
        //    long? newBoxId =0;
        //    CreateSyncBox createTask = smokeTask as CreateSyncBox;
        //    if (createTask != null && createTask.CreateNew == true)
        //    {
        //        reportBuilder.AppendLine("Preapring to create new SyncBoxs.");
        //        int iterations = 1;
        //        if (createTask.Count > 0)
        //            iterations = createTask.Count;
        //        for (int x = 0; x < iterations; x++)
        //        {
        //            newBoxId = SyncBoxManager.AddNewSyncBox(paramSet, createTask, ref ProcessingErrorHolder);
        //            if (newBoxId == (long)0)
        //            {
        //                Exception newSyncBoxException = new Exception("There was an error creating a new Sync Box.");
        //                lock (ProcessingErrorHolder)
        //                {
        //                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + newSyncBoxException;
        //                }
        //                responseCode = (int)FileManagerResponseCodes.InitializeSynBoxError;
        //            }
        //            else
        //            {
        //                reportBuilder.AppendLine(string.Format("Successfully Created SyncBox with ID: {0}", newBoxId));
        //                SyncBoxMapper.SyncBoxes.Add(SyncBoxMapper.SyncBoxes.Count(), newBoxId.Value);
        //                createTask.SelectedSyncBoxID = newBoxId.Value;
        //            }
        //        }
        //    }
        //    else
        //    {
        //        SyncBoxMapper.SyncBoxes.Add(SyncBoxMapper.SyncBoxes.Count(), paramSet.ManualSyncBoxID);
        //        newBoxId = paramSet.ManualSyncBoxID;
        //    }
        //    return responseCode;//newBoxId.HasValue ? newBoxId.Value : 0;
        //}

        //public static int RunSyncBoxDeletionTask(InputParams paramSet, SmokeTask smokeTask, ref StringBuilder reportBuilder, ref GenericHolder<CLError> ProcessingErrorHolder)
        //{
        //    int deleteTaskResponseCode = 0;
        //    Deletion deleteTask = smokeTask as Deletion;
        //    if (deleteTask == null)
        //        return (int)FileManagerResponseCodes.InvalidTaskType;
        //    long syncBoxId = 0;
        //    if (deleteTask.ID > 0)
        //        syncBoxId = deleteTask.ID;
        //    reportBuilder.AppendLine(string.Format("Preparing to Delete SyncBox Task with ID: {0}", syncBoxId));
        //    bool success = SyncBoxManager.DeleteSyncBox(paramSet, syncBoxId, ref ProcessingErrorHolder);
        //    if (success)
        //        reportBuilder.AppendLine(string.Format("Successfully Deleted SyncBox {0}.", syncBoxId));
        //    if (!success)
        //    {
        //        reportBuilder.AppendLine(string.Format("There was an Issue Deleting SyncBox {0}.", syncBoxId));
        //        deleteTaskResponseCode = -1;
        //    }
        //    reportBuilder.AppendLine("Exiting Delete SyncBox Task");
        //    return deleteTaskResponseCode;
        //}

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
            //bool areIdentical = CompareSyncBoxFolders(eventArgs);
            //reportBuilder.AppendLine(string.Format("SyncBox content is identical: {0}", areIdentical.ToString()));
            reportBuilder.AppendLine("Ending Compare SyncBoxFolders...");
            return responseCode;
        }

        public static long? AddNewSyncBox(SmokeTestManagerEventArgs e, CreateSyncBox createBoxTask, ref GenericHolder<CLError> ProcessingExceptionHolder)
        {
            long? syncBoxId = null;
            CLCredential creds; CLCredentialCreationStatus credsStatus;
            string token = null;
            if(!string.IsNullOrEmpty(e.ParamSet.Token))
                token = e.ParamSet.Token;
            CLError error = CLCredential.CreateAndInitialize(e.ParamSet.API_Key, e.ParamSet.API_Secret, out creds, out credsStatus, token);
            if (credsStatus != CLCredentialCreationStatus.Success || error != null)
            {
                AddExceptions(credsStatus, null, null, "SyncBox Manager Add New SyncBox", error, ref ProcessingExceptionHolder);
                return null;
            }

            syncBoxId = CreateNewSyncBoxAndAddToDictionary(e, creds, ref ProcessingExceptionHolder);
            return syncBoxId;
        }

        public static int InitilizeSyncBox(SmokeTestManagerEventArgs e, long? syncBoxId,  out CLSyncBox syncBox)
        {
            int initResponse = 0;
            GenericHolder<CLError> refHolder = e.ProcessingErrorHolder;
            CLSyncBoxCreationStatus boxCreateStatus;
            ICLSyncSettings syncSettings = new AdvancedSyncSettings(e.ParamSet.ManualSync_Folder.Replace("\"", ""));

            //If the Id passed in is null or Zero Check the task object for the selected Id
            if (syncBoxId.HasValue == false || syncBoxId.Value == 0)
                syncBoxId = e.CurrentTask.SelectedSyncBoxID;
            
            //If the SyncBoxId was not assigned in the event arguments, determine a syncboxId using last resort
            if (syncBoxId.HasValue == false || syncBoxId.Value == 0)
                syncBoxId = SyncBoxMapper.SyncBoxes.Count > 0 ? SyncBoxMapper.SyncBoxes[0] : e.ParamSet.ManualSyncBoxID;
            
            CLError initSyncBoxError = CLSyncBox.CreateAndInitialize(e.Creds, syncBoxId.Value, out syncBox, out boxCreateStatus, syncSettings);
            e.boxCreationStatus = boxCreateStatus;
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

        //public static bool DeleteSyncBox(InputParams paramSet, long syncBoxId, ref GenericHolder<CLError> ProcessingExceptionHolder)
        //{
        //    bool success = false;
        //    CLCredential creds; CLCredentialCreationStatus credsStatus;
        //    string token = null;
        //    if (!string.IsNullOrEmpty(paramSet.Token))
        //        token = paramSet.Token;
        //    CLError error = CLCredential.CreateAndInitialize(paramSet.API_Key, paramSet.API_Secret, out creds, out credsStatus, token);
        //    if (credsStatus != CLCredentialCreationStatus.Success || error != null)
        //    {
        //        AddExceptions(credsStatus, null, null, "SyncBox Manager Delete SyncBox Init Creds", error, ref ProcessingExceptionHolder);
        //        return success;
        //    }
            
        //    CLSyncBox syncBox; CLSyncBoxCreationStatus boxCreateStatus;
        //    CLError getSyncBoxError = CLSyncBox.CreateAndInitialize(creds, syncBoxId, out syncBox,out boxCreateStatus);
        //    if (getSyncBoxError != null || boxCreateStatus != CLSyncBoxCreationStatus.Success)
        //    {
        //        AddExceptions(null, null, boxCreateStatus, "SyncBox Manager  Delete SyncBox Init SyncBox",getSyncBoxError, ref ProcessingExceptionHolder);
        //        return success;
        //    }

        //    CLHttpRestStatus restStatus;
        //    CloudApiPublic.JsonContracts.SyncBoxHolder response;
        //    CLError deleteError = syncBox.DeleteSyncBox(ManagerConstants.TimeOutMilliseconds, out restStatus, out response);
        //    if (deleteError != null || restStatus != CLHttpRestStatus.Success)
        //    { 
        //        AddExceptions(null, restStatus, null, "SyncBox Manager Delete SyncBox Delete Method", deleteError, ref ProcessingExceptionHolder);
        //    }
        //    try
        //    {
        //        ItemsListManager mgr = ItemsListManager.GetInstance();
        //        CloudApiPublic.JsonContracts.SyncBox toRemove = mgr.SyncBoxes.Where(sb => sb.Id == syncBoxId).FirstOrDefault();
        //        if (mgr.SyncBoxes.Contains(toRemove))
        //            mgr.SyncBoxes.Remove(toRemove);
        //    }
        //    catch (Exception exception)
        //    {
        //        lock (ProcessingExceptionHolder)
        //        {
        //            ProcessingExceptionHolder.Value = ProcessingExceptionHolder.Value + exception;
        //        }
        //    }
            
        //    return success;
        //}

        public static bool CompareSyncBoxFolders(SmokeTestManagerEventArgs e)
        {
            bool areItemsItedntical = false;
            StringBuilder report = e.ReportBuilder;
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
                    report.AppendLine(string.Format("The comparison for {0} failed due to an unexpected count of {1}", comparePath, compareTo.Count().ToString()));
                    areItemsItedntical = false;
                    break;
                }
                FileSystemInfo manualCompare = compareTo.ElementAt(0);
                FileInfo manualFileItem = manualCompare as FileInfo;
                // If its not a folder 
                if (manualFileItem != null)
                {
                    areItemsItedntical = CompareFiles(manualFileItem, fileOrFolder, ref report);
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
            StringBuilder newBuilder = new StringBuilder(); 
            int createResponseCode = 0;
            GenericHolder<CLError> refHolder = e.ProcessingErrorHolder;
            long? newBoxId = 0;
            CreateSyncBox createTask = e.CurrentTask as CreateSyncBox;
            if (createTask == null)
            {
                Creation create = e.CurrentTask as Creation;
                if (create != null)
                    createTask = new CreateSyncBox()
                    {
                        Count = create.Count,
                        CreateNew = create.CreateNew,
                        ObjectType = create.ObjectType,
                        SelectedSyncBoxID = create.SelectedSyncBoxID,
                        InnerTask = create.InnerTask,
                        SyncType = create.SyncType,
                        type = create.type,
                    };
            }
            if (createTask != null && createTask.CreateNew == true)
            {
                newBuilder.AppendLine("Preapring to create new SyncBoxs.");
                int iterations = 1;
                if (createTask.Count > 0)
                    iterations = createTask.Count;
                for (int x = 0; x < iterations; x++)
                {
                    newBoxId = SyncBoxManager.AddNewSyncBox(e, createTask, ref refHolder);
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
                        newBuilder.AppendLine(string.Format("Successfully Created SyncBox with ID: {0}", newBoxId));
                        SyncBoxMapper.SyncBoxes.Add(SyncBoxMapper.SyncBoxes.Count(), newBoxId.Value);
                        e.CurrentTask.SelectedSyncBoxID = newBoxId.Value;
                    }
                }
            }
            else
            {
                SyncBoxMapper.SyncBoxes.Add(SyncBoxMapper.SyncBoxes.Count(), e.ParamSet.ManualSyncBoxID);
                newBoxId = e.ParamSet.ManualSyncBoxID;
            }
            e.StringBuilderList.Add(new StringBuilder(newBuilder.ToString()));
            return createResponseCode;
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
            StringBuilder newBuilder = new StringBuilder();
            int deleteTaskResponseCode = 0;
            Deletion deleteTask = e.CurrentTask as Deletion;
            if (deleteTask == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;

            int iterations = 1;
            if (deleteTask.DeleteCount > 0)
                iterations = deleteTask.DeleteCount;
            long syncBoxId = 0;
            if (deleteTask.ID > 0)
                syncBoxId = deleteTask.ID;
            
            int success = BeginDelete(iterations, e);

            newBuilder.AppendLine("Exiting Delete SyncBox Task");
            e.StringBuilderList.Add(new StringBuilder(newBuilder.ToString()));
            return deleteTaskResponseCode;
        }

        #region Private Delete
        public int BeginDelete(int count, SmokeTestManagerEventArgs e)
        {
            StringBuilder newBuilder = new StringBuilder();
            int responseCode = 0;
            ICLCredentialSettings settings;
            CLError credError;
            TaskEventArgs taskArgs = (e as TaskEventArgs);
            CredentialHelper.InitializeCreds(ref taskArgs, out settings, out credError);
            if (credError != null || e.CredsStatus != CLCredentialCreationStatus.Success)
            { 
                ExceptionManagerEventArgs failArgs = new ExceptionManagerEventArgs(){ Error = credError, ProcessingErrorHolder = e.ProcessingErrorHolder, OpperationName = "SyncBoxManager.BeginDelete.InitializeCreds"};
                SmokeTaskManager.HandleFailure(failArgs);
                return (int)FileManagerResponseCodes.InitializeCredsError;
            }

            CLSyncBox syncBox;
            ItemsListManager mgr = ItemsListManager.GetInstance();
            List<SyncBox> toDelete = new List<SyncBox>(mgr.SyncBoxes.Take(count));
            
            foreach (SyncBox box in toDelete)
            {
                InitilizeSyncBox(e, box.Id, out syncBox);
                e.SyncBox = syncBox;
                if (e.boxCreationStatus != CLSyncBoxCreationStatus.Success)
                {
                    Exception ex = ExceptionManager.ReturnException("Error Initializing SyncBox", e.boxCreationStatus.ToString());
                    CLError error = new CLError();
                    error.AddException(ex);
                    ExceptionManagerEventArgs failArgs = new ExceptionManagerEventArgs() { Error = error, OpperationName = "SyncBoxManager.BeginDelete", ProcessingErrorHolder = e.ProcessingErrorHolder, SyncBoxCreateStatus = e.boxCreationStatus };
                    return (int)FileManagerResponseCodes.InitializeSynBoxError;
                }
                else
                {
                    newBuilder.AppendLine(string.Format("Preparing to Delete SyncBox Task with ID: {0}", box.Id));
                    responseCode = ExecuteDelete(e, mgr, newBuilder);
                }
            }
            e.StringBuilderList.Add(new StringBuilder(newBuilder.ToString()));
            return responseCode;
        }

        public int ExecuteDelete(SmokeTestManagerEventArgs e, ItemsListManager mgr, StringBuilder newBuilder)
        {
            int responseCode = 0;
            CLHttpRestStatus restStatus;
            CloudApiPublic.JsonContracts.SyncBoxHolder response;
            CLError deleteError = e.SyncBox.DeleteSyncBox(ManagerConstants.TimeOutMilliseconds, out restStatus, out response);
            if (deleteError != null || restStatus != CLHttpRestStatus.Success)
            {
                ExceptionManagerEventArgs failArgs = new ExceptionManagerEventArgs()
                {
                    Error = deleteError,
                    OpperationName = "SyncBoxManager.ExecuteDelete",
                    RestStatus = restStatus,
                    ProcessingErrorHolder = e.ProcessingErrorHolder
                };
                SmokeTaskManager.HandleFailure(failArgs);
                responseCode = (int)FileManagerResponseCodes.UnknownError;
            }
            else
            { 
                newBuilder.AppendLine(string.Format("Successfully Deleted SyncBox ID: {0}", e.SyncBox.SyncBoxId ));
            }

            //Remove Deleted Items from ItemsManagerList if they currently exist there
            try
            {
                CloudApiPublic.JsonContracts.SyncBox toRemove = mgr.SyncBoxes.Where(sb => sb.Id == e.SyncBox.SyncBoxId).FirstOrDefault();
                if (mgr.SyncBoxes.Contains(toRemove))
                    mgr.SyncBoxes.Remove(toRemove);
            }
            catch (Exception exception)
            {
                responseCode = (int)FileManagerResponseCodes.UnknownError;
                lock (e.ProcessingErrorHolder)
                {
                    e.ProcessingErrorHolder.Value = e.ProcessingErrorHolder.Value + exception;
                }
            }
            return responseCode;
        }
        #endregion 

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
            Exception ex = new NotImplementedException("Can Not Download a SyncBox");
            AddException(ex, ref refHolder);
            return (int)FileManagerResponseCodes.InvalidTaskType;
        }

        public int ListItems(SmokeTestManagerEventArgs e)
        {
            StringBuilder newBuilder = new StringBuilder(); 
            int getListResponseCode = 0;
            ListItems listItemsTask = e.CurrentTask as ListItems;
            if (listItemsTask == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;
            //Initiate Credentials 
            ICLCredentialSettings settings = new AdvancedSyncSettings(e.ParamSet.ManualSync_Folder.Replace("\"", ""));
            CLError initializeCredsError;
            TaskEventArgs args = (e as TaskEventArgs);
            bool success = CredentialHelper.InitializeCreds(ref args, out settings, out initializeCredsError);
            if (!success)
                return (int)FileManagerResponseCodes.InitializeCredsError;

            //Initiate SyncBox List.
            CLHttpRestStatus restStatus;
            ListSyncBoxes syncBoxList;
            GenericHolder<CLError> refHolder = e.ProcessingErrorHolder;
            CLError getSyncBoxesError = e.Creds.ListSyncBoxes(ManagerConstants.TimeOutMilliseconds, out restStatus, out syncBoxList, settings);
            if (getSyncBoxesError != null || restStatus != CLHttpRestStatus.Success)
            {
                ExceptionManagerEventArgs failArgs = new ExceptionManagerEventArgs() { RestStatus = restStatus, ProcessingErrorHolder = e.ProcessingErrorHolder, OpperationName = "SyncBoxManager.ListItems ListSyncBoxes", Error = getSyncBoxesError };
                SmokeTaskManager.HandleFailure(failArgs);
                return (int)FileManagerResponseCodes.UnknownError;
            }

            //Compare Results to Expected Results 
            if (listItemsTask.ExpectedCount > 0)
            {
                if (syncBoxList.SyncBoxes.Count() != listItemsTask.ExpectedCount)
                    return (int)FileManagerResponseCodes.ExpectedItemMatchFailure;
            }

            //Add List Items To Manager 
            ItemsListManager listManager = ItemsListManager.GetInstance();
            listManager.SyncBoxes.Clear();
            if (syncBoxList.SyncBoxes.Count() == 0)
                newBuilder.AppendLine("There are no SyncBoxes To List.");
            else
            {
                foreach (CloudApiPublic.JsonContracts.SyncBox syncBox in syncBoxList.SyncBoxes)
                {
                    listManager.SyncBoxes.Add(syncBox);
                    newBuilder.AppendLine(string.Format("The SyncBox Name:{0} with ID:{1} and PlanID:{2} was retrieved and added to ItemsListManager's List of SyncBoxes", syncBox.FriendlyName, syncBox.Id, syncBox.PlanId));
                    getListResponseCode = 0;
                }
            }
            e.StringBuilderList.Add(new StringBuilder(newBuilder.ToString()));
            return getListResponseCode;
        }
        #endregion 

        #region Private
        private static bool CompareFiles(FileInfo manualFile, FileSystemInfo fso, ref StringBuilder reportBuilder)
        {
            bool areIdentical = true;
            FileInfo fi = fso as FileInfo;
            
            if (fi == null)
                return false;
            if (manualFile.LastWriteTime.Date != fi.LastWriteTime.Date)
            {
                areIdentical = false;
                reportBuilder.AppendLine("File Last Written Date Mismatch:");
                reportBuilder.AppendLine(string.Format("     {0} Date {1}", manualFile.FullName, manualFile.LastWriteTime.Date));
                reportBuilder.AppendLine(string.Format("     {0} Date {1}", fi.FullName, fi.LastWriteTime.Date));
            }
            if (manualFile.LastWriteTime.Hour != fi.LastWriteTime.Hour)
            {
                areIdentical = false;
                reportBuilder.AppendLine("File Last Written Time Mismatch:");
                reportBuilder.AppendLine(string.Format("     {0} Hour {1}", manualFile.FullName, manualFile.LastWriteTime.Hour));
                reportBuilder.AppendLine(string.Format("     {0} Hour {1}", fi.FullName, fi.LastWriteTime.Hour));
            }
            if(manualFile.LastWriteTime.Minute != fi.LastWriteTime.Minute)
            {
                areIdentical = false;
                reportBuilder.AppendLine("File Last Written Time Mismatch:");
                reportBuilder.AppendLine(string.Format("     {0} Minute {1}", manualFile.FullName, manualFile.LastWriteTime.Minute));
                reportBuilder.AppendLine(string.Format("     {0} Minute {1}", fi.FullName, fi.LastWriteTime.Minute));
            }
            if(manualFile.LastWriteTime.Second != fi.LastWriteTime.Second)
            {
                areIdentical = false;
                reportBuilder.AppendLine("File Last Written Time Mismatch:");
                reportBuilder.AppendLine(string.Format("     {0}  Second {1}", manualFile.FullName, manualFile.LastWriteTime.Minute));
                reportBuilder.AppendLine(string.Format("     {0}  Second {1}", fi.FullName, fi.LastWriteTime.Minute));
            }

            if (manualFile.CreationTime.Date != fi.CreationTime.Date)
            { 
                areIdentical = false;
                reportBuilder.AppendLine("File Creation Date Mismatch:");
                reportBuilder.AppendLine(string.Format("     {0} Date {1}", manualFile.FullName, manualFile.CreationTime.Date));
                reportBuilder.AppendLine(string.Format("     {0} Date {1}", fi.FullName, fi.CreationTime.Date));
            }
            if (manualFile.CreationTime.Hour != fi.CreationTime.Hour)
            { 
                areIdentical = false;
                reportBuilder.AppendLine("File Creation Time Mismatch:");
                reportBuilder.AppendLine(string.Format("     {0} Hour {1}", manualFile.FullName, manualFile.CreationTime.Hour));
                reportBuilder.AppendLine(string.Format("     {0} Hour {1}", fi.FullName, fi.CreationTime.Hour));
            }
            if (manualFile.CreationTime.Minute != fi.CreationTime.Minute)
            {                
                areIdentical = false;
                reportBuilder.AppendLine("File Creation Time Mismatch:");
                reportBuilder.AppendLine(string.Format("     {0} Minute {1}", manualFile.FullName, manualFile.CreationTime.Minute));
                reportBuilder.AppendLine(string.Format("     {0} Minute {1}", fi.FullName, fi.CreationTime.Minute));
            }
            if(manualFile.CreationTime.Second != fi.CreationTime.Second)
            {
                areIdentical = false;
                reportBuilder.AppendLine("File Creation Time Mismatch:");
                reportBuilder.AppendLine(string.Format("     {0} Second {1}", manualFile.FullName, manualFile.CreationTime.Second));
                reportBuilder.AppendLine(string.Format("     {0} Second {1}", fi.FullName, fi.CreationTime.Second));
            }
            if (fi.Length != manualFile.Length)
            {
                areIdentical = false;
                reportBuilder.AppendLine("File Size Mismatch:");
                reportBuilder.AppendLine(string.Format("     {0} Size {1}", manualFile.FullName, manualFile.Length));
                reportBuilder.AppendLine(string.Format("     {0} Size {1}", fi.FullName, fi.Length));
            }
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

        private static long? CreateNewSyncBoxAndAddToDictionary(SmokeTestManagerEventArgs e, CloudApiPublic.CLCredential creds, ref GenericHolder<CLError> ProcessingExceptionHolder)
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
