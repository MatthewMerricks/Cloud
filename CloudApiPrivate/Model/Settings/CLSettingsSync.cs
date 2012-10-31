//
// CLSettingsSync.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPrivate.Model;
using CloudApiPrivate.Model.Settings;
using CloudApiPublic.Interfaces;
using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPrivate.Model.Settings
{
    public sealed class CLSettingsSync : ISyncSettings
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

        /// <summary>
        /// Only required if LogErrors is set to true
        /// </summary>
        public string ErrorLogLocation
        {
            get
            {
                return Settings.Instance.ErrorLogLocation;
            }
        }
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
        public string Udid
        {
            get
            {
                return Settings.Instance.Udid;
            }
        }
        public string Uuid
        {
            get
            {
                return Settings.Instance.Uuid;
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
        public string ClientVersion
        {
            get
            {
                return CLPrivateDefinitions.CLClientVersion;
            }
        }
        public string DeviceName
        {
            get
            {
                return Settings.Instance.DeviceName;
            }
        }
        public string CloudRoot
        {
            get
            {
                return Settings.Instance.CloudFolderPath;
            }
        }
    }
}