// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Channels
{
    using System;
    using System.ServiceModel.Channels;
    using Microsoft.ApplicationServer.ServiceModel.Channels;

    internal class HttpMessageEncodingChannelListener : LayeredChannelListener<IReplyChannel>
    {
        private IChannelListener<IReplyChannel> innerChannelListener;

        public HttpMessageEncodingChannelListener(Binding binding, IChannelListener<IReplyChannel> innerListener) :
            base(binding, innerListener)
        {
        }

        protected override void OnOpening()
        {
            this.innerChannelListener = (IChannelListener<IReplyChannel>)this.InnerChannelListener;
            base.OnOpening();
        }

        protected override IReplyChannel OnAcceptChannel(TimeSpan timeout)
        {
            IReplyChannel innerChannel = this.innerChannelListener.AcceptChannel(timeout);
            return this.WrapInnerChannel(innerChannel);
        }

        protected override IAsyncResult OnBeginAcceptChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return this.innerChannelListener.BeginAcceptChannel(timeout, callback, state);
        }

        protected override IReplyChannel OnEndAcceptChannel(IAsyncResult result)
        {
            IReplyChannel innerChannel = this.innerChannelListener.EndAcceptChannel(result);
            return this.WrapInnerChannel(innerChannel);
        }

        protected override IAsyncResult OnBeginWaitForChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return this.innerChannelListener.BeginWaitForChannel(timeout, callback, state);
        }

        protected override bool OnEndWaitForChannel(IAsyncResult result)
        {
            return this.innerChannelListener.EndWaitForChannel(result);
        }

        protected override bool OnWaitForChannel(TimeSpan timeout)
        {
            return this.innerChannelListener.WaitForChannel(timeout);
        }

        private IReplyChannel WrapInnerChannel(IReplyChannel innerChannel)
        {
            return (innerChannel != null) ?
                new HttpMessageEncodingReplyChannel(this, innerChannel) :
                (IReplyChannel)null;
        }
    }
}
