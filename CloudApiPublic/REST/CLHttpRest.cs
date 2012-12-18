using CloudApiPublic.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudApiPublic.Sync;
using System.IO;
using System.Threading;
using System.Net;
using CloudApiPublic.Model;
using CloudApiPublic.JsonContracts;
using CloudApiPublic.Static;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using CloudApiPublic.Support;

namespace CloudApiPublic.REST
{
    public sealed class CLHttpRest
    {
        #region private static readonly fields
        // hash set for http communication methods which are good when the status is ok, created, or not modified
        private static readonly HashSet<HttpStatusCode> okCreatedNotModified = new HashSet<HttpStatusCode>(new[]
            {
                HttpStatusCode.OK,
                HttpStatusCode.Created,
                HttpStatusCode.NotModified,
            });

        // hash set for http communication methods which are good when the status is ok or accepted
        private static readonly HashSet<HttpStatusCode> okAccepted = new HashSet<HttpStatusCode>(new[]
            {
                HttpStatusCode.OK,
                HttpStatusCode.Accepted
            });

        // dictionary to find which Json contract serializer to use given a provided input type
        private static readonly Dictionary<Type, DataContractJsonSerializer> SerializableResponseTypes = new Dictionary<Type, DataContractJsonSerializer>()
        {
            { typeof(JsonContracts.Metadata), JsonContractHelpers.GetMetadataResponseSerializer },
            { typeof(JsonContracts.NotificationResponse), JsonContractHelpers.NotificationResponseSerializer },
            { typeof(JsonContracts.PurgePendingResponse), JsonContractHelpers.PurgePendingResponseSerializer },
            { typeof(JsonContracts.PushResponse), JsonContractHelpers.PushResponseSerializer },
            { typeof(JsonContracts.To), JsonContractHelpers.ToSerializer }
        };
        #endregion

        #region construct with settings so they do not always need to be passed in
        // storage of settings, which should be a copy of settings passed in on construction so they do not change throughout communication
        private readonly ISyncSettingsAdvanced settings;

        // private constructor requiring settings to copy and store for the life of this http client
        private CLHttpRest(IHttpSettings settings)
        {
            if (settings == null)
            {
                throw new NullReferenceException("settings cannot be null");
            }

            this.settings = settings.CopySettings();
        }

        /// <summary>
        /// Creates a CLHttpRest client object for HTTP REST calls to the server
        /// </summary>
        /// <param name="settings">Required settings for communication</param>
        /// <param name="client">(output) Created CLHttpRest client or default (null) for errors</param>
        /// <returns>Returns any error creating the CLHttpRest client, if any</returns>
        public static CLError CreateAndInitialize(IHttpSettings settings, out CLHttpRest client)
        {
            try
            {
                client = new CLHttpRest(settings);
            }
            catch (Exception ex)
            {
                client = Helpers.DefaultForType<CLHttpRest>();
                return ex;
            }
            return null;
        }
        #endregion

        #region public API calls

