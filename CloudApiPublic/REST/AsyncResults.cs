// 
// AsyncResults.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.CLSync;
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
    public sealed class GetFileVersionsResult : BaseCLHttpRestResult<JsonContracts.FileVersions>
    {
        // construct with all readonly properties
        internal GetFileVersionsResult(CLError Error, JsonContracts.FileVersions Response)
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
    public sealed class GetArchivesResult : BaseCLHttpRestResult<JsonContracts.Archives>
    {
        // construct with all readonly properties
        internal GetArchivesResult(CLError Error, JsonContracts.Archives Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class GetFolderHierarchyResult : BaseCLHttpRestResult<JsonContracts.FoldersResponse>
    {
        // construct with all readonly properties
        internal GetFolderHierarchyResult(CLError Error, JsonContracts.FoldersResponse Response)
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
        public CLSyncbox Syncbox
        {
            get
            {
                return _syncbox;
            }
        }
        private readonly CLSyncbox _syncbox;

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
        internal CreateSyncboxResult(CLError error, CLSyncbox syncbox)
        {
            this._error = error;
            this._syncbox = syncbox;
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
        public CLSyncbox Syncbox
        {
            get
            {
                return _syncbox;
            }
        }
        private readonly CLSyncbox _syncbox;

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
        internal SyncboxAllocAndInitResult(CLError error, CLSyncbox syncbox)
        {
            this._error = error;
            this._syncbox = syncbox;
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
        public CLStoragePlan [] StoragePlans
        {
            get
            {
                return _storagePlans;
            }
        }
        private readonly CLStoragePlan [] _storagePlans;

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
        internal ListStoragePlansResult(CLError error, CLStoragePlan [] storagePlans)
        {
            this._error = Error;
            this._storagePlans = storagePlans;
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
        public CLCredentials[] Credentials
        {
            get
            {
                return _credentials;
            }
        }
        private readonly CLCredentials[] _credentials;

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
        internal CredentialsListSessionsResult(CLError error, CLCredentials[] credentials)
        {
            this._error = error;
            this._credentials = credentials;
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
        public CLCredentials Credentials
        {
            get
            {
                return _credentials;
            }
        }
        private readonly CLCredentials _credentials;

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
        internal CredentialsSessionCreateResult(CLError error, CLCredentials credentials)
        {
            this._error = error;
            this._credentials = credentials;
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
        public CLCredentials Credentials
        {
            get
            {
                return _credentials;
            }
        }
        private readonly CLCredentials _credentials;

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
        internal CredentialsSessionGetForKeyResult(CLError error, CLCredentials credentials)
        {
            this._error = error;
            this._credentials = credentials;
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
                return _fileItem;
            }
        }
        private readonly CLFileItem _fileItem;

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
        internal SyncboxRenameFileResult(CLError error, CLFileItem _fileItem)
        {
            this._error = error;
            this._fileItem = _fileItem;
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
                return _fileItem;
            }
        }
        private readonly CLFileItem _fileItem;

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
        internal SyncboxMoveFileResult(CLError error, CLFileItem fileItem)
        {
            this._error = error;
            this._fileItem = fileItem;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxRenameFilesResult
    {
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
        internal SyncboxRenameFilesResult(CLError overallError)
        {
            this._overallError = overallError;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxUpdateFriendlyNameResult
    {
        /// <summary>
        /// Any overall error which may have occurred during communication
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
        internal SyncboxUpdateFriendlyNameResult(CLError error)
        {
            this._error = error;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxStatusResult
    {
        /// <summary>
        /// Any overall error which may have occurred during communication
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
        internal SyncboxStatusResult(CLError error)
        {
            this._error = error;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxUsageResult
    {
        /// <summary>
        /// Any overall error which may have occurred during communication
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
        internal SyncboxUsageResult(CLError error)
        {
            this._error = error;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxUpdateStoragePlanResult
    {
        /// <summary>
        /// Any overall error which may have occurred during communication
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
        internal SyncboxUpdateStoragePlanResult(CLError error)
        {
            this._error = error;
        }
    }


    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxGetItemAtPathResult
    {
        /// <summary>
        /// The returned file or folder item.
        /// </summary>
        public CLFileItem Item
        {
            get
            {
                return _item;
            }
        }
        private readonly CLFileItem _item;

        /// <summary>
        /// Any overall error which may have occurred during communication
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
        internal SyncboxGetItemAtPathResult(CLError error, CLFileItem item)
        {
            this._error = error;
            this._item = item;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxItemsAtPathResult
    {
        /// <summary>
        /// The returned file or folder item.
        /// </summary>
        public CLFileItem[] Items
        {
            get
            {
                return _items;
            }
        }
        private readonly CLFileItem[] _items;

        /// <summary>
        /// Any overall error which may have occurred during communication
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
        internal SyncboxItemsAtPathResult(CLError error, CLFileItem[] items)
        {
            this._error = error;
            this._items = items;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxGetCurrentStatusResult
    {
        /// <summary>
        /// Any overall error which may have occurred during communication
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
        internal SyncboxGetCurrentStatusResult(CLError error)
        {
            this._error = error;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxMoveFilesResult
    {
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
        internal SyncboxMoveFilesResult(CLError overallError)
        {
            this._overallError = overallError;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxDeleteFilesResult
    {
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
        internal SyncboxDeleteFilesResult(CLError overallError)
        {
            this._overallError = overallError;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxAllImageItemsResult
    {
        /// <summary>
        /// Any overall error which may have occurred during communication
        /// </summary>
        public CLFileItem[] Items
        {
            get
            {
                return _items;
            }
        }
        private readonly CLFileItem[] _items;

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
        internal SyncboxAllImageItemsResult(CLError overallError, CLFileItem[] items)
        {
            this._overallError = overallError;
            this._items = items;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxAllVideoItemsResult
    {
        /// <summary>
        /// The resulting file items.
        /// </summary>
        public CLFileItem[] Items
        {
            get
            {
                return _items;
            }
        }
        private readonly CLFileItem[] _items;

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
        internal SyncboxAllVideoItemsResult(CLError overallError, CLFileItem[] items)
        {
            this._overallError = overallError;
            this._items = items;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxAllAudioItemsResult
    {
        /// <summary>
        /// The resulting file items.
        /// </summary>
        public CLFileItem[] Items
        {
            get
            {
                return _items;
            }
        }
        private readonly CLFileItem[] _items;

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
        internal SyncboxAllAudioItemsResult(CLError overallError, CLFileItem[] items)
        {
            this._overallError = overallError;
            this._items = items;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxListResult
    {
        /// <summary>
        /// The returned syncboxes.
        /// </summary>
        public CLSyncbox[] ReturnedSyncboxes
        {
            get
            {
                return _returnedSyncboxes;
            }
        }
        private readonly CLSyncbox[] _returnedSyncboxes;

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
        internal SyncboxListResult(CLError overallError, CLSyncbox[] returnedSyncboxes)
        {
            this._overallError = overallError;
            this._returnedSyncboxes = returnedSyncboxes;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxAllDocumentItemsResult
    {
        /// <summary>
        /// The resulting file items.
        /// </summary>
        public CLFileItem[] Items
        {
            get
            {
                return _items;
            }
        }
        private readonly CLFileItem[] _items;

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
        internal SyncboxAllDocumentItemsResult(CLError overallError, CLFileItem[] items)
        {
            this._overallError = overallError;
            this._items = items;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxAllPresentationItemsResult
    {
        /// <summary>
        /// The resulting file items.
        /// </summary>
        public CLFileItem[] Items
        {
            get
            {
                return _items;
            }
        }
        private readonly CLFileItem[] _items;

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
        internal SyncboxAllPresentationItemsResult(CLError overallError, CLFileItem[] items)
        {
            this._overallError = overallError;
            this._items = items;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxAllTextItemsResult
    {
        /// <summary>
        /// The resulting file items.
        /// </summary>
        public CLFileItem[] Items
        {
            get
            {
                return _items;
            }
        }
        private readonly CLFileItem[] _items;

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
        internal SyncboxAllTextItemsResult(CLError overallError, CLFileItem[] items)
        {
            this._overallError = overallError;
            this._items = items;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxAllArchiveItemsResult
    {
        /// <summary>
        /// The resulting file items.
        /// </summary>
        public CLFileItem[] Items
        {
            get
            {
                return _items;
            }
        }
        private readonly CLFileItem[] _items;

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
        internal SyncboxAllArchiveItemsResult(CLError overallError, CLFileItem[] items)
        {
            this._overallError = overallError;
            this._items = items;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxRecentFilesResult
    {
        /// <summary>
        /// The resulting file items.
        /// </summary>
        public CLFileItem[] Items
        {
            get
            {
                return _items;
            }
        }
        private readonly CLFileItem[] _items;

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
        internal SyncboxRecentFilesResult(CLError overallError, CLFileItem[] items)
        {
            this._overallError = overallError;
            this._items = items;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxAllItemsOfTypesResult
    {
        /// <summary>
        /// The resulting file items.
        /// </summary>
        public CLFileItem[] Items
        {
            get
            {
                return _items;
            }
        }
        private readonly CLFileItem[] _items;

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
        internal SyncboxAllItemsOfTypesResult(CLError overallError, CLFileItem[] items)
        {
            this._overallError = overallError;
            this._items = items;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxDeleteResult
    {
        /// <summary>
        /// Any overall error which may have occurred during communication
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
        internal SyncboxDeleteResult(CLError error)
        {
            this._error = error;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxRenameFoldersResult
    {
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
        internal SyncboxRenameFoldersResult(CLError overallError)
        {
            this._overallError = overallError;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxMoveFoldersResult
    {
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
        internal SyncboxMoveFoldersResult(CLError overallError)
        {
            this._overallError = overallError;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxDeleteFoldersResult
    {
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
        internal SyncboxDeleteFoldersResult(CLError overallError)
        {
            this._overallError = overallError;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxAddFoldersResult
    {
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
        internal SyncboxAddFoldersResult(CLError overallError)
        {
            this._overallError = overallError;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class SyncboxAddFilesResult
    {
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
        internal SyncboxAddFilesResult(CLError overallError)
        {
            this._overallError = overallError;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    public sealed class CredentialsSessionDeleteResult
    {
        /// <summary>
        /// Any overall error which may have occurred during communication
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
        internal CredentialsSessionDeleteResult(CLError error)
        {
            this._error = error;
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

}