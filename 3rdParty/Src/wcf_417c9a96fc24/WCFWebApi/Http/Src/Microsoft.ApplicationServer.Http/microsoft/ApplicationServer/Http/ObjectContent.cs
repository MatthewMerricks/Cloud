// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Serialization;

    /// <summary>
    /// Derived <see cref="HttpContent"/> class that contains a strongly typed object.
    /// </summary>
    public class ObjectContent : HttpContent
    {
        private const string HeadersContentTypeName = "Headers.ContentType";

        private static readonly Type ObjectContentType = typeof(ObjectContent);
        private static readonly Type HttpContentType = typeof(HttpContent);
        private static readonly Type MediaTypeHeaderValueType = typeof(MediaTypeHeaderValue);
        private static readonly Type MediaTypeFormatterType = typeof(MediaTypeFormatter);

        private MediaTypeFormatterCollection formatters;
        private HttpRequestMessage requestMessage;
        private HttpResponseMessage responseMessage;
        private object defaultValue;
        private MediaTypeFormatter defaultFormatter;
        private MediaTypeFormatter selectedWriteFormatter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectContent"/> class.
        /// </summary>
        /// <param name="type">The type of object this instance will contain.</param>
        /// <param name="value">The value of the object this instance will contain.</param>
        public ObjectContent(Type type, object value)
            : this(type)
        {
            this.VerifyAndSetObject(value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectContent"/> class.
        /// </summary>
        /// <param name="type">The type of object this instance will contain.</param>
        /// <param name="value">The value of the object this instance will contain.</param>
        /// <param name="mediaType">The media type to associate with this object.</param>
        public ObjectContent(Type type, object value, string mediaType)
            : this(type)
        {
            this.VerifyAndSetObjectAndMediaType(value, mediaType);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectContent"/> class.
        /// </summary>
        /// <param name="type">The type of object this instance will contain.</param>
        /// <param name="value">The value of the object this instance will contain.</param>
        /// <param name="mediaType">The media type to associate with this object.</param>
        public ObjectContent(Type type, object value, MediaTypeHeaderValue mediaType)
            : this(type)
        {
            this.VerifyAndSetObjectAndMediaType(value, mediaType);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectContent"/> class.
        /// </summary>
        /// <param name="type">The type of object this instance will contain.</param>
        /// <param name="content">An existing <see cref="HttpContent"/> instance to use for the object's content.</param>
        public ObjectContent(Type type, HttpContent content)
            : this(type)
        {
            this.VerifyAndSetHttpContent(content);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectContent"/> class.
        /// </summary>
        /// <param name="type">The type of object this instance will contain.</param>
        /// <param name="value">The value of the object this instance will contain.</param>
        /// <param name="formatters">The collection of <see cref="MediaTypeFormatter"/> instances
        /// to use for serialization or deserialization.</param>
        public ObjectContent(Type type, object value, IEnumerable<MediaTypeFormatter> formatters)
            : this(type, value)
        {
            this.VerifyAndSetFormatters(formatters);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectContent"/> class.
        /// </summary>
        /// <param name="type">The type of object this instance will contain.</param>
        /// <param name="value">The value of the object this instance will contain.</param>
        /// <param name="mediaType">The media type to associate with this object.</param>
        /// <param name="formatters">The collection of <see cref="MediaTypeFormatter"/> instances
        /// to use for serialization or deserialization.</param>
        public ObjectContent(Type type, object value, string mediaType, IEnumerable<MediaTypeFormatter> formatters)
            : this(type, value, mediaType)
        {
            this.VerifyAndSetFormatters(formatters);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectContent"/> class.
        /// </summary>
        /// <param name="type">The type of object this instance will contain.</param>
        /// <param name="value">The value of the object this instance will contain.</param>
        /// <param name="mediaType">The media type to associate with this object.</param>
        /// <param name="formatters">The collection of <see cref="MediaTypeFormatter"/> instances
        /// to use for serialization or deserialization.</param>
        public ObjectContent(Type type, object value, MediaTypeHeaderValue mediaType, IEnumerable<MediaTypeFormatter> formatters)
            : this(type, value, mediaType)
        {
            this.VerifyAndSetFormatters(formatters);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectContent"/> class.
        /// </summary>
        /// <param name="type">The type of object this instance will contain.</param>
        /// <param name="content">An existing <see cref="HttpContent"/> instance to use for the object's content.</param>
        /// <param name="formatters">The collection of <see cref="MediaTypeFormatter"/> instances
        /// to use for serialization or deserialization.</param>
        public ObjectContent(Type type, HttpContent content, IEnumerable<MediaTypeFormatter> formatters)
            : this(type, content)
        {
            this.VerifyAndSetFormatters(formatters);
        }

        private ObjectContent(Type type)
        {
            if (type == null)
            {
                throw Fx.Exception.ArgumentNull("type");
            }

            if (HttpContentType.IsAssignableFrom(type))
            {
                throw Fx.Exception.AsError(new ArgumentException(SR.CannotUseThisParameterType(HttpContentType.Name, ObjectContentType.Name), "type"));
            }

            this.Type = type;
        }

        /// <summary>
        /// Gets the type of object managed by this <see cref="ObjectContent"/> instance.
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// Gets the mutable collection of <see cref="MediaTypeFormatter"/> instances used to
        /// serialize or deserialize the value of this <see cref="ObjectContent"/>.
        /// </summary>
        public MediaTypeFormatterCollection Formatters
        {
            get
            {
                if (this.formatters == null)
                {
                    this.formatters = new MediaTypeFormatterCollection();
                }

                return this.formatters;
            }
        }

        internal MediaTypeFormatter DefaultFormatter 
        {
            get
            {
                if (this.defaultFormatter == null)
                {
                    this.defaultFormatter = this.Formatters.XmlFormatter;
                    if (this.defaultFormatter == null)
                    {
                        this.defaultFormatter = this.Formatters.JsonFormatter;
                    }
                }

                return this.defaultFormatter;
            }

            set
            {
                this.defaultFormatter = value;
            }
        }

        internal HttpRequestMessage HttpRequestMessage
        {
            get
            {
                return this.requestMessage != null && object.ReferenceEquals(this.requestMessage.Content, this)
                        ? this.requestMessage
                        : null;
            }

            set
            {
                this.requestMessage = value;

                // Pairing to a request unpairs from response
                if (value != null)
                {
                    this.HttpResponseMessage = null;
                }
            }
        }

        internal HttpResponseMessage HttpResponseMessage
        {
            get
            {
                return this.responseMessage != null && object.ReferenceEquals(this.responseMessage.Content, this)
                        ? this.responseMessage
                        : null;
            }

            set
            {
                this.responseMessage = value;

                // pairing to a response unpairs from a request
                if (value != null)
                {
                    this.HttpRequestMessage = null;
                }
            }
        }

        private HttpContent HttpContent { get; set; }

        private object Value { get; set; }

        private object DefaultValue
        {
            get
            {
                if (this.defaultValue == null)
                {
                    this.defaultValue = TypeHelper.GetDefaultValueForType(this.Type);
                }

                return this.defaultValue;
            }
        }

        private bool IsValueCached
        {
            get
            {
                return this.HttpContent == null;
            }
        }

        private MediaTypeHeaderValue MediaType
        {
            get
            {
                return this.Headers.ContentType;
            }

            set
            {
                this.Headers.ContentType = value;
            }
        }

        /// <summary>
        /// Returns the object instance for this <see cref="ObjectContent"/>.
        /// </summary>
        /// <returns>The object instance.</returns>
        public object ReadAs()
        {
            return this.ReadAsInternal(allowDefaultIfNoFormatter: false);
        }

        /// <summary>
        /// Returns the object instance for this <see cref="ObjectContent"/> or the default
        /// value for the type if content is not available.
        /// </summary>
        /// <returns>The object instance or default value.</returns>
        public object ReadAsOrDefault()
        {
            return this.ReadAsInternal(allowDefaultIfNoFormatter: true);
        }

        /// <summary>
        /// Asynchronously returns the object instance for this <see cref="ObjectContent"/>.
        /// </summary>
        /// <returns>A <see cref="Task"/> instance that will yield the object instance.</returns>
        public Task<object> ReadAsAsync()
        {
            return this.ReadAsAsyncInternal(allowDefaultIfNoFormatter: false);
        }

        /// <summary>
        /// Asynchronously returns the object instance for this <see cref="ObjectContent"/>
        /// or the default value for the type if content is not available.
        /// </summary>
        /// <returns>A <see cref="Task"/> instance that will yield the object instance.</returns>
        public Task<object> ReadAsOrDefaultAsync()
        {
            return this.ReadAsAsyncInternal(allowDefaultIfNoFormatter: true);
        }

        /// <summary>
        /// Forces selection of the write <see cref="MediaTypeFormatter"/> and content-type.  Used
        /// within the <see cref="HttpMessageEncodingRequestContext"/> to determine
        /// the content-type since it must be set on the <see cref="HttpResponseMessageProperty"/>
        /// before serizlization is performed in the <see cref="HttpMessageEncoder"/>.
        /// </summary>
        internal void DetermineWriteSerializerAndContentType()
        {
            this.selectedWriteFormatter = this.SelectAndValidateWriteFormatter();
        }

        /// <summary>
        /// Asynchronously serializes the object's content to the given <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to which to write.</param>
        /// <param name="context">The associated <see cref="TransportContext"/>.</param>
        /// <returns>A <see cref="Task"/> instance that is asynchronously serializing the object's content.</returns>
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            return this.WriteToStreamAsyncInternal(stream, context);
        }

        /// <summary>
        /// Serializes the object's content to the given <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to which to write.</param>
        /// <param name="context">The associated <see cref="TransportContext"/>.</param>
        protected override void SerializeToStream(Stream stream, TransportContext context)
        {
            this.WriteToStreamInternal(stream, context);
        }

        /// <summary>
        /// Computes the length of the stream if possible.
        /// </summary>
        /// <param name="length">The computed length of the stream.</param>
        /// <returns><c>true</c> if the length has been computed; otherwise <c>false</c>.</returns>
        protected override bool TryComputeLength(out long length)
        {
            HttpContent httpContent = this.HttpContent;
            if (httpContent != null)
            {
                long? contentLength = httpContent.Headers.ContentLength;
                if (contentLength.HasValue)
                {
                    length = contentLength.Value;
                    return true;
                }
            }

            length = -1;
            return false;
        }

        /// <summary>
        /// Selects the appropriate <see cref="MediaTypeFormatter"/> to read the object content.
        /// </summary>
        /// <returns>The selected <see cref="MediaTypeFormatter"/> or null.</returns>
        protected MediaTypeFormatter SelectReadFormatter()
        {
            HttpRequestMessage request = this.HttpRequestMessage;
            HttpResponseMessage response = this.HttpResponseMessage;
            Type type = this.Type;

            if (request != null)
            {
                foreach (MediaTypeFormatter formatter in this.Formatters)
                {
                    if (formatter.CanReadAs(type, request))
                    {
                        return formatter;
                    }
                }
            }
            else if (response != null)
            {
                foreach (MediaTypeFormatter formatter in this.Formatters)
                {
                    if (formatter.CanReadAs(type, response))
                    {
                        return formatter;
                    }
                }
            }
            else
            {
                foreach (MediaTypeFormatter formatter in this.Formatters)
                {
                    if (formatter.CanReadAs(type, this))
                    {
                        return formatter;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Selects the appropriate <see cref="MediaTypeFormatter"/> to write the object content.
        /// </summary>
        /// <param name="mediaType">The <see cref="MediaTypeHeaderValue"/> to use to describe the object's content type.</param>
        /// <returns>The selected <see cref="MediaTypeFormatter"/> or null.</returns>
        protected MediaTypeFormatter SelectWriteFormatter(out MediaTypeHeaderValue mediaType)
        {
            mediaType = null;

            // We are paired with a request, or a response, or neither.
            HttpRequestMessage request = this.HttpRequestMessage;
            HttpResponseMessage response = this.HttpResponseMessage;
            Type type = this.Type;

            if (request != null)
            {
                foreach (MediaTypeFormatter formatter in this.Formatters)
                {
                    if (formatter.CanWriteAs(type, request, out mediaType))
                    {
                        return formatter;
                    }
                }
            }
            else if (response != null)
            {
                foreach (MediaTypeFormatter formatter in this.Formatters)
                {
                    if (formatter.CanWriteAs(type, response, out mediaType))
                    {
                        return formatter;
                    }
                }
            }
            else
            {
                foreach (MediaTypeFormatter formatter in this.Formatters)
                {
                    if (formatter.CanWriteAs(type, this, out mediaType))
                    {
                        return formatter;
                    }
                }
            }

            mediaType = null;
            return null;
        }

        /// <summary>
        /// Determines if the given <paramref name="value"/> is an instance of
        /// <see cref="HttpContent"/> or is some type we automatically wrap inside
        /// <see cref="HttpContent"/>.
        /// </summary>
        /// <param name="value">The object value to test.</param>
        /// <returns>A non-null <see cref="HttpContent"/> if the <paramref name="value"/>
        /// was an instance of <see cref="HttpContent"/> or needed to be wrapped
        /// inside one.  A <c>null</c> indicates the <paramref name="value"/> is not
        /// <see cref="HttpContent"/> or needed to be wrapped in one.</returns>
        private static HttpContent WrapOrCastAsHttpContent(object value)
        {
            Stream stream = value as Stream;
            return stream == null ? value as HttpContent : new StreamContent(stream);
        }

        private void CacheValueAndDisposeWrappedHttpContent(object value)
        {
            this.Value = value;

            if (this.HttpContent != null)
            {
                this.HttpContent.Dispose();
                this.HttpContent = null;
            }

            Fx.Assert(this.IsValueCached, "IsValueCached must be true.");
        }

        private object ReadAsInternal(bool allowDefaultIfNoFormatter)
        {
            if (this.IsValueCached)
            {
                return this.Value;
            }

            object value;
            MediaTypeFormatter formatter = this.SelectAndValidateReadFormatter(acceptNullFormatter: allowDefaultIfNoFormatter);
            if (formatter == null)
            {
                Fx.Assert(allowDefaultIfNoFormatter, "allowDefaultIfNoFormatter should always be true here.");
                value = this.DefaultValue;
            }
            else
            {
                // Delegate to the wrapped HttpContent for the stream
                HttpContent httpContent = this.HttpContent;
                value = formatter.ReadFromStream(this.Type, httpContent.ContentReadStream, this.Headers);
            }

            this.CacheValueAndDisposeWrappedHttpContent(value);
            return value;
        }

        private Task<object> ReadAsAsyncInternal(bool allowDefaultIfNoFormatter)
        {
            if (this.IsValueCached)
            {
                return Task.Factory.StartNew<object>(() => this.Value);
            }

            MediaTypeFormatter formatter = this.SelectAndValidateReadFormatter(acceptNullFormatter: allowDefaultIfNoFormatter);
            if (formatter == null)
            {
                Fx.Assert(allowDefaultIfNoFormatter, "allowDefaultIfNoFormatter should always be true here.");
                object defaultValue = this.DefaultValue;
                this.CacheValueAndDisposeWrappedHttpContent(defaultValue);
                return Task.Factory.StartNew<object>(() => defaultValue);
            }

            // If we wrap an HttpContent, delegate to its stream..
            HttpContent httpContent = this.HttpContent;
            return formatter.ReadFromStreamAsync(
                this.Type, 
                httpContent.ContentReadStream, 
                this.Headers)
                    .ContinueWith<object>((task) => 
                    {
                        object value = task.Result;
                        this.CacheValueAndDisposeWrappedHttpContent(value); 
                        return value;
                    });
        }

        private MediaTypeFormatter SelectAndValidateReadFormatter(bool acceptNullFormatter)
        {
            MediaTypeFormatter formatter = this.SelectReadFormatter();
            if (formatter == null)
            {
                if (!acceptNullFormatter)
                {
                    MediaTypeHeaderValue mediaType = this.Headers.ContentType;
                    string mediaTypeAsString = mediaType != null ? mediaType.MediaType : SR.UndefinedMediaType;
                    throw Fx.Exception.AsError(new InvalidOperationException(SR.NoReadSerializerAvailable(MediaTypeFormatterType.Name, this.Type.Name, mediaTypeAsString)));
                }
            }

            return formatter;
        }

        private void WriteToStreamInternal(Stream stream, TransportContext context)
        {
            if (this.HttpContent != null)
            {
                this.HttpContent.CopyTo(stream, context);
            }

            MediaTypeFormatter formatter = this.selectedWriteFormatter ?? this.SelectAndValidateWriteFormatter();
            formatter.WriteToStream(this.Type, this.Value, stream, this.Headers, context);
        }

        private Task WriteToStreamAsyncInternal(Stream stream, TransportContext context)
        {
            if (this.HttpContent != null)
            {
                return this.HttpContent.CopyToAsync(stream, context);
            }

            MediaTypeFormatter formatter = this.selectedWriteFormatter ?? this.SelectAndValidateWriteFormatter();
            return formatter.WriteToStreamAsync(this.Type, this.Value, stream, this.Headers, context);
        }

        private MediaTypeFormatter SelectAndValidateWriteFormatter()
        {
            MediaTypeHeaderValue mediaType = null;
            MediaTypeFormatter formatter = this.SelectWriteFormatter(out mediaType);

            if (formatter == null)
            {
                if (this.DefaultFormatter != null &&
                    this.DefaultFormatter.SupportedMediaTypes.Count > 0)
                {
                    formatter = this.DefaultFormatter;
                    mediaType = this.DefaultFormatter.SupportedMediaTypes[0];
                }
                else
                {
                    string errorMessage = this.MediaType == null
                                            ? SR.MediaTypeMustBeSetBeforeWrite(HeadersContentTypeName, ObjectContentType.Name)
                                            : SR.NoWriteSerializerAvailable(MediaTypeFormatterType.Name, this.Type.Name, this.MediaType.ToString());
                    throw Fx.Exception.AsError(new InvalidOperationException(errorMessage));
                }
            }

            // Update our MediaType based on what the formatter said it would produce
            if (mediaType != null)
            {
                this.MediaType = mediaType;
            }

            return formatter;
        }

        private void VerifyAndSetObject(object value)
        {
            Fx.Assert(this.Type != null, "this.Type cannot be null");

            if (value == null)
            {
                // Null may not be assigned to value types (unless Nullable<T>)
                if (!DataContract.IsTypeNullable(this.Type))
                {
                    throw Fx.Exception.AsError(new InvalidOperationException(SR.CannotUseNullValueType(ObjectContentType.Name, this.Type.Name)));
                }
            }
            else
            {
                // It is possible to pass HttpContent as object and arrive at this
                // code path.  Detect and redirect.
                HttpContent objAsHttpContent = WrapOrCastAsHttpContent(value);
                if (objAsHttpContent != null)
                {
                    this.VerifyAndSetHttpContent(objAsHttpContent);
                    return;
                }
                else
                {
                    // Non-null objects must be a type assignable to this.Type
                    Type objectType = value.GetType();
                    if (!this.Type.IsAssignableFrom(objectType))
                    {
                        throw Fx.Exception.AsError(
                            new ArgumentException(
                                SR.ObjectAndTypeDisagree(objectType.Name, this.Type.Name),
                                "value"));
                    }
                }
            }

            this.Value = value;
        }

        private void VerifyAndSetObjectAndMediaType(object value, MediaTypeHeaderValue mediaType)
        {
            Fx.Assert(this.Type != null, "this.Type cannot be null");

            // It is possible to pass HttpContent as object and arrive at this
            // code path.  Detect and redirect.  We do not use the media type
            // specified unless the given HttpContent's media type is null.
            HttpContent objAsHttpContent = WrapOrCastAsHttpContent(value);
            if (objAsHttpContent != null)
            {
                this.VerifyAndSetHttpContent(objAsHttpContent);
                if (objAsHttpContent.Headers.ContentType == null)
                {
                    this.VerifyAndSetMediaType(mediaType);
                }
            }
            else
            {
                this.VerifyAndSetObject(value);
                this.VerifyAndSetMediaType(mediaType);
            }
        }

        private void VerifyAndSetObjectAndMediaType(object value, string mediaType)
        {
            Fx.Assert(this.Type != null, "this.Type cannot be null");

            // It is possible to pass HttpContent as object and arrive at this
            // code path.  Detect and redirect.  We do not use the media type
            // specified unless the given HttpContent's media type is null.
            HttpContent objAsHttpContent = WrapOrCastAsHttpContent(value);

            if (objAsHttpContent != null)
            {
                this.VerifyAndSetHttpContent(objAsHttpContent);
                if (objAsHttpContent.Headers.ContentType == null)
                {
                    this.VerifyAndSetMediaType(mediaType);
                }
            }
            else
            {
                this.VerifyAndSetObject(value);
                this.VerifyAndSetMediaType(mediaType);
            }
        }

        private void VerifyAndSetHttpContent(HttpContent content)
        {
            if (content == null)
            {
                throw Fx.Exception.ArgumentNull("content");
            }

            this.HttpContent = content;
            content.Headers.CopyTo(this.Headers);
        }

        private void VerifyAndSetMediaType(MediaTypeHeaderValue mediaType)
        {
            if (mediaType == null)
            {
                throw Fx.Exception.ArgumentNull("mediaType");
            }

            if (mediaType.IsMediaRange())
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.MediaTypeCanNotBeMediaRange(mediaType.MediaType)));
            }

            this.MediaType = mediaType;
        }

        private void VerifyAndSetMediaType(string mediaType)
        {
            if (string.IsNullOrWhiteSpace(mediaType))
            {
                throw Fx.Exception.ArgumentNull("mediaType");
            }

            MediaTypeHeaderValue mediaTypeHeaderValue = null;
            try
            {
                mediaTypeHeaderValue = new MediaTypeHeaderValue(mediaType);
            }
            catch (FormatException formatException)
            {
                throw Fx.Exception.AsError(new ArgumentException(
                    SR.InvalidMediaType(mediaType, MediaTypeHeaderValueType.Name),
                    "mediaType",
                    formatException));
            }

            this.VerifyAndSetMediaType(mediaTypeHeaderValue);
        }

        private void VerifyAndSetFormatters(IEnumerable<MediaTypeFormatter> formatters)
        {
            if (formatters == null)
            {
                throw Fx.Exception.ArgumentNull("formatters");
            }

            this.formatters = new MediaTypeFormatterCollection(formatters);
        }
    }

    /// <summary>
    /// Generic form of <see cref="ObjectContent"/>.
    /// </summary>
    /// <typeparam name="T">The type of object this <see cref="ObjectContent"/> class will contain.</typeparam>
    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Class contains generic forms")]
    public class ObjectContent<T> : ObjectContent
    {
        private static readonly Type MediaTypeFormatterType = typeof(MediaTypeFormatter);

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectContent[T]"/> class.
        /// </summary>
        /// <param name="value">The value of the object this instance will contain.</param>
        public ObjectContent(T value) 
            : base(typeof(T), value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectContent[T]"/> class.
        /// </summary>
        /// <param name="value">The value of the object this instance will contain.</param>
        /// <param name="mediaType">The media type to associate with this object.</param>
        public ObjectContent(T value, string mediaType)
            : base(typeof(T), value, mediaType)
        {
            this.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectContent[T]"/> class.
        /// </summary>
        /// <param name="value">The value of the object this instance will contain.</param>
        /// <param name="mediaType">The media type to associate with this object.</param>
        public ObjectContent(T value, MediaTypeHeaderValue mediaType)
            : base(typeof(T), value, mediaType)
        {
            this.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectContent[T]"/> class.
        /// </summary>
        /// <param name="content">An existing <see cref="HttpContent"/> instance to use for the object's content.</param>
        public ObjectContent(HttpContent content)
            : base(typeof(T), content)
        {
            this.HttpContent = content;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectContent[T]"/> class.
        /// </summary>
        /// <param name="value">The value of the object this instance will contain.</param>
        /// <param name="formatters">The collection of <see cref="MediaTypeFormatter"/> instances
        /// to serialize or deserialize the object content.</param>
        public ObjectContent(T value, IEnumerable<MediaTypeFormatter> formatters)
            : base(typeof(T), value, formatters)
        {
            this.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectContent[T]"/> class.
        /// </summary>
        /// <param name="value">The value of the object this instance will contain.</param>
        /// <param name="mediaType">The media type to associate with this object.</param>
        /// <param name="formatters">The collection of <see cref="MediaTypeFormatter"/> instances
        /// to serialize or deserialize the object content.</param>
        public ObjectContent(T value, string mediaType, IEnumerable<MediaTypeFormatter> formatters)
            : base(typeof(T), value, mediaType, formatters)
        {
            this.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectContent[T]"/> class.
        /// </summary>
        /// <param name="value">The value of the object this instance will contain.</param>
        /// <param name="mediaType">The media type to associate with this object.</param>
        /// <param name="formatters">The collection of <see cref="MediaTypeFormatter"/> instances
        /// to serialize or deserialize the object content.</param>
        public ObjectContent(T value, MediaTypeHeaderValue mediaType, IEnumerable<MediaTypeFormatter> formatters)
            : base(typeof(T), value, mediaType, formatters)
        {
            this.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectContent[T]"/> class.
        /// </summary>
        /// <param name="content">An existing <see cref="HttpContent"/> instance to use for the object's content.</param>
        /// <param name="formatters">The collection of <see cref="MediaTypeFormatter"/> instances
        /// to serialize or deserialize the object content.</param>
        public ObjectContent(HttpContent content, IEnumerable<MediaTypeFormatter> formatters)
            : base(typeof(T), content, formatters)
        {
            this.HttpContent = content;
        }

        private MediaTypeHeaderValue MediaType
        {
            get
            {
                return this.Headers.ContentType;
            }

            set
            {
                this.Headers.ContentType = value;
            }
        }

        private bool IsValueCached
        {
            get
            {
                return this.HttpContent == null;
            }
        }

        private HttpContent HttpContent { get; set; }

        private object Value { get; set; }

        /// <summary>
        /// Returns the object instance for this <see cref="ObjectContent"/>.
        /// </summary>
        /// <returns>The object instance.</returns>
        public new T ReadAs()
        {
            return this.ReadAsInternal(allowDefaultIfNoFormatter: false);
        }

        /// <summary>
        /// Returns the object instance for this <see cref="ObjectContent"/> or
        /// the default value for the type if content is not available.
        /// </summary>
        /// <returns>The object instance.</returns>
        public new T ReadAsOrDefault()
        {
            return this.ReadAsInternal(allowDefaultIfNoFormatter: true);
        }

        /// <summary>
        /// Returns a <see cref="Task"/> instance to yield the object instance for this <see cref="ObjectContent"/>.
        /// </summary>
        /// <returns>A <see cref="Task"/> that will yield the object instance.</returns>
        public new Task<T> ReadAsAsync()
        {
            return this.ReadAsAsyncInternal(allowDefaultIfNoFormatter: false);
        }

        /// <summary>
        /// Returns a <see cref="Task"/> instance to yield the object instance for this <see cref="ObjectContent"/>
        /// or the default value for the type if content is not available.
        /// </summary>
        /// <returns>A <see cref="Task"/> that will yield the object instance.</returns>
        public new Task<T> ReadAsOrDefaultAsync()
        {
            return this.ReadAsAsyncInternal(allowDefaultIfNoFormatter: true);
        }

        private void CacheValueAndDisposeWrappedHttpContent(T value)
        {
            this.Value = value;

            if (this.HttpContent != null)
            {
                this.HttpContent.Dispose();
                this.HttpContent = null;
            }

            Fx.Assert(this.IsValueCached, "IsValueCached must be true.");
        }

        private T ReadAsInternal(bool allowDefaultIfNoFormatter)
        {
            if (this.IsValueCached)
            {
                return (T)this.Value;
            }

            MediaTypeFormatter formatter = this.SelectAndValidateReadFormatter(acceptNullFormatter: allowDefaultIfNoFormatter);
            if (formatter == null)
            {
                Fx.Assert(allowDefaultIfNoFormatter, "allowDefaultIfNoFormatter must be true to execute this code path.");
                T defaultValue = default(T);
                this.CacheValueAndDisposeWrappedHttpContent(defaultValue);
                return defaultValue;
            }

            // If we wrap an HttpContent, delegate to its stream
            HttpContent httpContent = this.HttpContent;
            Fx.Assert(httpContent != null, "HttpContent must be non-null when not initialized from object instance.");

            T value = (T)formatter.ReadFromStream(this.Type, httpContent.ContentReadStream, this.Headers);
            this.CacheValueAndDisposeWrappedHttpContent(value);
            return value;
        }

        private Task<T> ReadAsAsyncInternal(bool allowDefaultIfNoFormatter)
        {
            if (this.IsValueCached)
            {
                return Task.Factory.StartNew<T>(() => (T)this.Value);
            }

            MediaTypeFormatter formatter = this.SelectAndValidateReadFormatter(acceptNullFormatter: allowDefaultIfNoFormatter);
            if (formatter == null)
            {
                Fx.Assert(allowDefaultIfNoFormatter, "allowDefaultIfNoFormatter must be true to execute this code path.");
                T defaultValue = default(T);
                this.CacheValueAndDisposeWrappedHttpContent(defaultValue);
                return Task.Factory.StartNew<T>(() => defaultValue);
            }

            // If we wrap an HttpContent, delegate to its stream.
            HttpContent httpContent = this.HttpContent;
            return formatter.ReadFromStreamAsync(
                    this.Type,
                    httpContent.ContentReadStream,
                    this.Headers)
                        .ContinueWith<T>((task) =>
                            {
                                T value = (T)task.Result;
                                this.CacheValueAndDisposeWrappedHttpContent(value);
                                return value;
                            });
        }

        private MediaTypeFormatter SelectAndValidateReadFormatter(bool acceptNullFormatter)
        {
            MediaTypeFormatter formatter = this.SelectReadFormatter();

            if (formatter == null)
            {
                if (!acceptNullFormatter)
                {
                    MediaTypeHeaderValue mediaType = this.Headers.ContentType;
                    string mediaTypeAsString = mediaType != null ? mediaType.MediaType : SR.UndefinedMediaType;
                    throw Fx.Exception.AsError(new InvalidOperationException(SR.NoReadSerializerAvailable(MediaTypeFormatterType.Name, this.Type.Name, mediaTypeAsString)));
                }
            }

            return formatter;
        }
    }
}
