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
        public string FriendlyDeviceName { get; set; }
        public string TempDownloadFolderFullPath { get; set; }
        public string DatabaseFileFullPath { get; set; }
        public bool LogErrors { get; set; }
        public int TraceType { get; set; }
        public string TraceFilesFullPath { get; set; }
        public bool TraceExcludeAuthorization { get; set; }
        public int TraceLevel { get; set; }
    }
}
