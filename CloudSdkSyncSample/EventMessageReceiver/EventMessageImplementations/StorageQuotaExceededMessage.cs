using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cloud.Static;

namespace SampleLiveSync.EventMessageReceiver
{
    public sealed class StorageQuotaExceededMessage : EventMessage<StorageQuotaExceededMessage>
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
                return "Storage quota exceeded";
            }
        }
        #endregion

        internal StorageQuotaExceededMessage()
        {
        }
    }
}