//
// CLSyncBox.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Interfaces;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic
{
    public sealed class CLSyncBox
    {
        /// <summary>
        /// Contains authentication information required for all communication and services
        /// </summary>
        public CLCredentials Credentials
        {
            get
            {
                return _credentials;
            }
        }
        private readonly CLCredentials _credentials;

        /// <summary>
        /// The unique ID of this SyncBox assigned by Cloud
        /// </summary>
        public long SyncBoxId
        {
            get
            {
                return _syncBoxId;
            }
        }
        private readonly long _syncBoxId;

        /// <summary>
        /// Settings copied upon creation of this SyncBox
        /// </summary>
        public ICLSyncSettingsAdvanced CopiedSettings
        {
            get
            {
                return _copiedSettings;
            }
        }
        private readonly ICLSyncSettingsAdvanced _copiedSettings;

        public static CLError CreateAndInitializeExistingSyncBox(
            CLCredentials Credentials,
            long SyncBoxId,
            out CLSyncBox syncBox,
            out CLSyncBoxCreationStatus status,
            ICLSyncSettings Settings = null)
        {
            status = CLSyncBoxCreationStatus.ErrorUnknown;

            try
            {
                syncBox = new CLSyncBox(
                    Credentials,
                    SyncBoxId,
                    Settings,
                    ref status);
            }
            catch (Exception ex)
            {
                syncBox = Helpers.DefaultForType<CLSyncBox>();
                return ex;
            }

            status = CLSyncBoxCreationStatus.Success;
            return null;
        }
        private CLSyncBox(CLCredentials Credentials,
            long SyncBoxId,
            ICLSyncSettings Settings,
            ref CLSyncBoxCreationStatus status)
        {
            // check input parameters

            if (Credentials == null)
            {
                status = CLSyncBoxCreationStatus.ErrorNullCredentials;
                throw new NullReferenceException("Credentials cannot be null");
            }

            this._credentials = Credentials;
            this._syncBoxId = SyncBoxId;
            if (Settings == null)
            {
                this._copiedSettings = new AdvancedSyncSettings(
                    false,
                    TraceType.NotEnabled,
                    null,
                    true,
                    0,
                    Environment.MachineName + Guid.NewGuid().ToString("N"),
                    null,
                    "SimpleClient01",
                    Environment.MachineName,
                    null,
                    null);
            }
            else
            {
                this._copiedSettings = Settings.CopySettings();
            }
        }
    }
    /// <summary>
    /// Status of creation of CLCredentials
    /// </summary>
    public enum CLSyncBoxCreationStatus : byte
    {
        Success,
        ErrorNullCredentials,
        ErrorUnknown
    }
}