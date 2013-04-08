//
// CLSettingsSync.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPrivate.Model;
using CloudApiPrivate.Model.Settings;
using Cloud.Interfaces;
using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPrivate.Model.Settings
{
    public sealed class CLSettingsSync : ICLSyncSettingsAdvanced
    {
        #region singleton pattern
        public static CLSettingsSync Instance
        {
            get
            {
                lock (InstanceLocker)
                {
                    return _instance
                        ?? (_instance = new CLSettingsSync());
                }
            }
        }
        private static CLSettingsSync _instance = null;
        private static readonly object InstanceLocker = new object();

        private CLSettingsSync() { }
        #endregion

        public bool LogErrors
        {
            get
            {
                return Settings.Instance.LogErrors;
            }
        }
        public TraceType TraceType
        {
            get
            {
                return Settings.Instance.TraceType;
            }
        }
        /// <summary>
        /// Only required if TraceType has any flags set (TraceType.NotEnabled means no flags are set)
        /// </summary>
        public string TraceLocation
        {
            get
            {
                return Settings.Instance.TraceLocation;
            }
        }
        public bool TraceExcludeAuthorization
        {
            get
            {
                return Settings.Instance.TraceExcludeAuthorization;
            }
        }

        public int TraceLevel
        {
            get
            {
                return Settings.Instance.TraceLevel;

            }
        }

        public string DeviceId
        {
            get
            {
                return Settings.Instance.DeviceId;
            }
        }

        public bool BadgingEnabled
        {
            get
            {
                return Settings.Instance.BadgingEnabled;
            }
        }

        /// <summary>
        /// Application secret.
        /// </summary>
        /// <remarks>NOTE: This should not be stored in the settings.  It should be retrieved dynamically from the developer's server.</remarks>
        public string ApplicationKey
        {
            get
            {
                //TODO: Fix this.
                return "";
            }
        }

        public string ApplicationSecret
        {
            get
            {
                //TODO: Fix this.
                return "";
            }
        }

        public Nullable<long> SyncboxId
        {
            get
            {
                return null;
            }
        }

        public string Uuid
        {
            get
            {
                return Settings.Instance.SyncboxId;
            }
        }
        public string Akey
        {
            get
            {
                return Settings.Instance.Akey;
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
        public string ClientDescription
        {
            get
            {
                return CLPrivateDefinitions.CLClientVersion;
            }
        }
        public string SyncRoot
        {
            get
            {
                return Settings.Instance.CloudFolderPath;
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