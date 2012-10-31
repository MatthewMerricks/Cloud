//
// EventIdAndCompletionProcessor.cs
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
        private long _eventId;

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
        private ISyncDataObject _syncData;

        public ISyncSettings SyncSettings
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
        private ISyncSettings _syncSettings;

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
        private string _tempDownloadFolderPath;

        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private bool _isValid;

        public EventIdAndCompletionProcessor(long EventId, ISyncDataObject syncData, ISyncSettings syncSettings, string TempDownloadFolderPath = null)
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
            this._isValid = true;
            this._tempDownloadFolderPath = TempDownloadFolderPath;
        }
    }
}