// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Configuration
{
    using System;
    using System.Configuration;
    using System.ServiceModel.Configuration;
    using System.ServiceModel.Web;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Http.Description;
    using Microsoft.ApplicationServer.ServiceModel.Configuration;
    using SR = Microsoft.ApplicationServer.Http.SR;

    /// <summary>
    /// Class that provides a behavior element for the <see cref="HttpBinding"/> binding.
    /// </summary>
    public sealed class HttpBehaviorElement : BehaviorExtensionElement
    {
        private static readonly Type httpBehaviorType = typeof(HttpBehavior);
        private static readonly Type processorProviderType = typeof(HttpOperationHandlerFactory);

        private ConfigurationPropertyCollection properties;

        /// <summary>
        /// Gets or sets a value indicating whether the automatic help page will be available.
        /// </summary>
        [ConfigurationProperty(HttpConfigurationStrings.HelpEnabled, DefaultValue = HttpBehavior.DefaultHelpEnabled)]
        public bool HelpEnabled
        {
            get { return (bool)base[HttpConfigurationStrings.HelpEnabled]; }
            set { base[HttpConfigurationStrings.HelpEnabled] = value; }
        }

        /// <summary>
        /// Gets or sets a value specifying how trailing slashes in a <see cref="Uri"/> will be handled.
        /// </summary>
        [ConfigurationProperty(HttpConfigurationStrings.TrailingSlashMode, DefaultValue = HttpBehavior.DefaultTrailingSlashMode)]
        [ServiceModelEnumValidator(typeof(TrailingSlashModeHelper))]
        public TrailingSlashMode TrailingSlashMode
        {
            get { return (TrailingSlashMode)base[HttpConfigurationStrings.TrailingSlashMode]; }
            set { base[HttpConfigurationStrings.TrailingSlashMode] = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="OperationHandlerFactory"/>.
        /// </summary>
        [ConfigurationProperty(HttpConfigurationStrings.OperationHandlerFactory, DefaultValue = "")]
        [StringValidator(MinLength = 0)]
        public string OperationHandlerFactory
        {
            get
            {
                return (string)base[HttpConfigurationStrings.OperationHandlerFactory];
            }

            set
            {
                if (String.IsNullOrWhiteSpace(value))
                {
                    value = String.Empty;
                }

                base[HttpConfigurationStrings.OperationHandlerFactory] = value;
            }
        }

        /// <summary>
        /// Gets the type of this behavior element.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Configuration", "Configuration102:ConfigurationPropertyAttributeRule", Justification = "Not a configurable property; a property that had to be overridden from abstract parent class")]
        public override Type BehaviorType
        {
            get { return httpBehaviorType; }
        }

        /// <summary>
        /// Gets the collection of properties of the current behavior element.
        /// </summary>
        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                if (this.properties == null)
                {
                    ConfigurationPropertyCollection localProperties = new ConfigurationPropertyCollection();
                    localProperties.Add(new ConfigurationProperty(HttpConfigurationStrings.HelpEnabled, typeof(bool), HttpBehavior.DefaultHelpEnabled, null, null, ConfigurationPropertyOptions.None));
                    localProperties.Add(new ConfigurationProperty(HttpConfigurationStrings.TrailingSlashMode, typeof(TrailingSlashMode), HttpBehavior.DefaultTrailingSlashMode, null, new ServiceModelEnumValidator(typeof(TrailingSlashModeHelper)), ConfigurationPropertyOptions.None));
                    localProperties.Add(new ConfigurationProperty(HttpConfigurationStrings.OperationHandlerFactory, typeof(string), string.Empty, null, new System.Configuration.StringValidator(0), System.Configuration.ConfigurationPropertyOptions.None));
                    this.properties = localProperties;
                }

                return this.properties;
            }
        }

        internal static HttpOperationHandlerFactory GetHttpOperationHandlerFactory(string processorProviderTypeString)
        {
            HttpOperationHandlerFactory processorProvider = null;
            if (!string.IsNullOrWhiteSpace(processorProviderTypeString))
            {
                Type type = System.Type.GetType(processorProviderTypeString, true);
                if (!processorProviderType.IsAssignableFrom(type))
                {
                    throw Fx.Exception.AsError(
                        new ConfigurationErrorsException(
                            SR.HttpMessageConfigurationPropertyTypeMismatch(
                                processorProviderTypeString,
                                HttpConfigurationStrings.OperationHandlerFactory,
                                processorProviderType.ToString())));
                }

                processorProvider = (HttpOperationHandlerFactory)Activator.CreateInstance(type);
            }

            return processorProvider;
        }

        /// <summary>
        /// Creates the <see cref="HttpBehavior"/> instance.
        /// </summary>
        /// <returns>A new <see cref="HttpBehavior"/> instance.</returns>
        protected override object CreateBehavior()
        {
            HttpBehavior httpBehavior = new HttpBehavior();

            if (this.IsSet(HttpConfigurationStrings.HelpEnabled))
            {
                httpBehavior.HelpEnabled = this.HelpEnabled;
            }

            if (this.IsSet(HttpConfigurationStrings.TrailingSlashMode))
            {
                httpBehavior.TrailingSlashMode = this.TrailingSlashMode;
            }

            if (this.IsSet(HttpConfigurationStrings.OperationHandlerFactory))
            {
                httpBehavior.OperationHandlerFactory = HttpBehaviorElement.GetHttpOperationHandlerFactory(this.OperationHandlerFactory);
            }

            return httpBehavior;
        }

        private bool IsSet(string propertyName)
        {
            return this.ElementInformation.Properties[propertyName].IsModified;
        }
    }
}
