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
        #region Public Static
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
                syncBoxId = e.ParamSet.ManualSyncBoxID;
                //syncBoxId = SyncBoxMapper.SyncBoxes.Count > 0 ? SyncBoxMapper.SyncBoxes[0] : e.ParamSet.ManualSyncBoxID;
            
            CLError initSyncBoxError = CLSyncBox.CreateAndInitialize(e.Creds, syncBoxId.Value, out syncBox, out boxCreateStatus, syncSettings);
            e.boxCreationStatus = boxCreateStatus;
            if (initSyncBoxError != null || boxCreateStatus != CLSyncBoxCreationStatus.Success)
            {
                ManualSyncManager.HandleFailure(initSyncBoxError, null, boxCreateStatus, "SyncBoxManager.RunSyncboxRename - Init Box Failure", ref refHolder);
                return (int)FileManagerResponseCodes.InitializeSynBoxError;
            }
            return initResponse;
        }

        public static int CompareSyncBoxFolders(SmokeTestManagerEventArgs e)
        {
            Comparison compareTask = e.CurrentTask as Comparison;
            if(compareTask == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;
            bool areItemsItdentical = false;
            StringBuilder report = new StringBuilder();
            string manualSyncFolderPath = e.ParamSet.ManualSync_Folder.Replace("\"", "");
            string activeSyncFolderPath = e.ParamSet.ActiveSync_Folder.Replace("\"","");
            
            DirectoryInfo activeFolder = new DirectoryInfo(activeSyncFolderPath);
            DirectoryInfo manualFolder = new DirectoryInfo(manualSyncFolderPath);
            

            if (!activeFolder.Exists || !manualFolder.Exists)
                return (int)FileManagerResponseCodes.UnknownError;
            if (compareTask.SyncType == SmokeTaskSyncType.Manual)
                areItemsItdentical = SyncBoxManager.CompareLocalFileStructure(activeFolder, manualFolder, ref report);
            else if (compareTask.SyncType == SmokeTaskSyncType.Active && compareTask.ComparisonType == ComparisonComparisonType.ActiveToServer)
            {
                System.Threading.Thread.Sleep(ManagerConstants.WaitMillisecondsBeforeCompare);
                if (e.SyncBox == null)
                    e.SyncBox = SyncBoxManager.InitializeCredentialsAndSyncBox(e);
                if (e.SyncBox == null)
                    return (int)FileManagerResponseCodes.InitializeSynBoxError;                   
                areItemsItdentical = SyncBoxManager.CompareLocalDirectoryToServerHierarchy(e.SyncBox, activeFolder);
            }
            else if (compareTask.SyncType == SmokeTaskSyncType.Active && compareTask.ComparisonType == ComparisonComparisonType.ActiveToActive)
            {
                System.Threading.Thread.Sleep(ManagerConstants.WaitMillisecondsBeforeCompare);
                string secondActiveSyncFolderPath = e.ParamSet.ActiveSync_Folder2.Replace("\"", "");
                DirectoryInfo secondActiveFolder = new DirectoryInfo(secondActiveSyncFolderPath);
                areItemsItdentical = SyncBoxManager.CompareLocalFileStructure(activeFolder, secondActiveFolder, ref report);
            }
            return areItemsItdentical == true ? 0 : (int)FileManagerResponseCodes.ExpectedItemMatchFailure;
        }

        public static bool CompareLocalFileStructure(DirectoryInfo initalDirectory, DirectoryInfo comparisonDirectory, ref StringBuilder report)
        {
            bool areItemsIdentical = true;
            FileSystemInfo[] activeFolderStructure = initalDirectory.GetFileSystemInfos();
            FileSystemInfo[] manualFolderStructure = comparisonDirectory.GetFileSystemInfos();
            foreach (FileSystemInfo fileOrFolder in activeFolderStructure)
            {
                string comparePath = fileOrFolder.FullName.Replace(initalDirectory.FullName, comparisonDirectory.FullName);
                IEnumerable<FileSystemInfo> compareTo = manualFolderStructure.Where(fso => fso.FullName.Contains(comparePath));
                if (compareTo.Count() == 0 || compareTo.Count() > 1)
                {
                    report.AppendLine(string.Format("The comparison for {0} failed due to an unexpected count of {1}", comparePath, compareTo.Count().ToString()));
                    areItemsIdentical = false;
                    break;
                }
                FileSystemInfo manualCompare = compareTo.ElementAt(0);
                FileInfo manualFileItem = manualCompare as FileInfo;
                DirectoryInfo manualFolderItem = null;
                // If its not a folder 
                if (manualFileItem != null)
                {
                    areItemsIdentical = CompareLocalFiles(manualFileItem, fileOrFolder, ref report);
                }
                else
                {
                    manualFolderItem = manualCompare as DirectoryInfo;
                    areItemsIdentical = CompareFolders(manualFolderItem, fileOrFolder, ref report);
                }

                if (areItemsIdentical)
                {
                    if (manualFileItem == null)
                    {
                        DirectoryInfo comparisonInfoFolder = new DirectoryInfo(manualFolderItem.FullName.Replace(comparisonDirectory.FullName,initalDirectory.FullName));
                        FileSystemInfo[] comparinsonInfoHolder = comparisonInfoFolder.GetFileSystemInfos();
                        FileSystemInfo[] initialInfoHolder = manualFolderItem.GetFileSystemInfos();
                        foreach (FileSystemInfo fsi in initialInfoHolder)
                        {
                            FileInfo secondInitialCompare = fsi as FileInfo;
                            if (secondInitialCompare == null)
                            {
                                DirectoryInfo dirInfo = comparisonInfoFolder.EnumerateFileSystemInfos().Where(i => i.Name == fsi.Name).FirstOrDefault() as DirectoryInfo;
                                DirectoryInfo fsiAsDirectoryInfo = fsi as DirectoryInfo;
                                areItemsIdentical = CompareLocalFileStructure(fsiAsDirectoryInfo, dirInfo, ref report);
                            }
                            else
                            {
                                FileInfo fromCompare = comparinsonInfoHolder.ToList().Where(i => i.Name == secondInitialCompare.Name).FirstOrDefault() as FileInfo;
                                areItemsIdentical = CompareLocalFiles(fromCompare, secondInitialCompare, ref report);
                            }
                        }
                    }
                }
            }
            return areItemsIdentical;
        }

        public static bool CompareLocalDirectoryToServerHierarchy(CLSyncBox syncBox, DirectoryInfo localDirectory)
        {
            bool areStructuresIdentical = true;
            CloudApiPublic.JsonContracts.FolderContents serverContents = GetServerHierarchy(syncBox);
            FileSystemInfo[] localContents = localDirectory.GetFileSystemInfos();
            foreach (Metadata serverItem in serverContents.Objects)
            { 
                if(areStructuresIdentical == false)
                    break;
                string root = string.Empty;
                string localPath = string.Empty;
                if(FileHelper.PathEndsWithSlash(localDirectory.FullName.ToString()))
                {
                    root = localDirectory.FullName.ToString().Remove(localDirectory.FullName.ToString().Count() - 1, 1);
                    localPath = root + serverItem.RelativePath.Replace("/", "\\");
                }
                FileSystemInfo fso = localContents.Where(lci => lci.FullName == localPath).FirstOrDefault();
                if (fso != null)
                {
                    areStructuresIdentical = CompareFileSystemInfoToMetadata(fso, serverItem);
                }
                else
                {
                    break;
                }
                
            }
            return areStructuresIdentical;
        }

        public static bool CompareFileSystemInfoToMetadata(FileSystemInfo fileSystemItem, Metadata metadata)
        {
            bool areItemsIdentical = true;
            FileInfo fileInfo = fileSystemItem as FileInfo;
            if (fileInfo == null)
            {
                DirectoryInfo folderInfo = fileSystemItem as DirectoryInfo;
                if (folderInfo.CreationTime != metadata.CreatedDate)
                    areItemsIdentical = false;
            }
            else
            {
                if (fileInfo.CreationTime != metadata.CreatedDate)
                    areItemsIdentical = false;
                if (fileInfo.LastWriteTime != metadata.ModifiedDate)
                    areItemsIdentical = false;
                if (fileInfo.Length != metadata.Size)
                    areItemsIdentical = false;
            }
            return areItemsIdentical;
        }

        public static FolderContents GetServerHierarchy(CLSyncBox syncBox)
        {
            CLHttpRestStatus restStatus;
            CloudApiPublic.JsonContracts.FolderContents folderContents = new CloudApiPublic.JsonContracts.FolderContents();
            CLError getAllContentError = syncBox.GetFolderContents(ManagerConstants.TimeOutMilliseconds, out restStatus, out folderContents, includeCount: false, contentsRoot: null, depthLimit: 9, includeDeleted: false);
            if (restStatus != CLHttpRestStatus.Success || getAllContentError != null)
            {
                ExceptionManagerEventArgs failArgs = new ExceptionManagerEventArgs()
                {
                    Error = getAllContentError,
                    RestStatus = restStatus,
                    OpperationName = "DownloadManager.BeginGetAllContent(syncBox.GetFolderContents())",
                };
                SmokeTaskManager.HandleFailure(failArgs);
                return null;
            }
            return folderContents;
            
        }

        public static CLSyncBox InitializeCredentialsAndSyncBox(SmokeTestManagerEventArgs e)
        {
            ICLCredentialSettings settings;
            CLError credsError;
            TaskEventArgs taskArgs = new TaskEventArgs() { Creds = e.Creds, ProcessingErrorHolder = e.ProcessingErrorHolder };
            CredentialHelper.InitializeCreds(ref taskArgs, out settings, out credsError);
            if (credsError != null || taskArgs.CredsStatus != CLCredentialCreationStatus.Success)
                return null;

            if (taskArgs.Creds == null)
                return null;

            e.Creds = taskArgs.Creds;
            CLSyncBox syncBox;
            InitilizeSyncBox(e, e.CurrentTask.SelectedSyncBoxID, out syncBox);
            if (e.boxCreationStatus != CLSyncBoxCreationStatus.Success)
                return null;

            return syncBox;

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
                createTask = TransformCreateToCreateSyncBox(e);

            if(createTask == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;

            if (e.Creds == null)
            {
                CLError initError;
                ICLCredentialSettings settings;
                TaskEventArgs ea = new TaskEventArgs(e);
                CredentialHelper.InitializeCreds(ref ea, out settings, out initError);
                if (ea.CredsStatus == CLCredentialCreationStatus.Success && initError == null)
                    e.Creds = ea.Creds;
                else
                    return (int)FileManagerResponseCodes.InitializeCredsError;
            }
            int initialCount;
            int response = ItemsListHelper.GetSyncBoxCount(e, out initialCount);
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
                        e.CurrentTask.SelectedSyncBoxID = newBoxId.Value;
                    }
                }
            }
            else
            {
                newBoxId = e.ParamSet.ManualSyncBoxID;
            }
            int currentCount;
            response = ItemsListHelper.GetSyncBoxCount(e, out currentCount);
            int expectedCount = initialCount + createTask.Count;
            StringBuilder explanation = new StringBuilder(string.Format("SyncBox Count Before Add {0}.", initialCount));
            explanation.AppendLine("Results:");
            explanation.AppendLine(string.Format("Expected Count: {0}", expectedCount.ToString()));
            explanation.AppendLine(string.Format("Actual Count  : {0}", currentCount.ToString()));
            if (currentCount == expectedCount)
                explanation.AppendLine("Successfully Completed Create SyncBox Task");
            else
            {
                explanation.AppendLine("Create Sync Box Task was Expecting a different result.");
                createResponseCode = (int)FileManagerResponseCodes.ExpectedItemMatchFailure;
            }

            newBuilder.AppendLine(explanation.ToString());
            e.StringBuilderList.Add(new StringBuilder(newBuilder.ToString()));
            return createResponseCode;
        }

        #region Rename
        public int Rename(SmokeTestManagerEventArgs e)
        {
            StringBuilder reportBuilder = new StringBuilder();
            int responseCode = 0;
            Rename renameTask = e.CurrentTask as Rename;
            if (renameTask == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;


            ICLCredentialSettings credSettings;
            CLError initializeCredsError;
            TaskEventArgs eventArgs = new TaskEventArgs()
            {
                CurrentTask = e.CurrentTask,
                ParamSet = e.ParamSet,
                ProcessingErrorHolder = e.ProcessingErrorHolder,
                ReportBuilder = e.ReportBuilder,

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

            e.StringBuilderList.Add(new StringBuilder(reportBuilder.ToString()));
            return responseCode;
        }

        #region Private Rename
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
        #endregion 
        #endregion 

        #region Delete
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
            else if (deleteTask.DeleteAll)
                iterations = -1;

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

            int initialCount; 
            int response =ItemsListHelper.GetSyncBoxCount(e, out initialCount);
            CLSyncBox syncBox;
            ItemsListManager mgr = ItemsListManager.GetInstance();
            ItemListHelperEventArgs eventArgs = new ItemListHelperEventArgs()
            {
                ProcessingErrorHolder = e.ProcessingErrorHolder,
                ParamSet = e.ParamSet,
                ReportBuilder = e.ReportBuilder,
            };

            ItemsListHelper.RunListSubscribtionSyncBoxes(eventArgs);
            List<SyncBox> toDelete;
            if(count == -1 )
                toDelete = new List<SyncBox>(mgr.SyncBoxes);
            else
                toDelete = new List<SyncBox>(mgr.SyncBoxes.Take(count));
            
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

            int currentCount;
            response = ItemsListHelper.GetSyncBoxCount(e, out currentCount);
            int expectedCount;
            if ((e.CurrentTask as Deletion).DeleteAllSpecified && (e.CurrentTask as Deletion).DeleteAll || count == -1)
                expectedCount = 0;
            else
                expectedCount = initialCount - count;

            if (expectedCount < 0)
                expectedCount = 0;

            StringBuilder explanation = new StringBuilder(string.Format("SyncBox Count Before Delete {0}.", initialCount));
            explanation.AppendLine("Results:");
            explanation.AppendLine(string.Format("Expected Count: {0}", expectedCount.ToString()));
            explanation.AppendLine(string.Format("Actual Count  : {0}", currentCount.ToString()));
            if (currentCount == expectedCount)
                explanation.AppendLine("Successfully Completed Delete SyncBox Task");
            else
            {
                explanation.AppendLine("Create Sync Box Task was Expecting a different result.");
                responseCode = (int)FileManagerResponseCodes.ExpectedItemMatchFailure;
            }
            newBuilder.AppendLine(explanation.ToString());
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
        #endregion 

        #region Undelete
        public int UnDelete(SmokeTestManagerEventArgs e)
        {
            GenericHolder<CLError> refHolder = e.ProcessingErrorHolder;
            Exception ex = new NotImplementedException("Can Not UnDelete a SyncBox");
            AddException(ex, ref refHolder);
            return (int)FileManagerResponseCodes.InvalidTaskType;
        }
        #endregion 

        #region Download
        public int Download(SmokeTestManagerEventArgs e)
        {
            GenericHolder<CLError> refHolder = e.ProcessingErrorHolder;
            Exception ex = new NotImplementedException("Can Not Download a SyncBox");
            AddException(ex, ref refHolder);
            return (int)FileManagerResponseCodes.InvalidTaskType;
        }
        #endregion 

        #region List Items
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
        #endregion 

        #region Private
        private static bool CompareLocalFiles(FileInfo manualFile, FileSystemInfo fso, ref StringBuilder reportBuilder)
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

        private static bool CompareFolders(DirectoryInfo manualDirectory, FileSystemInfo fso, ref StringBuilder reportBuilder)
        {
            bool areIdentical = true;

            DirectoryInfo di = fso as DirectoryInfo;
            if(di == null)
                return false;

            if (manualDirectory.CreationTime.Date != di.CreationTime.Date)
            {
                areIdentical = false;
                reportBuilder.AppendLine("File Creation Date Mismatch:");
                reportBuilder.AppendLine(string.Format("     {0} Date {1}", manualDirectory.FullName, manualDirectory.CreationTime.Date));
                reportBuilder.AppendLine(string.Format("     {0} Date {1}", di.FullName, di.CreationTime.Date));
            }
            if (manualDirectory.CreationTime.Hour != di.CreationTime.Hour)
            {
                areIdentical = false;
                reportBuilder.AppendLine("File Creation Time Mismatch:");
                reportBuilder.AppendLine(string.Format("     {0} Hour {1}", manualDirectory.FullName, manualDirectory.CreationTime.Hour));
                reportBuilder.AppendLine(string.Format("     {0} Hour {1}", di.FullName, di.CreationTime.Hour));
            }
            if (manualDirectory.CreationTime.Minute != di.CreationTime.Minute)
            {
                areIdentical = false;
                reportBuilder.AppendLine("File Creation Time Mismatch:");
                reportBuilder.AppendLine(string.Format("     {0} Minute {1}", manualDirectory.FullName, manualDirectory.CreationTime.Minute));
                reportBuilder.AppendLine(string.Format("     {0} Minute {1}", di.FullName, di.CreationTime.Minute));
            }
            if (manualDirectory.CreationTime.Second != di.CreationTime.Second)
            {
                areIdentical = false;
                reportBuilder.AppendLine("File Creation Time Mismatch:");
                reportBuilder.AppendLine(string.Format("     {0} Second {1}", manualDirectory.FullName, manualDirectory.CreationTime.Second));
                reportBuilder.AppendLine(string.Format("     {0} Second {1}", di.FullName, di.CreationTime.Second));
            }

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
            //int key = SyncBoxMapper.SyncBoxes.Count();
            if (returnValue > 0)
            {
                //SyncBoxMapper.SyncBoxes.Add(key, (long)returnValue);
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

        private CreateSyncBox TransformCreateToCreateSyncBox(SmokeTestManagerEventArgs e)
        {
            Creation create = e.CurrentTask as Creation;
            if (create != null)
            {
                return new CreateSyncBox()
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
            return null;

        }
        #endregion
    }
}
