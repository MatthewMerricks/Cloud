// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Services
{
    using System.IO;
    using System.Net;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Web;

    [ServiceContract]
    public class InvalidParameterService4
    {
        [WebGet()]
        public string Operation(WebHeaderCollection param1)
        {
            return null;
        }
    }
}
