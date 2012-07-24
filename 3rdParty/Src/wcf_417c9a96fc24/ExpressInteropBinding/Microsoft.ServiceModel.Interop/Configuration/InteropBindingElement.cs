// <copyright file="InteropBindingElement.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.Configuration
{
    using System;
    using System.ComponentModel;
    using System.Configuration;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Configuration;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Base class for the binding configuration element that the rest of the service stacks derive
    /// </summary>
    public abstract class InteropBindingElement : StandardBindingElement
    {
        private const string SecurityProperty = "security";
        private const string BypassProxyOnLocalProperty = "bypassProxyOnLocal";
        private const string HostNameComparisonModeProperty = "hostNameComparisonMode";
        private const string MaxBufferPoolSizeProperty = "maxBufferPoolSize";
        private const string MaxReceivedMessageSizeProperty = "maxReceivedMessageSize";
        private const string MessageEncodingProperty = "messageEncoding";
        private const string ProxyAddressProperty = "proxyAddress";
        private const string ReaderQuotasProperty = "readerQuotas";
        private const string TextEncodingProperty = "textEncoding";
        private const string UseDefaultWebProxyProperty = "useDefaultWebProxy";
        private const string ReliableSessionProperty = "reliableSession";

        /// <summary>
        /// Initializes a new instance of the InteropBindingElement class
        /// </summary>
        /// <param name="configurationName">Binding configuration name</param>
        protected InteropBindingElement(string configurationName) :
            base(configurationName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InteropBindingElement class
        /// </summary>
        protected InteropBindingElement()
            : this(null)
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether to bypass the proxy server for local addresses
        /// </summary>
        [ConfigurationProperty(BypassProxyOnLocalProperty, DefaultValue = false)]
        public bool BypassProxyOnLocal
        {
            get { return (bool)base[BypassProxyOnLocalProperty]; }
            set { base[BypassProxyOnLocalProperty] = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating how the host name should be used in URI comparisons when dispatching an incoming message to a service endpoint
        /// </summary>
        [ConfigurationProperty(HostNameComparisonModeProperty, DefaultValue = HostNameComparisonMode.StrongWildcard)]
        public HostNameComparisonMode HostNameComparisonMode
        {
            get { return (HostNameComparisonMode)base[HostNameComparisonModeProperty]; }
            set { base[HostNameComparisonModeProperty] = value; }
        }

        /// <summary>
        /// Gets or sets the maximum size of any buffer pools used by the transport
        /// </summary>
        [LongValidator(MinValue = 0L), ConfigurationProperty(MaxBufferPoolSizeProperty, DefaultValue = 0x80000L)]
        public long MaxBufferPoolSize
        {
            get { return (long)base[MaxBufferPoolSizeProperty]; }
            set { base[MaxBufferPoolSizeProperty] = value; }
        }

        /// <summary>
        /// Gets or sets the maximum allowable message size that can be received
        /// </summary>
        [LongValidator(MinValue = 1L), ConfigurationProperty(MaxReceivedMessageSizeProperty, DefaultValue = 0x10000L)]
        public long MaxReceivedMessageSize
        {
            get { return (long)base[MaxReceivedMessageSizeProperty]; }
            set { base[MaxReceivedMessageSizeProperty] = value; }
        }

        /// <summary>
        /// Gets or sets the message encoding
        /// </summary>
        [ConfigurationProperty(MessageEncodingProperty, DefaultValue = WSMessageEncoding.Text)]
        public WSMessageEncoding MessageEncoding
        {
            get { return (WSMessageEncoding)base[MessageEncodingProperty]; }
            set { base[MessageEncodingProperty] = value; }
        }

        /// <summary>
        /// Gets or sets a URI that contains the address of the proxy to use for HTTP requests
        /// </summary>
        [ConfigurationProperty(ProxyAddressProperty, DefaultValue = null)]
        public Uri ProxyAddress
        {
            get { return (Uri)base[ProxyAddressProperty]; }
            set { base[ProxyAddressProperty] = value; }
        }

        /// <summary>
        /// Gets the xml reader quotas
        /// </summary>
        [ConfigurationProperty(ReaderQuotasProperty)]
        public XmlDictionaryReaderQuotasElement ReaderQuotas
        {
            get { return (XmlDictionaryReaderQuotasElement)base[ReaderQuotasProperty]; }
        }

        /// <summary>
        /// Gets or sets the text encoding
        /// </summary>
        [ConfigurationProperty(TextEncodingProperty, DefaultValue = "utf-8"), TypeConverter(typeof(EncodingConverter))]
        public Encoding TextEncoding
        {
            get { return (Encoding)base[TextEncodingProperty]; }
            set { base[TextEncodingProperty] = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the machine-wide proxy settings are used rather than the user specific settings
        /// </summary>
        [ConfigurationProperty(UseDefaultWebProxyProperty, DefaultValue = true)]
        public bool UseDefaultWebProxy
        {
            get { return (bool)base[UseDefaultWebProxyProperty]; }
            set { base[UseDefaultWebProxyProperty] = value; }
        }

        /// <summary>
        /// Gets all the available configuration properties
        /// </summary>
        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                ConfigurationPropertyCollection properties = base.Properties;
                properties.Add(new ConfigurationProperty(BypassProxyOnLocalProperty, typeof(bool), false));
                properties.Add(new ConfigurationProperty(HostNameComparisonModeProperty, typeof(System.ServiceModel.HostNameComparisonMode), HostNameComparisonMode.StrongWildcard));
                properties.Add(new ConfigurationProperty(MaxBufferPoolSizeProperty, typeof(long), 0x80000L, null, new LongValidator(0L, long.MaxValue), ConfigurationPropertyOptions.None));
                properties.Add(new ConfigurationProperty(MaxReceivedMessageSizeProperty, typeof(long), 0x10000L, null, new LongValidator(1L, long.MaxValue), ConfigurationPropertyOptions.None));
                properties.Add(new ConfigurationProperty(ProxyAddressProperty, typeof(System.Uri), null));
                properties.Add(new ConfigurationProperty(UseDefaultWebProxyProperty, typeof(bool), true));
                properties.Add(new ConfigurationProperty(ReaderQuotasProperty, typeof(XmlDictionaryReaderQuotasElement)));
                properties.Add(new ConfigurationProperty(TextEncodingProperty, typeof(Encoding), "utf-8", new EncodingConverter(), null, ConfigurationPropertyOptions.None));
                properties.Add(new ConfigurationProperty(MessageEncodingProperty, typeof(WSMessageEncoding), WSMessageEncoding.Text));

                return properties;
            }
        }

        /// <summary>
        /// Initializes this configuration element from an existing and valid binding instance
        /// </summary>
        /// <param name="binding">Binding instance</param>
        protected override void InitializeFrom(Binding binding)
        {
            base.InitializeFrom(binding);

            InteropBinding webLogicBinding = (InteropBinding)binding;

            this.BypassProxyOnLocal = webLogicBinding.BypassProxyOnLocal;
            this.CloseTimeout = webLogicBinding.CloseTimeout;
            this.HostNameComparisonMode = webLogicBinding.HostNameComparisonMode;
            this.MaxBufferPoolSize = webLogicBinding.MaxBufferPoolSize;
            this.MaxReceivedMessageSize = webLogicBinding.MaxReceivedMessageSize;
            this.MessageEncoding = webLogicBinding.MessageEncoding;
            this.OpenTimeout = webLogicBinding.OpenTimeout;
            this.ProxyAddress = webLogicBinding.ProxyAddress;
            this.ReceiveTimeout = webLogicBinding.ReceiveTimeout;
            this.SendTimeout = webLogicBinding.SendTimeout;
            this.TextEncoding = webLogicBinding.TextEncoding;
            this.UseDefaultWebProxy = webLogicBinding.UseDefaultWebProxy;

            this.InitializeReaderQuotasFrom(webLogicBinding.ReaderQuotas);
        }

        /// <summary>
        /// Applies this configuration element to an existing binding instance
        /// </summary>
        /// <param name="binding">Binding instance</param>
        protected override void OnApplyConfiguration(Binding binding)
        {
            InteropBinding webLogicBinding = (InteropBinding)binding;
            webLogicBinding.BypassProxyOnLocal = this.BypassProxyOnLocal;
            webLogicBinding.CloseTimeout = this.CloseTimeout;
            webLogicBinding.HostNameComparisonMode = this.HostNameComparisonMode;
            webLogicBinding.MaxBufferPoolSize = this.MaxBufferPoolSize;
            webLogicBinding.MaxReceivedMessageSize = this.MaxReceivedMessageSize;
            webLogicBinding.MessageEncoding = this.MessageEncoding;
            webLogicBinding.OpenTimeout = this.OpenTimeout;
            webLogicBinding.ProxyAddress = this.ProxyAddress;
            webLogicBinding.ReceiveTimeout = this.ReceiveTimeout;
            webLogicBinding.SendTimeout = this.SendTimeout;
            webLogicBinding.TextEncoding = this.TextEncoding;
            webLogicBinding.UseDefaultWebProxy = this.UseDefaultWebProxy;

            this.ApplyReaderQuotasConfiguration(webLogicBinding.ReaderQuotas);
        }

        private void InitializeReaderQuotasFrom(XmlDictionaryReaderQuotas readerQuotas)
        {
            if (readerQuotas.MaxArrayLength > 0)
            {
                this.ReaderQuotas.MaxArrayLength = readerQuotas.MaxArrayLength;
            }

            if (readerQuotas.MaxBytesPerRead > 0)
            {
                this.ReaderQuotas.MaxBytesPerRead = readerQuotas.MaxBytesPerRead;
            }

            if (readerQuotas.MaxDepth > 0)
            {
                this.ReaderQuotas.MaxDepth = readerQuotas.MaxDepth;
            }

            if (readerQuotas.MaxNameTableCharCount > 0)
            {
                this.ReaderQuotas.MaxNameTableCharCount = readerQuotas.MaxNameTableCharCount;
            }

            if (readerQuotas.MaxStringContentLength > 0)
            {
                this.ReaderQuotas.MaxStringContentLength = readerQuotas.MaxStringContentLength;
            }
        }

        private void ApplyReaderQuotasConfiguration(XmlDictionaryReaderQuotas readerQuotas)
        {
            if (this.ReaderQuotas.MaxArrayLength > 0)
            {
                readerQuotas.MaxArrayLength = this.ReaderQuotas.MaxArrayLength;
            }

            if (this.ReaderQuotas.MaxBytesPerRead > 0)
            {
                readerQuotas.MaxBytesPerRead = this.ReaderQuotas.MaxBytesPerRead;
            }

            if (this.ReaderQuotas.MaxDepth > 0)
            {
                readerQuotas.MaxDepth = this.ReaderQuotas.MaxDepth;
            }

            if (this.ReaderQuotas.MaxNameTableCharCount > 0)
            {
                readerQuotas.MaxNameTableCharCount = this.ReaderQuotas.MaxNameTableCharCount;
            }

            if (this.ReaderQuotas.MaxStringContentLength > 0)
            {
                readerQuotas.MaxStringContentLength = this.ReaderQuotas.MaxStringContentLength;
            }
        }
    }
}

