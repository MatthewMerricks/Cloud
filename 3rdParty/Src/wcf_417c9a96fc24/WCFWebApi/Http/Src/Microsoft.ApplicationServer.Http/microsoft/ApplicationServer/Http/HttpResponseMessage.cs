// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;

    /// <summary>
    /// Derived <see cref="HttpResponseMessage"/> class that contains a strongly typed object as its content.
    /// </summary>
    /// <typeparam name="T">The type of object instances of this class will contain as its content.</typeparam>
    public class HttpResponseMessage<T> : HttpResponseMessage
    {
        //// TODO: CSDMAIN 210906: Set the ReasonPhrase in the ctors after formal HttpClient port

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponseMessage[T]"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor sets <see cref="Content"/> to contain an instance of
        /// <see cref="ObjectContent"/> containing the default value for <typeparamref name="T"/>.
        /// </remarks>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> to use for this response.</param>
        public HttpResponseMessage(HttpStatusCode statusCode)
            : base(statusCode, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponseMessage[T]"/> class.
        /// </summary>
        /// <param name="value">The value to use as the content of this new instance.</param>
        public HttpResponseMessage(T value)
            : base()
        {
            this.Content = new ObjectContent<T>(value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponseMessage[T]"/> class.
        /// </summary>
        /// <param name="value">The value to use as the content of this new instance.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> to use for this response.</param>
        public HttpResponseMessage(T value, HttpStatusCode statusCode)
            : base(statusCode, null)
        {
            this.Content = new ObjectContent<T>(value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponseMessage[T]"/> class.
        /// </summary>
        /// <param name="value">The value to use as the content of this new instance.</param>
        /// <param name="formatters">The collection of <see cref="MediaTypeFormatter"/> instances 
        /// to use for serialization.</param>
        public HttpResponseMessage(T value, IEnumerable<MediaTypeFormatter> formatters)
        {
            this.Content = new ObjectContent<T>(value, formatters);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponseMessage[T]"/> class.
        /// </summary>
        /// <param name="value">The value to use as the content of this new instance.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> to use for this response.</param>
        /// <param name="formatters">The collection of <see cref="MediaTypeFormatter"/> instances 
        /// to use for serialization.</param>
        public HttpResponseMessage(T value, HttpStatusCode statusCode, IEnumerable<MediaTypeFormatter> formatters)
            : base(statusCode, null)
        {
            this.Content = new ObjectContent<T>(value, formatters);
        }

        /// <summary>
        /// Gets or sets the <see cref="ObjectContent"/> that manages the value for this instance.
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
                    // current HttpResponseMessage, pair them now.
                    if (!object.ReferenceEquals(objectContentOfT.HttpRequestMessage, this))
                    {
                        objectContentOfT.HttpResponseMessage = this;
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
                        baseContent.HttpResponseMessage = null;
                    }

                    base.Content = value;

                    if (value != null)
                    {
                        // Unconditionally pair content and request
                        value.HttpResponseMessage = this;
                    }
                }
            }
        }
    }
}
