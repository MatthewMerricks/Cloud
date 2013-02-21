using CloudApiPublic;
using CloudSDK_SmokeTest.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Events.CLEventArgs
{
    public class CLEventArgs
    {
        public CLSyncBox SyncBox { get; set; }
        public CLSyncBoxCreationStatus boxCreationStatus { get; set; }
        public CLCredential Creds { get; set; }
        public CLCredentialCreationStatus CredsStatus { get; set; }
        public InputParams ParamSet { get; set; }
    }
}
