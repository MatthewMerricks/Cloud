// <copyright file="WebSphereBinding.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.ServiceModel.Interop.WebSphere
{
    using System;
    using System.Configuration;
    using System.Globalization;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Configuration;
    using Microsoft.ServiceModel.Interop.Properties;
    using Microsoft.ServiceModel.Interop.WebSphere.Configuration;

    /// <summary>
    /// WebSphere WCF binding implementation
    /// </summary>
    public class WebSphereBinding : InteropBinding, IBindingRuntimePreferences
    {
        private WebSphereReliableSession innerReliableSession;
        private WebSphereSecurity innerSecurity;

        /// <summary>
        /// Initializes a new instance of the WebSphereBinding class
        /// </summary>
        public WebSphereBinding()
            : this(WebSphereSecurityMode.UserNameOverCertificate)
        {
        }

        /// <summary>
        /// Initializes a new instance of the WebSphereBinding class
        /// </summary>
        /// <param name="configName">Binding configuration name</param>
        public WebSphereBinding(string configName)
        {
            if (string.IsNullOrEmpty(configName))
            {
                throw new ArgumentNullException("configName");
            }

            this.innerSecurity = new WebSphereSecurity(this.InnerBinding.Security, WebSphereSecurityMode.UserNameOverCertificate);
            this.innerReliableSession = new WebSphereReliableSession(this.InnerBinding.ReliableSession);

            this.ApplyConfiguration(configName);
        }

        /// <summary>
        /// Initializes a new instance of the WebSphereBinding class
        /// </summary>
        /// <param name="securityMode">Security Mode</param>
        public WebSphereBinding(WebSphereSecurityMode securityMode)
            : this(securityMode, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the WebSphereBinding class
        /// </summary>
        /// <param name="securityMode">Security Mode</param>
        /// <param name="reliableSessionEnabled">Specifies whether reliable session is enabled or not</param>
        public WebSphereBinding(WebSphereSecurityMode securityMode, bool reliableSessionEnabled)
            : base(new WS2007HttpBinding(SecurityMode.Message, reliableSessionEnabled))
        {
            this.innerReliableSession = new WebSphereReliableSession(InnerBinding.ReliableSession);
            this.innerSecurity = new WebSphereSecurity(InnerBinding.Security, securityMode);
            this.innerSecurity.EstablishSecurityContext = true;
        }

        /// <summary>
        /// Gets or sets the security binding element
        /// </summary>
        public WebSphereSecurity Security
        {
            get { return this.innerSecurity; }
            set { this.innerSecurity = value; }
        }

        /// <summary>
        /// Gets or sets the reliable session binding element
        /// </summary>
        public WebSphereReliableSession ReliableSession
        {
            get { return this.innerReliableSession; }
            set { this.innerReliableSession = value; }
        }

        private void ApplyConfiguration(string configurationName)
        {
            BindingsSection bindings = (BindingsSection)ConfigurationManager.GetSection("system.serviceModel/bindings");
            WebSphereBindingCollectionElement section = (WebSphereBindingCollectionElement)bindings["webSphereBinding"];
            WebSphereBindingElement element = section.Bindings[configurationName];
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

