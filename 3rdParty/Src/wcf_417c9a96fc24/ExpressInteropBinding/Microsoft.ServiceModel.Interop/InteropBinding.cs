// <copyright file="InteropBinding.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Base class for the WCF interop bindings
    /// </summary>
    public abstract class InteropBinding : Binding, IBindingRuntimePreferences
    {
        private WSHttpBinding innerBinding;

        /// <summary>
        /// Initializes a new instance of the InteropBinding class
        /// </summary>
        protected InteropBinding()
        {
            this.innerBinding = new WS2007HttpBinding();
        }

        /// <summary>
        /// Initializes a new instance of the InteropBinding class
        /// </summary>
        /// <param name="innerBinding">Existing WSHttp binding instance</param>
        protected InteropBinding(WSHttpBinding innerBinding)
        {
            this.innerBinding = innerBinding;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to bypass the proxy server for local addresses
        /// </summary>
        public bool BypassProxyOnLocal
        {
            get { return this.innerBinding.BypassProxyOnLocal; }
            set { this.innerBinding.BypassProxyOnLocal = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating how the host name should be used in URI comparisons when dispatching an incoming message to a service endpoint
        /// </summary>
        public HostNameComparisonMode HostNameComparisonMode
        {
            get { return this.innerBinding.HostNameComparisonMode; }
            set { this.innerBinding.HostNameComparisonMode = value; }
        }

        /// <summary>
        /// Gets or sets the maximum size of any buffer pools used by the transport
        /// </summary>
        public long MaxBufferPoolSize
        {
            get { return this.innerBinding.MaxBufferPoolSize; }
            set { this.innerBinding.MaxBufferPoolSize = value; }
        }

        /// <summary>
        /// Gets or sets the maximum allowable message size that can be received
        /// </summary>
        public long MaxReceivedMessageSize
        {
            get { return this.innerBinding.MaxReceivedMessageSize; }
            set { this.innerBinding.MaxReceivedMessageSize = value; }
        }

        /// <summary>
        /// Gets or sets the message encoding
        /// </summary>
        public WSMessageEncoding MessageEncoding
        {
            get { return this.innerBinding.MessageEncoding; }
            set { this.innerBinding.MessageEncoding = value; }
        }

        /// <summary>
        /// Gets or sets a URI that contains the address of the proxy to use for HTTP requests
        /// </summary>
        public Uri ProxyAddress
        {
            get { return this.innerBinding.ProxyAddress; }
            set { this.innerBinding.ProxyAddress = value; }
        }

        /// <summary>
        /// Gets or sets the xml reader quotas
        /// </summary>
        public XmlDictionaryReaderQuotas ReaderQuotas
        {
            get { return this.innerBinding.ReaderQuotas; }
            set { this.innerBinding.ReaderQuotas = value; }
        }

        /// <summary>
        /// Gets or sets the text encoding
        /// </summary>
        public Encoding TextEncoding
        {
            get { return this.innerBinding.TextEncoding; }
            set { this.innerBinding.TextEncoding = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the machine-wide proxy settings are used rather than the user specific settings
        /// </summary>
        public bool UseDefaultWebProxy
        {
            get { return this.innerBinding.UseDefaultWebProxy; }
            set { this.innerBinding.UseDefaultWebProxy = value; }
        }

        /// <summary>
        /// Gets the transport scheme
        /// </summary>
        public override string Scheme
        {
            get { return this.innerBinding.Scheme; }
        }

        /// <summary>
        /// Gets a value indicating whether incoming requests can be handled more efficiently synchronously
        /// </summary>
        public bool ReceiveSynchronously
        {
            get { return ((IBindingRuntimePreferences)this.innerBinding).ReceiveSynchronously; }
        }

        /// <summary>
        /// Gets the inner WCF binding
        /// </summary>
        protected WSHttpBinding InnerBinding
        {
            get { return this.innerBinding; }
        }

        /// <summary>
        /// Creates a list of binding elements for initializing the WCF channel
        /// </summary>
        /// <returns>A collection with binding elements for initializing the WCF channel stack</returns>
        public override BindingElementCollection CreateBindingElements()
        {
            return this.innerBinding.CreateBindingElements();
        }
    }
}

