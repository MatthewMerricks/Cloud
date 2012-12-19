//
// ISyncSettings.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Interfaces
{
    public interface IAddTraceSettings
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
        /// Whether to exclude authorization information (authentication keys, usernames/passwords, etc.) from the trace file; can return based on TraceType (i.e. "return (TraceType & TraceType.AddAuthorization) != TraceType.AddAuthorization;")
        /// </summary>
        bool TraceExcludeAuthorization { get; }
        /// <summary>
        /// Specify 1 for the only the most important traces.  Use a higher number for more detail.  Specify 0 to disable Cloud trace.
        /// </summary>
        int TraceLevel { get; }
    }

    public interface IHttpSettings
    {
        /// <summary>
        /// Device id (each SyncBox may contain multiple devices, each with a unique id within the SyncBox).
        /// </summary>
        string Udid { get; }

        /// <summary>
        /// The public key that identifies this application.
        /// </summary>
        string ApplicationKey { get; }

        /// <summary>
        /// The application secret private key.
        /// </summary>
        /// <remarks>NOTE: This should not be stored in the settings.  It should be retrieved dynamically from the developer's server.</remarks>
        string ApplicationSecret { get; }

        /// <summary>
        /// The unique ID of this SyncBox assigned by the auth server.
        /// </summary>
        string SyncBoxId { get; }

        /// <summary>
        /// User id, provided by server upon authentication
        /// </summary>
        //string Uuid { get; }

        /// <summary>
        /// Authorization key, provided by server upon authentication
        /// </summary>
        //string Akey { get; }
    }

    public interface IHttpSettingsAdvanced : IHttpSettings, IAddTraceSettings { }

    public interface ISyncSettings : IHttpSettings
    {
        /// <summary>
        /// Version letters/numbers used in communication with the server to identify the type of client (i.e. "MyClient01"); do not mimic values passed by other Cloud applications
        /// </summary>
        string ClientVersion { get; }
        /// <summary>
        /// Full path to the directory to be synced (do not include a trailing slash except for a drive root)
        /// </summary>
        string SyncRoot { get; }
    }

    public interface ISyncSettingsAdvanced : ISyncSettings, IAddTraceSettings
    {
        /// <summary>
        /// Location to store temporary downloads before they complete downloading and get moved to the final location;
        /// Use a different download folder path for each SyncBox or SyncEngine (the SyncEngine will clean out existing files in the provided directory);
        /// If null, a precalculated value will be used based on the local, non-roaming user's application data in the Cloud subdirectory
        /// </summary>
        string TempDownloadFolderFullPath { get; }
        /// <summary>
        /// Friendly name of the current device (we use computer name)
        /// </summary>
        string DeviceName { get; }
        /// <summary>
        /// Full path to a folder location where the database will be stored when using a SyncBox (you must handle your own database when using SyncEngine directly); If null, a precalculated value will be used based on the local, non-roaming user's application data in the Cloud subdirectory.  The file will be IndexDB.sdf.
        /// </summary>
        string DatabaseFolder { get; }
    }
}