// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Services
{
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Web;

    [ServiceContract]
    public class MessageService2
    {
        [WebGet()]
        public int MessageOperation(out Message message)
        {
            message = null;
            return 0;
        }
    }
}
