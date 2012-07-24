// <copyright file="WebHttpElement3.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Configuration
{
    using System;
    using System.Configuration;
    using System.ServiceModel.Configuration;
    using System.ServiceModel.Web;
    using Microsoft.ServiceModel.Web;

    /// <summary>
    /// Enables the <see cref="Microsoft.ServiceModel.Web.WebHttpBehavior3"/> for an endpoint through configuration.
    /// </summary>
    public sealed class WebHttpElement3 : BehaviorExtensionElement
    {
        private ConfigurationPropertyCollection properties;

        /// <summary>
        /// Initializes a new instance of the <see cref="Microsoft.ServiceModel.Configuration.WebHttpElement3"/> class.
        /// </summary>
        public WebHttpElement3()
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether the message format can be automatically selected.
        /// </summary>
        [ConfigurationProperty(WebConfigurationStrings.AutomaticFormatSelectionEnabled, DefaultValue = true)]
        public bool AutomaticFormatSelectionEnabled
        {
            get { return (bool)base[WebConfigurationStrings.AutomaticFormatSelectionEnabled]; }
            set { base[WebConfigurationStrings.AutomaticFormatSelectionEnabled] = value; }
        }

        /// <summary>
        /// Gets or sets the default message body style.
        /// </summary>
        [ConfigurationProperty(WebConfigurationStrings.DefaultBodyStyle, DefaultValue = WebMessageBodyStyle.Bare)]
        [EnumValidator(typeof(WebMessageBodyStyle))]
        public WebMessageBodyStyle DefaultBodyStyle
        {
            get { return (WebMessageBodyStyle)base[WebConfigurationStrings.DefaultBodyStyle]; }
            set { base[WebConfigurationStrings.DefaultBodyStyle] = value; }
        }

        /// <summary>
        /// Gets or sets the default outgoing response format.
        /// </summary>
        [ConfigurationProperty(WebConfigurationStrings.DefaultOutgoingResponseFormat, DefaultValue = WebMessageFormat.Xml)]
        [EnumValidator(typeof(WebMessageFormat))]
        public WebMessageFormat DefaultOutgoingResponseFormat
        {
            get { return (WebMessageFormat)base[WebConfigurationStrings.DefaultOutgoingResponseFormat]; }
            set { base[WebConfigurationStrings.DefaultOutgoingResponseFormat] = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether a FaultException is generated when an internal server error (HTTP status code: 500) occurs.
        /// </summary>
        [ConfigurationProperty(WebConfigurationStrings.FaultExceptionEnabled, DefaultValue = false)]
        public bool FaultExceptionEnabled
        {
            get { return (bool)base[WebConfigurationStrings.FaultExceptionEnabled]; }
            set { base[WebConfigurationStrings.FaultExceptionEnabled] = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether help is enabled.
        /// </summary>
        [ConfigurationProperty(WebConfigurationStrings.HelpEnabled, DefaultValue = true)]
        public bool HelpEnabled
        {
            get { return (bool)base[WebConfigurationStrings.HelpEnabled]; }
            set { base[WebConfigurationStrings.HelpEnabled] = value; }
        }

        /// <summary>
        /// Gets the type of the behavior enabled by this configuration element.
        /// </summary>
        public override Type BehaviorType
        {
            get { return typeof(WebHttpBehavior3); }
        }

        /// <summary>
        /// Gets the collection of properties.
        /// </summary>
        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                if (this.properties == null)
                {
                    ConfigurationPropertyCollection configProperties = new ConfigurationPropertyCollection();
                    configProperties.Add(new ConfigurationProperty(WebConfigurationStrings.AutomaticFormatSelectionEnabled, typeof(bool), true, null, null, ConfigurationPropertyOptions.None));
                    configProperties.Add(new ConfigurationProperty(WebConfigurationStrings.DefaultBodyStyle, typeof(WebMessageBodyStyle), WebMessageBodyStyle.Bare, null, new EnumValidator(typeof(WebMessageBodyStyle)), ConfigurationPropertyOptions.None));
                    configProperties.Add(new ConfigurationProperty(WebConfigurationStrings.DefaultOutgoingResponseFormat, typeof(WebMessageFormat), WebMessageFormat.Xml, null, new EnumValidator(typeof(WebMessageFormat)), ConfigurationPropertyOptions.None));
                    configProperties.Add(new ConfigurationProperty(WebConfigurationStrings.FaultExceptionEnabled, typeof(bool), false, null, null, ConfigurationPropertyOptions.None));
                    configProperties.Add(new ConfigurationProperty(WebConfigurationStrings.HelpEnabled, typeof(bool), true, null, null, ConfigurationPropertyOptions.None));
                    this.properties = configProperties;
                }

                return this.properties;
            }
        }

        /// <summary>
        /// Creates a behavior extension based on the current configuration settings.
        /// </summary>
        /// <returns>The behavior extension.</returns>
        protected override object CreateBehavior()
        {
            return new WebHttpBehavior3
            {
                AutomaticFormatSelectionEnabled = this.AutomaticFormatSelectionEnabled,
                DefaultBodyStyle = this.DefaultBodyStyle,
                DefaultOutgoingResponseFormat = this.DefaultOutgoingResponseFormat,
                FaultExceptionEnabled = this.FaultExceptionEnabled,
                HelpEnabled = this.HelpEnabled,
            };
        }
    }
}
