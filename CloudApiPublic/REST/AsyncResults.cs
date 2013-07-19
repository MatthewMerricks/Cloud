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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
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
    [Obfuscation(Exclude = true)]
    public sealed class DownloadFileResult : BaseCLHttpRestResult
    {
        // construct with all readonly properties
        internal DownloadFileResult(CLError Error)
            : base(Error) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    [Obfuscation(Exclude = true)]
    public sealed class UploadFileResult : BaseCLHttpRestResult<string>
    {
        /// <summary>
        /// Determines whether upload failed due to a mismatched hash detected during transfer. This is caused by the file being modified during upload.
        /// </summary>
        public bool HashMismatchFound {
            get
            {
                return _hashMismatchFound;
            }
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
    [Obfuscation(Exclude = true)]
    public sealed class GetMetadataResult : BaseCLHttpRestResult<JsonContracts.SyncboxMetadataResponse>
    {
        // construct with all readonly properties
        internal GetMetadataResult(CLError Error, JsonContracts.SyncboxMetadataResponse Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    [Obfuscation(Exclude = true)]
    public sealed class GetAllPendingResult : BaseCLHttpRestResult<JsonContracts.PendingResponse>
    {
        // construct with all readonly properties
        internal GetAllPendingResult(CLError Error, JsonContracts.PendingResponse Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    [Obfuscation(Exclude = true)]
    public sealed class FileChangeResult : BaseCLHttpRestResult<JsonContracts.FileChangeResponse>
    {
        // construct with all readonly properties
        internal FileChangeResult(CLError Error, JsonContracts.FileChangeResponse Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    [Obfuscation(Exclude = true)]
    public sealed class UndoDeletionFileChangeResult : BaseCLHttpRestResult<JsonContracts.FileChangeResponse>
    {
        // construct with all readonly properties
        internal UndoDeletionFileChangeResult(CLError Error, JsonContracts.FileChangeResponse Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    [Obfuscation(Exclude = true)]
    public sealed class GetFileVersionsResult : BaseCLHttpRestResult<JsonContracts.FileVersions>
    {
        // construct with all readonly properties
        internal GetFileVersionsResult(CLError Error, JsonContracts.FileVersions Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    [Obfuscation(Exclude = true)]
    public sealed class CopyFileResult : BaseCLHttpRestResult<JsonContracts.FileChangeResponse>
    {
        // construct with all readonly properties
        internal CopyFileResult(CLError Error, JsonContracts.FileChangeResponse Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    [Obfuscation(Exclude = true)]
    public sealed class PurgePendingResult : BaseCLHttpRestResult<JsonContracts.PendingResponse>
    {
        // construct with all readonly properties
        internal PurgePendingResult(CLError Error, JsonContracts.PendingResponse Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
    public sealed class FileItemDownloadFileResult
    {
        /// <summary>
        /// The full path of the downloaded file in the tempoarary download director.
        /// </summary>
        public string FullPathTempDownloadedFile
        {
            get
            {
                return _fullPathTempDownloadedFile;
            }
        }
        private readonly string _fullPathTempDownloadedFile;

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
        internal FileItemDownloadFileResult(CLError error, string fullPathTempDownloadedFile)
        {
            this._error = error;
            this._fullPathTempDownloadedFile = fullPathTempDownloadedFile;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
    public sealed class ListStoragePlanResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLStoragePlan StoragePlan
        {
            get
            {
                return _storagePlan;
            }
        }
        private readonly CLStoragePlan _storagePlan;

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
        internal ListStoragePlanResult(CLError error, CLStoragePlan storagePlan)
        {
            this._error = Error;
            this._storagePlan = storagePlan;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    [Obfuscation(Exclude = true)]
    public sealed class CredentialsListSessionsResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLCredentials[] ActiveSessionCredentials
        {
            get
            {
                return _activeSessionCredentials;
            }
        }
        private readonly CLCredentials[] _activeSessionCredentials;

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
        internal CredentialsListSessionsResult(CLError error, CLCredentials[] activeSessionCredentials)
        {
            this._error = error;
            this._activeSessionCredentials = activeSessionCredentials;
        }
    }
    
    /// <summary>
    /// Holds result properties
    /// </summary>
    [Obfuscation(Exclude = true)]
    public sealed class CredentialsIsValidResult
    {
        /// <summary>
        /// The result.  True: The session credentials have not expired.
        /// </summary>
        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private readonly bool _isValid;

        /// <summary>
        /// The UTC time when the session will expire.
        /// </summary>
        public DateTime ExpirationDate
        {
            get
            {
                return _expirationDate;
            }
        }
        private readonly DateTime _expirationDate;

        /// <summary>
        /// The list of syncbox ids which are valid for this session, or null for all syncbox ids.
        /// </summary>
        public ReadOnlyCollection<long> SyncboxIds
        {
            get
            {
                return _syncboxIds;
            }
        }
        private readonly ReadOnlyCollection<long> _syncboxIds;
        
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
        internal CredentialsIsValidResult(CLError error, bool isValid, DateTime ExpirationDate, ReadOnlyCollection<long> SyncboxIds)
        {
            this._error = error;
            this._isValid = isValid;
            this._expirationDate = ExpirationDate;
            this._syncboxIds = SyncboxIds;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    [Obfuscation(Exclude = true)]
    public sealed class CredentialsSessionCreateResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLCredentials SessionCredentials
        {
            get
            {
                return _sessionCredentials;
            }
        }
        private readonly CLCredentials _sessionCredentials;

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
        internal CredentialsSessionCreateResult(CLError error, CLCredentials sessionCredentials)
        {
            this._error = error;
            this._sessionCredentials = sessionCredentials;
        }
    }


    /// <summary>
    /// Holds result properties
    /// </summary>
    [Obfuscation(Exclude = true)]
    public sealed class CredentialsSessionCreateSeperatedResult
    {
        /// <summary>
        /// The key from the result returned from the server
        /// </summary>
        public string SessionCredentialsKey
        {
            get
            {
                return _sessionCredentialsKey;
            }
        }
        private readonly string _sessionCredentialsKey;
        
        /// <summary>
        /// The secret from the result returned from the server
        /// </summary>
        public string SessionCredentialsSecret
        {
            get
            {
                return _sessionCredentialsSecret;
            }
        }
        private readonly string _sessionCredentialsSecret;

        /// <summary>
        /// The token from the result returned from the server
        /// </summary>
        public string SessionCredentialsToken
        {
            get
            {
                return _sessionCredentialsToken;
            }
        }
        private readonly string _sessionCredentialsToken;

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
        internal CredentialsSessionCreateSeperatedResult(CLError error, string sessionCredentialsKey, string sessionCredentialsSecret, string sessionCredentialsToken)
        {
            this._error = error;
            this._sessionCredentialsKey = sessionCredentialsKey;
            this._sessionCredentialsSecret = sessionCredentialsSecret;
            this._sessionCredentialsToken = sessionCredentialsToken;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    [Obfuscation(Exclude = true)]
    public sealed class CredentialsSessionGetForKeyResult
    {
        /// <summary>
        /// The result returned from the server
        /// </summary>
        public CLCredentials SessionCredentials
        {
            get
            {
                return _sessionCredentials;
            }
        }
        private readonly CLCredentials _sessionCredentials;

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
        internal CredentialsSessionGetForKeyResult(CLError error, CLCredentials sessionCredentials)
        {
            this._error = error;
            this._sessionCredentials = sessionCredentials;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
    public sealed class FileItemDownloadImageResult
    {
        /// <summary>
        /// The returned file or folder item.
        /// </summary>
        public Stream Stream
        {
            get
            {
                return _stream;
            }
        }
        private readonly Stream _stream;

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
        internal FileItemDownloadImageResult(CLError error, Stream stream)
        {
            this._error = error;
            this._stream = stream;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    [Obfuscation(Exclude = true)]
    public sealed class SyncboxGetItemAtItemUidResult
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
        internal SyncboxGetItemAtItemUidResult(CLError error, CLFileItem item)
        {
            this._error = error;
            this._item = item;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
    public sealed class SyncboxHierarchyOfFolderAtPathResult
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
        internal SyncboxHierarchyOfFolderAtPathResult(CLError error, CLFileItem[] items)
        {
            this._error = error;
            this._items = items;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    [Obfuscation(Exclude = true)]
    public sealed class SyncboxItemsForFolderItemResult
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
        internal SyncboxItemsForFolderItemResult(CLError error, CLFileItem[] items)
        {
            this._error = error;
            this._items = items;
        }
    }

    [Obfuscation(Exclude = true)]
    internal sealed class ContentsUnderFolderUidResult : BaseCLHttpRestResult<JsonContracts.SyncboxFolderContentsResponse>
    {
        internal ContentsUnderFolderUidResult(CLError Error, JsonContracts.SyncboxFolderContentsResponse Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    [Obfuscation(Exclude = true)]
    public sealed class SyncboxHierarchyOfFolderAtFolderItemResult
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
        internal SyncboxHierarchyOfFolderAtFolderItemResult(CLError error, CLFileItem[] items)
        {
            this._error = error;
            this._items = items;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
    public sealed class SyncboxPurgePendingFilesResult
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
        internal SyncboxPurgePendingFilesResult(CLError overallError)
        {
            this._overallError = overallError;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
    public sealed class SyncboxRecentFilesSinceDateResult
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
        internal SyncboxRecentFilesSinceDateResult(CLError overallError, CLFileItem[] items)
        {
            this._overallError = overallError;
            this._items = items;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
    public sealed class SyncboxModifyFilesResult
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
        internal SyncboxModifyFilesResult(CLError overallError)
        {
            this._overallError = overallError;
        }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    [Obfuscation(Exclude = true)]
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
    [Obfuscation(Exclude = true)]
    public sealed class SyncboxUpdateExtendedMetadataResult : BaseCLHttpRestResult<JsonContracts.SyncboxResponse>
    {
        // construct with all readonly properties
        internal SyncboxUpdateExtendedMetadataResult(CLError Error, JsonContracts.SyncboxResponse Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    [Obfuscation(Exclude = true)]
    public sealed class LinkDeviceFirstTimeResult : BaseCLHttpRestResult<JsonContracts.LinkDeviceFirstTimeResponse>
    {
        // construct with all readonly properties
        internal LinkDeviceFirstTimeResult(CLError Error, JsonContracts.LinkDeviceFirstTimeResponse Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    [Obfuscation(Exclude = true)]
    public sealed class LinkDeviceResult : BaseCLHttpRestResult<JsonContracts.LinkDeviceResponse>
    {
        // construct with all readonly properties
        internal LinkDeviceResult(CLError Error, JsonContracts.LinkDeviceResponse Response)
            : base(Error, Response) { }
    }

    /// <summary>
    /// Holds result properties
    /// </summary>
    [Obfuscation(Exclude = true)]
    public sealed class UnlinkDeviceResult : BaseCLHttpRestResult<JsonContracts.UnlinkDeviceResponse>
    {
        // construct with all readonly properties
        internal UnlinkDeviceResult(CLError Error, JsonContracts.UnlinkDeviceResponse Response)
            : base(Error, Response) { }
    }
}