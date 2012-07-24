// <copyright file="WebSphereSecurityElement.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.WebSphere.Configuration
{
    using System.Configuration;
    using Microsoft.ServiceModel.Interop.Configuration;

    /// <summary>
    /// Security binding element for WebSphere
    /// </summary>
    public class WebSphereSecurityElement : InteropSecurityElement
    {
        private const string ModeProperty = "mode";

        /// <summary>
        /// Initializes a new instance of the WebSphereSecurityElement class
        /// </summary>
        public WebSphereSecurityElement()
        {
        }

        /// <summary>
        /// Gets or sets the security mode
        /// </summary>
        [ConfigurationProperty(ModeProperty, IsRequired = true)]
        public WebSphereSecurityMode Mode
        {
            get { return (WebSphereSecurityMode)base[ModeProperty]; }
            set { base[ModeProperty] = value; }
        }

        /// <summary>
        /// Gets all the available configuration properties
        /// </summary>
        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                ConfigurationPropertyCollection properties = base.Properties;
                properties.Add(new ConfigurationProperty(ModeProperty, typeof(WebSphereSecurityMode), WebSphereSecurityMode.UserNameOverCertificate, ConfigurationPropertyOptions.IsRequired));

                return properties;
            }
        }

        /// <summary>
        /// Applies this configuration element to an existing security binding element
        /// </summary>
        /// <param name="security">Security binding element</param>
        public override void ApplyConfiguration(InteropSecurity security)
        {
            base.ApplyConfiguration(security);

            WebSphereSecurity webSphereSecurity = (WebSphereSecurity)security;
            webSphereSecurity.Mode = this.Mode;
        }

        /// <summary>
        /// Initializes this configuration element from an existing security binding element
        /// </summary>
        /// <param name="security">Security binding element</param>
        public override void InitializeFrom(InteropSecurity security)
        {
            base.InitializeFrom(security);

            WebSphereSecurity webSphereSecurity = (WebSphereSecurity)security;
            this.Mode = webSphereSecurity.Mode;
        }
    }
}

