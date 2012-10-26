//
// PossiblyStreamableFileChangeWithSyncData.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Interfaces;
using CloudApiPublic.Model;
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

        public ISyncSettings SyncSettings
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
        private ISyncSettings _syncSettings;

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

        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private bool _isValid;

        public PossiblyStreamableFileChangeWithSyncData(PossiblyStreamableFileChange FileChange, ISyncDataObject SyncData, ISyncSettings SyncSettings, string TempDownloadFolderPath = null, Nullable<Guid> TempDownloadFileId = null)
        {
            if (SyncData == null)
            {
                throw new NullReferenceException("SyncData cannot be null");
            }
            if (SyncSettings == null)
            {
                throw new NullReferenceException("SyncSettings cannot be null");
            }

            this._fileChange = FileChange;
            this._syncData = SyncData;
            this._syncSettings = SyncSettings;
            this._tempDownloadFolderPath = TempDownloadFolderPath;
            this._tempDownloadFileId = TempDownloadFileId;
            this._isValid = true;
        }
    }
}