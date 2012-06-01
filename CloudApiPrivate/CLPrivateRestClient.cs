﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Http;
using CloudApiPublic;
using CloudApiPublic.Support;
using CloudApiPublic.Model;

namespace CloudApiPrivate
{
    // Private Rest Client for Cloud.com syncing:
    // 
    // This is the private rest client. The public facing Rest client is at CLRestClient.h
    // See the CLObject event protocol for details on how to format the dictionary items that need to be 
    // passed to and from the Metadata Server. 

    public class CLPrivateRestClient
    {
        public CLPrivateRestClient()
        {
      
        }

        /// <summary>
        /// Adding files to the Meta Data Server is a two step proccess involving asking the server about the current state of
        /// a file system item. When this request completes the server will send back the metadata as a JSON string with 
        /// instructions on what to do with the file.. Exp. Upload, exists, duplicate..Public method that will asynchronously ask the server to create a new account for this user.
        /// </summary>
        /// <param name="path">the system file path as a string. This needs to be the full file path not just the cloud folder.</param>
        /// <param name="completionHandler">An Action object to operate on entries in the dictionary or to handle the error if there is one.</param>
        /// TODO: Deprecated?
        void RequestToAddFile_WithCompletionHandler(string path,  Action<Dictionary<string, object>, CLError> completionHandler)
        {
        }

        /// <summary>
        /// Upload file to file storage. When this request completes the server will send back the actions and metadata in a block for you to handle.
        /// </summary>
        /// <param name="filePath">a path to the file to upload.</param>
        /// <param name="completionHandler">An Action object to operate on entries in the dictionary or to handle the error if there is one.</param>
        /// <remarks>
        ///  Error codes:
        ///  304 – Not Modified – if storing fails for some reason.
        ///  404 – Not Found – if the file referenced in the metadata is not found.
        ///  </remarks>
        /// TODO: Deprecated?
        void UploadFile_WithCompletionHandler(string filePath, Action<Dictionary<string, object>, CLError> completionHandler)
        {
        
        }

        /// <summary>
        /// Delete file from the Meta Data Server. When this request completes the server will send back the actions and metadata in a block for you to handle.
        /// </summary>
        /// <param name="path"> the path to the item to delete.</param>
        /// <param name="completionHandler">An Action object to operate on entries in the dictionary or to handle the error if there is one.</param>
        /// TODO: Deprecated?
        void DeleteFile_WithCompletionHandler(string path, Action<Dictionary<string, object>, CLError> completionHandler)
        {
        
        }

        /// <summary>
        /// Modify a file on the Meta Data Server. When this request completes the server will send back the actions and metadata in a block for you to handle.
        /// </summary>
        /// <param name="path"> the path to the item to modify.</param>
        /// <param name="completionHandler">An Action object to operate on entries in the dictionary or to handle the error if there is one.</param>
        /// TODO: Deprecated?
        void RequestToModifyFile_WithCompletionHandler(string path, Action<Dictionary<string, object>, CLError> completionHandler)
        {
        
        }

        /// <summary>
        /// Move file. When this request completes the server will send back the actions and metadata in a block for you to handle.
        /// </summary>
        /// <param name="fromPath"> the previous path to the item.</param>
        /// <param name="toPath"> the new path to the item.</param>
        /// <param name="completionHandler">An Action object to operate on entries in the dictionary or to handle the error if there is one.</param>
        /// TODO: Deprecated?
        void MoveFileFromPath_ToPath_WithCompletionHandler(string fromPath, string toPath, Action<Dictionary<string, object>, CLError> completionHandler)
        {
        
        }

        /// <summary>
        /// Rename file. When this request completes the server will send back the actions and metadata in a block for you to handle.
        /// </summary>
        /// <param name="fromPath"> the previous path to the item.</param>
        /// <param name="toPath"> the new path to the item.</param>
        /// <param name="completionHandler">An Action object to operate on entries in the dictionary or to handle the error if there is one.</param>
        /// TODO: Deprecated?
        void RenameFileFromPath_ToPath_WithCompletionHandler(string fromPath, string toPath, Action<Dictionary<string, object>, CLError> completionHandler)
        {
        
        }

        /// <summary>
        /// Add Folder item . When this request completes the server will send back the actions and metadata in a block for you to handle.
        /// </summary>
        /// <param name="path"> the path to the item.</param>
        /// <param name="completionHandler">An Action object to operate on entries in the dictionary or to handle the error if there is one.</param>
        /// TODO: Deprecated?
        void AddFolder_WithCompletionHandler(string path, Action<Dictionary<string, object>, CLError> completionHandler)
        {
        
        }

