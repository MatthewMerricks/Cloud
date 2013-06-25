using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cloud.Static;

namespace SampleLiveSync.EventMessageReceiver
{
    public sealed class DidExceedStorageQuotaMessage : EventMessage<DidExceedStorageQuotaMessage>
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
                return "Storage usage exceeded quota";
            }
        }
        #endregion

        internal DidExceedStorageQuotaMessage()
        {
        }
    }
}