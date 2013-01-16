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
        //public string DeviceId
        //{
        //    get
        //    {
        //        return _deviceId;
        //    }
        //}
        //private readonly string _deviceId;

        //public string ApplicationKey
        //{
        //    get
        //    {
        //        return _applicationKey;
        //    }
        //}
        //private readonly string _applicationKey;

        ///// <summary>
        ///// Application secret.
        ///// </summary>
        ///// <remarks>NOTE: This should not be stored in the settings.  It should be retrieved dynamically from the developer's server.</remarks>
        //public string ApplicationSecret
        //{
        //    get
        //    {
        //        return _applicationSecret;
        //    }
        //}
        //private readonly string _applicationSecret;

        //public Nullable<long> SyncBoxId
        //{
        //    get
        //    {
        //        return _syncBoxId;
        //    }
        //}
        //private readonly Nullable<long> _syncBoxId;

        //public string ClientVersion
        //{
        //    get
        //    {
        //        return _clientVersion;
        //    }
        //}
        //private readonly string _clientVersion = null;

        public string SyncRoot
        {
            get
            {
                return _syncRoot;
            }
        }
        private readonly string _syncRoot = null;

        public CLSyncSettings(
                    //string udid,
                    //string applicationKey,
                    //string applicationSecret,
                    //Nullable<long> syncBoxId,
                    //string clientVersion,
                    string syncRoot)
        {
            //this._deviceId = udid;
            //this._applicationKey = applicationKey;
            //this._applicationSecret = applicationSecret;
            //this._syncBoxId = syncBoxId;
            //this._clientVersion = clientVersion;
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

        //public string ApplicationKey
        //{
        //    get
        //    {
        //        return _applicationKey;
        //    }
        //}
        //private readonly string _applicationKey;

        //public string ApplicationSecret
        //{
        //    get
        //    {
        //        return _applicationSecret;
        //    }
        //}
        //private readonly string _applicationSecret;

        //public Nullable<long> SyncBoxId
        //{
        //    get
        //    {
        //        return _syncBoxId;
        //    }
        //}
        //private readonly Nullable<long> _syncBoxId;

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

        public AdvancedSyncSettings(
                    bool logErrors,
                    TraceType traceType,
                    string traceLocation,
                    bool traceExcludeAuthorization,
                    int traceLevel,
                    string deviceId,
                    //string applicationKey,
                    //string applicationSecret,
                    //Nullable<long> syncBoxId,
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
            //this._applicationKey = applicationKey;
            //this._applicationSecret = applicationSecret;
            //this._syncBoxId = syncBoxId;
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
            return new AdvancedSyncSettings(
                toCopy.LogErrors,
                toCopy.TraceType,
                toCopy.TraceLocation,
                toCopy.TraceExcludeAuthorization,
                toCopy.TraceLevel,
                toCopy.DeviceId,
                //toCopy.ApplicationKey,
                //toCopy.ApplicationSecret,
                //toCopy.SyncBoxId,
                toCopy.TempDownloadFolderFullPath,
                toCopy.ClientVersion,
                toCopy.FriendlyName,
                toCopy.SyncRoot,
                toCopy.DatabaseFolder);
        }

        public static AdvancedSyncSettings CopySettings(this ICLSyncSettings toCopy)
        {
            ICLSyncSettingsAdvanced advancedCopy = toCopy as ICLSyncSettingsAdvanced;
            if (advancedCopy == null)
            {
                return new AdvancedSyncSettings(
                    false,
                    TraceType.NotEnabled,
                    null,
                    true,
                    0,
                    Environment.MachineName + Guid.NewGuid().ToString("N"),
                    //toCopy.ApplicationKey,
                    //toCopy.ApplicationSecret,
                    //toCopy.SyncBoxId,
                    null,
                    "SimpleClient01",
                    Environment.MachineName,
                    toCopy.SyncRoot,
                    null);
            }
            else
            {
                return advancedCopy.CopySettings();
            }
        }

        //public static AdvancedSyncSettings CopySettings(this IHttpSettingsAdvanced toCopy)
        //{
        //    ICLSyncSettingsAdvanced syncCopy = toCopy as ICLSyncSettingsAdvanced;

        //    if (syncCopy == null)
        //    {
        //        return new AdvancedSyncSettings(
        //            toCopy.LogErrors,
        //            toCopy.TraceType,
        //            toCopy.TraceLocation,
        //            toCopy.TraceExcludeAuthorization,
        //            toCopy.TraceLevel,
        //            toCopy.DeviceId,
        //            toCopy.ApplicationKey,
        //            toCopy.ApplicationSecret,
        //            toCopy.SyncBoxId,
        //            null,
        //            null,
        //            Helpers.GetComputerFriendlyName(),
        //            null,
        //            null);
        //    }
        //    else
        //    {
        //        return syncCopy.CopySettings();
        //    }
        //}

        //public static AdvancedSyncSettings CopySettings(this IHttpSettings toCopy)
        //{
        //    IHttpSettingsAdvanced advancedCopy = toCopy as IHttpSettingsAdvanced;
        //    if (advancedCopy == null)
        //    {
        //        ICLSyncSettings syncCopy = toCopy as ICLSyncSettings;
        //        if (syncCopy == null)
        //        {
        //            return new AdvancedSyncSettings(
        //                false,
        //                TraceType.NotEnabled,
        //                null,
        //                true,
        //                0,
        //                toCopy.DeviceId,
        //                toCopy.ApplicationKey,
        //                toCopy.ApplicationSecret,
        //                toCopy.SyncBoxId,
        //                null,
        //                null,
        //                Helpers.GetComputerFriendlyName(),
        //                null,
        //                null);
        //        }
        //        else
        //        {
        //            return syncCopy.CopySettings();
        //        }
        //    }
        //    else
        //    {
        //        return advancedCopy.CopySettings();
        //    }
        //}
    }
}