        /// <summary>
        /// Uploads a file from a provided stream and file upload change
        /// </summary>
        /// <param name="uploadStream">Stream to upload, if it is a FileStream then make sure the file is locked to prevent simultaneous writes</param>
        /// <param name="changeToUpload">File upload change, requires Metadata.HashableProperties.Size, NewPath, StorageKey, and MD5 hash to be set</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception, does not restrict time for the actual file upload</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="shutdownToken">(optional) Token used to request cancellation of the upload.</param>
        /// <returns>Returns any error that occurred during communication, or null.</returns>
        public CLError UploadFile(Stream uploadStream,
            FileChange changeToUpload,
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            CancellationTokenSource shutdownToken = null)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the file upload, on catch return the error
            try
            {
                // check input parameters (other checks are done on constructing the private upload class upon ProcessHttp)

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // run the HTTP communication
                ProcessHttp<object, object>(null, // the stream inside the upload parameter object is the request content, so no JSON contract object
                    CLDefinitions.CLUploadDownloadServerURL,  // Server URL
                    CLDefinitions.MethodPathUpload, // path to upload
                    requestMethod.put, // upload is a put
                    timeoutMilliseconds, // time before communication timeout (does not restrict time for the actual file upload)
                    new upload( // this is a special communication method and requires passing upload parameters
                        uploadStream, // stream for file to upload
                        HandleUploadDownloadStatus, // private event handler to relay status change events
                        changeToUpload, // the FileChange describing the upload
                        shutdownToken, // a provided, possibly null CancellationTokenSource which can be cancelled to stop in the middle of communication
                        settings.SyncRoot), // pass in the full path to the sync root folder which is used to calculate a relative path for firing the status change event
                    okCreatedNotModified, // use the hashset for ok/created/not modified as successful HttpStatusCodes
                    ref status); // reference to update the output success/failure status for the communication
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Downloads a file to a provided stream using a file download change
        /// </summary>
        /// <param name="beforeDownloadCallback">Called back before downloading the file.</param>
        /// <param name="beforeDownloadUserState">User state before downloading the file.</param>
        /// <param name="afterDownloadToTempFileCallback">Called back after downloading the file to a temp file in the temp file folder.</param>
        /// <param name="afterDownloadUserState">User state after downloading the file to a temp file.</param>
        /// <param name="tempDownloadFolderPath">Folder to contain the temp downloaded files.</param>
        /// <param name="sendUploadDownloadStatusCallback">Called back with download status.</param>
        /// <param name="changeToDownload">File download change, requires the StorageKey to be set</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception, does not restrict time for the actual file download</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="shutdownToken">(optional) Token used to request cancellation of the download.</param>
        /// <returns>Returns any error that occurred during communication, or null.</returns>
        public CLError DownloadFile(
            BeforeDownloadToTempFile beforeDownloadCallback,
            object beforeDownloadUserState,
            AfterDownloadToTempFile afterDownloadToTempFileCallback,
            object afterDownloadUserState,
            string tempDownloadFolderPath,
            SendUploadDownloadStatus sendUploadDownloadStatusCallback,
            FileChange changeToDownload,
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            out string responseBody,
            CancellationTokenSource shutdownToken = null)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            responseBody = "---Incomplete file download---";

            // try/catch to process the file download, on catch return the error
            try
            {
                // check input parameters (other checks are done on constructing the private upload class upon ProcessHttp)

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // run the HTTP communication
                responseBody = ProcessHttp<object, string>(null, // the stream inside the upload parameter object is the request content, so no JSON contract object
                    CLDefinitions.CLUploadDownloadServerURL,  // Server URL
                    CLDefinitions.MethodPathDownload, // path to download
                    requestMethod.post, // download request is an HTTP POST
                    timeoutMilliseconds, // time before communication timeout (does not restrict time for the actual file upload)
                    new download(                   // this is a special communication method and requires passing download parameters
                        BeforeDownloadCallback: beforeDownloadCallback,             // called before download
                        BeforeDownloadUserState: beforeDownloadUserState,           // user state before download
                        AfterDownloadCallback: afterDownloadToTempFileCallback,     // called after download to temp file completes
                        AfterDownloadUserState: afterDownloadUserState,             // user state after download
                        TempDownloadFolderPath: tempDownloadFolderPath,             // the temp file folder path
                        StatusCallback: sendUploadDownloadStatusCallback,           // called with download status
                        ChangeToTransfer: changeToDownload,                         // FileChange representing the file to download
                        ShutdownToken: shutdownToken, // a provided, possibly null CancellationTokenSource which can be cancelled to stop in the middle of communication
                        SyncRootFullPath: settings.SyncRoot), // pass in the full path to the sync root folder which is used to calculate a relative path for firing the status change event
                    okCreatedNotModified, // use the hashset for ok/created/not modified as successful HttpStatusCodes
                    ref status); // reference to update the output success/failure status for the communication
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Queries the server at a given file or folder path (must be specified) for existing metadata at that path; outputs CLHttpRestStatus.NoContent for status if not found on server
        /// </summary>
        /// <param name="fullPath">Full path to where file or folder would exist locally on disk</param>
        /// <param name="isFolder">Whether the query is for a folder (as opposed to a file/link)</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, or null.</returns>
        public CLError GetMetadataAtPath(FilePath fullPath, bool isFolder, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.Metadata response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the metadata query, on catch return the error
            try
            {
                // check input parameters

                if (fullPath == null)
                {
                    throw new NullReferenceException("fullPath cannot be null");
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // build the location of the metadata retrieval method on the server dynamically
                string serverMethodPath = 
                    (isFolder
                        ? CLDefinitions.MethodPathGetFolderMetadata // if the current metadata is for a folder, then retrieve it from the folder method
                        : CLDefinitions.MethodPathGetFileMetadata) + // else if the current metadata is for a file, then retrieve it from the file method
                    Helpers.QueryStringBuilder(new[] // both methods grab their parameters by query string (since this method is an HTTP GET)
                    {
                        // query string parameter for the path to query, built by turning the full path location into a relative path from the cloud root and then escaping the whole thing for a url
                        new KeyValuePair<string, string>(CLDefinitions.CLMetadataCloudPath, Uri.EscapeDataString(fullPath.GetRelativePath((settings.SyncRoot ?? string.Empty), true) + "/")),

                        // query string parameter for the current user id, should not need escaping since it should be an integer in string format, but do it anyways
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringUserId, Uri.EscapeDataString(settings.SyncBoxId))
                    });

                // run the HTTP communication and store the response object to the output parameter
                response = ProcessHttp<object, JsonContracts.Metadata>(null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL,   // base domain is the MDS server
                    serverMethodPath, // path to query metadata (dynamic based on file or folder)
                    requestMethod.get, // query metadata is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    okAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status); // reference to update the output success/failure status for the communication
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.Metadata>();
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Sends a list of sync events to the server.  The events must be batched in groups of 1,000 or less.
        /// </summary>
        /// <param name="syncTo">The array of events to send to the server.</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, or null.</returns>
        public CLError PostSyncToCloud(To syncTo, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.To response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;

            // try/catch to process the sync_to request, on catch return the error
            try
            {
                // check input parameters
                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // build the location of the metadata retrieval method on the server dynamically
                string serverMethodPath = CLDefinitions.MethodPathSyncTo;

                // run the HTTP communication and store the response object to the output parameter
                response = ProcessHttp<object, JsonContracts.To>(
                    syncTo,
                    CLDefinitions.CLMetaDataServerURL,   // base domain is the MDS server
                    serverMethodPath, // path to query metadata (dynamic based on file or folder)
                    requestMethod.post, // sync_to is a post
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    okAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status); // reference to update the output success/failure status for the communication
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.To>();
                return ex;
            }

            return null;
        }
        #endregion

        #region internal API calls
        /// <summary>
        /// Purges any pending changes for the provided user/device combination in the request object (pending file uploads) and outputs the files which were purged
        /// </summary>
        /// <param name="request">Object to store the user/device combination to purge</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError PurgePending(JsonContracts.PurgePending request, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.PurgePendingResponse response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the metadata query, on catch return the error
            try
            {
                // check input parameters

                if (request == null)
                {
                    throw new NullReferenceException("request cannot be null");
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                response = ProcessHttp<JsonContracts.PurgePending, JsonContracts.PurgePendingResponse>(request, // json contract object for purge pending method
                    CLDefinitions.CLMetaDataServerURL, CLDefinitions.MethodPathPurgePending, // purge pending address
                    requestMethod.post, // purge pending is a post operation
                    timeoutMilliseconds, // set the timeout for the operation
                    null, // not an upload or download
                    okAccepted, // purge pending should give OK or Accepted
                    ref status); // reference to update output status
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.PurgePendingResponse>();
                return ex;
            }
            return null;
        }
        #endregion

        #region private helpers
        // event handler fired upon transfer buffer clears for uploads/downloads to relay to the global event
        private void HandleUploadDownloadStatus(CLStatusFileTransferUpdateParameters status, FileChange eventSource)
        {
            // validate parameter which can throw an exception in this method

            if (eventSource == null)
            {
                throw new NullReferenceException("eventSource cannot be null");
            }

            // direction of communication determines which event to fire
            if (eventSource.Direction == SyncDirection.To)
            {
                MessageEvents.UpdateFileUpload(eventSource, // source of the event (the event itself)
                    eventSource.EventId, // the id for the event
                    status); // the event arguments describing the status change
            }
            else
            {
                MessageEvents.UpdateFileDownload(eventSource, // source of the event (the event itself)
                    eventSource.EventId, // the id for the event
                    status); // the event arguments describing the status change
            }
        }

        // main HTTP REST routine helper method which processes the actual communication
        // Tin should be the type of the JSON contract object to serialize and send up if any, otherwise use string/object type
        // Tout should be the type of the JSON contract object which can be deserialized from the return response of the server if any, otherwise use string/object type which will be filled in as the entire string response
        private Tout ProcessHttp<Tin, Tout>(Tin requestContent, // JSON contract object to serialize and send up as the request content, if any
            string serverUrl, // the server URL
            string serverMethodPath,    // the server method path
            requestMethod method, // type of HTTP method (get vs. put vs. post)
            int timeoutMilliseconds, // time before communication timeout (does not restrict time for the upload or download of files)
            uploadDownloadParams uploadDownload, // parameters if the method is for a file upload or download, or null otherwise
            HashSet<HttpStatusCode> validStatusCodes, // a HashSet with HttpStatusCodes which should be considered all possible successful return codes from the server
            ref CLHttpRestStatus status) // reference to the successful/failed state of communication
            where Tin : class // restrict Tin to an object type to allow default null return
            where Tout : class // restrict Tout to an object type to allow default null return
        {
            // create the main request object for the provided uri location
            HttpWebRequest httpRequest = (HttpWebRequest)HttpWebRequest.Create(serverUrl + serverMethodPath);

            #region set request parameters
            // switch case to set the HTTP method (GET vs. POST vs. PUT); throw exception if not supported yet
            switch (method)
            {
                case requestMethod.get:
                    httpRequest.Method = CLDefinitions.HeaderAppendMethodGet;
                    break;
                case requestMethod.post:
                    httpRequest.Method = CLDefinitions.HeaderAppendMethodPost;
                    break;
                case requestMethod.put:
                    httpRequest.Method = CLDefinitions.HeaderAppendMethodPut;
                    break;

                default:
                    throw new ArgumentException("Unknown method: " + method.ToString());
            }

            // set more request parameters

            httpRequest.UserAgent = CLDefinitions.HeaderAppendCloudClient; // set client
            // Add the client type and version.  For the Windows client, it will be Wnn.  e.g., W01 for the 0.1 client.
            httpRequest.Headers[CLDefinitions.CLClientVersionHeaderName] = settings.ClientVersion; // set client version
            httpRequest.Headers[CLDefinitions.HeaderKeyAuthorization] = CLDefinitions.HeaderAppendToken +
                             CLDefinitions.WrapInDoubleQuotes(
                                        GenerateAuthorizationHeaderToken(
                                            settings,
                                            httpMethod: httpRequest.Method,
                                            pathAndQueryStringAndFragment: CLDefinitions.MethodPathUpload,
                                            serverUrl: CLDefinitions.CLUploadDownloadServerURL));   // set the authentication token
            httpRequest.SendChunked = false; // do not send chunked
            httpRequest.Timeout = timeoutMilliseconds; // set timeout by input parameter, timeout does not apply to the amount of time it takes to perform uploading or downloading of a file

            // declare the bytes for the serialized request body content
            byte[] requestContentBytes;
            
            // for any communication which is not a file upload, determine the bytes which will be sent up in the request
            if (uploadDownload == null ||
                !(uploadDownload is upload))
            {
                // if there is no content for the request (such as for an HTTP Get method call), then set the bytes as null
                if (requestContent == null)
                {
                    requestContentBytes = null;
                }
                // else if there is content for the request, then serialize the requestContent object and store the bytes to send up
                else
                {
                    // declare a string for the request body content
                    string requestString;
                    // create a stream for serializing the request object
                    using (MemoryStream requestMemory = new MemoryStream())
                    {
                        // serialize the request object into the stream with the appropriate serializer based on the input type, and if the type is not supported then throw an exception

                        if (requestContent is JsonContracts.Download)
                        {
                            JsonContractHelpers.DownloadSerializer.WriteObject(requestMemory, requestContent);
                        }
                        else if (requestContent is JsonContracts.PurgePending)
                        {
                            JsonContractHelpers.PurgePendingSerializer.WriteObject(requestMemory, requestContent);
                        }
                        else if (requestContent is JsonContracts.Push)
                        {
                            JsonContractHelpers.PushSerializer.WriteObject(requestMemory, requestContent);
                        }
                        else if (requestContent is JsonContracts.To)
                        {
                            JsonContractHelpers.ToSerializer.WriteObject(requestMemory, requestContent);
                        }
                        else
                        {
                            throw new ArgumentException("Unknown requestContent Type: " + requestContent.GetType().FullName);
                        }

                        // grab the string from the serialized data
                        requestString = Encoding.Default.GetString(requestMemory.ToArray());
                    }

                    // grab the bytes for the serialized request body content
                    requestContentBytes = Encoding.UTF8.GetBytes(requestString);

                    // configure request parameters based on a json request body content

                    httpRequest.ContentType = CLDefinitions.HeaderAppendContentTypeJson; // the request body content is json-formatted
                    httpRequest.ContentLength = requestContentBytes.LongLength; // set the size of the request content
                    httpRequest.Headers[CLDefinitions.HeaderKeyContentEncoding] = CLDefinitions.HeaderAppendContentEncoding; // the json content is utf8 encoded
                }
            }
            // else if communication is for a file upload, then set the appropriate request parameters
            else
            {
                httpRequest.ContentType = CLDefinitions.HeaderAppendContentTypeBinary; // content will be direct binary stream
                httpRequest.ContentLength = uploadDownload.ChangeToTransfer.Metadata.HashableProperties.Size ?? 0; // content length will be file size
                httpRequest.Headers[CLDefinitions.HeaderAppendStorageKey] = uploadDownload.ChangeToTransfer.Metadata.StorageKey; // add header for destination location of file
                httpRequest.Headers[CLDefinitions.HeaderAppendContentMD5] = ((upload)uploadDownload).Hash; // set MD5 content hash for verification of upload stream
                httpRequest.KeepAlive = true; // do not close connection (is this needed?)
                requestContentBytes = null; // do not write content bytes since they will come from the Stream inside the upload object
            }
            #endregion

            #region trace request
            // if communication is supposed to be traced, then trace it
            if ((settings.TraceType & TraceType.Communication) == TraceType.Communication)
            {
                // trace communication for the current request
                Trace.LogCommunication(settings.TraceLocation, // location of trace file
                    settings.Udid, // device id
                    settings.SyncBoxId, // user id
                    CommunicationEntryDirection.Request, // direction is request
                    serverUrl + serverMethodPath, // location for the server method
                    true, // trace is enabled
                    httpRequest.Headers, // headers of request
                    ((uploadDownload != null && uploadDownload is upload) // special condition for the request body content based on whether this is a file upload or not
                        ? "---File upload started---" // truncate the request body content to a predefined string so that the entire uploaded file is not written as content
                        : (requestContentBytes == null // condition on whether there were bytes to write in the request content body
                            ? null // if there were no bytes to write in the request content body, then log for none
                            : Encoding.UTF8.GetString(requestContentBytes))), // if there were no bytes to write in the request content body, then log them (in string form)
                    null, // no status code for requests
                    settings.TraceExcludeAuthorization, // whether or not to exclude authorization information (like the authentication key)
                    httpRequest.Host, // host value which would be part of the headers (but cannot be pulled from headers directly)
                    ((requestContentBytes != null || (uploadDownload != null && uploadDownload is upload))
                        ? httpRequest.ContentLength.ToString() // if the communication had bytes to upload from an input object or a stream to upload for a file, then set the content length value which would be part of the headers (but cannot be pulled from headers directly)
                        : null), // else if the communication would not have any request content, then log no content length header
                    (httpRequest.Expect == null ? "100-continue" : httpRequest.Expect), // expect value which would be part of the headers (but cannot be pulled from headers directly)
                    (httpRequest.KeepAlive ? "Keep-Alive" : "Close")); // keep-alive value which would be part of the headers (but cannot be pulled from headers directly)
            }
            #endregion

            // status setup is for file uploads and downloads which fire event callbacks to fire global status events
            #region status setup
            // define size to be used for status update event callbacks
            long storeSizeForStatus;
            // declare the time when the transfer started (inaccurate for file downloads since the time is set before the request for the download and not before the download actually starts)
            DateTime transferStartTime;

            // if this communiction is not for a file upload or download, then the status parameters won't be used and can be set as nothing
            if (uploadDownload == null)
            {
                storeSizeForStatus = 0;
                transferStartTime = DateTime.MinValue;
            }
            // else if this communication is for a file upload or download, then set the status event parameters
            else
            {
                // check to make sure this is in fact an upload or download
                if (!(uploadDownload is upload)
                    && !(uploadDownload is download))
                {
                    throw new ArgumentException("uploadDownload must be either upload or download");
                }

                // set the status event parameters

                storeSizeForStatus = uploadDownload.ChangeToTransfer.Metadata.HashableProperties.Size ?? 0; // pull size from the change to transfer
                transferStartTime = DateTime.Now; // use the current local time as transfer start time
            }
            #endregion

            #region write request
            // if this communication is for a file upload or download, then 
            if (uploadDownload != null)
            {
                // get the request stream
                Stream httpRequestStream = null;

                // finish commenting here

                try
                {
                    if (uploadDownload is upload)
                    {
                        httpRequestStream = AsyncGetUploadRequestStreamOrDownloadResponse(uploadDownload.ShutdownToken, httpRequest, true) as Stream;

                        if (httpRequestStream == null)
                        {
                            status = CLHttpRestStatus.Cancelled;
                            return null;
                        }

                        byte[] uploadBuffer = new byte[FileConstants.BufferSize];
                        int bytesRead;
                        long totalBytesUploaded = 0;
                        while ((bytesRead = ((upload)uploadDownload).Stream.Read(uploadBuffer, 0, uploadBuffer.Length)) != 0)
                        {
                            httpRequestStream.Write(uploadBuffer, 0, bytesRead);
                            totalBytesUploaded += bytesRead;

                            if (uploadDownload.ShutdownToken != null)
                            {
                                Monitor.Enter(uploadDownload.ShutdownToken);
                                try
                                {
                                    if (uploadDownload.ShutdownToken.Token.IsCancellationRequested)
                                    {
                                        status = CLHttpRestStatus.Cancelled;
                                        return null;
                                    }
                                }
                                finally
                                {
                                    Monitor.Exit(uploadDownload.ShutdownToken);
                                }
                            }

                            uploadDownload.StatusCallback(new CLStatusFileTransferUpdateParameters(
                                    transferStartTime, // time of upload start
                                    storeSizeForStatus, // total size of file
                                    uploadDownload.RelativePathForStatus, // relative path of file
                                    totalBytesUploaded), // bytes uploaded so far
                                uploadDownload.ChangeToTransfer);
                        }

                        ((upload)uploadDownload).DisposeStream();
                    }
                    else
                    {
                        httpRequestStream = httpRequest.GetRequestStream();

                        // write the request for the download
                        httpRequestStream.Write(requestContentBytes, 0, requestContentBytes.Length);
                    }
                }
                finally
                {
                    if (httpRequestStream != null)
                    {
                        try
                        {
                            httpRequestStream.Dispose();
                        }
                        catch
                        {
                        }
                    }
                }
            }
            else if (requestContentBytes != null)
            {
                using (Stream httpRequestStream = httpRequest.GetRequestStream())
                {
                    httpRequestStream.Write(requestContentBytes, 0, requestContentBytes.Length);
                }
            }
            #endregion

            // define the web response outside the regions "get response" and "process response stream" so it can finally be closed (if it ever gets set); also for trace
            HttpWebResponse httpResponse = null;
            string responseBody = null;
            Stream responseStream = null;
            Stream serializationStream = null;

            try
            {
                #region get response
                if (uploadDownload != null
                    && uploadDownload is download)
                {
                    httpResponse = AsyncGetUploadRequestStreamOrDownloadResponse(uploadDownload.ShutdownToken, httpRequest, false) as HttpWebResponse;

                    if (httpRequest == null)
                    {
                        status = CLHttpRestStatus.Cancelled;
                        return null;
                    }
                }
                else
                {
                    try
                    {
                        httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                    }
                    catch (WebException ex)
                    {
                        if (ex.Response == null)
                        {
                            throw new NullReferenceException(String.Format("httpResponse GetResponse at URL {0}, MethodPath {1}",
                                        (serverUrl ?? "{missing serverUrl}"),
                                        (serverMethodPath ?? "{missing serverMethodPath}"))
                                        + " threw a WebException without a WebResponse");
                        }

                        httpResponse = (HttpWebResponse)ex.Response;
                    }
                }

                if (!validStatusCodes.Contains(httpResponse.StatusCode))
                {
                    if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                    {
                        status = CLHttpRestStatus.NotFound;
                    }
                    else if (httpResponse.StatusCode == HttpStatusCode.NoContent)
                    {
                        status = CLHttpRestStatus.NoContent;
                    }
                    else if (((HttpStatusCode)(((int)httpResponse.StatusCode) - (((int)httpResponse.StatusCode) % 100))) == HttpStatusCode.InternalServerError)
                    {
                        status = CLHttpRestStatus.ServerError;
                    }

                    // try/catch to set the response body from the content of the response, on catch silence the error
                    try
                    {
                        // grab the response stream
                        using (Stream downloadResponseStream = httpResponse.GetResponseStream())
                        {
                            // read the response as UTF8 text
                            using (StreamReader downloadResponseStreamReader = new StreamReader(downloadResponseStream, Encoding.UTF8))
                            {
                                // set the response text
                                responseBody = downloadResponseStreamReader.ReadToEnd();
                            }
                        }
                    }
                    catch
                    {
                    }

                    // throw the exception for an invalid response
                    throw new Exception(String.Format("Invalid HTTP response status code at URL {0}, MethodPath {1}", 
                                    (serverUrl ?? "{missing serverUrl"), 
                                    (serverMethodPath ?? "{missing serverMethodPath")) + 
                                    ": " + ((int)httpResponse.StatusCode).ToString() +
                                    (responseBody == null ? string.Empty
                                        : Environment.NewLine + "Response:" + Environment.NewLine +
                                            responseBody)); // either the default "incomplete" body or the body retrieved from the response content
                }
                #endregion

                #region process response stream
                Tout toReturn = null;
                if (uploadDownload != null)
                {
                    if (uploadDownload is upload)
                    {
                        // set body as successful value
                        responseBody = "---File upload complete---";

                        if (typeof(Tout) == typeof(string)
                            || typeof(Tout) == typeof(object))
                        {
                            toReturn = (Tout)((object)responseBody);
                        }
                    }
                    else
                    {
                        // set the response body to a value that will be displayed if the actual response fails to process
                        responseBody = "---Incomplete file download---";

                        // create a new unique id for the download
                        Guid newTempFile = Guid.NewGuid();

                        ((download)uploadDownload).BeforeDownloadCallback(newTempFile, ((download)uploadDownload).BeforeDownloadUserState);

                        // calculate location for downloading the file
                        string newTempFileString = ((download)uploadDownload).TempDownloadFolderPath + "\\" + ((Guid)newTempFile).ToString("N");

                        // get the stream of the download
                        using (Stream downloadResponseStream = httpResponse.GetResponseStream())
                        {
                            // create a stream by creating a non-shared writable file at the file path
                            using (FileStream tempFileStream = new FileStream(newTempFileString, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                // define a count for the total bytes downloaded
                                long totalBytesDownloaded = 0;
                                // create the buffer for transferring bytes from the download stream to the file stream
                                byte[] data = new byte[CLDefinitions.SyncConstantsResponseBufferSize];
                                // declare an int for the amount of bytes read in each buffer transfer
                                int read;
                                // loop till there are no more bytes to read, on the loop condition perform the buffer transfer from the download stream and store the read byte count
                                while ((read = downloadResponseStream.Read(data, 0, data.Length)) > 0)
                                {
                                    // write the current buffer to the file
                                    tempFileStream.Write(data, 0, read);
                                    // append the count of the read bytes on this buffer transfer to the total downloaded
                                    totalBytesDownloaded += read;

                                    if (uploadDownload.ShutdownToken != null)
                                    {
                                        // check for sync shutdown
                                        Monitor.Enter(uploadDownload.ShutdownToken);
                                        try
                                        {
                                            if (uploadDownload.ShutdownToken.Token.IsCancellationRequested)
                                            {
                                                status = CLHttpRestStatus.Cancelled;
                                                return null;
                                            }
                                        }
                                        finally
                                        {
                                            Monitor.Exit(uploadDownload.ShutdownToken);
                                        }
                                    }

                                    uploadDownload.StatusCallback(
                                        new CLStatusFileTransferUpdateParameters(
                                                transferStartTime, // start time for download
                                                storeSizeForStatus, // total file size
                                                uploadDownload.RelativePathForStatus, // relative path of file
                                                totalBytesDownloaded), // current count of completed download bytes
                                            uploadDownload.ChangeToTransfer);
                                }
                                // flush file stream to finish the file
                                tempFileStream.Flush();
                            }
                        }

                        // set the file attributes so when the file move triggers a change in the event source its metadata should match the current event;
                        // also, perform each attribute change with up to 4 retries since it seems to throw errors under normal conditions (if it still fails then it rethrows the exception);
                        // attributes to set: creation time, last modified time, and last access time

                        Helpers.RunActionWithRetries(() => System.IO.File.SetCreationTimeUtc(newTempFileString, uploadDownload.ChangeToTransfer.Metadata.HashableProperties.CreationTime), true);
                        Helpers.RunActionWithRetries(() => System.IO.File.SetLastAccessTimeUtc(newTempFileString, uploadDownload.ChangeToTransfer.Metadata.HashableProperties.LastTime), true);
                        Helpers.RunActionWithRetries(() => System.IO.File.SetLastWriteTimeUtc(newTempFileString, uploadDownload.ChangeToTransfer.Metadata.HashableProperties.LastTime), true);

                        // fire callback to perform the actual move of the temp file to the final destination
                        ((download)uploadDownload).AfterDownloadCallback(newTempFileString, // location of temp file
                            uploadDownload.ChangeToTransfer,
                            ref responseBody, // reference to response string (sets to "---Completed file download---" on success)
                            ((download)uploadDownload).AfterDownloadUserState, // timer for failure queue
                            newTempFile); // id for the downloaded file

                        if (responseBody == null)
                        {
                            responseBody = "---responseBody set to null---";
                        }

                        if (typeof(Tout) == typeof(string)
                            || typeof(Tout) == typeof(object))
                        {
                            toReturn = (Tout)((object)responseBody);
                        }
                    }
                }
                else
                {
                    DataContractJsonSerializer outSerializer;
                    if (SerializableResponseTypes.TryGetValue(typeof(Tout), out outSerializer))
                    {
                        responseStream = httpResponse.GetResponseStream();

                        // set the stream for processing the response by a copy of the communication stream (if trace enabled) or the communication stream itself (if trace is not enabled)
                        serializationStream = (((settings.TraceType & TraceType.Communication) == TraceType.Communication)
                            ? Helpers.CopyHttpWebResponseStreamAndClose(responseStream) // if trace is enabled, then copy the communications stream to a memory stream
                            : responseStream); // if trace is not enabled, use the communication stream

                        // if tracing communication, then trace communication
                        if ((settings.TraceType & TraceType.Communication) == TraceType.Communication)
                        {
                            // log communication for stream body
                            Trace.LogCommunication(settings.TraceLocation, // trace file location
                                settings.Udid, // device id
                                settings.SyncBoxId, // user id
                                CommunicationEntryDirection.Response, // communication direction is response
                                serverUrl + serverMethodPath, // input parameter method path
                                true, // trace is enabled
                                httpResponse.Headers, // response headers
                                serializationStream, // copied response stream
                                (int)httpResponse.StatusCode, // status code of the response
                                settings.TraceExcludeAuthorization); // whether to include authorization in the trace (such as the authentication key)
                        }

                        // deserialize the response content into the appropriate json contract object
                        toReturn = (Tout)outSerializer.ReadObject(serializationStream);
                    }
                    else if (typeof(Tout) == typeof(string)
                        || (typeof(Tout) == typeof(object)))
                    {
                        responseStream = httpResponse.GetResponseStream();

                        // create a reader for the response content
                        using (TextReader purgeResponseStreamReader = new StreamReader(responseStream, Encoding.UTF8))
                        {
                            // set the error string from the response
                            toReturn = (Tout)((object)purgeResponseStreamReader.ReadToEnd());
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Tout is not a serializable output type nor object/string");
                    }
                }

                status = CLHttpRestStatus.Success;
                return toReturn;
                #endregion
            }
            finally
            {
                // for communication logging, log communication
                if ((settings.TraceType & TraceType.Communication) == TraceType.Communication)
                {
                    if (serializationStream == null)
                    {
                        // log communication for string body
                        Trace.LogCommunication(settings.TraceLocation, // trace file location
                            settings.Udid, // device id
                            settings.SyncBoxId, // user id
                            CommunicationEntryDirection.Response, // communication direction is response
                            serverUrl + serverMethodPath, // input parameter method path
                            true, // trace is enabled
                            httpResponse.Headers, // response headers
                            responseBody, // response body (either an overridden string that says "complete" or "incomplete" or an error message from the actual response)
                            (int)httpResponse.StatusCode, // status code of the response
                            settings.TraceExcludeAuthorization); // whether to include authorization in the trace (such as the authentication key)
                    }
                    else if (serializationStream != null)
                    {
                        try
                        {
                            serializationStream.Dispose();
                        }
                        catch
                        {
                        }
                    }
                }

                if (responseStream != null)
                {
                    try
                    {
                        responseStream.Dispose();
                    }
                    catch
                    {
                    }
                }

                if (httpResponse != null)
                {
                    try
                    {
                        httpResponse.Close();
                    }
                    catch
                    {
                    }
                }
            }
        }

        private static object AsyncGetUploadRequestStreamOrDownloadResponse(CancellationTokenSource shutdownToken, HttpWebRequest httpRequest, bool upload)
        {
            object toReturn;

            // create new async holder used to make async http calls synchronous
            AsyncRequestHolder requestOrResponseHolder = new AsyncRequestHolder(shutdownToken);

            // declare result from async http call
            IAsyncResult requestOrResponseAsyncResult;

            // lock on async holder for modification
            lock (requestOrResponseHolder)
            {
                AsyncCallback requestOrResponseCallback = new AsyncCallback(MakeAsyncRequestSynchronous);
                if (upload)
                {
                    // begin upload request asynchronously, using callback which will take the async holder and make the request synchronous again, storing the result
                    requestOrResponseAsyncResult = httpRequest.BeginGetRequestStream(requestOrResponseCallback, requestOrResponseHolder);
                }
                else
                {
                    requestOrResponseAsyncResult = httpRequest.BeginGetResponse(requestOrResponseCallback, requestOrResponseHolder);
                }

                // wait on the request to become synchronous again
                Monitor.Wait(requestOrResponseHolder);
            }

            // if there was an error that occurred on the async http call, then rethrow the error
            if (requestOrResponseHolder.Error != null)
            {
                throw requestOrResponseHolder.Error;
            }

            // if the http call was cancelled, then return immediately with default
            if (requestOrResponseHolder.IsCanceled)
            {
                return null;
            }

            if (upload)
            {
                toReturn = httpRequest.EndGetRequestStream(requestOrResponseAsyncResult);
            }
            else
            {
                try
                {
                    toReturn = httpRequest.EndGetResponse(requestOrResponseAsyncResult);
                }
                catch (WebException ex)
                {
                    if (ex.Response == null)
                    {
                        throw new NullReferenceException("Download httpRequest EndGetResponse threw a WebException without a WebResponse", ex);
                    }

                    toReturn = ex.Response;
                }
            }
            return toReturn;
        }

        /// <summary>
        /// Async HTTP operation holder used to help make async calls synchronous
        /// </summary>
        private sealed class AsyncRequestHolder
        {
            /// <summary>
            /// cancelation token to check between async calls to cancel out of the operation
            /// </summary>
            public CancellationTokenSource FullShutdownToken
            {
                get
                {
                    return _fullShutdownToken;
                }
            }
            private readonly CancellationTokenSource _fullShutdownToken;

            /// <summary>
            /// Constructor for the async HTTP operation holder
            /// </summary>
            /// <param name="FullShutdownToken">Token to check for cancelation upon async calls</param>
            public AsyncRequestHolder(CancellationTokenSource FullShutdownToken)
            {
                // store the cancellation token
                this._fullShutdownToken = FullShutdownToken;
            }

            /// <summary>
            /// Whether the current async HTTP operation holder detected cancellation
            /// </summary>
            public bool IsCanceled
            {
                get
                {
                    return _isCanceled;
                }
            }
            // storage for cancellation
            private bool _isCanceled = false;

            /// <summary>
            /// Marks the current async HTTP operation holder as cancelled
            /// </summary>
            public void Cancel()
            {
                _isCanceled = true;
            }

            /// <summary>
            /// Any error that happened during current async HTTP operation
            /// </summary>
            public Exception Error
            {
                get
                {
                    return _error;
                }
            }
            // storage for any error that occurs
            private Exception _error = null;

            /// <summary>
            /// Marks the current async HTTP operation holder with any error that occurs
            /// </summary>
            /// <param name="toMark"></param>
            public void MarkException(Exception toMark)
            {
                // null coallesce the exception with a new exception that the exception was null
                _error = toMark ?? new NullReferenceException("toMark is null");
                // lock on this current async HTTP operation holder for pulsing waiters
                lock (this)
                {
                    Monitor.Pulse(this);
                }
            }
        }

        // Method to make async HTTP operations synchronous which can be ; requires passing an AsyncRequestHolder as the userstate
        private static void MakeAsyncRequestSynchronous(IAsyncResult makeSynchronous)
        {
            // try cast userstate as AsyncRequestHolder
            AsyncRequestHolder castHolder = makeSynchronous.AsyncState as AsyncRequestHolder;

            // ensure the cast userstate was successful
            if (castHolder == null)
            {
                throw new NullReferenceException("makeSynchronous AsyncState must be castable as AsyncRequestHolder");
            }

            // try/catch check for completion or cancellation to pulse the AsyncRequestHolder, on catch mark the exception in the AsyncRequestHolder (which will also pulse out)
            try
            {
                // if asynchronous task completed, then pulse the AsyncRequestHolder
                if (makeSynchronous.IsCompleted)
                {
                    lock (castHolder)
                    {
                        Monitor.Pulse(castHolder);
                    }
                }
                // else if asychronous task is not completed, then check for cancellation
                else if (castHolder.FullShutdownToken != null)
                {
                    // check for cancellation
                    Monitor.Enter(castHolder.FullShutdownToken);
                    try
                    {
                        // if cancelled, then mark the AsyncRequestHolder as cancelled and pulse out
                        if (castHolder.FullShutdownToken.Token.IsCancellationRequested)
                        {
                            castHolder.Cancel();

                            lock (castHolder)
                            {
                                Monitor.Pulse(castHolder);
                            }
                        }
                    }
                    finally
                    {
                        Monitor.Exit(castHolder.FullShutdownToken);
                    }
                }
            }
            catch (Exception ex)
            {
                // mark AsyncRequestHolder with error (which will also pulse out)
                castHolder.MarkException(ex);
            }
        }

        private enum requestMethod : byte
        {
            put,
            get,
            post
        }

        private abstract class uploadDownloadParams
        {
            public string RelativePathForStatus
            {
                get
                {
                    return _relativePathForStatus;
                }
            }
            private readonly string _relativePathForStatus;

            public SendUploadDownloadStatus StatusCallback
            {
                get
                {
                    return _statusCallback;
                }
            }
            private readonly SendUploadDownloadStatus _statusCallback;

            public FileChange ChangeToTransfer
            {
                get
                {
                    return _changeToTransfer;
                }
            }
            private readonly FileChange _changeToTransfer;

            public CancellationTokenSource ShutdownToken
            {
                get
                {
                    return _shutdownToken;
                }
            }
            private readonly CancellationTokenSource _shutdownToken;

            public uploadDownloadParams(SendUploadDownloadStatus StatusCallback, FileChange ChangeToTransfer, CancellationTokenSource ShutdownToken, string SyncRootFullPath)
            {
                if (ChangeToTransfer == null)
                {
                    throw new NullReferenceException("ChangeToTransfer cannot be null");
                }
                if (ChangeToTransfer.Metadata == null)
                {
                    throw new NullReferenceException("ChangeToTransfer Metadata cannot be null");
                }
                if (ChangeToTransfer.Metadata.HashableProperties.Size == null)
                {
                    throw new NullReferenceException("ChangeToTransfer Metadata HashableProperties Size cannot be null");
                }
                if (ChangeToTransfer.NewPath == null)
                {
                    throw new NullReferenceException("ChangeToTransfer NewPath cannot be null");
                }

                this._statusCallback = StatusCallback;
                this._changeToTransfer = ChangeToTransfer;
                this._relativePathForStatus = this.ChangeToTransfer.NewPath.GetRelativePath((SyncRootFullPath ?? string.Empty), false);
                this._shutdownToken = ShutdownToken;
            }
        }

        private sealed class download : uploadDownloadParams
        {
            public BeforeDownloadToTempFile BeforeDownloadCallback
            {
                get
                {
                    return _beforeDownloadCallback;
                }
            }
            private readonly BeforeDownloadToTempFile _beforeDownloadCallback;

            public object BeforeDownloadUserState
            {
                get
                {
                    return _beforeDownloadUserState;
                }
            }
            private readonly object _beforeDownloadUserState;

            public AfterDownloadToTempFile AfterDownloadCallback
            {
                get
                {
                    return _afterDownloadCallback;
                }
            }
            private readonly AfterDownloadToTempFile _afterDownloadCallback;

            public object AfterDownloadUserState
            {
                get
                {
                    return _afterDownloadUserState;
                }
            }
            private readonly object _afterDownloadUserState;

            public string TempDownloadFolderPath
            {
                get
                {
                    return _tempDownloadFolderPath;
                }
            }
            private readonly string _tempDownloadFolderPath;

            public download(BeforeDownloadToTempFile BeforeDownloadCallback, object BeforeDownloadUserState, AfterDownloadToTempFile AfterDownloadCallback, object AfterDownloadUserState, string TempDownloadFolderPath, SendUploadDownloadStatus StatusCallback, FileChange ChangeToTransfer, CancellationTokenSource ShutdownToken, string SyncRootFullPath)
                : base(StatusCallback, ChangeToTransfer, ShutdownToken, SyncRootFullPath)
            {
                this._beforeDownloadCallback = BeforeDownloadCallback;
                this._beforeDownloadUserState = BeforeDownloadUserState;
                this._afterDownloadCallback = AfterDownloadCallback;
                this._afterDownloadUserState = AfterDownloadUserState;
                this._tempDownloadFolderPath = TempDownloadFolderPath;


                if (base.ChangeToTransfer.Direction != SyncDirection.From)
                {
                    throw new ArgumentException("Invalid ChangeToTransfer Direction for a download: " + base.ChangeToTransfer.Direction.ToString());
                }
            }
        }

        private sealed class upload : uploadDownloadParams
        {
            public Stream Stream
            {
                get
                {
                    return (_streamDisposed
                        ? null
                        : _stream);
                }
            }
            private readonly Stream _stream;

            public void DisposeStream()
            {
                if (!_streamDisposed)
                {
                    try
                    {
                        _stream.Dispose();
                    }
                    catch
                    {
                    }
                    _streamDisposed = true;
                }
            }
            private bool _streamDisposed = false;

            public string Hash
            {
                get
                {
                    return _hash;
                }
            }
            private readonly string _hash;

            public upload(Stream Stream, SendUploadDownloadStatus StatusCallback, FileChange ChangeToTransfer, CancellationTokenSource ShutdownToken, string SyncRootFullPath)
                : base(StatusCallback, ChangeToTransfer, ShutdownToken, SyncRootFullPath)
            {
                if (Stream == null)
                {
                    throw new Exception("Stream cannot be null");
                }
                if (base.ChangeToTransfer.Metadata.StorageKey == null)
                {
                    throw new Exception("ChangeToTransfer Metadata StorageKey cannot be null");
                }
                if (base.ChangeToTransfer.Direction != SyncDirection.To)
                {
                    throw new ArgumentException("Invalid ChangeToTransfer Direction for an upload: " + base.ChangeToTransfer.Direction.ToString());
                }

                // hash is used in http header for MD5 validation of content stream
                CLError retrieveHashError = this.ChangeToTransfer.GetMD5LowercaseString(out this._hash);
                if (retrieveHashError != null)
                {
                    throw new AggregateException("Unable to retrieve MD5 from ChangeToTransfer", retrieveHashError.GrabExceptions());
                }
                if (this._hash == null)
                {
                    throw new NullReferenceException("ChangeToTransfer must have an MD5 hash");
                }

                this._stream = Stream;
            }
        }
        #endregion

        #region Support
        
        /// <summary>
        /// Generate the signed token for the platform auth Authorization header.
        /// </summary>
        /// <param name="settings">The settings to use for this generation.</param>
        /// <param name="httpMethod">The HTTP method.  e.g.: "POST".</param>
        /// <param name="pathAndQueryStringAndFragment">The HTTP path, query string and fragment.  The path is required.</param>
        /// <param name="serverUrl">The server URL.</param>
        /// <returns></returns>
        private string GenerateAuthorizationHeaderToken(ISyncSettingsAdvanced settings, string httpMethod, string pathAndQueryStringAndFragment, string serverUrl)
        {
            string toReturn = String.Empty;
            try
            {
                string methodPath = String.Empty;
                string queryString = String.Empty;

                // Determine the methodPath and the queryString
                char[] delimiterChars = { '?' };
                string[] parts = pathAndQueryStringAndFragment.Split(delimiterChars);
                if (parts.Length > 1)
                {
                    methodPath = parts[0].Trim();
                    queryString = parts[parts.Length - 1].Trim();
                }
                else
                {
                    methodPath = pathAndQueryStringAndFragment;
                }

                // Build the string that we will hash.
                string stringToHash = String.Format("{0}{1}{2}{3}{4}{5}{6}{7}{8}",
                        CLDefinitions.AuthorizationFormatType,
                        "\n",
                        httpMethod.ToUpper(),
                        "\n",
                        serverUrl.Replace('\\', '/'),
                        "\n",
                        methodPath,
                        "\n",
                        queryString);

                // Hash the string
                System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
                byte[] secretByte = Encoding.UTF8.GetBytes(settings.ApplicationSecret);
                HMACSHA256 hmac = new HMACSHA256(secretByte);
                byte[] stringToHashBytes = encoding.GetBytes(stringToHash);
                byte[] hashMessage = hmac.ComputeHash(stringToHashBytes);
                toReturn = ByteToString(hashMessage);
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(CLTrace.Instance.TraceLocation, CLTrace.Instance.LogErrors);
                CLTrace.Instance.writeToLog(1, "CLGen: Gen: ERROR. Exception.  Msg: <{0}>.", ex.Message);
            }

            return toReturn;
        }

        /// <summary>
        /// Convert a byte array to a string.
        /// </summary>
        /// <param name="buff"></param>
        /// <returns></returns>
        private string ByteToString(byte[] buff)
        {
            string sbinary = "";

            for (int i = 0; i < buff.Length; i++)
            {
                sbinary += buff[i].ToString("X2"); // hex format
            }
            return (sbinary);
        }

        #endregion
    }

    //TODO: Should this be internal?
    public delegate void SendUploadDownloadStatus(CLStatusFileTransferUpdateParameters status, FileChange eventSource);
    public delegate void BeforeDownloadToTempFile(Guid tempId, object UserState);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="tempFileFullPath"></param>
    /// <param name="downloadChange"></param>
    /// <param name="responseBody">Reference to string used to trace communication, should be set to "---Completed file download---" upon successfully moving the file to its destination</param>
    /// <param name="UserState"></param>
    /// <param name="tempId"></param>
    public delegate void AfterDownloadToTempFile(string tempFileFullPath, FileChange downloadChange, ref string responseBody, object UserState, Guid tempId);

    public enum CLHttpRestStatus : byte
    {
        Success,
        NotFound,
        ServerError,
        BadRequest,
        Cancelled,
        NoContent
    }
}