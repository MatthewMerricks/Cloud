// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Configuration
{
    using System.Configuration;
    using System.ServiceModel.Configuration;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.ServiceModel.Configuration;

    /// <summary>
    /// An XML element that configures the security for a service with endpoints that use the
    /// <see cref="Microsoft.ApplicationServer.Http.HttpBinding">HttpBinding</see>. 
    /// This class cannot be inherited.
    /// </summary>
    public sealed class HttpBindingSecurityElement : ServiceModelConfigurationElement
    {
        private ConfigurationPropertyCollection properties;

        /// <summary>
        /// Gets or sets an XML element that specifies the security mode for a basic HTTP service.
        /// </summary>
        [ConfigurationProperty(ConfigurationStrings.Mode, DefaultValue = HttpBindingSecurity.DefaultMode)]
        [ServiceModelEnumValidator(typeof(HttpBindingSecurityModeHelper))]
        public HttpBindingSecurityMode Mode
        {
            get { return (HttpBindingSecurityMode)base[ConfigurationStrings.Mode]; }
            set { base[ConfigurationStrings.Mode] = value; }
        }
        
        /// <summary>
        /// Gets an XML element that indicates the transport-level security settings 
        /// for a service endpoint configured to receive HTTP requests.
        /// </summary>
        [ConfigurationProperty(ConfigurationStrings.Transport)]
        public HttpTransportSecurityElement Transport
        {
            get { return (HttpTransportSecurityElement)base[ConfigurationStrings.Transport]; }
        }

        /// <summary>
        /// Gets the collection of properties. (Inherited from 
        /// <see cref="System.Configuration.ConfigurationElement">ConfigurationElement</see>.)
        /// </summary>
        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                if (this.properties == null)
                {
                    ConfigurationPropertyCollection newProperties = new ConfigurationPropertyCollection();
                    newProperties.Add(new ConfigurationProperty(ConfigurationStrings.Mode, typeof(HttpBindingSecurityMode), HttpBindingSecurity.DefaultMode, null, new ServiceModelEnumValidator(typeof(HttpBindingSecurityModeHelper)), ConfigurationPropertyOptions.None));
                    newProperties.Add(new ConfigurationProperty(ConfigurationStrings.Transport, typeof(HttpTransportSecurityElement), null, null, null, ConfigurationPropertyOptions.None));
                    this.properties = newProperties;
                }

                return this.properties;
            }
        }

        internal void ApplyConfiguration(HttpBindingSecurity security)
        {
            if (security == null)
            {
                throw Fx.Exception.ArgumentNull("security");
            }

            if (this.ElementInformation.Properties[ConfigurationStrings.Mode].IsModified)
            {
                security.Mode = this.Mode;
                this.Transport.ApplyConfiguration(security.Transport);
            }
        }

        internal void InitializeFrom(HttpBindingSecurity security)
        {
            if (security == null)
            {
                throw Fx.Exception.ArgumentNull("security");
            }

            SetPropertyValueIfNotDefaultValue(ConfigurationStrings.Mode, security.Mode);
            this.Transport.InitializeFrom(security.Transport);
        }
    }
}
