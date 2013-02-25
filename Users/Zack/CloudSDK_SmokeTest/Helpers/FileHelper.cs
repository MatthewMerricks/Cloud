using CloudApiPublic;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
using CloudSDK_SmokeTest.Events.CLEventArgs;
using CloudSDK_SmokeTest.Events.ManagerEventArgs;
using CloudSDK_SmokeTest.Managers;
using CloudSDK_SmokeTest.Settings;
using CloudSDK_SmopkeTest.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace CloudSDK_SmokeTest.Helpers
{
    public class FileHelper
    {
        #region Create 
        public static int CreateFile(Settings.InputParams InputParams, ManualSyncManager manager,  CreateFileEventArgs createEventArgs)
        {
            GenericHolder<CLError> refProcessErrorHolder = createEventArgs.ProcessingErrorHolder;
            int createReturnCode = 0;
            string fullPath = createEventArgs.CreateTaskFileInfo.FullName;
            if (!File.Exists(createEventArgs.CreateTaskFileInfo.FullName))
                WriteFile(createEventArgs.CreateTaskFileInfo);

            FileChange fileChange = PrepareMD5FileChange(InputParams, createEventArgs.Creds, createEventArgs.CreateTaskFileInfo, ref refProcessErrorHolder);
            CloudApiPublic.JsonContracts.Event returnEvent;
            CLHttpRestStatus restStatus = new CLHttpRestStatus();

            CLError postFileError = createEventArgs.SyncBox.HttpRestClient.PostFileChange(fileChange, ManagerConstants.TimeOutMilliseconds, out restStatus, out returnEvent);
            if (postFileError != null || restStatus != CLHttpRestStatus.Success)
            {                
                FileHelper.HandleUnsuccessfulUpload(restStatus, postFileError, ManagerConstants.RequestTypes.PostFileChange, ref refProcessErrorHolder);
            }
            string response = returnEvent.Header.Status.ToLower();
            CreateFileResponseEventArgs responseArgs = new CreateFileResponseEventArgs(createEventArgs, fileChange, response, restStatus, returnEvent);
            createReturnCode = FileHelper.CreateFileResponseSwitch(responseArgs, fileChange, manager, ref refProcessErrorHolder);
            return createReturnCode;
        }

        private static int RenameAndTryUpload(CreateFileEventArgs responseArgs, FileChange currentFileChange, ManualSyncManager manager)
        {
            FileChange newFileChange = manager.CreateFileChangeWithNewName(currentFileChange);
            manager.TryUpload(responseArgs, newFileChange);
            return 0;
        }

        public static FileChange PrepareMD5FileChange(InputParams paramSet, CLCredential creds, FileInfo fi,  ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            FileChange fileChange = new FileChange();
            if (!File.Exists(fi.FullName))
                WriteFile(fi);
            byte[] md5Bytes = FileHelper.CreateFileChangeObject(fi.FullName, FileChangeType.Created, true, null, null, string.Empty, out fileChange);
            CLError hashError = fileChange.SetMD5(md5Bytes);
            if (hashError != null)
            {
                Exception[] exceptions = hashError.GrabExceptions().ToArray();
                lock (ProcessingErrorHolder)
                {
                    foreach (Exception ex in exceptions)
                    {
                        ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + ex;
                    }
                }
            }
            return fileChange;
        }

         public static FileChange PrepareMD5FileChange(SmokeTestManagerEventArgs e)
        {
            FileChange fileChange = new FileChange();
            if (!File.Exists(e.FileInfo.FullName))
                WriteFile(e.FileInfo);
            byte[] md5Bytes = FileHelper.CreateFileChangeObject(e.FileInfo.FullName, FileChangeType.Created, true, null, null, string.Empty, out fileChange);
            CLError hashError = fileChange.SetMD5(md5Bytes);
            if (hashError != null)
            {
                Exception[] exceptions = hashError.GrabExceptions().ToArray();
                lock (e.ProcessingErrorHolder)
                {
                    foreach (Exception ex in exceptions)
                    {
                        e.ProcessingErrorHolder.Value = e.ProcessingErrorHolder.Value + ex;
                    }
                }
            }
            return fileChange;
        }

        
        public static bool WriteFile(string path, string fileName)
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
                        foreach (Byte b in bytes)
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

        public static bool WriteFile(FileInfo fileInfo)
        {
            string fullPath = fileInfo.FullName;
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
                        foreach (Byte b in bytes)
                            fs.WriteByte(b);

                        int rem = i % 100;
                        if (rem == 0)
                            Console.WriteLine(string.Format("{0} - Value: {1}", i, currentRandom));
                    }
                }
            }
            else
            {
                Console.WriteLine("File \"{0}\" already exists.", fileInfo.Name);
                returnValue = false;
            }
            return returnValue;
        }

        public static string CreateNewFileName(string filePath, bool isCopy, bool isCaseSentitiveIssue, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            string returnValue = string.Empty;
            int pathEndsAt = filePath.LastIndexOf('\\');
            int fileNameBeginsAt = pathEndsAt + 1;
            int extensionBeginsAt = filePath.LastIndexOf('.');
            string suffix = filePath.Substring(extensionBeginsAt, ((filePath.Count()) - extensionBeginsAt));
            StringBuilder builder = new StringBuilder(filePath.Substring(0, pathEndsAt) + '\\');
            if (!isCopy && !isCaseSentitiveIssue)
            {
                builder.Append(filePath.Substring(pathEndsAt, (filePath.Count() - pathEndsAt)).Replace(suffix, "") + "_Copy" + suffix);
                returnValue = builder.ToString();
            }
            else if (!isCopy && isCaseSentitiveIssue)
            {
                string fileName = filePath.Substring(fileNameBeginsAt, (filePath.Count() - fileNameBeginsAt)).Replace(suffix, "");
                builder.Append("BADNAME_" + fileName + suffix);
                returnValue = builder.ToString();
            }
            else
            {
                builder = new StringBuilder(filePath.Substring(0, pathEndsAt) + '\\');
                string fileName = filePath.Substring(pathEndsAt + 1, (filePath.Count() - (pathEndsAt + 1))).Replace(suffix, "");
                int endOfCopy = fileName.LastIndexOf("_Copy") + 4;
                int length = (fileName.Count()-1) - endOfCopy;
                string appendedToCopy = fileName.Substring(endOfCopy+1, length);
                int nCopy = 0;
                Int32.TryParse(appendedToCopy, out nCopy);
                if (nCopy >= 0)
                    nCopy++;
                else
                {
                    Exception ex = new Exception("RenameFileError: Error Incrementing File Name.");
                    lock (ProcessingErrorHolder)
                    {
                        ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + ex;
                    }
                }
                returnValue = fileName.Substring(0, (endOfCopy + 1)) + nCopy + suffix;
            }
            return returnValue;
        }

        public static string CreateNewFileName(SmokeTestManagerEventArgs e, bool isCopy)
        {
            string returnValue = string.Empty;
            string filePath = e.FileInfo.FullName;
            int pathEndsAt = e.FileInfo.FullName.LastIndexOf('\\');
            int fileNameBeginsAt = pathEndsAt + 1;
            int extensionBeginsAt = filePath.LastIndexOf('.');
            string suffix = filePath.Substring(extensionBeginsAt, ((filePath.Count()) - extensionBeginsAt));
            StringBuilder builder = new StringBuilder(filePath.Substring(0, pathEndsAt) + '\\');
            if (!isCopy)
            {
                builder.Append(filePath.Substring(pathEndsAt, (filePath.Count() - pathEndsAt)).Replace(suffix, "") + "_Copy" + suffix);
                returnValue = builder.ToString();
            }
            else
            {
                builder = new StringBuilder(filePath.Substring(0, pathEndsAt) + '\\');
                string fileName = filePath.Substring(pathEndsAt + 1, (filePath.Count() - (pathEndsAt + 1))).Replace(suffix, "");
                int endOfCopy = fileName.LastIndexOf("_Copy") + 4;
                int length = (fileName.Count() - 1) - endOfCopy;
                string appendedToCopy = fileName.Substring(endOfCopy + 1, length);
                int nCopy = 0;
                Int32.TryParse(appendedToCopy, out nCopy);
                if (nCopy >= 0)
                    nCopy++;
                else
                {
                    Exception ex = new Exception("RenameFileError: Error Incrementing File Name.");
                    lock (e.ProcessingErrorHolder)
                    {
                        e.ProcessingErrorHolder.Value = e.ProcessingErrorHolder.Value + ex;
                    }
                }
                returnValue = fileName.Substring(0, (endOfCopy + 1)) + nCopy + suffix;
            }
            return returnValue;
        }

        public static byte[] CreateFileChangeObject(string filePath, FileChangeType type, bool getHash, long? size, string storageKey, string serverId, out FileChange fileChange)
        {
            byte[] md5Bytes = null;
            long fileSize = 0;
            DateTime currentTime = DateTime.UtcNow;
            if (getHash)
            {
                md5Bytes = MD5_Helper.GetHashOfStream(
                                                        new System.IO.FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read),
                                                        out fileSize);
            }
            else
            {
                if (size.HasValue)
                {
                    fileSize = size.HasValue ? size.Value : 0;
                }
            }
            fileChange = new CloudApiPublic.Model.FileChange()
            {
                Direction = CloudApiPublic.Static.SyncDirection.To,
                Metadata = new CloudApiPublic.Model.FileMetadata()
                {
                    //TODO: Get the filesize of the file being uploaded 
                    HashableProperties = new CloudApiPublic.Model.FileMetadataHashableProperties(false, currentTime, currentTime, fileSize),
                    //LinkTargetPath -- TTarget Pth of a Shortcut file 
                    //MimeType = null, //unsude by windows but could be calced by file extension 
                    StorageKey = storageKey,
                    ServerId = serverId
                },
                NewPath = filePath,//path to the file as it exists on disk 
                Type = type,
            };
            return md5Bytes;
        }

        public static void CreateFilePathString(Creation creation, out string fullPath)
        {
            string modPath = string.Empty;
            string modName = string.Empty;
            if (creation.Name != null && creation.Path != null)
            {
                modPath = creation.Path.Replace("\"", "");
                modName = creation.Name.Replace("\"", "");
                if (PathEndsWithSlash(modPath))
                    modPath = modPath.Remove(modPath.Count() - 1, 1);

                fullPath = modPath + "\\" + modName;
            }
            else
            {
                fullPath = string.Empty;
                Console.WriteLine("Both FileName and FilePath are Required");
            }
        }

        public static int TryCreate(InputParams paramSet, SmokeTask smokeTask, FileInfo fi, string fileName, ref GenericHolder<CLError> ProcessingErrorHolder, ref ManualSyncManager manager)
        {
            int responseCode = -1;
            try
            {
                responseCode = manager.Create(paramSet, smokeTask, fi, fileName, ref ProcessingErrorHolder);
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

        #endregion 

        #region Modify 
        public static int TryUpload(FileInfo info, CLSyncBox syncBox, FileChange fileChange,
                                CLHttpRestStatus restStatus, CloudApiPublic.JsonContracts.Event returnEvent, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            CLHttpRestStatus newStatus;
            int responseCode = 0;
            fileChange.Metadata.Revision = returnEvent.Metadata.Revision;
            fileChange.Metadata.StorageKey = returnEvent.Metadata.StorageKey;
            string message = string.Empty;
            Stream stream = new System.IO.FileStream(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            CLError updateFileError = syncBox.HttpRestClient.UploadFile(stream, fileChange, ManagerConstants.TimeOutMilliseconds, out newStatus, out message);
            if (restStatus != CLHttpRestStatus.Success || updateFileError != null)
            {
                HandleUnsuccessfulUpload(restStatus, updateFileError, ManagerConstants.RequestTypes.RestCreateFile, ref ProcessingErrorHolder);
                responseCode = 1;
            }
            else
            {
                Console.WriteLine("Successfully Uploaded File {0} to the Sync Box {1}.", info.Name, syncBox.SyncBoxId);
            }
            return responseCode;
        }


        #endregion 

        #region Compare
        public static bool ShouldUpdateFile(InputParams paramSet, CLSyncBox syncBox, string filePath, CloudApiPublic.JsonContracts.Metadata mdObject, ref GenericHolder<CLError> ProcessingErrorHolder)
        {

            //AllMappings mappings = XMLHelper.GetMappingItems(paramSet.FileNameMappingFile, ref ProcessingErrorHolder);
            //PathMappingElement mappingElement = mappings.MappingRecords.Items.Where(i => i.ID == mdObject.ServerId).FirstOrDefault();
            return false;
        }

        public static bool PathEndsWithSlash(string path)
        {
            if (path.LastIndexOf('\\') == (path.Count() - 1))
                return true;
            else
                return false;
        }
        #endregion 

        #region Search
        public static FileInfo FindFirstFileInDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                throw new Exception("The specified directory path does not exist.");

            FileInfo returnValue = null;

            DirectoryInfo dInfo = new DirectoryInfo(directoryPath);
            returnValue = dInfo.EnumerateFiles().FirstOrDefault();
            if (returnValue == null)
            {
                IEnumerable<DirectoryInfo> childFolders = dInfo.EnumerateDirectories();
                foreach (DirectoryInfo directory in childFolders)
                {
                    FileInfo fInfo = directory.EnumerateFiles().FirstOrDefault();
                    if (fInfo != null)
                    {
                        returnValue = fInfo;
                    }
                    else
                    {
                        //recursive call to nested folders.
                        fInfo = FindFirstFileInDirectory(directory.FullName);
                        if (fInfo != null)
                            returnValue = fInfo;
                    }
                    if (fInfo != null)
                        break;
                }
            }
            return returnValue;
        }
        #endregion

        #region Responses
        public static int CreateFileResponseSwitch(CreateFileResponseEventArgs responseArgs, FileChange currentFileChange, ManualSyncManager manager, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            GenericHolder<CLError> refHolder = ProcessingErrorHolder;
            int responseCode = 0;
            switch (responseArgs.ResponseText)
            {
                case "upload":
                case "uploading":
                    responseCode = FileHelper.TryUpload(responseArgs.CreateTaskFileInfo, responseArgs.SyncBox, responseArgs.FileChange, responseArgs.RestStatus, responseArgs.ReturnEvent, ref refHolder);
                    break;
                case "duplicate":
                case "exists":
                case "conflict":
                    //ThrowDuplicateException(ref ProcessingErrorHolder);
                    responseCode = FileHelper.RenameAndTryUpload(responseArgs, currentFileChange, manager);
                    break;
                default:
                    responseCode = (int)FileManagerResponseCodes.InvalidResponseType;
                    Console.Write(string.Format("The Server Response is {0}", responseArgs.ReturnEvent.Header.Status));
                    break;

            }
            return responseCode;
        }

        public static HttpPostReponseCodes TransformHttpPostResponse(string response) 
        {
            HttpPostReponseCodes responseCode = HttpPostReponseCodes.None;
            switch (response)
            {
                case "upload":
                case "uploading":
                case "ok":
                    responseCode = HttpPostReponseCodes.Upload;
                    break;
                case "duplicate":
                case "exists":
                case "conflict":
                    responseCode = HttpPostReponseCodes.Duplicate;
                    break;
                default:
                    Console.Write(string.Format("The Server Response is {0}", response));
                    break;

            }
            return responseCode;
        }

        public static void HandleUnsuccessfulUpload(CLHttpRestStatus restStatus, CLError updateError, string requestType, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            if (restStatus == CLHttpRestStatus.Success)
                HandleUploadError(updateError, ref ProcessingErrorHolder);
            else
            {
                HandleRestFailedResponse(restStatus, requestType, ref ProcessingErrorHolder);
                HandleUploadError(updateError, ref ProcessingErrorHolder);
            }
        }

        public static void HandleUploadError(CLError updateError, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            lock (ProcessingErrorHolder)
            {
                foreach (Exception exception in updateError.GrabExceptions())
                {
                    lock (ProcessingErrorHolder)
                    {
                        ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
                    }
                }
            }
        }

        public static void HandleRestFailedResponse(CLHttpRestStatus restStatus, string requestType, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            switch (restStatus)
            {
                case CLHttpRestStatus.BadRequest:
                    RestStatusResponseManager.HandleBadRequest(requestType);
                    break;
                case CLHttpRestStatus.Cancelled:
                    RestStatusResponseManager.HandleCancelled(requestType);
                    break;
                case CLHttpRestStatus.ConnectionFailed:
                    RestStatusResponseManager.HandleConnectionFailed(requestType);
                    break;
                case CLHttpRestStatus.NoContent:
                    RestStatusResponseManager.HandleNoContent(requestType);
                    break;
                case CLHttpRestStatus.NotAuthorized:
                    RestStatusResponseManager.HandleNotAutorized(requestType);
                    break;
                case CLHttpRestStatus.NotFound:
                    RestStatusResponseManager.HandleNotFound(requestType);
                    break;
                case CLHttpRestStatus.QuotaExceeded:
                    RestStatusResponseManager.HandleQuotaExceeded(requestType);
                    break;
                case CLHttpRestStatus.ServerError:
                    RestStatusResponseManager.HandleServerError(requestType);
                    break;
            }
        }
        
        #endregion 
    }
}
