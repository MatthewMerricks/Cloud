using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Events.CLEventArgs
{
    public class GetFolderDeleteEventArgs : TaskEventArgs
    {
        public DirectoryInfo SyncBoxRoot { get; set;}
    }
}
