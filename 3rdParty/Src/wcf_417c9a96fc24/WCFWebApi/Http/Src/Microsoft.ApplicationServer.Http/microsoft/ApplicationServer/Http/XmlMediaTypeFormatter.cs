// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

using System.Collections.Generic;
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
    using System.Runtime.Serialization;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;
    using Microsoft.ApplicationServer.Common;

    /// <summary>
    /// <see cref="MediaTypeFormatter"/> class to handle Xml.
    /// </summary>
    public class XmlMediaTypeFormatter : MediaTypeFormatter
    {
        private static readonly XmlWriterSettings defaultXmlWriterSettings = new XmlWriterSettings() { Encoding = new UTF8Encoding(false) };
        private static readonly Type enumerableInterfaceType = typeof(IEnumerable);
        private static readonly Type xmlSerializerType = typeof(XmlSerializer);

        private ConcurrentDictionary<Type, object> serializerCache = new ConcurrentDictionary<Type, object>();
        private XmlSerializerNamespaces xmlSerializerNamespaces;
        
        //private Type listType = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlMediaTypeFormatter"/> class.
        /// </summary>
        public XmlMediaTypeFormatter()
            : base()
        {
            this.SupportedMediaTypes.Add(MediaTypeConstants.ApplicationXmlMediaType);
            this.SupportedMediaTypes.Add(MediaTypeConstants.TextXmlMediaType);

            this.XmlWriterSettings = new XmlWriterSettings() 
            { 
                Encoding = new UTF8Encoding(false), 
                NamespaceHandling = NamespaceHandling.OmitDuplicates,
                Indent = false
            };

            this.XmlReaderSettings = new XmlReaderSettings();         

            this.xmlSerializerNamespaces = new XmlSerializerNamespaces();
            this.xmlSerializerNamespaces.Add(string.Empty, string.Empty);
        }

        /// <summary>
        /// Gets the default media type for xml, namely "application/xml".
        /// </summary>
        /// <value>
        /// Because <see cref="MediaTypeHeaderValue"/> is mutable, the value
        /// returned will be a new instance everytime.
        /// </value>
        public static MediaTypeHeaderValue DefaultMediaType
        {
            get
            {
                return MediaTypeConstants.ApplicationXmlMediaType;
            }
        }

        /// <summary>
        /// Gets the settings to use when serializing a given type.
        /// </summary>
        public XmlWriterSettings XmlWriterSettings { get; private set; }

        /// <summary>
        /// Gets the settings to use when deserializing a given type.
        /// </summary>
        public XmlReaderSettings XmlReaderSettings { get; private set; }

        /// <summary>
        /// Gets or sets the namespaces to use when serializing with an <see cref="XmlSerializer"/> for 
        /// a given type.
        /// </summary>
        public XmlSerializerNamespaces XmlSerializerNamespaces
        {
            get
            {
                return this.xmlSerializerNamespaces;
            }

            set
            {
                this.xmlSerializerNamespaces = value ?? new XmlSerializerNamespaces();
            }
        }

        /// <summary>
        /// Registers the <see cref="XmlObjectSerializer"/> to use to read or write
        /// the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of object that will be serialized or deserialized with <paramref name="serializer"/>.</param>
        /// <param name="serializer">The <see cref="XmlObjectSerializer"/> instance to use.</param>
        public void SetSerializer(Type type, XmlObjectSerializer serializer)
        {
            this.VerifyAndSetSerializer(type, serializer);
        }

        /// <summary>
        /// Registers the <see cref="XmlObjectSerializer"/> to use to read or write
        /// the specified <typeparamref name="T"/> type.
        /// </summary>
        /// <typeparam name="T">The type of object that will be serialized or deserialized with <paramref name="serializer"/>.</typeparam>
        /// <param name="serializer">The <see cref="XmlObjectSerializer"/> instance to use.</param>
        public void SetSerializer<T>(XmlObjectSerializer serializer)
        {
            this.SetSerializer(typeof(T), serializer);
        }

        /// <summary>
        /// Registers the <see cref="XmlSerializer"/> to use to read or write
        /// the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of objects for which <paramref name="serializer"/> will be used.</param>
        /// <param name="serializer">The <see cref="XmlSerializer"/> instance to use.</param>
        /// <param name="isIQueryable">Whether or not the instance is IQueryable</param>
        public void SetSerializer(Type type, XmlSerializer serializer, bool isQueryableType = false)
        {
            
            /*
             * if (isQueryableType)
            {
                this.listType = type;
            }
             */
            this.VerifyAndSetSerializer(type, serializer);
        }

        /// <summary>
        /// Registers the <see cref="XmlSerializer"/> to use to read or write
        /// the specified <typeparamref name="T"/> type.
        /// </summary>
        /// <typeparam name="T">The type of object that will be serialized or deserialized with <paramref name="serializer"/>.</typeparam>
        /// <param name="serializer">The <see cref="XmlSerializer"/> instance to use.</param>
        public void SetSerializer<T>(XmlSerializer serializer)
        {
            this.SetSerializer(typeof(T), serializer);
        }

        /// <summary>
        /// Unregisters the serializer currently associated with the given <paramref name="type"/>.
        /// </summary>
        /// <remarks>
        /// Unless another serializer is registered for the <paramref name="type"/>, a default one will be created.
        /// </remarks>
        /// <param name="type">The type of object whose serializer should be removed.</param>
        /// <returns><c>true</c> if a serializer was registered for the <paramref name="type"/>; otherwise <c>false</c>.</returns>
        public bool RemoveSerializer(Type type)
        {
            if (type == null)
            {
                throw Fx.Exception.ArgumentNull("type");
            }

            object value = null;
            return this.serializerCache.TryRemove(type, out value);
        }

        /// <summary>
        /// Determines whether this <see cref="XmlMediaTypeFormatter"/> can read objects
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
            // Otherwise attempt to create the default serializer.
            object serializer = this.serializerCache.GetOrAdd(
                                    type,
                                    (t) => this.CreateDefaultSerializer(t, throwOnError: false));

            // Null means we tested it before and know it is not supported
            return serializer != null;
        }

        /// <summary>
        /// Determines whether this <see cref="XmlMediaTypeFormatter"/> can write objects
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
            object serializer = this.serializerCache.GetOrAdd(
                                    type,
                                    (t) => this.CreateDefaultSerializer(t, throwOnError: false));

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

            this.XmlReaderSettings.CloseInput = false;
            using (XmlReader reader = XmlReader.Create(stream, this.XmlReaderSettings))
            {
                object serializer = this.GetSerializerForType(type);
                XmlSerializer xmlSerializer = serializer as XmlSerializer;
                if (xmlSerializer != null)
                {
                    return xmlSerializer.Deserialize(reader);
                }

                XmlObjectSerializer xmlObjectSerializer = (XmlObjectSerializer)serializer;
                return xmlObjectSerializer.ReadObject(reader);
            }
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
                var listType = typeof(List<>).MakeGenericType(type.GetGenericArguments());
                value = Activator.CreateInstance(listType, value);
            }

            this.XmlWriterSettings.CloseOutput = false;
            using (XmlWriter writer = XmlWriter.Create(stream, this.XmlWriterSettings))
            {
                object serializer = this.GetSerializerForType(type);
                XmlSerializer xmlSerializer = serializer as XmlSerializer;
                if (xmlSerializer != null)
                {
                    xmlSerializer.Serialize(writer, value, this.XmlSerializerNamespaces);
                }
                else
                {
                    XmlObjectSerializer xmlObjectSerializer = (XmlObjectSerializer)serializer;
                    xmlObjectSerializer.WriteObject(writer, value);
                }

                writer.Flush();
            }
        }

        private object CreateDefaultSerializer(Type type, bool throwOnError)
        {
            Fx.Assert(type != null, "type cannot be null.");
            Exception exception = null;
            XmlSerializer xmlSerializer = null;

            //FIX: GDB - IQueryable
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>))
            {
                type = typeof (List<>).MakeGenericType(type.GetGenericArguments());
            }
            
            try
            {
                xmlSerializer = new XmlSerializer(type);
            }
            catch (InvalidOperationException invalidOperationException)
            {
                exception = invalidOperationException;
            }
            catch (NotSupportedException notSupportedException)
            {
                exception = notSupportedException;
            }

            // XmlSerializer throws one of the exceptions above if it cannot
            // support this type.
            if (exception != null)
            {
                if (throwOnError)
                {
                    throw Fx.Exception.AsError(
                        new InvalidOperationException(
                            SR.SerializerCannotSerializeType(xmlSerializerType.Name, type.Name),
                            exception));
                }
            }

            return xmlSerializer;
        }

        private void VerifyAndSetSerializer(Type type, object serializer)
        {
            if (type == null)
            {
                throw Fx.Exception.ArgumentNull("type");
            }

            if (serializer == null)
            {
                throw Fx.Exception.ArgumentNull("serializer");
            }

            this.SetSerializerInternal(type, serializer);
        }

        private void SetSerializerInternal(Type type, object serializer)
        {
            Fx.Assert(type != null, "type cannot be null.");
            Fx.Assert(serializer != null, "serializer cannot be null.");

            this.serializerCache.AddOrUpdate(type, serializer, (key, value) => value);
        }

        private object GetSerializerForType(Type type)
        {
            Fx.Assert(type != null, "Type cannot be null");
            object serializer = this.serializerCache.GetOrAdd(type, (t) => this.CreateDefaultSerializer(t, throwOnError: true));
            
            if (serializer == null)
            {
                // A null serializer indicates the type has already been tested
                // and found unsupportable.
                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.SerializerCannotSerializeType(xmlSerializerType.Name, type.Name)));
            }

            Fx.Assert(serializer is XmlSerializer || serializer is XmlObjectSerializer, "Only XmlSerializer or XmlObjectSerializer are supported.");
            return serializer;
        }

        
    }
}
