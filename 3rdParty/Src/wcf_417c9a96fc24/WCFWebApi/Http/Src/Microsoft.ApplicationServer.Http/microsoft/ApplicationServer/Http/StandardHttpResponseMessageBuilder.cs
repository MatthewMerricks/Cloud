// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Xml.Linq;
    using Microsoft.ApplicationServer.Common;

    /// <summary>
    /// Provides various helper methods for creating <see cref="HttpResponseMessage">HttpResponseMessage</see> instances.
    /// </summary>
    internal static class StandardHttpResponseMessageBuilder
    {
        /// <summary>
        /// Initializes a new <see cref="HttpResponseMessage"/> instance as an "HTTP/1.1 202 Accepted" response.
        /// </summary>
        /// <param name="request">The request message for which to generate the response message.</param>
        /// <returns>The initialized HTTP response.</returns>
        public static HttpResponseMessage CreateAcceptedResponse(HttpRequestMessage request)
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.Accepted, "Accepted");
            response.RequestMessage = request;
            return response;
        }

        /// <summary>
        /// Initializes a new <see cref="HttpResponseMessage"/> instance as an "HTTP/1.1 307 Accepted" response.
        /// </summary>
        /// <param name="request">The request message for which to generate the response message.</param>
        /// <param name="oldLocation">The URI being redirected from.</param>
        /// <param name="newLocation">The URI being redirected to.</param>
        /// <returns>The initialized HTTP response.</returns>
        public static HttpResponseMessage CreateTemporaryRedirectResponse(HttpRequestMessage request, Uri oldLocation, Uri newLocation)
        {
            Fx.Assert(oldLocation != null && newLocation != null, "old and new location cannot be null.");

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.TemporaryRedirect, "Temporary Redirect");
            Uri normalizedOldLocationUri = request != null ? oldLocation.GetHostNormalizedUri(request) : oldLocation;
            Uri normalizedNewLocationUri = request != null ? newLocation.GetHostNormalizedUri(request) : newLocation;

            response.Content = new ActionOfStreamContent(
                stream =>
                {
                    var xDoc = HtmlPageBuilder.CreateTransferRedirectPage(normalizedOldLocationUri.AbsoluteUri, normalizedNewLocationUri.AbsoluteUri);
                    xDoc.Save(stream, SaveOptions.OmitDuplicateNamespaces);
                });

            response.Headers.Location = normalizedNewLocationUri;
            response.Content.Headers.ContentType = MediaTypeConstants.HtmlMediaType;
            response.RequestMessage = request;
            return response;
        }

        /// <summary>
        /// Initializes a new <see cref="HttpResponseMessage"/> instance as an "HTTP/1.1 400 Bad Request" response.
        /// </summary>
        /// <param name="request">The request message for which to generate the response message.</param>
        /// <returns>The initialized HTTP response.</returns>
        public static HttpResponseMessage CreateBadRequestResponse(HttpRequestMessage request)
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.BadRequest, "Bad Request");
            response.RequestMessage = request;
            return response;
        }

        /// <summary>
        /// Initializes a new <see cref="HttpResponseMessage"/> instance as an "HTTP/1.1 404 Not Found" response.
        /// </summary>
        /// <param name="request">The request message for which to generate the response message.</param>
        /// <param name="helpPageUri">Help page URI or null if not available.</param>
        /// <returns>The initialized HTTP response.</returns>
        public static HttpResponseMessage CreateNotFoundResponse(HttpRequestMessage request, Uri helpPageUri)
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.NotFound, "Not Found");
            Uri normalizedHelpPageUri = (helpPageUri != null && request != null) ?
                helpPageUri.GetHostNormalizedUri(request) :
                null;

            response.Content = new ActionOfStreamContent(
                stream =>
                {
                    var xDoc = HtmlPageBuilder.CreateEndpointNotFound(normalizedHelpPageUri);
                    xDoc.Save(stream, SaveOptions.OmitDuplicateNamespaces);
                });

            response.Content.Headers.ContentType = MediaTypeConstants.HtmlMediaType;
            response.RequestMessage = request;
            return response;
        }

        /// <summary>
        /// Initializes a new <see cref="HttpResponseMessage"/> instance as an "HTTP/1.1 405 Method Not Allowed" response.
        /// </summary>
        /// <param name="request">The request message for which to generate the response message.</param>
        /// <param name="allowedMethods">List of allowed methods.</param>
        /// <param name="helpPageUri">Help page URI or null if not available.</param>
        /// <returns>The initialized HTTP response.</returns>
        public static HttpResponseMessage CreateMethodNotAllowedResponse(HttpRequestMessage request, IEnumerable<HttpMethod> allowedMethods, Uri helpPageUri)
        {
            Fx.Assert(allowedMethods != null, "Allowed methods should not be null.");

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.MethodNotAllowed, "Method Not Allowed");
            Uri normalizedHelpPageUri = (helpPageUri != null && request != null) ?
                helpPageUri.GetHostNormalizedUri(request) :
                null;

            response.Content = new ActionOfStreamContent(
                stream =>
                {
                    var xDoc = HtmlPageBuilder.CreateMethodNotAllowedPage(normalizedHelpPageUri);
                    xDoc.Save(stream, SaveOptions.OmitDuplicateNamespaces);
                });

            response.Content.Headers.ContentType = MediaTypeConstants.HtmlMediaType;
            foreach (HttpMethod allowedMethod in allowedMethods)
            {
                response.Content.Headers.Allow.Add(allowedMethod.Method);
            }

            response.RequestMessage = request;
            return response;
        }

        /// <summary>
        /// Initializes a new <see cref="HttpResponseMessage"/> instance as an "HTTP/1.1 501 Internal Server Error" response.
        /// </summary>
        /// <param name="request">The request message for which to generate the response message.</param>
        /// <param name="error">The exception to include if <paramref name="includeExceptionDetailInFaults"/> is set to true.</param>
        /// <param name="includeExceptionDetailInFaults">Indicates whether to include exception details in the response. This value should be obtained
        /// from the current <see cref="System.ServiceModel.Description.ServiceDebugBehavior"/> instance.</param>
        /// <param name="helpPageUri">Help page URI or null if not available.</param>
        /// <returns>The initialized HTTP response.</returns>
        public static HttpResponseMessage CreateInternalServerErrorResponse(HttpRequestMessage request, Exception error, bool includeExceptionDetailInFaults, Uri helpPageUri)
        {
            Fx.Assert(error != null, "Error cannot be null.");

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.InternalServerError, "Internal Server Error");
            Uri normalizedHelpPageUri = (helpPageUri != null && request != null) ?
                helpPageUri.GetHostNormalizedUri(request) :
                null;

            response.Content = new ActionOfStreamContent(
                stream =>
                {
                    var xDoc = HtmlPageBuilder.CreateServerErrorPage(normalizedHelpPageUri, includeExceptionDetailInFaults ? error : null);
                    xDoc.Save(stream, SaveOptions.OmitDuplicateNamespaces);
                });

            response.Content.Headers.ContentType = MediaTypeConstants.HtmlMediaType;
            response.RequestMessage = request;
            return response;
        }
    }
}
