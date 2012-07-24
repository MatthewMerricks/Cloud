// <copyright>
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Channels
{
    using System;
    using System.Net.Http;
    using System.Reflection;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.ServiceModel.Channels;

    /// <summary>
    /// This <see cref="IReplyChannel"/> encapsulates an <see cref="HttpMessageChannel"/> enabling an easy way for 
    /// plugging into an <see cref="System.Security.Authentication.ExtendedProtection.ChannelBinding"/> using the <see cref="Task"/> based extensibility model provided by 
    /// <see cref="HttpMessageChannel"/>.
    /// </summary>
    internal class HttpMessageHandlerChannel : LayeredChannel<IReplyChannel>, IReplyChannel
    {
        private static readonly string requestPropertyKey = typeof(HttpMessageHandlerChannel).Name;
        private HttpMessageHandlerFactory httpMessageHandlerFactory;
        private volatile CancellationTokenSource channelCancellationTokenSource = new CancellationTokenSource();
        private volatile InnerHandler pipeline;
        private bool asynchronousSendEnabled;
        private bool includeExceptionDetailInFaults;
        private object thisLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpMessageHandlerChannel"/> class.
        /// </summary>
        /// <param name="channelManager">The <see cref="ChannelManagerBase"/> that provides default timeouts for the channel operations (send, receive, open, and close).</param>
        /// <param name="innerChannel">The underlying <see cref="IReplyChannel"/> instance.</param>
        /// <param name="httpMessageHandlerFactory">The <see cref="HttpMessageHandlerFactory"/> used on instantiate a <see cref="HttpMessageChannel"/>.</param>
        /// <param name="asynchronousSendEnabled">Value indicating whether asynchronous send behavior is enabled on <see cref="RequestContext"/>.</param>
        /// <param name="includeExceptionDetailInFaults">Value indicating whether to include managed exception information in the detail of HTTP Internal Error responses.</param>
        public HttpMessageHandlerChannel(ChannelManagerBase channelManager, IReplyChannel innerChannel, HttpMessageHandlerFactory httpMessageHandlerFactory, bool asynchronousSendEnabled, bool includeExceptionDetailInFaults)
            : base(channelManager, innerChannel)
        {
            this.httpMessageHandlerFactory = httpMessageHandlerFactory;
            this.asynchronousSendEnabled = asynchronousSendEnabled;
            this.includeExceptionDetailInFaults = includeExceptionDetailInFaults;
        }

        /// <summary>
        /// Gets the address on which this reply channel receives messages.
        /// </summary>
        public EndpointAddress LocalAddress
        {
            get { return this.InnerChannel.LocalAddress; }
        }

        /// <summary>
        /// Gets a value indicating whether asynchronous send behavior is enabled on <see cref="RequestContext"/>. 
        /// </summary>
        protected bool AsynchronousSendEnabled
        {
            get { return this.asynchronousSendEnabled; }
        }

        /// <summary>
        /// Gets a value indicating whether to include managed exception information in the detail of HTTP Server Error responses.
        /// </summary>
        protected bool IncludeExceptionDetailInFaults
        {
            get { return this.includeExceptionDetailInFaults; }
        }

        /// <summary>
        /// Gets the key used to identify the <see cref="HttpMessageHandlerRequestContext"/> instance used to process incoming 
        /// <see cref="HttpRequestMessage"/> requests.
        /// </summary>
        private static string RequestPropertyKey
        {
            get { return HttpMessageHandlerChannel.requestPropertyKey; }
        }

        /// <summary>
        /// Begins an asynchronous operation to receive an available request with a specified timeout.
        /// </summary>
        /// <param name="timeout">The <see cref="TimeSpan"/> that specifies the interval of time to wait for the reception of an available request.</param>
        /// <param name="callback">The <see cref="AsyncCallback"/> delegate that receives the notification of the asynchronous receive that a request operation completes.</param>
        /// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous receive of a request operation.</param>
        /// <returns>The <see cref="IAsyncResult"/> that references the asynchronous reception of the request.</returns>
        public IAsyncResult BeginReceiveRequest(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new ReceiveAsyncResult(this, timeout, callback, state, this.channelCancellationTokenSource);
        }

        /// <summary>
        /// Begins an asynchronous operation to receive an available request with a specified timeout.
        /// </summary>
        /// <param name="callback">The <see cref="AsyncCallback"/> delegate that receives the notification of the asynchronous receive that a request operation completes.</param>
        /// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous receive of a request operation.</param>
        /// <returns>The <see cref="IAsyncResult"/> that references the asynchronous reception of the request.</returns>
        public IAsyncResult BeginReceiveRequest(AsyncCallback callback, object state)
        {
            return this.BeginReceiveRequest(this.DefaultReceiveTimeout, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to receive a request message that has a specified time out and state object associated with it. 
        /// </summary>
        /// <param name="timeout">The <see cref="TimeSpan"/> that specifies how long the receive request operation has to complete before timing out and returning false.</param>
        /// <param name="callback">The <see cref="AsyncCallback"/> delegate that receives the notification of the asynchronous receive that a request operation completes.</param>
        /// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous receive of a request operation.</param>
        /// <returns>The <see cref="IAsyncResult"/> that references the asynchronous reception of the request.</returns>
        public IAsyncResult BeginTryReceiveRequest(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new TryReceiveAsyncResult(this, timeout, callback, state, this.channelCancellationTokenSource);
        }

        /// <summary>
        /// Begins an asynchronous request operation that has a specified time out and state object associated with it. 
        /// </summary>
        /// <param name="timeout">The <see cref="TimeSpan"/> that specifies the interval of time to wait for the reception of an available request.</param>
        /// <param name="callback">The <see cref="AsyncCallback"/> delegate that receives the notification of the asynchronous receive that a request operation completes.</param>
        /// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous receive of a request operation.</param>
        /// <returns>The <see cref="IAsyncResult"/> that references the asynchronous operation to wait for a request message to arrive.</returns>
        public IAsyncResult BeginWaitForRequest(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return this.InnerChannel.BeginWaitForRequest(timeout, callback, state);
        }

        /// <summary>
        /// Completes an asynchronous operation to receive an available request. 
        /// </summary>
        /// <param name="result">The <see cref="IAsyncResult"/> returned by a call to the BeginReceive() method.</param>
        /// <returns>The <see cref="RequestContext"/> used to construct a reply to the request. </returns>
        public RequestContext EndReceiveRequest(IAsyncResult result)
        {
            return ReceiveAsyncResult.End(result);
        }

        /// <summary>
        /// Completes the specified asynchronous operation to receive a request message.
        /// </summary>
        /// <param name="result">The <see cref="IAsyncResult"/> returned by a call to the <see cref="BeginTryReceiveRequest"/> method.</param>
        /// <param name="context">The <see cref="RequestContext"/> received.</param>
        /// <returns>true if a request message is received before the specified interval of time elapses; otherwise false.</returns>
        public bool EndTryReceiveRequest(IAsyncResult result, out RequestContext context)
        {
            return TryReceiveAsyncResult.End(result, out context);
        }

        /// <summary>
        /// Completes the specified asynchronous wait-for-a-request message operation.
        /// </summary>
        /// <param name="result">The <see cref="IAsyncResult"/> that identifies the <see cref="BeginWaitForRequest"/> operation to finish, and from which to retrieve an end result.</param>
        /// <returns>true if a request is received before the specified interval of time elapses; otherwise false.</returns>
        public bool EndWaitForRequest(IAsyncResult result)
        {
            return this.InnerChannel.EndWaitForRequest(result);
        }

        /// <summary>
        /// Returns the context of the request received, if one is available. If a context is not available, waits until there is one available. 
        /// </summary>
        /// <param name="timeout">The <see cref="TimeSpan"/> that specifies how long the receive of a request operation has to complete before timing out.</param>
        /// <returns>The <see cref="RequestContext"/> used to construct replies. </returns>
        public RequestContext ReceiveRequest(TimeSpan timeout)
        {
            ReceiveAsyncResult result = new ReceiveAsyncResult(this, timeout, null, null, this.channelCancellationTokenSource);
            return ReceiveAsyncResult.End(result);
        }

        /// <summary>
        /// Returns the context of the request received, if one is available. If a context is not available, waits until there is one available. 
        /// </summary>
        /// <returns>The <see cref="RequestContext"/> used to construct replies.</returns>
        public RequestContext ReceiveRequest()
        {
            return this.ReceiveRequest(this.DefaultReceiveTimeout);
        }

        /// <summary>
        /// Returns a value that indicates whether a request is received before a specified interval of time elapses. 
        /// </summary>
        /// <param name="timeout">The <see cref="TimeSpan"/> that specifies how long the receive of a request operation has to complete before timing out and returning false.</param>
        /// <param name="context">The <see cref="RequestContext"/> received.</param>
        /// <returns>true if a request message is received before the specified interval of time elapses; otherwise false.</returns>
        public bool TryReceiveRequest(TimeSpan timeout, out RequestContext context)
        {
            TryReceiveAsyncResult result = new TryReceiveAsyncResult(this, timeout, null, null, this.channelCancellationTokenSource);
            return TryReceiveAsyncResult.End(result, out context);
        }

        /// <summary>
        /// Returns a value that indicates whether a request message is received before a specified interval of time elapses.
        /// </summary>
        /// <param name="timeout">The <see cref="TimeSpan"/> that specifies how long a request operation has to complete before timing out and returning false.</param>
        /// <returns>true if a request is received before the specified interval of time elapses; otherwise false.</returns>
        public bool WaitForRequest(TimeSpan timeout)
        {
            return this.InnerChannel.WaitForRequest(timeout);
        }

        /// <summary>
        /// Invoked during the transition of a communication object into the opened state.
        /// </summary>
        protected override void OnOpened()
        {
            HttpMessageChannel innerPipeline;

            // Create pipeline which we use for all request/response pairs going through this channel instance.
            // In case of an exception or null we use an error handler to serve error-only responses instead. This
            // makes it much easier to see what went wrong.
            try
            {
                innerPipeline = this.httpMessageHandlerFactory.Create(new HttpMessageHandlerChannel.OuterHandler(this));
                if (innerPipeline == null)
                {
                    throw Fx.Exception.AsError(
                        new InvalidOperationException(SR.HttpMessageHandlerChannelFactoryNullPipeline(this.httpMessageHandlerFactory.GetType().Name, typeof(HttpMessageHandlerChannel).Name)));
                }
            }
            catch (Exception e)
            {
                Fx.Exception.AsError(e);

                if (Fx.IsFatal(e))
                {
                    throw;
                }

                innerPipeline = new HttpMessageHandlerChannel.ErrorHandler(new HttpMessageHandlerChannel.OuterHandler(this), this, e);
            }

            this.pipeline = new HttpMessageHandlerChannel.InnerHandler(innerPipeline);
            base.OnOpened();
        }

        /// <summary>
        /// Invoked during the transition of a communication object into the closing state.
        /// </summary>
        protected override void OnClosed()
        {
            base.OnClosed();
            this.Cleanup();
        }

        /// <summary>
        /// Inserts processing on a communication object after it transitions to the closing state due to the invocation of a synchronous abort operation.
        /// </summary>
        protected override void OnAbort()
        {
            base.OnAbort();
            this.Cleanup();
        }

        private void Cleanup()
        {
            if (this.channelCancellationTokenSource != null)
            {
                lock (this.thisLock)
                {
                    if (this.channelCancellationTokenSource != null)
                    {
                        this.channelCancellationTokenSource.Cancel();

                        // TODO: CSDMAIN 204727 Revert this commented code when move to 4.5
                        // this.channelCancellationTokenSource.Dispose();
                        this.channelCancellationTokenSource = null;
                    }
                }

                if (this.pipeline != null)
                {
                    lock (this.thisLock)
                    {
                        if (this.pipeline != null)
                        {
                            this.pipeline.Dispose();
                            this.pipeline = null;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Encapsulates the results of <see cref="BeginTryReceiveRequest"/>.
        /// </summary>
        private sealed class TryReceiveAsyncResult : ReceiveAsyncResultBase
        {
            private static AsyncCallback onReceive = Fx.ThunkCallback(new AsyncCallback(TryReceiveAsyncResult.OnReceive));
            private bool receiveSuccess;

            /// <summary>
            /// Initializes a new instance of the <see cref="TryReceiveAsyncResult"/> class.
            /// </summary>
            /// <param name="channel">The <see cref="HttpMessageHandlerChannel"/> within which we are operating.</param>
            /// <param name="timeout">The <see cref="TimeSpan"/> that specifies how long the receive request operation has to complete before timing out and returning false.</param>
            /// <param name="callback">The <see cref="AsyncCallback"/> delegate that receives the notification of the asynchronous receive that a request operation completes.</param>
            /// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous receive of a request operation.</param>
            /// <param name="cancellationTokenSource">Token source used to cancel operation.</param>
            public TryReceiveAsyncResult(HttpMessageHandlerChannel channel, TimeSpan timeout, AsyncCallback callback, object state, CancellationTokenSource cancellationTokenSource)
                : base(channel, timeout, callback, state, cancellationTokenSource)
            {
                if (channel.IsDisposed)
                {
                    this.Context = null;
                    this.receiveSuccess = true;
                    this.Complete(true);
                }
                else
                {
                    this.StartReceive();
                }
            }

            /// <summary>
            /// Completes the specified asynchronous operation to receive a request.
            /// </summary>
            /// <param name="result">The <see cref="IAsyncResult"/> instance.</param>
            /// <param name="context">The <see cref="RequestContext"/> received.</param>
            /// <returns>true if a request message is received before the specified interval of time elapses; otherwise false.</returns>
            public static bool End(IAsyncResult result, out RequestContext context)
            {
                TryReceiveAsyncResult thisPtr = AsyncResult.End<TryReceiveAsyncResult>(result);
                context = thisPtr.Context;
                return thisPtr.receiveSuccess;
            }

            /// <summary>
            /// Start receiving a request from the inner channel
            /// </summary>
            protected override void StartReceive()
            {
                if (this.IsCompleted)
                {
                    return;
                }

                try
                {
                    IAsyncResult result = this.Channel.InnerChannel.BeginTryReceiveRequest(this.Timeout, TryReceiveAsyncResult.onReceive, this);
                    if (result.CompletedSynchronously)
                    {
                        this.CompleteReceive(result);
                    }
                }
                catch (TimeoutException e)
                {
                    Fx.Exception.AsError(e);
                    this.Complete(true);
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    this.Complete(true, e);
                }
            }

            /// <summary>
            /// Complete receiving a request from the inner channel
            /// </summary>
            /// <param name="result">The <see cref="IAsyncResult"/> returned by a call to the BeginTryReceiveRequest method.</param>
            protected override void CompleteReceive(IAsyncResult result)
            {
                RequestContext innerContext;
                if (this.receiveSuccess = this.Channel.InnerChannel.EndTryReceiveRequest(result, out innerContext))
                {
                    if (innerContext != null)
                    {
                        this.Context = new HttpMessageHandlerRequestContext(
                            this,
                            innerContext,
                            this.Channel.DefaultSendTimeout,
                            this.CancellationToken,
                            this.Channel);
                        this.Context.StartHttpMessageChannel();
                    }
                    else
                    {
                        this.Context = null;
                        this.Complete(result.CompletedSynchronously);
                    }
                }
                else
                {
                    this.Context = null;
                    this.Complete(result.CompletedSynchronously);
                }
            }

            private static void OnReceive(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }

                TryReceiveAsyncResult thisPtr = (TryReceiveAsyncResult)result.AsyncState;
                try
                {
                    thisPtr.CompleteReceive(result);
                }
                catch (TimeoutException e)
                {
                    Fx.Exception.AsError(e);
                    thisPtr.Complete(false);
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    thisPtr.Complete(false, e);
                }
            }
        }

        /// <summary>
        /// Encapsulates the results of <see cref="HttpMessageHandlerChannel.BeginReceiveRequest"/>.
        /// </summary>
        private sealed class ReceiveAsyncResult : ReceiveAsyncResultBase
        {
            private static AsyncCallback onReceive = Fx.ThunkCallback(new AsyncCallback(ReceiveAsyncResult.OnReceive));

            /// <summary>
            /// Initializes a new instance of the <see cref="ReceiveAsyncResult"/> class.
            /// </summary>
            /// <param name="channel">The <see cref="HttpMessageHandlerChannel"/> within which we are operating.</param>
            /// <param name="timeout">The <see cref="TimeSpan"/> that specifies how long the receive request operation has to complete before timing out.</param>
            /// <param name="callback">The <see cref="AsyncCallback"/> delegate that receives the notification of the asynchronous receive that a request operation completes.</param>
            /// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous receive of a request operation.</param>
            /// <param name="cancellationTokenSource">Token source used to cancel operation.</param>
            public ReceiveAsyncResult(HttpMessageHandlerChannel channel, TimeSpan timeout, AsyncCallback callback, object state, CancellationTokenSource cancellationTokenSource)
                : base(channel, timeout, callback, state, cancellationTokenSource)
            {
                if (channel.IsDisposed)
                {
                    this.Context = null;
                    this.Complete(true);
                }
                else
                {
                    this.StartReceive();
                }
            }

            /// <summary>
            /// Completes the specified asynchronous operation to receive a request.
            /// </summary>
            /// <param name="result">The <see cref="IAsyncResult"/> instance.</param>
            /// <returns>The <see cref="RequestContext"/> used to construct a reply to the request. </returns>
            public static RequestContext End(IAsyncResult result)
            {
                ReceiveAsyncResult thisPtr = AsyncResult.End<ReceiveAsyncResult>(result);
                return thisPtr.Context;
            }

            /// <summary>
            /// Start receiving a request from the inner channel
            /// </summary>
            protected override void StartReceive()
            {
                if (this.IsCompleted)
                {
                    return;
                }

                try
                {
                    IAsyncResult result = this.Channel.InnerChannel.BeginReceiveRequest(this.Timeout, ReceiveAsyncResult.onReceive, this);
                    if (result.CompletedSynchronously)
                    {
                        this.CompleteReceive(result);
                    }
                }
                catch (TimeoutException e)
                {
                    Fx.Exception.AsInformation(e);
                    this.Complete(true, e);
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    this.Complete(true, e);
                }
            }

            /// <summary>
            /// Complete receiving a request from the inner channel
            /// </summary>
            /// <param name="result">The <see cref="IAsyncResult"/> returned by a call to the BeginReceiveRequest method.</param>
            protected override void CompleteReceive(IAsyncResult result)
            {
                RequestContext innerContext = this.Channel.InnerChannel.EndReceiveRequest(result);
                if (innerContext != null)
                {
                    this.Context = new HttpMessageHandlerRequestContext(
                        this,
                        innerContext,
                        this.Channel.DefaultSendTimeout,
                        this.CancellationToken,
                        this.Channel);
                    this.Context.StartHttpMessageChannel();
                }
                else
                {
                    this.Context = null;
                    this.Complete(result.CompletedSynchronously);
                }
            }

            private static void OnReceive(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }

                ReceiveAsyncResult thisPtr = (ReceiveAsyncResult)result.AsyncState;
                try
                {
                    thisPtr.CompleteReceive(result);
                }
                catch (TimeoutException e)
                {
                    Fx.Exception.AsInformation(e);
                    thisPtr.Complete(false, e);
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    thisPtr.Complete(false, e);
                }
            }
        }

        /// <summary>
        /// Base class encapsulating the shared results of <see cref="HttpMessageHandlerChannel.BeginTryReceiveRequest"/> and 
        /// <see cref="HttpMessageHandlerChannel.BeginReceiveRequest"/>.
        /// </summary>
        private abstract class ReceiveAsyncResultBase : AsyncResult
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ReceiveAsyncResultBase"/> class.
            /// </summary>
            /// <param name="channel">The <see cref="HttpMessageHandlerChannel"/> within which we are operating.</param>
            /// <param name="timeout">The <see cref="TimeSpan"/> that specifies how long the receive request operation has to complete before timing out.</param>
            /// <param name="callback">The <see cref="AsyncCallback"/> delegate that receives the notification of the asynchronous receive that a request operation completes.</param>
            /// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous receive of a request operation.</param>
            /// <param name="cancellationTokenSource">Token source used to cancel operation.</param>
            protected ReceiveAsyncResultBase(HttpMessageHandlerChannel channel, TimeSpan timeout, AsyncCallback callback, object state, CancellationTokenSource cancellationTokenSource)
                : base(callback, state)
            {
                this.Channel = channel;
                if (!channel.IsDisposed)
                {
                    this.CancellationToken = cancellationTokenSource.Token;
                }

                this.Timeout = timeout;
            }

            /// <summary>
            /// Gets the token used to cancel operation.
            /// </summary>
            protected CancellationToken CancellationToken { get; private set; }

            /// <summary>
            /// Gets or sets an event that indicates when the synchronous reply path has completed.
            /// </summary>
            protected ManualResetEvent SyncCompletedEvent { get; set; }

            /// <summary>
            /// Gets the <see cref="HttpMessageHandlerChannel"/> within which we are operating.
            /// </summary>
            protected HttpMessageHandlerChannel Channel { get; private set; }

            /// <summary>
            /// Gets the <see cref="TimeSpan"/> that specifies how long the receive request operation has to complete before timing out.
            /// </summary>
            protected TimeSpan Timeout { get; private set; }

            /// <summary>
            /// Gets or sets the <see cref="RequestContext"/> representing the reply.
            /// </summary>
            protected HttpMessageHandlerRequestContext Context { get; set; }

            /// <summary>
            /// Start receiving a request from the inner channel
            /// </summary>
            protected abstract void StartReceive();

            /// <summary>
            /// Complete receiving a request from the inner channel
            /// </summary>
            /// <param name="result">The <see cref="IAsyncResult"/> returned by a call to BeginReceiveRequest or BeginTryReceiveRequest.</param>
            protected abstract void CompleteReceive(IAsyncResult result);

            /// <summary>
            /// Provides a reply that is correlated to an incoming request.
            /// </summary>
            internal class HttpMessageHandlerRequestContext : RequestContext
            {
                private static AsyncCallback onReplyCompleted = Fx.ThunkCallback(new AsyncCallback(HttpMessageHandlerRequestContext.OnReplyCompletedCallback));
                private static AsyncCallback onInnerReplyCompleted = Fx.ThunkCallback(new AsyncCallback(HttpMessageHandlerRequestContext.OnInnerReplyCompletedCallback));
                private static Action<object> onCompletePipelineForSyncSend = Fx.ThunkCallback<object>(HttpMessageHandlerRequestContext.CompletePipelineForSyncSendCallback);

                private CancellationTokenSource contextCancellationTokenSource;
                private TimeSpan defaultSendTimeout;
                private ReceiveAsyncResultBase receiveResult;
                private ReplyAsyncResult replyResult;
                private Task<HttpResponseMessage> innerHandlerTask;
                private TaskCompletionSource<HttpResponseMessage> outerHandlerTask;
                private HttpMessageHandlerChannel channel;
                private RequestContext innerContext;
                private Message replyMessage;
                private bool disposed;
                private bool isIndependentResponse;

                /// <summary>
                /// Initializes a new instance of the <see cref="HttpMessageHandlerRequestContext"/> class.
                /// </summary>
                /// <param name="receiveResult">The <see cref="ReceiveAsyncResultBase"/> associated with this operation.</param>
                /// <param name="innerContext">The inner context on which we send the reply.</param>
                /// <param name="defaultSendTimeout">The default interval of time provided for a send operation to complete.</param>
                /// <param name="channelCancellationToken">Token used to cancel operation.</param>
                /// <param name="channel"><see cref="HttpMessageHandlerChannel"/> instance.</param>
                public HttpMessageHandlerRequestContext(ReceiveAsyncResultBase receiveResult, RequestContext innerContext, TimeSpan defaultSendTimeout, CancellationToken channelCancellationToken, HttpMessageHandlerChannel channel)
                {
                    Fx.Assert(innerContext != null, "inner context cannot be null");
                    Fx.Assert(channel != null, "channel cannot be null");
                    Fx.Assert(channel.pipeline != null, "pipeline cannot be null");

                    this.contextCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(channelCancellationToken);
                    this.receiveResult = receiveResult;
                    this.innerContext = innerContext;
                    this.defaultSendTimeout = defaultSendTimeout;
                    this.channel = channel;
                }

                /// <summary>
                /// Gets the message that contains the request.
                /// </summary>
                public override Message RequestMessage
                {
                    get { return this.innerContext.RequestMessage; }
                }

                /// <summary>
                /// Starts the <see cref="HttpMessageChannel"/> provided by the <see cref="HttpMessageHandlerFactory"/>.
                /// </summary>
                public void StartHttpMessageChannel()
                {
                    // Getting the HTTP request from the WCF Message
                    HttpRequestMessage request = this.RequestMessage.ToHttpRequestMessage();
                    if (request == null)
                    {
                        throw Fx.Exception.AsError(
                            new InvalidOperationException(SR.HttpMessageHandlerInvalidMessage(this.RequestMessage.GetType().Name)));
                    }

                    // Adding 'this' as property
                    request.Properties.Add(HttpMessageHandlerChannel.RequestPropertyKey, this);

                    try
                    {
                        this.innerHandlerTask = this.channel.pipeline.SendAsync(request, this.contextCancellationTokenSource.Token).ContinueWith(
                            task =>
                            {
                                HttpResponseMessage httpResponse = null;
                                if (task.IsFaulted)
                                {
                                    // We must inspect task.Exception -- otherwise it is automatically rethrown.
                                    AggregateException flattenedException = task.Exception.Flatten();
                                    Fx.Exception.AsError(flattenedException);
                                    httpResponse = StandardHttpResponseMessageBuilder.CreateInternalServerErrorResponse(request, flattenedException, this.channel.IncludeExceptionDetailInFaults, null);
                                }
                                else if (task.IsCanceled)
                                {
                                    httpResponse = StandardHttpResponseMessageBuilder.CreateInternalServerErrorResponse(request, new OperationCanceledException(), this.channel.IncludeExceptionDetailInFaults, null);
                                }
                                else
                                {
                                    httpResponse = task.Result;
                                }

                                if (!this.receiveResult.IsCompleted)
                                {
                                    // If the request didn't make it to the service then we handle the response directly and start another request.
                                    this.receiveResult.StartReceive();

                                    // Send short cirquited response
                                    this.SendShortCircuitHttpResponse(httpResponse);
                                }
                                else if (this.channel.AsynchronousSendEnabled)
                                {
                                    // Otherwise follow the normal reply route through RequestContext
                                    this.replyResult.ContinueReplyPath(httpResponse);
                                }

                                return httpResponse;
                            },
                            this.contextCancellationTokenSource.Token);

                        if (this.innerHandlerTask.IsCanceled)
                        {
                            Fx.Exception.AsWarning(new OperationCanceledException(this.contextCancellationTokenSource.Token));
                            this.AbortInternal(true);
                        }

                        // If we are using the synchronous reply path then we have to wait until this.task has been set
                        // before we can complete the response.
                        if (!this.channel.AsynchronousSendEnabled && this.receiveResult.SyncCompletedEvent != null)
                        {
                            this.receiveResult.SyncCompletedEvent.Set();
                        }
                    }
                    catch (Exception e)
                    {
                        Fx.Exception.AsError(e);

                        if (Fx.IsFatal(e))
                        {
                            throw;
                        }

                        // Check to see if we should restart the receive loop
                        if (!this.receiveResult.IsCompleted)
                        {
                            this.receiveResult.StartReceive();
                        }

                        // Check to see if we have a reply result that we need to complete
                        if (this.replyResult != null)
                        {
                            this.isIndependentResponse = true;
                            this.replyResult.CompleteReplyPath(false, e);
                        }

                        HttpResponseMessage httpResponse = StandardHttpResponseMessageBuilder.CreateInternalServerErrorResponse(request, e, this.channel.IncludeExceptionDetailInFaults, null);
                        this.SendShortCircuitHttpResponse(httpResponse);
                    }
                }

                /// <summary>
                /// Creates the outer handler task which is completed when reply is returned from Service Model.
                /// </summary>
                /// <param name="request"><see cref="HttpRequestMessage"/> to submit</param>
                /// <param name="cancellationToken">Token used to cancel operation.</param>
                /// <returns>A <see cref="Task&lt;T&gt;"/> representing the operation.</returns>
                public Task<HttpResponseMessage> CreateOuterHandlerTask(HttpRequestMessage request, CancellationToken cancellationToken)
                {
                    // We ran up through the pipeline and are now ready to hook back into the WCF channel model
                    this.outerHandlerTask = new TaskCompletionSource<HttpResponseMessage>();

                    // If we don't use AsynchronousSendEnabled then we must do a context switch here to preserve the semantics of Reply
                    if (this.channel.AsynchronousSendEnabled)
                    {
                        this.receiveResult.Complete(false);
                    }
                    else
                    {
                        this.receiveResult.SyncCompletedEvent = new ManualResetEvent(false);
                        ActionItem.Schedule(HttpMessageHandlerRequestContext.onCompletePipelineForSyncSend, this.receiveResult);
                    }

                    return this.outerHandlerTask.Task;
                }

                /// <summary>
                /// Aborts processing the request associated with the context. 
                /// </summary>
                public override void Abort()
                {
                    if (!this.isIndependentResponse)
                    {
                        bool cleanup = this.channel.State != CommunicationState.Opened;
                        this.AbortInternal(cleanup);
                    }
                }

                /// <summary>
                /// Closes the operation that is replying to the request context associated with the current context.
                /// </summary>
                /// <param name="timeout">The <see cref="Timespan"/> that specifies the interval of time within which the reply operation associated with the current context must close.</param>
                public override void Close(TimeSpan timeout)
                {
                    if (!this.isIndependentResponse)
                    {
                        this.CloseInternal(timeout);
                    }
                }

                /// <summary>
                /// Closes the operation that is replying to the request context associated with the current context.
                /// </summary>
                public override void Close()
                {
                    if (!this.isIndependentResponse)
                    {
                        this.CloseInternal();
                    }
                }

                /// <summary>
                /// Begins an asynchronous operation to reply to the request associated with the current context.
                /// </summary>
                /// <param name="message">The incoming <see cref="Message"/> that contains the request.</param>
                /// <param name="timeout">The <see cref="TimeSpan"/> that specifies the interval of time to wait for the reply to an available request.</param>
                /// <param name="callback">The <see cref="AsyncCallback"/> delegate that receives the notification of the asynchronous reply operation completion.</param>
                /// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous reply operation.</param>
                /// <returns>The <see cref="IAsyncResult"/> that references the asynchronous reply operation.</returns>
                public override IAsyncResult BeginReply(Message message, TimeSpan timeout, AsyncCallback callback, object state)
                {
                    if (!this.channel.AsynchronousSendEnabled)
                    {
                        throw Fx.Exception.AsError(
                            new InvalidOperationException(
                                SR.HttpMessageHandlerRequestContextReplyPathMismatch2(
                                    typeof(DispatcherSynchronizationBehavior).Name,
                                    typeof(HttpMessageHandlerChannel).Name)));
                    }

                    this.replyResult = new ReplyAsyncResult(this.innerContext, timeout, callback, state);
                    this.replyMessage = message;

                    if (this.channel.State != CommunicationState.Opened)
                    {
                        OperationCanceledException operationCanceledException = new OperationCanceledException();
                        Fx.Exception.AsInformation(operationCanceledException);
                        this.replyResult.CompleteReplyPath(true, operationCanceledException);
                        return this.replyResult;
                    }

                    // Complete outer task and wait for ContinueReplyPath to be called
                    this.CompleteOuterHandlerTask();

                    return this.replyResult;
                }

                /// <summary>
                /// Begins an asynchronous operation to reply to the request associated with the current context.
                /// </summary>
                /// <param name="message">The incoming <see cref="Message"/> that contains the request.</param>
                /// <param name="callback">The <see cref="AsyncCallback"/> delegate that receives the notification of the asynchronous reply operation completion.</param>
                /// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous reply operation.</param>
                /// <returns>The <see cref="IAsyncResult"/> that references the asynchronous reply operation.</returns>
                public override IAsyncResult BeginReply(Message message, AsyncCallback callback, object state)
                {
                    return this.BeginReply(message, this.defaultSendTimeout, callback, state);
                }

                /// <summary>
                /// Completes an asynchronous operation to reply to a request message.
                /// </summary>
                /// <param name="result">The <see cref="IAsyncResult"/> returned by a call to <see cref="HttpMessageHandlerRequestContext.BeginReply"/>.</param>
                public override void EndReply(IAsyncResult result)
                {
                    ReplyAsyncResult.End(result);
                }

                /// <summary>
                /// Replies to a request message within a specified interval of time.
                /// </summary>
                /// <param name="message">The incoming <see cref="Message"/> that contains the request.</param>
                /// <param name="timeout">The <see cref="TimeSpan"/> that specifies the interval of time to wait for the reply to a request.</param>
                public override void Reply(Message message, TimeSpan timeout)
                {
                    if (this.channel.State != CommunicationState.Opened)
                    {
                        Fx.Exception.AsInformation(new OperationCanceledException());
                        return;
                    }

                    if (this.channel.AsynchronousSendEnabled)
                    {
                        if (message != null)
                        {
                            // Extract HttpResponseMessage so that we own it from now on.
                            HttpResponseMessage httpResponse = message.ExtractHttpResponseMessage();
                            if (httpResponse == null)
                            {
                                // The service model (or service instance) didn't send us an HTTP response message so we create one
                                Exception exception;
                                if (message.IsFault)
                                {
                                    MessageFault fault = MessageFault.CreateFault(this.replyMessage, TransportDefaults.MaxFaultSize);
                                    exception = new FaultException(fault, this.replyMessage.Headers.Action);
                                }
                                else
                                {
                                    exception = Fx.Exception.AsError(
                                        new InvalidOperationException(
                                            SR.HttpMessageHandlerChannelInvalidResponse(
                                            typeof(HttpMessageHandlerChannel).Name, 
                                            typeof(Message).Name,
                                            typeof(HttpResponseMessage).Name)));
                                }

                                // Get the HTTP request from the WCF Message so that we can create a proper HttpResponseMessage
                                HttpRequestMessage request = this.RequestMessage.ToHttpRequestMessage();
                                if (request == null)
                                {
                                    throw Fx.Exception.AsError(
                                        new InvalidOperationException(SR.HttpMessageHandlerInvalidMessage(this.RequestMessage.GetType().Name)));
                                }

                                httpResponse = StandardHttpResponseMessageBuilder.CreateInternalServerErrorResponse(request, exception, this.channel.IncludeExceptionDetailInFaults, null);
                            }

                            message = httpResponse.ToMessage();
                            Fx.Assert(httpResponse != null, "Could not convert HttpResponseMessage to Message");
                        }

                        // We now flip to the async reply path and continue the reply there with the cloned message
                        try
                        {
                            this.isIndependentResponse = true;
                            IAsyncResult result = this.BeginReply(message, HttpMessageHandlerRequestContext.onReplyCompleted, this);
                            if (result.CompletedSynchronously)
                            {
                                this.OnReplyCompleted(result);
                            }
                        }
                        catch (Exception e)
                        {
                            Fx.Exception.AsError(e);
                            if (Fx.IsFatal(e))
                            {
                                throw;
                            }

                            this.CloseReply();
                        }
                    }
                    else
                    {
                        // If message is null then we create a 202 Accepted response when completing the task.
                        this.replyMessage = message;

                        // Complete outer task and wait for ContinueReplyPath to be called
                        this.CompleteOuterHandlerTask();

                        HttpResponseMessage httpResponse = null;
                        try
                        {
                            this.innerHandlerTask.Wait(this.contextCancellationTokenSource.Token);
                            httpResponse = this.innerHandlerTask.Result;
                        }
                        catch (AggregateException ae)
                        {
                            Fx.Exception.AsError(ae);
                            HttpRequestMessage request = this.RequestMessage.ToHttpRequestMessage();
                            httpResponse = StandardHttpResponseMessageBuilder.CreateInternalServerErrorResponse(request, ae, this.channel.IncludeExceptionDetailInFaults, null);
                        }
                        catch (NullReferenceException nre)
                        {
                            InvalidOperationException exception = new InvalidOperationException(
                                SR.HttpMessageHandlerRequestContextReplyPathMismatch1(typeof(DispatcherSynchronizationBehavior).Name, typeof(HttpMessageHandlerChannel).Name),
                                nre);
                            Fx.Exception.AsError(exception);
                            HttpRequestMessage request = this.RequestMessage.ToHttpRequestMessage();
                            httpResponse = StandardHttpResponseMessageBuilder.CreateInternalServerErrorResponse(request, exception, this.channel.IncludeExceptionDetailInFaults, null);
                        }
                        catch (OperationCanceledException oce)
                        {
                            Fx.Exception.AsError(oce);
                            throw;
                        }
                        catch (Exception e)
                        {
                            if (Fx.IsFatal(e))
                            {
                                throw;
                            }

                            HttpRequestMessage request = this.RequestMessage.ToHttpRequestMessage();
                            httpResponse = StandardHttpResponseMessageBuilder.CreateInternalServerErrorResponse(request, e, this.channel.IncludeExceptionDetailInFaults, null);
                        }

                        Message msg = httpResponse.ToMessage();

                        TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
                        this.innerContext.Reply(msg, timeoutHelper.RemainingTime());
                    }
                }

                /// <summary>
                /// Replies to a request message within a specified interval of time.
                /// </summary>
                /// <param name="message">The incoming <see cref="Message"/> that contains the request.</param>
                public override void Reply(Message message)
                {
                    this.Reply(message, this.defaultSendTimeout);
                }
                                
                /// <summary>
                /// Releases resources associated with the context.
                /// </summary>
                /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
                protected override void Dispose(bool disposing)
                {
                    try
                    {
                        if (!this.disposed)
                        {
                            if (disposing)
                            {
                                // Cancel CancellationTokenSource
                                // TODO: CSDMAIN 204727 Revert this commented code when move to 4.5
                                // this.contextCancellationTokenSource.Dispose();
                            }

                            this.disposed = true;
                        }
                    }
                    finally
                    {
                        base.Dispose(disposing);
                    }
                }

                private static void OnInnerReplyCompletedCallback(IAsyncResult result)
                {
                    if (result.CompletedSynchronously)
                    {
                        return;
                    }

                    HttpMessageHandlerRequestContext thisPtr = (HttpMessageHandlerRequestContext)result.AsyncState;
                    try
                    {
                        thisPtr.OnInnerReplyCompleted(result);
                    }
                    catch (Exception e)
                    {
                        Fx.Exception.AsError(e);
                        if (Fx.IsFatal(e))
                        {
                            throw;
                        }
                    }
                }

                private static void OnReplyCompletedCallback(IAsyncResult result)
                {
                    if (result.CompletedSynchronously)
                    {
                        return;
                    }

                    HttpMessageHandlerRequestContext thisPtr = (HttpMessageHandlerRequestContext)result.AsyncState;
                    try
                    {
                        thisPtr.OnReplyCompleted(result);
                    }
                    catch (Exception e)
                    {
                        Fx.Exception.AsError(e);
                        if (Fx.IsFatal(e))
                        {
                            throw;
                        }
                    }
                }

                private static void CompletePipelineForSyncSendCallback(object state)
                {
                    ReceiveAsyncResultBase result = state as ReceiveAsyncResultBase;
                    Fx.Assert(result != null, "Expected ReceiveAsyncResultBase");

                    // Ensure that we have completed the first leg of the pipeline by unrolling the stack.
                    try
                    {
                        result.SyncCompletedEvent.WaitOne();
                    }
                    finally
                    {
                        result.SyncCompletedEvent.Dispose();
                        result.SyncCompletedEvent = null;
                    }

                    result.Complete(false);
                }

                private void CompleteOuterHandlerTask()
                {
                    Fx.Assert(this.outerHandlerTask != null, "Outer handler task cannot be null");

                    // If Service Model (or service instance) sent us null then we create a 202 HTTP response
                    HttpResponseMessage httpResponse = null;
                    
                    if (this.replyMessage != null)
                    {
                        httpResponse = this.replyMessage.ToHttpResponseMessage();
                    }
                    else
                    {
                        HttpRequestMessage request = this.RequestMessage.ToHttpRequestMessage();
                        httpResponse = StandardHttpResponseMessageBuilder.CreateAcceptedResponse(request);
                    }

                    if (httpResponse == null)
                    {
                        // The service model (or service instance) didn't send us an HTTP response message so we create one
                        Exception exception;
                        if (this.replyMessage != null && this.replyMessage.IsFault)
                        {
                            MessageFault fault = MessageFault.CreateFault(this.replyMessage, TransportDefaults.MaxFaultSize);
                            exception = new FaultException(fault, this.replyMessage.Headers.Action);
                        }
                        else
                        {
                            exception = new InvalidOperationException();
                        }

                        HttpRequestMessage request = this.RequestMessage.ToHttpRequestMessage();
                        httpResponse = StandardHttpResponseMessageBuilder.CreateInternalServerErrorResponse(request, exception, this.channel.IncludeExceptionDetailInFaults, null);
                    }

                    this.outerHandlerTask.TrySetResult(httpResponse);
                }

                private void CancelOuterHandlerTask()
                {
                    if (this.outerHandlerTask != null)
                    {
                        this.outerHandlerTask.TrySetCanceled();
                    }
                }

                private void SendShortCircuitHttpResponse(HttpResponseMessage httpResponse)
                {
                    IAsyncResult result = null;
                    this.replyMessage = httpResponse.ToMessage();
                    Fx.Assert(this.replyMessage != null, "Could not convert HttpResponseMessage to Message");

                    try
                    {
                        result = this.innerContext.BeginReply(this.replyMessage, this.defaultSendTimeout, HttpMessageHandlerRequestContext.onInnerReplyCompleted, this);
                        if (result.CompletedSynchronously)
                        {
                            this.OnInnerReplyCompleted(result);
                        }
                    }
                    catch (Exception e)
                    {
                        Fx.Exception.AsError(e);
                        if (Fx.IsFatal(e))
                        {
                            throw;
                        }

                        this.CloseReply();
                    }
                }

                private void OnInnerReplyCompleted(IAsyncResult result)
                {
                    try
                    {
                        this.innerContext.EndReply(result);
                    }
                    catch (Exception e)
                    {
                        Fx.Exception.AsError(e);
                        if (Fx.IsFatal(e))
                        {
                            throw;
                        }
                    }
                    finally
                    {
                        this.CloseReply();
                    }
                }

                private void OnReplyCompleted(IAsyncResult result)
                {
                    try
                    {
                        this.EndReply(result);
                    }
                    catch (Exception e)
                    {
                        Fx.Exception.AsError(e);
                        if (Fx.IsFatal(e))
                        {
                            throw;
                        }
                    }
                    finally
                    {
                        this.CloseReply();
                    }
                }

                private void AbortRequestContext()
                {
                    try
                    {
                        this.AbortInternal(true);
                    }
                    catch (Exception e)
                    {
                        Fx.Exception.AsError(e);
                        if (Fx.IsFatal(e))
                        {
                            throw;
                        }
                    }
                }

                private void CloseRequestContext()
                {
                    try
                    {
                        this.CloseInternal();
                    }
                    catch (Exception e)
                    {
                        Fx.Exception.AsError(e);
                        if (Fx.IsFatal(e))
                        {
                            throw;
                        }

                        this.AbortRequestContext();
                    }
                }

                private void CloseReplyMessage()
                {
                    if (this.replyMessage != null)
                    {
                        try
                        {
                            this.replyMessage.Close();
                        }
                        catch (Exception e)
                        {
                            Fx.Exception.AsError(e);
                            if (Fx.IsFatal(e))
                            {
                                throw;
                            }
                        }
                        finally
                        {
                            this.replyMessage = null;
                        }
                    }
                }

                private void CloseReply()
                {
                    this.CloseReplyMessage();
                    this.CloseRequestContext();
                }

                private void AbortInternal(bool cleanup)
                {
                    if (cleanup)
                    {
                        if (this.replyResult != null)
                        {
                            OperationCanceledException oce = new OperationCanceledException();
                            this.replyResult.CompleteReplyPath(false, oce);
                        }

                        this.innerContext.Abort();
                        this.Dispose(true);
                    }
                    else
                    {
                        this.contextCancellationTokenSource.Cancel();
                        this.CancelOuterHandlerTask();
                    }
                }

                private void CloseInternal()
                {
                    this.innerContext.Close();
                    this.Dispose(true);
                }

                private void CloseInternal(TimeSpan timeout)
                {
                    this.innerContext.Close(timeout);
                    this.Dispose(true);
                }

                /// <summary>
                /// Encapsulates the results of <see cref="HttpMessageHandlerRequestContext.BeginReply"/>.
                /// </summary>
                private class ReplyAsyncResult : AsyncResult
                {
                    private static AsyncCallback onReply = Fx.ThunkCallback(new AsyncCallback(OnReplyCompletedCallback));
                    private RequestContext innerContext; 
                    private TimeSpan timeout;

                    /// <summary>
                    /// Initializes a new instance of the <see cref="ReplyAsyncResult"/> class.
                    /// </summary>
                    /// <param name="innerContext">The inner context where to send the reply.</param>
                    /// <param name="timeout">The <see cref="TimeSpan"/> that specifies the interval of time to wait for the reply to an available request.</param>
                    /// <param name="callback">The <see cref="AsyncCallback"/> delegate that receives the notification of the asynchronous reply operation completion.</param>
                    /// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous reply operation.</param>
                    public ReplyAsyncResult(RequestContext innerContext, TimeSpan timeout, AsyncCallback callback, object state)
                        : base(callback, state)
                    {
                        this.innerContext = innerContext;
                        this.timeout = timeout;
                    }

                    /// <summary>
                    /// Completes the specified asynchronous operation to send a reply.
                    /// </summary>
                    /// <param name="result">The <see cref="IAsyncResult"/> instance.</param>
                    public static void End(IAsyncResult result)
                    {
                        AsyncResult.End<ReplyAsyncResult>(result);
                    }

                    /// <summary>
                    /// When the <see cref="HttpMessageChannel"/> has been completed we continue sending the
                    /// reply.
                    /// </summary>
                    /// <param name="httpResponse">The <see cref="HttpResponseMessage"/> output from the <see cref="HttpMessageChannel"/>.</param>
                    public void ContinueReplyPath(HttpResponseMessage httpResponse)
                    {
                        IAsyncResult result = null;
                        Message message = httpResponse.ToMessage();
                        Fx.Assert(message != null, "Could not convert HttpResponseMessage to Message");
                        TimeoutHelper timeoutHelper = new TimeoutHelper(this.timeout);
                        try
                        {
                            result = this.innerContext.BeginReply(message, timeoutHelper.RemainingTime(), ReplyAsyncResult.onReply, this);
                            if (result.CompletedSynchronously)
                            {
                                this.OnReplyCompleted(result);
                                this.Complete(false);
                            }
                        }
                        catch (Exception e)
                        {
                            if (Fx.IsFatal(e))
                            {
                                throw;
                            }

                            this.Complete(false, e);
                        }
                    }

                    /// <summary>
                    /// Completes the reply path.
                    /// </summary>
                    /// <param name="completedSynchronously">if set to <c>true</c> the result completed synchronously.</param>
                    /// <param name="exception">The exception (if any) to complete the result with.</param>
                    public void CompleteReplyPath(bool completedSynchronously, Exception exception)
                    {
                        if (!this.IsCompleted)
                        {
                            this.Complete(completedSynchronously, exception);
                        }
                    }

                    private static void OnReplyCompletedCallback(IAsyncResult result)
                    {
                        if (result.CompletedSynchronously)
                        {
                            return;
                        }

                        ReplyAsyncResult thisPtr = (ReplyAsyncResult)result.AsyncState;
                        Exception completionException = null;
                        try
                        {
                            thisPtr.OnReplyCompleted(result);
                        }
                        catch (Exception e)
                        {
                            if (Fx.IsFatal(e))
                            {
                                throw;
                            }

                            completionException = e;
                        }

                        thisPtr.Complete(false, completionException);
                    }

                    private void OnReplyCompleted(IAsyncResult result)
                    {
                        this.innerContext.EndReply(result);
                    }
                }
            }
        }

        /// <summary>
        /// Handler wrapping the bottom (towards network) of the <see cref="HttpMessageChannel"/> and integrates
        /// back into the <see cref="IReplyChannel"/>.
        /// </summary>
        private class InnerHandler : DelegatingChannel
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="InnerHandler"/> class.
            /// </summary>
            /// <param name="innerChannel">The inner <see cref="HttpMessageChannel"/> on which we send the <see cref="HttpRequestMessage"/>.</param>
            public InnerHandler(HttpMessageChannel innerChannel)
                : base(innerChannel)
            {
            }

            /// <summary>
            /// Submits an <see cref="HttpRequestMessage"/> on the inner channel asynchronously.
            /// </summary>
            /// <param name="request"><see cref="HttpRequestMessage"/> to submit</param>
            /// <param name="cancellationToken">Token used to cancel operation.</param>
            /// <returns>A <see cref="Task&lt;T&gt;"/> representing the operation.</returns>
            public new Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return base.SendAsync(request, cancellationToken);
            }

            /// <summary>
            /// Submits an <see cref="HttpRequestMessage"/> on the inner channel synchronously.
            /// </summary>
            /// <param name="request"><see cref="HttpRequestMessage"/> to submit</param>
            /// <param name="cancellationToken">Token used to cancel operation.</param>
            /// <returns>The <see cref="HttpResponseMessage"/> resulting from the operation.</returns>
            /// <remarks>Throws <see cref="NotImplementedException"/>.</remarks>
            protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                throw Fx.Exception.AsError(new NotImplementedException());
            }
        }

        /// <summary>
        /// Handler wrapping the top (towards Service Model) of the <see cref="HttpMessageChannel"/> and integrates
        /// bask into the <see cref="IReplyChannel"/>.
        /// </summary>
        private class OuterHandler : HttpMessageChannel
        {
            private HttpMessageHandlerChannel channel;

            /// <summary>
            /// Initializes a new instance of the <see cref="OuterHandler"/> class.
            /// </summary>
            /// <param name="channel">The channel instance this handler instance is associated with.</param>
            public OuterHandler(HttpMessageHandlerChannel channel)
            {
                Fx.Assert(channel != null, "channel cannot be null");
                this.channel = channel;
            }

            /// <summary>
            /// Submits an <see cref="HttpRequestMessage"/> on the inner channel synchronously.
            /// </summary>
            /// <param name="request"><see cref="HttpRequestMessage"/> to submit</param>
            /// <param name="cancellationToken">Token used to cancel operation.</param>
            /// <returns>The <see cref="HttpResponseMessage"/> resulting from the operation.</returns>
            /// <remarks>Throws <see cref="NotImplementedException"/>.</remarks>
            protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                throw Fx.Exception.AsError(new NotImplementedException());
            }

            /// <summary>
            /// Submits an <see cref="HttpRequestMessage"/> on the inner channel asynchronously.
            /// </summary>
            /// <param name="request"><see cref="HttpRequestMessage"/> to submit</param>
            /// <param name="cancellationToken">Token used to cancel operation.</param>
            /// <returns>A <see cref="Task&lt;T&gt;"/> representing the operation.</returns>
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                HttpMessageHandlerChannel.ReceiveAsyncResultBase.HttpMessageHandlerRequestContext context;
                object contextAsObj;
                if (request.Properties.TryGetValue(HttpMessageHandlerChannel.RequestPropertyKey, out contextAsObj) &&
                    (context = contextAsObj as HttpMessageHandlerChannel.ReceiveAsyncResultBase.HttpMessageHandlerRequestContext) != null)
                {
                    return context.CreateOuterHandlerTask(request, cancellationToken);
                }
                else
                {
                    InvalidOperationException exception = new InvalidOperationException(SR.HttpMessageHandlerMissingProperty(typeof(HttpRequestMessage).Name, HttpMessageHandlerChannel.RequestPropertyKey));
                    Fx.Exception.AsError(exception);
                    return Task.Factory.StartNew(
                        () =>
                        {
                            return StandardHttpResponseMessageBuilder.CreateInternalServerErrorResponse(request, exception, this.channel.IncludeExceptionDetailInFaults, null);
                        });
                }
            }
        }

        /// <summary>
        /// Error handler used in case the pipeline cannot be constructed. This handler always returns an HTTP response
        /// based on the exception provided in the constructor.
        /// </summary>
        private class ErrorHandler : DelegatingChannel
        {
            private HttpMessageHandlerChannel channel;
            private Exception exception;

            /// <summary>
            /// Initializes a new instance of the <see cref="ErrorHandler"/> class.
            /// </summary>
            /// <param name="innerChannel">The inner <see cref="HttpMessageChannel"/> on which we send the <see cref="HttpRequestMessage"/>.</param>
            /// <param name="channel">The channel instance this handler instance is associated with.</param>
            /// <param name="exception">The exception to include in the HTTP response.</param>
            public ErrorHandler(HttpMessageChannel innerChannel, HttpMessageHandlerChannel channel, Exception exception)
                : base(innerChannel)
            {
                Fx.Assert(channel != null, "Channel cannot be null");
                Fx.Assert(exception != null, "Exception cannot be null");
                this.channel = channel;
                this.exception = exception is TargetInvocationException ? exception.InnerException : exception;
            }

            /// <summary>
            /// Creates a <see cref="Task&lt;HttpResponseMessage&gt;"/> that always returns an HTTP error response.
            /// </summary>
            /// <param name="request"><see cref="HttpRequestMessage"/> to submit</param>
            /// <param name="cancellationToken">Token used to cancel operation.</param>
            /// <returns>Task that always returns an HTTP response.</returns>
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.Factory.StartNew(
                    () =>
                    {
                        return StandardHttpResponseMessageBuilder.CreateInternalServerErrorResponse(request, exception, this.channel.IncludeExceptionDetailInFaults, null);
                    });
            }
        }
    }
}