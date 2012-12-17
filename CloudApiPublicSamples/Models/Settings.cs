using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublicSamples.Models
{
    public class Settings
    {
        public string SyncBoxFullPath { get; set; }
        public string ApplicationKey { get; set; }
        public string ApplicationSecret { get; set; }
        public string SyncBoxId { get; set; }
        public string UniqueDeviceId { get; set; }
        public string TempDownloadFolderFullPath { get; set; }
        public string DatabaseFolderFullPath { get; set; }
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
            SyncBoxFullPath = settingsCopy.SyncBoxFullPath;
            ApplicationKey = settingsCopy.ApplicationKey;
            ApplicationSecret = settingsCopy.ApplicationSecret;
            SyncBoxId = settingsCopy.SyncBoxId;
            UniqueDeviceId = settingsCopy.UniqueDeviceId;
            TempDownloadFolderFullPath = settingsCopy.TempDownloadFolderFullPath;
            DatabaseFolderFullPath = settingsCopy.DatabaseFolderFullPath;
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
            if (!Object.Equals(SyncBoxFullPath, otherSettings.SyncBoxFullPath)) return false;
            if (!Object.Equals(ApplicationKey, otherSettings.ApplicationKey)) return false;
            if (!Object.Equals(ApplicationSecret, otherSettings.ApplicationSecret)) return false;
            if (!Object.Equals(SyncBoxId, otherSettings.SyncBoxId)) return false;
            if (!Object.Equals(UniqueDeviceId, otherSettings.UniqueDeviceId)) return false;
            if (!Object.Equals(TempDownloadFolderFullPath, otherSettings.TempDownloadFolderFullPath)) return false;
            if (!Object.Equals(DatabaseFolderFullPath, otherSettings.DatabaseFolderFullPath)) return false;
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
            this.SyncBoxFullPath = Properties.Settings.Default.SyncBoxFullPath;
            this.ApplicationKey = Properties.Settings.Default.ApplicationKey;
            this.ApplicationSecret = Properties.Settings.Default.ApplicationSecret;
            this.SyncBoxId = Properties.Settings.Default.SyncBoxId;
            this.UniqueDeviceId = Properties.Settings.Default.UniqueDeviceId;
            this.TempDownloadFolderFullPath = Properties.Settings.Default.TempDownloadFolderFullPath;
            this.DatabaseFolderFullPath = Properties.Settings.Default.DatabaseFolderFullPath;
            this.LogErrors = Properties.Settings.Default.LogErrors;
            this.TraceType = Properties.Settings.Default.TraceType;
            this.TraceFolderFullPath = Properties.Settings.Default.TraceFolderFullPath;
            this.TraceExcludeAuthorization = Properties.Settings.Default.TraceExcludeAuthorization;
            this.TraceLevel = Properties.Settings.Default.TraceLevel;
        }

    }
}
