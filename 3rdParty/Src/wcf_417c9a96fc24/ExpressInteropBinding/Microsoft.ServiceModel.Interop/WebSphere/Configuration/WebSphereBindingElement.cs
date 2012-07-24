// <copyright file="WebSphereBindingElement.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.WebSphere.Configuration
{
    using System;
    using System.Configuration;
    using System.ServiceModel.Channels;
    using Microsoft.ServiceModel.Interop.Configuration;

    /// <summary>
    /// Configuration binding element implementation for WebSphere
    /// </summary>
    public class WebSphereBindingElement : InteropBindingElement
    {
        private const string SecurityProperty = "security";
        private const string ReliableSessionProperty = "reliableSession";

        /// <summary>
        /// Initializes a new instance of the WebSphereBindingElement class
        /// </summary>
        /// <param name="configurationName">Binding configuration name</param>
        public WebSphereBindingElement(string configurationName) :
            base(configurationName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the WebSphereBindingElement class
        /// </summary>
        public WebSphereBindingElement()
            : this(null)
        {
        }
        
        /// <summary>
        /// Gets the security configuration element
        /// </summary>
        [ConfigurationProperty(SecurityProperty)]
        public WebSphereSecurityElement Security
        {
            get { return (WebSphereSecurityElement)base[SecurityProperty]; }
        }

        /// <summary>
        /// Gets the reliable session configuration element
        /// </summary>
        [ConfigurationProperty(ReliableSessionProperty)]
        public WebSphereReliableSessionElement ReliableSession
        {
            get { return (WebSphereReliableSessionElement)base[ReliableSessionProperty]; }
        }

        /// <summary>
        /// Gets all the available configuration properties
        /// </summary>
        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                ConfigurationPropertyCollection properties = base.Properties;
                properties.Add(new ConfigurationProperty(SecurityProperty, typeof(WebSphereSecurityElement)));
                properties.Add(new ConfigurationProperty(ReliableSessionProperty, typeof(WebSphereReliableSessionElement)));

                return properties;
            }
        }

        /// <summary>
        /// Gets the binding implementation type
        /// </summary>
        protected override Type BindingElementType
        {
            get { return typeof(WebSphereBinding); }
        }

        /// <summary>
        /// Initializes this configuration element from an existing binding instance
        /// </summary>
        /// <param name="binding">Binding instance</param>
        protected override void InitializeFrom(Binding binding)
        {
            base.InitializeFrom(binding);

            WebSphereBinding webLogicBinding = (WebSphereBinding)binding;

            this.ReliableSession.InitializeFrom(webLogicBinding.ReliableSession);
            this.Security.InitializeFrom(webLogicBinding.Security);
        }

        /// <summary>
        /// Applies this configuration element to an existing binding instance
        /// </summary>
        /// <param name="binding">Binding instance</param>
        protected override void OnApplyConfiguration(Binding binding)
        {
            base.OnApplyConfiguration(binding);

            WebSphereBinding webLogicBinding = (WebSphereBinding)binding;

            this.ReliableSession.ApplyConfiguration(webLogicBinding.ReliableSession);
            this.Security.ApplyConfiguration(webLogicBinding.Security);

            if (this.ReliableSession.Enabled)
            {
                webLogicBinding.Security.EstablishSecurityContext = true;
            }
        }
    }
}

