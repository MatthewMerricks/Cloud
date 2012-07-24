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
    /// Defines the contract that associates incoming messages with a local operation 
    /// to customize service execution behavior using
    /// <see cref="HttpBinding">HttpBinding</see>.
    /// </summary>
    public abstract class HttpOperationSelector : IDispatchOperationSelector
    {
        /// <summary>
        /// The name of the message property that will store the name of the selected operation.
        /// </summary>
        public const string SelectedOperationPropertyName = "HttpOperationName";

        /// <summary>
        /// Associates a local operation with the incoming method.
        /// </summary>
        /// <param name="message">The incoming <see cref="HttpRequestMessage"/>.</param>
        /// <returns>The name of the operation to be associated with the message.</returns>
        public string SelectOperation(HttpRequestMessage message)
        {
            if (message == null)
            {
                throw Fx.Exception.ArgumentNull("message");
            }

            return this.OnSelectOperation(message);
        }

        /// <summary>
        /// Associates a local operation with the incoming method.
        /// </summary>
        /// <param name="message">The incoming <see cref="Message"/> to be associated with an operation.</param>
        /// <returns>The name of the operation to be associated with the message.</returns>
        string IDispatchOperationSelector.SelectOperation(ref Message message)
        {
            if (message == null)
            {
                throw Fx.Exception.ArgumentNull("message");
            }

            HttpRequestMessage requestMessage = message.ToHttpRequestMessage();
            if (requestMessage == null)
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.HttpOperationSelectorNullRequest(this.GetType().Name, typeof(HttpRequestMessage).Name, "SelectOperation")));
            }

            string operation = this.SelectOperation(requestMessage);
            if (operation == null)
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.HttpOperationSelectorNullOperation(this.GetType().Name)));
            }
            
            message.Properties[SelectedOperationPropertyName] = operation;

            ////TODO, CSDMain 205175, need to decide what to do with the tracing
            ////if (operation != null)
            ////{
            ////    if (TD2.WebRequestMatchesOperationIsEnabled())
            ////    {
            ////        TD2.WebRequestMatchesOperation(
            ////            requestMessage.RequestUri != null ? requestMessage.RequestUri.ToString() : string.Empty, 
            ////            operation,
            ////            typeof(HttpOperationSelector).Name);
            ////    }
            ////}

            return operation;
        }

        /// <summary>
        /// Abstract method called by <see cref="SelectOperation(HttpRequestMessage)"/>
        /// to determine the associated operation.   Derived classes must implement this.
        /// </summary>
        /// <param name="message">The incoming <see cref="HttpRequestMessage"/>.</param>
        /// <returns>The name of the operation to be associated with the message.</returns>
        protected abstract string OnSelectOperation(HttpRequestMessage message);
    }
}
