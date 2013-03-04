using Cloud.Interfaces;
using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Settings
{
    internal sealed class AdvancedSyncSettings : ICLSyncSettingsAdvanced
    {
        public bool BadgingEnabled
        {
            get
            {
                return _badgingEnabled;
            }
        }
        private readonly bool _badgingEnabled = false;

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
                    bool badgingEnabled,
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
            this._badgingEnabled = badgingEnabled;
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

        public AdvancedSyncSettings(string syncRootPath)
        {
            this._logErrors = true;
            this._traceType = TraceType.CommunicationIncludeAuthorization | TraceType.FileChangeFlow;
            this._traceLocation = "C:\\Users\\Public\\Documents\\Cloud";
            this._traceExcludeAuthorization = false;
            this._traceLevel = 9;
            this._deviceId = "SimpleClient";
            this._tempDownloadFolderFullPath = null;
            this._clientVersion = "SmokeTest1";
            this._friendlyName = "Smoke Test";
            if (syncRootPath.LastIndexOf('\\') == syncRootPath.Count() - 1)
                this._syncRoot = syncRootPath.Substring(0, (syncRootPath.Count() - 1));
            else
                this._syncRoot = syncRootPath;
            this._databaseFolder = null;
        }
    }
}
