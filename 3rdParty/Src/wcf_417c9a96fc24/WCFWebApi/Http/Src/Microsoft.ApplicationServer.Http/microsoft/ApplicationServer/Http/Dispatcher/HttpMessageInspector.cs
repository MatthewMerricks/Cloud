// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Net.Http;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Dispatcher;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Http.Channels;

    /// <summary>
    /// Defines the methods that enable custom inspection or modification of inbound and 
    /// outbound application messages in service applications based on
    /// <see cref="HttpBinding">HttpBinding</see>.
    /// </summary>
    public abstract class HttpMessageInspector : IDispatchMessageInspector
    {
        /// <summary>
        /// Called after an inbound message has been received but before the message
        /// is dispatched to the intended operation.
        /// </summary>
        /// <param name="request">The request message.</param>
        /// <param name="channel">The incoming channel.</param>
        /// <param name="instanceContext">The current service instance.</param>
        /// <returns>The object used to correlate state. This object is passed back 
        /// in the <see cref="BeforeSendReply(ref Message, Object)"/> method.</returns>
        object IDispatchMessageInspector.AfterReceiveRequest(
                                             ref Message request, 
                                             IClientChannel channel, 
                                             InstanceContext instanceContext)
        {
            if (request == null)
            {
                throw Fx.Exception.ArgumentNull("request");
            }

            HttpRequestMessage httpRequest = request.ToHttpRequestMessage();

            if (httpRequest == null)
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                            SR.HttpMessageInspectorNullMessage(this.GetType().Name, typeof(HttpRequestMessage).Name, "AfterReceiveRequest")));
            }

            return this.AfterReceiveRequest(httpRequest);
        }

        /// <summary>
        /// Called after the operation has returned but before the reply message is sent.
        /// </summary>
        /// <param name="reply">The reply message. This value is null if the operation is one way.</param>
        /// <param name="correlationState">The correlation object returned from the <see cref="AfterReceiveRequest(ref Message, IClientChannel, InstanceContext)"/> method.</param>
        void IDispatchMessageInspector.BeforeSendReply(
                                           ref Message reply, 
                                           object correlationState)
        {
            if (reply == null)
            {
                throw Fx.Exception.ArgumentNull("reply");
            }

            HttpResponseMessage httpResponse = reply.ToHttpResponseMessage();

            if (httpResponse == null)
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.HttpMessageInspectorNullMessage(this.GetType().Name, typeof(HttpResponseMessage).Name, "BeforeSendReply")));
            }

            this.BeforeSendReply(httpResponse, correlationState);
        }

        /// <summary>
        /// Called after an inbound message has been received but 
        /// before the message is dispatched to the intended operation.
        /// </summary>
        /// <param name="request">The request message</param>
        /// <returns>The object used to correlate state. This object is 
        /// passed back in the <see cref="BeforeSendReply"/>method.</returns>
        public object AfterReceiveRequest(HttpRequestMessage request)
        {
            if (request == null)
            {
                throw Fx.Exception.ArgumentNull("request");
            }

            return this.OnAfterReceiveRequest(request);
        }

        /// <summary>
        /// Called after the operation has returned but before the response message is sent.
        /// </summary>
        /// <param name="response">The response message that will be sent.</param>
        /// <param name="correlationState">The correlction object returned from the
        /// <see cref="AfterReceiveRequest"/> method.</param>
        public void BeforeSendReply(
                                 HttpResponseMessage response,
                                 object correlationState)
        {
            if (response == null)
            {
                throw Fx.Exception.ArgumentNull("response");
            }

            this.OnBeforeSendReply(response, correlationState);
        }

        /// <summary>
        /// Called after an inbound message has been received but 
        /// before the message is dispatched to the intended operation.
        /// </summary>
        /// <param name="request">The request message</param>
        /// <returns>The object used to correlate state. This object is 
        /// passed back in the <see cref="BeforeSendReply"/>method.</returns>
        protected abstract object OnAfterReceiveRequest(HttpRequestMessage request);

        /// <summary>
        /// Called after the operation has returned but before the response message is sent.
        /// </summary>
        /// <param name="response">The response message that will be sent.</param>
        /// <param name="correlationState">The correlction object returned from the
        /// <see cref="AfterReceiveRequest"/> method.</param>
        protected abstract void OnBeforeSendReply(
                                 HttpResponseMessage response,
                                 object correlationState);
    }
}
