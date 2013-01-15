using CloudApiPublic.Interfaces;
using CloudApiPublic.Static;
using System;

namespace CloudSdkSyncSample.Models
{
    /// <summary>
    /// An implementation of the advanced settings interface.
    /// </summary>
    public sealed class SettingsAdvancedImpl : ICLSyncSettingsAdvanced
    {
        #region singleton pattern
        public static SettingsAdvancedImpl Instance
        {
            get
            {
                lock (InstanceLocker)
                {
                    return _instance
                        ?? (_instance = new SettingsAdvancedImpl());
                }
            }
        }
        private static SettingsAdvancedImpl _instance = null;
        private static readonly object InstanceLocker = new object();

        private SettingsAdvancedImpl() { }
        #endregion

        /// <summary>
        /// True to log sync errors.
        /// </summary>
        public bool LogErrors
        {
            get
            {
                return Properties.Settings.Default.LogErrors;
            }
        }
        /// <summary>
        /// Bit flags representing the types of sync log records to trace.
        /// </summary>
        public TraceType TraceType
        {
            get
            {
                return Properties.Settings.Default.TraceType;
            }
        }
        /// <summary>
        /// The full path of the sync trace file folder.  Null for the default.  Only required if TraceType has any flags set (TraceType.NotEnabled means no flags are set)
        /// </summary>
        public string TraceLocation
        {
            get
            {
                return Properties.Settings.Default.TraceFolderFullPath;
            }
        }
        /// <summary>
        /// True to exclude authorization information from the trace.
        /// </summary>
        public bool TraceExcludeAuthorization
        {
            get
            {
                return Properties.Settings.Default.TraceExcludeAuthorization;
            }
        }
        /// <summary>
        /// Level of functional trace records to trace.  0: Trace none.  9: Trace all.
        /// </summary>
        public int TraceLevel
        {
            get
            {
                return Properties.Settings.Default.TraceLevel;

            }
        }
        /// <summary>
        /// The unique device ID within SyncBoxId.
        /// </summary>
        public string DeviceId
        {
            get
            {
                return Properties.Settings.Default.UniqueDeviceId;
            }
        }

        /// <summary>
        /// Application key is the identity of this application.
        /// </summary>
        public string ApplicationKey
        {
            get
            {
                return Properties.Settings.Default.ApplicationKey;
            }
        }

        /// <summary>
        /// Application secret.
        /// </summary>
        /// <remarks>NOTE: This should NOT be stored in the settings.  It should be retrieved dynamically from the developer's server.</remarks>
        public string ApplicationSecret
        {
            get
            {
                return Properties.Settings.Default.ApplicationSecret;
            }
        }

        /// <summary>
        /// The unique SyncBox ID within this Application key.
        /// </summary>
        public Nullable<long> SyncBoxId
        {
            get
            {
                long syncBoxIdParsed;
                if (long.TryParse(Properties.Settings.Default.SyncBoxId, out syncBoxIdParsed))
                {
                    return syncBoxIdParsed;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// This is the full path of the folder to be used to store temporary files that are being downloaded.
        /// If null, a precalculated value will be used based on the local, non-roaming user's application data in the Cloud subdirectory
        /// </summary>
        public string TempDownloadFolderFullPath
        {
            get
            {
                return null;
            }
        }
        public string ClientVersion
        {
            get
            {
                //TODO: ClientVersion should be deleted from settings.
                return String.Empty;
            }
        }
        public string FriendlyName
        {
            get
            {
                //TODO: FriendlyName should be deleted from settings.
                return Environment.MachineName;
            }
        }
        /// <summary>
        /// The full path of the folder to be synced.
        /// </summary>
        public string SyncRoot
        {
            get
            {
                return Properties.Settings.Default.SyncBoxFullPath;
            }
        }

        /// <summary>
        /// This is the full path of the folder to be used to store the sync database file for this SyncBoxId and DeviceId.
        /// If null, a precalculated value will be used based on the local, non-roaming user's application data in the Cloud subdirectory
        /// </summary>
        public string DatabaseFolder
        {
            get
            {
                return null;
            }
        }
    }
}
