//
// EventIdAndCompletionProcessor.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Interfaces;
using Cloud.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Sync.Model
{
    internal struct EventIdAndCompletionProcessor
    {
        public long EventId
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid EventIdAndCompletionProcessor");
                }
                return _eventId;
            }
        }
        private readonly long _eventId;

        public ISyncDataObject SyncData
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid EventIdAndCompletionProcessor");
                }
                return _syncData;
            }
        }
        private readonly ISyncDataObject _syncData;

        public ICLSyncSettingsAdvanced SyncSettings
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid EventIdAndCompletionProcessor");
                }
                return _syncSettings;
            }
        }
        private readonly ICLSyncSettingsAdvanced _syncSettings;

        public string TempDownloadFolderPath
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid EventIdAndCompletionProcessor");
                }
                return _tempDownloadFolderPath;
            }
        }
        private readonly string _tempDownloadFolderPath;

        public long SyncBoxId
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid EventIdAndCompletionProcessor");
                }
                return _syncBoxId;
            }
        }
        private readonly long _syncBoxId;

        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private readonly bool _isValid;

        public EventIdAndCompletionProcessor(long EventId, ISyncDataObject syncData, ICLSyncSettingsAdvanced syncSettings, long SyncBoxId, string TempDownloadFolderPath = null)
        {
            if (syncData == null)
            {
                throw new NullReferenceException("syncData cannot be null");
            }
            if (syncSettings == null)
            {
                throw new NullReferenceException("syncSettings cannot be null");
            }

            this._eventId = EventId;
            this._syncData = syncData;
            this._syncSettings = syncSettings;
            this._syncBoxId = SyncBoxId;
            this._isValid = true;
            this._tempDownloadFolderPath = TempDownloadFolderPath;
        }
    }
}