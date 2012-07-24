// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Channels
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http;
    using Microsoft.ApplicationServer.Http.Channels;
    using Microsoft.ApplicationServer.Common.Test.Mocks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HttpMessageEncodingChannelListenerTests
    {
        #region Type Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingChannelListener is an internal, non-abstract class.")]
        public void HttpMessageEncodingChannelListener_Is_An_Internal_Non_Abstract_Class()
        {
            Type type = typeof(HttpMessageEncodingChannelListener);
            Assert.IsTrue(type.IsNotPublic, "HttpMessageEncodingChannelListener should not be public.");
            Assert.IsFalse(type.IsVisible, "HttpMessageEncodingChannelListener should not be visible.");
            Assert.IsFalse(type.IsAbstract, "HttpMessageEncodingChannelListener should not be abstract.");
            Assert.IsTrue(type.IsClass, "HttpMessageEncodingChannelListener should be a class.");
        }

        #endregion Type Tests

        #region Property Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingChannelListener.Uri returns the Uri of the inner listener.")]
        public void Uri_Returns_Inner_Listener_Uri()
        {
            MockChannelListener innerListener = new MockChannelListener();
            Uri innerUri = new Uri("http://innerListenerUri.org/");
            innerListener.SetUri(innerUri);
            HttpMessageEncodingChannelListener listener = new HttpMessageEncodingChannelListener(new HttpBinding(), innerListener);
            Uri uri = listener.Uri;

            Assert.IsNotNull(uri, "HttpMessageEncodingChannelListener.Uri should not have returned null.");
            Assert.AreSame(innerUri, uri, "HttpMessageEncodingChannelListener.Uri should have returned the same Uri instance as the inner listener.");
        }

        #endregion Property Tests

        #region AcceptChannel Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingChannelListener.AcceptChannel calls AcceptChannel on the inner listener with the given timeout value.")]
        public void AcceptChannel_Calls_The_Inner_Listener_With_Timeout()
        {
            MockChannelListener innerListener = new MockChannelListener();
            HttpMessageEncodingChannelListener listener = new HttpMessageEncodingChannelListener(new HttpBinding(), innerListener);
            listener.Open();

            TimeSpan timeout = new TimeSpan(0,2,0);
            IReplyChannel replyChannel = listener.AcceptChannel(timeout);

            Assert.IsTrue(innerListener.OnAcceptChannelCalled, "HttpMessageEncodingChannelListener.AcceptChannel should have called AcceptChannel on the inner listener.");
            Assert.AreEqual(innerListener.TimeoutParameter, timeout, "HttpMessageEncodingChannelListener.AcceptChannel should have passed along the same timeout instance to the inner listener.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingChannelListener.AcceptChannel returns null if the inner listener returns null.")]
        public void AcceptChannel_Returns_Null_If_Inner_Listener_Returns_Null()
        {
            MockChannelListener innerListener = new MockChannelListener(true /* return null channel */);
            HttpMessageEncodingChannelListener listener = new HttpMessageEncodingChannelListener(new HttpBinding(), innerListener);
            listener.Open();

            IReplyChannel replyChannel = listener.AcceptChannel();

            Assert.IsNull(replyChannel, "HttpMessageEncodingChannelListener.AcceptChannel should have returned null since the inner listener returned null.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingChannelListener.AcceptChannel returns an HttpMessageEncodingReplyChannel.")]
        public void AcceptChannel_Returns_HttpMessageEncodingReplyChannel()
        {
            MockChannelListener innerListener = new MockChannelListener();
            HttpMessageEncodingChannelListener listener = new HttpMessageEncodingChannelListener(new HttpBinding(), innerListener);
            listener.Open();

            IReplyChannel replyChannel = listener.AcceptChannel();

            Assert.IsNotNull(replyChannel, "HttpMessageEncodingChannelListener.AcceptChannel should not have returned null.");
            Assert.IsInstanceOfType(replyChannel, typeof(HttpMessageEncodingReplyChannel), "HttpMessageEncodingChannelListener.AcceptChannel should have returned an HttpMessageEncodingReplyChannel.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingChannelListener.BeginAcceptChannel calls BeginAcceptChannel on the inner listener with the given parameters.")]
        public void BeginAcceptChannel_Calls_The_Inner_Listener()
        {
            MockChannelListener innerListener = new MockChannelListener();
            HttpMessageEncodingChannelListener listener = new HttpMessageEncodingChannelListener(new HttpBinding(), innerListener);
            listener.Open();

            object state = new object();
            AsyncCallback callback = MockAsyncCallback.Create();
            TimeSpan timeout = new TimeSpan(0,2,0);
            IAsyncResult asyncResult = listener.BeginAcceptChannel(timeout, callback, state);

            Assert.IsTrue(innerListener.OnBeginAcceptChannelCalled, "HttpMessageEncodingChannelListener.BeginAcceptChannel should have called BeginAcceptChannel on the inner listener.");
            Assert.AreSame(callback, innerListener.CallbackParameter, "HttpMessageEncodingChannelListener.BeginAcceptChannel should have passed the callback parameter along to the inner listener.");
            Assert.AreEqual(timeout, innerListener.TimeoutParameter, "HttpMessageEncodingChannelListener.BeginAcceptChannel should have passed the timeout parameter along to the inner listener.");
            Assert.AreSame(state, innerListener.StateParameter, "HttpMessageEncodingChannelListener.BeginAcceptChannel should have passed the state parameter along to the inner listener.");
            Assert.AreSame(asyncResult, innerListener.AsyncResultReturned, "HttpMessageEncodingChannelListener.BeginAcceptChannel should returned the same asyncResult as the inner listener.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingChannelListener.EndAcceptChannel calls EndAcceptChannel on the inner listener with the given parameters.")]
        public void EndAcceptChannel_Calls_The_Inner_Listener()
        {
            MockChannelListener innerListener = new MockChannelListener();
            HttpMessageEncodingChannelListener listener = new HttpMessageEncodingChannelListener(new HttpBinding(), innerListener);
            listener.Open();

            object state = new object();
            AsyncCallback callback = MockAsyncCallback.Create();
            TimeSpan timeout = new TimeSpan(0, 2, 0);
            IAsyncResult result = listener.BeginAcceptChannel(timeout, callback, state);
            listener.EndAcceptChannel(result);

            Assert.IsTrue(innerListener.OnEndAcceptChannelCalled, "HttpMessageEncodingChannelListener.EndAcceptChannel should have called EndAcceptChannel on the inner listener.");
            Assert.AreSame(result, innerListener.ResultParameter, "HttpMessageEncodingChannelListener.EndAcceptChannel should have passed the result parameter along to the inner listener.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingChannelListener.EndAcceptChannel returns null if the inner listener returns null.")]
        public void EndAcceptChannel_Returns_Null_If_The_Inner_Listener_Returns_Null()
        {
            MockChannelListener innerListener = new MockChannelListener(true /* return null channel */);
            HttpMessageEncodingChannelListener listener = new HttpMessageEncodingChannelListener(new HttpBinding(), innerListener);
            listener.Open();

            object state = new object();
            AsyncCallback callback = MockAsyncCallback.Create();
            TimeSpan timeout = new TimeSpan(0, 2, 0);
            IAsyncResult result = listener.BeginAcceptChannel(timeout, callback, state);
            IReplyChannel channel = listener.EndAcceptChannel(result);

            Assert.IsNull(channel, "HttpMessageEncodingChannelListener.EndAcceptChannel should have returned null since the inner listener returned null.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingChannelListener.EndAcceptChannel returns HttpMessageEncodingReplyChannel if the inner listener does not return null.")]
        public void EndAcceptChannel_Returns_HttpMessageEncodingChannel()
        {
            MockChannelListener innerListener = new MockChannelListener();
            HttpMessageEncodingChannelListener listener = new HttpMessageEncodingChannelListener(new HttpBinding(), innerListener);
            listener.Open();

            object state = new object();
            AsyncCallback callback = MockAsyncCallback.Create();
            TimeSpan timeout = new TimeSpan(0, 2, 0);
            IAsyncResult result = listener.BeginAcceptChannel(timeout, callback, state);
            IReplyChannel channel = listener.EndAcceptChannel(result);

            Assert.IsNotNull(channel, "HttpMessageEncodingChannelListener.EndAcceptChannel should not have returned null since the inner listener did not return null.");
            Assert.IsInstanceOfType(channel, typeof(HttpMessageEncodingReplyChannel), "HttpMessageEncodingChannelListener.EndAcceptChannel should have returned an HttpMessageEncodingReplyChannel.");
        }

        #endregion AcceptChannel Tests

        #region WaitForChannel Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingChannelListener.WaitForChannel calls WaitForChannel on the inner listener with the given timeout value.")]
        public void WaitForChannel_Calls_The_Inner_Listener_With_Timeout()
        {
            MockChannelListener innerListener = new MockChannelListener();
            HttpMessageEncodingChannelListener listener = new HttpMessageEncodingChannelListener(new HttpBinding(), innerListener);
            listener.Open();

            TimeSpan timeout = new TimeSpan(0, 2, 0);
            bool waitResult = listener.WaitForChannel(timeout);

            Assert.IsTrue(waitResult, "HttpMessageEncodingChannelListener.WaitForChannel should have returned the value from calling WaitForChannel on the inner listener.");
            Assert.IsTrue(innerListener.OnWaitForChannelCalled, "HttpMessageEncodingChannelListener.WaitForChannel should have called WaitForChannel on the inner listener.");
            Assert.AreEqual(innerListener.TimeoutParameter, timeout, "HttpMessageEncodingChannelListener.WaitForChannel should have passed along the same timeout instance to the inner listener.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingChannelListener.BeginWaitForChannel calls BeginWaitForChannel on the inner listener with the given parameters.")]
        public void BeginWaitForChannel_Calls_The_Inner_Listener()
        {
            MockChannelListener innerListener = new MockChannelListener();
            HttpMessageEncodingChannelListener listener = new HttpMessageEncodingChannelListener(new HttpBinding(), innerListener);
            listener.Open();

            object state = new object();
            AsyncCallback callback = MockAsyncCallback.Create();
            TimeSpan timeout = new TimeSpan(0, 2, 0);
            IAsyncResult asyncResult = listener.BeginWaitForChannel(timeout, callback, state);

            Assert.IsTrue(innerListener.OnBeginWaitForChannelCalled, "HttpMessageEncodingChannelListener.BeginWaitForChannel should have called BeginWaitForChannel on the inner listener.");
            Assert.AreSame(callback, innerListener.CallbackParameter, "HttpMessageEncodingChannelListener.BeginWaitForChannel should have passed the callback parameter along to the inner listener.");
            Assert.AreEqual(timeout, innerListener.TimeoutParameter, "HttpMessageEncodingChannelListener.BeginWaitForChannel should have passed the timeout parameter along to the inner listener.");
            Assert.AreSame(state, innerListener.StateParameter, "HttpMessageEncodingChannelListener.BeginWaitForChannel should have passed the state parameter along to the inner listener.");
            Assert.AreSame(asyncResult, innerListener.AsyncResultReturned, "HttpMessageEncodingChannelListener.BeginWaitForChannel should returned the same asyncResult as the inner listener.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingChannelListener.EndWaitForChannel calls EndWaitForChannel on the inner listener with the given parameters.")]
        public void EndWaitForChannel_Calls_The_Inner_Listener()
        {
            MockChannelListener innerListener = new MockChannelListener();
            HttpMessageEncodingChannelListener listener = new HttpMessageEncodingChannelListener(new HttpBinding(), innerListener);
            listener.Open();

            object state = new object();
            AsyncCallback callback = MockAsyncCallback.Create();
            TimeSpan timeout = new TimeSpan(0, 2, 0);
            IAsyncResult result = listener.BeginWaitForChannel(timeout, callback, state);
            bool waitResult = listener.EndWaitForChannel(result);

            Assert.IsTrue(waitResult, "HttpMessageEncodingChannelListener.EndWaitForChannel should have returned the value from calling EndWaitForChannel on the inner listener.");
            Assert.IsTrue(innerListener.OnEndWaitForChannelCalled, "HttpMessageEncodingChannelListener.EndWaitForChannel should have called EndWaitForChannel on the inner listener.");
            Assert.AreSame(result, innerListener.ResultParameter, "HttpMessageEncodingChannelListener.EndWaitForChannel should have passed the result parameter along to the inner listener.");
        }

        #endregion WaitForChannel Tests

        #region Open Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingChannelListener.Open calls Open on the inner listener with the given timeout value.")]
        public void Open_Calls_The_Inner_Listener_With_Timeout()
        {
            MockChannelListener innerListener = new MockChannelListener();
            HttpMessageEncodingChannelListener listener = new HttpMessageEncodingChannelListener(new HttpBinding(), innerListener);

            TimeSpan timeout = new TimeSpan(0, 2, 0);
            listener.Open(timeout);

            Assert.IsTrue(innerListener.OnOpenCalled, "HttpMessageEncodingChannelListener.Open should have called Open on the inner listener.");
            Assert.AreEqual(innerListener.TimeoutParameter, timeout, "HttpMessageEncodingChannelListener.Open should have passed along the same timeout instance to the inner listener.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingChannelListener.BeginOpen calls BeginOpen on the inner listener with the given parameters.")]
        public void BeginOpen_Calls_The_Inner_Listener()
        {
            MockChannelListener innerListener = new MockChannelListener();
            HttpMessageEncodingChannelListener listener = new HttpMessageEncodingChannelListener(new HttpBinding(), innerListener);

            TimeSpan timeout = new TimeSpan(0, 2, 0);
            listener.BeginOpen(timeout, null, null);

            Assert.IsTrue(innerListener.OnBeginOpenCalled, "HttpMessageEncodingChannelListener.BeginOpen should have called BeginOpen on the inner listener.");
            Assert.AreEqual(timeout, innerListener.TimeoutParameter, "HttpMessageEncodingChannelListener.BeginOpen should have passed the timeout parameter along to the inner listener.");
        }
        
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingChannelListener.EndOpen calls EndOpen on the inner listener with the given parameters.")]
        public void EndOpen_Calls_The_Inner_Listener()
        {
            MockChannelListener innerListener = new MockChannelListener();
            HttpMessageEncodingChannelListener listener = new HttpMessageEncodingChannelListener(new HttpBinding(), innerListener);

            TimeSpan timeout = new TimeSpan(0, 2, 0);
            IAsyncResult result = listener.BeginOpen(timeout, null, null);
            listener.EndOpen(result);

            Assert.IsTrue(innerListener.OnEndOpenCalled, "HttpMessageEncodingChannelListener.EndOpen should have called EndOpen on the inner listener.");
        }

        #endregion Open Tests

        #region Close Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingChannelListener.Close calls Close on the inner listener with the given timeout value.")]
        public void Close_Calls_The_Inner_Listener_With_Timeout()
        {
            MockChannelListener innerListener = new MockChannelListener();
            HttpMessageEncodingChannelListener listener = new HttpMessageEncodingChannelListener(new HttpBinding(), innerListener);
            listener.Open();

            TimeSpan timeout = new TimeSpan(0, 2, 0);
            listener.Close(timeout);

            Assert.IsTrue(innerListener.OnCloseCalled, "HttpMessageEncodingChannelListener.Close should have called Close on the inner listener.");
            Assert.AreEqual(innerListener.TimeoutParameter, timeout, "HttpMessageEncodingChannelListener.Close should have passed along the same timeout instance to the inner listener.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingChannelListener.BeginClose calls BeginClose on the inner listener with the given parameters.")]
        public void BeginClose_Calls_The_Inner_Listener()
        {
            MockChannelListener innerListener = new MockChannelListener();
            HttpMessageEncodingChannelListener listener = new HttpMessageEncodingChannelListener(new HttpBinding(), innerListener);
            listener.Open();

            TimeSpan timeout = new TimeSpan(0, 2, 0);
            IAsyncResult asyncResult = listener.BeginClose(timeout, null, null);

            Assert.IsTrue(innerListener.OnBeginCloseCalled, "HttpMessageEncodingChannelListener.BeginClose should have called BeginClose on the inner listener.");
            Assert.AreEqual(timeout, innerListener.TimeoutParameter, "HttpMessageEncodingChannelListener.BeginClose should have passed the timeout parameter along to the inner listener.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingChannelListener.EndClose calls EndClose on the inner listener with the given parameters.")]
        public void EndClose_Calls_The_Inner_Listener()
        {
            MockChannelListener innerListener = new MockChannelListener();
            HttpMessageEncodingChannelListener listener = new HttpMessageEncodingChannelListener(new HttpBinding(), innerListener);
            listener.Open();

            object state = new object();
            AsyncCallback callback = MockAsyncCallback.Create();
            TimeSpan timeout = new TimeSpan(0, 2, 0);
            IAsyncResult result = listener.BeginClose(timeout, callback, state);
            listener.EndClose(result);

            Assert.IsTrue(innerListener.OnEndCloseCalled, "HttpMessageEncodingChannelListener.EndClose should have called EndClose on the inner listener.");
        }

        #endregion Close Tests

        #region Abort Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingChannelListener.Abort calls Abort on the inner listener.")]
        public void Abort_Calls_The_Inner_Listener()
        {
            MockChannelListener innerListener = new MockChannelListener();
            HttpMessageEncodingChannelListener listener = new HttpMessageEncodingChannelListener(new HttpBinding(), innerListener);

            listener.Abort();

            Assert.IsTrue(innerListener.OnAbortCalled, "HttpMessageEncodingChannelListener.Abort should have called Abort on the inner listener.");
        }

        #endregion Abort Tests
    }
}
