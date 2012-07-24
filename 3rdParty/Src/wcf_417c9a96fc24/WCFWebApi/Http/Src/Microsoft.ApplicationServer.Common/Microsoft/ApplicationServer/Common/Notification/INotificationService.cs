//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace Microsoft.ApplicationServer.Common.Notification
{
    using System.Collections.Generic;
    using System.ServiceModel;

    [ServiceContract]
    public interface INotificationService
    {
        [OperationContract()]
        void Notify(IList<NotificationEvent> events);
    }
}
