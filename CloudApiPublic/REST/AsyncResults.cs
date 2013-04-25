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

        /// <summary>
        /// The status resulting from communication
        /// </summary>
        public CLHttpRestStatus Status
        {
            get
            {
                return _status;
            }
        }
        private readonly CLHttpRestStatus _status;

        // construct with all readonly properties
        protected internal BaseCLHttpRestResult(CLError Error, CLHttpRestStatus Status)
        {
            this._error = Error;
            this._status = Status;
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
        protected internal BaseCLHttpRestResult(CLError Error, CLHttpRestStatus Status, T Response)
            : base(Error, Status)
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
        internal DownloadFileResult(CLError Error, CLHttpRestStatus Status)
            : base(Error, Status) { }
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
        internal UploadFileResult(CLError Error, CLHttpRestStatus Status, string Result, bool hashMismatchFound)
            : base(Error, Status, Result) 
        {
            _hashMismatchFound = hashMismatchFound;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetMetadataResult : BaseCLHttpRestResult<JsonContracts.Metadata>
    {
        // construct with all readonly properties
        internal GetMetadataResult(CLError Error, CLHttpRestStatus Status, JsonContracts.Metadata Response)
            : base(Error, Status, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetAllPendingResult : BaseCLHttpRestResult<JsonContracts.PendingResponse>
    {
        // construct with all readonly properties
        internal GetAllPendingResult(CLError Error, CLHttpRestStatus Status, JsonContracts.PendingResponse Response)
            : base(Error, Status, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class FileChangeResult : BaseCLHttpRestResult<JsonContracts.FileChangeResponse>
    {
        // construct with all readonly properties
        internal FileChangeResult(CLError Error, CLHttpRestStatus Status, JsonContracts.FileChangeResponse Response)
            : base(Error, Status, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class UndoDeletionFileChangeResult : BaseCLHttpRestResult<JsonContracts.FileChangeResponse>
    {
        // construct with all readonly properties
        internal UndoDeletionFileChangeResult(CLError Error, CLHttpRestStatus Status, JsonContracts.FileChangeResponse Response)
            : base(Error, Status, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetFileVersionsResult : BaseCLHttpRestResult<JsonContracts.FileVersion[]>
    {
        // construct with all readonly properties
        internal GetFileVersionsResult(CLError Error, CLHttpRestStatus Status, JsonContracts.FileVersion[] Response)
            : base(Error, Status, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class CopyFileResult : BaseCLHttpRestResult<JsonContracts.FileChangeResponse>
    {
        // construct with all readonly properties
        internal CopyFileResult(CLError Error, CLHttpRestStatus Status, JsonContracts.FileChangeResponse Response)
            : base(Error, Status, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetAudiosResult : BaseCLHttpRestResult<JsonContracts.SyncboxGetAllAudioItemsResponse>
    {
        // construct with all readonly properties
        internal GetAudiosResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SyncboxGetAllAudioItemsResponse Response)
            : base(Error, Status, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetArchivesResult : BaseCLHttpRestResult<JsonContracts.Archives>
    {
        // construct with all readonly properties
        internal GetArchivesResult(CLError Error, CLHttpRestStatus Status, JsonContracts.Archives Response)
            : base(Error, Status, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxUsageResult : BaseCLHttpRestResult<JsonContracts.SyncboxUsageResponse>
    {
        // construct with all readonly properties
        internal SyncboxUsageResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SyncboxUsageResponse Response)
            : base(Error, Status, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetFolderHierarchyResult : BaseCLHttpRestResult<JsonContracts.Folders>
    {
        // construct with all readonly properties
        internal GetFolderHierarchyResult(CLError Error, CLHttpRestStatus Status, JsonContracts.Folders Response)
            : base(Error, Status, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetFolderContentsResult : BaseCLHttpRestResult<JsonContracts.FolderContents>
    {
        // construct with all readonly properties
        internal GetFolderContentsResult(CLError Error, CLHttpRestStatus Status, JsonContracts.FolderContents Response)
            : base(Error, Status, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class PurgePendingResult : BaseCLHttpRestResult<JsonContracts.PendingResponse>
    {
        // construct with all readonly properties
        internal PurgePendingResult(CLError Error, CLHttpRestStatus Status, JsonContracts.PendingResponse Response)
            : base(Error, Status, Response) { }
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

        /// <summary>
        /// The status resulting from communication
        /// </summary>
        public CLSyncboxCreationStatus Status
        {
            get
            {
                return _status;
            }
        }
        private readonly CLSyncboxCreationStatus _status;

        // construct with all readonly properties
        internal CreateSyncboxResult(CLError Error, CLSyncboxCreationStatus Status, CLSyncbox Response)
        {
            this._error = Error;
            this._status = Status;
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

        /// <summary>
        /// The status resulting from communication
        /// </summary>
        public CLHttpRestStatus Status   // &&&& Fix this
        {
            get
            {
                return _status;
            }
        }
        private readonly CLHttpRestStatus _status;   // &&&& fix this

        // construct with all readonly properties
        internal SyncboxAllocAndInitResult(CLError Error, CLHttpRestStatus Status  /* &&&&: Fix this */, CLSyncbox Response)
        {
            this._error = Error;
            this._status = Status;
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

        /// <summary>
        /// The status resulting from communication
        /// </summary>
        public CLHttpRestStatus Status  // &&&& fix this
        {
            get
            {
                return _status;
            }
        }
        private readonly CLHttpRestStatus _status;  // &&&& fix this

        // construct with all readonly properties
        internal SyncboxCreateResult(CLError Error, CLHttpRestStatus Status /* &&&& fix this */, CLSyncbox Response)
        {
            this._error = Error;
            this._status = Status;
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

        /// <summary>
        /// The status resulting from communication
        /// </summary>
        public CLHttpRestStatus Status  // &&&& fix this
        {
            get
            {
                return _status;
            }
        }
        private readonly CLHttpRestStatus _status;  // &&&& fix this

        // construct with all readonly properties
        internal ListStoragePlansResult(CLError Error, CLHttpRestStatus Status /* &&&& fix this */, CLStoragePlan [] Response)
        {
            this._error = Error;
            this._status = Status;
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

        /// <summary>
        /// The status resulting from communication
        /// </summary>
        public CLHttpRestStatus Status  // &&&& fix this
        {
            get
            {
                return _status;
            }
        }
        private readonly CLHttpRestStatus _status;  // &&&& fix this

        // construct with all readonly properties
        internal CredentialsListSessionsResult(CLError Error, CLHttpRestStatus Status /* &&&& fix this */, CLCredentials[] Response)
        {
            this._error = Error;
            this._status = Status;
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

        /// <summary>
        /// The status resulting from communication
        /// </summary>
        public CLHttpRestStatus Status  // &&&& fix this
        {
            get
            {
                return _status;
            }
        }
        private readonly CLHttpRestStatus _status;  // &&&& fix this

        // construct with all readonly properties
        internal CredentialsSessionCreateResult(CLError Error, CLHttpRestStatus Status /* &&&& fix this */, CLCredentials Response)
        {
            this._error = Error;
            this._status = Status;
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

        /// <summary>
        /// The status resulting from communication
        /// </summary>
        public CLHttpRestStatus Status  // &&&& fix this
        {
            get
            {
                return _status;
            }
        }
        private readonly CLHttpRestStatus _status;  // &&&& fix this

        // construct with all readonly properties
        internal CredentialsSessionGetForKeyResult(CLError Error, CLHttpRestStatus Status /* &&&& fix this */, CLCredentials Response)
        {
            this._error = Error;
            this._status = Status;
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

        /// <summary>
        /// The status resulting from communication
        /// </summary>
        public CLHttpRestStatus Status  // &&&& fix this
        {
            get
            {
                return _status;
            }
        }
        private readonly CLHttpRestStatus _status;  // &&&& fix this

        // construct with all readonly properties
        internal SyncboxGetRecentsResult(CLError Error, CLHttpRestStatus Status /* &&&& fix this */, CLFileItem[] Response)
        {
            this._error = Error;
            this._status = Status;
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

        /// <summary>
        /// The status resulting from communication
        /// </summary>
        public CLHttpRestStatus Status  // &&&& fix this
        {
            get
            {
                return _status;
            }
        }
        private readonly CLHttpRestStatus _status;  // &&&& fix this

        // construct with all readonly properties
        internal SyncboxListResult(CLError Error, CLHttpRestStatus Status /* &&&& fix this */, CLSyncbox[] Response)
        {
            this._error = Error;
            this._status = Status;
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

        /// <summary>
        /// The status resulting from communication
        /// </summary>
        public CLHttpRestStatus Status  // &&&& fix this
        {
            get
            {
                return _status;
            }
        }
        private readonly CLHttpRestStatus _status;  // &&&& fix this

        // construct with all readonly properties
        internal SyncboxGetAllImageItemsResult(CLError Error, CLHttpRestStatus Status /* &&&& fix this */, CLFileItem[] Response)
        {
            this._error = Error;
            this._status = Status;
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

        /// <summary>
        /// The status resulting from communication
        /// </summary>
        public CLHttpRestStatus Status  // &&&& fix this
        {
            get
            {
                return _status;
            }
        }
        private readonly CLHttpRestStatus _status;  // &&&& fix this

        // construct with all readonly properties
        internal SyncboxGetAllVideoItemsResult(CLError Error, CLHttpRestStatus Status /* &&&& fix this */, CLFileItem[] Response)
        {
            this._error = Error;
            this._status = Status;
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

        /// <summary>
        /// The status resulting from communication
        /// </summary>
        public CLHttpRestStatus Status  // &&&& fix this
        {
            get
            {
                return _status;
            }
        }
        private readonly CLHttpRestStatus _status;  // &&&& fix this

        // construct with all readonly properties
        internal SyncboxGetAllAudioItemsResult(CLError Error, CLHttpRestStatus Status /* &&&& fix this */, CLFileItem[] Response)
        {
            this._error = Error;
            this._status = Status;
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

        /// <summary>
        /// The status resulting from communication
        /// </summary>
        public CLHttpRestStatus Status  // &&&& fix this
        {
            get
            {
                return _status;
            }
        }
        private readonly CLHttpRestStatus _status;  // &&&& fix this

        // construct with all readonly properties
        internal SyncboxGetAllDocumentItemsResult(CLError Error, CLHttpRestStatus Status /* &&&& fix this */, CLFileItem[] Response)
        {
            this._error = Error;
            this._status = Status;
            this._response = Response;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SessionShowResult : BaseCLHttpRestResult<JsonContracts.CredentialsSessionGetForKeyResponse>
    {
        // construct with all readonly properties
        internal SessionShowResult(CLError Error, CLHttpRestStatus Status, JsonContracts.CredentialsSessionGetForKeyResponse Response)
            : base(Error, Status, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class CredentialsSessionDeleteResult : BaseCLHttpRestResult<JsonContracts.CredentialsSessionDeleteResponse>
    {
        // construct with all readonly properties
        internal CredentialsSessionDeleteResult(CLError Error, CLHttpRestStatus Status, JsonContracts.CredentialsSessionDeleteResponse Response)
            : base(Error, Status, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxUpdateExtendedMetadataResult : BaseCLHttpRestResult<JsonContracts.SyncboxResponse>
    {
        // construct with all readonly properties
        internal SyncboxUpdateExtendedMetadataResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SyncboxResponse Response)
            : base(Error, Status, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class LinkDeviceFirstTimeResult : BaseCLHttpRestResult<JsonContracts.LinkDeviceFirstTimeResponse>
    {
        // construct with all readonly properties
        internal LinkDeviceFirstTimeResult(CLError Error, CLHttpRestStatus Status, JsonContracts.LinkDeviceFirstTimeResponse Response)
            : base(Error, Status, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class LinkDeviceResult : BaseCLHttpRestResult<JsonContracts.LinkDeviceResponse>
    {
        // construct with all readonly properties
        internal LinkDeviceResult(CLError Error, CLHttpRestStatus Status, JsonContracts.LinkDeviceResponse Response)
            : base(Error, Status, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class UnlinkDeviceResult : BaseCLHttpRestResult<JsonContracts.UnlinkDeviceResponse>
    {
        // construct with all readonly properties
        internal UnlinkDeviceResult(CLError Error, CLHttpRestStatus Status, JsonContracts.UnlinkDeviceResponse Response)
            : base(Error, Status, Response) { }
    }

    #region UpdateSyncboxQuota (deprecated)
    ///// <summary>
    ///// Holds result properties
    ///// </summary>
    //public sealed class UpdateSyncboxQuotaResult : BaseCLHttpRestResult<JsonContracts.SyncboxHolder>
    //{
    //    // construct with all readonly properties
    //    internal UpdateSyncboxQuotaResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SyncboxHolder Response)
    //        : base(Error, Status, Response) { }
    //}
    #endregion

    #region SyncboxUpdate
    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxUpdateFriendlyNameResult : BaseCLHttpRestResult<JsonContracts.SyncboxResponse>
    {
        // construct with all readonly properties
        internal SyncboxUpdateFriendlyNameResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SyncboxResponse Response)
            : base(Error, Status, Response) { }
    }
    #endregion

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxUpdateStoragePlanResult : BaseCLHttpRestResult<JsonContracts.SyncboxUpdateStoragePlanResponse>
    {
        // construct with all readonly properties
        internal SyncboxUpdateStoragePlanResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SyncboxUpdateStoragePlanResponse Response)
            : base(Error, Status, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxDeleteResult : BaseCLHttpRestResult<JsonContracts.SyncboxDeleteResponse>
    {
        // construct with all readonly properties
        internal SyncboxDeleteResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SyncboxDeleteResponse Response)
            : base(Error, Status, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxStatusResult : BaseCLHttpRestResult<JsonContracts.SyncboxStatusResponse>
    {
        // construct with all readonly properties
        internal SyncboxStatusResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SyncboxStatusResponse Response)
            : base(Error, Status, Response) { }
    }
}