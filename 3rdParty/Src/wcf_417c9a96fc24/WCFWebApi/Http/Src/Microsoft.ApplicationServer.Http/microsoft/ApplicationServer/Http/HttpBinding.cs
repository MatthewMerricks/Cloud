// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System.ComponentModel;
    using System.Configuration;
    using System.Diagnostics.CodeAnalysis;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Http.Channels;
    using Microsoft.ApplicationServer.Http.Configuration;
    using Microsoft.ApplicationServer.ServiceModel;
    using Microsoft.ApplicationServer.ServiceModel.Channels;

    /// <summary>
    /// A binding used with endpoints for web services that use strongly-type HTTP request 
    /// and response messages.
    /// </summary>
    public class HttpBinding : Binding, IBindingRuntimePreferences
    {
        internal const string CollectionElementName = "httpBinding";
        internal const TransferMode DefaultTransferMode = System.ServiceModel.TransferMode.Buffered;

        private HttpsTransportBindingElement httpsTransportBindingElement;
        private HttpTransportBindingElement httpTransportBindingElement;
        private HttpBindingSecurity security;
        private HttpMessageEncodingBindingElement httpMessageEncodingBindingElement;
        private HttpMessageHandlerBindingElement httpHandlerBindingElement;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpBinding"/> class.
        /// </summary>
        public HttpBinding() : base()
        {
            this.Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpBinding"/> class with a 
        /// binding specified by its configuration name.
        /// </summary>
        /// <param name="configurationName">
        /// The binding configuration name.
        /// </param>
        public HttpBinding(string configurationName) : this()
        {
            if (configurationName == null)
            {
                throw Fx.Exception.ArgumentNull("configurationName");
            }

            this.ApplyConfiguration(configurationName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpBinding"/> class with the 
        /// type of security used by the binding explicitly specified.
        /// </summary>
        /// <param name="securityMode">The value of <see cref="HttpBindingSecurityMode"/> that 
        /// specifies the type of security that is used to configure a service endpoint using the
        /// <see cref="HttpBinding"/> binding.
        /// </param>
        public HttpBinding(HttpBindingSecurityMode securityMode) : this()
        {
            this.security.Mode = securityMode;
        }

        /// <summary>
        /// Gets the envelope version that is used by endpoints that are configured to use an 
        /// <see cref="HttpBinding"/> binding.  Always returns <see cref="System.ServiceModel.EnvelopeVersion.None"/>.
        /// </summary>
        [SuppressMessage(FxCop.Category.Performance, FxCop.Rule.MarkMembersAsStatic, Justification = "This is existing public API")]
        public EnvelopeVersion EnvelopeVersion
        {
            get 
            { 
                return EnvelopeVersion.None; 
            }
        }

        /// <summary>
        /// Gets or sets a value representing the <see cref="HttpMessageHandlerFactory"/> to use for
        /// instantiating <see cref="HttpMessageHandlerChannel"/> instances.
        /// </summary>
        [DefaultValue(null)]
        public HttpMessageHandlerFactory MessageHandlerFactory 
        {
            get
            {
                return this.httpHandlerBindingElement.MessageHandlerFactory;
            }

            set
            {
                this.httpHandlerBindingElement.MessageHandlerFactory = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the hostname is used to reach the 
        /// service when matching the URI.
        /// </summary>
        [DefaultValue(HttpTransportDefaults.HostNameComparisonMode)]
        public HostNameComparisonMode HostNameComparisonMode
        {
            get 
            { 
                return this.httpTransportBindingElement.HostNameComparisonMode; 
            }

            set
            {
                this.httpTransportBindingElement.HostNameComparisonMode = value;
                this.httpsTransportBindingElement.HostNameComparisonMode = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum amount of memory allocated for the buffer manager that manages the buffers 
        /// required by endpoints that use this binding.
        /// </summary>
        [DefaultValue(TransportDefaults.MaxBufferPoolSize)]
        public long MaxBufferPoolSize
        {
            get 
            { 
                return this.httpTransportBindingElement.MaxBufferPoolSize; 
            }

            set
            {
                this.httpTransportBindingElement.MaxBufferPoolSize = value;
                this.httpsTransportBindingElement.MaxBufferPoolSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum amount of memory that is allocated for use by the manager of the message 
        /// buffers that receive messages from the channel.
        /// </summary>
        [DefaultValue(TransportDefaults.MaxBufferSize)]
        public int MaxBufferSize
        {
            get 
            { 
                return this.httpTransportBindingElement.MaxBufferSize; 
            }

            set
            {
                this.httpTransportBindingElement.MaxBufferSize = value;
                this.httpsTransportBindingElement.MaxBufferSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum size for a message that can be processed by the binding.
        /// </summary>
        [DefaultValue(TransportDefaults.MaxReceivedMessageSize)]
        public long MaxReceivedMessageSize
        {
            get 
            { 
                return this.httpTransportBindingElement.MaxReceivedMessageSize; 
            }

            set
            {
                this.httpTransportBindingElement.MaxReceivedMessageSize = value;
                this.httpsTransportBindingElement.MaxReceivedMessageSize = value;
            }
        }

        /// <summary>
        /// Gets the URI transport scheme for the channels and listeners that are configured 
        /// with this binding. (Overrides <see cref="System.ServiceModel.Channels.Binding.Scheme">
        /// Binding.Scheme</see>.)
        /// </summary>
        public override string Scheme
        { 
            get 
            { 
                return this.GetTransport().Scheme; 
            } 
        }

        /// <summary>
        /// Gets or sets the security settings used with this binding. 
        /// </summary>
        public HttpBindingSecurity Security
        {
            get 
            { 
                return this.security; 
            }
            
            set
            {
                if (value == null)
                {
                    throw Fx.Exception.ArgumentNull("value");
                }

                this.security = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the service configured with the 
        /// binding uses streamed or buffered (or both) modes of message transfer.
        /// </summary>
        [DefaultValue(HttpTransportDefaults.TransferMode)]
        public TransferMode TransferMode
        {
            get 
            { 
                return this.httpTransportBindingElement.TransferMode; 
            }
            
            set
            {
                this.httpTransportBindingElement.TransferMode = value;
                this.httpsTransportBindingElement.TransferMode = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether incoming requests can be handled more efficiently synchronously or asynchronously.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Justification = "This is the pattern used by all standard bindings.")]
        bool IBindingRuntimePreferences.ReceiveSynchronously
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns an ordered collection of binding elements contained in the current binding. 
        /// (Overrides <see cref="System.ServiceModel.Channels.Binding.CreateBindingElements">
        /// Binding.CreateBindingElements</see>.)
        /// </summary>
        /// <returns>
        /// An ordered collection of binding elements contained in the current binding.
        /// </returns>
        public override BindingElementCollection CreateBindingElements()
        {
            BindingElementCollection bindingElements = new BindingElementCollection();

            bindingElements.Add(this.httpHandlerBindingElement);
            bindingElements.Add(this.httpMessageEncodingBindingElement);
            bindingElements.Add(this.GetTransport());

            return bindingElements.Clone();
        }

        private void ApplyConfiguration(string configurationName)
        {
            HttpBindingCollectionElement section = HttpBindingCollectionElement.GetBindingCollectionElement();

            HttpBindingElement element = null;
            if (section != null && section.Bindings.ContainsKey(configurationName))
            {
                element = section.Bindings[configurationName];
            }

            if (element == null)
            {
                throw Fx.Exception.AsError(
                   new ConfigurationErrorsException(
                       SR.ConfigInvalidBindingConfigurationName(
                           configurationName,
                           CollectionElementName)));
            }

            element.ApplyConfiguration(this);
        }

        private TransportBindingElement GetTransport()
        {
            if (this.security.Mode == HttpBindingSecurityMode.Transport)
            {
                this.security.Transport.ConfigureTransportProtectionAndAuthentication(this.httpsTransportBindingElement);
                return this.httpsTransportBindingElement;
            }
            else if (this.security.Mode == HttpBindingSecurityMode.TransportCredentialOnly)
            {
                this.security.Transport.ConfigureTransportAuthentication(this.httpTransportBindingElement);
                return this.httpTransportBindingElement;
            }

            this.security.Transport.DisableTransportAuthentication(this.httpTransportBindingElement);
            return this.httpTransportBindingElement;
        }

        private void Initialize()
        {
            this.security = new HttpBindingSecurity();
            
            this.httpTransportBindingElement = new HttpTransportBindingElement();
            this.httpTransportBindingElement.ManualAddressing = true;
            
            this.httpsTransportBindingElement = new HttpsTransportBindingElement();
            this.httpsTransportBindingElement.ManualAddressing = true;

            this.httpHandlerBindingElement = new HttpMessageHandlerBindingElement();

            this.httpMessageEncodingBindingElement = new HttpMessageEncodingBindingElement();
        }
    }
}
