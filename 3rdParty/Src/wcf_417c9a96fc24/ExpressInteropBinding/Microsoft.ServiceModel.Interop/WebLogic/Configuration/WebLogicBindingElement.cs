// <copyright file="WebLogicBindingElement.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.WebLogic.Configuration
{
    using System;
    using System.Configuration;
    using System.ServiceModel.Channels;
    using Microsoft.ServiceModel.Interop.Configuration;

    /// <summary>
    /// Configuration binding element for WebLogic
    /// </summary>
    public class WebLogicBindingElement : InteropBindingElement
    {
        private const string SecurityProperty = "security";
        private const string ReliableSessionProperty = "reliableSession";

        /// <summary>
        /// Initializes a new instance of the WebLogicBindingElement class
        /// </summary>
        /// <param name="configurationName">Binding configuration name</param>
        public WebLogicBindingElement(string configurationName) :
            base(configurationName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the WebLogicBindingElement class
        /// </summary>
        public WebLogicBindingElement()
            : this(null)
        {
        }

        /// <summary>
        /// Gets the security configuration element
        /// </summary>
        [ConfigurationProperty(SecurityProperty)]
        public WebLogicSecurityElement Security
        {
            get { return (WebLogicSecurityElement)base[SecurityProperty]; }
        }

        /// <summary>
        /// Gets the reliable session configuration element
        /// </summary>
        [ConfigurationProperty(ReliableSessionProperty)]
        public WebLogicReliableSessionElement ReliableSession
        {
            get { return (WebLogicReliableSessionElement)base[ReliableSessionProperty]; }
        }

        /// <summary>
        /// Gets the binding implementation type
        /// </summary>
        protected override Type BindingElementType
        {
            get { return typeof(WebLogicBinding); }
        }

        /// <summary>
        /// Gets all the available configuration properties
        /// </summary>
        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                ConfigurationPropertyCollection properties = base.Properties;
                properties.Add(new ConfigurationProperty(SecurityProperty, typeof(WebLogicSecurityElement)));
                properties.Add(new ConfigurationProperty(ReliableSessionProperty, typeof(WebLogicReliableSessionElement)));

                return properties;
            }
        }

        /// <summary>
        /// Initializes this configuration element from an existing binding instance
        /// </summary>
        /// <param name="binding">Binding instance</param>
        protected override void InitializeFrom(Binding binding)
        {
            base.InitializeFrom(binding);

            WebLogicBinding webLogicBinding = (WebLogicBinding)binding;

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

            WebLogicBinding webLogicBinding = (WebLogicBinding)binding;

            this.ReliableSession.ApplyConfiguration(webLogicBinding.ReliableSession);
            this.Security.ApplyConfiguration(webLogicBinding.Security);

            if (this.ReliableSession.Enabled)
            {
                webLogicBinding.Security.EstablishSecurityContext = true;
            }
        }
    }
}

