using Cloud;
using Cloud.Interfaces;
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
    public class DownloadManager: ISmokeTaskManager
    {
        #region Fields
        int _addFileCounter = 0;
        int _callbackCounter = 0;
        #endregion

        public GenericHolder<CLError> ProcessingErrorHolder { get; set; }
        public DirectoryInfo RootDirectory { get; set; }

        #region Implementation
        public int InitiateDownloadAll(SmokeTestManagerEventArgs e)
        {
            DownloadAllSyncBoxContent CurrentTask  = e.CurrentTask as DownloadAllSyncBoxContent;
            RootDirectory = e.RootDirectory;
            int dloadAllResponseCode = 0;
            ICLCredentialSettings settings = new AdvancedSyncSettings(e.ParamSet.ManualSync_Folder.Replace("\"", ""));
            CLError credsError;
            TaskEventArgs taskArgs = e as TaskEventArgs;
            CredentialHelper.InitializeCreds(ref taskArgs, out settings, out credsError);  

            CLSyncBox syncBox;
            CLSyncBoxCreationStatus boxCreateStatus;
            long syncBoxId = SmokeTaskManager.GetOpperationSyncBoxID(e);
            CLError error = Cloud.CLSyncBox.CreateAndInitialize(e.Creds,
                                                            syncBoxId,
                                                            out syncBox,
                                                            out boxCreateStatus,
                                                            settings as ICLSyncSettings);

            if (boxCreateStatus != CLSyncBoxCreationStatus.Success)
            {
                ExceptionManagerEventArgs failArgs = new ExceptionManagerEventArgs()
                {
                    SyncBoxCreateStatus = boxCreateStatus,
                    OpperationName = "DownloadManager.InitiateDownloadAll",
                    Error = error,
                    ProcessingErrorHolder = e.ProcessingErrorHolder,
                };
                SmokeTaskManager.HandleFailure(failArgs);
                return (int)FileManagerResponseCodes.InitializeSynBoxError;
            }


            GenericHolder<CLError> refHolder = e.ProcessingErrorHolder;
            dloadAllResponseCode = BeginGetAllContent(e, CurrentTask, ref syncBox);
            return dloadAllResponseCode;
        }
        #endregion

        #region Private
        private int BeginGetAllContent(SmokeTestManagerEventArgs e, DownloadAllSyncBoxContent currentTask, ref CLSyncBox syncBox)
        {
            int responseCode = 0;
            CLHttpRestStatus restStatus;
            Cloud.JsonContracts.FolderContents folderContents;
            CLError getAllContentError = syncBox.GetFolderContents(ManagerConstants.TimeOutMilliseconds, out restStatus, out folderContents, includeCount: false, contentsRoot: null, depthLimit: 9, includeDeleted: false);
            if (restStatus != CLHttpRestStatus.Success || getAllContentError != null)
            {
                ExceptionManagerEventArgs failArgs = new ExceptionManagerEventArgs()
                {
                    Error = getAllContentError,
                    RestStatus = restStatus,
                    OpperationName = "DownloadManager.BeginGetALlContent(syncBox.GetFolderContents())",
                };
                SmokeTaskManager.HandleFailure(failArgs);
                return (int)FileManagerResponseCodes.UnknownError;
            }
            foreach (Cloud.JsonContracts.Metadata mdObject in folderContents.Objects)
            {
                HandleAdd(e, mdObject, syncBox);
            }
            Console.WriteLine(string.Format("Add File Counter: {0}", _addFileCounter.ToString()));
            return responseCode;           
        }

        private void HandleAdd(SmokeTestManagerEventArgs e, Cloud.JsonContracts.Metadata mdObject, CLSyncBox syncBox)
        {
            if (mdObject.IsFolder.HasValue && mdObject.IsFolder.Value)
            {
                string directoryPath = e.RootDirectory.FullName + mdObject.RelativePathWithoutEnclosingSlashes.Replace("\"", "");
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
            }
            else
            {
                _addFileCounter++;
                string filePath = e.RootDirectory.FullName + mdObject.RelativePathWithoutEnclosingSlashes.Replace("/", "\\").Replace("\"", "");
                FileInfo fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    CompleteAddFile(syncBox, filePath, mdObject, e);
                }
            }
        }

        private int CompleteAddFile(CLSyncBox syncBox, string filePath, Cloud.JsonContracts.Metadata mdObject, SmokeTestManagerEventArgs e)
        { 
            int responseCode = 0;
            FileChange currentFile;
            CLHttpRestStatus restStatus;
            FileHelper.CreateFileChangeObject(filePath, FileChangeType.Created, false, mdObject.Size, mdObject.StorageKey, mdObject.ServerId, out currentFile);
            currentFile.Direction = SyncDirection.From;
            object state = new object();
            CLError downloadError = syncBox.HttpRestClient.DownloadFile(currentFile, AfterDownloadCallback, state, ManagerConstants.TimeOutMilliseconds, out restStatus);
            if (downloadError != null || restStatus != CLHttpRestStatus.Success)
            {
                GenericHolder<CLError> refProcessingErrorHolder = e.ProcessingErrorHolder;
                ManualSyncManager.HandleFailure(downloadError, restStatus, null, "CompleteAddFile", ref refProcessingErrorHolder);
                responseCode = (int)FileManagerResponseCodes.UnknownError;
            }
            if (this.ProcessingErrorHolder == null)
                this.ProcessingErrorHolder = e.ProcessingErrorHolder;
            return responseCode;
            
        }

        private void AfterDownloadCallback(string inputString, FileChange fileChange, ref string refString, object state, Guid id)
        {
            _callbackCounter++;
            List<Exception> exList = new List<Exception>();
            if (string.IsNullOrEmpty(inputString) || !File.Exists(inputString))
            {
                exList.Add(ExceptionManager.ReturnException("DownloadManager.AfterDownloadCallback", "The local file can not be reached"));
            }
            
            string rootString = RootDirectory.ToString().Replace(RootDirectory.Name + '\\', RootDirectory.Name);
            string destination = rootString + fileChange.NewPath.GetRelativePath(RootDirectory, false);
            if (File.Exists(destination))
            {
                exList.Add(ExceptionManager.ReturnException("DownloadManager.AfterDownloadCallback", "There is already a File at the File Path returned from download response"));
            }
            Exception moveException = TryFileMove(inputString, destination);
            if (moveException != null)
                exList.Add(moveException);
            lock (ProcessingErrorHolder)
            {
                foreach (Exception ex in exList)
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + ex;
            }
        }

        private Exception TryFileMove(string inputString, string destination)
        {
            try
            {
                File.Move(inputString, destination);
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }
        #endregion 

        #region Interface Implementation
        public int Download(Events.ManagerEventArgs.SmokeTestManagerEventArgs e)
        {
            int responseCode = -1;
            try
            {
                Console.WriteLine("Initiating Download All Content...");
                if (e.CurrentTask.SyncType == SmokeTaskSyncType.Active)
                    e.RootDirectory = new DirectoryInfo(e.ParamSet.ActiveSync_Folder.Replace("\"", ""));
                else
                    e.RootDirectory = new DirectoryInfo(e.ParamSet.ManualSync_Folder.Replace("\"", ""));

                responseCode = InitiateDownloadAll(e);
                Console.WriteLine("End Download All Content...");
            }
            catch (Exception exception)
            {
                lock (e.ProcessingErrorHolder)
                {
                    e.ProcessingErrorHolder.Value = e.ProcessingErrorHolder.Value + exception;
                }
            }
            return responseCode;
        }

        #region Not Implementaed
        public int Create(Events.ManagerEventArgs.SmokeTestManagerEventArgs e)
        {
            throw new NotImplementedException();
        }

        public int Rename(Events.ManagerEventArgs.SmokeTestManagerEventArgs e)
        {
            throw new NotImplementedException();
        }

        public int Delete(Events.ManagerEventArgs.SmokeTestManagerEventArgs e)
        {
            throw new NotImplementedException();
        }

        public int UnDelete(Events.ManagerEventArgs.SmokeTestManagerEventArgs e)
        {
            throw new NotImplementedException();
        }  

        public int ListItems(Events.ManagerEventArgs.SmokeTestManagerEventArgs e)
        {
            throw new NotImplementedException();
        }

        public int AlternativeAction(SmokeTestManagerEventArgs e)
        {
            throw new NotImplementedException();
        }
        #endregion 
        #endregion
    }
}
