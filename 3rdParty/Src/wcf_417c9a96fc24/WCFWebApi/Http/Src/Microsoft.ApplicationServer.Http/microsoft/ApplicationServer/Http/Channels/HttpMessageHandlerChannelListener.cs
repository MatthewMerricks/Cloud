// <copyright>
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Channels
{
    using System;
    using System.ServiceModel.Channels;
    using Microsoft.ApplicationServer.ServiceModel.Channels;

    // TODO: CSDMAIN 205175 -- reactivate when tracing and logging are available:
    //// using TD2 = System.ServiceModel.Web.Diagnostics.Application.TD;

    /// <summary>
    /// Defines a <see cref="IChannelListener"/> for creating <see cref="HttpMessageHandlerChannel"/> instances.
    /// </summary>
    internal class HttpMessageHandlerChannelListener : LayeredChannelListener<IReplyChannel>
    {
        private IChannelListener<IReplyChannel> innerChannelListener;
        private HttpMessageHandlerFactory httpMessageHandlerFactory;
        private bool asynchronousSendEnabled;
        private bool includeExceptionDetailInFaults;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpMessageHandlerChannelListener"/> class.
        /// </summary>
        /// <param name="binding">The <see cref="Binding"/> using this <see cref="HttpMessageHandlerChannelListener"/>.</param>
        /// <param name="innerListener">The underlying <see cref="IChannelListener"/> instance.</param>
        /// <param name="httpMessageHandlerFactory">The <see cref="HttpMessageHandlerFactory"/> used on instantiate a <see cref="HttpMessageHandlerChannel"/>.</param>
        /// <param name="asynchronousSendEnabled">Value indicating whether asynchronous send behavior is enabled on <see cref="RequestContext"/>.</param>
        /// <param name="includeExceptionDetailInFaults">Value indicating whether to include managed exception information in the detail of HTTP Internal Error responses.</param>
        public HttpMessageHandlerChannelListener(Binding binding, IChannelListener<IReplyChannel> innerListener, HttpMessageHandlerFactory httpMessageHandlerFactory, bool asynchronousSendEnabled, bool includeExceptionDetailInFaults)
            : base(binding, innerListener)
        {
            this.httpMessageHandlerFactory = httpMessageHandlerFactory;
            this.asynchronousSendEnabled = asynchronousSendEnabled;
            this.includeExceptionDetailInFaults = includeExceptionDetailInFaults;
        }

        /// <summary>
        /// Invoked during the transition of a communication object into the opening state.
        /// </summary>
        protected override void OnOpening()
        {
            this.innerChannelListener = (IChannelListener<IReplyChannel>)this.InnerChannelListener;
            base.OnOpening();
        }

        /// <summary>
        /// Provides an extensibility point when accepting a channel.
        /// </summary>
        /// <param name="timeout">The <see cref="TimeSpan"/> that specifies how long the accept channel operation has to complete before timing out.</param>
        /// <returns>The <see cref="IReplyChannel"/> accepted.</returns>
        protected override IReplyChannel OnAcceptChannel(TimeSpan timeout)
        {
            IReplyChannel innerChannel = this.innerChannelListener.AcceptChannel(timeout);
            return this.WrapInnerChannel(innerChannel);
        }

        /// <summary>
        /// Provides an asynchronous extensibility point when beginning to accept a channel.
        /// </summary>
        /// <param name="timeout">The <see cref="TimeSpan"/> that specifies how long the accept channel operation has to complete before timing out.</param>
        /// <param name="callback">The <see cref="AsyncCallback"/> delegate that receives the notification of the asynchronous completion of the accept channel operation.</param>
        /// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous accept channel operation.</param>
        /// <returns>The <see cref="IAsyncResult"/> that references the asynchronous accept channel operation.</returns>
        protected override IAsyncResult OnBeginAcceptChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return this.innerChannelListener.BeginAcceptChannel(timeout, callback, state);
        }

        /// <summary>
        /// Provides an asynchronous extensibility point when completing the acceptance a channel.
        /// </summary>
        /// <param name="result">The <see cref="IAsyncResult"/> returned by a call to the <see cref="OnBeginAcceptChannel"/> method.</param>
        /// <returns>The <see cref="IReplyChannel"/> accepted by the listener.</returns>
        protected override IReplyChannel OnEndAcceptChannel(IAsyncResult result)
        {
            IReplyChannel innerChannel = this.innerChannelListener.EndAcceptChannel(result);
            return this.WrapInnerChannel(innerChannel);
        }

        /// <summary>
        /// Provides a point of extensibility when starting to wait for a channel to arrive.
        /// </summary>
        /// <param name="timeout">The <see cref="TimeSpan"/> that specifies how long the on begin wait operation has to complete before timing out.</param>
        /// <param name="callback">The <see cref="AsyncCallback"/> delegate that receives the notification of the asynchronous operation on begin wait completion.</param>
        /// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous on begin wait operation.</param>
        /// <returns>The <see cref="IAsyncResult"/> that references the asynchronous on begin wait operation. </returns>
        protected override IAsyncResult OnBeginWaitForChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return this.innerChannelListener.BeginWaitForChannel(timeout, callback, state);
        }

        /// <summary>
        /// Provides a point of extensibility when ending the waiting for a channel to arrive.
        /// </summary>
        /// <param name="result">The <see cref="IAsyncResult"/> returned by a call to the <see cref="OnBeginWaitForChannel"/> method.</param>
        /// <returns>true if the method completed before the interval of time specified by the timeout expired; otherwise false.</returns>
        protected override bool OnEndWaitForChannel(IAsyncResult result)
        {
            return this.innerChannelListener.EndWaitForChannel(result);
        }

        /// <summary>
        /// Provides a point of extensibility when waiting for a channel to arrive.
        /// </summary>
        /// <param name="timeout">The <see cref="TimeSpan"/> that specifies how long the on wait for a channel operation has to complete before timing out.</param>
        /// <returns>true if the method completed before the interval of time specified by the timeout expired; otherwise false.</returns>
        protected override bool OnWaitForChannel(TimeSpan timeout)
        {
            return this.innerChannelListener.WaitForChannel(timeout);
        }

        private IReplyChannel WrapInnerChannel(IReplyChannel innerChannel)
        {
            if (innerChannel == null)
            {
                return innerChannel;
            }

            if (this.httpMessageHandlerFactory == null)
            {
                return innerChannel;
            }

            // TODO: CSDMAIN 205175 -- reactivate when tracing and logging are available:
            //// if (TD2.CreateHttpMessageHandlerChannelIsEnabled())
            //// {
            ////     TD2.CreateHttpMessageHandlerChannel(typeof(HttpHandlerChannel).Name, this);
            //// }

            return new HttpMessageHandlerChannel(this, innerChannel, this.httpMessageHandlerFactory, this.asynchronousSendEnabled, this.includeExceptionDetailInFaults);
        }
    }
}
