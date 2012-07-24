// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Net.Http.Headers;
    using Microsoft.ApplicationServer.Common;

    /// <summary>
    /// Extension methods to provide convenience in adding <see cref="MediaTypeMapping"/>
    /// items to a <see cref="MediaTypeFormatter"/>.
    /// </summary>
    public static class MediaTypeFormatterExtensionMethods
    {
        /// <summary>
        /// Updates the given <see cref="formatter"/>'s set of <see cref="MediaTypeMapping"/> elements
        /// so that it associates the <paramref name="mediaType"/> with <see cref="Uri"/>s containing
        /// a specific query parameter and value.
        /// </summary>
        /// <param name="formatter">The <see cref="MediaTypeFormatter"/> to receive the new <see cref="QueryStringMapping"/> item.</param>
        /// <param name="queryStringParameterName">The name of the query parameter.</param>
        /// <param name="queryStringParameterValue">The value assigned to that query parameter.</param>
        /// <param name="mediaType">The <see cref="MediaTypeHeaderValue"/> to associate 
        /// with a <see cref="Uri"/> containing a query string matching <see cref="queryStringParameterName"/> 
        /// and <see cref="queryStringParameterValue"/>.</param>
        public static void AddQueryStringMapping(
                                this MediaTypeFormatter formatter, 
                                string queryStringParameterName, 
                                string queryStringParameterValue,
                                MediaTypeHeaderValue mediaType)
        {
            if (formatter == null)
            {
                throw Fx.Exception.ArgumentNull("formatter");
            }

            QueryStringMapping mapping = new QueryStringMapping(queryStringParameterName, queryStringParameterValue, mediaType);
            formatter.MediaTypeMappings.Add(mapping);
        }

        /// <summary>
        /// Updates the given <see cref="formatter"/>'s set of <see cref="MediaTypeMapping"/> elements
        /// so that it associates the <paramref name="mediaType"/> with <see cref="Uri"/>s containing
        /// a specific query parameter and value.
        /// </summary>
        /// <param name="formatter">The <see cref="MediaTypeFormatter"/> to receive the new <see cref="QueryStringMapping"/> item.</param>
        /// <param name="queryStringParameterName">The name of the query parameter.</param>
        /// <param name="queryStringParameterValue">The value assigned to that query parameter.</param>
        /// <param name="mediaType">The media type to associate 
        /// with a <see cref="Uri"/> containing a query string matching <see cref="queryStringParameterName"/> 
        /// and <see cref="queryStringParameterValue"/>.</param>
        public static void AddQueryStringMapping(
                                this MediaTypeFormatter formatter,
                                string queryStringParameterName,
                                string queryStringParameterValue,
                                string mediaType)
        {
            if (formatter == null)
            {
                throw Fx.Exception.ArgumentNull("formatter");
            }

            QueryStringMapping mapping = new QueryStringMapping(queryStringParameterName, queryStringParameterValue, mediaType);
            formatter.MediaTypeMappings.Add(mapping);
        }

        /// <summary>
        /// Updates the given <see cref="formatter"/>'s set of <see cref="MediaTypeMapping"/> elements
        /// so that it associates the <paramref name="mediaType"/> with <see cref="Uri"/>s ending with
        /// the given <paramref name="uriPathExtension"/>.
        /// </summary>
        /// <param name="formatter">The <see cref="MediaTypeFormatter"/> to receive the new <see cref="UriPathExtensionMapping"/> item.</param>
        /// <param name="uriPathExtension">The string of the <see cref="Uri"/> path extension.</param>
        /// <param name="mediaType">The <see cref="MediaTypeHeaderValue"/> to associate with <see cref="Uri"/>s
        /// ending with <paramref name="uriPathExtension"/>.</param>
        public static void AddUriPathExtensionMapping(
                                this MediaTypeFormatter formatter, 
                                string uriPathExtension, 
                                MediaTypeHeaderValue mediaType)
        {
            if (formatter == null)
            {
                throw Fx.Exception.ArgumentNull("formatter");
            }

            UriPathExtensionMapping mapping = new UriPathExtensionMapping(uriPathExtension, mediaType);
            formatter.MediaTypeMappings.Add(mapping);
        }

        /// <summary>
        /// Updates the given <see cref="formatter"/>'s set of <see cref="MediaTypeMapping"/> elements
        /// so that it associates the <paramref name="mediaType"/> with <see cref="Uri"/>s ending with
        /// the given <paramref name="uriPathExtension"/>.
        /// </summary>
        /// <param name="formatter">The <see cref="MediaTypeFormatter"/> to receive the new <see cref="UriPathExtensionMapping"/> item.</param>
        /// <param name="uriPathExtension">The string of the <see cref="Uri"/> path extension.</param>
        /// <param name="mediaType">The string media type to associate with <see cref="Uri"/>s
        /// ending with <paramref name="uriPathExtension"/>.</param>
        public static void AddUriPathExtensionMapping(this MediaTypeFormatter formatter, string uriPathExtension, string mediaType)
        {
            if (formatter == null)
            {
                throw Fx.Exception.ArgumentNull("formatter");
            }

            UriPathExtensionMapping mapping = new UriPathExtensionMapping(uriPathExtension, mediaType);
            formatter.MediaTypeMappings.Add(mapping);
        }

        /// <summary>
        /// Updates the given <see cref="formatter"/>'s set of <see cref="MediaTypeMapping"/> elements
        /// so that it associates the <paramref name="mediaType"/> with requests or responses containing
        /// <paramref name="mediaRange"/> in the content headers.
        /// </summary>
        /// <param name="formatter">The <see cref="MediaTypeFormatter"/> to receive the new <see cref="MediaRangeMapping"/> item.</param>
        /// <param name="mediaRange">The media range that will appear in the content headers.</param>
        /// <param name="mediaType">The media type to associate with that <paramref name="mediaRange"/>.</param>
        public static void AddMediaRangeMapping(this MediaTypeFormatter formatter, string mediaRange, string mediaType)
        {
            if (formatter == null)
            {
                throw Fx.Exception.ArgumentNull("formatter");
            }

            MediaRangeMapping mapping = new MediaRangeMapping(mediaRange, mediaType);
            formatter.MediaTypeMappings.Add(mapping);
        }

        /// <summary>
        /// Updates the given <see cref="formatter"/>'s set of <see cref="MediaTypeMapping"/> elements
        /// so that it associates the <paramref name="mediaType"/> with requests or responses containing
        /// <paramref name="mediaRange"/> in the content headers.
        /// </summary>
        /// <param name="formatter">The <see cref="MediaTypeFormatter"/> to receive the new <see cref="MediaRangeMapping"/> item.</param>
        /// <param name="mediaRange">The media range that will appear in the content headers.</param>
        /// <param name="mediaType">The media type to associate with that <paramref name="mediaRange"/>.</param>
        public static void AddMediaRangeMapping(
                                this MediaTypeFormatter formatter, 
                                MediaTypeHeaderValue mediaRange, 
                                MediaTypeHeaderValue mediaType)
        {
            if (formatter == null)
            {
                throw Fx.Exception.ArgumentNull("formatter");
            }

            MediaRangeMapping mapping = new MediaRangeMapping(mediaRange, mediaType);
            formatter.MediaTypeMappings.Add(mapping);
        }
    }
}
