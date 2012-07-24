// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationServer.Http.Channels;

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Collections;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Dispatcher;
    using System.ServiceModel.Web;
    using System.ServiceModel;

    internal class QueryCompositionOperationInvoker : IOperationInvoker
    {
        private IOperationInvoker inner;
        private IQueryComposer composer;

        public QueryCompositionOperationInvoker(IOperationInvoker invoker, IQueryComposer queryComposer)
        {
            this.inner = invoker;
            this.composer = queryComposer;
        }

        public bool IsSynchronous
        {
            get { return this.inner.IsSynchronous; }
        }

        public object[] AllocateInputs()
        {
            return this.inner.AllocateInputs();
        }

        public object Invoke(object instance, object[] inputs, out object[] outputs)
        {
            object result = this.inner.Invoke(instance, inputs, out outputs);

            return this.TryApplyQuery(result);
        }

        public IAsyncResult InvokeBegin(object instance, object[] inputs, AsyncCallback callback, object state)
        {
            return this.inner.InvokeBegin(instance, inputs, callback, state);
        }

        public object InvokeEnd(object instance, out object[] outputs, IAsyncResult result)
        {
            return this.TryApplyQuery(this.inner.InvokeEnd(instance, out outputs, result));
        }

        private static string GetRequestUri()
        {
            QueryCompositionMessageProperty queryCompositionMessageProperty = null;
            MessageProperties messageProperties = OperationContext.Current.IncomingMessageProperties;
            var message = OperationContext.Current.RequestContext.RequestMessage;
            var httpRequest = message.ToHttpRequestMessage();
            if (httpRequest != null)
            {
                if (messageProperties.ContainsKey(QueryCompositionMessageProperty.Name))
                {
                    queryCompositionMessageProperty =
                        messageProperties[QueryCompositionMessageProperty.Name] as QueryCompositionMessageProperty;
                    return queryCompositionMessageProperty.RequestUri;
                }

                return httpRequest.RequestUri.AbsoluteUri;
            }
            else
            {
                if (messageProperties.ContainsKey(QueryCompositionMessageProperty.Name))
                {
                    queryCompositionMessageProperty = messageProperties[QueryCompositionMessageProperty.Name] as QueryCompositionMessageProperty;
                    return queryCompositionMessageProperty.RequestUri;
                }
                else
                {
                    UriTemplateMatch uriTemplateMatch = WebOperationContext.Current.IncomingRequest.UriTemplateMatch;
                    if (uriTemplateMatch != null && uriTemplateMatch.RequestUri != null && uriTemplateMatch.RequestUri.AbsoluteUri != null)
                    {
                        return WebOperationContext.Current.IncomingRequest.UriTemplateMatch.RequestUri.AbsoluteUri;
                    }
                }    
            }

            return null;
        }

        private object TryApplyQuery(object result)
        {
            IEnumerable enumerableResult = result as IEnumerable;

            if (enumerableResult != null)
            {
                return this.composer.ComposeQuery(enumerableResult, GetRequestUri());
            }
            else
            {
                return result;
            }
        }
    }
}
