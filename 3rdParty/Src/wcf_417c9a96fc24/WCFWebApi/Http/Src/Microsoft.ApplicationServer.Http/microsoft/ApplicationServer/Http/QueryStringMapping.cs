// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Collections.Specialized;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Web;
    using Microsoft.ApplicationServer.Common;

    /// <summary>
    /// Class that provides <see cref="MediaTypeHeaderValue"/>s from query strings.
    /// </summary>
    public sealed class QueryStringMapping : MediaTypeMapping
    {
        private static readonly Type queryStringMappingType = typeof(QueryStringMapping);

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryStringMapping"/> class.
        /// </summary>
        /// <param name="queryStringParameterName">The name of the query string parameter to match, if present.</param>
        /// <param name="queryStringParameterValue">The value of the query string parameter specified by <paramref name="queryStringParameterName"/>.</param>
        /// <param name="mediaType">The media type to use if the query parameter specified by <paramref name="queryStringParameterName"/> is present
        /// and assigned the value specified by <paramref name="queryStringParameterValue"/>.</param>
        public QueryStringMapping(string queryStringParameterName, string queryStringParameterValue, string mediaType)
            : base(mediaType)
        {
            this.Initialize(queryStringParameterName, queryStringParameterValue);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryStringMapping"/> class.
        /// </summary>
        /// <param name="queryStringParameterName">The name of the query string parameter to match, if present.</param>
        /// <param name="queryStringParameterValue">The value of the query string parameter specified by <paramref name="queryStringParameterName"/>.</param>
        /// <param name="mediaType">The <see cref="MediaTypeHeaderValue"/> to use if the query parameter specified by <paramref name="queryStringParameterName"/> is present
        /// and assigned the value specified by <paramref name="queryStringParameterValue"/>.</param>
        public QueryStringMapping(string queryStringParameterName, string queryStringParameterValue, MediaTypeHeaderValue mediaType)
            : base(mediaType)
        {
            this.Initialize(queryStringParameterName, queryStringParameterValue);
        }

        /// <summary>
        /// Gets the query string parameter name.
        /// </summary>
        public string QueryStringParameterName { get; private set; }

        /// <summary>
        /// Gets the query string parameter value.
        /// </summary>
        public string QueryStringParameterValue { get; private set; }

        /// <summary>
        /// Returns a value indicating whether the current <see cref="QueryStringMapping"/>
        /// instance can return a <see cref="MediaTypeHeaderValue"/> from <paramref name="request"/>.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to check.</param>
        /// <returns>If this instance can produce a <see cref="MediaTypeHeaderValue"/> from <paramref name="request"/>
        /// it returns <c>true</c> otherwise <c>false</c>.</returns>
        protected override sealed bool OnSupportsMediaType(HttpRequestMessage request)
        {
            Fx.Assert(request != null, "Base class ensures that the 'request' parameter will never be null.");

            NameValueCollection queryString = GetQueryString(request.RequestUri);
            return this.DoesQueryStringMatch(queryString);
        }

        /// <summary>
        /// Returns a value indicating whether the current <see cref="QueryStringMapping"/>
        /// instance can return a <see cref="MediaTypeHeaderValue"/> from <paramref name="response"/>.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/> to check.</param>
        /// <returns>If this instance can produce a <see cref="MediaTypeHeaderValue"/> from <paramref name="response"/>
        /// it returns <c>true</c> otherwise <c>false</c>.</returns>
        protected override sealed bool OnSupportsMediaType(HttpResponseMessage response)
        {
            Fx.Assert(response != null, "Base class ensures that the 'response' parameter will never be null.");
            Fx.Assert(response.RequestMessage != null, "Base class ensures that the 'response.RequestMessage' will never be null.");

            NameValueCollection queryString = GetQueryString(response.RequestMessage.RequestUri);
            return this.DoesQueryStringMatch(queryString);
        }

        private static NameValueCollection GetQueryString(Uri uri)
        {
            if (uri == null)
            {
                throw Fx.Exception.AsError(
                        new InvalidOperationException(SR.NonNullUriRequiredForMediaTypeMapping(queryStringMappingType.Name)));
            }

            return HttpUtility.ParseQueryString(uri.Query);
        }

        private void Initialize(string queryStringParameterName, string queryStringParameterValue)
        {
            if (string.IsNullOrWhiteSpace(queryStringParameterName))
            {
                throw Fx.Exception.ArgumentNull("queryStringParameterName");
            }

            if (string.IsNullOrWhiteSpace(queryStringParameterValue))
            {
                throw Fx.Exception.ArgumentNull("queryStringParameterValue");
            }

            this.QueryStringParameterName = queryStringParameterName.Trim();
            this.QueryStringParameterValue = queryStringParameterValue.Trim();
        }

        private bool DoesQueryStringMatch(NameValueCollection queryString)
        {
            if (queryString != null)
            {
                foreach (string queryParameter in queryString.AllKeys)
                {
                    if (string.Equals(queryParameter, this.QueryStringParameterName, StringComparison.Ordinal))
                    {
                        string queryValue = queryString[queryParameter];
                        if (string.Equals(queryValue, this.QueryStringParameterValue, StringComparison.Ordinal))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}