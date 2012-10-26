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

namespace FileMonitor.SyncSettings
{
    public sealed class SyncSettings : ISyncSettings
    {
        /// <summary>
        /// Only required if LogErrors is set to true
        /// </summary>
        public string ErrorLogLocation
        {
            get
            {
                return _errorLogLocation;
            }
        }
        private string _errorLogLocation;


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
        private bool _logErrors = false;

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
        private TraceType _traceType = TraceType.NotEnabled;

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
        private string _traceLocation;

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
        private bool _traceExcludeAuthorization;


        public string Udid
        {
            get
            {
                return _udid;
            }
        }
        private string _udid;

        public string Uuid
        {
            get
            {
                return _uuid;
            }
        }
        private string _uuid;

        public string Akey
        {
            get
            {
                return _akey;
            }
        }
        private string _akey;

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
        private string _tempDownloadFolderFullPath = null;

        public string ClientVersion
        {
            get
            {
                return _clientVersion;
            }
        }
        private string _clientVersion = null;

        public string DeviceName
        {
            get
            {
                return _deviceName;
            }
        }
        private string _deviceName = null; 

        public string CloudRoot
        {
            get
            {
                return _cloudRoot;
            }
        }
        private string _cloudRoot = null;

        public SyncSettings(
                    string errorLogLocation,
                    bool logErrors,
                    TraceType traceType,
                    string traceLocation,
                    bool traceExcludeAuthorization,
                    string udid,
                    string uuid,
                    string akey,
                    string tempDownloadFolderFullPath,
                    string clientVersion,
                    string deviceName,
                    string cloudRoot
                            )
        {
            _errorLogLocation = errorLogLocation;
            _logErrors = logErrors;
            _traceType = traceType;
            _traceLocation = traceLocation;
            _traceExcludeAuthorization = traceExcludeAuthorization;
            _udid = udid;
            _uuid = uuid;
            _akey = akey;
            _tempDownloadFolderFullPath = tempDownloadFolderFullPath;
            _clientVersion = clientVersion;
            _deviceName = deviceName;
            _cloudRoot = cloudRoot;
        }
    }
}