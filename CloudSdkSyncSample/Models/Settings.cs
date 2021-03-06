﻿using Cloud.Static;
using System;

namespace SampleLiveSync.Models
{
    public class Settings
    {
        public string SyncboxFullPath { get; set; }
        public string Key { get; set; }
        public string Secret { get; set; }
        public string Token { get; set; }
        public string SyncboxId { get; set; }
        public string UniqueDeviceId { get; set; }
        public string TempDownloadFolderFullPath { get; set; }
        public string DatabaseFolderFullPath { get; set; }
        public bool BadgingEnabled { get; set; }
        public bool LogErrors { get; set; }
        public TraceType TraceType { get; set; }
        public string TraceFolderFullPath { get; set; }
        public bool TraceExcludeAuthorization { get; set; }
        public int TraceLevel { get; set; }

        public Settings()
        {
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="settingsCopy"></param>
        public Settings(Settings settingsCopy)
        {
            SyncboxFullPath = settingsCopy.SyncboxFullPath;
            Key = settingsCopy.Key;
            Secret = settingsCopy.Secret;
            Token = settingsCopy.Token;
            SyncboxId = settingsCopy.SyncboxId;
            UniqueDeviceId = settingsCopy.UniqueDeviceId;
            TempDownloadFolderFullPath = settingsCopy.TempDownloadFolderFullPath;
            DatabaseFolderFullPath = settingsCopy.DatabaseFolderFullPath;
            BadgingEnabled = settingsCopy.BadgingEnabled;
            LogErrors = settingsCopy.LogErrors;
            TraceType = settingsCopy.TraceType;
            TraceFolderFullPath = settingsCopy.TraceFolderFullPath;
            TraceExcludeAuthorization = settingsCopy.TraceExcludeAuthorization;
            TraceLevel = settingsCopy.TraceLevel;
        }

        /// <summary>
        /// Deep comparison.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            Settings otherSettings = obj as Settings;
            if (otherSettings == null)
            {
                return false;
            }

            // Use this pattern to compare reference members
            if (!Object.Equals(SyncboxFullPath, otherSettings.SyncboxFullPath)) return false;
            if (!Object.Equals(Key, otherSettings.Key)) return false;
            if (!Object.Equals(Secret, otherSettings.Secret)) return false;
            if (!Object.Equals(Token, otherSettings.Token)) return false;
            if (!Object.Equals(SyncboxId, otherSettings.SyncboxId)) return false;
            if (!Object.Equals(UniqueDeviceId, otherSettings.UniqueDeviceId)) return false;
            if (!Object.Equals(TempDownloadFolderFullPath, otherSettings.TempDownloadFolderFullPath)) return false;
            if (!Object.Equals(DatabaseFolderFullPath, otherSettings.DatabaseFolderFullPath)) return false;
            if (!Object.Equals(BadgingEnabled, otherSettings.BadgingEnabled)) return false;
            if (!Object.Equals(TraceFolderFullPath, otherSettings.TraceFolderFullPath)) return false;

            // Use this pattern to compare value members
            if (!LogErrors.Equals(otherSettings.LogErrors)) return false;
            if (!TraceType.Equals(otherSettings.TraceType)) return false;
            if (!TraceExcludeAuthorization.Equals(otherSettings.TraceExcludeAuthorization)) return false;
            if (!TraceLevel.Equals(otherSettings.TraceLevel)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Get the settings from this user's isolated settings.
        /// </summary>
        public void GetSavedSettings()
        {
            this.SyncboxFullPath = Properties.Settings.Default.SyncboxFullPath;
            this.Key = Properties.Settings.Default.Key;
            this.Secret = Properties.Settings.Default.Secret;
            this.Token = Properties.Settings.Default.Token;
            this.SyncboxId = Properties.Settings.Default.SyncboxId;
            this.UniqueDeviceId = Properties.Settings.Default.UniqueDeviceId;
            this.TempDownloadFolderFullPath = Properties.Settings.Default.TempDownloadFolderFullPath;
            this.DatabaseFolderFullPath = Properties.Settings.Default.DatabaseFolderFullPath;
            this.BadgingEnabled = Properties.Settings.Default.BadgingEnabled;
            this.LogErrors = Properties.Settings.Default.LogErrors;
            this.TraceType = Properties.Settings.Default.TraceType;
            this.TraceFolderFullPath = Properties.Settings.Default.TraceFolderFullPath;
            this.TraceExcludeAuthorization = Properties.Settings.Default.TraceExcludeAuthorization;
            this.TraceLevel = Properties.Settings.Default.TraceLevel;
        }

    }
}
