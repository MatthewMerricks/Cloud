// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.CIT.Scenario.Common
{
    using System.Net.Http;

    internal static class HttpMessageResponseExtensionMethods
    {
        public static void CopyTo(this HttpResponseMessage from, HttpResponseMessage to)
        {
            to.ReasonPhrase = from.ReasonPhrase;
            to.StatusCode = from.StatusCode;
            to.Version = from.Version;
            to.RequestMessage = from.RequestMessage;
            to.Content = from.Content;
            to.Headers.Clear();
            foreach (var header in from.Headers)
            {
                to.Headers.Add(header.Key, header.Value);
            }
        }

    }
}
