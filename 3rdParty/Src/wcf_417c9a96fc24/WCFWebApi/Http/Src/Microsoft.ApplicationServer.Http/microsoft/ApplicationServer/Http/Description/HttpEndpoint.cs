// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Description
{
    using System;
    using System.ComponentModel;
    using System.ServiceModel;
    using System.ServiceModel.Description;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Http.Channels;

    /// <summary>
    /// Class that provides a <see cref="ServiceEndpoint"/> for the <see cref="HttpBinding"/> binding.
    /// </summary>
    public class HttpEndpoint : ServiceEndpoint
    {
        internal const string Kind = "httpEndpoint";

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpEndpoint"/> class.
        /// </summary>
        /// <param name="contract">The <see cref="ContractDescription"/> for the service endpoint.</param>
        public HttpEndpoint(ContractDescription contract)
            : this(contract, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpEndpoint"/> class.
        /// </summary>
        /// <param name="contract">The <see cref="ContractDescription"/> for the service endpoint.</param>
        /// <param name="address">The <see cref="EndpointAddress"/> for the service endpoint.</param>
        public HttpEndpoint(ContractDescription contract, EndpointAddress address)
            : base(contract, new HttpBinding(), address)
        {
            this.Behaviors.Add(new HttpBehavior());
        }

        /// <summary>
        /// Gets or sets the default <see cref="HttpMessageHandlerFactory"/> to use for
        /// the <see cref="HttpEndpoint"/>.
        /// </summary>
        public HttpMessageHandlerFactory MessageHandlerFactory
        {
            get { return this.HttpBinding.MessageHandlerFactory; }
            set { this.HttpBinding.MessageHandlerFactory = value; }
        }

        /// <summary>
        /// Gets or sets the default <see cref="HttpOperationHandlerFactory"/> to use for
        /// the <see cref="HttpEndpoint"/>.
        /// </summary>
        [DefaultValue(null)]
        public HttpOperationHandlerFactory OperationHandlerFactory
        {
            get { return this.HttpBehavior.OperationHandlerFactory; }
            set { this.HttpBehavior.OperationHandlerFactory = value; }
        }

        /// <summary>
        /// Gets or sets a value specifying how the host name should be use in <see cref="Uri"/>
        /// comparisons when dispatching an incoming message to a service endpoint.
        /// </summary>
        public HostNameComparisonMode HostNameComparisonMode
        {
            get { return this.HttpBinding.HostNameComparisonMode; }
            set { this.HttpBinding.HostNameComparisonMode = value; }
        }

        /// <summary>
        /// Gets or sets the maximum amount of memory that is allocated for use by the manager
        /// of the message buffers that receive messages from the endpoint.
        /// </summary>
        public long MaxBufferPoolSize
        {
            get { return this.HttpBinding.MaxBufferPoolSize; }
            set { this.HttpBinding.MaxBufferPoolSize = value; }
        }

        /// <summary>
        /// Gets or sets the maximum amount of memory that is allocated for use by the manager of the message 
        /// buffers that receive messages from the channel.
        /// </summary>
        public int MaxBufferSize
        {
            get { return this.HttpBinding.MaxBufferSize; }
            set { this.HttpBinding.MaxBufferSize = value; }
        }

        /// <summary>
        /// Gets or sets the maximum size for a message that can be processed by the endpoint.
        /// </summary>
        public long MaxReceivedMessageSize
        {
            get { return this.HttpBinding.MaxReceivedMessageSize; }
            set { this.HttpBinding.MaxReceivedMessageSize = value; }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the service configured with the endpoint uses streamed or 
        /// buffered (or both) modes of message transfer.
        /// </summary>
        public TransferMode TransferMode
        {
            get { return this.HttpBinding.TransferMode; }
            set { this.HttpBinding.TransferMode = value; }
        }

        /// <summary>
        /// Gets the <see cref="HttpBindingSecurity"/> for this endpoint.
        /// </summary>
        public HttpBindingSecurity Security
        {
            get { return this.HttpBinding.Security; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether an automatic help page will be generated.
        /// </summary>
        public bool HelpEnabled 
        {
            get { return this.HttpBehavior.HelpEnabled; }
            set { this.HttpBehavior.HelpEnabled = value; }
        }

        /// <summary>
        /// Gets or sets a value specifying how trailing slashes on <see cref="Uri"/>s should be treated.
        /// </summary>
        public TrailingSlashMode TrailingSlashMode
        {
            get { return this.HttpBehavior.TrailingSlashMode; }
            set { this.HttpBehavior.TrailingSlashMode = value; }
        }

        private HttpBinding HttpBinding
        {
            get
            {
                HttpBinding localHttpBinding = this.Binding as HttpBinding;
                if (localHttpBinding == null)
                {
                    throw Fx.Exception.AsError(
                        new InvalidOperationException(
                            Http.SR.HttpEndpointRequiresHttpBinding(
                                typeof(HttpEndpoint).Name,
                                typeof(HttpBinding).Name)));
                }

                return localHttpBinding;
            }
        }

        private HttpBehavior HttpBehavior
        {
            get
            {
                HttpBehavior localHttpBehavior = this.Behaviors.Find<HttpBehavior>();
                if (localHttpBehavior == null)
                {
                    throw Fx.Exception.AsError(
                        new InvalidOperationException(
                            Http.SR.HttpBehaviorNotFoundWithEndpoint(
                                typeof(HttpEndpoint).Name, 
                                typeof(HttpBehavior).Name)));
                }

                return localHttpBehavior;
            }
        }
    }
}
