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
    /// <summary>
    /// Represents a SyncBox in Cloud where everything is stored
    /// </summary>
    public sealed class CLSyncBox
    {
        /// <summary>
        /// Contains authentication information required for all communication and services
        /// </summary>
        public CLCredential Credential
        {
            get
            {
                return _credential;
            }
        }
        private readonly CLCredential _credential;

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

        public static CLError CreateAndInitialize(
            CLCredential Credential,
            long SyncBoxId,
            out CLSyncBox syncBox,
            out CLSyncBoxCreationStatus status,
            ICLSyncSettings Settings = null)
        {
            status = CLSyncBoxCreationStatus.ErrorUnknown;

            try
            {
                syncBox = new CLSyncBox(
                    Credential,
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
        private CLSyncBox(CLCredential Credential,
            long SyncBoxId,
            ICLSyncSettings Settings,
            ref CLSyncBoxCreationStatus status)
        {
            // check input parameters

            if (Credential == null)
            {
                status = CLSyncBoxCreationStatus.ErrorNullCredential;
                throw new NullReferenceException("Credential cannot be null");
            }

            this._credential = Credential;
            this._syncBoxId = SyncBoxId;
            if (Settings == null)
            {
                this._copiedSettings = AdvancedSyncSettings.CreateDefaultSettings();
            }
            else
            {
                this._copiedSettings = Settings.CopySettings();
            }
        }
    }
    /// <summary>
    /// Status of creation of <see cref="CLSyncBox"/>
    /// </summary>
    public enum CLSyncBoxCreationStatus : byte
    {
        Success,
        ErrorNullCredential,
        ErrorUnknown
    }
}