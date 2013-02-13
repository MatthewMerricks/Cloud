using CloudApiPublic;
using CloudApiPublic.Interfaces;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
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

        #region Create File
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
        public override int Create(Settings.InputParams paramSet, FileInfo fileInfo, string fileName, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            this.ProcessingErrorHolder = ProcessingErrorHolder;
            int uploadResponseCode = 0;
            CLCredential creds; CLCredentialCreationStatus credsCreateStatus;
            DateTime currentTime = DateTime.UtcNow;
 
            string fullPath = fileInfo.FullName;
            if (!File.Exists(fullPath))
                WriteFile(RootDirectory.FullName, fileInfo.Name);

            InitalizeCredentials("ManualSyncManager.CreateFile", out creds, out credsCreateStatus);
            // If Status returns anything other than success notify the user and stop the process.
            if (credsCreateStatus != CLCredentialCreationStatus.Success)
            {
                Console.WriteLine("There was an error Crteating Credentials In Create File Method. Credential Create Status: {0}", credsCreateStatus.ToString());
                Console.WriteLine("Exiting Process...");
                uploadResponseCode = (int)FileManagerResponseCodes.InitializeCredsError;
                return uploadResponseCode;
            }
            // FileChange object defines the File and its metadata that will be opperated in 
            //ZW Change: FileChange fileChange =  PrepareMD5FileChange(paramSet, creds,  RootDirectory.FullName, fileName, ref ProcessingErrorHolder);
            FileChange fileChange = PrepareMD5FileChange(paramSet, creds, RootDirectory.FullName, fileName);
            //return TryUpload(paramSet, creds, filePath, fileInfo, ref fileChange, ref ProcessingErrorHolder); 
            Console.WriteLine("Try Upload Entered...");
            CloudApiPublic.JsonContracts.Event returnEvent;
            CLSyncBox syncBox;
            CLSyncBoxCreationStatus boxCreateStatus;
            CLHttpRestStatus restStatus = new CLHttpRestStatus();
            string stripped = paramSet.ManualSync_Folder.Replace("\"", string.Empty);
            ICLSyncSettings settings = new AdvancedSyncSettings(InputParams.ManualSync_Folder.Replace("\"", ""));
            long syncBoxId= SyncBoxMapper.SyncBoxes.Count > 0 ? SyncBoxMapper.SyncBoxes[0] : paramSet.ManualSyncBoxID;
            CloudApiPublic.CLSyncBox.CreateAndInitialize(creds, syncBoxId, out syncBox, out boxCreateStatus, settings as ICLSyncSettings);
            CLError postFileError = syncBox.HttpRestClient.PostFileChange(fileChange, ManagerConstants.TimeOutMilliseconds, out restStatus, out returnEvent);
            if (postFileError != null || restStatus != CLHttpRestStatus.Success)
            {
                FileHelper.HandleUnsuccessfulUpload(fileChange, returnEvent, restStatus, postFileError, ManagerConstants.RequestTypes.PostFileChange, ref ProcessingErrorHolder);
            }
            string response = returnEvent.Header.Status.ToLower();
            switch (response)
            {
                case "upload":
                case "uploading":
                    uploadResponseCode = FileHelper.TryUpload(fullPath, fileInfo.Name, syncBox, fileChange, restStatus, returnEvent, ref ProcessingErrorHolder);
                    break;
                case "duplicate":
                case "exists":
                    //ThrowDuplicateException(ref ProcessingErrorHolder);
                    uploadResponseCode = RenameAndTryUpload(paramSet, syncBox, creds, fileChange, fileInfo, fullPath, restStatus, returnEvent, ref ProcessingErrorHolder);
                    break;
                case "conflict":
                    uploadResponseCode = RenameAndTryUpload(paramSet, syncBox, creds, fileChange, fileInfo, fullPath, restStatus, returnEvent, ref ProcessingErrorHolder);
                    break;
                default:
                    Console.Write(string.Format("The Server Response is {0}", returnEvent.Header.Status));
                    break;

            }
            return uploadResponseCode;
        }

        private int RenameAndTryUpload(InputParams paramSet,CLSyncBox syncBox, CLCredential creds, FileChange fileChange, FileInfo fileInfo, string filePath, CLHttpRestStatus restStatus, CloudApiPublic.JsonContracts.Event returnEvent, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            FileChange newFileChange = CreateFileChangeWithNewName(fileChange);
            TryUpload(paramSet, creds, newFileChange.NewPath.ToString(), fileInfo, ref newFileChange);
            return 0;
        }

        private FileChange CreateFileChangeWithNewName(FileChange oldFileChange)
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
                    //ZW Change newFileName = FileHelper.CreateNewFileName(oldFileChange.NewPath.ToString(), false, ref ProcessingErrorHolder);                    
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
                            //ZW Change CreateReplaceFileAndCreateFileChangeObject(newFileName, oldFileChange, ref md5, ref fileChange, ref ProcessingErrorHolder);
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
                        //ZW Change CreateReplaceFileAndCreateFileChangeObject(newFileName, oldFileChange, ref md5, ref fileChange, ref ProcessingErrorHolder);
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
                WriteFile(oldFileChange.NewPath.Parent.ToString(), newFileName);
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

        private bool WriteFile(string path, string fileName)
        {
            string fullPath = path + '\\' + fileName;
            bool returnValue = true;
            if (!System.IO.File.Exists(fullPath))
            {
                using (System.IO.FileStream fs = System.IO.File.Create(fullPath))
                {
                    Random rnd = new Random();
                    int maxRandom = 1000000000;
                    int maxforCount = 1000;
                    int byteCount = rnd.Next(maxforCount);
                    Console.WriteLine(string.Format("The total number of iterations will be {0}", byteCount.ToString()));
                    for (int i = 0; i < byteCount; i++)
                    {
                        int currentRandom = rnd.Next(maxRandom);
                        byte[] bytes = Encoding.ASCII.GetBytes(currentRandom.ToString());
                        foreach(Byte b in bytes)
                            fs.WriteByte(b);

                        int rem = i % 100;
                        if (rem == 0)
                            Console.WriteLine(string.Format("{0} - Value: {1}", i, currentRandom));
                    }
                }
            }
            else
            {
                Console.WriteLine("File \"{0}\" already exists.", fileName);
                returnValue = false;
            }
            return returnValue;
        }

        private FileChange PrepareMD5FileChange(InputParams paramSet, CLCredential creds, string filePath, string fileName)
        {
            FileChange fileChange = new FileChange();
            string fullPath = filePath + '\\' + fileName;
            if (!File.Exists(filePath))
                WriteFile(filePath, fileName);
            byte[] md5Bytes = FileHelper.CreateFileChangeObject(fullPath, FileChangeType.Created, true, null, null, string.Empty, out fileChange);
            CLError hashError = fileChange.SetMD5(md5Bytes);
            if (hashError != null)
            {
                Exception[] exceptions = hashError.GrabExceptions().ToArray();
                lock (this.ProcessingErrorHolder)
                {
                    foreach (Exception ex in exceptions)
                    {
                        this.ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + ex;
                    }                    
                }                
            }
            return fileChange;
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
                    CLError metaDataRequestError =  syncBox.GetMetadataAtPath(filePath, false, ManagerConstants.TimeOutMilliseconds, out restStatus, out metaDataResponse);
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

        private int TryUpload(InputParams paramSet, CLCredential creds,  string filePath, FileInfo fileInfo, ref FileChange fileChange)
        {
            Console.WriteLine("Try Upload Entered...");
            CloudApiPublic.JsonContracts.Event returnEvent;
            CLSyncBox syncBox;
            CLSyncBoxCreationStatus boxCreateStatus;
            CLHttpRestStatus restStatus = new CLHttpRestStatus();
            int uploadResponseCode = 0;

            GenericHolder<CLError> errorHolder;
            if(this.ProcessingErrorHolder != null)
                errorHolder = this.ProcessingErrorHolder;
            else 
                errorHolder = new GenericHolder<CLError>();
     
            ICLSyncSettings settings = new AdvancedSyncSettings(InputParams.ManualSync_Folder.Replace("\"", ""));
            long syncBoxId = SyncBoxMapper.SyncBoxes.Count > 0 ? SyncBoxMapper.SyncBoxes[0] : paramSet.ManualSyncBoxID;
            CloudApiPublic.CLSyncBox.CreateAndInitialize(creds, syncBoxId, out syncBox, out boxCreateStatus, settings as ICLSyncSettings);
            CLError postFileError = syncBox.HttpRestClient.PostFileChange(fileChange, ManagerConstants.TimeOutMilliseconds, out restStatus, out returnEvent);
            if (postFileError != null || restStatus != CLHttpRestStatus.Success)
            {
                FileHelper.HandleUnsuccessfulUpload(fileChange, returnEvent, restStatus, postFileError, ManagerConstants.RequestTypes.PostFileChange, ref errorHolder);
            }
            string response = returnEvent.Header.Status.ToLower();
            switch (response)
            {
                case "upload":
                case "uploading":
                    uploadResponseCode = FileHelper.TryUpload(filePath, fileInfo.Name, syncBox, fileChange, restStatus, returnEvent, ref errorHolder);
                    break;
                case "duplicate":
                case "exists":
                    //ThrowDuplicateException(ref ProcessingErrorHolder);
                    uploadResponseCode = RenameAndTryUpload(paramSet, syncBox, creds, fileChange, fileInfo, filePath, restStatus, returnEvent, ref errorHolder);
                    break;
                case "conflict":
                    uploadResponseCode = RenameAndTryUpload(paramSet, syncBox, creds, fileChange, fileInfo, filePath, restStatus, returnEvent, ref errorHolder);
                    break;
                default:
                    Console.Write(string.Format("The Server Response is {0}", returnEvent.Header.Status));
                    break;

            }
            Console.WriteLine("TryUpload Exited...");
            lock (ProcessingErrorHolder)
            {
                IEnumerable<Exception> exceptions = errorHolder.Value.GrabExceptions();
                foreach (Exception exception in exceptions)
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
            }
            return uploadResponseCode;            
        }
        #endregion

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
        public override int Delete(Settings.InputParams paramSet, string filePath)
        {
            int deleteResponseCode = 0;
            CLCredential creds;
            CLCredentialCreationStatus credsCreateStatus;
            CLSyncBox syncBox;
            CLSyncBoxCreationStatus boxCreateStatus;
            CLHttpRestStatus restStatus = new CLHttpRestStatus();
            
            InitalizeCredentials("ManualSyncManager.CreateFile", out creds, out credsCreateStatus);
            ICLSyncSettings settings = new AdvancedSyncSettings(InputParams.ManualSync_Folder.Replace("\"", ""));
            if (credsCreateStatus != CLCredentialCreationStatus.Success)
            {
                Console.WriteLine("There was an error Crteating Credentials In Create File Method. Credential Create Status: {0}", credsCreateStatus.ToString());
                Console.WriteLine("Exiting Process...");
                return (int)FileManagerResponseCodes.InitializeCredsError;
            }
            CloudApiPublic.JsonContracts.Event returnEvent; 
            long syncBoxId = SyncBoxMapper.SyncBoxes.Count > 0 ? SyncBoxMapper.SyncBoxes[0] : paramSet.ManualSyncBoxID;
            CloudApiPublic.CLSyncBox.CreateAndInitialize(creds, syncBoxId, out syncBox, out boxCreateStatus, settings as ICLSyncSettings);
            FileChange fileChange = PrepareFileChangeForModification(paramSet, FileChangeType.Deleted, syncBox, filePath);
            CLError postFileError = syncBox.HttpRestClient.PostFileChange(fileChange, ManagerConstants.TimeOutMilliseconds, out restStatus, out returnEvent);
            if (postFileError != null || restStatus != CLHttpRestStatus.Success)
            {
                if (postFileError != null)
                {
                    Exception[] exceptions = postFileError.GrabExceptions().ToArray();
                    lock (this.ProcessingErrorHolder)
                    {
                        foreach (Exception exception in exceptions)
                        {
                            this.ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
                        }                        
                    }                    
                }
                if (restStatus != CLHttpRestStatus.Success)
                { 
                    FileInfo info = new FileInfo(filePath);
                    Exception unsuccessfulDeleteError = new Exception(string.Format("The Returned Response for Attempt to Delete File {0} is {1}", info.Name, restStatus));
                }                
            }
            return deleteResponseCode;
        }

        public override int Undelte(Settings.InputParams paramSet)
        {
            CLCredential creds;
            CLCredentialCreationStatus credsCreateStatus;
            CLSyncBox syncBox;
            CLSyncBoxCreationStatus boxCreateStatus;
            ICLSyncSettings settings = new CLSyncSettings(paramSet.ActiveSync_Folder);            
            //TODO: Add Undelete File Functionality
            
            return 0;
        }

        public override int Rename(Settings.InputParams paramSet, string directoryRelativeToRoot, string oldFileName, string newFileName)
        {
            int renameResponseCode = 0;
            CLCredential creds;
            CLCredentialCreationStatus credsCreateStatus;
            CLSyncBox syncBox;
            CLSyncBoxCreationStatus boxCreateStatus;
            CLHttpRestStatus restStatus = new CLHttpRestStatus();
            ICLSyncSettings settings = new AdvancedSyncSettings(paramSet.ManualSync_Folder.Replace("\"", ""));

            InitalizeCredentials("ManualSync.RenameFile", out creds, out credsCreateStatus);
            if (credsCreateStatus != CLCredentialCreationStatus.Success)
            {
                Console.WriteLine("There was an error Crteating Credentials In Create File Method. Credential Create Status: {0}", credsCreateStatus.ToString());
                Console.WriteLine("Exiting Process...");
                return (int)FileManagerResponseCodes.InitializeCredsError;
            }
            string fullPath = directoryRelativeToRoot + oldFileName;
            CloudApiPublic.JsonContracts.Event returnEvent;
            long syncBoxId = SyncBoxMapper.SyncBoxes.Count > 0 ? SyncBoxMapper.SyncBoxes[0] : paramSet.ManualSyncBoxID;
            CloudApiPublic.CLSyncBox.CreateAndInitialize(creds, syncBoxId, out syncBox, out boxCreateStatus, settings as ICLSyncSettings);
            FileChange fileChange = PrepareFileChangeForModification(paramSet, FileChangeType.Renamed, syncBox, fullPath);
            
            CLError postFileError = syncBox.HttpRestClient.PostFileChange(fileChange, ManagerConstants.TimeOutMilliseconds, out restStatus, out returnEvent);

            return 0;
        }

        #region Download All Content

        /// <summary>
        /// This method is called to overwrite the entire contents of the clients Machine Sync Path with the current SyncBox content
        /// </summary>
        /// <param name="smokeTask"></param>
        /// <param name="ProcessingErrorHolder"></param>
        /// <returns>
        ///     int downloadAllResponseCode -- Description of Completion of Process 
        /// </returns>
        public int InitiateDownloadAll(SmokeTask smokeTask, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            CurrentTask = smokeTask;
            if (this.ProcessingErrorHolder == null)
                this.ProcessingErrorHolder = ProcessingErrorHolder;
            int dloadAllResponseCode = 0;
            CLCredential creds; 
            CLCredentialCreationStatus credsCreateStatus;
            // Try to Create a Credentail set to make the Change 
            InitalizeCredentials("ManualSyncManager.InitiateDownloadAll", out creds, out credsCreateStatus);
            // If Status returns anything other than success notify the user and stop the process.
            if (credsCreateStatus != CLCredentialCreationStatus.Success)
            {
                Console.WriteLine("There was an error Creating Credentials Initiate Download All Method. Credential Create Status: {0}", credsCreateStatus.ToString());
                Console.WriteLine("Exiting Process...");
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
            GetAllContentFromSyncBox(syncBox, smokeTask, creds, ref ProcessingErrorHolder);
            return dloadAllResponseCode; 
        }

        private void GetAllContentFromSyncBox(CLSyncBox syncBox, SmokeTask smokeTask, CLCredential creds, ref GenericHolder<CLError> ProcessingErrorHolder)
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
                    HandleGetFolderHierarchyFailure(getAllContentError, restStatus, ref ProcessingErrorHolder);
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
                HandleAdd(mdObject, syncBox);

            Console.WriteLine(string.Format("Add File Counter: {0}", AddFileCounter.ToString()));           

        }

        private void HandleAdd(CloudApiPublic.JsonContracts.Metadata mdObject, CLSyncBox syncBox)
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
            if (downloadError != null)
            {
                IEnumerable<Exception> exceptions = downloadError.GrabExceptions();
                foreach (Exception ex in exceptions)
                {
                    lock (ProcessingErrorHolder)
                    {
                        ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + ex;
                    }
                }
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
        
        private void HandleGetFolderHierarchyFailure(CLError getFolderHierarchyError, CLHttpRestStatus restStatus, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            if (getFolderHierarchyError != null)
            {
                foreach (Exception exception in getFolderHierarchyError.GrabExceptions())
                {
                    lock (ProcessingErrorHolder)
                    {
                        ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
                    }
                }
            }
            if (restStatus != CLHttpRestStatus.Success)
            {
                Exception exception = new Exception(string.Format("The Status Returned From GetFolderHeirarchy is {0}", restStatus.ToString()));
                lock (ProcessingErrorHolder)
                {
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
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

        private void InitalizeCredentials(string callerName, out CLCredential creds, out CLCredentialCreationStatus credsCreateStatus)
        {
            Console.WriteLine("Initializing Credentials for Active Sync Create Method... ");
            Console.WriteLine();
            CLCredential.CreateAndInitialize(InputParams.API_Key, InputParams.API_Secret, out creds, out credsCreateStatus);
            Console.WriteLine(string.Format("Credential Initialization {0}", credsCreateStatus.ToString()));
        }

        private void ThrowDuplicateException(ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            Exception outerException = new Exception("Attepmting to CreateFile duplicate file.");
            lock (ProcessingErrorHolder)
            {
                ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + outerException;
            }
        }
        #endregion 
    }
}
