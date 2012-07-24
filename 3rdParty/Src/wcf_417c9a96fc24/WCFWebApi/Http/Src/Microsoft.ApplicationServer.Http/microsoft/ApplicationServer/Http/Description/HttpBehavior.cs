// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Description
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using System.ServiceModel.Dispatcher;
    using System.ServiceModel.Web;
    using System.Xml;
    using System.Xml.Linq;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Http;
    using Microsoft.ApplicationServer.Http.Channels;
    using Microsoft.ApplicationServer.Http.Dispatcher;

    /// <summary>
    /// Class that provides <see cref="IEndpointBehavior"/> for the <see cref="HttpBinding"/> binding.
    /// </summary>
    public class HttpBehavior : IEndpointBehavior
    {
        internal const string WildcardAction = "*";
        internal const string WildcardMethod = "*";
        internal const bool DefaultHelpEnabled = false;
        internal const TrailingSlashMode DefaultTrailingSlashMode = TrailingSlashMode.AutoRedirect;

        private static readonly Type xmlSerializerFormatAttributeType = typeof(XmlSerializerFormatAttribute);
        private static readonly Type messageContractAttributeType = typeof(MessageContractAttribute);
        private static readonly Type httpMessageEncodingBindingElementType = typeof(HttpMessageEncodingBindingElement);
        private static readonly Type messageEncodingBindingElementType = typeof(MessageEncodingBindingElement);
        private static readonly Type httpRequestMessageType = typeof(HttpRequestMessage);
        private static readonly Type httpResponseMessageType = typeof(HttpResponseMessage);
        private static readonly Type httpBehaviorType = typeof(HttpBehavior);
        private static readonly Type webGetAttributeType = typeof(WebGetAttribute);
        private static readonly Type webInvokeAttributeType = typeof(WebInvokeAttribute);

        private TrailingSlashMode trailingSlashMode;
        private HttpOperationHandlerFactory httpOperationHandlerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpBehavior"/> class.
        /// </summary>
        public HttpBehavior()
        {
            this.HelpEnabled = DefaultHelpEnabled;
            this.TrailingSlashMode = DefaultTrailingSlashMode;
        }

        /// <summary>
        /// Gets or sets a value indicating whether an automatic help page will be
        /// available.
        /// </summary>
        public bool HelpEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value specifying how trailing slashes will be handled
        /// with URI-based operation selection.
        /// </summary>
        public TrailingSlashMode TrailingSlashMode
        {
            get
            {
                return this.trailingSlashMode;
            }

            set
            {
                if (!TrailingSlashModeHelper.IsDefined(value))
                {
                    throw Fx.Exception.AsError(
                        new ArgumentOutOfRangeException("value"));
                }

                this.trailingSlashMode = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="OperationHandlerFactory"/>.
        /// </summary>
        /// <value>
        /// The value returned will never be <c>null</c>.  
        /// If no <see cref="HttpOperationHandlerFactory"/> has been set,
        /// the default factory will be returned.
        /// </value>
        public HttpOperationHandlerFactory OperationHandlerFactory
        {
            get
            {
                if (this.httpOperationHandlerFactory == null)
                {
                    this.httpOperationHandlerFactory = new HttpOperationHandlerFactory();
                }

                return this.httpOperationHandlerFactory;
            }

            set
            {
                this.httpOperationHandlerFactory = value;
            }
        }

        /// <summary>
        /// Passes data at runtime to bindings to support custom behavior.
        /// </summary>
        /// <param name="endpoint">The endpoint to modify.</param>
        /// <param name="bindingParameters">The objects that binding elements require to support the behavior.</param>
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
            if (endpoint == null)
            {
                throw Fx.Exception.ArgumentNull("endpoint");
            }

            if (bindingParameters == null)
            {
                throw Fx.Exception.ArgumentNull("bindingParameters");
            }

            this.OnAddBindingParameters(endpoint, bindingParameters);
        }

        /// <summary>
        /// Implements a modification or extension of the client across and endpoint
        /// </summary>
        /// <param name="endpoint">The endpoint that is to be customized.</param>
        /// <param name="clientRuntime">The client runtime to be customized</param>
        void IEndpointBehavior.ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            throw Fx.Exception.AsError(
                new NotSupportedException(SR.ApplyClientBehaviorNotSupportedByHttpBehavior(httpBehaviorType.Name)));
        }

        /// <summary>
        /// Confirms that the endpoint meets some intended criteria.
        /// </summary>
        /// <param name="endpoint">The endpoint to validate.</param>
        public void Validate(ServiceEndpoint endpoint)
        {
            if (endpoint == null)
            {
                throw Fx.Exception.ArgumentNull("endpoint");
            }

            this.OnValidate(endpoint);
        }

        /// <summary>
        /// Allows for extension or modification of the service across an endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint that exposes the contract.</param>
        /// <param name="endpointDispatcher">The endpoint dispatcher to be modified or extended.</param>
        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            if (endpoint == null)
            {
                throw Fx.Exception.ArgumentNull("endpoint");
            }

            if (endpointDispatcher == null)
            {
                throw Fx.Exception.ArgumentNull("endpointDispatcher");
            }

            this.OnApplyDispatchBehavior(endpoint, endpointDispatcher);
        }

        /// <summary>
        /// Generates a ContractDescription that can be used for generating the help page.
        /// </summary>
        /// <param name="originalContract"><see cref="ContractDescription"/> of the Service endpoint for which the help page contract desciption is generated.</param>
        /// <returns><see cref="ContractDescription"/> that should be used for the HelpPage generation.</returns>
        internal static ContractDescription GenerateClientContractDescription(ContractDescription originalContract)
        {
            if (originalContract == null)
            {
                return null;
            }

            ContractDescription contract = new ContractDescription(originalContract.Name, originalContract.Namespace)
            {
                ProtectionLevel = originalContract.ProtectionLevel,
                SessionMode = originalContract.SessionMode,
                CallbackContractType = originalContract.CallbackContractType,
                ConfigurationName = originalContract.ConfigurationName,
                ContractType = originalContract.ContractType, // this will point to the original contract
            };

            // add contract behaviors
            foreach (IContractBehavior behavior in originalContract.Behaviors)
            {
                contract.Behaviors.Add(behavior);
            }

            // add operations
            foreach (OperationDescription operationDescription in originalContract.Operations)
            {
                contract.Operations.Add(ClientContractDescriptionHelper.GetEquivalentOperationDescription(operationDescription));
            }

            return contract;
        }

        /// <summary>
        /// Override in a derived class to pass data at runtime to bindings to support custom behavior.
        /// </summary>
        /// <remarks>This base implementation does nothing.</remarks>
        /// <param name="endpoint">The endpoint to modify.</param>
        /// <param name="bindingParameters">The objects that binding elements require to support the behavior.</param>
        protected virtual void OnAddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {     
            // do nothing
        }

        /// <summary>
        /// Override in a derived class to confirm that the endpoint meets some intended criteria.
        /// </summary>
        /// <remarks>
        /// This base implementation provides a some standard validation for the endpoint and the service operations, that
        /// derived implementations should generally leverage.
        /// </remarks>
        /// <param name="endpoint">The endpoint to validate.</param>
        protected virtual void OnValidate(ServiceEndpoint endpoint) 
        {
            if (endpoint == null)
            {
                throw Fx.Exception.ArgumentNull("endpoint");
            }

            ValidateEndpoint(endpoint);

            foreach (OperationDescription operationDescription in endpoint.Contract.Operations)
            {
                ValidateOperationDescription(operationDescription);
            }
        }

        /// <summary>
        /// Override in a derived class to extened or modify the behavior of the service across an endpoint.
        /// </summary>
        /// <remarks>
        /// This base implementation sets up the proper operation dispatcher, formatter, and effor handler.
        /// Derived implementations shyould always call the base.
        /// </remarks>
        /// <param name="endpoint">The endpoint that exposes the contract.</param>
        /// <param name="endpointDispatcher">The endpoint dispatcher to be modified or extended.</param>
        protected virtual void OnApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            if (endpoint == null)
            {
                throw Fx.Exception.ArgumentNull("endpoint");
            }

            if (endpointDispatcher == null)
            {
                throw Fx.Exception.ArgumentNull("endpointDispatcher");
            }

            Uri helpUri = null;
            OperationDescription[] helpOperations = null;
            if (this.HelpEnabled)
            {
                helpUri = new UriTemplate(HelpPage.OperationListHelpPageUriTemplate).BindByPosition(endpoint.ListenUri);
                helpOperations = HelpPage.AddHelpOperations(endpoint.Contract, endpointDispatcher.DispatchRuntime);
            }

            List<HttpOperationDescription> httpOperations = new List<HttpOperationDescription>();
            foreach (OperationDescription operationDescription in endpoint.Contract.Operations)
            {
                HttpOperationDescription httpOperationDescription = operationDescription.ToHttpOperationDescription();
                httpOperations.Add(httpOperationDescription);
            }

            // endpoint filter
            endpointDispatcher.AddressFilter = new PrefixEndpointAddressMessageFilter(endpoint.Address);
            endpointDispatcher.ContractFilter = new MatchAllMessageFilter();

            // operation selector
            endpointDispatcher.DispatchRuntime.OperationSelector = this.OnGetOperationSelector(endpoint, httpOperations);
            UriAndMethodOperationSelector httpOperationSelector = endpointDispatcher.DispatchRuntime.OperationSelector as UriAndMethodOperationSelector;
            if (httpOperationSelector != null)
            {
                httpOperationSelector.HelpPageUri = helpUri;
            }

            // unhandled operation
            string actionStarOperationName = null;
            foreach (OperationDescription operation in endpoint.Contract.Operations)
            {
                if (operation.Messages[0].Direction == MessageDirection.Input && 
                    operation.Messages[0].Action == WildcardAction)
                {
                    actionStarOperationName = operation.Name;
                    break;
                }
            }

            if (actionStarOperationName != null)
            {
                endpointDispatcher.DispatchRuntime.Operations.Add(
                    endpointDispatcher.DispatchRuntime.UnhandledDispatchOperation);
            }

            // message formatter
            foreach (HttpOperationDescription httpOperationDescription in httpOperations)
            {
                DispatchOperation dispatchOperation = null;
                if (endpointDispatcher.DispatchRuntime.Operations.Contains(httpOperationDescription.Name))
                {
                    dispatchOperation = endpointDispatcher.DispatchRuntime.Operations[httpOperationDescription.Name];
                }
                else if (endpointDispatcher.DispatchRuntime.UnhandledDispatchOperation.Name == httpOperationDescription.Name)
                {
                    dispatchOperation = endpointDispatcher.DispatchRuntime.UnhandledDispatchOperation;
                }

                if (dispatchOperation != null)
                {
                    dispatchOperation.Formatter = this.OnGetMessageFormatter(endpoint, httpOperationDescription);
                    dispatchOperation.DeserializeRequest = true;
                    dispatchOperation.SerializeReply = !dispatchOperation.IsOneWay;
                }

                //FIX: GB - IQueryable
                if (httpOperationDescription.ReturnValue != null)
                {
                    var returnType = httpOperationDescription.ReturnValue.Type;
                    if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(IQueryable<>))
                    {
                        httpOperationDescription.Behaviors.Add(new QueryCompositionAttribute());
                    }
                }
            }

            // add any user error handlers
            IEnumerable<HttpErrorHandler> errorHandlers = this.OnGetHttpErrorHandlers(endpoint, httpOperations);
            if (errorHandlers != null)
            {
                foreach (HttpErrorHandler errorHandler in errorHandlers)
                {
                    endpointDispatcher.ChannelDispatcher.ErrorHandlers.Add(errorHandler);
                }
            }

            // add the default error handler
            HttpErrorHandler defaultErrorHandler = new HttpResponseErrorHandler(
                this.OperationHandlerFactory.Formatters,
                helpUri,
                endpointDispatcher.DispatchRuntime.ChannelDispatcher.IncludeExceptionDetailInFaults);
            endpointDispatcher.ChannelDispatcher.ErrorHandlers.Add(defaultErrorHandler);

            // remove the help operations from the contract if they were added
            if (helpOperations != null)
            {
                foreach (OperationDescription helpOperation in helpOperations)
                {
                    if (endpoint.Contract.Operations.Contains(helpOperation))
                    {
                        endpoint.Contract.Operations.Remove(helpOperation);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the collection of <see cref="HttpErrorHandler"/> instances for the given
        /// set of <paramref name="operations"/> for the specified <paramref name="endpoint"/>.
        /// </summary>
        /// <remarks>
        /// The base implementation does nothing.
        /// </remarks>
        /// <param name="endpoint">The endpoint whose error handlers are required.</param>
        /// <param name="operations">The set of <see cref="HttpOperationDescription"/>s.</param>
        /// <returns>The seet of error handlers. The default is an empty collection.</returns>
        protected virtual IEnumerable<HttpErrorHandler> OnGetHttpErrorHandlers(ServiceEndpoint endpoint, IEnumerable<HttpOperationDescription> operations)
        {
            return Enumerable.Empty<HttpErrorHandler>();
        }

        /// <summary>
        /// Gets the <see cref="HttpOperationSelector"/> to use for the given set
        /// of <paramref name="operations"/> for the specified <paramref name="endpoint"/>.
        /// </summary>
        /// <remarks>The base implementation returns the default <see cref="UriAndMethodOperationSelector"/>.
        /// </remarks>
        /// <param name="endpoint">The endpoint exposing the operations.</param>
        /// <param name="operations">The set of <see cref="HttpOperationDescription"/>.</param>
        /// <returns>The <see cref="HttpOperationSelector"/> to use.</returns>
        protected virtual HttpOperationSelector OnGetOperationSelector(ServiceEndpoint endpoint, IEnumerable<HttpOperationDescription> operations)
        {
            if (endpoint == null)
            {
                throw Fx.Exception.ArgumentNull("endpoint");
            }

            return new UriAndMethodOperationSelector(endpoint.Address.Uri, operations, this.TrailingSlashMode);
        }

        /// <summary>
        /// Gets the <see cref="HttpMessageFormatter"/> to use for the given 
        /// <paramref name="operations"/> for the specified <paramref name="endpoint"/>.
        /// </summary>
        /// <remarks>
        /// The base implementation returns an <see cref="HttpMessageFormatter"/> with the
        /// <see cref="HttpOperationHandler"/> insances applied to the given operation.
        /// </remarks>
        /// <param name="endpoint">The endpoint exposing the operations.</param>
        /// <param name="operation">The <see cref="HttpOperationDescription"/>.</param>
        /// <returns>The <see cref="HttpMessageFormatter"/> to use for the <paramref name="operation"/>.</returns>
        protected virtual HttpMessageFormatter OnGetMessageFormatter(ServiceEndpoint endpoint, HttpOperationDescription operation)
        {
            Fx.Assert(endpoint != null, "The 'endpoint' parameter should not be null.");
            Fx.Assert(operation != null, "The 'operation' parameter should not be null.");

            OperationHandlerPipeline pipeline = null;
            if (this.OperationHandlerFactory == null)
            {
                Collection<HttpOperationHandler> handlers = new Collection<HttpOperationHandler>();
                pipeline = new OperationHandlerPipeline(handlers, handlers, operation);
            }
            else
            { 
                Collection<HttpOperationHandler> requestHandlers = this.OperationHandlerFactory.CreateRequestHandlers(endpoint, operation);
                Collection<HttpOperationHandler> responseHandlers = this.OperationHandlerFactory.CreateResponseHandlers(endpoint, operation);

                pipeline = new OperationHandlerPipeline(requestHandlers, responseHandlers, operation);
            }

            OperationHandlerFormatter formatter = new OperationHandlerFormatter(pipeline);
            return formatter;
        }

        private static void ValidateEndpoint(ServiceEndpoint endpoint)
        {
            Fx.Assert(endpoint != null, "The 'endpoint' parameter should not be null.");

            // Ensure no message headers
            if (endpoint.Address != null && endpoint.Address.Headers.Count > 0)
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.HttpServiceEndpointCannotHaveMessageHeaders(
                            endpoint.Address)));
            }

            // Ensure http(s) scheme
            Binding binding = endpoint.Binding;
            if (binding == null)
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(SR.HttpBehaviorBindingRequired(httpBehaviorType.Name)));
            }

            if (binding.Scheme != Uri.UriSchemeHttp && binding.Scheme != Uri.UriSchemeHttps)
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.InvalidUriScheme(
                            endpoint.Address.Uri.AbsoluteUri)));
            }

            // Ensure MessageVersion.None
            if (binding.MessageVersion != MessageVersion.None)
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.InvalidMessageVersion(
                            endpoint.Address.Uri.AbsoluteUri)));
            }

            // Ensure manual addressing
            TransportBindingElement transportBindingElement = binding.CreateBindingElements().Find<TransportBindingElement>();
            Fx.Assert(transportBindingElement != null, "The MessageVersion check would have failed if there is not a transportBindingElement.");
            if (!transportBindingElement.ManualAddressing)
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.InvalidManualAddressingValue(
                            endpoint.Address.Uri.AbsoluteUri)));
            }

            // Ensure HttpMessageEncodingBindingElement
            HttpMessageEncodingBindingElement httpMessageEncodingBindingElement = binding.CreateBindingElements().Find<HttpMessageEncodingBindingElement>();
            if (httpMessageEncodingBindingElement == null)
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.InvalidMessageEncodingBindingElement(
                            endpoint.Address.Uri.AbsoluteUri,
                            messageEncodingBindingElementType.Name,
                            httpMessageEncodingBindingElementType.Name)));
            }
        }

        private static void ValidateOperationDescription(OperationDescription operation)
        {
            Fx.Assert(operation != null, "The 'operationDescription' parameter should not be null.");

            // Ensure no operations with XmlSerializer Rpc style
            XmlSerializerOperationBehavior xmlSerializerOperationBehavior = operation.Behaviors.Find<XmlSerializerOperationBehavior>();
            if (xmlSerializerOperationBehavior != null &&
                (xmlSerializerOperationBehavior.XmlSerializerFormatAttribute.Style == OperationFormatStyle.Rpc))
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.InvalidXmlSerializerFormatAttribute(
                            operation.Name,
                            operation.DeclaringContract.Name,
                            xmlSerializerFormatAttributeType.Name)));
            }

            // Ensure operations don't have message headers
            if (operation.Messages[0].Headers.Count > 0 ||
                (operation.Messages.Count > 1 &&
                 operation.Messages[1].Headers.Count > 0))
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.InvalidOperationWithMessageHeaders(
                            operation.Name,
                            operation.DeclaringContract.Name)));
            }

            // Ensure operations don't have typed messages
            if (operation.Messages[0].MessageType != null)
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.InvalidMessageContractParameter(
                            operation.Name,
                            operation.DeclaringContract.Name,
                            messageContractAttributeType.Name,
                            operation.Messages[0].MessageType.Name)));
            }

            if (operation.Messages.Count > 1 &&
                operation.Messages[1].MessageType != null)
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.InvalidMessageContractParameter(
                            operation.Name,
                            operation.DeclaringContract.Name,
                            messageContractAttributeType.Name,
                            operation.Messages[1].MessageType.Name)));
            }
        }
    }
}
