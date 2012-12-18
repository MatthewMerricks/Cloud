﻿//
// SyncSettings.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Interfaces;
using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Sync
{
    /// <summary>
    /// Simple implementation of ISyncSettings
    /// </summary>
    public sealed class SyncSettings : ISyncSettings
    {
        public string Udid
        {
            get
            {
                return _udid;
            }
        }
        private string _udid;

        public string ApplicationKey
        {
            get
            {
                return _applicationKey;
            }
        }
        private string _applicationKey;

        public string ApplicationSecret
        {
            get
            {
                return _applicationSecret;
            }
        }
        private string _applicationSecret;

        public string SyncBoxId
        {
            get
            {
                return _syncBoxId;
            }
        }
        private string _syncBoxId;

        public string ClientVersion
        {
            get
            {
                return _clientVersion;
            }
        }
        private string _clientVersion = null;

        public string SyncRoot
        {
            get
            {
                return _cloudRoot;
            }
        }
        private string _cloudRoot = null;

        public SyncSettings(
                    string udid,
                    string applicationKey,
                    string applicationSecret,
                    string syncBoxId,
                    string clientVersion,
                    string cloudRoot)
        {
            this._udid = udid;
            this._applicationKey = applicationKey;
            this._applicationSecret = applicationSecret;
            this._syncBoxId = syncBoxId;
            this._clientVersion = clientVersion;
            this._cloudRoot = cloudRoot;
        }
    }

    internal sealed class AdvancedSyncSettings : ISyncSettingsAdvanced
    {
        /// <summary>
        /// Set to true if errors should be logged.
        /// </summary>
        public bool LogErrors
        {
            get
            {
                return _logErrors;
            }
        }
        private bool _logErrors = false;

        /// <summary>
        /// Determines the information that is traced.
        /// </summary>
        public TraceType TraceType
        {
            get
            {
                return _traceType;
            }
        }
        private TraceType _traceType = TraceType.NotEnabled;

        /// <summary>
        /// Only required if TraceType has any flags set (TraceType.NotEnabled means no flags are set).
        /// Provides the path to the directory that will hold the trace files.
        /// </summary>
        public string TraceLocation
        {
            get
            {
                return _traceLocation;
            }
        }
        private string _traceLocation;

        /// <summary>
        /// Set to true to exclude authorization information from the trace.
        /// </summary>
        public bool TraceExcludeAuthorization
        {
            get
            {
                return _traceExcludeAuthorization;
            }
        }
        private bool _traceExcludeAuthorization;

        /// <summary>
        /// Specify 1 for the only the most important traces.  Use a higher number for more detail.
        /// </summary>
        public int TraceLevel
        {
            get
            {
                return _traceLevel;
            }
        }
        private int _traceLevel;

        public string Udid
        {
            get
            {
                return _udid;
            }
        }
        private string _udid;

        public string ApplicationKey
        {
            get
            {
                return _applicationKey;
            }
        }
        private string _applicationKey;

        public string ApplicationSecret
        {
            get
            {
                return _applicationSecret;
            }
        }
        private string _applicationSecret;

        public string SyncBoxId
        {
            get
            {
                return _syncBoxId;
            }
        }
        private string _syncBoxId;

        /// <summary>
        /// If null, a precalculated value will be used based on the local, non-roaming user's application data in the Cloud subdirectory
        /// </summary>
        public string TempDownloadFolderFullPath
        {
            get
            {
                return _tempDownloadFolderFullPath;
            }
        }
        private string _tempDownloadFolderFullPath = null;

        public string ClientVersion
        {
            get
            {
                return _clientVersion;
            }
        }
        private string _clientVersion = null;

        public string DeviceName
        {
            get
            {
                return _deviceName;
            }
        }
        private string _deviceName = null; 

        public string SyncRoot
        {
            get
            {
                return _cloudRoot;
            }
        }
        private string _cloudRoot = null;

        public string DatabaseFolder
        {
            get
            {
                return _databaseFolder;
            }
        }
        private string _databaseFolder = null;

        public AdvancedSyncSettings(
                    bool logErrors,
                    TraceType traceType,
                    string traceLocation,
                    bool traceExcludeAuthorization,
                    int traceLevel,
                    string udid,
                    string applicationKey,
                    string applicationSecret,
                    string syncBoxId,
                    string tempDownloadFolderFullPath,
                    string clientVersion,
                    string deviceName,
                    string cloudRoot,
                    string databaseFolder)
        {
            this._logErrors = logErrors;
            this._traceType = traceType;
            this._traceLocation = traceLocation;
            this._traceExcludeAuthorization = traceExcludeAuthorization;
            this._traceLevel = traceLevel;
            this._udid = udid;
            this._applicationKey = applicationKey;
            this._applicationSecret = applicationSecret;
            this._syncBoxId = syncBoxId;
            this._tempDownloadFolderFullPath = tempDownloadFolderFullPath;
            this._clientVersion = clientVersion;
            this._deviceName = deviceName;
            this._cloudRoot = cloudRoot;
            this._databaseFolder = databaseFolder;
        }
    }

    internal static class SyncSettingsExtensions
    {
        public static AdvancedSyncSettings CopySettings(this ISyncSettingsAdvanced toCopy)
        {
            return new AdvancedSyncSettings(
                toCopy.LogErrors,
                toCopy.TraceType,
                toCopy.TraceLocation,
                toCopy.TraceExcludeAuthorization,
                toCopy.TraceLevel,
                toCopy.Udid,
                toCopy.ApplicationKey,
                toCopy.ApplicationSecret,
                toCopy.SyncBoxId,
                toCopy.TempDownloadFolderFullPath,
                toCopy.ClientVersion,
                toCopy.DeviceName,
                toCopy.SyncRoot,
                toCopy.DatabaseFolder);
        }

        public static AdvancedSyncSettings CopySettings(this ISyncSettings toCopy)
        {
            ISyncSettingsAdvanced advancedCopy = toCopy as ISyncSettingsAdvanced;
            if (advancedCopy == null)
            {
                return new AdvancedSyncSettings(
                    false,
                    TraceType.NotEnabled,
                    null,
                    true,
                    0,
                    toCopy.Udid,
                    toCopy.ApplicationKey,
                    toCopy.ApplicationSecret,
                    toCopy.SyncBoxId,
                    null,
                    toCopy.ClientVersion,
                    Helpers.GetComputerFriendlyName(),
                    toCopy.SyncRoot,
                    null);
            }
            else
            {
                return advancedCopy.CopySettings();
            }
        }

        public static AdvancedSyncSettings CopySettings(this IHttpSettingsAdvanced toCopy)
        {
            ISyncSettingsAdvanced syncCopy = toCopy as ISyncSettingsAdvanced;

            if (syncCopy == null)
            {
                return new AdvancedSyncSettings(
                    toCopy.LogErrors,
                    toCopy.TraceType,
                    toCopy.TraceLocation,
                    toCopy.TraceExcludeAuthorization,
                    toCopy.TraceLevel,
                    toCopy.Udid,
                    toCopy.ApplicationKey,
                    toCopy.ApplicationSecret,
                    toCopy.SyncBoxId,
                    null,
                    null,
                    Helpers.GetComputerFriendlyName(),
                    null,
                    null);
            }
            else
            {
                return syncCopy.CopySettings();
            }
        }

        public static AdvancedSyncSettings CopySettings(this IHttpSettings toCopy)
        {
            IHttpSettingsAdvanced advancedCopy = toCopy as IHttpSettingsAdvanced;
            if (advancedCopy == null)
            {
                ISyncSettings syncCopy = toCopy as ISyncSettings;
                if (syncCopy == null)
                {
                    return new AdvancedSyncSettings(
                        false,
                        TraceType.NotEnabled,
                        null,
                        true,
                        0,
                        toCopy.Udid,
                        toCopy.ApplicationKey,
                        toCopy.ApplicationSecret,
                        toCopy.SyncBoxId,
                        null,
                        null,
                        Helpers.GetComputerFriendlyName(),
                        null,
                        null);
                }
                else
                {
                    return syncCopy.CopySettings();
                }
            }
            else
            {
                return advancedCopy.CopySettings();
            }
        }
    }
}