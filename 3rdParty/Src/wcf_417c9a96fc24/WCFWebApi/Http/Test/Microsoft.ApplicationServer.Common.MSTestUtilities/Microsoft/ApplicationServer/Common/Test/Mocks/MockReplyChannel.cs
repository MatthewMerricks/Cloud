// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Mocks
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Channels;

    public class MockReplyChannel : ChannelBase, IReplyChannel
    {
        private EndpointAddress localAddress;

        public MockReplyChannel(ChannelManagerBase channelManager)
            : base(channelManager)
        {
        }

        public TimeSpan TimeoutParameter { get; private set; }

        public AsyncCallback CallbackParameter { get; private set; }

        public object StateParameter { get; private set; }

        public IAsyncResult ResultParameter { get; private set; }

        public bool OnAbortCalled { get; private set; }

        public bool OnOpenCalled { get; private set; }

        public bool OnBeginOpenCalled { get; private set; }

        public bool OnEndOpenCalled { get; private set; }

        public bool OnCloseCalled { get; private set; }

        public bool OnBeginCloseCalled { get; private set; }

        public bool OnEndCloseCalled { get; private set; }
        
        public bool BeginReceiveRequestCalled { get; private set; }

        public bool BeginTryReceiveRequestCalled { get; private set; }

        public bool BeginWaitForRequestCalled { get; private set; }

        public bool EndReceiveRequestCalled { get; private set; }

        public bool EndTryReceiveRequestCalled { get; private set; }

        public bool EndWaitForRequestCalled { get; private set; }

        public bool ReceiveRequestCalled { get; private set; }

        public bool TryReceiveRequestCalled { get; private set; }

        public bool WaitForRequestCalled { get; private set; }

        public MockAsyncResult AsyncResultReturned { get; private set; }

        public RequestContext RequestContextToReturn { get; set; }

        public bool TryReceiveRequestReturnsTrue { get; set; }

        public bool WaitReceiveRequestReturnsTrue { get; set; }

        public EndpointAddress LocalAddress
        {
            get
            {
                return this.localAddress;
            }
        }

        public IAsyncResult BeginReceiveRequest(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.BeginReceiveRequestCalled = true;
            this.TimeoutParameter = timeout;
            this.CallbackParameter = callback;
            this.StateParameter = state;
            this.AsyncResultReturned = new MockAsyncResult();
            this.AsyncResultReturned.SetAsyncState(state);
            return this.AsyncResultReturned;
        }

        public IAsyncResult BeginReceiveRequest(AsyncCallback callback, object state)
        {
            this.BeginReceiveRequestCalled = true;
            this.CallbackParameter = callback;
            this.StateParameter = state;
            this.AsyncResultReturned = new MockAsyncResult();
            this.AsyncResultReturned.SetAsyncState(state);
            return this.AsyncResultReturned;
        }

        public IAsyncResult BeginTryReceiveRequest(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.BeginTryReceiveRequestCalled = true;
            this.TimeoutParameter = timeout;
            this.CallbackParameter = callback;
            this.StateParameter = state;
            this.AsyncResultReturned = new MockAsyncResult();
            this.AsyncResultReturned.SetAsyncState(state);
            return this.AsyncResultReturned;
        }

        public IAsyncResult BeginWaitForRequest(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.BeginWaitForRequestCalled = true;
            this.TimeoutParameter = timeout;
            this.CallbackParameter = callback;
            this.StateParameter = state;
            this.AsyncResultReturned = new MockAsyncResult();
            this.AsyncResultReturned.SetAsyncState(state);
            return this.AsyncResultReturned;
        }

        public RequestContext EndReceiveRequest(IAsyncResult result)
        {
            this.EndReceiveRequestCalled = true;
            this.ResultParameter = result;
            return this.RequestContextToReturn;
        }

        public bool EndTryReceiveRequest(IAsyncResult result, out RequestContext context)
        {
            this.EndTryReceiveRequestCalled = true;
            this.ResultParameter = result;
            context = this.RequestContextToReturn;
            return this.TryReceiveRequestReturnsTrue;
        }

        public bool EndWaitForRequest(IAsyncResult result)
        {
            this.EndWaitForRequestCalled = true;
            this.ResultParameter = result;
            return this.WaitReceiveRequestReturnsTrue;
        }

        public void SetLocalAddress(EndpointAddress localAddress)
        {
            this.localAddress = localAddress;
        }

        public RequestContext ReceiveRequest(TimeSpan timeout)
        {
            this.ReceiveRequestCalled = true;
            this.TimeoutParameter = timeout;
            return this.RequestContextToReturn;
        }

        public RequestContext ReceiveRequest()
        {
            return this.RequestContextToReturn;
        }

        public bool TryReceiveRequest(TimeSpan timeout, out RequestContext context)
        {
            this.TryReceiveRequestCalled = true;
            this.TimeoutParameter = timeout;
            context = this.RequestContextToReturn;
            return this.TryReceiveRequestReturnsTrue;
        }

        public bool WaitForRequest(TimeSpan timeout)
        {
            this.WaitForRequestCalled = true;
            this.TimeoutParameter = timeout;
            return this.WaitReceiveRequestReturnsTrue;
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
