using CloudApiPublic.Model;
using CloudApiPublic.Static;
using CloudSDK_SmokeTest.Events.CLEventArgs;
using CloudSDK_SmokeTest.Managers;
using CloudSDK_SmokeTest.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Helpers
{
    public static class FolderHelper
    {

        public static FileChange GetFolderFileChange(DirectoryInfo dInfo, CloudApiPublic.JsonContracts.Metadata metaData, FileChangeType type, string directoryPath, string newPath)
        {
            FileChange returnValue = new FileChange();
            if (type == FileChangeType.Renamed)
            {
                returnValue = new FileChange()
                {
                    Direction = SyncDirection.To,
                    Metadata = new FileMetadata()
                    {
                        HashableProperties = new FileMetadataHashableProperties(true, null, dInfo.CreationTime, null),
                        ServerId = metaData.ServerId,
                        Revision = metaData.Version,
                        StorageKey = metaData.StorageKey
                    },
                    OldPath = directoryPath,
                    NewPath = newPath,
                    Type = type,

                };
            }
            else if (type == FileChangeType.Created)
            {
                returnValue = new FileChange()
                {
                    Direction = SyncDirection.To,
                    Metadata = new FileMetadata()
                    {
                        HashableProperties = new FileMetadataHashableProperties(true, null, dInfo.CreationTime, null),
                        MimeType = null,
                        LinkTargetPath = null,
                        Revision = null,
                        ServerId = null,
                        StorageKey = null,
                    },
                    NewPath = newPath,
                    Type = type
                };
            }
            else if (type == FileChangeType.Deleted)
            {
                returnValue = new FileChange()
                {
                    Direction = SyncDirection.To,
                    Metadata = new FileMetadata()
                    {
                        HashableProperties = new FileMetadataHashableProperties(true, null, dInfo.CreationTime, null),
                        MimeType = null,
                        LinkTargetPath = null,
                        Revision = null,
                        ServerId = null,
                        StorageKey = null,
                    },
                    NewPath = directoryPath,
                    Type = type
                };
            }
            return returnValue;
        }

        public static int CreateDirectory(ManualSyncManager manager, CreateFolderEventArgs createEventArgs)
        {
            //TODO: Find out if We will ever just be creating a folder without it being initiated by adding a file to a non existent folder. 
            Creation task = createEventArgs.CurrentTask as Creation;
            if(task == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;

            int createReturnCode = 0;
            if (!Directory.Exists(createEventArgs.CreateTaskDirectoryInfo.FullName))
                Directory.CreateDirectory(createEventArgs.CreateTaskDirectoryInfo.FullName);

            string newPath = createEventArgs.CreateTaskDirectoryInfo.FullName ;
            CLHttpRestStatus restStatus = new CLHttpRestStatus();
            if(!task.Name.Contains(".") && !newPath.Contains(task.Name))
                newPath = newPath + "\\" + task.Name;
            FileChange fileChange = FolderHelper.GetFolderFileChange(createEventArgs.CreateTaskDirectoryInfo, null, FileChangeType.Created, string.Empty, newPath);
            CloudApiPublic.JsonContracts.Event returnEvent;

            CLError postFolderError = createEventArgs.SyncBox.HttpRestClient.PostFileChange(fileChange, ManagerConstants.TimeOutMilliseconds, out restStatus, out returnEvent);
            if (postFolderError != null || restStatus != CLHttpRestStatus.Success)
            {
                GenericHolder<CLError> refHolder = manager.ProcessingErrorHolder;
                ManualSyncManager.HandleFailure(postFolderError, restStatus, null, "CreateFolder", ref refHolder);
            }

            return createReturnCode;
        }

        public static DirectoryInfo GetFolderForDelete(GetFolderDeleteEventArgs getForDeleteArgs)
        {
            Deletion deleteTask = getForDeleteArgs.CurrentTask as Deletion;
            if (deleteTask == null)
                return null;

            string directoryPath = deleteTask.FullPath.Replace("\"", "");
            DirectoryInfo dInfo = null;
            if (!string.IsNullOrEmpty(directoryPath))
            {
                if (Directory.Exists(directoryPath))
                    dInfo = new DirectoryInfo(directoryPath);
            }
            
            if(dInfo == null)
            {
                string rootName = getForDeleteArgs.SyncBoxRoot.FullName;
                if(rootName.LastIndexOf("\\") == rootName.Count() -1)
                {
                    rootName = rootName.Substring(0, (rootName.Count() -1));
                }
                directoryPath = rootName + deleteTask.RelativePath.Replace("\"", "");
                string testString = deleteTask.Name.Replace("\"", "");
                if (directoryPath.Contains(testString))//+"\\" + deleteTask.Name.Replace("\"", "");
                    dInfo = new DirectoryInfo(directoryPath);
                else
                    dInfo = new DirectoryInfo(directoryPath + "\\" + deleteTask.Name.Replace("\"", ""));
            }

            
            return dInfo;
        }
    }
}
