using Cloud;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallingAllPublicMethods.Models
{
    public sealed class CLSyncboxPickedEventArgs : EventArgs
    {
        public CLSyncboxProxy Syncbox
        {
            get
            {
                return _syncbox;
            }
        }
        private readonly CLSyncboxProxy _syncbox;

        public CLSyncboxPickedEventArgs(CLSyncboxProxy Syncbox)
        {
            this._syncbox = Syncbox;
        }
    }
}