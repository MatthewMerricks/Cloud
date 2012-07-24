// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Net.Http;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Dispatcher;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Http.Channels;

    /// <summary>
    /// Abstract base class that defines methods to deserialize request messages
    /// and serialize response messages for the
    /// <see cref="HttpBinding">HttpBinding</see>.
    /// </summary>
    public abstract class HttpMessageFormatter : IDispatchMessageFormatter
    {
        /// <summary>
        /// Deserializes a message into an array of parameters.
        /// </summary>
        /// <param name="message">The incoming message.</param>
        /// <param name="parameters">The objects that are passed to the operation as parameters.</param>
        void IDispatchMessageFormatter.DeserializeRequest(Message message, object[] parameters)
        {
            if (message == null)
            {
                throw Fx.Exception.ArgumentNull("message");
            }

            HttpRequestMessage request = message.ToHttpRequestMessage();
            if (request == null)
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(SR.HttpMessageFormatterNullMessage(this.GetType().Name, typeof(HttpRequestMessage).Name, "DeserializeRequest")));
            }

            this.DeserializeRequest(request, parameters);
        }

        /// <summary>
        /// Serializes a reply message from a specified message version, array of parameters, and a return value.
        /// </summary>
        /// <param name="messageVersion">The SOAP message version.</param>
        /// <param name="parameters">The out parameters.</param>
        /// <param name="result">The return value.</param>
        /// <returns>The serialized reply message.</returns>
        Message IDispatchMessageFormatter.SerializeReply(
                                              MessageVersion messageVersion, 
                                              object[] parameters, 
                                              object result)
        {
            if (messageVersion != MessageVersion.None)
            {
                throw Fx.Exception.AsError(
                    new NotSupportedException(SR.HttpMessageFormatterMessageVersion(this.GetType(), typeof(MessageVersion), "None")));
            }

            HttpResponseMessage response = this.SerializeReply(parameters, result);
            Fx.Assert(response != null, "Response cannot be null.");
            return response.ToMessage();
        }

        /// <summary>
        /// Deserializes a message into an array of parameters.
        /// </summary>
        /// <param name="request">The incoming message.</param>
        /// <param name="parameters">The objects that are passed to the operation as parameters.</param>
        public void DeserializeRequest(HttpRequestMessage request, object[] parameters)
        {
            if (request == null)
            {
                throw Fx.Exception.ArgumentNull("request");
            }

            if (parameters == null)
            {
                throw Fx.Exception.ArgumentNull("parameters");
            }

            this.OnDeserializeRequest(request, parameters);
        }

        /// <summary>
        /// Serializes a reply message from an array of parameters and a return value into the
        /// given <paramref name="response"/>.
        /// </summary>
        /// <param name="parameters">The out parameters.</param>
        /// <param name="result">The return value.</param>
        /// <returns>The <see cref="HttpResponseMessage"/> to return.  It cannot be <c>null</c>.</returns>
        public HttpResponseMessage SerializeReply(object[] parameters, object result)
        {
            if (parameters == null)
            {
                throw Fx.Exception.ArgumentNull("parameters");
            }

            HttpResponseMessage responseMessage = this.OnSerializeReply(parameters, result);
            if (responseMessage == null)
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(SR.HttpMessageFormatterNullMessage(this.GetType(), typeof(HttpResponseMessage).Name, "SerializeReply")));
            }

            return responseMessage;
        }

        /// <summary>
        /// Deserializes a message into an array of parameters.
        /// </summary>
        /// <param name="message">The incoming message.</param>
        /// <param name="parameters">The objects that are passed to the operation as parameters.</param>
        protected abstract void OnDeserializeRequest(HttpRequestMessage message, object[] parameters);

        /// <summary>
        /// Serializes a reply message from an array of parameters and a return value into the
        /// given <paramref name="response"/>.
        /// </summary>
        /// <param name="parameters">The out parameters.</param>
        /// <param name="result">The return value.</param>
        /// <returns>The <see cref="HttpResponseMessage"/> to return.  It cannot be <c>null</c>.</returns>
        protected abstract HttpResponseMessage OnSerializeReply(object[] parameters, object result);
    }
}
