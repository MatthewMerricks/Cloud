using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileMonitor;

namespace SQLIndexer
{
    public class SyncedObject
    {
        public string ServerLinkedPath { get; set; }
        public FileMetadata Metadata { get; set; }
    }
}
