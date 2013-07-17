using Cloud;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallingAllPublicMethods.Models
{
    public sealed class CLSyncboxProxy
    {
        public CLSyncbox Syncbox
        {
            get
            {
                return _syncbox;
            }
        }
        private readonly CLSyncbox _syncbox;

        public long SyncboxId
        {
            get
            {
                return (_syncbox == null
                    ? 1
                    : _syncbox.SyncboxId);
            }
        }

        public string FriendlyName
        {
            get
            {
                return ((_syncbox == null
                        || _syncbox.FriendlyName == null)
                    ? "{null}"
                    : _syncbox.FriendlyName);
            }
        }

        public long StoragePlanId
        {
            get
            {
                return (_syncbox == null
                    ? 1
                    : _syncbox.StoragePlanId);
            }
        }

        public string DeviceId
        {
            get
            {
                return ((_syncbox == null
                        || _syncbox.CopiedSettings.DeviceId == null)
                    ? "{null}"
                    : _syncbox.CopiedSettings.DeviceId);
            }
        }

        public CLSyncboxProxy(CLSyncbox syncbox)
        {
            Debug.Assert(
                syncbox != null
                    || DesignDependencyObject.IsInDesignTool,
                "syncbox cannot be null");

            this._syncbox = syncbox;
        }
    }
}