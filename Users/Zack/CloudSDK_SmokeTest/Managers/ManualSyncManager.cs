using CloudApiPublic;
using CloudApiPublic.Interfaces;
using CloudApiPublic.JsonContracts;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
using CloudSDK_SmokeTest.Events.CLEventArgs;
using CloudSDK_SmokeTest.Helpers;
using CloudSDK_SmokeTest.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Managers
{
    public sealed class ManualSyncManager : ManagerBase
    {
        #region Constants
        public const string DuplicateFileErrorString = "A file with a different ServerID already exists at that path.";

        public const string CaseSensitiveFileNameString = "Cannot create a file when that file already exists.\r\n";

        public static string[] defaultFolderNames = new string[] { "/Documents/", "/Videos/", "/Pictures/" };
        #endregion 

        #region Properties
        public InputParams InputParams { get; set; }

        public DirectoryInfo RootDirectory { get; set; }

        //Destination is the Key, Value is the ServerId
        public Dictionary<string, string> ServerIdAndPaths { get; set; }

        public GenericHolder<CLError> ProcessingErrorHolder { get; set; }

        public SmokeTask CurrentTask { get; set; }

        public int AddFileCounter { get; set; }

        public int AfterDownloadCallbackCounter { get; set; }

        public StringBuilder Report { get; set; }

        #endregion 

        #region Init
        public ManualSyncManager(InputParams paramSet)
        {
            this.InputParams = paramSet;
            string stripped = paramSet.ManualSync_Folder.Replace("\"", "");
            if (!string.IsNullOrEmpty(stripped))
            {
                if (Directory.Exists(stripped))
                    RootDirectory = new DirectoryInfo(stripped);
            }

        }
        #endregion 

        #region Create
        /// <summary>
        ///    This method is called to create a file in the Cloud Syncbox from a File on the Client Machine 
        /// </summary>
        /// <param name="paramSet"></param>
        /// <param name="fileInfo"></param>
        /// <param name="fileName"></param>
        /// <param name="ProcessingErrorHolder"></param>
        /// <returns>
        ///     int uploadResponseCode -- Defines the type of response returned form the server.
        /// </returns>
        public override int Create(InputParams paramSet, SmokeTask smokeTask,  FileInfo fileInfo, string fileName, ref StringBuilder reportBuilder, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            reportBuilder.Append(string.Format("Create {0} begins...", smokeTask.type));
            Creation createTask = smokeTask as Creation;
            if (createTask == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;
            int iterations = 1;
            this.ProcessingErrorHolder = ProcessingErrorHolder;
            CLCredential creds; 
            CLCredentialCreationStatus credsCreateStatus;
            int createResponseCode = 0;
            InitalizeCredentials("ManualSyncManager.Create", ref reportBuilder, out creds, out credsCreateStatus);
            // If Status returns anything other than success notify the user and stop the process.
            if (credsCreateStatus != CLCredentialCreationStatus.Success)
            {
                reportBuilder.AppendLine();
                reportBuilder.AppendFormat("There was an error Creating Credentials In Create File Method. Credential Create Status: {0}", credsCreateStatus.ToString());
                reportBuilder.AppendLine();
                reportBuilder.AppendLine("Exiting Creation Task...");
                return (int)FileManagerResponseCodes.InitializeCredsError;
            }

            CLSyncBox syncBox; 
            CLSyncBoxCreationStatus boxCreateStatus;
            ICLSyncSettings settings = new AdvancedSyncSettings(InputParams.ManualSync_Folder.Replace("\"", ""));
            long syncBoxId = SyncBoxMapper.SyncBoxes.Count > 0 ? SyncBoxMapper.SyncBoxes[0] : paramSet.ManualSyncBoxID;
            CloudApiPublic.CLSyncBox.CreateAndInitialize(creds, syncBoxId, out syncBox, out boxCreateStatus, settings as ICLSyncSettings);
            if (boxCreateStatus != CLSyncBoxCreationStatus.Success)
            {
                reportBuilder.AppendLine();
                reportBuilder.AppendFormat("There was an error Initializing the SyncBox In Create File Method. Credential Create Status: {0}", credsCreateStatus.ToString());
                reportBuilder.AppendLine();
                reportBuilder.AppendLine("Exiting Process...");
                return (int)FileManagerResponseCodes.InitializeSynBoxError;
            }
            if (createTask.ObjectType.type == ModificationObjectType.File)
            {
                CreateFileEventArgs eventArgs = new CreateFileEventArgs()
                {
                    boxCreationStatus = boxCreateStatus,
                    CreateTaskFileInfo = fileInfo,
                    Creds = creds,
                    CredsStatus = credsCreateStatus,
                    CurrentTask = createTask,
                    ProcessingErrorHolder = ProcessingErrorHolder,
                    SyncBox = syncBox,
                    CreateCurrentTime = DateTime.UtcNow,
                    RootDirectory = RootDirectory,
                    ReportBuilder = reportBuilder,
                };
               createResponseCode  =  CreateFiles(eventArgs);
            }
            else if (createTask.ObjectType.type == ModificationObjectType.Folder)
            {
                string directoryPath = createTask.Path.Replace("\"", "");

                CreateFolderEventArgs eventArgs = new CreateFolderEventArgs()
                {
                    boxCreationStatus = boxCreateStatus,
                    CreateTaskDirectoryInfo = new DirectoryInfo(directoryPath),
                    Creds = creds,
                    CredsStatus = credsCreateStatus,
                    CurrentTask = createTask,
                    ProcessingErrorHolder = ProcessingErrorHolder,
                    SyncBox = syncBox,
                    CreationTime = DateTime.UtcNow,
                    ReportBuilder = reportBuilder,
                };
                createResponseCode = CreateFolders(eventArgs);
            }
            else if (createTask.ObjectType.type == ModificationObjectType.Session)
            {
                throw new NotImplementedException("Create Task Type Session Not Implemented");
            }
            else if (createTask.ObjectType.type == ModificationObjectType.SyncBox)
            {
                throw new NotImplementedException("This Condition Should Never Be Met, Creating A SyncBox Should Occur Through CreateSyncBox not Create.");
            }
            return createResponseCode;
        }      

        public FileChange CreateFileChangeWithNewName(FileChange oldFileChange)
        {   
            FileChange fileChange = new FileChange();
            bool isDuplicate = true;
            int counter = 0;
            int newPathCharCount = oldFileChange.NewPath.Name.Count();
            string newFileName = oldFileChange.NewPath.Name;
            GenericHolder<CLError> holder;
            if (this.ProcessingErrorHolder != null)
                holder = this.ProcessingErrorHolder;
            else
                holder = new GenericHolder<CLError>();
            while (isDuplicate)
            {                
                counter++;
                if (!newFileName.Contains("_Copy"))
                {             
                    newFileName = FileHelper.CreateNewFileName(oldFileChange.NewPath.ToString(), false, false, ref holder);
                    if (!File.Exists(newFileName))
                    {
                        try
                        {
                            File.Copy(oldFileChange.NewPath.ToString(), newFileName);
                            byte[] md5 = FileHelper.CreateFileChangeObject(newFileName, FileChangeType.Created, true, null, null, string.Empty, out fileChange);
                            fileChange.SetMD5(md5);
                            isDuplicate = false;
                        }
                        catch (Exception ex)
                        {
                            lock (ProcessingErrorHolder)
                            {
                                ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + ex;
                            }
                        }
                    }
                    else
                    {

                        newFileName = FileHelper.CreateNewFileName(newFileName, true, false, ref holder);
                        string newFullPath = oldFileChange.NewPath.Parent.ToString() + '\\' + newFileName;
                        if (!File.Exists(newFullPath))
                        {
                            byte[] md5 = null;
                            CreateReplaceFileAndCreateFileChangeObject(newFileName, oldFileChange, ref md5, ref fileChange);
                            fileChange.SetMD5(md5);
                            isDuplicate = false;
                        }
                    }
                }
                //If the name already contains "_Copy" 
                else
                {
                    string fullPath = oldFileChange.NewPath.Parent.ToString() + '\\' + newFileName;
                    newFileName = FileHelper.CreateNewFileName(fullPath, true, false, ref holder);
                    string newFullPath = oldFileChange.NewPath.Parent.ToString() + '\\' + newFileName;
                    if (!File.Exists(newFullPath))
                    {
                        byte[] md5 = null;
                        CreateReplaceFileAndCreateFileChangeObject(newFileName, oldFileChange, ref md5, ref fileChange);
                        isDuplicate = false;
                    }
                }
            }
            return fileChange;
        }

        private void CreateReplaceFileAndCreateFileChangeObject(string newFileName, FileChange oldFileChange, ref byte[] md5, ref FileChange fileChange)
        {
            try
            {
                string fullPath = oldFileChange.NewPath.Parent.ToString() + '\\' + newFileName;
                FileHelper.WriteFile(oldFileChange.NewPath.Parent.ToString(), newFileName);
                md5 = FileHelper.CreateFileChangeObject(fullPath, FileChangeType.Created, true, null, null, string.Empty, out fileChange);
                fileChange.SetMD5(md5);
            }
            catch (Exception exception)
            {
                lock (ProcessingErrorHolder)
                {
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
                }
            }
        }              
        
        public int TryUpload(CreateFileEventArgs createEventArgs, FileChange newFileChange)
        {
            Console.WriteLine("Try Upload Entered...");
            CLHttpRestStatus restStatus;
            CloudApiPublic.JsonContracts.Event returnEvent;
            int createResponseCode = -1;
            GenericHolder<CLError> errorHolder;
            if(this.ProcessingErrorHolder != null)
                errorHolder = this.ProcessingErrorHolder;
            else 
                errorHolder = new GenericHolder<CLError>();

            try
            {
                CLError postFileError = createEventArgs.SyncBox.HttpRestClient.PostFileChange(newFileChange, ManagerConstants.TimeOutMilliseconds, out restStatus, out returnEvent);
                if (postFileError != null || restStatus != CLHttpRestStatus.Success)
                {
                    FileHelper.HandleUnsuccessfulUpload(restStatus, postFileError, ManagerConstants.RequestTypes.PostFileChange, ref errorHolder);
                }
                string response = returnEvent.Header.Status.ToLower();
                CreateFileResponseEventArgs responseArgs = new CreateFileResponseEventArgs(createEventArgs, newFileChange, response, restStatus, returnEvent);
                responseArgs.CreateTaskFileInfo = new FileInfo(newFileChange.NewPath.ToString());
                createResponseCode = FileHelper.CreateFileResponseSwitch(responseArgs, newFileChange, this, ref errorHolder);
                Console.WriteLine("TryUpload Exited...");
            }
            catch (Exception exception)
            {
                lock (ProcessingErrorHolder)
                {
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
                }
            }
            return createResponseCode;            
        }

        private int CreateFiles(CreateFileEventArgs eventArgs)
        {
            Creation createTask = eventArgs.CurrentTask as Creation;
            int createResponseCode = 0;
            int iterations = 1;
            if (createTask.Count > 0)
                iterations = createTask.Count;
            FileInfo fileInfo = eventArgs.CreateTaskFileInfo;
            for (int x = 0; x < iterations; x++)
            {
                if (x > 0)
                {
                    string newFullPath = fileInfo.Directory.FullName + "\\" + fileInfo.Name.Replace(fileInfo.Extension, "") + x.ToString() + fileInfo.Extension;
                    fileInfo = new FileInfo(newFullPath);
                    eventArgs.CreateTaskFileInfo = fileInfo;
                }
                if (createResponseCode == 0)
                    createResponseCode = FileHelper.CreateFile(InputParams, this, eventArgs);
            }
            return createResponseCode;
        }

        private int CreateFolders(CreateFolderEventArgs eventArgs)
        {
            Creation createTask = eventArgs.CurrentTask as Creation;
            int createResponseCode = 0;
            int iterations = 1;
            if (createTask.Count > 0)
                iterations = createTask.Count;
            DirectoryInfo folderInfo = eventArgs.CreateTaskDirectoryInfo;
            for (int x = 0; x < iterations; x++)
            {
                if (x > 0)
                {
                    string fullPath = eventArgs.CreateTaskDirectoryInfo.FullName + x.ToString();
                    folderInfo = new DirectoryInfo(fullPath);
                    eventArgs.CreateTaskDirectoryInfo = folderInfo;
                }
                if (createResponseCode == 0)
                {
                    createResponseCode = FolderHelper.CreateDirectory(this, eventArgs);
                    if (!Directory.Exists(eventArgs.CreateTaskDirectoryInfo.FullName))
                        Directory.CreateDirectory(eventArgs.CreateTaskDirectoryInfo.FullName);
                }


            }
            return createResponseCode;

        }
        #endregion

        #region Delete
        /// <summary>
        /// This methid is used to delete a specific file stored oin the SyncBox -- This operation only affects the server version of the file 
        /// this will not delete the client version of the file. 
        /// Note: If a Sync From happens After the server has deleted the file, but the file still exists in the client root, the file will be re-added to SyncBox
        /// </summary>
        /// <param name="paramSet"></param>
        /// <param name="filePath"></param>
        /// <returns>
        ///     int deleteFileResponse -- value returned depending on the completion status of the operation
        /// </returns>
        //public override int Delete(Settings.InputParams paramSet, SmokeTask smokeTask, ref StringBuilder reportBuilder)
        //{
        //    Deletion deleteTask = smokeTask as Deletion;
        //    if (deleteTask == null)
        //        return (int)FileManagerResponseCodes.InvalidTaskType;

        //    int deleteResponseCode = 0;
        //    CLCredential creds;
        //    CLCredentialCreationStatus credsCreateStatus;
        //    CLSyncBox syncBox;
        //    CLSyncBoxCreationStatus boxCreateStatus;
        //    CLHttpRestStatus restStatus = new CLHttpRestStatus();
        //    GenericHolder<CLError> refHolder = ProcessingErrorHolder;

        //    InitalizeCredentials("ManualSyncManager.DeleteFile", ref reportBuilder, out creds, out credsCreateStatus);            
        //    if (credsCreateStatus != CLCredentialCreationStatus.Success)
        //        return (int)FileManagerResponseCodes.InitializeCredsError;

        //    ICLSyncSettings settings = new AdvancedSyncSettings(InputParams.ManualSync_Folder.Replace("\"", ""));
        //    FileChange fileChange = null;
        //    CloudApiPublic.JsonContracts.Event returnEvent; 
        //    long syncBoxId = SyncBoxMapper.SyncBoxes.Count > 0 ? SyncBoxMapper.SyncBoxes[0] : paramSet.ManualSyncBoxID;
        //    CLError boxError = CloudApiPublic.CLSyncBox.CreateAndInitialize(creds, syncBoxId, out syncBox, out boxCreateStatus, settings as ICLSyncSettings);
        //    if (boxError != null || boxCreateStatus != CLSyncBoxCreationStatus.Success)
        //    {
        //        HandleFailure(boxError, null, boxCreateStatus, "Create SyncBox For Delete", ref refHolder);
        //        return (int)FileManagerResponseCodes.InitializeSynBoxError;
        //    }

        //    if (deleteTask.ObjectType.type == ModificationObjectType.File)
        //    {
        //        GetFileDeleteEventArgs getDeleteEventArgs = new GetFileDeleteEventArgs() 
        //        {
        //             ParamSet = paramSet,
        //             CurrentTask = smokeTask,
        //             SyncBox = syncBox,
        //             ReportBuilder = reportBuilder,
        //        };
        //        deleteResponseCode = DeleteFiles(getDeleteEventArgs);
        //    }
        //    if (deleteTask.ObjectType.type == ModificationObjectType.Folder)
        //    {

        //        GetFolderDeleteEventArgs eventArgs = new GetFolderDeleteEventArgs() 
        //        { 
        //            boxCreationStatus= boxCreateStatus, 
        //            CurrentTask = deleteTask, 
        //            Creds = creds,
        //            CredsStatus = credsCreateStatus, 
        //            ProcessingErrorHolder = ProcessingErrorHolder, 
        //            SyncBox = syncBox,
        //            SyncBoxRoot = new DirectoryInfo(paramSet.ManualSync_Folder.Replace("\"", "")),
        //        };
        //        deleteResponseCode = DeleteFolders(eventArgs);            
        //    }
        //    return deleteResponseCode;
        //}

        private IEnumerable<Metadata> GetFilesForDelete(GetFileDeleteEventArgs fileDeleteArgs, Deletion deleteTask)
        {
            List<Metadata> returnValues = new List<Metadata>();
            GenericHolder<CLError> refHolder = fileDeleteArgs.ProcessingErrorHolder;
            CLHttpRestStatus restStatus;
            CloudApiPublic.JsonContracts.Folders folders;
            FolderContents folderContents;
            CLError getFilesError = fileDeleteArgs.SyncBox.GetFolderContents(ManagerConstants.TimeOutMilliseconds, out restStatus, out folderContents);
            if (getFilesError != null || restStatus != CLHttpRestStatus.Success)
            {
                HandleFailure(getFilesError, restStatus, null, "MaunalSyncManager.GetFilesForDelete", ref refHolder);
                return new List<Metadata>();
            }
            if (deleteTask.DeleteAll == true)
            {
                returnValues.AddRange(AddMetadataToList(folderContents.Objects, false, -1, string.Empty));
            }
            else if (deleteTask.DeleteCountSpecified && deleteTask.DeleteCount > 0)
            {
                if (deleteTask.DeleteCount == 1)
                    returnValues.AddRange(AddMetadataToList(folderContents.Objects, false, deleteTask.DeleteCount, deleteTask.RelativePath));
                else
                    returnValues.AddRange(AddMetadataToList(folderContents.Objects, false, deleteTask.DeleteCount, string.Empty));
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

        private IEnumerable<Metadata> GetFoldersForDelete(GetFolderDeleteEventArgs folderDeleteArgs, Deletion deleteTask)
        {
            List<Metadata> returnValues = new List<Metadata>();
            GenericHolder<CLError> refHolder = folderDeleteArgs.ProcessingErrorHolder;
            CLHttpRestStatus restStatus;
            CloudApiPublic.JsonContracts.Folders folders;
            CLError getFilesError = folderDeleteArgs.SyncBox.GetFolderHierarchy(ManagerConstants.TimeOutMilliseconds, out restStatus, out folders);
            if (getFilesError != null || restStatus != CLHttpRestStatus.Success)
            {
                HandleFailure(getFilesError, restStatus, null, "MaunalSyncManager.GetFilesForDelete", ref refHolder);
                return new List<Metadata>();
            }

            if (deleteTask.DeleteAll == true)
            {
                returnValues.AddRange(AddMetadataToList(folders.Metadata, true,  -1, string.Empty));
            }
            else if (deleteTask.DeleteCountSpecified && deleteTask.DeleteCount > 0)
            {
                if(deleteTask.DeleteCount ==1)
                    returnValues.AddRange(AddMetadataToList(folders.Metadata, true, deleteTask.DeleteCount, deleteTask.RelativePath));
                else
                    returnValues.AddRange(AddMetadataToList(folders.Metadata, true, deleteTask.DeleteCount, string.Empty));
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

        private int DeleteFiles(GetFileDeleteEventArgs args)
        {
            int deleteResponseCode = 0;
            GenericHolder<CLError> refHolder = args.ProcessingErrorHolder;
            Deletion deleteTask = args.CurrentTask as Deletion;
            if (deleteTask == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;
            int iterations = 1;
            CLHttpRestStatus restStatus;
            List<Metadata> metadataList = GetFilesForDelete(args, deleteTask).ToList();
            List<FileChange> changes = GetFileChangesFromMetadata(args.SyncBox, metadataList).ToList();
            CloudApiPublic.JsonContracts.Event returnEvent;
            StringBuilder report = args.ReportBuilder;
            foreach (FileChange fc in changes)
            {
                if (deleteResponseCode == 0)
                {
                    CLError postFileError = args.SyncBox.HttpRestClient.PostFileChange(fc, ManagerConstants.TimeOutMilliseconds, out restStatus, out returnEvent);
                    if (postFileError != null || restStatus != CLHttpRestStatus.Success)
                    {
                        HandleFailure(postFileError, restStatus, null, "ManualSyncManager.DeleteFiles", ref refHolder);
                        return (int)FileManagerResponseCodes.UnknownError;
                    }
                    else
                    {
                        string rootFolder = args.ParamSet.ManualSync_Folder.Replace("\"", "");
                        if (rootFolder.ElementAt(rootFolder.Count()-1) == '\\')
                            rootFolder = rootFolder.Remove(rootFolder.Count()-1, 1);
                        string fullPath = rootFolder  + fc.NewPath.ToString().Replace("/", "\\");
                        report.AppendLine(string.Format("Successfully Deleted File: {0} ID: {1} From Syncbox: {2}", fc.NewPath, fc.Metadata.ServerId, args.SyncBox.SyncBoxId));
                        TryDeleteLocal(fullPath, ref refHolder, ref report);
                    }
                }
            }
            return deleteResponseCode;
        }

        private int TryDeleteLocal(string path, ref GenericHolder<CLError> ProcessingErrorHolder, ref StringBuilder reportBuilder)
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
                lock (ProcessingErrorHolder)
                {
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + ex;
                }
            }
            return responseCode;
        }

        private int DeleteFolders(GetFolderDeleteEventArgs args)
        {
            int deleteResponseCode = 0;
            GenericHolder<CLError> refHolder = args.ProcessingErrorHolder;
            Deletion deleteTask = args.CurrentTask as Deletion;
            if (deleteTask == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;
            int iterations = 1;
            CLHttpRestStatus restStatus;
            List<Metadata> metadataList = GetFoldersForDelete(args, deleteTask).ToList();
            List<FileChange> filechangeList = GetFileChangesFromMetadata(args.SyncBox, metadataList).ToList();
            CloudApiPublic.JsonContracts.Event returnEvent;
            foreach (FileChange fc in filechangeList)
            {
                if (deleteResponseCode == 0)
                {
                    CLError postFileError = args.SyncBox.HttpRestClient.PostFileChange(fc, ManagerConstants.TimeOutMilliseconds, out restStatus, out returnEvent);
                    if (postFileError != null || restStatus != CLHttpRestStatus.Success)
                    {
                        HandleFailure(postFileError, restStatus, null, "ManualSyncManager.DeleteFolders", ref refHolder);
                        return (int)FileManagerResponseCodes.UnknownError;
                    }
                    else
                        Console.WriteLine(string.Format("Successfully Deleted Folder: {0} with ID: {1}", fc.NewPath, fc.Metadata.ServerId));
                }
            }
            return deleteResponseCode;
        }
        #endregion

        #region Undelete
        public override int Undelete(Settings.InputParams paramSet, SmokeTask smokeTask, ref StringBuilder reportBuilder)
        {
            int deleteResponseCode = 0;
            CLCredential creds;
            CLCredentialCreationStatus credsCreateStatus;
            CLSyncBox syncBox;
            CLSyncBoxCreationStatus boxCreateStatus;
            CLHttpRestStatus restStatus = new CLHttpRestStatus();

            InitalizeCredentials("ManualSyncManager.CreateFile", ref reportBuilder, out creds, out credsCreateStatus);
            ICLSyncSettings settings = new AdvancedSyncSettings(InputParams.ManualSync_Folder.Replace("\"", ""));
            if (credsCreateStatus != CLCredentialCreationStatus.Success)
            {
                Console.WriteLine("There was an error Crteating Credentials In Create File Method. Credential Create Status: {0}", credsCreateStatus.ToString());
                Console.WriteLine("Exiting Process...");
                return (int)FileManagerResponseCodes.InitializeCredsError;
            }
            //CloudApiPublic.JsonContracts.Event returnEvent;
            //long syncBoxId = SyncBoxMapper.SyncBoxes.Count > 0 ? SyncBoxMapper.SyncBoxes[0] : paramSet.ManualSyncBoxID;
            //CloudApiPublic.CLSyncBox.CreateAndInitialize(creds, syncBoxId, out syncBox, out boxCreateStatus, settings as ICLSyncSettings);
            //FileInfo forDelete = GetUnDeleteInfo(paramSet, smokeTask);         
            ////TODO: Add Undelete File Functionality
            
            return 0;
        }
        #endregion

        #region Rename
        /// <summary>
        /// Currently this method will rename a specific file if specified but if not will rename the first file it arrives at by iterating through the Directory Structure 
        /// if 
        /// </summary>
        /// <param name="paramSet"></param>
        /// <param name="directoryRelativeToRoot"></param>
        /// <param name="oldFileName"></param>
        /// <param name="newFileName"></param>
        /// <returns></returns>
        public override int Rename(Settings.InputParams paramSet, SmokeTask smokeTask, string directoryRelativeToRoot, string oldFileName, string newFileName, ref StringBuilder reportBuilder, ref GenericHolder<CLError> inputProcessingErrorHolder)
        {
            Settings.Rename renameTask = smokeTask as Settings.Rename;
            if (renameTask == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;

            int renameResponseCode = 0;
            CLCredential creds;
            CLCredentialCreationStatus credsCreateStatus;
            CLSyncBox syncBox;
            CLSyncBoxCreationStatus boxCreateStatus;
            CLHttpRestStatus restStatus = new CLHttpRestStatus();
            ICLSyncSettings settings = new AdvancedSyncSettings(paramSet.ManualSync_Folder.Replace("\"", ""));

            InitalizeCredentials("ManualSync.RenameFile", ref reportBuilder, out creds, out credsCreateStatus);
            if (credsCreateStatus != CLCredentialCreationStatus.Success)
            {
                reportBuilder.AppendLine(string.Format("There was an error Crteating Credentials In Create File Method. Credential Create Status: {0}", credsCreateStatus.ToString()));
                reportBuilder.AppendLine("Exiting Process...");
                return (int)FileManagerResponseCodes.InitializeCredsError;
            }
            CloudApiPublic.JsonContracts.Event returnEvent;
            long syncBoxId = SyncBoxMapper.SyncBoxes.Count > 0 ? SyncBoxMapper.SyncBoxes[0] : paramSet.ManualSyncBoxID;
            CloudApiPublic.CLSyncBox.CreateAndInitialize(creds, syncBoxId, out syncBox, out boxCreateStatus, settings as ICLSyncSettings);
            CloudApiPublic.JsonContracts.FolderContents folderContents = null; 
            FileChange fileChange = null;
            if (renameTask.ObjectType.type == ModificationObjectType.Folder)
            {
                string rootFolder = paramSet.ManualSync_Folder.Replace("\"", "");
                string directoryPath = string.Empty;
                if (!renameTask.OldName.Contains(rootFolder))
                    directoryPath = rootFolder + "\\" + renameTask.OldName.Replace("\"", "");
                else
                    directoryPath = renameTask.OldName.Replace("\"", "");
                DirectoryInfo dinfo = new DirectoryInfo(directoryPath);
                CloudApiPublic.JsonContracts.Metadata metaData;
                CLError getMetaDataError = syncBox.GetMetadata(dinfo.FullName, true, ManagerConstants.TimeOutMilliseconds, out restStatus, out metaData);
                string newPath = directoryPath.Replace(oldFileName.Replace("\"", ""), newFileName.Replace("\"", ""));
                if(getMetaDataError != null || restStatus != CLHttpRestStatus.Success)
                {
                    GenericHolder<CLError> refHolder = inputProcessingErrorHolder;
                    HandleFailure(getMetaDataError, restStatus, null, "FolderRename", ref refHolder);
                }
                else
                {
                    if (!Directory.Exists(dinfo.FullName))
                        Directory.CreateDirectory(dinfo.FullName);
                    if (Directory.Exists(dinfo.FullName))
                    {
                        fileChange = FolderHelper.GetFolderFileChange(dinfo, metaData, FileChangeType.Renamed, dinfo.FullName, newPath);
                    }
                }           
            }
            else if(renameTask.ObjectType.type == ModificationObjectType.File)
            {
                FileInfo forRename = GetFileForRename(paramSet, directoryRelativeToRoot, oldFileName);
                fileChange = PrepareFileChangeForModification(paramSet, FileChangeType.Renamed, syncBox, forRename.FullName);
                fileChange.NewPath = forRename.DirectoryName + '\\' + newFileName.Replace("\"", "");
                fileChange.OldPath = forRename.FullName;
            }
            CLError postFileError = syncBox.HttpRestClient.PostFileChange(fileChange, ManagerConstants.TimeOutMilliseconds, out restStatus, out returnEvent);

            if (postFileError != null || restStatus != CLHttpRestStatus.Success)
            {
                GenericHolder<CLError> refprocessingErrorHolder = ProcessingErrorHolder;
                HandleFailure(postFileError, restStatus, null, "RenameFile", ref refprocessingErrorHolder);
            }
            else 
            {
                try
                {
                    if (renameTask.ObjectType.type == ModificationObjectType.Folder)
                        Directory.Move(fileChange.OldPath.ToString(), fileChange.NewPath.ToString());
                    else if (renameTask.ObjectType.type == ModificationObjectType.File)
                        File.Move(fileChange.OldPath.ToString(), fileChange.NewPath.ToString());
                }
                catch (Exception excetpion)
                {
                    GenericHolder<CLError> refprocessingErrorHolder = inputProcessingErrorHolder;
                    lock (refprocessingErrorHolder)
                    {
                        refprocessingErrorHolder.Value = refprocessingErrorHolder.Value + excetpion;
                    }
                }
            }            
            return 0;
        }
        #endregion

        #region Download All Content

        public static int RunDownloadAllSyncBoxContentTask(InputParams paramSet, SmokeTask smokeTask, ref StringBuilder reportBuilder,  ref ManualSyncManager manager, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            int responseCode = -1;
            try
            {
                reportBuilder.AppendLine("Initiating Download All Content...");
                responseCode = manager.InitiateDownloadAll(smokeTask, ref reportBuilder,  ref ProcessingErrorHolder);
                reportBuilder.AppendLine("End Download All Content...");
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

        /// <summary>
        /// This method is called to overwrite the entire contents of the clients Machine Sync Path with the current SyncBox content
        /// </summary>
        /// <param name="smokeTask"></param>
        /// <param name="ProcessingErrorHolder"></param>
        /// <returns>
        ///     int downloadAllResponseCode -- Description of Completion of Process 
        /// </returns>
        public int InitiateDownloadAll(SmokeTask smokeTask, ref StringBuilder reportBuilder, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            CurrentTask = smokeTask;
            if (this.ProcessingErrorHolder == null)
                this.ProcessingErrorHolder = ProcessingErrorHolder;
            int dloadAllResponseCode = 0;
            CLCredential creds; 
            CLCredentialCreationStatus credsCreateStatus;
            // Try to Create a Credentail set to make the Change 
            InitalizeCredentials("ManualSyncManager.InitiateDownloadAll", ref reportBuilder, out creds, out credsCreateStatus);
            // If Status returns anything other than success notify the user and stop the process.
            if (credsCreateStatus != CLCredentialCreationStatus.Success)
            {
                reportBuilder.AppendLine(string.Format(
                                            "There was an error Creating Credentials Initiate Download All Method. Credential Create Status: {0}", 
                                            credsCreateStatus.ToString()
                                         ));
                reportBuilder.AppendLine("Exiting Process...");
                dloadAllResponseCode = (int)FileManagerResponseCodes.InitializeCredsError;
                return dloadAllResponseCode;
            }

            CloudApiPublic.JsonContracts.Event returnEvent;
            CLSyncBox syncBox;
            CLSyncBoxCreationStatus boxCreateStatus;
            CLHttpRestStatus restStatus = new CLHttpRestStatus();

            ICLSyncSettings settings = new AdvancedSyncSettings(InputParams.ManualSync_Folder.Replace("\"", "")); 
                                                                

            CloudApiPublic.CLSyncBox.CreateAndInitialize(   creds, 
                                                            InputParams.ActiveSyncBoxID, 
                                                            out syncBox, 
                                                            out boxCreateStatus, 
                                                            settings as ICLSyncSettings);
            
            if (restStatus != CLHttpRestStatus.Success)
            {
                lock (ProcessingErrorHolder)
                {
                    Exception exception = new Exception(string.Format("There was an error creating the Sync Box with ID: {0} in the Initiate Download All Method"));
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
                }
                return 1;
            }
            GetAllContentFromSyncBox(syncBox, smokeTask, creds, ref reportBuilder, ref ProcessingErrorHolder);
            return dloadAllResponseCode; 
        }

        private void GetAllContentFromSyncBox(CLSyncBox syncBox, SmokeTask smokeTask, CLCredential creds, ref StringBuilder reportBuilder, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            if (this.ProcessingErrorHolder == null)
                this.ProcessingErrorHolder = ProcessingErrorHolder;
            CLHttpRestStatus restStatus;
            CloudApiPublic.JsonContracts.FolderContents folderContents = new CloudApiPublic.JsonContracts.FolderContents();
            try
            {
                CLError getAllContentError = syncBox.GetFolderContents(ManagerConstants.TimeOutMilliseconds, out restStatus, out folderContents, includeCount: false, contentsRoot:null, depthLimit:9, includeDeleted:false);
                if (restStatus != CLHttpRestStatus.Success || getAllContentError != null)
                {
                    HandleFailure(getAllContentError, restStatus, null, "GetAllContentFromSyncBox", ref ProcessingErrorHolder);
                    return;
                }
            }
            catch(Exception exception)
            {
                lock (ProcessingErrorHolder)
                {
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
                }
                return;
            }

            DownloadAllSyncBoxContent downloadAllTest = smokeTask as DownloadAllSyncBoxContent;
            if(downloadAllTest == null)
            {
                Exception exception = new Exception("Smoke Task was unable to be cast as DownloadAllSyncBoxContent");
                lock(ProcessingErrorHolder)
                {
                    ProcessingErrorHolder.Value  = ProcessingErrorHolder.Value + exception;
                }
                return;
            }

            //Async Parallel Opperations
            //System.Threading.Tasks.Parallel.ForEach(folderContents.Objects, (mdObject) =>
            //{
            //      HandleAdd(mdObject, syncBox);
            //});
            
            //Synchronous Opperations
            foreach (CloudApiPublic.JsonContracts.Metadata mdObject in folderContents.Objects)
            {
                HandleAdd(mdObject, syncBox, ref reportBuilder);
            }
            reportBuilder.AppendLine(string.Format("Add File Counter: {0}", AddFileCounter.ToString()));           

        }

        private void HandleAdd(CloudApiPublic.JsonContracts.Metadata mdObject, CLSyncBox syncBox, ref StringBuilder reportBuilder)
        {
            if (mdObject.IsFolder.HasValue && mdObject.IsFolder.Value)
            {
                string directoryPath = RootDirectory.FullName + mdObject.RelativePathWithoutEnclosingSlashes.Replace("\"", "");
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
            }
            else
            {
                AddFileCounter++;
                string filePath = RootDirectory.FullName + mdObject.RelativePathWithoutEnclosingSlashes.Replace("/", "\\").Replace("\"", "");
                FileInfo fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    CompleteAddFile(syncBox, filePath, mdObject);
                }
                else
                {
                    HandleFileComparison(syncBox, filePath, mdObject);
                }
            }
        }

        private void CompleteAddFile(CLSyncBox syncBox, string filePath, CloudApiPublic.JsonContracts.Metadata mdObject)
        {
            //Possibly need to create a FileChange off of the current metadata
            //Move file Upon copletion -- give an old path (temp) and a new path (truePath), move the file form old to new 
            FileChange currentFile;
            CLHttpRestStatus restStatus;
            FileHelper.CreateFileChangeObject(filePath, FileChangeType.Created, false, mdObject.Size, mdObject.StorageKey, mdObject.ServerId, out currentFile);
            currentFile.Direction = SyncDirection.From;
            object state = new object();
            CLError downloadError = syncBox.HttpRestClient.DownloadFile(currentFile, AfterDownloadCallback, state, ManagerConstants.TimeOutMilliseconds, out restStatus);
            if (ProcessingErrorHolder == null)
                ProcessingErrorHolder = new GenericHolder<CLError>();
            if (downloadError != null || restStatus != CLHttpRestStatus.Success)
            {
                GenericHolder<CLError> refProcessingErrorHolder = ProcessingErrorHolder;
                HandleFailure(downloadError, restStatus, null, "CompleteAddFile", ref refProcessingErrorHolder);
            }
        }

        private void HandleFileComparison(CLSyncBox syncBox, string filePath, CloudApiPublic.JsonContracts.Metadata mdObject)
        {
            GenericHolder<CLError> refToProcessErrorHolder = ProcessingErrorHolder;
            bool downloadServerFile = FileHelper.ShouldUpdateFile(InputParams, syncBox, filePath, mdObject, ref refToProcessErrorHolder);
        }

        private void AfterDownloadCallback(string inputString, FileChange fileChange, ref string refString, object state, Guid id)
        { 
            //This is where we will move the file from temp styorage to its place on the disk. 
            if (!string.IsNullOrEmpty(inputString))
            {
                if (!File.Exists(inputString))
                {
                    lock (this.ProcessingErrorHolder)
                    {
                        Exception ex = new Exception("The Expected file Does Not Exist in the Temporary Folder.");
                        ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + ex;
                    }
                }
                else
                {
                    AfterDownloadCallbackCounter++;
                    string rootString = RootDirectory.ToString().Replace(RootDirectory.Name + '\\', RootDirectory.Name);
                    string destination = rootString + fileChange.NewPath.GetRelativePath(RootDirectory, false);
                    try
                    {
                        bool continueMove = true;
                        if (File.Exists(destination) && CurrentTask is DownloadAllSyncBoxContent)
                        {
                            Exception currentException;
                            string serverId = fileChange.Metadata.ServerId.ToString();
                            int count = ServerIdAndPaths.Where(kvp => kvp.Value == serverId && kvp.Key == destination).Count();
                            bool shouldRetry = true;
                            if (count > 0)
                            {
                                FileInfo localFile = new FileInfo(destination);
                                if (localFile.CreationTime == fileChange.Metadata.HashableProperties.CreationTime)
                                    shouldRetry = false;
                                currentException = new Exception(DuplicateFileErrorString);
                            }
                            else
                                currentException = new Exception(CaseSensitiveFileNameString);

                            if (shouldRetry)
                            {
                                RetryDownloadCallbackRecursive(currentException, fileChange, inputString, destination, rootString);
                                continueMove = false;
                            }
                            
                        }
                        if (continueMove)
                        {
                            File.Move(inputString, destination);
                            //No Need to Map the files, we are not yewt supporting duplicate file names.
                            //if (!string.IsNullOrEmpty(fileChange.Metadata.ServerId))
                            //    ServerIdAndPaths.Add(destination, fileChange.Metadata.ServerId);
                        }
                    }
                    catch (Exception exception)
                    {
                        ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
                        //if the exception is of type File Exists Exception it means there is a duplicate name,
                        // or the Delta is case sensitivity
                        //RetryDownloadCallbackRecursive(exception, fileChange, inputString, destination, rootString);                  
                    }
                }
                Console.WriteLine(string.Format("After File Download Counter: {0}", AfterDownloadCallbackCounter.ToString()));
            }
        }

        private void RetryDownloadCallbackRecursive(Exception exception, FileChange fileChange, string originalPath, string destination, string rootString)
        {
            //ZW: Since we are not yet supporting mutiple files with the same name, this function should not ever get called. 
            

            if (exception.Message == CaseSensitiveFileNameString || exception.Message == DuplicateFileErrorString)
            {
                GenericHolder<CLError> errorHolder;
                if(this.ProcessingErrorHolder == null)
                    this.ProcessingErrorHolder = new GenericHolder<CLError>();
                
                errorHolder = this.ProcessingErrorHolder;
                destination = FileHelper.CreateNewFileName(destination, false, true, ref errorHolder);

                try
                {
                    File.Move(originalPath, destination);
                    //ZW no need to map the files, duplicate names unsupported 
                    //AddEntryToMappingFile(originalPath, destination);
                }
                catch (Exception exception2)
                {
                    lock(ProcessingErrorHolder)
                    {
                        ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception2;
                    }
                    //if the exception is of type File Exists Exception it means there is a duplicate name,
                    // or the Delta is case sensitivity
                    //RetryDownloadCallbackRecursive(exception2, fileChange, originalPath, destination, rootString);
                }
            }
        }               

        private void AddEntryToMappingFile(string originalName, string localName)
        {
            //ZW: Not yet mapping files Throw breaking exception if this methiod gets called 
            throw new NotImplementedException("AddEntryToMappingFile Method of Manual Sync Manager");

            string mappingFilePath = InputParams.FileNameMappingFile.Replace("\"", "");
            FileStream fileStream;
            try
            {
                StringBuilder kvpBuilder = new StringBuilder(originalName);
                kvpBuilder.Append(":");
                kvpBuilder.Append(localName);

                if (!File.Exists(mappingFilePath))
                {
                    fileStream = new FileStream(mappingFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);                 
                    StreamWriter writer = new StreamWriter(fileStream);
                    writer.WriteLine(kvpBuilder.ToString());
                    writer.Close();
                }
                else
                {
                    using (StreamWriter sw = File.AppendText(mappingFilePath))
                    {
                        sw.WriteLine(kvpBuilder.ToString());
                    }
                }
            }
            catch (Exception exception)
            {
                if (this.ProcessingErrorHolder == null)
                    this.ProcessingErrorHolder = new GenericHolder<CLError>();

                GenericHolder<CLError> errorHolder = this.ProcessingErrorHolder;
                lock (errorHolder)
                {
                    errorHolder.Value = errorHolder.Value + exception;
                }
            }
        }

        #endregion 

        #region All
        private void InitalizeCredentials(string callerName, ref StringBuilder reportBuilder, out CLCredential creds, out CLCredentialCreationStatus credsCreateStatus)
        {
            reportBuilder.AppendLine("Initializing Credentials for Manual Sync Create Method... ");
            reportBuilder.AppendLine();
            CLCredential.CreateAndInitialize(InputParams.API_Key, InputParams.API_Secret, out creds, out credsCreateStatus);
            reportBuilder.AppendLine(string.Format("Credential Initialization {0}", credsCreateStatus.ToString()));
        }

        private void ThrowDuplicateException(ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            Exception outerException = new Exception("Attepmting to CreateFile duplicate file.");
            lock (ProcessingErrorHolder)
            {
                ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + outerException;
            }
        }

        private FileChange PrepareFileChangeForModification(InputParams paramSet, FileChangeType changeType, CLSyncBox syncBox, string filePath)
        {
            CLHttpRestStatus restStatus;
            FileChange fileChange = new FileChange();
            byte[] md5Bytes;
            if (!File.Exists(filePath))
            {
                Exception fileNotFound = new FileNotFoundException(string.Format("PrepareFileChangeForModification: {0}", filePath));
                lock (this.ProcessingErrorHolder)
                {
                    this.ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + fileNotFound;
                }
            }
            else
            {
                try
                {
                    FileInfo filetoDelete = new FileInfo(filePath);
                    CloudApiPublic.JsonContracts.Metadata metaDataResponse;
                    CLError badPathError = CloudApiPublic.Static.Helpers.CheckForBadPath(filePath);
                    //TODO: Add call to check files exists   /// try syncbox.GetVersions 
                    CLError metaDataRequestError = syncBox.GetMetadata(filePath, false, ManagerConstants.TimeOutMilliseconds, out restStatus, out metaDataResponse);
                    if (metaDataRequestError == null && (metaDataResponse.Deleted == null || metaDataResponse.Deleted == false))
                        md5Bytes = FileHelper.CreateFileChangeObject(filePath, changeType, true, metaDataResponse.Size, metaDataResponse.StorageKey, metaDataResponse.ServerId, out fileChange);
                    else
                    {
                        Exception[] exceptions = metaDataRequestError.GrabExceptions().ToArray();
                        if (exceptions.Length > 0)
                        {
                            lock (this.ProcessingErrorHolder)
                            {
                                foreach (Exception ex in exceptions)
                                {
                                    this.ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + ex;
                                }
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    lock (this.ProcessingErrorHolder)
                    {
                        this.ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
                    }
                }
            }
            return fileChange;
        }

        private FileInfo GetFileForRename(InputParams paramSet, string relativePathToRoot, string oldFileName)
        {
            FileInfo returnValue = null;
            try 
            {
                string rootFolder = paramSet.ManualSync_Folder.Replace("\"", "");
                string fullPath = string.Concat(rootFolder, string.Concat(relativePathToRoot, oldFileName.Replace("\"", "")));
                if (File.Exists(fullPath))
                    returnValue = new FileInfo(fullPath);
                else
                    returnValue = FileHelper.FindFirstFileInDirectory(rootFolder);
                
            }
            catch (Exception exception)
            {
                lock (ProcessingErrorHolder)
                {
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
                }
            }
            return returnValue;
        }

        public static void HandleFailure(CLError error, CLHttpRestStatus? restStatus, CLSyncBoxCreationStatus? boxCreateStatus, string opperationName,  ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            List<Exception> errors = new List<Exception>();
            if (error != null)
            {
                foreach (Exception exception in error.GrabExceptions())
                {
                    errors.Add(exception);
                }
            }
            if (restStatus.HasValue && restStatus.Value != CLHttpRestStatus.Success)
                errors.Add(ExceptionManager.ReturnException(opperationName, restStatus.ToString()));
                
            else if (boxCreateStatus.HasValue && boxCreateStatus.Value != CLSyncBoxCreationStatus.Success)
                errors.Add(ExceptionManager.ReturnException(opperationName, boxCreateStatus.ToString()));
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
                            if(!defaultFolderNames.Contains(contentItem.RelativePath))
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
                    if (contentItem.RelativePath.Replace("/", "\\") == relativePath && returnValues.Count <1)
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
