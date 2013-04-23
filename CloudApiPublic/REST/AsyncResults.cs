﻿// 
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

    //// GetUsedBytes is deprecated
    //
    ///// <summary>
    ///// Holds result properties
    ///// </summary>
    //public sealed class GetUsedBytesResult : BaseCLHttpRestResult<JsonContracts.UsedBytes>
    //{
    //    // construct with all readonly properties
    //    internal GetUsedBytesResult(CLError Error, CLHttpRestStatus Status, JsonContracts.UsedBytes Response)
    //        : base(Error, Status, Response) { }
    //}

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
    public sealed class GetPicturesResult : BaseCLHttpRestResult<JsonContracts.Pictures>
    {
        // construct with all readonly properties
        internal GetPicturesResult(CLError Error, CLHttpRestStatus Status, JsonContracts.Pictures Response)
            : base(Error, Status, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetVideosResult : BaseCLHttpRestResult<JsonContracts.Videos>
    {
        // construct with all readonly properties
        internal GetVideosResult(CLError Error, CLHttpRestStatus Status, JsonContracts.Videos Response)
            : base(Error, Status, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetAudiosResult : BaseCLHttpRestResult<JsonContracts.Audios>
    {
        // construct with all readonly properties
        internal GetAudiosResult(CLError Error, CLHttpRestStatus Status, JsonContracts.Audios Response)
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
    public sealed class GetRecentsResult : BaseCLHttpRestResult<JsonContracts.Recents>
    {
        // construct with all readonly properties
        internal GetRecentsResult(CLError Error, CLHttpRestStatus Status, JsonContracts.Recents Response)
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
    public sealed class SyncboxListResult : BaseCLHttpRestResult<JsonContracts.SyncboxListResponse>
    {
        // construct with all readonly properties
        internal SyncboxListResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SyncboxListResponse Response)
            : base(Error, Status, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class ListSessionsResult : BaseCLHttpRestResult<JsonContracts.ListSessionsResponse>
    {
        // construct with all readonly properties
        internal ListSessionsResult(CLError Error, CLHttpRestStatus Status, JsonContracts.ListSessionsResponse Response)
            : base(Error, Status, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SessionCreateResult : BaseCLHttpRestResult<JsonContracts.SessionCreateResponse>
    {
        // construct with all readonly properties
        internal SessionCreateResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SessionCreateResponse Response)
            : base(Error, Status, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SessionShowResult : BaseCLHttpRestResult<JsonContracts.SessionShowResponse>
    {
        // construct with all readonly properties
        internal SessionShowResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SessionShowResponse Response)
            : base(Error, Status, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SessionDeleteResult : BaseCLHttpRestResult<JsonContracts.SessionDeleteResponse>
    {
        // construct with all readonly properties
        internal SessionDeleteResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SessionDeleteResponse Response)
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