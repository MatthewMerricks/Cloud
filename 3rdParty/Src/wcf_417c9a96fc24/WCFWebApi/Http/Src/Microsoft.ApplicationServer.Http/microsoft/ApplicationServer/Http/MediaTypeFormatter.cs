// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.ApplicationServer.Common;

    /// <summary>
    /// Base class to handle serializing and deserializing strongly-typed objects using <see cref="ObjectContent"/>.
    /// </summary>
    public abstract class MediaTypeFormatter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaTypeFormatter"/> class.
        /// </summary>
        protected MediaTypeFormatter()
        {
            this.SupportedMediaTypes = new MediaTypeHeaderValueCollection();
            this.MediaTypeMappings = new Collection<MediaTypeMapping>();
        }

        /// <summary>
        /// Gets the mutable collection of <see cref="MediaTypeHeaderValue"/> elements supported by
        /// this <see cref="MediaTypeFormatter"/> instance.
        /// </summary>
        public Collection<MediaTypeHeaderValue> SupportedMediaTypes { get; private set; }

        /// <summary>
        /// Gets the mutable collection of <see cref="MediaTypeMapping"/> elements used
        /// by this <see cref="MediaTypeFormatter"/> instance to determine the 
        /// <see cref="MediaTypeHeaderValue"/> of requests or responses.
        /// </summary>
        public Collection<MediaTypeMapping> MediaTypeMappings { get; private set; }

        internal bool CanReadAs(Type type, HttpContent content)
        {
            if (type == null)
            {
                throw Fx.Exception.ArgumentNull("type");
            }

            if (content == null)
            {
                throw Fx.Exception.ArgumentNull("content");
            }

            if (!this.CanReadType(type))
            {
                return false;
            }

            // Content type must be set and must be supported
            MediaTypeHeaderValue mediaType = content.Headers.ContentType;
            return mediaType == null ? false : this.TryMatchSupportedMediaType(mediaType, out mediaType);
        }

        internal bool CanWriteAs(Type type, HttpContent content, out MediaTypeHeaderValue mediaType)
        {
            if (type == null)
            {
                throw Fx.Exception.ArgumentNull("type");
            }

            if (content == null)
            {
                throw Fx.Exception.ArgumentNull("content");
            }

            if (!this.CanWriteType(type))
            {
                mediaType = null;
                return false;
            }

            // Content type must be set and must be supported
            mediaType = content.Headers.ContentType;
            return mediaType != null && this.TryMatchSupportedMediaType(mediaType, out mediaType);
        }

        internal bool CanReadAs(Type type, HttpRequestMessage request)
        {
            if (type == null)
            {
                throw Fx.Exception.ArgumentNull("type");
            }

            if (request == null)
            {
                throw Fx.Exception.ArgumentNull("request");
            }

            if (!this.CanReadType(type))
            {
                return false;
            }

            // Content type must be set and must be supported
            MediaTypeHeaderValue mediaType = request.Content.Headers.ContentType;
            return mediaType != null && this.TryMatchSupportedMediaType(mediaType, out mediaType);
        }

        internal bool CanWriteAs(Type type, HttpRequestMessage request, out MediaTypeHeaderValue mediaType)
        {
            if (type == null)
            {
                throw Fx.Exception.ArgumentNull("type");
            }

            if (request == null)
            {
                throw Fx.Exception.ArgumentNull("request");
            }

            mediaType = null;

            if (!this.CanWriteType(type))
            {
                return false;
            }

            mediaType = request.Content.Headers.ContentType;
            if (mediaType != null)
            {
                if (this.TryMatchSupportedMediaType(mediaType, out mediaType))
                {
                    return true;
                }
            }
            else
            {
                if (this.TryMatchMediaTypeMapping(request, out mediaType))
                {
                    return true;
                }
            }

            mediaType = null;
            return false;
        }

        internal bool CanReadAs(Type type, HttpResponseMessage response)
        {
            if (type == null)
            {
                throw Fx.Exception.ArgumentNull("type");
            }

            if (response == null)
            {
                throw Fx.Exception.ArgumentNull("response");
            }

            if (!this.CanReadType(type))
            {
                return false;
            }

            // Content type must be set and must be supported
            MediaTypeHeaderValue mediaType = response.Content.Headers.ContentType;
            return mediaType != null && this.TryMatchSupportedMediaType(mediaType, out mediaType);
        }

        internal bool CanWriteAs(Type type, HttpResponseMessage response, out MediaTypeHeaderValue mediaType)
        {
            if (type == null)
            {
                throw Fx.Exception.ArgumentNull("type");
            }

            if (response == null)
            {
                throw Fx.Exception.ArgumentNull("response");
            }

            mediaType = null;

            if (!this.CanWriteType(type))
            {
                return false;
            }

            mediaType = response.Content.Headers.ContentType;
            if (mediaType != null && this.TryMatchSupportedMediaType(mediaType, out mediaType))
            {
                return true;
            }

            HttpRequestMessage request = response.RequestMessage;
            if (request != null)
            {
                IEnumerable<MediaTypeWithQualityHeaderValue> acceptHeaderMediaTypes = request.Headers.Accept.OrderBy((m) => m, MediaTypeHeaderValueComparer.Comparer);

                if (this.TryMatchSupportedMediaType(acceptHeaderMediaTypes, out mediaType))
                {
                    return true;
                }
                    
                if (this.TryMatchMediaTypeMapping(response, out mediaType))
                {
                    return true;
                }
                    
                HttpContent requestContent = request.Content;
                if (requestContent != null)
                {
                    MediaTypeHeaderValue requestContentType = requestContent.Headers.ContentType;
                    if (requestContentType != null && this.TryMatchSupportedMediaType(requestContentType, out mediaType))
                    {
                        return true;
                    }
                }
            }

            mediaType = null;
            return false;
        }

        public Task<object> ReadFromStreamAsync(Type type, Stream stream, HttpContentHeaders contentHeaders)
        {
            Fx.Assert(type != null, "type cannot be null.");
            Fx.Assert(stream != null, "stream cannot be null.");
            Fx.Assert(contentHeaders != null, "contentHeaders cannot be null.");

            return this.OnReadFromStreamAsync(type, stream, contentHeaders);
        }

        public Task WriteToStreamAsync(Type type, object instance, Stream stream, HttpContentHeaders contentHeaders, TransportContext context)
        {
            Fx.Assert(type != null, "type cannot be null.");
            Fx.Assert(stream != null, "stream cannot be null.");
            Fx.Assert(contentHeaders != null, "contentHeaders cannot be null.");

            return this.OnWriteToStreamAsync(type, instance, stream, contentHeaders, context);
        }

        public object ReadFromStream(Type type, Stream stream, HttpContentHeaders contentHeaders)
        {
            Fx.Assert(type != null, "type cannot be null.");
            Fx.Assert(stream != null, "stream cannot be null.");
            Fx.Assert(contentHeaders != null, "contentHeaders cannot be null.");

            return this.OnReadFromStream(type, stream, contentHeaders);
        }

        public void WriteToStream(Type type, object instance, Stream stream, HttpContentHeaders contentHeaders, TransportContext context)
        {
            Fx.Assert(type != null, "type cannot be null.");
            Fx.Assert(stream != null, "stream cannot be null.");
            Fx.Assert(contentHeaders != null, "contentHeaders cannot be null.");

            this.OnWriteToStream(type, instance, stream, contentHeaders, context);
        }

        /// <summary>
        /// Determines whether this <see cref="MediaTypeFormatter"/> can deserialize
        /// an object of the specified type.
        /// </summary>
        /// <remarks>
        /// The base class unconditionally returns <c>true</c>.  Derived classes must
        /// override this to exclude types they cannot deserialize.
        /// </remarks>
        /// <param name="type">The type of object that will be deserialized.</param>
        /// <returns><c>true</c> if this <see cref="MediaTypeFormatter"/> can deserialize an object of that type; otherwise <c>false</c>.</returns>
        public bool CanReadType(Type type)
        {
            if (type == null)
            {
                throw Fx.Exception.ArgumentNull("type");
            }

            return this.OnCanReadType(type);
        }

        protected virtual bool OnCanReadType(Type type)
        {
            return true;
        }

        /// <summary>
        /// Determines whether this <see cref="MediaTypeFormatter"/> can serialize
        /// an object of the specified type.
        /// </summary>
        /// <remarks>
        /// The base class unconditionally returns <c>true</c>.  Derived classes must
        /// override this to exclude types they cannot serialize.
        /// </remarks>
        /// <param name="type">The type of object that will be serialized.</param>
        /// <returns><c>true</c> if this <see cref="MediaTypeFormatter"/> can serialize an object of that type; otherwise <c>false</c>.</returns>
        public bool CanWriteType(Type type)
        {
            if (type == null)
            {
                throw Fx.Exception.ArgumentNull("type");
            }

            return this.OnCanWriteType(type);
        }

        protected virtual bool OnCanWriteType(Type type)
        {
            return true;
        }



        /// <summary>
        /// Called to read an object from the <paramref name="stream"/> asynchronously.
        /// Derived classes may override this to do custom deserialization.
        /// </summary>
        /// <param name="type">The type of the object to read.</param>
        /// <param name="stream">The <see cref="Stream"/> from which to read.</param>
        /// <param name="contentHeaders">The content headers from the respective request or response.</param>
        /// <returns>A <see cref="Task"/> that will yield an object instance when it completes.</returns>
        public virtual Task<object> OnReadFromStreamAsync(Type type, Stream stream, HttpContentHeaders contentHeaders)
        {
            // Base implementation provides only a Task wrapper over the synchronous operation.
            // More intelligent derived formatters should override.
            return Task.Factory.StartNew<object>(() => this.OnReadFromStream(type, stream, contentHeaders));
        }

        /// <summary>
        /// Called to write an object to the <paramref name="stream"/> asynchronously.
        /// </summary>
        /// <param name="type">The type of object to write.</param>
        /// <param name="value">The object instance to write.</param>
        /// <param name="stream">The <see cref="Stream"/> to which to write.</param>
        /// <param name="contentHeaders">The content headers from the respective request or response.</param>
        /// <param name="context">The <see cref="TransportContext"/>.</param>
        /// <returns>A <see cref="Task"/> that will write the object to the stream asynchronously.</returns>
        public virtual Task OnWriteToStreamAsync(Type type, object value, Stream stream, HttpContentHeaders contentHeaders, TransportContext context)
        {
            // Base implementation provides only a Task wrapper over the synchronous operation.
            // More intelligent derived formatters should override.
            return Task.Factory.StartNew(() => this.OnWriteToStream(type, value, stream, contentHeaders, context));
        }

        /// <summary>
        /// Called to read an object from the <paramref name="stream"/>.
        /// Derived classes may override this to do custom deserialization.
        /// </summary>
        /// <param name="type">The type of the object to read.</param>
        /// <param name="stream">The <see cref="Stream"/> from which to read.</param>
        /// <param name="contentHeaders">The content headers from the respective request or response.</param>
        /// <returns>The object instance read from the <paramref name="stream"/>.</returns>
        public abstract object OnReadFromStream(Type type, Stream stream, HttpContentHeaders contentHeaders);

        /// <summary>
        /// Called to write an object to the <paramref name="stream"/>.
        /// </summary>
        /// <param name="type">The type of object to write.</param>
        /// <param name="value">The object instance to write.</param>
        /// <param name="stream">The <see cref="Stream"/> to which to write.</param>
        /// <param name="contentHeaders">The content headers from the respective request or response.</param>
        /// <param name="context">The <see cref="TransportContext"/>.</param>
        public abstract void OnWriteToStream(Type type, object value, Stream stream, HttpContentHeaders contentHeaders, TransportContext context);

        private bool TryMatchSupportedMediaType(MediaTypeHeaderValue mediaType, out MediaTypeHeaderValue matchedMediaType)
        {
            Fx.Assert(mediaType != null, "mediaType cannot be null.");

            foreach (MediaTypeHeaderValue supportedMediaType in this.SupportedMediaTypes)
            {
                if (MediaTypeHeaderValueEqualityComparer.EqualityComparer.Equals(supportedMediaType, mediaType, MediaTypeConstants.DefaultCharSet))
                {
                    matchedMediaType = string.IsNullOrWhiteSpace(mediaType.CharSet) ? supportedMediaType : mediaType;
                    return true;
                }
            }

            matchedMediaType = null;
            return false;
        }

        private bool TryMatchSupportedMediaType(IEnumerable<MediaTypeHeaderValue> mediaTypes, out MediaTypeHeaderValue matchedMediaType)
        {
            Fx.Assert(mediaTypes != null, "mediaTypes cannot be null.");
            foreach (MediaTypeHeaderValue mediaType in mediaTypes)
            {
                if (this.TryMatchSupportedMediaType(mediaType, out matchedMediaType))
                {
                    return true;
                }
            }

            matchedMediaType = null;
            return false;
        }

        private bool TryMatchMediaTypeMapping(HttpRequestMessage request, out MediaTypeHeaderValue mediaType)
        {
            Fx.Assert(request != null, "request cannot be null.");

            foreach (MediaTypeMapping mapping in this.MediaTypeMappings)
            {
                // Collection<T> is not protected against null, so avoid them
                if (mapping != null && mapping.SupportsMediaType(request))
                {
                    mediaType = mapping.MediaType;
                    return true;
                }
            }

            mediaType = null;
            return false;
        }

        private bool TryMatchMediaTypeMapping(HttpResponseMessage response, out MediaTypeHeaderValue mediaType)
        {
            Fx.Assert(response != null, "response cannot be null.");

            foreach (MediaTypeMapping mapping in this.MediaTypeMappings)
            {
                // Collection<T> is not protected against null, so avoid them
                if (mapping != null && mapping.SupportsMediaType(response))
                {
                    mediaType = mapping.MediaType;
                    return true;
                }
            }

            mediaType = null;
            return false;
        }

        /// <summary>
        /// Collection class that validates it contains only <see cref="MediaTypeHeaderValue"/> instances
        /// that are not null and not media ranges.
        /// </summary>
        internal class MediaTypeHeaderValueCollection : Collection<MediaTypeHeaderValue>
        {
            private static readonly Type mediaTypeHeaderValueType = typeof(MediaTypeHeaderValue);

            /// <summary>
            /// Inserts the <paramref name="item"/> into the collection at the specified <paramref name="index"/>.
            /// </summary>
            /// <param name="index">The zero-based index at which item should be inserted.</param>
            /// <param name="item">The object to insert. It cannot be <c>null</c>.</param>
            protected override void InsertItem(int index, MediaTypeHeaderValue item)
            {
                ValidateMediaType(item);
                base.InsertItem(index, item);
            }

            /// <summary>
            /// Replaces the element at the specified <paramref name="index"/>.
            /// </summary>
            /// <param name="index">The zero-based index of the item that should be replaced.</param>
            /// <param name="item">The new value for the element at the specified index.  It cannot be <c>null</c>.</param>
            protected override void SetItem(int index, MediaTypeHeaderValue item)
            {
                ValidateMediaType(item);
                base.SetItem(index, item);
            }

            private static void ValidateMediaType(MediaTypeHeaderValue item)
            {
                if (item == null)
                {
                    throw Fx.Exception.ArgumentNull("item");
                }

                ParsedMediaTypeHeaderValue parsedMediaType = new ParsedMediaTypeHeaderValue(item);
                if (parsedMediaType.IsAllMediaRange || parsedMediaType.IsSubTypeMediaRange)
                {
                    throw Fx.Exception.AsError(
                        new ArgumentException(
                            SR.CannotUseMediaRangeForSupportedMediaType(mediaTypeHeaderValueType.Name, item.MediaType),
                            "item"));
                }
            }
        }
    }
}
