//
// ICLSyncSettings.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Interfaces
{
    /// <summary>
    /// Never used by itself. Inherited by advanced settings interfaces to add properties which can enable tracing/logging.
    /// </summary>
    public interface ICLAddTraceSettings
    {
        /// <summary>
        /// Whether to log errors to a flat text file
        /// </summary>
        bool LogErrors { get; }
        /// <summary>
        /// Type of additional tracing outside of error logging to perform, flag AddAuthorization is invalid if flag Communication is not also set
        /// </summary>
        TraceType TraceType { get; }
        /// <summary>
        /// Location where the trace file (does not include errors) will be stored; Only required to not be null if TraceType has any flags set (TraceType.NotEnabled means no flags are set)
        /// </summary>
        string TraceLocation { get; }
        /// <summary>
        /// Whether to exclude authorization information (authentication keys, usernames/passwords, etc.) from the trace file;
        /// can return based on TraceType (i.e. "return (TraceType &amp; TraceType.AddAuthorization) != TraceType.AddAuthorization;")
        /// </summary>
        bool TraceExcludeAuthorization { get; }
        /// <summary>
        /// Specify 1 for the only the most important traces.  Use a higher number for more detail.  Specify 0 to disable Cloud trace.
        /// </summary>
        int TraceLevel { get; }
    }

    /// <summary>
    /// Basic settings for active sync (<see cref="Cloud.CLSyncEngine"/>).
    /// </summary>
    public interface ICLSyncSettings
    {
        /// <summary>
        /// Device id (each Syncbox may contain multiple devices, each with a unique id within the Syncbox).
        /// When running multiple instances of the sync engine on one machine, each combination of SyncboxId and DeviceId used with an engine must be unique on the machine.
        /// </summary>
        string DeviceId { get; }
    }

    /// <summary>
    /// Advanced settings for active sync (<see cref="Cloud.CLSyncEngine"/>). The addition over basic settings is <see cref="ICLAddTraceSettings"/>.
    /// </summary>
    public interface ICLSyncSettingsAdvanced : ICLSyncSettings, ICLCredentialsSettings
    {
        /// <summary>
        /// Location to store temporary downloads before they complete downloading and get moved to the final location;
        /// Use a different download folder path for each Syncbox or SyncEngine (the SyncEngine will clean out existing files in the provided directory);
        /// If null, a precalculated value will be used based on the local, non-roaming user's application data in the Cloud subdirectory
        /// </summary>
        string TempDownloadFolderFullPath { get; }
        /// <summary>
        /// Full path to a folder location where the database will be stored when using a Syncbox (you must handle your own database when using SyncEngine directly); If null, a precalculated value will be used based on the local, non-roaming user's application data in the Cloud subdirectory.  The file will be IndexDB.sdf.
        /// </summary>
        string DatabaseFolder { get; }
    }

    /// <summary>
    /// Additional sync options.
    /// </summary>
    public interface ICLAddSyncOptions
    {
        /// <summary>
        /// Client description.  Limited to 32-characters with no commas. (e.g., "My Product Name"); do not mimic values passed by other Cloud applications
        /// </summary>
        string ClientDescription { get; }
        /// <summary>
        /// Http timeout in milliseconds.
        /// </summary>
        /// <remarks>Return CLDefinitions.HttpTimeoutDefaultMilliseconds to use the SDK default.</remarks>
        int HttpTimeoutMilliseconds { get; }
    }

    /// <summary>
    /// Settings required for CLCredentials.
    /// </summary>
    public interface ICLCredentialsSettings : ICLAddTraceSettings, ICLAddSyncOptions { }
}