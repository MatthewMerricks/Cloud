// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Services
{
    using System.IO;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Web;

    [ServiceContract]
    public class InvalidParameterService2
    {
        [WebGet()]
        public string Operation(HttpRequestMessageProperty param1)
        {
            return null;
        }
    }
}
