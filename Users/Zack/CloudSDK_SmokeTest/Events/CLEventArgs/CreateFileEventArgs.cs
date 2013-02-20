using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Events.CLEventArgs
{
    public class CreateFileEventArgs : TaskEventArgs
    {
        public FileInfo CreateTaskFileInfo { get; set; }
        public DateTime CreateCurrentTime { get; set; }
        public DirectoryInfo RootDirectory { get; set; }
    }
}
