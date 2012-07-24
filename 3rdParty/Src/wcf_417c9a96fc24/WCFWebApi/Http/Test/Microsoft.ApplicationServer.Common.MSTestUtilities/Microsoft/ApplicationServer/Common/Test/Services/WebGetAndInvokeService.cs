// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Services
{
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Web;

    [ServiceContract]
    public class WebGetAndInvokeService
    {
        [WebGet()]
        [WebInvoke()]
        public void WebGetAndInvokeOperation()
        {
        }
    }
}
