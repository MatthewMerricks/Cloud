// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;

    /// <summary>
    /// Derived <see cref="HttpRequestMessage"/> class that contains a strongly typed object as its content.
    /// </summary>
    /// <typeparam name="T">The type of object it contains as its content.</typeparam>
    public class HttpRequestMessage<T> : HttpRequestMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRequestMessage[T]"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor sets <see cref="HttpRequestMessage.Content"/> to contain an instance of
        /// <see cref="ObjectContent"/> containing the default value for <typeparamref name="T"/>.
        /// </remarks>
        public HttpRequestMessage()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRequestMessage[T]"/> class.
        /// </summary>
        /// <param name="value">The object to use as the content of this new instance.</param>
        public HttpRequestMessage(T value)
            : base()
        {
            this.Content = new ObjectContent<T>(value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRequestMessage[T]"/> class.
        /// </summary>
        /// <param name="value">The object to use as the content of this new instance.</param>
        /// <param name="formatters">The collection of <see cref="MediaTypeFormatter"/> instances
        /// to use for serialization.</param>
        public HttpRequestMessage(T value, IEnumerable<MediaTypeFormatter> formatters)
            : base()
        {
            this.Content = new ObjectContent<T>(value, formatters);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRequestMessage[T]"/> class.
        /// </summary>
        /// <param name="value">The object to use as the content of this new instance.</param>
        /// <param name="method">The <see cref="HttpMethod"/> for this request.</param>
        /// <param name="requestUri">The <see cref="Uri"/> to use for this request.</param>
        /// <param name="formatters">The collection of <see cref="MediaTypeFormatter"/> instances
        /// to use for serialization.</param>
        public HttpRequestMessage(T value, HttpMethod method, Uri requestUri, IEnumerable<MediaTypeFormatter> formatters)
            : base(method, requestUri)
        {
            this.Content = new ObjectContent<T>(value, formatters);
        }

        /// <summary>
        /// Gets or sets the <see cref="ObjectContent"/> that manages the object value for this <see cref="HttpRequestMessage"/>.
        /// </summary>
        public new ObjectContent<T> Content
        {
            get
            {
                HttpContent baseContent = base.Content;
                ObjectContent<T> objectContentOfT = baseContent as ObjectContent<T>;
                if (objectContentOfT == null)
                {
                    if (baseContent != null)
                    {
                        // If a developer attached HttpContent that is not an ObjectContent<T>,
                        // wrap an ObjectContent<T> around it and put back into base.Content
                        objectContentOfT = new ObjectContent<T>(baseContent);
                        base.Content = objectContentOfT;
                    }
                }

                if (objectContentOfT != null)
                {
                    // If the objectContent (new or existing) is not paired with the
                    // current HttpRequestMessage, pair them now.
                    if (!object.ReferenceEquals(objectContentOfT.HttpRequestMessage, this))
                    {
                        objectContentOfT.HttpRequestMessage = this;
                    }
                }

                return objectContentOfT;
            }

            set
            {
                if (value != base.Content)
                {
                    // Break a prior link if it exists
                    ObjectContent baseContent = base.Content as ObjectContent;
                    if (baseContent != null)
                    {
                        baseContent.HttpRequestMessage = null;
                    }

                    base.Content = value;

                    if (value != null)
                    {
                        // Unconditionally pair content and request
                        value.HttpRequestMessage = this;
                    }
                }
            }
        }
    }
}
