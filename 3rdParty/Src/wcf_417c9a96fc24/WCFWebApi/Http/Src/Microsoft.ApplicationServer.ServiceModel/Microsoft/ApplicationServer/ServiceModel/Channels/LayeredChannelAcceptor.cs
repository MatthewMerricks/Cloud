// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.ServiceModel.Channels
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using Microsoft.ApplicationServer.Common;

    internal abstract class LayeredChannelAcceptor<TChannel, TInnerChannel> : ChannelAcceptor<TChannel>
        where TChannel : class, IChannel
        where TInnerChannel : class, IChannel
    {
        private IChannelListener<TInnerChannel> innerListener;

        protected LayeredChannelAcceptor(ChannelManagerBase channelManager, IChannelListener<TInnerChannel> innerListener)
            : base(channelManager)
        {
            this.innerListener = innerListener;
        }

        public override TChannel AcceptChannel(TimeSpan timeout)
        {
            TInnerChannel innerChannel = this.innerListener.AcceptChannel(timeout);
            if (innerChannel == null)
            {
                return null;
            }
            else
            {
                return this.OnAcceptChannel(innerChannel);
            }
        }

        public override IAsyncResult BeginAcceptChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return this.innerListener.BeginAcceptChannel(timeout, callback, state);
        }

        public override TChannel EndAcceptChannel(IAsyncResult result)
        {
            TInnerChannel innerChannel = this.innerListener.EndAcceptChannel(result);
            if (innerChannel == null)
            {
                return null;
            }
            else
            {
                return this.OnAcceptChannel(innerChannel);
            }
        }

        public override bool WaitForChannel(TimeSpan timeout)
        {
            return this.innerListener.WaitForChannel(timeout);
        }

        public override IAsyncResult BeginWaitForChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return this.innerListener.BeginWaitForChannel(timeout, callback, state);
        }

        public override bool EndWaitForChannel(IAsyncResult result)
        {
            return this.innerListener.EndWaitForChannel(result);
        }

        protected abstract TChannel OnAcceptChannel(TInnerChannel innerChannel);
    }
}
