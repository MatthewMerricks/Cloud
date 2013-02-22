using CloudSDK_SmokeTest.Events.CLEventArgs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Events.ManagerEventArgs
{
    public class SmokeTaskManagerCreateArgs : TaskEventArgs
    {
        public FileInfo FileInfo { get; set; }
        public DateTime CurrentTime { get; set; }
        public DirectoryInfo RootDirectory { get; set; }
    }
}
