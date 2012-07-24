// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.Security;
    using System.Security.Permissions;
    using System.Security.Policy;
    using System.Threading;
    using System.Xml.Serialization;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// MSTest utility for testing code operating against a stream.
    /// </summary>
    public static class StreamAssert
    {
        /// <summary>
        /// Creates a <see cref="Stream"/>, invokes <paramref name="codeThatWrites"/> to write to it,
        /// rewinds the stream to the beginning and invokes <paramref name="codeThatReads"/>.
        /// </summary>
        /// <param name="codeThatWrites">Code to write to the stream.  It cannot be <c>null</c>.</param>
        /// <param name="codeThatReads">Code that reads from the stream.  It cannot be <c>null</c>.</param>
        public static void WriteAndRead(Action<Stream> codeThatWrites, Action<Stream> codeThatReads)
        {
            Assert.IsNotNull(codeThatWrites, "Test error: codeThatWrites cannot be null.");
            Assert.IsNotNull(codeThatReads, "Test error: codeThatReads cannot be null.");
            using (MemoryStream stream = new MemoryStream())
            {
                codeThatWrites(stream);

                stream.Flush();
                stream.Seek(0L, SeekOrigin.Begin);

                codeThatReads(stream);
            }
        }

        /// <summary>
        /// Creates a <see cref="Stream"/>, invokes <paramref name="codeThatWrites"/> to write to it,
        /// rewinds the stream to the beginning and invokes <paramref name="codeThatReads"/> to obtain
        /// the result to return from this method.
        /// </summary>
        /// <param name="codeThatWrites">Code to write to the stream.  It cannot be <c>null</c>.</param>
        /// <param name="codeThatReads">Code that reads from the stream and returns the result.  It cannot be <c>null</c>.</param>
        /// <returns>The value returned from <paramref name="codeThatReads"/>.</returns>
        public static object WriteAndReadResult(Action<Stream> codeThatWrites, Func<Stream, object> codeThatReads)
        {
            Assert.IsNotNull(codeThatWrites, "Test error: codeThatWrites cannot be null.");
            Assert.IsNotNull(codeThatReads, "Test error: codeThatReads cannot be null.");

            object result = null;
            using (MemoryStream stream = new MemoryStream())
            {
                codeThatWrites(stream);

                stream.Flush();
                stream.Seek(0L, SeekOrigin.Begin);

                result = codeThatReads(stream);
            }

            return result;
        }

        /// <summary>
        /// Creates a <see cref="Stream"/>, invokes <paramref name="codeThatWrites"/> to write to it,
        /// rewinds the stream to the beginning and invokes <paramref name="codeThatReads"/> to obtain
        /// the result to return from this method.
        /// </summary>
        /// <typeparam name="T">The type of the result expected.</typeparam>
        /// <param name="codeThatWrites">Code to write to the stream.  It cannot be <c>null</c>.</param>
        /// <param name="codeThatReads">Code that reads from the stream and returns the result.  It cannot be <c>null</c>.</param>
        /// <returns>The value returned from <paramref name="codeThatReads"/>.</returns>
        public static T WriteAndReadResult<T>(Action<Stream> codeThatWrites, Func<Stream, object> codeThatReads)
        {
            return (T)WriteAndReadResult(codeThatWrites, codeThatReads);
        }

        /// <summary>
        /// Creates a <see cref="Stream"/>, serializes <paramref name="objectInstance"/> to it using
        /// <see cref="XmlSerializer"/>, rewinds the stream and calls <see cref="codeThatChecks"/>.
        /// </summary>
        /// <param name="type">The type to serialize.  It cannot be <c>null</c>.</param>
        /// <param name="objectInstance">The value to serialize.</param>
        /// <param name="codeThatChecks">Code to check the contents of the stream.</param>
        public static void UsingXmlSerializer(Type type, object objectInstance, Action<Stream> codeThatChecks)
        {
            Assert.IsNotNull(type, "Test error: type cannot be null.");
            Assert.IsNotNull(codeThatChecks, "Test error: codeThatChecks cannot be null.");

            XmlSerializer serializer = new XmlSerializer(type);

            using (MemoryStream stream = new MemoryStream())
            {
                serializer.Serialize(stream, objectInstance);

                stream.Flush();
                stream.Seek(0L, SeekOrigin.Begin);

                codeThatChecks(stream);
            }
        }

        /// <summary>
        /// Creates a <see cref="Stream"/>, serializes <paramref name="objectInstance"/> to it using
        /// <see cref="XmlSerializer"/>, rewinds the stream and calls <see cref="codeThatChecks"/>.
        /// </summary>
        /// <typeparam name="T">The type to serialize.</typeparam>
        /// <param name="objectInstance">The value to serialize.</param>
        /// <param name="codeThatChecks">Code to check the contents of the stream.</param>
        public static void UsingXmlSerializer<T>(T objectInstance, Action<Stream> codeThatChecks)
        {
            UsingXmlSerializer(typeof(T), objectInstance, codeThatChecks);
        }

        /// <summary>
        /// Creates a <see cref="Stream"/>, serializes <paramref name="objectInstance"/> to it using
        /// <see cref="DataContractSerializer"/>, rewinds the stream and calls <see cref="codeThatChecks"/>.
        /// </summary>
        /// <param name="type">The type to serialize.  It cannot be <c>null</c>.</param>
        /// <param name="objectInstance">The value to serialize.</param>
        /// <param name="codeThatChecks">Code to check the contents of the stream.</param>
        public static void UsingDataContractSerializer(Type type, object objectInstance, Action<Stream> codeThatChecks)
        {
            Assert.IsNotNull(type, "Test error: type cannot be null.");
            Assert.IsNotNull(codeThatChecks, "Test error: codeThatChecks cannot be null.");

            DataContractSerializer serializer = new DataContractSerializer(type);

            using (MemoryStream stream = new MemoryStream())
            {
                serializer.WriteObject(stream, objectInstance);

                stream.Flush();
                stream.Seek(0L, SeekOrigin.Begin);

                codeThatChecks(stream);
            }
        }

        /// <summary>
        /// Creates a <see cref="Stream"/>, serializes <paramref name="objectInstance"/> to it using
        /// <see cref="DataContractSerializer"/>, rewinds the stream and calls <see cref="codeThatChecks"/>.
        /// </summary>
        /// <typeparam name="T">The type to serialize.</typeparam>
        /// <param name="objectInstance">The value to serialize.</param>
        /// <param name="codeThatChecks">Code to check the contents of the stream.</param>
        public static void UsingDataContractSerializer<T>(T objectInstance, Action<Stream> codeThatChecks)
        {
            UsingDataContractSerializer(typeof(T), objectInstance, codeThatChecks);
        }

        /// <summary>
        /// Creates a <see cref="Stream"/>, serializes <paramref name="objectInstance"/> to it using
        /// <see cref="DataContractJsonSerializer"/>, rewinds the stream and calls <see cref="codeThatChecks"/>.
        /// </summary>
        /// <param name="type">The type to serialize.  It cannot be <c>null</c>.</param>
        /// <param name="objectInstance">The value to serialize.</param>
        /// <param name="codeThatChecks">Code to check the contents of the stream.</param>
        public static void UsingDataContractJsonSerializer(Type type, object objectInstance, Action<Stream> codeThatChecks)
        {
            Assert.IsNotNull(type, "Test error: type cannot be null.");
            Assert.IsNotNull(codeThatChecks, "Test error: codeThatChecks cannot be null.");

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(type);

            using (MemoryStream stream = new MemoryStream())
            {
                serializer.WriteObject(stream, objectInstance);

                stream.Flush();
                stream.Seek(0L, SeekOrigin.Begin);

                codeThatChecks(stream);
            }
        }

        /// <summary>
        /// Creates a <see cref="Stream"/>, serializes <paramref name="objectInstance"/> to it using
        /// <see cref="DataContractJsonSerializer"/>, rewinds the stream and calls <see cref="codeThatChecks"/>.
        /// </summary>
        /// <typeparam name="T">The type to serialize.</typeparam>
        /// <param name="objectInstance">The value to serialize.</param>
        /// <param name="codeThatChecks">Code to check the contents of the stream.</param>
        public static void UsingDataContractJsonSerializer<T>(T objectInstance, Action<Stream> codeThatChecks)
        {
            UsingDataContractJsonSerializer(typeof(T), objectInstance, codeThatChecks);
        }
    }
}
