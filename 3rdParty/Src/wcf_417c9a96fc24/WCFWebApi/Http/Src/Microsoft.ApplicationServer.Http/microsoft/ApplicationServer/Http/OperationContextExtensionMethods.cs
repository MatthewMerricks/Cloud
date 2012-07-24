// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System.Net.Http;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Http.Channels;
    using Microsoft.ApplicationServer.Http.Dispatcher;

    /// <summary>
    /// Extension Methods that provide access to common values from the 
    /// <see cref="OperationContext"/>.
    /// </summary>
    internal static class OperationContextExtensionMethods
    {
        internal static HttpRequestMessage GetHttpRequestMessage(this OperationContext context)
        {
            if (context != null)
            {
                RequestContext requestContext = context.RequestContext;
                if (requestContext != null)
                {
                    Message message = requestContext.RequestMessage;
                    if (message != null)
                    {
                        return message.ToHttpRequestMessage();
                    }
                }
            }

            return null;
        }

        internal static string GetCurrentOperationName(this OperationContext context)
        {
            if (context != null)
            {
                MessageProperties properties = context.IncomingMessageProperties;
                Fx.Assert(properties != null, "incomingMessageProperties cannot be null");
                return properties[HttpOperationSelector.SelectedOperationPropertyName] as string;
            }

            return SR.UnknownName;
        }
    }
}