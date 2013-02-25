using CloudSDK_SmokeTest.Events.CLEventArgs;
using CloudSDK_SmokeTest.Managers;
using CloudSDK_SmokeTest.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Events.ManagerEventArgs
{
    public class FileManagerEventArgs : TaskEventArgs
    {
        public ManualSyncManager SyncManager {get; set;}

        public FileManagerEventArgs() { }
        public FileManagerEventArgs(TaskEventArgs taskArgs)
            : base(taskArgs)
        { 

        }
    }
}
