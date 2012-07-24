// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Services
{
    using System.Net.Http;
    using System.ServiceModel;
    using System.ServiceModel.Web;

    [ServiceContract]
    public class HttpRequestMessageService2
    {
        [WebGet()]
        public void Operation(out HttpRequestMessage message)
        {
            message = null;
        }
    }
}
