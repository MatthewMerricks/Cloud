using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Events.CLEventArgs
{
    public class CreateFolderEventArgs : TaskEventArgs
    {
        public DirectoryInfo CreateTaskDirectoryInfo { get; set; }
        public DateTime CreationTime { get; set; }
    }
}