        /// <summary>
        /// Delete folder from the Meta Data Server. When this request completes the server will send back the actions and metadata in a block for you to handle.
        /// </summary>
        /// <param name="path"> the path to the item.</param>
        /// <param name="completionHandler">An Action object to operate on entries in the dictionary or to handle the error if there is one.</param>
        /// TODO: Deprecated?
        void DeleteFolder_WithCompletionHandler(string path, Action<Dictionary<string, object>, CLError> completionHandler)
        {
        
        }

        /// <summary>
        /// Move folder from one path to another.
        /// </summary>
        /// <param name="fromPath"> the previous path to the item.</param>
        /// <param name="toPath"> the new path to the item.</param>
        /// <param name="completionHandler">An Action object to operate on entries in the dictionary or to handle the error if there is one.</param>
        /// TODO: Deprecated?
        void MoveFolderFromPath_ToPath_WithCompletionHandler(string fromPath, string toPath, Action<Dictionary<string, object>, CLError> completionHandler)
        {
        
        }

        /// <summary>
        /// Rename folder.
        /// </summary>
        /// <param name="fromPath"> the previous path to the item.</param>
        /// <param name="toPath"> the new path to the item.</param>
        /// <param name="completionHandler">An Action object to operate on entries in the dictionary or to handle the error if there is one.</param>
        /// TODO: Deprecated?
        void RenameFolderFromPath_ToPath_WithCompletionHandler(string fromPath, string toPath, Action<Dictionary<string, object>, CLError> completionHandler)
        {
        
        }

        /// <summary>
        /// Sync to cloud. When this request completes the server will send back the actions and metadata in a block for you to handle.
        /// </summary>
        /// <param name="metadata"> a dictionary of actions and items to sync to the cloud.</param>
        /// <param name="completionHandler">An Action object to operate on entries in the dictionary or to handle the error if there is one.</param>
        /// <param name="queue">The GCD queue.</param>
        void SyncToCloud_WithCompletionHandler_OnQueue(Dictionary<string, object> metadata, Action<Dictionary<string, object>, CLError> completionHandler, DispatchQueue queue)
        {
    
        }

        /// <summary>
        /// Sync from cloud. When this request completes the server will send back the actions and metadata in a block for you to handle.
        /// </summary>
        /// <param name="metadata"> a dictionary of actions and items to sync from the cloud.</param>
        /// <param name="completionHandler">An Action object to operate on entries in the dictionary or to handle the error if there is one.</param>
        /// <param name="queue">The GCD queue.</param>
        void SyncFromCloud_WithCompletionHandler_OnQueue(Dictionary<string, object> metadata, Action<Dictionary<string, object>, CLError> completionHandler, DispatchQueue queue)
        {
        
        }

        /// <summary>
        /// Upload a file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="storageKey">The file's storage key.</param>
        /// <param name="queue">The GCD queue.</param>
        /// TODO: Deprecated?
        CLHTTPConnectionOperation UploadOperationForFile_WithStorageKey(string path, string storageKey)
        {
            HttpRequestMessage request = null;
            return new CLHTTPConnectionOperation(request, "");
        }

        /// <summary>
        /// Create a streaming operation to upload a file.
        /// </summary>
        /// <param name="storageKey">The file's storage key.</param>
        /// <param name="path">The path to the file.</param>
        /// <param name="fileSize">The size of the file.</param>
        /// <param name="hash">The MD5 hash of the file.</param>
        CLHTTPConnectionOperation StreamingUploadOperationForStorageKey_WithFileSystemPath_FileSize_AndMd5Hash(string storageKey, string path, string fileSize, string hash)
        {
            HttpRequestMessage request = null;
            return new CLHTTPConnectionOperation(request, "");
        
        }

        /// <summary>
        /// Create a streaming operation to download a file.
        /// </summary>
        /// <param name="storageKey">The file's storage key.</param>
        /// <param name="path">The path to the file.</param>
        /// <param name="fileSize">The size of the file.</param>
        /// <param name="hash">The MD5 hash of the file.</param>
        CLHTTPConnectionOperation StreamingDownloadOperationForStorageKey_WithFileSystemPath_FileSize_AndMd5Hash(string storageKey, string path, string fileSize, string hash)
        {
            HttpRequestMessage request = null;
            return new CLHTTPConnectionOperation(request, "");
        }
    }
}
