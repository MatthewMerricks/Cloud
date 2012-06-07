﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Http;
using CloudApiPublic;
using CloudApiPublic.Support;
using CloudApiPublic.Model;
using CloudApiPrivate.Model.Settings;
using Newtonsoft.Json;

namespace CloudApiPrivate
{
    // Private Rest Client for Cloud.com syncing:
    // 
    // This is the private rest client. The public facing Rest client is at CLRestClient.h
    // See the CLObject event protocol for details on how to format the dictionary items that need to be 
    // passed to and from the Metadata Server. 

    public class CLPrivateRestClient
    {
        private HttpClient _client = null;
        private Uri _uri = null;

        public CLPrivateRestClient()
        {


            //_urlConstructor = [[CLURLRequestConstructor alloc] initWithBaseURL:[NSURL URLWithString:CLMetaDataServerURL]];
            //[_urlConstructor setAuthorizationHeaderWithToken:[[CLSettings sharedSettings] aKey]];
            // NSLog(@"%s - Key:%@",__FUNCTION__, [[CLSettings sharedSettings] aKey] );
            //_urlConstructor.parameterEncoding = AFJSONParameterEncoding;
            //_JSONParams = [NSDictionary dictionaryWithObjectsAndKeys:@"Content-Type",@"application/json", nil];      
            _client = new HttpClient();
            _uri = new Uri(CLDefinitions.CLMetaDataServerURL);
            _client.BaseAddress = _uri;
            _client.DefaultRequestHeaders.Add("Content-Type", "application/json");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Authorization", Settings.Instance.Akey);
        }

        /// <summary>
        /// Adding files to the Meta Data Server is a two step proccess involving asking the server about the current state of
        /// a file system item. When this request completes the server will send back the metadata as a JSON string with 
        /// instructions on what to do with the file.. Exp. Upload, exists, duplicate..Public method that will asynchronously ask the server to create a new account for this user.
        /// </summary>
        /// <param name="path">the system file path as a string. This needs to be the full file path not just the cloud folder.</param>
        /// <param name="completionHandler">An Action object to operate on entries in the dictionary or to handle the error if there is one.</param>
        /// TODO: Deprecated?
        public void RequestToAddFile_WithCompletionHandler(string path, Action<Dictionary<string, object>, CLError> completionHandler)
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
        public void UploadFile_WithCompletionHandler(string filePath, Action<Dictionary<string, object>, CLError> completionHandler)
        {
        
        }

        /// <summary>
        /// Delete file from the Meta Data Server. When this request completes the server will send back the actions and metadata in a block for you to handle.
        /// </summary>
        /// <param name="path"> the path to the item to delete.</param>
        /// <param name="completionHandler">An Action object to operate on entries in the dictionary or to handle the error if there is one.</param>
        /// TODO: Deprecated?
        public void DeleteFile_WithCompletionHandler(string path, Action<Dictionary<string, object>, CLError> completionHandler)
        {
        
        }

        /// <summary>
        /// Modify a file on the Meta Data Server. When this request completes the server will send back the actions and metadata in a block for you to handle.
        /// </summary>
        /// <param name="path"> the path to the item to modify.</param>
        /// <param name="completionHandler">An Action object to operate on entries in the dictionary or to handle the error if there is one.</param>
        /// TODO: Deprecated?
        public void RequestToModifyFile_WithCompletionHandler(string path, Action<Dictionary<string, object>, CLError> completionHandler)
        {
        
        }

        /// <summary>
        /// Move file. When this request completes the server will send back the actions and metadata in a block for you to handle.
        /// </summary>
        /// <param name="fromPath"> the previous path to the item.</param>
        /// <param name="toPath"> the new path to the item.</param>
        /// <param name="completionHandler">An Action object to operate on entries in the dictionary or to handle the error if there is one.</param>
        /// TODO: Deprecated?
        public void MoveFileFromPath_ToPath_WithCompletionHandler(string fromPath, string toPath, Action<Dictionary<string, object>, CLError> completionHandler)
        {
        
        }

        /// <summary>
        /// Rename file. When this request completes the server will send back the actions and metadata in a block for you to handle.
        /// </summary>
        /// <param name="fromPath"> the previous path to the item.</param>
        /// <param name="toPath"> the new path to the item.</param>
        /// <param name="completionHandler">An Action object to operate on entries in the dictionary or to handle the error if there is one.</param>
        /// TODO: Deprecated?
        public void RenameFileFromPath_ToPath_WithCompletionHandler(string fromPath, string toPath, Action<Dictionary<string, object>, CLError> completionHandler)
        {
        
        }

