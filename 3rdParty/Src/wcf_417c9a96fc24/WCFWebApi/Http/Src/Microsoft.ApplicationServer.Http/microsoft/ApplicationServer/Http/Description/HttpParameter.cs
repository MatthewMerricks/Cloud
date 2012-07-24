// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Description
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.ServiceModel.Description;
    using Microsoft.ApplicationServer.Common;

    /// <summary>
    /// Represents the description of input parameters, output parameters,
    /// or return values for an <see cref="HttpOperationDescription"/>.
    /// </summary>
    public class HttpParameter
    {
        internal const string IsContentParameterPropertyName = "IsContentParameter";
        internal static readonly Type HttpParameterType = typeof(HttpParameter);

        private MessagePartDescription messagePartDescription;
        private string name;
        private Type type;
        private HttpParameterValueConverter valueConverter;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpParameter"/> class.
        /// </summary>
        /// <param name="name">The name of the <see cref="HttpParameter"/>.</param>
        /// <param name="type">The type of the <see cref="HttpParameter"/>.</param>
        public HttpParameter(string name, Type type)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw Fx.Exception.ArgumentNull("name");
            }

            if (type == null)
            {
                throw Fx.Exception.ArgumentNull("type");
            }

            this.name = name;
            this.type = type;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="HttpParameter"/> class.
        /// </summary>
        /// <remarks>
        /// This form of the constructor creates an instance based on an existing
        /// <see cref="MessagePartDescription"/> instance.  To create such an instance, use the extension method
        /// <see cref="HttpParameterExtensionMethods.ToHttpParameter"/>.
        /// </remarks>
        /// <param name="messagePartDescription">The existing <see cref="MessagePartDescription"/>.</param>
        internal HttpParameter(MessagePartDescription messagePartDescription)
        {
            Fx.Assert(messagePartDescription != null, "messagePartDescription should not be null");
            this.messagePartDescription = messagePartDescription;
        }

        /// <summary>
        /// Gets an <see cref="HttpParameter"/> instance for an <see cref="HttpRequestMessage"/>.
        /// </summary>
        public static HttpParameter RequestMessage 
        { 
            get 
            { 
                return new HttpParameter("RequestMessage", HttpTypeHelper.HttpRequestMessageType);
            } 
        }

        /// <summary>
        /// Gets an <see cref="HttpParameter"/> instance for the request <see cref="Uri"/>.
        /// </summary>
        public static HttpParameter RequestUri  
        { 
            get 
            { 
                return new HttpParameter("RequestUri", HttpTypeHelper.UriType);
            } 
        }

        /// <summary>
        /// Gets an <see cref="HttpParameter"/> instance for an <see cref="HttpMethod"/>.
        /// </summary>
        public static HttpParameter RequestMethod
        { 
            get 
            {
                return new HttpParameter("RequestMethod", HttpTypeHelper.HttpMethodType);
            } 
        }

        /// <summary>
        /// Gets an <see cref="HttpParameter"/> instance for the <see cref="HttpRequestHeaders"/>.
        /// </summary>
        public static HttpParameter RequestHeaders
        { 
            get 
            {
                return new HttpParameter("RequestHeaders", HttpTypeHelper.HttpRequestHeadersType);
            } 
        }

        /// <summary>
        /// Gets an <see cref="HttpParameter"/> instance for the request <see cref="HttpContent"/>.
        /// </summary>
        public static HttpParameter RequestContent
        { 
            get 
            {
                return new HttpParameter("RequestContent", HttpTypeHelper.HttpContentType);
            } 
        }

        /// <summary>
        /// Gets an <see cref="HttpParameter"/> instance for an <see cref="HttpResponseMessage"/>.
        /// </summary>
        public static HttpParameter ResponseMessage
        { 
            get 
            {
                return new HttpParameter("ResponseMessage", HttpTypeHelper.HttpResponseMessageType);
            } 
        }

        /// <summary>
        /// Gets an <see cref="HttpParameter"/> instance for the response <see cref="HttpStatusCode"/>.
        /// </summary>
        public static HttpParameter ResponseStatusCode
        { 
            get 
            {
                return new HttpParameter("ResponseStatusCode", HttpTypeHelper.HttpStatusCodeType);
            } 
        }

        /// <summary>
        /// Gets an <see cref="HttpParameter"/> instance for the <see cref="HttpResponseHeaders"/>.
        /// </summary>
        public static HttpParameter ResponseHeaders
        { 
            get 
            {
                return new HttpParameter("ResponseHeaders", HttpTypeHelper.HttpResponseHeadersType);
            } 
        }

        /// <summary>
        /// Gets an <see cref="HttpParameter"/> instance for the response <see cref="HttpContent"/>.
        /// </summary>
        public static HttpParameter ResponseContent
        {
            get
            {
                return new HttpParameter("ResponseContent", HttpTypeHelper.HttpContentType);
            }
        }

        /// <summary>
        /// Gets the name of the current instance.
        /// </summary>
        public string Name
        {
            get
            {
                return (this.messagePartDescription != null) ?
                    this.messagePartDescription.Name : 
                    this.name;
            }
        }

        /// <summary>
        /// Gets the type of the current instance.
        /// </summary>
        public Type Type
        {
            get
            {
                return (this.messagePartDescription != null) ? 
                    this.messagePartDescription.Type :
                    this.type;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is content parameter.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is content parameter; otherwise, <c>false</c>.
        /// </value>
        public bool IsContentParameter { get; set; }

        internal MessagePartDescription MessagePartDescription
        {
            get
            {
                return this.messagePartDescription;
            }
        }

        internal HttpParameterValueConverter ValueConverter
        {
            get
            {
                if (this.valueConverter == null || 
                   this.valueConverter.Type != this.Type)
                {
                    this.valueConverter = HttpParameterValueConverter.GetValueConverter(this.Type);
                }

                return this.valueConverter;
            }
        }

        internal string Namespace
        {
            get
            {
                string nameSpace = null;
                if (this.MessagePartDescription != null)
                {
                    nameSpace = this.MessagePartDescription.Namespace;
                }

                return nameSpace ?? HttpOperationDescription.DefaultNamespace;
            }
        }

        /// <summary>
        /// Determines if the <see cref="HttpParameter"/> could function as
        /// an input <see cref="HttpParameter"/> that would be able to bind
        /// to an output <see cref="HttpParameter"/> with the given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">
        /// The <see cref="Type"/> of the output <see cref="HttpParameter"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the <see cref="HttpParameter"/> could function as
        /// an input <see cref="HttpParameter"/> that would be able to bind
        /// to an output <see cref="HttpParameter"/> with the given <paramref name="type"/>;
        /// <c>false</c> otherwise.
        /// </returns>
        public bool IsAssignableFromParameter(Type type)
        {
            if (type == null)
            {
                throw Fx.Exception.ArgumentNull("type");
            }

            return this.ValueConverter.CanConvertFromType(type);
        }

        /// <summary>
        /// Determines if the <see cref="HttpParameter"/> could function as
        /// an input <see cref="HttpParameter"/> that would be able to bind
        /// to an output <see cref="HttpParameter"/> with the given <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the output <see cref="HttpParameter"/>.</typeparam>
        /// <returns>
        /// <c>true</c> if the <see cref="HttpParameter"/> could function as
        /// an input <see cref="HttpParameter"/> that would be able to bind
        /// to an output <see cref="HttpParameter"/> with the given <paramref name="type"/>;
        /// <c>false</c> otherwise.
        /// </returns>
        public bool IsAssignableFromParameter<T>()
        {
            return this.ValueConverter.CanConvertFromType(typeof(T));
        }

        internal HttpParameter Clone()
        {
            return new HttpParameter(this.Name, this.Type);
        }

        internal void SynchronizeToMessagePartDescription(MessageDescription message)
        {
            if (this.messagePartDescription == null)
            {
                string nameSpace = message == null ? HttpOperationDescription.DefaultNamespace : message.Body.WrapperNamespace;
                this.messagePartDescription = new MessagePartDescription(this.name, nameSpace);
                this.messagePartDescription.Type = this.type;
                this.name = null;
                this.type = null;
            }
        }
    }
}
