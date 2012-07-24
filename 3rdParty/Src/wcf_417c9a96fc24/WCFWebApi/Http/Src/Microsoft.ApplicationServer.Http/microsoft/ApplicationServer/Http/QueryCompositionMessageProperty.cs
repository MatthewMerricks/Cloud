// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

using System;

namespace Microsoft.ApplicationServer.Http
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// QueryComposition message property can be added to the message property collection in the incoming reqest
    /// to indicate the request uri, from which the query is going to be retieved.
    /// </summary>
    public sealed class QueryCompositionMessageProperty
    {
        private const string PropertyName = "queryComposition";
        private string requestUri;
        
        /// <summary>
        /// Constructs a QueryCompositionMessageProperty instance with the request Uri
        /// </summary>
        /// <param name="requestUri">The request Uri which will be saved with this message property</param>
        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Justification = "The url represents the resource string used for query composition and not a network location")]
        public QueryCompositionMessageProperty(string requestUri)
        {
            if (String.IsNullOrEmpty(requestUri))
            {
                throw new ArgumentException("RequestUri cannot be null or empty");
            }

            this.requestUri = requestUri;
        }

        private QueryCompositionMessageProperty()
        {
        }

        /// <summary>
        /// Gets the message property name used as a key in the message property collection
        /// </summary>
        public static string Name
        {
            get { return PropertyName; }
        }

        /// <summary>
        /// Gets or sets the request Uri
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "The url represents the resource string used for query composition and not a network location")]
        public string RequestUri
        {
            get
            {
                return this.requestUri;
            }

            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("RequestUri cannot be null or empty");
                }

                this.requestUri = value;
            }
        }
    }
}
