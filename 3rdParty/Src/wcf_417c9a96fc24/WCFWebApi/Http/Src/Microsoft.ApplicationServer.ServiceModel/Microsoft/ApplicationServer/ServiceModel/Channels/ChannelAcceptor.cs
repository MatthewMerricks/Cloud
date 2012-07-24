// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.ServiceModel.Channels
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using Microsoft.ApplicationServer.Common;

    internal abstract class ChannelAcceptor<TChannel> : CommunicationObject, IChannelAcceptor<TChannel>
        where TChannel : class, IChannel
    {
        private ChannelManagerBase channelManager;

        protected ChannelAcceptor(ChannelManagerBase channelManager)
        {
            this.channelManager = channelManager;
        }

        protected ChannelManagerBase ChannelManager
        {
            get { return this.channelManager; }
        }

        protected override TimeSpan DefaultCloseTimeout
        {
            get { return ((IDefaultCommunicationTimeouts)this.channelManager).CloseTimeout; }
        }

        protected override TimeSpan DefaultOpenTimeout
        {
            get { return ((IDefaultCommunicationTimeouts)this.channelManager).OpenTimeout; }
        }

        public abstract TChannel AcceptChannel(TimeSpan timeout);

        public abstract IAsyncResult BeginAcceptChannel(TimeSpan timeout, AsyncCallback callback, object state);
        
        public abstract TChannel EndAcceptChannel(IAsyncResult result);

        public abstract bool WaitForChannel(TimeSpan timeout);
        
        public abstract IAsyncResult BeginWaitForChannel(TimeSpan timeout, AsyncCallback callback, object state);
        
        public abstract bool EndWaitForChannel(IAsyncResult result);

        protected override void OnAbort()
        {
        }

        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new CompletedAsyncResult(callback, state);
        }

        protected override void OnEndClose(IAsyncResult result)
        {
            CompletedAsyncResult.End(result);
        }

        protected override void OnClose(TimeSpan timeout)
        {
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new CompletedAsyncResult(callback, state);
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            CompletedAsyncResult.End(result);
        }

        protected override void OnOpen(TimeSpan timeout)
        {
        }
    }
}
