// <copyright file="MetroSecurityElement.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.Metro.Configuration
{
    using System.Configuration;
    using Microsoft.ServiceModel.Interop.Configuration;

    /// <summary>
    /// Security binding element for Metro
    /// </summary>
    public class MetroSecurityElement : InteropSecurityElement
    {
        private const string ModeProperty = "mode";

        /// <summary>
        /// Initializes a new instance of the MetroSecurityElement class
        /// </summary>
        public MetroSecurityElement()
        {
        }

        /// <summary>
        /// Gets or sets the security mode
        /// </summary>
        [ConfigurationProperty(ModeProperty, IsRequired = true)]
        public MetroSecurityMode Mode
        {
            get { return (MetroSecurityMode)base[ModeProperty]; }
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
                properties.Add(new ConfigurationProperty(ModeProperty, typeof(MetroSecurityMode), MetroSecurityMode.UserNameOverCertificate, ConfigurationPropertyOptions.IsRequired));

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

            MetroSecurity metroSecurity = (MetroSecurity)security;
            metroSecurity.Mode = this.Mode;
        }

        /// <summary>
        /// Initializes this configuration element from an existing security binding element
        /// </summary>
        /// <param name="security">Security binding element</param>
        public override void InitializeFrom(InteropSecurity security)
        {
            base.InitializeFrom(security);

            MetroSecurity metroSecurity = (MetroSecurity)security;
            this.Mode = metroSecurity.Mode;
        }
    }
}

