using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CloudApiPublic;
using CloudApiPublic.Support;
using CloudApiPublic.Model;
using CloudApiPrivate.Model.Settings;
using Newtonsoft.Json;
using System.Net.Http.Headers;


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
        private const int _CLRestClientDefaultHTTPTimeOutInterval = 180;

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
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Authorization", Settings.Instance.Akey);
            _client.DefaultRequestHeaders.TransferEncodingChunked = false;
            _client.Timeout = TimeSpan.FromSeconds(_CLRestClientDefaultHTTPTimeOutInterval);

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
        public async void SyncToCloud_WithCompletionHandler_OnQueue_Async(Dictionary<string, object> metadata, Action<CLJsonResultWithError> completionHandler, DispatchQueueGeneric queue)
        {
            //NSString *methodPath = [NSString stringWithFormat:@"%@?user_id=%@", @"/sync/to_cloud", [[CLSettings sharedSettings] uuid]];
            //NSMutableURLRequest *syncRequest = [self.urlConstructor requestWithMethod:@"POST" path:methodPath parameters:self.JSONParams];
    
            //[syncRequest setTimeoutInterval:180]; // 3 mins.
            //[syncRequest setHTTPBody:[NSJSONSerialization dataWithJSONObject:metadata options:NSJSONWritingPrettyPrinted error:nil]];
            //syncRequest = [self addCurrentClientVersionValueToHeaderFieldInRequest:syncRequest];
    
            //NSLog(@"%s - Headers:%@",__FUNCTION__, [syncRequest allHTTPHeaderFields]);
            //NSLog(@"Request size: %li", [syncRequest.HTTPBody length]);
    
            //[NSURLConnection sendAsynchronousRequest:syncRequest queue:[NSOperationQueue mainQueue] completionHandler:^(NSURLResponse *response, NSData *data, NSError *error) {
       
            //    NSDictionary* jsonResult;
            //    if (!error) {
            //        if (([(NSHTTPURLResponse *)response statusCode] == 200)) {
            //            NSError *jsonParsingError;
            //            jsonResult = [NSJSONSerialization JSONObjectWithData:data options:NSJSONReadingAllowFragments error:&jsonParsingError];
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

            string methodPath = String.Format("{0}?user_id={1}", "/sync/to_cloud", Settings.Instance.Uuid);
            string json = JsonConvert.SerializeObject(metadata, Formatting.Indented);

            // Build the request
            HttpRequestMessage syncRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(methodPath, UriKind.Relative));
            syncRequest.Headers.Add("Content-Type", "application/json");
            syncRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            // Add the client type and version.  For the W{indows client, it will be Wnn.  e.g., W01 for the 0.1 client.
            syncRequest.Headers.Add(CLPrivateDefinitions.CLClientVersionHeaderName, CLPrivateDefinitions.CLClientVersion);

            _client.DefaultRequestHeaders.TransferEncodingChunked = false;

            // Send the request asynchronously
            await _client.SendAsync(syncRequest).ContinueWith(task =>
            {
                HandleResponseFromServerCallback(completionHandler, queue, task, "ErrorPostingSyncToServer");
            });
        }

        /// <summary>
        /// Sync from cloud. When this request completes the server will send back the actions and metadata in a block for you to handle.
        /// </summary>
        /// <param name="metadata"> a dictionary of actions and items to sync from the cloud.</param>
        /// <param name="completionHandler">An Action object to operate on entries in the dictionary or to handle the error if there is one.</param>
        /// <param name="queue">The GCD queue.</param>
        public async void SyncFromCloud_WithCompletionHandler_OnQueue_Async(Dictionary<string, object> metadata, Action<CLJsonResultWithError> completionHandler, DispatchQueueGeneric queue)
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

            _client.DefaultRequestHeaders.TransferEncodingChunked = false;

            // Send the request asynchronously
            await _client.SendAsync(syncRequest).ContinueWith(task =>
            {
                HandleResponseFromServerCallback(completionHandler, queue, task, "ErrorPostingSyncFromServer");
            });
        }

        private static void HandleResponseFromServerCallback(Action<CLJsonResultWithError> completionHandler, DispatchQueueGeneric queue, Task<HttpResponseMessage> task,
                                string resourceErrorMessageKey)
        {
                Dictionary<string, object> jsonResult = null;
                HttpResponseMessage response = null;
                CLError error = new Exception(CLSptResourceManager.Instance.ResMgr.GetString(resourceErrorMessageKey));  // init error which may not be used
                bool isError = false;       // T: an error was posted
                bool isSuccess = true;

                Exception ex = task.Exception;
                if (ex == null)
                {
                    response = task.Result;
                }

                if (ex != null)
                {
                    // Exception
                    isError = true;
                    error.AddException(ex);
                    isSuccess = false;
                }
                else if (response == null)
                {
                    isError = true;
                    error.AddException(new Exception("Response from server was null"));
                    isSuccess = false;
                }

                if (isSuccess)
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        try 
	                    {
		                    jsonResult = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content.ToString());
	                    }
	                    catch (Exception exInner)
	                    {
                            isError = true;
                            error.AddException(exInner);
		                }
                    }
                    else
                    {
                        isError = true;
                        error.AddException(new Exception(String.Format("Expected status code 200 from server.  Got: {0}", response.StatusCode)));
                    }  
                }

                if (!isError)
                {
                    error = null;
                }

                CLJsonResultWithError userstate = new CLJsonResultWithError()
                {
                    JsonResult = jsonResult,
                    Error = error
                }; 
                
                Dispatch.Async(queue, completionHandler, userstate);
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
            return new CLHTTPConnectionOperation(_client, request, "", isUpload: true);
        }

        /// <summary>
        /// Create a streaming operation to upload a file.
        /// </summary>
        /// <param name="storageKey">The file's storage key.</param>
        /// <param name="path">The path to the file.</param>
        /// <param name="fileSize">The size of the file.</param>
        /// <param name="hash">The MD5 hash of the file.</param>
        public CLHTTPConnectionOperation StreamingUploadOperationForStorageKey_WithFileSystemPath_FileSize_AndMd5Hash(string storageKey, string path, string size, string hash)
        {
            //CLURLRequestConstructor *uploadURLConstructor = [[CLURLRequestConstructor alloc] initWithBaseURL:[NSURL URLWithString:CLUploadDownloadServerURL]];
            //[uploadURLConstructor setAuthorizationHeaderWithToken:[[CLSettings sharedSettings] aKey]];
    
            //[uploadURLConstructor setDefaultHeader:@"X-Ctx-Storage-Key" value:storageKey];
            //[uploadURLConstructor setDefaultHeader:@"Content-MD5" value:hash];
            //[uploadURLConstructor setDefaultHeader:@"Content-Length"  value:size];
    
            //NSMutableURLRequest *request = [uploadURLConstructor requestWithMethod:@"PUT" path:@"/put_file" parameters:nil];
            //request = [self addCurrentClientVersionValueToHeaderFieldInRequest:request];
            //CLHTTPConnectionOperation *operation = [[CLHTTPConnectionOperation alloc] initForStreamingUploadWithRequest:request andFileSystemPath:fileSystemPath];
            //[request setTimeoutInterval:CLRestClientDefaultHTTPTimeOutInterval];
            //return operation;

            //&&&&
            string methodPath = "/put_file";

            // Build the request
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, new Uri(new Uri(CLDefinitions.CLUploadDownloadServerURL), methodPath));
            request.Headers.Add("X-Ctx-Storage-Key", storageKey);
            request.Headers.Add("Content-MD5", hash);
            request.Headers.Add("Content-Length", size);

            // Add the client type and version.  For the Windows client, it will be Wnn.  e.g., W01 for the 0.1 client.
            request.Headers.Add(CLPrivateDefinitions.CLClientVersionHeaderName, CLPrivateDefinitions.CLClientVersion);

            return new CLHTTPConnectionOperation(_client, request, path, isUpload: true);
        }

        /// <summary>
        /// Create a streaming operation to download a file.
        /// </summary>
        /// <param name="storageKey">The file's storage key.</param>
        /// <param name="path">The path to the file.</param>
        /// <param name="size">The size of the file.</param>
        /// <param name="hash">The MD5 hash of the file.</param>
        public CLHTTPConnectionOperation StreamingDownloadOperationForStorageKey_WithFileSystemPath_FileSize_AndMd5Hash(string storageKey, string path, string size, string hash)
        {
            //CLURLRequestConstructor *downloadURLConstructor = [[CLURLRequestConstructor alloc] initWithBaseURL:[NSURL URLWithString:CLUploadDownloadServerURL]];
            //[downloadURLConstructor setAuthorizationHeaderWithToken:[[CLSettings sharedSettings] aKey]];
            //downloadURLConstructor.parameterEncoding = AFJSONParameterEncoding;
    
            //NSDictionary *params = [NSDictionary dictionaryWithObjectsAndKeys:storageKey, @"storage_key", nil];
            //NSMutableURLRequest *request = [downloadURLConstructor requestWithMethod:@"POST" path:@"/get_file" parameters:params];
            //request = [self addCurrentClientVersionValueToHeaderFieldInRequest:request];
            //[request setTimeoutInterval:CLRestClientDefaultHTTPTimeOutInterval];
    
            //CLHTTPConnectionOperation *downloadOperation = [[CLHTTPConnectionOperation alloc] initForStreamingDownloadWithRequest:request andFileSystemPath:fileSystemPath];
            //[downloadOperation setResponseFilePath:fileSystemPath];
       
            //return downloadOperation;
            //&&&&
            string methodPath = "/get_file";

            // Build the request
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(CLDefinitions.CLUploadDownloadServerURL), methodPath));
            request.Headers.Add("storage-Key", storageKey);
            request.Headers.Add("Content-MD5", hash);
            request.Headers.Add("Content-Length", size);

            // Add the client type and version.  For the Windows client, it will be Wnn.  e.g., W01 for the 0.1 client.
            request.Headers.Add(CLPrivateDefinitions.CLClientVersionHeaderName, CLPrivateDefinitions.CLClientVersion);

            return new CLHTTPConnectionOperation(_client, request, path, isUpload: false);
        }
    }
}
