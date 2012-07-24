// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Microsoft.ApplicationServer.Common;

    /// <summary>
    /// Class that provides <see cref="MediaTypeHeaderValue"/>s for a request or response
    /// from a media range.
    /// </summary>
    public sealed class MediaRangeMapping : MediaTypeMapping
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaRangeMapping"/> class.
        /// </summary>
        /// <param name="mediaRange">The <see cref="MediaTypeHeaderValue"/> that provides a description
        /// of the media range.</param>
        /// <param name="mediaType">The <see cref="MediaTypeHeaderValue"/> to return on a match.</param>
        public MediaRangeMapping(MediaTypeHeaderValue mediaRange, MediaTypeHeaderValue mediaType)
            : base(mediaType)
        {
            this.Initialize(mediaRange);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaRangeMapping"/> class.
        /// </summary>
        /// <param name="mediaRange">The description of the media range.</param>
        /// <param name="mediaType">The media type to return on a match.</param>
        public MediaRangeMapping(string mediaRange, string mediaType)
            : base(mediaType)
        {
            if (string.IsNullOrWhiteSpace(mediaRange))
            {
                throw Fx.Exception.ArgumentNull("mediaRange");
            }

            this.Initialize(new MediaTypeHeaderValue(mediaRange));
        }

        /// <summary>
        /// Gets the <see cref="MediaTypeHeaderValue"/>
        /// describing the known media range.
        /// </summary>
        public MediaTypeHeaderValue MediaRange { get; private set; }

        /// <summary>
        /// Returns a value indicating whether this <see cref="MediaRangeMapping"/>
        /// instance can provide a <see cref="MediaTypeHeaderValue"/> for the <paramref name="request"/>.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to check.</param>
        /// <returns>This method always returns <c>false</c>.</returns>
        protected override sealed bool OnSupportsMediaType(HttpRequestMessage request)
        {
            return false;
        }

        /// <summary>
        /// Returns a value indicating whether this <see cref="MediaRangeMapping"/>
        /// instance can provide a <see cref="MediaTypeHeaderValue"/> for the <paramref name="response"/>.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/> to check.</param>
        /// <returns>If this instance can match <paramref name="response"/>
        /// it returns <c>true</c> otherwise <c>false</c>.</returns>
        protected override sealed bool OnSupportsMediaType(HttpResponseMessage response)
        {
            Fx.Assert(response != null, "Base class ensures that the 'response' parameter will never be null.");
            Fx.Assert(response.RequestMessage != null, "Base class ensures that the 'response.RequestMessage' will never be null.");

            ICollection<MediaTypeWithQualityHeaderValue> acceptHeader = response.RequestMessage.Headers.Accept;
            if (acceptHeader != null)
            {
                foreach (MediaTypeWithQualityHeaderValue mediaType in acceptHeader)
                {
                    if (mediaType != null && MediaTypeHeaderValueEqualityComparer.EqualityComparer.Equals(mediaType, this.MediaRange))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void Initialize(MediaTypeHeaderValue mediaRange)
        {
            if (mediaRange == null)
            {
                throw Fx.Exception.ArgumentNull("mediaRange");
            }

            if (!mediaRange.IsMediaRange())
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.InvalidMediaRange(mediaRange.ToString())));
            }

            this.MediaRange = mediaRange;
        }
    }
}