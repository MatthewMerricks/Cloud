// <copyright file="WebLogicBinding.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.WebLogic
{
    using System;
    using System.Configuration;
    using System.Globalization;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Configuration;
    using Microsoft.ServiceModel.Interop.Properties;
    using Microsoft.ServiceModel.Interop.WebLogic.Configuration;

    /// <summary>
    /// WebLogic WCF binding implementation
    /// </summary>
    public class WebLogicBinding : InteropBinding, IBindingRuntimePreferences
    {
        private WebLogicReliableSession innerReliableSession;
        private WebLogicSecurity innerSecurity;

        /// <summary>
        /// Initializes a new instance of the WebLogicBinding class
        /// </summary>
        public WebLogicBinding()
            : this(WebLogicSecurityMode.UserNameOverCertificate)
        {
        }

        /// <summary>
        /// Initializes a new instance of the WebLogicBinding class
        /// </summary>
        /// <param name="configName">Binding configuration name</param>
        public WebLogicBinding(string configName)
        {
            if (string.IsNullOrEmpty(configName))
            {
                throw new ArgumentNullException("configName");
            }

            this.innerSecurity = new WebLogicSecurity(this.InnerBinding.Security, WebLogicSecurityMode.UserNameOverCertificate);
            this.innerReliableSession = new WebLogicReliableSession(this.InnerBinding.ReliableSession);

            this.ApplyConfiguration(configName);
        }

        /// <summary>
        /// Initializes a new instance of the WebLogicBinding class
        /// </summary>
        /// <param name="securityMode">Security Mode</param>
        public WebLogicBinding(WebLogicSecurityMode securityMode)
            : this(securityMode, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the WebLogicBinding class
        /// </summary>
        /// <param name="securityMode">Security Mode</param>
        /// <param name="reliableSessionEnabled">Specifies whether reliable session is enabled or not</param>
        public WebLogicBinding(WebLogicSecurityMode securityMode, bool reliableSessionEnabled)
            : base(new WS2007HttpBinding(SecurityMode.Message, reliableSessionEnabled))
        {
            this.innerReliableSession = new WebLogicReliableSession(InnerBinding.ReliableSession);
            this.innerSecurity = new WebLogicSecurity(InnerBinding.Security, securityMode);
            this.innerSecurity.EstablishSecurityContext = true;
        }

        /// <summary>
        /// Gets or sets the security binding element
        /// </summary>
        public WebLogicSecurity Security
        {
            get { return this.innerSecurity; }
            set { this.innerSecurity = value; }
        }

        /// <summary>
        /// Gets or sets the reliable session binding element
        /// </summary>
        public WebLogicReliableSession ReliableSession
        {
            get { return this.innerReliableSession; }
            set { this.innerReliableSession = value; }
        }

        private void ApplyConfiguration(string configurationName)
        {
            BindingsSection bindings = (BindingsSection)ConfigurationManager.GetSection("system.serviceModel/bindings");
            WebLogicBindingCollectionElement section = (WebLogicBindingCollectionElement)bindings["webLogicBinding"];
            WebLogicBindingElement element = section.Bindings[configurationName];
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

