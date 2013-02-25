using CloudApiPublic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.CloudEventArgs
{
    public class CLEventArgs : EventArgs
    {
        public CLSyncBox SyncBox { get; set; }
        public CLSyncBoxCreationStatus boxCreationStatus { get; set; }
        public CLCredential Creds { get; set; }
        public CLCredentialCreationStatus credsStatus { get; set; }
    }
}
