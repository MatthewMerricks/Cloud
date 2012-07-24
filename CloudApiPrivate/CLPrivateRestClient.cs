using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CloudApiPublic;
using CloudApiPublic.Support;
using CloudApiPrivate.Model.Settings;
using CloudApiPrivate.Model;
using CloudApiPublic.Model;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.IO;
using CloudApiPublic.Static;


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
        private CLTrace _trace = null;

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
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Authorization", "Token=\"" + Settings.Instance.Akey + "\"");
            _client.DefaultRequestHeaders.TransferEncodingChunked = false;
            _client.Timeout = TimeSpan.FromSeconds(_CLRestClientDefaultHTTPTimeOutInterval);
            _trace = CLTrace.Instance;
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
        public void SyncToCloud_WithCompletionHandler_OnQueue_Async(string sid, KeyValuePair<FileChange, FileStream>[] processedChanges, Action<CLJsonResultWithError, object> completionHandler, DispatchQueueGeneric queue, Func<string> getCloudDirectory)
        //public async void SyncToCloud_WithCompletionHandler_OnQueue_Async(Dictionary<string, object> metadata, Action<CLJsonResultWithError> completionHandler, DispatchQueueGeneric queue)
        {
            // Merged 7/3/12
            //- (void)syncToCloud:(NSDictionary *)metadata completionHandler:(void (^)(NSDictionary*, NSError *))handler onQueue:(dispatch_queue_t)queue
            //{
            //    NSString *methodPath = [NSString stringWithFormat:@"%@?user_id=%@", @"/sync/to_cloud", [[CLSettings sharedSettings] uuid]];
            //    NSMutableURLRequest *syncRequest = [self.urlConstructor requestWithMethod:@"POST" path:methodPath parameters:self.JSONParams];
    
            //    [syncRequest setTimeoutInterval:CLRestClientDefaultHTTPTimeOutInterval];
            //    [syncRequest setHTTPBody:[NSJSONSerialization dataWithJSONObject:metadata options:NSJSONWritingPrettyPrinted error:nil]];
            //    syncRequest = [self addCurrentClientVersionValueToHeaderFieldInRequest:syncRequest];
    
            //    NSLog(@"%s - Headers:%@",__FUNCTION__, [syncRequest allHTTPHeaderFields]);
            //    //NSLog(@"Request size: %li", [syncRequest.HTTPBody length]);
    
            //    [NSURLConnection sendAsynchronousRequest:syncRequest queue:[NSOperationQueue mainQueue] completionHandler:^(NSURLResponse *response, NSData *data, NSError *error) {
       
            //        NSDictionary* jsonResult;
            //        if (!error) {
            //            if (([(NSHTTPURLResponse *)response statusCode] == 200)) {
            //                NSError *jsonParsingError;
            //                jsonResult = [NSJSONSerialization JSONObjectWithData:data options:NSJSONReadingAllowFragments error:&jsonParsingError];
            //                if (jsonParsingError) {
            //                    NSLog(@"Error parsing JSON in %s with error: %@", __FUNCTION__, [jsonParsingError description]);
            //                }
            //            }else {
            //                NSMutableDictionary *userInfo = [NSMutableDictionary dictionary];
            //                [userInfo setValue:[NSString stringWithFormat:NSLocalizedString(@"Expected status code 200, got %d", nil), [(NSHTTPURLResponse *)response statusCode]] forKey:NSLocalizedDescriptionKey];
            //                [userInfo setValue:[syncRequest URL] forKey:NSURLErrorFailingURLErrorKey];
            //                error = [[NSError alloc]initWithDomain:CLCloudAppRestAPIErrorDomain code:[(NSHTTPURLResponse *)response statusCode] userInfo:userInfo];
            //            }
            //        }
        
            //        dispatch_async(queue, ^{
            //            handler(jsonResult, error);
            //        });
            //    }];
            //}

            string methodPath = String.Format("{0}?user_id={1}", "/sync/to_cloud", Settings.Instance.Uuid);
            Dictionary<string, object> metadata = MetadataDictionaryFromFileChanges(sid,
                processedChanges
                    .Select(currentChange => currentChange.Key),
                getCloudDirectory);
            string json = JsonConvert.SerializeObject(metadata, Formatting.Indented);

            // Build the request
            HttpRequestMessage syncRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(methodPath, UriKind.Relative));
            syncRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            // Add the client type and version.  For the W{indows client, it will be Wnn.  e.g., W01 for the 0.1 client.
            syncRequest.Headers.Add(CLPrivateDefinitions.CLClientVersionHeaderName, CLPrivateDefinitions.CLClientVersion);

            _client.DefaultRequestHeaders.TransferEncodingChunked = false;

            // Send the request asynchronously
            _trace.writeToLog(9, "CLPrivateRestClient: SyncToCloud_withCompletionHandler_onQueue_async: Sending sync-to request to server.  json: {0}.", json);

            (new Task<ServerCallbackParameters>(state =>
                {
                    ServerCallbackParameters castState = state as ServerCallbackParameters;

                    if (castState != null)
                    {
                        try
                        {
                            castState.Response = castState.Client.SendAsync(castState.Message).Result;
                        }
                        catch (Exception ex)
                        {
                            castState.CommunicationException = ex;
                        }
                    }

                    return castState;

                }, new ServerCallbackParameters()
                    {
                        Client = _client,
                        Message = syncRequest,
                        CompletionHandler = completionHandler,
                        Queue = queue,
                        HandleResponseFromServer = this.HandleResponseFromServerCallbackAsync,
                        UserState = processedChanges
                    })
                    .ContinueWith<ServerCallbackParameters>(continueTask =>
                        {
                            if (continueTask.Result != null
                                && continueTask.Result.HandleResponseFromServer != null)
                            {
                                continueTask.Result.HandleResponseFromServer(continueTask.Result,
                                    "ErrorPostingSyncToServer");
                            }

                            return continueTask.Result;
                        })).RunSynchronously();
        }

        /// <summary>
        /// Builds a Dictionary of metadata for converting to JSON for communication from FileChanges
        /// </summary>
        /// <param name="changesForDictionary">Enumerable FileChanges for building dictionary</param>
        /// <returns>Returns dictionary for building metadata JSON</returns>
        public Dictionary<string, object> MetadataDictionaryFromFileChanges(string sid, IEnumerable<FileChange> changesForDictionary, Func<string> getCloudDirectory)
        {
            FileChange[] changesForDictionaryArray = changesForDictionary.ToArray();
            Dictionary<string, object>[] eventsArray = new Dictionary<string, object>[changesForDictionaryArray.Length];

            for (int currentChangeIndex = 0; currentChangeIndex < changesForDictionaryArray.Length; currentChangeIndex++)
            {
                Dictionary<string, object> evt;
                if (changesForDictionaryArray[currentChangeIndex].Metadata != null)
                {
                    // Build an event to represent this change.
                    string action = "";
                    switch (changesForDictionaryArray[currentChangeIndex].Type)
                    {
                        case FileChangeType.Created:
                            if (changesForDictionaryArray[currentChangeIndex].Metadata.HashableProperties.IsFolder)
                            {
                                action = CLDefinitions.CLEventTypeAddFolder;
                            }
                            else
                            {
                                action = CLDefinitions.CLEventTypeAddFile;
                            }
                            break;
                        case FileChangeType.Deleted:
                            if (changesForDictionaryArray[currentChangeIndex].Metadata.HashableProperties.IsFolder)
                            {
                                action = CLDefinitions.CLEventTypeDeleteFolder;
                            }
                            else
                            {
                                action = CLDefinitions.CLEventTypeDeleteFile;
                            }
                            break;
                        case FileChangeType.Modified:
                            action = CLDefinitions.CLEventTypeModifyFile;
                            break;
                        case FileChangeType.Renamed:
                            if (changesForDictionaryArray[currentChangeIndex].Metadata.HashableProperties.IsFolder)
                            {
                                action = CLDefinitions.CLEventTypeRenameFolder;
                            }
                            else
                            {
                                action = CLDefinitions.CLEventTypeRenameFile;
                            }
                            break;
                    }

                    // Build the metadata dictionary
                    Dictionary<string, object> metadata = new Dictionary<string, object>();

                    FilePath cloudPath = getCloudDirectory();
                    string relativeNewPath = FilePath.GetRelativePath(changesForDictionaryArray[currentChangeIndex].NewPath, cloudPath, replaceWithForwardSlashes: true);
                    string relativeOldPath = FilePath.GetRelativePath(changesForDictionaryArray[currentChangeIndex].OldPath, cloudPath, replaceWithForwardSlashes: true);

                    // Format the time like "2012-03-20T19:50:25Z"
                    metadata.Add(CLDefinitions.CLMetadataFileCreateDate, changesForDictionaryArray[currentChangeIndex].Metadata.HashableProperties.CreationTime.ToString("o"));
                    metadata.Add(CLDefinitions.CLMetadataFileModifiedDate, changesForDictionaryArray[currentChangeIndex].Metadata.HashableProperties.LastTime.ToString("o"));
                    metadata.Add(CLDefinitions.CLMetadataFileSize, changesForDictionaryArray[currentChangeIndex].Metadata.HashableProperties.Size);
                    metadata.Add(CLDefinitions.CLMetadataFromPath, relativeOldPath);
                    metadata.Add(CLDefinitions.CLMetadataCloudPath, relativeNewPath);
                    metadata.Add(CLDefinitions.CLMetadataToPath, (changesForDictionaryArray[currentChangeIndex].Type == FileChangeType.Renamed ? relativeNewPath : string.Empty));//String.Empty);       // not used?
                    string md5;
                    changesForDictionaryArray[currentChangeIndex].GetMD5LowercaseString(out md5);
                    metadata.Add(CLDefinitions.CLMetadataFileHash, md5);
                    metadata.Add(CLDefinitions.CLMetadataFileIsDirectory, changesForDictionaryArray[currentChangeIndex].Metadata.HashableProperties.IsFolder);
                    bool isLink = false;
                    if (changesForDictionaryArray[currentChangeIndex].Metadata.LinkTargetPath != null
                        && !string.IsNullOrWhiteSpace(changesForDictionaryArray[currentChangeIndex].Metadata.LinkTargetPath.ToString()))
                    {
                        isLink = true;
                    }
                    metadata.Add(CLDefinitions.CLMetadataFileIsLink, isLink);
                    metadata.Add(CLDefinitions.CLMetadataFileRevision, changesForDictionaryArray[currentChangeIndex].Metadata.Revision);
                    metadata.Add(CLDefinitions.CLMetadataFileCAttributes, String.Empty);
                    metadata.Add(CLDefinitions.CLMetadataItemStorageKey, changesForDictionaryArray[currentChangeIndex].Metadata.StorageKey);
                    //metadata.Add(CLDefinitions.CLMetadataLastEventID, changesForDictionaryArray[currentChangeIndex].EventId.ToString()); the client id is NOT the server's last_event_id; the client's EventId is passed below

                    metadata.Add(CLDefinitions.CLMetadataFileTarget, isLink ? changesForDictionaryArray[currentChangeIndex].Metadata.LinkTargetPath.ToString().Replace('\\', '/') : string.Empty);


                    // Force server forward slash normalization

                    evt = new Dictionary<string, object>()
                    {
                    // Add this event and its metadata to the events dictionary
                        { CLDefinitions.CLSyncEvent, action },             // just one in the group for now.

                        // This one is new, added to identify client events upon server response (client EventId is passed up and back down for each event)
                        { CLDefinitions.CLClientEventId, changesForDictionaryArray[currentChangeIndex].EventId.ToString() },

                        { CLDefinitions.CLSyncEventMetadata, metadata }
                    };
                }
                else
                {
                    evt = new Dictionary<string, object>();
                }
                // Add the event to the array.
                eventsArray[currentChangeIndex] = evt;
            }

            return new Dictionary<string, object>()
            {
                { CLDefinitions.CLSyncEvents, eventsArray },
                { CLDefinitions.CLSyncID, sid }
            };
        }

        /// <summary>
        /// Sync from cloud. When this request completes the server will send back the actions and metadata in a block for you to handle.
        /// </summary>
        /// <param name="metadata"> a dictionary of actions and items to sync from the cloud.</param>
        /// <param name="completionHandler">An Action object to operate on entries in the dictionary or to handle the error if there is one.</param>
        /// <param name="queue">The GCD queue.</param>
        public void SyncFromCloud_WithCompletionHandler_OnQueue_Async(Dictionary<string, object> metadata, Action<CLJsonResultWithError, object> completionHandler, DispatchQueueGeneric queue)
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
            syncRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            // Add the client type and version.  For the Windows client, it will be Wnn.  e.g., W01 for the 0.1 client.
            syncRequest.Headers.Add(CLPrivateDefinitions.CLClientVersionHeaderName, CLPrivateDefinitions.CLClientVersion);

            _client.DefaultRequestHeaders.TransferEncodingChunked = false;

            // Send the request asynchronously
            _trace.writeToLog(9, "CLPrivateRestClient: SyncFromCloud_withCompletionHandler_onQueue_async: Sending sync-from request to server.  json: {0}.", json);

            (new Task<ServerCallbackParameters>(state =>
                {
                    ServerCallbackParameters castState = state as ServerCallbackParameters;

                    if (castState != null)
                    {
                        try
                        {
                            castState.Response = castState.Client.SendAsync(castState.Message).Result;
                        }
                        catch (Exception ex)
                        {
                            castState.CommunicationException = ex;
                        }
                        return castState;
                    }

                    return null;
                }, new ServerCallbackParameters()
                    {
                        Client = _client,
                        Message = syncRequest,
                        CompletionHandler = completionHandler,
                        Queue = queue,
                        HandleResponseFromServer = this.HandleResponseFromServerCallbackAsync
                    }).ContinueWith<ServerCallbackParameters>(lastTask =>
                        {
                            if (lastTask.Result != null)
                            {
                                lastTask.Result.HandleResponseFromServer(lastTask.Result, "ErrorPostingSyncFromServer");
                            }
                            return lastTask.Result;
                        })).RunSynchronously();
        }

        private class ServerCallbackParameters
        {
            public HttpClient Client { get; set; }
            public HttpRequestMessage Message { get; set; }
            public Action<CLJsonResultWithError, object> CompletionHandler { get; set; }
            public DispatchQueueGeneric Queue { get; set; }
            public Action<ServerCallbackParameters, string> HandleResponseFromServer { get; set; }
            public HttpResponseMessage Response { get; set; }
            public Exception CommunicationException { get; set; }
            public object UserState { get; set; }
        }

        /// <summary>
        /// Handle the response from a SyncToCloud or SyncFromCloud operation.
        /// </summary>
        /// <param name="callbackParams">Parameters required for method</param>
        /// <param name="resourceErrorMessageKey">The task continued from the request.</param>
        private void HandleResponseFromServerCallbackAsync(ServerCallbackParameters callbackParams, string resourceErrorMessageKey)
        {
                Dictionary<string, object> jsonResult = null;
                CLError error = new Exception(CLSptResourceManager.Instance.ResMgr.GetString(resourceErrorMessageKey));  // init error which may not be used
                bool isError = false;       // T: an error was posted
                bool isSuccess = true;

                if (callbackParams.CommunicationException == null)
                {
                    if (callbackParams.Response == null)
                    {
                        isError = true;
                        error.AddException(new Exception("Response from server was null"));
                        isSuccess = false;
                    }
                    else
                    {
                        _trace.writeToLog(9, "CLPrivateRestClient: HandleResponseFromServerCallbackAsync: Response from sync-from: {0}.", (callbackParams.Response == null ? "Null response" : callbackParams.Response.ToString()));
                    }
                }
                else
                {
                    // Exception
                    isError = true;
                    error.AddException(callbackParams.CommunicationException);
                    isSuccess = false;
                }

                if (isSuccess)
                {
                    if (callbackParams.Response.StatusCode == HttpStatusCode.OK)
                    {
                        try 
	                    {
                            string responseBody = callbackParams.Response.Content.ReadAsString();
		                    jsonResult = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseBody);
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
                        error.AddException(new Exception(String.Format("Expected status code 200 from server.  Got: {0}", callbackParams.Response.StatusCode)));
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
                
                Dispatch.Async(callbackParams.Queue,
                    callbackParams.CompletionHandler,
                    userstate,
                    callbackParams.UserState);
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
            //HttpRequestMessage request = null;
            //return new CLHTTPConnectionOperation(_client, request, "", isUpload: true);
            return null;
        }

        /// <summary>
        /// Create a streaming operation to upload a file.
        /// </summary>
        /// <param name="storageKey">The file's storage key.</param>
        /// <param name="path">The path to the file.</param>
        /// <param name="fileSize">The size of the file.</param>
        /// <param name="hash">The MD5 hash of the file.</param>
        public CLHTTPConnectionOperation StreamingUploadOperationForStorageKey_WithFileSystemPath_FileSize_AndMd5Hash(string storageKey, string path, string size, string hash, FileStream uploadStream)
        {
            // Merged 7/13/12
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
            request.Headers.TransferEncodingChunked = true;

            // Add the client type and version.  For the Windows client, it will be Wnn.  e.g., W01 for the 0.1 client.
            request.Headers.Add(CLPrivateDefinitions.CLClientVersionHeaderName, CLPrivateDefinitions.CLClientVersion);

            _trace.writeToLog(9, "CLPrivateRestClient: StreamingUploadOperationForStorageKey_WithFileSystemPath_FileSize_AndMd5Hash: Built operation to upload file.  Path: {0}, Request: {1}.", path, request.Headers.ToString());
            return new CLHTTPConnectionOperation(_client, request, path, size, hash, isUpload: true, uploadStream: uploadStream);
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
            // Merged 7/13/12
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
            Dictionary<string, object> httpParms = new Dictionary<string, object>() { {"storage_key", storageKey} };
            string json = JsonConvert.SerializeObject(httpParms, Formatting.Indented);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            // Add the client type and version.  For the Windows client, it will be Wnn.  e.g., W01 for the 0.1 client.
            request.Headers.Add(CLPrivateDefinitions.CLClientVersionHeaderName, CLPrivateDefinitions.CLClientVersion);

            _trace.writeToLog(9, "CLPrivateRestClient: StreamingDownloadOperationForStorageKey_WithFileSystemPath_FileSize_AndMd5Hash: Built operation to download file.  Path: {0}, json: {1}, Request: {2}.", path, json, request.Headers.ToString());
            return new CLHTTPConnectionOperation(_client, request, path, size, hash, isUpload: false, uploadStream: null);
        }
    }
}
