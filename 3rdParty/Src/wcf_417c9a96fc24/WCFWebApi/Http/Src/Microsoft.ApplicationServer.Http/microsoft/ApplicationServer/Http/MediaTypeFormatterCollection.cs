// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Microsoft.ApplicationServer.Common;

    /// <summary>
    /// Collection class that contains <see cref="MediaTypeFormatter"/> instances.
    /// </summary>
    public class MediaTypeFormatterCollection : Collection<MediaTypeFormatter>
    {
        private static readonly Type mediaTypeFormatterCollectionType = typeof(MediaTypeFormatterCollection);
        private static readonly Type mediaTypeFormatterType = typeof(MediaTypeFormatter);
        private static readonly Type xmlMediaTypeFormatterType = typeof(XmlMediaTypeFormatter);
        private static readonly Type jsonMediaTypeFormatterType = typeof(JsonMediaTypeFormatter);

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaTypeFormatterCollection"/> class.
        /// </summary>
        /// <remarks>
        /// This collection will be initialized to contain default <see cref="MediaTypeFormatter"/>
        /// instances for Xml and Json.
        /// </remarks>
        public MediaTypeFormatterCollection()
        {
            this.Add(new XmlMediaTypeFormatter());
            this.Add(new JsonValueMediaTypeFormatter());
            this.Add(new JsonMediaTypeFormatter());
            this.Add(new FormUrlEncodedMediaTypeFormatter());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaTypeFormatterCollection"/> class with the
        /// given <paramref name="formatters"/>.
        /// </summary>
        /// <param name="formatters">A collection of <see cref="MediaTypeFormatter"/> instances to place in the collection.</param>
        public MediaTypeFormatterCollection(IEnumerable<MediaTypeFormatter> formatters)
        {
            if (formatters == null)
            {
                throw Fx.Exception.ArgumentNull("formatters");
            }

            foreach (MediaTypeFormatter formatter in formatters)
            {
                if (formatter == null)
                {
                    throw Fx.Exception.Argument("formatters", SR.CannotHaveNullInList(mediaTypeFormatterType.Name));
                }

                this.Add(formatter);
            }
        }

        /// <summary>
        /// Gets the <see cref="MediaTypeFormatter"/> to use for Xml.
        /// </summary>
        public XmlMediaTypeFormatter XmlFormatter
        {
            get
            {
                return this.Items.OfType<XmlMediaTypeFormatter>().FirstOrDefault();
            }
        }

        /// <summary>
        /// Gets the <see cref="MediaTypeFormatter"/> to use for Json.
        /// </summary>
        public JsonMediaTypeFormatter JsonFormatter
        {
            get
            {
                return this.Items.OfType<JsonMediaTypeFormatter>().FirstOrDefault();
            }
        }

        internal void ReplaceAllWith(IEnumerable<MediaTypeFormatter> formatters)
        {
            if (formatters == null)
            {
                throw Fx.Exception.ArgumentNull("formatters");
            }

            this.Clear();
            foreach (MediaTypeFormatter formatter in formatters)
            {
                this.Add(formatter);
            }
        }
    }
}
