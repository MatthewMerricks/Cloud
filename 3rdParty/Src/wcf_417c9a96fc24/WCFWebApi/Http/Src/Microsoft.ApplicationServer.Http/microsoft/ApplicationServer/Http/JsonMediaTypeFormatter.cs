// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

using System.Linq;

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Json;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Runtime.Serialization.Json;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// <see cref="MediaTypeFormatter"/> class to handle Json.
    /// </summary>
    public class JsonMediaTypeFormatter : MediaTypeFormatter
    {
        private static readonly Type dataContractJsonSerializerType = typeof(DataContractJsonSerializer);

        private ConcurrentDictionary<Type, DataContractJsonSerializer> serializerCache = new ConcurrentDictionary<Type, DataContractJsonSerializer>();

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonMediaTypeFormatter"/> class.
        /// </summary>
        public JsonMediaTypeFormatter()
            : base()
        {
            this.SupportedMediaTypes.Add(MediaTypeConstants.ApplicationJsonMediaType);
            this.SupportedMediaTypes.Add(MediaTypeConstants.TextJsonMediaType);
        }

        /// <summary>
        /// Gets the default media type for json, namely "application/json".
        /// </summary>
        /// <value>
        /// Because <see cref="MediaTypeHeaderValue"/> is mutable, the value
        /// returned will be a new instance everytime.
        /// </value>
        public static MediaTypeHeaderValue DefaultMediaType
        {
            get
            {
                return MediaTypeConstants.ApplicationJsonMediaType;
            }
        }

        /// <summary>
        /// Registers the <see cref="DataContractJsonSerializer"/> to use to read or write
        /// the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of object that will be serialized or deserialized with the <paramref name="serializer"/>.</param>
        /// <param name="serializer">The <see cref="DataContractJsonSerializer"/> instance to use.</param>
        public void SetSerializer(Type type, DataContractJsonSerializer serializer)
        {
            if (type == null)
            {
                throw Fx.Exception.ArgumentNull("type");
            }

            if (serializer == null)
            {
                throw Fx.Exception.ArgumentNull("serializer");
            }

            this.serializerCache.AddOrUpdate(type, serializer, (key, value) => value);
        }

        /// <summary>
        /// Registers the <see cref="DataContractJsonSerializer"/> to use to read or write
        /// the specified <typeparamref name="T"/> type.
        /// </summary>
        /// <typeparam name="T">The type of object that will be serialized or deserialized with the <paramref name="serializer"/>.</typeparam>
        /// <param name="serializer">The <see cref="DataContractJsonSerializer"/> instance to use.</param>
        public void SetSerializer<T>(DataContractJsonSerializer serializer)
        {
            this.SetSerializer(typeof(T), serializer);
        }

        /// <summary>
        /// Unregisters the serializer currently associated with the given <paramref name="type"/>.
        /// </summary>
        /// <remarks>
        /// Unless another serializer is registered for the <paramref name="type"/>, a default one will be created
        /// the next time an instance of the type needs to be serialized or deserialized.
        /// </remarks>
        /// <param name="type">The type of object whose serializer should be removed.</param>
        /// <returns><c>true</c> if a serializer was registered for the <paramref name="type"/>; otherwise <c>false</c>.</returns>
        public bool RemoveSerializer(Type type)
        {
            if (type == null)
            {
                throw Fx.Exception.ArgumentNull("type");
            }

            DataContractJsonSerializer value = null;
            return this.serializerCache.TryRemove(type, out value);
        }

        /// <summary>
        /// Determines whether this <see cref="JsonMediaTypeFormatter"/> can read objects
        /// of the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of object that will be read.</param>
        /// <returns><c>true</c> if objects of this <paramref name="type"/> can be read, otherwise <c>false</c>.</returns>
        protected override bool OnCanReadType(Type type)
        {
            if (type == null)
            {
                throw Fx.Exception.ArgumentNull("type");
            }

            if (type == typeof(JsonValue))
            {
                return false;
            }

            // If there is a registered non-null serializer, we can support this type.
            DataContractJsonSerializer serializer = this.serializerCache.GetOrAdd(
                                                        type, 
                                                        (t) => this.CreateDefaultSerializer(t));

            // Null means we tested it before and know it is not supported
            return serializer != null;
        }

        /// <summary>
        /// Determines whether this <see cref="JsonMediaTypeFormatter"/> can write objects
        /// of the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of object that will be written.</param>
        /// <returns><c>true</c> if objects of this <paramref name="type"/> can be written, otherwise <c>false</c>.</returns>
        protected override bool OnCanWriteType(Type type)
        {
            if (type == null)
            {
                throw Fx.Exception.ArgumentNull("type");
            }

            if (type == typeof(JsonValue))
            {
                return false;
            }

            // If there is a registered non-null serializer, we can support this type.
            DataContractJsonSerializer serializer = this.serializerCache.GetOrAdd(
                                                        type,
                                                        (t) => this.CreateDefaultSerializer(t));

            // Null means we tested it before and know it is not supported
            return serializer != null;
        }

        /// <summary>
        /// Called during deserialization to read an object of the specified <paramref name="type"/>
        /// from the specified <paramref name="stream"/>.
        /// </summary>
        /// <param name="type">The type of object to read.</param>
        /// <param name="stream">The <see cref="Stream"/> from which to read.</param>
        /// <param name="contentHeaders">The content headers associated with the request or response.</param>
        /// <returns>The object instance that has been read.</returns>
        public override object OnReadFromStream(Type type, Stream stream, HttpContentHeaders contentHeaders)
        {
            if (type == null)
            {
                throw Fx.Exception.ArgumentNull("type");
            }

            if (stream == null)
            {
                throw Fx.Exception.ArgumentNull("stream");
            }

            DataContractJsonSerializer serializer = this.GetSerializerForType(type);
            return serializer.ReadObject(stream);
        }

        /// <summary>
        /// Called during serialization to write an object of the specified <paramref name="type"/>
        /// to the specified <paramref name="stream"/>.
        /// </summary>
        /// <param name="type">The type of object to write.</param>
        /// <param name="value">The object to write.</param>
        /// <param name="stream">The <see cref="Stream"/> to which to write.</param>
        /// <param name="contentHeaders">The content headers associated with the request or response.</param>
        /// <param name="context">The <see cref="TransportContext"/>.</param>
        public override void OnWriteToStream(Type type, object value, Stream stream, HttpContentHeaders contentHeaders, TransportContext context)
        {
            if (type == null)
            {
                throw Fx.Exception.ArgumentNull("type");
            }

            if (stream == null)
            {
                throw Fx.Exception.ArgumentNull("stream");
            }

            //FIX: GB - IQueryable
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>))
            {
                type = typeof (List<>).MakeGenericType(type.GetGenericArguments());
                value = Activator.CreateInstance(type, value);
            }

            DataContractJsonSerializer serializer = this.GetSerializerForType(type);

            serializer.WriteObject(stream, value);
        }

        private static bool IsKnownUnserializableType(Type type)
        {
            if (type.IsGenericType)
            {
                if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    if (type.GetMethod("Add") == null)
                    {
                        return true;
                    }

                    return IsKnownUnserializableType(type.GetGenericArguments()[0]);
                }
            }

            if (!type.IsVisible)
            {
                return true;
            }

            if (type.HasElementType && IsKnownUnserializableType(type.GetElementType()))
            {
                return true;
            }

            return false;
        }

        private DataContractJsonSerializer GetSerializerForType(Type type)
        {
            Fx.Assert(type != null, "Type cannot be null");

            DataContractJsonSerializer serializer = 
                this.serializerCache.GetOrAdd(type, (t) => this.CreateDefaultSerializer(type));

            if (serializer == null)
            {
                // A null serializer means the type cannot be serialized
                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.SerializerCannotSerializeType(dataContractJsonSerializerType.Name, type.Name)));
            }

            return serializer;
        }

        private DataContractJsonSerializer CreateDefaultSerializer(Type type)
        {
            Fx.Assert(type != null, "type cannot be null.");
            DataContractJsonSerializer serializer = null;

            try
            {
                //// TODO: CSDMAIN 211321 -- determine the correct algorithm to know what is serializable.

                DataContract.GetDataContract(type);
                serializer = IsKnownUnserializableType(type) ? null : new DataContractJsonSerializer(type);
            }
            catch (Exception ex)
            {
                if (Fx.IsFatal(ex))
                {
                    throw;
                }
            }

            return serializer;
        }
    }
}
