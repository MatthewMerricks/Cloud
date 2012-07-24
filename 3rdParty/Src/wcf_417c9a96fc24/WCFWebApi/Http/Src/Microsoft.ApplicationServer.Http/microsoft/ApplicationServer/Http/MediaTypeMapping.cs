// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Microsoft.ApplicationServer.Common;

    /// <summary>
    /// An abstract base class used to create an association between <see cref="HttpRequestMessage"/> or 
    /// <see cref="HttpResponseMessage"/> instances that have certain characteristics 
    /// and a specific <see cref="MediaTypeHeaderValue"/>. 
    /// </summary>
    public abstract class MediaTypeMapping
    {
        private static readonly Type httpRequestMessageType = typeof(HttpRequestMessage);
        private static readonly Type httpResponseMessageType = typeof(HttpResponseMessage);

        /// <summary>
        /// Initializes a new instance of a <see cref="MediaTypeMapping"/> with the
        /// given <paramref name="mediaType"/> value.
        /// </summary>
        /// <param name="mediaType">
        /// The <see cref="MediaTypeHeaderValue"/> that is associated with <see cref="HttpRequestMessage"/> or 
        /// <see cref="HttpResponseMessage"/> instances that have the given characteristics of the 
        /// <see cref="MediaTypeMapping"/>.
        /// </param>
        protected MediaTypeMapping(MediaTypeHeaderValue mediaType)
        {
            if (mediaType == null)
            {
                throw Fx.Exception.ArgumentNull("mediaType");
            }

            this.MediaType = mediaType;
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="MediaTypeMapping"/> with the
        /// given <paramref name="mediaType"/> value.
        /// </summary>
        /// <param name="mediaType">
        /// The <see cref="string"/> that is associated with <see cref="HttpRequestMessage"/> or 
        /// <see cref="HttpResponseMessage"/> instances that have the given characteristics of the 
        /// <see cref="MediaTypeMapping"/>.
        /// </param>
        protected MediaTypeMapping(string mediaType)
        {
            if (string.IsNullOrWhiteSpace(mediaType))
            {
                throw Fx.Exception.ArgumentNull("mediaType");
            }

            this.MediaType = new MediaTypeHeaderValue(mediaType);
        }

        /// <summary>
        /// Gets the <see cref="MediaTypeHeaderValue"/> that is associated with <see cref="HttpRequestMessage"/> or 
        /// <see cref="HttpResponseMessage"/> instances that have the given characteristics of the 
        /// <see cref="MediaTypeMapping"/>.
        /// </summary>
        public MediaTypeHeaderValue MediaType { get; private set; }

        /// <summary>
        /// Returns true if the given <paramref name="request"/> has the
        /// characteristics that are associated with the <see cref="MediaTypeHeaderValue"/>
        /// of the <see cref="MediaTypeMapping"/>.
        /// </summary>
        /// <param name="request">
        /// The <see cref="HttpRequestMessage"/> to evaluate for the characteristics 
        /// associated with the <see cref="MediaTypeHeaderValue"/>
        /// of the <see cref="MediaTypeMapping"/>.
        /// </param> 
        /// <returns>
        /// True if the given <paramref name="request"/> has the
        /// characteristics that are associated with the <see cref="MediaTypeHeaderValue"/>
        /// of the <see cref="MediaTypeMapping"/>. 
        /// </returns>
        public bool SupportsMediaType(HttpRequestMessage request)
        {
            if (request == null)
            {
                throw Fx.Exception.ArgumentNull("request");
            }

            return this.OnSupportsMediaType(request);
        }

        /// <summary>
        /// Returns true if the given <paramref name="response"/> has the
        /// characteristics that are associated with the <see cref="MediaTypeHeaderValue"/>
        /// of the <see cref="MediaTypeMapping"/>.
        /// </summary>
        /// <param name="response">
        /// The <see cref="HttpResponseMessage"/> to evaluate for the characteristics 
        /// associated with the <see cref="MediaTypeHeaderValue"/>
        /// of the <see cref="MediaTypeMapping"/>.
        /// </param> 
        /// <returns>
        /// True if the given <paramref name="response"/> has the
        /// characteristics that are associated with the <see cref="MediaTypeHeaderValue"/>
        /// of the <see cref="MediaTypeMapping"/>. 
        /// </returns>
        public bool SupportsMediaType(HttpResponseMessage response)
        {
            if (response == null)
            {
                throw Fx.Exception.ArgumentNull("response");
            }

            if (response.RequestMessage == null)
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(SR.ResponseMustReferenceRequest(httpResponseMessageType.Name, "response", httpRequestMessageType.Name, "RequestMessage")));
            }

            return this.OnSupportsMediaType(response);
        }

        /// <summary>
        /// Implemented in a derived class to determine if the <see cref="HttpRequestMessage"/> 
        /// should be associated with the <see cref="MediaTypeHeaderValue"/>
        /// of the <see cref="MediaTypeMapping"/>. 
        /// </summary>
        /// <param name="request">
        /// The <see cref="HttpRequestMessage"/> to evaluate for the characteristics 
        /// associated with the <see cref="MediaTypeHeaderValue"/>
        /// of the <see cref="MediaTypeMapping"/>.
        /// </param> 
        /// <returns>
        /// True if the given <paramref name="request"/> has the
        /// characteristics that are associated with the <see cref="MediaTypeHeaderValue"/>
        /// of the <see cref="MediaTypeMapping"/>. 
        /// </returns>
        protected abstract bool OnSupportsMediaType(HttpRequestMessage request);

        /// <summary>
        /// Implemented in a derived class to determine if the <see cref="HttpResponseMessage"/> 
        /// should be associated with the <see cref="MediaTypeHeaderValue"/>
        /// of the <see cref="MediaTypeMapping"/>. 
        /// </summary>
        /// <param name="response">
        /// The <see cref="HttpResponseMessage"/> to evaluate for the characteristics 
        /// associated with the <see cref="MediaTypeHeaderValue"/>
        /// of the <see cref="MediaTypeMapping"/>.
        /// </param> 
        /// <returns>
        /// True if the given <paramref name="response"/> has the
        /// characteristics that are associated with the <see cref="MediaTypeHeaderValue"/>
        /// of the <see cref="MediaTypeMapping"/>. 
        /// </returns>
        protected abstract bool OnSupportsMediaType(HttpResponseMessage response);
    }
}