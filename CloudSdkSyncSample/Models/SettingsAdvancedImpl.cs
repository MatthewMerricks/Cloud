using Cloud.Interfaces;
using Cloud.Static;
using System;

namespace SampleLiveSync.Models
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
        /// The unique device ID within SyncboxId.
        /// </summary>
        public string DeviceId
        {
            get
            {
                return Properties.Settings.Default.UniqueDeviceId;
            }
        }

        /// <summary>
        /// True: Enable badging.
        /// </summary>
        public bool BadgingEnabled
        {
            get
            {
                return Properties.Settings.Default.BadgingEnabled;
            }
        }

        /// <summary>
        /// Key is the identity of this application.
        /// </summary>
        public string Key
        {
            get
            {
                return Properties.Settings.Default.Key;
            }
        }

        /// <summary>
        /// Secret.
        /// </summary>
        /// <remarks>NOTE: This should NOT be stored in the settings.  It should be retrieved dynamically from the developer's server.</remarks>
        public string Secret
        {
            get
            {
                return Properties.Settings.Default.Secret;
            }
        }

        /// <summary>
        /// Token.
        /// </summary>
        /// <remarks>NOTE: This should NOT be stored in the settings.  It should be retrieved dynamically from the developer's server.</remarks>
        public string Token
        {
            get
            {
                return Properties.Settings.Default.Token;
            }
        }

        /// <summary>
        /// The unique Syncbox ID within an application.
        /// </summary>
        public Nullable<long> SyncboxId
        {
            get
            {
                long syncboxIdParsed;
                if (long.TryParse(Properties.Settings.Default.SyncboxId, out syncboxIdParsed))
                {
                    return syncboxIdParsed;
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
                return Properties.Settings.Default.TempDownloadFolderFullPath;
            }
        }
        /// <summary>
        /// Provide an optional description of the client application.  This description may be up to 32 characters, and must not contain commas.
        /// </summary>
        public string ClientDescription
        {
            get
            {
                return "Cloud Sample App";
            }
        }
        /// <summary>
        /// The full path of the folder to be synced.
        /// </summary>
        public string SyncRoot
        {
            get
            {
                return Properties.Settings.Default.SyncboxFullPath;
            }
        }

        /// <summary>
        /// This is the full path of the folder to be used to store the sync database file for this SyncboxId and DeviceId.
        /// If null, a precalculated value will be used based on the local, non-roaming user's application data in the Cloud subdirectory
        /// </summary>
        public string DatabaseFolder
        {
            get
            {
                return Properties.Settings.Default.DatabaseFolderFullPath;
            }
        }
    }
}
