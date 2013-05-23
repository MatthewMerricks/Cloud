//
// CLCredentialsSettings.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cloud.Interfaces;
using Cloud.Static;

namespace Cloud
{
    internal sealed class CLCredentialsSettings : ICLCredentialsSettings
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
        private readonly string _traceLocation = null;

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

        public string ClientDescription
        {
            get
            {
                return _clientDescription;
            }
        }
        private readonly string _clientDescription = null;

        public int HttpTimeoutMilliseconds
        {
            get
            {
                return _httpTimeoutMilliseconds;
            }
        }
        private readonly int _httpTimeoutMilliseconds = CLDefinitions.HttpTimeoutDefaultMilliseconds;

        public CLCredentialsSettings(
                bool LogErrors, 
                TraceType TraceType, 
                string TraceLocation, 
                bool TraceExcludeAuthorization, 
                int TraceLevel, 
                string ClientDescription,
                int HttpTimeoutMilliseconds)
        {
            this._logErrors = LogErrors;
            this._traceType = TraceType;
            this._traceLocation = TraceLocation;
            this._traceExcludeAuthorization = TraceExcludeAuthorization;
            this._traceLevel = TraceLevel;
            this._clientDescription = ClientDescription;
            this._httpTimeoutMilliseconds = HttpTimeoutMilliseconds;
        }
    }

    internal static class CLCredentialSettingsExtensions
    {
        public static AdvancedSyncSettings CopySettings(this ICLCredentialsSettings toCopy)
        {
            if (toCopy == null)
            {
                throw new ArgumentNullException(Resources.CLCredentialSettingsToCopyNotBeNull);
            }
            ICLSyncSettingsAdvanced advancedCopy = toCopy as ICLSyncSettingsAdvanced;
            if (advancedCopy == null)
            {
                return new AdvancedSyncSettings(
                    toCopy.LogErrors,
                    toCopy.TraceType,
                    toCopy.TraceLocation,
                    toCopy.TraceExcludeAuthorization,
                    toCopy.TraceLevel,
                    Environment.MachineName + Guid.NewGuid().ToString("N"),
                    String.Empty,
                    toCopy.ClientDescription,
                    toCopy.HttpTimeoutMilliseconds,
                    String.Empty);
            }
            else
            {
                return advancedCopy.CopySettings();
            }
        }
    }
}