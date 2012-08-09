using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using CloudApiPublic.Model;

namespace Sync.JsonContracts
{
    [DataContract]
    public class Push
    {
        [DataMember(Name = CLDefinitions.CLMetadataCloudPath, IsRequired = false)]
        public string RelativeRootPath
        {
            get
            {
                return CLDefinitions.SyncBodyRelativeRootPath;
            }
        }

        [DataMember(Name = CLDefinitions.CLSyncID, IsRequired = false)]
        public string LastSyncId { get; private set; }

        public Push(string lastSyncId)
        {
            this.LastSyncId = lastSyncId;
        }
    }
}
