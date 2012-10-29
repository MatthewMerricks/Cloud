//
// SyncSettings.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPrivate.Model;
using CloudApiPrivate.Model.Settings;
using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace win_client.SyncImplementations
{
    public sealed class SyncSettings
    {
        #region singleton pattern
        public static SyncSettings Instance
        {
            get
            {
                lock (InstanceLocker)
                {
                    return _instance
                        ?? (_instance = new SyncSettings());
                }
            }
        }
        private static SyncSettings _instance = null;
        private static readonly object InstanceLocker = new object();

        private SyncSettings() { }
        #endregion

        /// <summary>
        /// Only required if LogErrors is set to true
        /// </summary>
        string ErrorLogLocation
        {
            get
            {
                return Settings.Instance.ErrorLogLocation;
            }
        }
        bool LogErrors
        {
            get
            {
                return Settings.Instance.LogErrors;
            }
        }
        TraceType TraceType
        {
            get
            {
                return Settings.Instance.TraceType;
            }
        }
        /// <summary>
        /// Only required if TraceType has any flags set (TraceType.NotEnabled means no flags are set)
        /// </summary>
        string TraceLocation
        {
            get
            {
                return Settings.Instance.TraceLocation;
            }
        }
        bool TraceExcludeAuthorization
        {
            get
            {
                return Settings.Instance.TraceExcludeAuthorization;
            }
        }
        string Udid
        {
            get
            {
                return Settings.Instance.Udid;
            }
        }
        string Uuid
        {
            get
            {
                return Settings.Instance.Uuid;
            }
        }
        string Akey
        {
            get
            {
                return Settings.Instance.Akey;
            }
        }
        /// <summary>
        /// If null, a precalculated value will be used based on the local, non-roaming user's application data in the Cloud subdirectory
        /// </summary>
        string TempDownloadFolderFullPath
        {
            get
            {
                return null;
            }
        }
        string ClientVersion
        {
            get
            {
                return CLPrivateDefinitions.CLClientVersion;
            }
        }
        public string getDeviceName
        {
            get
            {
                return Settings.Instance.DeviceName;
            }
        }
    }
}