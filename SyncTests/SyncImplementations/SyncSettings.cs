//
// SyncSettings.cs
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

namespace SyncTests.SyncImplementations
{
    public sealed class SyncSettings : ISyncSettings
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
        public string ErrorLogLocation
        {
            get
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create) +
                    "\\Cloud\\ErrorLog";
            }
        }
        public bool LogErrors
        {
            get
            {
                return true;
            }
        }
        public TraceType TraceType
        {
            get
            {
                return CloudApiPublic.Static.TraceType.CommunicationIncludeAuthorization | CloudApiPublic.Static.TraceType.FileChangeFlow;
            }
        }
        /// <summary>
        /// Only required if TraceType has any flags set (TraceType.NotEnabled means no flags are set)
        /// </summary>
        public string TraceLocation
        {
            get
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create) +
                   "\\Cloud\\Trace";
            }
        }
        public bool TraceExcludeAuthorization
        {
            get
            {
                return (TraceType & CloudApiPublic.Static.TraceType.CommunicationIncludeAuthorization) == CloudApiPublic.Static.TraceType.CommunicationIncludeAuthorization;
            }
        }
        public string Udid
        {
            get
            {
                return Guid.Empty.ToString();
            }
        }
        public string Uuid
        {
            get
            {
                return "1";
            }
        }
        public string Akey
        {
            get
            {
                return "0000000000000000000000000000000000000000000000000000000000000000";
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
                return "T01"; // Test 01
            }
        }
        public string DeviceName
        {
            get
            {
                return "Test";
            }
        }

        /// <summary>
        /// Should be null-coallesced to an empty string wherever used
        /// </summary>
        public string CloudRoot
        {
            get
            {
                return "C:\\Users\\Public\\Documents\\CloudTests";
            }
        }

        /// <summary>
        /// If null, a precalculated value will be used based on the local, non-roaming user's application data in the Cloud subdirectory
        /// </summary>
        public string DatabaseFile
        {
            get
            {
                return null;
            }
        }
    }
}