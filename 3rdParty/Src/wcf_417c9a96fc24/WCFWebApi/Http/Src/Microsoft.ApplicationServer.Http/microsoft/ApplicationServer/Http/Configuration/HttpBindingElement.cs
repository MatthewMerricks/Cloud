// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Configuration
{
    using System;
    using System.Configuration;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Configuration;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.ServiceModel;
    using Microsoft.ApplicationServer.ServiceModel.Channels;
    using Microsoft.ApplicationServer.ServiceModel.Configuration;

    /// <summary>
    /// A configuration element for the <see cref="Microsoft.ApplicationServer.Http.HttpBinding">HttpBinding</see>
    /// binding. 
    /// </summary>
    public class HttpBindingElement : StandardBindingElement
    {
        private ConfigurationPropertyCollection properties;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpBindingElement"/> class and specifies 
        /// the name of the element. 
        /// </summary>
        /// <param name="name">The name that is used for this binding configuration element</param>
        public HttpBindingElement(string name)
            : base(name)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpBindingElement"/> class.
        /// </summary>
        public HttpBindingElement()
            : this(null)
        {
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
        [ConfigurationProperty(ConfigurationStrings.TransferMode, DefaultValue = TransferMode.Buffered)]
        [ServiceModelEnumValidator(typeof(TransferModeHelper))]
        public TransferMode TransferMode
        {
            get { return (TransferMode)base[ConfigurationStrings.TransferMode]; }
            set { base[ConfigurationStrings.TransferMode] = value; }
        }

        /// <summary>
        /// Gets the <see cref="System.Type">Type</see> of binding that this configuration element represents. 
        /// (Overrides <see cref="System.ServiceModel.Configuration.StandardBindingElement.BindingElementType">
        /// StandardBindingElement.BindingElementType</see>.)
        /// </summary>
        protected override Type BindingElementType
        {
            get 
            { 
                return typeof(HttpBinding); 
            }
        }

        /// <summary>
        /// Gets the collection of properties. (Inherited from 
        /// <see cref="System.Configuration.ConfigurationElement">ConfigurationElement</see>.)
        /// </summary>
        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                if (this.properties == null)
                {
                    ConfigurationPropertyCollection baseProperties = base.Properties;

                    baseProperties.Add(new ConfigurationProperty(ConfigurationStrings.HostNameComparisonMode, typeof(HostNameComparisonMode), HostNameComparisonMode.StrongWildcard, null, new ServiceModelEnumValidator(typeof(HostNameComparisonModeHelper)), ConfigurationPropertyOptions.None));
                    baseProperties.Add(new ConfigurationProperty(ConfigurationStrings.MaxBufferSize, typeof(int), 65536, null, new IntegerValidator(1, 2147483647, false), ConfigurationPropertyOptions.None));
                    baseProperties.Add(new ConfigurationProperty(ConfigurationStrings.MaxBufferPoolSize, typeof(long), (long)524288, null, new LongValidator(0, 9223372036854775807, false), ConfigurationPropertyOptions.None));
                    baseProperties.Add(new ConfigurationProperty(ConfigurationStrings.MaxReceivedMessageSize, typeof(long), (long)65536, null, new LongValidator(1, 9223372036854775807, false), ConfigurationPropertyOptions.None));
                    baseProperties.Add(new ConfigurationProperty(ConfigurationStrings.Security, typeof(HttpBindingSecurityElement), null, null, null, ConfigurationPropertyOptions.None));
                    
                    baseProperties.Add(new ConfigurationProperty(ConfigurationStrings.TransferMode, typeof(TransferMode), TransferMode.Buffered, null, new ServiceModelEnumValidator(typeof(TransferModeHelper)), ConfigurationPropertyOptions.None));
                    this.properties = baseProperties;
                }

                return this.properties;
            }
        }

        /// <summary>
        /// Initializes the <see cref="HttpBindingElement"/> from an 
        /// <see cref="Microsoft.ApplicationServer.Http.HttpBinding">HttpBinding</see> instance.
        /// </summary>
        /// <param name="binding">
        /// The <see cref="Microsoft.ApplicationServer.Http.HttpBinding">HttpBinding</see> instance from which
        /// the <see cref="HttpBindingElement"/> will be initialized.
        /// </param>
        protected override void InitializeFrom(Binding binding)
        {
            base.InitializeFrom(binding);
            HttpBinding httpBinding = (HttpBinding)binding;

            SetPropertyValueIfNotDefaultValue(ConfigurationStrings.HostNameComparisonMode, httpBinding.HostNameComparisonMode);
            SetPropertyValueIfNotDefaultValue(ConfigurationStrings.MaxBufferSize, httpBinding.MaxBufferSize);
            SetPropertyValueIfNotDefaultValue(ConfigurationStrings.MaxBufferPoolSize, httpBinding.MaxBufferPoolSize);
            SetPropertyValueIfNotDefaultValue(ConfigurationStrings.MaxReceivedMessageSize, httpBinding.MaxReceivedMessageSize);
            SetPropertyValueIfNotDefaultValue(ConfigurationStrings.TransferMode, httpBinding.TransferMode);

            this.Security.InitializeFrom(httpBinding.Security);
        }

        /// <summary>
        /// Applies the configuration of the the <see cref="HttpBindingElement"/> to the given
        /// <see cref="Microsoft.ApplicationServer.Http.HttpBinding">HttpBinding</see> instance.
        /// </summary>
        /// <param name="binding">The <see cref="Microsoft.ApplicationServer.Http.HttpBinding">HttpBinding</see> 
        /// instance to which the <see cref="HttpBindingElement"/> configuration will be applied.
        /// </param>
        protected override void OnApplyConfiguration(Binding binding)
        {
            HttpBinding httpBinding = (HttpBinding)binding;

            httpBinding.HostNameComparisonMode = this.HostNameComparisonMode;
            httpBinding.MaxBufferPoolSize = this.MaxBufferPoolSize;
            httpBinding.MaxReceivedMessageSize = this.MaxReceivedMessageSize;
            httpBinding.TransferMode = this.TransferMode;
            
            PropertyInformationCollection propertyInfo = this.ElementInformation.Properties;
            if (propertyInfo[ConfigurationStrings.MaxBufferSize].ValueOrigin != PropertyValueOrigin.Default)
            {
                httpBinding.MaxBufferSize = this.MaxBufferSize;
            }

            this.Security.ApplyConfiguration(httpBinding.Security);
        }

        /// <summary>
        /// Used by InitializeFrom() pattern to avoid writing default values to generated .config files.
        /// </summary>
        /// <typeparam name="T">The type of the property value.</typeparam>
        /// <param name="propertyName">ConfigurationProperty.Name for the configuration property to set</param>
        /// <param name="value">Value to set</param>
        protected void SetPropertyValueIfNotDefaultValue<T>(string propertyName, T value)
        {
            ConfigurationProperty configurationProperty = this.Properties[propertyName];
            Fx.Assert(configurationProperty != null, "Parameter 'propertyName' should be the name of a configuration property of type T");
            Fx.Assert(configurationProperty.Type.IsAssignableFrom(typeof(T)), "Parameter 'propertyName' should be the name of a configuration property of type T");

            if (!object.Equals(value, configurationProperty.DefaultValue))
            {
                SetPropertyValue(configurationProperty, value, /*ignoreLocks = */ false);
            }
        }
    }
}
