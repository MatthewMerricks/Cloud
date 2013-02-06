//
// CLSyncSettings.cs
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

namespace CloudApiPublic
{
    /// <summary>
    /// Simple implementation of ICLSyncSettings
    /// </summary>
    public sealed class CLSyncSettings : ICLSyncSettings
    {
        /// <summary>
        /// Full path to the directory to be synced (do not include a trailing slash except for a drive root)
        /// </summary>
        public string SyncRoot
        {
            get
            {
                return _syncRoot;
            }
        }
        private readonly string _syncRoot = null;

        /// <summary>
        /// Creates a simple implementation of ICLSyncSettings with only SyncRoot
        /// </summary>
        /// <param name="syncRoot">Full path to the directory to be synced (do not include a trailing slash except for a drive root)</param>
        public CLSyncSettings(
                    string syncRoot)
        {
            this._syncRoot = syncRoot;
        }
    }

    internal sealed class AdvancedSyncSettings : ICLSyncSettingsAdvanced
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
        private readonly bool _logErrors = false;

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
        private readonly TraceType _traceType = TraceType.NotEnabled;

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
        private readonly string _traceLocation;

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
        private readonly bool _traceExcludeAuthorization;

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
        private readonly int _traceLevel;

        public string DeviceId
        {
            get
            {
                return _deviceId;
            }
        }
        private readonly string _deviceId;

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
        private readonly string _tempDownloadFolderFullPath = null;

        public string ClientVersion
        {
            get
            {
                return _clientVersion;
            }
        }
        private readonly string _clientVersion = null;

        public string FriendlyName
        {
            get
            {
                return _friendlyName;
            }
        }
        private readonly string _friendlyName = null;

        public string SyncRoot
        {
            get
            {
                return _syncRoot;
            }
        }
        private readonly string _syncRoot = null;

        public string DatabaseFolder
        {
            get
            {
                return _databaseFolder;
            }
        }
        private readonly string _databaseFolder = null;

        public static AdvancedSyncSettings CreateDefaultSettings()
        {
            return new AdvancedSyncSettings(
                false,
                TraceType.NotEnabled,
                null,
                true,
                0,
                Environment.MachineName + Guid.NewGuid().ToString("N"),
                null,
                "SimpleClient01",
                Environment.MachineName,
                null,
                null);
        }

        public static AdvancedSyncSettings CreateDefaultSettings(ICLSyncSettings syncSettings)
        {
            if (syncSettings == null)
            {
                throw new ArgumentNullException("syncSettings must not be null");
            }

            return new AdvancedSyncSettings(
                false,
                TraceType.NotEnabled,
                null,
                true,
                0,
                Environment.MachineName + Guid.NewGuid().ToString("N"),
                null,
                "SimpleClient01",
                Environment.MachineName,
                syncSettings.SyncRoot,
                null);
        }

        public AdvancedSyncSettings(
                    bool logErrors,
                    TraceType traceType,
                    string traceLocation,
                    bool traceExcludeAuthorization,
                    int traceLevel,
                    string deviceId,
                    string tempDownloadFolderFullPath,
                    string clientVersion,
                    string friendlyName,
                    string cloudRoot,
                    string databaseFolder)
        {
            this._logErrors = logErrors;
            this._traceType = traceType;
            this._traceLocation = traceLocation;
            this._traceExcludeAuthorization = traceExcludeAuthorization;
            this._traceLevel = traceLevel;
            this._deviceId = deviceId;
            this._tempDownloadFolderFullPath = tempDownloadFolderFullPath;
            this._clientVersion = clientVersion;
            this._friendlyName = friendlyName;
            this._syncRoot = cloudRoot;
            this._databaseFolder = databaseFolder;
        }
    }

    internal static class SyncSettingsExtensions
    {
        public static AdvancedSyncSettings CopySettings(this ICLSyncSettingsAdvanced toCopy)
        {
            if (toCopy == null)
            {
                throw new ArgumentNullException("toCopy must not be null");
            }
            return new AdvancedSyncSettings(
                toCopy.LogErrors,
                toCopy.TraceType,
                toCopy.TraceLocation,
                toCopy.TraceExcludeAuthorization,
                toCopy.TraceLevel,
                String.IsNullOrWhiteSpace(toCopy.DeviceId) ? Environment.MachineName + Guid.NewGuid().ToString("N") : toCopy.DeviceId,
                toCopy.TempDownloadFolderFullPath,
                toCopy.ClientVersion,
                toCopy.FriendlyName,
                toCopy.SyncRoot,
                toCopy.DatabaseFolder);
        }

        public static AdvancedSyncSettings CopySettings(this ICLSyncSettings toCopy)
        {
            if (toCopy == null)
            {
                throw new ArgumentNullException("toCopy must not be null");
            }
            ICLSyncSettingsAdvanced advancedCopy = toCopy as ICLSyncSettingsAdvanced;
            if (advancedCopy == null)
            {
                return AdvancedSyncSettings.CreateDefaultSettings(toCopy);
            }
            else
            {
                return advancedCopy.CopySettings();
            }
        }
    }
}