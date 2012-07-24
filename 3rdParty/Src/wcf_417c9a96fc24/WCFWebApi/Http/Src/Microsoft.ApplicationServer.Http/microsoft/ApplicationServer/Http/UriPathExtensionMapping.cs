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
    /// Class that provides <see cref="MediaTypeHeaderValue"/>s from path extensions appearing
    /// in a <see cref="Uri"/>.
    /// </summary>
    public sealed class UriPathExtensionMapping : MediaTypeMapping
    {
        private static readonly Type uriPathExtensionMappingType = typeof(UriPathExtensionMapping);

        /// <summary>
        /// Initializes a new instance of the <see cref="UriPathExtensionMapping"/> class.
        /// </summary>
        /// <param name="uriPathExtension">The extension corresponding to <paramref name="mediaType"/>.
        /// This value should not include a dot or wildcards.</param>
        /// <param name="mediaType">The media type that will be returned
        /// if <paramref name="uriPathExtension"/> is matched.</param>
        public UriPathExtensionMapping(string uriPathExtension, string mediaType)
            : base(mediaType)
        {
            this.Initialize(uriPathExtension);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UriPathExtensionMapping"/> class.
        /// </summary>
        /// <param name="uriPathExtension">The extension corresponding to <paramref name="mediaType"/>.
        /// This value should not include a dot or wildcards.</param>
        /// <param name="mediaType">The <see cref="MediaTypeHeaderValue"/> that will be returned
        /// if <paramref name="uriPathExtension"/> is matched.</param>
        public UriPathExtensionMapping(string uriPathExtension, MediaTypeHeaderValue mediaType)
            : base(mediaType)
        {
            this.Initialize(uriPathExtension);
        }

        /// <summary>
        /// Gets the <see cref="Uri"/> path extension.
        /// </summary>
        public string UriPathExtension { get; private set; }

        /// <summary>
        /// Returns a value indicating whether this <see cref="UriPathExtensionMapping"/>
        /// instance can provide a <see cref="MediaTypeHeaderValue"/> for the <see cref="Uri"/> 
        /// of <paramref name="request"/>.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to check.</param>
        /// <returns>If this instance can match a file extension in <paramref name="request"/>
        /// it returns <c>true</c> otherwise <c>false</c>.</returns>
        protected override sealed bool OnSupportsMediaType(HttpRequestMessage request)
        {
            Fx.Assert(request != null, "Base class ensures that the 'request' parameter will never be null.");

            string extension = GetUriPathExtensionOrNull(request.RequestUri);
            return string.Equals(extension, this.UriPathExtension, StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns a value indicating whether this <see cref="UriPathExtensionMapping"/>
        /// instance can provide a <see cref="MediaTypeHeaderValue"/> for the <see cref="Uri"/> 
        /// of <paramref name="response"/>.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/> to check.</param>
        /// <returns>If this instance can match a file extension in <paramref name="response"/>
        /// it returns <c>true</c> otherwise <c>false</c>.</returns>
        protected override sealed bool OnSupportsMediaType(HttpResponseMessage response)
        {
            Fx.Assert(response != null, "Base class ensures that the 'response' parameter will never be null.");
            Fx.Assert(response.RequestMessage != null, "Base class ensures that the 'response.RequestMessage' will never be null.");

            string extension = GetUriPathExtensionOrNull(response.RequestMessage.RequestUri);
            return string.Equals(extension, this.UriPathExtension, StringComparison.Ordinal);
        }

        private static string GetUriPathExtensionOrNull(Uri uri)
        {
            if (uri == null)
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(SR.NonNullUriRequiredForMediaTypeMapping(uriPathExtensionMappingType.Name)));
            }

            string uriPathExtension = null;
            int numberOfSegments = uri.Segments.Length;
            if (numberOfSegments > 0)
            {
                string lastSegment = uri.Segments[numberOfSegments - 1];
                int indexAfterFirstPeriod = lastSegment.IndexOf('.') + 1;
                if (indexAfterFirstPeriod > 0 && indexAfterFirstPeriod < lastSegment.Length)
                {
                    uriPathExtension = lastSegment.Substring(indexAfterFirstPeriod);
                }
            }

            return uriPathExtension;
        }

        private void Initialize(string uriPathExtension)
        {
            if (string.IsNullOrWhiteSpace(uriPathExtension))
            {
                throw Fx.Exception.ArgumentNull("uriPathExtension");
            }

            this.UriPathExtension = uriPathExtension.Trim().TrimStart('.');
        }
    }
}