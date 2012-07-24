// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Configuration
{
    using System;
    using System.Configuration;
    using System.Net.Http;
    using System.ServiceModel;
    using System.ServiceModel.Configuration;
    using System.ServiceModel.Description;
    using System.ServiceModel.Web;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Http.Description;
    using Microsoft.ApplicationServer.ServiceModel;
    using Microsoft.ApplicationServer.ServiceModel.Channels;
    using Microsoft.ApplicationServer.ServiceModel.Configuration;

    /// <summary>
    /// Class that provides an endpoint element for the <see cref="HttpBinding"/> binding.
    /// </summary>
    public sealed class HttpEndpointElement : StandardEndpointElement
    {
        private const string HttpBindingCollectionElementName = "httpBinding";
        private const string NameAsKeyPropertyName = "name";
        private static readonly Type httpClientType = typeof(HttpClient);
        private static readonly Type httpEndpointType = typeof(HttpEndpoint);
        private ConfigurationPropertyCollection properties;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpEndpoint"/> class.
        /// </summary>
        public HttpEndpointElement()
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether the automatic help page will be available.
        /// </summary>
        [ConfigurationProperty(HttpConfigurationStrings.HelpEnabled, DefaultValue = HttpBehavior.DefaultHelpEnabled)]
        public bool HelpEnabled
        {
            get { return (bool)base[HttpConfigurationStrings.HelpEnabled]; }
            set { base[HttpConfigurationStrings.HelpEnabled] = value; }
        }

        /// <summary>
        /// Gets or sets a value specifying how trailing slashes in the <see cref="Uri"/> will be handled.
        /// </summary>
        [ConfigurationProperty(HttpConfigurationStrings.TrailingSlashMode, DefaultValue = HttpBehavior.DefaultTrailingSlashMode)]
        [ServiceModelEnumValidator(typeof(TrailingSlashModeHelper))]
        public TrailingSlashMode TrailingSlashMode
        {
            get { return (TrailingSlashMode)base[HttpConfigurationStrings.TrailingSlashMode]; }
            set { base[HttpConfigurationStrings.TrailingSlashMode] = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="OperationHandlerFactory"/>.
        /// </summary>
        [ConfigurationProperty(HttpConfigurationStrings.OperationHandlerFactory, DefaultValue = "")]
        [StringValidator(MinLength = 0)]
        public string OperationHandlerFactory
        {
            get
            {
                return (string)base[HttpConfigurationStrings.OperationHandlerFactory];
            }

            set
            {
                if (String.IsNullOrWhiteSpace(value))
                {
                    value = String.Empty;
                }

                base[HttpConfigurationStrings.OperationHandlerFactory] = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the hostname is used to reach the service when matching 
        /// the URI.
        /// </summary>
        [ConfigurationProperty(ConfigurationStrings.HostNameComparisonMode, DefaultValue = HttpTransportDefaults.HostNameComparisonMode)]
        [ServiceModelEnumValidator(typeof(HostNameComparisonModeHelper))]
        public HostNameComparisonMode HostNameComparisonMode
        {
            get { return (HostNameComparisonMode)base[ConfigurationStrings.HostNameComparisonMode]; }
            set { base[ConfigurationStrings.HostNameComparisonMode] = value; }
        }

        /// <summary>
        /// Gets or sets the maximum amount of memory allocated for the buffer manager that manages the buffers 
        /// required by endpoints that use this binding.
        /// </summary>
        [ConfigurationProperty(ConfigurationStrings.MaxBufferPoolSize, DefaultValue = TransportDefaults.MaxBufferPoolSize)]
        [LongValidator(MinValue = 0)]
        public long MaxBufferPoolSize
        {
            get { return (long)base[ConfigurationStrings.MaxBufferPoolSize]; }
            set { base[ConfigurationStrings.MaxBufferPoolSize] = value; }
        }

        /// <summary>
        /// Gets or sets the maximum amount of memory that is allocated for use by the manager of the message 
        /// buffers that receive messages from the channel.
        /// </summary>
        [ConfigurationProperty(ConfigurationStrings.MaxBufferSize, DefaultValue = TransportDefaults.MaxBufferSize)]
        [IntegerValidator(MinValue = 1)]
        public int MaxBufferSize
        {
            get { return (int)base[ConfigurationStrings.MaxBufferSize]; }
            set { base[ConfigurationStrings.MaxBufferSize] = value; }
        }

        /// <summary>
        /// Gets or sets the maximum size for a message that can be processed by the binding.
        /// </summary>
        [ConfigurationProperty(ConfigurationStrings.MaxReceivedMessageSize, DefaultValue = TransportDefaults.MaxReceivedMessageSize)]
        [LongValidator(MinValue = 1)]
        public long MaxReceivedMessageSize
        {
            get { return (long)base[ConfigurationStrings.MaxReceivedMessageSize]; }
            set { base[ConfigurationStrings.MaxReceivedMessageSize] = value; }
        }

        /// <summary>
        /// Gets the configuration element that contains the security settings used with this binding.
        /// </summary>
        [ConfigurationProperty(ConfigurationStrings.Security)]
        public HttpBindingSecurityElement Security
        {
            get { return (HttpBindingSecurityElement)base[ConfigurationStrings.Security]; }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the service configured with the binding uses streamed or 
        /// buffered (or both) modes of message transfer.
        /// </summary>
        [ConfigurationProperty(ConfigurationStrings.TransferMode, DefaultValue = HttpBinding.DefaultTransferMode)]
        [ServiceModelEnumValidator(typeof(TransferModeHelper))]
        public TransferMode TransferMode
        {
            get { return (TransferMode)base[ConfigurationStrings.TransferMode]; }
            set { base[ConfigurationStrings.TransferMode] = value; }
        }

        /// <summary>
        /// Gets the type of the endpoint.
        /// </summary>
        protected override Type EndpointType
        {
            get 
            {
                return httpEndpointType;
            }
        }

        /// <summary>
        /// Gets the collection of properties for the current endpoint element.
        /// </summary>
        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                if (this.properties == null)
                {
                    ConfigurationPropertyCollection localProperties = new ConfigurationPropertyCollection();

                    localProperties.Add(new ConfigurationProperty(NameAsKeyPropertyName, typeof(string), string.Empty, null, new StringValidator(0, 0x7fffffff, null), ConfigurationPropertyOptions.IsKey));
                    localProperties.Add(new ConfigurationProperty(HttpConfigurationStrings.HelpEnabled, typeof(bool), HttpBehavior.DefaultHelpEnabled, null, null, ConfigurationPropertyOptions.None));
                    localProperties.Add(new ConfigurationProperty(HttpConfigurationStrings.TrailingSlashMode, typeof(TrailingSlashMode), HttpBehavior.DefaultTrailingSlashMode, null, new ServiceModelEnumValidator(typeof(TrailingSlashModeHelper)), ConfigurationPropertyOptions.None));
                    localProperties.Add(new ConfigurationProperty(HttpConfigurationStrings.OperationHandlerFactory, typeof(string), string.Empty, null, new System.Configuration.StringValidator(0), System.Configuration.ConfigurationPropertyOptions.None));
                    localProperties.Add(new ConfigurationProperty(ConfigurationStrings.HostNameComparisonMode, typeof(HostNameComparisonMode), HttpTransportDefaults.HostNameComparisonMode, null, new ServiceModelEnumValidator(typeof(HostNameComparisonModeHelper)), ConfigurationPropertyOptions.None));
                    localProperties.Add(new ConfigurationProperty(ConfigurationStrings.MaxBufferSize, typeof(int), TransportDefaults.MaxBufferSize, null, new IntegerValidator(1, 2147483647, false), ConfigurationPropertyOptions.None));
                    localProperties.Add(new ConfigurationProperty(ConfigurationStrings.MaxBufferPoolSize, typeof(long), TransportDefaults.MaxBufferPoolSize, null, new LongValidator(0, 9223372036854775807, false), ConfigurationPropertyOptions.None));
                    localProperties.Add(new ConfigurationProperty(ConfigurationStrings.MaxReceivedMessageSize, typeof(long), TransportDefaults.MaxReceivedMessageSize, null, new LongValidator(1, 9223372036854775807, false), ConfigurationPropertyOptions.None));
                    localProperties.Add(new ConfigurationProperty(ConfigurationStrings.Security, typeof(HttpBindingSecurityElement), null, null, null, ConfigurationPropertyOptions.None));
                    localProperties.Add(new ConfigurationProperty(ConfigurationStrings.TransferMode, typeof(TransferMode), HttpBinding.DefaultTransferMode, null, new ServiceModelEnumValidator(typeof(TransferModeHelper)), ConfigurationPropertyOptions.None));
                    this.properties = localProperties;
                }

                return this.properties;
            }
        }

        /// <summary>
        /// When implemente in a derived class, creates a service endpoint from message information
        /// contained in the specified <see cref="ContractDescription"/>.
        /// </summary>
        /// <param name="contractDescription">Information about what is contained in messages
        /// being sent from an endpoint.</param>
        /// <returns>A service endpoint.</returns>
        protected override ServiceEndpoint CreateServiceEndpoint(ContractDescription contractDescription)
        {
            return new HttpEndpoint(contractDescription);
        }

        /// <summary>
        /// When called from a derived class, initializes and verifies the format of the
        /// specified service endpoint element in a service application configuration file.
        /// </summary>
        /// <param name="channelEndpointElement">An endpoint element that defineds the physical transfer
        /// of messages back and forth between the client and the service.</param>
        protected override void OnInitializeAndValidate(ChannelEndpointElement channelEndpointElement)
        {
            throw Fx.Exception.AsError(
                new NotSupportedException(
                        Http.SR.HttpEndpointNotSupported(
                        httpEndpointType.Name,
                        httpClientType.Name)));
        }

        /// <summary>
        /// When called from a derived class, initializes and verifies the format of the
        /// specified service endpoint element in a service application configuration file.
        /// </summary>
        /// <param name="serviceEndpointElement">A service endpoint element that enables clients to find and
        /// communicate with a service.</param>
        protected override void OnInitializeAndValidate(ServiceEndpointElement serviceEndpointElement)
        {
            if (string.IsNullOrEmpty(serviceEndpointElement.Binding))
            {
                serviceEndpointElement.Binding = HttpBindingCollectionElementName;
            }
            else if (!string.Equals(serviceEndpointElement.Binding, HttpBindingCollectionElementName, StringComparison.Ordinal))
            {
                throw Fx.Exception.AsError(
                    new NotSupportedException(
                        Http.SR.HttpEndpointRequiredBinding(
                            typeof(HttpEndpoint).Name,
                            HttpBindingCollectionElementName)));              
            }
        }

        /// <summary>
        /// When called from a derived class, loads the service description information
        /// from the configuration file and applies it to the runtime being constructed.
        /// </summary>
        /// <param name="endpoint">And endpoint that enables clients to find and communicate with a service.</param>
        /// <param name="serviceEndpointElement">A service endpoint element of a service application.</param>
        protected override void OnApplyConfiguration(ServiceEndpoint endpoint, ServiceEndpointElement serviceEndpointElement)
        {
            this.InternalOnApplyConfiguration(endpoint);
        }

        /// <summary>
        /// When called from a derived class, loads the service description information
        /// from the configuration file and applies it to the runtime being constructed.
        /// </summary>
        /// <param name="endpoint">And endpoint that enables clients to find and communicate with a service.</param>
        /// <param name="channelEndpointElement">An endpoint element that defined the physical
        /// transfer of messages ack and forth between the client and the service.</param>
        protected override void OnApplyConfiguration(ServiceEndpoint endpoint, ChannelEndpointElement channelEndpointElement)
        {
            throw Fx.Exception.AsError(
                new NotSupportedException(
                    Http.SR.HttpEndpointNotSupported(
                        httpEndpointType.Name,
                        httpClientType.Name)));
        }

        private void InternalOnApplyConfiguration(ServiceEndpoint endpoint)
        {
            HttpEndpoint httpEndpoint = endpoint as HttpEndpoint;
            Fx.Assert(httpEndpoint != null, "The endpoint should be of type httpEndpoint since this is what was returned with CreateServiceEndpoint().");

            if (this.IsSet(HttpConfigurationStrings.HelpEnabled))
            {
                httpEndpoint.HelpEnabled = this.HelpEnabled;
            }

            if (this.IsSet(HttpConfigurationStrings.TrailingSlashMode))
            {
                httpEndpoint.TrailingSlashMode = this.TrailingSlashMode;
            }

            if (this.IsSet(HttpConfigurationStrings.OperationHandlerFactory))
            {
                httpEndpoint.OperationHandlerFactory = HttpBehaviorElement.GetHttpOperationHandlerFactory(this.OperationHandlerFactory);
            }

            if (this.IsSet(ConfigurationStrings.HostNameComparisonMode))
            {
                httpEndpoint.HostNameComparisonMode = this.HostNameComparisonMode;
            }

            if (this.IsSet(ConfigurationStrings.MaxBufferPoolSize))
            {
                httpEndpoint.MaxBufferPoolSize = this.MaxBufferPoolSize;
            }

            if (this.IsSet(ConfigurationStrings.MaxBufferSize))
            {
                httpEndpoint.MaxBufferSize = this.MaxBufferSize;
            }

            if (this.IsSet(ConfigurationStrings.MaxReceivedMessageSize))
            {
                httpEndpoint.MaxReceivedMessageSize = this.MaxReceivedMessageSize;
            }

            if (this.IsSet(ConfigurationStrings.TransferMode))
            {
                httpEndpoint.TransferMode = this.TransferMode;
            }

            this.Security.ApplyConfiguration(httpEndpoint.Security);
        }

        private bool IsSet(string propertyName)
        {
            return this.ElementInformation.Properties[propertyName].IsModified;
        }
    }
}
