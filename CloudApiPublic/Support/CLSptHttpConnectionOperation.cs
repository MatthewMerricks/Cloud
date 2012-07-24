//
//  CLSptHttpConnectionOperation.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Web;
using System.IO;
using CloudApiPublic.Model;
using CloudApiPublic.Support;
using CloudApiPublic.Static;
using System.Net.Http.Headers;


namespace CloudApiPublic.Support
{
    public class CLHTTPConnectionOperation : CLSptNSOperation
    {
        private const string CLHTTPConnectionOperaionDidEndNotification = "CLHTTPConnectionOperaionDidEndNotification";
        private const string CLHTTPConnectionOperaionDidStartNotification = "CLHTTPConnectionOperaionDidStartNotification";

        private static CLTrace _trace;

        delegate /*Task*/ void CLHTTPConnectionOperationProgressBlock(ulong bytes, ulong totalBytes, ulong totalBytesExpected);

        public Action<CLHTTPConnectionOperation, CLError> CompletionBlock { get; set; }

        private HttpClient _client;
        public HttpClient Client
        { 
            get { return _client; }
            set { _client = value; }
        }
        

        private string _syncID;
        public string SyncID
        {
            get
            {
                return _syncID;
            }
            set
            {
                _syncID = value;
            }
        }

        private long _eventID;
        public long EventID
        {
            get
            {
                return _eventID;
            }
            set
            {
                _eventID = value;
            }
        }

        public CLMetadata _metadata;
        public CLMetadata Metadata
        {
            get
            {
                return _metadata;
            }
            set
            {
                _metadata = value;
            }
        }

        private byte[] _responseData;
        public byte[] ResponseData
        {
            get
            {
                return _responseData;
            }
            set
            {
                _responseData = value;
            }
        }

        private HttpResponseMessage _response;
        public HttpResponseMessage Response
        {
            get
            {
                return _response;
            }
            set
            {
                _response = value;
            }
        }

        private HttpRequestMessage _operationRequest;
        public HttpRequestMessage OperationRequest
        {
            get
            {
                return _operationRequest;
            }
            set
            {
                _operationRequest = value;
            }
        }

        private bool _finished;
        public bool Finished
        {
            get
            {
                return _finished;
            }
            set
            {
                _finished = value;
            }
        }

        private string _responseFilePath;
        public string ResponseFilePath
        {
            get
            {
                return _responseFilePath;
            }
            set
            {
                _responseFilePath = value;
            }
        }

        private string _size;
        public string Size
        {
            get { return _size; }
            set { _size = value; }
        }

        private string _hash;

        public string Hash
        {
            get { return _hash; }
            set { _hash = value; }
        }

        private string _tempFilePath;
        public string TempFilePath
        {
            get { return _tempFilePath; }
            set { _tempFilePath = value; }
        }
        

        private bool _isDownloadOperation;
        public bool IsDownloadOperation
        {
            get { return _isDownloadOperation; }
            set { _isDownloadOperation = value; }
        }

        public FileStream UploadStream { get; set; }

        //TODO: Not used?
        // - (id)initWithURLRequest:(NSMutableURLRequest *)request andMetadata:(CLMetadata *)metadata
        //public CLHTTPConnectionOperation(HttpClient client, HttpRequestMessage request, CLMetadata metadata) : this(client, request)
        //{
        //    //if(self = [super init]) {
        //    //    _operationRequest = request;
        //    //    _executing = NO;
        //    //    _finished = NO;
        //    //    _metadata = metadata;
        //    //}
        //    //return self;
        //    _metadata = metadata;
        //}

        // - (id)initForStreamingUploadWithRequest:(NSMutableURLRequest *)request andFileSystemPath:(NSString *)fsPath
        // - (id)initForStreamingDownloadWithRequest:(NSMutableURLRequest *)request andFileSystemPath:(NSString *)fsPath
        public CLHTTPConnectionOperation(HttpClient client, HttpRequestMessage request, string fsPath, string size, string hash, bool isUpload, FileStream uploadStream)
            : this(client, request)
        {
            //if(self = [self initWithRequest:request]) {
            //    _responseFilePath = fsPath;
            //    _downloadOperation = NO;
            //}
            //return self;
            _responseFilePath = fsPath;
            _size = size;
            _hash = hash;
            _isDownloadOperation = !isUpload;
            this.UploadStream = uploadStream;
        }

