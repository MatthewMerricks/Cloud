// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Services
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Web;

    [ServiceContract]
    public class DefaultBodyStyleService2
    {
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Wrapped)]
        public string NoAttributeOperation()
        {
            return "NoAttributeOperationString";
        }

        [WebGet(BodyStyle = WebMessageBodyStyle.Wrapped)]
        public string GetWithNoBodyStyle()
        {
            return "GetWithNoBodyStyleString";
        }

        [WebGet(BodyStyle = WebMessageBodyStyle.Wrapped)]
        public string GetWithWrappedBodyStyle()
        {
            return "GetWithWrappedBodyStyleString";
        }

        [WebInvoke(BodyStyle = WebMessageBodyStyle.Wrapped)]
        public string InvokeWithNoBodyStyle()
        {
            return "InvokeWithNoBodyStyleString";
        }

        [WebInvoke(BodyStyle = WebMessageBodyStyle.Wrapped)]
        public string InvokeWithWrappedBodyStyle()
        {
            return "InvokeWithWrappedBodyStyleString";
        }
    }
}
