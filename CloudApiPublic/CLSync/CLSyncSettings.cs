﻿//
// CLSyncSettings.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Interfaces;
using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud
{
    /// <summary>
    /// Simple implementation of ICLSyncSettings
    /// </summary>
    public sealed class CLSyncSettings : ICLSyncSettings
    {
        /// <summary>
        /// The ID of this device, unique within the Syncbox.
        /// </summary>
        public string DeviceId
        {
            get
            {
                return _deviceId;
            }
        }
        private readonly string _deviceId;

        /// <summary>
        /// Creates a simple implementation of ICLSyncSettings with only DeviceId
        /// </summary>
        /// <param name="deviceId">The device ID.</param>
        public CLSyncSettings(string deviceId)
        {
            this._deviceId = deviceId;
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
        private readonly string _traceLocation = null;

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

        /// <summary>
        /// The ID of this device, unique within the Syncbox.
        /// </summary>
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

        public string ClientDescription
        {
            get
            {
                return _clientDescription;
            }
        }
        private readonly string _clientDescription = null;

        public int HttpTimeoutMilliseconds
        {
            get
            {
                return _httpTimeoutMilliseconds;
            }
        }
        private readonly int _httpTimeoutMilliseconds = CLDefinitions.HttpTimeoutDefaultMilliseconds;

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
                String.Empty,
                CLDefinitions.HttpTimeoutDefaultMilliseconds,
                null);
        }

        public static AdvancedSyncSettings CreateDefaultSettings(ICLSyncSettings syncSettings)
        {
            if (syncSettings == null)
            {
                throw new ArgumentNullException(Resources.CLSyncSettingsMustNotBeNull);
            }

            return new AdvancedSyncSettings(
                false,
                TraceType.NotEnabled,
                null,
                true,
                0,
                syncSettings.DeviceId,
                null,
                String.Empty,
                CLDefinitions.HttpTimeoutDefaultMilliseconds,
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
                    string clientDescription,
                    int httpTimeoutMilliseconds,
                    string databaseFolder)
        {
            if (clientDescription.Length > CLDefinitions.MaxClientDescriptionLength)
            {
                throw new ArgumentException(Resources.CLSyncSettingsClientDescriptionTooLong);
            }
            if (clientDescription.Contains(","))
            {
                throw new ArgumentException(Resources.CLSyncSettingsClientDescriptionCannotContainCommas);
            }

            if (tempDownloadFolderFullPath != null && tempDownloadFolderFullPath.Length >= 2 && tempDownloadFolderFullPath[1] == ':')
            {
                tempDownloadFolderFullPath = char.ToUpper(tempDownloadFolderFullPath[0]) + tempDownloadFolderFullPath.Substring(1);
            }
            if (databaseFolder != null && databaseFolder.Length >= 2 && databaseFolder[1] == ':')
            {
                databaseFolder = char.ToUpper(databaseFolder[0]) + databaseFolder.Substring(1);
            }
            if (httpTimeoutMilliseconds < 10000)
            {
                throw new ArgumentException("HttpTimeoutMilliseconds is too small");
            }

            this._logErrors = logErrors;
            this._traceType = traceType;
            this._traceLocation = traceLocation;
            this._traceExcludeAuthorization = traceExcludeAuthorization;
            this._traceLevel = traceLevel;
            this._deviceId = deviceId;
            this._tempDownloadFolderFullPath = tempDownloadFolderFullPath;
            this._clientDescription = clientDescription;
            this._httpTimeoutMilliseconds = httpTimeoutMilliseconds;
            this._databaseFolder = databaseFolder;
        }
    }

    internal static class SyncSettingsExtensions
    {
        public static AdvancedSyncSettings CopySettings(this ICLSyncSettingsAdvanced toCopy)
        {
            if (toCopy == null)
            {
                throw new ArgumentNullException(Resources.CLSyncSettingstoCopyMustNotBeNull);
            }
            return new AdvancedSyncSettings(
                toCopy.LogErrors,
                toCopy.TraceType,
                toCopy.TraceLocation,
                toCopy.TraceExcludeAuthorization,
                toCopy.TraceLevel,
                toCopy.DeviceId,
                toCopy.TempDownloadFolderFullPath,
                toCopy.ClientDescription,
                toCopy.HttpTimeoutMilliseconds,
                toCopy.DatabaseFolder);
        }

        public static AdvancedSyncSettings CopySettings(this ICLSyncSettings toCopy)
        {
            if (toCopy == null)
            {
                throw new ArgumentNullException(Resources.CLSyncSettingstoCopyMustNotBeNull);
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