        /// <summary>
        /// Add Folder item . When this request completes the server will send back the actions and metadata in a block for you to handle.
        /// </summary>
        /// <param name="path"> the path to the item.</param>
        /// <param name="completionHandler">An Action object to operate on entries in the dictionary or to handle the error if there is one.</param>
        /// TODO: Deprecated?
        public void AddFolder_WithCompletionHandler(string path, Action<Dictionary<string, object>, CLError> completionHandler)
        {
        
        }

        /// <summary>
        /// Delete folder from the Meta Data Server. When this request completes the server will send back the actions and metadata in a block for you to handle.
        /// </summary>
        /// <param name="path"> the path to the item.</param>
        /// <param name="completionHandler">An Action object to operate on entries in the dictionary or to handle the error if there is one.</param>
        /// TODO: Deprecated?
        public void DeleteFolder_WithCompletionHandler(string path, Action<Dictionary<string, object>, CLError> completionHandler)
        {
        
        }

        /// <summary>
        /// Move folder from one path to another.
        /// </summary>
        /// <param name="fromPath"> the previous path to the item.</param>
        /// <param name="toPath"> the new path to the item.</param>
        /// <param name="completionHandler">An Action object to operate on entries in the dictionary or to handle the error if there is one.</param>
        /// TODO: Deprecated?
        public void MoveFolderFromPath_ToPath_WithCompletionHandler(string fromPath, string toPath, Action<Dictionary<string, object>, CLError> completionHandler)
        {
        
        }

        /// <summary>
        /// Rename folder.
        /// </summary>
        /// <param name="fromPath"> the previous path to the item.</param>
        /// <param name="toPath"> the new path to the item.</param>
        /// <param name="completionHandler">An Action object to operate on entries in the dictionary or to handle the error if there is one.</param>
        /// TODO: Deprecated?
        public void RenameFolderFromPath_ToPath_WithCompletionHandler(string fromPath, string toPath, Action<Dictionary<string, object>, CLError> completionHandler)
        {
        
        }

        /// <summary>
        /// Sync to cloud. When this request completes the server will send back the actions and metadata in a block for you to handle.
        /// </summary>
        /// <param name="metadata"> a dictionary of actions and items to sync to the cloud.</param>
        /// <param name="completionHandler">An Action object to operate on entries in the dictionary or to handle the error if there is one.</param>
        /// <param name="queue">The GCD queue.</param>
        public void SyncToCloud_WithCompletionHandler_OnQueue(Dictionary<string, object> metadata, Action<Dictionary<string, object>, CLError> completionHandler, DispatchQueue queue)
        {
    
        }

        /// <summary>
        /// Sync from cloud. When this request completes the server will send back the actions and metadata in a block for you to handle.
        /// </summary>
        /// <param name="metadata"> a dictionary of actions and items to sync from the cloud.</param>
        /// <param name="completionHandler">An Action object to operate on entries in the dictionary or to handle the error if there is one.</param>
        /// <param name="queue">The GCD queue.</param>
        public async void SyncFromCloud_WithCompletionHandler_OnQueue_Async(Dictionary<string, object> metadata, Action<Dictionary<string, object>, CLError> completionHandler, DispatchQueue queue)
        {
            //NSString *methodPath = @"/sync/from_cloud";
            //NSMutableURLRequest *syncRequest = [self.urlConstructor requestWithMethod:@"POST" path:methodPath parameters:self.JSONParams];
            //[syncRequest setTimeoutInterval:120]; // 2 mins.
            //[syncRequest setHTTPBody:[NSJSONSerialization dataWithJSONObject:metadata options:NSJSONWritingPrettyPrinted error:nil]];
            //syncRequest = [self addCurrentClientVersionValueToHeaderFieldInRequest:syncRequest];
    
            //NSLog(@"%s - Headers:%@",__FUNCTION__, [syncRequest allHTTPHeaderFields]);
    
            //[NSURLConnection sendAsynchronousRequest:syncRequest queue:[NSOperationQueue mainQueue] completionHandler:^(NSURLResponse *response, NSData *data, NSError *error) {
        
            //    NSDictionary* jsonResult;
            //    if (!error) {
            //        if (([(NSHTTPURLResponse *)response statusCode] == 200)) {
            //            NSError *jsonParsingError;
            //            jsonResult = [NSJSONSerialization JSONObjectWithData:data options:NSJSONReadingAllowFragments error:nil];
            //            if (jsonParsingError) {
            //                NSLog(@"Error parsing JSON in %s with error: %@", __FUNCTION__, [jsonParsingError description]);
            //            }
            //        }else {
            //            NSMutableDictionary *userInfo = [NSMutableDictionary dictionary];
            //            [userInfo setValue:[NSString stringWithFormat:NSLocalizedString(@"Expected status code 200, got %d", nil), [(NSHTTPURLResponse *)response statusCode]] forKey:NSLocalizedDescriptionKey];
            //            [userInfo setValue:[syncRequest URL] forKey:NSURLErrorFailingURLErrorKey];
            //            error = [[NSError alloc]initWithDomain:CLCloudAppRestAPIErrorDomain code:[(NSHTTPURLResponse *)response statusCode] userInfo:userInfo];
            //        }
            //    }
            //    dispatch_async(queue, ^{
            //        handler(jsonResult, error);
            //    });
            //}];
            string methodPath = "/sync/from_cloud";
            string json = JsonConvert.SerializeObject(metadata, Formatting.Indented);

            // Build the request
            HttpRequestMessage syncRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(methodPath, UriKind.Relative));
            syncRequest.Headers.Add("Content-Type", "application/json");
            syncRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            // Add the client type and version.  For the Windows client, it will be Wnn.  e.g., W01 for the 0.1 client.
            syncRequest.Headers.Add(CLPrivateDefinitions.CLClientVersionHeaderName, CLPrivateDefinitions.CLClientVersion);

