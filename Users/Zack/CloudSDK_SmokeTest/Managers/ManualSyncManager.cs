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

        #region Properties 
        public InputParams InputParams { get; set; }

        #endregion 


        #region Init
        public ManualSyncManager(InputParams paramSet)
        {
            this.InputParams = paramSet;
        }
        #endregion 

        #region Create File

        public override int Create(Settings.InputParams paramSet, FileInfo fileInfo, string fileName, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            int uploadResponseCode = 0;
            CLCredential creds; CLCredentialCreationStatus credsCreateStatus;
            DateTime currentTime = DateTime.UtcNow;
 
            string filePath = fileInfo.FullName;
            if (!File.Exists(filePath))
                WriteFile(fileInfo.FullName, fileInfo.Name);

            // Try to Create a Credentail set to make the Change 
            InitalizeCredentials(out creds, out credsCreateStatus);
            // If Status returns anything other than success notify the user and stop the process.
            if (credsCreateStatus != CLCredentialCreationStatus.Success)
            {
                Console.WriteLine("There was an error Crteating Credentials In Create File Method. Credential Create Status: {0}", credsCreateStatus.ToString());
                Console.WriteLine("Exiting Process...");
                uploadResponseCode = (int)FileManagerResponseCodes.InitializeCredsError;
                return uploadResponseCode;
            }
            // FileChange object defines the File and its metadata that will be opperated in 
            FileChange fileChange =  PrepareMD5FileChange(paramSet, creds,  filePath, fileName, ref ProcessingErrorHolder);
            //return TryUpload(paramSet, creds, filePath, fileInfo, ref fileChange, ref ProcessingErrorHolder); 
            Console.WriteLine("Try Upload Entered...");
            CloudApiPublic.JsonContracts.Event returnEvent;
            CLSyncBox syncBox;
            CLSyncBoxCreationStatus boxCreateStatus;
            CLHttpRestStatus restStatus = new CLHttpRestStatus();
            ICLSyncSettings settings = new CLSyncSettings(paramSet.ActiveSync_Folder);
            CloudApiPublic.CLSyncBox.CreateAndInitialize(creds, paramSet.ActiveSyncBoxID, out syncBox, out boxCreateStatus, settings as ICLSyncSettings);
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
                    uploadResponseCode = FileHelper.TryUpload(filePath, fileInfo.Name, syncBox, fileChange, restStatus, returnEvent, ref ProcessingErrorHolder);
                    break;
                case "duplicate":
                case "exists":
                    //ThrowDuplicateException(ref ProcessingErrorHolder);
                    uploadResponseCode = RenameAndTryUpload(paramSet, syncBox, creds, fileChange, fileInfo, filePath, restStatus, returnEvent, ref ProcessingErrorHolder);
                    break;
                case "conflict":
                    uploadResponseCode = RenameAndTryUpload(paramSet, syncBox, creds, fileChange, fileInfo, filePath, restStatus, returnEvent, ref ProcessingErrorHolder);
                    break;
                default:
                    Console.Write(string.Format("The Server Response is {0}", returnEvent.Header.Status));
                    break;

            }
            return uploadResponseCode;
        }

        private int RenameAndTryUpload(InputParams paramSet,CLSyncBox syncBox, CLCredential creds, FileChange fileChange, FileInfo fileInfo, string filePath, CLHttpRestStatus restStatus, CloudApiPublic.JsonContracts.Event returnEvent, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            FileChange newFileChange = CreateFileChangeWithNewName(fileChange, ref ProcessingErrorHolder);
            TryUpload(paramSet, creds, newFileChange.NewPath.ToString(), fileInfo, ref newFileChange, ref ProcessingErrorHolder);
            return 0;
        }

        private FileChange CreateFileChangeWithNewName(FileChange oldFileChange, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            FileChange fileChange = new FileChange();
            bool isDuplicate = true;
            int counter = 0;
            int newPathCharCount = oldFileChange.NewPath.Name.Count();
            string newFileName = oldFileChange.NewPath.Name;
            while (isDuplicate)
            {                
                counter++;
                if (!newFileName.Contains("_Copy"))
                {
                    newFileName = FileHelper.CreateNewFileName(oldFileChange.NewPath.ToString(), false, ref ProcessingErrorHolder);
                    if (!File.Exists(newFileName))
                    {
                        try
                        {
                            File.Copy(oldFileChange.NewPath.ToString(), newFileName);
                            byte[] md5 = FileHelper.CreateFileChangeObject(newFileName, FileChangeType.Created, true, null, out fileChange);
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

                        newFileName = FileHelper.CreateNewFileName(newFileName, true, ref ProcessingErrorHolder);
                        string newFullPath = oldFileChange.NewPath.Parent.ToString() + '\\' + newFileName;
                        if (!File.Exists(newFullPath))
                        {
                            byte[] md5 = null;
                            CreateReplaceFileAndCreateFileChangeObject(newFileName, oldFileChange, ref md5, ref fileChange, ref ProcessingErrorHolder);
                            fileChange.SetMD5(md5);
                            isDuplicate = false;
                        }
                    }
                }
                //If the name already contains "_Copy" 
                else
                {
                    string fullPath = oldFileChange.NewPath.Parent.ToString() + '\\' + newFileName;
                    newFileName = FileHelper.CreateNewFileName(fullPath, true, ref ProcessingErrorHolder);
                    string newFullPath = oldFileChange.NewPath.Parent.ToString() + '\\' + newFileName;
                    if (!File.Exists(newFullPath))
                    {
                        byte[] md5 = null;
                        CreateReplaceFileAndCreateFileChangeObject(newFileName, oldFileChange, ref md5, ref fileChange, ref ProcessingErrorHolder);
                        isDuplicate = false;
                    }
                }
            }
            return fileChange;
        }

        private void CreateReplaceFileAndCreateFileChangeObject(string newFileName, FileChange oldFileChange, ref byte[] md5, ref FileChange fileChange, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            try
            {
                string fullPath = oldFileChange.NewPath.Parent.ToString() + '\\' + newFileName; 
                WriteFile(fullPath, newFileName);
                //File.Create(oldFileChange.NewPath.Parent.ToString() + newFileName);
                //File.Replace(oldFileChange.NewPath.ToString(), oldFileChange.NewPath.Parent.ToString() + newFileName, newFileName + "_bak");
                md5 = FileHelper.CreateFileChangeObject(fullPath, FileChangeType.Created, true, null, out fileChange);
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

        private bool WriteFile(string fullPath, string fileName)
        {
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

        private FileChange PrepareMD5FileChange(InputParams paramSet, CLCredential creds, string filePath, string fileName, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            FileChange fileChange = new FileChange();
            if (!File.Exists(filePath))
                WriteFile(filePath, fileName);
            byte[] md5Bytes = FileHelper.CreateFileChangeObject(filePath, FileChangeType.Created, true, null, out fileChange);
            CLError hashError = fileChange.SetMD5(md5Bytes);
            if (hashError != null)
            {
                IEnumerable<Exception> exceptions = hashError.GrabExceptions();
                foreach (Exception exception in exceptions)
                {
                    lock (ProcessingErrorHolder)
                    {
                        ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
                    }
                }
            }
            return fileChange;
        }

        private int TryUpload(InputParams paramSet, CLCredential creds,  string filePath, FileInfo fileInfo, ref FileChange fileChange, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            Console.WriteLine("Try Upload Entered...");
            CloudApiPublic.JsonContracts.Event returnEvent;
            CLSyncBox syncBox;
            CLSyncBoxCreationStatus boxCreateStatus;
            CLHttpRestStatus restStatus = new CLHttpRestStatus();
            int uploadResponseCode = 0;
            ICLSyncSettings settings = new AdvancedSyncSettings(true, TraceType.CommunicationIncludeAuthorization | TraceType.FileChangeFlow, "C:\\Users\\Public\\Documents\\Cloud", false, 9, "SimpleClient", null, "SmokeTest1", "Smoke Test", paramSet.ManualSync_Folder, null); 
            CloudApiPublic.CLSyncBox.CreateAndInitialize(creds, paramSet.ActiveSyncBoxID, out syncBox, out boxCreateStatus, settings as ICLSyncSettings);
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
                    uploadResponseCode = FileHelper.TryUpload(filePath, fileInfo.Name, syncBox, fileChange, restStatus, returnEvent, ref ProcessingErrorHolder);
                    break;
                case "duplicate":
                case "exists":
                    //ThrowDuplicateException(ref ProcessingErrorHolder);
                    uploadResponseCode = RenameAndTryUpload(paramSet, syncBox, creds, fileChange, fileInfo, filePath, restStatus, returnEvent, ref ProcessingErrorHolder);
                    break;
                case "conflict":
                    uploadResponseCode = RenameAndTryUpload(paramSet, syncBox, creds, fileChange, fileInfo, filePath, restStatus, returnEvent, ref ProcessingErrorHolder);
                    break;
                default:
                    Console.Write(string.Format("The Server Response is {0}", returnEvent.Header.Status));
                    break;

            }
            Console.WriteLine("TryUpload Exited...");
            return uploadResponseCode;            
        }
        #endregion 

        public override int Delete(Settings.InputParams paramSet)
        {
            CLCredential creds;
            CLCredentialCreationStatus credsCreateStatus;
            CLSyncBox syncBox;
            CLSyncBoxCreationStatus boxCreateStatus;
            ICLSyncSettings settings = new CLSyncSettings(paramSet.ActiveSync_Folder);
            //TODO: Add Delete File Functionality 
            return 0;
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

        public override int Rename(Settings.InputParams paramSet)
        {
            CLCredential creds;
            CLCredentialCreationStatus credsCreateStatus;
            CLSyncBox syncBox;
            CLSyncBoxCreationStatus boxCreateStatus;
            ICLSyncSettings settings = new CLSyncSettings(paramSet.ActiveSync_Folder);
            //TODO: Add Rename File Functionality
            return 0;
        }

        #region Download All Content
        public int InitiateDownloadAll(SmokeTask smokeTask, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            int dloadAllResponseCode = 0;
            CLCredential creds; 
            CLCredentialCreationStatus credsCreateStatus;
            // Try to Create a Credentail set to make the Change 
            InitalizeCredentials(out creds, out credsCreateStatus);
            // If Status returns anything other than success notify the user and stop the process.
            if (credsCreateStatus != CLCredentialCreationStatus.Success)
            {
                Console.WriteLine("There was an error Creating Credentials Initiate Download ALl Method. Credential Create Status: {0}", credsCreateStatus.ToString());
                Console.WriteLine("Exiting Process...");
                dloadAllResponseCode = (int)FileManagerResponseCodes.InitializeCredsError;
                return dloadAllResponseCode;
            }

            CloudApiPublic.JsonContracts.Event returnEvent;
            CLSyncBox syncBox;
            CLSyncBoxCreationStatus boxCreateStatus;
            CLHttpRestStatus restStatus = new CLHttpRestStatus();

            ICLSyncSettings settings = new AdvancedSyncSettings(true, 
                                                                TraceType.CommunicationIncludeAuthorization | TraceType.FileChangeFlow, 
                                                                "C:\\Users\\Public\\Documents\\Cloud", 
                                                                false, 
                                                                9, 
                                                                "SimpleClient", 
                                                                null, 
                                                                "SmokeTest1", 
                                                                "Smoke Test", 
                                                                InputParams.ManualSync_Folder, 
                                                                null);

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
            CLHttpRestStatus restStatus;
            CloudApiPublic.JsonContracts.FolderContents folderContents = new CloudApiPublic.JsonContracts.FolderContents();
            try
            {
                CLError getAllContentError = syncBox.GetFolderContents(ManagerConstants.TimeOutMilliseconds, out restStatus, out folderContents, includeCount: false, contentsRoot:null, depthLimit:null, includeDeleted:false);
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
            System.Threading.Tasks.Parallel.ForEach(folderContents.Objects, (mdObject) => {
                    string settingsFolderPath = downloadAllTest.FolderPath;
                    if (mdObject.IsFolder == true)
                        HandleAddFolder(mdObject, syncBox, settingsFolderPath);
                    else if (mdObject.IsFolder == false)
                        HandleAddFile(creds, mdObject, syncBox, settingsFolderPath);
            });
        }

        private void HandleAddFolder(CloudApiPublic.JsonContracts.Metadata mdObject, CLSyncBox syncBox, string settingsFolderPath)
        {
            string pathToAssign = settingsFolderPath;
            if (settingsFolderPath.LastIndexOf('\\') == (settingsFolderPath.Count() - 1))
                pathToAssign.Remove(settingsFolderPath.LastIndexOf('\\'), 1);
            string directoryPath = settingsFolderPath.Replace("\"", "") + '\\' + '\\' + mdObject.RelativePathWithoutEnclosingSlashes.Replace("\"", "");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        private void HandleAddFile(CLCredential creds, CloudApiPublic.JsonContracts.Metadata mdObject, CLSyncBox syncBox, string settingsFolderPath)
        {
            CLHttpRestStatus restStatus;
            string pathToAssign = settingsFolderPath;
            if (settingsFolderPath.LastIndexOf('\\') == (settingsFolderPath.Count() - 1))
                pathToAssign.Remove(settingsFolderPath.LastIndexOf('\\'), 1);
            string filePath = settingsFolderPath.Replace("\"", "") + '\\' + '\\' + mdObject.RelativePathWithoutEnclosingSlashes.Replace("/", "\\\\").Replace("\"", "");
            if (!File.Exists(filePath))
            { 
                //Possibly need to create a FileChange off of the current metadata
                //Move file Upon copletion -- give an old path (temp) and a new path (truePath), move the file form old to new 
                FileChange currentFile;
                FileHelper.CreateFileChangeObject(filePath, FileChangeType.Created, false, mdObject.Size, out currentFile);
                object state = new object();
                CLError downloadError = syncBox.HttpRestClient.DownloadFile(currentFile, AfterDownloadCallback , state, ManagerConstants.TimeOutMilliseconds, out restStatus);
                
            
            }
        }

        private void AfterDownloadCallback(string inputString, FileChange fileChange, ref string refString, object state, Guid id)
        { 
            //This is where we will move the file from temp styorage to its place on the disk. 
            if (!string.IsNullOrEmpty(inputString))
            { 
                
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

        private void WriteSyncBoxObjectToLocal()
        { 
            
        }
        #endregion 

        #region All

        private void InitalizeCredentials(out CLCredential creds, out CLCredentialCreationStatus credsCreateStatus)
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
