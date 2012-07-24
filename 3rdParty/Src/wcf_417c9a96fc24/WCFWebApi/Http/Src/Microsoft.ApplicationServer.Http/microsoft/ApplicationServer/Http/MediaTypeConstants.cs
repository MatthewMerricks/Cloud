// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System.Net.Http.Headers;

    /// <summary>
    /// Constants related to media types.
    /// </summary>
    internal class MediaTypeConstants
    {
        internal const string DefaultApplicationXmlMediaType = "application/xml";
        internal const string DefaultTextXmlMediaType = "text/xml";
        internal const string DefaultApplicationJsonMediaType = "application/json";
        internal const string DefaultTextJsonMediaType = "text/json";
        internal const string DefaultTextHtmlMediaType = "text/html";
        internal const string DefaultCharSet = "utf-8";

        internal static MediaTypeHeaderValue HtmlMediaType
        {
            get
            {
                return new MediaTypeHeaderValue(DefaultTextHtmlMediaType) { CharSet = DefaultCharSet };
            }
        }

        internal static MediaTypeHeaderValue ApplicationXmlMediaType
        {
            get
            {
                return new MediaTypeHeaderValue(DefaultApplicationXmlMediaType) { CharSet = DefaultCharSet };
            }
        }

        internal static MediaTypeHeaderValue ApplicationJsonMediaType
        {
            get
            {
                return new MediaTypeHeaderValue(DefaultApplicationJsonMediaType) { CharSet = DefaultCharSet };
            }
        }

        internal static MediaTypeHeaderValue TextXmlMediaType
        {
            get
            {
                return new MediaTypeHeaderValue(DefaultTextXmlMediaType) { CharSet = DefaultCharSet };
            }
        }

        internal static MediaTypeHeaderValue TextJsonMediaType
        {
            get
            {
                return new MediaTypeHeaderValue(DefaultTextJsonMediaType) { CharSet = DefaultCharSet };
            }
        }
    }
}