// <copyright file="MetroBinding.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.Metro
{
    using System;
    using System.Configuration;
    using System.Globalization;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Configuration;
    
    using Microsoft.ServiceModel.Interop.Metro.Configuration;
    using Microsoft.ServiceModel.Interop.Properties;

    /// <summary>
    /// WCF Binding implementation for Metro
    /// </summary>
    public class MetroBinding : InteropBinding, IBindingRuntimePreferences
    {
        private MetroReliableSession innerReliableSession;
        private MetroSecurity innerSecurity;

        /// <summary>
        /// Initializes a new instance of the MetroBinding class
        /// </summary>
        public MetroBinding()
            : this(MetroSecurityMode.UserNameOverCertificate)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MetroBinding class
        /// </summary>
        /// <param name="configName">Binding configuration name</param>
        public MetroBinding(string configName)
        {
            if (string.IsNullOrEmpty(configName))
            {
                throw new ArgumentNullException("configName");
            }

            this.innerSecurity = new MetroSecurity(this.InnerBinding.Security, MetroSecurityMode.UserNameOverCertificate);
            this.innerReliableSession = new MetroReliableSession(this.InnerBinding.ReliableSession);

            this.ApplyConfiguration(configName);
        }

        /// <summary>
        /// Initializes a new instance of the MetroBinding class
        /// </summary>
        /// <param name="securityMode">Security Mode</param>
        public MetroBinding(MetroSecurityMode securityMode)
            : this(securityMode, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MetroBinding class
        /// </summary>
        /// <param name="securityMode">Security mode</param>
        /// <param name="reliableSessionEnabled">A boolean value for specifing whehter reliable session is enabled</param>
        public MetroBinding(MetroSecurityMode securityMode, bool reliableSessionEnabled)
            : base(new WS2007HttpBinding(SecurityMode.Message, reliableSessionEnabled))
        {
            this.innerReliableSession = new MetroReliableSession(InnerBinding.ReliableSession);
            this.innerSecurity = new MetroSecurity(InnerBinding.Security, securityMode);
            this.innerSecurity.EstablishSecurityContext = true;
        }

        /// <summary>
        /// Gets or sets the security binding element
        /// </summary>
        public MetroSecurity Security
        {
            get { return this.innerSecurity; }
            set { this.innerSecurity = value; }
        }

        /// <summary>
        /// Gets or sets the reliable session binding element
        /// </summary>
        public MetroReliableSession ReliableSession
        {
            get { return this.innerReliableSession; }
            set { this.innerReliableSession = value; }
        }

        private void ApplyConfiguration(string configurationName)
        {
            BindingsSection bindings = (BindingsSection)ConfigurationManager.GetSection("system.serviceModel/bindings");
            MetroBindingCollectionElement section = (MetroBindingCollectionElement)bindings["metroBinding"];
            MetroBindingElement element = section.Bindings[configurationName];
            if (element == null)
            {
                throw new System.Configuration.ConfigurationErrorsException(
                    string.Format(CultureInfo.CurrentCulture, Strings.Binding_Not_Found, configurationName, section.BindingName));
            }
            else
            {
                element.ApplyConfiguration(this);
            }
        }
    }
}

