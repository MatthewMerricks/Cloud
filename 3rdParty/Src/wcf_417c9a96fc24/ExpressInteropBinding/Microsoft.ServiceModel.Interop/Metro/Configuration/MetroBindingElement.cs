// <copyright file="MetroBindingElement.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.Metro.Configuration
{
    using System;
    using System.Configuration;
    using System.ServiceModel.Channels;
    using Microsoft.ServiceModel.Interop.Configuration;

    /// <summary>
    /// Binding element implementation for Metro
    /// </summary>
    public class MetroBindingElement : InteropBindingElement
    {
        private const string SecurityProperty = "security";
        private const string ReliableSessionProperty = "reliableSession";

        /// <summary>
        /// Initializes a new instance of the MetroBindingElement class
        /// </summary>
        /// <param name="configurationName">Binding configuration name</param>
        public MetroBindingElement(string configurationName) :
            base(configurationName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MetroBindingElement class
        /// </summary>
        public MetroBindingElement()
            : this(null)
        {
        }
        
        /// <summary>
        /// Gets the Security binding element
        /// </summary>
        [ConfigurationProperty(SecurityProperty)]
        public MetroSecurityElement Security
        {
            get { return (MetroSecurityElement)base[SecurityProperty]; }
        }

        /// <summary>
        /// Gets the reliable session binding element
        /// </summary>
        [ConfigurationProperty(ReliableSessionProperty)]
        public MetroReliableSessionElement ReliableSession
        {
            get { return (MetroReliableSessionElement)base[ReliableSessionProperty]; }
        }

        /// <summary>
        /// Gets all the available configuration properties
        /// </summary>
        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                ConfigurationPropertyCollection properties = base.Properties;
                properties.Add(new ConfigurationProperty(SecurityProperty, typeof(MetroSecurityElement)));
                properties.Add(new ConfigurationProperty(ReliableSessionProperty, typeof(MetroReliableSessionElement)));

                return properties;
            }
        }

        /// <summary>
        /// Gets the binding element implementation type
        /// </summary>
        protected override Type BindingElementType
        {
            get { return typeof(MetroBinding); }
        }

        /// <summary>
        /// Initializes this configuration element from an existing binding instance 
        /// </summary>
        /// <param name="binding">Binding instance</param>
        protected override void InitializeFrom(Binding binding)
        {
            base.InitializeFrom(binding);

            MetroBinding metroBinding = (MetroBinding)binding;

            this.ReliableSession.InitializeFrom(metroBinding.ReliableSession);
            this.Security.InitializeFrom(metroBinding.Security);
        }

        /// <summary>
        /// Applies this configuration element to an existing binding instance
        /// </summary>
        /// <param name="binding">Binding instance</param>
        protected override void OnApplyConfiguration(Binding binding)
        {
            base.OnApplyConfiguration(binding);

            MetroBinding metroBinding = (MetroBinding)binding;

            this.ReliableSession.ApplyConfiguration(metroBinding.ReliableSession);
            this.Security.ApplyConfiguration(metroBinding.Security);

            if (this.ReliableSession.Enabled)
            {
                metroBinding.Security.EstablishSecurityContext = true;
            }
        }
    }
}

