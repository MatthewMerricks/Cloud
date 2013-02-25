using CloudSDK_SmokeTest.Events.CLEventArgs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Events.ManagerEventArgs
{
    public class SmokeTaskManagerDeleteArgs : TaskEventArgs
    {
        public DirectoryInfo SyncBoxRoot { get; set; }
    }
}