        private class HttpClientTaskParameters
        {
            public HttpClient Client { get; set; }
            public HttpRequestMessage Message { get; set; }
            public object UserState { get; set; }
            public Action<HttpResponseMessage> SetResponse { get; set; }
        }

        public CLHTTPConnectionOperation(HttpClient client, HttpRequestMessage request)
        {
            //    _operationRequest = request;
            //    _executing = false;
            //    _finished = false;
            _client = client;
            _operationRequest = request;
            _finished = false;
            _trace = CLTrace.Instance;
        }

        CLHTTPConnectionOperation()
        {
            throw new NotImplementedException("Default constructor not supported.");
        }


        //- (void)start {
        public override CLError Main()
        {
            // Merged 7/13/12
            // if (self.isCancelled != YES) {
        
            //     NSRunLoop *currentRunLoop = [NSRunLoop currentRunLoop];
        
            //     [self willChangeValueForKey: @"isExecuting"];
            //     self.executing = YES;
            //     [self didChangeValueForKey: @"isExecuting"];
        
            //     if (self.responseFilePath && self.isDownloadOperation) {
            //         [self createStreamToTempFilePath];
            //     }else {
            //         self.inputStream = [NSInputStream inputStreamWithFileAtPath:self.responseFilePath];
            //     }
        
            //     self.urlConnection = [NSURLConnection connectionWithRequest:self.operationRequest delegate:self];
            //     [self.urlConnection scheduleInRunLoop:currentRunLoop forMode:NSRunLoopCommonModes];
            //     [self.urlConnection start];
        
            //     [[NSNotificationCenter defaultCenter] postNotificationName:CLHTTPConnectionOperaionDidStartNotification object:self];
    
            //     while (self.isExecuting) {
            //         [currentRunLoop runUntilDate:[NSDate dateWithTimeIntervalSinceNow:1]];
            //     }
            // }

            //&&&&&&

            CLError toReturn = null;
            try
            {
                // if (self.isCancelled != YES) {
                if (!this.IsCancelled)
                {
                    // NSRunLoop *currentRunLoop = [NSRunLoop currentRunLoop];

                    // [self willChangeValueForKey: @"isExecuting"];
                    // self.executing = YES;
                    // [self didChangeValueForKey: @"isExecuting"];
                    this.Executing = true;

                    // Note: Not necessary.  Reworked below.
                    // if (self.responseFilePath && self.isDownloadOperation) {
                    //     [self createStreamToTempFilePath];
                    // }else {
                    //     self.inputStream = [NSInputStream inputStreamWithFileAtPath:self.responseFilePath];
                    // }
                    // self.urlConnection = [NSURLConnection connectionWithRequest:self.operationRequest delegate:self];
                    // [self.urlConnection scheduleInRunLoop:currentRunLoop forMode:NSRunLoopCommonModes];
                    // [self.urlConnection start];

                    Task<HttpResponseMessage> task = null;
                    try
                    {
                        _trace.writeToLog(9, "CLSptHttpConnectionOperation: Main: Send the {0} operation to the server.", this.IsDownloadOperation ? "download" : "upload");
                        _trace.writeToLog(9, "CLSptHttpConnectionOperation: Main: Response file path: <{0}>.", this.ResponseFilePath);

                        if (this.IsDownloadOperation)
                        {
                            // Download operation
                            CreateStreamToTempFilePath();
                            task = _client.SendAsync(_operationRequest).ContinueWith<HttpResponseMessage>((requestTask) =>
                            {
                                HttpResponseMessage response = null;
                                try
                                {
                                    // Get HTTP response from completed task. 
                                    response = requestTask.Result;
                                    this.Response = response;

                                    // Check that response was successful or throw exception 
                                    response.EnsureSuccessStatusCode();

                                    // Read response asynchronously and save to file 
                                    response.Content.ReadAsFileAsync(this.TempFilePath, true).ContinueWith(
                                        (readTask) =>
                                        {
                                            _trace.writeToLog(1, "CLSptHttpConnectionOperation: Main: File copied to disk.");
                                        });
                                }
                                catch (Exception ex)
                                {
                                    toReturn += ex;
                                    _trace.writeToLog(1, "CLSptHttpConnectionOperation: Main: ERROR: Exception requesting download file transfer.  Msg: {0}, Code: {1}.",
                                                        toReturn.errorDescription, toReturn.errorCode);
                                }
                                return response;
                            });
                        }
                        else
                        {
                            // Upload operation
                            StreamContent fileContent = new StreamContent(this.UploadStream);

                            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                            _operationRequest.Content = fileContent;
                            _operationRequest.Content.Headers.Add("Content-MD5", Hash);
                            _operationRequest.Content.Headers.Add("Content-Length", Size);

                            task = new Task<HttpResponseMessage>(state =>
                                {
                                    HttpClientTaskParameters castState = state as HttpClientTaskParameters;
                                    HttpResponseMessage response = null;

                                    if (castState != null
                                        && castState.Client != null
                                        && castState.Message != null
                                        && castState.SetResponse != null)
                                    {
                                        try
                                        {
                                            response = castState.Client.SendAsync(castState.Message).Result;
                                        }
                                        catch (Exception ex)
                                        {
                                            toReturn += ex;
                                            _trace.writeToLog(1, "CLSptHttpConnectionOperation: Main: ERROR: Communication failure for upload. Msg: {0}, Code: {1}.",
                                                toReturn.errorDescription, toReturn.errorCode);
                                        }

                                        if (response != null)
                                        {
                                            try
                                            {
                                                FileStream toUnlock = castState.UserState as FileStream;
                                                if (toUnlock != null)
                                                {
                                                    toUnlock.Dispose();
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                toReturn += ex;
                                                _trace.writeToLog(1, "CLSptHttpConnectionOperation: Main: ERROR: Exception unlocking upload filestream.  Msg: {0}, Code: {1}.",
                                                                    toReturn.errorDescription, toReturn.errorCode);
                                            }

                                            try
                                            {
                                                // set the response
                                                castState.SetResponse(response);

                                                // Check that response was successful or throw exception 
                                                response.EnsureSuccessStatusCode();
                                            }
                                            catch (Exception ex)
                                            {
                                                toReturn += ex;
                                                _trace.writeToLog(1, "CLSptHttpConnectionOperation: Main: ERROR: Exception getting response from upload file transfer.  Msg: {0}, Code: {1}.",
                                                                    toReturn.errorDescription, toReturn.errorCode);
                                            }
                                        }
                                    }
                                    return response;

                                }, new HttpClientTaskParameters()
                                    {
                                        Client = _client,
                                        Message = _operationRequest,
                                        UserState = this.UploadStream,
                                        SetResponse = response => this.Response = response
                                    });

                            task.Start();
                        }
                    }
                    catch (Exception ex)
                    {
                        toReturn += ex;
                        _trace.writeToLog(1, "CLSptHttpConnectionOperation: Main: ERROR: Exception sending request for file upload/download.  Msg: {0}, Code: {1}.",
                                            toReturn.errorDescription, toReturn.errorCode);
                    }

                    if (task != null)
                    {
                        task.Wait();            // wait here for the file to transfer, or for an error
                        HandleTaskCompletion(task, "ErrorPuttingUploadOrDownloadToServer");
                    }
                    else
                    {
                        _trace.writeToLog(1, "CLSptHttpConnectionOperation: Main: ERROR: Task is null.");
                    }


                    //TODO: Notification required?
                    // [[NSNotificationCenter defaultCenter] postNotificationName:CLHTTPConnectionOperaionDidStartNotification object:self];

                    // while (self.isExecuting) {
                    //     [currentRunLoop runUntilDate:[NSDate dateWithTimeIntervalSinceNow:1]];
                    // }
                }
            }
            catch (Exception ex)
            {
                toReturn += ex;
                _trace.writeToLog(1, "CLSptHttpConnectionOperation: Main: ERROR: Unknown error.  Msg: {0}, Code: {1}.",
                                    toReturn.errorDescription, toReturn.errorCode);
            }
            return toReturn;
        }

        private void HandleTaskCompletion(Task<HttpResponseMessage> task, string resourceErrorMessageKey)
        {
            HttpResponseMessage response = null;
            CLError error = new Exception(CLSptResourceManager.Instance.ResMgr.GetString(resourceErrorMessageKey));  // init error which may not be used
            bool isError = false;       // T: an error was posted

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
            }
            else if (response == null)
            {
                isError = true;
                error.AddException(new Exception("Response from server was null"));
            }

            if (!isError)
            {
                error = null;
            }

            if (CompletionBlock != null)
            {
                CompletionBlock(this, error);
            }
        }

