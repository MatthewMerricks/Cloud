using Cloud;
using Cloud.Interfaces;
using Cloud.JsonContracts;
using Cloud.Model;
using Cloud.Static;
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
    public class FileManager : ISmokeTaskManager
    {
        #region Public Static 
        public static string TrimTrailingSlash(string path)
        {
            if (FileHelper.PathEndsWithSlash(path))
            {
                return path.Remove(path.Count() - 1, 1);
            }
            else
                return path;
        }
        #endregion

        #region Interface Impletementation

        #region Create

        public int Create(SmokeTestManagerEventArgs e)
        {
            CLSyncBox syncBox;
            TaskEventArgs refArgs = (e as TaskEventArgs);
            syncBox = SyncBoxManager.InitializeCredentialsAndSyncBox(e);
            if (syncBox.SyncBoxId != 0)
            {
                e.SyncBox = syncBox;
                return BeginCreate(e);
            }
            else
            {
                return (int)FileManagerResponseCodes.UnknownError;
            }
        }

        #region Create Private
        private int BeginCreate(SmokeTestManagerEventArgs eventArgs)
        {
            Creation createTask = eventArgs.CurrentTask as Creation;
            int createResponseCode = 0;
            int iterations = 1;
            if (createTask.Count > 0)
                iterations = createTask.Count;

            //First figure out if we are opperating on a file or a folder 
            bool isFile = ApplyFileOrDirectoryInfo(createTask, eventArgs);
            //For each of the times specified from the SmokeTask 
            for (int x = 0; x < iterations; x++)
            {
                //If reposne code for any of the files was not successful ... break the opperation
                if (createResponseCode > 0)
                    return createResponseCode;
                //If thius is not the first iteration make sure to incrtement the file/flder name so we dont get a same name error 
                //once the name is incremented return a new FileFolderInfo object with the new path 
                if (x > 0)
                {
                    string path = IncrementNameReturnPath(isFile, x, eventArgs);
                    if (isFile)
                        eventArgs.FileInfo = new FileInfo(path);
                    else
                        eventArgs.DirectoryInfo = new DirectoryInfo(path);
                }
                //If the create type is manual, we have to pass this call to the REST call that will attempt to see
                //If this file exists in the syncBox already
                if (createTask.SyncType == SmokeTaskSyncType.Manual)
                    createResponseCode = PostCreate(eventArgs, isFile);
                else // If Create Type is Active, we will just make the change, and record the results 
                {
                    // If the task is to create to files or folders at the same time
                    if (createTask.ActInTwoFoldersSpecified && createTask.ActInTwoFolders)
                    {
                        // then we need to copy the Task, and change the copy's destination name 
                        createTask.SpecifiedFolder = FolderToUse.Active;
                        List<Creation> creationItemsList = GetCreationList(createTask);
                        // then run both tasks in paralell 
                        System.Threading.Tasks.Parallel.ForEach(creationItemsList, createItem =>
                        {
                            BeginAutoCreate(eventArgs, x, isFile, createItem);
                        });
                    }
                    else
                    {
                        BeginAutoCreate(eventArgs, x, isFile, createTask);

                    }
                }
            }
            return createResponseCode;
        }

        private List<Creation> GetCreationList(Creation createTask)
        {
            List<Creation> creationList = new List<Creation>();
            Creation creation2 = CopyFromCreation(createTask);
            creation2.SpecifiedFolder = FolderToUse.Active2;
            creationList.Add(createTask);
            creationList.Add(creation2);
            return creationList;
        }

        private int BeginAutoCreate(SmokeTestManagerEventArgs e, int iteration, bool isFile, Creation thisTask)
        {

            int responseCode = 0;
            Creation createTask = e.CurrentTask as Creation;

            string activePath = string.Empty;
            if (isFile)
                activePath = e.FileInfo.FullName;
            else
                activePath = e.DirectoryInfo.FullName;

            if (thisTask.ActInTwoFoldersSpecified && thisTask.ActInTwoFolders && thisTask.SpecifiedFolder == FolderToUse.Active2)
                activePath.Replace(e.ParamSet.ActiveSync_Folder.Replace("\"", ""), e.ParamSet.ActiveSync_Folder2.Replace("\"", ""));

            if (isFile)
            {
                FileInfo info = new FileInfo(activePath);
                List<FileChange> folderChanges = new List<FileChange>();
                responseCode = FileHelper.WriteFile(info, ref folderChanges) == true ? 0: (int)FileManagerResponseCodes.UnknownError;
            }
            else
            {
                DirectoryInfo dInfo = null;
                if (!Directory.Exists(activePath))
                   dInfo = Directory.CreateDirectory(activePath);
                responseCode = dInfo.Exists ? 0 : (int)FileManagerResponseCodes.UnknownError;
            }
            return responseCode;
        }

        private int PostCreate(SmokeTestManagerEventArgs eventArgs, bool isFile)
        {
            FileChange fileChange;
            int createReturnCode = 0;
            List<FileChange> folderChanges = new List<FileChange>();
            fileChange = GetFileChange(isFile, eventArgs, ref folderChanges);
            Cloud.JsonContracts.Event returnEvent;
            CLHttpRestStatus restStatus = eventArgs.RestStatus;

            CLError postFolderError;
            //If we created subfolders for the new fiels to exist in, we should post those first 
            foreach (FileChange folder in folderChanges)
            {
                postFolderError = eventArgs.SyncBox.HttpRestClient.PostFileChange(folder, ManagerConstants.TimeOutMilliseconds, out restStatus, out returnEvent);
                 if (postFolderError != null)
                    HandleExceptions(eventArgs, postFolderError);
            }
            
            // then post the actual file we are tyring to create on the server 
            CLError postFileError = eventArgs.SyncBox.HttpRestClient.PostFileChange(fileChange, ManagerConstants.TimeOutMilliseconds, out restStatus, out returnEvent);
            if (postFileError != null || restStatus != CLHttpRestStatus.Success)
            {
                ExceptionManagerEventArgs failArgs = new ExceptionManagerEventArgs()
                {
                     Error = postFileError,
                     RestStatus = restStatus,
                     ProcessingErrorHolder = eventArgs.ProcessingErrorHolder,
                };
                SmokeTaskManager.HandleFailure(failArgs);
            }

            HttpPostReponseCodes response = FileHelper.TransformHttpPostResponse(returnEvent.Header.Status.ToLower());

            if (response == HttpPostReponseCodes.Upload && isFile)
            {     
                FileManagerReturnEventArgs returnArgs = new FileManagerReturnEventArgs(eventArgs){FileChange = fileChange, ReturnEvent = returnEvent, StringBuilderList = eventArgs.StringBuilderList};
                ExecuteManualCreate(returnArgs);
            }
            else if (response != HttpPostReponseCodes.Upload && response == HttpPostReponseCodes.Conflict)
            {
                RetryPostCreate(isFile, eventArgs);
            }
            return createReturnCode;
        }

        private int ExecuteManualCreate(FileManagerReturnEventArgs eventArgs)
        {
            StringBuilder newBuilder = new StringBuilder();
            CLHttpRestStatus newStatus;
            int responseCode = 0;
            GenericHolder<CLError> refHolder = eventArgs.ProcessingErrorHolder;
            eventArgs.FileChange.Metadata.Revision = eventArgs.ReturnEvent.Metadata.Revision;
            eventArgs.FileChange.Metadata.StorageKey = eventArgs.ReturnEvent.Metadata.StorageKey;
            string message = string.Empty;
            newBuilder.AppendLine(string.Format("Initating Post File Upload {0}", eventArgs.FileInfo.Name));
            Stream stream = new System.IO.FileStream(eventArgs.FileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            CLError updateFileError = eventArgs.SyncBox.HttpRestClient.UploadFile(stream, eventArgs.FileChange, ManagerConstants.TimeOutMilliseconds, out newStatus, out message);
            if (newStatus != CLHttpRestStatus.Success || updateFileError != null)
            {
                FileHelper.HandleUnsuccessfulUpload(newStatus, updateFileError, ManagerConstants.RequestTypes.RestCreateFile, ref refHolder);
                responseCode = 1;
                newBuilder.AppendLine(string.Format("There was an error uploading File {0} to server.", eventArgs.FileInfo.Name));
            }
            else
            {
                CLHttpRestStatus confirmationStatus = CLHttpRestStatus.BadRequest;
                Metadata confirmationMetadata;
                eventArgs.SyncBox.GetMetadata(false, eventArgs.ReturnEvent.Metadata.ServerId, ManagerConstants.TimeOutMilliseconds, out confirmationStatus, out confirmationMetadata);

                if (confirmationMetadata.IsNotPending != true)
                    newBuilder.AppendLine(string.Format("File {0} is in Pending State", eventArgs.FileInfo.Name));
                else
                    newBuilder.AppendLine(string.Format("Successfully Uploaded File {0} to the Sync Box {1}.", eventArgs.FileInfo.Name, eventArgs.SyncBox.SyncBoxId));
            }
            eventArgs.StringBuilderList.Add(new StringBuilder(newBuilder.ToString()));
            return responseCode;
        }

        private int RetryPostCreate(bool isFile, SmokeTestManagerEventArgs eventArgs)
        {
            SmokeTestManagerEventArgs newArgs = new SmokeTestManagerEventArgs(eventArgs);
            newArgs.StringBuilderList = eventArgs.StringBuilderList;
            if (isFile)
            {
                bool isCopy = eventArgs.FileInfo.FullName.Contains("_Copy");
                string newPath = eventArgs.FileInfo.Directory.FullName + "\\" + FileHelper.CreateNewFileName(eventArgs, isCopy);
                newArgs.FileInfo = new FileInfo(newPath);
            }
            else
            {
                string newPath = eventArgs.DirectoryInfo.FullName + "_New";
                newArgs.DirectoryInfo = new DirectoryInfo(newPath);
            }
            return PostCreate(newArgs, isFile);
        }        

        private bool ApplyFileOrDirectoryInfo(Creation createTask, SmokeTestManagerEventArgs eventArgs)
        {
            bool isFile = false;
            if (eventArgs.CurrentTask.ObjectType.type == ModificationObjectType.File && eventArgs.FileInfo == null)
            { 
                string fullPath = string.Empty;
                FileHelper.CreateFilePathString(createTask, out fullPath);
                eventArgs.FileInfo = new FileInfo(fullPath);
                isFile = true;
            }
            else if (eventArgs.CurrentTask.ObjectType.type == ModificationObjectType.Folder && eventArgs.DirectoryInfo == null)
            {
                if (createTask.Path.Count() > 0 && createTask.Path.Contains("C:/"))
                    eventArgs.DirectoryInfo = new DirectoryInfo(createTask.Path);
                else
                    eventArgs.DirectoryInfo = new DirectoryInfo(eventArgs.RootDirectory + createTask.Path + createTask.Name);
            }
            return isFile;
        }

        public Creation CopyFromCreation(Creation creation)
        {
            Creation thisCreation = new Creation();
            thisCreation.ActInTwoFolders = creation.ActInTwoFolders;
            thisCreation.ActInTwoFoldersSpecified = creation.ActInTwoFoldersSpecified;
            thisCreation.AtIndex = creation.AtIndex;
            thisCreation.AtIndexSpecified = creation.AtIndexSpecified;
            thisCreation.Count = creation.Count;
            thisCreation.CountSpecified = creation.CountSpecified;
            thisCreation.CreateNew = creation.CreateNew;
            thisCreation.InnerTask = creation.InnerTask;
            thisCreation.Name = creation.Name;
            thisCreation.ObjectType = creation.ObjectType;
            thisCreation.Path = creation.Path;
            thisCreation.SelectedSyncBoxID = creation.SelectedSyncBoxID;
            thisCreation.SpecifiedFolder = creation.SpecifiedFolder;
            thisCreation.SyncType = creation.SyncType;
            thisCreation.type = creation.type;
            return thisCreation;
        }
        #endregion

        #endregion

        #region Rename
        public int Rename(SmokeTestManagerEventArgs e)
        {
            int responseCode = 0;
            ICLCredentialSettings settings;
            CLError error = null;
            TaskEventArgs refArgs = (e as TaskEventArgs);
            long id = SmokeTaskManager.GetOpperationSyncBoxID(e);

            CLSyncBox syncBox = SyncBoxManager.InitializeCredentialsAndSyncBox(e);
            if (syncBox.SyncBoxId != 0)
            {
                e.SyncBox = syncBox;
                responseCode = BeginRename(e);
            }
            else
            {
                return (int)FileManagerResponseCodes.UnknownError;
            }

            return responseCode;
        }

        #region Rename Private
        private int BeginRename(SmokeTestManagerEventArgs e)
        { 
            Settings.Rename renameTask = e.CurrentTask as Settings.Rename;
            if (renameTask == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;

            if (renameTask.SyncType == SmokeTaskSyncType.Manual)
            {
                return BeginManualRename(e, renameTask);
            }
            else 
            {
                return BeginAutoRename(e, renameTask);
            }           
        }

        private int BeginManualRename(SmokeTestManagerEventArgs e, Rename renameTask)
        {
            FileChange fileChange = null;
            Cloud.JsonContracts.Metadata metaData = new Cloud.JsonContracts.Metadata();
            bool isFile = renameTask.ObjectType.type == ModificationObjectType.File;
            if ((isFile && e.FileInfo == null) || (!isFile && e.DirectoryInfo == null))
            {
                SetInfo(isFile, e);
            }
            bool success = GetMetadata(e, isFile, out metaData);
            if (!success)
                return (int)FileManagerResponseCodes.UnknownError;
            if (isFile)
            {
                fileChange = PrepareFileChangeForModification(e, FileChangeType.Renamed, metaData);
                fileChange.NewPath = e.FileInfo.DirectoryName + '\\' + renameTask.NewName.Replace("\"", "") + e.FileInfo.Extension;
                fileChange.OldPath = e.FileInfo.FullName;
            }
            else
            {
                e.DirectoryInfo = new DirectoryInfo(GetOriginalDirectoryPath(e, renameTask));
                string newPath = e.DirectoryInfo.FullName.Replace(renameTask.OldName.Replace("\"", ""), renameTask.NewName.Replace("\"", ""));
                if (!Directory.Exists(e.DirectoryInfo.FullName))
                    Directory.CreateDirectory(e.DirectoryInfo.FullName);
                if (Directory.Exists(e.DirectoryInfo.FullName))
                {
                    fileChange = FolderHelper.GetFolderFileChange(e.DirectoryInfo, metaData, FileChangeType.Renamed, e.DirectoryInfo.FullName, newPath);
                }
            }

            FileManagerReturnEventArgs returnArgs = new FileManagerReturnEventArgs(e)
            {
                FileChange = fileChange,
                StringBuilderList = e.StringBuilderList,
            };
            return ExecuteManualRename(returnArgs, renameTask); 
        }

        private int ExecuteManualRename(FileManagerReturnEventArgs e, Settings.Rename renameTask)
        {
            StringBuilder newBuilder = new StringBuilder("Begining File Rename...");
            int responseCode = (int)FileManagerResponseCodes.Success;
            CLHttpRestStatus restStatus = e.RestStatus;
            Cloud.JsonContracts.Event returnEvent = e.ReturnEvent;
            CLError postFileError = e.SyncBox.HttpRestClient.PostFileChange(e.FileChange, ManagerConstants.TimeOutMilliseconds, out restStatus, out returnEvent);
            e.ReturnEvent = returnEvent;
            e.RestStatus = restStatus;
            if (postFileError != null || restStatus != CLHttpRestStatus.Success)
            {
                ExceptionManagerEventArgs failArgs = new ExceptionManagerEventArgs()
                {
                    Error = postFileError,
                    OpperationName = "FileManager.ExecuteRename",
                    RestStatus = restStatus,
                    ProcessingErrorHolder = e.ProcessingErrorHolder,
                };
                SmokeTaskManager.HandleFailure(failArgs);
                return (int)FileManagerResponseCodes.UnknownError;
            }
            else
            {
                try
                {
                    if (renameTask.ObjectType.type == ModificationObjectType.Folder)
                    {
                        if (!Directory.Exists(e.FileChange.NewPath.ToString()))
                        {
                            Directory.Move(e.FileChange.OldPath.ToString(), e.FileChange.NewPath.ToString());
                            if(Directory.Exists(e.FileChange.NewPath.ToString()))
                            {
                                newBuilder.AppendLine("Successfully Renamed Folder:");
                                newBuilder.AppendLine(string.Format("  From: {0}", e.FileChange.OldPath.ToString()));
                                newBuilder.AppendLine(string.Format("    To: {0}", e.FileChange.NewPath.ToString()));
                            }
                        }
                        else
                        {
                            newBuilder.AppendLine("Failed to Rename Folder Because Source Already Exists: ");
                            newBuilder.AppendLine(string.Format("     Source Folder      : {0}", e.FileChange.OldPath.ToString()));
                            newBuilder.AppendLine(string.Format("     Destination Folder : {0}", e.FileChange.NewPath.ToString()));
                            newBuilder.AppendLine();
                        }
                    }
                    else if (renameTask.ObjectType.type == ModificationObjectType.File)
                    {
                        if (!File.Exists(e.FileChange.NewPath.ToString()))
                        {
                            File.Move(e.FileChange.OldPath.ToString(), e.FileChange.NewPath.ToString());
                            if(File.Exists(e.FileChange.NewPath.ToString()))
                            {
                                newBuilder.AppendLine("Successfully Renamed File:");
                                newBuilder.AppendLine(string.Format("   From: {0}", e.FileChange.OldPath.ToString()));
                                newBuilder.AppendLine(string.Format("     To: {0}", e.FileChange.NewPath.ToString()));
                            }
                        }
                        else
                        {
                            newBuilder.AppendLine("Failed to Rename File Because Source Already Exists: ");
                            newBuilder.AppendLine(string.Format("     Source File      : {0}", e.FileChange.OldPath.ToString()));
                            newBuilder.AppendLine(string.Format("     Destination File : {0}", e.FileChange.NewPath.ToString()));
                            newBuilder.AppendLine();
                        }
                    }

                   
                }
                catch (Exception excetpion)
                {
                    lock (e.ProcessingErrorHolder)
                    {
                        e.ProcessingErrorHolder.Value = e.ProcessingErrorHolder.Value + excetpion;
                    }
                    newBuilder.AppendLine(string.Format("There was an error Renaming File {0}. Message : {1}", e.FileChange.OldPath.ToString(), excetpion.Message));
                    return (int)FileManagerResponseCodes.UnknownError;
                }
            }
            e.StringBuilderList.Add(new StringBuilder(newBuilder.ToString()));
            return responseCode;
        }

        private int BeginAutoRename(SmokeTestManagerEventArgs e, Rename renameTask)
        {
            int responseCode = 0;
            StringBuilder reportBuilder = new StringBuilder();
            reportBuilder.AppendLine("Begining Auto Rename ... ");
            string oldPath = string.Empty;
            string newPath = string.Empty;
            try
            {
                bool isFile = renameTask.ObjectType.type == ModificationObjectType.File;
                if(renameTask.ActInTwoFoldersSpecified && renameTask.ActInTwoFolders)
                {
                    List<Rename> renameList = GetRenameList(renameTask);
                    if(isFile)
                    {
                        System.Threading.Tasks.Parallel.ForEach(renameList, renameItem => {
                            ExecuteAutoRenameFile(e, renameItem);
                        });
                    }
                    else
                    {
                        System.Threading.Tasks.Parallel.ForEach(renameList, renameItem => {
                            ExecuteAutoRenameFolder(e, renameItem);
                        });
                    }
                }
                else
                {
                    if (isFile)
                        ExecuteAutoRenameFile(e, renameTask);
                    else
                        ExecuteAutoRenameFolder(e, renameTask);
                }

            }
            catch (Exception ex)
            {
                lock (e.ProcessingErrorHolder)
                {
                    e.ProcessingErrorHolder.Value = e.ProcessingErrorHolder.Value + ex;
                }
                responseCode =  (int)FileManagerResponseCodes.UnknownError;
                reportBuilder.AppendLine();
                reportBuilder.AppendLine("There was an error Renaming Item:");
                reportBuilder.AppendLine(string.Format("    From: {0}", oldPath));
                reportBuilder.AppendLine(string.Format("      To: {0}", newPath));
                reportBuilder.AppendLine();
            }
            e.StringBuilderList.Add(new StringBuilder(reportBuilder.ToString()));
            return responseCode;
        }

        private int ExecuteAutoRenameFile(SmokeTestManagerEventArgs e, Rename renameTask)
        {
            StringBuilder reportBuilder = new StringBuilder();
            string oldPath = string.Empty;
            string newPath = string.Empty;
            int responseCode = 0;
            FileInfo toRename = GetFileForRename(e, renameTask);
            oldPath = toRename.FullName;
            newPath = toRename.FullName.ToString().Replace(toRename.Name, renameTask.NewName) + toRename.Extension;
            string backup = toRename.FullName.ToString().Replace(toRename.Name, renameTask.NewName) + "_bak" + toRename.Extension;
            try
            {
                File.Move(oldPath, newPath);
                FileInfo newInfo = new FileInfo(newPath);
                if(!newInfo.Exists)
                    throw new Exception(string.Format("{0} Not Moved to {1}.", oldPath, newPath));
                reportBuilder.AppendLine();
                reportBuilder.AppendLine("Successfully Renamed Item:");
                reportBuilder.AppendLine(string.Format("    From: {0}", oldPath));
                reportBuilder.AppendLine(string.Format("      To: {0}", newPath));
                reportBuilder.AppendLine();
                responseCode = 0;
            }
            catch(Exception ex)
            {
                lock(e.ProcessingErrorHolder)
                {
                    e.ProcessingErrorHolder.Value = e.ProcessingErrorHolder.Value + ex;
                   
                }
                reportBuilder.AppendLine();
                reportBuilder.AppendLine("There was an error Renaming Item:");
                reportBuilder.AppendLine(string.Format("    From: {0}", oldPath));
                reportBuilder.AppendLine(string.Format("      To: {0}", newPath));
                reportBuilder.AppendLine();
                
                responseCode = (int)FileManagerResponseCodes.UnknownError;
            }
            return responseCode;
        }

        private int ExecuteAutoRenameFolder(SmokeTestManagerEventArgs e, Rename renameTask)
        {
            string oldPath = string.Empty;
            string newPath = string.Empty;
            int responseCode = 0;
            StringBuilder reportBuilder = new StringBuilder();
            DirectoryInfo directoryInfo = GetFolderForRename(e);
            oldPath = directoryInfo.FullName;
            if (directoryInfo.FullName.Contains(renameTask.OldName))
                newPath = directoryInfo.FullName.Replace(renameTask.OldName, renameTask.NewName);
            else
            {
                string newName = directoryInfo.Name + "_new";
                newPath = directoryInfo.FullName.Replace(directoryInfo.Name, newName);
            }
            try
            {
                Directory.Move(oldPath, newPath);
                reportBuilder.AppendLine();
                reportBuilder.AppendLine("Successfully Renamed Item:");
                reportBuilder.AppendLine(string.Format("    From: {0}", oldPath));
                reportBuilder.AppendLine(string.Format("      To: {0}", newPath));
                reportBuilder.AppendLine();
            }
            catch (Exception exception)
            {
                lock (e.ProcessingErrorHolder)
                {
                    e.ProcessingErrorHolder.Value = e.ProcessingErrorHolder.Value + exception;

                }
                reportBuilder.AppendLine();
                reportBuilder.AppendLine("There was an error Renaming Item:");
                reportBuilder.AppendLine(string.Format("    From: {0}", oldPath));
                reportBuilder.AppendLine(string.Format("      To: {0}", newPath));
                reportBuilder.AppendLine();

                responseCode = (int)FileManagerResponseCodes.UnknownError;
            }
            e.StringBuilderList.Add(new StringBuilder(reportBuilder.ToString()));
            return responseCode;
        }

        private string GetOriginalDirectoryPath(SmokeTestManagerEventArgs e, Rename renameTask)
        {
            string rootFolder = e.ParamSet.ManualSync_Folder.Replace("\"", "");
            string directoryPath = string.Empty;
            if (!renameTask.OldName.Contains(rootFolder))
                directoryPath = rootFolder + "\\" + renameTask.OldName.Replace("\"", "");
            else
                directoryPath = renameTask.OldName.Replace("\"", "");

            return directoryPath;
        }

        private FileInfo GetFileForRename(SmokeTestManagerEventArgs e, Rename renameTask)
        {
            Rename newRenameTask = new Rename();
            if (renameTask == null)
                newRenameTask = e.CurrentTask as Rename;
            else
                newRenameTask = renameTask;

            FileInfo returnValue = null;
            string rootFolder = string.Empty;
            string relative = string.Empty;
            string fullPath = string.Empty;
            try
            {
                if (e.CurrentTask.SyncType == SmokeTaskSyncType.Manual)
                {                    
                    rootFolder = e.ParamSet.ManualSync_Folder.Replace("\"", "");
                    if(renameTask != null)
                        relative = newRenameTask.RelativeDirectoryPath.Replace("\"", "");
                    if (relative.Count() == 1)
                        relative = string.Empty;

                    fullPath = string.Concat(rootFolder, string.Concat(relative, newRenameTask.OldName.Replace("\"", "")));
                    if (File.Exists(fullPath))
                        returnValue = new FileInfo(fullPath);
                    else
                        returnValue = FileHelper.FindFirstFileInDirectory(rootFolder);
                }
                else
                {
                    if (newRenameTask.SpecifiedFolder == FolderToUse.Active2)
                        rootFolder = e.ParamSet.ActiveSync_Folder2.Replace("\"", "");
                    else
                        rootFolder = e.ParamSet.ActiveSync_Folder.Replace("\"", "");

                    relative = newRenameTask.RelativeDirectoryPath.Replace("\"", "");
                    if (relative.Count() == 1)
                        relative = string.Empty;

                    fullPath = string.Concat(rootFolder, string.Concat(relative, newRenameTask.OldName.Replace("\"", "")));
                    if (File.Exists(fullPath))
                        returnValue = new FileInfo(fullPath);
                    else
                        returnValue = FileHelper.FindFirstFileInDirectory(rootFolder);
                }            
            }
            catch (Exception exception)
            {
                lock (e.ProcessingErrorHolder)
                {
                    e.ProcessingErrorHolder.Value = e.ProcessingErrorHolder.Value + exception;
                }
            }
            return returnValue;
        }

        private DirectoryInfo GetFolderForRename(SmokeTestManagerEventArgs e)
        {
            DirectoryInfo returnInfo = null;
            Rename renameTask = e.CurrentTask as Rename;
            string rootFolder = string.Empty;
            string relative = string.Empty;
            string fullPath = string.Empty;

            if (e.CurrentTask.SyncType == SmokeTaskSyncType.Manual)
                rootFolder = TrimTrailingSlash(e.ParamSet.ManualSync_Folder.Replace("\"", ""));
            else
                rootFolder = TrimTrailingSlash(e.ParamSet.ActiveSync_Folder.Replace("\"", ""));

            if (FileHelper.PathEndsWithSlash(renameTask.RelativeDirectoryPath) && renameTask.RelativeDirectoryPath.Count() > 1)
                relative = TrimTrailingSlash(renameTask.RelativeDirectoryPath);
            else
                relative = renameTask.RelativeDirectoryPath;

            fullPath = string.Concat(rootFolder, relative, renameTask.OldName.Replace("\"", ""));
            if (Directory.Exists(fullPath))
                returnInfo = new DirectoryInfo(fullPath);
            else
                returnInfo = FileHelper.FindFirstSubFolder(rootFolder);

            return returnInfo;
        }

        private void SetInfo(bool isFile, SmokeTestManagerEventArgs e)
        {
            string root = string.Empty;
            if (e.CurrentTask.SyncType == SmokeTaskSyncType.Active)
                root = TrimTrailingSlash(e.ParamSet.ActiveSync_Folder.Replace("\"", ""));
            else
                root = TrimTrailingSlash(e.ParamSet.ManualSync_Folder.Replace("\"", ""));

            string relativePath = TrimTrailingSlash((e.CurrentTask as Rename).RelativeDirectoryPath);
            string fullPath = root + relativePath + "\\" + (e.CurrentTask as Rename).OldName;
            if (isFile)
                e.FileInfo = new FileInfo(fullPath);
            else
                e.DirectoryInfo = new DirectoryInfo(fullPath);
        }

        private Rename CopyFromRename(Rename renameTask)
        {
            Rename thisRename = new Rename();
            thisRename.ActInTwoFolders = renameTask.ActInTwoFolders;
            thisRename.ActInTwoFoldersSpecified = renameTask.ActInTwoFoldersSpecified;
            thisRename.AtIndex = renameTask.AtIndex;
            thisRename.AtIndexSpecified = renameTask.AtIndexSpecified;
            thisRename.InnerTask = renameTask.InnerTask;
            thisRename.NewName = renameTask.NewName;
            thisRename.ObjectType = renameTask.ObjectType;
            thisRename.OldName = renameTask.OldName;
            thisRename.RelativeDirectoryPath = renameTask.RelativeDirectoryPath;
            thisRename.SelectedSyncBoxID = renameTask.SelectedSyncBoxID;
            thisRename.ServerID = renameTask.ServerID;
            thisRename.ServerIDSpecified = renameTask.ServerIDSpecified;
            thisRename.SpecifiedFolder = renameTask.SpecifiedFolder;
            thisRename.SyncType = renameTask.SyncType;
            thisRename.type = renameTask.type;
            return thisRename;
           
        }

        private List<Rename> GetRenameList(Rename renameTask)
        {
            Rename rename2 = CopyFromRename(renameTask);
            renameTask.SpecifiedFolder = FolderToUse.Active;
            rename2.SpecifiedFolder = FolderToUse.Active2;
            List<Rename> returnList = new List<Rename>();
            returnList.Add(renameTask);
            returnList.Add(rename2);
            return returnList;
        }
        #endregion 

        #endregion

        #region Delete
        public int Delete(SmokeTestManagerEventArgs e)
        {
            CLSyncBox syncBox;
            TaskEventArgs refArgs = (e as TaskEventArgs);
            syncBox = SyncBoxManager.InitializeCredentialsAndSyncBox(e);
            if (syncBox.SyncBoxId != 0)
            {
                e.SyncBox = syncBox;
                return BeginDelete(e);
            }
            else
            {
                return (int)FileManagerResponseCodes.UnknownError;
            }
        }

        #region Private Delete
        private int BeginDelete(SmokeTestManagerEventArgs e)
        {
            StringBuilder reportBuilder = new StringBuilder();
            Deletion deleteTask = e.CurrentTask as Deletion;
            if (deleteTask == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;

            int deleteResponseCode = 0;

            GenericHolder<CLError> refHolder = e.ProcessingErrorHolder;
            SmokeTaskManagerDeleteArgs getDeleteEventArgs = new SmokeTaskManagerDeleteArgs();
            if (deleteTask.ObjectType.type == ModificationObjectType.File)
            {
                getDeleteEventArgs = new SmokeTaskManagerDeleteArgs()
                {
                    ParamSet = e.ParamSet,
                    CurrentTask = e.CurrentTask,
                    SyncBox = e.SyncBox,
                    ProcessingErrorHolder = e.ProcessingErrorHolder,
                };
            }
            if (deleteTask.ObjectType.type == ModificationObjectType.Folder)
            {

                getDeleteEventArgs = new SmokeTaskManagerDeleteArgs()
                {
                    CurrentTask = deleteTask,
                    ProcessingErrorHolder = e.ProcessingErrorHolder,
                    SyncBox = e.SyncBox,
                    SyncBoxRoot = new DirectoryInfo(e.ParamSet.ManualSync_Folder.Replace("\"", "")),
                };                
            }
            deleteResponseCode = ExecuteDelete(getDeleteEventArgs, reportBuilder);
            e.StringBuilderList.Add(new StringBuilder(reportBuilder.ToString()));
            return deleteResponseCode;
        }

        private int ExecuteDelete(SmokeTaskManagerDeleteArgs e, StringBuilder reportBuilder)
        {
            int deleteResponseCode = 0;
            bool isFile = e.CurrentTask.ObjectType.type == ModificationObjectType.File;
            CLHttpRestStatus restStatus;
            List<Metadata> metadataList = new List<Metadata>();
            if (isFile)
                metadataList = GetFilesForDelete(e, e.CurrentTask as Deletion).ToList();
            else
                metadataList = GetFoldersForDelete(e, e.CurrentTask as Deletion).ToList();
            List<FileChange> changes = GetFileChangesFromMetadata(e.SyncBox, metadataList).ToList();
            Cloud.JsonContracts.Event returnEvent;

            foreach (FileChange fc in changes)
            {
                if (deleteResponseCode == 0)
                {
                    CLError postFileError = e.SyncBox.HttpRestClient.PostFileChange(fc, ManagerConstants.TimeOutMilliseconds, out restStatus, out returnEvent);
                    if (postFileError != null || restStatus != CLHttpRestStatus.Success)
                    {
                        ExceptionManagerEventArgs failArgs = new ExceptionManagerEventArgs()
                        {
                            ProcessingErrorHolder = e.ProcessingErrorHolder,
                            RestStatus = restStatus,
                            OpperationName = "FileManager.ExecuteDelete",
                            Error = postFileError,
                        };
                        SmokeTaskManager.HandleFailure(failArgs);
                        return (int)FileManagerResponseCodes.UnknownError;
                    }
                    else
                    {
                        reportBuilder.AppendLine(string.Format("Successfully Deleted: {0} with ID: {1}", fc.NewPath, fc.Metadata.ServerId));
                        string rootFolder = e.ParamSet.ManualSync_Folder.Replace("\"", "");
                        if (rootFolder.ElementAt(rootFolder.Count() - 1) == '\\')
                            rootFolder = rootFolder.Remove(rootFolder.Count() - 1, 1);
                        string fullPath = rootFolder + fc.NewPath.ToString().Replace("/", "\\");
                        TryDeleteLocal(fullPath, e, reportBuilder);
                    }
                }
            }
            return deleteResponseCode;
        }

        private IEnumerable<Metadata> GetFilesForDelete(SmokeTaskManagerDeleteArgs fileDeleteArgs, Deletion deleteTask)
        {
            List<Metadata> returnValues = new List<Metadata>();
            GenericHolder<CLError> refHolder = fileDeleteArgs.ProcessingErrorHolder;
            CLHttpRestStatus restStatus;
            FolderContents folderContents;
            CLError getFilesError = fileDeleteArgs.SyncBox.GetFolderContents(ManagerConstants.TimeOutMilliseconds, out restStatus, out folderContents);
            if (getFilesError != null || restStatus != CLHttpRestStatus.Success)
            {
                ExceptionManagerEventArgs failArgs = new ExceptionManagerEventArgs()
                {
                     ProcessingErrorHolder = fileDeleteArgs.ProcessingErrorHolder,
                     RestStatus = restStatus,
                     OpperationName = "FileManager.GetFilesForDelete",
                     Error = getFilesError,
                };
                SmokeTaskManager.HandleFailure(failArgs);
                return new List<Metadata>();
            }
            if (deleteTask.DeleteAll == true)
            {
                returnValues.AddRange(AddMetadataToList(folderContents.Objects, false, -1, deleteTask.RelativePath));
            }
            else if (deleteTask.DeleteCountSpecified && deleteTask.DeleteCount > 0)
            {
                returnValues.AddRange(AddMetadataToList(folderContents.Objects, false, deleteTask.DeleteCount, deleteTask.RelativePath));
            }
            else if (!string.IsNullOrEmpty(deleteTask.ServerID))
            {
                Metadata item = folderContents.Objects.Where(file => file.ServerId == deleteTask.ServerID).FirstOrDefault();
                if (item != null)
                    returnValues.Add(item);
            }
            else
            {
                throw new NotImplementedException("ManualSyncManager.GetFilesForDelete: There was no deletion type specified.");
            }

            return returnValues;
        }

        private IEnumerable<Metadata> GetFoldersForDelete(SmokeTaskManagerDeleteArgs folderDeleteArgs, Deletion deleteTask)
        {
            List<Metadata> returnValues = new List<Metadata>();
            GenericHolder<CLError> refHolder = folderDeleteArgs.ProcessingErrorHolder;
            CLHttpRestStatus restStatus;
            Cloud.JsonContracts.Folders folders;
            CLError getFilesError = folderDeleteArgs.SyncBox.GetFolderHierarchy(ManagerConstants.TimeOutMilliseconds, out restStatus, out folders);
            if (getFilesError != null || restStatus != CLHttpRestStatus.Success)
            {
                ExceptionManagerEventArgs failArgs = new ExceptionManagerEventArgs()
                {
                    ProcessingErrorHolder = folderDeleteArgs.ProcessingErrorHolder,
                    RestStatus = restStatus,
                    OpperationName = "FileManager.GetFilesForDelete",
                    Error = getFilesError,
                };
                SmokeTaskManager.HandleFailure(failArgs);
                return new List<Metadata>();
            }

            if (deleteTask.DeleteAll == true)
            {
                returnValues.AddRange(AddMetadataToList(folders.Metadata, true, -1, deleteTask.RelativePath));
            }
            else if (deleteTask.DeleteCountSpecified && deleteTask.DeleteCount > 0)
            {
                returnValues.AddRange(AddMetadataToList(folders.Metadata, true, deleteTask.DeleteCount, deleteTask.RelativePath));
            }
            else if (!string.IsNullOrEmpty(deleteTask.ServerID))
            {
                Metadata item = folders.Metadata.Where(file => file.ServerId == deleteTask.ServerID).FirstOrDefault();
                if (item != null)
                    returnValues.Add(item);
            }
            else
            {
                throw new NotImplementedException("ManualSyncManager.GetFilesForDelete: There was no deletion type specified.");
            }
            return returnValues;
        }

        private int TryDeleteLocal(string path, SmokeTaskManagerDeleteArgs e, StringBuilder reportBuilder)
        {
            int responseCode = 0;
            try
            {
                File.Delete(path);
                reportBuilder.AppendLine(string.Format("Successfully Deleted Local File {0}", path));
            }
            catch (Exception ex)
            {
                responseCode = -1;
                lock (e.ProcessingErrorHolder)
                {
                    e.ProcessingErrorHolder.Value = e.ProcessingErrorHolder.Value + ex;
                }
                reportBuilder.AppendLine(string.Format("There Was an Error Deleting Local File {0}", path));
            }
            return responseCode;
        }
        #endregion
        #endregion

        #region UnDelete
        public int UnDelete(SmokeTestManagerEventArgs e)
        {
            throw new NotImplementedException();
        }
        #endregion UnDelete

        #region Download
        public int Download(SmokeTestManagerEventArgs e)
        {
            throw new NotImplementedException(string.Format("Download All should only be called from a SyncBox Manager not {0}", this.GetType().ToString()));
        }
        #endregion Download

        #region ListItems
        public int ListItems(SmokeTestManagerEventArgs e)
        {
            throw new NotImplementedException();
        }
        #endregion ListItems


        public int AlternativeAction(Events.ManagerEventArgs.SmokeTestManagerEventArgs e)
        {
            throw new NotImplementedException();
        }
        #endregion 

        #region Private
        private FileChange GetFileChange(bool isFile, SmokeTestManagerEventArgs eventArgs, ref List<FileChange> folderChanges)
        {
            if (isFile)
            {
                if (!File.Exists(eventArgs.FileInfo.FullName))
                    FileHelper.WriteFile(eventArgs.FileInfo, ref folderChanges);
                return FileHelper.PrepareMD5FileChange(eventArgs);
            }
            else
            {
                if (!Directory.Exists(eventArgs.DirectoryInfo.FullName))
                    Directory.CreateDirectory(eventArgs.DirectoryInfo.FullName);
                Cloud.JsonContracts.Metadata metadata = new Cloud.JsonContracts.Metadata()
                {
                    CreatedDate = DateTime.UtcNow,
                    IsFolder = true,
                    ModifiedDate = DateTime.UtcNow,
                };
                return FolderHelper.GetFolderFileChange(eventArgs.DirectoryInfo, metadata, FileChangeType.Created, string.Empty, eventArgs.DirectoryInfo.FullName);
            }
        }

        private FileChange PrepareFileChangeForModification(SmokeTestManagerEventArgs e, FileChangeType changeType, Cloud.JsonContracts.Metadata metaDataResponse)
        {

            FileChange fileChange = new FileChange();
            byte[] md5Bytes;
            if (!File.Exists(e.FileInfo.FullName))
            {
                Exception fileNotFound = new FileNotFoundException(string.Format("PrepareFileChangeForModification: {0}", e.FileInfo.FullName));
                lock (e.ProcessingErrorHolder)
                {
                    e.ProcessingErrorHolder.Value = e.ProcessingErrorHolder.Value + fileNotFound;
                }
                return null;
            }

            try
            {
                CLError badPathError = Cloud.Static.Helpers.CheckForBadPath(e.FileInfo.FullName);
                //TODO: Add call to check files exists   /// try syncbox.GetVersions 
                if (metaDataResponse.Deleted == null || metaDataResponse.Deleted == false)
                    md5Bytes = FileHelper.CreateFileChangeObject(e.FileInfo.FullName, changeType, true, metaDataResponse.Size, metaDataResponse.StorageKey, metaDataResponse.ServerId, out fileChange);
            }
            catch (Exception exception)
            {
                lock (e.ProcessingErrorHolder)
                {
                    e.ProcessingErrorHolder.Value = e.ProcessingErrorHolder.Value + exception;
                }
            }

            return fileChange;
        }

        private bool GetMetadata(SmokeTestManagerEventArgs e, bool isFile, out Cloud.JsonContracts.Metadata metaData)
        {
            bool success = true;
            CLHttpRestStatus restStatus = CLHttpRestStatus.BadRequest;
            CLError getMetaDataError;
            if (!isFile)
            {
                getMetaDataError = e.SyncBox.GetMetadata(e.DirectoryInfo.FullName, true, ManagerConstants.TimeOutMilliseconds, out restStatus, out metaData);
            }
            else
            {
                e.FileInfo = GetFileForRename(e, null);
                getMetaDataError = e.SyncBox.GetMetadata(e.FileInfo.FullName, false, ManagerConstants.TimeOutMilliseconds, out restStatus, out metaData);
            }
            if (getMetaDataError != null || restStatus != CLHttpRestStatus.Success)
            {
                ExceptionManagerEventArgs args = new ExceptionManagerEventArgs()
                {
                    RestStatus = restStatus,
                    Error = getMetaDataError,
                    OpperationName = "FileManger.BeginRename.FolderRename",
                    ProcessingErrorHolder = e.ProcessingErrorHolder,
                };
                SmokeTaskManager.HandleFailure(args);
                success = false;
                
            }
            return success;
        }

        private List<Metadata> AddMetadataToList(IEnumerable<Metadata> folderOrContents, bool folderOperations, int count, string relativePath)
        {
            List<Metadata> returnValues = new List<Metadata>();
            foreach (Metadata contentItem in folderOrContents)
            {
                
                switch (folderOperations)
                {
                    case true:
                        if (contentItem.IsFolder.HasValue && contentItem.IsFolder == true && contentItem.RelativePath.Count() > 1)
                            if (!ManagerConstants.DefaultFolderNames.Contains(contentItem.RelativePath))
                                returnValues.Add(contentItem);
                        break;
                    case false:
                        bool shouldDelete = ShouldDelete(contentItem, count, relativePath, ref returnValues);
                        if (shouldDelete)
                        {
                            returnValues.Add(contentItem);
                        }
                        break;
                }
                if (count > 0 && returnValues.Count() == count)
                    break;
            }
            return returnValues;
        }

        private bool ShouldDelete(Metadata contentItem, int count, string relativePath, ref List<Metadata> returnValues)
        {
            bool shouldDelete = false;
            if (!contentItem.IsFolder.HasValue || !contentItem.IsFolder.Value)
            {
                if (count == 1 && !string.IsNullOrEmpty(relativePath))
                {
                    if (contentItem.RelativePath.Replace("/", "\\") == relativePath && returnValues.Count < 1)
                    {
                        shouldDelete = true;
                    }
                }
                else if (!string.IsNullOrEmpty(relativePath))
                {

                }
            }
            return shouldDelete;
        }

        private IEnumerable<FileChange> GetFileChangesFromMetadata(CLSyncBox syncBox, IEnumerable<Metadata> metadataList)
        {
            CLHttpRestStatus restStatus;
            Metadata response;
            List<FileChange> fileChanges = new List<FileChange>();
            foreach (Metadata item in metadataList)
            {
                CLError getFileMetadataError = syncBox.GetMetadata(false, item.ServerId, ManagerConstants.TimeOutMilliseconds, out restStatus, out response);
                bool isFolder = item.IsFolder.HasValue == true ? item.IsFolder.Value : false;
                fileChanges.Add(new FileChange()
                {
                    Direction = SyncDirection.To,
                    Type = FileChangeType.Deleted,
                    Metadata = GetFileMetadataFromMetadata(item, isFolder),
                    NewPath = item.RelativePath,
                });
            }
            return fileChanges;
        }

        private FileMetadata GetFileMetadataFromMetadata(Cloud.JsonContracts.Metadata item, bool isFolder)
        {
            FileMetadata metadata = new FileMetadata();
            metadata.HashableProperties = new FileMetadataHashableProperties(isFolder, item.ModifiedDate.Value, item.CreatedDate.Value, item.Size);
            metadata.Revision = item.Revision;
            metadata.ServerId = item.ServerId;
            metadata.StorageKey = item.StorageKey;
            return metadata;
        }

        private string IncrementNameReturnPath(bool isFile, int increment, SmokeTestManagerEventArgs eventArgs)
        {
            if (isFile)
            {
                FileInfo fileInfo = eventArgs.FileInfo;
                StringBuilder sb = new StringBuilder();
                string name = fileInfo.Name.Replace(fileInfo.Extension, "");
                name = name.Remove(name.Count() -1, 1);
                sb.AppendFormat("{0}{1}", fileInfo.Directory.FullName, "\\");
                sb.AppendFormat("{0}{1}{2}", name, increment.ToString(), fileInfo.Extension);
                return sb.ToString();
            }
            else
            {
                return eventArgs.DirectoryInfo.FullName + increment.ToString();
            }
        }

        private void HandleExceptions(SmokeTestManagerEventArgs e, CLError error)
        {
            lock (e.ProcessingErrorHolder)
            {
                foreach (Exception exception in error.GrabExceptions())
                    e.ProcessingErrorHolder.Value = e.ProcessingErrorHolder.Value + exception;
            }
        }
        #endregion

    }
}
