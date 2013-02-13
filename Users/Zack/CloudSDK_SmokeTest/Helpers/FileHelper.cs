using CloudApiPublic;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
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
        #endregion 

        #region Modify 
        public static int TryUpload(string filePath, string fileName, CLSyncBox syncBox, FileChange fileChange,
                                CLHttpRestStatus restStatus, CloudApiPublic.JsonContracts.Event returnEvent, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            CLHttpRestStatus newStatus;
            int responseCode = 0;
            fileChange.Metadata.Revision = returnEvent.Metadata.Revision;
            fileChange.Metadata.StorageKey = returnEvent.Metadata.StorageKey;
            Stream stream = new System.IO.FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            CLError updateFileError = syncBox.HttpRestClient.UploadFile(stream, fileChange, ManagerConstants.TimeOutMilliseconds, out newStatus);
            if (restStatus != CLHttpRestStatus.Success || updateFileError != null)
            {
                HandleUnsuccessfulUpload(fileChange, returnEvent, restStatus, updateFileError, ManagerConstants.RequestTypes.RestCreateFile, ref ProcessingErrorHolder);
                responseCode = 1;
            }
            else
            {
                Console.Write("Successfully Uploaded File {0} to the Sync Box Server.", fileName);
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
        #endregion 


        #region Responses

        public static void HandleUnsuccessfulUpload(FileChange fileChange, CloudApiPublic.JsonContracts.Event returnEvent, CLHttpRestStatus restStatus, CLError updateError, string requestType, ref GenericHolder<CLError> ProcessingErrorHolder)
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
