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
    public class FileManager : ISmokeTaskManager
    {

        #region Old Implementation
        public static int RunFileCreationTask(InputParams paramSet, SmokeTask smokeTask, ref StringBuilder reportBuilder, ref ManualSyncManager manager, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            Creation creation = smokeTask as Creation;
            if (creation == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;

            int responseCode = -1;
            string fullPath = string.Empty;            
            FileHelper.CreateFilePathString(creation, out fullPath);
            try
            {
                reportBuilder.AppendLine(string.Format("Entering Creation Task. Current Creation Type: {0}", smokeTask.ObjectType.type.ToString()));
                responseCode = manager.Create(paramSet, smokeTask, new FileInfo(fullPath), creation.Name, ref reportBuilder, ref ProcessingErrorHolder);
                reportBuilder.AppendLine("Exiting Creation Task.");
            }
            catch (Exception ex)
            {
                lock (ProcessingErrorHolder)
                {
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + ex;
                }
            }
            return responseCode;

        }

        public static int RunFileDeletionTask(InputParams paramSet, SmokeTask smokeTask, ref StringBuilder reportBuilder,  ref ManualSyncManager manager, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            int deleteReturnCode = 0;
            try
            {
                if (!(smokeTask is Deletion))
                    return (int)FileManagerResponseCodes.InvalidTaskType;

                Console.WriteLine(string.Format("Entering Delete {0}", smokeTask.ObjectType.type.ToString()));
                deleteReturnCode = manager.Delete(paramSet, smokeTask, ref reportBuilder);
                Console.WriteLine(string.Format("Delete {0} Exiting", smokeTask.ObjectType.type.ToString()));


            }
            catch (Exception exception)
            {
                lock (ProcessingErrorHolder)
                {
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
                }
            }
            return deleteReturnCode;
        }

        public static int RunFileRenameTask(InputParams paramSet, SmokeTask smokeTask, ref StringBuilder reportBuilder, ref ManualSyncManager manager, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            int responseCode = -1;
            try
            {
                Rename task = smokeTask as Rename;
                if (task == null)
                    return (int)FileManagerResponseCodes.InvalidTaskType;

                reportBuilder.AppendLine(string.Format("Entering Rename {0}", smokeTask.ObjectType.type.ToString()));
                responseCode = manager.Rename(paramSet, task, task.RelativeDirectoryPath, task.OldName, task.NewName, ref reportBuilder, ref ProcessingErrorHolder);
                reportBuilder.AppendLine(string.Format("Rename {0} Exiting", smokeTask.ObjectType.type.ToString()));
            }
            catch (Exception exception)
            {
                lock (ProcessingErrorHolder)
                {
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
                }
            }
            return responseCode;
        }
        #endregion 

        #region Interface Impletementation

        #region Create

        public int Create(SmokeTestManagerEventArgs e)
        {
            CLSyncBox syncBox;
            ICLCredentialSettings settings;
            CLError error = null;
            TaskEventArgs refArgs = (e as TaskEventArgs);
            CredentialHelper.InitializeCreds(ref refArgs, out settings, out error);  
            int response = SyncBoxManager.InitilizeSyncBox(e, out syncBox);
            if (response == 0)
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
            FileInfo fileInfo = null;
            DirectoryInfo dInfo = null;
            bool isFile = ApplyFileOrDirectoryInfo(createTask, eventArgs);     
            for (int x = 0; x < iterations; x++)
            {
                if (x > 0)
                {
                    string path = IncrementNameReturnPath(isFile, x, eventArgs);
                    if (isFile)
                        eventArgs.FileInfo = new FileInfo(path);
                    else
                        eventArgs.DirectoryInfo = new DirectoryInfo(path);
                }
                if (createResponseCode == 0)
                    createResponseCode = PostCreate(eventArgs, isFile);
            }
            return createResponseCode;
        }

        private int PostCreate(SmokeTestManagerEventArgs eventArgs, bool isFile)
        {
            FileChange fileChange;
            int createReturnCode = 0;
            string fullPath;
            CloudApiPublic.JsonContracts.Metadata metadata;
            fileChange = GetFileChange(isFile, eventArgs);
            CloudApiPublic.JsonContracts.Event returnEvent;
            CLHttpRestStatus restStatus = eventArgs.RestStatus;

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
                FileManagerReturnEventArgs returnArgs = new FileManagerReturnEventArgs(eventArgs);
                returnArgs.FileChange = fileChange;
                returnArgs.ReturnEvent = returnEvent;
                ExecuteCreate(returnArgs);
            }
            else if (response != HttpPostReponseCodes.Upload)
            {
                RetryPostCreate(isFile, eventArgs);
            }
            return createReturnCode;
        }

        private int ExecuteCreate(FileManagerReturnEventArgs eventArgs)
        {
            CLHttpRestStatus newStatus;
            int responseCode = 0;
            GenericHolder<CLError> refHolder = eventArgs.ProcessingErrorHolder;
            eventArgs.FileChange.Metadata.Revision = eventArgs.ReturnEvent.Metadata.Revision;
            eventArgs.FileChange.Metadata.StorageKey = eventArgs.ReturnEvent.Metadata.StorageKey;
            string message = string.Empty;
            Stream stream = new System.IO.FileStream(eventArgs.FileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            CLError updateFileError = eventArgs.SyncBox.HttpRestClient.UploadFile(stream, eventArgs.FileChange, ManagerConstants.TimeOutMilliseconds, out newStatus, out message);
            if (newStatus != CLHttpRestStatus.Success || updateFileError != null)
            {
                FileHelper.HandleUnsuccessfulUpload(newStatus, updateFileError, ManagerConstants.RequestTypes.RestCreateFile, ref refHolder);
                responseCode = 1;
            }
            else
            {
                eventArgs.ReportBuilder.Append(string.Format("Successfully Uploaded File {0} to the Sync Box {1}.", eventArgs.FileInfo.Name, eventArgs.SyncBox.SyncBoxId));
            }
            return responseCode;
        }

        private int RetryPostCreate(bool isFile, SmokeTestManagerEventArgs eventArgs)
        {
            SmokeTestManagerEventArgs newArgs = new SmokeTestManagerEventArgs(eventArgs);
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
                eventArgs.DirectoryInfo = new DirectoryInfo(createTask.Path);
            }
            return isFile;
        }

        private string IncrementNameReturnPath(bool isFile, int increment, SmokeTestManagerEventArgs eventArgs)
        {
            if (isFile)
            {
                FileInfo fileInfo = eventArgs.FileInfo;
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0}{1}", fileInfo.Directory.FullName, "\\");
                sb.AppendFormat("{0}{1}{2}", fileInfo.Name.Replace(fileInfo.Extension, ""), increment.ToString(), fileInfo.Extension);
                return sb.ToString();
            }
            else
            {
                return eventArgs.DirectoryInfo.FullName + increment.ToString();
            }
        }
        #endregion

        #endregion

        #region Rename
        public int Rename(SmokeTestManagerEventArgs e)
        {
            CLSyncBox syncBox;
            ICLCredentialSettings settings;
            CLError error = null;
            TaskEventArgs refArgs = (e as TaskEventArgs);
            CredentialHelper.InitializeCreds(ref refArgs, out settings, out error);
            int response = SyncBoxManager.InitilizeSyncBox(e, out syncBox);
            if (response == 0)
            {
                e.SyncBox = syncBox;
                return BeginRename(e);
            }
            else
            {
                return (int)FileManagerResponseCodes.UnknownError;
            }
        }

        #region Rename Private
        private int BeginRename(SmokeTestManagerEventArgs e)
        { 
            Settings.Rename renameTask = e.CurrentTask as Settings.Rename;
            if (renameTask == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;
       
            FileChange fileChange = null;
            CloudApiPublic.JsonContracts.Metadata metaData = new CloudApiPublic.JsonContracts.Metadata();
            bool isFile = renameTask.ObjectType.type == ModificationObjectType.File;
            bool success = GetMetadata(e, isFile, out metaData);
            if (!success)
                return (int)FileManagerResponseCodes.UnknownError;
            if (isFile)
            {
                fileChange = PrepareFileChangeForModification(e, FileChangeType.Renamed, metaData);
                fileChange.NewPath = e.FileInfo.DirectoryName + '\\' + renameTask.NewName.Replace("\"", "");
                fileChange.OldPath = e.FileInfo.FullName;
            }
            else
            { 
                e.DirectoryInfo  = new DirectoryInfo(GetOriginalDirectoryPath(e, renameTask));               
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
            };
            return ExecuteRename(returnArgs, renameTask);            
        }

        private int ExecuteRename(FileManagerReturnEventArgs e, Settings.Rename renameTask)
        {
            int responseCode = (int)FileManagerResponseCodes.Success;
            CLHttpRestStatus restStatus = e.RestStatus;
            CloudApiPublic.JsonContracts.Event returnEvent = e.ReturnEvent;
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
                        Directory.Move(e.FileChange.OldPath.ToString(), e.FileChange.NewPath.ToString());
                    else if (renameTask.ObjectType.type == ModificationObjectType.File)
                        File.Move(e.FileChange.OldPath.ToString(), e.FileChange.NewPath.ToString());
                }
                catch (Exception excetpion)
                {
                    lock (e.ProcessingErrorHolder)
                    {
                        e.ProcessingErrorHolder.Value = e.ProcessingErrorHolder.Value + excetpion;
                    }
                    return (int)FileManagerResponseCodes.UnknownError;
                }
            }
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

        private FileInfo GetFileForRename(SmokeTestManagerEventArgs e)
        {
            FileInfo returnValue = null;
            try
            {
                Settings.Rename renameTask = e.CurrentTask as Settings.Rename;
                string rootFolder = e.ParamSet.ManualSync_Folder.Replace("\"", "");
                string relative = renameTask.RelativeDirectoryPath.Replace("\"", "");
                if(relative.Count() == 1)
                    relative = string.Empty;

                string fullPath = string.Concat(rootFolder, string.Concat(relative, renameTask.OldName.Replace("\"", "")));
                if (File.Exists(fullPath))
                    returnValue = new FileInfo(fullPath);
                else
                    returnValue = FileHelper.FindFirstFileInDirectory(rootFolder);

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
        #endregion 

        #endregion

        #region Delete
        public int Delete(SmokeTestManagerEventArgs e)
        {
            CLSyncBox syncBox;
            ICLCredentialSettings settings;
            CLError error = null;
            TaskEventArgs refArgs = (e as TaskEventArgs);
            CredentialHelper.InitializeCreds(ref refArgs, out settings, out error);
            int response = SyncBoxManager.InitilizeSyncBox(e, out syncBox);
            if (response == 0)
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
            Deletion deleteTask = e.CurrentTask as Deletion;
            if (deleteTask == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;

            int deleteResponseCode = 0;
            CLHttpRestStatus restStatus = new CLHttpRestStatus();
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
            deleteResponseCode = ExecuteDelete(getDeleteEventArgs);
            return deleteResponseCode;
        }

        private int ExecuteDelete(SmokeTaskManagerDeleteArgs e)
        {
            int deleteResponseCode = 0;
            bool isFile = e.CurrentTask.ObjectType.type == ModificationObjectType.File;
            int iterations = 1;
            CLHttpRestStatus restStatus;
            List<Metadata> metadataList = new List<Metadata>();
            if (isFile)
                metadataList = GetFilesForDelete(e, e.CurrentTask as Deletion).ToList();
            else
                metadataList = GetFoldersForDelete(e, e.CurrentTask as Deletion).ToList();
            List<FileChange> changes = GetFileChangesFromMetadata(e.SyncBox, metadataList).ToList();
            CloudApiPublic.JsonContracts.Event returnEvent;
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
                        Console.WriteLine(string.Format("Successfully Deleted: {0} with ID: {1}", fc.NewPath, fc.Metadata.ServerId));
                }
            }
            return deleteResponseCode;
        }

        private IEnumerable<Metadata> GetFilesForDelete(SmokeTaskManagerDeleteArgs fileDeleteArgs, Deletion deleteTask)
        {
            List<Metadata> returnValues = new List<Metadata>();
            GenericHolder<CLError> refHolder = fileDeleteArgs.ProcessingErrorHolder;
            CLHttpRestStatus restStatus;
            CloudApiPublic.JsonContracts.Folders folders;
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
                returnValues.AddRange(AddMetadataToList(folderContents.Objects, false, -1));
            }
            else if (deleteTask.DeleteCountSpecified && deleteTask.DeleteCount > 0)
            {
                returnValues.AddRange(AddMetadataToList(folderContents.Objects, false, deleteTask.DeleteCount));
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
            CloudApiPublic.JsonContracts.Folders folders;
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
                returnValues.AddRange(AddMetadataToList(folders.Metadata, true, -1));
            }
            else if (deleteTask.DeleteCountSpecified && deleteTask.DeleteCount > 0)
            {
                returnValues.AddRange(AddMetadataToList(folders.Metadata, true, deleteTask.DeleteCount));
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
        #endregion
        #endregion

        public int UnDelete(SmokeTestManagerEventArgs e)
        {
            throw new NotImplementedException();
        }

        public int Download(SmokeTestManagerEventArgs e)
        {
            throw new NotImplementedException(string.Format("Download All should only be called from a SyncBox Manager not {0}", this.GetType().ToString()));
        }

        public int ListItems(SmokeTestManagerEventArgs e)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Private
        private FileChange GetFileChange(bool isFile, SmokeTestManagerEventArgs eventArgs)
        {
            if (isFile)
            {
                if (!File.Exists(eventArgs.FileInfo.FullName))
                    FileHelper.WriteFile(eventArgs.FileInfo);
                return FileHelper.PrepareMD5FileChange(eventArgs);
            }
            else
            {
                if (!Directory.Exists(eventArgs.DirectoryInfo.FullName))
                    Directory.CreateDirectory(eventArgs.DirectoryInfo.FullName);
                CloudApiPublic.JsonContracts.Metadata metadata = new CloudApiPublic.JsonContracts.Metadata()
                {
                    CreatedDate = DateTime.UtcNow,
                    IsFolder = true,
                    ModifiedDate = DateTime.UtcNow,
                };
                return FolderHelper.GetFolderFileChange(eventArgs.DirectoryInfo, metadata, FileChangeType.Created, string.Empty, eventArgs.DirectoryInfo.FullName);
            }
        }

        private FileChange PrepareFileChangeForModification(SmokeTestManagerEventArgs e, FileChangeType changeType, CloudApiPublic.JsonContracts.Metadata metaDataResponse)
        {
            CLHttpRestStatus restStatus;
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
                CLError badPathError = CloudApiPublic.Static.Helpers.CheckForBadPath(e.FileInfo.FullName);
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

        private bool GetMetadata(SmokeTestManagerEventArgs e, bool isFile, out CloudApiPublic.JsonContracts.Metadata metaData)
        {
            bool success = true;
            CloudApiPublic.JsonContracts.Event returnEvent;
            CLHttpRestStatus restStatus = CLHttpRestStatus.BadRequest;
            CLError getMetaDataError;
            if (!isFile)
            {
                getMetaDataError = e.SyncBox.GetMetadata(e.DirectoryInfo.FullName, true, ManagerConstants.TimeOutMilliseconds, out restStatus, out metaData);
            }
            else
            {
                e.FileInfo = GetFileForRename(e);
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

        private List<Metadata> AddMetadataToList(IEnumerable<Metadata> folderOrContents, bool folderOperations, int count)
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
                        if (!contentItem.IsFolder.HasValue || !contentItem.IsFolder.Value)
                            returnValues.Add(contentItem);
                        break;
                }

                if (count > 0 && returnValues.Count() == count)
                    break;
            }
            return returnValues;
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

        private FileMetadata GetFileMetadataFromMetadata(CloudApiPublic.JsonContracts.Metadata item, bool isFolder)
        {
            FileMetadata metadata = new FileMetadata();
            metadata.HashableProperties = new FileMetadataHashableProperties(isFolder, item.ModifiedDate.Value, item.CreatedDate.Value, item.Size);
            metadata.Revision = item.Revision;
            metadata.ServerId = item.ServerId;
            metadata.StorageKey = item.StorageKey;
            return metadata;
        }
        #endregion



    }
}
