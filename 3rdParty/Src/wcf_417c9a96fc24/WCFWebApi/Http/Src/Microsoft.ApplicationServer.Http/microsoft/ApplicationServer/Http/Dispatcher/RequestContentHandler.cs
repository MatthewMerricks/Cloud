// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Http.Description;

    /// <summary>
    /// A <see cref="HttpOperationHandler"/> that takes in an <see cref="HttpRequestMessage"/>
    /// and returns an <see cref="HttpRequestMessage{}"/> instance with the same type as the content
    /// <see cref="HttpParameter"/> passed into the <see cref="RequestContentHandler"/>
    /// constructor.
    /// </summary>
    public class RequestContentHandler : HttpOperationHandler
    {
        private static readonly HttpRequestMessageConverter simpleHttpRequestMessageConverter = new SimpleHttpRequestMessageConverter();
        private static readonly Type httpRequestMessageConverterGenericType = typeof(HttpRequestMessageConverter<>);
        private static Type requestContentHandlerType = typeof(RequestContentHandler);

        private HttpParameter outputParameter;
        private HttpRequestMessageConverter requestMessageConverter;

        /// <summary>
        /// Initializes a new instance of a <see cref="RequestContentHandler"/> with the
        /// given <paramref name="requestContentParameter"/> and <paramref name="formatters"/>.
        /// </summary>
        /// <param name="requestContentParameter">The <see cref="HttpParameter"/> for the content of the request.</param>
        /// <param name="formatters">The collection of <see cref="MediaTypeFormatter"/> instances to use for deserializing the request content.</param>
        public RequestContentHandler(HttpParameter requestContentParameter, IEnumerable<MediaTypeFormatter> formatters)
        {
            if (requestContentParameter == null)
            {
                throw Fx.Exception.ArgumentNull("requestContentParameter");
            }

            if (formatters == null)
            {
                throw Fx.Exception.ArgumentNull("formatters");
            }

            Type paramType = requestContentParameter.Type;
            paramType = HttpTypeHelper.GetHttpRequestOrContentInnerTypeOrNull(paramType) ?? paramType;

            if (HttpTypeHelper.IsHttpResponse(paramType))
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.InvalidParameterForContentHandler(
                            HttpParameter.HttpParameterType.Name,
                            requestContentParameter.Name,
                            requestContentParameter.Type.Name,
                            requestContentHandlerType.Name)));
            }
            else if (HttpTypeHelper.IsHttpRequestOrContent(paramType))
            {
                this.outputParameter = new HttpParameter(requestContentParameter.Name, HttpParameter.RequestMessage.Type);
                this.requestMessageConverter = simpleHttpRequestMessageConverter;             
            }
            else
            {
                Type outputParameterType = HttpTypeHelper.MakeHttpRequestMessageOf(paramType);
                this.outputParameter = new HttpParameter(requestContentParameter.Name, outputParameterType);

                Type closedConverterType = httpRequestMessageConverterGenericType.MakeGenericType(new Type[] { paramType });
                ConstructorInfo constructor = closedConverterType.GetConstructor(Type.EmptyTypes);
                this.requestMessageConverter = constructor.Invoke(null) as HttpRequestMessageConverter;
            }
            
            this.Formatters = new MediaTypeFormatterCollection(formatters);
        }

        /// <summary>
        /// Gets the default <see cref="MediaTypeFormatter"/> instances to use for the <see cref="HttpRequestMessage{}"/>
        /// instances created by the <see cref="RequestContentHandler"/>.
        /// </summary>
        public MediaTypeFormatterCollection Formatters { get; private set; }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing the
        /// input values for this <see cref="RequestContentHandler"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="UriTemplateHandler"/> always returns a single input of
        /// <see cref="HttpParameter.RequestMessage"/>.
        /// </remarks>
        /// <returns>A collection that consists of just the <see cref="HttpParameter.RequestMessage"/>.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetInputParameters()
        {
            return new HttpParameter[] { HttpParameter.RequestMessage };
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/>s describing the
        /// output values of this <see cref="RequestContentHandler"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="UriTemplateHandler"/> always returns the <see cref="HttpParameter"/> 
        /// that was passed into the constructor of the <see cref="RequestContentHandler"/>.
        /// </remarks>
        /// <returns>
        /// A collection that consists of just the <see cref="HttpParameter"/> 
        /// that was passed into the constructor of the <see cref="RequestContentHandler"/>.
        /// </returns>
        protected override sealed IEnumerable<HttpParameter> OnGetOutputParameters()
        {
            return new HttpParameter[] { this.outputParameter };
        }

        /// <summary>
        /// Called to execute this <see cref="RequestContentHandler"/>.
        /// </summary>
        /// <param name="input">
        /// The input values to handle corresponding to the <see cref="HttpParameter"/> 
        /// returned by <see cref="OnGetInputParameters"/>.
        /// </param>
        /// <returns>
        /// The output values corresponding to the <see cref="HttpParameter"/> returned 
        /// by <see cref="OnGetOutputParameters"/>.
        /// </returns>
        protected override sealed object[] OnHandle(object[] input)
        {
            Fx.Assert(input != null, "The 'input' parameter should not be null.");
            Fx.Assert(input.Length == 1, "There should be one element in the 'input' array");

            HttpRequestMessage requestMessage = input[0] as HttpRequestMessage;
            if (requestMessage == null)
            {
                throw Fx.Exception.ArgumentNull(HttpParameter.RequestMessage.Name);
            }

            HttpRequestMessage convertedRequestMessage = this.requestMessageConverter.Convert(requestMessage, this.Formatters);
            return new object[] { convertedRequestMessage };
        }

        /// <summary>
        /// Abstract base class used by the <see cref="RequestContentHandler"/> to create 
        /// <see cref="HttRequestMessage{}"/> instances for a particular <typeparamref name="T"/> 
        /// without the performance hit of using reflection for every new instance.
        /// </summary>
        private abstract class HttpRequestMessageConverter
        {
            /// <summary>
            /// Base abstract method that is overridden by the <see cref="HttpRequestMessageConverter{}"/>
            /// to convert an <see cref="HttRequestMessage"/> into an <see cref="HttRequestMessage{}"/> of 
            /// a particular type.
            /// </summary>
            /// <param name="requestMessage">The <see cref="HttpRequestMessage"/> to convert.</param>
            /// <param name="formatters">
            /// The <see cref="MediaTypeFormatter"/> collection to use with the <see cref="ObjectContent{}"/>
            /// used by the converted <see cref="HttpRequestMessageConverter{}"/>.
            /// </param>
            /// <returns>The converted <see cref="HttpRequestMessageConverter{}"/>.</returns>
            public abstract HttpRequestMessage Convert(HttpRequestMessage requestMessage, IEnumerable<MediaTypeFormatter> formatters);
        }

        /// <summary>
        /// An <see cref="HttpRequestMessageConverter"/> that is only used when the request content should be a non-generic <see cref="HttpRequestMessage"/>.
        /// </summary>
        private class SimpleHttpRequestMessageConverter : HttpRequestMessageConverter
        {
            /// <summary>
            /// Overridden method that simply passes the <see cref="HttpRequestMessage"/> instance through.
            /// </summary>
            /// <param name="requestMessage">The <see cref="HttpRequestMessage"/> to pass through.</param>
            /// <param name="formatters">The <see cref="MediaTypeFormatter"/> collection to use. This parameter is not used.</param>
            /// <returns>
            /// The <see cref="HttpRequestMessage"/>.
            /// </returns>
            public override HttpRequestMessage Convert(HttpRequestMessage requestMessage, IEnumerable<MediaTypeFormatter> formatters)
            {
                return requestMessage;
            }
        }

        /// <summary>
        /// Generic version of the <see cref="HttpRequestMessageConverter"/> used by the 
        /// <see cref="RequestContentHandler"/> to create <see cref="HttRequestMessage{}"/> instances 
        /// for a particular <typeparamref name="T"/> without the performance hit of using reflection
        /// for every new instance.
        /// </summary>
        /// <typeparam name="T">The type with which to create new <see cref="HttpRequestMessage{}"/> instances.</typeparam>
        private class HttpRequestMessageConverter<T> : HttpRequestMessageConverter
        {
            /// <summary>
            /// Converts an <see cref="HttRequestMessage"/> into an <see cref="HttRequestMessage{}"/> of
            /// a particular type.
            /// </summary>
            /// <param name="requestMessage">The <see cref="HttpRequestMessage"/> to convert.</param>
            /// <param name="formatters">The <see cref="MediaTypeFormatter"/> collection to use with the <see cref="ObjectContent{}"/>
            /// used by the converted <see cref="HttpRequestMessageConverter{}"/>.</param>
            /// <returns>
            /// The converted <see cref="HttpRequestMessageConverter{}"/>.
            /// </returns>
            public override HttpRequestMessage Convert(HttpRequestMessage requestMessage, IEnumerable<MediaTypeFormatter> formatters)
            {
                Fx.Assert(requestMessage != null, "The 'requestMessage' parameter should not be null.");

                HttpRequestMessage<T> convertedRequestMessage = new HttpRequestMessage<T>();
                ObjectContent<T> objectContent = new ObjectContent<T>(requestMessage.Content, formatters);
                
                convertedRequestMessage.Content = objectContent;
                convertedRequestMessage.Method = requestMessage.Method;
                convertedRequestMessage.RequestUri = requestMessage.RequestUri;
                convertedRequestMessage.Version = requestMessage.Version;
                requestMessage.Headers.CopyTo(convertedRequestMessage.Headers);
                foreach (KeyValuePair<string, object> property in requestMessage.Properties)
                {
                    convertedRequestMessage.Properties.Add(property);
                }

                return convertedRequestMessage;
            }
        }
    }
}