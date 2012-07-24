// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Globalization;
    using System.Net.Http;
    using Microsoft.ApplicationServer.Common;

    /// <summary>
    /// Extension methods for <see cref="Uri"/>
    /// </summary>
    internal static class UriExtensionMethods
    {
        /// <summary>
        /// Returns a <see cref="Uri"/> that has been normalized to contain information
        /// from the host headers of <paramref name="request"/>.
        /// </summary>
        /// <param name="uri">The existing <see cref="Uri"/>.</param>
        /// <param name="request">The <see cref="HttpRequestMessage"/> whose host headers should be used.</param>
        /// <returns>A <see cref="Uri"/> containing the host header information if it was available.</returns>
        public static Uri GetHostNormalizedUri(this Uri uri, HttpRequestMessage request)
        {
            Fx.Assert(uri != null, "The 'uri' parameter should not be null.");

            if (request != null)
            {
                string hostHeader = request.Headers.Host;
                if (!string.IsNullOrEmpty(hostHeader))
                {
                    string uriHost = uri.IsDefaultPort ?
                        uri.Host :
                        string.Format(CultureInfo.InvariantCulture, "{0}:{1}", uri.Host, uri.Port);

                    if (!string.Equals(uriHost, hostHeader, StringComparison.OrdinalIgnoreCase))
                    {
                        string schemeAndHost = string.Format(CultureInfo.InvariantCulture, "{0}://{1}", uri.Scheme, hostHeader);
                        Uri schemeAndHostOnlyUri = new Uri(schemeAndHost);
                        UriBuilder builder = new UriBuilder(uri);
                        builder.Host = schemeAndHostOnlyUri.Host;
                        builder.Port = schemeAndHostOnlyUri.Port;
                        return builder.Uri;
                    }
                }
            }

            return uri;
        }
    }
}
