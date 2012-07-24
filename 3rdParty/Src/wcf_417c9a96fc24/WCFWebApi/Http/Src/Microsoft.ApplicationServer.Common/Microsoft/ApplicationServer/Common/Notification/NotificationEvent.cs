//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace Microsoft.ApplicationServer.Common.Notification
{
    using System.Runtime.Serialization;

    [DataContract(Name = "NotificationEvent", Namespace = "http://schemas.microsoft.com/2010/04/appfabric/management/")]
    public sealed class NotificationEvent
    {
        public NotificationEvent(string name)
        {
            this.Name = name;
        }

        [DataMember]
        public string Name { get; set; }

        // TODO: Add a Notification argument list when needed
    }
}
