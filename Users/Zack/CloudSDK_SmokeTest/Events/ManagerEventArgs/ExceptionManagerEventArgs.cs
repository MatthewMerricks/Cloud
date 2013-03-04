using Cloud;
using Cloud.Model;
using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Events.ManagerEventArgs
{
    public class ExceptionManagerEventArgs : EventArgs
    {
        public CLHttpRestStatus? RestStatus { get; set; }
        public CLCredentialCreationStatus? CredsCreateStatus { get; set; }
        public CLSyncBoxCreationStatus? SyncBoxCreateStatus { get; set; }
        public CLError Error { get; set; }
        public string OpperationName { get; set; }
        public GenericHolder<CLError> ProcessingErrorHolder { get; set; }
    }
}
