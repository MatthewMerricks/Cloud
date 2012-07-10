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

        private bool _isDownloadOperation;
        public bool IsDownloadOperation
        {
            get { return _isDownloadOperation; }
            set { _isDownloadOperation = value; }
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
            _operationRequest = request;
            _finished = false;
        }

        CLHTTPConnectionOperation()
        {
            throw new NotImplementedException("Default constructor not supported.");
        }

        public override void Main()
        {
            //if (this.IsCancelled != true) {
            //    NSRunLoop currentRunLoop = NSRunLoop.CurrentRunLoop();
            //    this.WillChangeValueForKey("isExecuting");
            //    this.Executing = true;
            //    this.DidChangeValueForKey("isExecuting");
            //    if (this.ResponseFilePath && this.IsDownloadOperation) {
            //        this.CreateStreamToTempFilePath();
            //    }
            //    else {
            //        this.InputStream = NSInputStream.InputStreamWithFileAtPath(this.ResponseFilePath);
            //    }

            //    this.UrlConnection = NSURLConnection.ConnectionWithRequestMydelegate(this.OperationRequest, this);
            //    (this.UrlConnection).ScheduleInRunLoopForMode(currentRunLoop, NSRunLoopCommonModes);
            //    (this.UrlConnection).Start();
            //    (NSNotificationCenter.DefaultCenter()).PostNotificationNameMyobject(CLHTTPConnectionOperaionDidStartNotification, this);
            //    while (this.IsExecuting) {
            //        currentRunLoop.RunUntilDate(NSDate.DateWithTimeIntervalSinceNow(1));
            //    }

            //}
            //&&&&&&

            if (!this.IsCancelled)
            {
                // if (this.ResponseFilePath && this.IsDownloadOperation) {
                HttpContent fileContent;
                if (this.ResponseFilePath != null && this.IsDownloadOperation)
                {
                    // this.CreateStreamToTempFilePath();
                    //this.TempFilePath = (NSTemporaryDirectory()).StringByAppendingPathComponent((this.ResponseFilePath).LastPathComponent());
                    //NSOutputStream downloadStream = new NSOutputStream(this.TempFilePath, false);
                    //this.OutputStream = downloadStream;                }
                    FileStream streamOutput = File.Open(Path.GetTempPath() + "\\" + this.ResponseFilePath.Substring(this.ResponseFilePath.LastIndexOf('\\')), 
                                                        FileMode.Create, FileAccess.Write);
                    fileContent = new StreamContent(streamOutput);
                }
                else
                {
                    // this.InputStream = NSInputStream.InputStreamWithFileAtPath(this.ResponseFilePath);
                    FileStream streamInput = File.Open(this.ResponseFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    fileContent = new StreamContent(streamInput);
                }

                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                _operationRequest.Content = fileContent;

                // this.UrlConnection = NSURLConnection.ConnectionWithRequestMydelegate(this.OperationRequest, this);
                // (this.UrlConnection).ScheduleInRunLoopForMode(currentRunLoop, NSRunLoopCommonModes);
                // (this.UrlConnection).Start();
                // (NSNotificationCenter.DefaultCenter()).PostNotificationNameMyobject(CLHTTPConnectionOperaionDidStartNotification, this);
                // while (this.IsExecuting) {
                //    currentRunLoop.RunUntilDate(NSDate.DateWithTimeIntervalSinceNow(1));
                // }
                Task<HttpResponseMessage> task = _client.SendAsync(_operationRequest);
                task.Wait();            // wait here for the file to transfer, or for an error
                HandleTaskCompletion(task, "ErrorPuttingUploadOrDownloadToServer");
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
            //this.TempFilePath = (NSTemporaryDirectory()).StringByAppendingPathComponent((this.ResponseFilePath).LastPathComponent());
            //NSOutputStream downloadStream = new NSOutputStream(this.TempFilePath, false);
            //this.OutputStream = downloadStream;
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

