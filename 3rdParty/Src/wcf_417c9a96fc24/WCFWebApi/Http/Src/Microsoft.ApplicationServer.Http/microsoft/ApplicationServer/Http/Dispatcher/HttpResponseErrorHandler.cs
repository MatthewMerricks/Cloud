// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.ServiceModel;
    using System.ServiceModel.Web;
    using System.Text;
    using Microsoft.ApplicationServer.Common;

    internal class HttpResponseErrorHandler : HttpErrorHandler
    {
        private static readonly Encoding Utf8EncodingSansBOM = new UTF8Encoding(false);

        private MediaTypeFormatterCollection formatters;
        private Uri helpUri;
        private bool includeExceptionDetail;

        internal HttpResponseErrorHandler(IEnumerable<MediaTypeFormatter> formatters, Uri helpUri, bool includeExceptionDetail)
        {
            Fx.Assert(formatters != null, "The 'formatters' parameter should not be null.");

            this.formatters = new MediaTypeFormatterCollection(formatters);
            this.helpUri = helpUri;
            this.includeExceptionDetail = includeExceptionDetail;
        }

        /// <summary>
        /// Enables error-related processing and returns a value that indicates whether
        /// the dispatcher should abort the session and the instance context in certain cases.
        /// </summary>
        /// <param name="error">The exception thrown during processing.</param>
        /// <returns><c>true</c> if should not abort the session (if there is one) and instance
        /// context if the instance context is not single; otherwise <c>false</c>.
        /// The default is <c>true</c> because this class's <see cref="OnProvideResponse"/>
        /// will unconditionally provide a response for all errors.</returns>
        protected override bool OnHandleError(Exception error)
        {
            return true;
        }

        /// <summary>
        /// Enables the creation of a custom <see cref="HttpResponseMessage"/> that is returned
        /// when an exception is encountered servicing an Http request.
        /// </summary>
        /// <param name="error">The exception thrown in the course of executing the Http request.</param>
        /// <returns>The <see cref="HttpResponseMessage"/> to return.  It cannot be <c>null</c>.</returns>
        protected override HttpResponseMessage OnProvideResponse(Exception error)
        {
            HttpResponseMessage response = null;

            HttpResponseException errorAsResponseException = error as HttpResponseException;
            if (errorAsResponseException != null)
            {
                response = errorAsResponseException.Response;
            }
            else
            {
                WebFaultExceptionWrapper webFault = new WebFaultExceptionWrapper(error);
                if (webFault.IsWebFaultException)
                {
                    object detailObject = webFault.DetailObject;
                    if (detailObject != null)
                    {
                        response = detailObject as HttpResponseMessage;
                        if (response == null)
                        {
                            response = new HttpResponseMessage();
                            response.Content = new ObjectContent(webFault.DetailType, detailObject);
                        }
                    }
                    else
                    {
                        response = new HttpResponseMessage();
                    }

                    response.StatusCode = webFault.StatusCode;
                }
                else
                {
                    response = this.CreateHtmlResponse(error);
                }
            }

            this.PrepareHttpResponse(response);
            return response;
        }

        private HttpResponseMessage CreateHtmlResponse(Exception error)
        {
            HttpRequestMessage requestMessage = OperationContext.Current.GetHttpRequestMessage();
            HttpResponseMessage responseMessage = StandardHttpResponseMessageBuilder.CreateInternalServerErrorResponse(
                                                                                                                    requestMessage,
                                                                                                                    error,
                                                                                                                    this.includeExceptionDetail,
                                                                                                                    this.helpUri);

            return responseMessage;
        }

        private void PrepareHttpResponse(HttpResponseMessage response)
        {
            ObjectContent objectContent = response.Content as ObjectContent;
            if (objectContent != null)
            {
                objectContent.Formatters.ReplaceAllWith(this.formatters);
            }

            if (response.RequestMessage == null)
            {
                response.RequestMessage = OperationContext.Current.GetHttpRequestMessage();
            }
        }

        private class WebFaultExceptionWrapper
        {
            private static readonly Type genericWebFaultExceptionType = typeof(WebFaultException<>);

            internal WebFaultExceptionWrapper(Exception error)
            {
                Fx.Assert(error != null, "error cannot be null");

                WebFaultException asWebFaultException = error as WebFaultException;
                Type errorType = error.GetType();
                bool isGenericWebFaultException = errorType.IsGenericType && errorType.GetGenericTypeDefinition() == genericWebFaultExceptionType;

                if (isGenericWebFaultException || asWebFaultException != null)
                {
                    this.IsWebFaultException = true;

                    if (isGenericWebFaultException)
                    {
                        this.InitializeFromGenericWebFaultException(error);
                    }
                    else
                    {
                        this.InitializeFromWebFaultException(asWebFaultException);
                    }
                }
            }

            internal bool IsWebFaultException { get; private set; }

            internal object DetailObject { get; private set; }

            internal Type DetailType { get; private set; }

            internal HttpStatusCode StatusCode { get; private set; }

            private void InitializeFromWebFaultException(WebFaultException webFaultException)
            {
                this.StatusCode = webFaultException.StatusCode;
                this.DetailObject = null;
                this.DetailType = null;
            }

            private void InitializeFromGenericWebFaultException(Exception error)
            {
                Type exceptionType = error.GetType();
                this.DetailType = exceptionType.GetGenericArguments()[0];

                // The following 2 Reflection accessors only involve public API.
                // StatusCode is defined in WebFaultException<T>.
                PropertyInfo statusProperty = exceptionType.GetProperty("StatusCode", BindingFlags.Instance | BindingFlags.Public);
                Fx.Assert(statusProperty != null, "Could not get StatusCode property");
                this.StatusCode = (HttpStatusCode)statusProperty.GetValue(error, null);

                // Detail is defined in FaultException<T>
                PropertyInfo detailObjectProperty = exceptionType.GetProperty("Detail", BindingFlags.Instance | BindingFlags.Public);
                Fx.Assert(detailObjectProperty != null, "Could not get DetailObject property");
                this.DetailObject = detailObjectProperty.GetValue(error, null);
            }
        }
    }
}
