using CloudApiPublic.Interfaces;
using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublicSamples.Models
{
    public sealed class SettingsAvancedImpl : ISyncSettingsAdvanced
    {
        #region singleton pattern
        public static SettingsAvancedImpl Instance
        {
            get
            {
                lock (InstanceLocker)
                {
                    return _instance
                        ?? (_instance = new SettingsAvancedImpl());
                }
            }
        }
        private static SettingsAvancedImpl _instance = null;
        private static readonly object InstanceLocker = new object();

        private SettingsAvancedImpl() { }
        #endregion

        public bool LogErrors
        {
            get
            {
                return Properties.Settings.Default.LogErrors;
            }
        }
        public TraceType TraceType
        {
            get
            {
                return Properties.Settings.Default.TraceType;
            }
        }
        /// <summary>
        /// Only required if TraceType has any flags set (TraceType.NotEnabled means no flags are set)
        /// </summary>
        public string TraceLocation
        {
            get
            {
                return Properties.Settings.Default.TraceFolderFullPath;
            }
        }
        public bool TraceExcludeAuthorization
        {
            get
            {
                return Properties.Settings.Default.TraceExcludeAuthorization;
            }
        }

        public int TraceLevel
        {
            get
            {
                return Properties.Settings.Default.TraceLevel;

            }
        }

        public string Udid
        {
            get
            {
                return Properties.Settings.Default.UniqueDeviceId;
            }
        }
        public string Uuid
        {
            get
            {
                //TODO: Uuid should be deleted from settings.
                return String.Empty;
            }
        }
        public string Akey
        {
            get
            {
                //TODO: AKey should be deleted from settings.
                return String.Empty;
            }
        }
        /// <summary>
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
        public string DeviceName
        {
            get
            {
                //TODO: DeviceName should be deleted from settings.
                return String.Empty;
            }
        }
        public string SyncRoot
        {
            get
            {
                return Properties.Settings.Default.SyncBoxFullPath;
            }
        }

        /// <summary>
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
