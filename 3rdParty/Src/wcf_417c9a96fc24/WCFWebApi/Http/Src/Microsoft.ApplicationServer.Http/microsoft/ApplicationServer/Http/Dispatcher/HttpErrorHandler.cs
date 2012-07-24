// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using System.Net.Http;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Dispatcher;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Http.Channels;

    /// <summary>
    /// Abstract base class to provide an <see cref="IErrorHandler"/> for the
    /// <see cref="HttpBinding">HttpBinding</see>
    /// </summary>
    public abstract class HttpErrorHandler : IErrorHandler
    {
        /// <summary>
        /// Enables the creation of a custom fault <see cref="Message"/>
        /// that is returned from an exception in the course of a service method.
        /// </summary>
        /// <remarks>
        /// This method is implemented solely to delegate control to 
        /// <see cref="ProvideResponse(Exception, HttpResponseMessage)"/>
        /// </remarks>
        /// <param name="error">The <see cref="Exception"/> object thrown in the course 
        /// of the service operation.</param>
        /// <param name="version">The SOAP version of the message.</param>
        /// <param name="fault">The <see cref="System.ServiceModel.Channels.Message"/> object 
        /// that is returned to the client, or service, in the duplex case.</param>
        [SuppressMessage(FxCop.Category.Design,  FxCop.Rule.InterfaceMethodsShouldBeCallableByChildTypes, 
            Justification = "The 'ProvideReponse' method provides this functionality and it is visible to derived classes.")]
        void IErrorHandler.ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {
            if (error == null)
            {
                throw Fx.Exception.ArgumentNull("error");
            }

            HttpResponseMessage responseMessage = this.ProvideResponse(error);
            fault = responseMessage.ToMessage();
        }

        /// <summary>
        /// Enables error-related processing and returns a value that indicates whether the dispatcher aborts the session and the instance context in certain cases. 
        /// </summary>
        /// <param name="error">The exception thrown during processing.</param>
        /// <returns>true if should not abort the session (if there is one) and instance context if the instance context is not Single; otherwise, false. The default is false.</returns>
        public bool HandleError(Exception error)
        {
            if (error == null)
            {
                throw Fx.Exception.ArgumentNull("error");
            }

            return this.OnHandleError(error);
        }

        /// <summary>
        /// Enables the creation of a custom response describing the specified <paramref name="error"/>.
        /// </summary>
        /// <param name="error">The error for which a response is required.</param>
        /// <returns>The <see cref="HttpResponseMessage"/> to return.  It cannot be <c>null</c>.</returns>
        public HttpResponseMessage ProvideResponse(Exception error)
        {
            if (error == null)
            {
                throw Fx.Exception.ArgumentNull("error");
            }

            HttpResponseMessage responseMessage = this.OnProvideResponse(error);
            if (responseMessage == null)
            {
                string errorMessage = SR.HttpErrorMessageNullResponse(this.GetType().Name, typeof(HttpResponseMessage).Name, "ProvideResponse");
                throw Fx.Exception.AsError(new InvalidOperationException(errorMessage));
            }
            var httpError = error as HttpResponseException;
            if (httpError != null) httpError.Response = responseMessage;
            return responseMessage;
        }

        /// <summary>
        /// Called by <see cref="HandleError(Exception)"/>.
        /// Derived classes must implement this.
        /// </summary>
        /// <param name="error">The exception thrown during processing.</param>
        /// <returns>true if should not abort the session (if there is one) and instance context if the instance context is not Single; otherwise, false. The default is false.</returns>
        protected abstract bool OnHandleError(Exception error);

        /// <summary>
        /// Called from <see cref="ProvideResponse(Exception, HttpResponseMessage)"/>.
        /// Derived classes must implement this.
        /// </summary>
        /// <param name="error">The error for which a response is required.</param>
        /// <returns>The <see cref="HttpResponseMessage"/> to return.  It cannot be <c>null</c>.</returns>
        protected abstract HttpResponseMessage OnProvideResponse(Exception error);
    }
}