        public override void Cancel()
        {
            //if (!this.IsFinished() && !this.IsCancelled()) {
            //    this.WillChangeValueForKey("isCancelled");
            //    this.Cancelled = true;
            //    this.DidChangeValueForKey("isCancelled");
            //    this.CancelConnection();
            //}

            //base.Cancel();
        }

        void CancelConnection()
        {
            //if (this.UrlConnection) {
            //    (this.UrlConnection).Cancel();
            //    NSDictionary userInfo = null;
            //    if ((this.OperationRequest).URL()) {
            //        userInfo = NSDictionary.DictionaryWithObjectForKey((this.OperationRequest).URL(), NSURLErrorFailingURLErrorKey);
            //    }

            //    this.PerformSelectorWithObjectWithObject(@selector (connection:didFailWithError:), this.UrlConnection, NSError.ErrorWithDomainCodeUserInfo(
            //      NSURLErrorDomain, NSURLErrorCancelled, userInfo));
            //}

        }

        void CreateStreamToTempFilePath()
        {
            // Merged 7/13/12
            //NSString *guid = [[NSProcessInfo processInfo] globallyUniqueString];
            //NSString *uFileName = [NSString stringWithFormat:@"%@-%@", guid, [self.responseFilePath lastPathComponent]];
            //self.tempFilePath = [NSTemporaryDirectory() stringByAppendingPathComponent:uFileName];
            this.TempFilePath = GetUniqueFilePathInTempDirectory();
    
            //NSOutputStream *downloadStream = [[NSOutputStream alloc] initToFileAtPath:self.tempFilePath append:NO];
            //self.outputStream = downloadStream;
            //&&&&this.StreamOutput = File.Open(this.TempFilePath, FileMode.Create, FileAccess.Write);
        }

