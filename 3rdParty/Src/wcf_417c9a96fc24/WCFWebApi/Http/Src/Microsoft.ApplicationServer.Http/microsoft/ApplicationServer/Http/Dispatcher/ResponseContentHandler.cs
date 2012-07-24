// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Http.Description;

    /// <summary>
    /// A <see cref="HttpOperationHandler"/> that takes in a instance of some type
    /// and returns an <see cref="HttpResponseMessage{}"/> instance of that same type.
    /// </summary>
    public class ResponseContentHandler : HttpOperationHandler
    {
        private static readonly HttpResponseMessageConverter simpleHttpResponseMessageConverter = new SimpleHttpResponseMessageConverter();
        private static readonly HttpResponseMessageConverter httpContentMessageConverter = new HttpContentMessageConverter();
        private static readonly HttpResponseMessageConverter voidHttpResponseMessageConverter = new VoidHttpResponseMessageConverter();
        private static readonly Type httpResponseMessageConverterGenericType = typeof(HttpResponseMessageConverter<>);
        private static readonly Type responseContentHandlerType = typeof(ResponseContentHandler);

        private HttpParameter outputParameter;
        private HttpParameter inputParameter;
        private HttpResponseMessageConverter responseMessageConverter;

        /// <summary>
        /// Initializes a new instance of a <see cref="ResponseContentHandler"/> with the
        /// given <paramref name="responseContentParameter"/> and <paramref name="formatters"/>.
        /// </summary>
        /// <param name="responseContentParameter">The <see cref="HttpParameter"/> for the content of the response.</param>
        /// <param name="formatters">The collection of <see cref="MediaTypeFormatter"/> instances to use for deserializing the response content.</param>
        public ResponseContentHandler(HttpParameter responseContentParameter, IEnumerable<MediaTypeFormatter> formatters)
        {
            if (formatters == null)
            {
                throw Fx.Exception.ArgumentNull("formatters");
            }

            Type paramType = responseContentParameter == null ? 
                TypeHelper.VoidType :
                responseContentParameter.Type;

            if (paramType == TypeHelper.VoidType)
            {
                this.responseMessageConverter = voidHttpResponseMessageConverter;
                this.outputParameter = HttpParameter.ResponseMessage;
            }
            else
            {
                paramType = HttpTypeHelper.GetHttpResponseOrContentInnerTypeOrNull(paramType) ?? paramType;

                if (HttpTypeHelper.IsHttpRequest(paramType))
                {
                    throw Fx.Exception.AsError(
                        new InvalidOperationException(
                            SR.InvalidParameterForContentHandler(
                                HttpParameter.HttpParameterType.Name,
                                responseContentParameter.Name,
                                responseContentParameter.Type.Name,
                                responseContentHandlerType.Name)));
                }
              
                Type outputParameterType = HttpTypeHelper.IsHttp(paramType) ? paramType : HttpTypeHelper.MakeHttpResponseMessageOf(paramType);
                this.outputParameter = new HttpParameter(responseContentParameter.Name, outputParameterType);

                this.inputParameter = responseContentParameter;

                if (HttpTypeHelper.IsHttpResponse(paramType))
                {
                    this.responseMessageConverter = simpleHttpResponseMessageConverter;
                }
                else if (HttpTypeHelper.IsHttpContent(paramType))
                {
                    this.responseMessageConverter = httpContentMessageConverter;
                }
                else
                {
                    Type closedConverterType = httpResponseMessageConverterGenericType.MakeGenericType(new Type[] { paramType });
                    ConstructorInfo constructor = closedConverterType.GetConstructor(Type.EmptyTypes);
                    this.responseMessageConverter = constructor.Invoke(null) as HttpResponseMessageConverter;
                }
            }

            this.Formatters = new MediaTypeFormatterCollection(formatters);
        }

        /// <summary>
        /// Gets the default <see cref="MediaTypeFormatter"/> instances to use for the <see cref="HttpResponseMessage{}"/>
        /// instances created by the <see cref="ResponseContentHandler"/>.
        /// </summary>
        public MediaTypeFormatterCollection Formatters { get; private set; }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing the
        /// input values for this <see cref="ResponseContentHandler"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="UriTemplateHandler"/> always returns a single input of
        /// <see cref="HttpParameter.ResponseMessage"/>.
        /// </remarks>
        /// <returns>A collection that consists of just the <see cref="HttpParameter.ResponseMessage"/>.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetInputParameters()
        {
            return this.inputParameter != null ?
                new HttpParameter[] { HttpParameter.RequestMessage, this.inputParameter } :
                new HttpParameter[] { HttpParameter.RequestMessage };
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/>s describing the
        /// output values of this <see cref="ResponseContentHandler"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="UriTemplateHandler"/> always returns the <see cref="HttpParameter"/> 
        /// instance that was passed into the constructor of the <see cref="ResponseContentHandler"/>.
        /// </remarks>
        /// <returns>
        /// A collection that consists of just the <see cref="HttpParameter"/> 
        /// instance that was passed into the constructor of the <see cref="ResponseContentHandler"/>.
        /// </returns>
        protected override sealed IEnumerable<HttpParameter> OnGetOutputParameters()
        {
            return new HttpParameter[] { this.outputParameter };
        }

        /// <summary>
        /// Called to execute this <see cref="ResponseContentHandler"/>.
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
            Fx.Assert(input.Length == 2, "There should be one element in the 'input' array");

            HttpRequestMessage requestMessage = input[0] as HttpRequestMessage;
            if (requestMessage == null)
            {
                throw Fx.Exception.ArgumentNull(HttpParameter.ResponseMessage.Name);
            }

            HttpResponseMessage convertedResponseMessage = this.responseMessageConverter.Convert(requestMessage, input[1], this.Formatters);
            return new object[] { convertedResponseMessage };
        }

        /// <summary>
        /// Abstract base class used by the <see cref="ResponseContentHandler"/> to create 
        /// <see cref="HttResponseMessage{}"/> instances for a particular <typeparamref name="T"/> 
        /// without the performance hit of using reflection for every new instance.
        /// </summary>
        private abstract class HttpResponseMessageConverter
        {
            /// <summary>
            /// Base abstract method that is overridden by the <see cref="HttpResponseMessageConverter{}"/>
            /// to convert an <see cref="HttResponseMessage"/> into an <see cref="HttResponseMessage{}"/> of
            /// a particular type.
            /// </summary>
            /// <param name="requestMessage">The <see cref="HttpRequestMessage"/> to attach to the converted <see cref="HttpResponseMessage"/>.</param>
            /// <param name="responseContent">The content of the response message.</param>
            /// <param name="formatters">The <see cref="MediaTypeFormatter"/> collection to use with the <see cref="ObjectContent{}"/>
            /// used by the converted <see cref="HttpResponseMessageConverter{}"/>.</param>
            /// <returns>
            /// The converted <see cref="HttpResponseMessageConverter{}"/>.
            /// </returns>
            public abstract HttpResponseMessage Convert(HttpRequestMessage requestMessage, object responseContent, IEnumerable<MediaTypeFormatter> formatters);
        }

        /// <summary>
        /// An <see cref="HttpResponseMessageConverter"/> that is only used when the response content is a non-generic <see cref="HttpResponseMessage"/>.
        /// </summary>
        private class SimpleHttpResponseMessageConverter : HttpResponseMessageConverter
        {
            /// <summary>
            /// Overridden method that simply sets the <see cref="HttpReponseMessage.RequestMessage"/> on the <see cref="HttpResponseMessage"/> instance that
            /// is already provided as the <paramref name="responseContent"/>.
            /// </summary>
            /// <param name="requestMessage">The <see cref="HttpRequestMessage"/> to attach to the <see cref="HttpResponseMessage"/>.</param>
            /// <param name="responseContent">The response message.</param>
            /// <param name="formatters">The <see cref="MediaTypeFormatter"/> collection to use with the <see cref="ObjectContent{}"/>
            /// used by the converted <see cref="HttpResponseMessageConverter{}"/>.</param>
            /// <returns>
            /// The <see cref="HttpResponseMessage"/>.
            /// </returns>
            public override HttpResponseMessage Convert(HttpRequestMessage requestMessage, object responseContent, IEnumerable<MediaTypeFormatter> formatters)
            {
                HttpResponseMessage response = responseContent as HttpResponseMessage;
                if (response == null)
                {
                    response = new HttpResponseMessage();
                }
                else
                {
                    ObjectContent objectContent = response.Content as ObjectContent;
                    if (objectContent != null)
                    {
                        objectContent.Formatters.ReplaceAllWith(formatters);
                    }
                }

                response.RequestMessage = requestMessage;

                return response;
            }
        }

        /// <summary>
        /// An <see cref="VoidHttpResponseMessageConverter"/> that is only used when the response content is a void.
        /// </summary>
        private class VoidHttpResponseMessageConverter : HttpResponseMessageConverter
        {
            /// <summary>
            /// Overridden method that simply sets the <see cref="HttpReponseMessage.RequestMessage"/> on a new <see cref="HttpResponseMessage"/> instance.
            /// </summary>
            /// <param name="requestMessage">The <see cref="HttpRequestMessage"/> to attach to the <see cref="HttpResponseMessage"/>.</param>
            /// <param name="responseContent">The value is always null.</param>
            /// <param name="formatters">The value is not used.</param>
            /// <returns>
            /// The <see cref="HttpResponseMessage"/>.
            /// </returns>
            public override HttpResponseMessage Convert(HttpRequestMessage requestMessage, object responseContent, IEnumerable<MediaTypeFormatter> formatters)
            {
                HttpResponseMessage response = new HttpResponseMessage();
                response.RequestMessage = requestMessage;

                return response;
            }
        }

        /// <summary>
        /// An <see cref="HttpResponseMessageConverter"/> that is only used when the response content is a an <see cref="HttpContent"/>.
        /// </summary>
        private class HttpContentMessageConverter : HttpResponseMessageConverter
        {
            /// <summary>
            /// Overriden method that simply creates a new the <see cref="HttpResponseMessage"/> instance and sets the <see cref="HttpContent"/> that
            /// is already provided as the <paramref name="responseContent"/>.
            /// </summary>
            /// <param name="requestMessage">The <see cref="HttpRequestMessage"/> to attach to the <see cref="HttpResponseMessage"/>.</param>
            /// <param name="responseContent">The response message content.</param>
            /// <param name="formatters">The <see cref="MediaTypeFormatter"/> collection to use with the <see cref="ObjectContent{}"/>
            /// used by the converted <see cref="HttpResponseMessageConverter{}"/>.</param>
            /// <returns>
            /// The <see cref="HttpResponseMessage"/>.
            /// </returns>
            public override HttpResponseMessage Convert(HttpRequestMessage requestMessage, object responseContent, IEnumerable<MediaTypeFormatter> formatters)
            {
                HttpResponseMessage response = new HttpResponseMessage();
                response.Content = responseContent as HttpContent;
                response.RequestMessage = requestMessage;

                ObjectContent objectContent = response.Content as ObjectContent;
                if (objectContent != null)
                {
                    objectContent.Formatters.ReplaceAllWith(formatters);
                }

                return response;
            }
        }

        /// <summary>
        /// Generic version of the <see cref="HttpResponseMessageConverter"/> used by the 
        /// <see cref="ResponseContentHandler"/> to create <see cref="HttResponseMessage{}"/> instances 
        /// for a particular <typeparamref name="T"/> without the performance hit of using reflection
        /// for every new instance.
        /// </summary>
        /// <typeparam name="T">The type with which to create new <see cref="HttpResponseMessage{}"/> instances.</typeparam>
        private class HttpResponseMessageConverter<T> : HttpResponseMessageConverter
        {
            /// <summary>
            /// Converts an <see cref="HttResponseMessage"/> into an <see cref="HttResponseMessage{}"/> of
            /// a particular type.
            /// </summary>
            /// <param name="requestMessage">The <see cref="HttpRequestMessage"/> to attach to the converted <see cref="HttpResponseMessage"/>.</param>
            /// <param name="responseContent">The content of the response message.</param>
            /// <param name="formatters">The <see cref="MediaTypeFormatter"/> collection to use with the <see cref="ObjectContent{}"/>
            /// used by the converted <see cref="HttpResponseMessageConverter{}"/>.</param>
            /// <returns>
            /// The converted <see cref="HttpResponseMessage{}"/>.
            /// </returns>
            public override HttpResponseMessage Convert(HttpRequestMessage requestMessage, object responseContent, IEnumerable<MediaTypeFormatter> formatters)
            {
                Fx.Assert(requestMessage != null, "The 'requestMessage' parameter should not be null.");

                HttpResponseMessage<T> convertedResponseMessage = null;
                if (responseContent == null || responseContent is T)
                {
                    convertedResponseMessage = new HttpResponseMessage<T>((T)responseContent, formatters);
                    convertedResponseMessage.RequestMessage = requestMessage;
                    return convertedResponseMessage;
                }

                if (responseContent is ObjectContent<T>)
                {
                    convertedResponseMessage = new HttpResponseMessage<T>(HttpStatusCode.OK);
                    convertedResponseMessage.Content = (ObjectContent<T>)responseContent;
                }
                else
                {
                    convertedResponseMessage = (HttpResponseMessage<T>)responseContent;
                }

                ObjectContent<T> objectContent = convertedResponseMessage.Content;
                if (objectContent != null)
                {
                    objectContent.Formatters.ReplaceAllWith(formatters);
                }

                convertedResponseMessage.RequestMessage = requestMessage;

                return convertedResponseMessage;
            }
        }
    }
}