// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System.Collections.Generic;
    using System.Net.Http.Headers;
    using Microsoft.ApplicationServer.Common;

    internal static class HttpHeaderExtensionMethods
    {
        public static void CopyTo(this HttpContentHeaders fromHeaders, HttpContentHeaders toHeaders)
        {
            Fx.Assert(fromHeaders != null, "fromHeaders cannot be null.");
            Fx.Assert(toHeaders != null, "toHeaders cannot be null.");

            foreach (KeyValuePair<string, IEnumerable<string>> header in fromHeaders)
            {
                toHeaders.Add(header.Key, header.Value);
            }
        }

        public static void CopyTo(this HttpRequestHeaders fromHeaders, HttpRequestHeaders toHeaders)
        {
            Fx.Assert(fromHeaders != null, "fromHeaders cannot be null.");
            Fx.Assert(toHeaders != null, "toHeaders cannot be null.");

            foreach (KeyValuePair<string, IEnumerable<string>> header in fromHeaders)
            {
                toHeaders.Add(header.Key, header.Value);
            }
        }

        public static void CopyTo(this HttpResponseHeaders fromHeaders, HttpResponseHeaders toHeaders)
        {
            Fx.Assert(fromHeaders != null, "fromHeaders cannot be null.");
            Fx.Assert(toHeaders != null, "toHeaders cannot be null.");

            foreach (KeyValuePair<string, IEnumerable<string>> header in fromHeaders)
            {
                toHeaders.Add(header.Key, header.Value);
            }
        }
    }
}