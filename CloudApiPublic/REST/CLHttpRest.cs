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
using System.Linq.Expressions;

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

        private static readonly Dictionary<Type, DataContractJsonSerializer> SerializableRequestTypes = new Dictionary<Type, DataContractJsonSerializer>()
        {
            { typeof(JsonContracts.Download), JsonContractHelpers.DownloadSerializer },
            { typeof(JsonContracts.PurgePending), JsonContractHelpers.PurgePendingSerializer },
            { typeof(JsonContracts.Push), JsonContractHelpers.PushSerializer },
            { typeof(JsonContracts.To), JsonContractHelpers.ToSerializer },
            
            #region one-offs
            { typeof(JsonContracts.FolderAdd), JsonContractHelpers.FolderAddSerializer },

            { typeof(JsonContracts.FileAdd), JsonContractHelpers.FileAddSerializer },
            { typeof(JsonContracts.FileModify), JsonContractHelpers.FileModifySerializer },

            { typeof(JsonContracts.FileOrFolderDelete), JsonContractHelpers.FileOrFolderDeleteSerializer },
            { typeof(JsonContracts.FileOrFolderMove), JsonContractHelpers.FileOrFolderMoveSerializer },
            { typeof(JsonContracts.FileOrFolderUndelete), JsonContractHelpers.FileOrFolderUndeleteSerializer },
            #endregion

            { typeof(JsonContracts.FileCopy), JsonContractHelpers.FileCopySerializer }
        };

        // dictionary to find which Json contract serializer to use given a provided input type
        private static readonly Dictionary<Type, DataContractJsonSerializer> SerializableResponseTypes = new Dictionary<Type, DataContractJsonSerializer>()
        {
            { typeof(JsonContracts.Metadata), JsonContractHelpers.GetMetadataResponseSerializer },
            { typeof(JsonContracts.NotificationResponse), JsonContractHelpers.NotificationResponseSerializer },
            { typeof(JsonContracts.PendingResponse), JsonContractHelpers.PendingResponseSerializer },
            { typeof(JsonContracts.PushResponse), JsonContractHelpers.PushResponseSerializer },
            { typeof(JsonContracts.To), JsonContractHelpers.ToSerializer },
            { typeof(JsonContracts.Event), JsonContractHelpers.EventSerializer },
            { typeof(JsonContracts.FileVersion[]), JsonContractHelpers.FileVersionsSerializer },
            { typeof(JsonContracts.UsedBytes), JsonContractHelpers.UsedBytesSerializer }
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
        /// Downloads a file from a provided file download change
        /// </summary>
        /// <param name="changeToDownload">File download change, requires Metadata.</param>
        /// <param name="moveFileUponCompletion"></param>
        /// <param name="moveFileUponCompletionState"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <param name="status"></param>
        /// <param name="beforeDownload"></param>
        /// <param name="beforeDownloadState"></param>
        /// <param name="shutdownToken"></param>
        /// <param name="customDownloadFolderFullPath"></param>
        /// <returns></returns>
        public CLError DownloadFile(FileChange changeToDownload,
            AfterDownloadToTempFile moveFileUponCompletion,
            object moveFileUponCompletionState,
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            BeforeDownloadToTempFile beforeDownload = null,
            object beforeDownloadState = null,
            CancellationTokenSource shutdownToken = null,
            string customDownloadFolderFullPath = null)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the file download, on catch return the error
            try
            {
                // check input parameters (other checks are done on constructing the private download class upon ProcessHttp)

                if (timeoutMilliseconds <= 0)
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                string currentDownloadFolder;

                if (customDownloadFolderFullPath != null)
                {
                    currentDownloadFolder = customDownloadFolderFullPath;
                }
                else if (settings.TempDownloadFolderFullPath != null)
                {
                    currentDownloadFolder = settings.TempDownloadFolderFullPath;
                }
                else
                {
                    currentDownloadFolder = Helpers.GetTempFileDownloadPath(settings);
                }

                CLError badTempFolderError = Helpers.CheckForBadPath(currentDownloadFolder);

                if (badTempFolderError != null)
                {
                    throw new AggregateException("The customDownloadFolderFullPath is bad", badTempFolderError.GrabExceptions());
                }

                if (currentDownloadFolder.Length > 222)
                {
                    throw new ArgumentException("Folder path for temp download files is too long by " + (currentDownloadFolder.Length - 222).ToString());
                }

                // prepare the downloadParams before the ProcessHttp because it does additional parameter checks first
                downloadParams currentDownload = new downloadParams( // this is a special communication method and requires passing download parameters
                    moveFileUponCompletion, // callback which should move the file to final location
                    moveFileUponCompletionState, // userstate for the move file callback
                    customDownloadFolderFullPath ?? // first try to use a provided custom folder full path
                        Helpers.GetTempFileDownloadPath(settings),
                    HandleUploadDownloadStatus, // private event handler to relay status change events
                    changeToDownload, // the FileChange describing the download
                    shutdownToken, // a provided, possibly null CancellationTokenSource which can be cancelled to stop in the middle of communication
                    settings.SyncRoot, // pass in the full path to the sync root folder which is used to calculate a relative path for firing the status change event
                    beforeDownload,
                    beforeDownloadState);

                // run the actual communication
                ProcessHttp(
                    new Download() // JSON contract to serialize
                    {
                        StorageKey = changeToDownload.Metadata.StorageKey // storage key parameter
                    },
                    CLDefinitions.CLUploadDownloadServerURL, // server for download
                    CLDefinitions.MethodPathDownload, // download method path
                    requestMethod.post, // download is a post
                    timeoutMilliseconds, // time before communication timeout (does not restrict time
                    currentDownload, // download-specific parameters holder constructed directly above
                    okAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status); // reference to update the output success/failure status for the communication
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Uploads a file from a provided stream and file upload change
        /// </summary>
        /// <param name="uploadStream">Stream to upload, if it is a FileStream then make sure the file is locked to prevent simultaneous writes</param>
        /// <param name="changeToUpload">File upload change, requires Metadata.HashableProperties.Size, NewPath, Metadata.StorageKey, and MD5 hash to be set</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception, does not restrict time for the actual file upload</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="shutdownToken">(optional) Token used to request cancellation of the upload.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
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

                if (timeoutMilliseconds <= 0)
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // run the HTTP communication
                ProcessHttp(null, // the stream inside the upload parameter object is the request content, so no JSON contract object
                    CLDefinitions.CLUploadDownloadServerURL,  // Server URL
                    CLDefinitions.MethodPathUpload, // path to upload
                    requestMethod.put, // upload is a put
                    timeoutMilliseconds, // time before communication timeout (does not restrict time for the actual file upload)
                    new uploadParams( // this is a special communication method and requires passing upload parameters
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
        /// Queries the server at a given file or folder path (must be specified) for existing metadata at that path; outputs CLHttpRestStatus.NoContent for status if not found on server
        /// </summary>
        /// <param name="fullPath">Full path to where file or folder would exist locally on disk</param>
        /// <param name="isFolder">Whether the query is for a folder (as opposed to a file/link)</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
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
                CLError pathError = Helpers.CheckForBadPath(fullPath);
                if (pathError != null)
                {
                    throw new AggregateException("fullPath is not in the proper format", pathError.GrabExceptions());
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }
                if (settings.SyncBoxId == null)
                {
                    throw new NullReferenceException("settings SyncBoxId cannot be null");
                }
                if (string.IsNullOrEmpty(settings.SyncRoot))
                {
                    throw new NullReferenceException("settings SyncRoot cannot be null");
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

                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncBoxId, ((long)settings.SyncBoxId).ToString())
                    });

                // run the HTTP communication and store the response object to the output parameter
                response = ProcessHttp<JsonContracts.Metadata>(null, // HTTP Get method does not have content
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
        /// Queries the server for a given sync box and device to get all files which are still pending upload
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetAllPending(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.PendingResponse response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the pending query, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }
                if (string.IsNullOrEmpty(settings.DeviceId))
                {
                    throw new NullReferenceException("settings DeviceId cannot be null");
                }
                if (settings.SyncBoxId == null)
                {
                    throw new NullReferenceException("settings SyncBoxId cannot be null");
                }

                // build the location of the pending retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathGetPending + // get pending
                    Helpers.QueryStringBuilder(new[] // grab parameters by query string (since this method is an HTTP GET)
                    {
                        // query string parameter for the id of the device, escaped as needed for the URI
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringDeviceId, Uri.EscapeDataString(settings.DeviceId)),
                        
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncBoxId, ((long)settings.SyncBoxId).ToString())
                    });

                // run the HTTP communication and store the response object to the output parameter
                response = ProcessHttp<JsonContracts.PendingResponse>(null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL,   // base domain is the MDS server
                    serverMethodPath, // path to get pending
                    requestMethod.get, // get pending is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    okAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status); // reference to update the output success/failure status for the communication
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.PendingResponse>();
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Posts a single FileChange to the server to update the sync box in the cloud.
        /// May still require uploading a file with a returned storage key if the Header.Status property in response is "upload" or "uploading".
        /// Check Header.Status property in response for errors or conflict.
        /// </summary>
        /// <param name="toCommunicate">Single FileChange to send</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError PostFileChange(FileChange toCommunicate, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.Event response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the file change post, on catch return the error
            try
            {
                // check input parameters

                if (toCommunicate == null)
                {
                    throw new NullReferenceException("toCommunicate cannot be null");
                }
                if (toCommunicate.Direction == SyncDirection.From)
                {
                    throw new ArgumentException("toCommunicate Direction is not To the server");
                }
                if (toCommunicate.Metadata == null)
                {
                    throw new NullReferenceException("toCommunicate Metadata cannot be null");
                }
                if (toCommunicate.Type == FileChangeType.Modified
                    && toCommunicate.Metadata.HashableProperties.IsFolder)
                {
                    throw new ArgumentException("toCommunicate cannot be both a folder and of type Modified");
                }
                if (settings.DeviceId == null)
                {
                    throw new NullReferenceException("settings DeviceId cannot be null");
                }
                if (settings.SyncRoot == null)
                {
                    throw new NullReferenceException("settings SyncRoot cannot be null");
                }
                if (settings.SyncBoxId == null)
                {
                    throw new NullReferenceException("settings SyncBoxId cannot be null");
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // build the location of the one-off method on the server dynamically
                string serverMethodPath;
                object requestContent;

                // set server method path and the request content dynamically based on whether change is a file or folder and based on the type of change
                switch (toCommunicate.Type)
                {
                    // file or folder created
                    case FileChangeType.Created:

                        // check additional parameters for file or folder creation

                        if (toCommunicate.NewPath == null)
                        {
                            throw new NullReferenceException("toCommunicate NewPath cannot be null");
                        }

                        // if change is a folder, set path and create request content for folder creation
                        if (toCommunicate.Metadata.HashableProperties.IsFolder)
                        {
                            serverMethodPath = CLDefinitions.MethodPathOneOffFolderCreate;

                            requestContent = new JsonContracts.FolderAdd()
                            {
                                CreatedDate = toCommunicate.Metadata.HashableProperties.CreationTime,
                                DeviceId = settings.DeviceId,
                                RelativePath = toCommunicate.NewPath.GetRelativePath(settings.SyncRoot, true) + "/",
                                SyncBoxId = settings.SyncBoxId
                            };
                        }
                        // else if change is a file, set path and create request content for file creation
                        else
                        {
                            string addHashString;
                            CLError addHashStringError = toCommunicate.GetMD5LowercaseString(out addHashString);
                            if (addHashStringError != null)
                            {
                                throw new AggregateException("Error retrieving toCommunicate MD5 lowercase string", addHashStringError.GrabExceptions());
                            }

                            // check additional parameters for file creation

                            if (string.IsNullOrEmpty(addHashString))
                            {
                                throw new NullReferenceException("MD5 lowercase string retrieved from toCommunicate cannot be null, set via toCommunicate.SetMD5");
                            }
                            if (toCommunicate.Metadata.HashableProperties.Size == null)
                            {
                                throw new NullReferenceException("toCommunicate Metadata HashableProperties Size cannot be null");
                            }

                            serverMethodPath = CLDefinitions.MethodPathOneOffFileCreate;

                            requestContent = new JsonContracts.FileAdd()
                            {
                                CreatedDate = toCommunicate.Metadata.HashableProperties.CreationTime,
                                DeviceId = settings.DeviceId,
                                Hash = addHashString,
                                MimeType = toCommunicate.Metadata.MimeType,
                                ModifiedDate = toCommunicate.Metadata.HashableProperties.LastTime,
                                RelativePath = toCommunicate.NewPath.GetRelativePath(settings.SyncRoot, true),
                                Size = toCommunicate.Metadata.HashableProperties.Size,
                                SyncBoxId = settings.SyncBoxId
                            };
                        }
                        break;

                    case FileChangeType.Deleted:

                        // check additional parameters for file or folder deletion

                        if (toCommunicate.NewPath == null
                            && string.IsNullOrEmpty(toCommunicate.Metadata.ServerId))
                        {
                            throw new NullReferenceException("Either toCommunicate NewPath must not be null or toCommunicate Metadata ServerId must not be null or both must not be null");
                        }

                        // file deletion and folder deletion share a json contract object for deletion
                        requestContent = new JsonContracts.FileOrFolderDelete()
                        {
                            DeviceId = settings.DeviceId,
                            RelativePath = (toCommunicate.NewPath == null
                                ? null
                                : toCommunicate.NewPath.GetRelativePath(settings.SyncRoot, true) +
                                    (toCommunicate.Metadata.HashableProperties.IsFolder ? "/" : string.Empty)),
                            ServerId = toCommunicate.Metadata.ServerId,
                            SyncBoxId = settings.SyncBoxId
                        };

                        // server method path switched from whether change is a folder or not
                        serverMethodPath = (toCommunicate.Metadata.HashableProperties.IsFolder
                            ? CLDefinitions.MethodPathOneOffFolderDelete
                            : CLDefinitions.MethodPathOneOffFileDelete);
                        break;

                    case FileChangeType.Modified:

                        // grab MD5 hash string and rethrow any error that occurs

                        string modifyHashString;
                        CLError modifyHashStringError = toCommunicate.GetMD5LowercaseString(out modifyHashString);
                        if (modifyHashStringError != null)
                        {
                            throw new AggregateException("Error retrieving toCommunicate MD5 lowercase string", modifyHashStringError.GrabExceptions());
                        }

                        // check additional parameters for file modification

                        if (string.IsNullOrEmpty(modifyHashString))
                        {
                            throw new NullReferenceException("MD5 lowercase string retrieved from toCommunicate cannot be null, set via toCommunicate.SetMD5");
                        }
                        if (toCommunicate.Metadata.HashableProperties.Size == null)
                        {
                            throw new NullReferenceException("toCommunicate Metadata HashableProperties Size cannot be null");
                        }
                        if (toCommunicate.NewPath == null
                            && string.IsNullOrEmpty(toCommunicate.Metadata.ServerId))
                        {
                            throw new NullReferenceException("Either toCommunicate NewPath must not be null or toCommunicate Metadata ServerId must not be null or both must not be null");
                        }
                        if (string.IsNullOrEmpty(toCommunicate.Metadata.Revision))
                        {
                            throw new NullReferenceException("toCommunicate Metadata Revision cannot be null");
                        }

                        // there is no folder modify, so json contract object and server method path for modify are only for files

                        requestContent = new JsonContracts.FileModify()
                        {
                            CreatedDate = toCommunicate.Metadata.HashableProperties.CreationTime,
                            DeviceId = settings.DeviceId,
                            Hash = modifyHashString,
                            MimeType = toCommunicate.Metadata.MimeType,
                            ModifiedDate = toCommunicate.Metadata.HashableProperties.LastTime,
                            RelativePath = (toCommunicate.NewPath == null
                                ? null
                                : toCommunicate.NewPath.GetRelativePath(settings.SyncRoot, true)),
                            Revision = toCommunicate.Metadata.Revision,
                            ServerId = toCommunicate.Metadata.ServerId,
                            Size = toCommunicate.Metadata.HashableProperties.Size,
                            SyncBoxId = settings.SyncBoxId
                        };

                        serverMethodPath = CLDefinitions.MethodPathOneOffFileModify;
                        break;

                    case FileChangeType.Renamed:

                        // check additional parameters for file or folder move (rename)

                        if (toCommunicate.NewPath == null
                            && string.IsNullOrEmpty(toCommunicate.Metadata.ServerId))
                        {
                            throw new NullReferenceException("Either toCommunicate NewPath must not be null or toCommunicate Metadata ServerId must not be null or both must not be null");
                        }
                        if (toCommunicate.OldPath == null)
                        {
                            throw new NullReferenceException("toCommunicate OldPath cannot be null");
                        }

                        // file move (rename) and folder move (rename) share a json contract object for move (rename)
                        requestContent = new JsonContracts.FileOrFolderMove()
                        {
                            DeviceId = settings.DeviceId,
                            RelativeFromPath = toCommunicate.OldPath.GetRelativePath(settings.SyncRoot, true) +
                                (toCommunicate.Metadata.HashableProperties.IsFolder ? "/" : string.Empty),
                            RelativeToPath = (toCommunicate.NewPath == null
                                ? null
                                : toCommunicate.NewPath.GetRelativePath(settings.SyncRoot, true)
                                    + (toCommunicate.Metadata.HashableProperties.IsFolder ? "/" : string.Empty)),
                            ServerId = toCommunicate.Metadata.ServerId,
                            SyncBoxId = settings.SyncBoxId
                        };

                        // server method path switched on whether change is a folder or not
                        serverMethodPath = (toCommunicate.Metadata.HashableProperties.IsFolder
                            ? CLDefinitions.MethodPathOneOffFolderMove
                            : CLDefinitions.MethodPathOneOffFileMove);
                        break;

                    default:
                        throw new ArgumentException("toCommunicate Type is an unknown FileChangeType: " + toCommunicate.Type.ToString());
                }

                // run the HTTP communication and store the response object to the output parameter
                response = ProcessHttp<JsonContracts.Event>(requestContent, // dynamic type of request content based on method path
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    requestMethod.post, // one-off methods are all posts
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    okAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status); // reference to update the output success/failure status for the communication
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.Event>();
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Queries the server for a given sync box and device to get all files which are still pending upload
        /// </summary>
        /// <param name="deletionChange">Deletion change which needs to be undone</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError UndoDeletionFileChange(FileChange deletionChange, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.Event response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the undeletion, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }
                if (deletionChange == null)
                {
                    throw new NullReferenceException("deletionChange cannot be null");
                }
                if (deletionChange.Direction == SyncDirection.From)
                {
                    throw new ArgumentException("deletionChange Direction is not To the server");
                }
                if (deletionChange.Metadata == null)
                {
                    throw new NullReferenceException("deletionChange Metadata cannot be null");
                }
                if (deletionChange.Type != FileChangeType.Deleted)
                {
                    throw new ArgumentException("deletionChange is not of Type Deletion");
                }
                if (settings.SyncRoot == null)
                {
                    throw new NullReferenceException("settings SyncRoot cannot be null");
                }
                if (string.IsNullOrEmpty(deletionChange.Metadata.ServerId))
                {
                    throw new NullReferenceException("deletionChange Metadata ServerId must not be null");
                }
                if (string.IsNullOrEmpty(settings.DeviceId))
                {
                    throw new NullReferenceException("settings DeviceId cannot be null");
                }
                if (settings.SyncBoxId == null)
                {
                    throw new NullReferenceException("settings SyncBoxId cannot be null");
                }

                // run the HTTP communication and store the response object to the output parameter
                response = ProcessHttp<JsonContracts.Event>(new JsonContracts.FileOrFolderUndelete() // files and folders share a request content object for undelete
                    {
                        DeviceId = settings.DeviceId, // device id
                        ServerId = deletionChange.Metadata.ServerId, // unique id on server
                        SyncBoxId = settings.SyncBoxId // id of sync box
                    },
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    (deletionChange.Metadata.HashableProperties.IsFolder // folder/file switch
                        ? CLDefinitions.MethodPathFolderUndelete // path for folder undelete
                        : CLDefinitions.MethodPathFileUndelete), // path for file undelete
                    requestMethod.post, // undelete file or folder is a post
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    okAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status); // reference to update the output success/failure status for the communication
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.Event>();
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Queries the server for all versions of a given file
        /// </summary>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="includeDeletedVersions">(optional) whether to include file versions which are deleted</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFileVersions(string fileServerId, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.FileVersion[] response, bool includeDeletedVersions = false)
        {
            return GetFileVersions(fileServerId, timeoutMilliseconds, null, out status, out response, includeDeletedVersions);
        }

        /// <summary>
        /// Queries the server for all versions of a given file
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Full path to the file where it would be placed locally within the sync root</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="includeDeletedVersions">(optional) whether to include file versions which are deleted</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFileVersions(int timeoutMilliseconds, FilePath pathToFile, out CLHttpRestStatus status, out JsonContracts.FileVersion[] response, bool includeDeletedVersions = false)
        {
            return GetFileVersions(null, timeoutMilliseconds, pathToFile, out status, out response, includeDeletedVersions);
        }

        /// <summary>
        /// Queries the server for all versions of a given file
        /// </summary>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Full path to the file where it would be placed locally within the sync root</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="includeDeletedVersions">(optional) whether to include file versions which are deleted</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFileVersions(string fileServerId, int timeoutMilliseconds, FilePath pathToFile, out CLHttpRestStatus status, out JsonContracts.FileVersion[] response, bool includeDeletedVersions = false)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the undeletion, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }
                if (pathToFile == null
                    && string.IsNullOrEmpty(fileServerId))
                {
                    throw new NullReferenceException("Either pathToFile must not be null or fileServerId must not be null or both must not be null");
                }
                if (settings.SyncRoot == null)
                {
                    throw new NullReferenceException("settings SyncRoot cannot be null");
                }
                if (string.IsNullOrEmpty(settings.DeviceId))
                {
                    throw new NullReferenceException("settings DeviceId cannot be null");
                }
                if (settings.SyncBoxId == null)
                {
                    throw new NullReferenceException("settings SyncBoxId cannot be null");
                }
                
                // build the location of the metadata retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathFileGetVersions + // get file versions
                    Helpers.QueryStringBuilder(new[] // both methods grab their parameters by query string (since this method is an HTTP GET)
                    {
                        // query string parameter for the device id
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringDeviceId, Uri.EscapeDataString(settings.DeviceId)),

                        // query string parameter for the server id for the file to check, only filled in if it's not null
                        (string.IsNullOrEmpty(fileServerId)
                            ? new KeyValuePair<string, string>()
                            : new KeyValuePair<string, string>(CLDefinitions.CLMetadataServerId, Uri.EscapeDataString(fileServerId))),

                        // query string parameter for the path to the file to check, only filled in if it's not null
                        (pathToFile == null
                            ? new KeyValuePair<string, string>()
                            : new KeyValuePair<string, string>(CLDefinitions.CLMetadataCloudPath, Uri.EscapeDataString(pathToFile.GetRelativePath(settings.SyncRoot, true)))),

                        // query string parameter for whether to include delete versions in the check, but only set if it's not default (if it's false)
                        (includeDeletedVersions
                            ? new KeyValuePair<string, string>()
                            : new KeyValuePair<string, string>(CLDefinitions.QueryStringIncludeDeleted, "false")),

                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncBoxId, ((long)settings.SyncBoxId).ToString())
                    });

                // run the HTTP communication and store the response object to the output parameter
                response = ProcessHttp<JsonContracts.FileVersion[]>(null, // get file versions has no request content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // use a dynamic method path because it needs query string parameters
                    requestMethod.get, // get file versions is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    okAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status); // reference to update the output success/failure status for the communication
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.FileVersion[]>();
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Grabs the bytes used by the sync box and the bytes which are pending for upload
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetUsedBytes(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.UsedBytes response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the undeletion, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }
                if (string.IsNullOrEmpty(settings.DeviceId))
                {
                    throw new NullReferenceException("settings DeviceId cannot be null");
                }
                if (settings.SyncBoxId == null)
                {
                    throw new NullReferenceException("settings SyncBoxId cannot be null");
                }

                // run the HTTP communication and store the response object to the output parameter
                response = ProcessHttp<JsonContracts.UsedBytes>(null, // getting used bytes requires no request content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    CLDefinitions.MethodPathGetUsedBytes + // path to get used bytes
                        Helpers.QueryStringBuilder(new[]
                        {
                            new KeyValuePair<string, string>(CLDefinitions.QueryStringDeviceId, Uri.EscapeDataString(settings.DeviceId)), // device id, escaped since it's a user-input
                            new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncBoxId, ((long)settings.SyncBoxId).ToString()) // sync box id, not escaped since it's from an integer
                        }),
                    requestMethod.get, // getting used bytes is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    okAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status); // reference to update the output success/failure status for the communication
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.UsedBytes>();
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Copies a file on the server to another location
        /// </summary>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="copyTargetPath">Location where file shoule be copied to</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError CopyFile(string fileServerId, int timeoutMilliseconds, FilePath copyTargetPath, out CLHttpRestStatus status, out JsonContracts.Event response)
        {
            return CopyFile(fileServerId, timeoutMilliseconds, null, copyTargetPath, out status, out response);
        }

        /// <summary>
        /// Copies a file on the server to another location
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Location of existing file to copy from</param>
        /// <param name="copyTargetPath">Location where file shoule be copied to</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError CopyFile(int timeoutMilliseconds, FilePath pathToFile, FilePath copyTargetPath, out CLHttpRestStatus status, out JsonContracts.Event response)
        {
            return CopyFile(null, timeoutMilliseconds, pathToFile, copyTargetPath, out status, out response);
        }

        /// <summary>
        /// Copies a file on the server to another location
        /// </summary>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Location of existing file to copy from</param>
        /// <param name="copyTargetPath">Location where file shoule be copied to</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError CopyFile(string fileServerId, int timeoutMilliseconds, FilePath pathToFile, FilePath copyTargetPath, out CLHttpRestStatus status, out JsonContracts.Event response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the undeletion, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }
                if (settings.SyncRoot == null)
                {
                    throw new NullReferenceException("settings SyncRoot cannot be null");
                }
                if (copyTargetPath == null)
                {
                    throw new NullReferenceException("copyTargetPath cannot be null");
                }
                if (pathToFile == null
                    && string.IsNullOrEmpty(fileServerId))
                {
                    throw new NullReferenceException("Either pathToFile must not be null or fileServerId must not be null or both must not be null");
                }
                if (string.IsNullOrEmpty(settings.DeviceId))
                {
                    throw new NullReferenceException("settings DeviceId cannot be null");
                }
                if (settings.SyncBoxId == null)
                {
                    throw new NullReferenceException("settings SyncBoxId cannot be null");
                }

                // run the HTTP communication and store the response object to the output parameter
                response = ProcessHttp<JsonContracts.Event>(new JsonContracts.FileCopy() // object for file copy
                    {
                        DeviceId = settings.DeviceId, // device id
                        ServerId = fileServerId, // unique id on server
                        RelativePath = (pathToFile == null
                            ? null
                            : pathToFile.GetRelativePath(settings.SyncRoot, true)), // path of existing file to copy
                        RelativeToPath = copyTargetPath.GetRelativePath(settings.SyncRoot, true), // location to copy file to
                        SyncBoxId = settings.SyncBoxId // id of sync box
                    },
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    CLDefinitions.MethodPathFileCopy, // path for file copy
                    requestMethod.post, // file copy is a post
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    okAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status); // reference to update the output success/failure status for the communication
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.Event>();
                return ex;
            }
            return null;
        }
        #endregion

        #region internal API calls
        /// <summary>
        /// Sends a list of sync events to the server.  The events must be batched in groups of 1,000 or less.
        /// </summary>
        /// <param name="syncToRequest">The array of events to send to the server.</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, or null.</returns>
        internal CLError SyncToCloud(To syncToRequest, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.To response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;

            // try/catch to process the sync_to request, on catch return the error
            try
            {
                // check input parameters
                if (syncToRequest == null)
                {
                    throw new ArgumentException("syncToRequest must not be null");
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // run the HTTP communication and store the response object to the output parameter
                string serverMethodPath = CLDefinitions.MethodPathSyncTo;
                response = ProcessHttp<JsonContracts.To>(
                    syncToRequest,
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

        /// <summary>
        /// Sends a list of sync events to the server.  The events must be batched in groups of 1,000 or less.
        /// </summary>
        /// <param name="pushRequest">The parameters to send to the server.</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, or null.</returns>
        internal CLError SyncFromCloud(Push pushRequest, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.PushResponse response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;

            // try/catch to process the sync_to request, on catch return the error
            try
            {
                // check input parameters
                if (pushRequest == null)
                {
                    throw new ArgumentException("pushRequest must not be null");
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // run the HTTP communication and store the response object to the output parameter
                string serverMethodPath = CLDefinitions.MethodPathSyncFrom;
                response = ProcessHttp<JsonContracts.PushResponse>(
                    pushRequest, // object to write as request content to the server
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query metadata (dynamic based on file or folder)
                    requestMethod.post, // sync_to is a post
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    okAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status); // reference to update the output success/failure status for the communication
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.PushResponse>();
                return ex;
            }

            return null;
        }

        /// <summary>
        /// Purges any pending changes for the provided user/device combination in the request object (pending file uploads) and outputs the files which were purged
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError PurgePending(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.PendingResponse response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the metadata query, on catch return the error
            try
            {
                // check input parameters

                if (string.IsNullOrEmpty(settings.DeviceId))
                {
                    throw new NullReferenceException("settings DeviceId cannot be null");
                }
                if (settings.SyncBoxId == null)
                {
                    throw new NullReferenceException("settings SyncBoxId cannot be null");
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                response = ProcessHttp<JsonContracts.PendingResponse>(new JsonContracts.PurgePending() // json contract object for purge pending method
                    {
                        DeviceId = settings.DeviceId,
                        SyncBoxId = settings.SyncBoxId
                    },
                    CLDefinitions.CLMetaDataServerURL, CLDefinitions.MethodPathPurgePending, // purge pending address
                    requestMethod.post, // purge pending is a post operation
                    timeoutMilliseconds, // set the timeout for the operation
                    null, // not an upload or download
                    okAccepted, // purge pending should give OK or Accepted
                    ref status); // reference to update output status
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.PendingResponse>();
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

        // forwards to the main HTTP REST routine helper method which processes the actual communication, but only where the return type is object
        private object ProcessHttp(object requestContent, // JSON contract object to serialize and send up as the request content, if any
            string serverUrl, // the server URL
            string serverMethodPath, // the server method path
            requestMethod method, // type of HTTP method (get vs. put vs. post)
            int timeoutMilliseconds, // time before communication timeout (does not restrict time for the upload or download of files)
            uploadDownloadParams uploadDownload, // parameters if the method is for a file upload or download, or null otherwise
            HashSet<HttpStatusCode> validStatusCodes, // a HashSet with HttpStatusCodes which should be considered all possible successful return codes from the server
            ref CLHttpRestStatus status) // reference to the successful/failed state of communication
        {
            return ProcessHttp<object>(requestContent,
                serverUrl,
                serverMethodPath,
                method,
                timeoutMilliseconds,
                uploadDownload,
                validStatusCodes,
                ref status);
        }

        // main HTTP REST routine helper method which processes the actual communication
        // T should be the type of the JSON contract object which an be deserialized from the return response of the server if any, otherwise use string/object type which will be filled in as the entire string response
        private T ProcessHttp<T>(object requestContent, // JSON contract object to serialize and send up as the request content, if any
            string serverUrl, // the server URL
            string serverMethodPath, // the server method path
            requestMethod method, // type of HTTP method (get vs. put vs. post)
            int timeoutMilliseconds, // time before communication timeout (does not restrict time for the upload or download of files)
            uploadDownloadParams uploadDownload, // parameters if the method is for a file upload or download, or null otherwise
            HashSet<HttpStatusCode> validStatusCodes, // a HashSet with HttpStatusCodes which should be considered all possible successful return codes from the server
            ref CLHttpRestStatus status) // reference to the successful/failed state of communication
            where T : class // restrict T to an object type to allow default null return
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
            httpRequest.Headers[CLDefinitions.HeaderKeyAuthorization] = CLDefinitions.HeaderAppendCWS0 +
                                CLDefinitions.HeaderAppendKey +
                                settings.ApplicationKey + ", " +
                                CLDefinitions.HeaderAppendSignature +
                                        Helpers.GenerateAuthorizationHeaderToken(
                                            settings,
                                            httpMethod: httpRequest.Method,
                                            pathAndQueryStringAndFragment: serverMethodPath);   // set the authentication token
            httpRequest.SendChunked = false; // do not send chunked
            httpRequest.Timeout = timeoutMilliseconds; // set timeout by input parameter, timeout does not apply to the amount of time it takes to perform uploading or downloading of a file

            // declare the bytes for the serialized request body content
            byte[] requestContentBytes;

            // for any communication which is not a file upload, determine the bytes which will be sent up in the request
            if (uploadDownload == null ||
                !(uploadDownload is uploadParams))
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

                        Type requestType = requestContent.GetType();
                        DataContractJsonSerializer getRequestSerializer;
                        if (!SerializableRequestTypes.TryGetValue(requestType, out getRequestSerializer))
                        {
                            throw new ArgumentException("Unknown requestContent Type: " + requestType.FullName);
                        }

                        getRequestSerializer.WriteObject(requestMemory, requestContent);

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
                httpRequest.Headers[CLDefinitions.HeaderAppendContentMD5] = ((uploadParams)uploadDownload).Hash; // set MD5 content hash for verification of upload stream
                if (!string.IsNullOrEmpty(settings.DeviceId)) // conditionally add device id if available
                {
                    httpRequest.Headers[CLDefinitions.QueryStringDeviceId] = settings.DeviceId; // add device id so it will come through on push notifications
                }
                httpRequest.KeepAlive = true; // do not close connection (is this needed?)
                requestContentBytes = null; // do not write content bytes since they will come from the Stream inside the upload object
            }
            #endregion

            #region trace request
            // if communication is supposed to be traced, then trace it
            if ((settings.TraceType & TraceType.Communication) == TraceType.Communication)
            {
                // trace communication for the current request
                ComTrace.LogCommunication(settings.TraceLocation, // location of trace file
                    settings.DeviceId, // device id
                    settings.SyncBoxId, // user id
                    CommunicationEntryDirection.Request, // direction is request
                    serverUrl + serverMethodPath, // location for the server method
                    true, // trace is enabled
                    httpRequest.Headers, // headers of request
                    ((uploadDownload != null && uploadDownload is uploadParams) // special condition for the request body content based on whether this is a file upload or not
                        ? "---File upload started---" // truncate the request body content to a predefined string so that the entire uploaded file is not written as content
                        : (requestContentBytes == null // condition on whether there were bytes to write in the request content body
                            ? null // if there were no bytes to write in the request content body, then log for none
                            : Encoding.UTF8.GetString(requestContentBytes))), // if there were no bytes to write in the request content body, then log them (in string form)
                    null, // no status code for requests
                    settings.TraceExcludeAuthorization, // whether or not to exclude authorization information (like the authentication key)
                    httpRequest.Host, // host value which would be part of the headers (but cannot be pulled from headers directly)
                    ((requestContentBytes != null || (uploadDownload != null && uploadDownload is uploadParams))
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
                if (!(uploadDownload is uploadParams)
                    && !(uploadDownload is downloadParams))
                {
                    throw new ArgumentException("uploadDownload must be either upload or download");
                }

                // set the status event parameters

                storeSizeForStatus = uploadDownload.ChangeToTransfer.Metadata.HashableProperties.Size ?? 0; // pull size from the change to transfer
                transferStartTime = DateTime.Now; // use the current local time as transfer start time
            }
            #endregion

            #region write request
            // if this communication is for a file upload or download, then process its request accordingly
            if (uploadDownload != null)
            {
                // get the request stream
                Stream httpRequestStream = null;

                // try/finally process the upload request (which actually uploads the file) or download request, finally dispose the request stream if it was set
                try
                {
                    // if the current communication is file upload, then upload the file
                    if (uploadDownload is uploadParams)
                    {
                        // grab the upload request stream asynchronously since it can take longer than the provided timeout milliseconds
                        httpRequestStream = AsyncGetUploadRequestStreamOrDownloadResponse(uploadDownload.ShutdownToken, httpRequest, upload: true) as Stream;

                        // if there was no request stream retrieved, then the request was cancelled so return cancelled
                        if (httpRequestStream == null)
                        {
                            status = CLHttpRestStatus.Cancelled;
                            return null;
                        }

                        // define a transfer buffer between the file and the upload stream
                        byte[] uploadBuffer = new byte[FileConstants.BufferSize];

                        // declare a count of the bytes read in each buffer read from the file
                        int bytesRead;
                        // define a count for the total amount of bytes uploaded so far
                        long totalBytesUploaded = 0;

                        // loop till there are no more bytes to read, on the loop condition perform the buffer transfer from the file and store the read byte count
                        while ((bytesRead = ((uploadParams)uploadDownload).Stream.Read(uploadBuffer, 0, uploadBuffer.Length)) != 0)
                        {
                            // write the buffer from the file to the upload stream
                            httpRequestStream.Write(uploadBuffer, 0, bytesRead);
                            // add the number of bytes read on the current buffer transfer to the total bytes uploaded
                            totalBytesUploaded += bytesRead;

                            // check for sync shutdown
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

                            // fire event callbacks for status change on uploading
                            uploadDownload.StatusCallback(new CLStatusFileTransferUpdateParameters(
                                    transferStartTime, // time of upload start
                                    storeSizeForStatus, // total size of file
                                    uploadDownload.RelativePathForStatus, // relative path of file
                                    totalBytesUploaded), // bytes uploaded so far
                                uploadDownload.ChangeToTransfer); // the source of the event (the event itself)
                        }

                        // upload is finished so stream can be disposed
                        ((uploadParams)uploadDownload).DisposeStream();
                    }
                    // else if the communication is a file download, write the request stream content from the serialized download request object
                    else
                    {
                        // grab the request stream for writing
                        httpRequestStream = httpRequest.GetRequestStream();

                        // write the request for the download
                        httpRequestStream.Write(requestContentBytes, 0, requestContentBytes.Length);
                    }
                }
                finally
                {
                    // dispose the request stream if it was set
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
            // else if the communication is neither an upload nor download and there is a serialized request object to write, then get the request stream and write to it
            else if (requestContentBytes != null)
            {
                using (Stream httpRequestStream = httpRequest.GetRequestStream())
                {
                    httpRequestStream.Write(requestContentBytes, 0, requestContentBytes.Length);
                }
            }
            #endregion

            // define the web response outside the regions "get response" and "process response stream" so it can finally be closed (if it ever gets set); also for trace
            HttpWebResponse httpResponse = null; // communication response
            string responseBody = null; // string body content of response (for a string output is used instead of the response stream itself)
            Stream responseStream = null; // response stream (when the communication output is a deserialized object instead of a simple string representation)
            Stream serializationStream = null; // a possible copy of the response stream for when the stream has to be used both for trace and for deserializing a return object

            // try/catch/finally get the response and process its stream for output,
            // on error send a final status event if communication is for upload or download,
            // finally possibly trace if a string response was used and dispose any response/response streams
            try
            {
                #region get response
                // if the communication is a download, then grab the download response asynchronously so its time is not limited to the timeout milliseconds
                if (uploadDownload != null
                    && uploadDownload is downloadParams)
                {
                    // grab the download response asynchronously so its time is not limited to the timeout milliseconds
                    httpResponse = AsyncGetUploadRequestStreamOrDownloadResponse(uploadDownload.ShutdownToken, httpRequest, false) as HttpWebResponse;

                    // if there was no download response, then it was cancelled so return as such
                    if (httpRequest == null)
                    {
                        status = CLHttpRestStatus.Cancelled;
                        return null;
                    }
                }
                // else if the communication is not a download, then grab the response
                else
                {
                    // try/catch grab the communication response, on catch try to pull the response from the exception otherwise rethrow the exception
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

                // if the status code of the response is not in the provided HashSet of those which represent success,
                // then try to provide a more specific return status and try to pull the content from the response as a string and throw an exception for invalid status code
                if (!validStatusCodes.Contains(httpResponse.StatusCode))
                {
                    // if response status code is a not found, then set the output status accordingly
                    if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                    {
                        status = CLHttpRestStatus.NotFound;
                    }
                    // else if response status was not a not found and is a no content, then set the output status accordingly
                    else if (httpResponse.StatusCode == HttpStatusCode.NoContent)
                    {
                        status = CLHttpRestStatus.NoContent;
                    }
                    // else if the response status was neither a not found nor a no content and is an unauthorized, then set the output state accordingly
                    else if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        status = CLHttpRestStatus.NotAuthorized;
                    }
                    // else if response status was neither a not found nor a no content and is within the range of a server error (5XX), then set the output status accordingly
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
                // define an object for the communication return, defaulting to null
                T toReturn = null;

                // if the communication was an upload or a download, then process the response stream for a download (which is the download itself) or use a predefined return for an upload
                if (uploadDownload != null)
                {
                    // if communication is an upload, then use a predefined return
                    if (uploadDownload is uploadParams)
                    {
                        // set body as successful value
                        responseBody = "---File upload complete---";

                        // if we can use a string output for the return, then use it
                        if (typeof(T) == typeof(string)
                            || typeof(T) == typeof(object))
                        {
                            toReturn = (T)((object)responseBody);
                        }
                    }
                    // else if communication is a download, then process the actual download itself
                    else
                    {
                        // set the response body to a value that will be displayed if the actual response fails to process
                        responseBody = "---Incomplete file download---";

                        // create a new unique id for the download
                        Guid newTempFile = Guid.NewGuid();

                        // if a callback was provided to fire before a download starts, then fire it
                        if (((downloadParams)uploadDownload).BeforeDownloadCallback != null)
                        {
                            ((downloadParams)uploadDownload).BeforeDownloadCallback(newTempFile, ((downloadParams)uploadDownload).BeforeDownloadUserState);
                        }

                        // calculate location for downloading the file
                        string newTempFileString = ((downloadParams)uploadDownload).TempDownloadFolderPath + "\\" + ((Guid)newTempFile).ToString("N");

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

                                    // check for sync shutdown
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

                                    // fire event callbacks for status change on uploading
                                    uploadDownload.StatusCallback(
                                        new CLStatusFileTransferUpdateParameters(
                                                transferStartTime, // start time for download
                                                storeSizeForStatus, // total file size
                                                uploadDownload.RelativePathForStatus, // relative path of file
                                                totalBytesDownloaded), // current count of completed download bytes
                                        uploadDownload.ChangeToTransfer); // the source of the event, the event itself
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
                        ((downloadParams)uploadDownload).AfterDownloadCallback(newTempFileString, // location of temp file
                            uploadDownload.ChangeToTransfer,
                            ref responseBody, // reference to response string (sets to "---Completed file download---" on success)
                            ((downloadParams)uploadDownload).AfterDownloadUserState, // timer for failure queue
                            newTempFile); // id for the downloaded file

                        // if the after downloading callback set the response to null, then replace it saying it was null
                        if (responseBody == null)
                        {
                            responseBody = "---responseBody set to null---";
                        }

                        // if a string can be output as the return type, then return the response (which is not the actual download, but a simple string status representation)
                        if (typeof(T) == typeof(string)
                            || typeof(T) == typeof(object))
                        {
                            toReturn = (T)((object)responseBody);
                        }
                    }
                }
                // else if the communication was neither an upload nor a download, then process the response stream for return
                else
                {
                    // declare the serializer which will be used to deserialize the response content for output
                    DataContractJsonSerializer outSerializer;
                    // try to get the serializer for the output by the type of output from dictionary and if successful, process response content as stream to deserialize
                    if (SerializableResponseTypes.TryGetValue(typeof(T), out outSerializer))
                    {
                        // grab the stream for response content
                        responseStream = httpResponse.GetResponseStream();

                        // set the stream for processing the response by a copy of the communication stream (if trace enabled) or the communication stream itself (if trace is not enabled)
                        serializationStream = (((settings.TraceType & TraceType.Communication) == TraceType.Communication)
                            ? Helpers.CopyHttpWebResponseStreamAndClose(responseStream) // if trace is enabled, then copy the communications stream to a memory stream
                            : responseStream); // if trace is not enabled, use the communication stream

                        // if tracing communication, then trace communication
                        if ((settings.TraceType & TraceType.Communication) == TraceType.Communication)
                        {
                            // log communication for stream body
                            ComTrace.LogCommunication(settings.TraceLocation, // trace file location
                                settings.DeviceId, // device id
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
                        toReturn = (T)outSerializer.ReadObject(serializationStream);
                    }
                    // else if the output type is not in the dictionary of those serializable and if the output type is either object or string,
                    // then process the response content as a string to output directly
                    else if (typeof(T) == typeof(string)
                        || (typeof(T) == typeof(object)))
                    {
                        // grab the stream from the response content
                        responseStream = httpResponse.GetResponseStream();

                        // create a reader for the response content
                        using (TextReader purgeResponseStreamReader = new StreamReader(responseStream, Encoding.UTF8))
                        {
                            // set the error string from the response
                            toReturn = (T)((object)purgeResponseStreamReader.ReadToEnd());
                        }
                    }
                    // else if the output type is not in the dictionary of those serializable and if the output type is also neither object nor string,
                    // then throw an argument exception
                    else
                    {
                        throw new ArgumentException("T is not a serializable output type nor object/string");
                    }
                }

                // if the code has not thrown an exception by now then it was successful so mark it so in the output
                status = CLHttpRestStatus.Success;
                // return any object set to return for the response, if any
                return toReturn;
                #endregion
            }
            catch
            {
                // if there was an event for the upload or download, then fire the event callback for a final transfer status
                if (uploadDownload != null
                    && (uploadDownload is uploadParams
                        || uploadDownload is downloadParams))
                {
                    // try/catch fire the event callback for final transfer status, silencing errors
                    try
                    {
                        uploadDownload.StatusCallback(
                            new CLStatusFileTransferUpdateParameters(
                                transferStartTime, // retrieve the upload start time

                                // need to send a file size which matches the total uploaded bytes so they are equal to cancel the status
                                uploadDownload.ChangeToTransfer.Metadata.HashableProperties.Size ?? 0,

                                // try to build the same relative path that would be used in the normal status, falling back first to the full path then to an empty string
                                uploadDownload.RelativePathForStatus,

                                // need to send a total uploaded bytes which matches the file size so they are equal to cancel the status
                                uploadDownload.ChangeToTransfer.Metadata.HashableProperties.Size ?? 0),
                            uploadDownload.ChangeToTransfer); // sender of event (the event itself)
                    }
                    catch
                    {
                    }
                }

                // rethrow
                throw;
            }
            finally
            {
                // for communication logging, log communication if it hasn't already been logged in stream deserialization or dispose the serialization stream
                if ((settings.TraceType & TraceType.Communication) == TraceType.Communication)
                {
                    // if there was no stream set for deserialization, then the response was handled as a string and needs to be logged here as such
                    if (serializationStream == null)
                    {
                        // log communication for string body
                        ComTrace.LogCommunication(settings.TraceLocation, // trace file location
                            settings.DeviceId, // device id
                            settings.SyncBoxId, // user id
                            CommunicationEntryDirection.Response, // communication direction is response
                            serverUrl + serverMethodPath, // input parameter method path
                            true, // trace is enabled
                            httpResponse.Headers, // response headers
                            responseBody, // response body (either an overridden string that says "complete" or "incomplete" or an error message from the actual response)
                            (int)httpResponse.StatusCode, // status code of the response
                            settings.TraceExcludeAuthorization); // whether to include authorization in the trace (such as the authentication key)
                    }
                    // else if there was a stream set for deserialization then the response was already logged, but it still needs to be disposed here
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

                // if there was a response stream retrieved then try to dispose it
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

                // if there was a response retrieved then try to close it
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

        // a dual-function wrapper for making asynchronous calls for either retrieving an upload request stream or retrieving a download response
        private static object AsyncGetUploadRequestStreamOrDownloadResponse(CancellationTokenSource shutdownToken, HttpWebRequest httpRequest, bool upload)
        {
            // declare the output object which would be either a Stream for upload request or an HttpWebResponse for a download response
            object toReturn;

            // create new async holder used to make async http calls synchronous
            AsyncRequestHolder requestOrResponseHolder = new AsyncRequestHolder(shutdownToken);

            // declare result from async http call
            IAsyncResult requestOrResponseAsyncResult;

            // lock on async holder for modification
            lock (requestOrResponseHolder)
            {
                // create a callback which handles the IAsyncResult style used in wrapping an asyncronous method to make it synchronous
                AsyncCallback requestOrResponseCallback = new AsyncCallback(MakeAsyncRequestSynchronous);

                // if this helper was called for an upload, then the action is for the request stream
                if (upload)
                {
                    // begin getting the upload request stream asynchronously, using callback which will take the async holder and make the request synchronous again, storing the result
                    requestOrResponseAsyncResult = httpRequest.BeginGetRequestStream(requestOrResponseCallback, requestOrResponseHolder);
                }
                // else if this helper was called for a download, then the action is for the response
                else
                {
                    // begin getting the download response asynchronously, using callback which will take the async holder and make the request synchronous again, storing the result
                    requestOrResponseAsyncResult = httpRequest.BeginGetResponse(requestOrResponseCallback, requestOrResponseHolder);
                }

                // if the request was not already completed synchronously, wait on it to complete
                if (!requestOrResponseHolder.CompletedSynchronously)
                {
                    // wait on the request to become synchronous again
                    Monitor.Wait(requestOrResponseHolder);
                }
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

            // if this helper was called for an upload, then the action is for the request stream
            if (upload)
            {
                toReturn = httpRequest.EndGetRequestStream(requestOrResponseAsyncResult);
            }
            // else if this helper was called for a download, then the action is for the response
            else
            {
                // try/catch to retrieve the response and on catch try to pull the response from the exception otherwise rethrow the exception
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

            // output the retrieved request stream or the retrieved response
            return toReturn;
        }

        /// <summary>
        /// Async HTTP operation holder used to help make async calls synchronous
        /// </summary>
        private sealed class AsyncRequestHolder
        {
            /// <summary>
            /// Whether IAsyncResult was found to be CompletedSynchronously: if so, do not Monitor.Wait
            /// </summary>
            public bool CompletedSynchronously
            {
                get
                {
                    return _completedSynchronously;
                }
            }
            /// <summary>
            /// Mark this when IAsyncResult was found to be CompletedSynchronously
            /// </summary>
            public void MarkCompletedSynchronously()
            {
                _completedSynchronously = true;
            }
            // storage for CompletedSynchronously, only marked when true so default to false
            private bool _completedSynchronously = false;

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
                // if marked as completed synchronously pass through to the userstate which is used within the callstack to prevent blocking on Monitor.Wait
                if (makeSynchronous.CompletedSynchronously)
                {
                    lock (castHolder)
                    {
                        castHolder.MarkCompletedSynchronously();
                    }
                }

                // if asynchronous task completed, then pulse the AsyncRequestHolder
                if (makeSynchronous.IsCompleted)
                {
                    if (!makeSynchronous.CompletedSynchronously)
                    {
                        lock (castHolder)
                        {
                            Monitor.Pulse(castHolder);
                        }
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

                            if (!makeSynchronous.CompletedSynchronously)
                            {
                                lock (castHolder)
                                {
                                    Monitor.Pulse(castHolder);
                                }
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

        // simple enumeration of currently supported HTTP methods
        private enum requestMethod : byte
        {
            put,
            get,
            post
        }

        // class which is inherited by both the class for storing upload parameters and the class for storing download parameters, with the common properties between them
        private abstract class uploadDownloadParams
        {
            /// <summary>
            /// Path for the file where it would look on disk after truncating the location of the sync directory from the beginning
            /// </summary>
            public string RelativePathForStatus
            {
                get
                {
                    return _relativePathForStatus;
                }
            }
            private readonly string _relativePathForStatus;

            /// <summary>
            /// A handler delegate to be fired whenever there is new status information for an upload or download (the progress of the upload/download or completion)
            /// </summary>
            public SendUploadDownloadStatus StatusCallback
            {
                get
                {
                    return _statusCallback;
                }
            }
            private readonly SendUploadDownloadStatus _statusCallback;

            /// <summary>
            /// UserState object which is required for calling the StatusCallback for sending status information events
            /// </summary>
            public FileChange ChangeToTransfer
            {
                get
                {
                    return _changeToTransfer;
                }
            }
            private readonly FileChange _changeToTransfer;

            /// <summary>
            /// A non-required (possibly null) user-provided token source which is checked through an upload or download in order to cancel it
            /// </summary>
            public CancellationTokenSource ShutdownToken
            {
                get
                {
                    return _shutdownToken;
                }
            }
            private readonly CancellationTokenSource _shutdownToken;

            /// <summary>
            /// The constructor for this abstract base object with all parameters corresponding to all properties
            /// </summary>
            /// <param name="StatusCallback">A handler delegate to be fired whenever there is new status information for an upload or download (the progress of the upload/download or completion)</param>
            /// <param name="ChangeToTransfer">UserState object which is required for calling the StatusCallback for sending status information events</param>
            /// <param name="ShutdownToken">A non-required (possibly null) user-provided token source which is checked through an upload or download in order to cancel it</param>
            /// <param name="SyncRootFullPath">Full path to the root directory being synced</param>
            public uploadDownloadParams(SendUploadDownloadStatus StatusCallback, FileChange ChangeToTransfer, CancellationTokenSource ShutdownToken, string SyncRootFullPath)
            {
                // check for required parameters and error out if not set

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
                if (((long)ChangeToTransfer.Metadata.HashableProperties.Size) < 0)
                {
                    throw new ArgumentException("ChangeToTransfer Metadata HashableProperties Size must be greater than or equal to zero");
                }
                if (ChangeToTransfer.Metadata.StorageKey == null)
                {
                    throw new ArgumentException("ChangeToTransfer Metadata StorageKey cannot be null");
                }
                if (ChangeToTransfer.NewPath == null)
                {
                    throw new NullReferenceException("ChangeToTransfer NewPath cannot be null");
                }
                if (StatusCallback == null)
                {
                    throw new NullReferenceException("StatusCallback cannot be null");
                }

                // set the readonly properties for this instance from the construction parameters

                this._statusCallback = StatusCallback;
                this._changeToTransfer = ChangeToTransfer;
                this._relativePathForStatus = this.ChangeToTransfer.NewPath.GetRelativePath((SyncRootFullPath ?? string.Empty), false); // relative path is calculated from full path to file minus full path to sync directory
                this._shutdownToken = ShutdownToken;
            }
        }

        // class for storing download properties which inherits abstract base uploadDownloadParams which stores more necessary properties
        private sealed class downloadParams : uploadDownloadParams
        {
            /// <summary>
            /// A non-required (possibly null) event handler for before a download starts
            /// </summary>
            public BeforeDownloadToTempFile BeforeDownloadCallback
            {
                get
                {
                    return _beforeDownloadCallback;
                }
            }
            private readonly BeforeDownloadToTempFile _beforeDownloadCallback;

            /// <summary>
            /// UserState object passed through as-is when the BeforeDownloadCallback handler is fired
            /// </summary>
            public object BeforeDownloadUserState
            {
                get
                {
                    return _beforeDownloadUserState;
                }
            }
            private readonly object _beforeDownloadUserState;

            /// <summary>
            /// Event handler for after a download completes which needs to move the file from the temp location to its final location and set the response body to "---Completed file download---"
            /// </summary>
            public AfterDownloadToTempFile AfterDownloadCallback
            {
                get
                {
                    return _afterDownloadCallback;
                }
            }
            private readonly AfterDownloadToTempFile _afterDownloadCallback;

            /// <summary>
            /// UserState object passed through as-is when the AfterDownloadCallback handler is fired
            /// </summary>
            public object AfterDownloadUserState
            {
                get
                {
                    return _afterDownloadUserState;
                }
            }
            private readonly object _afterDownloadUserState;

            /// <summary>
            /// Full path location to the directory where temporary download files will be stored
            /// </summary>
            public string TempDownloadFolderPath
            {
                get
                {
                    return _tempDownloadFolderPath;
                }
            }
            private readonly string _tempDownloadFolderPath;

            /// <summary>
            /// The sole constructor for this class with all parameters corresponding to all properties in this class and within its base class uploadDownloadParams
            /// </summary>
            /// <param name="StatusCallback">A handler delegate to be fired whenever there is new status information for an upload or download (the progress of the upload/download or completion)</param>
            /// <param name="ChangeToTransfer">UserState object which is required for calling the StatusCallback for sending status information events</param>
            /// <param name="ShutdownToken">A non-required (possibly null) user-provided token source which is checked through an upload or download in order to cancel it</param>
            /// <param name="SyncRootFullPath">Full path to the root directory being synced</param>
            /// <param name="AfterDownloadCallback">Event handler for after a download completes which needs to move the file from the temp location to its final location and set the response body to "---Completed file download---"</param>
            /// <param name="AfterDownloadUserState">UserState object passed through as-is when the AfterDownloadCallback handler is fired</param>
            /// <param name="TempDownloadFolderPath">Full path location to the directory where temporary download files will be stored</param>
            /// <param name="BeforeDownloadCallback">A non-required (possibly null) event handler for before a download starts</param>
            /// <param name="BeforeDownloadUserState">UserState object passed through as-is when the BeforeDownloadCallback handler is fired</param>
            public downloadParams(AfterDownloadToTempFile AfterDownloadCallback, object AfterDownloadUserState, string TempDownloadFolderPath, SendUploadDownloadStatus StatusCallback, FileChange ChangeToTransfer, CancellationTokenSource ShutdownToken, string SyncRootFullPath, BeforeDownloadToTempFile BeforeDownloadCallback = null, object BeforeDownloadUserState = null)
                : base(StatusCallback, ChangeToTransfer, ShutdownToken, SyncRootFullPath)
            {
                // additional checks for parameters which were not already checked via the abstract base constructor

                if (base.ChangeToTransfer.Direction != SyncDirection.From)
                {
                    throw new ArgumentException("Invalid ChangeToTransfer Direction for a download: " + base.ChangeToTransfer.Direction.ToString());
                }
                //// I changed my mind about this one. We can allow the before download callback to be null.
                //// But, the after download callback is still required since that needs to perform the actual file move operation from temp directory to final location.
                //if (BeforeDownloadCallback == null)
                //{
                //    throw new NullReferenceException("BeforeDownloadCallback cannot be null");
                //}
                if (AfterDownloadCallback == null)
                {
                    throw new NullReferenceException("AfterDownloadCallback cannot be null");
                }

                // set all the readonly fields for public properties by all the parameters which were not passed to the abstract base class

                this._beforeDownloadCallback = BeforeDownloadCallback;
                this._beforeDownloadUserState = BeforeDownloadUserState;
                this._afterDownloadCallback = AfterDownloadCallback;
                this._afterDownloadUserState = AfterDownloadUserState;
                this._tempDownloadFolderPath = TempDownloadFolderPath;
            }
        }

        // class for storing download properties which inherits abstract base uploadDownloadParams which stores more necessary properties
        private sealed class uploadParams : uploadDownloadParams
        {
            /// <summary>
            /// Stream which will be read from to buffer to write into the upload stream, or null if already disposed
            /// </summary>
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

            /// <summary>
            /// Disposes Stream for the upload if it was not already disposed and marks that it was disposed; not thread-safe disposal checking
            /// </summary>
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

            /// <summary>
            /// MD5 hash lowercase hexadecimal string for the entire upload content
            /// </summary>
            public string Hash
            {
                get
                {
                    return _hash;
                }
            }
            private readonly string _hash;

            /// <summary>
            /// The sole constructor for this class with all parameters corresponding to all properties in this class and within its base class uploadDownloadParams
            /// </summary>
            /// <param name="StatusCallback">A handler delegate to be fired whenever there is new status information for an upload or download (the progress of the upload/download or completion)</param>
            /// <param name="ChangeToTransfer">UserState object which is required for calling the StatusCallback for sending status information events; also used to retrieve the StorageKey and MD5 hash for upload</param>
            /// <param name="ShutdownToken">A non-required (possibly null) user-provided token source which is checked through an upload or download in order to cancel it</param>
            /// <param name="SyncRootFullPath">Full path to the root directory being synced</param>
            /// <param name="Stream">Stream which will be read from to buffer to write into the upload stream, or null if already disposed</param>
            public uploadParams(Stream Stream, SendUploadDownloadStatus StatusCallback, FileChange ChangeToTransfer, CancellationTokenSource ShutdownToken, string SyncRootFullPath)
                : base(StatusCallback, ChangeToTransfer, ShutdownToken, SyncRootFullPath)
            {
                // additional checks for parameters which were not already checked via the abstract base constructor

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

                // set the readonly field for the public property by all the parameters which were not passed to the abstract base class

                this._stream = Stream;
            }
        }
        #endregion
    }


    /// <summary>
    /// Handler called whenever progress has been made uploading or downloading a file or if the file was cancelled or completed
    /// </summary>
    /// <param name="status">The parameters which describe the progress itself</param>
    /// <param name="eventSource">The FileChange describing the change to upload or download</param>
    internal delegate void SendUploadDownloadStatus(CLStatusFileTransferUpdateParameters status, FileChange eventSource);

    /// <param name="tempId">Unique ID created for the file and used as the file's name in the temp download directory</param>
    /// <param name="UserState">Object passed through from the download method call specific to before download</param>
    public delegate void BeforeDownloadToTempFile(Guid tempId, object UserState);

    /// <summary>
    /// ¡¡ Action required: move the completed download file from the temp directory to the final destination !!
    /// Handler called after a file download completes with the id used as the file name in the originally provided temporary download folder,
    /// passes through UserState, passes the download change itself, gives a constructed full path where the downloaded file can be found in the temp folder,
    /// and references a string which should be set to something useful for communications trace to denote a completed file such as "---Completed file download---" (but only set after the file was succesfully moved)
    /// </summary>
    /// <param name="tempFileFullPath">Full path to where the downloaded file can be found in the temp folder (which needs to be moved)</param>
    /// <param name="downloadChange">The download change itself</param>
    /// <param name="responseBody">Reference to string used to trace communication, should be set to something useful to read in communications trace such as "---Completed file download---" (but only after the file was successfully moved)</param>
    /// <param name="UserState">Object passed through from the download method call specific to after download</param>
    /// <param name="tempId">Unique ID created for the file and used as the file's name in the temp download directory</param>
    public delegate void AfterDownloadToTempFile(string tempFileFullPath, FileChange downloadChange, ref string responseBody, object UserState, Guid tempId);

    /// <summary>
    /// Status from a call to one of the CLHttpRest communications methods
    /// </summary>
    public enum CLHttpRestStatus : byte
    {
        /// <summary>
        /// Method completed without error and has a normal response
        /// </summary>
        Success,
        /// <summary>
        /// Method invoked a not found (404) response from the server
        /// </summary>
        NotFound,
        /// <summary>
        /// Method invoked a server error (5xx) response from the server
        /// </summary>
        ServerError,
        /// <summary>
        /// Method had some other problem with parameters processed locally or parameters sent up to the server
        /// </summary>
        BadRequest,
        /// <summary>
        /// Method was cancelled via a provided cancellation token before completion
        /// </summary>
        Cancelled,
        /// <summary>
        /// Method completed without error but has no response; it means that no data exists for given parameter(s)
        /// </summary>
        NoContent,
        /// <summary>
        /// Method invoked an unauthorized (401) resposne from the server
        /// </summary>
        NotAuthorized
    }
}