        string GetUniqueFilePathInTempDirectory()
        {
            string guid = Guid.NewGuid().ToString();
            string uFileName = String.Format("{0}-{1}", guid, ResponseFilePath.LastPathComponent());
            return Path.GetTempPath() + uFileName;
        }

        //- (void)moveTempFileToResourceFilePath
        void MoveTempFileToResourceFilePath()
        {
            // Merged 7/14/12
            // __weak CLHTTPConnectionOperation *weakSelf = self;
            // NOTE: The threading is commented out.  This runs on the operation's thread.
            // //dispatch_sync(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_HIGH, 0),^{
            //     __strong CLHTTPConnectionOperation *strongSelf = weakSelf;
            //     NSFileManager *fileManager = [NSFileManager defaultManager];
            //     if (strongSelf) {
            //         if ([fileManager fileExistsAtPath:strongSelf.tempFilePath]) {
            //             if ([fileManager fileExistsAtPath:strongSelf.responseFilePath]) {
                    
            //                 if (![fileManager contentsEqualAtPath:strongSelf.tempFilePath andPath:strongSelf.responseFilePath]){
            //                     NSError *replacementError;
            //                     NSURL *responseFilePathURL = [NSURL fileURLWithPath:strongSelf.responseFilePath];
            //                     NSURL *tempFilePathURL = [NSURL fileURLWithPath:strongSelf.tempFilePath];
            //                     NSString *backupName = [[strongSelf.responseFilePath lastPathComponent] stringByAppendingString:@"cloudbackup"];
                        
            //                     [fileManager replaceItemAtURL:responseFilePathURL withItemAtURL:tempFilePathURL
            //                                                                      backupItemName:backupName
            //                                                                             options:NSFileManagerItemReplacementUsingNewMetadataOnly
            //                                                                    resultingItemURL:nil error:&replacementError];
            //                     if (replacementError){
            //                         NSLog(@"%s - Could not replace file: %@ ", __FUNCTION__, replacementError);
            //                     }
            //                 }
            //             }else {
            //                 NSError *error;
            //                 if ([fileManager fileExistsAtPath:strongSelf.tempFilePath]){
            //                     [fileManager moveItemAtPath:strongSelf.tempFilePath toPath:strongSelf.responseFilePath error:&error];
            //                     if (error) {
            //                         NSLog(@"%s - Could not move file: %@ ", __FUNCTION__, error);
            //                     }
            //                 }
            //             }
            //         }
            //     }
            // [self updateCompletedState];
            //// });
            //&&&&

            // Note: The threading is commented out.  This runs on the operation's thread.
            // __weak CLHTTPConnectionOperation *weakSelf = self;
            // //dispatch_sync(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_HIGH, 0),^{

            // Note: Not required.
            // __strong CLHTTPConnectionOperation *strongSelf = weakSelf;
            // NSFileManager *fileManager = [NSFileManager defaultManager];
            // if (strongSelf) {

            // if ([fileManager fileExistsAtPath:strongSelf.tempFilePath]) {
            CLError error = null;
            if (File.Exists(this.TempFilePath))
            {
                // if ([fileManager fileExistsAtPath:strongSelf.responseFilePath]) {
                if (File.Exists(this.ResponseFilePath))
                {
                    // A file already exists at the target location.  See if it has the same contents.
                    // if (![fileManager contentsEqualAtPath:strongSelf.tempFilePath andPath:strongSelf.responseFilePath]){
                    try
                    {
                        bool filesSame = CLSptFileCompare.FileCompare(this.TempFilePath, this.ResponseFilePath, out error);
                        if (error == null)
                        {
                            if (!filesSame)
                            {
                                // NSError *replacementError;
                                // NSURL *responseFilePathURL = [NSURL fileURLWithPath:strongSelf.responseFilePath];
                                // NSURL *tempFilePathURL = [NSURL fileURLWithPath:strongSelf.tempFilePath];
                                // NSString *backupName = [[strongSelf.responseFilePath lastPathComponent] stringByAppendingString:@"cloudbackup"];

                                // [fileManager replaceItemAtURL:responseFilePathURL withItemAtURL:tempFilePathURL
                                //                                                  backupItemName:backupName
                                //                                                         options:NSFileManagerItemReplacementUsingNewMetadataOnly
                                //                                                resultingItemURL:nil error:&replacementError];
                                // if (replacementError){
                                //     NSLog(@"%s - Could not replace file: %@ ", __FUNCTION__, replacementError);
                                // }

                                // The target file exists, and it is different than the source file.
                                // Generate a unique filename in the temp directory
                                string uSaveFilePath = GetUniqueFilePathInTempDirectory();

                                // Move the existing target file from the target location to the unique file in the temp directory.
                                try
                                {
                                    File.Move(this.ResponseFilePath, uSaveFilePath);
                                }
                                catch (Exception ex)
                                {
                                    error += ex;
                                }
                                if (error == null)
                                {
                                    // The backup rename was successful
                                    // Move the downloaded temp file to the target location.
                                    try
                                    {
                                        // Move the downloaded temp file to the target location
                                        File.Move(this.TempFilePath, this.ResponseFilePath);
                                    }
                                    catch (Exception ex)
                                    {
                                        error += ex;
                                    }
                                    if (error == null)
                                    {
                                        // The downloaded file was moved to the target location successfully.
                                        // Delete the saved unique file in the temp directory.
                                        try
                                        {
                                            File.Delete(uSaveFilePath);
                                        }
                                        catch (Exception ex)
                                        {
                                            error += ex;
                                        }
                                    }

                                    else
                                    {
                                        // Error moving the downloaded file to the target location.
                                        // Try to recover by moving the saved file back to the target location
                                        // Log the error
                                        Exception ex = new Exception("Error moving downloaded file <" + this.TempFilePath + "> to the target location <" + this.ResponseFilePath + ">.");
                                        error += ex;

                                        // Try to recover the saved file
                                        try
                                        {
                                            File.Move(uSaveFilePath, this.ResponseFilePath);
                                        }
                                        catch (Exception)
                                        {
                                            error += ex;
                                        }
                                    }
                                }
                                else
                                {
                                    // Error renaming the backup file.
                                    Exception ex = new Exception("Error saving the existing target file <" + this.ResponseFilePath + "> to the target location <" + uSaveFilePath + ">.");
                                    error += ex;
                                }
                            }
                        }
                        else
                        {
                            // Error comparing the source file and the existing target file.
                            // Do nothing here.  error has the result of that error.  It will be logged below.
                        }
                    }
                    catch (Exception ex)
                    {
                        // Error comparing the existing target file to the downloaded file.
                        error += ex;
                    }
                }
                else
                {
                    // The target file does not already exist.  Move the downloaded file to the target location.
                    // NSError *error;
                    // if ([fileManager fileExistsAtPath:strongSelf.tempFilePath]){
                    //     [fileManager moveItemAtPath:strongSelf.tempFilePath toPath:strongSelf.responseFilePath error:&error];
                    //     if (error) {
                    //         NSLog(@"%s - Could not move file: %@ ", __FUNCTION__, error);
                    //     }
                    // }
                    try
                    {
                        File.Move(this.TempFilePath, this.ResponseFilePath);
                    }
                    catch (Exception ex)
                    {
                        error += ex;
                    }
                }
            }
            else
            {
                // The downloaded file does not exist!
                Exception ex = new Exception("Error: Expected the downloaded file to exist at <" + this.TempFilePath + ">, but it didn't exist.");
                error += ex;
            }

            // Log any errors.
            //TODO: Handle this error.  Enqueue the event to an error retry queue?
            //TODO: Badge this file in error?
            if (error != null)
            {
                _trace.writeToLog(1, "CLSptHttpConnectionOperation: MoveTempFileToResourceFilePath: ERROR: Exception. Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
            }

            // This operation is now complete.
            // [self updateCompletedState];
            UpdateCompletedState();
        }

        bool IsConcurrent()
        {
            return true;
        }

        bool IsFinished()
        {
            return this.Finished;
        }

        void UpdateCompletedState()
        {
            //this.WillChangeValueForKey("isExecuting");
            //this.Executing = false;
            //this.DidChangeValueForKey("isExecuting");
            //this.WillChangeValueForKey("isFinished");
            //this.Finished = true;
            //this.DidChangeValueForKey("isFinished");

            //TODO:  Is this notification required?
            //(NSNotificationCenter.DefaultCenter()).PostNotificationNameMyobject(CLHTTPConnectionOperaionDidEndNotification, this);
        }

        public void SetUploadProgressBlock(bool /*void*/ block)
        {
            //this.UploadProgress = block;
        }

        public void SetDownloadProgressBlock(bool /*void*/ block)
        {
            //this.DownloadProgress = block;
        }

        public void SetOperationCompletionBlock(Action<CLHTTPConnectionOperation, CLError> completionBlock)
        {
            //__weak CLHTTPConnectionOperation *weakSelf = self;
    
            //self.completionBlock = ^ {
        
            //    __strong CLHTTPConnectionOperation *strongSelf = weakSelf;
            //    if (strongSelf) {
            //        if ([strongSelf isCancelled]) {
            //            return;
            //        }
            
            //        if (completionBlock) {
            //            dispatch_async(dispatch_get_main_queue(), ^ {
            //                if (strongSelf.isDownloadOperation) {
            //                    [strongSelf moveTempFileToResourceFilePath];
            //                }
            //                completionBlock(strongSelf, strongSelf.responseData, strongSelf.error);
            //            });
            //        }
            //    }
            //};
            this.CompletionBlock = (CLHTTPConnectionOperation operation, CLError error) =>
            {
                if (this.IsCancelled)
                {
                    return;
                }

                if (completionBlock != null)
                {
                    Dispatch.Async<object>(CLSptResourceManager.Instance.MainGcdQueue, (obj, userState) =>
                    {
                        if (this.IsDownloadOperation)
                        {
                            MoveTempFileToResourceFilePath();
                        }
                        completionBlock(this, error);
                    }, null, null);
                }
            };
        }

        void ConnectionDidReceiveResponse(HttpClient connection, HttpResponseMessage response)
        {
            //this.Response = (NSHTTPURLResponse) response;
            //if (this.OutputStream) {
            //    (this.OutputStream).Open();
            //}

            //this.DataAccumulator = NSMutableData.Data();
        }

        void ConnectionDidReceiveData(HttpClient connection, byte[] data)
        {
            //NSUInteger dataLength = data.Length();
            //this.TotalBytesRead += dataLength;
            //if (this.OutputStream) {
            //    if ((this.OutputStream).HasSpaceAvailable()) {
            //        const uint8_t dataBuffer = (uint8_t) data.Bytes();
            //        (this.OutputStream).WriteMaxLength(dataBuffer[0], dataLength);
            //    }

            //}
            //else {
            //    (this.DataAccumulator).AppendData(data);
            //}

            //CLHTTPConnectionOperation blockSafeSelf = this;
            //if (this.DownloadProgress) {
            //    dispatch_async (dispatch_get_main_queue ());
            //    if (0) {
            //        blockSafeSelf.DownloadProgress(dataLength, blockSafeSelf.TotalBytesRead, blockSafeSelf.Response.ExpectedContentLength);
            //    }

            //}

        }

        void ConnectionDidFinishLoading(HttpClient connection)
        {
            //if (this.OutputStream) {
            //    this.ResponseData = (this.OutputStream).PropertyForKey(NSStreamDataWrittenToMemoryStreamKey);
            //    (this.OutputStream).Close();
            //}
            //else {
            //    this.ResponseData = NSData.DataWithData(this.DataAccumulator);
            //    this.DataAccumulator = null;
            //    this.UrlConnection = null;
            //}

            //this.UpdateCompletedState();
        }

        void ConnectionDidFailWithError(HttpClient connection, CLError connectionError)
        {
            //this.Error = connectionError;
            //(this.OutputStream).Close();
            //this.DataAccumulator = null;
            //this.UrlConnection = null;
            //this.UpdateCompletedState();
        }

        void ConnectionDidSendBodyDataTotalBytesWrittenTotalBytesExpectedToWrite(HttpClient connection, ulong bytesWritten, ulong totalBytesWritten, ulong totalBytesExpectedToWrite)
        {
            //CLHTTPConnectionOperation blockSafeSelf = this;
            //if (this.UploadProgress) {
            //    dispatch_async (dispatch_get_main_queue ());
            //    if (0) {
            //        blockSafeSelf.UploadProgress(bytesWritten, totalBytesWritten, totalBytesExpectedToWrite);
            //    }

            //}

        }

        void ConnectionDidReceiveAuthenticationChallenge(HttpClient connection, bool /* NSURLAuthenticationChallenge */ challenge)
        {
            //if (challenge.PreviousFailureCount() == 0) {
            //    NSURLCredential credential = null;
            //    if (credential) {
            //        (challenge.Sender()).UseCredentialForAuthenticationChallenge(credential, challenge);
            //    }
            //    else {
            //        (challenge.Sender()).ContinueWithoutCredentialForAuthenticationChallenge(challenge);
            //    }

            //}
            //else {
            //    (challenge.Sender()).ContinueWithoutCredentialForAuthenticationChallenge(challenge);
            //}

        }

        bool ConnectionCanAuthenticateAgainstProtectionSpace(HttpClient connection, bool /*NSURLProtectionSpace*/ protectionSpace)
        {
            return true;
        }

    }
}

