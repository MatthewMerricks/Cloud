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


namespace CloudApiPublic.Support
{
    public class CLHTTPConnectionOperation : CLSptNSOperation
    {
        private const string CLHTTPConnectionOperaionDidEndNotification = "CLHTTPConnectionOperaionDidEndNotification";
        private const string CLHTTPConnectionOperaionDidStartNotification = "CLHTTPConnectionOperaionDidStartNotification";

        delegate /*Task*/ void CLHTTPConnectionOperationProgressBlock(ulong bytes, ulong totalBytes, ulong totalBytesExpected);

        private string _path;
        public string Path
        {
            get
            {
                return _path;
            }
            set
            {
                _path = value;
            }
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

        private CLError _error;
        public CLError Error
        {
            get
            {
                return _error;
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

        private HttpClient _httpConnection;
        public HttpClient HttpConnection
        {
            get
            {
                return _httpConnection;
            }
            set
            {
                _httpConnection = value;
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

        private Stream _inputStream;
        public Stream InputStream
        {
            get
            {
                return _inputStream;
            }
            set
            {
                //this.WillChangeValueForKey("inputStream");
                //this.OperationRequest.HTTPBodyStream = value;
                //this.DidChangeValueForKey("inputStream");
            }
        }

        private Stream _outputStream;
        public Stream OutputStream
        {
            get
            {
                return _outputStream;
            }
            set
            {
                //this.WillChangeValueForKey("outputStream");
                //if (_outputStream) {
                //    _outputStream.Close();
                //    _outputStream = null;
                //}

                //_outputStream = value;
                //this.DidChangeValueForKey("outputStream");
                //NSRunLoop runLoop = NSRunLoop.CurrentRunLoop();
                //(this.Value).ScheduleInRunLoopForMode(runLoop, NSRunLoopCommonModes);
            }
        }

        private bool _executing;
        public bool Executing
        {
            get
            {
                return _executing;
            }
            set
            {
                _executing = value;
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

        public CLHTTPConnectionOperation(HttpRequestMessage request, CLMetadata metadata)
        {
            _operationRequest = request;
            _executing = false;
            _finished = false;
            _metadata = metadata;
        }

        public CLHTTPConnectionOperation(HttpRequestMessage request, string fsPath)
        {
            //if (this = this.initWithRequest(request)) {
            //    _responseFilePath = fsPath;
            //    _isDownloadOperation = false;
            //}

            //return this;
        }

        public CLHTTPConnectionOperation(HttpResponseMessage request, string fsPath)
        {
            //if (this = this.initWithRequest(request)) {
            //    _responseFilePath = fsPath;
            //    _isDownloadOperation = true;
            //}

            //return this;
        }

        CLHTTPConnectionOperation(HttpRequestMessage request)
        {
            //if (this = this.initWithRequest(request)) {
            //}

            //return this;
        }

        //public CLHTTPConnectionOperation(HttpRequestMessage request)
        //{
        //    _operationRequest = request;
        //    _executing = false;
        //    _finished = false;
        //}

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

        bool IsExecuting()
        {
            return this.Executing;
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

        public void SetCompletionBlockWithSuccessFailure(bool success, bool failure)
        {
            //CLHTTPConnectionOperation blockSafeSelf = this;
            //this.CompletionBlock = 9999;
            //if (0) {
            //    if (blockSafeSelf.IsCancelled()) {
            //        return;
            //    }

            //    if (blockSafeSelf.Error) {
            //        if (failure) {
            //            dispatch_async(dispatch_get_main_queue(), ^ (void) {
            //                failure(blockSafeSelf, blockSafeSelf.Error);
            //            }
            //            );
            //        }

            //    }
            //    else {
            //        if (success) {
            //            dispatch_async (dispatch_get_main_queue ());
            //            if (0) {
            //                success(blockSafeSelf, blockSafeSelf.ResponseData);
            //            };

            //        }

            //    }

            //};

        }

        public void SetOperationCompletionBlock(bool completionBlock)
        {
            //CLHTTPConnectionOperation blockSafeSelf = this;
            //this.CompletionBlock = 9999;
            //if (0) {
            //    if (blockSafeSelf.IsCancelled()) {
            //        return;
            //    }

            //    if (completionBlock) {
            //        dispatch_async (dispatch_get_main_queue ());
            //        if (0) {
            //            blockSafeSelf.MoveTempFileToResourceFilePath();
            //            completionBlock(blockSafeSelf, blockSafeSelf.ResponseData, blockSafeSelf.Error);
            //        }

            //    }

            //};

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

