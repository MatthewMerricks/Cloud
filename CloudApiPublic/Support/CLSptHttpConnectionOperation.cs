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

        delegate /*Task*/ void CLHTTPConnectionOperationProgressBlock(ulong bytes, ulong totalBytes, ulong totalBytesExpected);

        private Action<CLHTTPConnectionOperation, CLError> _completionBlock;
        public Action<CLHTTPConnectionOperation, CLError> CompletionBlock
        {
            get { return _completionBlock; }
            set { _completionBlock = value; }
        }
        

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

        private ulong _eventID;
        public ulong EventID
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

        private FileStream _streamOutput;
        public FileStream StreamOutput
        {
            get { return _streamOutput; }
            set { _streamOutput = value; }
        }

        private FileStream _streamInput;
        public FileStream StreamInput
        {
            get { return _streamInput; }
            set { _streamInput = value; }
        }
        

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
        public CLHTTPConnectionOperation(HttpClient client, HttpRequestMessage request, string fsPath, bool isUpload)
            : this(client, request)
        {
            //if(self = [self initWithRequest:request]) {
            //    _responseFilePath = fsPath;
            //    _downloadOperation = NO;
            //}
            //return self;
            _responseFilePath = fsPath;
            _isDownloadOperation = !isUpload;
        }

        public CLHTTPConnectionOperation(HttpClient client, HttpRequestMessage request)
        {
            //    _operationRequest = request;
            //    _executing = false;
            //    _finished = false;
            _client = client;
            _operationRequest = request;
            _finished = false;
        }

        CLHTTPConnectionOperation()
        {
            throw new NotImplementedException("Default constructor not supported.");
        }


        //- (void)start {
        public override void Main()
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

            // if (self.isCancelled != YES) {
            if (!this.IsCancelled)
            {
                // NSRunLoop *currentRunLoop = [NSRunLoop currentRunLoop];

                // [self willChangeValueForKey: @"isExecuting"];
                // self.executing = YES;
                // [self didChangeValueForKey: @"isExecuting"];
                this.Executing = true;

                // if (this.ResponseFilePath && this.IsDownloadOperation) {
                HttpContent fileContent;
                if (this.ResponseFilePath != null && this.IsDownloadOperation)
                {
                    // [self createStreamToTempFilePath];
                    CreateStreamToTempFilePath();
           
                    fileContent = new StreamContent(this.StreamOutput);
                }
                else
                {
                    // self.inputStream = [NSInputStream inputStreamWithFileAtPath:self.responseFilePath];
                    this.StreamInput = File.Open(this.ResponseFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    fileContent = new StreamContent(this.StreamInput);
                }

                // self.urlConnection = [NSURLConnection connectionWithRequest:self.operationRequest delegate:self];
                // [self.urlConnection scheduleInRunLoop:currentRunLoop forMode:NSRunLoopCommonModes];
                // [self.urlConnection start];
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                _operationRequest.Content = fileContent;

                Task<HttpResponseMessage> task = _client.SendAsync(_operationRequest);
                task.Wait();            // wait here for the file to transfer, or for an error
                HandleTaskCompletion(task, "ErrorPuttingUploadOrDownloadToServer");

                //TODO: Notification required?
                // [[NSNotificationCenter defaultCenter] postNotificationName:CLHTTPConnectionOperaionDidStartNotification object:self];

                // while (self.isExecuting) {
                //     [currentRunLoop runUntilDate:[NSDate dateWithTimeIntervalSinceNow:1]];
                // }
            }

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
            string guid = Guid.NewGuid().ToString();
            string uFileName = String.Format("{0}-{1}", guid, ResponseFilePath.LastPathComponent());
            this.TempFilePath = Path.GetTempPath() + uFileName;
    
            //NSOutputStream *downloadStream = [[NSOutputStream alloc] initToFileAtPath:self.tempFilePath append:NO];
            //self.outputStream = downloadStream;
            this.StreamOutput = File.Open(this.TempFilePath, FileMode.Create, FileAccess.Write);

        }

        void MoveTempFileToResourceFilePath()
        {
            //dispatch_sync(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_HIGH, 0));
            //if (0) {
            //    NSFileManager fileManager = NSFileManager.DefaultManager();
            //    if (fileManager.FileExistsAtPath(this.TempFilePath)) {
            //        NSError error;
            //        fileManager.MoveItemAtPathToPathError(this.TempFilePath, this.ResponseFilePath, error);
            //        if (error) {
            //            Console.WriteLine("%s - Could not move file: %@ ", __FUNCTION__, error);
            //        }

            //    }

            //};

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
                    Dispatch.Async<object>(CLSptResourceManager.Instance.MainGcdQueue, (obj) =>
                    {
                        if (this.IsDownloadOperation)
                        {
                            MoveTempFileToResourceFilePath();
                        }
                        completionBlock(this, error);
                    }, null);
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

