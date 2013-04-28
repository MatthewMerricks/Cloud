// 
// AsyncResults.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Model;
using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.REST
{
    #region base asynchronous result
    /// <summary>
    /// Exposes the result properties, must be inherited by a specific result implementation
    /// </summary>
    public abstract class BaseCLHttpRestResult
    {
        /// <summary>
        /// Any error which may have occurred during communication
        /// </summary>
        public CLError Error
        {
            get
            {
                return _error;
            }
        }
        private readonly CLError _error;

        // construct with all readonly properties
        protected internal BaseCLHttpRestResult(CLError Error)
        {
            this._error = Error;
        }
    }

    /// <summary>
    /// Exposes the result properties, must be inherited by a specific result implementation
    /// </summary>
    public abstract class BaseCLHttpRestResult<T> : BaseCLHttpRestResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public T Response
        {
            get
            {
                return _response;
            }
        }
        private readonly T _response;

        // construct with all readonly properties
        protected internal BaseCLHttpRestResult(CLError Error, T Response)
            : base(Error)
        {
            this._response = Response;
        }
    }
    #endregion

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class DownloadFileResult : BaseCLHttpRestResult
    {
        // construct with all readonly properties
        internal DownloadFileResult(CLError Error)
            : base(Error) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class UploadFileResult : BaseCLHttpRestResult<string>
    {
        public bool HashMismatchFound {
            get { return _hashMismatchFound; }
        }

        private bool _hashMismatchFound;

        // construct with all readonly properties
        internal UploadFileResult(CLError Error, string Result, bool hashMismatchFound)
            : base(Error, Result) 
        {
            _hashMismatchFound = hashMismatchFound;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetMetadataResult : BaseCLHttpRestResult<JsonContracts.SyncboxMetadataResponse>
    {
        // construct with all readonly properties
        internal GetMetadataResult(CLError Error, JsonContracts.SyncboxMetadataResponse Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetAllPendingResult : BaseCLHttpRestResult<JsonContracts.PendingResponse>
    {
        // construct with all readonly properties
        internal GetAllPendingResult(CLError Error, JsonContracts.PendingResponse Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class FileChangeResult : BaseCLHttpRestResult<JsonContracts.FileChangeResponse>
    {
        // construct with all readonly properties
        internal FileChangeResult(CLError Error, JsonContracts.FileChangeResponse Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class UndoDeletionFileChangeResult : BaseCLHttpRestResult<JsonContracts.FileChangeResponse>
    {
        // construct with all readonly properties
        internal UndoDeletionFileChangeResult(CLError Error, JsonContracts.FileChangeResponse Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetFileVersionsResult : BaseCLHttpRestResult<JsonContracts.FileVersion[]>
    {
        // construct with all readonly properties
        internal GetFileVersionsResult(CLError Error, JsonContracts.FileVersion[] Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class CopyFileResult : BaseCLHttpRestResult<JsonContracts.FileChangeResponse>
    {
        // construct with all readonly properties
        internal CopyFileResult(CLError Error, JsonContracts.FileChangeResponse Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetAudiosResult : BaseCLHttpRestResult<JsonContracts.SyncboxGetAllAudioItemsResponse>
    {
        // construct with all readonly properties
        internal GetAudiosResult(CLError Error, JsonContracts.SyncboxGetAllAudioItemsResponse Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetArchivesResult : BaseCLHttpRestResult<JsonContracts.Archives>
    {
        // construct with all readonly properties
        internal GetArchivesResult(CLError Error, JsonContracts.Archives Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxUsageResult : BaseCLHttpRestResult<JsonContracts.SyncboxUsageResponse>
    {
        // construct with all readonly properties
        internal SyncboxUsageResult(CLError Error, JsonContracts.SyncboxUsageResponse Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetFolderHierarchyResult : BaseCLHttpRestResult<JsonContracts.Folders>
    {
        // construct with all readonly properties
        internal GetFolderHierarchyResult(CLError Error, JsonContracts.Folders Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetFolderContentsResult : BaseCLHttpRestResult<JsonContracts.SyncboxFolderContentsResponse>
    {
        // construct with all readonly properties
        internal GetFolderContentsResult(CLError Error, JsonContracts.SyncboxFolderContentsResponse Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class PurgePendingResult : BaseCLHttpRestResult<JsonContracts.PendingResponse>
    {
        // construct with all readonly properties
        internal PurgePendingResult(CLError Error, JsonContracts.PendingResponse Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class CreateSyncboxResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLSyncbox Response
        {
            get
            {
                return _response;
            }
        }
        private readonly CLSyncbox _response;

        /// <summary>
        /// Any error which may have occurred during communication
        /// </summary>
        public CLError Error
        {
            get
            {
                return _error;
            }
        }
        private readonly CLError _error;

        // construct with all readonly properties
        internal CreateSyncboxResult(CLError Error, CLSyncbox Response)
        {
            this._error = Error;
            this._response = Response;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxAllocAndInitResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLSyncbox Response
        {
            get
            {
                return _response;
            }
        }
        private readonly CLSyncbox _response;

        /// <summary>
        /// Any error which may have occurred during communication
        /// </summary>
        public CLError Error
        {
            get
            {
                return _error;
            }
        }
        private readonly CLError _error;

        // construct with all readonly properties
        internal SyncboxAllocAndInitResult(CLError Error, CLSyncbox Response)
        {
            this._error = Error;
            this._response = Response;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxCreateResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLSyncbox Response
        {
            get
            {
                return _response;
            }
        }
        private readonly CLSyncbox _response;

        /// <summary>
        /// Any error which may have occurred during communication
        /// </summary>
        public CLError Error
        {
            get
            {
                return _error;
            }
        }
        private readonly CLError _error;

        // construct with all readonly properties
        internal SyncboxCreateResult(CLError Error, CLSyncbox Response)
        {
            this._error = Error;
            this._response = Response;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class ListStoragePlansResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLStoragePlan [] Response
        {
            get
            {
                return _response;
            }
        }
        private readonly CLStoragePlan [] _response;

        /// <summary>
        /// Any error which may have occurred during communication
        /// </summary>
        public CLError Error
        {
            get
            {
                return _error;
            }
        }
        private readonly CLError _error;

        // construct with all readonly properties
        internal ListStoragePlansResult(CLError Error, CLStoragePlan [] Response)
        {
            this._error = Error;
            this._response = Response;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class CredentialsListSessionsResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLCredentials[] Response
        {
            get
            {
                return _response;
            }
        }
        private readonly CLCredentials[] _response;

        /// <summary>
        /// Any error which may have occurred during communication
        /// </summary>
        public CLError Error
        {
            get
            {
                return _error;
            }
        }
        private readonly CLError _error;

        // construct with all readonly properties
        internal CredentialsListSessionsResult(CLError Error, CLCredentials[] Response)
        {
            this._error = Error;
            this._response = Response;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class CredentialsSessionCreateResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLCredentials Response
        {
            get
            {
                return _response;
            }
        }
        private readonly CLCredentials _response;

        /// <summary>
        /// Any error which may have occurred during communication
        /// </summary>
        public CLError Error
        {
            get
            {
                return _error;
            }
        }
        private readonly CLError _error;

        // construct with all readonly properties
        internal CredentialsSessionCreateResult(CLError Error, CLCredentials Response)
        {
            this._error = Error;
            this._response = Response;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class CredentialsSessionGetForKeyResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLCredentials Response
        {
            get
            {
                return _response;
            }
        }
        private readonly CLCredentials _response;

        /// <summary>
        /// Any error which may have occurred during communication
        /// </summary>
        public CLError Error
        {
            get
            {
                return _error;
            }
        }
        private readonly CLError _error;

        // construct with all readonly properties
        internal CredentialsSessionGetForKeyResult(CLError Error, CLCredentials Response)
        {
            this._error = Error;
            this._response = Response;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxGetRecentsResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLFileItem[] Response
        {
            get
            {
                return _response;
            }
        }
        private readonly CLFileItem[] _response;

        /// <summary>
        /// Any error which may have occurred during communication
        /// </summary>
        public CLError Error
        {
            get
            {
                return _error;
            }
        }
        private readonly CLError _error;

        // construct with all readonly properties
        internal SyncboxGetRecentsResult(CLError Error, CLFileItem[] Response)
        {
            this._error = Error;
            this._response = Response;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxListResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLSyncbox[] Response
        {
            get
            {
                return _response;
            }
        }
        private readonly CLSyncbox[] _response;

        /// <summary>
        /// Any error which may have occurred during communication
        /// </summary>
        public CLError Error
        {
            get
            {
                return _error;
            }
        }
        private readonly CLError _error;

        // construct with all readonly properties
        internal SyncboxListResult(CLError Error, CLSyncbox[] Response)
        {
            this._error = Error;
            this._response = Response;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxGetAllImageItemsResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLFileItem[] Response
        {
            get
            {
                return _response;
            }
        }
        private readonly CLFileItem[] _response;

        /// <summary>
        /// Any error which may have occurred during communication
        /// </summary>
        public CLError Error
        {
            get
            {
                return _error;
            }
        }
        private readonly CLError _error;

        // construct with all readonly properties
        internal SyncboxGetAllImageItemsResult(CLError Error, CLFileItem[] Response)
        {
            this._error = Error;
            this._response = Response;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxGetAllVideoItemsResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLFileItem[] Response
        {
            get
            {
                return _response;
            }
        }
        private readonly CLFileItem[] _response;

        /// <summary>
        /// Any error which may have occurred during communication
        /// </summary>
        public CLError Error
        {
            get
            {
                return _error;
            }
        }
        private readonly CLError _error;

        // construct with all readonly properties
        internal SyncboxGetAllVideoItemsResult(CLError Error, CLFileItem[] Response)
        {
            this._error = Error;
            this._response = Response;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxGetAllAudioItemsResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLFileItem[] Response
        {
            get
            {
                return _response;
            }
        }
        private readonly CLFileItem[] _response;

        /// <summary>
        /// Any error which may have occurred during communication
        /// </summary>
        public CLError Error
        {
            get
            {
                return _error;
            }
        }
        private readonly CLError _error;

        // construct with all readonly properties
        internal SyncboxGetAllAudioItemsResult(CLError Error, CLFileItem[] Response)
        {
            this._error = Error;
            this._response = Response;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxGetAllDocumentItemsResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLFileItem[] Response
        {
            get
            {
                return _response;
            }
        }
        private readonly CLFileItem[] _response;

        /// <summary>
        /// Any error which may have occurred during communication
        /// </summary>
        public CLError Error
        {
            get
            {
                return _error;
            }
        }
        private readonly CLError _error;

        // construct with all readonly properties
        internal SyncboxGetAllDocumentItemsResult(CLError Error, CLFileItem[] Response)
        {
            this._error = Error;
            this._response = Response;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxGetAllPresentationItemsResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLFileItem[] Response
        {
            get
            {
                return _response;
            }
        }
        private readonly CLFileItem[] _response;

        /// <summary>
        /// Any error which may have occurred during communication
        /// </summary>
        public CLError Error
        {
            get
            {
                return _error;
            }
        }
        private readonly CLError _error;

        // construct with all readonly properties
        internal SyncboxGetAllPresentationItemsResult(CLError Error, CLFileItem[] Response)
        {
            this._error = Error;
            this._response = Response;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxGetAllTextItemsResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLFileItem[] Response
        {
            get
            {
                return _response;
            }
        }
        private readonly CLFileItem[] _response;

        /// <summary>
        /// Any error which may have occurred during communication
        /// </summary>
        public CLError Error
        {
            get
            {
                return _error;
            }
        }
        private readonly CLError _error;

        // construct with all readonly properties
        internal SyncboxGetAllTextItemsResult(CLError Error, CLFileItem[] Response)
        {
            this._error = Error;
            this._response = Response;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxGetAllArchiveItemsResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLFileItem[] Response
        {
            get
            {
                return _response;
            }
        }
        private readonly CLFileItem[] _response;

        /// <summary>
        /// Any error which may have occurred during communication
        /// </summary>
        public CLError Error
        {
            get
            {
                return _error;
            }
        }
        private readonly CLError _error;

        // construct with all readonly properties
        internal SyncboxGetAllArchiveItemsResult(CLError Error, CLFileItem[] Response)
        {
            this._error = Error;
            this._response = Response;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxGetFolderContentsAtPathResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLFileItem[] Response
        {
            get
            {
                return _response;
            }
        }
        private readonly CLFileItem[] _response;

        /// <summary>
        /// Any error which may have occurred during communication
        /// </summary>
        public CLError Error
        {
            get
            {
                return _error;
            }
        }
        private readonly CLError _error;

        // construct with all readonly properties
        internal SyncboxGetFolderContentsAtPathResult(CLError Error, CLFileItem[] Response)
        {
            this._error = Error;
            this._response = Response;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxGetItemAtPathResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLFileItem Response
        {
            get
            {
                return _response;
            }
        }
        private readonly CLFileItem _response;

        /// <summary>
        /// Any error which may have occurred during communication
        /// </summary>
        public CLError Error
        {
            get
            {
                return _error;
            }
        }
        private readonly CLError _error;

        // construct with all readonly properties
        internal SyncboxGetItemAtPathResult(CLError Error, CLFileItem Response)
        {
            this._error = Error;
            this._response = Response;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxRenameFileResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLFileItem FileItem
        {
            get
            {
                return _response;
            }
        }
        private readonly CLFileItem _response;

        /// <summary>
        /// Any error which may have occurred during communication
        /// </summary>
        public CLError Error
        {
            get
            {
                return _error;
            }
        }
        private readonly CLError _error;

        // construct with all readonly properties
        internal SyncboxRenameFileResult(CLError Error, CLFileItem Response)
        {
            this._error = Error;
            this._response = Response;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxMoveFileResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLFileItem FileItem
        {
            get
            {
                return _response;
            }
        }
        private readonly CLFileItem _response;

        /// <summary>
        /// Any error which may have occurred during communication
        /// </summary>
        public CLError Error
        {
            get
            {
                return _error;
            }
        }
        private readonly CLError _error;

        // construct with all readonly properties
        internal SyncboxMoveFileResult(CLError Error, CLFileItem Response)
        {
            this._error = Error;
            this._response = Response;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxRenameFilesResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLFileItem [] FileItems
        {
            get
            {
                return _responses;
            }
        }
        private readonly CLFileItem [] _responses;

        /// <summary>
        /// Any item errors which may have occurred during communication
        /// </summary>
        public CLError [] Errors
        {
            get
            {
                return _errors;
            }
        }
        private readonly CLError [] _errors;

        /// <summary>
        /// Any overall error which may have occurred during communication
        /// </summary>
        public CLError OverallError
        {
            get
            {
                return _overallError;
            }
        }
        private readonly CLError _overallError;

        // construct with all readonly properties
        internal SyncboxRenameFilesResult(CLError overallError, CLError [] Errors, CLFileItem [] Responses)
        {
            this._errors = Errors;
            this._responses = Responses;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxMoveFilesResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLFileItem[] FileItems
        {
            get
            {
                return _responses;
            }
        }
        private readonly CLFileItem[] _responses;

        /// <summary>
        /// Any item errors which may have occurred during communication
        /// </summary>
        public CLError[] Errors
        {
            get
            {
                return _errors;
            }
        }
        private readonly CLError[] _errors;

        /// <summary>
        /// Any overall error which may have occurred during communication
        /// </summary>
        public CLError OverallError
        {
            get
            {
                return _overallError;
            }
        }
        private readonly CLError _overallError;

        // construct with all readonly properties
        internal SyncboxMoveFilesResult(CLError overallError, CLError[] Errors, CLFileItem[] Responses)
        {
            this._errors = Errors;
            this._responses = Responses;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxRenameFolderResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLFileItem FolderItem
        {
            get
            {
                return _response;
            }
        }
        private readonly CLFileItem _response;

        /// <summary>
        /// Any error which may have occurred during communication
        /// </summary>
        public CLError Error
        {
            get
            {
                return _error;
            }
        }
        private readonly CLError _error;

        // construct with all readonly properties
        internal SyncboxRenameFolderResult(CLError Error, CLFileItem Response)
        {
            this._error = Error;
            this._response = Response;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxMoveFolderResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLFileItem FolderItem
        {
            get
            {
                return _response;
            }
        }
        private readonly CLFileItem _response;

        /// <summary>
        /// Any error which may have occurred during communication
        /// </summary>
        public CLError Error
        {
            get
            {
                return _error;
            }
        }
        private readonly CLError _error;

        // construct with all readonly properties
        internal SyncboxMoveFolderResult(CLError Error, CLFileItem Response)
        {
            this._error = Error;
            this._response = Response;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxRenameFoldersResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLFileItem[] FolderItems
        {
            get
            {
                return _responses;
            }
        }
        private readonly CLFileItem[] _responses;

        /// <summary>
        /// Any item errors which may have occurred during communication
        /// </summary>
        public CLError[] Errors
        {
            get
            {
                return _errors;
            }
        }
        private readonly CLError[] _errors;

        /// <summary>
        /// Any overall error which may have occurred during communication
        /// </summary>
        public CLError OverallError
        {
            get
            {
                return _overallError;
            }
        }
        private readonly CLError _overallError;

        // construct with all readonly properties
        internal SyncboxRenameFoldersResult(CLError overallError, CLError[] Errors, CLFileItem[] Responses)
        {
            this._errors = Errors;
            this._responses = Responses;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxMoveFoldersResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLFileItem[] FolderItems
        {
            get
            {
                return _responses;
            }
        }
        private readonly CLFileItem[] _responses;

        /// <summary>
        /// Any item errors which may have occurred during communication
        /// </summary>
        public CLError[] Errors
        {
            get
            {
                return _errors;
            }
        }
        private readonly CLError[] _errors;

        /// <summary>
        /// Any overall error which may have occurred during communication
        /// </summary>
        public CLError OverallError
        {
            get
            {
                return _overallError;
            }
        }
        private readonly CLError _overallError;

        // construct with all readonly properties
        internal SyncboxMoveFoldersResult(CLError overallError, CLError[] Errors, CLFileItem[] Responses)
        {
            this._errors = Errors;
            this._responses = Responses;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SessionShowResult : BaseCLHttpRestResult<JsonContracts.CredentialsSessionGetForKeyResponse>
    {
        // construct with all readonly properties
        internal SessionShowResult(CLError Error, JsonContracts.CredentialsSessionGetForKeyResponse Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class CredentialsSessionDeleteResult : BaseCLHttpRestResult<JsonContracts.CredentialsSessionDeleteResponse>
    {
        // construct with all readonly properties
        internal CredentialsSessionDeleteResult(CLError Error, JsonContracts.CredentialsSessionDeleteResponse Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxUpdateExtendedMetadataResult : BaseCLHttpRestResult<JsonContracts.SyncboxResponse>
    {
        // construct with all readonly properties
        internal SyncboxUpdateExtendedMetadataResult(CLError Error, JsonContracts.SyncboxResponse Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class LinkDeviceFirstTimeResult : BaseCLHttpRestResult<JsonContracts.LinkDeviceFirstTimeResponse>
    {
        // construct with all readonly properties
        internal LinkDeviceFirstTimeResult(CLError Error, JsonContracts.LinkDeviceFirstTimeResponse Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class LinkDeviceResult : BaseCLHttpRestResult<JsonContracts.LinkDeviceResponse>
    {
        // construct with all readonly properties
        internal LinkDeviceResult(CLError Error, JsonContracts.LinkDeviceResponse Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class UnlinkDeviceResult : BaseCLHttpRestResult<JsonContracts.UnlinkDeviceResponse>
    {
        // construct with all readonly properties
        internal UnlinkDeviceResult(CLError Error, JsonContracts.UnlinkDeviceResponse Response)
            : base(Error, Response) { }
    }

    #region UpdateSyncboxQuota (deprecated)
    ///// <summary>
    ///// Holds result properties
    ///// </summary>
    //public sealed class UpdateSyncboxQuotaResult : BaseCLHttpRestResult<JsonContracts.SyncboxHolder>
    //{
    //    // construct with all readonly properties
    //    internal UpdateSyncboxQuotaResult(CLError Error, JsonContracts.SyncboxHolder Response)
    //        : base(Error, Response) { }
    //}
    #endregion

    #region SyncboxUpdate
    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxUpdateFriendlyNameResult : BaseCLHttpRestResult<JsonContracts.SyncboxResponse>
    {
        // construct with all readonly properties
        internal SyncboxUpdateFriendlyNameResult(CLError Error, JsonContracts.SyncboxResponse Response)
            : base(Error, Response) { }
    }
    #endregion

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxUpdateStoragePlanResult : BaseCLHttpRestResult<JsonContracts.SyncboxUpdateStoragePlanResponse>
    {
        // construct with all readonly properties
        internal SyncboxUpdateStoragePlanResult(CLError Error, JsonContracts.SyncboxUpdateStoragePlanResponse Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxDeleteResult : BaseCLHttpRestResult<JsonContracts.SyncboxDeleteResponse>
    {
        // construct with all readonly properties
        internal SyncboxDeleteResult(CLError Error, JsonContracts.SyncboxDeleteResponse Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxStatusResult : BaseCLHttpRestResult<JsonContracts.SyncboxStatusResponse>
    {
        // construct with all readonly properties
        internal SyncboxStatusResult(CLError Error, JsonContracts.SyncboxStatusResponse Response)
            : base(Error, Response) { }
    }
}