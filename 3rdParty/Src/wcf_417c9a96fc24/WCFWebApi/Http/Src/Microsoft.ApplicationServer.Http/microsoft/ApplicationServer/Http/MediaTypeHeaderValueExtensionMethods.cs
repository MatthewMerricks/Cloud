// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Net.Http.Headers;
    using Microsoft.ApplicationServer.Common;

    internal static class MediaTypeHeaderValueExtensionMethods
    {
        public static bool IsMediaRange(this MediaTypeHeaderValue mediaType)
        {
            Fx.Assert(mediaType != null, "The 'mediaType' parameter should not be null.");
            return new ParsedMediaTypeHeaderValue(mediaType).IsSubTypeMediaRange;
        }

        public static bool IsWithinMediaRange(this MediaTypeHeaderValue mediaType, MediaTypeHeaderValue mediaRange)
        {
            Fx.Assert(mediaType != null, "The 'mediaType' parameter should not be null.");
            Fx.Assert(mediaRange != null, "The 'mediaRange' parameter should not be null.");

            ParsedMediaTypeHeaderValue parsedMediaType = new ParsedMediaTypeHeaderValue(mediaType);
            ParsedMediaTypeHeaderValue parsedMediaRange = new ParsedMediaTypeHeaderValue(mediaRange);

            if (!string.Equals(parsedMediaType.Type, parsedMediaRange.Type, StringComparison.OrdinalIgnoreCase))
            {
                return parsedMediaRange.IsAllMediaRange;
            }
            else if (!string.Equals(parsedMediaType.SubType, parsedMediaRange.SubType, StringComparison.OrdinalIgnoreCase))
            {
                return parsedMediaRange.IsSubTypeMediaRange;
            }

            if (!string.IsNullOrWhiteSpace(parsedMediaRange.CharSet))
            {
                return string.Equals(parsedMediaRange.CharSet, parsedMediaType.CharSet, StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }
    }
}