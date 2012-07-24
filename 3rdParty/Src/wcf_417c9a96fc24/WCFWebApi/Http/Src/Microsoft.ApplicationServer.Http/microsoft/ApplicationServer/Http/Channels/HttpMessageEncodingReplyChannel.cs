// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Channels
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using Microsoft.ApplicationServer.ServiceModel.Channels;

    internal class HttpMessageEncodingReplyChannel : LayeredChannel<IReplyChannel>, IReplyChannel, IChannel, ICommunicationObject
    {
        public HttpMessageEncodingReplyChannel(ChannelManagerBase channelManager, IReplyChannel innerChannel)
            : base(channelManager, innerChannel)
        {
        }

        public EndpointAddress LocalAddress
        {
            get
            {
                return this.InnerChannel.LocalAddress;
            }
        }

        public IAsyncResult BeginReceiveRequest(AsyncCallback callback, object state)
        {
            return this.InnerChannel.BeginReceiveRequest(callback, state);
        }

        public IAsyncResult BeginReceiveRequest(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return this.InnerChannel.BeginReceiveRequest(timeout, callback, state);
        }

        public IAsyncResult BeginTryReceiveRequest(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return this.InnerChannel.BeginTryReceiveRequest(timeout, callback, state);
        }

        public IAsyncResult BeginWaitForRequest(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return this.InnerChannel.BeginWaitForRequest(timeout, callback, state);
        }

        public RequestContext EndReceiveRequest(IAsyncResult result)
        {
            RequestContext innerContext = this.InnerChannel.EndReceiveRequest(result);
            return WrapRequestContext(innerContext);
        }

        public bool EndTryReceiveRequest(IAsyncResult result, out RequestContext context)
        {
            RequestContext innerContext;
            context = null;
            if (!this.InnerChannel.EndTryReceiveRequest(result, out innerContext))
            {
                return false;
            }

            context = WrapRequestContext(innerContext);
            return true;
        }

        public bool EndWaitForRequest(IAsyncResult result)
        {
            return this.InnerChannel.EndWaitForRequest(result);
        }

        public RequestContext ReceiveRequest()
        {
            RequestContext innerContext = this.InnerChannel.ReceiveRequest();
            return WrapRequestContext(innerContext);
        }

        public RequestContext ReceiveRequest(TimeSpan timeout)
        {
            RequestContext innerContext = this.InnerChannel.ReceiveRequest(timeout);
            return WrapRequestContext(innerContext);
        }

        public bool TryReceiveRequest(TimeSpan timeout, out RequestContext context)
        {
            RequestContext innerContext;
            if (this.InnerChannel.TryReceiveRequest(timeout, out innerContext))
            {
                context = WrapRequestContext(innerContext);
                return true;
            }

            context = null;
            return false;
        }

        public bool WaitForRequest(TimeSpan timeout)
        {
            return this.InnerChannel.WaitForRequest(timeout);
        }

        private static RequestContext WrapRequestContext(RequestContext innerContext)
        {
            return (innerContext != null) ?
                new HttpMessageEncodingRequestContext(innerContext) :
                (RequestContext)null;
        }
    }
}
