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
    public sealed class GetSyncboxUsageResult : BaseCLHttpRestResult<JsonContracts.SyncboxUsage>
    {
        // construct with all readonly properties
        internal GetSyncboxUsageResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SyncboxUsage Result)
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
    public sealed class AddSyncboxOnServerResult : BaseCLHttpRestResult<JsonContracts.SyncboxHolder>
    {
        // construct with all readonly properties
        internal AddSyncboxOnServerResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SyncboxHolder Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class ListSyncboxesResult : BaseCLHttpRestResult<JsonContracts.ListSyncboxes>
    {
        // construct with all readonly properties
        internal ListSyncboxesResult(CLError Error, CLHttpRestStatus Status, JsonContracts.ListSyncboxes Result)
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
    public sealed class SyncboxUpdateExtendedMetadataResult : BaseCLHttpRestResult<JsonContracts.SyncboxHolder>
    {
        // construct with all readonly properties
        internal SyncboxUpdateExtendedMetadataResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SyncboxHolder Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class LinkDeviceFirstTimeResult : BaseCLHttpRestResult<JsonContracts.LinkDeviceFirstTimeResponse>
    {
        // construct with all readonly properties
        internal LinkDeviceFirstTimeResult(CLError Error, CLHttpRestStatus Status, JsonContracts.LinkDeviceFirstTimeResponse Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class LinkDeviceResult : BaseCLHttpRestResult<JsonContracts.LinkDeviceResponse>
    {
        // construct with all readonly properties
        internal LinkDeviceResult(CLError Error, CLHttpRestStatus Status, JsonContracts.LinkDeviceResponse Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class UnlinkDeviceResult : BaseCLHttpRestResult<JsonContracts.UnlinkDeviceResponse>
    {
        // construct with all readonly properties
        internal UnlinkDeviceResult(CLError Error, CLHttpRestStatus Status, JsonContracts.UnlinkDeviceResponse Result)
            : base(Error, Status, Result) { }
    }

    #region UpdateSyncboxQuota (deprecated)
    ///// <summary>
    ///// Holds result properties
    ///// </summary>
    //public sealed class UpdateSyncboxQuotaResult : BaseCLHttpRestResult<JsonContracts.SyncboxHolder>
    //{
    //    // construct with all readonly properties
    //    internal UpdateSyncboxQuotaResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SyncboxHolder Result)
    //        : base(Error, Status, Result) { }
    //}
    #endregion

    #region SyncboxUpdate
    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxUpdateResult : BaseCLHttpRestResult<JsonContracts.SyncboxHolder>
    {
        // construct with all readonly properties
        internal SyncboxUpdateResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SyncboxHolder Result)
            : base(Error, Status, Result) { }
    }
    #endregion

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxUpdatePlanResult : BaseCLHttpRestResult<JsonContracts.SyncboxUpdatePlanResponse>
    {
        // construct with all readonly properties
        internal SyncboxUpdatePlanResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SyncboxUpdatePlanResponse Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class DeleteSyncboxResult : BaseCLHttpRestResult<JsonContracts.SyncboxHolder>
    {
        // construct with all readonly properties
        internal DeleteSyncboxResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SyncboxHolder Result)
            : base(Error, Status, Result) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetSyncboxStatusResult : BaseCLHttpRestResult<JsonContracts.SyncboxHolder>
    {
        // construct with all readonly properties
        internal GetSyncboxStatusResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SyncboxHolder Result)
            : base(Error, Status, Result) { }
    }
}