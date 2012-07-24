// <copyright file="WebLogicSecurityElement.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.WebLogic.Configuration
{
    using System.Configuration;
    using Microsoft.ServiceModel.Interop.Configuration;

    /// <summary>
    /// WebLogic security configuration element
    /// </summary>
    public class WebLogicSecurityElement : InteropSecurityElement
    {
        private const string ModeProperty = "mode";

        /// <summary>
        /// Initializes a new instance of the WebLogicSecurityElement class
        /// </summary>
        public WebLogicSecurityElement()
        {
        }

        /// <summary>
        /// Gets or sets the security mode
        /// </summary>
        [ConfigurationProperty(ModeProperty, IsRequired = true)]
        public WebLogicSecurityMode Mode
        {
            get { return (WebLogicSecurityMode)base[ModeProperty]; }
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
                properties.Add(new ConfigurationProperty(ModeProperty, typeof(WebLogicSecurityMode), WebLogicSecurityMode.UserNameOverCertificate, ConfigurationPropertyOptions.IsRequired));

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

            WebLogicSecurity webLogicSecurity = (WebLogicSecurity)security;
            webLogicSecurity.Mode = this.Mode;
        }

        /// <summary>
        /// Initializes this configuration element from an existing security binding element
        /// </summary>
        /// <param name="security">Security binding element</param>
        public override void InitializeFrom(InteropSecurity security)
        {
            base.InitializeFrom(security);

            WebLogicSecurity webLogicSecurity = (WebLogicSecurity)security;
            this.Mode = webLogicSecurity.Mode;
        }
    }
}