            // Send the request asynchronously
            _client.Timeout = TimeSpan.FromMinutes(2.0);
            _client.SendAsync(syncRequest).ContinueWith(task =>
            {
                Dictionary<string, object> jsonResult = null;
                HttpResponseMessage response = null;
                CLError error = null;
                bool isSuccess = true;
                DispatchQueue localQueue = queue;

                Exception ex = task.Exception;
                if (ex == null)
                {
                    response = task.Result;
                }

                if (ex != null)
                {
                    // Exception
                    error = new CLError();
                    error.AddException(ex, replaceErrorDescription:true);
                    isSuccess = false;
                }
                else if (response == null)
                {
                    error = new CLError();
                    error.errorDomain = CLError.ErrorDomain_Application;
                    error.errorDescription = CLSptResourceManager.Instance.ResMgr.GetString("ErrorPostingSyncFromServer");
                    error.errorCode = -1;
                    isSuccess = false;
                }

                if (isSuccess)
                {
                    jsonResult = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content.ToString());
                    Dispatch.Async(localQueue, completionHandler);
                }


                if (!error) {
                    if (([(NSHTTPURLResponse *)response statusCode] == 200)) {
                        NSError *jsonParsingError;
                        jsonResult = [NSJSONSerialization JSONObjectWithData:data options:NSJSONReadingAllowFragments error:nil];
                        if (jsonParsingError) {
                            NSLog(@"Error parsing JSON in %s with error: %@", __FUNCTION__, [jsonParsingError description]);
                        }
                    }else {
                        NSMutableDictionary *userInfo = [NSMutableDictionary dictionary];
                        [userInfo setValue:[NSString stringWithFormat:NSLocalizedString(@"Expected status code 200, got %d", nil), [(NSHTTPURLResponse *)response statusCode]] forKey:NSLocalizedDescriptionKey];
                        [userInfo setValue:[syncRequest URL] forKey:NSURLErrorFailingURLErrorKey];
                        error = [[NSError alloc]initWithDomain:CLCloudAppRestAPIErrorDomain code:[(NSHTTPURLResponse *)response statusCode] userInfo:userInfo];
                    }
                }
                dispatch_async(queue, ^{
                    handler(jsonResult, error);
                });            });


            

            NSMutableURLRequest *syncRequest = [self.urlConstructor requestWithMethod:@"POST" path:methodPath parameters:self.JSONParams];
            [syncRequest setTimeoutInterval:120]; // 2 mins.
            [syncRequest setHTTPBody:[NSJSONSerialization dataWithJSONObject:metadata options:NSJSONWritingPrettyPrinted error:nil]];
            syncRequest = [self addCurrentClientVersionValueToHeaderFieldInRequest:syncRequest];

            NSLog(@"%s - Headers:%@",__FUNCTION__, [syncRequest allHTTPHeaderFields]);

            [NSURLConnection sendAsynchronousRequest:syncRequest queue:[NSOperationQueue mainQueue] completionHandler:^(NSURLResponse *response, NSData *data, NSError *error) {

                //completion removed
            }];        
        }

        /// <summary>
        /// Upload a file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="storageKey">The file's storage key.</param>
        /// <param name="queue">The GCD queue.</param>
        /// TODO: Deprecated?
        public CLHTTPConnectionOperation UploadOperationForFile_WithStorageKey(string path, string storageKey)
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
        public CLHTTPConnectionOperation StreamingUploadOperationForStorageKey_WithFileSystemPath_FileSize_AndMd5Hash(string storageKey, string path, string fileSize, string hash)
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
        public CLHTTPConnectionOperation StreamingDownloadOperationForStorageKey_WithFileSystemPath_FileSize_AndMd5Hash(string storageKey, string path, string fileSize, string hash)
        {
            HttpRequestMessage request = null;
            return new CLHTTPConnectionOperation(request, "");
        }
    }
}
