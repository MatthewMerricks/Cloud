// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Channels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.ServiceModel.Channels;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.ServiceModel.Activation;

    internal class HttpMessageEncodingRequestContext : RequestContext
    {
        private const string ContentLengthHeader = "Content-Length";
        private const string DefaultReasonPhrase = "OK";

        // TODO: Remove this list of content-type headers once the NCL team publicly exposes
        //       this list. Opened bug #50459 in DevDiv2 TFS on the NCL team.
        //       Opened #189321 in CSDmain to track this. [randallt] 
        private static readonly HashSet<string> httpContentHeaders = new HashSet<string>()
            {
                "Allow", "Content-Encoding", "Content-Language", "Content-Location", "Content-MD5",
                "Content-Range", "Expires", "Last-Modified", "Content-Type", ContentLengthHeader
            };

        private RequestContext innerContext;
        private Message configuredRequestMessage;
        private bool isRequestConfigured;
        private object requestConfigurationLock;

        public HttpMessageEncodingRequestContext(RequestContext innerContext)
        {
            Fx.Assert(innerContext != null, "The 'innerContext' parameter should not be null.");
            this.innerContext = innerContext;
            this.requestConfigurationLock = new object();         
        }

        public override Message RequestMessage
        {
            get
            {
                if (!this.isRequestConfigured)
                {
                    lock (this.requestConfigurationLock)
                    {
                        if (!this.isRequestConfigured)
                        {
                            Message innerMessage = this.innerContext.RequestMessage;
                            this.configuredRequestMessage = ConfigureRequestMessage(innerMessage);
                            this.isRequestConfigured = true;
                        }
                    }
                }

                return this.configuredRequestMessage;
            }
        }

        public override void Abort()
        {
            this.innerContext.Abort();
        }

        public override IAsyncResult BeginReply(Message message, TimeSpan timeout, AsyncCallback callback, object state)
        {
            message = ConfigureResponseMessage(message);
            return this.innerContext.BeginReply(message, timeout, callback, state);
        }

        public override IAsyncResult BeginReply(Message message, AsyncCallback callback, object state)
        {
            message = ConfigureResponseMessage(message);
            return this.innerContext.BeginReply(message, callback, state);
        }

        public override void Close(TimeSpan timeout)
        {
            this.innerContext.Close(timeout);
        }

        public override void Close()
        {
            this.innerContext.Close();
        }

        public override void EndReply(IAsyncResult result)
        {
            this.innerContext.EndReply(result);
        }

        public override void Reply(Message message, TimeSpan timeout)
        {
            message = ConfigureResponseMessage(message);
            this.innerContext.Reply(message, timeout);
        }

        public override void Reply(Message message)
        {
            ConfigureResponseMessage(message);
            this.innerContext.Reply(message);
        }

        private static void AddHeaderToHttpRequestMessageAndHandleExceptions(HttpRequestMessage httpRequestMessage, string headerName, string headerValue)
        {
            try
            {
                AddHeaderToHttpRequestMessage(httpRequestMessage, headerName, headerValue);
            }
            catch (FormatException e)
            {
                Fx.Exception.AsInformation(e);
            }
            catch (InvalidOperationException e)
            {
                Fx.Exception.AsInformation(e);
            }
        }

        private static void AddHeaderToHttpRequestMessage(HttpRequestMessage httpRequestMessage, string headerName, string headerValue)
        {
            if (httpContentHeaders.Contains(headerName))
            {
                // Only set the content-length header if it is not already set
                if (string.Equals(headerName, ContentLengthHeader, StringComparison.Ordinal) &&
                    httpRequestMessage.Content.Headers.ContentLength != null)
                {
                    return;
                }

                httpRequestMessage.Content.Headers.Add(headerName, headerValue);
            }
            else
            {
                httpRequestMessage.Headers.Add(headerName, headerValue);
            }
        }

        private static void CopyHeadersToNameValueCollection(HttpHeaders headers, NameValueCollection nameValueCollection)
        {
            foreach (KeyValuePair<string, IEnumerable<string>> header in headers)
            {
                foreach (string value in header.Value)
                {
                    nameValueCollection.Add(header.Key, value);
                }
            }
        }

        private static Message ConfigureRequestMessage(Message message)
        {
            if (message == null)
            {
                return null;
            }
            
            HttpRequestMessageProperty requestProperty = message.GetHttpRequestMessageProperty();
            if (requestProperty == null)
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.RequestMissingHttpRequestMessageProperty(
                            HttpRequestMessageProperty.Name,
                            typeof(HttpRequestMessageProperty).FullName)));
            }

            Uri uri = message.Headers.To;
            if (uri == null)
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(SR.RequestMissingToHeader));
            }

            // We also need to copy the hosted context so that HttpContext.Current
            // will be set up correctly by the MessageRpc.ApplyHostingIntegrationContext()
            object hostingProperty = AspNetEnvironment.Current.GetHostingProperty(message);

            HttpRequestMessage httpRequestMessage = message.ToHttpRequestMessage();
            if (httpRequestMessage == null)
            {
                if (!message.IsEmpty)
                {
                    throw Fx.Exception.AsError(
                        new InvalidOperationException(
                            SR.NonHttpMessageMustBeEmpty(
                                HttpMessageExtensionMethods.ToHttpRequestMessageMethodName,
                                HttpMessage.MessageTypeFullName)));
                }

                httpRequestMessage = new HttpRequestMessage();               
                message.Close();
                message = httpRequestMessage.ToMessage();
            }
            else
            {
                message.Headers.Clear();
                message.Properties.Clear();            
            }

            if (hostingProperty != null)
            {
                message.Properties.Add(AspNetEnvironment.HostingMessagePropertyName, hostingProperty);
            }

            if (httpRequestMessage.Content == null)
            {
                httpRequestMessage.Content = new ByteArrayContent(new byte[0]);
            }
            else
            {
                httpRequestMessage.Content.Headers.Clear();
            }

            message.Headers.To = uri;

            httpRequestMessage.RequestUri = uri;
            httpRequestMessage.Method = new HttpMethod(requestProperty.Method);
            foreach (var headerName in requestProperty.Headers.AllKeys)
            {
                AddHeaderToHttpRequestMessageAndHandleExceptions(
                    httpRequestMessage, 
                    headerName, 
                    requestProperty.Headers[headerName]);
            }

            return message;
        }

        private static Message ConfigureResponseMessage(Message message)
        {
            if (message == null)
            {
                return null;
            }

            HttpResponseMessageProperty responseProperty = new HttpResponseMessageProperty();

            HttpResponseMessage httpResponseMessage = message.ToHttpResponseMessage();
            if (httpResponseMessage == null)
            {
                responseProperty.StatusCode = HttpStatusCode.InternalServerError;
                responseProperty.SuppressEntityBody = true;
            }
            else
            {
                responseProperty.StatusCode = httpResponseMessage.StatusCode;
                if (httpResponseMessage.ReasonPhrase != null && 
                    httpResponseMessage.ReasonPhrase != DefaultReasonPhrase)
                {
                    responseProperty.StatusDescription = httpResponseMessage.ReasonPhrase;
                }
            
                CopyHeadersToNameValueCollection(httpResponseMessage.Headers, responseProperty.Headers);
                HttpContent content = httpResponseMessage.Content;
                if (content != null)
                {
                    ObjectContent objectContent = content as ObjectContent;
                    if (objectContent != null)
                    {
                        objectContent.DetermineWriteSerializerAndContentType();
                    }

                    CopyHeadersToNameValueCollection(httpResponseMessage.Content.Headers, responseProperty.Headers);
                }
                else
                {
                    responseProperty.SuppressEntityBody = true;
                }
            }

            message.Properties.Clear();
            message.Headers.Clear();

            message.Properties.Add(HttpResponseMessageProperty.Name, responseProperty);

            return message;
        }
    }
}
