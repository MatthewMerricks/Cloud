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
        public T Result
        {
            get
            {
                return _result;
            }
        }
        private readonly T _result;

        // construct with all readonly properties
        protected internal BaseCLHttpRestResult(CLError Error, CLHttpRestStatus Status, T Result)
            : base(Error, Status)
        {
            this._result = Result;
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
        // construct with all readonly properties
        internal UploadFileResult(CLError Error, CLHttpRestStatus Status, string Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetMetadataResult : BaseCLHttpRestResult<JsonContracts.Metadata>
    {
        // construct with all readonly properties
        internal GetMetadataResult(CLError Error, CLHttpRestStatus Status, JsonContracts.Metadata Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetAllPendingResult : BaseCLHttpRestResult<JsonContracts.PendingResponse>
    {
        // construct with all readonly properties
        internal GetAllPendingResult(CLError Error, CLHttpRestStatus Status, JsonContracts.PendingResponse Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class PostFileChangeResult : BaseCLHttpRestResult<JsonContracts.Event>
    {
        // construct with all readonly properties
        internal PostFileChangeResult(CLError Error, CLHttpRestStatus Status, JsonContracts.Event Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class UndoDeletionFileChangeResult : BaseCLHttpRestResult<JsonContracts.Event>
    {
        // construct with all readonly properties
        internal UndoDeletionFileChangeResult(CLError Error, CLHttpRestStatus Status, JsonContracts.Event Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetFileVersionsResult : BaseCLHttpRestResult<JsonContracts.FileVersion[]>
    {
        // construct with all readonly properties
        internal GetFileVersionsResult(CLError Error, CLHttpRestStatus Status, JsonContracts.FileVersion[] Result)
            : base(Error, Status, Result) { }
    }

    //// GetUsedBytes is deprecated
    //
    ///// <summary>
    ///// Holds result properties
    ///// </summary>
    //public sealed class GetUsedBytesResult : BaseCLHttpRestResult<JsonContracts.UsedBytes>
    //{
    //    // construct with all readonly properties
    //    internal GetUsedBytesResult(CLError Error, CLHttpRestStatus Status, JsonContracts.UsedBytes Result)
    //        : base(Error, Status, Result) { }
    //}

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class CopyFileResult : BaseCLHttpRestResult<JsonContracts.Event>
    {
        // construct with all readonly properties
        internal CopyFileResult(CLError Error, CLHttpRestStatus Status, JsonContracts.Event Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetPicturesResult : BaseCLHttpRestResult<JsonContracts.Pictures>
    {
        // construct with all readonly properties
        internal GetPicturesResult(CLError Error, CLHttpRestStatus Status, JsonContracts.Pictures Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetVideosResult : BaseCLHttpRestResult<JsonContracts.Videos>
    {
        // construct with all readonly properties
        internal GetVideosResult(CLError Error, CLHttpRestStatus Status, JsonContracts.Videos Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetAudiosResult : BaseCLHttpRestResult<JsonContracts.Audios>
    {
        // construct with all readonly properties
        internal GetAudiosResult(CLError Error, CLHttpRestStatus Status, JsonContracts.Audios Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetArchivesResult : BaseCLHttpRestResult<JsonContracts.Archives>
    {
        // construct with all readonly properties
        internal GetArchivesResult(CLError Error, CLHttpRestStatus Status, JsonContracts.Archives Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetRecentsResult : BaseCLHttpRestResult<JsonContracts.Recents>
    {
        // construct with all readonly properties
        internal GetRecentsResult(CLError Error, CLHttpRestStatus Status, JsonContracts.Recents Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetSyncBoxUsageResult : BaseCLHttpRestResult<JsonContracts.SyncBoxUsage>
    {
        // construct with all readonly properties
        internal GetSyncBoxUsageResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SyncBoxUsage Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetFolderHierarchyResult : BaseCLHttpRestResult<JsonContracts.Folders>
    {
        // construct with all readonly properties
        internal GetFolderHierarchyResult(CLError Error, CLHttpRestStatus Status, JsonContracts.Folders Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetFolderContentsResult : BaseCLHttpRestResult<JsonContracts.FolderContents>
    {
        // construct with all readonly properties
        internal GetFolderContentsResult(CLError Error, CLHttpRestStatus Status, JsonContracts.FolderContents Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class PurgePendingResult : BaseCLHttpRestResult<JsonContracts.PendingResponse>
    {
        // construct with all readonly properties
        internal PurgePendingResult(CLError Error, CLHttpRestStatus Status, JsonContracts.PendingResponse Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class AddSyncBoxOnServerResult : BaseCLHttpRestResult<JsonContracts.SyncBoxHolder>
    {
        // construct with all readonly properties
        internal AddSyncBoxOnServerResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SyncBoxHolder Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class ListSyncBoxesResult : BaseCLHttpRestResult<JsonContracts.ListSyncBoxes>
    {
        // construct with all readonly properties
        internal ListSyncBoxesResult(CLError Error, CLHttpRestStatus Status, JsonContracts.ListSyncBoxes Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class ListPlansResult : BaseCLHttpRestResult<JsonContracts.ListPlansResponse>
    {
        // construct with all readonly properties
        internal ListPlansResult(CLError Error, CLHttpRestStatus Status, JsonContracts.ListPlansResponse Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class ListSessionsResult : BaseCLHttpRestResult<JsonContracts.ListSessionsResponse>
    {
        // construct with all readonly properties
        internal ListSessionsResult(CLError Error, CLHttpRestStatus Status, JsonContracts.ListSessionsResponse Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SessionCreateResult : BaseCLHttpRestResult<JsonContracts.SessionCreateResponse>
    {
        // construct with all readonly properties
        internal SessionCreateResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SessionCreateResponse Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SessionShowResult : BaseCLHttpRestResult<JsonContracts.SessionShowResponse>
    {
        // construct with all readonly properties
        internal SessionShowResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SessionShowResponse Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SessionDeleteResult : BaseCLHttpRestResult<JsonContracts.SessionDeleteResponse>
    {
        // construct with all readonly properties
        internal SessionDeleteResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SessionDeleteResponse Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncBoxUpdateExtendedMetadataResult : BaseCLHttpRestResult<JsonContracts.SyncBoxHolder>
    {
        // construct with all readonly properties
        internal SyncBoxUpdateExtendedMetadataResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SyncBoxHolder Result)
            : base(Error, Status, Result) { }
    }

    #region UpdateSyncBoxQuota (deprecated)
    ///// <summary>
    ///// Holds result properties
    ///// </summary>
    //public sealed class UpdateSyncBoxQuotaResult : BaseCLHttpRestResult<JsonContracts.SyncBoxHolder>
    //{
    //    // construct with all readonly properties
    //    internal UpdateSyncBoxQuotaResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SyncBoxHolder Result)
    //        : base(Error, Status, Result) { }
    //}
    #endregion

    #region SyncBoxUpdate
    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncBoxUpdateResult : BaseCLHttpRestResult<JsonContracts.SyncBoxHolder>
    {
        // construct with all readonly properties
        internal SyncBoxUpdateResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SyncBoxHolder Result)
            : base(Error, Status, Result) { }
    }
    #endregion

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncBoxUpdatePlanResult : BaseCLHttpRestResult<JsonContracts.SyncBoxUpdatePlanResponse>
    {
        // construct with all readonly properties
        internal SyncBoxUpdatePlanResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SyncBoxUpdatePlanResponse Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class DeleteSyncBoxResult : BaseCLHttpRestResult<JsonContracts.SyncBoxHolder>
    {
        // construct with all readonly properties
        internal DeleteSyncBoxResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SyncBoxHolder Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetSyncBoxStatusResult : BaseCLHttpRestResult<JsonContracts.SyncBoxHolder>
    {
        // construct with all readonly properties
        internal GetSyncBoxStatusResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SyncBoxHolder Result)
            : base(Error, Status, Result) { }
    }
}