// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Mocks
{
    using System;
    using System.ServiceModel.Channels;

    public class MockChannelListener : ChannelListenerBase<IReplyChannel>
    {
        private Uri uri;
        private bool returnNullChannel;

        public MockChannelListener(bool returnNullChannel = false)
        {
            this.returnNullChannel = returnNullChannel;
        }

        public bool OnAbortCalled { get; private set; }

        public bool OnOpenCalled { get; private set; }

        public bool OnBeginOpenCalled { get; private set; }

        public bool OnEndOpenCalled { get; private set; }

        public bool OnCloseCalled { get; private set; }

        public bool OnBeginCloseCalled { get; private set; }

        public bool OnEndCloseCalled { get; private set; }

        public bool OnWaitForChannelCalled { get; private set; }

        public bool OnBeginWaitForChannelCalled { get; private set; }

        public bool OnEndWaitForChannelCalled { get; private set; }

        public bool OnAcceptChannelCalled { get; private set; }

        public bool OnBeginAcceptChannelCalled { get; private set; }

        public bool OnEndAcceptChannelCalled { get; private set; }

        public TimeSpan TimeoutParameter { get; private set; }

        public AsyncCallback CallbackParameter { get; private set; }

        public object StateParameter { get; private set; }

        public IAsyncResult ResultParameter { get; private set; }

        public MockAsyncResult AsyncResultReturned { get; private set; }

        public override Uri Uri
        {
            get { return this.uri; }
        }

        public void SetUri(Uri uri)
        {
            this.uri = uri;
        }

        protected override IReplyChannel OnAcceptChannel(TimeSpan timeout)
        {
            this.OnAcceptChannelCalled = true;
            this.TimeoutParameter = timeout;
            return (this.returnNullChannel) ? (IReplyChannel)null : new MockReplyChannel(this);
        }

        protected override IAsyncResult OnBeginAcceptChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.OnBeginAcceptChannelCalled = true;
            this.TimeoutParameter = timeout;
            this.CallbackParameter = callback;
            this.StateParameter = state;
            this.AsyncResultReturned = new MockAsyncResult();
            this.AsyncResultReturned.SetAsyncState(state);
            return this.AsyncResultReturned;
        }

        protected override IReplyChannel OnEndAcceptChannel(IAsyncResult result)
        {
            this.OnEndAcceptChannelCalled = true;
            this.ResultParameter = result;
            return (this.returnNullChannel) ? (IReplyChannel)null : new MockReplyChannel(this);
        }

        protected override IAsyncResult OnBeginWaitForChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.OnBeginWaitForChannelCalled = true;
            this.TimeoutParameter = timeout;
            this.CallbackParameter = callback;
            this.StateParameter = state;
            this.AsyncResultReturned = new MockAsyncResult();
            this.AsyncResultReturned.SetAsyncState(state);
            return this.AsyncResultReturned;
        }

        protected override bool OnEndWaitForChannel(IAsyncResult result)
        {
            this.OnEndWaitForChannelCalled = true;
            this.ResultParameter = result;
            return true;
        }

        protected override bool OnWaitForChannel(TimeSpan timeout)
        {
            this.OnWaitForChannelCalled = true;
            this.TimeoutParameter = timeout;
            return true;
        }

        protected override void OnAbort()
        {
            this.OnAbortCalled = true;
        }

        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.OnBeginCloseCalled = true;
            this.TimeoutParameter = timeout;
            this.CallbackParameter = callback;
            this.StateParameter = state;
            this.AsyncResultReturned = new MockAsyncResult();
            this.AsyncResultReturned.SetAsyncState(state);
            return this.AsyncResultReturned;
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.OnBeginOpenCalled = true;
            this.TimeoutParameter = timeout;
            this.CallbackParameter = callback;
            this.StateParameter = state;
            this.AsyncResultReturned = new MockAsyncResult();
            this.AsyncResultReturned.SetAsyncState(state);
            return this.AsyncResultReturned;
        }

        protected override void OnClose(TimeSpan timeout)
        {
            this.OnCloseCalled = true;
            this.TimeoutParameter = timeout;
        }

        protected override void OnEndClose(IAsyncResult result)
        {
            this.OnEndCloseCalled = true;
            this.ResultParameter = result;
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            this.OnEndOpenCalled = true;
            this.ResultParameter = result;
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            this.OnOpenCalled = true;
            this.TimeoutParameter = timeout;
        }
    }

}
