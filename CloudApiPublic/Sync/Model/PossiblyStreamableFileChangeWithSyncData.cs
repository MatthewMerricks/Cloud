//
// PossiblyStreamableFileChangeWithSyncData.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Interfaces;
using CloudApiPublic.Model;
using CloudApiPublic.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Sync.Model
{
    internal struct PossiblyStreamableFileChangeWithSyncData
    {
        public PossiblyStreamableFileChange FileChange
        {
            get
            {
                if (!_fileChange.IsValid
                    || !IsValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableFileChangeWithSyncData");
                }
                return _fileChange;
            }
        }
        private PossiblyStreamableFileChange _fileChange;

        public ProcessingQueuesTimer DownloadErrorTimer
        {
            get
            {
                if (!_fileChange.IsValid
                    || !IsValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableFileChangeWithSyncData");
                }
                return _downloadErrorTimer;
            }
        }
        private ProcessingQueuesTimer _downloadErrorTimer;

        public ISyncDataObject SyncData
        {
            get
            {
                if (!_fileChange.IsValid
                    || !IsValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableFileChangeWithSyncData");
                }
                return _syncData;
            }
        }
        private ISyncDataObject _syncData;

        public ISyncSettingsAdvanced SyncSettings
        {
            get
            {
                if (!_fileChange.IsValid
                    || !IsValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableFileChangeWithSyncData");
                }
                return _syncSettings;
            }
        }
        private ISyncSettingsAdvanced _syncSettings;

        public string TempDownloadFolderPath
        {
            get
            {
                if (!_fileChange.IsValid
                    || !IsValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableFileChangeWithSyncData");
                }
                return _tempDownloadFolderPath;
            }
        }
        private string _tempDownloadFolderPath;

        public Nullable<Guid> TempDownloadFileId
        {
            get
            {
                if (!_fileChange.IsValid
                    || !IsValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableFileChangeWithSyncData");
                }
                return _tempDownloadFileId;
            }
        }
        private Nullable<Guid> _tempDownloadFileId;

        public byte MaxNumberOfFailureRetries
        {
            get
            {
                if (!_fileChange.IsValid
                    || !IsValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableFileChangeWithSyncData");
                }
                return _maxNumberOfFailureRetries;
            }
        }
        private byte _maxNumberOfFailureRetries;

        public byte MaxNumberOfNotFounds
        {
            get
            {
                if (!_fileChange.IsValid
                    || !IsValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableFileChangeWithSyncData");
                }
                return _maxNumberOfNotFounds;
            }
        }
        private byte _maxNumberOfNotFounds;

        public Queue<FileChange> FailedChangesQueue
        {
            get
            {
                if (!_fileChange.IsValid
                    || !IsValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableFileChangeWithSyncData");
                }
                return _failedChangesQueue;
            }
        }
        private Queue<FileChange> _failedChangesQueue;

        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private bool _isValid;

        public PossiblyStreamableFileChangeWithSyncData(Queue<FileChange> FailedChangesQueue, byte MaxNumberOfFailureRetries, byte MaxNumberOfNotFounds, ProcessingQueuesTimer DownloadErrorTimer, PossiblyStreamableFileChange FileChange, ISyncDataObject SyncData, ISyncSettingsAdvanced SyncSettings, string TempDownloadFolderPath = null, Nullable<Guid> TempDownloadFileId = null)
        {
            if (SyncData == null)
            {
                throw new NullReferenceException("SyncData cannot be null");
            }
            if (SyncSettings == null)
            {
                throw new NullReferenceException("SyncSettings cannot be null");
            }
            if (DownloadErrorTimer == null)
            {
                throw new NullReferenceException("DownloadErrorTimer cannot be null");
            }
            if (FailedChangesQueue == null)
            {
                throw new NullReferenceException("FailedChangesQueue cannot be null");
            }

            this._fileChange = FileChange;
            this._syncData = SyncData;
            this._syncSettings = SyncSettings;
            this._tempDownloadFolderPath = TempDownloadFolderPath;
            this._tempDownloadFileId = TempDownloadFileId;
            this._isValid = true;
            this._downloadErrorTimer = DownloadErrorTimer;
            this._maxNumberOfFailureRetries = MaxNumberOfFailureRetries;
            this._maxNumberOfNotFounds = MaxNumberOfNotFounds;
            this._failedChangesQueue = FailedChangesQueue;
        }
    }
}