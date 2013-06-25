using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cloud.Static;

namespace SampleLiveSync.EventMessageReceiver
{
    public sealed class DidReturnUnderStorageQuotaMessage : EventMessage<DidReturnUnderStorageQuotaMessage>
    {
        #region EventMessage abstract overrides
        public override EventMessageImage Image
        {
            get
            {
                return EventMessageImage.Error;
            }
        }

        public override string Message
        {
            get
            {
                return "Storage usage back under or at quota";
            }
        }
        #endregion

        internal DidReturnUnderStorageQuotaMessage()
        {
        }
    }
}