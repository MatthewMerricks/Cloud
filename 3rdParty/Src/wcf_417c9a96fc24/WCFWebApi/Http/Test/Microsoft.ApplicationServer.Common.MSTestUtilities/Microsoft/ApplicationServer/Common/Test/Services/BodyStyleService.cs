// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Services
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Web;

    [ServiceContract]
    public class BodyStyleService
    {
        [OperationContract]
        public void NoAttributeOperation()
        {
        }

        [WebInvoke]
        public void WebInvokeOperation()
        {
        }

        [WebGet]
        public void WebGetOperation()
        {
        }

        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare)]
        public void WebInvokeBareOperation()
        {
        }

        [WebGet(BodyStyle = WebMessageBodyStyle.Bare)]
        public void WebGetBareOperation()
        {
        }

        [WebInvoke(BodyStyle = WebMessageBodyStyle.Wrapped)]
        public void WebInvokeWrappedOperation()
        {
        }

        [WebGet(BodyStyle = WebMessageBodyStyle.Wrapped)]
        public void WebGetWrappedOperation()
        {
        }

        [WebInvoke(BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        public void WebInvokeWrappedRequestOperation()
        {
        }

        [WebGet(BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        public void WebGetWrappedRequestOperation()
        {
        }

        [WebInvoke(BodyStyle = WebMessageBodyStyle.WrappedResponse)]
        public void WebInvokeWrappedResponseOperation()
        {
        }

        [WebGet(BodyStyle = WebMessageBodyStyle.WrappedResponse)]
        public void WebGetWrappedResponseOperation()
        {
        }
    }
}
