// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http.Headers;
    using Microsoft.ApplicationServer.Common;

    internal class MediaTypeHeaderValueEqualityComparer : IEqualityComparer<MediaTypeHeaderValue>
    {
        private static readonly MediaTypeHeaderValueEqualityComparer mediaTypeEqualityComparer = new MediaTypeHeaderValueEqualityComparer();

        private MediaTypeHeaderValueEqualityComparer()
        {
        }

        public static MediaTypeHeaderValueEqualityComparer EqualityComparer
        {
            get
            {
                return mediaTypeEqualityComparer;
            }
        }

        public bool Equals(MediaTypeHeaderValue mediaType1, MediaTypeHeaderValue mediaType2)
        {
            return this.Equals(mediaType1, mediaType2, false);
        }

        public bool Equals(MediaTypeHeaderValue mediaType1, MediaTypeHeaderValue mediaType2, bool ignoreCharSet)
        {
            Fx.Assert(mediaType1 != null, "The 'mediaType1' parameter should not be null.");
            Fx.Assert(mediaType2 != null, "The 'mediaType2' parameter should not be null.");

            if (!string.Equals(mediaType1.MediaType, mediaType2.MediaType, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (ignoreCharSet)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(mediaType1.CharSet))
            {
                return string.IsNullOrWhiteSpace(mediaType2.CharSet);
            }

            return string.Equals(mediaType1.CharSet, mediaType2.CharSet, StringComparison.OrdinalIgnoreCase);
        }

        public bool Equals(MediaTypeHeaderValue mediaType1, MediaTypeHeaderValue mediaType2, string defaultCharSet)
        {
            Fx.Assert(mediaType1 != null, "The 'mediaType1' parameter should not be null.");
            Fx.Assert(mediaType2 != null, "The 'mediaType2' parameter should not be null.");
            Fx.Assert(!string.IsNullOrWhiteSpace(defaultCharSet), "The 'defaultCharSet' parameter should not be empty.");

            if (!string.Equals(mediaType1.MediaType, mediaType2.MediaType, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(mediaType1.CharSet, mediaType2.CharSet, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string charSet1 = string.IsNullOrWhiteSpace(mediaType1.CharSet) ? defaultCharSet : mediaType1.CharSet;
            string charSet2 = string.IsNullOrWhiteSpace(mediaType2.CharSet) ? defaultCharSet : mediaType2.CharSet;

            return string.Equals(charSet1, charSet2, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(MediaTypeHeaderValue mediaType)
        {
            return mediaType.MediaType.ToUpperInvariant().GetHashCode();
        }
    }
}